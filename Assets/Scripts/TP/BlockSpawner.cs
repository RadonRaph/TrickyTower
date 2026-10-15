using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// [TP] BlockSpawner : fait apparaître le prochain bloc dans la main du joueur.
///
/// La liste "Blocks" contient vos fiches BlockData (glissez-les dans l'Inspector).
/// Le jeu (TowerGame) appelle SpawnNext() au début de la partie, puis à chaque fois
/// qu'un bloc est posé ou tombé.
///
/// Voir Assets/README_TP.md, étapes 3 et 4.
/// </summary>
public class BlockSpawner : MonoBehaviour
{
    [Tooltip("Les fiches des blocs qui peuvent apparaître")]
    public List<BlockData> blocks = new List<BlockData>();

    [Tooltip("Le point de la manette droite où le bloc apparaît")]
    public Transform hand;

    // Appelée par TowerGame quand il faut un nouveau bloc
    public void SpawnNext()
    {
        // TODO 1 : choisir une fiche au hasard avec la fonction PickRandomBlock (plus bas).
        //          Astuce : on range le résultat d'une fonction dans une variable :
        //          BlockData data = PickRandomBlock();

        // TODO 2 : créer le bloc à partir du prefab de la fiche, à la position et à la rotation de la main.
        //          Astuce : Instantiate(prefab, position, rotation) crée une copie du prefab dans la scène
        //          et la renvoie. La fiche est dans data, son prefab dans data.prefab.
        //          GameObject bloc = Instantiate(data.prefab, hand.position, hand.rotation);

        // TODO 3 : donner au bloc les points de sa fiche.
        //          Chaque prefab de bloc a un composant TowerBlock qui a une variable score.
        //          Astuce : GetComponent<Type>() récupère un composant de l'objet :
        //          bloc.GetComponent<TowerBlock>().score = data.score;

        // TODO 4 : décommentez la ligne ci-dessous (enlevez les //) : elle met le bloc dans la main.
        // GiveToHand(bloc);
    }

    // Renvoie une fiche choisie au hasard dans la liste blocks
    BlockData PickRandomBlock()
    {
        // TODO 5 : version simple, chaque bloc a la même chance.
        //          Tirer un numéro de case au hasard, puis renvoyer la fiche de cette case.
        //          Astuce : Random.Range(0, n) avec deux nombres entiers donne 0, 1, ... ou n-1 (jamais n).
        //                   blocks.Count  = le nombre de fiches dans la liste
        //                   blocks[i]     = la fiche de la case numéro i
        //          int i = Random.Range(0, blocks.Count);
        //          return blocks[i];
        //          (puis supprimez la ligne "return null;" en bas)
        //          -> Testez ! Le jeu doit marcher. Ensuite seulement, passez au TODO 6.

        // TODO 6 : version avec les chances. Remplacez le code du TODO 5 par ceci.
        //          Imaginez une règle : chaque bloc y prend une longueur égale à sa chance.
        //          Bloc_I (chance 10) | Bloc_O (chance 10) | Bloc_Planche (chance 2)
        //          0 ---------------- 10 --------------- 20 ------ 22
        //          On tire un nombre au hasard sur la règle et on regarde dans quel bloc il tombe.
        //
        //          a) Calculer la longueur de la règle : la somme des chances.
        //             float total = 0f;
        //             foreach (BlockData b in blocks)
        //             {
        //                 total = total + b.chance;
        //             }
        //
        //          b) Tirer un nombre à virgule entre 0 et total.
        //             Astuce : avec des float, Random.Range(0f, total) donne un nombre à virgule.
        //             float tirage = Random.Range(0f, total);
        //
        //          c) Parcourir la liste : si le tirage est plus petit que la chance du bloc,
        //             c'est lui ! Sinon on enlève sa chance au tirage et on passe au suivant.
        //             foreach (BlockData b in blocks)
        //             {
        //                 if (tirage < b.chance)
        //                 {
        //                     return b;
        //                 }
        //                 tirage = tirage - b.chance;
        //             }
        //
        //          d) Sécurité (tirage pile égal à total) : renvoyer le dernier bloc de la liste.
        //             return blocks[blocks.Count - 1];

        return null;
    }

    // ------------------------------------------------------------------
    // Ne pas modifier : met le bloc dans la main et prévient le jeu.
    // ------------------------------------------------------------------
    void GiveToHand(GameObject bloc)
    {
        TowerGame game = FindAnyObjectByType<TowerGame>();
        game.OnBlockSpawned(bloc.GetComponent<TowerBlock>(), hand);
    }
}
