# Confidentialité des publications

## Contenu autorisé dans Git

Sources originales MCG, métadonnées Unity stables, documentation, tests de la bibliothèque et illustrations promotionnelles. Les `.meta` Unity portent des GUID d'assets ; ces GUID ne sont pas des identifiants de compte.

Sont exclus : dépendances propriétaires, binaires, PDB/MDB, fichiers de réponse du compilateur, logs, dumps, réglages IDE ou machine, sauvegardes, records des joueurs, clés et identifiants d'authentification. FlappyAmbition appartient à un autre dépôt.

## Scripts et compilation

Les scripts reçoivent les chemins en paramètres ou les déduisent de leur emplacement. Aucun nom de compte, de poste ou de répertoire personnel n'est prédéfini. Les exemples utilisent des variables ou des chemins relatifs.

Le build crée forcément des fichiers intermédiaires contenant les chemins de la machine qui compile. Ils restent dans `artifacts/`, ignoré par Git, et ne doivent jamais être joints à un signalement. Seul le sous-dossier `LIB_BA_MoreComputerGames` est un paquet distribuable.

La DLL est compilée sans symboles, avec un mapping neutre des chemins source. Le script refuse les références de PDB, les chemins privés connus dans la DLL et les artefacts de compilation dans le paquet. Les tests .NET n'émettent pas de PDB.

## Avant chaque publication

1. Examiner `git diff --cached --stat` et `git diff --cached --check`.
2. Vérifier qu'aucun fichier binaire généré, chemin absolu personnel ou donnée de joueur n'est suivi.
3. Scanner les fichiers destinés au dépôt avec un détecteur de secrets, puis l'historique avant le push.
4. Examiner les métadonnées EXIF/XMP/IPTC/commentaires des images, pas seulement leur apparence.
5. Utiliser une adresse Git publique ou `noreply` pour ne pas exposer une adresse privée dans les commits.

Un `.gitignore` ne retire pas un secret déjà commité. Si cela arrive, révoquer le secret et traiter aussi l'historique, les logs et les éventuelles pièces jointes.

## Données des joueurs

MCG lit l'identifiant du profil Steam local pour choisir son fichier de records, avec un profil `offline` séparé. Cette identité n'est pas incluse dans l'événement public de fin de manche. MCG ne transmet ni records, ni chemins, ni identifiants à un service réseau.

Avant de partager un log de Big Ambitions ou une capture, masquer les noms de compte, chemins, identifiants Steam et autres informations privées. Aucun outil de scan ne peut garantir l'absence de toute donnée sensible : conserver une revue humaine du contenu publié.
