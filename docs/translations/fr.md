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
| The anonymous report | Le rapport anonyme |
| InstallerClean couldn't read the report. | InstallerClean n'a pas pu lire le rapport. |

## Section headings

| English | Français |
| --- | --- |
| PATCHES | CORRECTIFS |
| PRODUCT DETAILS | DÉTAILS DU PRODUIT |
| BACKUP FOLDER | DOSSIER DE SAUVEGARDE |
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
| Carry on _without it | Continuer _sans attendre |
| Check for _updates | Rechercher des _mises à jour |
| _Close | _Fermer |
| _Close without donating | _Fermer sans faire de don |
| _Delete permanently | _Supprimer définitivement |
| _Done | _Terminé |
| Details | Détails |
| _Donate | Faire un _don |
| _Donate $3 | Faire un _don de 3 $ |
| Donate | Faire un don |
| Leave a _star on GitHub | _Laisser une étoile sur GitHub |
| Apache 2.0 licence | Licence Apache 2.0 |
| _Move | _Déplacer |
| Path to folder if you move rather than delete. | Chemin du dossier si vous déplacez plutôt que supprimez. |
| _Re-scan | _Réanalyser |

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
| (no program) | (aucun programme) |
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
| Checking registry for additional packages... | Vérification du registre pour des paquets supplémentaires... |
| Matching files against installed programs... | Comparaison des fichiers avec les programmes installés... |
| {0} of {1} | {0} sur {1} |
| Checking remaining files... | Vérification des fichiers restants... |
| Waiting for drive {0} to respond... | En attente de la réponse du lecteur {0}... |
| Waiting for {0} to respond... | En attente de la réponse de {0}... |
| Any file still to be checked against it will be left alone. | Tout fichier qui reste à vérifier par rapport à ce qui s'y trouve sera laissé de côté. |
| Scan complete ({0}) | Analyse terminée ({0}) |
| Found {0} {1} you can safely delete. | {0} {1} à supprimer sans risque. |
| Preparing destination folder... | Préparation du dossier de destination... |
| Moving unneeded files... | Déplacement des fichiers inutiles... |
| Deleting unneeded files... | Suppression des fichiers inutiles... |
| Move cancelled. {0} of {1} {2} processed. | Déplacement annulé après avoir traité {0} sur {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Suppression annulée après avoir traité {0} sur {1} {2}. |
| {0}. Details are in {1}. | {0}. Détails dans {1}. |
| {0}. The crash log could not be written. | {0}. Le crash.log n'a pas pu être écrit. |
| {0}. Details are in {1}. | {0}. Détails dans {1}. |
| {0}. The crash log could not be written. | {0}. Le crash.log n'a pas pu être écrit. |
| Access denied. Windows refused the scan. | Accès refusé. Windows a refusé l'analyse. |
| Scan stopped. | Analyse arrêtée. |
| Scan cancelled. | Analyse annulée. |
| Ready | Prêt |
| Scan failed ({0}). Details in {1}. | Échec de l'analyse ({0}). Détails dans {1}. |
| Scan failed ({0}). The crash log could not be written. | Échec de l'analyse ({0}). Le crash.log n'a pas pu être écrit. |

## Main screen text

| English | Français |
| --- | --- |
| Any unneeded files below are safe to delete. | Tous les fichiers inutiles ci-dessous sont supprimables sans risque. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Ils se trouvent dans {InstallerFolder}. InstallerClean interroge Windows sur chaque programme installé : un fichier est listé quand aucun programme ne le revendique ({0}), ou quand un correctif plus récent l'a remplacé et qu'aucun programme ne pourrait revenir à lui ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Déplacez-les vers un dossier de sauvegarde de votre choix, puis supprimez ce dossier dès que vous serez convaincu que vos programmes se mettent toujours à jour et se désinstallent normalement. Les remettre dans {InstallerFolder} restaure tout. Ou supprimez-les définitivement maintenant. |
| Nothing scanned yet. | Rien n'a encore été analysé. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Cliquez sur Réanalyser pour parcourir {InstallerFolder} à la recherche de fichiers d'installation dont aucun programme n'a plus besoin. |
| Nothing to show until the next scan. | Rien à afficher avant la prochaine analyse. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | L'analyse qui suit Déplacer ou Supprimer ne s'est pas terminée, il n'y a donc aucune liste à afficher. Cliquez sur Réanalyser pour analyser de nouveau {InstallerFolder}. |
| What stopped it is recorded in {0}. | Ce qui l'a interrompue est consigné dans {0}. |
| These files can't be cleaned up right now. | Ces fichiers ne peuvent pas être nettoyés pour le moment. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Quelque chose utilise Windows Installer en ce moment, par exemple une mise à jour de Windows ou un programme qui s'installe en arrière-plan. Déplacer et Supprimer sont en pause pendant ce temps, pour qu'InstallerClean ne touche pas à {InstallerFolder} pendant qu'il change. Une fois terminé, réanalysez et ils reviennent. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows a refusé à InstallerClean l'autorisation de vérifier si Windows Installer est occupé, donc Déplacer et Supprimer sont en pause et InstallerClean ne touchera pas à {InstallerFolder}. Redémarrez Windows puis Réanalyser. Si Windows refuse toujours, ce n'est pas une machine qu'InstallerClean peut nettoyer. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean n'a pas pu vérifier si Windows Installer est occupé, donc Déplacer et Supprimer sont en pause et InstallerClean ne touchera pas à {InstallerFolder}. Redémarrez Windows puis Réanalyser. S'il ne peut toujours pas vérifier, ce n'est pas une machine qu'InstallerClean peut nettoyer. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean n'a pas pu vérifier si Windows Installer est occupé, car Windows a renvoyé une erreur : {0} (erreur {1}). Déplacer et Supprimer sont en pause et InstallerClean ne touchera pas à {InstallerFolder}. Redémarrez Windows puis Réanalyser. S'il ne peut toujours pas vérifier, ce n'est pas une machine qu'InstallerClean peut nettoyer. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Une transaction Windows Installer précédente est suspendue sur cette machine. Reprenez ou annulez cette installation (ou redémarrez Windows) avant de nettoyer {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows a mis en file d'attente pour le prochain redémarrage un renommage de fichier qui concerne {InstallerFolder}. Redémarrez Windows avant de nettoyer. |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | Une opération sur fichiers est en file d'attente pour le prochain redémarrage et InstallerClean ne peut pas savoir quels fichiers elle nomme, il ne peut donc pas exclure qu'ils soient dans {InstallerFolder}. Redémarrez Windows avant de nettoyer. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean n'a pas pu lire l'un des paramètres Windows qu'il vérifie avant de toucher à {InstallerFolder}, il ne peut donc pas savoir si une opération d'installation est en cours ou attend un redémarrage. Redémarrez Windows puis Réanalyser. Si le paramètre reste illisible, ce n'est pas une machine qu'InstallerClean peut nettoyer. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer a quelque chose en cours, donc Déplacer et Supprimer sont en pause. InstallerClean ne touchera pas à {InstallerFolder} pendant qu'il change. Une fois terminé, réanalysez et ils reviennent. |
| Select a file to view details. | Sélectionnez un fichier pour voir les détails. |
| Select a product to view details. | Sélectionnez un produit pour voir les détails. |
| No metadata available. | Aucune métadonnée disponible. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Ce fichier d'installation est manquant. Il ne cause aucun problème pour l'instant, et n'en causera pas avant le jour où vous essaierez de mettre à jour ou de désinstaller le programme auquel il appartient. Cette étape peut alors échouer, parce que Windows cherche ce fichier et qu'il n'est pas là.<br><br>Pour le remettre, il vous faut l'installateur de la version que vous avez déjà. Procurez-vous-le auprès de l'éditeur du programme et exécutez-le par-dessus votre copie actuelle. Une version plus récente ne fera pas l'affaire : elle devrait d'abord retirer celle que vous avez, et c'est justement l'étape qui a besoin de ce fichier. Désinstaller d'abord ne marchera pas non plus, pour la même raison. Cela devrait restaurer le fichier et laisser vos réglages intacts, mais Microsoft ne le garantit pas. |
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
| Nothing offered on this PC | Rien n'a été proposé sur ce PC |
| One file ({2}) was held back because Windows shows it was changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check it again. | Un fichier ({2}) a été retenu parce que Windows indique qu'il a été modifié après qu'InstallerClean a commencé l'analyse. Si un programme était en cours d'installation ou de mise à jour, relancez une analyse une fois qu'il a terminé et InstallerClean le vérifiera de nouveau. |
| {0} {1} ({2}) were held back because Windows shows they were changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check them again. | {0} {1} ({2}) ont été retenus parce que Windows indique qu'ils ont été modifiés après qu'InstallerClean a commencé l'analyse. Si un programme était en cours d'installation ou de mise à jour, relancez une analyse une fois qu'il a terminé et InstallerClean les vérifiera de nouveau. |
| Delete that folder when you're satisfied all is well. | Supprimez ce dossier dès que vous serez convaincu que tout va bien. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | Supprimez ce dossier dès que vous serez convaincu que tout va bien. Vous ne récupérerez l'espace qu'à ce moment-là. |
| {0} freed | {0} libérés |
| {0} moved | {0} déplacés |
| Nothing was moved | Rien n'a été déplacé |
| Nothing was deleted | Rien n'a été supprimé |
| {0} file could not be moved. | {0} fichier n'a pas pu être déplacé. |
| {0} files could not be moved. | {0} fichiers n'ont pas pu être déplacés. |
| {0} file could not be deleted. | {0} fichier n'a pas pu être supprimé. |
| {0} files could not be deleted. | {0} fichiers n'ont pas pu être supprimés. |
| {0} {1} moved to: {2} | Déplacé {0} {1} vers : {2} |
| {0} {1} moved to: {2} | Déplacé {0} {1} vers : {2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | {0} fichier retenu. L'analyse l'a jugé inutile. La vérification finale n'a pas pu le confirmer. |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | {0} fichiers retenus. L'analyse les a jugés inutiles. La vérification finale n'a pas pu le confirmer. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} laissés en place, parce que Windows a un enregistrement du programme nommé à l'intérieur. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} laissés en place, parce qu'InstallerClean n'a trouvé aucun programme nommé à l'intérieur. |
| Moved {0} of {1} {2} to {3} before you cancelled. | Déplacé {0} sur {1} {2} vers {3} avant votre annulation. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Supprimé définitivement {0} sur {1} {2} avant votre annulation. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | C'est simple à annuler. Redéplacez-les dans {InstallerFolder} et tout redeviendra comme avant. |
| {0} {1} permanently deleted | {0} {1} supprimé définitivement |
| {0} {1} permanently deleted | {0} {1} supprimés définitivement |
| Any small donation is much appreciated | Tout don, même modeste, est très apprécié |
| Send _anonymous report | Envoyer un rapport _anonyme |
| The report adds your result to [this chart] on GitHub, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Le rapport ajoute votre résultat à [ce graphique] sur GitHub, qui montre combien d'espace les gens libèrent avec InstallerClean. Il contient aussi ces détails techniques, qui m'aident à repérer d'éventuels bugs : |
| How much space was freed, and how many files were moved or deleted | La quantité d'espace libérée et le nombre de fichiers déplacés ou supprimés |
| Whether a Move went to the same drive, another drive, a removable drive or a network share | Si Déplacer a envoyé les fichiers vers le même lecteur, un autre lecteur, un lecteur amovible ou un partage réseau |
| How long the scan and the Move or Delete took | La durée de l'analyse et de Déplacer ou Supprimer |
| How many installer files there are of each kind, and how much space they take | Le nombre de fichiers d'installation de chaque type et l'espace qu'ils occupent |
| How many files were left alone, and why | Le nombre de fichiers laissés de côté, et pourquoi |
| How often InstallerClean waited for a slow drive or network share, and how many it stopped waiting for | Combien de fois InstallerClean a attendu un lecteur ou un partage réseau lent, et combien il a cessé d'attendre |
| How many programs and patches Windows has a record of | Le nombre de programmes et de correctifs dont Windows garde un enregistrement |
| Whether Windows still makes old-style short file names | Si Windows crée encore des noms de fichiers courts à l'ancienne |
| Whether Windows is 10 or 11 and x64 or Arm, and which language and country or region it's set to | Si Windows est en version 10 ou 11, x64 ou Arm, et la langue et le pays ou la région qui y sont définis |
| InstallerClean's version, how you got it (setup, portable, slim, Scoop, the command-line tool on its own, built from source, or not known), whether the report was sent from InstallerClean or the command-line tool, and the language it was showing | La version d'InstallerClean, la façon dont vous l'avez obtenu (setup, portable, slim, Scoop, l'outil en ligne de commande seul, compilé depuis les sources, ou inconnu), si le rapport a été envoyé depuis InstallerClean ou depuis l'outil en ligne de commande, et la langue dans laquelle il s'affichait |
| Counts of anything that went wrong | Le décompte de tout ce qui a échoué |
| Each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Chacun de ces éléments est un nombre ou un libellé. Rien dans le rapport ne désigne ni vous, ni votre PC, ni vos fichiers, ni vos programmes, et je ne vois jamais votre adresse IP. |
| See exactly what's sent | Voir exactement ce qui est envoyé |

