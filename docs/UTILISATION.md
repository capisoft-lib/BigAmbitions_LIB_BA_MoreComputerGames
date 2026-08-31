# Installer et utiliser MCG 1.0.0

## Prérequis

- Big Ambitions pour Windows ; cible actuelle : **1.0 Build 3670**.
- Un paquet MCG compilé. Le dépôt GitHub contient les sources ; suivre [COMPILATION.md](COMPILATION.md) si aucun paquet de release n'est disponible.

MCG ne nécessite aucune autre bibliothèque de mod et fonctionne sans jeu supplémentaire grâce au casse-briques vanilla. FlappyAmbition n'est pas inclus et ComputerGameHighScore n'est pas nécessaire.

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
    │   ├── fr.json
    │   └── ... (20 autres langues)
    ├── Thumbnail.jpg
    ├── README.md
    ├── API.md
    ├── REQUIRED_MODS.md
    ├── VERIFICATION.md
    ├── CHANGELOG.md
    ├── releases/
    ├── LICENSE
    └── docs/
```

**Ne pas renommer la DLL** : les jeux compatibles référencent l'assembly `LIB_BaComputerGames`, même si son dossier d'installation porte un autre nom.

Ne pas ajouter de DLL de dépendance dans ce dossier. Conserver les bibliothèques dont les autres mods ont besoin dans leurs installations séparées. Déplacer une ancienne installation `ModsLocal/LIB_BaComputerGames` hors de ModsLocal avant d'installer la nouvelle pour éviter deux copies. Retirer le prototype ComputerArcade si son jeu existe déjà sous forme de mod séparé.

## Jouer

L'interface MCG suit la langue choisie dans Big Ambitions et couvre ses **22 langues sélectionnables**, dont le portugais brésilien (`pt`), le chinois simplifié (`zh-cn`) et traditionnel (`zh-tw`). Les jeux supplémentaires fournissent leurs propres traductions de gameplay. L'en-tête « Jeu sélectionné » est actualisé avec les autres libellés si la langue change pendant que le menu du moniteur reste ouvert.

1. Relancer Big Ambitions et vérifier l'état actif des mods requis.
2. Charger une partie et sélectionner un ordinateur utilisable.
3. Cliquer sur **Jouer aux jeux vidéo** : le personnage rejoint l'ordinateur et le menu MCG apparaît sur son moniteur.
4. Utiliser **↑ / ↓** pour sélectionner un jeu, puis **Entrée** pour le charger et jouer. Le casse-briques est toujours proposé, et le record local du jeu sélectionné est affiché.

Les ressources ne sont préparées qu'après validation du choix sur le moniteur. **Retour arrière** annule un chargement ou revient au menu depuis un jeu, sans quitter l'ordinateur. **Tab** quitte l'ordinateur depuis le menu, un chargement ou un jeu. **Échap** conserve le menu pause natif de Big Ambitions ; MCG ne consomme pas cette touche. Les raccourcis MCG sont inactifs pendant le menu pause ou les options, et Tab conserve sa navigation native lorsqu'un contrôle UI a le focus. Un chargement échoué laisse le menu accessible ; Entrée permet de réessayer.

Les contrôles habituels des jeux restent clic/espace pour l'action principale et R pour recommencer si le jeu le prévoit. Chaque jeu décrit ses règles. Les jeux supplémentaires s'installent séparément, sans copier MCG dans leur dossier. Le menu est une image de mini-jeu rendue dans l'écran natif, avec Unity UI. La fermeture du panneau d'action natif et les contrôles de raccourcis du jeu restent en place ; MCG n'efface pas globalement le focus des autres interfaces.

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
| Le bouton lance directement le casse-briques | MCG est-il chargé ? Le jeu a-t-il été relancé ? Reste-t-il un doublon ? |
| Un jeu manque | Son mod distinct doit être installé et enregistré ; MCG ne recherche pas d'exécutables sur disque. |
| Erreur de chargement de classe ou méthode | Vérifier les versions du jeu et de MCG ; partager uniquement un extrait de log anonymisé. |
| Le record ne change pas | Terminer une manche et dépasser le record du même jeu/ruleset/profil. |
| Stockage indisponible | Vérifier les droits d'écriture et l'avertissement MCG ; conserver le fichier d'origine. |

Une inscription Workshop et un mod effectivement actif ne sont pas la même chose. Voir [VERIFICATION.md](../VERIFICATION.md) pour les limites actuelles des essais.
