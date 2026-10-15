using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

/// <summary>
/// [CORE] La partie de TrickyTower :
///  - place le socle devant le joueur, demande des blocs au BlockSpawner,
///  - gâchette droite : lâcher le bloc ; bouton A : replacer le socle et recommencer,
///  - compte le score, la hauteur de la tour et les blocs tombés (3 = perdu),
///  - affiche tout ça au-dessus du socle.
/// Pas besoin de le modifier.
/// </summary>
public class TowerGame : MonoBehaviour
{
    [Header("Références")]
    public BlockSpawner spawner;
    [Tooltip("Le socle : on le replace devant le joueur au début de chaque partie")]
    public Transform socle;
    [Tooltip("Le Rigidbody (kinématique) du socle : les premiers blocs s'y lient")]
    public Rigidbody socleBody;
    [Tooltip("Point au centre du dessus du socle : la hauteur est mesurée à partir de là")]
    public Transform socleTop;
    [Tooltip("La caméra du casque")]
    public Transform head;
    public TextMeshPro scoreText;
    public TextMeshPro messageText;
    [Tooltip("Ligne qui marque la hauteur record")]
    public Transform recordLine;
    [Tooltip("Vibration de la manette droite quand un bloc est posé (optionnel)")]
    public HapticImpulsePlayer rightHaptics;

    [Header("Règles")]
    public int lives = 3;
    [Tooltip("Force qui casse une liaison entre deux blocs (plus grand = tour plus solide)")]
    public float linkBreakForce = 800f;
    [Tooltip("Distance socle - joueur au début de la partie (m)")]
    public float socleDistance = 0.6f;

    [HideInInspector] public int score;
    [HideInInspector] public float height;
    [HideInInspector] public float record;
    [HideInInspector] public int fallen;
    [HideInInspector] public TowerBlock current;

    readonly List<TowerBlock> placed = new List<TowerBlock>();
    InputAction releaseAction;
    InputAction restartAction;
    bool gameOver;
    string message = "";

    void OnEnable()
    {
        // Gâchette de la manette droite (ou Espace au clavier)
        releaseAction = new InputAction("Lacher", InputActionType.Button);
        releaseAction.AddBinding("<XRController>{RightHand}/{TriggerButton}");
        releaseAction.AddBinding("<Keyboard>/space");
        releaseAction.Enable();

        // Bouton A de la manette droite (ou R au clavier)
        restartAction = new InputAction("Recommencer", InputActionType.Button);
        restartAction.AddBinding("<XRController>{RightHand}/{PrimaryButton}");
        restartAction.AddBinding("<Keyboard>/r");
        restartAction.Enable();
    }

    void OnDisable()
    {
        releaseAction.Disable();
        restartAction.Disable();
    }

    void Start()
    {
        // On attend que le casque soit suivi avant de placer le socle
        Invoke(nameof(Restart), 0.5f);
    }

    public void Restart()
    {
        CancelInvoke(nameof(NextBlock));
        foreach (TowerBlock block in FindObjectsByType<TowerBlock>(FindObjectsSortMode.None))
        {
            Destroy(block.gameObject);
        }
        placed.Clear();
        current = null;
        score = 0;
        height = 0f;
        record = 0f;
        fallen = 0;
        gameOver = false;
        message = "";
        PlaceSocle();
        NextBlock();
    }

