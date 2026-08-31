# Créer un jeu avec More Computer Games (MCG)

Namespace public : **Capisoft.Lib.BaComputerGames**. Assembly : **LIB_BaComputerGames.dll**. API **1.0.0**, avec les signatures publiques de la 0.2.0 conservées. Le nom de l'assembly, l'identifiant technique du mod et le schéma des records restent inchangés.

## Contrats

| Type | Responsabilité |
| --- | --- |
| ComputerGameDefinition | Identifiant unique, titre, description, version, règles, traductions, factory et chargeur optionnel. |
| ComputerGameMod&lt;TGame&gt; | Point d'entrée de mod qui enregistre automatiquement la définition au chargement, puis retire son inscription au déchargement. |
| ComputerGameBehaviour | Base recommandée : caméra, OnInitialize, OnTick, OnShutdown. L'initialisation et l'arrêt sont protégés contre les appels multiples. |
| IComputerGame | Alternative : implémenter directement l'interface sur un MonoBehaviour existant. |
| ComputerGameContext | Ressources préparées, répertoire du mod, traduction, sortie et événements de manche. |
| ComputerGameFrame | Delta indépendant du temps global, action principale, redémarrage, position normalisée du curseur sur le moniteur. |
| IComputerGameLoader | Prépare les ressources uniquement après sélection. |
| ComputerGameAssets | Possède les ressources et les libère dans Dispose. |
| AssetBundleGameLoader | Chargeur local fourni pour un AssetBundle Unity, avec annulation et libération. |
| ComputerGameRegistration | Jeton IDisposable pour une inscription manuelle. |

## Exemple minimal

Dans un mod SDK distinct, référencer LIB_BaComputerGames par son nom d'assembly ou son GUID dans l'asmdef. Conserver BigAmbitions.ModAPI comme référence précompilée pour les attributs du SDK. Ne pas référencer les classes internes du jeu ou BAUI depuis le gameplay. Voir aussi le [guide développeur](docs/CREER_UN_JEU.md).

~~~csharp
using BAModAPI;
using Capisoft.Lib.BaComputerGames;
using UnityEngine;

[assembly: RegisterModClass(typeof(MyStudio.MyGameMod))]

namespace MyStudio
{
    [ModEntryOnCityLoad]
    public sealed class MyGameMod : ComputerGameMod<MyGame>
    {
        protected override ComputerGameDefinition Definition =>
            ComputerGameDefinition.Create<MyGame>(
                "mystudio:my-game", "Mon jeu", "Une courte description",
                version: "0.1.0", ruleset: "standard-v1");
    }

    public sealed class MyGame : ComputerGameBehaviour
    {
        private Camera gameCamera;
        private int score;
        public override Camera Camera => gameCamera;

        protected override void OnInitialize()
        {
            var obj = new GameObject("Camera", typeof(Camera));
            obj.transform.SetParent(transform, false);
            gameCamera = obj.GetComponent<Camera>();
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = Color.blue;
            gameCamera.cullingMask = 0; // Ce squelette n'affiche qu'un fond.
            Context.BeginRound();
        }

        protected override void OnTick(ComputerGameFrame frame)
        {
            if (frame.PrimaryPressed) score++;
            if (frame.RestartPressed)
            {
                Context.CompleteRound(score);
                score = 0;
                Context.BeginRound();
            }
        }

        protected override void OnShutdown()
        {
            if (gameCamera != null) gameCamera.targetTexture = null;
        }
    }
}
~~~

Les attributs d'entrée restent nécessaires : c'est le SDK officiel qui choisit les mods activés. La bibliothèque ne scanne pas les assemblies pour exécuter du code arbitrairement.

Un seul identifiant lowercase avec un namespace, par exemple **mystudio:my-game**, par jeu. Le nom affiché peut changer sans changer cet identifiant. Réserver un nouveau ruleset si les règles de score changent.

## Cycle de vie et règles Unity

