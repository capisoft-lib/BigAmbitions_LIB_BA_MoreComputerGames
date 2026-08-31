# Compiler MCG seul

Ce dépôt est un dossier de mod, pas un projet Unity complet. Aucun fichier de FlappyAmbition, DLL propriétaire ou fichier de configuration personnel n'est nécessaire dans Git.

## Tests sans Unity

Installer le SDK .NET 8, puis lancer depuis la racine du dépôt :

```powershell
dotnet run --project tools/Tests~/MCG.Tests.csproj -c Release
```

Ces tests utilisent les sources réelles du registre, du cycle de manche et du stockage, avec le sérialiseur JSON managé de MCG. Les types Unity sont substitués pour ce test .NET ; le chargement dynamique de la DLL dans un Player Unity est une vérification distincte. Les fichiers de test sont créés dans un répertoire temporaire unique, jamais dans les sauvegardes du joueur. Le dossier `Tests~` est ignoré par l'import Unity.

## Build Player Windows autonome

Prérequis fournis localement par le développeur :

1. Une installation légitime de **Big Ambitions 1.0 Build 3670**, backend Mono, Unity **2022.3.62f2 / 7670c08855a9**.
2. Le même Editor **Unity 2022.3.62f2**, avec ses outils Roslyn et Mono.Cecil.

Aucune bibliothèque de mod supplémentaire n'est nécessaire.

Le script ne télécharge aucune dépendance. Passer les chemins de ses propres installations, par exemple au moyen de variables d'environnement définies localement :

```powershell
# BA_GAME_DIR : dossier contenant UnityPlayer.dll et Big Ambitions_Data.
# UNITY_EDITOR : chemin de l'exécutable Unity.exe correspondant.
powershell -NoProfile -File tools/build.ps1 `
  -GameDirectory $env:BA_GAME_DIR `
  -UnityEditorPath $env:UNITY_EDITOR
```

Les variables doivent être renseignées avant l'appel ; aucun chemin de machine n'est prédéfini dans le dépôt. Les installations du jeu et de Unity restent en lecture seule. Le script crée des copies privées de références Unity adaptées au profil Mono, compile uniquement `Scripts/**/*.cs`, puis produit :

```text
artifacts/build-<identifiant>/
├── LIB_BA_MoreComputerGames/   # Seul ce sous-dossier est un paquet distribuable.
├── private-references/        # Ne jamais publier : dépendances propriétaires.
└── private-build.rsp          # Ne jamais publier : chemins de compilation locaux.
```

Le répertoire `artifacts` entier est ignoré par Git. Le paquet contient uniquement la DLL MCG, les locales, le manifest, la vignette, la licence, le changelog, les textes de release et la documentation. Aucune DLL externe/Unity/Big Ambitions, PDB, log ou réponse de compilateur n'y est copiée. Le build désactive les symboles, remplace les chemins source par un préfixe neutre, vérifie la cohérence des versions du manifest, de l'API et de la DLL, et refuse une référence à une autre bibliothèque de mod.

**Ce script n'installe, ne lance et ne publie rien.** Pour installer manuellement le sous-dossier du paquet, suivre [UTILISATION.md](UTILISATION.md). Ne jamais partager le dossier de build parent.

## Travailler dans le SDK Unity

Pour éditer les assets et développer un consommateur, cloner le dépôt dans `Assets/Mods/LIB_BaComputerGames` du [SDK officiel](https://github.com/hovgaardgames/bigambitions) et conserver les `.meta`. Importer ses propres assemblies du jeu avec les outils du SDK. L'assembly reste `LIB_BaComputerGames` malgré le nom du dossier d'installation.

Les scripts de build/déploiement d'un workspace externe ne font pas partie du contrat de ce dépôt. Utiliser le build autonome ci-dessus pour reproduire le paquet MCG sans un autre mod.