## Summaries and counts

| English | Français |
| --- | --- |
| {0} file left alone | {0} fichier laissé de côté |
| {0} files left alone | {0} fichiers laissés de côté |
| {0} unneeded file to clean up | {0} fichier inutile à nettoyer |
| {0} unneeded files to clean up | {0} fichiers inutiles à nettoyer |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | Windows a un enregistrement pour {0} fichier qui n'est pas dans {InstallerFolder} : {1}. Cela ne pose aucun problème au quotidien, mais une mise à jour ou une désinstallation de ce programme peut échouer. Ouvrez Détails pour savoir quoi faire. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | Windows a des enregistrements pour {0} fichiers qui ne sont pas dans {InstallerFolder} : {1}. Cela ne pose aucun problème au quotidien, mais une mise à jour ou une désinstallation de ces programmes peut échouer. Ouvrez Détails pour savoir quoi faire. |
| {0} other program | {0} autre programme |
| {0} other programs | {0} autres programmes |
| {0} file with no program named in the records | {0} fichier sans programme nommé dans les enregistrements |
| {0} files with no program named in the records | {0} fichiers sans programme nommé dans les enregistrements |
| InstallerClean carried on without drive {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean a continué sans le lecteur {0} et a laissé de côté tout fichier qui restait à vérifier par rapport à celui-ci. Dès qu'il répond de nouveau normalement, cliquez sur Réanalyser. |
| InstallerClean carried on without {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean a continué sans {0} et a laissé de côté tout fichier qui restait à vérifier par rapport à ce qui s'y trouve. Dès que l'accès redevient normal, cliquez sur Réanalyser. |
| InstallerClean carried on without more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, Re-scan. | InstallerClean a continué sans plusieurs lecteurs ou partages ({0}) et a laissé de côté tout fichier qui restait à vérifier par rapport à l'un d'eux. Dès qu'ils répondent de nouveau normalement, cliquez sur Réanalyser. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has finished. | Quelque chose utilisait Windows Installer au début de l'analyse, par exemple une mise à jour de Windows ou un programme en cours d'installation, donc InstallerClean a retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Cliquez sur Réanalyser une fois que c'est terminé. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then Re-scan. | Une transaction Windows Installer était suspendue sur cette machine au début de l'analyse, donc InstallerClean a retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Reprenez ou annulez cette installation, ou redémarrez Windows, puis cliquez sur Réanalyser. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Au début de l'analyse, Windows avait mis en file d'attente pour le prochain redémarrage un renommage de fichier qui concerne {InstallerFolder}, donc InstallerClean y a retenu les fichiers qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Redémarrez Windows, puis cliquez sur Réanalyser. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | Au début de l'analyse, une opération sur fichiers était en file d'attente pour le prochain redémarrage et InstallerClean ne pouvait pas savoir quels fichiers elle nomme, il a donc retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Redémarrez Windows, puis cliquez sur Réanalyser. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Au début de l'analyse, Windows a refusé à InstallerClean l'autorisation de vérifier si Windows Installer était occupé, il a donc retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Redémarrez Windows et cliquez sur Réanalyser. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Au début de l'analyse, InstallerClean n'a pas pu lire l'un des paramètres Windows qu'il vérifie, il ne pouvait donc pas savoir si une opération d'installation était en cours ou attendait un redémarrage, et il a retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Redémarrez Windows et cliquez sur Réanalyser. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Au début de l'analyse, InstallerClean n'a pas pu vérifier si Windows Installer était occupé, il a donc retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Redémarrez Windows et cliquez sur Réanalyser. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | Au début de l'analyse, InstallerClean n'a pas pu vérifier si Windows Installer était occupé, car Windows a renvoyé une erreur : {0} (erreur {1}). Il a donc retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Redémarrez Windows et cliquez sur Réanalyser. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has cleared. | Windows Installer avait quelque chose en cours ou en attente au début de l'analyse, donc InstallerClean a retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Cliquez sur Réanalyser une fois que c'est réglé. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan. | L'horloge de ce PC a reculé ou pris du retard pendant l'analyse, donc InstallerClean a retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Cliquez sur Réanalyser. |
| {0} of {1} {2} | {0} sur {1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} à nettoyer ({2}) |
| {0} file left alone ({1}) | {0} fichier laissé de côté ({1}) |
| {0} files left alone ({1}) | {0} fichiers laissés de côté ({1}) |
| {0} missing | {0} manquant |
| {0} missing | {0} manquants |

## Confirmation dialogs

| English | Français |
| --- | --- |
| Move {0} {1} ({2})? | Déplacer {0} {1} ({2}) ? |
| This file will be moved to: | Ce fichier sera déplacé vers : |
| These files will be moved to: | Ces fichiers seront déplacés vers : |
| Delete {0} {1} ({2})? | Supprimer {0} {1} ({2}) ? |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Ce fichier sera supprimé définitivement. C'est sans risque, mais si vous voulez une sauvegarde, utilisez plutôt Déplacer. |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Ces fichiers seront supprimés définitivement. C'est sans risque, mais si vous voulez une sauvegarde, utilisez plutôt Déplacer. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Ce dossier est sur le même lecteur, l'espace ne reviendra donc pas tant que vous ne l'aurez pas supprimé. Choisissez plutôt un dossier sur un autre lecteur si vous voulez l'espace tout de suite. |

## Error messages

| English | Français |
| --- | --- |
| This is also recorded in {0}. | Cela est également consigné dans {0}. |
| Access denied | Accès refusé |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows a refusé l'accès à InstallerClean, qui s'est donc arrêté. Rien n'a été supprimé.<br><br>InstallerClean s'exécutait déjà en tant qu'administrateur, le relancer ainsi n'y changera rien. Windows n'en dit pas plus sur ce qui a refusé l'accès, il n'y a donc rien de précis à essayer. |
| InstallerClean stopped | InstallerClean s'est arrêté |
| Scan failed | Échec de l'analyse |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | Les enregistrements de Windows Installer sont revenus complètement vides : pas un seul programme installé ni une seule mise à jour ne revendique de fichier d'installation en cache. Cela n'arrive pas sur une machine qui fonctionne (même une installation neuve de Windows en a), donc soit les enregistrements sont endommagés, soit ils n'ont pas pu être lus, et une analyse qui croirait cette réponse qualifierait à tort d'orphelin chaque fichier de {InstallerFolder}. InstallerClean s'est arrêté à la place. Rien n'a été supprimé. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer a refusé de laisser InstallerClean lister ce qui est installé. InstallerClean s'exécutait déjà en tant qu'administrateur, le relancer en tant qu'administrateur n'y changera rien. Sans cette liste, il n'y a aucun moyen sûr de savoir quels fichiers en cache servent encore, donc InstallerClean s'est arrêté. Rien n'a été supprimé. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer n'a pas pu fournir à InstallerClean une liste lisible des programmes installés : il a lu {1} {2}, puis une entrée est revenue illisible (code d'erreur {0}). Plutôt que de travailler sur une liste lue en partie, InstallerClean s'est arrêté. Rien n'a été supprimé. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer n'a jamais signalé la fin de la liste des programmes installés : InstallerClean a lu {2} {3}, puis a renoncé après {0} entrées (dernier code d'erreur {1}). Une liste sans fin n'est pas fiable, donc InstallerClean s'est arrêté. Rien n'a été supprimé. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer n'a jamais signalé la fin de la liste des correctifs d'un programme : InstallerClean a lu {2} {3}, puis a renoncé après {0} entrées (dernier code d'erreur {1}). Une liste sans fin n'est pas fiable, donc InstallerClean s'est arrêté. Rien n'a été supprimé. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean n'a pas pu faire correspondre les enregistrements de Windows Installer avec le contenu de {InstallerFolder}. Presque rien de ce que désignent les enregistrements ne s'y trouve réellement, et presque rien de ce qui s'y trouve n'est nommé par un enregistrement, donc aucun fichier n'a pu être montré comme inutile. Rien n'a été proposé et rien n'a été retiré. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean n'a pas pu faire correspondre les enregistrements de Windows Installer avec le contenu de {InstallerFolder}. Le dossier contient des fichiers, mais pas un seul enregistrement ne désigne quoi que ce soit dedans, donc aucun fichier n'a pu être montré comme inutile. Rien n'a été proposé et rien n'a été retiré. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. Nothing has been offered and nothing has been removed. | InstallerClean ne peut pas analyser {InstallerFolder} sur ce PC. Windows indique que ce dossier se trouve à {0}, et InstallerClean n'analyse un dossier Installer que si Windows l'indique à son propre chemin. Rien n'a été proposé et rien n'a été retiré. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. Nothing has been offered and nothing has been removed. | Windows indique que {InstallerFolder} n'existe pas sur ce PC, donc InstallerClean s'est arrêté. Rien n'a été proposé et rien n'a été retiré. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. Nothing has been offered and nothing has been removed. | Windows a signalé une erreur quand InstallerClean a essayé de lister les fichiers de {InstallerFolder}, donc il n'a pas pu en obtenir la liste complète et s'est arrêté. Rien n'a été proposé et rien n'a été retiré. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean n'a pas pu lire assez des enregistrements de Windows Installer pour savoir avec certitude ce qui sert encore : la liste des programmes installés est revenue incomplète, et lire ces mêmes enregistrements directement dans le registre a également donné des erreurs. Un fichier pourrait sembler orphelin uniquement parce que l'enregistrement qui le nomme faisait partie des illisibles, donc InstallerClean s'est arrêté. Rien n'a été supprimé. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean n'a pas pu obtenir de Windows qu'il résolve le vrai chemin de {InstallerFolder}, donc aucun fichier n'a pu être montré comme s'y trouvant et aucun n'a été proposé au nettoyage. Cette analyse n'a rien trouvé parce que cette vérification a échoué, pas parce que le dossier est propre. Rien n'a été retiré. |
| Nothing was deleted | Rien n'a été supprimé |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows a refusé à InstallerClean l'autorisation de vérifier si Windows Installer était occupé, il n'a donc pas pu exclure qu'un fichier devienne nécessaire en cours de route, et rien n'a été supprimé. |
| Nothing was moved | Rien n'a été déplacé |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows a refusé à InstallerClean l'autorisation de vérifier si Windows Installer était occupé, il n'a donc pas pu exclure qu'un fichier devienne nécessaire en cours de route, et rien n'a été déplacé. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean n'a pas pu prendre le verrou que Windows Installer utilise pour empêcher deux programmes de modifier les logiciels installés en même temps, il n'a donc pas pu exclure qu'un fichier devienne nécessaire en cours de route, et rien n'a été supprimé. Réessayez, et redémarrez Windows si cela persiste. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean n'a pas pu prendre le verrou que Windows Installer utilise pour empêcher deux programmes de modifier les logiciels installés en même temps, il n'a donc pas pu exclure qu'un fichier devienne nécessaire en cours de route, et rien n'a été déplacé. Réessayez, et redémarrez Windows si cela persiste. |
| Invalid destination | Destination invalide |
| Move stopped | Déplacement arrêté |
| Couldn't use that backup folder | Impossible d'utiliser ce dossier de sauvegarde |
| Move failed | Échec du déplacement |
| Delete failed | Échec de la suppression |
| Setting not saved | Paramètre non enregistré |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | La modification n'a pas pu être enregistrée. Au prochain démarrage, InstallerClean reviendra au paramètre précédent. |
| The destination cannot be inside the Windows Installer folder. | La destination ne peut pas se trouver dans le dossier Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | La destination {0} se résout sous un dossier système de Windows. Choisissez un chemin en dehors de %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% et %ProgramData%. |
| Not enough space | Espace insuffisant |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | Il n'y a pas assez de place dans {0}<br><br>Nécessaire : {1}<br>Disponible : {2} |
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
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | Le dossier de sauvegarde doit être un chemin complet vers un dossier, commençant par une lettre de lecteur ou un partage réseau (par exemple D:\Backup, ou \\serveur\backup). InstallerClean ne peut pas utiliser celui-ci : {0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean n'a plus pu confirmer le dossier de sauvegarde, il s'est donc arrêté. Vérifiez {0}, puis Réanalyser et réessayez. |
| Cannot write to {0}. | Impossible d'écrire dans {0}. |
| You can save files in {0} but not open them again, so InstallerClean won't move files there. | Vous pouvez enregistrer des fichiers dans {0}, mais pas les rouvrir, donc InstallerClean n'y déplacera pas de fichiers. |
| A file called '{0}' is already in the backup folder. | Un fichier nommé « {0} » est déjà dans le dossier de sauvegarde. |

## Update check

| English | Français |
| --- | --- |
| Check for updates | Rechercher des mises à jour |
| Checking... | Vérification... |
| Up to date. | À jour. |
| InstallerClean {0} is available. | InstallerClean {0} est disponible. |
| It's strongly recommended to use the latest version. | Il est vivement recommandé d'utiliser la dernière version. |
| Download it from GitHub | Le télécharger sur GitHub |
| Download .NET Framework 4.8 from Microsoft | Télécharger .NET Framework 4.8 sur le site de Microsoft |
| Update now | Mettre à jour |
| This copy was installed by Scoop. To update it, close InstallerClean and run: | Cette copie a été installée par Scoop. Pour la mettre à jour, fermez InstallerClean et exécutez : |
| This copy was installed by Scoop for all users. To update it, close InstallerClean, open a terminal as administrator and run: | Cette copie a été installée par Scoop pour tous les utilisateurs. Pour la mettre à jour, fermez InstallerClean, ouvrez un terminal en tant qu'administrateur et exécutez : |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean to update. | Cette version nécessite .NET Framework 4.8. Installez-le depuis le site de Microsoft, redémarrez le PC, puis ouvrez InstallerClean pour effectuer la mise à jour. |
| InstallerClean {0} can't be installed automatically. | InstallerClean {0} ne peut pas être installé automatiquement. |
| InstallerClean can't tell whether its setup installed it, so it can't update itself. | InstallerClean ne peut pas savoir s'il a été installé par son programme d'installation, il ne peut donc pas se mettre à jour. |
| Copied | Copié |
| Couldn't copy it. Select the command and copy it yourself. | Impossible de la copier. Sélectionnez la commande et copiez-la vous-même. |
| _Continue without updating | _Continuer sans mettre à jour |
| Downloading InstallerClean {0}... | Téléchargement d'InstallerClean {0}... |
| Checking the download... | Vérification du téléchargement... |
| The update couldn't be downloaded. | La mise à jour n'a pas pu être téléchargée. |
| The update couldn't be installed. | La mise à jour n'a pas pu être installée. |
| Couldn't reach GitHub. Check your internet connection and try again. | Impossible de joindre GitHub. Vérifiez votre connexion internet et réessayez. |
| GitHub returned an error response. Try again in a few minutes. | GitHub a renvoyé une réponse d'erreur. Réessayez dans quelques minutes. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | La réponse de GitHub ne contenait pas de version reconnaissable. Réessayez plus tard, ou ouvrez directement la page des versions. |
| The check timed out. Your connection to GitHub may be slow; try again. | La vérification a expiré. Votre connexion à GitHub est peut-être lente ; réessayez. |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | La vérification a échoué pour une raison inconnue. Les détails sont dans {0} si vous devez le signaler. |
| The check failed for an unknown reason. The crash log could not be written. | La vérification a échoué pour une raison inconnue. Le crash.log n'a pas pu être écrit. |

## Opening links in your browser

| English | Français |
| --- | --- |
| Couldn't open your browser | Impossible d'ouvrir votre navigateur |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | Le lien est dans le presse-papiers, vous pouvez donc le coller vous-même :<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean n'a pas pu copier le lien dans le presse-papiers non plus, le voici :<br><br>{0} |

## Startup and crashes

| English | Français |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | Il est déjà en cours d'exécution. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Une erreur inattendue s'est produite et InstallerClean doit se fermer.<br><br>{0}<br><br>Détails écrits dans :<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Une erreur inattendue s'est produite et InstallerClean doit se fermer.<br><br>{0}<br><br>Le crash.log n'a pas pu être écrit. |
| Startup error | Erreur de démarrage |
| Failed to start ({0}). Details written to:<br>{1} | Échec du démarrage ({0}). Détails écrits dans :<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Échec du démarrage ({0}). Le crash.log n'a pas pu être écrit. |
| Administrator rights needed | Droits d'administrateur requis |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed.<br><br>Run InstallerClean again as administrator. | InstallerClean a besoin des droits d'administrateur pour voir tous les programmes installés sur ce PC, et il ne s'exécute pas en tant qu'administrateur, il ne lancera donc pas d'analyse. Rien n'a été modifié.<br><br>Relancez InstallerClean en tant qu'administrateur. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log recueille les exceptions non gérées d'InstallerClean.<br># Avec des privilèges élevés, les messages d'exception du framework<br># peuvent contenir des chemins de fichiers de la session en cours (y<br># compris des profils d'autres utilisateurs énumérés par les requêtes<br># Windows Installer). Les messages d'échec réseau de la vérification<br># des mises à jour ou de l'envoi du journal de résultats peuvent<br># contenir l'URL de destination et l'adresse IP ou proxy résolue. Les<br># entrées sur des enregistrements Windows Installer illisibles peuvent<br># contenir un SID de compte Windows (S-1-5-21-...) et les codes<br># produit des logiciels installés.<br># Supprimez ces trois types d'informations avant de joindre ce fichier<br># à un rapport de bogue public.<br> |

## Tooltips (hover text)

| English | Français |
| --- | --- |
| Any small donation is much appreciated | Tout don, même modeste, est très apprécié |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Annulation demandée. InstallerClean attend que l'étape en cours atteigne un point d'arrêt. Cela peut prendre quelques secondes lors d'opérations d'E/S intensives ou d'un appel à la base de données MSI. |
| Close | Fermer |
| I really should charge for InstallerClean - because it's been a huge amount of work - but I want to keep it free so it helps as many people as possible. Your donation makes it worthwhile and is very much appreciated. | Je devrais vraiment faire payer InstallerClean, car il m'a demandé énormément de travail, mais je veux qu'il reste gratuit pour aider le plus de monde possible. Votre don fait que cela en vaut la peine, et il est très apprécié. |
| A star helps other people find it | Une étoile aide les autres à découvrir InstallerClean |
| Minimise | Réduire |
| Move the unneeded files to the backup folder | Déplace les fichiers inutiles vers le dossier de sauvegarde |
| Move the unneeded files to a backup folder. You'll choose it next. | Déplace les fichiers inutiles vers un dossier de sauvegarde. Vous le choisirez juste après. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | Déplace les fichiers inutiles vers le dossier de sauvegarde. Il est sur le même disque, vous ne récupérerez donc l'espace qu'après avoir supprimé ce dossier. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | Supprime définitivement les fichiers inutiles. Utilisez plutôt Déplacer si vous voulez pouvoir vous convaincre que tout va bien. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nom du titulaire du certificat Authenticode incorporé. Chaîne non vérifiée. |
| Change language. The program will restart. | Changer la langue. Le programme redémarrera. |

## Screen reader labels

| English | Français |
| --- | --- |
| Donate | Faire un don |
| Donate $3 | Faire un don de 3 $ |
| Donate | Faire un don |
| Cancel operation | Annuler l'opération |
| Cancel scan | Annuler l'analyse |
| Cancel startup scan | Annuler l'analyse de démarrage |
| Carry on without it, drive {0} | Continuer sans attendre, lecteur {0} |
| Carry on without it, {0} | Continuer sans attendre, {0} |
| Close | Fermer |
| Close window | Fermer la fenêtre |
| Close result and return to main window | Fermer le résultat et revenir à la fenêtre principale |
| Leave a star on github | Laisser une étoile sur github |
| Minimise | Réduire |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Supprimer définitivement retire les fichiers inutiles. Annuler ferme sans rien supprimer. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | Déplacer place les fichiers inutiles dans le dossier de destination choisi. Annuler les laisse où ils sont. |
| Say thanks | Dire merci |
| Check for updates | Rechercher des mises à jour |
| Checks github's releases page for a newer version. | Vérifie sur la page des versions de github s'il existe une version plus récente. |
| Opens the readme on github in your browser. | Ouvre le readme sur github dans votre navigateur. |
| Opens the issue tracker on github.com in your browser. | Ouvre le suivi des problèmes (Issues) sur github.com dans votre navigateur. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Si la case est cochée, InstallerClean recherche une version plus récente sur github à son lancement. |
| Download it from github | Le télécharger sur github |
| Download .NET Framework 4.8 from Microsoft | Télécharger .NET Framework 4.8 sur le site de Microsoft |
| Copy the command | Copier la commande |
| Scoop's update command | Commande de mise à jour de Scoop |
| Opens the licence file on github.com in your browser. | Ouvre le fichier de licence sur github.com dans votre navigateur. |
| Backup folder | Dossier de sauvegarde |
| Patches | Correctifs |
| Product details | Détails du produit |
| Backup folder | Dossier de sauvegarde |
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
| Waiting for a drive or share | En attente d'un lecteur ou d'un partage |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | Fichiers qui n'ont pas pu être traités |
| Explains this folder, and how to recover a file, in the README | Explique ce dossier, et comment récupérer un fichier, dans le README |
| {0} Press Alt+{1} to carry on without it. {2} | {0} Appuyez sur Alt+{1} pour continuer sans attendre. {2} |
| What the report holds | Ce que contient le rapport |
| What the report holds | Ce que contient le rapport |
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
| drive {0} | lecteur {0} |
| .  | .  |
| {0} TB | {0} To |
| {0} GB | {0} Go |
| {0} MB | {0} Mo |
| {0} KB | {0} Ko |
| {0} B | {0} o |
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
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean n'a pas pu déterminer avec certitude quels fichiers en cache appartiennent aux programmes installés ici, il a donc retenu le seul fichier ({2}) au lieu de le proposer. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean n'a pas pu déterminer avec certitude quels fichiers en cache appartiennent aux programmes installés ici, il a donc retenu {0} {1} ({2}) au lieu de les proposer. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean n'a pas pu établir qu'un fichier est inutile, il l'a donc retenu ({2}) au lieu de le proposer. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean n'a pas pu établir que {0} {1} sont inutiles, il les a donc retenus ({2}) au lieu de les proposer. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean n'a pas pu déterminer avec certitude quels fichiers en cache appartiennent aux programmes installés ici, il a donc retenu le seul fichier ({2}) au lieu de le proposer. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean n'a pas pu déterminer avec certitude quels fichiers en cache appartiennent aux programmes installés ici, il a donc retenu {0} {1} ({2}) au lieu de les proposer. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean n'a pas pu établir qu'un fichier est inutile, il l'a donc retenu ({2}) au lieu de le proposer. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean n'a pas pu établir que {0} {1} sont inutiles, il les a donc retenus ({2}) au lieu de les proposer. |
| Why it couldn't be certain: | Pourquoi la certitude n'était pas possible : |
|   A file path in Windows Installer's own records wouldn't resolve, so nothing could be matched to it. |   Un chemin de fichier issu des propres enregistrements de Windows Installer n'a pas pu être résolu, rien n'a donc pu lui être rapproché. |
|   A file Windows has a record of couldn't be identified, so it couldn't be matched to what's in the folder. |   Un fichier dont Windows garde un enregistrement n'a pas pu être identifié, il n'a donc pas pu être rapproché du contenu du dossier. |
|   A program may be installed more than once on this PC, and the records can't say which copy a file belongs to. |   Un programme est peut-être installé plusieurs fois sur ce PC, et les enregistrements ne peuvent pas dire à quelle copie appartient un fichier. |
|   Part of Windows Installer's records wouldn't read, so a file could belong to a program recorded there. |   Une partie des enregistrements de Windows Installer n'a pas pu être lue, un fichier pourrait donc appartenir à un programme qui y est enregistré. |
|   A file in the folder couldn't be identified, so it couldn't be matched against the records. |   Un fichier du dossier n'a pas pu être identifié, il n'a donc pas pu être rapproché des enregistrements. |
|   Either a file wouldn't say which program it belongs to, or Windows wouldn't give a clear answer about that program. |   Soit un fichier n'a pas indiqué à quel programme il appartient, soit Windows n'a pas donné de réponse claire au sujet de ce programme. |
|   A check on which programs the files belong to gave answers that didn't line up with the files it was handed. |   Une vérification des programmes auxquels appartiennent les fichiers a donné des réponses qui ne correspondaient pas aux fichiers qui lui avaient été transmis. |
|   Either a patch file wouldn't say which patch it is, or it wouldn't say which programs it's for, or Windows wouldn't say in full which programs have that patch. |   Soit un fichier de correctif n'a pas indiqué de quel correctif il s'agit, soit il n'a pas indiqué à quels programmes il est destiné, soit Windows n'a pas indiqué de façon complète quels programmes ont ce correctif. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows a un enregistrement pour {0} fichier qui n'est pas dans {InstallerFolder} : {1}. Cela ne pose aucun problème au quotidien, mais une mise à jour ou une désinstallation de ce programme peut échouer. Pour remettre le fichier, il vous faut l'installateur de la version que vous avez déjà. Procurez-vous-le auprès de l'éditeur du programme et exécutez-le par-dessus votre copie actuelle. Une version plus récente ne fera pas l'affaire : elle devrait d'abord retirer celle que vous avez, et c'est justement l'étape qui a besoin de ce fichier. Désinstaller d'abord ne marchera pas non plus, pour la même raison. Cela devrait restaurer le fichier et laisser vos réglages intacts, mais Microsoft ne le garantit pas. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows a des enregistrements pour {0} fichiers qui ne sont pas dans {InstallerFolder} : {1}. Cela ne pose aucun problème au quotidien, mais une mise à jour ou une désinstallation de ces programmes peut échouer. Pour remettre un fichier, il vous faut l'installateur de la version que vous avez déjà de ce programme. Procurez-vous-le auprès de l'éditeur du programme et exécutez-le par-dessus votre copie actuelle. Une version plus récente ne fera pas l'affaire : elle devrait d'abord retirer celle que vous avez, et c'est justement l'étape qui a besoin du fichier. Désinstaller d'abord ne marchera pas non plus, pour la même raison. Cela devrait restaurer le fichier et laisser vos réglages intacts, mais Microsoft ne le garantit pas. |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | InstallerClean n'a pas pu déterminer avec certitude que le seul fichier remplacé n'est plus nécessaire, il l'a donc retenu. |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | InstallerClean n'a pas pu déterminer avec certitude que {0} fichiers remplacés ne sont plus nécessaires, il les a donc retenus. |
| InstallerClean stopped waiting for drive {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean a cessé d'attendre le lecteur {0} et a laissé de côté tout fichier qui restait à vérifier par rapport à celui-ci. Dès qu'il répond de nouveau normalement, relancez la commande. |
| InstallerClean stopped waiting for {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean a cessé d'attendre {0} et a laissé de côté tout fichier qui restait à vérifier par rapport à ce qui s'y trouve. Dès que l'accès redevient normal, relancez la commande. |
| InstallerClean stopped waiting for more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, run the command again. | InstallerClean a cessé d'attendre plusieurs lecteurs ou partages ({0}) et a laissé de côté tout fichier qui restait à vérifier par rapport à l'un d'eux. Dès qu'ils répondent de nouveau normalement, relancez la commande. |
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
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | Erreur : une opération sur fichiers est en file d'attente pour le prochain redémarrage et InstallerClean ne peut pas savoir quels fichiers elle nomme, il ne peut donc pas exclure {InstallerFolder}. Redémarrez Windows avant de nettoyer. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Erreur : InstallerClean n'a pas pu lire l'une des valeurs de registre qu'il vérifie avant de toucher à {InstallerFolder}, il ne peut donc pas exclure une opération Windows Installer en cours ou mise en file d'attente pour le prochain redémarrage. /m et /d sont bloqués. Redémarrez Windows et réessayez. Si la lecture échoue toujours, ce n'est pas une machine qu'InstallerClean peut nettoyer. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Erreur : InstallerClean n'a pas pu vérifier si Windows Installer est occupé, il ne peut donc pas exclure une installation en cours. /m et /d sont bloqués. Redémarrez Windows et réessayez. Si la vérification échoue toujours, ce n'est pas une machine qu'InstallerClean peut nettoyer. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Erreur : InstallerClean n'a pas pu vérifier si Windows Installer est occupé, car Windows a renvoyé une erreur : {0} (erreur {1}), il ne peut donc pas exclure une installation en cours. /m et /d sont bloqués. Redémarrez Windows et réessayez. Si la vérification échoue toujours, ce n'est pas une machine qu'InstallerClean peut nettoyer. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Erreur : Windows Installer a quelque chose en cours, donc /m et /d sont bloqués. InstallerClean ne touchera pas à {InstallerFolder} pendant qu'il change. Réessayez une fois terminé. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has finished. | Quelque chose utilisait Windows Installer au début de l'analyse, par exemple une mise à jour de Windows ou un programme en cours d'installation, donc InstallerClean a retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Relancez la commande une fois que c'est terminé. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then run it again. | Une transaction Windows Installer était suspendue sur cette machine au début de l'analyse, donc InstallerClean a retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Reprenez ou annulez cette installation, ou redémarrez Windows, puis relancez la commande. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Au début de l'analyse, Windows avait mis en file d'attente pour le prochain redémarrage un renommage de fichier qui concerne {InstallerFolder}, donc InstallerClean y a retenu les fichiers qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Redémarrez Windows, puis relancez la commande. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then run it again. | Au début de l'analyse, une opération sur fichiers était en file d'attente pour le prochain redémarrage et InstallerClean ne pouvait pas savoir quels fichiers elle nomme, il a donc retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Redémarrez Windows, puis relancez la commande. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Au début de l'analyse, Windows a refusé à InstallerClean l'autorisation de vérifier si Windows Installer était occupé, il a donc retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Redémarrez Windows et relancez la commande. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Au début de l'analyse, InstallerClean n'a pas pu lire l'un des paramètres Windows qu'il vérifie, il ne pouvait donc pas savoir si une opération d'installation était en cours ou attendait un redémarrage, et il a retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Redémarrez Windows et relancez la commande. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Au début de l'analyse, InstallerClean n'a pas pu vérifier si Windows Installer était occupé, il a donc retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Redémarrez Windows et relancez la commande. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | Au début de l'analyse, InstallerClean n'a pas pu vérifier si Windows Installer était occupé, car Windows a renvoyé une erreur : {0} (erreur {1}). Il a donc retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Redémarrez Windows et relancez la commande. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has cleared. | Windows Installer avait quelque chose en cours ou en attente au début de l'analyse, donc InstallerClean a retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Relancez la commande une fois que c'est réglé. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again. | L'horloge de ce PC a reculé ou pris du retard pendant l'analyse, donc InstallerClean a retenu les fichiers de {InstallerFolder} qu'aucun enregistrement de programme installé ne nomme, au lieu de les proposer. Relancez la commande. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Erreur : InstallerClean n'a pas pu prendre le verrou Windows Installer qui empêche deux programmes de modifier les logiciels installés en même temps, il n'a donc pas pu exclure qu'un fichier devienne nécessaire en cours de route. Rien n'a été supprimé. Réessayez, et redémarrez Windows si cela persiste. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Erreur : InstallerClean n'a pas pu prendre le verrou Windows Installer qui empêche deux programmes de modifier les logiciels installés en même temps, il n'a donc pas pu exclure qu'un fichier devienne nécessaire en cours de route. Rien n'a été déplacé. Réessayez, et redémarrez Windows si cela persiste. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Erreur : Windows a refusé à InstallerClean l'autorisation de vérifier si Windows Installer était occupé, il n'a donc pas pu exclure qu'un fichier devienne nécessaire en cours de route. Rien n'a été supprimé. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Erreur : Windows a refusé à InstallerClean l'autorisation de vérifier si Windows Installer était occupé, il n'a donc pas pu exclure qu'un fichier devienne nécessaire en cours de route. Rien n'a été déplacé. |
| Moving {0} unneeded {1} to {2}... | Déplacement de {0} {1} inutiles vers {2}... |
| Moved {0} unneeded {1}. | {0} {1} inutiles ont été déplacés. |
| Check that your programs still update and uninstall as normal, then delete {0}. | Vérifiez que vos programmes se mettent toujours à jour et se désinstallent normalement, puis supprimez {0}. |
| InstallerClean is free. If it helped, please donate $3: {0} | InstallerClean est gratuit. S'il vous a été utile, merci de faire un don de 3 $ : {0} |
| Update now? It's strongly recommended to use the latest version. [{0}/{1}] | Mettre à jour maintenant ? Il est vivement recommandé d'utiliser la dernière version. [{0}/{1}] |
| Y | O |
| N | N |
| No answer after a minute, so InstallerClean updates without asking, as it does when nobody is there to answer. | Pas de réponse après une minute, donc InstallerClean se met à jour sans demander, comme lorsque personne n'est là pour répondre. |
| Send an anonymous report of this run? It is saved at {0}. Type ? to see what it holds. [{1}/{2}/?] | Envoyer un rapport anonyme de cette exécution ? Il est enregistré dans {0}. Tapez ? pour voir ce qu'il contient. [{1}/{2}/?] |
| the chart at {0} | ce graphique à l'adresse {0} |
| The report adds your result to {0}, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Le rapport ajoute votre résultat à {0}, qui montre combien d'espace les gens libèrent avec InstallerClean. Il contient aussi ces détails techniques, qui m'aident à repérer d'éventuels bugs : |
| The full report is printed below. | Le rapport complet est affiché ci-dessous. |
| Report sent. Thank you. | Rapport envoyé. Merci. |
| Report not sent. | Rapport non envoyé. |
| No answer after a minute. | Pas de réponse après une minute. |
| The report could not be sent. It is kept in {0}. | Le rapport n'a pas pu être envoyé. Il est conservé dans {0}. |
| Sending a report left waiting by an earlier run. | Envoi d'un rapport en attente depuis une exécution précédente. |
| The new version will scan and list what it finds, without deleting anything. To delete, run /d again afterwards. | La nouvelle version va analyser et lister ce qu'elle trouve, sans rien supprimer. Pour supprimer, relancez /d ensuite. |
| The new version will scan and list what it finds, without moving anything. To move, run /m again afterwards. | La nouvelle version va analyser et lister ce qu'elle trouve, sans rien déplacer. Pour déplacer, relancez /m ensuite. |
| InstallerClean is in use, so it can't update itself now. | InstallerClean est en cours d'utilisation, il ne peut donc pas se mettre à jour maintenant. |
| Running {0} with InstallerClean {1} instead. | Exécution de {0} avec InstallerClean {1} à la place. |
| The newer version couldn't be run. | La version plus récente n'a pas pu être exécutée. |
| Running {0} with InstallerClean {1}. It takes this copy's place once the run ends. | Exécution de {0} avec InstallerClean {1}. Cette version prendra la place de cette copie une fois l'exécution terminée. |
| {0}: {1} | {0} : {1} |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again to update. | Cette version nécessite .NET Framework 4.8. Installez-le depuis le site de Microsoft, redémarrez le PC, puis exécutez à nouveau InstallerClean pour effectuer la mise à jour. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | C'est simple à annuler. Redéplacez-les depuis {0} dans {InstallerFolder} et tout redeviendra comme avant. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean n'a plus pu confirmer le dossier de sauvegarde, il s'est donc arrêté. Vérifiez {0}, puis relancez la commande. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Un autre processus InstallerClean détient le verrou d'instance unique (l'interface ou une autre exécution de la CLI). Code de sortie 75 (transitoire) ; vous pouvez réessayer plus tard sans risque. |
| Stopped after deleting {0} of {1} {2}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Arrêté après la suppression de {0} sur {1} {2} : InstallerClean ne pouvait plus être certain qu'aucun autre InstallerClean n'était en train d'agir. Relancez la même commande. Code de sortie 2 (partiel). |
| Stopped after moving {0} of {1} {2} to {3}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Arrêté après le déplacement de {0} sur {1} {2} vers {3} : InstallerClean ne pouvait plus être certain qu'aucun autre InstallerClean n'était en train d'agir. Relancez la même commande. Code de sortie 2 (partiel). |
| Stopped before deleting any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Arrêté avant d'avoir supprimé le moindre fichier : InstallerClean ne pouvait plus être certain qu'aucun autre InstallerClean n'était en train d'agir. Relancez la même commande. Code de sortie 75 (transitoire). |
| Stopped before moving any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Arrêté avant d'avoir déplacé le moindre fichier : InstallerClean ne pouvait plus être certain qu'aucun autre InstallerClean n'était en train d'agir. Relancez la même commande. Code de sortie 75 (transitoire). |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed. Run it again from an administrator prompt. | InstallerClean a besoin des droits d'administrateur pour voir tous les programmes installés sur ce PC, et il ne s'exécute pas en tant qu'administrateur, il ne lancera donc pas d'analyse. Rien n'a été modifié. Relancez-le depuis une invite de commandes administrateur. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Remarque : l'écriture dans le journal des événements a échoué. Vérifiez les autorisations du journal Application ou la stratégie de groupe. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - nettoyage de {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Retire les .msi et .msp en cache dont aucun programme installé n'a besoin. |
| Needs an elevated (administrator) prompt and won't scan without one. | Exige une invite de commandes administrateur ; sinon, aucune analyse. |
| Usage: | Utilisation : |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help     Affiche cette aide (accepte aussi /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version  Affiche la version (accepte aussi -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Analyse seule - liste les inutiles |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Supprime définitivement les inutiles |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Déplace vers le dossier enregistré |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m CHEMIN  Déplace vers le chemin spécifié |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli bloque l'invite jusqu'à la fin, pour qu'un script ou<br>une tâche planifiée puisse l'attendre. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Dossier propre à l'utilisateur ; tâches planifiées ou SYSTEM : /m CHEMIN. |
| Before it scans, it checks for a newer version of InstallerClean.<br>When you run it yourself, it asks before updating, and updates if nobody<br>answers within a minute. When it runs on its own, in a scheduled task or<br>a script, it updates without asking.<br>Add /noupdate to skip the check. | Avant l'analyse, il vérifie s'il existe une version plus récente<br>d'InstallerClean. Lancé par vous, il demande avant de mettre à jour, et<br>se met à jour si personne ne répond dans la minute. Lancé seul, dans une<br>tâche planifiée ou un script, il se met à jour sans demander. Ajoutez<br>/noupdate pour ignorer la vérification. |
| Exit codes: | Codes de sortie : |
|   0   success: the run did what it was asked |   0   succès : l'exécution a fait ce qui lui était demandé |
|   1   nothing processed: the run failed or was refused |   1   rien de traité : l'exécution a échoué ou a été refusée |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partiel : une partie traitée, l'autre non (un échec ou un Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitoire : quelque chose a bloqué l'exécution (voir le message) |
|   130 cancelled (Ctrl+C) |   130 annulé (Ctrl+C) |