1. La classe de mod décrit le jeu. Son constructeur, sa propriété Definition et le constructeur du loader doivent rester légers : pas d'objets Unity, pas de lecture de gros fichiers.
2. Le joueur rejoint l'ordinateur ; le lanceur MCG affiche le catalogue sur le moniteur. Aucune ressource de jeu n'est encore chargée.
3. Après validation par Entrée, le loader éventuel est appelé sur le thread Unity. Un écran de chargement reste visible dans le moniteur pendant cette opération.
4. La bibliothèque crée un GameObject enfant du conteneur natif et appelle la factory. Celle-ci doit attacher un MonoBehaviour implémentant IComputerGame à ce GameObject précis.
5. Initialize reçoit le contexte ; Camera doit ensuite fournir une caméra valide. OnInitialize est la bonne place pour créer la scène du mini-jeu.
6. La bibliothèque configure la résolution et la musique, puis appelle Tick. ComputerGameBehaviour distribue vers OnTick. Le frame utilise un delta non mis à l'échelle, plafonné à 0,25 seconde, et s'interrompt quand l'application perd le focus ou que les options sont visibles.
7. À la sortie : Shutdown/OnShutdown, arrêt des callbacks propres au jeu, puis Dispose des ressources. La hiérarchie Addressables est libérée par l'hôte.

Les contrôles standard sont espace/clic pour l'action principale et R pour redémarrer. Les premières entrées sont neutralisées brièvement pour ne pas réutiliser l'entrée de sélection. **Échap reste le menu pause natif de Big Ambitions**, sans interception par MCG. **Retour arrière est réservé au lanceur** : il ferme le jeu courant et revient au menu, ou annule le chargement. **Tab sans modificateur quitte l'ordinateur**, sauf si un contrôle natif a le focus ; les raccourcis MCG et les Tick sont suspendus pendant le menu pause ou les options. Les flèches et Entrée sont réservées au menu uniquement ; elles restent disponibles dans le gameplay.

Parenter tous les objets de gameplay à la racine fournie, avec positions locales. Le conteneur natif peut être très loin de l'origine du monde. Ne pas créer de caméra plein écran indépendante, ne pas remplacer la scène de Big Ambitions, son EventSystem ou son état global, et ne pas modifier Time.timeScale. Pour une UI de jeu, utiliser un Canvas WorldSpace/ScreenSpaceCamera adapté au moniteur, pas ScreenSpaceOverlay. Le monde doit être isolé par les couches et le cullingMask de la caméra.

Ne pas utiliser Update pour le gameplay : il contournerait la pause des contrôles gérée par la bibliothèque. Libérer abonnements, coroutines et ressources propres dans OnShutdown. Les ressources Unity indépendantes de la hiérarchie doivent avoir un propriétaire explicite.

Une fermeture abandonne la manche active sans fabriquer de score. Context.RequestExit ferme la session de ce jeu ; le lanceur revient au menu lors de sa prochaine mise à jour, sans terminer l'activité native sur l'ordinateur. Le jeu sera recréé au prochain lancement. Toutes les API de registre, de contexte et les opérations Unity se font sur le thread principal. Les signatures publiques restent compatibles : les mods de jeu existants n'ont pas besoin de se réenregistrer autrement.

Avec IComputerGame directement, implémenter Camera, Initialize, Tick, SetScreenResolution, SetMusicState et Shutdown. Le composant reste un MonoBehaviour ; conserver soi-même le contexte reçu et rendre Shutdown idempotent. ComputerGameBehaviour convient à la plupart des nouveaux jeux.

### Filtre rétro natif

Par défaut, les jeux conservent le filtre rétro du moniteur de Big Ambitions. Pour un affichage sans ce filtre, terminer la définition par **.WithNativeRetroEffects(false)**, comme FlappyAmbition. Cette méthode retourne une nouvelle définition, sans modifier celle d'origine.

