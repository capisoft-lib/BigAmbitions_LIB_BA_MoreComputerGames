# Installer et utiliser MCG

## Prérequis

- Big Ambitions pour Windows ; cible actuelle : **1.0 Build 3670**.
- **LIB BA Unified UI 1.0.2+**, installé comme un mod séparé : [dépendance Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3790426259).
- Un paquet MCG compilé. Le dépôt GitHub contient les sources ; suivre [COMPILATION.md](COMPILATION.md) si aucun paquet de release n'est disponible.

MCG fonctionne sans jeu supplémentaire grâce au casse-briques vanilla. FlappyAmbition n'est pas inclus et ComputerGameHighScore n'est pas nécessaire.

## Installation locale Windows

Fermer Big Ambitions avant de remplacer les fichiers d'un mod. Dans l'Explorateur, ouvrir :

```text
%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions\ModsLocal
```

`%USERPROFILE%` désigne le compte Windows du lecteur, aucun nom de compte n'est prédéfini.

Créer **LIB_BA_MoreComputerGames** et y copier le contenu du paquet :

```text
ModsLocal/
└── LIB_BA_MoreComputerGames/
    ├── LIB_BaComputerGames.dll
    ├── ModManifest.asset
    ├── Locales/
    │   ├── en.json
    │   └── fr.json
    ├── Thumbnail.jpg
    ├── README.md
    ├── API.md
    ├── REQUIRED_MODS.md
    ├── VERIFICATION.md
    ├── LICENSE
    └── docs/
```

**Ne pas renommer la DLL** : les jeux compatibles référencent l'assembly `LIB_BaComputerGames`, même si son dossier d'installation porte un autre nom.

Ne pas ajouter de copie de BAUI dans ce dossier. Une installation Workshop de BAUI peut rester dans son répertoire Steam. Déplacer une ancienne installation `ModsLocal/LIB_BaComputerGames` hors de ModsLocal avant d'installer la nouvelle pour éviter deux copies. Retirer le prototype ComputerArcade si son jeu existe déjà sous forme de mod séparé.

## Jouer

1. Relancer Big Ambitions et vérifier l'état actif des mods requis.
2. Charger une partie et sélectionner un ordinateur utilisable.
3. Cliquer sur **Jouer aux jeux vidéo**, puis choisir un jeu dans le catalogue.

MCG laisse le personnage rejoindre l'ordinateur avant de préparer les ressources du jeu choisi. Les contrôles habituels sont clic/espace pour l'action principale, R pour recommencer si le jeu le prévoit, et Échap pour quitter. Chaque jeu décrit ses règles. Les jeux supplémentaires s'installent séparément, sans copier MCG ou BAUI dans leur dossier.

## Records locaux

- Terminer réellement la manche : quitter en cours de partie ne compte pas.
- Seul un score **strictement supérieur** remplace le record. Un score nul ne crée pas de record initial.
- Les records sont séparés par jeu, ruleset et indicateur de règles modifiées.
- Ils sont communs aux sauvegardes du même profil Steam, avec un profil `offline` séparé si Steam est indisponible.
- MCG sauvegarde le record du casse-briques sans modifier son gameplay. Il n'ajoute pas de tableau des records au HUD vanilla ; les jeux ajoutés peuvent afficher leur record via l'API.

Le fichier est créé au premier record, hors du dossier du mod :

```text
%USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions\MoreComputerGames\Records\{SteamId}.json
```

`{SteamId}` désigne le profil courant, pas un identifiant réel fourni par le dépôt. Un fichier `.bak` conserve l'état précédent lors du remplacement. MCG n'envoie rien sur Internet et n'ajoute pas de synchronisation Steam Cloud.

Un fichier corrompu ou incompatible est préservé et signalé dans le log. Ne pas l'effacer sans copie de sauvegarde. Après un changement de compte Steam, recharger le mod pour accéder aux records de l'autre profil.

## Dépannage

| Symptôme | Vérification |
| --- | --- |
| Le bouton lance directement le casse-briques | MCG et BAUI sont-ils chargés ? Le jeu a-t-il été relancé ? Reste-t-il un doublon ? |
| Un jeu manque | Son mod distinct doit être installé et enregistré ; MCG ne recherche pas d'exécutables sur disque. |
| Erreur de chargement de classe ou méthode | Vérifier les versions du jeu, de BAUI et de MCG ; partager uniquement un extrait de log anonymisé. |
| Le record ne change pas | Terminer une manche et dépasser le record du même jeu/ruleset/profil. |
| Stockage indisponible | Vérifier les droits d'écriture et l'avertissement MCG ; conserver le fichier d'origine. |

Une inscription Workshop et un mod effectivement actif ne sont pas la même chose. Voir [VERIFICATION.md](../VERIFICATION.md) pour les limites actuelles des essais.
