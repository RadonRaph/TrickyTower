# TP TrickyTower – Les ScriptableObjects

## Le projet

Un petit jeu en **réalité mixte** pour le **Meta Quest 3** : un bloc apparaît dans votre main droite, vous le posez sur le socle. Les blocs se collent entre eux. Le but : **la tour la plus haute possible** !

- **Gâchette droite** : lâcher le bloc.
- **Bouton A** : replacer le socle devant vous et recommencer.
- 3 blocs tombés = perdu.

Le jeu est déjà fait, sauf une chose : **les blocs**. C'est votre travail, et c'est l'occasion d'apprendre les **ScriptableObjects**. Durée : moins d'une heure.

## Avant de commencer

- Version de Unity : **6000.3.24f1**, avec le module **Android Build Support**.
- Le projet est déjà configuré pour le Quest 3 (OpenXR, passthrough, manettes). Vous n'avez rien à régler.
- Ouvrez la scène `Assets/Scenes/TrickyTower_TP.unity`.

### Tester avec le casque

- **Build sur le casque** (recommandé) : casque branché en USB (mode développeur activé), `File` → `Build Profiles` → `Android` → `Build And Run`.
- **Quest Link** (PC Windows) : branchez le casque, activez Link, puis lancez Play dans Unity. Si le fond est noir au lieu de votre pièce, ce n'est pas grave : le jeu marche quand même. (Pour voir la pièce avec Link : application Meta Horizon Link → `Paramètres` → `Bêta` → activer `Fonctionnalités d'exécution pour les développeurs` et `Passthrough sur Meta Quest Link`.)
- Au clavier, dans Unity : **Espace** = lâcher, **R** = recommencer.

## Organisation du projet

| Dossier | Contenu |
|---|---|
| `Scripts/TP/` | **Les 2 scripts à compléter** : `BlockData` et `BlockSpawner`. |
| `Scripts/Core/` | Le reste du jeu (pas besoin de le modifier) : `TowerGame` (la partie, le score), `TowerBlock` (un bloc qui tombe et se colle), `BlockJuice` (les effets). |
| `Prefabs/Blocs/` | Les 10 blocs : 7 formes de Tetris (`Bloc_I`, `Bloc_O`, `Bloc_T`...) et 3 spéciaux (`Bloc_Planche`, `Bloc_Boule`, `Bloc_PetitCube`). |
| `Data/Blocs/` | Vide : c'est ici que vous rangerez vos fiches de blocs. |

Cherchez `TODO` dans le dossier `Scripts/TP` pour trouver le code à écrire.

---

## C'est quoi un ScriptableObject ?

Un **ScriptableObject** est une **fiche de données** rangée dans le projet, comme un fichier. Ce n'est pas un objet de la scène.

On écrit **une seule classe** (le modèle de la fiche), puis on crée **autant de fiches qu'on veut** depuis la fenêtre Project, sans écrire de code :

```
classe BlockData  ──►  Bloc_I.asset       prefab = Bloc_I        chance = 10   score = 10
 (le modèle)      ──►  Bloc_T.asset       prefab = Bloc_T        chance = 10   score = 10
                  ──►  Bloc_Boule.asset   prefab = Bloc_Boule    chance = 2    score = 50
