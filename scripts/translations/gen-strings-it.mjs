#!/usr/bin/env node
// Italian (it) satellite generator for InstallerClean.
//
// PROVENANCE: the Italian translation was a machine first cut by the project
// owner, then reviewed line by line by the native speaker bovirus (PRs #32 and #39),
// the project's only native sign-off. This generator's MAP holds bovirus's
// reviewed wording; regenerating reproduces the reviewed Strings.it.resx. Treat
// the MAP values as the native review's output, not a fresh machine draft.
//
// Structure (same as the other satellites): strips the machine-contract
// Cli.EventLog* keys (forced English at the emit site, see MachineContract.cs),
// and appends 5 satellite-only plural .One overrides (Italian counts n==1 as
// singular; the registered-count adjective, the CLI count participles and the
// re-verify-skipped summary each need their own n==1 form). See
// gen-strings-template.mjs for how the body works.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.it.resx`;

// Universal keeps: keys whose value is the same in every language, the brand names
// and the pure-placeholder announcement string. Their still-English value is NOT a
// miss. Explicit by KEY on purpose: a future brand key then defaults to "flag until
// someone adds it here", never silently passes. Do NOT translate these values. Do
// NOT edit this list per language.
//
// The four size suffixes and the two elapsed suffixes do not belong in this list,
// because they are not universal: French writes Go/Mo/Ko/o, Russian and Ukrainian
// write ГБ/МБ/КБ/Б and мс/с. Those three carry real
// values in their MAP; the languages that do abbreviate as English does keep them in
// ALSO_KEEP, which is the per-language list. Display.ListSeparator is the same shape,
// for the same reason.
const KEEP_ENGLISH = new Set([
  'Window.Main.Title',
  'Startup.AlreadyRunningTitle',
  'Startup.UnhandledTitle',
  'Automation.ScanResultAnnouncement',
]);

// Per-language keeps: Italian words byte-identical to English (genuine
// single-token matches, not misses). The self-check prints these so the keep
// stays honest.
const ALSO_KEEP = [
  'Field.File',
  'Plural.File.Singular',
  'Plural.Patch.Singular',
  // The list separator Italian uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
  // The size and elapsed unit suffixes. Italian abbreviates them exactly as
  // English does, so there is nothing to translate and nothing to get wrong.
  // A per-language keep rather than a universal one because fr, ru and uk do
  // NOT: French takes Go/Mo/Ko/o, Russian and Ukrainian take ГБ/МБ/КБ/Б and
  // мс/с, and all three carry real values in their MAP.
  'Display.Size.GB',           // {0:F2} GB
  'Display.Size.MB',           // {0:F1} MB
  'Display.Size.KB',           // {0:F1} KB
  'Display.Size.B',            // {0} B
  'Display.Elapsed.Ms',        // {0:F0}ms
  'Display.Elapsed.S',         // {0:F1}s
];

// Satellite-only CLDR plural overrides: keys absent from the neutral, appended
// before </root> and read by name at runtime (DisplayHelpers.Pluralise's One
// branch; an absent one falls back to the base).
const OVERRIDES = {
  'Status.RegisteredPackagesFound.One': `Trovato {0} {1} registrato.`,
  'Cli.FoundOrphans.One': `Trovato {0} {1} non necessario da eliminare ({2}).`,
  'Cli.DeletingFiles.One': `Eliminazione di {0} {1} non necessario...`,
  'Cli.DeletedFiles.One': `Eliminato definitivamente {0} {1} non necessario.`,
  'Cli.MovingFiles.One': `Spostamento di {0} {1} non necessario in {2}...`,
  'Cli.MovedFiles.One': `Spostato {0} {1} non necessario.`,
  // Participle and possessive agreement: "lasciato al suo posto" for one file.
  // Completion.ReverifyIdentityUnreadable.One was added and removed again in the 3.0.0 round. Its base is
  // one of the two retired identity causes: no code reads it, so nothing passes
  // the prefix to Pluralise and the override could never be selected.
  // CountedStringTests.Every_satellite_override_belongs_to_a_counted_prefix is
  // what says so. The base string itself stays translated, which is the point of
  // keeping those two keys at all.
};

