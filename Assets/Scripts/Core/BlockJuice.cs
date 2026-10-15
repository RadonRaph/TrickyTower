using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// [CORE] Les effets d'un bloc (le "juice") :
///  - il "pop" en apparaissant dans la main,
///  - une ligne de visée descend du bloc tenu jusqu'à la tour,
///  - quand il est posé : flash blanc, étincelles aux liaisons et "+score" qui s'envole.
/// Pas besoin de le modifier.
/// </summary>
public class BlockJuice : MonoBehaviour
{
    [Tooltip("Matériau des étincelles de liaison")]
    public Material sparkMaterial;
    [Tooltip("Couleur du flash et des étincelles")]
    public Color flashColor = Color.white;

    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    TowerBlock block;
    Renderer[] renderers;
    Material[] materials;
    Color[] baseColors;
    LineRenderer aimLine;
    Vector3 baseScale;

    void Awake()
    {
        block = GetComponent<TowerBlock>();
        renderers = GetComponentsInChildren<Renderer>();
        // Une copie du matériau par bloc : le flash ne change que ce bloc
        materials = new Material[renderers.Length];
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            materials[i] = renderers[i].material;
            baseColors[i] = materials[i].GetColor(BaseColor);
        }
        baseScale = transform.localScale;
        CreateAimLine();
    }

    void OnDestroy()
    {
        foreach (Material material in materials)
        {
            Destroy(material);
        }
    }

    void Start()
    {
        StartCoroutine(Pop(0.2f, 1f, 0.25f));
    }

    // ---------------------------------------------------------------- Ligne de visée

    void CreateAimLine()
    {
        GameObject go = new GameObject("LigneDeVisee");
        go.transform.SetParent(transform, false);
        aimLine = go.AddComponent<LineRenderer>();
        aimLine.useWorldSpace = true;
        aimLine.positionCount = 2;
        aimLine.widthMultiplier = 0.004f;
        aimLine.sharedMaterial = sparkMaterial;
        aimLine.startColor = new Color(1f, 1f, 1f, 0.8f);
        aimLine.endColor = new Color(1f, 1f, 1f, 0.1f);
        aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        aimLine.enabled = false;
    }

    void LateUpdate()
    {
        bool held = block != null && block.state == TowerBlock.State.InHand;
        aimLine.enabled = held;
        if (!held)
        {
            return;
        }

        Bounds bounds = GetBounds();
        Vector3 start = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        Vector3 end = start + Vector3.down * 2f;
        RaycastHit[] hits = Physics.RaycastAll(start, Vector3.down, 2f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform) || hit.distance >= best)
            {
                continue;
            }
            best = hit.distance;
            end = hit.point;
        }
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    Bounds GetBounds()
    {
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    // ---------------------------------------------------------------- Pose

    public void OnPlaced(int score, List<Vector3> linkPoints)
    {
        StartCoroutine(Flash(0.35f));
        foreach (Vector3 point in linkPoints)
        {
            Sparks(point);
        }
        FloatingText("+" + score, GetBounds().max.y + 0.05f);
    }

    IEnumerator Pop(float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            // Ease out back : dépasse un peu puis revient
            float s = 1.70158f;
            float eased = 1f + (s + 1f) * Mathf.Pow(k - 1f, 3f) + s * Mathf.Pow(k - 1f, 2f);
            transform.localScale = baseScale * Mathf.LerpUnclamped(from, to, eased);
            yield return null;
        }
        transform.localScale = baseScale * to;
    }

    IEnumerator Flash(float duration)
    {
        // Couleur actuelle (elle a pu être changée depuis l'apparition du bloc)
        for (int i = 0; i < materials.Length; i++)
        {
            baseColors[i] = materials[i].GetColor(BaseColor);
        }
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = 1f - Mathf.Clamp01(t / duration);
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].SetColor(BaseColor, Color.Lerp(baseColors[i], flashColor, k * k));
            }
            yield return null;
        }
        for (int i = 0; i < materials.Length; i++)
        {
            materials[i].SetColor(BaseColor, baseColors[i]);
        }
    }

    // Petite gerbe d'étincelles, détruite toute seule
    void Sparks(Vector3 position)
    {
        GameObject go = new GameObject("Etincelles");
        go.transform.position = position;
        ParticleSystem particles = go.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.3f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.015f);
        main.startColor = flashColor;
        main.gravityModifier = 0.6f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 18) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.01f;

        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        ParticleSystemRenderer particleRenderer = go.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = sparkMaterial;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particles.Play();
    }

    // "+10" qui monte et disparaît
    void FloatingText(string text, float y)
    {
        GameObject go = new GameObject("TextePoints");
        go.transform.position = new Vector3(transform.position.x, y, transform.position.z);
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = 0.6f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.color = new Color(1f, 0.9f, 0.3f);
        tmp.rectTransform.sizeDelta = new Vector2(0.4f, 0.1f);
        MeshRenderer textRenderer = go.GetComponent<MeshRenderer>();
        textRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        textRenderer.receiveShadows = false;
        go.AddComponent<FloatingTextMotion>();
    }
}

/// <summary>
/// [CORE] Fait monter un texte, le tourne vers le joueur puis le détruit.
/// </summary>
public class FloatingTextMotion : MonoBehaviour
{
    const float Duration = 0.9f;
    float time;
    TextMeshPro tmp;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
    }

    void Update()
    {
        time += Time.deltaTime;
        transform.position += Vector3.up * 0.15f * Time.deltaTime;
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
        Color color = tmp.color;
        color.a = 1f - Mathf.Clamp01((time - Duration * 0.5f) / (Duration * 0.5f));
        tmp.color = color;
        if (time > Duration)
        {
            Destroy(gameObject);
        }
    }
}