MCG applique ce choix seulement après la configuration de l'écran, en copiant le profil de volume de la session. L'exposition et les autres paramètres sont conservés ; les profils partagés et la caméra principale ne sont pas modifiés. La copie est libérée à la fermeture. Si ce point d'intégration optionnel n'est plus disponible dans une future version du jeu, le chargement continue avec les effets natifs et un avertissement dans les logs.

## Ressources chargées au dernier moment

Pour un jeu procédural comme FlappyAmbition, ne rien fournir comme loader.

Pour un AssetBundle local :

~~~csharp
ComputerGameDefinition.Create<MyGame>(
    "mystudio:my-game", "Mon jeu", "Description",
    loader: new AssetBundleGameLoader("Data/my-game.bundle"));
~~~

Puis, dans OnInitialize :

~~~csharp
var assets = (AssetBundleGameAssets)Context.Assets;
var prefab = assets.Bundle.LoadAsset<GameObject>("GameScene");
var scene = Object.Instantiate(prefab, transform, false);
// Récupérer la caméra dans scene ; elle sera branchée au moniteur par l'hôte.
~~~

Produire le bundle avec Unity **2022.3.62f2**, pour la plateforme cible. Le placer dans Data du package. Les chemins absolus et les sorties hors du dossier du mod sont refusés. Donner des noms de bundles uniques à chaque jeu.

La classe de base retourne RelativeAssetBundlePaths vide : ne pas y ajouter ces bundles, sinon le SDK les préchargerait au démarrage.

AssetBundleGameLoader conserve un bail jusqu'à la fermeture de la session, afin d'éviter deux chargements simultanés du même bundle par ce loader. Une annulation n'interrompt pas l'E/S native Unity déjà commencée ; son résultat est attendu puis déchargé. Une nouvelle demande attend la libération du bail.

Pour plusieurs bundles, des fichiers audio ou un format custom, fournir IComputerGameLoader et une classe ComputerGameAssets. Respecter le CancellationToken. Si un loader non coopératif rend ses ressources après annulation, la bibliothèque appelle tout de même Dispose. Si le loader échoue avant de retourner son résultat, il doit nettoyer lui-même ses allocations partielles.

Ne pas faire de ConfigureAwait(false) autour des opérations Unity. Un calcul CPU peut être déporté explicitement, mais la création/destruction des objets et le retour au moteur restent sur le thread Unity. Aucun loader réseau n'est fourni.

## Plusieurs jeux dans un mod existant

Un mod qui a déjà son point d'entrée peut utiliser les inscriptions manuelles :

~~~csharp
private ComputerGameRegistration registration;

// Dans OnLoadAsync :
registration = ComputerGames.Register(context.ModId, context.ModRootPath,
    ComputerGameDefinition.Create<MyGame>("mystudio:my-game", "Mon jeu", "Description"));

// Dans OnUnloadAsync :
registration?.Dispose();
registration = null;
~~~

Garder un jeton par jeu. Catalog est une liste de métadonnées en lecture seule et CatalogChanged signale ses modifications. Une inscription réussit même si le mod hôte n'a pas encore chargé ; le lancement exige un hôte actif. Un doublon lève InvalidOperationException sans remplacer le jeu existant.

ActivateHost, DeactivateHost, PrepareAsync, ComputerGameSession et ComputerGameProvider servent à l'hôte ou aux tests d'intégration. Les mods de contenu ordinaires ne doivent pas les appeler : choisir via le catalogue laisse la bibliothèque gérer l'activité native et ses ressources.

## Événement de fin de manche

Appeler Context.BeginRound au départ puis Context.CompleteRound(score, level) une seule fois après une fin effective. Le second appel pour la même manche est ignoré. Score et level sont des entiers non négatifs.

Un adaptateur peut s'abonner à ComputerGames.RoundCompleted. ComputerGameResult contient GameId, GameVersion, Ruleset, Score, Level, StartedAtUtc, EndedAtUtc, ActiveSeconds et ElapsedSeconds. ActiveSeconds représente le temps de simulation reçu ; ElapsedSeconds est la durée monotone réelle. Des tests accélérés peuvent donc les faire diverger.