    // Socle posé au sol, devant le joueur, face à lui
    void PlaceSocle()
    {
        if (head == null)
        {
            return;
        }
        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.01f)
        {
            forward = Vector3.forward;
        }
        forward.Normalize();
        Vector3 position = head.position + forward * socleDistance;
        position.y = 0f;
        socle.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
        Physics.SyncTransforms();
    }

    void Update()
    {
        if (restartAction.WasPressedThisFrame())
        {
            Restart();
            return;
        }

        if (releaseAction.WasPressedThisFrame() && current != null && current.state == TowerBlock.State.InHand)
        {
            current.Release();
        }

        // Un bloc posé qui tombe de la tour est perdu aussi
        for (int i = placed.Count - 1; i >= 0; i--)
        {
            if (placed[i] == null)
            {
                placed.RemoveAt(i);
            }
            else if (IsLost(placed[i]))
            {
                OnBlockLost(placed[i]);
            }
        }

        UpdateHeight();
        UpdateTexts();
    }

    // ---------------------------------------------------------------- Appelées par les blocs

    public void OnBlockSpawned(TowerBlock block, Transform hand)
    {
        if (block == null)
        {
            ShowMessage("Le prefab n'a pas de composant TowerBlock.");
            return;
        }
        current = block;
        block.Hold(hand, this);
    }

    public void OnBlockPlaced(TowerBlock block)
    {
        placed.Add(block);
        score = score + block.score;
        if (block == current)
        {
            current = null;
        }
        if (rightHaptics != null)
        {
            rightHaptics.SendHapticImpulse(0.4f, 0.08f);
        }
        Invoke(nameof(NextBlock), 0.3f);
    }

    public void OnBlockLost(TowerBlock block)
    {
        bool wasCurrent = block == current;
        placed.Remove(block);
        Destroy(block.gameObject);
        if (gameOver)
        {
            return;
        }

        fallen = fallen + 1;
        if (fallen >= lives)
        {
            gameOver = true;
            CancelInvoke(nameof(NextBlock));
            if (current != null)
            {
                Destroy(current.gameObject);
                current = null;
            }
            return;
        }
        if (wasCurrent)
        {
            current = null;
            Invoke(nameof(NextBlock), 0.3f);
        }
    }

    // Tombé à plus de 20 cm sous le dessus du socle = perdu
    public bool IsLost(TowerBlock block)
    {
        return block.transform.position.y < socleTop.position.y - 0.2f;
    }

    // ---------------------------------------------------------------- Bloc suivant

    void NextBlock()
    {
        if (gameOver || current != null)
        {
            return;
        }
        message = "";

        if (spawner == null)
        {
            ShowMessage("TowerGame : le champ Spawner est vide.");
            return;
        }
        if (spawner.blocks.Count == 0)
        {
            ShowMessage("La liste Blocks du BlockSpawner est vide :\nglisse tes fiches BlockData dedans.");
            return;
        }

        try
        {
            spawner.SpawnNext();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ShowMessage("Erreur dans BlockSpawner.SpawnNext :\nregarde la Console (fiche vide ? prefab à None ?).");
            return;
        }

        if (current == null)
        {
            ShowMessage("Aucun bloc dans la main :\ntermine les TODO de BlockSpawner.");
        }
    }

    void ShowMessage(string text)
    {
        message = text;
        Debug.LogWarning("TrickyTower : " + text.Replace("\n", " "));
    }

    // ---------------------------------------------------------------- Affichage

    void UpdateHeight()
    {
        height = 0f;
        foreach (TowerBlock block in placed)
        {
            if (block == null)
            {
                continue;
            }
            foreach (Renderer r in block.GetComponentsInChildren<Renderer>())
            {
                height = Mathf.Max(height, r.bounds.max.y - socleTop.position.y);
            }
        }
        record = Mathf.Max(record, height);

        if (recordLine != null)
        {
            recordLine.gameObject.SetActive(record > 0.01f);
            recordLine.position = socleTop.position + Vector3.up * record;
        }
    }

    void UpdateTexts()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = "Score " + score
            + "\n<size=60%>Hauteur " + (height * 100f).ToString("0") + " cm   Record " + (record * 100f).ToString("0") + " cm"
            + "\n<color=#ff6680>Vies " + Mathf.Max(0, lives - fallen) + " / " + lives + "</color></size>";

        string info = message;
        if (gameOver)
        {
            info = "Perdu ! Appuie sur A pour recommencer";
        }
        messageText.text = info;

        // Le texte suit le socle, en hauteur, et se tourne vers le joueur
        Transform textRoot = scoreText.transform.parent;
        textRoot.position = socleTop.position + Vector3.up * (record + 0.35f);
        if (head != null)
        {
            Vector3 toText = Vector3.ProjectOnPlane(textRoot.position - head.position, Vector3.up);
            if (toText.sqrMagnitude > 0.001f)
            {
                textRoot.rotation = Quaternion.LookRotation(toText);
            }
        }
    }
}
