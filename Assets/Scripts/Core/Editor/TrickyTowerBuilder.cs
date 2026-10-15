using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.Meta;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

/// <summary>
/// [CORE - Éditeur] Construit le projet TrickyTower :
///  - réglages Quest 3 + passthrough (OpenXR Meta : AR Session + AR Camera),
///  - matériaux et prefabs des blocs (tétrominos, planche, boule, petit cube),
///  - scène TrickyTower_TP (joueur en passthrough, socle, affichage, spawner).
/// Menu TrickyTower > Configuration. Les étudiants n'ont pas besoin de le relancer.
/// </summary>
public static class TrickyTowerBuilder
{
    const string OpenXRLoader = "UnityEngine.XR.OpenXR.OpenXRLoader";
    const string XRSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
    const string XROriginPath = "Assets/Samples/XR Interaction Toolkit/3.6.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
    public const string BlockFolder = "Assets/Prefabs/Blocs";
    const string MaterialFolder = "Assets/Materials";
    const string ScenePath = "Assets/Scenes/TrickyTower_TP.unity";

    // Taille d'un cube de tétromino (m)
    const float Cell = 0.08f;

    [MenuItem("TrickyTower/Configuration/Tout reconstruire (projet + blocs + scène)", false, 200)]
    public static void BuildAll()
    {
        ConfigureProject();
        BuildBlocks();
        BuildScene();
        AssetDatabase.SaveAssets();
        Debug.Log("TrickyTower : projet construit.");
    }

    // Pour la ligne de commande
    public static void BuildAllBatch()
    {
        BuildAll();
        EditorApplication.Exit(0);
    }

    // ---------------------------------------------------------------- Projet

    [MenuItem("TrickyTower/Configuration/Configurer le projet (Quest 3 + passthrough)", false, 201)]
    public static void ConfigureProject()
    {
        PlayerSettings.companyName = "TP VR";
        PlayerSettings.productName = "TrickyTower";
        PlayerSettings.colorSpace = ColorSpace.Linear;
        NamedBuildTarget android = NamedBuildTarget.Android;
        PlayerSettings.SetApplicationIdentifier(android, "com.tpvr.trickytower");
        PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new GraphicsDeviceType[] { GraphicsDeviceType.Vulkan });

        // Physique à la fréquence du casque (72 Hz) : blocs plus stables et plus fluides
        Time.fixedDeltaTime = 1f / 72f;
        Physics.defaultSolverIterations = 12;
        Physics.defaultSolverVelocityIterations = 4;

        // Ombres à l'échelle d'une table : des blocs de 8 cm ont besoin d'ombres fines
        foreach (string guid in AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset", new string[] { "Assets/Settings" }))
        {
            UniversalRenderPipelineAsset urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GUIDToAssetPath(guid));
            urp.shadowDistance = 4f;
            urp.shadowCascadeCount = 1;
            urp.mainLightShadowmapResolution = 2048;
            urp.shadowDepthBias = 0.5f;
            urp.shadowNormalBias = 0.5f;
            EditorUtility.SetDirty(urp);
        }