Le signal est local, sans identité Steam, sans réseau et sans mécanisme anti-triche. Un classement doit ajouter sa propre politique de consentement, ses règles et sa validation. Les erreurs d'un abonné n'empêchent pas les autres abonnés de recevoir le résultat ; elles sont remontées via ComputerGames.Error.

### Records locaux unifiés (depuis 0.2.0)

MCG traite le record **avant** de diffuser RoundCompleted. Aucune sauvegarde supplémentaire n'est nécessaire dans un jeu qui appelle déjà BeginRound / CompleteRound, comme FlappyAmbition. Quitter, décharger ou abandonner une manche ne produit aucun résultat. Un score égal ou inférieur au record produit toujours RoundCompleted, mais aucune écriture du fichier.

~~~csharp
long best = Context.HighScore; // Record standard du jeu/ruleset courant, zéro en l'absence de record.
ComputerGames.RoundCompleted += result => {
    // RoundId unique, IsNewHighScore, HighScore et HighScoreSaveFailed sont aussi disponibles.
    // Le nouveau record est déjà lisible ici, sauf en cas d'échec d'écriture signalé.
};
ComputerGames.HighScoreChanged += record => { /* Seulement un record effectivement enregistré. */ };
long vanillaBest = ComputerGames.GetHighScore(
    ComputerGames.VanillaBrickBreakerId, ComputerGames.VanillaBrickBreakerRuleset);
~~~

TryGetHighScore(gameId, ruleset, out record, modifiedRules: false) fournit aussi GameVersion, Level et AchievedAtUtc. LocalRecordsAvailable indique si le stockage du profil est accessible. Le record est séparé par gameId, ruleset et ModifiedRules. CompleteRound(score, level, modifiedRules: true) signale une manche modifiée et conserve son record séparément du standard. L'ancien appel à deux arguments reste compatible. Seuls les scores strictement supérieurs au record initial zéro créent une entrée.

L'adaptateur vanilla publie le même ComputerGameResult sous l'identifiant réservé **vanilla:brick-breaker**, ruleset **ba-1.0-standard**. Il observe d'abord le menu, puis le début de partie et la perte de la dernière vie, additionne les points en attente, et évite les doublons/reprises en cours de manche. IsVanillaScoreTrackingActive permet à un autre mod de désactiver sa propre capture. Le jeu vanilla ne fournit pas lui-même cet événement.

Stockage : Application.persistentDataPath/MoreComputerGames/Records/{SteamId}.json, avec profil **offline** séparé si Steam est indisponible. Les records sont communs aux sauvegardes du même profil, hors ModsLocal, sans modification des sauvegardes natives ni synchronisation Steam Cloud ajoutée par MCG. Écriture atomique, ancien fichier .bak conservé. Un fichier corrompu/incompatible est préservé et le stockage est désactivé pour ce chargement ; le gameplay et RoundCompleted restent disponibles. Un changement de compte en cours de ville bloque les accès jusqu'au prochain chargement du mod.

MCG fonctionne seul pour les records locaux, sans dépendance à ComputerGameHighScore. L'événement reste disponible pour de futurs consommateurs, sans réseau ni identité de profil. Aucune migration ou transmission d'une ancienne file d'envoi n'est effectuée.

## Adapter un jeu open source

Réutiliser sa logique et ses ressources selon sa licence, puis écrire cet adaptateur de caméra/cycle de vie. Un jeu Unity existant peut devenir un prefab/bundle sous la racine fournie. Un jeu HTML/JavaScript, Godot ou un exécutable autonome ne s'intègre pas automatiquement : il faudrait un autre moteur embarqué et une intégration dédiée. Cette bibliothèque n'est pas un lanceur universel d'exécutables.
