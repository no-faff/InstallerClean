#!/usr/bin/env node
// French (fr) satellite generator for InstallerClean. Copied from
// gen-strings-template.mjs; see that file's header for how it works.
//
// French specifics baked into the values below:
//   - Register: formal "vous" throughout (matches the French README and the
//     settled French desktop-software convention; Windows itself uses vous).
//   - Typography: a narrow no-break space (U+202F, written   so it survives
//     the Edit tool's flattening) before : ; ? ! and as the thousands separator;
//     guillemets « ... » with an inner U+202F for inline quotes,
//     matching the README's "« ... »" style. Straight ' apostrophes
//     (the README uses zero curly apostrophes).
//   - Terms: À propos (About), correctif(s) (patch),
//     fichiers inutiles / encore nécessaires (unneeded / still needed), café
//     (tip jar), journal des événements / journal Application / stratégie de
//     groupe (Cli.EventLogUnavailable). All anchored to the README, the
//     native-reviewed Italian and Windows-FR.
//   - Plurals: fr = 0 and 1 singular (already in DisplayHelpers.CategoryFor). Two
//     satellite-only .One overrides live in the OVERRIDES block below, each because
//     a post-nominal adjective/participle has to agree with the count:
//     Status.RegisteredPackagesFound.One (the adjective "enregistré") and
//     Completion.ReverifySkipped.One ("laissé ... redevenu nécessaire"). The block is
//     injected before </root> so a re-run reproduces the file exactly. The CLI count
//     lines and the cancelled-summary lines use an invariable leading participle
//     (Trouvé/Supprimé/Déplacé precede their object, so they do not inflect) and
//     need no .One.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.fr.resx`;

// Universal keeps (brand names, pure-placeholder, size/elapsed formats). Do NOT
// edit per language.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
  'Display.Size.GB',                   // {0:F2} GB
  'Display.Size.MB',                   // {0:F1} MB
  'Display.Size.KB',                   // {0:F1} KB
  'Display.Size.B',                    // {0} B
  'Display.Elapsed.Ms',                // {0:F0}ms
  'Display.Elapsed.S',                 // {0:F1}s
]);

// Per-language keeps: French words byte-identical to English. Both are genuine
// single-token translations, not misses: "Application" and "Version" are the same
// word in French. The self-check prints these so the keep stays honest.
const ALSO_KEEP = ['Field.Application', 'Version.Display'];

