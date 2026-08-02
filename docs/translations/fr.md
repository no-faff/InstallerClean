# InstallerClean in Français (French)

The text of InstallerClean's interface and command-line tool in English on the left, with the French translation beside it, grouped by where each line appears in the app. It is here so someone who really knows French can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.fr.resx`](../../src/InstallerClean.Core/Resources/Strings.fr.resx), so do not edit it by hand. The French translation itself lives in [`gen-strings-fr.mjs`](../../scripts/translations/gen-strings-fr.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Français |
| --- | --- |
| InstallerClean | InstallerClean |
| About | À propos |
| Registered files that should not be deleted | Fichiers enregistrés qui ne devraient pas être supprimés |
| Unneeded files that are safe to delete | Fichiers inutiles que vous pouvez supprimer sans risque |

## Section headings

| English | Français |
| --- | --- |
| PRODUCTS | PRODUITS |
| PATCHES | CORRECTIFS |
| PRODUCT DETAILS | DÉTAILS DU PRODUIT |
| BACKUP FOLDER | BACKUP FOLDER |
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
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
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
| Moving unneeded files... | Moving unneeded files... |
| Deleting unneeded files... | Deleting unneeded files... |
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
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Ils se trouvent dans {InstallerFolder}, laissés là quand un programme a été désinstallé ({0}), qu'un correctif plus récent en a remplacé un ({1}) ou que l'éditeur l'a retiré ({2}). InstallerClean ne liste jamais que les fichiers dont Windows lui-même déclare avoir fini de se servir. |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | Rien n'a encore été analysé. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Cliquez sur Réanalyser pour parcourir {InstallerFolder} à la recherche de fichiers d'installation dont aucun programme n'a plus besoin. |
| These files can't be cleaned up right now. | Ces fichiers ne peuvent pas être nettoyés pour le moment. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. |
| Select a file to view details. | Sélectionnez un fichier pour voir les détails. |
| Select a product to view details. | Sélectionnez un produit pour voir les détails. |
| No metadata available. | Aucune métadonnée disponible. |
| This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. |
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
| Nothing to clean up in {InstallerFolder} | Rien à nettoyer dans {InstallerFolder} |
| Scanned {0} {1} in {2} | Analyse de {0} {1} en {2} |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
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
| {0} {1} kept in place, because a program went back to needing what the scan flagged. | {0} {1} laissés en place, redevenus nécessaires à un programme après l'analyse. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | {0} {1} laissés en place, car les enregistrements de Windows Installer n'ont pas pu être entièrement lus lors de la nouvelle vérification. |
| Moved {0} of {1} {2} before you cancelled. | Déplacé {0} sur {1} {2} avant votre annulation. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Supprimé définitivement {0} sur {1} {2} avant votre annulation. |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | Content d'avoir pu aider. La cagnotte est là, si le cœur vous en dit. |

## Summaries and counts

| English | Français |
| --- | --- |
| {0} file still needed | {0} fichier encore nécessaire |
| {0} files still needed | {0} fichiers encore nécessaires |
| {0} unneeded file to clean up | {0} fichier inutile à nettoyer |
| {0} unneeded files to clean up | {0} fichiers inutiles à nettoyer |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} fichier enregistré est manquant (non supprimé par InstallerClean). Aucun problème pour l'instant, mais une future réparation, mise à jour ou désinstallation de ce programme pourrait échouer. Ouvrez les Détails pour savoir quoi faire. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} fichiers enregistrés sont manquants (non supprimés par InstallerClean). Aucun problème pour l'instant, mais une future réparation, mise à jour ou désinstallation de ces programmes pourrait échouer. Ouvrez les Détails pour savoir quoi faire. |
| Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| {0} of {1} {2} | {0} sur {1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} orphelins, {1} remplacés, {2} obsolètes ({3}) |
| {0} registered file that is still needed ({1}) | {0} fichier enregistré encore nécessaire ({1}) |
| {0} registered files that are still needed ({1}) | {0} fichiers enregistrés encore nécessaires ({1}) |

## Confirmation dialogs

