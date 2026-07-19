# InstallerClean in Italiano (Italian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Italian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Italian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. The translation file itself is [`Strings.it.resx`](../../src/InstallerClean.Core/Resources/Strings.it.resx). This page is generated from it by `scripts/gen-translation-table.mjs`, so do not edit it by hand.

## Window titles

| English | Italiano |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Informazioni |
| Registered files that should not be deleted | File registrati che non andrebbero eliminati |
| Unneeded files that are safe to delete | File non necessari, sicuri da eliminare |
| Confirm move | Conferma spostamento |
| Confirm delete | Conferma eliminazione |
| Recycle Bin unavailable | Cestino non disponibile |

## Section headings

| English | Italiano |
| --- | --- |
| PRODUCTS | PRODOTTI |
| PATCHES | PATCH |
| PRODUCT DETAILS | DETTAGLI PRODOTTO |
| MOVE LOCATION | DESTINAZIONE SPOSTAMENTO |
| SAY THANKS | PER RINGRAZIARMI |

## Buttons and actions

| English | Italiano |
| --- | --- |
| _About | _Informazioni |
| Copy | Copia |
| Cut | Taglia |
| Paste | Incolla |
| Select all | Seleziona tutto |
| _Browse... | S_foglia... |
| _Cancel | _Annulla |
| Check for _updates | Controlla _aggiornamenti |
| _Close | _Chiudi |
| _Delete | _Elimina |
| _Delete permanently | _Elimina definitivamente |
| _Done | _Fatto |
| Details | Dettagli |
| _Buy me a cuppa | _Offrimi un caffè |
| Leave a _star on GitHub | Lascia una _stella su GitHub |
| MIT licence | Licenza MIT |
| _Move | _Sposta |
| _Move instead | _Sposta invece |
| Path to folder if you Move instead of Delete | Percorso cartella, se scegli 'Sposta' anziché 'Elimina' |
| Open _release page | Apri pagina _release |
| _Re-scan | _Ripeti scansione |
| _Scan again | _Nuova scansione |
| Send report | Invia rapporto |
| _Send | _Invia |

## Field labels

| English | Italiano |
| --- | --- |
| Reason | Motivo |
| Author | Autore |
| Application | Applicazione |
| Title | Titolo |
| Subject | Oggetto |
| Keywords | Parole chiave |
| Signing certificate | Certificato firma |
| File size | Dimensione file |
| Comment | Commento |
| Product name | Nome prodotto |
| File | File |
| Size | Dimensione |
| Patches | Patch |
| (unknown) | (sconosciuto) |
| (patches only) | (solo patch) |
| missing | mancante |

## Status and progress