const MAP = {
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Informazioni`,
  'Window.Registered.Title': `File lasciati stare`,
  'Window.Orphaned.Title': `File non necessari, sicuri da eliminare`,
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `PATCH`,
  'Section.Registered.Details': `DETTAGLI PRODOTTO`,
  'Section.Backup.Folder': `CARTELLA DI BACKUP`,
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
  'Action.DeletePermanently': `_Elimina definitivamente`,
  'Action.Done': `_Fatto`,
  'Action.Details': `Dettagli`,
  'Action.BuyMeACuppa': `_Offrimi un caffè`,
  'Action.LeaveStarOnGitHub': `Lascia una _stella su GitHub`,
  'Action.Licence': `Licenza Apache 2.0`,
  'Action.Move': `_Sposta`,
  'Action.BackupFolderPlaceholder': `Percorso della cartella se sposti anziché eliminare.`,
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
  'Automation.ConfirmDelete': `Elimina definitivamente rimuove i file non necessari. Annulla chiude senza eliminare nulla.`,
  'Automation.ConfirmMove': `'Sposta' colloca i file non necessari nella cartella destinazione scelta. 'Annulla' li lascia dove sono.`,
  'Automation.SayThanks': `Per ringraziarmi`,
  'Automation.ConfirmSendResultLog': `'Invia' trasmette a No Faff il rapporto mostrato. Annulla non invia nulla.`,
  'Automation.CheckForUpdates': `Controlla aggiornamenti`,
  'Automation.CheckForUpdates.HelpText': `Verifica sulla pagina release di github se esiste una versione più recente.`,
  'Automation.UpdateAvailable.HelpText': `Apri pagina release per scaricare la versione più recente, o scegli 'Annulla' per mantenere quella attuale.`,
  'Automation.Licence.HelpText': `Apre file licenza in github.com nel browser.`,
  'Automation.Section.BackupFolder': `Cartella di backup`,
  'Automation.Section.Patches': `Patch`,
  'Automation.Section.ProductDetails': `Dettagli prodotto`,
  'Automation.BackupFolder': `Cartella di backup`,
  'Automation.OperationProgress': `Avanzamento operazione`,
  'Automation.RescanInstaller': `Nuova scansione di {InstallerFolder}`,
  'Automation.ScanningProgress': `Avanzamento scansione`,
  'Automation.StartupScanProgress': `Avanzamento scansione all'avvio`,
  'Automation.ViewOrphanedFiles': `Dettagli, file non necessari`,
  'Automation.ViewOrphanedFiles.HelpText': `Disponibili per la pulizia.`,
  'Automation.ViewRegisteredFiles': `Dettagli, file lasciati stare`,
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
  'Tooltip.Move': `Sposta i file non necessari nella cartella di backup.`,
  'Tooltip.MoveNeedsDestination': `Sposta i file non necessari in una cartella di backup. La scegli subito dopo.`,
  'Tooltip.Delete': `Elimina definitivamente i file non necessari. Usa invece Sposta se vuoi la possibilità di convincerti che vada tutto bene.`,
  'Tooltip.SigningCertificate': `Nome soggetto dal certificato Authenticode incorporato. Catena non verificata.`,
  'Body.MainExplanation.Lead': `Tutti i file non necessari qui sotto sono [sicuri da eliminare].`,
  'Body.MainExplanation.Why': `Si trovano in {InstallerFolder}. InstallerClean interroga Windows su ogni programma installato: un file compare nell'elenco quando nessun programma lo rivendica ({0}), oppure quando una patch più recente lo ha sostituito e nessun programma potrebbe tornare a esso ({1}).`,
  'Body.MainExplanation.Action': `Spostali in una cartella di backup che scegli tu, poi elimina quella cartella quando sei convinto che i tuoi programmi si aggiornino e si disinstallino ancora normalmente. Rimetterli in {InstallerFolder} ripristina tutto. Oppure eliminali definitivamente adesso.`,
  'Body.PendingReboot.MsiExecuteMutex': `Qualcosa sta usando Windows Installer in questo momento, ad esempio un aggiornamento di Windows o un programma che si installa in background. Sposta ed Elimina sono in pausa mentre accade, così InstallerClean non tocca {InstallerFolder} mentre cambia. Quando ha finito, ripeti la scansione e tornano disponibili.`,
  'Body.PendingReboot.InstallerInProgress': `Su questo computer c'è una transazione di Windows Installer sospesa. Riprendi o annulla quell'installazione (o riavvia Windows) prima di ripulire {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows ha in coda per il prossimo riavvio una ridenominazione di file che riguarda {InstallerFolder}. Riavvia Windows prima di ripulire.`,
  'Body.NoFileSelected': `Seleziona un file per vederne i dettagli.`,
  'Body.NoProductSelected': `Seleziona un prodotto per vederne i dettagli.`,
  'Body.NoMetadata': `Nessun metadato disponibile.`,
  'Body.RegisteredMissingFromDisk': `Questo file di installazione manca. Ora non causa alcun problema, e non ne causerà fino al giorno in cui proverai ad aggiornare o disinstallare il programma a cui appartiene. Quel passaggio può allora fallire, perché Windows cerca questo file e non lo trova.\n\nPer rimetterlo, ti serve il programma di installazione della versione che hai già. Procuratelo dal produttore del programma ed eseguilo sopra la copia esistente. Una versione più recente non va bene: dovrebbe prima rimuovere quella che hai, ed è proprio il passaggio che ha bisogno di questo file. Nemmeno disinstallare prima funziona, per lo stesso motivo. Questo dovrebbe ripristinare il file e lasciare intatte le tue impostazioni, ma Microsoft non lo garantisce.`,
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
  'Status.Moving': `Spostamento dei file non necessari...`,
  'Status.Deleting': `Eliminazione dei file non necessari...`,
  'Status.MoveCancelled.Partial': `Spostamento annullato dopo {0} di {1} {2}.`,
  'Status.DeleteCancelled.Partial': `Eliminazione annullata dopo {0} di {1} {2}.`,
  'Status.MoveFailed': `{0}. Dettagli in {1}.`,
  'Status.MoveFailed.NoLog': `{0}. Non è stato possibile scrivere il file crash.log.`,
  'Status.DeleteFailed': `{0}. Dettagli in {1}.`,
  'Status.DeleteFailed.NoLog': `{0}. Non è stato possibile scrivere il file crash.log.`,
  'Status.ScanAccessDenied': `Accesso negato. Windows ha rifiutato la scansione.`,
  'Status.ScanFailedDb': `Scansione non riuscita: impossibile leggere i record di Windows Installer.`,
  'Status.ScanCancelled': `Scansione annullata.`,
  'Status.Done': `Pronto`,
  'Status.ScanFailedDetails': `Scansione non riuscita ({0}). Dettagli in {1}.`,
  'Status.ScanFailedDetails.NoLog': `Scansione non riuscita ({0}). Non è stato possibile scrivere il file crash.log.`,
  'Completion.AllClean': `Tutto pulito`,
  'Completion.NothingToCleanUp': `Niente da eliminare in {InstallerFolder}`,
  'Completion.NothingToCleanUpReceipt': `Analizzati {0} {1} in {2}`,
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
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} eliminato definitivamente`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} eliminati definitivamente`,
  'Summary.RegisteredStillUsed.Singular': `{0} file lasciato stare`,
  'Summary.RegisteredStillUsed.Plural': `{0} file lasciati stare`,
  'Summary.OrphanedToCleanUp.Singular': `{0} file non necessario da eliminare`,
  'Summary.OrphanedToCleanUp.Plural': `{0} file non necessari da eliminare`,
  'Summary.NothingListed.Singular': `InstallerClean non è riuscito a stabilire con certezza quali file nella cache appartengono ai programmi installati qui, perciò ha trattenuto l'unico file invece di proporlo.`,
  'Summary.NothingListed.Plural': `InstallerClean non è riuscito a stabilire con certezza quali file nella cache appartengono ai programmi installati qui, perciò ha trattenuto {0} {1} invece di proporli.`,
  'Summary.MissingFromDisk.Singular': `Windows ha un record per {0} file che non è in {InstallerFolder}: {1}. Nell'uso quotidiano non crea problemi, ma un aggiornamento o una disinstallazione di quel programma può fallire. Apri Dettagli per sapere cosa fare.`,
  'Summary.MissingFromDisk.Plural': `Windows ha record per {0} file che non sono in {InstallerFolder}: {1}. Nell'uso quotidiano non creano problemi, ma un aggiornamento o una disinstallazione di quei programmi può fallire. Apri Dettagli per sapere cosa fare.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `{0} altro programma`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `{0} altri programmi`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} file senza alcun programma nominato nei record`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} file senza alcun programma nominato nei record`,
  'Summary.OperationFiles': `{0} di {1} {2}`,
  'Summary.OrphanedWindow': `{0} {1} da eliminare ({2})`,
  'Summary.RegisteredWindow.Singular': `{0} file lasciato stare ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} file lasciati stare ({1})`,
  'Confirm.MoveTitle': `Vuoi spostare {0} {1} ({2})?`,
  'Confirm.DeleteTitle': `Vuoi eliminare {0} {1} ({2})?`,
  'Error.AdminRequiredTitle': `Accesso negato`,
  'Error.AdminRequiredBody': `Windows ha negato l'accesso a InstallerClean, che si è quindi fermato. Non è stato rimosso nulla.\n\nInstallerClean era già in esecuzione come amministratore, quindi riavviarlo in quel modo non serve. Windows non dice altro su che cosa abbia negato l'accesso, quindi non c'è nulla di specifico da provare.`,
  'Error.InstallerDbUnavailableTitle': `Impossibile leggere i record di Windows Installer`,
  'Error.ScanFailedTitle': `Scansione non riuscita`,
  'Error.InstallerDbEmpty': `I record di Windows Installer sono tornati completamente vuoti: nemmeno un programma installato o un aggiornamento rivendica un file di installazione nella cache. Su una macchina funzionante questo non succede (perfino un'installazione di Windows appena fatta ne ha qualcuno), quindi o i record sono danneggiati o non è stato possibile leggerli, e una scansione che credesse a questa risposta classificherebbe erroneamente come orfano ogni file in {InstallerFolder}. InstallerClean si è fermato invece di farlo. Non è stato rimosso nulla.`,
  'Error.MsiAccessDenied': `Windows Installer ha impedito a InstallerClean di elencare ciò che è installato. InstallerClean era già in esecuzione come amministratore, quindi eseguirlo di nuovo come amministratore non cambia nulla. Senza quell'elenco non c'è modo sicuro di sapere quali file nella cache servono ancora, quindi InstallerClean si è fermato. Non è stato rimosso nulla.`,
  'Error.MsiNonSuccess': `Windows Installer non è riuscito a fornire a InstallerClean un elenco leggibile dei programmi installati: ha letto {2} {3}, poi {0} voci di seguito sono tornate illeggibili (ultimo codice di errore {1}). Invece di lavorare su un elenco letto solo in parte, InstallerClean si è fermato. Non è stato rimosso nulla.`,
  'Error.InvalidDestinationTitle': `Destinazione non valida`,
  'Error.DestinationWriteFailedTitle': `Impossibile scrivere nella destinazione`,
  'Error.MoveFailedTitle': `Spostamento non riuscito`,
  'Error.DeleteFailedTitle': `Eliminazione non riuscita`,
  'Error.SettingNotSavedTitle': `Impostazione non salvata`,
  'Error.SettingNotSavedBody': `Impossibile salvare la modifica. Al prossimo avvio InstallerClean tornerà all'impostazione precedente.`,
  'Error.DestinationInsideInstaller': `La destinazione non può trovarsi all'interno della cartella di Windows Installer.`,
  'Error.DestinationInSystemFolder': `La destinazione {0} si risolve dentro una cartella di sistema di Windows. Scegli un percorso fuori da %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% e %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Spazio insufficiente`,
  'Error.NotEnoughSpaceBody': `Non c'è abbastanza posto in {0}\n\nNecessario: {1}\nDisponibile: {2}`,
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
  'Error.FileInUse.Singular': `Questo file è aperto o bloccato da un altro programma, quindi al momento nulla può rimuoverlo. È stato lasciato al suo posto; riprova più tardi.`,
  'Error.FileInUse.Plural': `Questi file sono aperti o bloccati da un altro programma, quindi al momento nulla può rimuoverli. Sono stati lasciati al loro posto; riprova più tardi.`,
  'Error.IOFailure.Singular': `Windows ha segnalato un errore sul file; il file è stato lasciato al suo posto.`,
  'Error.IOFailure.Plural': `Windows ha segnalato errori sui file; questi file sono stati lasciati al loro posto.`,
  'Error.UnknownError.Singular': `Qualcosa è andato storto con questo file; è stato lasciato al suo posto.`,
  'Error.UnknownError.Plural': `Qualcosa è andato storto con questi file; sono stati lasciati al loro posto.`,
  'Error.MoveIntoInstaller': `Spostamento dei file nella cartella di Windows Installer rifiutato (destinazione: {0}).`,
  'Error.DestinationNotFullyQualified': `La cartella di backup deve essere un percorso completo a una cartella, che inizia con una lettera di unità o una condivisione di rete (ad esempio D:\\Backup, oppure \\\\server\\backup). InstallerClean non può usare questo: {0}`,
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
  'UpdateCheck.Failed.Unknown': `Il controllo non è riuscito per un motivo sconosciuto. I dettagli sono in {0}, se devi segnalarlo.`,
  'BrowserLaunch.ClipboardOk': `The link is on your clipboard, so you can paste it in yourself:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean couldn't copy the link to your clipboard either, so here it is:&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `InstallerClean non è più riuscito a confermare la cartella di backup, quindi si è fermato. Controlla {0}, poi Ripeti scansione e riprova.`,
  'Error.CannotWriteFolder': `Impossibile scrivere in {0}.`,
  'Error.DestinationCollision': `Un file di nome '{0}' è già nella cartella di backup.`,
  'ResultLog.Sending': `Invio...`,
  'ResultLog.Sent': `Grazie! Rapporto inviato.`,
  'ResultLog.Failed': `Invio non riuscito. Riprova più tardi.`,
  'ResultLog.NothingToSend': `Nessun rapporto da inviare.`,
  'ConfirmSendResultLog.Title': `Vuoi inviare questo?`,
  'ConfirmSendResultLog.Reassurance': `Viene inviato a nofaff.netlify.app/api/result-log. Niente ti identifica, né identifica il computer; mi fa solo sapere che InstallerClean funziona e [quanto spazio le persone stanno liberando].`,
  'Automation.ResultLogPreview': `Anteprima rapporto`,
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `È già in esecuzione.`,
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
  'Display.ListSeparator': `, `,
  'Display.ElapsedLong.LessThanASecond': `meno di un secondo`,
  'Display.ElapsedLong.Seconds': `{0:F1} secondi`,
  'Cli.UnknownArgument': `Errore: argomento sconosciuto '{0}'`,
  'Cli.Cancelling': `Annullamento...`,
  'Cli.Cancelled': `Operazione annullata.`,
  'Cli.GenericError': `Errore: crash inatteso ({0}). Dettagli scritti in {1}.`,
  'Cli.GenericError.NoLog': `Errore: crash inatteso ({0}). Non è stato possibile scrivere il registro dei crash.`,
  'Cli.ScanningInstaller': `Scansione di {InstallerFolder}...`,
  'Cli.FoundOrphans': `Trovati {0} {1} non necessari da eliminare ({2}).`,
  'Cli.DeletingFiles': `Eliminazione di {0} {1} non necessari...`,
  'Cli.DeletedFiles': `Eliminati definitivamente {0} {1} non necessari.`,
  'Cli.NoMoveDestination': `Errore: nessuna destinazione di spostamento specificata. Usa /m PERCORSO. (Una destinazione predefinita impostata nella GUI è specifica per utente e non si applica alle esecuzioni pianificate o con account di servizio.)`,
  'Cli.MoveDestinationInsideInstaller': `Errore: la destinazione non può trovarsi all'interno della cartella di Windows Installer.`,
  'Cli.MoveDestinationRelative': `Errore: la destinazione deve essere un percorso completo. Ricevuto: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Errore: la destinazione {0} si risolve dentro una cartella di sistema di Windows. Scegli un percorso fuori da %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% e %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Errore: qualcosa sta usando Windows Installer in questo momento, ad esempio un aggiornamento di Windows o un programma che si installa in background. /m e /d sono bloccati mentre accade. Riprova quando ha finito.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Errore: su questo computer c'è una transazione di Windows Installer sospesa. Riprendi o annulla quell'installazione (o riavvia Windows) prima di ripulire {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Errore: un'operazione su file in coda dopo il riavvio riguarda {InstallerFolder} ({0}). Riavvia Windows per completare quell'operazione prima di ripulire.`,
  'Cli.MovingFiles': `Spostamento di {0} {1} non necessari in {2}...`,
  'Cli.MovedFiles': `Spostati {0} {1} non necessari.`,
  'Cli.MutexBlocked': `Un altro processo InstallerClean mantiene il blocco a istanza singola (la GUI o un'altra esecuzione della CLI). Codice di uscita 75 (transitorio); è sicuro riprovare più tardi.`,
  'Cli.EventLogUnavailable': `Nota: scrittura nel registro eventi non riuscita. Controlla i permessi del registro Applicazione o i Criteri di gruppo.`,
  'CrashLog.PrivacyHeader': `# crash.log raccoglie le eccezioni non gestite di InstallerClean.\n# Con privilegi elevati, i messaggi di eccezione del framework possono\n# includere percorsi di file della sessione in corso (compresi i\n# profili di altri utenti enumerati dalle query di Windows Installer).\n# I messaggi di errore di rete del controllo aggiornamenti o dell'invio\n# del registro dei risultati possono includere l'URL di destinazione e\n# l'indirizzo IP o proxy risolto. Le voci sui record di Windows\n# Installer illeggibili possono includere un SID di account Windows\n# (S-1-5-21-...) e i codici prodotto del software installato.\n# Rimuovi tutte e tre le categorie di dati prima di allegare questo\n# file a una segnalazione di bug pubblica.\n`,
  'Cli.Help.Header': `InstallerClean - pulizia di {InstallerFolder}`,
  'Cli.Help.Usage': `Utilizzo:`,
  'Cli.Help.Help': `  installerclean-cli --help       Mostra questa guida (anche /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version    Mostra la versione (anche -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s           Solo scansione - elenca i superflui`,
  'Cli.Help.Delete': `  installerclean-cli /d           Elimina definitivamente i superflui`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m           Sposta nella cartella salvata`,
  'Cli.Help.MovePath': `  installerclean-cli /m PERCORSO  Sposta nel percorso specificato`,
  'Cli.Help.NoteLine1': `installerclean-cli blocca il prompt finché non termina, così uno script&#10;o un'operazione pianificata può attenderlo.`,
  'Cli.Help.ExitCodesHeader': `Codici di uscita:`,
  'Cli.Help.ExitCodeOk': `  0   riuscito: ha fatto quanto richiesto e nulla è fallito`,
  'Cli.Help.ExitCodeError': `  1   errore: nulla elaborato (argomenti o destinazione errati,&#10;       scansione fallita o tutti i file falliti)`,
  'Cli.Help.ExitCodePartial': `  2   parziale: alcuni elaborati, altri no (un errore o un Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  transitorio: qualcosa ha bloccato l'esecuzione (vedi il messaggio)`,
  'Cli.Help.ExitCodeCancelled': `  130 annullato (Ctrl+C)`,
  'Tooltip.ChangeLanguage': `Cambia lingua. Il programma verrà riavviato.`,
  'Automation.ChangeLanguage': `Cambia lingua`,
  'Automation.ChangeLanguage.HelpText': `Il programma verrà riavviato.`,
  'Body.NotScanned.Lead': `Ancora nessuna scansione.`,
  'Body.NotScanned.Why': `Premi Ripeti scansione per cercare in {InstallerFolder} i file di installazione che nessun programma usa più.`,
  'Confirm.MoveSameDrive': `Quella cartella è sulla stessa unità, quindi lo spazio non torna finché non la elimini. Scegli invece una cartella su un'altra unità se vuoi lo spazio subito.`,
  'Error.ScanCorrelationFailed': `InstallerClean non è riuscito a far corrispondere i record di Windows Installer con il contenuto di {InstallerFolder}. Quasi nulla di ciò che i record indicano si trova davvero lì, e quasi nulla di ciò che è lì è nominato da un record, quindi non si è potuto dimostrare che qualche file fosse non necessario. Non è stato proposto nulla e non è stato rimosso nulla.`,
  'Error.CandidateOutsideCache': `Questo file non si trova direttamente all'interno della cartella di Windows Installer; rifiutato per sicurezza.`,
  'Completion.MoveCancelledSummary': `Spostati {0} di {1} {2} prima dell'annullamento.`,
  'Completion.PermanentDeleteCancelledSummary': `Eliminati definitivamente {0} di {1} {2} prima dell'annullamento.`,
  'Body.PendingReboot.Lead': `Al momento questi file non si possono ripulire.`,
  'Cli.TooManyArguments': `Errore: argomento aggiuntivo imprevisto '{0}'. Se la cartella di destinazione contiene uno spazio, racchiudi l'intero percorso tra virgolette: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Cartella per utente; esecuzioni pianificate o SYSTEM: /m PERCORSO.`,
  'Error.ScanRecordsUnreadable': `InstallerClean non è riuscito a leggere abbastanza dei record di Windows Installer per essere sicuro di che cosa serva ancora: l'elenco dei programmi installati è tornato incompleto, e leggere gli stessi record direttamente dal registro di sistema ha dato errori a sua volta. Un file potrebbe sembrare orfano solo perché il record che lo nomina era fra quelli illeggibili, quindi InstallerClean si è fermato. Non è stato rimosso nulla.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer non ha mai segnalato la fine dell'elenco dei programmi installati: InstallerClean ha letto {2} {3}, poi ha rinunciato dopo {0} voci (ultimo codice di errore {1}). Di un elenco senza fine non ci si può fidare, quindi InstallerClean si è fermato. Non è stato rimosso nulla.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer non ha mai segnalato la fine dell'elenco delle patch di un programma: InstallerClean ha letto {2} {3}, poi ha rinunciato dopo {0} voci (ultimo codice di errore {1}). Di un elenco senza fine non ci si può fidare, quindi InstallerClean si è fermato. Non è stato rimosso nulla.`,
  'UpdateCheck.Status.UpdateAvailable': `È disponibile la versione {0}.`,
  'Completion.DonateAsk': `Felice di esserti stato utile. Un caffè è sempre bene accetto, se ti viene dal cuore.`,
  'About.Link.Guide': `Guida e FAQ`,
  'About.Link.ReportProblem': `Segnala un problema`,
  'About.AutoUpdateCheck': `Controlla automaticamente gli aggiornamenti`,
  'Automation.About.Guide.HelpText': `Apre readme su github nel browser.`,
  'Automation.About.ReportProblem.HelpText': `Apre elenco problemi (issue) in github.com nel browser.`,
  'Automation.AutoUpdateCheck.HelpText': `Se selezionata, all'avvio InstallerClean verifica su github se è disponibile una versione più recente.`,
  'Tooltip.MoveSameDrive': `Sposta i file non necessari nella cartella di backup. È sullo stesso disco, quindi non recuperi lo spazio finché non elimini quella cartella.`,
  'Confirm.DeletePermanently.Singular': `Questo file verrà eliminato definitivamente. È un'operazione sicura, ma se vuoi una copia di riserva usa invece Sposta.`,
  'Confirm.DeletePermanently.Plural': `Questi file verranno eliminati definitivamente. È un'operazione sicura, ma se vuoi una copia di riserva usa invece Sposta.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean non è riuscito a far risolvere a Windows il vero percorso di {InstallerFolder}, quindi non si è potuto dimostrare che qualche file fosse al suo interno e nessuno è stato proposto per la pulizia. Questa scansione non ha trovato nulla perché quel controllo non è riuscito, non perché la cartella sia pulita. Non è stato rimosso nulla.`,
  'Automation.Scroll.ProductDetails': `Dettagli del prodotto`,
  'Body.PendingReboot.Other': `Windows Installer ha qualcosa in corso, quindi Sposta ed Elimina sono in pausa. InstallerClean non tocca {InstallerFolder} mentre cambia. Quando ha finito, ripeti la scansione e tornano disponibili.`,
  'Cli.TooManyArgumentsNoPath': `Errore: argomento aggiuntivo inatteso '{0}'. /s e /d non accettano altri argomenti, e si può usare una sola opzione per esecuzione.`,
  'Cli.MissingFromDisk.Singular': `Windows ha un record per {0} file che non è in {InstallerFolder}: {1}. Nell'uso quotidiano non crea problemi, ma un aggiornamento o una disinstallazione di quel programma può fallire. Per rimettere il file, ti serve il programma di installazione della versione che hai già. Procuratelo dal produttore del programma ed eseguilo sopra la copia esistente. Una versione più recente non va bene: dovrebbe prima rimuovere quella che hai, ed è proprio il passaggio che ha bisogno di questo file. Nemmeno disinstallare prima funziona, per lo stesso motivo. Questo dovrebbe ripristinare il file e lasciare intatte le tue impostazioni, ma Microsoft non lo garantisce.`,
  'Cli.MissingFromDisk.Plural': `Windows ha record per {0} file che non sono in {InstallerFolder}: {1}. Nell'uso quotidiano non creano problemi, ma un aggiornamento o una disinstallazione di quei programmi può fallire. Per rimettere un file, ti serve il programma di installazione della versione di quel programma che hai già. Procuratelo dal produttore del programma ed eseguilo sopra la copia esistente. Una versione più recente non va bene: dovrebbe prima rimuovere quella che hai, ed è proprio il passaggio che ha bisogno del file. Nemmeno disinstallare prima funziona, per lo stesso motivo. Questo dovrebbe ripristinare il file e lasciare intatte le tue impostazioni, ma Microsoft non lo garantisce.`,
  'Cli.MoveNotEnoughSpace': `Errore: spazio insufficiente in {0}. Spostare questi file richiede {1} e ne sono liberi {2}. Non è stato spostato nulla.`,
  'Cli.PendingRebootBlocked.Other': `Errore: Windows Installer ha qualcosa in corso, quindi /m e /d sono bloccati. InstallerClean non tocca {InstallerFolder} mentre cambia. Riprova quando ha finito.`,
  'Cli.FoundNoOrphans': `Nessun file non necessario trovato.`,
  'Cli.NothingOffered.Singular': `InstallerClean non è riuscito a stabilire con certezza quali file nella cache appartengono ai programmi installati qui, perciò ha trattenuto l'unico file ({2}) invece di proporlo.`,
  'Cli.NothingOffered.Plural': `InstallerClean non è riuscito a stabilire con certezza quali file nella cache appartengono ai programmi installati qui, perciò ha trattenuto tutti i {0} {1} ({2}) invece di proporli.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean non è più riuscito a confermare la cartella di backup, quindi si è fermato. Controlla {0}, poi esegui di nuovo il comando.`,
  'Cli.Help.Summary': `Rimuove i file .msi/.msp in cache che nessun programma installato usa più.`,
  'Cli.Help.Elevation': `Richiede un prompt come amministratore; Windows non lo avvierà.`,
  'Error.InstallerLockUnavailableTitle': `Nessun file eliminato`,
  'Error.MoveInstallerLockUnavailableTitle': `Nessun file spostato`,
  'Error.InstallerLockUnavailable': `InstallerClean non è riuscito a prendere il blocco che Windows Installer usa per impedire a due programmi di modificare il software installato nello stesso momento, quindi non ha potuto escludere che un file diventasse necessario a metà strada, e non è stato eliminato nulla. Riprova, e riavvia Windows se continua a succedere.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean non è riuscito a prendere il blocco che Windows Installer usa per impedire a due programmi di modificare il software installato nello stesso momento, quindi non ha potuto escludere che un file diventasse necessario a metà strada, e non è stato spostato nulla. Riprova, e riavvia Windows se continua a succedere.`,
  'Cli.InstallerLockUnavailable': `Errore: InstallerClean non è riuscito a prendere il blocco di Windows Installer che impedisce a due programmi di modificare il software installato nello stesso momento, quindi non ha potuto escludere che un file diventasse necessario a metà strada. Non è stato eliminato nulla. Riprova, e riavvia Windows se continua a succedere.`,
  'Cli.MoveInstallerLockUnavailable': `Errore: InstallerClean non è riuscito a prendere il blocco di Windows Installer che impedisce a due programmi di modificare il software installato nello stesso momento, quindi non ha potuto escludere che un file diventasse necessario a metà strada. Non è stato spostato nulla. Riprova, e riavvia Windows se continua a succedere.`,
  'Completion.ReverifyIdentityClaimed': `{0} {1} lasciati al loro posto, perché Windows ha un record del programma nominato all'interno.`,
  'Completion.ReverifyIdentityUnreadable': `{0} {1} lasciati al loro posto, perché InstallerClean non ha trovato alcun programma nominato all'interno.`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean non è riuscito a far corrispondere i record di Windows Installer con il contenuto di {InstallerFolder}. La cartella contiene file, ma nemmeno un record indica qualcosa al suo interno, quindi non si è potuto dimostrare che qualche file fosse non necessario. Non è stato proposto nulla e non è stato rimosso nulla.`,
  'Completion.NothingOffered': `Nulla proposto su questo PC`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean non è riuscito a stabilire con certezza quali file nella cache appartengono ai programmi installati qui, perciò ha trattenuto l'unico file ({2}) invece di proporlo.`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean non è riuscito a stabilire con certezza quali file nella cache appartengono ai programmi installati qui, perciò ha trattenuto tutti i {0} {1} ({2}) invece di proporli.`,
  'Summary.SupersededHeldBack.Singular': `InstallerClean non è riuscito a stabilire con certezza che l'unico file sostituito non serva più, perciò l'ha trattenuto.`,
  'Summary.SupersededHeldBack.Plural': `InstallerClean non è riuscito a stabilire con certezza che {0} file sostituiti non servano più, perciò li ha trattenuti.`,
  'Cli.SupersededHeldBack.Singular': `InstallerClean non è riuscito a stabilire con certezza che l'unico file sostituito non serva più, perciò l'ha trattenuto.`,
  'Cli.SupersededHeldBack.Plural': `InstallerClean non è riuscito a stabilire con certezza che {0} file sostituiti non servano più, perciò li ha trattenuti.`,
  'Completion.HeldBack.Singular': `{0} file trattenuto. La scansione lo dava per non necessario. Il controllo finale non ha potuto confermarlo.`,
  'Completion.HeldBack.Plural': `{0} file trattenuti. La scansione li dava per non necessari. Il controllo finale non ha potuto confermarlo.`,
  'Body.PendingReboot.PendingRenameUnresolved': `Un'operazione sui file è in coda per il prossimo riavvio e InstallerClean non riesce a sapere quali file nomina, quindi non può escludere che siano in {InstallerFolder}. Riavvia Windows prima di pulire.`,
  'Completion.MoveRestoreHint': `Elimina quella cartella quando sei convinto che vada tutto bene.`,
  'Completion.MoveRestoreHintSameDrive': `Elimina quella cartella quando sei convinto che vada tutto bene. Solo allora recuperi davvero lo spazio.`,
  'Confirm.MoveDestination.Singular': `Questo file verrà spostato in:`,
  'Confirm.MoveDestination.Plural': `Questi file verranno spostati in:`,
  'Cli.NothingListed.Singular': `InstallerClean non è riuscito a stabilire con certezza quali file nella cache appartengono ai programmi installati qui, perciò ha trattenuto l'unico file ({2}) invece di proporlo.`,
  'Cli.NothingListed.Plural': `InstallerClean non è riuscito a stabilire con certezza quali file nella cache appartengono ai programmi installati qui, perciò ha trattenuto {0} {1} ({2}) invece di proporli.`,
  'Cli.WithheldReasons.Header': `Perché non è stato possibile averne certezza:`,
  'Cli.WithheldReasons.RecordedPath': `  Un percorso di file negli archivi di Windows Installer non si è risolto, perciò non è stato possibile associarvi nulla.`,
  'Cli.WithheldReasons.FileIdentity': `  Non è stato possibile identificare un file di cui Windows ha un archivio, perciò non è stato possibile confrontarlo con il contenuto della cartella.`,
  'Cli.WithheldReasons.SecondInstance': `  Un programma potrebbe essere installato più di una volta su questo PC, e gli archivi non possono dire a quale copia appartenga un file.`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `Errore: un'operazione sui file è in coda per il prossimo riavvio e InstallerClean non riesce a sapere quali file nomina, quindi non può escludere {InstallerFolder}. Riavvia Windows prima di pulire.`,
  'Cli.MoveRestoreHint': `Verifica che i tuoi programmi si aggiornino e si disinstallino ancora normalmente, poi elimina {0}.`,
  'Error.ScanStoppedDetails': `Questo viene registrato anche in {0}.`,
  'Cli.NothingListedPerFile.Singular': `InstallerClean non ha avuto la certezza su uno dei file nella cache che ha trovato, perciò ha trattenuto quello ({2}) invece di proporlo.`,
  'Cli.NothingListedPerFile.Plural': `InstallerClean non ha avuto la certezza su alcuni dei file nella cache che ha trovato, perciò ha trattenuto {0} {1} ({2}) invece di proporli.`,
  'Cli.NothingOfferedPerFile.Singular': `InstallerClean non è riuscito ad accertare che il file nella cache che ha trovato non sia necessario, perciò ha trattenuto quell'unico file ({2}) invece di proporlo.`,
  'Cli.NothingOfferedPerFile.Plural': `InstallerClean non è riuscito ad accertare di nessuno dei file nella cache che ha trovato che non sia necessario, perciò ha trattenuto tutti i {0} {1} ({2}) invece di proporli.`,
  'Completion.NothingOfferedPerFileBody.Singular': `InstallerClean non è riuscito ad accertare che il file nella cache che ha trovato non sia necessario, perciò ha trattenuto quell'unico file ({2}) invece di proporlo.`,
  'Completion.NothingOfferedPerFileBody.Plural': `InstallerClean non è riuscito ad accertare di nessuno dei file nella cache che ha trovato che non sia necessario, perciò ha trattenuto tutti i {0} {1} ({2}) invece di proporli.`,
  'Summary.NothingListedPerFile.Singular': `InstallerClean non ha avuto la certezza su uno dei file nella cache che ha trovato, perciò l'ha trattenuto invece di proporlo.`,
  'Summary.NothingListedPerFile.Plural': `InstallerClean non ha avuto la certezza su alcuni dei file nella cache che ha trovato, perciò ha trattenuto {0} {1} invece di proporli.`,
  'Cli.WithheldReasons.CandidateIdentity': `  Non è stato possibile identificare un file nella cartella, perciò non è stato possibile confrontarlo con gli archivi.`,
  'Cli.WithheldReasons.DeclaredProductInstalled': `  Un file dichiara di appartenere a un programma ancora installato, perciò potrebbe servire ancora.`,
  'Cli.WithheldReasons.DeclaredProductUnestablished': `  O un file non ha indicato a quale programma appartiene, oppure Windows non ha risposto riguardo a quel programma.`,
  'Cli.WithheldReasons.ScreenUnanswered': `  Un controllo su a quali programmi appartengono i file ha dato risposte che non corrispondevano ai file ricevuti.`,
  'Body.PendingReboot.RegistryCheckUnreadable': `InstallerClean non è riuscito a leggere una delle impostazioni di Windows che controlla prima di toccare {InstallerFolder}, quindi non può sapere se un'operazione di installazione è in corso o in attesa di un riavvio. Riavvia Windows e usa Ripeti scansione. Se l'impostazione continua a non essere leggibile, questo non è un computer che InstallerClean possa pulire.`,
  'Cli.InstallerLockAccessRefused': `Errore: Windows ha negato a InstallerClean il permesso di controllare se Windows Installer fosse occupato, quindi non ha potuto escludere che un file servisse a metà strada. Non è stato eliminato nulla.`,
  'Cli.MoveCancelledRestoreHint': `Annullarlo è semplice. Rispostali da {0} in {InstallerFolder} e tornerà tutto com'era.`,
  'Cli.MoveInstallerLockAccessRefused': `Errore: Windows ha negato a InstallerClean il permesso di controllare se Windows Installer fosse occupato, quindi non ha potuto escludere che un file servisse a metà strada. Non è stato spostato nulla.`,
  'Cli.PendingRebootBlocked.RegistryCheckUnreadable': `Errore: InstallerClean non è riuscito a leggere uno dei valori di registro che controlla prima di toccare {InstallerFolder}, quindi non può escludere un'operazione di Windows Installer in corso o in coda per il prossimo riavvio. /m e /d sono bloccati. Riavvia Windows e riprova. Se la lettura continua a non riuscire, questo non è un computer che InstallerClean possa pulire.`,
  'Completion.MoveCancelledRestoreHint': `Annullarlo è semplice. Rispostali in {InstallerFolder} e tornerà tutto com'era.`,
  'Error.InstallerLockAccessRefused': `Windows ha negato a InstallerClean il permesso di controllare se Windows Installer fosse occupato, quindi non ha potuto escludere che un file servisse a metà strada, e non è stato eliminato nulla.`,
  'Error.MoveInstallerLockAccessRefused': `Windows ha negato a InstallerClean il permesso di controllare se Windows Installer fosse occupato, quindi non ha potuto escludere che un file servisse a metà strada, e non è stato spostato nulla.`,
  'Error.MoveStoppedTitle': `Spostamento interrotto`,
  'Field.NoNamedProduct': `(nessun programma)`,
  'Summary.RegisteredWindow.Missing.Plural': `{0} mancanti`,
  'Summary.RegisteredWindow.Missing.Singular': `{0} mancante`,
  'UpdateCheck.Failed.Unknown.NoLog': `Il controllo non è riuscito per un motivo sconosciuto. Non è stato possibile scrivere il file crash.log.`,
};

