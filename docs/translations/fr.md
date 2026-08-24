# InstallerClean in Français (French)

The text of InstallerClean's interface and command-line tool in English on the left, with the French translation beside it, grouped by where each line appears in the app. It is here so someone who really knows French can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.fr.resx`](../../src/InstallerClean.Core/Resources/Strings.fr.resx), so do not edit it by hand. The French translation itself lives in [`gen-strings-fr.mjs`](../../scripts/translations/gen-strings-fr.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Français |
| --- | --- |
| InstallerClean | InstallerClean |
| About | À propos |
| Files left alone | Fichiers laissés de côté |
| Unneeded files that are safe to delete | Fichiers inutiles que vous pouvez supprimer sans risque |

## Section headings

| English | Français |
| --- | --- |
| PATCHES | CORRECTIFS |
| PRODUCT DETAILS | DÉTAILS DU PRODUIT |
| BACKUP FOLDER | DOSSIER DE DESTINATION |
| SAY THANKS | DIRE MERCI |

## Buttons and actions

| English | Français |
| --- | --- |
| _About | À pr_opos |
| Copy | Copier |
| Cut | Couper |
| Paste | Coller |
| Select all | Sélectionner tout |
| _Browse... | _Parcourir... |
| _Cancel | _Annuler |
| Check for _updates | Rechercher des _mises à jour |
| _Close | _Fermer |
| _Delete permanently | _Supprimer définitivement |
| _Done | _Terminé |
| Details | Détails |
| _Buy me a cuppa | Offrez-moi un _café |
| Leave a _star on GitHub | _Laisser une étoile sur GitHub |
| Apache 2.0 licence | Licence Apache 2.0 |
| _Move | _Déplacer |
| Path to folder if you move rather than delete. | Chemin du dossier si vous déplacez plutôt que supprimez. |
| Open _release page | Ouvrir la page de la _version |
| _Re-scan | _Réanalyser |
| _Scan again | Analyser à _nouveau |
| Send report | Envoyer le rapport |
| _Send | _Envoyer |

## About window

| English | Français |
| --- | --- |
| Guide and FAQ | Guide et FAQ |
| Report a problem | Signaler un problème |
| Check for updates automatically | Rechercher des mises à jour automatiquement |

## Field labels

| English | Français |
| --- | --- |
| Reason | Motif |
| Author | Auteur |
| Application | Application |
| Title | Titre |
| Subject | Objet |
| Keywords | Mots-clés |
| Signing certificate | Certificat de signature |
| File size | Taille du fichier |
| Comment | Commentaire |
| Product name | Nom du produit |
| File | Fichier |
| Size | Taille |
| Patches | Correctifs |
| (unknown) | (inconnu) |
| (patches only) | (correctifs uniquement) |
| missing | manquant |

## Status and progress