// Satellite-only CLDR plural overrides: keys absent from the neutral, appended
// before </root> and read by name at runtime (DisplayHelpers.Pluralise's
// One/Few/Many branches; an absent one falls back to the base). French counts 0
// and 1 as "one", and the registered-count line's adjective "enregistré" must
// agree with the count, so its singular drops the plural -s. The CLI count lines
// (Cli.FoundOrphans/DeletedFiles/MovedFiles) need NO override: their participle
// (Trouvé/Supprimé/Déplacé) precedes its object and so stays invariable.
const OVERRIDES = {
  'Status.RegisteredPackagesFound.One': `Trouvé {0} {1} enregistré.`,
  'Completion.ReverifySkipped.One': `{0} {1} laissé en place, redevenu nécessaire à un programme après l'analyse.`,
  // Participle agreement only: "laissé" for a single file. The reason clause
  // is about the records, not the files, so it does not inflect.
  'Completion.ReverifyIncomplete.One': `{0} {1} kept in place, because the Windows Installer records could not be fully read in the final check.`,
};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `À propos`,
  'Window.Registered.Title': `Fichiers enregistrés qui ne devraient pas être supprimés`,
  'Window.Orphaned.Title': `Fichiers inutiles que vous pouvez supprimer sans risque`,

  // Section headings
  'Section.Registered.Products': `PRODUITS`,
  'Section.Registered.Patches': `CORRECTIFS`,
  'Section.Registered.Details': `DÉTAILS DU PRODUIT`,
  'Section.Backup.Folder': `BACKUP FOLDER`,
  'Section.SayThanks': `DIRE MERCI`,

  // Field labels (used in detail panels)
  'Field.Reason': `Motif`,
  'Field.Author': `Auteur`,
  'Field.Application': `Application`,
  'Field.Title': `Titre`,
  'Field.Subject': `Objet`,
  'Field.Keywords': `Mots-clés`,
  'Field.SigningCertificate': `Certificat de signature`,
  'Field.FileSize': `Taille du fichier`,
  'Field.Comment': `Commentaire`,
  'Field.ProductName': `Nom du produit`,
  'Field.File': `Fichier`,
  'Field.Size': `Taille`,
  'Field.Patches': `Correctifs`,

  'Field.UnknownProductName': `(inconnu)`,
  'Field.PatchesOnly': `(correctifs uniquement)`,
  'Field.Missing': `manquant`,

  // Actions (button labels; underscore prefixes are WPF mnemonics)
  'Action.About': `À pr_opos`,
  'Action.Copy': `Copier`,
  'Action.Cut': `Couper`,
  'Action.Paste': `Coller`,
  'Action.SelectAll': `Sélectionner tout`,
  'Action.Browse': `_Parcourir...`,
  'Action.Cancel': `_Annuler`,
  'Action.CheckForUpdates': `Rechercher des _mises à jour`,
  'Action.Close': `_Fermer`,
  'Action.DeletePermanently': `_Supprimer définitivement`,
  'Action.Done': `_Terminé`,
  'Action.Details': `Détails`,
  'Action.BuyMeACuppa': `Offrez-moi un _café`,
  'Action.LeaveStarOnGitHub': `_Laisser une étoile sur GitHub`,
  'Action.Licence': `Licence Apache 2.0`,
  'Action.Move': `_Déplacer`,
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
  'Action.OpenReleasePage': `Ouvrir la page de la _version`,
  'Action.Rescan': `_Réanalyser`,
  'Action.ScanAgain': `Analyser à _nouveau`,
  'Action.SendResultLog': `Envoyer le rapport`,
  'Action.SendResultLogConfirm': `_Envoyer`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa': `Faire un don`,
  'Automation.BuyMeACuppa.About': `Offrez-moi un café`,
  'Automation.CancelOperation': `Annuler l'opération`,
  'Automation.CancelScan': `Annuler l'analyse`,
  'Automation.CancelStartupScan': `Annuler l'analyse de démarrage`,
  'Automation.Close': `Fermer`,
  'Automation.CloseWindow': `Fermer la fenêtre`,
  'Automation.CloseResult': `Fermer le résultat et revenir à la fenêtre principale`,
  'Automation.LeaveStarOnGitHub.About': `Laisser une étoile sur github`,
  'Automation.Minimise': `Réduire`,
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
  'Automation.ConfirmMove': `Déplacer place les fichiers inutiles dans le dossier de destination choisi. Annuler les laisse où ils sont.`,
  'Automation.SayThanks': `Dire merci`,
  'Automation.ConfirmSendResultLog': `Envoyer transmet à No Faff le rapport affiché. Annuler n'envoie rien.`,
  'Automation.CheckForUpdates': `Rechercher des mises à jour`,
  'Automation.CheckForUpdates.HelpText': `Vérifie sur la page des versions de github s'il existe une version plus récente.`,
  'Automation.UpdateAvailable.HelpText': `Ouvrez la page de la version pour télécharger la version plus récente, ou annulez pour conserver la version actuelle.`,
  'Automation.Licence.HelpText': `Ouvre le fichier de licence sur github.com dans votre navigateur.`,
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `Produits`,
  'Automation.Section.Patches': `Correctifs`,
  'Automation.Section.ProductDetails': `Détails du produit`,
  'Automation.BackupFolder': `Backup folder`,
  'Automation.OperationProgress': `Progression de l'opération`,
  'Automation.RescanInstaller': `Analyser à nouveau {InstallerFolder}`,
  'Automation.ScanningProgress': `Progression de l'analyse`,
  'Automation.StartupScanProgress': `Progression de l'analyse de démarrage`,
  'Automation.ViewOrphanedFiles': `Détails, fichiers inutiles`,
  'Automation.ViewOrphanedFiles.HelpText': `Disponibles pour le nettoyage.`,
  'Automation.ViewRegisteredFiles': `Détails, fichiers enregistrés`,
  'Automation.ViewRegisteredFiles.HelpText': `Inventaire en lecture seule.`,
  'Automation.SortStatus.Ascending': `Trié par {0}, ordre croissant`,
  'Automation.SortStatus.Descending': `Trié par {0}, ordre décroissant`,
  'Automation.Scroll.ScanResults': `Résultats de l'analyse`,
  'Automation.Scroll.ResultDetails': `Détails du résultat`,
  'Automation.Scroll.FileDetails': `Détails du fichier`,
  'Automation.Scroll.DialogBody': `Texte de la boîte de dialogue`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `Fichiers qui n'ont pas pu être traités`,
  'Automation.RegisteredMissingSeeAlso': `Explique ce dossier, et comment récupérer un fichier, dans le README`,

  // Tooltips
  'Tooltip.BuyMeACuppa.About': `Ça donne soif !`,
  'Tooltip.CancellingPending': `Annulation demandée. InstallerClean attend que l'étape en cours atteigne un point d'arrêt. Cela peut prendre quelques secondes lors d'opérations d'E/S intensives ou d'un appel à la base de données MSI.`,
  'Tooltip.Close': `Fermer`,
  'Tooltip.LeaveStarOnGitHub.About': `Une étoile aide les autres à découvrir InstallerClean.`,
  'Tooltip.Minimise': `Réduire`,
  'Tooltip.SendResultLog': `Comme vous voulez, mais c'est apprécié. Envoie un résumé anonyme qui me dit juste si l'outil fonctionne et combien d'espace les gens libèrent. L'écran suivant vous montre ce qui sera envoyé avant que vous confirmiez.`,
  'Tooltip.SendResultLog.NothingFound': `Comme vous voulez, mais c'est apprécié. Envoie un résumé anonyme qui me dit juste si l'outil fonctionne. L'écran suivant vous montre ce qui sera envoyé avant que vous confirmiez.`,
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Nom du titulaire du certificat Authenticode incorporé. Chaîne non vérifiée.`,

  // Body copy
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `Ils se trouvent dans {InstallerFolder}, laissés là quand un programme a été désinstallé ({0}), qu'un correctif plus récent en a remplacé un ({1}) ou que l'éditeur l'a retiré ({2}). InstallerClean ne liste jamais que les fichiers dont Windows lui-même déclare avoir fini de se servir.`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.NoFileSelected': `Sélectionnez un fichier pour voir les détails.`,
  'Body.NoProductSelected': `Sélectionnez un produit pour voir les détails.`,
  'Body.NoMetadata': `Aucune métadonnée disponible.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.&#10;&#10;It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.&#10;&#10;To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `Le README [explique ce dossier], et comment récupérer un fichier, avec les propres mots de Microsoft.`,
  'Body.NoPatches': `(aucun)`,

  // Reasons (Reason column in the orphaned-files list)
  'Reason.Orphaned': `Orphelin`,
  'Reason.Superseded': `Remplacé`,
  'Reason.Obsoleted': `Obsolète`,

  // Status / progress text
  'Status.Scanning': `Analyse...`,
  'Status.Cancelling': `Annulation...`,
  'Status.StartingScan': `Démarrage de l'analyse...`,
  'Status.QueryingApi': `Interrogation de Windows sur les logiciels installés...`,
  'Status.ScanningCache': `Analyse du dossier de cache d'installation...`,
  'Status.EnumeratingProducts': `Énumération des produits installés...`,
  'Status.CheckingRegistry': `Vérification du registre pour des paquets supplémentaires...`,

  // 0 = registered package count, 1 = pluralised "package"/"packages"
  'Status.RegisteredPackagesFound': `Trouvé {0} {1} enregistrés.`,

  // 0 = elapsed time text (e.g. "1.2s")
  'Status.ScanComplete': `Analyse terminée ({0})`,
  'Status.FoundProducts': `Analyse des paquets locaux...`,

  // 0 = file count, 1 = pluralised noun ("file"/"files")
  'Status.FoundUnused': `{0} {1} à supprimer sans risque.`,
  'Status.PreparingDestination': `Préparation du dossier de destination...`,

  // 0 = file count, 1 = pluralised noun
  'Status.Moving': `Moving unneeded files...`,
  'Status.Deleting': `Deleting unneeded files...`,
  'Status.MoveCancelled.Partial': `Déplacement annulé après avoir traité {0} sur {1} {2}.`,
  'Status.DeleteCancelled.Partial': `Suppression annulée après avoir traité {0} sur {1} {2}.`,
  'Status.MoveFailed': `Échec du déplacement ({0}). Détails dans {1}.`,
  'Status.MoveFailed.NoLog': `Échec du déplacement ({0}). Le crash.log n'a pas pu être écrit.`,
  'Status.DeleteFailed': `Échec de la suppression ({0}). Détails dans {1}.`,
  'Status.DeleteFailed.NoLog': `Échec de la suppression ({0}). Le crash.log n'a pas pu être écrit.`,
  'Status.ScanAccessDenied': `Accès refusé. Windows a refusé l'analyse.`,
  'Status.ScanFailedDb': `Échec de l'analyse : impossible de lire les enregistrements de Windows Installer.`,
  'Status.ScanCancelled': `Analyse annulée.`,
  'Status.Done': `Prêt`,
  'Status.ScanFailedDetails': `Échec de l'analyse ({0}). Détails dans {1}.`,
  'Status.ScanFailedDetails.NoLog': `Échec de l'analyse ({0}). Le crash.log n'a pas pu être écrit.`,

  // Completion screen
  'Completion.AllClean': `Tout est propre`,
  'Completion.NothingToCleanUp': `Rien à nettoyer dans {InstallerFolder}`,
  'Completion.NothingToCleanUpReceipt': `Analyse de {0} {1} en {2}`,

  // 0 = size freed (e.g. "120.5 MB")
  'Completion.Freed': `{0} libérés`,
  'Completion.Moved': `{0} déplacés`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `Rien n'a été déplacé`,
  'Completion.NothingDeleted': `Rien n'a été supprimé`,
  'Completion.FailedCount.Singular': `{0} fichier sur {1} n'a pas pu être déplacé.`,
  'Completion.FailedCount.Plural': `{0} fichiers sur {1} n'ont pas pu être déplacés.`,
  'Completion.FailedCountDelete.Singular': `{0} fichier sur {1} n'a pas pu être supprimé.`,
  'Completion.FailedCountDelete.Plural': `{0} fichiers sur {1} n'ont pas pu être supprimés.`,

  // 0 = moved count, 1 = pluralised noun, 2 = destination path
  'Completion.MoveSummary.Singular': `Déplacé {0} {1} vers : {2}`,
  'Completion.MoveSummary.Plural': `Déplacé {0} {1} vers : {2}`,

  // 0 = deleted count, 1 = pluralised noun

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} fichier encore nécessaire`,
  'Summary.RegisteredStillUsed.Plural': `{0} fichiers encore nécessaires`,
  'Summary.OrphanedToCleanUp.Singular': `{0} fichier inutile à nettoyer`,
  'Summary.OrphanedToCleanUp.Plural': `{0} fichiers inutiles à nettoyer`,
  'Summary.MissingFromDisk.Singular': `{0} fichier enregistré est manquant (non supprimé par InstallerClean). Aucun problème pour l'instant, mais une future réparation, mise à jour ou désinstallation de ce programme pourrait échouer. Ouvrez les Détails pour savoir quoi faire.`,
  'Summary.MissingFromDisk.Plural': `{0} fichiers enregistrés sont manquants (non supprimés par InstallerClean). Aucun problème pour l'instant, mais une future réparation, mise à jour ou désinstallation de ces programmes pourrait échouer. Ouvrez les Détails pour savoir quoi faire.`,

  // 0 = current file count, 1 = total count, 2 = pluralised noun.
  'Summary.OperationFiles': `{0} sur {1} {2}`,

  // 0 = orphaned count, 1 = superseded count, 2 = obsoleted count, 3 = size display.
  'Summary.OrphanedWindow': `{0} orphelins, {1} remplacés, {2} obsolètes ({3})`,

  // 0 = count, 1 = size display.
  'Summary.RegisteredWindow.Singular': `{0} fichier enregistré encore nécessaire ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} fichiers enregistrés encore nécessaires ({1})`,

  // Confirmation dialogs

  // 0 = file count, 1 = pluralised "file"/"files", 2 = size display
  'Confirm.MoveTitle': `Déplacer {0} {1} ({2}) ?`,

  // 0 = destination path
  'Confirm.MoveDestination': `Les fichiers seront déplacés vers :`,
  'Confirm.DeleteTitle': `Supprimer {0} {1} ({2}) ?`,

  // Error messages
  'Error.AdminRequiredTitle': `Accès refusé`,
  'Error.AdminRequiredBody': `Windows a refusé l'accès à InstallerClean, qui s'est donc arrêté. Rien n'a été supprimé.\n\nInstallerClean s'exécutait déjà en tant qu'administrateur, le relancer ainsi n'y changera rien. Windows n'en dit pas plus sur ce qui a refusé l'accès, il n'y a donc rien de précis à essayer.`,
  'Error.InstallerDbUnavailableTitle': `Impossible de lire les enregistrements de Windows Installer`,
  'Error.ScanFailedTitle': `Échec de l'analyse`,
  'Error.InstallerDbEmpty': `Les enregistrements de Windows Installer sont revenus complètement vides : pas un seul programme installé ni une seule mise à jour ne revendique de fichier d'installation en cache. Cela n'arrive pas sur une machine qui fonctionne (même une installation neuve de Windows en a), donc soit les enregistrements sont endommagés, soit ils n'ont pas pu être lus, et une analyse qui croirait cette réponse qualifierait à tort d'orphelin chaque fichier de {InstallerFolder}. InstallerClean s'est arrêté à la place. Rien n'a été supprimé.`,
  'Error.MsiAccessDenied': `Windows Installer a refusé de laisser InstallerClean lister ce qui est installé. InstallerClean s'exécutait déjà en tant qu'administrateur, le relancer en tant qu'administrateur n'y changera rien. Sans cette liste, il n'y a aucun moyen sûr de savoir quels fichiers en cache servent encore, donc InstallerClean s'est arrêté. Rien n'a été supprimé.`,
  'Error.MsiNonSuccess': `Windows Installer n'a pas pu fournir à InstallerClean une liste lisible des programmes installés : {0} entrées d'affilée sont revenues illisibles (dernier code d'erreur {1}). Plutôt que de travailler sur une liste lue en partie, InstallerClean s'est arrêté. Rien n'a été supprimé.`,
  'Error.InvalidDestinationTitle': `Destination invalide`,
  'Error.DestinationWriteFailedTitle': `Impossible d'écrire dans la destination`,
  'Error.MoveFailedTitle': `Échec du déplacement`,
  'Error.DeleteFailedTitle': `Échec de la suppression`,
  'Error.SettingNotSavedTitle': `Paramètre non enregistré`,
  'Error.SettingNotSavedBody': `La modification n'a pas pu être enregistrée. Au prochain démarrage, InstallerClean reviendra au paramètre précédent.`,
  'Error.DestinationInsideInstaller': `La destination ne peut pas se trouver dans le dossier Windows Installer.`,

  // 0 = the destination path the user typed
  'Error.DestinationInSystemFolder': `The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Espace insuffisant`,

  // 0 = destination, 1 = required size, 2 = available size
  'Error.NotEnoughSpaceBody': `Espace insuffisant dans {0}\n\nNécessaire : {1}\nDisponible : {2}`,

  // 0 = destination
  'Error.AccessDeniedDestination': `Vous n'avez pas l'autorisation d'écrire dans {0}.\nEssayez un dossier dans votre profil utilisateur ou sur un lecteur qui vous appartient.`,
  'Error.PathTooLong': `Le chemin {0} est trop long pour Windows. Choisissez un chemin plus court.`,
  'Error.DestinationMissing': `Le dossier {0} n'existe pas et n'a pas pu être créé. Vérifiez la lettre de lecteur ou le chemin réseau.`,
  'Error.IOWriteDestination': `Windows ne peut pas écrire dans {0}.\nDétails dans {1}.`,
  'Error.IOWriteDestination.NoLog': `Windows ne peut pas écrire dans {0}. Le crash.log n'a pas pu être écrit.`,
  'Error.WriteDestination': `Impossible d'écrire dans {0}.\nDétails dans {1}.`,
  'Error.WriteDestination.NoLog': `Impossible d'écrire dans {0}. Le crash.log n'a pas pu être écrit.`,
  'Error.MissingSourceFile': `Le fichier n'existe plus.`,
  'Error.SourceIsReparsePoint': `Le fichier source est un lien symbolique ou une jonction ; refusé par sécurité.`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows a refusé l'accès à ce fichier ; il a été laissé en place.`,
  'Error.AccessDenied.Plural': `Windows a refusé l'accès à ces fichiers ; ils ont été laissés en place.`,
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `Windows a signalé une erreur de fichier ; le fichier a été laissé en place.`,
  'Error.IOFailure.Plural': `Windows a signalé des erreurs de fichier ; ces fichiers ont été laissés en place.`,
  'Error.UnknownError.Singular': `Un problème est survenu avec ce fichier ; il a été laissé en place.`,
  'Error.UnknownError.Plural': `Un problème est survenu avec ces fichiers ; ils ont été laissés en place.`,

  // 0 = shell error code

  // 0 = destination
  'Error.MoveIntoInstaller': `Refus de déplacer des fichiers dans le dossier Windows Installer (destination : {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\\Backup, or \\\\server\\backup). InstallerClean can't use this one: {0}`,
  'BrowserLaunch.FailedTitle': `Impossible d'ouvrir votre navigateur`,
  'UpdateCheck.Title': `Rechercher des mises à jour`,
  'UpdateCheck.Status.Checking': `Vérification...`,
  'UpdateCheck.Status.UpToDate': `À jour.`,
  'UpdateCheck.UpdateAvailable.Title': `Mise à jour disponible`,

  // 0 = installed version, 1 = latest version on GitHub
  'UpdateCheck.UpdateAvailable.Body': `Vous utilisez la version {0}.&#10;La version {1} est disponible.`,
  'UpdateCheck.Failed.NetworkUnavailable': `Impossible de joindre GitHub. Vérifiez votre connexion internet et réessayez.`,
  'UpdateCheck.Failed.ServerError': `GitHub a renvoyé une réponse d'erreur. Réessayez dans quelques minutes.`,
  'UpdateCheck.Failed.ResponseParseError': `La réponse de GitHub ne contenait pas de version reconnaissable. Réessayez plus tard, ou ouvrez directement la page des versions.`,
  'UpdateCheck.Failed.Timeout': `La vérification a expiré. Votre connexion à GitHub est peut-être lente ; réessayez.`,
  'UpdateCheck.Failed.Unknown': `La vérification a échoué pour une raison inconnue. Les détails sont dans le crash.log si vous devez le signaler.`,

  // 0 = the URL the user was trying to reach
  'BrowserLaunch.ClipboardOk': `InstallerClean n'a pas pu ouvrir votre navigateur. Le lien est dans votre presse-papiers, vous pouvez donc le coller vous-même :&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean n'a pas pu ouvrir votre navigateur, ni copier le lien dans le presse-papiers. Voici le lien :&#10;&#10;{0}`,

  // 0 = the destination folder whose canonical path changed mid-batch
  'Error.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again.`,

  // 0 = folder, 1 = inner exception message
  'Error.CannotWriteFolder': `Impossible d'écrire dans {0}.`,

  // 0 = file name
  'Error.NoUniqueFilename': `Impossible de trouver un nom de fichier unique pour « {0} » après 10 000 tentatives.`,

  // Result log (post-cleanup diagnostic send)
  'ResultLog.Sending': `Envoi...`,
  'ResultLog.Sent': `Merci ! Rapport envoyé.`,
  'ResultLog.Failed': `Échec de l'envoi. Réessayez plus tard.`,
  'ResultLog.NothingToSend': `Aucun rapport à envoyer.`,
  'ConfirmSendResultLog.Title': `Envoyer ceci ?`,
  'ConfirmSendResultLog.Reassurance': `Ça va vers nofaff.netlify.app/api/result-log. Rien ne vous identifie, ni votre machine ; ça me dit juste qu'InstallerClean fonctionne et [combien d'espace les gens libèrent].`,
  'Automation.ResultLogPreview': `Aperçu du rapport`,

  // Single instance / startup / crash
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean est déjà en cours d'exécution.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `Une erreur inattendue s'est produite et InstallerClean doit se fermer.\n\n{0}\n\nDétails écrits dans :\n{1}`,
  'Startup.UnhandledBody.NoLog': `Une erreur inattendue s'est produite et InstallerClean doit se fermer.\n\n{0}\n\nLe crash.log n'a pas pu être écrit.`,
  'Startup.ErrorTitle': `Erreur de démarrage`,
  'Startup.FailedToStart': `Échec du démarrage ({0}). Détails écrits dans :\n{1}`,
  'Startup.FailedToStart.NoLog': `Échec du démarrage ({0}). Le crash.log n'a pas pu être écrit.`,

  // File picker
  'FilePicker.ChooseDestinationTitle': `Choisissez le dossier de destination des fichiers déplacés`,

  // Version display

  // 0 = major.minor.patch (e.g. "1.5.4")
  'Version.Display': `Version {0}`,
  'Plural.File.Singular': `fichier`,
  'Plural.File.Plural': `fichiers`,
  'Plural.Error.Singular': `erreur`,
  'Plural.Error.Plural': `erreurs`,
  'Plural.Package.Singular': `paquet`,
  'Plural.Package.Plural': `paquets`,
  'Plural.Product.Singular': `produit`,
  'Plural.Product.Plural': `produits`,
  'Plural.Patch.Singular': `correctif`,
  'Plural.Patch.Plural': `correctifs`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ElapsedLong.LessThanASecond': `moins d'une seconde`,
  'Display.ElapsedLong.Seconds': `{0:F1} secondes`,
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
  'Tooltip.ChangeLanguage': `Changer la langue. Le programme redémarrera.`,
  'Automation.ChangeLanguage': `Changer la langue`,
  'Automation.ChangeLanguage.HelpText': `Le programme redémarrera.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  'Cli.UnknownArgument': `Error: unknown argument '{0}'`,
  'Cli.Cancelling': `Annulation...`,
  'Cli.Cancelled': `Annulé.`,
  'Cli.GenericError': `Error: unexpected failure ({0}). Details written to {1}.`,
  'Cli.GenericError.NoLog': `Error: unexpected failure ({0}). The crash log could not be written.`,
  'Cli.ScanningInstaller': `Analyse de {InstallerFolder}...`,
  'Cli.FoundOrphans': `Found {0} unneeded {1} to clean up ({2}).`,
  'Cli.DeletingFiles': `Deleting {0} unneeded {1}...`,
  'Cli.DeletedFiles': `Permanently deleted {0} unneeded {1}.`,
  'Cli.NoMoveDestination': `Erreur : aucune destination de déplacement spécifiée. Utilisez /m CHEMIN. (Une valeur par défaut définie dans l'interface est propre à chaque utilisateur et ne s'applique pas aux exécutions planifiées ou par compte de service.)`,
  'Cli.MoveDestinationInsideInstaller': `Erreur : la destination ne peut pas se trouver dans le dossier Windows Installer.`,
  'Cli.MoveDestinationRelative': `Erreur : la destination doit être un chemin entièrement qualifié. Reçu : {0}`,
  'Cli.MoveDestinationInSystemFolder': `Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning.`,
  'Cli.MovingFiles': `Moving {0} unneeded {1} to {2}...`,
  'Cli.MovedFiles': `Moved {0} unneeded {1}.`,
  'Cli.MutexBlocked': `Un autre processus InstallerClean détient le verrou d'instance unique (l'interface ou une autre exécution de la CLI). Code de sortie 75 (transitoire) ; vous pouvez réessayer plus tard sans risque.`,
  'Cli.EventLogUnavailable': `Remarque : l'écriture dans le journal des événements a échoué. Vérifiez les autorisations du journal Application ou la stratégie de groupe.`,
  'Cli.Help.Header': `InstallerClean - nettoyage de {InstallerFolder}`,
  'Cli.Help.Usage': `Utilisation :`,
  'Cli.Help.Help': `  installerclean-cli --help     Affiche cette aide (accepte aussi /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  Affiche la version (accepte aussi -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Move to the saved backup folder`,
  'Cli.Help.MovePath': `  installerclean-cli /m CHEMIN  Déplace vers le chemin spécifié`,
  'Cli.Help.NoteLine1': `installerclean-cli blocks the prompt until it finishes, so a script or&#10;scheduled task can wait on it.`,
  'Cli.Help.ExitCodesHeader': `Codes de sortie :`,
  'Cli.Help.ExitCodeOk': `  0   success: the run finished with nothing left to do`,
  'Cli.Help.ExitCodeError': `  1   failure: nothing processed (bad arguments, a bad destination, a&#10;       failed scan or every file failed)`,
  'Cli.Help.ExitCodePartial': `  2   partial: some processed, some not (a failure or a Ctrl+C part way)`,
  'Cli.Help.ExitCodeTransient': `  75  transitoire : quelque chose a bloqué l'exécution (voir le message)`,
  'Cli.Help.ExitCodeCancelled': `  130 annulé (Ctrl+C)`,
  'Body.NotScanned.Lead': `Rien n'a encore été analysé.`,
  'Body.NotScanned.Why': `Cliquez sur Réanalyser pour parcourir {InstallerFolder} à la recherche de fichiers d'installation dont aucun programme n'a plus besoin.`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,
  'Error.ScanCorrelationFailed': `InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed.`,
  'Error.CandidateOutsideCache': `Ce fichier ne se trouve pas directement dans le dossier Windows Installer ; refusé par sécurité.`,
  'Completion.ReverifySkipped': `{0} {1} laissés en place, redevenus nécessaires à un programme après l'analyse.`,
  'Completion.MoveCancelledSummary': `Déplacé {0} sur {1} {2} avant votre annulation.`,
  'Completion.PermanentDeleteCancelledSummary': `Supprimé définitivement {0} sur {1} {2} avant votre annulation.`,
  'Body.PendingReboot.Lead': `Ces fichiers ne peuvent pas être nettoyés pour le moment.`,
  'Cli.TooManyArguments': `Erreur : argument supplémentaire inattendu « {0} ». Si votre dossier de déplacement contient un espace, mettez le chemin entier entre guillemets : /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `That folder is saved per-user; scheduled or SYSTEM runs need /m PATH.`,
  'Completion.ReverifyIncomplete': `{0} {1} kept in place, because the Windows Installer records could not be fully read in the final check.`,
  'Summary.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Summary.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Error.ScanRecordsUnreadable': `InstallerClean n'a pas pu lire assez des enregistrements de Windows Installer pour savoir avec certitude ce qui sert encore : la liste des programmes installés est revenue incomplète, et lire ces mêmes enregistrements directement dans le registre a également donné des erreurs. Un fichier pourrait sembler orphelin uniquement parce que l'enregistrement qui le nomme faisait partie des illisibles, donc InstallerClean s'est arrêté. Rien n'a été supprimé.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer n'a jamais signalé la fin de la liste des programmes installés : InstallerClean a renoncé après {0} entrées (dernier code d'erreur {1}). Une liste sans fin n'est pas fiable, donc InstallerClean s'est arrêté. Rien n'a été supprimé.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer n'a jamais signalé la fin de la liste des correctifs d'un programme : InstallerClean a renoncé après {0} entrées (dernier code d'erreur {1}). Une liste sans fin n'est pas fiable, donc InstallerClean s'est arrêté. Rien n'a été supprimé.`,
  'UpdateCheck.Status.UpdateAvailable': `La version {0} est disponible.`,
  'Completion.DonateAsk': `Content d'avoir pu aider. La cagnotte est là, si le cœur vous en dit.`,
  'About.Link.Guide': `Guide et FAQ`,
  'About.Link.ReportProblem': `Signaler un problème`,
  'About.AutoUpdateCheck': `Rechercher des mises à jour automatiquement`,
  'Automation.About.Guide.HelpText': `Ouvre le readme sur github dans votre navigateur.`,
  'Automation.About.ReportProblem.HelpText': `Ouvre le suivi des problèmes (Issues) sur github.com dans votre navigateur.`,
  'Automation.AutoUpdateCheck.HelpText': `Si la case est cochée, InstallerClean recherche une version plus récente sur github à son lancement.`,
  'Tooltip.MoveSameDrive': `Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Confirm.DeletePermanently.Plural': `Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed.`,
  'Automation.Scroll.ProductDetails': `Product details`,
  'Body.PendingReboot.Other': `Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back.`,
  'Cli.TooManyArgumentsNoPath': `Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run.`,
  'Cli.MissingFromDisk.Singular': `{0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it.`,
  'Cli.MissingFromDisk.Plural': `{0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them.`,
  'Cli.MoveNotEnoughSpace': `Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.Other': `Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes.`,
  'Cli.FoundNoOrphans': `Found no unneeded files.`,
  'Cli.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again.`,
  'Cli.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Cli.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Cli.Help.Summary': `Removes cached .msi and .msp files that no installed program still needs.`,
  'Cli.Help.Elevation': `Needs an elevated (administrator) prompt; Windows will not start it.`,
  'Error.InstallerLockUnavailableTitle': `Rien n'a été supprimé`,
  'Error.InstallerLockUnavailable': `InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
  'Cli.InstallerLockUnavailable': `Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
  'Completion.ReverifyRecordsChanged': `{0} {1} kept in place, because the Windows Installer records had changed by the final check.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the machine-contract Cli.* <data> elements BY NAME.
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
let cliMachineRemoved = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (isMachineCliKey(name)) { cliMachineRemoved++; return ''; } return m; });

// Replace each key's inner <value> from MAP.
const escRe = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
const notApplied = [];
for (const [key, val] of Object.entries(MAP)) {
  const re = new RegExp('(<data name="' + escRe(key) + '"[^>]*>\\s*<value>)([\\s\\S]*?)(</value>)');
  let applied = false;
  text = text.replace(re, (m, p1, p2, p3) => { applied = true; return p1 + val + p3; });
  if (!applied) notApplied.push(key);
}

// Append the satellite-only override <data> elements before </root>. Values carry
// no XML-special characters (same as the MAP). Empty OVERRIDES means no block, so
// the output is byte-identical to a no-override language (e.g. Korean).
const overrideBlock = Object.entries(OVERRIDES)
  .map(([k, v]) => `  <data name="${k}" xml:space="preserve"><value>${v}</value></data>`)
  .join('\n');
if (overrideBlock) text = text.replace('</root>', overrideBlock + '\n</root>');

// Normalise to LF with exactly one trailing newline.
text = text.replace(/\r\n/g, '\n');
if (!text.endsWith('\n')) text += '\n';

writeFileSync(OUT, text, 'utf8');

// ---------------- self-check the written file against the neutral ----------------
const placeholders = (s) => new Set([...s.matchAll(/\{(\d+)(?::[^}]*)?\}/g)].map((p) => p[1]));
const parse = (xml) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'));
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written);
const neutralRequired = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

// Satellite-only overrides: present, and each sharing its base key's {N} set
// (base = the <Prefix>.Plural sibling if the neutral has one, else the flat key).
const overrideKeys = Object.keys(OVERRIDES);
const overrideMissing = overrideKeys.filter((k) => !output.has(k));
const overrideArityMismatch = overrideKeys.filter((k) => {
  if (!output.has(k)) return true;
  const base = k.replace(/\.(?:One|Few|Many)$/, '');
  const ref = neutral.has(`${base}.Plural`) ? `${base}.Plural` : base;
  if (!neutral.has(ref)) return true; // base must exist in the neutral
  const a = placeholders(neutral.get(ref)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});

const missingFromMap = neutralRequired.filter((k) => !(k in MAP));
const strayMapKeys = Object.keys(MAP).filter((k) => !neutral.has(k));
const machineLeaked = [...output.keys()].filter(isMachineCliKey);

// The one human-facing Cli.EventLog* key, asserted present rather than left to
// the counts: a predicate that stopped discriminating it takes it out of the
// output AND out of the required set, so every figure above still agrees. The
// MAP substitution notices today only through the order the two run in.
const humanCliStripped = !output.has('Cli.EventLogUnavailable');
const missingFromOutput = neutralRequired.filter((k) => !output.has(k));
const arityMismatch = neutralRequired.filter((k) => {
  if (!output.has(k)) return false;
  const a = placeholders(neutral.get(k)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});
const crlf = (written.match(/\r/g) || []).length;

const alsoKeep = new Set(ALSO_KEEP);
const untranslated = neutralRequired.filter((k) =>
  output.has(k) && output.get(k) === neutral.get(k) && !KEEP_ENGLISH.has(k) && !alsoKeep.has(k));

// Breakdown computed, never pinned: the non-Cli and human-Cli totals both grow with
// every string the app gains, and a hardcoded pair goes stale silently while the
// checked figure beside it stays right.
const nonCliRequired = neutralRequired.filter((k) => !k.startsWith('Cli.')).length;
console.log('translatable <data> in output:', output.size,
  '(expect', neutralRequired.length + overrideKeys.length,
  '=', nonCliRequired, 'non-Cli +', neutralRequired.length - nonCliRequired, 'Cli +',
  overrideKeys.length, 'override)');
console.log('machine Cli <data> removed:', cliMachineRemoved, `(expect ${cliMachineExpected})`);
console.log('MAP entries:', Object.keys(MAP).length, '| override keys:', overrideKeys.length, '| CRLF:', crlf, '(expect 0)');

if (alsoKeep.size) {
  console.log('ALSO_KEEP (' + alsoKeep.size + '), kept identical to English:');
  for (const k of alsoKeep) {
    const v = output.get(k);
    const words = v == null ? 0 : v.replace(/\{\d+(?::[^}]*)?\}/g, ' ').trim().split(/\s+/).filter(Boolean).length;
    const suspicious = v != null && (words > 2 || v.length > 24);
    console.log('   ' + (suspicious ? '!! suspicious (longer than a word or two) ' : '') + k + ' = ' + JSON.stringify(v));
  }
}

if (notApplied.length) console.log('!! value not applied (regex miss):', notApplied);
if (missingFromMap.length) console.log('!! in neutral but missing from MAP:', missingFromMap);
if (strayMapKeys.length) console.log('!! in MAP but not in neutral:', strayMapKeys);
if (missingFromOutput.length) console.log('!! required key missing from output:', missingFromOutput);
if (arityMismatch.length) console.log('!! placeholder arity differs from neutral:', arityMismatch);
if (machineLeaked.length) console.log('!! machine Cli keys leaked into output:', machineLeaked);
if (humanCliStripped) console.log('!! Cli.EventLogUnavailable stripped: that key is human-facing and must stay');
if (overrideMissing.length) console.log('!! override key missing from output:', overrideMissing);
if (overrideArityMismatch.length) console.log('!! override arity differs from its base key:', overrideArityMismatch);
if (untranslated.length) {
  const show = untranslated.slice(0, 40).join(', ');
  console.log('!! still English (untranslated), ' + untranslated.length + ': ' + show +
    (untranslated.length > 40 ? ', ...and ' + (untranslated.length - 40) + ' more' : ''));
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  !humanCliStripped &&
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === cliMachineExpected && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
