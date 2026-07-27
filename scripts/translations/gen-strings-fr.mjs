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
//   - Terms: Corbeille (Recycle Bin), À propos (About), correctif(s) (patch),
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
  'Completion.ReverifyIncomplete.One': `{0} {1} laissé en place, car les enregistrements de Windows Installer n'ont pas pu être entièrement lus lors de la nouvelle vérification.`,
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
  'Section.Move.Location': `EMPLACEMENT DE DESTINATION`,
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
  'Action.Delete': `_Supprimer`,
  'Action.DeletePermanently': `_Supprimer définitivement`,
  'Action.Done': `_Terminé`,
  'Action.Details': `Détails`,
  'Action.BuyMeACuppa': `Offrez-moi un _café`,
  'Action.LeaveStarOnGitHub': `_Laisser une étoile sur GitHub`,
  'Action.Licence': `Licence Apache 2.0`,
  'Action.Move': `_Déplacer`,
  'Action.MoveInstead': `_Déplacer plutôt`,
  'Action.MoveDestinationPlaceholder': `Chemin du dossier si vous déplacez au lieu de supprimer`,
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
  'Automation.ConfirmDelete': `Supprimer déplace les fichiers inutiles vers la Corbeille. Annuler ferme sans rien supprimer.`,
  'Automation.ConfirmMove': `Déplacer place les fichiers inutiles dans le dossier de destination choisi. Annuler les laisse où ils sont.`,
  'Automation.RecycleUnavailable': `Choisissez quoi faire des fichiers inutiles : les déplacer en lieu sûr, les supprimer définitivement ou annuler.`,
  'Automation.RecycleUnavailableMove': `Déplacer les fichiers inutiles vers un dossier que vous choisissez`,
  'Automation.RecycleUnavailableDeletePermanently': `Supprimer définitivement les fichiers inutiles parce que la Corbeille est indisponible pour ce lecteur`,
  'Automation.SayThanks': `Dire merci`,
  'Automation.ConfirmSendResultLog': `Envoyer transmet à No Faff le rapport affiché. Annuler n'envoie rien.`,
  'Automation.CheckForUpdates': `Rechercher des mises à jour`,
  'Automation.CheckForUpdates.HelpText': `Vérifie sur la page des versions de github s'il existe une version plus récente.`,
  'Automation.UpdateAvailable.HelpText': `Ouvrez la page de la version pour télécharger la version plus récente, ou annulez pour conserver la version actuelle.`,
  'Automation.Licence.HelpText': `Ouvre le fichier de licence sur github.com dans votre navigateur.`,
  'Automation.Section.MoveLocation': `Emplacement de destination`,
  'Automation.Section.Products': `Produits`,
  'Automation.Section.Patches': `Correctifs`,
  'Automation.Section.ProductDetails': `Détails du produit`,
  'Automation.MoveDestinationFolder': `Emplacement de destination`,
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
  'Tooltip.Move': `Déplacer les fichiers inutiles vers l'emplacement de destination.`,
  'Tooltip.MoveNeedsDestination': `Déplacer les fichiers inutiles en lieu sûr. Vous choisirez le dossier ensuite.`,
  'Tooltip.Delete': `Déplacer les fichiers inutiles vers la Corbeille.`,
  'Tooltip.SigningCertificate': `Nom du titulaire du certificat Authenticode incorporé. Chaîne non vérifiée.`,

  // Body copy
  'Body.MainExplanation.Lead': `Les éventuels fichiers inutiles ci-dessous peuvent être supprimés sans risque.`,
  'Body.MainExplanation.Why': `Ils se trouvent dans {InstallerFolder}, laissés là quand un programme a été désinstallé ({0}), qu'un correctif plus récent en a remplacé un ({1}) ou que l'éditeur l'a retiré ({2}). InstallerClean ne liste jamais que les fichiers dont Windows lui-même déclare avoir fini de se servir.`,
  'Body.MainExplanation.Action': `Supprimez-les vers la Corbeille, ou utilisez plutôt Déplacer pour en conserver une copie de sauvegarde. Remettre les fichiers dans {InstallerFolder} vous ramène exactement à votre point de départ.`,
  'Body.PendingReboot.MsiExecuteMutex': `Quelque chose utilise Windows Installer en ce moment, généralement une mise à jour Windows ou un programme en cours d'installation en arrière-plan. Déplacer et Supprimer sont en pause pendant ce temps, pour qu'InstallerClean ne touche pas au cache d'installation tant qu'il change. Une fois terminé, réanalysez et ils réapparaissent.`,
  'Body.PendingReboot.InstallerInProgress': `Une transaction Windows Installer précédente est suspendue sur cette machine. Reprenez ou annulez cette installation (ou redémarrez Windows) avant de nettoyer le cache.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows a un renommage de fichier en file d'attente pour le prochain redémarrage qui affecte le cache d'installation. Redémarrez Windows avant de nettoyer.`,
  'Body.NoFileSelected': `Sélectionnez un fichier pour voir les détails.`,
  'Body.NoProductSelected': `Sélectionnez un produit pour voir les détails.`,
  'Body.NoMetadata': `Aucune métadonnée disponible.`,
  'Body.RegisteredMissingFromDisk': `Ce fichier d'installation a été supprimé. Ce n'est pas InstallerClean qui l'a fait : il ne retire jamais un fichier dont un programme a encore besoin ; autre chose a supprimé celui-ci avant que vous ne lanciez InstallerClean.&#10;&#10;Pour l'instant, cela ne pose aucun problème, et n'en posera pas avant le jour où vous tenterez de réparer, mettre à jour ou désinstaller le programme auquel il appartient. Cette étape pourra alors échouer, parce que Windows cherche ce fichier et qu'il n'est pas là.&#10;&#10;Pour tenter d'y remédier, téléchargez le programme d'installation de ce logiciel chez son éditeur et lancez-le par-dessus votre copie actuelle (ne désinstallez pas d'abord : désinstaller est en soi une étape qui a besoin de ce fichier). Utilisez si possible la version que vous avez installée, car Windows pourrait en refuser une autre. Cela rétablit en général le fichier, et vos réglages restent normalement intacts, mais Microsoft ne le garantit pas : son propre dernier recours est de réinstaller le programme, ou Windows lui-même.`,
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
  'Status.Moving': `Déplacement de {0} {1}...`,
  'Status.Deleting': `Suppression de {0} {1}...`,
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
  'Completion.MoveRestoreHint': `Recopiez-les dans {InstallerFolder} si jamais quelque chose ne fonctionne plus ([extrêmement improbable]).`,
  'Completion.DeleteRestoreHint': `D'ici là, vous pouvez les restaurer si jamais quelque chose ne fonctionne plus ([extrêmement improbable]).`,

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
  'Completion.DeleteSummary.Singular': `Déplacé {0} {1} vers la Corbeille`,
  'Completion.DeleteSummary.Plural': `Déplacé {0} {1} vers la Corbeille`,

  // 0 = deleted count, 1 = pluralised noun
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} supprimé définitivement. Il n'est pas allé à la Corbeille.`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} supprimés définitivement. Ils ne sont pas allés à la Corbeille.`,
  'Completion.PermanentDeleteRestoreHint.Singular': `C'est normal, il n'y avait aucun risque à le retirer. InstallerClean n'efface que les fichiers dont Windows déclare avoir fini de se servir, jamais un dont un programme a encore besoin. Dans le cas improbable où une suppression empêcherait un jour un programme de se réparer, se mettre à jour ou se désinstaller, le réinstaller depuis son éditeur rétablit en général le fichier, même si Microsoft ne le garantit pas.`,
  'Completion.PermanentDeleteRestoreHint.Plural': `C'est normal, il n'y avait aucun risque à les retirer. InstallerClean n'efface que les fichiers dont Windows déclare avoir fini de se servir, jamais un dont un programme a encore besoin. Dans le cas improbable où une suppression empêcherait un jour un programme de se réparer, se mettre à jour ou se désinstaller, le réinstaller depuis son éditeur rétablit en général le fichier, même si Microsoft ne le garantit pas.`,
  'RecycleUnavailable.Heading': `La Corbeille n'est pas disponible pour ce lecteur`,
  'RecycleUnavailable.Body.Singular': `Donc ce {1} ({2}) n'a pas été supprimé. Vous pouvez le déplacer en lieu sûr, ou le supprimer définitivement.`,
  'RecycleUnavailable.Body.Plural': `Donc ces {0} {1} ({2}) n'ont pas été supprimés. Vous pouvez les déplacer en lieu sûr, ou les supprimer définitivement.`,
  'RecycleUnavailable.Reassurance.Singular': `Le supprimer ne présente aucun risque. InstallerClean n'efface que les fichiers dont Windows déclare avoir fini de se servir, jamais un dont un programme a encore besoin, et la Corbeille n'est qu'une protection supplémentaire. Dans le cas improbable où une suppression empêcherait un jour un programme de se réparer, se mettre à jour ou se désinstaller, le réinstaller depuis son éditeur rétablit en général le fichier, même si Microsoft ne le garantit pas.`,
  'RecycleUnavailable.Reassurance.Plural': `Les supprimer ne présente aucun risque. InstallerClean n'efface que les fichiers dont Windows déclare avoir fini de se servir, jamais un dont un programme a encore besoin, et la Corbeille n'est qu'une protection supplémentaire. Dans le cas improbable où une suppression empêcherait un jour un programme de se réparer, se mettre à jour ou se désinstaller, le réinstaller depuis son éditeur rétablit en général le fichier, même si Microsoft ne le garantit pas.`,

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
  'Confirm.DeleteToRecycleBin': `Les fichiers seront déplacés vers la Corbeille. Si vous voulez des copies de sauvegarde, utilisez plutôt le bouton Déplacer.`,

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
  'Error.DestinationInSystemFolder': `La destination {0} se trouve dans un dossier système de Windows. Choisissez un chemin en dehors de %SystemRoot%, %ProgramFiles% et %ProgramData%.`,
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
  'Error.FileInUse.Singular': `Ce fichier est ouvert ou verrouillé par un autre programme, donc rien ne peut le déplacer pour l'instant. Il a été laissé en place ; réessayez plus tard.`,
  'Error.FileInUse.Plural': `Ces fichiers sont ouverts ou verrouillés par un autre programme, donc rien ne peut les déplacer pour l'instant. Ils ont été laissés en place ; réessayez plus tard.`,
  'Error.IOFailure.Singular': `Windows a signalé une erreur de fichier ; le fichier a été laissé en place.`,
  'Error.IOFailure.Plural': `Windows a signalé des erreurs de fichier ; ces fichiers ont été laissés en place.`,
  'Error.UnknownError.Singular': `Un problème est survenu avec ce fichier ; il a été laissé en place.`,
  'Error.UnknownError.Plural': `Un problème est survenu avec ces fichiers ; ils ont été laissés en place.`,

  // 0 = shell error code
  'Error.ShellRecycleFailed': `Impossible de déplacer ce fichier vers la Corbeille (erreur {0}), et ce code ne permet pas à InstallerClean de vous dire pourquoi. Le fichier a été laissé en place. Essayez plutôt le bouton Déplacer, qui n'utilise pas la Corbeille.`,
  'Error.RecycleAccessDenied': `Windows a refusé l'accès même avec des droits d'administrateur (erreur {0}), et InstallerClean ne peut pas déterminer si le problème vient du fichier ou de la Corbeille. Le fichier a été laissé en place. Le bouton Déplacer fonctionnera si le problème vient de la Corbeille, mais pas s'il vient du fichier.`,
  'Error.RecycleInUse': `Ce fichier est ouvert ou verrouillé par un autre programme (erreur {0}), donc rien ne peut le supprimer pour l'instant. Il a été laissé en place ; réessayez plus tard.`,
  'Error.DeletedNotRecycled': `Windows a supprimé ce fichier purement et simplement au lieu de le déplacer vers la Corbeille. InstallerClean avait demandé la Corbeille, et Windows a fait autrement. Le fichier n'existe plus.`,

  // 0 = destination
  'Error.MoveIntoInstaller': `Refus de déplacer des fichiers dans le dossier Windows Installer (destination : {0}).`,

  // 0 = the relative path the caller passed
  'Error.DestinationNotFullyQualified': `L'emplacement de destination doit être un chemin complet vers un dossier, commençant par une lettre de lecteur ou un partage réseau (par exemple D:\\Backup, ou \\\\server\\backup). InstallerClean ne peut pas utiliser celui-ci : {0}`,
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
  'Error.DestinationChangedMidBatch': `L'emplacement de destination a changé pendant le déplacement des fichiers (quelque chose a remplacé ou redirigé le dossier), donc InstallerClean s'est arrêté plutôt que d'écrire au mauvais endroit. Vérifiez {0}, puis réanalysez et réessayez.`,

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
  'CrashLog.PrivacyHeader': `# crash.log capture les exceptions non gérées d'InstallerClean.\n# Sous élévation, les messages d'exception du framework peuvent\n# inclure des chemins de fichiers de la session en cours (y compris\n# les profils d'autres utilisateurs énumérés par les requêtes Windows\n# Installer). Les messages d'échec réseau de la vérification de mises\n# à jour ou de l'envoi du rapport de résultats peuvent inclure l'URL\n# de destination et l'adresse IP / proxy résolue. Expurgez ces deux\n# types de détail avant de joindre ce fichier à un rapport de bug\n# public.\n`,
  'Tooltip.ChangeLanguage': `Changer la langue. Le programme redémarrera.`,
  'Automation.ChangeLanguage': `Changer la langue`,
  'Automation.ChangeLanguage.HelpText': `Le programme redémarrera.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys.
  'Cli.UnknownArgument': `Argument inconnu : « {0} »`,
  'Cli.Cancelling': `Annulation...`,
  'Cli.Cancelled': `Annulé.`,
  'Cli.GenericError': `Erreur : {0}. Détails écrits dans {1}.`,
  'Cli.GenericError.NoLog': `Erreur : {0}. Le crash.log n'a pas pu être écrit.`,
  'Cli.ScanningInstaller': `Analyse de {InstallerFolder}...`,
  'Cli.FoundOrphans': `Trouvé {0} {1} à nettoyer ({2}).`,
  'Cli.NothingToDo': `Rien à faire.`,
  'Cli.DeletingFiles': `Suppression de {0} {1}...`,
  'Cli.DeletedFiles': `Supprimé {0} {1}.`,
  'Cli.RecycleUnavailable': `Erreur : la Corbeille est indisponible pour ce volume, donc rien n'a été supprimé. Utilisez /m pour déplacer les fichiers, ou réactivez la Corbeille et relancez.`,
  'Cli.NoMoveDestination': `Erreur : aucune destination de déplacement spécifiée. Utilisez /m CHEMIN. (Une valeur par défaut définie dans l'interface est propre à chaque utilisateur et ne s'applique pas aux exécutions planifiées ou par compte de service.)`,
  'Cli.MoveDestinationInsideInstaller': `Erreur : la destination ne peut pas se trouver dans le dossier Windows Installer.`,
  'Cli.MoveDestinationRelative': `Erreur : la destination doit être un chemin entièrement qualifié. Reçu : {0}`,
  'Cli.MoveDestinationInSystemFolder': `Erreur : la destination {0} se trouve dans un dossier système de Windows. Choisissez un chemin en dehors de %SystemRoot%, %ProgramFiles% et %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Erreur : quelque chose utilise Windows Installer en ce moment, généralement une mise à jour Windows ou un programme en cours d'installation en arrière-plan. Déplacer et Supprimer sont bloqués pendant ce temps. Réessayez une fois que c'est terminé.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Erreur : une transaction Windows Installer précédente est suspendue sur cette machine. Reprenez ou annulez cette installation (ou redémarrez Windows) avant de nettoyer le cache.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Erreur : une opération de fichier en file d'attente pour le prochain redémarrage vise le cache d'installation ({0}). Redémarrez Windows pour terminer cette opération avant de nettoyer.`,
  'Cli.MovingFiles': `Déplacement de {0} {1} vers {2}...`,
  'Cli.MovedFiles': `Déplacé {0} {1}.`,
  'Cli.MutexBlocked': `Un autre processus InstallerClean détient le verrou d'instance unique (l'interface ou une autre exécution de la CLI). Code de sortie 75 (transitoire) ; vous pouvez réessayer plus tard sans risque.`,
  'Cli.EventLogUnavailable': `Remarque : l'écriture dans le journal des événements a échoué. Vérifiez les autorisations du journal Application ou la stratégie de groupe.`,
  'Cli.Help.Header': `InstallerClean - nettoyage de {InstallerFolder}`,
  'Cli.Help.Usage': `Utilisation :`,
  'Cli.Help.Help': `  installerclean-cli --help     Affiche cette aide (accepte aussi /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  Affiche la version (accepte aussi -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Analyse seule - fichiers inutiles`,
  'Cli.Help.Delete': `  installerclean-cli /d         Supprime les fichiers inutiles (Corbeille)`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Déplace vers l'emplacement enregistré`,
  'Cli.Help.MovePath': `  installerclean-cli /m CHEMIN  Déplace vers le chemin spécifié`,
  'Cli.Help.NoteLine1': `installerclean-cli est un vrai processus de console et bloque l'invite`,
  'Cli.Help.NoteLine2': `jusqu'à la fin ; redirigez ou acheminez sa sortie comme vous le feriez`,
  'Cli.Help.NoteLine3': `pour tout autre exécutable de console. L'interface est InstallerClean.exe.`,
  'Cli.Help.ExitCodesHeader': `Codes de sortie :`,
  'Cli.Help.ExitCodeOk': `  0   succès : tous les fichiers signalés ont été traités`,
  'Cli.Help.ExitCodeError': `  1   échec : rien de traité (arguments, analyse ou fichiers en échec)`,
  'Cli.Help.ExitCodePartial': `  2   partiel : certains fichiers traités, d'autres ont échoué`,
  'Cli.Help.ExitCodeTransient': `  75  transitoire : quelque chose a bloqué l'exécution (voir le message)`,
  'Cli.Help.ExitCodeCancelled': `  130 annulé (Ctrl+C)`,
  'Completion.CleanedUp': `{0} nettoyés`,
  'Completion.DeleteSpaceHint': `Videz la Corbeille pour vraiment récupérer l'espace.`,
  'Body.NotScanned.Lead': `Rien n'a encore été analysé.`,
  'Body.NotScanned.Why': `Cliquez sur Réanalyser pour parcourir {InstallerFolder} à la recherche de fichiers d'installation dont aucun programme n'a plus besoin.`,
  'Confirm.MoveSameDrive': `Ce dossier est sur le même lecteur, donc le déplacement ne libérera pas d'espace à lui seul. Vous récupérerez cet espace en supprimant les fichiers qui s'y trouvent, ou vous pouvez plutôt choisir un dossier sur un autre lecteur.`,
  'Error.ScanCorrelationFailed': `InstallerClean n'a pas pu faire concorder cette analyse avec les enregistrements de Windows Installer : tous les fichiers que Windows liste encore comme nécessaires sont absents de {InstallerFolder}, tandis que les fichiers réellement présents dans le dossier ne correspondent à aucun enregistrement. Aucune machine réelle ne ressemble à cela, ce qui indique un problème de lecture des enregistrements plutôt que des fichiers que vous pouvez supprimer sans risque. Rien n'a été proposé au nettoyage et rien n'a été supprimé.`,
  'Error.CandidateOutsideCache': `Ce fichier ne se trouve pas directement dans le dossier Windows Installer ; refusé par sécurité.`,
  'Completion.ReverifySkipped': `{0} {1} laissés en place, redevenus nécessaires à un programme après l'analyse.`,
  'Completion.MoveCancelledSummary': `Déplacé {0} sur {1} {2} avant votre annulation.`,
  'Completion.DeleteCancelledSummary': `Déplacé {0} sur {1} {2} vers la Corbeille avant votre annulation.`,
  'Completion.PermanentDeleteCancelledSummary': `Supprimé définitivement {0} sur {1} {2} avant votre annulation.`,
  'Body.PendingReboot.Lead': `Ces fichiers ne peuvent pas être nettoyés pour le moment.`,
  'Cli.TooManyArguments': `Erreur : argument supplémentaire inattendu « {0} ». Si votre dossier de déplacement contient un espace, mettez le chemin entier entre guillemets : /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Emplacement enregistré : par utilisateur ; planifié ou SYSTEM : /m CHEMIN.`,
  'Completion.ReverifyIncomplete': `{0} {1} laissés en place, car les enregistrements de Windows Installer n'ont pas pu être entièrement lus lors de la nouvelle vérification.`,
  'Summary.ProgramsUnreadable.Singular': `{0} programme installé n'a pas pu être lu lors de cette analyse, les correctifs remplacés ont donc été conservés. Les fichiers orphelins ne sont pas concernés.`,
  'Summary.ProgramsUnreadable.Plural': `{0} programmes installés n'ont pas pu être lus lors de cette analyse, les correctifs remplacés ont donc été conservés. Les fichiers orphelins ne sont pas concernés.`,
  'Error.ScanRecordsUnreadable': `InstallerClean n'a pas pu lire assez des enregistrements de Windows Installer pour savoir avec certitude ce qui sert encore : la liste des programmes installés est revenue incomplète, et lire ces mêmes enregistrements directement dans le registre a également donné des erreurs. Un fichier pourrait sembler orphelin uniquement parce que l'enregistrement qui le nomme faisait partie des illisibles, donc InstallerClean s'est arrêté. Rien n'a été supprimé.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer n'a jamais signalé la fin de la liste des programmes installés : InstallerClean a renoncé après {0} entrées (dernier code d'erreur {1}). Une liste sans fin n'est pas fiable, donc InstallerClean s'est arrêté. Rien n'a été supprimé.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer n'a jamais signalé la fin de la liste des correctifs d'un programme : InstallerClean a renoncé après {0} entrées (dernier code d'erreur {1}). Une liste sans fin n'est pas fiable, donc InstallerClean s'est arrêté. Rien n'a été supprimé.`,
  'Status.CheckingRecycleBin': `Vérification de la Corbeille...`,
  'UpdateCheck.Status.UpdateAvailable': `La version {0} est disponible.`,
  'Completion.DonateAsk': `Content d'avoir pu aider. La cagnotte est là, si le cœur vous en dit.`,
  'About.Link.Guide': `Guide et FAQ`,
  'About.Link.ReportProblem': `Signaler un problème`,
  'About.AutoUpdateCheck': `Rechercher des mises à jour automatiquement`,
  'Automation.About.Guide.HelpText': `Ouvre le readme sur github dans votre navigateur.`,
  'Automation.About.ReportProblem.HelpText': `Ouvre le suivi des problèmes (Issues) sur github.com dans votre navigateur.`,
  'Automation.AutoUpdateCheck.HelpText': `Si la case est cochée, InstallerClean recherche une version plus récente sur github à son lancement.`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the 21 machine-contract Cli.* <data> elements BY NAME.
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
console.log('machine Cli <data> removed:', cliMachineRemoved, '(expect 21)');
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
if (overrideMissing.length) console.log('!! override key missing from output:', overrideMissing);
if (overrideArityMismatch.length) console.log('!! override arity differs from its base key:', overrideArityMismatch);
if (untranslated.length) {
  const show = untranslated.slice(0, 40).join(', ');
  console.log('!! still English (untranslated), ' + untranslated.length + ': ' + show +
    (untranslated.length > 40 ? ', ...and ' + (untranslated.length - 40) + ' more' : ''));
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === 21 && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