| English | Italiano |
| --- | --- |
| Scanning... | Scansione... |
| Cancelling... | Annullamento... |
| Starting scan... | Avvio scansione... |
| Asking Windows about installed software... | Richiesta a Windows elenco software installati... |
| Scanning installer cache folder... | Scansione cartella cache di installazione... |
| Enumerating installed products... | Enumerazione prodotti installati... |
| Checking registry for additional packages... | Controllo registro per altri pacchetti... |
| Found {0} registered {1}. | Trovati {0} {1} registrati. |
| Scan complete ({0}) | Scansione completata ({0}) |
| Scanning local packages... | Scansione pacchetti locali... |
| Found {0} {1} you can safely delete. | {0} {1} che puoi eliminare in sicurezza. |
| Preparing destination folder... | Preparazione cartella destinazione... |
| Moving {0} {1}... | Spostamento di {0} {1}... |
| Deleting {0} {1}... | Eliminazione di {0} {1}... |
| Move cancelled. {0} of {1} {2} processed. | Spostamento annullato dopo {0} di {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Eliminazione annullata dopo {0} di {1} {2}. |
| Move failed ({0}). Details in {1}. | Spostamento non riuscito ({0}). Dettagli in {1}. |
| Move failed ({0}). The crash log could not be written. | Spostamento non riuscito ({0}). Non è stato possibile scrivere il file crash.log. |
| Delete failed ({0}). Details in {1}. | Eliminazione non riuscita ({0}). Dettagli in {1}. |
| Delete failed ({0}). The crash log could not be written. | Eliminazione non riuscita ({0}). Non è stato possibile scrivere il file crash.log. |
| Access denied. Windows refused the scan. | Accesso negato. Windows ha rifiutato la scansione. |
| Scan failed: couldn't read the Windows Installer records. | Scansione non riuscita: impossibile leggere i record di Windows Installer. |
| Scan cancelled. | Scansione annullata. |
| Ready | Pronto |
| Scan failed ({0}). Details in {1}. | Scansione non riuscita ({0}). Dettagli in {1}. |
| Scan failed ({0}). The crash log could not be written. | Scansione non riuscita ({0}). Non è stato possibile scrivere il file crash.log. |

## Main screen text

| English | Italiano |
| --- | --- |
| Any unneeded files below are safe to delete. | Eventuali file non necessari qui sotto si possono eliminare senza rischi. |
| They sit in C:\Windows\Installer, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Si trovano in C:\Windows\Installer, lasciati indietro quando un programma è stato disinstallato ({0}), una patch più recente ne ha sostituito uno ({1}) o l'editore lo ha ritirato ({2}). InstallerClean elenca sempre e solo i file che Windows stesso segnala come non più necessari. |
| Delete them to the Recycle Bin, or use Move instead to keep a backup. Putting the files back in C:\Windows\Installer returns you to exactly where you started. | Eliminali nel Cestino, oppure usa invece Sposta per tenerne una copia di backup. Rimettere i file in C:\Windows\Installer riporta tutto esattamente com'era. |
| Nothing scanned yet. | Ancora nessuna scansione. |
| Press Re-scan to look through C:\Windows\Installer for installer files that no program still needs. | Premi Ripeti scansione per cercare in C:\Windows\Installer i file di installazione che nessun programma usa più. |
| These files can't be cleaned up right now. | Al momento questi file non si possono ripulire. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch the installer cache while it's changing. Once it's done, Re-scan and they come back. | In questo momento qualcosa sta usando Windows Installer, di solito un aggiornamento di Windows o un programma che si sta installando in background. Mentre questo avviene 'Sposta' ed 'Elimina' sono in pausa, così InstallerClean non tocca la cache di installazione quando ci sono modifiche in corso. Ad attività completate ripeti la scansione e torneranno disponibili. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | In questo computer è in sospeso una precedente transazione di Windows Installer. Prima di pulire la cache riprendi o annulla quell'installazione (o riavvia Windows). |
| Windows has a file rename queued for the next restart that affects the Installer cache. Restart Windows before cleaning. | Windows ha in coda, per il prossimo riavvio, la ridenominazione di un file che riguarda la cache di installazione. Prima di pulire riavvia Windows. |
| Select a file to view details. | Seleziona un file per vederne i dettagli. |
| Select a product to view details. | Seleziona un prodotto per vederne i dettagli. |
| No metadata available. | Nessun metadato disponibile. |
| This installer file has been deleted. InstallerClean didn't do it, it never removes a file a program still needs; something else deleted this one before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | Questo file di installazione è stato eliminato. Non è stato InstallerClean: non rimuove mai un file che un programma usa ancora; qualcos'altro lo ha eliminato prima che tu eseguissi InstallerClean.<br><br>Per ora non crea problemi, e non ne creerà fino al giorno in cui proverai a riparare, aggiornare o disinstallare il programma a cui appartiene. Quel passaggio può allora non riuscire, perché Windows cerca questo file e non lo trova.<br><br>Per provare a risolvere, scarica il programma di installazione di quel software dal suo produttore ed eseguilo sopra la copia esistente (non disinstallare prima: la disinstallazione è essa stessa un passaggio che richiede questo file). Usa la stessa versione che hai installato, se riesci a procurartela, perché Windows potrebbe rifiutarne una diversa. Di solito questo ripristina il file, e le tue impostazioni di norma restano intatte, ma Microsoft non lo garantisce: la sua ultima risorsa è reinstallare il programma, o Windows stesso. |
| The README [explains this folder], and how to recover a file, in Microsoft's own words. | Il file README [spiega questa cartella], e come recuperare un file, con i termini di Microsoft. |
| (none) | (nessuna) |

## Reasons a file is unneeded

| English | Italiano |
| --- | --- |
| Orphaned | Orfano |
| Superseded | Sostituito |
| Obsoleted | Obsoleto |

## Completion screen

| English | Italiano |
| --- | --- |
| All clean | Tutto pulito |
| Nothing to clean up in C:\Windows\Installer | Niente da eliminare in C:\Windows\Installer |
| Scanned {0} {1} in {2} | Analizzati {0} {1} in {2} |
| Copy them back to C:\Windows\Installer if anything ever breaks ([extremely unlikely]). | Riportali in C:\Windows\Installer se mai qualcosa smettesse di funzionare ([estremamente improbabile]). |
| Until then, you can restore them if anything ever breaks ([extremely unlikely]). | Fino ad allora, puoi ripristinarli se mai qualcosa smettesse di funzionare ([estremamente improbabile]). |
| Empty it to actually reclaim the space. | Svuotalo per recuperare davvero lo spazio. |
| {0} freed | Liberati {0} |
| {0} cleaned up | Ripuliti {0} |
| {0} moved | Spostati {0} |
| Nothing was moved | Nessun file spostato |
| Nothing was deleted | Nessun file eliminato |
| {0} of {1} files could not be moved. | {0} di {1} file non è stato spostato. |
| {0} of {1} files could not be moved. | {0} di {1} file non sono stati spostati. |
| {0} of {1} files could not be deleted. | {0} di {1} file non è stato eliminato. |
| {0} of {1} files could not be deleted. | {0} di {1} file non sono stati eliminati. |
| {0} {1} moved to: {2} | Spostato {0} {1} in: {2} |
| {0} {1} moved to: {2} | Spostati {0} {1} in: {2} |
| {0} {1} moved to the Recycle Bin | Spostato {0} {1} nel Cestino |
| {0} {1} moved to the Recycle Bin | Spostati {0} {1} nel Cestino |
| {0} {1} kept in place, because a program started needing them again after the scan. | {0} {1} lasciati al loro posto, perché un programma è tornato ad averne bisogno dopo la scansione. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | {0} {1} lasciati al loro posto, perché non è stato possibile leggere completamente i record di Windows Installer quando il controllo è stato ripetuto. |
| Moved {0} of {1} {2} before you cancelled. | Spostati {0} di {1} {2} prima dell'annullamento. |
| Moved {0} of {1} {2} to the Recycle Bin before you cancelled. | Spostati {0} di {1} {2} nel Cestino prima dell'annullamento. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Eliminati definitivamente {0} di {1} {2} prima dell'annullamento. |
| {0} {1} permanently deleted. It did not go to the Recycle Bin. | Eliminato definitivamente {0} {1}. Non è stato spostato nel Cestino. |
| {0} {1} permanently deleted. They did not go to the Recycle Bin. | Eliminati definitivamente {0} {1}. Non sono finiti nel Cestino. |
| That's fine, it was safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Va bene così, si poteva rimuovere senza rischi. InstallerClean elimina solo i file che Windows segnala come non più in uso, mai uno che un programma usa ancora. Nell'improbabile caso in cui un'eliminazione lasciasse un programma incapace di ripararsi, aggiornarsi o disinstallarsi, reinstallarlo dal produttore di solito ripristina il file, anche se Microsoft non lo garantisce. |
| That's fine, they were safe to remove. InstallerClean only clears files Windows reports as finished with, never one a program still needs. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Va bene così, si potevano rimuovere senza rischi. InstallerClean elimina solo i file che Windows segnala come non più in uso, mai uno che un programma usa ancora. Nell'improbabile caso in cui un'eliminazione lasciasse un programma incapace di ripararsi, aggiornarsi o disinstallarsi, reinstallarlo dal produttore di solito ripristina il file, anche se Microsoft non lo garantisce. |

## Recycle Bin unavailable

| English | Italiano |
| --- | --- |
| The Recycle Bin isn't available for this drive | Per questa unità il Cestino non è disponibile |
| So this {1} ({2}) hasn't been deleted. You can move it somewhere safe, or delete it permanently. | Quindi questo {1} ({2}) non è stato eliminato. Puoi spostarlo in un luogo sicuro, oppure eliminarlo definitivamente. |
| So these {0} {1} ({2}) haven't been deleted. You can move them somewhere safe, or delete them permanently. | Quindi questi {0} {1} ({2}) non sono stati eliminati. Puoi spostarli in un luogo sicuro, oppure eliminarli definitivamente. |
| Deleting it is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Eliminarlo è sicuro. InstallerClean elimina solo i file che Windows segnala come non più in uso, mai uno che un programma usa ancora, e il Cestino è soltanto una garanzia in più. Nell'improbabile caso in cui un'eliminazione lasciasse un programma incapace di ripararsi, aggiornarsi o disinstallarsi, reinstallarlo dal produttore di solito ripristina il file, anche se Microsoft non lo garantisce. |
| Deleting them is safe. InstallerClean only clears files Windows reports as finished with, never one a program still needs, and the Recycle Bin is only an extra safeguard. In the unlikely event a deletion ever left a program unable to repair, update or uninstall, reinstalling it from its maker usually restores the file, though Microsoft doesn't guarantee it. | Eliminarli è sicuro. InstallerClean elimina solo i file che Windows segnala come non più in uso, mai uno che un programma usa ancora, e il Cestino è soltanto una garanzia in più. Nell'improbabile caso in cui un'eliminazione lasciasse un programma incapace di ripararsi, aggiornarsi o disinstallarsi, reinstallarlo dal produttore di solito ripristina il file, anche se Microsoft non lo garantisce. |

## Summaries and counts

| English | Italiano |
| --- | --- |
| {0} file still needed | {0} file ancora necessario |
| {0} files still needed | {0} file ancora necessari |
| {0} unneeded file to clean up | {0} file non necessario da eliminare |
| {0} unneeded files to clean up | {0} file non necessari da eliminare |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} file registrato risulta mancante (non eliminato da InstallerClean). Per ora nessun problema, ma in futuro una riparazione, un aggiornamento o una disinstallazione di quel programma potrebbe non riuscire. Per sapere cosa fare apri 'Dettagli'. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} file registrati risultano mancanti (non eliminati da InstallerClean). Per ora nessun problema, ma in futuro una riparazione, un aggiornamento o una disinstallazione di quei programmi potrebbe non riuscire. Per sapere cosa fare apri 'Dettagli'. |
| {0} installed program could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | Non è stato possibile leggere {0} programma installato durante questa scansione, quindi le patch sostituite sono state mantenute. I file orfani non sono interessati. |
| {0} installed programs could not be read during this scan, so superseded patches have been kept. Orphaned files are not affected. | Non è stato possibile leggere {0} programmi installati durante questa scansione, quindi le patch sostituite sono state mantenute. I file orfani non sono interessati. |
| {0} of {1} {2} | {0} di {1} {2} |
| {0} orphaned, {1} superseded, {2} obsoleted ({3}) | {0} orfani, {1} sostituiti, {2} obsoleti ({3}) |
| {0} registered file that is still needed ({1}) | {0} file registrato ancora necessario ({1}) |
| {0} registered files that are still needed ({1}) | {0} file registrati ancora necessari ({1}) |

