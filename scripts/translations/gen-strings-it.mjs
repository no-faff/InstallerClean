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
// The five size suffixes and the two elapsed suffixes do not belong in this list,
// because they are not universal: French writes To/Go/Mo/Ko/o, Russian and Ukrainian
// write ТБ/ГБ/МБ/КБ/Б and мс/с. Those three carry real
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
  // get wrong; only Japanese and Chinese differ, taking the ideographic
  // comma.
  'Display.ListSeparator',       // ", "
  // The stop that ends a heading before the body is the one English uses. A
  // punctuation mark rather than a word, so there is nothing to translate and
  // nothing to get wrong; only Japanese and Chinese differ, taking the
  // ideographic full stop, which carries no space after it.
  'Display.SentenceSeparator',   // ". "
  // The colon between a link's words and its address on the command line is the
  // one English uses. A punctuation mark rather than a word, so there is nothing to
  // translate and nothing to get wrong; French puts a narrow no-break space before
  // it, and Japanese and Chinese take the full-width colon.
  'Cli.UpdateDownloadLink',      // "{0}: {1}"
  // The size and elapsed unit suffixes. Italian abbreviates them exactly as
  // English does, so there is nothing to translate and nothing to get wrong.
  // A per-language keep rather than a universal one because fr, ru and uk do
  // NOT: French takes To/Go/Mo/Ko/o, Russian and Ukrainian take ТБ/ГБ/МБ/КБ/Б and
  // мс/с, and all three carry real values in their MAP.
  'Display.Size.TB',           // {0} TB
  'Display.Size.GB',           // {0} GB
  'Display.Size.MB',           // {0} MB
  'Display.Size.KB',           // {0} KB
  'Display.Size.B',            // {0} B
  'Display.Elapsed.Ms',        // {0:F0}ms
  'Display.Elapsed.S',         // {0:F1}s
  // The command line's answer letter for no is N, the first letter of this language's
  // word for no.
  'Cli.UpdateAnswerNo',
];

// Satellite-only CLDR plural overrides: keys absent from the neutral, appended
// before </root> and read by name at runtime (DisplayHelpers.Pluralise's One
// branch; an absent one falls back to the base).
const OVERRIDES = {
  'Cli.FoundOrphans.One': `Trovato {0} {1} non necessario da eliminare ({2}).`,
  'Cli.DeletingFiles.One': `Eliminazione di {0} {1} non necessario...`,
  'Cli.DeletedFiles.One': `Eliminato definitivamente {0} {1} non necessario.`,
  'Cli.MovingFiles.One': `Spostamento di {0} {1} non necessario in {2}...`,
  'Cli.MovedFiles.One': `Spostato {0} {1} non necessario.`,
};

