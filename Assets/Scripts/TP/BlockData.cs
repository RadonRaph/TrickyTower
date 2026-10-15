using UnityEngine;

// TODO 1 : ajouter l'attribut CreateAssetMenu juste AU-DESSUS de la ligne "public class BlockData".
//          C'est lui qui ajoute "TrickyTower > Bloc" dans le menu clic droit > Create de la fenêtre Project.
//          Astuce : un attribut s'écrit entre crochets, au-dessus de ce qu'il décore :
//          [CreateAssetMenu(menuName = "TrickyTower/Bloc")]

/// <summary>
/// [TP] BlockData : la fiche d'un type de bloc.
///
/// C'est un ScriptableObject : une "fiche de données" rangée dans le projet (un fichier .asset),
/// et PAS un objet de la scène. On écrit la classe une seule fois, puis on crée autant de fiches
/// qu'on veut depuis la fenêtre Project : Bloc_I, Bloc_T, Bloc_Planche...
/// Chaque fiche dit : quel prefab faire apparaître, avec quelle chance, et combien de points il rapporte.
///
/// Voir Assets/README_TP.md, étape 1.
/// </summary>
public class BlockData : ScriptableObject
{
    // TODO 2 : ajouter les 3 réglages de la fiche. Ce sont des variables publiques,
    //          comme dans un MonoBehaviour : elles apparaîtront dans l'Inspector de chaque fiche.
    //
    //          - prefab : le bloc à faire apparaître dans la main        -> type GameObject
    //          - chance : sa chance d'apparaître (10 = courant, 1 = rare) -> type float (nombre à virgule)
    //          - score  : les points gagnés quand il est posé            -> type int (nombre entier)
    //
    //          Astuce : une variable publique s'écrit   public <type> <nom>;
    //          Exemple : public GameObject prefab;
    //          On peut donner une valeur par défaut : public float chance = 10f;
    //
    //          Bonus : une bulle d'aide dans l'Inspector, au-dessus d'une variable :
    //          [Tooltip("Points gagnés quand le bloc est posé")]
}
