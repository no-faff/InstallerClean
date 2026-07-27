#!/usr/bin/env node
// Italian (it) satellite generator for InstallerClean.
//
// PROVENANCE: the Italian translation was a machine first cut by the project
// owner, then reviewed line by line by the native speaker bovirus (PRs #32 and #39),
// the project's only native sign-off. This generator's MAP holds bovirus's
// reviewed wording; regenerating reproduces the reviewed Strings.it.resx. Treat
// the MAP values as the native review's output, not a fresh machine draft.
//
// Structure (same as the other satellites): strips the 21 machine-contract
// Cli.EventLog* keys (forced English at the emit site, see MachineContract.cs),
// and appends 5 satellite-only plural .One overrides (Italian counts n==1 as
// singular; the registered-count adjective, the CLI count participles and the
// re-verify-skipped summary each need their own n==1 form). See
// gen-strings-template.mjs for how the body works.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.it.resx`;

// Universal keeps (brand names, pure-placeholder, size/elapsed formats).
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',
  'Startup.AlreadyRunningTitle',
  'Startup.UnhandledTitle',
  'Automation.ScanResultAnnouncement',
  'Display.Size.GB',
  'Display.Size.MB',
  'Display.Size.KB',
  'Display.Size.B',
  'Display.Elapsed.Ms',
  'Display.Elapsed.S',
]);

// Per-language keeps: Italian words byte-identical to English (genuine
// single-token matches, not misses). The self-check prints these so the keep
// stays honest.
const ALSO_KEEP = [
  'Field.File',
  'Plural.File.Singular',
  'Plural.Patch.Singular',
];

// Satellite-only CLDR plural overrides: keys absent from the neutral, appended
// before </root> and read by name at runtime (DisplayHelpers.Pluralise's One
// branch; an absent one falls back to the base).
const OVERRIDES = {
  'Status.RegisteredPackagesFound.One': `Trovato {0} {1} registrato.`,
  'Cli.FoundOrphans.One': `Trovato {0} {1} da eliminare ({2}).`,
  'Cli.DeletedFiles.One': `Eliminato {0} {1}.`,
  'Cli.MovedFiles.One': `Spostato {0} {1}.`,
  'Completion.ReverifySkipped.One': `{0} {1} lasciato al suo posto, perché un programma è tornato ad averne bisogno dopo la scansione.`,
  // Participle and possessive agreement: "lasciato al suo posto" for one file.
  'Completion.ReverifyIncomplete.One': `{0} {1} lasciato al suo posto, perché non è stato possibile leggere completamente i record di Windows Installer quando il controllo è stato ripetuto.`,
};

