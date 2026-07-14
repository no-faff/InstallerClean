# InstallerClean in Français (French)

The text of InstallerClean's interface and command-line tool in English on the left, with the French translation beside it, grouped by where each line appears in the app. It is here so someone who really knows French can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.fr.resx`](../../src/InstallerClean.Core/Resources/Strings.fr.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | Français |
| --- | --- |
| InstallerClean | InstallerClean |
| About | À propos |
| Registered files that should not be deleted | Fichiers enregistrés qui ne devraient pas être supprimés |
| Unneeded files that are safe to delete | Fichiers inutiles que vous pouvez supprimer sans risque |
| Confirm move | Confirmer le déplacement |
| Confirm delete | Confirmer la suppression |
| Recycle Bin unavailable | Corbeille indisponible |

## Section headings

| English | Français |
| --- | --- |
| PRODUCTS | PRODUITS |
| PATCHES | CORRECTIFS |
| PRODUCT DETAILS | DÉTAILS DU PRODUIT |
| MOVE LOCATION | EMPLACEMENT DE DESTINATION |
| SAY THANKS | DIRE MERCI |

## Buttons and actions

| English | Français |
| --- | --- |
| _About | À pr_opos |
| Copy | Copier |
| Cut | Couper |
| Paste | Coller |
| Select all | Tout sélectionner |
| _Browse... | _Parcourir... |
| _Cancel | _Annuler |
| Check for _updates | Rechercher des _mises à jour |
| _Close | _Fermer |
| _Delete | _Supprimer |
| _Delete permanently | _Supprimer définitivement |
| _Done | _Terminé |
| Details | Détails |
| _Buy me a cuppa | Offrez-moi un _café |
| Leave a _star on GitHub | _Laisser une étoile sur GitHub |
| MIT licence | Licence MIT |
| _Move | _Déplacer |
| _Move instead | _Déplacer plutôt |
| Path to folder if you Move instead of Delete | Chemin du dossier si vous déplacez au lieu de supprimer |
| Open _release page | Ouvrir la page de la _version |
| _Re-scan | _Réanalyser |
| _Scan again | Analyser à _nouveau |
| Send report | Envoyer le rapport |
| _Send | _Envoyer |

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
| Moving {0} {1}... | Déplacement de {0} {1}... |
| Deleting {0} {1}... | Suppression de {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Déplacement annulé après avoir traité {0} sur {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Suppression annulée après avoir traité {0} sur {1} {2}. |
| Move failed ({0}). Details in {1}. | Échec du déplacement ({0}). Détails dans {1}. |
| Move failed ({0}). The crash log could not be written. | Échec du déplacement ({0}). Le crash.log n'a pas pu être écrit. |
| Delete failed ({0}). Details in {1}. | Échec de la suppression ({0}). Détails dans {1}. |
| Delete failed ({0}). The crash log could not be written. | Échec de la suppression ({0}). Le crash.log n'a pas pu être écrit. |
| Access denied. Windows refused the scan. | Access denied. Windows refused the scan. |
| Scan failed: installer database unavailable. | Échec de l'analyse : base de données Windows Installer indisponible. |
| Scan cancelled. | Analyse annulée. |
| Ready | Prêt |
| Scan failed ({0}). Details in {1}. | Échec de l'analyse ({0}). Détails dans {1}. |
| Scan failed ({0}). The crash log could not be written. | Échec de l'analyse ({0}). Le crash.log n'a pas pu être écrit. |

## Main screen text

| English | Français |
| --- | --- |
| The unneeded files below are safe to delete. | Les fichiers inutiles ci-dessous peuvent être supprimés sans risque. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Ils se trouvent dans C:\Windows\Installer, laissés là quand un programme a été désinstallé ({0}), qu'un correctif plus récent en a remplacé un ({1}) ou que l'éditeur l'a retiré ({2}). InstallerClean ne liste jamais que les fichiers dont Windows lui-même déclare avoir fini de se servir. |
| Delete them to the Recycle Bin, or use Move instead if you'd rather keep a copy. | Supprimez-les vers la Corbeille, ou utilisez plutôt Déplacer si vous préférez en garder une copie. |
| Nothing scanned yet. | Nothing scanned yet. |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | Quelque chose utilise Windows Installer en ce moment, généralement une mise à jour Windows ou un programme en cours d'installation en arrière-plan. Déplacer et Supprimer sont en pause pendant ce temps, pour qu'InstallerClean ne touche pas au cache d'installation tant qu'il change. Une fois terminé, réanalysez et ils réapparaissent. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Une transaction Windows Installer précédente est suspendue sur cette machine. Reprenez ou annulez cette installation (ou redémarrez Windows) avant de nettoyer le cache. |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows a un renommage de fichier en file d'attente pour le prochain redémarrage qui affecte le cache d'installation. Redémarrez Windows avant de nettoyer. |
| Select a file to view details. | Sélectionnez un fichier pour voir les détails. |
| Select a product to view details. | Sélectionnez un produit pour voir les détails. |
| No metadata available. | Aucune métadonnée disponible. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | Ce fichier d'installation a été supprimé. Ce n'est pas InstallerClean qui l'a fait : il ne retire jamais un fichier dont un programme a encore besoin ; autre chose a supprimé celui-ci avant que vous ne lanciez InstallerClean.<br><br>Pour l'instant, cela ne pose aucun problème, et n'en posera pas avant le jour où vous tenterez de réparer, mettre à jour ou désinstaller le programme auquel il appartient. Cette étape pourra alors échouer, parce que Windows cherche ce fichier et qu'il n'est pas là.<br><br>Pour tenter d'y remédier, téléchargez le programme d'installation de ce logiciel chez son éditeur et lancez-le par-dessus votre copie actuelle (ne désinstallez pas d'abord : désinstaller est en soi une étape qui a besoin de ce fichier). Utilisez si possible la version que vous avez installée, car Windows pourrait en refuser une autre. Cela rétablit en général le fichier, et vos réglages restent normalement intacts, mais Microsoft ne le garantit pas : son propre dernier recours est de réinstaller le programme, ou Windows lui-même. |
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
| Nothing to clean up in C:\Windows\Installer | Rien à nettoyer dans C:\Windows\Installer |
| Scanned {0} {1} in {2} | Analyse de {0} {1} en {2} |
| Copy them back if anything breaks ([it won't!]). | Recopiez-les si quelque chose ne fonctionne plus ([ce ne sera pas le cas !]). |
| Until then, you can restore them if anything breaks ([it won't!]). | D'ici là, vous pouvez les restaurer si quelque chose ne fonctionne plus ([ce ne sera pas le cas !]). |
| Empty it to actually reclaim the space. | Videz la Corbeille pour vraiment récupérer l'espace. |
| {0} freed | {0} libérés |
| {0} cleaned up | {0} nettoyés |
| {0} moved | {0} déplacés |
| {0} moved, some files could not be processed | {0} déplacés, certains fichiers n'ont pas pu être traités |
| {0} freed, some files could not be processed | {0} libérés, certains fichiers n'ont pas pu être traités |
| {0} cleaned up, some files could not be processed | {0} nettoyés, certains fichiers n'ont pas pu être traités |
| {0} {1} moved to: {2} | Déplacé {0} {1} vers : {2} |
| {0} {1} moved to: {2} | Déplacé {0} {1} vers : {2} |
| {0} {1} moved to: {2}. {3} {4} | Déplacé {0} {1} vers : {2}. {3} {4} |
| {0} {1} moved to: {2}. {3} {4} | Déplacé {0} {1} vers : {2}. {3} {4} |
| {0} {1} moved to the Recycle Bin | Déplacé {0} {1} vers la Corbeille |
| {0} {1} moved to the Recycle Bin | Déplacé {0} {1} vers la Corbeille |
| {0} {1} moved to the Recycle Bin. {2} {3} | Déplacé {0} {1} vers la Corbeille. {2} {3} |
| {0} {1} moved to the Recycle Bin. {2} {3} | Déplacé {0} {1} vers la Corbeille. {2} {3} |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | {0} {1} supprimé définitivement. Il n'est pas allé à la Corbeille. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | {0} {1} supprimés définitivement. Ils ne sont pas allés à la Corbeille. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. {2} {3} | {0} {1} supprimé définitivement. Il n'est pas allé à la Corbeille. {2} {3} |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. {2} {3} | {0} {1} supprimés définitivement. Ils ne sont pas allés à la Corbeille. {2} {3} |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | C'est normal, il n'y avait aucun risque à le retirer. InstallerClean n'efface que les fichiers dont Windows déclare avoir fini de se servir, jamais un dont un programme a encore besoin. Dans le cas improbable où une suppression empêcherait un jour un programme de se réparer, se mettre à jour ou se désinstaller, le réinstaller depuis son éditeur rétablit en général le fichier, même si Microsoft ne le garantit pas. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | C'est normal, il n'y avait aucun risque à les retirer. InstallerClean n'efface que les fichiers dont Windows déclare avoir fini de se servir, jamais un dont un programme a encore besoin. Dans le cas improbable où une suppression empêcherait un jour un programme de se réparer, se mettre à jour ou se désinstaller, le réinstaller depuis son éditeur rétablit en général le fichier, même si Microsoft ne le garantit pas. |

## Recycle Bin unavailable

| English | Français |
| --- | --- |
| The Recycle Bin isn't available for this drive | La Corbeille n'est pas disponible pour ce lecteur |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Donc ce {1} ({2}) n'a pas été supprimé. Vous pouvez le déplacer en lieu sûr, ou le supprimer définitivement. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Donc ces {0} {1} ({2}) n'ont pas été supprimés. Vous pouvez les déplacer en lieu sûr, ou les supprimer définitivement. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Le supprimer ne présente aucun risque. InstallerClean n'efface que les fichiers dont Windows déclare avoir fini de se servir, jamais un dont un programme a encore besoin, et la Corbeille n'est qu'une protection supplémentaire. Dans le cas improbable où une suppression empêcherait un jour un programme de se réparer, se mettre à jour ou se désinstaller, le réinstaller depuis son éditeur rétablit en général le fichier, même si Microsoft ne le garantit pas. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Les supprimer ne présente aucun risque. InstallerClean n'efface que les fichiers dont Windows déclare avoir fini de se servir, jamais un dont un programme a encore besoin, et la Corbeille n'est qu'une protection supplémentaire. Dans le cas improbable où une suppression empêcherait un jour un programme de se réparer, se mettre à jour ou se désinstaller, le réinstaller depuis son éditeur rétablit en général le fichier, même si Microsoft ne le garantit pas. |

## Summaries and counts

| English | Français |
| --- | --- |
| {0} file still needed | {0} fichier encore nécessaire |
| {0} files still needed | {0} fichiers encore nécessaires |
| {0} unneeded file to clean up | {0} fichier inutile à nettoyer |
| {0} unneeded files to clean up | {0} fichiers inutiles à nettoyer |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} fichier enregistré est manquant (non supprimé par InstallerClean). Aucun problème pour l'instant, mais une future réparation, mise à jour ou désinstallation de ce programme pourrait échouer. Ouvrez les Détails pour savoir quoi faire. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} fichiers enregistrés sont manquants (non supprimés par InstallerClean). Aucun problème pour l'instant, mais une future réparation, mise à jour ou désinstallation de ces programmes pourrait échouer. Ouvrez les Détails pour savoir quoi faire. |
| {0} stale MSI entry detected (file already gone from disk; InstallerClean doesn't unregister it). | {0} entrée MSI obsolète détectée (le fichier a déjà disparu du disque ; InstallerClean ne la retire pas du registre). |
| {0} stale MSI entries detected (files already gone from disk; InstallerClean doesn't unregister them). | {0} entrées MSI obsolètes détectées (les fichiers ont déjà disparu du disque ; InstallerClean ne les retire pas du registre). |
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
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | Les fichiers seront déplacés vers la Corbeille. Si vous voulez des copies de sauvegarde, utilisez plutôt le bouton Déplacer. |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. |

## Error messages

| English | Français |
| --- | --- |
| Access denied | Access denied |
| Windows refused InstallerClean access. InstallerClean is already running as administrator, so starting it again that way won't help.<br><br>That leaves two likely causes: security software is holding C:\Windows\Installer, or the folder's permissions have been changed. Pausing the security software and trying again is the quickest one to rule out. | Windows refused InstallerClean access. InstallerClean is already running as administrator, so starting it again that way won't help.<br><br>That leaves two likely causes: security software is holding C:\Windows\Installer, or the folder's permissions have been changed. Pausing the security software and trying again is the quickest one to rule out. |
| Installer database unavailable | Base de données Windows Installer indisponible |
| Scan failed | Échec de l'analyse |
| The Windows Installer database appears to be empty or inaccessible. This is unusual even on a fresh Windows install and typically means the database is corrupt or a third-party tool has cleared it. Running 'sfc /scannow' from an elevated prompt usually repairs it. | La base de données Windows Installer semble vide ou inaccessible. C'est inhabituel même sur une installation neuve de Windows et signifie généralement que la base de données est corrompue ou qu'un outil tiers l'a vidée. Exécuter « sfc /scannow » depuis une invite élevée la répare en général. |
| Windows Installer refused to list the installed products, and InstallerClean is already running as administrator, so running it again won't help. The permissions on Windows's own installer records may have been changed, or security software may be blocking them. Running 'sfc /scannow' from an elevated prompt is worth a try. | Windows Installer refused to list the installed products, and InstallerClean is already running as administrator, so running it again won't help. The permissions on Windows's own installer records may have been changed, or security software may be blocking them. Running 'sfc /scannow' from an elevated prompt is worth a try. |
| Windows Installer refused to list products after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installer a refusé de lister les produits après {0} échecs consécutifs (dernier code d'erreur {1}). Essayez de redémarrer Windows, ou exécutez « sfc /scannow » depuis une invite élevée. |
| Windows Installer refused to list a product's patches after {0} consecutive failures (last error code {1}). Try restarting Windows, or run 'sfc /scannow' from an elevated prompt. | Windows Installer a refusé de lister les correctifs d'un produit après {0} échecs consécutifs (dernier code d'erreur {1}). Essayez de redémarrer Windows, ou exécutez « sfc /scannow » depuis une invite élevée. |
| Invalid destination | Destination invalide |
| Could not write to destination | Impossible d'écrire dans la destination |
| Move failed | Échec du déplacement |
| Delete failed | Échec de la suppression |
| The destination cannot be inside the Windows Installer folder. | La destination ne peut pas se trouver dans le dossier Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | La destination {0} se trouve dans un dossier système de Windows. Choisissez un chemin en dehors de %SystemRoot%, %ProgramFiles% et %ProgramData%. |
| Not enough space | Espace insuffisant |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Espace insuffisant dans {0}<br><br>Nécessaire : {1}<br>Disponible : {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | Vous n'avez pas la permission d'écrire dans {0}.<br>Essayez un dossier dans votre profil utilisateur ou sur un lecteur qui vous appartient. |
| The path {0} is too long for Windows. Pick a shorter path. | Le chemin {0} est trop long pour Windows. Choisissez un chemin plus court. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | Le dossier {0} n'existe pas et n'a pas pu être créé. Vérifiez la lettre de lecteur ou le chemin réseau. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows ne peut pas écrire dans {0}.<br>Détails dans {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows ne peut pas écrire dans {0}. Le crash.log n'a pas pu être écrit. |
| Cannot write to {0}.<br>Details in {1}. | Impossible d'écrire dans {0}.<br>Détails dans {1}. |
| Cannot write to {0}. The crash log could not be written. | Impossible d'écrire dans {0}. Le crash.log n'a pas pu être écrit. |
| File no longer exists. | Le fichier n'existe plus. |
| Source file is a symlink or junction; refused for safety. | Le fichier source est un lien symbolique ou une jonction ; refusé par sécurité. |
| Access denied. | Accès refusé. |
| The operation failed. Try again or restart Windows. | L'opération a échoué. Réessayez ou redémarrez Windows. |
| Unknown error. | Erreur inconnue. |
| Couldn't move this file to the Recycle Bin (error {0}). It may be locked, in use or blocked by Windows. Use the Move button instead. | Impossible de déplacer ce fichier vers la Corbeille (erreur {0}). Il est peut-être verrouillé, en cours d'utilisation ou bloqué par Windows. Utilisez plutôt le bouton Déplacer. |
| Windows blocked access to this file, even with administrator rights (error {0}). It is usually an ownership or permissions lock. Use the Move button instead. | Windows a bloqué l'accès à ce fichier, même avec des droits d'administrateur (erreur {0}). C'est généralement un verrou de propriété ou de permissions. Utilisez plutôt le bouton Déplacer. |
| This file is open or locked by another program (error {0}). Close that program, or whatever is scanning it, then try again, or use the Move button instead. | Ce fichier est ouvert ou verrouillé par un autre programme (erreur {0}). Fermez ce programme, ou ce qui l'analyse, puis réessayez, ou utilisez plutôt le bouton Déplacer. |
| The file was permanently deleted because it could not be moved to the Recycle Bin. | Le fichier a été supprimé définitivement parce qu'il n'a pas pu être déplacé vers la Corbeille. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Refus de déplacer des fichiers dans le dossier Windows Installer (destination : {0}). |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
| Cannot write to {0}. | Impossible d'écrire dans {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Impossible de trouver un nom de fichier unique pour « {0} » après 10 000 tentatives. |

## Update check

| English | Français |
| --- | --- |
| Check for updates | Rechercher des mises à jour |
| Checking... | Vérification... |
| Up to date. | À jour. |
| Update available | Mise à jour disponible |
| You're running version {0}.<br>Version {1} is available. | Vous utilisez la version {0}.<br>La version {1} est disponible. |
| Couldn't reach GitHub. Check your internet connection and try again. | Impossible de joindre GitHub. Vérifiez votre connexion internet et réessayez. |
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | GitHub a renvoyé une réponse d'erreur. L'API des versions est peut-être limitée ; réessayez dans quelques minutes. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | La réponse de GitHub ne contenait pas de version reconnaissable. Réessayez plus tard, ou ouvrez directement la page des versions. |
| The check timed out. Your connection to GitHub may be slow; try again. | La vérification a expiré. Votre connexion à GitHub est peut-être lente ; réessayez. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | La vérification a échoué pour une raison inconnue. Les détails sont dans le crash.log si vous devez le signaler. |

## Opening links in your browser

| English | Français |
| --- | --- |
| Couldn't open your browser | Impossible d'ouvrir votre navigateur |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} |

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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log capture les exceptions non gérées d'InstallerClean.<br># Sous élévation, les messages d'exception du framework peuvent<br># inclure des chemins de fichiers de la session en cours (y compris<br># les profils d'autres utilisateurs énumérés par les requêtes Windows<br># Installer). Les messages d'échec réseau de la vérification de mises<br># à jour ou de l'envoi du rapport de résultats peuvent inclure l'URL<br># de destination et l'adresse IP / proxy résolue. Expurgez ces deux<br># types de détail avant de joindre ce fichier à un rapport de bug<br># public.<br> |

## Tooltips (hover text)

| English | Français |
| --- | --- |
| Donate | Faire un don |
| It's thirsty work! | Ça donne soif ! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Annulation demandée. InstallerClean attend que l'étape en cours atteigne un point d'arrêt. Cela peut prendre quelques secondes lors d'opérations d'E/S intensives ou d'un appel à la base de données MSI. |
| Close | Fermer |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | Laissez une étoile sur GitHub, ouvrez une Issue ou écrivez dans les Discussions. Tout retour est le bienvenu. |
| or report an Issue or post in Discussions. Any feedback welcome. | ou ouvrez une Issue ou écrivez dans les Discussions. Tout retour est le bienvenu. |
| Minimise | Réduire |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Comme vous voulez, mais c'est apprécié. Envoie un résumé anonyme qui me dit juste si l'outil fonctionne et combien d'espace les gens libèrent. L'écran suivant vous montre ce qui sera envoyé avant que vous confirmiez. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Comme vous voulez, mais c'est apprécié. Envoie un résumé anonyme qui me dit juste si l'outil fonctionne. L'écran suivant vous montre ce qui sera envoyé avant que vous confirmiez. |
| Move the unneeded files to the Move location. | Déplacer les fichiers inutiles vers l'emplacement de destination. |
| Move the unneeded files to the Move location. Choose one first. | Déplacer les fichiers inutiles vers l'emplacement de destination. Choisissez-en un d'abord. |
| Move the unneeded files to the Recycle Bin. | Déplacer les fichiers inutiles vers la Corbeille. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nom du titulaire du certificat Authenticode incorporé. Chaîne non vérifiée. |
| Change language. The program will restart. | Changer la langue. Le programme redémarrera. |

## Screen reader labels

| English | Français |
| --- | --- |
| Donate | Faire un don |
| Buy me a cuppa (About window) | Offrez-moi un café (fenêtre À propos) |
| Cancel operation | Annuler l'opération |
| Cancel scan | Annuler l'analyse |
| Cancel startup scan | Annuler l'analyse de démarrage |
| Close | Fermer |
| Close window | Fermer la fenêtre |
| Close result and return to main window | Fermer le résultat et revenir à la fenêtre principale |
| Leave a star on GitHub | Laisser une étoile sur GitHub |
| Leave a star on GitHub (About window) | Laisser une étoile sur GitHub (fenêtre À propos) |
| Minimise | Réduire |
| Move all unneeded installer files to the chosen destination folder | Déplacer tous les fichiers d'installation inutiles vers le dossier de destination choisi |
| Move all unneeded installer files to the Recycle Bin | Déplacer tous les fichiers d'installation inutiles vers la Corbeille |
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | Supprimer déplace les fichiers inutiles vers la Corbeille. Annuler ferme sans rien supprimer. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Déplacer place les fichiers inutiles dans le dossier de destination choisi. Annuler les laisse où ils sont. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Choisissez quoi faire des fichiers inutiles : les déplacer en lieu sûr, les supprimer définitivement ou annuler. |
| Move the unneeded files to a folder you choose | Déplacer les fichiers inutiles vers un dossier que vous choisissez |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Supprimer définitivement les fichiers inutiles parce que la Corbeille est indisponible pour ce lecteur |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Envoie à nofaff.netlify.app. Uniquement des décomptes et des étiquettes. Vous verrez exactement ce qui sera envoyé avant l'envoi. |
| Say thanks | Dire merci |
| Send posts the report shown to No Faff. Cancel sends nothing. | Envoyer transmet à No Faff le rapport affiché. Annuler n'envoie rien. |
| Check for updates | Rechercher des mises à jour |
| Checks the GitHub releases API over HTTPS for a newer version. | Interroge l'API des versions de GitHub en HTTPS pour une version plus récente. |
| Open the release page to download the newer version, or cancel to keep the current version. | Ouvrez la page de la version pour télécharger la version plus récente, ou annulez pour conserver la version actuelle. |
| MIT licence | Licence MIT |
| Opens the licence file on github.com in your browser. | Ouvre le fichier de licence sur github.com dans votre navigateur. |
| Move location | Emplacement de destination |
| Products | Produits |
| Patches | Correctifs |
| Product details | Détails du produit |
| Move destination folder | Dossier de destination |
| Operation progress | Progression de l'opération |
| Scan C:\Windows\Installer again | Analyser à nouveau C:\Windows\Installer |
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
| Dialog text | Texte de la boîte de dialogue |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Fichiers qui n'ont pas pu être traités |
| Explains this folder, and how to recover a file, in the README | Explique ce dossier, et comment récupérer un fichier, dans le README |
| Result log preview | Aperçu du rapport de résultats |
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
| Unknown argument: '{0}' | Argument inconnu : « {0} » |
| Cancelling... | Annulation... |
| Cancelled. | Annulé. |
| Error: {0}. Details written to {1}. | Erreur : {0}. Détails écrits dans {1}. |
| Error: {0}. The crash log could not be written. | Erreur : {0}. Le crash.log n'a pas pu être écrit. |
| Scanning C:\Windows\Installer... | Analyse de C:\Windows\Installer... |
| Found {0} {1} to clean up ({2}). | Trouvé {0} {1} à nettoyer ({2}). |
| Nothing to do. | Rien à faire. |
| Deleting {0} {1}... | Suppression de {0} {1}... |
| Deleted {0} {1}. | Supprimé {0} {1}. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Erreur : la Corbeille est indisponible pour ce volume, donc rien n'a été supprimé. Utilisez /m pour déplacer les fichiers, ou réactivez la Corbeille et relancez. |
| Error: no move destination specified. Use /m PATH or set a default in the GUI. | Erreur : aucune destination de déplacement spécifiée. Utilisez /m CHEMIN ou définissez une valeur par défaut dans l'interface. |
| Error: destination cannot be inside the Windows Installer folder. | Erreur : la destination ne peut pas se trouver dans le dossier Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Erreur : la destination doit être un chemin entièrement qualifié. Reçu : {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Erreur : la destination {0} se trouve dans un dossier système de Windows. Choisissez un chemin en dehors de %SystemRoot%, %ProgramFiles% et %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Erreur : quelque chose utilise Windows Installer en ce moment, généralement une mise à jour Windows ou un programme en cours d'installation en arrière-plan. Déplacer et Supprimer sont bloqués pendant ce temps. Réessayez une fois que c'est terminé. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Erreur : une transaction Windows Installer précédente est suspendue sur cette machine. Reprenez ou annulez cette installation (ou redémarrez Windows) avant de nettoyer le cache. |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Erreur : une opération de fichier en file d'attente pour le prochain redémarrage vise le cache d'installation ({0}). Redémarrez Windows pour terminer cette opération avant de nettoyer. |
| Moving {0} {1} to {2}... | Déplacement de {0} {1} vers {2}... |
| Moved {0} {1}. | Déplacé {0} {1}. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Un autre processus InstallerClean détient le verrou d'instance unique (l'interface ou une autre exécution de la CLI). Code de sortie 75 (transitoire) ; vous pouvez réessayer plus tard sans risque. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Remarque : l'écriture dans le journal des événements a échoué. Vérifiez les autorisations du journal Application ou la stratégie de groupe. |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - nettoyage de C:\Windows\Installer |
| Usage: | Utilisation : |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help      Affiche cette aide (accepte aussi /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version   Affiche la version (accepte aussi -v) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s          Analyse seule - liste les fichiers inutiles |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d          Supprime les fichiers inutiles (Corbeille) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m          Déplace vers l'emplacement par défaut enregistré |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m CHEMIN   Déplace vers le chemin spécifié |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli est un vrai processus de console et bloque l'invite |
| until it finishes; redirect or pipe its output as you would any | jusqu'à la fin ; redirigez ou acheminez sa sortie comme vous le feriez |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | pour tout autre exécutable de console. L'interface se trouve dans InstallerClean.exe. |
| Exit codes: | Codes de sortie : |
|   0   success: every flagged file was processed |   0   succès : tous les fichiers signalés ont été traités |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   échec : rien de traité (arguments incorrects, échec de l'analyse, tous les fichiers ont échoué) |
|   2   partial: some files processed, some failed |   2   partiel : certains fichiers traités, d'autres ont échoué |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitoire : une condition temporaire a bloqué l'exécution (voir le message) |
|   130 cancelled (Ctrl+C) |   130 annulé (Ctrl+C) |
