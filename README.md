# LIB BA More Computer Games — preview 0.2.0

Bibliothèque commune pour ajouter des mini-jeux aux ordinateurs de Big Ambitions.

![Illustration promotionnelle de More Computer Games](Thumbnail.jpg)

**Ce dépôt contient uniquement MCG.** FlappyAmbition est un mod séparé : ses sources, ressources, tests et binaires ne sont pas inclus. Le casse-briques fourni par Big Ambitions reste disponible sans ajouter de jeu.

## Documentation

- [Installer et utiliser MCG](docs/UTILISATION.md).
- [Créer un jeu compatible](docs/CREER_UN_JEU.md), puis [référence complète de l'API](API.md).
- [Compiler et vérifier MCG](docs/COMPILATION.md).
- [Confidentialité des sources et paquets](docs/CONFIDENTIALITE.md).

Titre du mod sur Steam : **LIB BA More Computer Games**, également conservé dans **release-assets/WORKSHOP_TITLE.txt** pour le formulaire de publication. Nom court : **More Computer Games (MCG)**. Le dossier installé est **ModsLocal/LIB_BA_MoreComputerGames**. L'identifiant de build et la DLL restent **LIB_BaComputerGames** pour conserver les références des jeux et le contrat C#. Dans la liste des mods locaux, Big Ambitions utilise le nom du dossier ; le titre Workshop est saisi séparément lors de la publication.

Visuel Steam : **Thumbnail.jpg**. Le PNG original et le prompt sont conservés dans **release-assets/**. L'illustration représente le principe de la bibliothèque ; ce n'est pas une capture ni une liste de jeux inclus.

Lorsque MCG est activé, l'action native **Jouer aux jeux vidéo** de l'ordinateur ouvre notre catalogue. Aucun bouton supplémentaire n'est ajouté : le texte, la position et les conditions d'accès du bouton natif sont conservés. Le casse-brique d'origine reste dans la liste, même sans autre mod de jeu ; **Passer le temps** reste inchangé. La désactivation de MCG rétablit l'action d'origine.

L'auteur fournit sa description, son gameplay et éventuellement un chargeur de ressources ; la bibliothèque gère le menu, l'intégration au moniteur, les contrôles usuels et la fermeture.

MCG conserve aussi les records locaux du casse-briques vanilla et des jeux ajoutés via le même événement de fin de partie. Seul un score strictement supérieur remplace le record. Les records sont séparés par profil Steam, jeu et règles, communs aux sauvegardes et conservés hors ModsLocal. Aucun compte en ligne supplémentaire ni partage n'est nécessaire. Les parties abandonnées ne comptent pas.

Le mod **FlappyAmbition** est un exemple de consommateur développé séparément. Il n'est pas nécessaire pour installer, compiler ou utiliser cette bibliothèque.

Voir [API.md](API.md) pour les contrats, un exemple minimal et le chargement d'AssetBundles.

## Chargement

- Au chargement de la ville : enregistrement des métadonnées et lecture du petit fichier de records locaux ; aucun gameplay ou bundle préchargé.
- Après sélection et arrivée devant l'ordinateur : chargement optionnel des ressources.
- Lors de l'instanciation native : création du gameplay et de sa caméra.
- À la fermeture, au retrait du jeu ou au déchargement de la bibliothèque : arrêt du gameplay et libération des ressources.
- Les DLL restent chargées dans le processus, comme les autres mods Unity. Ce mécanisme ne décharge pas les assemblies.

Les mods peuvent s'enregistrer avant ou après l'activation de la bibliothèque. Un identifiant comme **capisoft:flappy-ambition** est unique dans le catalogue ; un doublon est refusé explicitement. Chaque inscription possède un jeton qui retire uniquement le jeu et les sessions appartenant à cette inscription.

## Dépendances et distribution

Big Ambitions 1.0 Build 3670 / Unity 2022.3.62f2, et **LIB_BaUnifiedUI 1.0.2+** installé séparément. Ne pas incorporer les DLL de cette bibliothèque ou de BAUI dans chaque jeu. Déclarer aussi les dépendances dans les Required Items Steam lors d'une future publication.

Cette preview n'est pas publiée au Workshop et ne dispose pas encore d'un identifiant Steam. L'API 0.2.0 est expérimentale. Le chargement des mods reste celui du SDK officiel ; aucune recherche de DLL sur disque, aucun téléchargement de code ou service réseau.

## Construire et vérifier

Depuis la racine de ce dépôt, avec .NET 8 pour les tests :

    dotnet run --project tools/Tests~/MCG.Tests.csproj -c Release

Le [guide de compilation](docs/COMPILATION.md) décrit le build MCG seul avec ses propres installations de Big Ambitions, Unity et BAUI. Les dépendances propriétaires et les binaires ne sont pas versionnés. Le ZIP « Code » de GitHub contient des sources, pas un mod prêt à jouer.

Les tests inclus ne lancent pas Big Ambitions et ne touchent pas ses sauvegardes. Les anciennes vérifications Unity utilisant un jeu externe sont rapportées séparément dans [VERIFICATION.md](VERIFICATION.md) ; ce dépôt ne contient pas ce jeu.

## Migration du prototype

L'ancien mod **ComputerArcade** ne doit pas être activé avec cette nouvelle architecture. Installer BAUI et MCG séparément ; ajouter ensuite les mods de jeux souhaités. Retirer aussi toute ancienne copie locale de MCG avant de copier le paquet dans **ModsLocal/LIB_BA_MoreComputerGames**. Le build inclus ne modifie jamais les mods installés.

MCG conserve lui-même les records locaux et n'a pas besoin de ComputerGameHighScore. Le signal **ComputerGames.RoundCompleted** reste disponible pour de futurs consommateurs. Aucun envoi en ligne ni import d'une ancienne file d'envoi n'est effectué.

Licence MIT pour les sources originales. Big Ambitions, Unity et les autres dépendances conservent leurs licences.
