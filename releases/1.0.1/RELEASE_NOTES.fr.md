# More Computer Games (MCG) 1.0.1

MCG transforme l'ordinateur du jeu en point d'accès commun aux mini-jeux. L'action native **Jouer aux jeux vidéo** ouvre le catalogue sur le moniteur, avec le casse-briques original et les jeux compatibles installés comme mods séparés.

## Corrections et améliorations

- Documentation et contrôle du bootstrap sûr pour les jeux publiés comme éléments Workshop séparés. La classe déclarée dans `RegisterModClass` doit rester chargeable avec BAModAPI seul ; elle peut enregistrer sa définition MCG depuis `OnLoad` une fois la dépendance disponible.
- Aucun appel natif à `VideoGameSetup.Finish()` lorsque Big Ambitions a déjà commencé sa fermeture ou le démontage de la ville. MCG libère toujours sa propre session et ses références ; la fermeture normale d'une session active conserve le chemin natif.
- Raccourcis **Retour au menu** et **Quitter** configurables dans **Options > Mods > More Computer Games**. **Retour arrière** et **Tab** restent les valeurs par défaut.
- Sauvegarde des combinaisons de touches exactes dans l'espace de préférences officiel des options de mods, avec capture, effacement, réinitialisation et avertissement de doublon. Les changements s'appliquent immédiatement aux commandes, aux instructions du moniteur et aux deux boutons natifs dans les 22 langues.

## Fonctionnalités existantes

- Un bouton traduit **Retour au menu**, à côté de **Quitter** dans le panneau natif sous le moniteur, disponible pendant un jeu, un chargement ou une erreur de lancement. Les deux boutons affichent les raccourcis actifs.
- L'interface MCG traduite dans les **22 langues sélectionnables du jeu**, selon la langue choisie dans Big Ambitions. Chaque mod de jeu fournit ses propres traductions de gameplay.
- Un menu sur le moniteur : **Haut/Bas** sélectionne, **Entrée** lance le jeu, **Retour arrière** revient au catalogue ou annule un chargement, et **Tab** quitte l'ordinateur. **Échap** conserve le menu pause natif de Big Ambitions.
- Les ressources d'un jeu ne sont chargées qu'après sa sélection. MCG gère le chargement, l'annulation, la nouvelle tentative, le passage entre caméras et la libération de chaque session.
- Un système de records locaux commun aux jeux vanilla et moddés. Seul un meilleur score de manche terminée remplace un record ; une manche abandonnée ne compte pas. Les records sont séparés par profil Steam, jeu et règles.
- Une correction des fichiers de scores incomplets produits par les versions précédentes. Le JSON managé conserve la liste des records et vérifie les données sérialisées avant écriture. Un ancien fichier reconnu ne contenant que l'en-tête est préservé en sauvegarde au prochain nouveau record ; les scores absents ne peuvent pas être reconstruits.
- Une API documentée pour enregistrer un jeu et gérer son cycle de vie. Les signatures publiques de la 0.2.0, le nom de l'assembly, l'identifiant du mod et le schéma des records sont conservés.

**Pour les auteurs de jeux :** recompiler les mods compatibles contre MCG **1.0.1**, assembly **1.0.1.0**. La classe décodée par `RegisterModClass` ne doit contenir ni classe de base, ni champ, ni signature MCG : Big Ambitions peut l'inspecter avant de résoudre MCG depuis un autre élément Workshop. Déclarer MCG comme dépendance Workshop séparée, sans incorporer sa DLL.

## Prérequis et installation

Cible : **Big Ambitions 1.0 Build 3670 sous Windows**, Unity 2022.3.62f2 / Mono. **MCG ne nécessite aucune autre bibliothèque de mod.** Activer MCG dans la liste des mods. Fermer le jeu avant de remplacer son paquet dans `ModsLocal/LIB_BA_MoreComputerGames`, puis le relancer.

MCG n'inclut aucun mod de jeu supplémentaire ni DLL de dépendance. [FlappyAmbitions](https://github.com/capisoft-lib/BigAmbitions_MCG_FlappyAmbitions), [Snacke](https://github.com/capisoft-lib/BigAmbitions_MCG_Snacke), [Ambitions Invaders](https://github.com/capisoft-lib/BigAmbitions_MCG_AmbitionsInvaders) et [Tetrix](https://github.com/capisoft-lib/BigAmbitions_MCG_Tetrix) sont des exemples séparés. ComputerGameHighScore n'est pas nécessaire. L'installation ne supprime pas les fichiers de records locaux existants.

## Roadmap et validation

Nous envisageons d'ajouter prochainement un leaderboard partagé. Il n'est pas encore disponible et aucune date de sortie n'est confirmée. Les scores restent locaux ; cette version ne les envoie pas en ligne et ne demande aucun compte supplémentaire.

Le [rapport de vérification](../../VERIFICATION.md) distingue compilation, tests isolés et fonctionnement en partie réelle. Le clavier, le déplacement vers l'ordinateur, le rendu HDRP du moniteur, la capture réelle des scores du casse-briques et la coexistence restent à vérifier en jeu. Les exemples en anglais sont des rendus isolés du moniteur, pas des captures d'une ville en cours de partie.

Les descriptions et notes de changement FR/EN prêtes à copier sont regroupées dans la [checklist de publication Steam](PUBLISHING_CHECKLIST.md). **MCG n'a aucun Required Item Steam.** Préparer ces fichiers ou pousser les sources sur GitHub ne publie pas l'élément Steam.