// PARSE CONTROL. About the READING and not about the content, and it exits 2,
// which is a code no ordinary run of this generator can produce: a generator is
// red by intent for the whole gap between a string landing in English and its
// translation round, so its verdict lines and its exit 0 are load-bearing in
// ci.yml and are deliberately untouched here. This says something different from
// "the translation is not done". It says the file could not be read.
//
// BOTH LEGS. raw === 0 catches a file that declares no entry at all, which the
// equality cannot see on its own because 0 === 0 holds. parsed !== raw catches
// entries the reader dropped, which one <comment> moved above its <value> does to
// any regex wanting <value> on the same whitespace run, and the Visual Studio resx
// editor writes that shape. Counted with <data\b so a tab after the tag name is
// not read as an empty file, and neither figure is written down, so a string added
// to the resx cannot make this go stale.
//
// WHY IT IS HERE WHEN THE SELF-CHECK BELOW ALREADY REDDENS. The self-check reaches
// the right verdict through what it happens to compare, not through knowing it
// read anything: with the neutral's attribute order changed, this generator wrote a
// 389-entry file and its own self-check parsed THREE entries out of it, said
// GENERATION HAS ISSUES, and was right for a reason with nothing to do with the
// truth. A tool reasoning over three entries of a 389-entry artefact should say so.
const parseControl = (where, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${where}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this generator cannot show it read.');
  process.exit(2);
};

let text = readFileSync(BASE, 'utf8');
// The transform below reaches every entry through '<data name="', one space and no
// \s+, which is NOT the spelling the self-check's parse() uses further down. A
// control that exercises a pattern the reader does not use proves the file has
// structure and proves nothing about whether this reader can reach it, so the
// source is controlled in its own spelling before a single value is replaced.
parseControl(BASE, text,
  [...text.matchAll(/<data name="([^"]+)"[^>]*>\s*<value>/g)].length);

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
const parse = (xml, where) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  parseControl(where, xml, map.size);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'), BASE);
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written, OUT);
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
  for (const k of alsoKeep) console.log('   ' + k + ' = ' + JSON.stringify(output.get(k)));
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
if (untranslated.length) console.log('!! still English (untranslated), ' + untranslated.length + ': ' + untranslated.slice(0, 40).join(', '));

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  !humanCliStripped &&
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === cliMachineExpected && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
