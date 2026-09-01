# Créer un jeu compatible MCG

Le jeu est un **mod séparé** qui dépend de MCG. Aucun fork de la bibliothèque ni modification du menu natif n'est nécessaire.

## 1. Préparer son mod

Utiliser le [SDK Big Ambitions](https://github.com/hovgaardgames/bigambitions) et Unity **2022.3.62f2**. Créer un dossier de mod distinct, son manifest, son asmdef et sa classe d'entrée. Donner de nouveaux GUID Unity aux assets du nouveau mod ; conserver ceux de MCG.

Dans l'asmdef du jeu, référencer **LIB_BaComputerGames** ou son GUID existant. Si `overrideReferences` est activé, conserver **BigAmbitions.ModAPI.dll** pour les attributs d'entrée du SDK. Ne pas incorporer les DLL de MCG ou Big Ambitions au paquet distribué.

Pour MCG 1.0.1, recompiler le jeu avec la DLL de la bibliothèque **1.0.1.0**. Le type déclaré par `RegisterModClass` doit dépendre uniquement de BAModAPI : Big Ambitions peut inspecter cet attribut avant de résoudre MCG depuis son élément Workshop séparé. Déplacer les types MCG et l'enregistrement du jeu dans un helper appelé depuis `OnLoad`.

## 2. Enregistrer automatiquement le jeu

```csharp
using System;
using System.Threading.Tasks;
using BAModAPI;
using Capisoft.Lib.BaComputerGames;

[assembly: RegisterModClass(typeof(MyStudio.MyGameMod))]

namespace MyStudio
{
    [ModEntryOnCityLoad]
    public sealed class MyGameMod : IModBigAmbitions
    {
        private IDisposable registration;
        public string[] RelativeAssetBundlePaths => Array.Empty<string>();

        public Task OnLoadAsync(ModContext context)
        {
            registration?.Dispose();
            registration = MyGameRegistration.Register(context);
            return Task.CompletedTask;
        }

        public Task OnUnloadAsync()
        {
            registration?.Dispose();
            registration = null;
            return Task.CompletedTask;
        }
    }

    internal static class MyGameRegistration
    {
        internal static IDisposable Register(ModContext context)
        {
            var definition = ComputerGameDefinition.Create<MyGame>(
                "mystudio:my-game", "Mon jeu", "Description courte",
                version: "1.0.0", ruleset: "standard-v1");
            return ComputerGames.Register(context.ModId, context.ModRootPath, definition);
        }
    }
}
```

`MyGame` est le composant à créer à l'étape suivante. Le type ciblé par `RegisterModClass` dépend uniquement de `BAModAPI` dans ses métadonnées ; le helper résout MCG pendant `OnLoadAsync`, puis l'inscription est retirée au déchargement. Cette séparation est nécessaire pour les éléments Workshop distincts : Mono peut décoder l'attribut avant d'avoir lié la DLL MCG. Choisir un identifiant stable, en minuscules, avec namespace. Les doublons sont refusés ; `vanilla:brick-breaker` est réservé au jeu natif.

## 3. Implémenter le gameplay

La base recommandée est `ComputerGameBehaviour` :

| Membre | Travail du jeu |
| --- | --- |
| `Camera` | Retourner la caméra du mini-jeu, que MCG relie au moniteur. |
| `OnInitialize()` | Construire la scène et son UI sous la racine fournie. |
| `OnTick(frame)` | Utiliser `DeltaSeconds`, `PrimaryPressed`, `RestartPressed` et `CursorViewport`. |
| `OnShutdown()` | Retirer les abonnements et libérer les ressources hors de la hiérarchie du jeu. |

La [référence de l'API](../API.md#exemple-minimal) contient un squelette C# complet de compteur à clics avec caméra. Ce code pédagogique n'ajoute pas un jeu inclus au paquet MCG.

Pour adapter un `MonoBehaviour` existant, implémenter `IComputerGame` : `Initialize`, `Tick`, `Camera`, `SetScreenResolution`, `SetMusicState` et `Shutdown`. La base `ComputerGameBehaviour` protège déjà l'initialisation et l'arrêt contre les doubles appels.

Ne pas avancer le gameplay dans `Update` en parallèle de `Tick`, remplacer la scène native, modifier `Time.timeScale` ou créer un EventSystem concurrent. Parenter les objets à la racine fournie et isoler leur rendu de celui du monde.

## 4. Déclarer les manches et lire les records

```csharp
// Au véritable début de chaque manche :
Context.BeginRound();

// Une fois après victoire/défaite, avant de remettre son score à zéro :
bool completed = Context.CompleteRound(score, level: 1);

// Record standard du jeu/ruleset courant :
long best = Context.HighScore;
```

MCG sauvegarde automatiquement un meilleur score **avant** `ComputerGames.RoundCompleted`. Un deuxième appel de fin pour la même manche retourne `false`. Fermer la session abandonne la manche : **ne pas appeler CompleteRound depuis OnShutdown** pour fabriquer une fin.

Pour une manche modifiée : `Context.CompleteRound(score, level: 1, modifiedRules: true)`. Pour un nouveau barème permanent, utiliser un autre `ruleset`. Un consommateur peut écouter `RoundCompleted` ou `HighScoreChanged`, en se désabonnant au déchargement. Le résultat fournit `RoundId`, `Score`, `HighScore`, `IsNewHighScore` et `HighScoreSaveFailed`.

## 5. Charger ses ressources à la demande

Un jeu procédural n'a pas besoin de loader. Pour un AssetBundle local :

```csharp
ComputerGameDefinition.Create<MyGame>(
    "mystudio:my-game", "Mon jeu", "Description",
    loader: new AssetBundleGameLoader("Data/my-game.bundle"));
```

Construire le bundle avec la même version Unity et pour la plateforme cible. MCG appelle le loader après sélection, conserve les ressources pendant la session et les libère à la fermeture. Les DLL restent chargées dans le processus.

Un loader personnalisé implémente `IComputerGameLoader` et retourne un `ComputerGameAssets` possédant ses ressources. Respecter l'annulation et le thread Unity : [détails](../API.md#ressources-chargées-au-dernier-moment).

## 6. Tester et distribuer

Tester inscription/retrait, chargement, pause, victoire/défaite, redémarrage, abandon et réouverture. Une fin peut améliorer le record ; un abandon ne le modifie pas. Tester aussi la dépendance absente et l'annulation du chargement sans bloquer le jeu hôte.

Le lanceur s'affiche sur le moniteur de l'ordinateur : ↑/↓ et Entrée choisissent le jeu. Retour arrière est réservé à MCG pour revenir au menu ou annuler un chargement ; Tab quitte l'ordinateur. `Context.RequestExit()` ferme le jeu et revient au menu. Échap conserve le menu pause natif, sans interception par MCG. Les jeux doivent laisser Tab/Retour arrière à l'hôte et indiquer « ESC: pause », pas « ESC: quitter ». Vérifier plusieurs lancements successifs, et libérer chaque session sans conserver de caméra ou d'assets d'une partie précédente.

Distribuer uniquement le paquet du nouveau jeu. MCG reste une dépendance séparée et ne nécessite aucune autre bibliothèque de mod. Déclarer [MCG, élément Workshop 3793604724](https://steamcommunity.com/sharedfiles/filedetails/?id=3793604724) dans les Required Items Steam du jeu. Aucune version de FlappyAmbition n'est nécessaire.