        // Pas de contour dans ce TP : on retire la Renderer Feature "Silhouette Outline"
        foreach (string guid in AssetDatabase.FindAssets("t:UniversalRendererData", new string[] { "Assets/Settings" }))
        {
            RemoveOutlineFeature(AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid)));
        }

        XRGeneralSettingsPerBuildTarget perTarget;
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out perTarget);
        if (perTarget == null)
        {
            perTarget = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(XRSettingsPath);
        }
        EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, perTarget, true);
        AssignOpenXR(perTarget, BuildTargetGroup.Android);
        AssignOpenXR(perTarget, BuildTargetGroup.Standalone);

        BuildTargetGroup[] groups = { BuildTargetGroup.Android, BuildTargetGroup.Standalone };
        foreach (BuildTargetGroup group in groups)
        {
            OpenXRSettings settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            if (group == BuildTargetGroup.Android)
            {
                EnableFeature<MetaQuestFeature>(settings);
            }
            EnableFeature<OculusTouchControllerProfile>(settings);
            EnableFeature<MetaQuestTouchPlusControllerProfile>(settings);
            // Passthrough : session AR + caméra (le décor réel derrière le jeu)
            EnableFeature<ARSessionFeature>(settings);
            EnableFeature<ARCameraFeature>(settings);
        }
    }

    static void RemoveOutlineFeature(UniversalRendererData renderer)
    {
        SerializedObject serialized = new SerializedObject(renderer);
        SerializedProperty features = serialized.FindProperty("m_RendererFeatures");
        SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
        for (int i = features.arraySize - 1; i >= 0; i--)
        {
            Object feature = features.GetArrayElementAtIndex(i).objectReferenceValue;
            if (feature != null && feature.GetType().Name != "SilhouetteOutlineFeature")
            {
                continue;
            }
            features.DeleteArrayElementAtIndex(i);
            if (i < map.arraySize)
            {
                map.DeleteArrayElementAtIndex(i);
            }
            if (feature != null)
            {
                AssetDatabase.RemoveObjectFromAsset(feature);
                Object.DestroyImmediate(feature, true);
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(renderer);
    }

    static void AssignOpenXR(XRGeneralSettingsPerBuildTarget perTarget, BuildTargetGroup group)
    {
        if (!perTarget.HasManagerSettingsForBuildTarget(group))
        {
            perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
        }
        XRGeneralSettings general = perTarget.SettingsForBuildTarget(group);
        general.InitManagerOnStart = true;
        XRPackageMetadataStore.AssignLoader(general.Manager, OpenXRLoader, group);
        EditorUtility.SetDirty(perTarget);
    }

    static void EnableFeature<T>(OpenXRSettings settings) where T : OpenXRFeature
    {
        T feature = settings != null ? settings.GetFeature<T>() : null;
        if (feature == null)
        {
            Debug.LogError("TrickyTower : feature OpenXR introuvable : " + typeof(T).Name);
            return;
        }
        feature.enabled = true;
        EditorUtility.SetDirty(feature);
    }

    // ---------------------------------------------------------------- Blocs

    [MenuItem("TrickyTower/Configuration/Construire les blocs", false, 202)]
    public static void BuildBlocks()
    {
        CreateFolder("Assets", "Prefabs");
        CreateFolder("Assets/Prefabs", "Blocs");
        CreateFolder("Assets", "Materials");
        CreateFolder("Assets", "Data");
        CreateFolder("Assets/Data", "Blocs");

        // Les 7 tétrominos : cases (x, y) d'une grille, posées à plat dans le plan de la manette
        Tetromino("Bloc_I", new Color(0.2f, 0.85f, 0.95f), new Vector2Int[] { V(0, 0), V(1, 0), V(2, 0), V(3, 0) });
        Tetromino("Bloc_O", new Color(1f, 0.85f, 0.15f), new Vector2Int[] { V(0, 0), V(1, 0), V(0, 1), V(1, 1) });
        Tetromino("Bloc_T", new Color(0.65f, 0.3f, 0.9f), new Vector2Int[] { V(0, 0), V(1, 0), V(2, 0), V(1, 1) });
        Tetromino("Bloc_L", new Color(1f, 0.55f, 0.1f), new Vector2Int[] { V(0, 0), V(1, 0), V(2, 0), V(2, 1) });
        Tetromino("Bloc_J", new Color(0.2f, 0.4f, 0.95f), new Vector2Int[] { V(0, 0), V(1, 0), V(2, 0), V(0, 1) });
        Tetromino("Bloc_S", new Color(0.3f, 0.85f, 0.3f), new Vector2Int[] { V(0, 0), V(1, 0), V(1, 1), V(2, 1) });
        Tetromino("Bloc_Z", new Color(0.95f, 0.25f, 0.25f), new Vector2Int[] { V(1, 0), V(2, 0), V(0, 1), V(1, 1) });

        // Blocs spéciaux, pour jouer avec les chances et les scores
        GameObject plank = BlockRoot("Bloc_Planche");
        Part(plank.transform, PrimitiveType.Cube, Vector3.zero, new Vector3(0.42f, 0.025f, 0.1f), Lit("Bloc_Planche", new Color(0.6f, 0.4f, 0.22f), 0.2f));
        SaveBlock(plank, 0.35f);

        GameObject ball = BlockRoot("Bloc_Boule");
        Part(ball.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.1f, Lit("Bloc_Boule", new Color(1f, 0.45f, 0.7f), 0.7f));
        SaveBlock(ball, 0.3f);

        GameObject gold = BlockRoot("Bloc_PetitCube");
        Part(gold.transform, PrimitiveType.Cube, Vector3.zero, Vector3.one * (Cell - 0.002f), Lit("Bloc_Or", new Color(1f, 0.78f, 0.2f), 0.9f, 1f));
        SaveBlock(gold, 0.1f);

        AssetDatabase.SaveAssets();
    }

    static Vector2Int V(int x, int y)
    {
        return new Vector2Int(x, y);
    }

    static void Tetromino(string name, Color color, Vector2Int[] cells)
    {
        // Centre de la forme : le pivot du bloc
        Vector2 center = Vector2.zero;
        foreach (Vector2Int c in cells)
        {
            center += new Vector2(c.x, c.y);
        }
        center /= cells.Length;

        GameObject root = BlockRoot(name);
        Material material = Lit(name, color, 0.45f);
        for (int i = 0; i < cells.Length; i++)
        {
            Vector3 position = new Vector3(cells[i].x - center.x, cells[i].y - center.y, 0f) * Cell;
            // Un peu plus petit que la case : on voit les joints entre les cubes
            GameObject cube = Part(root.transform, PrimitiveType.Cube, position, Vector3.one * (Cell - 0.003f), material);
            cube.name = "Cube_" + i;
            cube.GetComponent<BoxCollider>().size = Vector3.one * (Cell / (Cell - 0.003f));
        }
        SaveBlock(root, 0.1f * cells.Length);
    }

    static GameObject BlockRoot(string name)
    {
        return new GameObject(name);
    }

    static GameObject Part(Transform parent, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = type.ToString();
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<MeshRenderer>().sharedMaterial = material;
        part.GetComponent<Collider>().sharedMaterial = Friction();
        return part;
    }

    static void SaveBlock(GameObject root, float mass)
    {
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.mass = mass;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.linearDamping = 0.05f;
        body.angularDamping = 0.2f;
        root.AddComponent<TowerBlock>();
        BlockJuice juice = root.AddComponent<BlockJuice>();
        juice.sparkMaterial = Spark();

        PrefabUtility.SaveAsPrefabAsset(root, BlockFolder + "/" + root.name + ".prefab");
        Object.DestroyImmediate(root);
    }

    // ---------------------------------------------------------------- Scène

    [MenuItem("TrickyTower/Configuration/Construire la scène", false, 203)]
    public static void BuildScene()
    {
        CreateFolder("Assets", "Scenes");
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Lumière : pas de skybox (on voit la vraie pièce), une lumière douce venue d'en haut
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.55f, 0.55f, 0.6f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.42f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);
        GameObject sun = new GameObject("Lumiere");
        sun.transform.rotation = Quaternion.Euler(60f, -30f, 0f);
        Light light = sun.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.shadows = LightShadows.Soft;

        new GameObject("XR Interaction Manager").AddComponent<XRInteractionManager>();
        new GameObject("AR Session").AddComponent<ARSession>();

        // Le joueur
        GameObject origin = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(XROriginPath));
        origin.name = "XR Origin (Joueur)";
        Transform head = SetupRig(origin);
        Transform rightController = FindDeep(origin.transform, "Right Controller");
        GameObject handPoint = new GameObject("PointDeMain");
        handPoint.transform.SetParent(rightController, false);
        handPoint.transform.localPosition = new Vector3(0f, 0.01f, 0.12f);

        // Le socle : un pilier posé au sol, avec un plateau
        GameObject socle = new GameObject("Socle");
        Material dark = Lit("Socle_Pilier", new Color(0.18f, 0.19f, 0.22f), 0.3f);
        Material top = Lit("Socle_Plateau", new Color(0.85f, 0.85f, 0.88f), 0.5f);
        Part(socle.transform, PrimitiveType.Cube, new Vector3(0f, 0.39f, 0f), new Vector3(0.2f, 0.78f, 0.2f), dark).name = "Pilier";
        Part(socle.transform, PrimitiveType.Cube, new Vector3(0f, 0.795f, 0f), new Vector3(0.32f, 0.03f, 0.32f), top).name = "Plateau";
        Rigidbody socleBody = socle.AddComponent<Rigidbody>();
        socleBody.isKinematic = true;
        GameObject socleTop = new GameObject("DessusDuSocle");
        socleTop.transform.SetParent(socle.transform, false);
        socleTop.transform.localPosition = new Vector3(0f, 0.81f, 0f);

        // Ligne du record
        GameObject recordLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
        recordLine.name = "LigneRecord";
        Object.DestroyImmediate(recordLine.GetComponent<Collider>());
        recordLine.transform.localScale = new Vector3(0.5f, 0.003f, 0.003f);
        recordLine.GetComponent<MeshRenderer>().sharedMaterial = Lit("LigneRecord", new Color(1f, 0.85f, 0.2f), 0.5f, 0f, 2f);
        recordLine.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        recordLine.transform.SetParent(socle.transform, true);

        // Affichage du score
        GameObject display = new GameObject("Affichage");
        TextMeshPro scoreText = Text(display.transform, "Score", "Score 0", 1.1f, Vector3.zero);
        TextMeshPro messageText = Text(display.transform, "Message", "", 0.5f, new Vector3(0f, -0.18f, 0f));
        messageText.color = new Color(1f, 0.75f, 0.3f);

        // Le spawner (à compléter par les étudiants) et la partie
        BlockSpawner spawner = new GameObject("BlockSpawner").AddComponent<BlockSpawner>();
        spawner.hand = handPoint.transform;

        TowerGame game = new GameObject("TowerGame").AddComponent<TowerGame>();
        game.spawner = spawner;
        game.socle = socle.transform;
        game.socleBody = socleBody;
        game.socleTop = socleTop.transform;
        game.head = head;
        game.scoreText = scoreText;
        game.messageText = messageText;
        game.recordLine = recordLine.transform;
        game.rightHaptics = rightController.GetComponentInChildren<HapticImpulsePlayer>(true);

        socle.transform.position = new Vector3(0f, 0f, 0.6f);
        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    // Joueur en MR : suivi au niveau du sol, caméra transparente (passthrough),
    // pas de déplacement à la manette ni de rayons : on marche dans la vraie pièce.
    static Transform SetupRig(GameObject origin)
    {
        XROrigin xrOrigin = origin.GetComponent<XROrigin>();
        xrOrigin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        xrOrigin.CameraYOffset = 0f;

        foreach (LocomotionProvider provider in origin.GetComponentsInChildren<LocomotionProvider>(true))
        {
            provider.gameObject.SetActive(false);
        }
        foreach (XRBaseInteractor interactor in origin.GetComponentsInChildren<XRBaseInteractor>(true))
        {
            interactor.gameObject.SetActive(false);
        }
        foreach (MonoBehaviour behaviour in origin.GetComponentsInChildren<MonoBehaviour>(true))
        {
            // Gère les rayons et la téléportation au joystick : inutile ici
            if (behaviour.GetType().Name == "ControllerInputActionManager")
            {
                behaviour.enabled = false;
            }
        }

        // Pas de vignette de confort (utile seulement pour les déplacements au joystick),
        // et le joueur ne fait pas d'ombre sur la tour
        foreach (Transform t in origin.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Contains("Vignette"))
            {
                t.gameObject.SetActive(false);
            }
        }
        foreach (Renderer r in origin.GetComponentsInChildren<Renderer>(true))
        {
            r.shadowCastingMode = ShadowCastingMode.Off;
        }

        Camera camera = origin.GetComponentInChildren<Camera>(true);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        camera.nearClipPlane = 0.05f;
        camera.gameObject.AddComponent<ARCameraManager>();
        UniversalAdditionalCameraData cameraData = camera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = false;
        }
        return camera.transform;
    }

    static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform t in parent.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == name)
            {
                return t;
            }
        }
        Debug.LogError("TrickyTower : objet introuvable " + name);
        return parent;
    }

    static TextMeshPro Text(Transform parent, string name, string text, float size, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.rectTransform.sizeDelta = new Vector2(1f, 0.2f);
        // Un texte ne doit pas faire d'ombre (sinon un grand rectangle sombre couvre la tour)
        go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        go.GetComponent<MeshRenderer>().receiveShadows = false;
        return tmp;
    }

    // ---------------------------------------------------------------- Matériaux

    static Material Lit(string name, Color color, float smoothness, float metallic = 0f, float emission = 0f)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", color);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);
        if (emission > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    // Particules et ligne de visée : non éclairé, transparent, couleur des sommets
    static Material Spark()
    {
        string path = MaterialFolder + "/Etincelles.mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }
        material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        material.SetColor("_BaseColor", Color.white);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static PhysicsMaterial Friction()
    {
        string path = MaterialFolder + "/Bloc_Physique.physicMaterial";
        PhysicsMaterial material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (material != null)
        {
            return material;
        }
        material = new PhysicsMaterial("Bloc_Physique");
        material.dynamicFriction = 0.8f;
        material.staticFriction = 0.9f;
        material.bounciness = 0f;
        material.bounceCombine = PhysicsMaterialCombine.Minimum;
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    public static void CreateFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
