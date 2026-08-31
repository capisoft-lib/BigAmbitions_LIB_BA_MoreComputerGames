# Publication Steam Workshop — MCG 1.0.0

Cette checklist accompagne le paquet de la bibliothèque seule. Les mods de jeux se publient séparément. Un commit GitHub, un build réussi et un upload Steam sont trois opérations distinctes.

## Contenu de la fiche

| Champ | Valeur ou fichier |
| --- | --- |
| Titre | **LIB BA More Computer Games (MCG)** |
| Fiche existante à mettre à jour | [Workshop 3793604724](https://steamcommunity.com/sharedfiles/filedetails/?id=3793604724) |
| Version | **1.0.0** — assembly **1.0.0.0** |
| Dossier local à sélectionner | **LIB_BA_MoreComputerGames**, sous **ModsLocal** |
| Vignette | [Thumbnail.jpg](../../Thumbnail.jpg), à la racine du paquet |
| Description anglaise, BBCode | [WORKSHOP_DESCRIPTION.en.txt](WORKSHOP_DESCRIPTION.en.txt) |
| Description française, BBCode | [WORKSHOP_DESCRIPTION.fr.txt](WORKSHOP_DESCRIPTION.fr.txt) |
| Texte court anglais / français | [EN](WORKSHOP_SHORT_DESCRIPTION.en.txt) / [FR](WORKSHOP_SHORT_DESCRIPTION.fr.txt) |
| Notes de changement anglaises / françaises | [EN](WORKSHOP_CHANGE_NOTES.en.txt) / [FR](WORKSHOP_CHANGE_NOTES.fr.txt) |
| Notes de release détaillées | [EN](RELEASE_NOTES.en.md) / [FR](RELEASE_NOTES.fr.md) |
| Required Items de MCG | **Aucun** |

Les descriptions présentent les boutons **Retour au menu [Backspace]** et **Quitter [TAB]**, les 22 langues, les jeux installés séparément et les records locaux. Le leaderboard reste une intention, sans date confirmée, pas une fonctionnalité de la 1.0.0.

## Avant l'upload

- Utiliser le paquet produit par [tools/build.ps1](https://github.com/capisoft-lib/BigAmbitions_LIB_BA_MoreComputerGames/blob/main/tools/build.ps1), jamais le checkout Git ni le dossier parent contenant les références et réponses privées du compilateur.
- Vérifier la présence d'une seule DLL de mod, des 22 locales, de la vignette et du manifest. Ne publier aucun PDB, log, score, identifiant de profil, chemin personnel ou DLL propriétaire.
- Fermer le jeu avant de remplacer le paquet local, puis relancer. Conserver une copie de l'ancienne installation hors de ModsLocal.
- Si MCG est déjà installé depuis Steam, ne pas ajouter une seconde copie active dans ModsLocal. Préparer les fichiers d'upload à part, puis gérer délibérément la copie locale utilisée par Mod Creator sans modifier directement les fichiers du Workshop.
- Sur une copie de partie, vérifier le menu du moniteur, un jeu vanilla et un jeu MCG, les clics et touches Backspace/Tab, la pause Escape, le changement de langue et la persistance d'un record après redémarrage. Le [rapport de vérification](../../VERIFICATION.md) distingue les tests déjà effectués des essais natifs encore à faire.

## Upload depuis Big Ambitions

Le [SDK officiel](https://github.com/hovgaardgames/bigambitions#5-upload-your-mod-in-game) décrit le parcours via **Mods > Mod Creator**. MCG possède déjà sa fiche : utiliser **Edit mod** sur **LIB BA More Computer Games (MCG)** pour conserver l'identifiant **3793604724**.

Sélectionner **Browse mod folder**, puis le dossier local **LIB_BA_MoreComputerGames**. Renseigner le titre, la description et la vignette à partir des fichiers ci-dessus, contrôler la visibilité choisie et lancer l'upload. Ne sélectionner aucun jeu séparé comme dépendance de MCG.

Pour des descriptions localisées, l'outil d'upload doit prendre en charge la langue de mise à jour (`english` / `french`). L'API Steamworks utilise [SetItemUpdateLanguage](https://partner.steamgames.com/doc/api/ISteamUGC#SetItemUpdateLanguage) avant l'envoi ; cette option n'est pas nécessairement exposée dans l'éditeur du jeu. Ne pas écraser le texte anglais avec le français en pensant créer une seconde traduction.

## Après l'upload

Ouvrir la fiche Workshop et contrôler le titre, les textes, la vignette, la visibilité et l'absence de Required Items. Vérifier aussi le paquet téléchargé dans une installation de test sans doublon local actif. Conserver la même URL Workshop dans le README et les dépendances des jeux compatibles.

Les fiches des jeux compatibles doivent déclarer **MCG** dans leurs Required Items et leurs DLL doivent référencer l'assembly **1.0.0.0**, sans l'embarquer.