| English | Français |
| --- | --- |
| Move {0} {1} ({2})? | Déplacer {0} {1} ({2}) ? |
| Files will be moved to: | Les fichiers seront déplacés vers : |
| Delete {0} {1} ({2})? | Supprimer {0} {1} ({2}) ? |
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

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
| InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean n'a pas pu lire assez des enregistrements de Windows Installer pour savoir avec certitude ce qui sert encore : la liste des programmes installés est revenue incomplète, et lire ces mêmes enregistrements directement dans le registre a également donné des erreurs. Un fichier pourrait sembler orphelin uniquement parce que l'enregistrement qui le nomme faisait partie des illisibles, donc InstallerClean s'est arrêté. Rien n'a été supprimé. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. |
| Invalid destination | Destination invalide |
| Could not write to destination | Impossible d'écrire dans la destination |
| Move failed | Échec du déplacement |
| Delete failed | Échec de la suppression |
| Setting not saved | Paramètre non enregistré |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | La modification n'a pas pu être enregistrée. Au prochain démarrage, InstallerClean reviendra au paramètre précédent. |
| The destination cannot be inside the Windows Installer folder. | La destination ne peut pas se trouver dans le dossier Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows a signalé une erreur de fichier ; le fichier a été laissé en place. |
| Windows reported file errors; these files were left in place. | Windows a signalé des erreurs de fichier ; ces fichiers ont été laissés en place. |
| Something went wrong with this file; it was left in place. | Un problème est survenu avec ce fichier ; il a été laissé en place. |
| Something went wrong with these files; they were left in place. | Un problème est survenu avec ces fichiers ; ils ont été laissés en place. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Refus de déplacer des fichiers dans le dossier Windows Installer (destination : {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

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
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
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
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
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
| Backup folder | Backup folder |
| Products | Produits |
| Patches | Correctifs |
| Product details | Détails du produit |
| Backup folder | Backup folder |
| Operation progress | Progression de l'opération |
| Scan {InstallerFolder} again | Analyser à nouveau {InstallerFolder} |
| Scanning progress | Progression de l'analyse |
| Startup scan progress | Progression de l'analyse de démarrage |
| Details, unneeded files | Détails, fichiers inutiles |
| Available for cleanup. | Disponibles pour le nettoyage. |
| Details, registered files | Détails, fichiers enregistrés |
| Read-only inventory. | Inventaire en lecture seule. |
| Sorted by {0}, ascending | Trié par {0}, ordre croissant |
| Sorted by {0}, descending | Trié par {0}, ordre décroissant |
| Scan results | Résultats de l'analyse |
| Result details | Détails du résultat |
| File details | Détails du fichier |
| Product details | Product details |
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
| Error: unknown argument '{0}' | Error: unknown argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Erreur : argument supplémentaire inattendu « {0} ». Si votre dossier de déplacement contient un espace, mettez le chemin entier entre guillemets : /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. |
| Cancelling... | Annulation... |
| Cancelled. | Annulé. |
| Error: unexpected failure ({0}). Details written to {1}. | Error: unexpected failure ({0}). Details written to {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Error: unexpected failure ({0}). The crash log could not be written. |
| Scanning {InstallerFolder}... | Analyse de {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Found {0} unneeded {1} to clean up ({2}). |
| Found no unneeded files. | Found no unneeded files. |
| {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. | {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. |
| {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. | {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. |
| Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Deleting {0} unneeded {1}... | Deleting {0} unneeded {1}... |
| Permanently deleted {0} unneeded {1}. | Permanently deleted {0} unneeded {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Erreur : aucune destination de déplacement spécifiée. Utilisez /m CHEMIN. (Une valeur par défaut définie dans l'interface est propre à chaque utilisateur et ne s'applique pas aux exécutions planifiées ou par compte de service.) |
| Error: destination cannot be inside the Windows Installer folder. | Erreur : la destination ne peut pas se trouver dans le dossier Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Erreur : la destination doit être un chemin entièrement qualifié. Reçu : {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. |
| Moving {0} unneeded {1} to {2}... | Moving {0} unneeded {1} to {2}... |
| Moved {0} unneeded {1}. | Moved {0} unneeded {1}. |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Un autre processus InstallerClean détient le verrou d'instance unique (l'interface ou une autre exécution de la CLI). Code de sortie 75 (transitoire) ; vous pouvez réessayer plus tard sans risque. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Remarque : l'écriture dans le journal des événements a échoué. Vérifiez les autorisations du journal Application ou la stratégie de groupe. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - nettoyage de {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Removes cached .msi and .msp files that no installed program still needs. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Needs an elevated (administrator) prompt; Windows will not start it. |
| Usage: | Utilisation : |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Affiche cette aide (accepte aussi /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Affiche la version (accepte aussi -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m CHEMIN  Déplace vers le chemin spécifié |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. |
| Exit codes: | Codes de sortie : |
|   0   success: the run finished with nothing left to do |   0   success: the run finished with nothing left to do |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partial: some processed, some not (a failure or a Ctrl+C part way) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitoire : quelque chose a bloqué l'exécution (voir le message) |
|   130 cancelled (Ctrl+C) |   130 annulé (Ctrl+C) |