## Confirmation dialogs

| English | Italiano |
| --- | --- |
| Move {0} {1} ({2})? | Vuoi spostare {0} {1} ({2})? |
| Files will be moved to: | I file verranno spostati in: |
| Delete {0} {1} ({2})? | Vuoi eliminare {0} {1} ({2})? |
| Files will be moved to the Recycle Bin. If you'd like backup copies, use the Move button instead. | I file verranno spostati nel Cestino. Se vuoi fare delle copie di backup, usa invece il pulsante Sposta. |
| This folder is on the same drive, so the move won't free any space by itself. You'll get the space back when you delete the files from it, or you can pick a folder on another drive instead. | Questa cartella è sulla stessa unità, quindi lo spostamento da solo non libera spazio. Recupererai lo spazio quando eliminerai i file da lì, oppure puoi scegliere una cartella su un'altra unità. |

## Error messages

| English | Italiano |
| --- | --- |
| Access denied | Accesso negato |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows ha negato l'accesso a InstallerClean, che si è quindi fermato. Non è stato rimosso nulla.<br><br>InstallerClean era già in esecuzione come amministratore, quindi riavviarlo in quel modo non serve. Windows non dice altro su che cosa abbia negato l'accesso, quindi non c'è nulla di specifico da provare. |
| Couldn't read the Windows Installer records | Impossibile leggere i record di Windows Installer |
| Scan failed | Scansione non riuscita |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in C:\Windows\Installer orphaned. InstallerClean stopped instead. Nothing has been removed. | I record di Windows Installer sono tornati completamente vuoti: nemmeno un programma installato o un aggiornamento rivendica un file di installazione nella cache. Su una macchina funzionante questo non succede (perfino un'installazione di Windows appena fatta ne ha qualcuno), quindi o i record sono danneggiati o non è stato possibile leggerli, e una scansione che credesse a questa risposta classificherebbe erroneamente come orfano ogni file in C:\Windows\Installer. InstallerClean si è fermato invece di farlo. Non è stato rimosso nulla. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer ha impedito a InstallerClean di elencare ciò che è installato. InstallerClean era già in esecuzione come amministratore, quindi eseguirlo di nuovo come amministratore non cambia nulla. Senza quell'elenco non c'è modo sicuro di sapere quali file nella cache servono ancora, quindi InstallerClean si è fermato. Non è stato rimosso nulla. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer non è riuscito a fornire a InstallerClean un elenco leggibile dei programmi installati: {0} voci di seguito sono tornate illeggibili (ultimo codice di errore {1}). Invece di lavorare su un elenco letto solo in parte, InstallerClean si è fermato. Non è stato rimosso nulla. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer non ha mai segnalato la fine dell'elenco dei programmi installati: InstallerClean ha rinunciato dopo {0} voci (ultimo codice di errore {1}). Di un elenco senza fine non ci si può fidare, quindi InstallerClean si è fermato. Non è stato rimosso nulla. |
| Windows Installer couldn't give InstallerClean a readable list of one program's patches: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer non è riuscito a fornire a InstallerClean un elenco leggibile delle patch di un programma: {0} voci di seguito sono tornate illeggibili (ultimo codice di errore {1}). Invece di lavorare su un elenco letto solo in parte, InstallerClean si è fermato. Non è stato rimosso nulla. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer non ha mai segnalato la fine dell'elenco delle patch di un programma: InstallerClean ha rinunciato dopo {0} voci (ultimo codice di errore {1}). Di un elenco senza fine non ci si può fidare, quindi InstallerClean si è fermato. Non è stato rimosso nulla. |
| InstallerClean couldn't square this scan with the Windows Installer records: every file Windows still lists as needed is missing from C:\Windows\Installer, while the files actually in the folder match nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean non è riuscito a far quadrare questa scansione con i record di Windows Installer: ogni file che Windows elenca ancora come necessario manca da C:\Windows\Installer, mentre i file effettivamente presenti nella cartella non corrispondono a nessun record. Nessuna macchina reale si presenta così, quindi questo indica un problema nella lettura dei record, non file che puoi rimuovere senza rischi. Non è stato proposto nulla da eliminare e non è stato rimosso nulla. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean non è riuscito a leggere abbastanza dei record di Windows Installer per essere sicuro di che cosa serva ancora: l'elenco dei programmi installati è tornato incompleto, e leggere gli stessi record direttamente dal registro di sistema ha dato errori a sua volta. Un file potrebbe sembrare orfano solo perché il record che lo nomina era fra quelli illeggibili, quindi InstallerClean si è fermato. Non è stato rimosso nulla. |
| Invalid destination | Destinazione non valida |
| Could not write to destination | Impossibile scrivere nella destinazione |
| Move failed | Spostamento non riuscito |
| Delete failed | Eliminazione non riuscita |
| The destination cannot be inside the Windows Installer folder. | La destinazione non può trovarsi all'interno della cartella di Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | La destinazione {0} si trova in una cartella di sistema di Windows. Scegli un percorso al di fuori di %SystemRoot%, %ProgramFiles% e %ProgramData%. |
| Not enough space | Spazio insufficiente |
| Not enough space at {0}<br><br>Required: {1}<br>Available: {2} | Spazio insufficiente in {0}<br><br>Necessario: {1}<br>Disponibile: {2} |
| You don't have permission to write to {0}.<br>Try a folder in your user profile or on a drive you own. | Non hai i permessi per scrivere in {0}.<br>Prova una cartella nel profilo utente o in un'unità di tua proprietà. |
| The path {0} is too long for Windows. Pick a shorter path. | Il percorso {0} è troppo lungo per Windows. Scegli un percorso più corto. |
| The folder {0} does not exist and could not be created. Check the drive letter or network path. | La cartella {0} non esiste e non è stato possibile crearla. Controlla la lettera dell'unità o il percorso di rete. |
| Windows cannot write to {0}.<br>Details in {1}. | Windows non può scrivere in {0}.<br>Dettagli in {1}. |
| Windows cannot write to {0}. The crash log could not be written. | Windows non può scrivere in {0}. Non è stato possibile scrivere il file crash.log. |
| Cannot write to {0}.<br>Details in {1}. | Impossibile scrivere in {0}.<br>Dettagli in {1}. |
| Cannot write to {0}. The crash log could not be written. | Impossibile scrivere in {0}. Non è stato possibile scrivere il file crash.log. |
| File no longer exists. | Il file non esiste più. |
| Source file is a symlink or junction; refused for safety. | Il file sorgente è un collegamento simbolico o una giunzione; rifiutato per sicurezza. |
| This file is not inside the Windows Installer folder; refused for safety. | Questo file non si trova all'interno della cartella di Windows Installer; rifiutato per sicurezza. |
| Windows refused access to this file; it was left in place. | Windows ha negato l'accesso a questo file; è stato lasciato al suo posto. |
| Windows refused access to these files; they were left in place. | Windows ha negato l'accesso a questi file; sono stati lasciati al loro posto. |
| This file is open or locked by another program, so nothing can move it just now. It was left in place; try again later. | Questo file è aperto o bloccato da un altro programma, quindi al momento nulla può spostarlo. È stato lasciato al suo posto; riprova più tardi. |
| These files are open or locked by another program, so nothing can move them just now. They were left in place; try again later. | Questi file sono aperti o bloccati da un altro programma, quindi al momento nulla può spostarli. Sono stati lasciati al loro posto; riprova più tardi. |
| Windows reported a file error; the file was left in place. | Windows ha segnalato un errore sul file; il file è stato lasciato al suo posto. |
| Windows reported file errors; these files were left in place. | Windows ha segnalato errori sui file; questi file sono stati lasciati al loro posto. |
| Something went wrong with this file; it was left in place. | Qualcosa è andato storto con questo file; è stato lasciato al suo posto. |
| Something went wrong with these files; they were left in place. | Qualcosa è andato storto con questi file; sono stati lasciati al loro posto. |
| Couldn't move this file to the Recycle Bin (error {0}), and InstallerClean can't tell you why from that code. The file was left in place. Try the Move button instead, since it doesn't use the Recycle Bin. | Impossibile spostare questo file nel Cestino (errore {0}), e da quel codice InstallerClean non può dirti perché. Il file è stato lasciato al suo posto. Prova invece il pulsante Sposta, che non usa il Cestino. |
| Windows refused access even with administrator rights (error {0}), and InstallerClean can't tell whether the problem is the file or the Recycle Bin. The file was left in place. The Move button will work if it's the Recycle Bin, but not if it's the file. | Windows ha negato l'accesso anche con i diritti di amministratore (errore {0}), e InstallerClean non può stabilire se il problema sia il file o il Cestino. Il file è stato lasciato al suo posto. Il pulsante Sposta funziona se il problema è il Cestino, ma non se è il file. |
| This file is open or locked by another program (error {0}), so nothing can remove it just now. It was left in place; try again later. | Questo file è aperto o bloccato da un altro programma (errore {0}), quindi al momento nulla può rimuoverlo. È stato lasciato al suo posto; riprova più tardi. |
| Windows deleted this file outright rather than moving it to the Recycle Bin. InstallerClean asked for the Recycle Bin, and Windows did this instead. The file is gone. | Windows ha eliminato definitivamente questo file invece di spostarlo nel Cestino. InstallerClean aveva chiesto il Cestino, e Windows ha fatto altrimenti. Il file non c'è più. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Spostamento dei file nella cartella di Windows Installer rifiutato (destinazione: {0}). |
| The Move location needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | La destinazione dello spostamento deve essere un percorso completo verso una cartella, che inizia con una lettera di unità o una condivisione di rete (per esempio D:\Backup oppure \\server\backup). InstallerClean non può usare questo: {0} |
| The Move location changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | La destinazione dello spostamento è cambiata mentre i file venivano spostati (qualcosa ha sostituito o reindirizzato la cartella), quindi InstallerClean si è fermato invece di scrivere nel posto sbagliato. Controlla {0}, poi ripeti la scansione e riprova. |
| Cannot write to {0}. | Impossibile scrivere in {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Dopo 10.000 tentativi impossibile trovare un nome file univoco per '{0}'. |

## Update check

| English | Italiano |
| --- | --- |
| Check for updates | Controlla aggiornamenti |
| Checking... | Controllo aggiornamenti... |
| Up to date. | Versione aggiornata. |
| Update available | Aggiornamento disponibile |
| You're running version {0}.<br>Version {1} is available. | La versione in uso è la versione {0}.<br>È disponibile la versione {1}. |
| Couldn't reach GitHub. Check your internet connection and try again. | Impossibile raggiungere GitHub. Controlla la connessione internet e riprova. |
| GitHub returned an error response. The releases API may be rate-limited; try again in a few minutes. | GitHub ha restituito una risposta di errore. L'API delle release potrebbe avere un limite di frequenza; riprova tra qualche minuto. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | La risposta di GitHub non conteneva una release riconoscibile. Riprova più tardi, o apri direttamente la pagina delle release. |
| The check timed out. Your connection to GitHub may be slow; try again. | Il controllo è scaduto. La connessione a GitHub potrebbe essere lenta; riprova. |
| The check failed for an unknown reason. Details are in crash.log if you need to report it. | Il controllo non è riuscito per un motivo sconosciuto. I dettagli sono nel file crash.log, se devi segnalarlo. |

## Opening links in your browser

| English | Italiano |
| --- | --- |
| Couldn't open your browser | Impossibile aprire il browser |
| InstallerClean couldn't open your browser. The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | InstallerClean non è riuscito ad aprire il browser. Il link è negli appunti, così puoi incollarlo da te:<br><br>{0} |
| InstallerClean couldn't open your browser, and couldn't copy the link to your clipboard either. The link is:<br><br>{0} | InstallerClean non è riuscito ad aprire il browser e non è riuscito nemmeno a copiare il link negli appunti. Il link è:<br><br>{0} |

## Sending the summary

| English | Italiano |
| --- | --- |
| Sending... | Invio... |
| Thanks! Report sent. | Grazie! Rapporto inviato. |
| Sending failed. Try again later. | Invio non riuscito. Riprova più tardi. |
| No report to send. | Nessun rapporto da inviare. |
| Send this? | Vuoi inviare questo? |
| It goes to nofaff.netlify.app/api/result-log. Nothing identifies you or your machine; it just lets me know InstallerClean's working and [how much space people are freeing]. | Viene inviato a nofaff.netlify.app/api/result-log. Niente ti identifica, né identifica il computer; mi fa solo sapere che InstallerClean funziona e [quanto spazio le persone stanno liberando]. |

## Startup and crashes

| English | Italiano |
| --- | --- |
| InstallerClean | InstallerClean |
| InstallerClean is already running. | InstallerClean è già in esecuzione. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Si è verificato un errore imprevisto e InstallerClean verrà chiuso.<br><br>{0}<br><br>Dettagli salvati in:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Si è verificato un errore imprevisto e InstallerClean verrà chiuso.<br><br>{0}<br><br>Non è stato possibile scrivere il file crash.log. |
| Startup error | Errore in avvio |
| Failed to start ({0}). Details written to:<br>{1} | Avvio non riuscito ({0}). Dettagli salvati in:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Avvio non riuscito ({0}). Non è stato possibile scrivere il file crash.log. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Redact both classes of detail before attaching this file to a<br># public bug report.<br> | # crash.log registra le eccezioni non gestite di InstallerClean.<br># In esecuzione con privilegi elevati, i messaggi di eccezione del<br># framework possono includere percorsi di file della sessione in<br># corso (compresi i profili di altri utenti enumerati dalle query<br># di Windows Installer). I messaggi di errore di rete del controllo<br># aggiornamenti o dell'invio del log dei risultati possono<br># includere l'URL di destinazione e l'indirizzo IP / proxy<br># risolto. Rimuovi entrambi i tipi di dettaglio prima di allegare<br># questo file a una segnalazione di bug pubblica.<br> |

## Tooltips (hover text)

| English | Italiano |
| --- | --- |
| Donate | Dona |
| It's thirsty work! | Ci vuole un caffè! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Annullamento richiesto. InstallerClean sta aspettando che il passaggio in corso arrivi a un punto in cui fermarsi. Può richiedere qualche secondo durante operazioni di I/O intense o una chiamata al database MSI. |
| Close | Chiudi |
| Leave a star on GitHub, report an Issue or post in Discussions. Any feedback welcome. | Lascia una stella su GitHub, segnala un problema (issue) o scrivi nelle discussioni. Ogni feedback è il benvenuto. |
| or report an Issue or post in Discussions. Any feedback welcome. | o segnala un problema (issue) o scrivi nelle discussioni. Ogni feedback è il benvenuto. |
| Minimise | Riduci a icona |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Come preferisci, ma è apprezzato. Invia un riepilogo anonimo che mi fa solo sapere se funziona e quanto spazio le persone stanno liberando. La schermata successiva visualizza cosa verrà inviato prima di confermare. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Come preferisci, ma è apprezzato. Invia un riepilogo anonimo che mi fa solo sapere se funziona. La schermata successiva ti mostra cosa verrà inviato prima di confermare. |
| Move the unneeded files to the Move location. | Sposta i file non necessari nella destinazione spostamento. |
| Move the unneeded files somewhere safe. You'll choose the folder next. | Sposta i file non necessari in un luogo sicuro. Sceglierai la cartella subito dopo. |
| Move the unneeded files to the Recycle Bin. | Sposta i file non necessari nel Cestino. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nome soggetto dal certificato Authenticode incorporato. Catena non verificata. |
| Change language. The program will restart. | Cambia lingua. Il programma verrà riavviato. |

## Screen reader labels

| English | Italiano |
| --- | --- |
| Donate | Dona |
| Buy me a cuppa (About window) | Offrimi un caffè (finestra Informazioni) |
| Cancel operation | Annulla operazione |
| Cancel scan | Annulla scansione |
| Cancel startup scan | Annulla scansione all'avvio |
| Close | Chiudi |
| Close window | Chiudi finestra |
| Close result and return to main window | Chiudi finestra risultato e torna alla finestra principale |
| Leave a star on GitHub | Lascia una stella su GitHub |
| Leave a star on GitHub (About window) | Lascia una stella su GitHub (finestra Informazioni) |
| Minimise | Riduci a icona |
| Move all unneeded installer files to the Move location | Sposta tutti i file di installazione non necessari nella destinazione spostamento |
| Move all unneeded installer files to the Recycle Bin | Sposta tutti i file di installazione non necessari nel Cestino |
| Delete moves the unneeded files to the Recycle Bin. Cancel closes without deleting. | Elimina sposta i file non necessari nel Cestino. Annulla chiude senza eliminare. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | 'Sposta' colloca i file non necessari nella cartella destinazione scelta. 'Annulla' li lascia dove sono. |
| Choose how to handle the unneeded files: move them somewhere safe, delete them permanently or cancel. | Scegli come gestire i file non necessari: spostarli in un luogo sicuro, eliminarli definitivamente o annullare. |
| Move the unneeded files to a folder you choose | Sposta i file non necessari in una specifica cartella |
| Delete the unneeded files permanently because the Recycle Bin is unavailable for this drive | Elimina definitivamente i file non necessari perché per questa unità il Cestino non è disponibile |
| Posts to nofaff.netlify.app. Counts and labels only. You will see the exact payload before sending. | Invia a nofaff.netlify.app. vengono inviati solo conteggi ed etichette. Prima dell'invio vedrai esattamente cosa viene inviato. |
| Say thanks | Per ringraziarmi |
| Send posts the report shown to No Faff. Cancel sends nothing. | 'Invia' trasmette a No Faff il rapporto mostrato. Annulla non invia nulla. |
| Check for updates | Controlla aggiornamenti |
| Checks the GitHub releases API over HTTPS for a newer version. | Verifica tramite l'API delle release di GitHub, via HTTPS, se è disponibile una versione più recente. |
| Open the release page to download the newer version, or cancel to keep the current version. | Apri pagina release per scaricare la versione più recente, o scegli 'Annulla' per mantenere quella attuale. |
| MIT licence | Licenza MIT |
| Opens the licence file on github.com in your browser. | Apre file licenza in github.com nel browser. |
| Move location | Destinazione spostamento |
| Products | Prodotti |
| Patches | Patch |
| Product details | Dettagli prodotto |
| Move location | Destinazione spostamento |
| Operation progress | Avanzamento operazione |
| Scan C:\Windows\Installer again | Nuova scansione di C:\Windows\Installer |
| Scanning progress | Avanzamento scansione |
| Startup scan progress | Avanzamento scansione all'avvio |
| Details, unneeded files | Dettagli, file non necessari |
| Available for cleanup. | Disponibili per la pulizia. |
| Details, registered files | Dettagli, file registrati |
| Read-only inventory. | Elenco sola lettura. |
| Sorted by {0}, ascending | Ordinati per {0}, crescente |
| Sorted by {0}, descending | Ordinati per {0}, decrescente |
| Scan results | Risultati scansione |
| Result details | Dettagli risultato |
| File details | Dettagli file |
| Dialog text | Testo finestra di dialogo |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | File che non è stato possibile elaborare |
| Explains this folder, and how to recover a file, in the README | Spiega questa cartella, e come recuperare un file, nel README |
| Report preview | Anteprima rapporto |
| Change language | Cambia lingua |
| The program will restart. | Il programma verrà riavviato. |

## File picker

| English | Italiano |
| --- | --- |
| Choose destination folder for moved files | Scegli la cartella destinazione per i file spostati |

## Version

| English | Italiano |
| --- | --- |
| Version {0} | Versione {0} |

## Word forms (singular and plural)

| English | Italiano |
| --- | --- |
| file | file |
| files | file |
| error | errore |
| errors | errori |
| package | pacchetto |
| packages | pacchetti |
| product | prodotto |
| products | prodotti |
| patch | patch |
| patches | patch |

## Sizes and times

| English | Italiano |
| --- | --- |
| {0:F2} GB | {0:F2} GB |
| {0:F1} MB | {0:F1} MB |
| {0:F1} KB | {0:F1} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | meno di un secondo |
| {0:F1} seconds | {0:F1} secondi |

## Command-line tool (installerclean-cli)

| English | Italiano |
| --- | --- |
| Unknown argument: '{0}' | Argomento sconosciuto: '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Errore: argomento aggiuntivo imprevisto '{0}'. Se la cartella di destinazione contiene uno spazio, racchiudi l'intero percorso tra virgolette: /m "D:\My Backup" |
| Cancelling... | Annullamento... |
| Cancelled. | Operazione annullata. |
| Error: {0}. Details written to {1}. | Errore: {0}. Dettagli salvati in {1}. |
| Error: {0}. The crash log could not be written. | Errore: {0}. Non è stato possibile scrivere il file crash.log. |
| Scanning C:\Windows\Installer... | Scansione di C:\Windows\Installer... |
| Found {0} {1} to clean up ({2}). | Trovati {0} {1} da eliminare ({2}). |
| Nothing to do. | Nessuna operazione necessaria. |
| Deleting {0} {1}... | Eliminazione di {0} {1}... |
| Deleted {0} {1}. | Eliminati {0} {1}. |
| Error: the Recycle Bin is unavailable for this volume, so nothing was deleted. Use /m to move the files instead, or re-enable the Recycle Bin and run again. | Errore: per questa unità il Cestino non è disponibile, quindi non è stato eliminato nulla. Usa /m per spostare i file oppure riattiva il Cestino e riprova. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Errore: nessuna destinazione di spostamento specificata. Usa /m PERCORSO. (Una destinazione predefinita impostata nella GUI è specifica per utente e non si applica alle esecuzioni pianificate o con account di servizio.) |
| Error: destination cannot be inside the Windows Installer folder. | Errore: la destinazione non può trovarsi all'interno della cartella di Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Errore: la destinazione deve essere un percorso completo. Ricevuto: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles% and %ProgramData%. | Errore: la destinazione {0} si trova in una cartella di sistema di Windows. Scegli un percorso al di fuori di %SystemRoot%, %ProgramFiles% e %ProgramData%. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are blocked while that runs. Try again once it finishes. | Errore: in questo momento qualcosa sta usando Windows Installer, di solito un aggiornamento di Windows o un programma che si sta installando in background. Lo spostamento e l'eliminazione sono bloccati mentre questo avviene. Riprova una volta terminato. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning the cache. | Errore: in questo computer è in sospeso una precedente transazione di Windows Installer. Prima di pulire la cache riprendi o annulla quell'installazione (o riavvia Windows). |
| Error: a queued post-reboot file operation targets the Installer cache ({0}). Restart Windows to complete that operation before cleaning. | Errore: un'operazione su file in coda per il prossimo riavvio riguarda la cache di installazione ({0}). Per completare l'operazione prima della pulizia riavvia Windows. |
| Moving {0} {1} to {2}... | Spostamento di {0} {1} in {2}... |
| Moved {0} {1}. | Spostati {0} {1}. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Un altro processo InstallerClean mantiene il blocco a istanza singola (la GUI o un'altra esecuzione della CLI). Codice di uscita 75 (transitorio); è sicuro riprovare più tardi. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Nota: scrittura nel registro eventi non riuscita. Controlla i permessi del registro Applicazione o i Criteri di gruppo. |
| InstallerClean - clean up C:\Windows\Installer | InstallerClean - pulizia di C:\Windows\Installer |
| Usage: | Utilizzo: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help       Mostra questa guida (accetta anche /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version    Mostra la versione (accetta anche -v) |
|   installerclean-cli /s         Scan only - list removable files |   installerclean-cli /s           Solo scansione - elenca i file non necessari |
|   installerclean-cli /d         Delete removable files (Recycle Bin) |   installerclean-cli /d           Elimina i file non necessari (Cestino) |
|   installerclean-cli /m         Move to saved default location |   installerclean-cli /m           Sposta nella destinazione predefinita salvata |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PERCORSO  Sposta nel percorso specificato |
| installerclean-cli is a real console process and blocks the prompt | installerclean-cli è un vero processo console e blocca il prompt |
| until it finishes; redirect or pipe its output as you would any | finché non termina; reindirizza o usa una pipe sul suo output come |
| other console exe. The GUI lives in InstallerClean.exe alongside it. | per qualsiasi altro eseguibile console. La GUI è in InstallerClean.exe. |
| The saved default is per-user; scheduled or SYSTEM runs need /m PATH. | La destinazione predefinita salvata è specifica per utente; le esecuzioni pianificate o come SYSTEM richiedono /m PERCORSO. |
| Exit codes: | Codici di uscita: |
|   0   success: every flagged file was processed |   0   successo: ogni file segnalato è stato elaborato |
|   1   failure: nothing processed (bad args, scan failed, all files failed) |   1   errore: nessuna elaborazione (argomenti errati, scansione non riuscita, tutti i file non riusciti) |
|   2   partial: some files processed, some failed |   2   parziale: alcuni file elaborati, altri non riusciti |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitorio: una condizione temporanea ha bloccato l'esecuzione (vedi il messaggio) |
|   130 cancelled (Ctrl+C) |   130 annullato (Ctrl+C) |