const MAP = {
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Info programma`,
  'Window.Registered.Title': `File non toccati`,
  'Window.Orphaned.Title': `File non necessari, sicuri da eliminare`,
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `PATCH`,
  'Section.Registered.Details': `DETTAGLI PRODOTTO`,
  'Section.Backup.Folder': `CARTELLA BACKUP`,
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
  'Action.About': `_Info programma`,
  'Action.Copy': `Copia`,
  'Action.Cut': `Taglia`,
  'Action.Paste': `Incolla`,
  'Action.SelectAll': `Seleziona tutto`,
  'Action.Browse': `S_foglia...`,
  'Action.Cancel': `_Annulla`,
  'Action.CheckForUpdates': `Controlla _aggiornamenti`,
  'Action.Close': `_Chiudi`,
  'Action.DeletePermanently': `_Elimina definitivamente`,
  'Action.Done': `_OK`,
  'Action.Details': `Dettagli`,
  'Action.LeaveATip': `_Fai una donazione`,
  'Action.Donate': `_Fai una donazione`,
  'Action.DonateSmall': `Fai una donazione`,
  'Action.LeaveStarOnGitHub': `Lascia una _stella su GitHub`,
  'Action.Licence': `Licenza Apache 2.0`,
  'Action.Move': `_Sposta`,
  'Action.BackupFolderPlaceholder': `Percorso cartella salvataggio file se usi 'Sposta' anziché 'Elimina'.`,
  'Action.Rescan': `_Ripeti scansione`,
  'Automation.LeaveATip.About': `Fai una donazione`,
  'Automation.Donate': `Fai una donazione`,
  'Automation.DonateSmall': `Fai una donazione`,
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
  'Automation.CheckForUpdates': `Controlla aggiornamenti`,
  'Automation.CheckForUpdates.HelpText': `Verifica sulla pagina release di github se esiste una versione più recente.`,
  'Automation.Licence.HelpText': `Apre file licenza in github.com nel browser.`,
  'Automation.Section.BackupFolder': `Cartella backup`,
  'Automation.Section.Patches': `Patch`,
  'Automation.Section.ProductDetails': `Dettagli prodotto`,
  'Automation.BackupFolder': `Cartella backup`,
  'Automation.OperationProgress': `Avanzamento operazione`,
  'Automation.RescanInstaller': `Nuova scansione di {InstallerFolder}`,
  'Automation.ScanningProgress': `Avanzamento scansione`,
  'Automation.StartupScanProgress': `Avanzamento scansione all'avvio`,
  'Automation.ViewOrphanedFiles': `Dettagli, file non necessari`,
  'Automation.ViewOrphanedFiles.HelpText': `Disponibili per la pulizia.`,
  'Automation.ViewRegisteredFiles': `Dettagli, file non toccati`,
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
  'Tooltip.LeaveATip.About': `Ogni donazione è molto apprezzata`,
  'Tooltip.CancellingPending': `Annullamento richiesto. InstallerClean sta aspettando che il passaggio in corso arrivi a un punto in cui fermarsi. Può richiedere qualche secondo durante operazioni di I/O intense o una chiamata al database MSI.`,
  'Tooltip.Close': `Chiudi`,
  'Tooltip.LeaveStarOnGitHub.About': `Una stella aiuta altre persone a trovare InstallerClean`,
  'Tooltip.Minimise': `Riduci a icona`,
  'Tooltip.Move': `Sposta i file non necessari nella cartella di backup`,
  'Tooltip.MoveNeedsDestination': `Sposta i file non necessari in una cartella di backup. La scegli subito dopo.`,
  'Tooltip.Delete': `Elimina definitivamente i file non necessari. Usa invece Sposta se vuoi la possibilità di convincerti che vada tutto bene.`,
  'Tooltip.SigningCertificate': `Nome soggetto dal certificato Authenticode incorporato. Catena non verificata.`,
  'Body.MainExplanation.Lead': `I file non necessari qui sotto possono essere eliminati in sicurezza.`,
  'Body.MainExplanation.Why': `Si trovano in {InstallerFolder}. InstallerClean interroga Windows per ogni programma installato: un file viene indicato come sicuro da eliminare quando nessun programma lo rivendica ({0}), oppure quando una patch più recente lo ha sostituito e nessun programma potrebbe tornare a esso ({1}).`,
  'Body.MainExplanation.Action': `Spostali in una cartella di backup a tua scelta, poi elimina quella cartella quando sei convinto che i programmi si aggiornino e si disinstallino ancora normalmente. Se ricopi i file in {InstallerFolder} ripristini tutto. Oppure eliminali definitivamente adesso.`,
  'Body.PendingReboot.MsiExecuteMutex': `Qualcosa sta usando Windows Installer in questo momento, ad esempio Windows Update o un programma in fase di installazione o di rimozione. Sposta ed Elimina sono in pausa mentre accade, così InstallerClean non tocca {InstallerFolder} finché non ha finito. Poi premi Ripeti scansione e tornano disponibili.`,
  'Body.PendingReboot.MsiExecuteMutexAccessRefused': `Windows ha negato a InstallerClean il permesso di controllare se Windows Installer è occupato, quindi Sposta ed Elimina sono in pausa e InstallerClean non tocca {InstallerFolder}. Riavvia Windows. InstallerClean controllerà di nuovo la prossima volta che lo avvii. Se Windows continua a negarlo, questo non è un computer che InstallerClean possa pulire.`,
  'Body.PendingReboot.MsiExecuteMutexUnreadable': `InstallerClean non è riuscito a controllare se Windows Installer è occupato, quindi Sposta ed Elimina sono in pausa e InstallerClean non tocca {InstallerFolder}. Riavvia Windows. InstallerClean controllerà di nuovo la prossima volta che lo avvii. Se continua a non riuscire a controllarlo, questo non è un computer che InstallerClean possa pulire.`,
  'Body.PendingReboot.MsiExecuteMutexUnreadableWindowsError': `InstallerClean non è riuscito a controllare se Windows Installer è occupato, perché Windows ha restituito un errore: {0} (errore {1}). Sposta ed Elimina sono in pausa e InstallerClean non tocca {InstallerFolder}. Riavvia Windows. InstallerClean controllerà di nuovo la prossima volta che lo avvii. Se continua a non riuscire a controllarlo, questo non è un computer che InstallerClean possa pulire.`,
  'Body.PendingReboot.InstallerInProgress': `Un'installazione precedente eseguita da Windows Installer non è terminata, quindi Sposta ed Elimina sono in pausa e InstallerClean non tocca {InstallerFolder}. Riavvia Windows. Un'installazione in attesa di un riavvio prosegue dopo il riavvio, e InstallerClean controllerà di nuovo la prossima volta che lo avvii. Se questo messaggio ricompare, esegui di nuovo il programma di installazione dell'applicazione che si stava installando o rimuovendo, e Windows Installer proporrà di annullare le modifiche apportate dall'installazione non terminata.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows ha una modifica a {InstallerFolder} in attesa del prossimo riavvio, quindi Sposta ed Elimina sono in pausa. Riavvia Windows. InstallerClean controllerà di nuovo la prossima volta che lo avvii.`,
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
  'Status.CheckingRegistry': `Controllo registro per altri pacchetti...`,
  'Status.RegisteredPackagesFound': `Confronto file con programmi installati...`,
  'Status.CheckingRemaining': `Controllo file rimanenti...`,
  'Status.ScanComplete': `Scansione completata ({0})`,
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
  'Status.ScanStopped': `Scansione interrotta.`,
  'Status.ScanCancelled': `Scansione annullata.`,
  'Status.Ready': `Pronto`,
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
  'Completion.FailedCount.Singular': `{0} file non è stato spostato.`,
  'Completion.FailedCount.Plural': `{0} file non sono stati spostati.`,
  'Completion.FailedCountDelete.Singular': `{0} file non è stato eliminato.`,
  'Completion.FailedCountDelete.Plural': `{0} file non sono stati eliminati.`,
  'Completion.MoveSummary.Singular': `Spostato {0} {1} in: {2}`,
  'Completion.MoveSummary.Plural': `Spostati {0} {1} in: {2}`,
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} eliminato definitivamente`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} eliminati definitivamente`,
  'Summary.RegisteredStillUsed.Singular': `{0} file non toccato`,
  'Summary.RegisteredStillUsed.Plural': `{0} file non toccati`,
  'Summary.OrphanedToCleanUp.Singular': `{0} file non necessario che è possibile eliminare`,
  'Summary.OrphanedToCleanUp.Plural': `{0} file non necessari che è possibile eliminare`,
  'Summary.MissingFromDisk.Singular': `Windows ha un record per {0} file che non è in {InstallerFolder}: {1}. Nell'uso quotidiano non crea problemi, ma un aggiornamento o una disinstallazione di quel programma può fallire. Apri Dettagli per sapere cosa fare.`,
  'Summary.MissingFromDisk.Plural': `Windows ha record per {0} file che non sono in {InstallerFolder}: {1}. Nell'uso quotidiano non creano problemi, ma un aggiornamento o una disinstallazione di quei programmi può fallire. Apri Dettagli per sapere cosa fare.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `{0} altro programma`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `{0} altri programmi`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} file senza alcun programma nominato nei record`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} file senza alcun programma nominato nei record`,
  'Summary.OperationFiles': `{0} di {1} {2}`,
  'Summary.OrphanedWindow': `{0} {1} da eliminare ({2})`,
  'Summary.RegisteredWindow.Singular': `{0} file non toccato ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} file non toccati ({1})`,
  'Confirm.MoveTitle': `Vuoi spostare {0} {1} ({2})?`,
  'Confirm.DeleteTitle': `Vuoi eliminare {0} {1} ({2})?`,
  'Error.AdminRequiredTitle': `Accesso negato`,
  'Error.AdminRequiredBody': `Windows ha negato l'accesso a InstallerClean, che si è quindi fermato. Non è stato rimosso nulla.\n\nInstallerClean era già in esecuzione come amministratore, quindi riavviarlo in quel modo non serve. Windows non dice altro su che cosa abbia negato l'accesso, quindi non c'è nulla di specifico da provare.`,
  'Error.StoppedTitle': `InstallerClean si è fermato`,
  'Error.ScanFailedTitle': `Scansione non riuscita`,
  'Error.NothingRemoved': `Non è stato rimosso nulla.`,
  'Error.NothingOfferedNothingRemoved': `Nessun file è stato indicato come sicuro da eliminare e non è stato rimosso nulla.`,
  'Error.InstallerDbEmpty': `I record di Windows Installer sono tornati completamente vuoti: nemmeno un programma installato o un aggiornamento rivendica un file di installazione nella cache. Su una macchina funzionante questo non succede (perfino un'installazione di Windows appena fatta ne ha qualcuno), quindi o i record sono danneggiati o non è stato possibile leggerli, e una scansione che credesse a questa risposta classificherebbe erroneamente come orfano ogni file in {InstallerFolder}. InstallerClean si è fermato invece di farlo.`,
  'Error.MsiAccessDenied': `Windows Installer ha impedito a InstallerClean di elencare ciò che è installato. InstallerClean era già in esecuzione come amministratore, quindi eseguirlo di nuovo come amministratore non cambia nulla. Senza quell'elenco non c'è modo sicuro di sapere quali file nella cache servono ancora, quindi InstallerClean si è fermato.`,
  'Error.MsiNonSuccess': `Windows Installer non è riuscito a fornire a InstallerClean un elenco leggibile dei programmi installati: ha letto {1} {2}, poi una voce è tornata illeggibile (codice di errore {0}). Invece di lavorare su un elenco letto solo in parte, InstallerClean si è fermato.`,
  'Error.InvalidDestinationTitle': `Destinazione non valida`,
  'Error.DestinationWriteFailedTitle': `Impossibile usare quella cartella di backup`,
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
  'UpdateCheck.Failed.NetworkUnavailable': `Impossibile raggiungere GitHub. Controlla la connessione internet e riprova.`,
  'UpdateCheck.Failed.ServerError': `Il controllo degli aggiornamenti ha ricevuto un errore invece della pagina delle release di GitHub. Riprova più tardi, o apri [la pagina delle release] nel browser.`,
  'UpdateCheck.Failed.ResponseParseError': `Il controllo degli aggiornamenti non è riuscito a capire dalla risposta di GitHub quale sia la versione più recente. Riprova più tardi, o apri [la pagina delle release] nel browser.`,
  'UpdateCheck.Failed.Timeout': `Il controllo è scaduto. La connessione a GitHub potrebbe essere lenta; riprova.`,
  'UpdateCheck.Failed.Unknown': `Il controllo non è riuscito per un motivo sconosciuto. I dettagli sono in {0}, se devi segnalarlo.`,
  'BrowserLaunch.ClipboardOk': `Il link è negli appunti, così puoi incollarlo tu:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean non è riuscito nemmeno a copiare il link negli appunti, eccolo:&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `InstallerClean non è più riuscito a confermare la cartella di backup, quindi si è fermato. Controlla {0}, poi Ripeti scansione e riprova.`,
  'Error.CannotWriteFolder': `Impossibile scrivere in {0}.`,
  'Error.DestinationCollision': `Un file di nome '{0}' è già nella cartella di backup.`,
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
  'Display.Size.TB': `{0} TB`,
  'Display.Size.GB': `{0} GB`,
  'Display.Size.MB': `{0} MB`,
  'Display.Size.KB': `{0} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ListSeparator': `, `,
  'Display.SentenceSeparator': `. `,
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
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Errore: qualcosa sta usando Windows Installer in questo momento, ad esempio Windows Update o un programma in fase di installazione o di rimozione. /m e /d sono bloccati mentre accade. Riprova quando ha finito.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Errore: un'installazione precedente eseguita da Windows Installer non è terminata. /m e /d sono bloccati, quindi InstallerClean non tocca {InstallerFolder}. Riavvia Windows, poi esegui di nuovo il comando. Un'installazione in attesa di un riavvio prosegue dopo il riavvio. Se questo messaggio ricompare, esegui di nuovo il programma di installazione dell'applicazione che si stava installando o rimuovendo, e Windows Installer proporrà di annullare le modifiche apportate dall'installazione non terminata.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Errore: Windows ha una modifica a {InstallerFolder} in attesa del prossimo riavvio ({0}). /m e /d sono bloccati. Riavvia Windows, poi riprova.`,
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
  'Cli.Help.ExitCodeOk': `  0   riuscito: ha fatto quanto richiesto`,
  'Cli.Help.ExitCodeError': `  1   nulla elaborato: l'esecuzione è fallita o è stata rifiutata`,
  'Cli.Help.ExitCodePartial': `  2   parziale: alcuni elaborati, altri no (un errore o un Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  transitorio: qualcosa ha bloccato l'esecuzione (vedi il messaggio)`,
  'Cli.Help.ExitCodeCancelled': `  130 annullato (Ctrl+C)`,
  'Tooltip.ChangeLanguage': `Cambia lingua. Il programma verrà riavviato.`,
  'Automation.ChangeLanguage': `Cambia lingua`,
  'Automation.ChangeLanguage.HelpText': `Il programma verrà riavviato.`,
  'Body.NotScanned.Lead': `Ancora nessuna scansione.`,
  'Body.NotScanned.Why': `Premi Ripeti scansione per cercare in {InstallerFolder} i file di installazione che nessun programma usa più.`,
  'Confirm.MoveSameDrive': `Quella cartella è sulla stessa unità, quindi lo spazio non torna finché non la elimini. Scegli invece una cartella su un'altra unità se vuoi lo spazio subito.`,
  'Error.ScanCorrelationFailed': `InstallerClean non è riuscito a far corrispondere i record di Windows Installer con il contenuto di {InstallerFolder}. Quasi nulla di ciò che i record indicano si trova davvero lì, e quasi nulla di ciò che è lì è nominato da un record, quindi non si è potuto dimostrare che qualche file fosse non necessario.`,
  'Error.CandidateOutsideCache': `Questo file non si trova direttamente all'interno della cartella di Windows Installer; rifiutato per sicurezza.`,
  'Completion.MoveCancelledSummary': `Spostati {0} di {1} {2} in {3} prima dell'annullamento.`,
  'Completion.PermanentDeleteCancelledSummary': `Eliminati definitivamente {0} di {1} {2} prima dell'annullamento.`,
  'Body.PendingReboot.Lead': `Al momento questi file non si possono ripulire.`,
  'Cli.TooManyArguments': `Errore: argomento aggiuntivo imprevisto '{0}'. Se la cartella di destinazione contiene uno spazio, racchiudi l'intero percorso tra virgolette: /m "D:\\My Backup"`,
  'Cli.Help.MoveScheduledNote': `Cartella per utente; esecuzioni pianificate o SYSTEM: /m PERCORSO.`,
  'Error.ScanRecordsUnreadable': `InstallerClean non è riuscito a leggere abbastanza dei record di Windows Installer per essere sicuro di che cosa serva ancora: l'elenco dei programmi installati è tornato incompleto, e leggere gli stessi record direttamente dal registro di sistema ha dato errori a sua volta. Un file potrebbe sembrare orfano solo perché il record che lo nomina era fra quelli illeggibili, quindi InstallerClean si è fermato.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer non ha mai segnalato la fine dell'elenco dei programmi installati: InstallerClean ha letto {2} {3}, poi ha rinunciato dopo {0} voci (ultimo codice di errore {1}). Di un elenco senza fine non ci si può fidare, quindi InstallerClean si è fermato.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer non ha mai segnalato la fine dell'elenco delle patch di un programma: InstallerClean ha letto {2} {3}, poi ha rinunciato dopo {0} voci (ultimo codice di errore {1}). Di un elenco senza fine non ci si può fidare, quindi InstallerClean si è fermato.`,
  'Completion.DonateAsk': `Ogni donazione è molto apprezzata`,
  'About.Link.Guide': `Guida e domande frequenti (FAQ)`,
  'About.Link.ReportProblem': `Segnala un problema`,
  'About.AutoUpdateCheck': `Controlla automaticamente gli aggiornamenti`,
  'Automation.About.Guide.HelpText': `Apre readme su github nel browser.`,
  'Automation.About.ReportProblem.HelpText': `Apre elenco problemi (issue) in github.com nel browser.`,
  'Automation.AutoUpdateCheck.HelpText': `Se selezionata, all'avvio InstallerClean verifica su github se è disponibile una versione più recente.`,
  'Tooltip.MoveSameDrive': `Sposta i file non necessari nella cartella di backup. È sullo stesso disco, quindi non recuperi lo spazio finché non elimini quella cartella.`,
  'Confirm.DeletePermanently.Singular': `Questo file verrà eliminato definitivamente. È un'operazione sicura, ma se vuoi una copia di riserva usa invece Sposta.`,
  'Confirm.DeletePermanently.Plural': `Questi file verranno eliminati definitivamente. È un'operazione sicura, ma se vuoi una copia di riserva usa invece Sposta.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean non è riuscito a far risolvere a Windows il vero percorso di {InstallerFolder}, quindi non si è potuto dimostrare che qualche file fosse al suo interno e nessuno è stato indicato come sicuro da eliminare. Questa scansione non ha trovato nulla perché quel controllo non è riuscito, non perché la cartella sia pulita.`,
  'Automation.Scroll.ProductDetails': `Dettagli del prodotto`,
  'Body.PendingReboot.Other': `Windows Installer ha qualcosa di non terminato, quindi Sposta ed Elimina sono in pausa e InstallerClean non tocca {InstallerFolder}. Quando si è risolto, premi Ripeti scansione e tornano disponibili.`,
  'Cli.TooManyArgumentsNoPath': `Errore: argomento aggiuntivo inatteso '{0}'. /s e /d non accettano altri argomenti, e si può usare una sola opzione per esecuzione.`,
  'Cli.MissingFromDisk.Singular': `Windows ha un record per {0} file che non è in {InstallerFolder}: {1}. Nell'uso quotidiano non crea problemi, ma un aggiornamento o una disinstallazione di quel programma può fallire. Per rimettere il file, ti serve il programma di installazione della versione che hai già. Procuratelo dal produttore del programma ed eseguilo sopra la copia esistente. Una versione più recente non va bene: dovrebbe prima rimuovere quella che hai, ed è proprio il passaggio che ha bisogno di questo file. Nemmeno disinstallare prima funziona, per lo stesso motivo. Questo dovrebbe ripristinare il file e lasciare intatte le tue impostazioni, ma Microsoft non lo garantisce.`,
  'Cli.MissingFromDisk.Plural': `Windows ha record per {0} file che non sono in {InstallerFolder}: {1}. Nell'uso quotidiano non creano problemi, ma un aggiornamento o una disinstallazione di quei programmi può fallire. Per rimettere un file, ti serve il programma di installazione della versione di quel programma che hai già. Procuratelo dal produttore del programma ed eseguilo sopra la copia esistente. Una versione più recente non va bene: dovrebbe prima rimuovere quella che hai, ed è proprio il passaggio che ha bisogno del file. Nemmeno disinstallare prima funziona, per lo stesso motivo. Questo dovrebbe ripristinare il file e lasciare intatte le tue impostazioni, ma Microsoft non lo garantisce.`,
  'Cli.MoveNotEnoughSpace': `Errore: spazio insufficiente in {0}. Spostare questi file richiede {1} e ne sono liberi {2}. Non è stato spostato nulla.`,
  'Cli.PendingRebootBlocked.Other': `Errore: Windows Installer ha qualcosa di non terminato, quindi /m e /d sono bloccati e InstallerClean non tocca {InstallerFolder}. Riprova quando si è risolto.`,
  'Cli.ScanSignal.Busy': `All'avvio della scansione, Windows Installer era occupato, ad esempio con Windows Update o con un programma in fase di installazione o di rimozione, perciò InstallerClean non ha toccato i file che non può controllare mentre Windows Installer è occupato. Esegui di nuovo il comando quando Windows Installer ha finito.`,
  'Cli.ScanSignal.Suspended': `All'avvio della scansione, un'installazione precedente eseguita da Windows Installer non era terminata, perciò InstallerClean non ha toccato i file che non può controllare finché non sarà terminata. Riavvia Windows, poi esegui di nuovo il comando. Un'installazione in attesa di un riavvio prosegue dopo il riavvio. Se questo messaggio ricompare, esegui di nuovo il programma di installazione dell'applicazione che si stava installando o rimuovendo, e Windows Installer proporrà di annullare le modifiche apportate dall'installazione non terminata.`,
  'Cli.ScanSignal.RenameInCache': `All'avvio della scansione, Windows aveva una modifica a {InstallerFolder} in attesa del prossimo riavvio, perciò InstallerClean non ha toccato i file che non può controllare finché quella modifica non viene applicata. Riavvia Windows, poi esegui di nuovo il comando.`,
  'Cli.ScanSignal.RenameUnresolved': `All'avvio della scansione, Windows aveva una modifica ai file in attesa del prossimo riavvio, e InstallerClean non è riuscito a capire quali file riguarda, perciò non ha toccato i file che non può controllare finché quella modifica non viene applicata. Riavvia Windows, poi esegui di nuovo il comando.`,
  'Cli.ScanSignal.AccessRefused': `All'avvio della scansione, Windows ha negato a InstallerClean il permesso di controllare se Windows Installer era occupato, perciò InstallerClean non ha toccato i file che non può controllare mentre Windows Installer è occupato. Riavvia Windows, poi esegui di nuovo il comando.`,
  'Cli.ScanSignal.RegistryUnreadable': `All'avvio della scansione, InstallerClean non è riuscito a leggere un'impostazione di Windows che controlla, quindi non ha potuto sapere se Windows Installer era occupato o aveva lavoro in attesa di un riavvio. Non ha toccato i file che non può controllare mentre Windows Installer è occupato. Riavvia Windows, poi esegui di nuovo il comando.`,
  'Cli.ScanSignal.LockUnreadable': `All'avvio della scansione, InstallerClean non è riuscito a controllare se Windows Installer era occupato, perciò non ha toccato i file che non può controllare mentre Windows Installer è occupato. Riavvia Windows, poi esegui di nuovo il comando.`,
  'Cli.ScanSignal.LockUnreadableWindowsError': `All'avvio della scansione, InstallerClean non è riuscito a controllare se Windows Installer era occupato. Windows ha restituito un errore: {0} (errore {1}). InstallerClean non ha toccato i file che non può controllare mentre Windows Installer è occupato. Riavvia Windows, poi esegui di nuovo il comando.`,
  'Cli.ScanSignal.Other': `All'avvio della scansione, Windows Installer aveva qualcosa di non terminato, perciò InstallerClean non ha toccato i file che non può controllare finché Windows Installer non ha finito. Esegui di nuovo il comando quando Windows Installer ha finito.`,
  'Cli.ScanSignal.ClockSetBack': `L'orologio di questo PC è tornato indietro o ha perso tempo durante la scansione, perciò InstallerClean non poteva sapere quali file erano appena stati aggiunti e non ha toccato nessun file che potesse essere stato appena aggiunto. Esegui di nuovo il comando.`,
  'Cli.FoundNoOrphans': `Nessun file non necessario identificato.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean non è più riuscito a confermare la cartella di backup, quindi si è fermato. Controlla {0}, poi esegui di nuovo il comando.`,
  'Cli.Help.Summary': `Rimuove i file .msi/.msp in cache che nessun programma installato usa più.`,
  'Cli.Help.Elevation': `Richiede un prompt come amministratore; senza, non esegue la scansione.`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean non è riuscito a far corrispondere i record di Windows Installer con il contenuto di {InstallerFolder}. La cartella contiene file, ma nemmeno un record indica qualcosa al suo interno, quindi non si è potuto dimostrare che qualche file fosse non necessario.`,
  'Error.ScanInstallerFolderElsewhere': `InstallerClean non può eseguire la scansione di {InstallerFolder} su questo PC. Windows indica questa cartella come {0}, e InstallerClean esegue la scansione solo di una cartella Installer che Windows indica nel suo stesso percorso.`,
  'Completion.NothingOffered': `Nessun file può essere eliminato`,
  'Completion.HeldBack.Singular': `{0} file non toccato. La scansione lo indicava come sicuro da eliminare, ma il controllo finale prima dello spostamento o dell'eliminazione non ha potuto confermarlo.`,
  'Completion.HeldBack.Plural': `{0} file non toccati. La scansione li indicava come sicuri da eliminare, ma il controllo finale prima dello spostamento o dell'eliminazione non ha potuto confermarlo.`,
  'Body.PendingReboot.PendingRenameUnresolved': `Windows ha una modifica ai file in attesa del prossimo riavvio, e InstallerClean non riesce a capire quali file riguarda, quindi non può escludere che siano in {InstallerFolder}. Sposta ed Elimina sono in pausa. Riavvia Windows. InstallerClean controllerà di nuovo la prossima volta che lo avvii.`,
  'Completion.MoveRestoreHint': `Elimina quella cartella quando sei convinto che vada tutto bene.`,
  'Completion.MoveRestoreHintSameDrive': `Elimina quella cartella quando sei convinto che vada tutto bene. Solo allora recuperi davvero lo spazio.`,
  'Confirm.MoveDestination.Singular': `Questo file verrà spostato in:`,
  'Confirm.MoveDestination.Plural': `Questi file verranno spostati in:`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `Errore: Windows ha una modifica ai file in attesa del prossimo riavvio, e InstallerClean non riesce a capire quali file riguarda, quindi non può escludere che siano in {InstallerFolder}. /m e /d sono bloccati. Riavvia Windows, poi riprova.`,
  'Cli.MoveRestoreHint': `Verifica che i tuoi programmi si aggiornino e si disinstallino ancora normalmente, poi elimina {0}.`,
  'Error.ScanStoppedDetails': `Questo viene registrato anche in {0}.`,
  'Body.PendingReboot.RegistryCheckUnreadable': `InstallerClean non è riuscito a leggere una delle impostazioni di Windows che controlla prima di toccare {InstallerFolder}, quindi non può sapere se Windows Installer è occupato o ha lavoro in attesa di un riavvio. Sposta ed Elimina sono in pausa. Riavvia Windows. InstallerClean controllerà di nuovo la prossima volta che lo avvii. Se l'impostazione continua a non essere leggibile, questo non è un computer che InstallerClean possa pulire.`,
  'Cli.InstallerLockAccessRefused': `Errore: Windows ha negato a InstallerClean il permesso di controllare se Windows Installer fosse occupato, quindi non ha potuto escludere che un file servisse a metà strada. Non è stato eliminato nulla.`,
  'Cli.MoveCancelledRestoreHint': `Annullarlo è semplice. Rispostali da {0} in {InstallerFolder} e tornerà tutto com'era.`,
  'Cli.MoveInstallerLockAccessRefused': `Errore: Windows ha negato a InstallerClean il permesso di controllare se Windows Installer fosse occupato, quindi non ha potuto escludere che un file servisse a metà strada. Non è stato spostato nulla.`,
  'Cli.PendingRebootBlocked.RegistryCheckUnreadable': `Errore: InstallerClean non è riuscito a leggere uno dei valori di registro che controlla prima di toccare {InstallerFolder}, quindi non può sapere se Windows Installer è occupato o ha lavoro in attesa di un riavvio. /m e /d sono bloccati. Riavvia Windows e riprova. Se la lettura continua a non riuscire, questo non è un computer che InstallerClean possa pulire.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutexUnreadable': `Errore: InstallerClean non è riuscito a controllare se Windows Installer è occupato, quindi non può escludere un'installazione in corso. /m e /d sono bloccati. Riavvia Windows e riprova. Se il controllo continua a non riuscire, questo non è un computer che InstallerClean possa pulire.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutexUnreadableWindowsError': `Errore: InstallerClean non è riuscito a controllare se Windows Installer è occupato, perché Windows ha restituito un errore: {0} (errore {1}), quindi non può escludere un'installazione in corso. /m e /d sono bloccati. Riavvia Windows e riprova. Se il controllo continua a non riuscire, questo non è un computer che InstallerClean possa pulire.`,
  'Completion.MoveCancelledRestoreHint': `Annullarlo è semplice. Rispostali in {InstallerFolder} e tornerà tutto com'era.`,
  'Error.MoveStoppedTitle': `Spostamento interrotto`,
  'Field.NoNamedProduct': `(nessun programma)`,
  'Summary.RegisteredWindow.Missing.Plural': `{0} mancanti`,
  'Summary.RegisteredWindow.Missing.Singular': `{0} mancante`,
  'UpdateCheck.Failed.Unknown.NoLog': `Il controllo non è riuscito per un motivo sconosciuto. Non è stato possibile scrivere il file crash.log.`,
  'Startup.AdminRightsNeededTitle': `Servono i diritti di amministratore`,
  'Startup.AdminRightsNeededBody': `InstallerClean ha bisogno dei diritti di amministratore per vedere tutti i programmi installati su questo PC e non è in esecuzione come amministratore, quindi non eseguirà la scansione. Non è stato modificato nulla.\n\nEsegui di nuovo InstallerClean come amministratore.`,
  'Cli.AdminRightsNeeded': `InstallerClean ha bisogno dei diritti di amministratore per vedere tutti i programmi installati su questo PC e non è in esecuzione come amministratore, quindi non eseguirà la scansione. Non è stato modificato nulla. Eseguilo di nuovo da un prompt come amministratore.`,
  'Error.ScanInstallerFolderNotFound': `Windows segnala che {InstallerFolder} non esiste su questo PC, quindi InstallerClean si è fermato.`,
  'Error.ScanInstallerFolderListFailed': `Windows ha segnalato un errore quando InstallerClean ha provato a elencare i file in {InstallerFolder}, quindi non è riuscito a ottenerne l'elenco completo e si è fermato.`,
  'Body.RescanNotFinished.Lead': `Niente da mostrare fino alla prossima scansione.`,
  'Body.RescanNotFinished.Why': `La scansione eseguita dopo Sposta o Elimina non è terminata, quindi non c'è nessun elenco da mostrare. Premi Ripeti scansione per eseguire di nuovo la scansione di {InstallerFolder}.`,
  'Body.RescanNotFinished.Recorded': `Il motivo dell'interruzione è registrato in {0}.`,
  'Status.MatchingCount': `{0} di {1}`,
  'Status.WaitingForDrive': `In attesa che l'unità {0} risponda...`,
  'Status.WaitingForPath': `In attesa che {0} risponda...`,
  'Action.StopWaiting': `Continua _senza attendere`,
  'Automation.StopWaitingForDrive': `Continua senza attendere, unità {0}`,
  'Automation.StopWaitingForPath': `Continua senza attendere, {0}`,
  'Status.StopWaitingLeavesAlone': `Ogni file che InstallerClean non può controllare senza questa posizione non verrà toccato.`,
  'Automation.WaitingLineWithStopKey': `{0} Premi Alt+{1} per continuare senza attendere. {2}`,
  'Summary.SourceGivenUp.Drive': `InstallerClean ha smesso di attendere l'unità {0}, perciò non ha toccato i file che senza quell'unità non poteva controllare. Premi Ripeti scansione quando torna a rispondere normalmente.`,
  'Summary.SourceGivenUp.Path': `InstallerClean ha smesso di attendere {0}, perciò non ha toccato i file che senza quella posizione non poteva controllare. Premi Ripeti scansione quando torna a rispondere normalmente.`,
  'Summary.SourcesGivenUp': `InstallerClean ha smesso di attendere più di un'unità o condivisione ({0}), perciò non ha toccato i file che senza di esse non poteva controllare. Premi Ripeti scansione quando tornano a rispondere normalmente.`,
  'Summary.ScanSignal.Busy': `All'avvio della scansione, Windows Installer era occupato, ad esempio con Windows Update o con un programma in fase di installazione o di rimozione, perciò InstallerClean non ha toccato i file che non può controllare mentre Windows Installer è occupato. Premi Ripeti scansione quando Windows Installer ha finito.`,
  'Summary.ScanSignal.Suspended': `All'avvio della scansione, un'installazione precedente eseguita da Windows Installer non era terminata, perciò InstallerClean non ha toccato i file che non può controllare finché non sarà terminata. Riavvia Windows. Un'installazione in attesa di un riavvio prosegue dopo il riavvio, e InstallerClean controllerà di nuovo la prossima volta che lo avvii. Se questo messaggio ricompare, esegui di nuovo il programma di installazione dell'applicazione che si stava installando o rimuovendo, e Windows Installer proporrà di annullare le modifiche apportate dall'installazione non terminata.`,
  'Summary.ScanSignal.RenameInCache': `All'avvio della scansione, Windows aveva una modifica a {InstallerFolder} in attesa del prossimo riavvio, perciò InstallerClean non ha toccato i file che non può controllare finché quella modifica non viene applicata. Riavvia Windows. InstallerClean controllerà di nuovo la prossima volta che lo avvii.`,
  'Summary.ScanSignal.RenameUnresolved': `All'avvio della scansione, Windows aveva una modifica ai file in attesa del prossimo riavvio, e InstallerClean non è riuscito a capire quali file riguarda, perciò non ha toccato i file che non può controllare finché quella modifica non viene applicata. Riavvia Windows. InstallerClean controllerà di nuovo la prossima volta che lo avvii.`,
  'Summary.ScanSignal.AccessRefused': `All'avvio della scansione, Windows ha negato a InstallerClean il permesso di controllare se Windows Installer era occupato, perciò InstallerClean non ha toccato i file che non può controllare mentre Windows Installer è occupato. Riavvia Windows. InstallerClean controllerà di nuovo la prossima volta che lo avvii.`,
  'Summary.ScanSignal.RegistryUnreadable': `All'avvio della scansione, InstallerClean non è riuscito a leggere un'impostazione di Windows che controlla, quindi non ha potuto sapere se Windows Installer era occupato o aveva lavoro in attesa di un riavvio. Non ha toccato i file che non può controllare mentre Windows Installer è occupato. Riavvia Windows. InstallerClean controllerà di nuovo la prossima volta che lo avvii.`,
  'Summary.ScanSignal.LockUnreadable': `All'avvio della scansione, InstallerClean non è riuscito a controllare se Windows Installer era occupato, perciò non ha toccato i file che non può controllare mentre Windows Installer è occupato. Riavvia Windows. InstallerClean controllerà di nuovo la prossima volta che lo avvii.`,
  'Summary.ScanSignal.LockUnreadableWindowsError': `All'avvio della scansione, InstallerClean non è riuscito a controllare se Windows Installer era occupato. Windows ha restituito un errore: {0} (errore {1}). InstallerClean non ha toccato i file che non può controllare mentre Windows Installer è occupato. Riavvia Windows. InstallerClean controllerà di nuovo la prossima volta che lo avvii.`,
  'Summary.ScanSignal.Other': `All'avvio della scansione, Windows Installer aveva qualcosa di non terminato, perciò InstallerClean non ha toccato i file che non può controllare finché Windows Installer non ha finito. Premi Ripeti scansione quando Windows Installer ha finito.`,
  'Summary.ScanSignal.ClockSetBack': `L'orologio di questo PC è tornato indietro o ha perso tempo durante la scansione, perciò InstallerClean non poteva sapere quali file erano appena stati aggiunti e non ha toccato nessun file che potesse essere stato appena aggiunto. Premi Ripeti scansione.`,
  'Display.DriveName': `unità {0}`,
  'Cli.SourceGivenUp.Drive': `InstallerClean ha smesso di attendere l'unità {0}, perciò non ha toccato i file che senza quell'unità non poteva controllare. Quando torna a rispondere normalmente, esegui di nuovo il comando.`,
  'Cli.SourceGivenUp.Path': `InstallerClean ha smesso di attendere {0}, perciò non ha toccato i file che senza quella posizione non poteva controllare. Quando torna a rispondere normalmente, esegui di nuovo il comando.`,
  'Cli.SourcesGivenUp': `InstallerClean ha smesso di attendere più di un'unità o condivisione ({0}), perciò non ha toccato i file che senza di esse non poteva controllare. Quando tornano a rispondere normalmente, esegui di nuovo il comando.`,
  'Action.CloseWithoutDonating': `_Chiudi senza fare una donazione`,
  'Tooltip.Donate': `Dovrei proprio far pagare InstallerClean, perché mi è costato un'enorme quantità di lavoro, ma voglio mantenerlo gratuito perché aiuti quante più persone possibile. La tua donazione ripaga lo sforzo ed è molto apprezzata.`,
  'Cli.DonateAsk': `InstallerClean è gratuito. Se ti è stato utile, una donazione sarà molto apprezzata: {0}`,
  'Completion.SendReport': `Invia rapporto _anonimo`,
  'Completion.ReportPanel.Intro': `Il rapporto aggiunge il tuo risultato a [questo grafico] su GitHub, che mostra quanto spazio le persone stanno recuperando con InstallerClean. Contiene anche questi dettagli tecnici, che mi aiutano a individuare eventuali bug:`,
  'Completion.ReportPanel.Freed': `Quanto spazio è stato liberato e quanti file sono stati spostati o eliminati`,
  'Completion.ReportPanel.Destination': `Se Sposta ha portato i file sulla stessa unità, su un'altra unità, su un'unità rimovibile o su una condivisione di rete`,
  'Completion.ReportPanel.Durations': `Quanto sono durati la scansione e Sposta o Elimina`,
  'Completion.ReportPanel.InstallerFiles': `Quanti file di installazione ci sono di ogni tipo e quanto spazio occupano`,
  'Completion.ReportPanel.LeftAlone': `Quanti file non sono stati toccati e perché`,
  'Completion.ReportPanel.Waits': `Quante volte InstallerClean ha atteso un'unità o una condivisione di rete lenta e quante ha smesso di attendere`,
  'Completion.ReportPanel.Records': `Di quanti programmi e patch Windows ha un record`,
  'Completion.ReportPanel.ShortNames': `Se Windows crea ancora i nomi di file brevi vecchio stile`,
  'Completion.ReportPanel.Windows': `Se Windows è 10 o 11 e x64 o Arm, e in quale lingua e con quale paese o area geografica è impostato`,
  'Completion.ReportPanel.AppVersion': `La versione di InstallerClean, come l'hai ottenuto (setup, portable, Scoop, lo strumento da riga di comando da solo, compilato dal codice sorgente, o sconosciuto), se l'esecuzione è avvenuta in InstallerClean o nello strumento da riga di comando, e la lingua in cui era visualizzato`,
  'Completion.ReportPanel.Errors': `Il conteggio di qualsiasi cosa sia andata storta`,
  'Completion.ReportPanel.ReportId': `Un codice casuale creato per questo rapporto, in modo che venga contato una sola volta se viene inviato più di una volta`,
  'Completion.ReportPanel.Closing': `A parte quel codice, ognuno di questi è un numero o un'etichetta. Niente nel rapporto indica te, il tuo PC, i tuoi file o i tuoi programmi, e non vedo mai il tuo indirizzo IP.`,
  'Automation.ReportInfo': `Cosa contiene il rapporto`,
  'Automation.Scroll.ReportContents': `Cosa contiene il rapporto`,
  'Completion.ReportPanel.SeeReport': `Vedi esattamente cosa viene inviato`,
  'Window.Report.Title': `Il rapporto anonimo`,
  'Window.Report.Unreadable': `InstallerClean non è riuscito a leggere il rapporto.`,
  'Automation.Scroll.Waiting': `In attesa di un'unità o condivisione`,
  'Completion.RecentChange.Singular': `Un file ({2}) non è stato toccato perché Windows indica che è cambiato dopo l'avvio della scansione. Se qualcosa si stava installando o aggiornando, ripeti la scansione quando ha finito.`,
  'Completion.RecentChange.Plural': `{0} {1} ({2}) non sono stati toccati perché Windows indica che sono cambiati dopo l'avvio della scansione. Se qualcosa si stava installando o aggiornando, ripeti la scansione quando ha finito.`,
  'UpdateNotice.Available': `È disponibile InstallerClean {0}. Hai la versione {1}.`,
  'UpdateNotice.Download': `Scaricalo da GitHub`,
  'Automation.UpdateNotice.Download': `Scaricalo da github`,
  'UpdateNotice.Recommended': `Si consiglia vivamente di usare la versione più recente.`,
  'UpdateNotice.UpdateNow': `Aggiorna ora`,
  'UpdateNotice.Downloading': `Download di InstallerClean {0}...`,
  'UpdateNotice.DownloadFailed': `Non è stato possibile scaricare l'aggiornamento.`,
  'UpdateNotice.InstallFailed': `Non è stato possibile installare l'aggiornamento.`,
  'UpdateNotice.ContinueWithoutUpdating': `_Continua senza aggiornare`,
  'Cli.UpdatePrompt': `Aggiornare ora? Si consiglia vivamente di usare la versione più recente. [{0}/{1}]`,
  'Cli.UpdateAnswerYes': `S`,
  'Cli.UpdateAnswerNo': `N`,
  'Cli.UpdateListsOnlyAfterDelete': `La nuova versione eseguirà la scansione ed elencherà ciò che trova, senza eliminare nulla. Per eliminare, esegui di nuovo /d al termine.`,
  'Cli.UpdateListsOnlyAfterMove': `La nuova versione eseguirà la scansione ed elencherà ciò che trova, senza spostare nulla. Per spostare, esegui di nuovo /m al termine.`,
  'Cli.UpdateRunningInstead': `Si esegue invece {0} con InstallerClean {1}.`,
  'Cli.UpdateDownloadLink': `{0}: {1}`,
  'Cli.Help.UpdateNote': `Prima della scansione controlla se esiste una versione più recente di&#10;InstallerClean, a meno che l'opzione “Controlla automaticamente gli&#10;aggiornamenti” sia disattivata per l'account con cui viene eseguito. Se la&#10;sua finestra è visibile sullo schermo e nulla è reindirizzato in ingresso&#10;o in uscita, chiede prima di aggiornare, e si aggiorna se nessuno risponde&#10;entro un minuto. In qualsiasi altro caso, per esempio un'operazione&#10;pianificata impostata per essere eseguita con o senza utente connesso, si&#10;aggiorna senza chiedere.&#10;Per saltare il controllo aggiungi '/noupdate'.`,
  'UpdateNotice.Scoop': `Questa copia è stata installata da Scoop. Per aggiornarla, chiudi InstallerClean ed esegui:`,
  'UpdateNotice.ScoopGlobal': `Questa copia è stata installata da Scoop per tutti gli utenti. Per aggiornarla, chiudi InstallerClean, apri un terminale come amministratore ed esegui:`,
  'UpdateNotice.CommandCopied': `Copiato`,
  'UpdateNotice.CopyFailed': `Impossibile copiarlo. Seleziona il comando e copialo a mano.`,
  'Automation.UpdateNotice.CopyCommand': `Copia il comando`,
  'Automation.UpdateNotice.ScoopCommand': `Comando di aggiornamento di Scoop`,
  'Cli.UpdateInUse': `InstallerClean è in uso, quindi ora non può aggiornarsi.`,
  'Error.FolderWillNotGiveFilesBack': `Puoi salvare file in {0} ma non riaprirli, quindi InstallerClean non sposterà file lì.`,
  'UpdateNotice.NeedsNetFramework48': `Questa versione richiede .NET Framework 4.8. Installalo dal sito di Microsoft, riavvia il PC, poi apri InstallerClean per aggiornare.`,
  'UpdateNotice.CannotInstallAutomatically': `InstallerClean {0} non può essere installato automaticamente.`,
  'UpdateNotice.CopyCannotTell': `InstallerClean non riesce a stabilire se è stato installato dal suo programma di installazione, quindi non può aggiornarsi.`,
  'UpdateNotice.DownloadNetFramework48': `Scarica .NET Framework 4.8 da Microsoft`,
  'Automation.UpdateNotice.DownloadNetFramework48': `Scarica .NET Framework 4.8 da Microsoft`,
  'Cli.UpdateNeedsNetFramework48': `Questa versione richiede .NET Framework 4.8. Installalo dal sito di Microsoft, riavvia il PC, poi esegui di nuovo InstallerClean per aggiornare.`,
  'Cli.ReportPrompt': `Inviare un rapporto anonimo di questa esecuzione? È salvato in {0}. Digita ? per vedere cosa contiene. [{1}/{2}/?]`,
  'Cli.ReportChartAt': `questo grafico all'indirizzo {0}`,
  'Cli.ReportIntro': `Il rapporto aggiunge il tuo risultato a {0}, che mostra quanto spazio le persone stanno recuperando con InstallerClean. Contiene anche questi dettagli tecnici, che mi aiutano a individuare eventuali bug:`,
  'Cli.ReportInFull': `Il rapporto completo è riportato qui sotto.`,
  'Cli.ReportSent': `Rapporto inviato. Grazie.`,
  'Cli.ReportNotSent': `Rapporto non inviato.`,
  'Cli.ReportCouldNotBeSent': `Non è stato possibile inviare il rapporto. È conservato in {0}.`,
  'Cli.ReportWaiting': `Invio di un rapporto in attesa da un'esecuzione precedente.`,
  'Cli.UpdateRunningNewer': `Si esegue {0} con InstallerClean {1}. Prenderà il posto di questa copia al termine dell'esecuzione.`,
  'Cli.UpdateNotRun': `Non è stato possibile eseguire la versione più recente.`,
  'UpdateNotice.CheckingDownload': `Verifica del download...`,
  'Cli.MutexLostDelete': `Interrotto dopo aver eliminato {0} di {1} {2}: InstallerClean non poteva più essere certo che nessun altro InstallerClean stesse agendo. Esegui di nuovo lo stesso comando. Codice di uscita 2 (parziale).`,
  'Cli.MutexLostMove': `Interrotto dopo aver spostato {0} di {1} {2} in {3}: InstallerClean non poteva più essere certo che nessun altro InstallerClean stesse agendo. Esegui di nuovo lo stesso comando. Codice di uscita 2 (parziale).`,
  'Cli.CoverLostNoWorkDelete': `Interrotto prima di eliminare qualsiasi file: InstallerClean non poteva più essere certo che nessun altro InstallerClean stesse agendo. Esegui di nuovo lo stesso comando. Codice di uscita 75 (transitorio).`,
  'Cli.CoverLostNoWorkMove': `Interrotto prima di spostare qualsiasi file: InstallerClean non poteva più essere certo che nessun altro InstallerClean stesse agendo. Esegui di nuovo lo stesso comando. Codice di uscita 75 (transitorio).`,
  'Cli.UpdateNoAnswer': `Nessuna risposta dopo un minuto, quindi InstallerClean si aggiorna senza chiedere, come quando non c'è nessuno a rispondere.`,
  'Cli.ReportNoAnswer': `Nessuna risposta dopo un minuto.`,
  'Startup.PlantedConfigRefusal': `InstallerClean non si avvia perché accanto ad esso c'è un file chiamato {0}, e InstallerClean non ne include nessuno. Elimina quel file, poi apri di nuovo InstallerClean.\n\n{1}`,
  'Startup.NeedsNetFramework48': `InstallerClean richiede .NET Framework 4.8. Installalo dal sito di Microsoft, riavvia il PC, poi apri di nuovo InstallerClean.`,
  'Cli.NeedsNetFramework48': `InstallerClean richiede .NET Framework 4.8. Installalo dal sito di Microsoft, riavvia il PC, poi esegui di nuovo InstallerClean.`,
  'Window.Notices.Title': `Avvisi di terze parti`,
  'About.Link.Notices': `Avvisi di terze parti`,
  'Automation.About.Notices.HelpText': `Mostra le licenze di caratteri, icone e codice inclusi in InstallerClean.`,
  'Cli.Help.Notices': `  installerclean-cli --notices    Mostra gli avvisi di terze parti`,
  'Startup.NeedsWindows10Version1607': `InstallerClean richiede Windows 10 versione 1607 o successiva, oppure Windows 11.`,
  'Cli.ReportCancelledWhileSending': `Annullato durante l’invio del rapporto. Potrebbe essere arrivato comunque.`,
  'UpdateNotice.Updated': `InstallerClean è stato aggiornato alla versione {0}.`,
  'UpdateNotice.WhatsNew': `Novità`,
  'Automation.UpdateNotice.WhatsNew.HelpText': `Apre le release di InstallerClean su github nel browser, dalla più recente.`,
  'About.Link.WhatsNew': `Novità di questa versione`,
  'Automation.About.WhatsNew.HelpText': `Apre la pagina di questa versione su github nel browser.`,
  'Body.PendingReboot.InstallerServiceRunning': `Windows Installer is still running, as it does for a few minutes after a program has been installed, updated or removed, and for as long as an installer's window is left open. Move and Delete are paused until it has stopped, so InstallerClean won't touch {InstallerFolder}. Then Re-scan and they come back.`,
  'Cli.PendingRebootBlocked.InstallerServiceRunning': `Error: Windows Installer is still running, as it does for a few minutes after a program has been installed, updated or removed, and for as long as an installer's window is left open. /m and /d are blocked until it has stopped. Try again in a few minutes.`,
  'Error.FileLeftInHand': `This file could not be put back in its place. It is whole, in {0}.`,
  'Status.WaitingForInstaller': `Waiting for Windows Installer to finish...`,
  'Status.CheckingAgain': `Checking the remaining files again...`,
  'Error.DeleteStoppedTitle': `Delete stopped`,
  'Error.BatchEnded.RecordsKeptChanging': `InstallerClean stopped because Windows Installer's records kept changing while it checked the files, which happens while programs are being installed or updated. Files already moved or deleted stay so. The rest were not touched.`,
  'Error.BatchEnded.WatchUnavailable': `InstallerClean stopped because Windows would not let it watch the registry keys where Windows Installer keeps its records, so it cannot tell whether they change while it works. Files already moved or deleted stay so. The rest were not touched.`,
  'Error.BatchEnded.InHandFolderUnavailable': `InstallerClean could not make its own folder, {0}, inside the Installer folder (Windows error {1}), so it did not move or delete anything.`,
  'Cli.WaitingForInstaller': `Waiting for Windows Installer to finish. InstallerClean carries on by itself when it has.`,
  'Cli.CheckingAgain': `Checking the remaining files again.`,
  'Cli.BatchEnded.WaitBoundReached': `Error: Windows Installer stayed busy for 30 minutes, so InstallerClean stopped. Files already moved or deleted stay so. The rest were not touched. Run the command again later to carry on.`,
  'Cli.BatchEnded.RecordsKeptChanging': `Error: Windows Installer's records kept changing while InstallerClean checked the files, which happens while programs are being installed or updated, so it stopped. Files already moved or deleted stay so. The rest were not touched. Try again once the installations have finished.`,
  'Cli.BatchEnded.WatchUnavailable': `Error: Windows would not let InstallerClean watch the registry keys where Windows Installer keeps its records, so it cannot tell whether they change while it works, and it stopped. Files already moved or deleted stay so. The rest were not touched.`,
  'Cli.BatchEnded.InHandFolderUnavailable': `Error: InstallerClean could not make its own folder, {0}, inside {InstallerFolder} (Windows error {1}), so it did not move or delete anything.`,
  'Cli.BatchEnded.AccessRefused': `Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it stopped. Files already moved or deleted stay so. The rest were not touched.`,
  'Status.WaitingForUnfinishedInstall': `Waiting for an installation that has not finished. If nothing is being installed, cancel and restart Windows.`,
  'Cli.ScanPutBack': `This scan put back into {InstallerFolder} files an earlier Move or Delete had left in InstallerClean's own folder there: {0}`,
  'Summary.PutBack': `A scan has put back into {InstallerFolder} files an earlier Move or Delete had left in InstallerClean's own folder there: {0}`,
  'Summary.LeftInHand': `This scan couldn't put back into {InstallerFolder} these files an earlier Move or Delete had left in InstallerClean's own folder there: {0}. InstallerClean tries again each time it scans.`,
  'Summary.LeftAsSource': `Files an earlier Move or Delete had left in InstallerClean's own folder inside {InstallerFolder} stay there, because a program or update was installed from them where they are: {0}`,
  'Cli.ScanLeftInHand': `This scan couldn't put back into {InstallerFolder} these files an earlier Move or Delete had left in InstallerClean's own folder there: {0}. InstallerClean tries again each time it scans.`,
  'Cli.ScanLeftAsSource': `Files an earlier Move or Delete had left in InstallerClean's own folder inside {InstallerFolder} stay there, because a program or update was installed from them where they are: {0}`,
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