```

| | MonoBehaviour | ScriptableObject |
|---|---|---|
| Où il vit | sur un GameObject, dans la scène | dans le projet, comme un fichier `.asset` |
| Comment on le crée | `Add Component` | clic droit → `Create` |
| À quoi il sert | un comportement (bouger, réagir...) | des données partagées (réglages, catalogue...) |

L'intérêt : pour ajouter un bloc ou changer ses points, on modifie une fiche, **pas le code**. Un game designer peut équilibrer le jeu sans programmer.

### Mémo C#

| Je veux... | J'écris | Exemple |
|---|---|---|
| Une variable réglable dans l'Inspector | `public <type> <nom>;` | `public int score;` |
| Une liste de fiches | `List<Type>` | `public List<BlockData> blocks;` |
| Le nombre d'éléments d'une liste | `.Count` | `blocks.Count` |
| L'élément numéro i d'une liste | `liste[i]` | `blocks[0]` (le premier) |
| Parcourir toute une liste | `foreach` | `foreach (BlockData b in blocks) { ... }` |
| Un nombre entier au hasard | `Random.Range(int, int)` | `Random.Range(0, 4)` donne 0, 1, 2 ou 3 |
| Un nombre à virgule au hasard | `Random.Range(float, float)` | `Random.Range(0f, 22f)` donne 13.7... |
| Renvoyer le résultat d'une fonction | `return` | `return blocks[i];` |
| Créer un objet à partir d'un prefab | `Instantiate(prefab, position, rotation)` | `Instantiate(data.prefab, hand.position, hand.rotation)` |
| Récupérer un composant | `GetComponent<Type>()` | `bloc.GetComponent<TowerBlock>()` |
| Écrire dans la Console | `Debug.Log(...)` | `Debug.Log("Bloc : " + data.name);` |

---

## Étape 1 : La fiche de bloc (`BlockData`)

Ouvrez `Scripts/TP/BlockData.cs` et faites les **TODO 1 et 2** :

1. Ajoutez l'attribut `[CreateAssetMenu(...)]` : il ajoute la fiche au menu `Create`.
2. Ajoutez les 3 variables de la fiche : `prefab`, `chance`, `score`.

Enregistrez, revenez dans Unity et attendez la fin de la compilation (pas d'erreur rouge dans la Console).

## Étape 2 : Créer les fiches

1. Dans la fenêtre Project, ouvrez le dossier `Data/Blocs`.
2. Clic droit → `Create` → `TrickyTower` → `Bloc`. Nommez la fiche `Bloc_I`.
3. Dans l'Inspector de la fiche : glissez le prefab `Prefabs/Blocs/Bloc_I` dans `Prefab`, réglez `Chance` et `Score`.
4. Recommencez pour **au moins 4 blocs** (astuce : sélectionnez une fiche et faites `Ctrl+D` / `Cmd+D` pour la dupliquer).
5. Dans la scène, sélectionnez `BlockSpawner` et glissez vos fiches dans la liste `Blocks`.

> Pour l'instant, rien n'apparaît encore dans la main : c'est l'étape 3.

## Étape 3 : Faire apparaître les blocs (`BlockSpawner`)

Ouvrez `Scripts/TP/BlockSpawner.cs` :

1. **TODO 1 à 4** dans `SpawnNext` : choisir une fiche, créer son prefab dans la main, lui donner son score.
2. **TODO 5** dans `PickRandomBlock` : le tirage simple, chaque bloc a la même chance.

**Testez !** Un bloc doit apparaître dans votre main droite. Lâchez-le sur le socle : il se colle, des étincelles, `+10` s'envole, un nouveau bloc arrive.

## Étape 4 : Les chances

**TODO 6** : remplacez le tirage simple par un tirage qui tient compte de `chance`. Les explications sont dans les commentaires.

Pour vérifier : mettez `Chance = 100` à un bloc et `Chance = 1` aux autres. Vous ne devez presque plus voir que lui.

---

## Bonus

- **Équilibrer le jeu** : la boule roule, la planche est large... Donnez à chaque bloc une chance et un score qui vont bien ensemble (un bloc rare et difficile rapporte gros).
- **Un nouveau bloc sans toucher au code** : dupliquez un prefab de `Prefabs/Blocs`, changez sa forme (ajoutez/déplacez des cubes) ou sa couleur, créez sa fiche, ajoutez-la à la liste. C'est tout l'intérêt des ScriptableObjects !
- **Une info de plus dans la fiche** : ajoutez une variable `public Color couleur;` à `BlockData` et appliquez-la au bloc créé dans `SpawnNext`.
  Astuce : un bloc a plusieurs cubes, donc plusieurs Renderer :
  `foreach (Renderer r in bloc.GetComponentsInChildren<Renderer>()) { r.material.color = data.couleur; }`
- **Plusieurs listes** : deux `BlockSpawner` avec des listes différentes (mode facile : que des `Bloc_O` ; mode difficile : des boules).

---

## Ça ne marche pas ?

Regardez d'abord le **message orange** sous le score, au-dessus du socle : il dit souvent ce qui manque.

### Une erreur rouge dans la Console

- **On ne peut pas lancer Play tant qu'il reste une erreur rouge.** Double-cliquez sur l'erreur : elle vous emmène à la ligne du problème.
- `error CS1513: } expected` : il manque une accolade `}`. Chaque `{` doit avoir sa `}`.
- `error CS1002: ; expected` : il manque un `;` à la fin d'une ligne.
- `error CS0103: The name 'bloc' does not exist` : la variable est utilisée avant d'être créée (vérifiez l'ordre des TODO), ou il y a une faute de frappe.
- `error CS1061: 'BlockData' does not contain a definition for 'prefab'` : la variable n'existe pas dans `BlockData` (étape 1), ou elle est écrite autrement (majuscules comprises).

### Le menu `Create` → `TrickyTower` n'existe pas

- L'attribut `[CreateAssetMenu(...)]` est-il bien juste au-dessus de `public class BlockData` ?
- Y a-t-il une erreur rouge dans la Console ? Tant qu'il y en a une, Unity ne prend pas en compte vos changements.

### `NullReferenceException`

Ça veut dire : **« tu utilises quelque chose qui est vide »**.

- Une case de la liste `Blocks` est à `None` ? Une fiche a son `Prefab` à `None` ?
- `PickRandomBlock` renvoie encore `null` ? (avez-vous fait le TODO 5 et supprimé `return null;` ?)

### Aucun bloc n'apparaît dans la main

- Message « liste vide » : glissez vos fiches dans `Blocks` (sélectionnez l'objet `BlockSpawner` dans la scène).
- Message « termine les TODO » : avez-vous décommenté `GiveToHand(bloc);` (TODO 4) ?

### Le bloc tombe tout le temps

- Posez-le doucement, à plat, juste au-dessus de la tour, puis appuyez sur la gâchette. La ligne blanche sous le bloc montre où il va tomber.
- Le socle est trop loin ou trop près ? Bouton **A** pour le replacer devant vous.