| English | Français |
| --- | --- |
| Scanning... | Analyse... |
| Cancelling... | Annulation... |
| Starting scan... | Démarrage de l'analyse... |
| Asking Windows about installed software... | Interrogation de Windows sur les logiciels installés... |
| Scanning installer cache folder... | Analyse du dossier de cache d'installation... |
| Enumerating installed products... | Énumération des produits installés... |
| Checking registry for additional packages... | Vérification du registre pour des paquets supplémentaires... |
| Found {0} registered {1}. | Trouvé {0} {1} enregistrés. |
| Scan complete ({0}) | Analyse terminée ({0}) |
| Scanning local packages... | Analyse des paquets locaux... |
| Found {0} {1} you can safely delete. | {0} {1} à supprimer sans risque. |
| Preparing destination folder... | Préparation du dossier de destination... |
| Moving unneeded files... | Déplacement des fichiers inutiles... |
| Deleting unneeded files... | Suppression des fichiers inutiles... |
| Move cancelled. {0} of {1} {2} processed. | Déplacement annulé après avoir traité {0} sur {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Suppression annulée après avoir traité {0} sur {1} {2}. |
| Move failed ({0}). Details in {1}. | Échec du déplacement ({0}). Détails dans {1}. |
| Move failed ({0}). The crash log could not be written. | Échec du déplacement ({0}). Le crash.log n'a pas pu être écrit. |
| Delete failed ({0}). Details in {1}. | Échec de la suppression ({0}). Détails dans {1}. |
| Delete failed ({0}). The crash log could not be written. | Échec de la suppression ({0}). Le crash.log n'a pas pu être écrit. |
| Access denied. Windows refused the scan. | Accès refusé. Windows a refusé l'analyse. |
| Scan failed: couldn't read the Windows Installer records. | Échec de l'analyse : impossible de lire les enregistrements de Windows Installer. |
| Scan cancelled. | Analyse annulée. |
| Ready | Prêt |
| Scan failed ({0}). Details in {1}. | Échec de l'analyse ({0}). Détails dans {1}. |
| Scan failed ({0}). The crash log could not be written. | Échec de l'analyse ({0}). Le crash.log n'a pas pu être écrit. |

## Main screen text

| English | Français |
| --- | --- |
| Any unneeded files below are [safe to delete]. | Tous les fichiers inutiles ci-dessous sont [supprimables sans risque]. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Ils se trouvent dans {InstallerFolder}. InstallerClean interroge Windows sur chaque programme installé : un fichier est listé quand aucun programme ne le revendique ({0}), ou quand un correctif plus récent l'a remplacé et qu'aucun programme ne pourrait revenir à lui ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update, repair and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Déplacez-les vers un dossier de destination de votre choix, puis supprimez ce dossier une fois que vous aurez constaté que vos programmes se mettent à jour, se réparent et se désinstallent normalement. Les remettre dans {InstallerFolder} restaure tout. Ou supprimez-les définitivement maintenant. |
| Nothing scanned yet. | Rien n'a encore été analysé. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Cliquez sur Réanalyser pour parcourir {InstallerFolder} à la recherche de fichiers d'installation dont aucun programme n'a plus besoin. |
| These files can't be cleaned up right now. | Ces fichiers ne peuvent pas être nettoyés pour le moment. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Quelque chose utilise Windows Installer en ce moment, par exemple une mise à jour de Windows ou un programme qui s'installe en arrière-plan. Déplacer et Supprimer sont en pause pendant ce temps, pour qu'InstallerClean ne touche pas à {InstallerFolder} pendant qu'il change. Une fois terminé, réanalysez et ils reviennent. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Une transaction Windows Installer précédente est suspendue sur cette machine. Reprenez ou annulez cette installation (ou redémarrez Windows) avant de nettoyer {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows a mis en file d'attente pour le prochain redémarrage un renommage de fichier qui concerne {InstallerFolder}. Redémarrez Windows avant de nettoyer. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer a quelque chose en cours, donc Déplacer et Supprimer sont en pause. InstallerClean ne touchera pas à {InstallerFolder} pendant qu'il change. Une fois terminé, réanalysez et ils reviennent. |
| Select a file to view details. | Sélectionnez un fichier pour voir les détails. |
| Select a product to view details. | Sélectionnez un produit pour voir les détails. |
| No metadata available. | Aucune métadonnée disponible. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it, and its own last resort is reinstalling the program. | Ce fichier d'installation est absent. Cela ne pose aucun problème pour l'instant, et n'en posera pas jusqu'au jour où vous essaierez de réparer, mettre à jour ou désinstaller le programme auquel il appartient. Cette étape peut alors échouer, parce que Windows cherche ce fichier et qu'il n'est pas là.<br><br>Pour tenter d'y remédier, téléchargez le programme d'installation chez son éditeur et exécutez-le par-dessus votre copie existante (ne désinstallez pas d'abord : la désinstallation est elle-même une étape qui a besoin de ce fichier). Utilisez la version que vous avez installée si vous pouvez l'obtenir, car Windows peut en refuser une autre. Cela devrait restaurer le fichier et laisser vos paramètres intacts, mais Microsoft ne le garantit pas, et son propre dernier recours est de réinstaller le programme. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | Le README [explique ce dossier], et comment récupérer un fichier, avec les propres mots de Microsoft. |
| (none) | (aucun) |

## Reasons a file is unneeded

| English | Français |
| --- | --- |
| Orphaned | Orphelin |
| Superseded | Remplacé |
| Obsoleted | Obsolète |

## Completion screen

| English | Français |
| --- | --- |
| All clean | Tout est propre |
| Nothing removed | Rien n'a été retiré |
| Nothing to clean up in {InstallerFolder} | Rien à nettoyer dans {InstallerFolder} |
| Scanned {0} {1} in {2} | Analyse de {0} {1} en {2} |
| Nothing offered on this PC | Rien n'a été proposé sur ce PC |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({1}) it might otherwise have offered. | InstallerClean n'a pas pu déterminer avec certitude quels fichiers en cache appartiennent aux programmes installés ici, il a donc retenu le seul fichier ({1}) qu'il aurait pu proposer. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back all {0} files ({1}) it might otherwise have offered. | InstallerClean n'a pas pu déterminer avec certitude quels fichiers en cache appartiennent aux programmes installés ici, il a donc retenu l'ensemble des {0} fichiers ({1}) qu'il aurait pu proposer. |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | Le fichier de ce dossier est [retirable sans risque], vous pouvez donc supprimer le dossier quand vous voulez. D'ici là, vous pouvez le remettre dans {InstallerFolder} si un programme s'avérait en avoir besoin (extrêmement improbable). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | Les fichiers de ce dossier sont [retirables sans risque], vous pouvez donc le supprimer quand vous voulez. D'ici là, vous pouvez les remettre dans {InstallerFolder} si un programme s'avérait avoir besoin de l'un d'eux (extrêmement improbable). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | Le fichier de ce dossier est [retirable sans risque], vous pouvez donc supprimer le dossier ou le déplacer sur un autre lecteur quand vous voudrez vraiment récupérer l'espace. D'ici là, vous pouvez le remettre dans {InstallerFolder} si un programme s'avérait en avoir besoin (extrêmement improbable). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | Les fichiers de ce dossier sont [retirables sans risque], vous pouvez donc le supprimer ou le déplacer sur un autre lecteur quand vous voudrez vraiment récupérer l'espace. D'ici là, vous pouvez les remettre dans {InstallerFolder} si un programme s'avérait avoir besoin de l'un d'eux (extrêmement improbable). |
| {0} freed | {0} libérés |
| {0} moved | {0} déplacés |
| Nothing was moved | Rien n'a été déplacé |
| Nothing was deleted | Rien n'a été supprimé |
| {0} of {1} could not be moved. | {0} fichier sur {1} n'a pas pu être déplacé. |
| {0} of {1} could not be moved. | {0} fichiers sur {1} n'ont pas pu être déplacés. |
| {0} of {1} could not be deleted. | {0} fichier sur {1} n'a pas pu être supprimé. |
| {0} of {1} could not be deleted. | {0} fichiers sur {1} n'ont pas pu être supprimés. |
| {0} {1} moved to: {2} | Déplacé {0} {1} vers : {2} |
| {0} {1} moved to: {2} | Déplacé {0} {1} vers : {2} |
| {0} {1} kept in place, because the records now claim what the scan flagged. | {0} {1} laissés en place, parce que les enregistrements revendiquent maintenant ce que l'analyse avait signalé. |
| {0} {1} kept in place, because the Windows Installer records had changed by the final check. | {0} {1} laissés en place, parce que les enregistrements de Windows Installer avaient changé au moment de la vérification finale. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read in the final check. | {0} {1} laissés en place, parce que les enregistrements de Windows Installer n'ont pas pu être lus entièrement lors de la vérification finale. |
| {0} {1} kept in place, because by the final check InstallerClean could not be certain which cached files belong to the programs installed here. | {0} {1} laissés en place, parce qu'au moment de la vérification finale InstallerClean n'a pas pu déterminer avec certitude quels fichiers en cache appartiennent aux programmes installés ici. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} laissés en place, parce que Windows a un enregistrement du programme nommé à l'intérieur. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} laissés en place, parce qu'InstallerClean n'a trouvé aucun programme nommé à l'intérieur. |
| Moved {0} of {1} {2} before you cancelled. | Déplacé {0} sur {1} {2} avant votre annulation. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Supprimé définitivement {0} sur {1} {2} avant votre annulation. |
| {0} {1} permanently deleted | {0} {1} supprimé définitivement |
| {0} {1} permanently deleted | {0} {1} supprimés définitivement |
| Glad to help. There's a tip jar if you're feeling kind. | Content d'avoir pu aider. La cagnotte est là, si le cœur vous en dit. |

## Summaries and counts

| English | Français |
| --- | --- |
| {0} file left alone | {0} fichier laissé de côté |
| {0} files left alone | {0} fichiers laissés de côté |
| {0} unneeded file to clean up | {0} fichier inutile à nettoyer |
| {0} unneeded files to clean up | {0} fichiers inutiles à nettoyer |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Open Details for what to do. | Windows a un enregistrement pour {0} fichier qui n'est pas dans {InstallerFolder} : {1}. Cela ne gêne pas au quotidien, mais une réparation, une mise à jour ou une désinstallation peut échouer à cause de lui. Ouvrez Détails pour savoir quoi faire. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Open Details for what to do. | Windows a des enregistrements pour {0} fichiers qui ne sont pas dans {InstallerFolder} : {1}. Cela ne gêne pas au quotidien, mais une réparation, une mise à jour ou une désinstallation peut échouer à cause d'eux. Ouvrez Détails pour savoir quoi faire. |
| {0} other program | {0} autre programme |
| {0} other programs | {0} autres programmes |
| {0} file with no program named in the records | {0} fichier sans programme nommé dans les enregistrements |
| {0} files with no program named in the records | {0} fichiers sans programme nommé dans les enregistrements |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file rather than listing it. | Sur ce PC, InstallerClean n'a pas pu déterminer avec certitude quels fichiers en cache appartiennent aux programmes installés ici, il a donc retenu le seul fichier au lieu de le lister. |
| On this PC InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} rather than listing them. | Sur ce PC, InstallerClean n'a pas pu déterminer avec certitude quels fichiers en cache appartiennent aux programmes installés ici, il a donc retenu {0} {1} au lieu de les lister. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. The unneeded files above are unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Re-scan to try again. | InstallerClean n'a pas pu faire correspondre tout ce que contiennent les enregistrements de Windows, il ne les a donc pas tous lus. Les fichiers inutiles ci-dessus ne sont pas concernés, mais ce qu'il dit des fichiers absents de {InstallerFolder} peut être incomplet. Réanalysez pour réessayer. |
| {0} of {1} {2} | {0} sur {1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} à nettoyer ({2}) |
| {0} file left alone ({1}) | {0} fichier laissé de côté ({1}) |
| {0} files left alone ({1}) | {0} fichiers laissés de côté ({1}) |

## Confirmation dialogs

| English | Français |
| --- | --- |
| Move {0} {1} ({2})? | Déplacer {0} {1} ({2}) ? |
| Move to: | Déplacer vers : |
| Delete {0} {1} ({2})? | Supprimer {0} {1} ({2}) ? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | Ce fichier sera supprimé définitivement. Il est [supprimable sans risque], mais si vous voulez une sauvegarde, utilisez plutôt le bouton Déplacer. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Les fichiers seront supprimés définitivement. Ils sont [supprimables sans risque], mais si vous voulez une sauvegarde, utilisez plutôt le bouton Déplacer. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Ce dossier est sur le même lecteur, l'espace ne reviendra donc pas tant que vous ne l'aurez pas supprimé. Choisissez plutôt un dossier sur un autre lecteur si vous voulez l'espace tout de suite. |

## Error messages

| English | Français |
| --- | --- |
| Access denied | Accès refusé |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows a refusé l'accès à InstallerClean, qui s'est donc arrêté. Rien n'a été supprimé.<br><br>InstallerClean s'exécutait déjà en tant qu'administrateur, le relancer ainsi n'y changera rien. Windows n'en dit pas plus sur ce qui a refusé l'accès, il n'y a donc rien de précis à essayer. |
| Couldn't read the Windows Installer records | Impossible de lire les enregistrements de Windows Installer |
| Scan failed | Échec de l'analyse |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Les enregistrements de Windows Installer sont revenus complètement vides : pas un seul programme installé ni une seule mise à jour ne revendique de fichier d'installation en cache. Cela n'arrive pas sur une machine qui fonctionne (même une installation neuve de Windows en a), donc soit les enregistrements sont endommagés, soit ils n'ont pas pu être lus, et une analyse qui croirait cette réponse qualifierait à tort d'orphelin chaque fichier de {InstallerFolder}. InstallerClean s'est arrêté à la place. Rien n'a été supprimé. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer a refusé de laisser InstallerClean lister ce qui est installé. InstallerClean s'exécutait déjà en tant qu'administrateur, le relancer en tant qu'administrateur n'y changera rien. Sans cette liste, il n'y a aucun moyen sûr de savoir quels fichiers en cache servent encore, donc InstallerClean s'est arrêté. Rien n'a été supprimé. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer n'a pas pu fournir à InstallerClean une liste lisible des programmes installés : {0} entrées d'affilée sont revenues illisibles (dernier code d'erreur {1}). Plutôt que de travailler sur une liste lue en partie, InstallerClean s'est arrêté. Rien n'a été supprimé. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer n'a jamais signalé la fin de la liste des programmes installés : InstallerClean a renoncé après {0} entrées (dernier code d'erreur {1}). Une liste sans fin n'est pas fiable, donc InstallerClean s'est arrêté. Rien n'a été supprimé. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer n'a jamais signalé la fin de la liste des correctifs d'un programme : InstallerClean a renoncé après {0} entrées (dernier code d'erreur {1}). Une liste sans fin n'est pas fiable, donc InstallerClean s'est arrêté. Rien n'a été supprimé. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean n'a pas pu faire correspondre les enregistrements de Windows Installer avec le contenu de {InstallerFolder}. Presque rien de ce que désignent les enregistrements ne s'y trouve réellement, et presque rien de ce qui s'y trouve n'est nommé par un enregistrement, donc aucun fichier n'a pu être montré comme inutile. Rien n'a été proposé et rien n'a été retiré. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean n'a pas pu faire correspondre les enregistrements de Windows Installer avec le contenu de {InstallerFolder}. Le dossier contient des fichiers, mais pas un seul enregistrement ne désigne quoi que ce soit dedans, donc aucun fichier n'a pu être montré comme inutile. Rien n'a été proposé et rien n'a été retiré. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean n'a pas pu lire assez des enregistrements de Windows Installer pour savoir avec certitude ce qui sert encore : la liste des programmes installés est revenue incomplète, et lire ces mêmes enregistrements directement dans le registre a également donné des erreurs. Un fichier pourrait sembler orphelin uniquement parce que l'enregistrement qui le nomme faisait partie des illisibles, donc InstallerClean s'est arrêté. Rien n'a été supprimé. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean n'a pas pu obtenir de Windows qu'il résolve le vrai chemin de {InstallerFolder}, donc aucun fichier n'a pu être montré comme s'y trouvant et aucun n'a été proposé au nettoyage. Cette analyse n'a rien trouvé parce que cette vérification a échoué, pas parce que le dossier est propre. Rien n'a été retiré. |
| Nothing was deleted | Rien n'a été supprimé |
| Nothing was moved | Rien n'a été déplacé |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean n'a pas pu prendre le verrou que Windows Installer utilise pour empêcher deux programmes de modifier les logiciels installés en même temps, il n'a donc pas pu exclure qu'un fichier devienne nécessaire en cours de route, et rien n'a été supprimé. Réessayez, et redémarrez Windows si cela persiste. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean n'a pas pu prendre le verrou que Windows Installer utilise pour empêcher deux programmes de modifier les logiciels installés en même temps, il n'a donc pas pu exclure qu'un fichier devienne nécessaire en cours de route, et rien n'a été déplacé. Réessayez, et redémarrez Windows si cela persiste. |
| Invalid destination | Destination invalide |
| Could not write to destination | Impossible d'écrire dans la destination |
| Move failed | Échec du déplacement |
| Delete failed | Échec de la suppression |
| Setting not saved | Paramètre non enregistré |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | La modification n'a pas pu être enregistrée. Au prochain démarrage, InstallerClean reviendra au paramètre précédent. |
| The destination cannot be inside the Windows Installer folder. | La destination ne peut pas se trouver dans le dossier Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | La destination {0} se résout sous un dossier système de Windows. Choisissez un chemin en dehors de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% et %ProgramData%. |
| Not enough space | Espace insuffisant |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Espace insuffisant dans {0}<br><br>Nécessaire : {1}<br>Disponible : {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | Vous n'avez pas l'autorisation d'écrire dans {0}.<br>Essayez un dossier dans votre profil utilisateur ou sur un lecteur qui vous appartient. |
| The path {0} is too long for Windows. Pick a shorter path. | Le chemin {0} est trop long pour Windows. Choisissez un chemin plus court. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | Le dossier {0} n'existe pas et n'a pas pu être créé. Vérifiez la lettre de lecteur ou le chemin réseau. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows ne peut pas écrire dans {0}.<br>Détails dans {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows ne peut pas écrire dans {0}. Le crash.log n'a pas pu être écrit. |
| Cannot write to {0}.<br>Details in {1}. | Impossible d'écrire dans {0}.<br>Détails dans {1}. |
| Cannot write to {0}. The crash log could not be written. | Impossible d'écrire dans {0}. Le crash.log n'a pas pu être écrit. |
| File no longer exists. | Le fichier n'existe plus. |
| Source file is a symlink or junction; refused for safety. | Le fichier source est un lien symbolique ou une jonction ; refusé par sécurité. |
| This file is not directly inside the Windows Installer folder; refused for safety. | Ce fichier ne se trouve pas directement dans le dossier Windows Installer ; refusé par sécurité. |
| Windows refused access to this file; it was left in place. | Windows a refusé l'accès à ce fichier ; il a été laissé en place. |
| Windows refused access to these files; they were left in place. | Windows a refusé l'accès à ces fichiers ; ils ont été laissés en place. |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | Ce fichier est ouvert ou verrouillé par un autre programme, rien ne peut donc le retirer pour l'instant. Il a été laissé en place ; réessayez plus tard. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | Ces fichiers sont ouverts ou verrouillés par un autre programme, rien ne peut donc les retirer pour l'instant. Ils ont été laissés en place ; réessayez plus tard. |
| Windows reported a file error; the file was left in place. | Windows a signalé une erreur de fichier ; le fichier a été laissé en place. |
| Windows reported file errors; these files were left in place. | Windows a signalé des erreurs de fichier ; ces fichiers ont été laissés en place. |
| Something went wrong with this file; it was left in place. | Un problème est survenu avec ce fichier ; il a été laissé en place. |
| Something went wrong with these files; they were left in place. | Un problème est survenu avec ces fichiers ; ils ont été laissés en place. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Refus de déplacer des fichiers dans le dossier Windows Installer (destination : {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Le dossier de destination doit être un chemin complet vers un dossier, commençant par une lettre de lecteur ou un partage réseau (par exemple D:\Backup, ou \\serveur\backup). InstallerClean ne peut pas utiliser celui-ci : {0} |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | InstallerClean n'a plus pu confirmer le dossier de destination, il s'est donc arrêté plutôt que d'écrire au mauvais endroit. Vérifiez {0}, puis Réanalyser et réessayez. |
| Cannot write to {0}. | Impossible d'écrire dans {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Impossible de trouver un nom de fichier unique pour « {0} » après 10 000 tentatives. |

## Update check

| English | Français |
| --- | --- |
| Check for updates | Rechercher des mises à jour |
| Checking... | Vérification... |
| Up to date. | À jour. |
| Version {0} is available. | La version {0} est disponible. |
| Update available | Mise à jour disponible |
| You're running version {0}.<br>Version {1} is available. | Vous utilisez la version {0}.<br>La version {1} est disponible. |
| Couldn't reach GitHub. Check your internet connection and try again. | Impossible de joindre GitHub. Vérifiez votre connexion internet et réessayez. |
| GitHub returned an error response. Try again in a few minutes. | GitHub a renvoyé une réponse d'erreur. Réessayez dans quelques minutes. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | La réponse de GitHub ne contenait pas de version reconnaissable. Réessayez plus tard, ou ouvrez directement la page des versions. |
| The check timed out. Your connection to GitHub may be slow; try again. | La vérification a expiré. Votre connexion à GitHub est peut-être lente ; réessayez. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | La vérification a échoué pour une raison inconnue. Les détails sont dans le crash.log si vous devez le signaler. |

## Opening links in your browser

| English | Français |
| --- | --- |
| Couldn't open your browser | Impossible d'ouvrir votre navigateur |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean n'a pas pu ouvrir votre navigateur. Le lien est dans votre presse-papiers, vous pouvez donc le coller vous-même :<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean n'a pas pu ouvrir votre navigateur, ni copier le lien dans le presse-papiers. Voici le lien :<br><br>{0} |

## Sending the summary

| English | Français |
| --- | --- |
| Sending... | Envoi... |
| Thanks! Report sent. | Merci ! Rapport envoyé. |
| Sending failed. Try again later. | Échec de l'envoi. Réessayez plus tard. |
| No report to send. | Aucun rapport à envoyer. |
| Send this? | Envoyer ceci ? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | Ça va vers nofaff.netlify.app/api/result-log. Rien ne vous identifie, ni votre machine ; ça me dit juste qu'InstallerClean fonctionne et [combien d'espace les gens libèrent]. |

## Startup and crashes

| English | Français |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean est déjà en cours d'exécution. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Une erreur inattendue s'est produite et InstallerClean doit se fermer.<br><br>{0}<br><br>Détails écrits dans :<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Une erreur inattendue s'est produite et InstallerClean doit se fermer.<br><br>{0}<br><br>Le crash.log n'a pas pu être écrit. |
| Startup error | Erreur de démarrage |
| Failed to start ({0}). Details written to:<br>{1} | Échec du démarrage ({0}). Détails écrits dans :<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Échec du démarrage ({0}). Le crash.log n'a pas pu être écrit. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log recueille les exceptions non gérées d'InstallerClean.<br># Avec des privilèges élevés, les messages d'exception du framework<br># peuvent contenir des chemins de fichiers de la session en cours (y<br># compris des profils d'autres utilisateurs énumérés par les requêtes<br># Windows Installer). Les messages d'échec réseau de la vérification<br># des mises à jour ou de l'envoi du journal de résultats peuvent<br># contenir l'URL de destination et l'adresse IP ou proxy résolue. Les<br># entrées sur des enregistrements Windows Installer illisibles peuvent<br># contenir un SID de compte Windows (S-1-5-21-...) et les codes<br># produit des logiciels installés.<br># Supprimez ces trois types d'informations avant de joindre ce fichier<br># à un rapport de bogue public.<br> |

## Tooltips (hover text)

| English | Français |
| --- | --- |
| It's thirsty work! | Ça donne soif ! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Annulation demandée. InstallerClean attend que l'étape en cours atteigne un point d'arrêt. Cela peut prendre quelques secondes lors d'opérations d'E/S intensives ou d'un appel à la base de données MSI. |
| Close | Fermer |
| A star helps other people find it. | Une étoile aide les autres à découvrir InstallerClean. |
| Minimise | Réduire |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Comme vous voulez, mais c'est apprécié. Envoie un résumé anonyme qui me dit juste si l'outil fonctionne et combien d'espace les gens libèrent. L'écran suivant vous montre ce qui sera envoyé avant que vous confirmiez. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Comme vous voulez, mais c'est apprécié. Envoie un résumé anonyme qui me dit juste si l'outil fonctionne. L'écran suivant vous montre ce qui sera envoyé avant que vous confirmiez. |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Déplace les fichiers inutiles vers le dossier de destination. Supprimez ce dossier dès que vous serez convaincu que rien n'en a besoin. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Déplace les fichiers inutiles vers un dossier de destination. Vous le choisirez juste après. Supprimez ce dossier dès que vous serez convaincu que rien n'en a besoin. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Déplace les fichiers inutiles vers le dossier de destination. Il est sur le même lecteur, vous ne récupérerez donc l'espace qu'une fois ce dossier supprimé ou déplacé sur un autre lecteur. Vous pourrez le faire dès que vous serez convaincu que rien n'en a besoin. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Supprime définitivement les fichiers inutiles. Ils sont retirables sans risque, et vous récupérerez l'espace tout de suite. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nom du titulaire du certificat Authenticode incorporé. Chaîne non vérifiée. |
| Change language. The program will restart. | Changer la langue. Le programme redémarrera. |

## Screen reader labels

| English | Français |
| --- | --- |
| Donate | Faire un don |
| Buy me a cuppa | Offrez-moi un café |
| Cancel operation | Annuler l'opération |
| Cancel scan | Annuler l'analyse |
| Cancel startup scan | Annuler l'analyse de démarrage |
| Close | Fermer |
| Close window | Fermer la fenêtre |
| Close result and return to main window | Fermer le résultat et revenir à la fenêtre principale |
| Leave a star on github | Laisser une étoile sur github |
| Minimise | Réduire |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Supprimer définitivement retire les fichiers inutiles. Annuler ferme sans rien supprimer. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Déplacer place les fichiers inutiles dans le dossier de destination choisi. Annuler les laisse où ils sont. |
| Say thanks | Dire merci |
| Send posts the report shown to No Faff. Cancel sends nothing. | Envoyer transmet à No Faff le rapport affiché. Annuler n'envoie rien. |
| Check for updates | Rechercher des mises à jour |
| Checks github's releases page for a newer version. | Vérifie sur la page des versions de github s'il existe une version plus récente. |
| Opens the readme on github in your browser. | Ouvre le readme sur github dans votre navigateur. |
| Opens the issue tracker on github.com in your browser. | Ouvre le suivi des problèmes (Issues) sur github.com dans votre navigateur. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Si la case est cochée, InstallerClean recherche une version plus récente sur github à son lancement. |
| Open the release page to download the newer version, or cancel to keep the current version. | Ouvrez la page de la version pour télécharger la version plus récente, ou annulez pour conserver la version actuelle. |
| Opens the licence file on github.com in your browser. | Ouvre le fichier de licence sur github.com dans votre navigateur. |
| Backup folder | Dossier de destination |
| Patches | Correctifs |
| Product details | Détails du produit |
| Backup folder | Dossier de destination |
| Operation progress | Progression de l'opération |
| Scan {InstallerFolder} again | Analyser à nouveau {InstallerFolder} |
| Scanning progress | Progression de l'analyse |
| Startup scan progress | Progression de l'analyse de démarrage |
| Details, unneeded files | Détails, fichiers inutiles |
| Available for cleanup. | Disponibles pour le nettoyage. |
| Details, files left alone | Détails, fichiers laissés de côté |
| Read-only inventory. | Inventaire en lecture seule. |
| Sorted by {0}, ascending | Trié par {0}, ordre croissant |
| Sorted by {0}, descending | Trié par {0}, ordre décroissant |
| Scan results | Résultats de l'analyse |
| Result details | Détails du résultat |
| File details | Détails du fichier |
| Product details | Détails du produit |
| Dialog text | Texte de la boîte de dialogue |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Fichiers qui n'ont pas pu être traités |
| Explains this folder, and how to recover a file, in the README | Explique ce dossier, et comment récupérer un fichier, dans le README |
| Report preview | Aperçu du rapport |
| Change language | Changer la langue |
| The program will restart. | Le programme redémarrera. |

## File picker

| English | Français |
| --- | --- |
| Choose destination folder for moved files | Choisissez le dossier de destination des fichiers déplacés |

## Version

| English | Français |
| --- | --- |
| Version {0} | Version {0} |

## Word forms (singular and plural)

| English | Français |
| --- | --- |
| file | fichier |
| files | fichiers |
| error | erreur |
| errors | erreurs |
| package | paquet |
| packages | paquets |
| product | produit |
| products | produits |
| patch | correctif |
| patches | correctifs |

## Sizes and times

| English | Français |
| --- | --- |
| ,  | ,  |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | moins d'une seconde |
| {0:F1} seconds | {0:F1} secondes |

## Command-line tool (installerclean-cli)

| English | Français |
| --- | --- |
| Error: unknown argument '{0}' | Erreur : argument inconnu '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Erreur : argument supplémentaire inattendu « {0} ». Si votre dossier de destination contient un espace, mettez le chemin entier entre guillemets : /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Erreur : argument supplémentaire inattendu '{0}'. /s et /d n'acceptent aucun autre argument, et un seul indicateur peut être utilisé par exécution. |
| Cancelling... | Annulation... |
| Cancelled. | Annulé. |
| Error: unexpected failure ({0}). Details written to {1}. | Erreur : échec inattendu ({0}). Détails écrits dans {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Erreur : échec inattendu ({0}). Le journal de plantage n'a pas pu être écrit. |
| Scanning {InstallerFolder}... | Analyse de {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | {0} {1} inutiles à nettoyer ont été trouvés ({2}). |
| Found no unneeded files. | Aucun fichier inutile trouvé. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back the one file ({2}) it might otherwise have offered. | InstallerClean n'a pas pu déterminer avec certitude quels fichiers en cache appartiennent aux programmes installés ici, il a donc retenu le seul fichier ({2}) qu'il aurait pu proposer. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it held back all {0} {1} ({2}) it might otherwise have offered. | InstallerClean n'a pas pu déterminer avec certitude quels fichiers en cache appartiennent aux programmes installés ici, il a donc retenu l'ensemble des {0} {1} ({2}) qu'il aurait pu proposer. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but a repair, update or uninstall can fail on it. Running that program's installer again, preferably the same version, usually restores the file. | Windows a un enregistrement pour {0} fichier qui n'est pas dans {InstallerFolder} : {1}. Cela ne gêne pas au quotidien, mais une réparation, une mise à jour ou une désinstallation peut échouer à cause de lui. Réexécuter le programme d'installation de ce logiciel, de préférence dans la même version, restaure généralement le fichier. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but a repair, update or uninstall can fail on them. Running each program's installer again, preferably the same version, usually restores the files. | Windows a des enregistrements pour {0} fichiers qui ne sont pas dans {InstallerFolder} : {1}. Cela ne gêne pas au quotidien, mais une réparation, une mise à jour ou une désinstallation peut échouer à cause d'eux. Réexécuter le programme d'installation de chaque logiciel, de préférence dans la même version, restaure généralement les fichiers. |
| InstallerClean couldn't match up everything in the Windows records, so it didn't read all of them. What it found is unaffected, but what it says about files missing from {InstallerFolder} may be short of the full picture. Running it again may pick up more. | InstallerClean n'a pas pu faire correspondre tout ce que contiennent les enregistrements de Windows, il ne les a donc pas tous lus. Ce qu'il a trouvé n'est pas concerné, mais ce qu'il dit des fichiers absents de {InstallerFolder} peut être incomplet. Le relancer permettra peut-être d'en détecter davantage. |
| Deleting {0} unneeded {1}... | Suppression de {0} {1} inutiles... |
| Permanently deleted {0} unneeded {1}. | {0} {1} inutiles ont été supprimés définitivement. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Erreur : aucune destination de déplacement spécifiée. Utilisez /m CHEMIN. (Une valeur par défaut définie dans l'interface est propre à chaque utilisateur et ne s'applique pas aux exécutions planifiées ou par compte de service.) |
| Error: destination cannot be inside the Windows Installer folder. | Erreur : la destination ne peut pas se trouver dans le dossier Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Erreur : la destination doit être un chemin entièrement qualifié. Reçu : {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Erreur : la destination {0} se résout sous un dossier système de Windows. Choisissez un chemin en dehors de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% et %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Erreur : espace insuffisant dans {0}. Déplacer ces fichiers nécessite {1} et {2} sont libres. Rien n'a été déplacé. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Erreur : quelque chose utilise Windows Installer en ce moment, par exemple une mise à jour de Windows ou un programme qui s'installe en arrière-plan. /m et /d sont bloqués pendant ce temps. Réessayez une fois terminé. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Erreur : une transaction Windows Installer précédente est suspendue sur cette machine. Reprenez ou annulez cette installation (ou redémarrez Windows) avant de nettoyer {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Erreur : une opération de fichier mise en file d'attente pour après le redémarrage vise {InstallerFolder} ({0}). Redémarrez Windows pour terminer cette opération avant de nettoyer. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Erreur : Windows Installer a quelque chose en cours, donc /m et /d sont bloqués. InstallerClean ne touchera pas à {InstallerFolder} pendant qu'il change. Réessayez une fois terminé. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Erreur : InstallerClean n'a pas pu prendre le verrou Windows Installer qui empêche deux programmes de modifier les logiciels installés en même temps, il n'a donc pas pu exclure qu'un fichier devienne nécessaire en cours de route. Rien n'a été supprimé. Réessayez, et redémarrez Windows si cela persiste. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Erreur : InstallerClean n'a pas pu prendre le verrou Windows Installer qui empêche deux programmes de modifier les logiciels installés en même temps, il n'a donc pas pu exclure qu'un fichier devienne nécessaire en cours de route. Rien n'a été déplacé. Réessayez, et redémarrez Windows si cela persiste. |
| Moving {0} unneeded {1} to {2}... | Déplacement de {0} {1} inutiles vers {2}... |
| Moved {0} unneeded {1}. | {0} {1} inutiles ont été déplacés. |
| InstallerClean could no longer confirm the backup folder, so it stopped rather than write into the wrong place. Check {0}, then run the command again. | InstallerClean n'a plus pu confirmer le dossier de destination, il s'est donc arrêté plutôt que d'écrire au mauvais endroit. Vérifiez {0}, puis relancez la commande. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Un autre processus InstallerClean détient le verrou d'instance unique (l'interface ou une autre exécution de la CLI). Code de sortie 75 (transitoire) ; vous pouvez réessayer plus tard sans risque. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Remarque : l'écriture dans le journal des événements a échoué. Vérifiez les autorisations du journal Application ou la stratégie de groupe. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - nettoyage de {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Retire les .msi et .msp en cache dont aucun programme installé n'a besoin. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Exige une invite de commandes administrateur ; Windows ne le lancera pas. |
| Usage: | Utilisation : |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Affiche cette aide (accepte aussi /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Affiche la version (accepte aussi -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Analyse seule - liste les inutiles |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Supprime définitivement les inutiles |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Déplace vers le dossier enregistré |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m CHEMIN  Déplace vers le chemin spécifié |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli bloque l'invite jusqu'à la fin, pour qu'un script ou<br>une tâche planifiée puisse l'attendre. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Dossier propre à l'utilisateur ; tâches planifiées ou SYSTEM : /m CHEMIN. |
| Exit codes: | Codes de sortie : |
|   0   success: the run did what it was asked and nothing failed |   0   succès : l'exécution a fait ce qui lui était demandé, sans échec |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   échec : rien de traité (arguments ou destination incorrects,<br>       analyse échouée ou tous les fichiers en échec) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partiel : une partie traitée, l'autre non (un échec ou un Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitoire : quelque chose a bloqué l'exécution (voir le message) |
|   130 cancelled (Ctrl+C) |   130 annulé (Ctrl+C) |