const MAP = {
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Informazioni`,
  'Window.Registered.Title': `File registrati che non andrebbero eliminati`,
  'Window.Orphaned.Title': `File non necessari, sicuri da eliminare`,
  'Section.Registered.Products': `PRODOTTI`,
  'Section.Registered.Patches': `PATCH`,
  'Section.Registered.Details': `DETTAGLI PRODOTTO`,
  'Section.Move.Location': `DESTINAZIONE SPOSTAMENTO`,
  'Section.SayThanks': `PER RINGRAZIARMI`,
  'Field.Reason': `Motivo`,
  'Field.Author': `Autore`,
  'Field.Application': `Applicazione`,
  'Field.Title': `Titolo`,
  'Field.Subject': `Oggetto`,
  'Field.Keywords': `Parole chiave`,
  'Field.SigningCertificate': `Certificato firma`,
  'Field.FileSize': `Dimensione file`,
  'Field.Comment': `Commento`,
  'Field.ProductName': `Nome prodotto`,
  'Field.File': `File`,
  'Field.Size': `Dimensione`,
  'Field.Patches': `Patch`,
  'Field.UnknownProductName': `(sconosciuto)`,
  'Field.PatchesOnly': `(solo patch)`,
  'Field.Missing': `mancante`,
  'Action.About': `_Informazioni`,
  'Action.Copy': `Copia`,
  'Action.Cut': `Taglia`,
  'Action.Paste': `Incolla`,
  'Action.SelectAll': `Seleziona tutto`,
  'Action.Browse': `S_foglia...`,
  'Action.Cancel': `_Annulla`,
  'Action.CheckForUpdates': `Controlla _aggiornamenti`,
  'Action.Close': `_Chiudi`,
  'Action.Delete': `_Elimina`,
  'Action.DeletePermanently': `_Elimina definitivamente`,
  'Action.Done': `_Fatto`,
  'Action.Details': `Dettagli`,
  'Action.BuyMeACuppa': `_Offrimi un caffè`,
  'Action.LeaveStarOnGitHub': `Lascia una _stella su GitHub`,
  'Action.Licence': `Licenza Apache 2.0`,
  'Action.Move': `_Sposta`,
  'Action.MoveInstead': `_Sposta invece`,
  'Action.MoveDestinationPlaceholder': `Percorso cartella, se scegli 'Sposta' anziché 'Elimina'`,
  'Action.OpenReleasePage': `Apri pagina _release`,
  'Action.Rescan': `_Ripeti scansione`,
  'Action.ScanAgain': `_Nuova scansione`,
  'Action.SendResultLog': `Invia rapporto`,
  'Action.SendResultLogConfirm': `_Invia`,
  'Automation.BuyMeACuppa': `Dona`,
  'Automation.BuyMeACuppa.About': `Offrimi un caffè`,
  'Automation.CancelOperation': `Annulla operazione`,
  'Automation.CancelScan': `Annulla scansione`,
  'Automation.CancelStartupScan': `Annulla scansione all'avvio`,
  'Automation.Close': `Chiudi`,
  'Automation.CloseWindow': `Chiudi finestra`,
  'Automation.CloseResult': `Chiudi finestra risultato e torna alla finestra principale`,
  'Automation.LeaveStarOnGitHub.About': `Lascia una stella su github`,
  'Automation.Minimise': `Riduci a icona`,
  'Automation.ConfirmDelete': `Elimina sposta i file non necessari nel Cestino. Annulla chiude senza eliminare.`,
  'Automation.ConfirmMove': `'Sposta' colloca i file non necessari nella cartella destinazione scelta. 'Annulla' li lascia dove sono.`,
  'Automation.RecycleUnavailable': `Scegli come gestire i file non necessari: spostarli in un luogo sicuro, eliminarli definitivamente o annullare.`,
  'Automation.RecycleUnavailableMove': `Sposta i file non necessari in una cartella a tua scelta`,
  'Automation.RecycleUnavailableDeletePermanently': `Elimina definitivamente i file non necessari perché per questa unità il Cestino non è disponibile`,
  'Automation.SayThanks': `Per ringraziarmi`,
  'Automation.ConfirmSendResultLog': `'Invia' trasmette a No Faff il rapporto mostrato. Annulla non invia nulla.`,
  'Automation.CheckForUpdates': `Controlla aggiornamenti`,
  'Automation.CheckForUpdates.HelpText': `Verifica sulla pagina release di github se esiste una versione più recente.`,
  'Automation.UpdateAvailable.HelpText': `Apri pagina release per scaricare la versione più recente, o scegli 'Annulla' per mantenere quella attuale.`,
  'Automation.Licence.HelpText': `Apre file licenza in github.com nel browser.`,
  'Automation.Section.MoveLocation': `Destinazione spostamento`,
  'Automation.Section.Products': `Prodotti`,
  'Automation.Section.Patches': `Patch`,
  'Automation.Section.ProductDetails': `Dettagli prodotto`,
  'Automation.MoveDestinationFolder': `Destinazione spostamento`,
  'Automation.OperationProgress': `Avanzamento operazione`,
  'Automation.RescanInstaller': `Nuova scansione di {InstallerFolder}`,
  'Automation.ScanningProgress': `Avanzamento scansione`,
  'Automation.StartupScanProgress': `Avanzamento scansione all'avvio`,
  'Automation.ViewOrphanedFiles': `Dettagli, file non necessari`,
  'Automation.ViewOrphanedFiles.HelpText': `Disponibili per la pulizia.`,
  'Automation.ViewRegisteredFiles': `Dettagli, file registrati`,
  'Automation.ViewRegisteredFiles.HelpText': `Elenco sola lettura.`,
  'Automation.SortStatus.Ascending': `Ordinati per {0}, crescente`,
  'Automation.SortStatus.Descending': `Ordinati per {0}, decrescente`,
  'Automation.Scroll.ScanResults': `Risultati scansione`,
  'Automation.Scroll.ResultDetails': `Dettagli risultato`,
  'Automation.Scroll.FileDetails': `Dettagli file`,
  'Automation.Scroll.DialogBody': `Testo finestra di dialogo`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `File che non è stato possibile elaborare`,
  'Automation.RegisteredMissingSeeAlso': `Spiega questa cartella, e come recuperare un file, nel README`,
  'Tooltip.BuyMeACuppa.About': `Ci vuole un caffè!`,
  'Tooltip.CancellingPending': `Annullamento richiesto. InstallerClean sta aspettando che il passaggio in corso arrivi a un punto in cui fermarsi. Può richiedere qualche secondo durante operazioni di I/O intense o una chiamata al database MSI.`,
  'Tooltip.Close': `Chiudi`,
  'Tooltip.LeaveStarOnGitHub.About': `Una stella aiuta altre persone a trovare InstallerClean.`,
  'Tooltip.Minimise': `Riduci a icona`,
  'Tooltip.SendResultLog': `Come preferisci, ma è apprezzato. Invia un riepilogo anonimo che mi fa solo sapere se funziona e quanto spazio le persone stanno liberando. La schermata successiva visualizza cosa verrà inviato prima di confermare.`,
  'Tooltip.SendResultLog.NothingFound': `Come preferisci, ma è apprezzato. Invia un riepilogo anonimo che mi fa solo sapere se funziona. La schermata successiva ti mostra cosa verrà inviato prima di confermare.`,
  'Tooltip.Move': `Sposta i file non necessari nella destinazione spostamento.`,
  'Tooltip.MoveNeedsDestination': `Sposta i file non necessari in un luogo sicuro. Sceglierai la cartella subito dopo.`,
  'Tooltip.Delete': `Sposta i file non necessari nel Cestino.`,
  'Tooltip.SigningCertificate': `Nome soggetto dal certificato Authenticode incorporato. Catena non verificata.`,
  'Body.MainExplanation.Lead': `Eventuali file non necessari qui sotto si possono eliminare senza rischi.`,
  'Body.MainExplanation.Why': `Si trovano in {InstallerFolder}, lasciati indietro quando un programma è stato disinstallato ({0}), una patch più recente ne ha sostituito uno ({1}) o l'editore lo ha ritirato ({2}). InstallerClean elenca sempre e solo i file che Windows stesso segnala come non più necessari.`,
  'Body.MainExplanation.Action': `Eliminali nel Cestino, oppure usa invece Sposta per tenerne una copia di backup. Rimettere i file in {InstallerFolder} riporta tutto esattamente com'era.`,
  'Body.PendingReboot.MsiExecuteMutex': `In questo momento qualcosa sta usando Windows Installer, di solito un aggiornamento di Windows o un programma che si sta installando in background. Mentre questo avviene 'Sposta' ed 'Elimina' sono in pausa, così InstallerClean non tocca la cache di installazione quando ci sono modifiche in corso. Ad attività completate ripeti la scansione e torneranno disponibili.`,
  'Body.PendingReboot.InstallerInProgress': `In questo computer è in sospeso una precedente transazione di Windows Installer. Prima di pulire la cache riprendi o annulla quell'installazione (o riavvia Windows).`,
  'Body.PendingReboot.PendingRenameInCache': `Windows ha in coda, per il prossimo riavvio, la ridenominazione di un file che riguarda la cache di installazione. Prima di pulire riavvia Windows.`,
  'Body.NoFileSelected': `Seleziona un file per vederne i dettagli.`,
  'Body.NoProductSelected': `Seleziona un prodotto per vederne i dettagli.`,
  'Body.NoMetadata': `Nessun metadato disponibile.`,
  'Body.RegisteredMissingFromDisk': `Questo file di installazione è stato eliminato. Non è stato InstallerClean: non rimuove mai un file che un programma usa ancora; qualcos'altro lo ha eliminato prima che tu eseguissi InstallerClean.&#10;&#10;Per ora non crea problemi, e non ne creerà fino al giorno in cui proverai a riparare, aggiornare o disinstallare il programma a cui appartiene. Quel passaggio può allora non riuscire, perché Windows cerca questo file e non lo trova.&#10;&#10;Per provare a risolvere, scarica il programma di installazione di quel software dal suo produttore ed eseguilo sopra la copia esistente (non disinstallare prima: la disinstallazione è essa stessa un passaggio che richiede questo file). Usa la stessa versione che hai installato, se riesci a procurartela, perché Windows potrebbe rifiutarne una diversa. Di solito questo ripristina il file, e le tue impostazioni di norma restano intatte, ma Microsoft non lo garantisce: la sua ultima risorsa è reinstallare il programma, o Windows stesso.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `Il file README [spiega questa cartella], e come recuperare un file, con i termini di Microsoft.`,
  'Body.NoPatches': `(nessuna)`,
  'Reason.Orphaned': `Orfano`,
  'Reason.Superseded': `Sostituito`,
  'Reason.Obsoleted': `Obsoleto`,
  'Status.Scanning': `Scansione...`,
  'Status.Cancelling': `Annullamento...`,
  'Status.StartingScan': `Avvio scansione...`,
  'Status.QueryingApi': `Richiesta a Windows elenco software installati...`,
  'Status.ScanningCache': `Scansione cartella cache di installazione...`,
  'Status.EnumeratingProducts': `Enumerazione prodotti installati...`,
  'Status.CheckingRegistry': `Controllo registro per altri pacchetti...`,
  'Status.RegisteredPackagesFound': `Trovati {0} {1} registrati.`,
  'Status.ScanComplete': `Scansione completata ({0})`,
  'Status.FoundProducts': `Scansione pacchetti locali...`,
  'Status.FoundUnused': `{0} {1} che puoi eliminare in sicurezza.`,
  'Status.PreparingDestination': `Preparazione cartella destinazione...`,
  'Status.Moving': `Spostamento di {0} {1}...`,
  'Status.Deleting': `Eliminazione di {0} {1}...`,
  'Status.MoveCancelled.Partial': `Spostamento annullato dopo {0} di {1} {2}.`,
  'Status.DeleteCancelled.Partial': `Eliminazione annullata dopo {0} di {1} {2}.`,
  'Status.MoveFailed': `Spostamento non riuscito ({0}). Dettagli in {1}.`,
  'Status.MoveFailed.NoLog': `Spostamento non riuscito ({0}). Non è stato possibile scrivere il file crash.log.`,
  'Status.DeleteFailed': `Eliminazione non riuscita ({0}). Dettagli in {1}.`,
  'Status.DeleteFailed.NoLog': `Eliminazione non riuscita ({0}). Non è stato possibile scrivere il file crash.log.`,
  'Status.ScanAccessDenied': `Accesso negato. Windows ha rifiutato la scansione.`,
  'Status.ScanFailedDb': `Scansione non riuscita: impossibile leggere i record di Windows Installer.`,
  'Status.ScanCancelled': `Scansione annullata.`,
  'Status.Done': `Pronto`,
  'Status.ScanFailedDetails': `Scansione non riuscita ({0}). Dettagli in {1}.`,
  'Status.ScanFailedDetails.NoLog': `Scansione non riuscita ({0}). Non è stato possibile scrivere il file crash.log.`,
  'Completion.AllClean': `Tutto pulito`,
  'Completion.NothingToCleanUp': `Niente da eliminare in {InstallerFolder}`,
  'Completion.NothingToCleanUpReceipt': `Analizzati {0} {1} in {2}`,
  'Completion.MoveRestoreHint': `Riportali in {InstallerFolder} se mai qualcosa smettesse di funzionare ([estremamente improbabile]).`,
  'Completion.DeleteRestoreHint': `Fino ad allora, puoi ripristinarli se mai qualcosa smettesse di funzionare ([estremamente improbabile]).`,
  'Completion.Freed': `Liberati {0}`,
  'Completion.Moved': `Spostati {0}`,
  // Heading for the outcome where the operation acted on no file at all, then
  // the failure count line beneath it: 0 = files that failed, 1 = files tried
  // (succeeded + failed). The count selecting the form is {0}; the noun belongs
  // to {1}, so treat the two independently.
  'Completion.NothingMoved': `Nessun file spostato`,
  'Completion.NothingDeleted': `Nessun file eliminato`,
  'Completion.FailedCount.Singular': `{0} di {1} file non è stato spostato.`,
  'Completion.FailedCount.Plural': `{0} di {1} file non sono stati spostati.`,
  'Completion.FailedCountDelete.Singular': `{0} di {1} file non è stato eliminato.`,
  'Completion.FailedCountDelete.Plural': `{0} di {1} file non sono stati eliminati.`,
  'Completion.MoveSummary.Singular': `Spostato {0} {1} in: {2}`,
  'Completion.MoveSummary.Plural': `Spostati {0} {1} in: {2}`,
  'Completion.DeleteSummary.Singular': `Spostato {0} {1} nel Cestino`,
  'Completion.DeleteSummary.Plural': `Spostati {0} {1} nel Cestino`,
  'Completion.PermanentDeleteSummary.Singular': `Eliminato definitivamente {0} {1}. Non è stato spostato nel Cestino.`,
  'Completion.PermanentDeleteSummary.Plural': `Eliminati definitivamente {0} {1}. Non sono finiti nel Cestino.`,
  'Completion.PermanentDeleteRestoreHint.Singular': `Va bene così, si poteva rimuovere senza rischi. InstallerClean elimina solo i file che Windows segnala come non più in uso, mai uno che un programma usa ancora. Nell'improbabile caso in cui un'eliminazione lasciasse un programma incapace di ripararsi, aggiornarsi o disinstallarsi, reinstallarlo dal produttore di solito ripristina il file, anche se Microsoft non lo garantisce.`,
  'Completion.PermanentDeleteRestoreHint.Plural': `Va bene così, si potevano rimuovere senza rischi. InstallerClean elimina solo i file che Windows segnala come non più in uso, mai uno che un programma usa ancora. Nell'improbabile caso in cui un'eliminazione lasciasse un programma incapace di ripararsi, aggiornarsi o disinstallarsi, reinstallarlo dal produttore di solito ripristina il file, anche se Microsoft non lo garantisce.`,
  'RecycleUnavailable.Heading': `Per questa unità il Cestino non è disponibile`,
  'RecycleUnavailable.Body.Singular': `Quindi questo {1} ({2}) non è stato eliminato. Puoi spostarlo in un luogo sicuro, oppure eliminarlo definitivamente.`,
  'RecycleUnavailable.Body.Plural': `Quindi questi {0} {1} ({2}) non sono stati eliminati. Puoi spostarli in un luogo sicuro, oppure eliminarli definitivamente.`,
  'RecycleUnavailable.Reassurance.Singular': `Eliminarlo è sicuro. InstallerClean elimina solo i file che Windows segnala come non più in uso, mai uno che un programma usa ancora, e il Cestino è soltanto una garanzia in più. Nell'improbabile caso in cui un'eliminazione lasciasse un programma incapace di ripararsi, aggiornarsi o disinstallarsi, reinstallarlo dal produttore di solito ripristina il file, anche se Microsoft non lo garantisce.`,
  'RecycleUnavailable.Reassurance.Plural': `Eliminarli è sicuro. InstallerClean elimina solo i file che Windows segnala come non più in uso, mai uno che un programma usa ancora, e il Cestino è soltanto una garanzia in più. Nell'improbabile caso in cui un'eliminazione lasciasse un programma incapace di ripararsi, aggiornarsi o disinstallarsi, reinstallarlo dal produttore di solito ripristina il file, anche se Microsoft non lo garantisce.`,
  'Summary.RegisteredStillUsed.Singular': `{0} file ancora necessario`,
  'Summary.RegisteredStillUsed.Plural': `{0} file ancora necessari`,
  'Summary.OrphanedToCleanUp.Singular': `{0} file non necessario da eliminare`,
  'Summary.OrphanedToCleanUp.Plural': `{0} file non necessari da eliminare`,
  'Summary.MissingFromDisk.Singular': `{0} file registrato risulta mancante (non eliminato da InstallerClean). Per ora nessun problema, ma in futuro una riparazione, un aggiornamento o una disinstallazione di quel programma potrebbe non riuscire. Per sapere cosa fare apri 'Dettagli'.`,
  'Summary.MissingFromDisk.Plural': `{0} file registrati risultano mancanti (non eliminati da InstallerClean). Per ora nessun problema, ma in futuro una riparazione, un aggiornamento o una disinstallazione di quei programmi potrebbe non riuscire. Per sapere cosa fare apri 'Dettagli'.`,
  'Summary.OperationFiles': `{0} di {1} {2}`,
  'Summary.OrphanedWindow': `{0} orfani, {1} sostituiti, {2} obsoleti ({3})`,
  'Summary.RegisteredWindow.Singular': `{0} file registrato ancora necessario ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} file registrati ancora necessari ({1})`,
  'Confirm.MoveTitle': `Vuoi spostare {0} {1} ({2})?`,
  'Confirm.MoveDestination': `I file verranno spostati in:`,
  'Confirm.DeleteTitle': `Vuoi eliminare {0} {1} ({2})?`,
  'Confirm.DeleteToRecycleBin': `I file verranno spostati nel Cestino. Se vuoi fare delle copie di backup, usa invece il pulsante Sposta.`,
  'Error.AdminRequiredTitle': `Accesso negato`,
  'Error.AdminRequiredBody': `Windows ha negato l'accesso a InstallerClean, che si è quindi fermato. Non è stato rimosso nulla.\n\nInstallerClean era già in esecuzione come amministratore, quindi riavviarlo in quel modo non serve. Windows non dice altro su che cosa abbia negato l'accesso, quindi non c'è nulla di specifico da provare.`,
  'Error.InstallerDbUnavailableTitle': `Impossibile leggere i record di Windows Installer`,
  'Error.ScanFailedTitle': `Scansione non riuscita`,
  'Error.InstallerDbEmpty': `I record di Windows Installer sono tornati completamente vuoti: nemmeno un programma installato o un aggiornamento rivendica un file di installazione nella cache. Su una macchina funzionante questo non succede (perfino un'installazione di Windows appena fatta ne ha qualcuno), quindi o i record sono danneggiati o non è stato possibile leggerli, e una scansione che credesse a questa risposta classificherebbe erroneamente come orfano ogni file in {InstallerFolder}. InstallerClean si è fermato invece di farlo. Non è stato rimosso nulla.`,
  'Error.MsiAccessDenied': `Windows Installer ha impedito a InstallerClean di elencare ciò che è installato. InstallerClean era già in esecuzione come amministratore, quindi eseguirlo di nuovo come amministratore non cambia nulla. Senza quell'elenco non c'è modo sicuro di sapere quali file nella cache servono ancora, quindi InstallerClean si è fermato. Non è stato rimosso nulla.`,
  'Error.MsiNonSuccess': `Windows Installer non è riuscito a fornire a InstallerClean un elenco leggibile dei programmi installati: {0} voci di seguito sono tornate illeggibili (ultimo codice di errore {1}). Invece di lavorare su un elenco letto solo in parte, InstallerClean si è fermato. Non è stato rimosso nulla.`,
  'Error.InvalidDestinationTitle': `Destinazione non valida`,
  'Error.DestinationWriteFailedTitle': `Impossibile scrivere nella destinazione`,
  'Error.MoveFailedTitle': `Spostamento non riuscito`,
  'Error.DeleteFailedTitle': `Eliminazione non riuscita`,
  'Error.SettingNotSavedTitle': `Impostazione non salvata`,
  'Error.SettingNotSavedBody': `Impossibile salvare la modifica. Al prossimo avvio InstallerClean tornerà all'impostazione precedente.`,
  'Error.DestinationInsideInstaller': `La destinazione non può trovarsi all'interno della cartella di Windows Installer.`,
  'Error.DestinationInSystemFolder': `La destinazione {0} si trova in una cartella di sistema di Windows. Scegli un percorso al di fuori di %SystemRoot%, %ProgramFiles% e %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Spazio insufficiente`,
  'Error.NotEnoughSpaceBody': `Spazio insufficiente in {0}\n\nNecessario: {1}\nDisponibile: {2}`,
  'Error.AccessDeniedDestination': `Non hai i permessi per scrivere in {0}.\nProva una cartella nel profilo utente o in un'unità di tua proprietà.`,
  'Error.PathTooLong': `Il percorso {0} è troppo lungo per Windows. Scegli un percorso più corto.`,
  'Error.DestinationMissing': `La cartella {0} non esiste e non è stato possibile crearla. Controlla la lettera dell'unità o il percorso di rete.`,
  'Error.IOWriteDestination': `Windows non può scrivere in {0}.\nDettagli in {1}.`,
  'Error.IOWriteDestination.NoLog': `Windows non può scrivere in {0}. Non è stato possibile scrivere il file crash.log.`,
  'Error.WriteDestination': `Impossibile scrivere in {0}.\nDettagli in {1}.`,
  'Error.WriteDestination.NoLog': `Impossibile scrivere in {0}. Non è stato possibile scrivere il file crash.log.`,
  'Error.MissingSourceFile': `Il file non esiste più.`,
  'Error.SourceIsReparsePoint': `Il file sorgente è un collegamento simbolico o una giunzione; rifiutato per sicurezza.`,
  // Singular = the sentence for ONE file, which the CLI prints after "filename: ";
  // plural = the heading the completion overlay puts over a list of filenames.
  'Error.AccessDenied.Singular': `Windows ha negato l'accesso a questo file; è stato lasciato al suo posto.`,
  'Error.AccessDenied.Plural': `Windows ha negato l'accesso a questi file; sono stati lasciati al loro posto.`,
  'Error.FileInUse.Singular': `Questo file è aperto o bloccato da un altro programma, quindi al momento nulla può spostarlo. È stato lasciato al suo posto; riprova più tardi.`,
  'Error.FileInUse.Plural': `Questi file sono aperti o bloccati da un altro programma, quindi al momento nulla può spostarli. Sono stati lasciati al loro posto; riprova più tardi.`,
  'Error.IOFailure.Singular': `Windows ha segnalato un errore sul file; il file è stato lasciato al suo posto.`,
  'Error.IOFailure.Plural': `Windows ha segnalato errori sui file; questi file sono stati lasciati al loro posto.`,
  'Error.UnknownError.Singular': `Qualcosa è andato storto con questo file; è stato lasciato al suo posto.`,
  'Error.UnknownError.Plural': `Qualcosa è andato storto con questi file; sono stati lasciati al loro posto.`,
  'Error.ShellRecycleFailed': `Impossibile spostare questo file nel Cestino (errore {0}), e da quel codice InstallerClean non può dirti perché. Il file è stato lasciato al suo posto. Prova invece il pulsante Sposta, che non usa il Cestino.`,
  'Error.RecycleAccessDenied': `Windows ha negato l'accesso anche con i diritti di amministratore (errore {0}), e InstallerClean non può stabilire se il problema sia il file o il Cestino. Il file è stato lasciato al suo posto. Il pulsante Sposta funziona se il problema è il Cestino, ma non se è il file.`,
  'Error.RecycleInUse': `Questo file è aperto o bloccato da un altro programma (errore {0}), quindi al momento nulla può rimuoverlo. È stato lasciato al suo posto; riprova più tardi.`,
  'Error.DeletedNotRecycled': `Windows ha eliminato definitivamente questo file invece di spostarlo nel Cestino. InstallerClean aveva chiesto il Cestino, e Windows ha fatto altrimenti. Il file non c'è più.`,
  'Error.MoveIntoInstaller': `Spostamento dei file nella cartella di Windows Installer rifiutato (destinazione: {0}).`,
  'Error.DestinationNotFullyQualified': `La destinazione dello spostamento deve essere un percorso completo verso una cartella, che inizia con una lettera di unità o una condivisione di rete (per esempio D:\\Backup oppure \\\\server\\backup). InstallerClean non può usare questo: {0}`,
  'BrowserLaunch.FailedTitle': `Impossibile aprire il browser`,
  'UpdateCheck.Title': `Controlla aggiornamenti`,
  'UpdateCheck.Status.Checking': `Controllo aggiornamenti...`,
  'UpdateCheck.Status.UpToDate': `Versione aggiornata.`,
  'UpdateCheck.UpdateAvailable.Title': `Aggiornamento disponibile`,
  'UpdateCheck.UpdateAvailable.Body': `La versione in uso è la versione {0}.&#10;È disponibile la versione {1}.`,
  'UpdateCheck.Failed.NetworkUnavailable': `Impossibile raggiungere GitHub. Controlla la connessione internet e riprova.`,
  'UpdateCheck.Failed.ServerError': `GitHub ha restituito una risposta di errore. Riprova tra qualche minuto.`,
  'UpdateCheck.Failed.ResponseParseError': `La risposta di GitHub non conteneva una release riconoscibile. Riprova più tardi, o apri direttamente la pagina delle release.`,
  'UpdateCheck.Failed.Timeout': `Il controllo è scaduto. La connessione a GitHub potrebbe essere lenta; riprova.`,
  'UpdateCheck.Failed.Unknown': `Il controllo non è riuscito per un motivo sconosciuto. I dettagli sono nel file crash.log, se devi segnalarlo.`,
  'BrowserLaunch.ClipboardOk': `InstallerClean non è riuscito ad aprire il browser. Il link è negli appunti, così puoi incollarlo da te:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean non è riuscito ad aprire il browser e non è riuscito nemmeno a copiare il link negli appunti. Il link è:&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `La destinazione dello spostamento è cambiata mentre i file venivano spostati (qualcosa ha sostituito o reindirizzato la cartella), quindi InstallerClean si è fermato invece di scrivere nel posto sbagliato. Controlla {0}, poi ripeti la scansione e riprova.`,
  'Error.CannotWriteFolder': `Impossibile scrivere in {0}.`,
  'Error.NoUniqueFilename': `Dopo 10.000 tentativi impossibile trovare un nome file univoco per '{0}'.`,
  'ResultLog.Sending': `Invio...`,
  'ResultLog.Sent': `Grazie! Rapporto inviato.`,
  'ResultLog.Failed': `Invio non riuscito. Riprova più tardi.`,
  'ResultLog.NothingToSend': `Nessun rapporto da inviare.`,
  'ConfirmSendResultLog.Title': `Vuoi inviare questo?`,
  'ConfirmSendResultLog.Reassurance': `Viene inviato a nofaff.netlify.app/api/result-log. Niente ti identifica, né identifica il computer; mi fa solo sapere che InstallerClean funziona e [quanto spazio le persone stanno liberando].`,
  'Automation.ResultLogPreview': `Anteprima rapporto`,
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean è già in esecuzione.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `Si è verificato un errore imprevisto e InstallerClean verrà chiuso.\n\n{0}\n\nDettagli salvati in:\n{1}`,
  'Startup.UnhandledBody.NoLog': `Si è verificato un errore imprevisto e InstallerClean verrà chiuso.\n\n{0}\n\nNon è stato possibile scrivere il file crash.log.`,
  'Startup.ErrorTitle': `Errore in avvio`,
  'Startup.FailedToStart': `Avvio non riuscito ({0}). Dettagli salvati in:\n{1}`,
  'Startup.FailedToStart.NoLog': `Avvio non riuscito ({0}). Non è stato possibile scrivere il file crash.log.`,
  'FilePicker.ChooseDestinationTitle': `Scegli la cartella destinazione per i file spostati`,
  'Version.Display': `Versione {0}`,
  'Plural.File.Singular': `file`,
  'Plural.File.Plural': `file`,
  'Plural.Error.Singular': `errore`,
  'Plural.Error.Plural': `errori`,
  'Plural.Package.Singular': `pacchetto`,
  'Plural.Package.Plural': `pacchetti`,
  'Plural.Product.Singular': `prodotto`,
  'Plural.Product.Plural': `prodotti`,
  'Plural.Patch.Singular': `patch`,
  'Plural.Patch.Plural': `patch`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ElapsedLong.LessThanASecond': `meno di un secondo`,
  'Display.ElapsedLong.Seconds': `{0:F1} secondi`,
  'Cli.UnknownArgument': `Argomento sconosciuto: '{0}'`,
  'Cli.Cancelling': `Annullamento...`,
  'Cli.Cancelled': `Operazione annullata.`,
  'Cli.GenericError': `Errore: {0}. Dettagli salvati in {1}.`,
  'Cli.GenericError.NoLog': `Errore: {0}. Non è stato possibile scrivere il file crash.log.`,
  'Cli.ScanningInstaller': `Scansione di {InstallerFolder}...`,
  'Cli.FoundOrphans': `Trovati {0} {1} da eliminare ({2}).`,
  'Cli.NothingToDo': `Nessuna operazione necessaria.`,
  'Cli.DeletingFiles': `Eliminazione di {0} {1}...`,
  'Cli.DeletedFiles': `Eliminati {0} {1}.`,
  'Cli.RecycleUnavailable': `Errore: per questa unità il Cestino non è disponibile, quindi non è stato eliminato nulla. Usa /m per spostare i file oppure riattiva il Cestino e riprova.`,
  'Cli.NoMoveDestination': `Errore: nessuna destinazione di spostamento specificata. Usa /m PERCORSO. (Una destinazione predefinita impostata nella GUI è specifica per utente e non si applica alle esecuzioni pianificate o con account di servizio.)`,
  'Cli.MoveDestinationInsideInstaller': `Errore: la destinazione non può trovarsi all'interno della cartella di Windows Installer.`,
  'Cli.MoveDestinationRelative': `Errore: la destinazione deve essere un percorso completo. Ricevuto: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Errore: la destinazione {0} si trova in una cartella di sistema di Windows. Scegli un percorso al di fuori di %SystemRoot%, %ProgramFiles% e %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Errore: in questo momento qualcosa sta usando Windows Installer, di solito un aggiornamento di Windows o un programma che si sta installando in background. Lo spostamento e l'eliminazione sono bloccati mentre questo avviene. Riprova una volta terminato.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Errore: in questo computer è in sospeso una precedente transazione di Windows Installer. Prima di pulire la cache riprendi o annulla quell'installazione (o riavvia Windows).`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Errore: un'operazione su file in coda per il prossimo riavvio riguarda la cache di installazione ({0}). Per completare l'operazione prima della pulizia riavvia Windows.`,
  'Cli.MovingFiles': `Spostamento di {0} {1} in {2}...`,
  'Cli.MovedFiles': `Spostati {0} {1}.`,
  'Cli.MutexBlocked': `Un altro processo InstallerClean mantiene il blocco a istanza singola (la GUI o un'altra esecuzione della CLI). Codice di uscita 75 (transitorio); è sicuro riprovare più tardi.`,
  'Cli.EventLogUnavailable': `Nota: scrittura nel registro eventi non riuscita. Controlla i permessi del registro Applicazione o i Criteri di gruppo.`,
  'CrashLog.PrivacyHeader': `# crash.log registra le eccezioni non gestite di InstallerClean.\n# In esecuzione con privilegi elevati, i messaggi di eccezione del\n# framework possono includere percorsi di file della sessione in\n# corso (compresi i profili di altri utenti enumerati dalle query\n# di Windows Installer). I messaggi di errore di rete del controllo\n# aggiornamenti o dell'invio del log dei risultati possono\n# includere l'URL di destinazione e l'indirizzo IP / proxy\n# risolto. Rimuovi entrambi i tipi di dettaglio prima di allegare\n# questo file a una segnalazione di bug pubblica.\n`,
  'Cli.Help.Header': `InstallerClean - pulizia di {InstallerFolder}`,
  'Cli.Help.Usage': `Utilizzo:`,
  'Cli.Help.Help': `  installerclean-cli --help       Mostra questa guida (anche /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version    Mostra la versione (anche -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s           Solo scansione - file non necessari`,
  'Cli.Help.Delete': `  installerclean-cli /d           Elimina i file non necessari (Cestino)`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m           Sposta nella destinazione salvata`,
  'Cli.Help.MovePath': `  installerclean-cli /m PERCORSO  Sposta nel percorso specificato`,
  'Cli.Help.NoteLine1': `installerclean-cli è un vero processo console e blocca il prompt`,
  'Cli.Help.NoteLine2': `finché non termina; reindirizza o usa una pipe sul suo output come`,
  'Cli.Help.NoteLine3': `per qualsiasi altro eseguibile console. La GUI è in InstallerClean.exe.`,
  'Cli.Help.ExitCodesHeader': `Codici di uscita:`,
  'Cli.Help.ExitCodeOk': `  0   successo: ogni file segnalato è stato elaborato`,
  'Cli.Help.ExitCodeError': `  1   errore: nessuna elaborazione (argomenti, scansione o file)`,
  'Cli.Help.ExitCodePartial': `  2   parziale: alcuni file elaborati, altri non riusciti`,
  'Cli.Help.ExitCodeTransient': `  75  transitorio: qualcosa ha bloccato l'esecuzione (vedi il messaggio)`,
  'Cli.Help.ExitCodeCancelled': `  130 annullato (Ctrl+C)`,
  'Tooltip.ChangeLanguage': `Cambia lingua. Il programma verrà riavviato.`,
  'Automation.ChangeLanguage': `Cambia lingua`,
  'Automation.ChangeLanguage.HelpText': `Il programma verrà riavviato.`,
  'Completion.CleanedUp': `Ripuliti {0}`,
  'Completion.DeleteSpaceHint': `Svuotalo per recuperare davvero lo spazio.`,
  'Body.NotScanned.Lead': `Ancora nessuna scansione.`,
  'Body.NotScanned.Why': `Premi Ripeti scansione per cercare in {InstallerFolder} i file di installazione che nessun programma usa più.`,
  'Confirm.MoveSameDrive': `Questa cartella è sulla stessa unità, quindi lo spostamento da solo non libera spazio. Recupererai lo spazio quando eliminerai i file da lì, oppure puoi scegliere una cartella su un'altra unità.`,
  'Error.ScanCorrelationFailed': `InstallerClean non è riuscito a far quadrare questa scansione con i record di Windows Installer: ogni file che Windows elenca ancora come necessario manca da {InstallerFolder}, mentre i file effettivamente presenti nella cartella non corrispondono a nessun record. Nessuna macchina reale si presenta così, quindi questo indica un problema nella lettura dei record, non file che puoi rimuovere senza rischi. Non è stato proposto nulla da eliminare e non è stato rimosso nulla.`,
  'Error.CandidateOutsideCache': `Questo file non si trova direttamente all'interno della cartella di Windows Installer; rifiutato per sicurezza.`,
  'Completion.ReverifySkipped': `{0} {1} lasciati al loro posto, perché un programma è tornato ad averne bisogno dopo la scansione.`,
  'Completion.MoveCancelledSummary': `Spostati {0} di {1} {2} prima dell'annullamento.`,
  'Completion.DeleteCancelledSummary': `Spostati {0} di {1} {2} nel Cestino prima dell'annullamento.`,
  'Completion.PermanentDeleteCancelledSummary': `Eliminati definitivamente {0} di {1} {2} prima dell'annullamento.`,
  'Body.PendingReboot.Lead': `Al momento questi file non si possono ripulire.`,
  'Cli.TooManyArguments': `Errore: argomento aggiuntivo imprevisto '{0}'. Se la cartella di destinazione contiene uno spazio, racchiudi l'intero percorso tra virgolette: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `La destinazione salvata è per utente; SYSTEM o pianificato: /m PERCORSO.`,
  'Completion.ReverifyIncomplete': `{0} {1} lasciati al loro posto, perché non è stato possibile leggere completamente i record di Windows Installer quando il controllo è stato ripetuto.`,
  'Summary.ProgramsUnreadable.Singular': `Non è stato possibile leggere {0} programma installato durante questa scansione, quindi le patch sostituite sono state mantenute. I file orfani non sono interessati.`,
  'Summary.ProgramsUnreadable.Plural': `Non è stato possibile leggere {0} programmi installati durante questa scansione, quindi le patch sostituite sono state mantenute. I file orfani non sono interessati.`,
  'Error.ScanRecordsUnreadable': `InstallerClean non è riuscito a leggere abbastanza dei record di Windows Installer per essere sicuro di che cosa serva ancora: l'elenco dei programmi installati è tornato incompleto, e leggere gli stessi record direttamente dal registro di sistema ha dato errori a sua volta. Un file potrebbe sembrare orfano solo perché il record che lo nomina era fra quelli illeggibili, quindi InstallerClean si è fermato. Non è stato rimosso nulla.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer non ha mai segnalato la fine dell'elenco dei programmi installati: InstallerClean ha rinunciato dopo {0} voci (ultimo codice di errore {1}). Di un elenco senza fine non ci si può fidare, quindi InstallerClean si è fermato. Non è stato rimosso nulla.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer non ha mai segnalato la fine dell'elenco delle patch di un programma: InstallerClean ha rinunciato dopo {0} voci (ultimo codice di errore {1}). Di un elenco senza fine non ci si può fidare, quindi InstallerClean si è fermato. Non è stato rimosso nulla.`,
  'Status.CheckingRecycleBin': `Controllo del Cestino...`,
  'UpdateCheck.Status.UpdateAvailable': `È disponibile la versione {0}.`,
  'Completion.DonateAsk': `Felice di esserti stato utile. Un caffè è sempre bene accetto, se ti viene dal cuore.`,
  'About.Link.Guide': `Guida e FAQ`,
  'About.Link.ReportProblem': `Segnala un problema`,
  'About.AutoUpdateCheck': `Controlla automaticamente gli aggiornamenti`,
  'Automation.About.Guide.HelpText': `Apre readme su github nel browser.`,
  'Automation.About.ReportProblem.HelpText': `Apre elenco problemi (issue) in github.com nel browser.`,
  'Automation.AutoUpdateCheck.HelpText': `Se selezionata, all'avvio InstallerClean verifica su github se è disponibile una versione più recente.`,
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

// Append the satellite-only override <data> elements before </root>.
const overrideBlock = Object.entries(OVERRIDES)
  .map(([k, v]) => `  <data name="${k}" xml:space="preserve"><value>${v}</value></data>`)
  .join('\n');
if (overrideBlock) text = text.replace('</root>', overrideBlock + '\n</root>');

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

const overrideKeys = Object.keys(OVERRIDES);
const overrideMissing = overrideKeys.filter((k) => !output.has(k));
const overrideArityMismatch = overrideKeys.filter((k) => {
  if (!output.has(k)) return true;
  const base = k.replace(/\.(?:One|Few|Many)$/, '');
  const ref = neutral.has(`${base}.Plural`) ? `${base}.Plural` : base;
  if (!neutral.has(ref)) return true;
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
  for (const k of alsoKeep) console.log('   ' + k + ' = ' + JSON.stringify(output.get(k)));
}
if (notApplied.length) console.log('!! value not applied (regex miss):', notApplied);
if (missingFromMap.length) console.log('!! in neutral but missing from MAP:', missingFromMap);
if (strayMapKeys.length) console.log('!! in MAP but not in neutral:', strayMapKeys);
if (missingFromOutput.length) console.log('!! required key missing from output:', missingFromOutput);
if (arityMismatch.length) console.log('!! placeholder arity differs from neutral:', arityMismatch);
if (machineLeaked.length) console.log('!! machine Cli keys leaked into output:', machineLeaked);
if (overrideMissing.length) console.log('!! override key missing from output:', overrideMissing);
if (overrideArityMismatch.length) console.log('!! override arity differs from its base key:', overrideArityMismatch);
if (untranslated.length) console.log('!! still English (untranslated), ' + untranslated.length + ': ' + untranslated.slice(0, 40).join(', '));

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === 21 && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
