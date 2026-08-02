# InstallerClean in Italiano (Italian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Italian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Italian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.it.resx`](../../src/InstallerClean.Core/Resources/Strings.it.resx), so do not edit it by hand. The Italian translation itself lives in [`gen-strings-it.mjs`](../../scripts/translations/gen-strings-it.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Italiano |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Informazioni |
| Registered files that should not be deleted | File registrati che non andrebbero eliminati |
| Unneeded files that are safe to delete | File non necessari, sicuri da eliminare |

## Section headings

| English | Italiano |
| --- | --- |
| PRODUCTS | PRODOTTI |
| PATCHES | PATCH |
| PRODUCT DETAILS | DETTAGLI PRODOTTO |
| BACKUP FOLDER | BACKUP FOLDER |
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
| _Delete permanently | _Elimina definitivamente |
| _Done | _Fatto |
| Details | Dettagli |
| _Buy me a cuppa | _Offrimi un caffè |
| Leave a _star on GitHub | Lascia una _stella su GitHub |
| Apache 2.0 licence | Licenza Apache 2.0 |
| _Move | _Sposta |
| Path to folder if you move rather than delete. | Path to folder if you move rather than delete. |
| Open _release page | Apri pagina _release |
| _Re-scan | _Ripeti scansione |
| _Scan again | _Nuova scansione |
| Send report | Invia rapporto |
| _Send | _Invia |

## About window

| English | Italiano |
| --- | --- |
| Guide and FAQ | Guida e FAQ |
| Report a problem | Segnala un problema |
| Check for updates automatically | Controlla automaticamente gli aggiornamenti |

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
| Moving unneeded files... | Moving unneeded files... |
| Deleting unneeded files... | Deleting unneeded files... |
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
| Any unneeded files below are [safe to delete]. | Any unneeded files below are [safe to delete]. |
| They sit in {InstallerFolder}, left behind when a program was uninstalled ({0}), a newer patch replaced one ({1}) or the publisher withdrew it ({2}). InstallerClean only ever lists files Windows itself reports as finished with. | Si trovano in {InstallerFolder}, lasciati indietro quando un programma è stato disinstallato ({0}), una patch più recente ne ha sostituito uno ({1}) o l'editore lo ha ritirato ({2}). InstallerClean elenca sempre e solo i file che Windows stesso segnala come non più necessari. |
| Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. | Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored. |
| Nothing scanned yet. | Ancora nessuna scansione. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Premi Ripeti scansione per cercare in {InstallerFolder} i file di installazione che nessun programma usa più. |
| These files can't be cleaned up right now. | Al momento questi file non si possono ripulire. |
| Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. |
| Select a file to view details. | Seleziona un file per vederne i dettagli. |
| Select a product to view details. | Seleziona un prodotto per vederne i dettagli. |
| No metadata available. | Nessun metadato disponibile. |
| This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. | This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.<br><br>It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself. |
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
| Nothing to clean up in {InstallerFolder} | Niente da eliminare in {InstallerFolder} |
| Scanned {0} {1} in {2} | Analizzati {0} {1} in {2} |
| The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). | The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely). |
| The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). | The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely). |
| {0} freed | Liberati {0} |
| {0} moved | Spostati {0} |
| Nothing was moved | Nessun file spostato |
| Nothing was deleted | Nessun file eliminato |
| {0} of {1} could not be moved. | {0} di {1} file non è stato spostato. |
| {0} of {1} could not be moved. | {0} di {1} file non sono stati spostati. |
| {0} of {1} could not be deleted. | {0} di {1} file non è stato eliminato. |
| {0} of {1} could not be deleted. | {0} di {1} file non sono stati eliminati. |
| {0} {1} moved to: {2} | Spostato {0} {1} in: {2} |
| {0} {1} moved to: {2} | Spostati {0} {1} in: {2} |
| {0} {1} kept in place, because a program went back to needing what the scan flagged. | {0} {1} lasciati al loro posto, perché un programma è tornato ad averne bisogno dopo la scansione. |
| {0} {1} kept in place, because the Windows Installer records could not be fully read when the check was repeated. | {0} {1} lasciati al loro posto, perché non è stato possibile leggere completamente i record di Windows Installer quando il controllo è stato ripetuto. |
| Moved {0} of {1} {2} before you cancelled. | Spostati {0} di {1} {2} prima dell'annullamento. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Eliminati definitivamente {0} di {1} {2} prima dell'annullamento. |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| {0} {1} permanently deleted | {0} {1} permanently deleted |
| Glad to help. There's a tip jar if you're feeling kind. | Felice di esserti stato utile. Un caffè è sempre bene accetto, se ti viene dal cuore. |

## Summaries and counts

| English | Italiano |
| --- | --- |
| {0} file still needed | {0} file ancora necessario |
| {0} files still needed | {0} file ancora necessari |
| {0} unneeded file to clean up | {0} file non necessario da eliminare |
| {0} unneeded files to clean up | {0} file non necessari da eliminare |
| {0} registered file is missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Open Details for what to do. | {0} file registrato risulta mancante (non eliminato da InstallerClean). Per ora nessun problema, ma in futuro una riparazione, un aggiornamento o una disinstallazione di quel programma potrebbe non riuscire. Per sapere cosa fare apri 'Dettagli'. |
| {0} registered files are missing (not deleted by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Open Details for what to do. | {0} file registrati risultano mancanti (non eliminati da InstallerClean). Per ora nessun problema, ma in futuro una riparazione, un aggiornamento o una disinstallazione di quei programmi potrebbe non riuscire. Per sapere cosa fare apri 'Dettagli'. |
| Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
| Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. | Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again. |
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
| This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. | This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead. |
| Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. | Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. |

## Error messages

| English | Italiano |
| --- | --- |
| Access denied | Accesso negato |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows ha negato l'accesso a InstallerClean, che si è quindi fermato. Non è stato rimosso nulla.<br><br>InstallerClean era già in esecuzione come amministratore, quindi riavviarlo in quel modo non serve. Windows non dice altro su che cosa abbia negato l'accesso, quindi non c'è nulla di specifico da provare. |
| Couldn't read the Windows Installer records | Impossibile leggere i record di Windows Installer |
| Scan failed | Scansione non riuscita |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | I record di Windows Installer sono tornati completamente vuoti: nemmeno un programma installato o un aggiornamento rivendica un file di installazione nella cache. Su una macchina funzionante questo non succede (perfino un'installazione di Windows appena fatta ne ha qualcuno), quindi o i record sono danneggiati o non è stato possibile leggerli, e una scansione che credesse a questa risposta classificherebbe erroneamente come orfano ogni file in {InstallerFolder}. InstallerClean si è fermato invece di farlo. Non è stato rimosso nulla. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer ha impedito a InstallerClean di elencare ciò che è installato. InstallerClean era già in esecuzione come amministratore, quindi eseguirlo di nuovo come amministratore non cambia nulla. Senza quell'elenco non c'è modo sicuro di sapere quali file nella cache servono ancora, quindi InstallerClean si è fermato. Non è stato rimosso nulla. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: {0} entries in a row came back unreadable (last error code {1}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer non è riuscito a fornire a InstallerClean un elenco leggibile dei programmi installati: {0} voci di seguito sono tornate illeggibili (ultimo codice di errore {1}). Invece di lavorare su un elenco letto solo in parte, InstallerClean si è fermato. Non è stato rimosso nulla. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer non ha mai segnalato la fine dell'elenco dei programmi installati: InstallerClean ha rinunciato dopo {0} voci (ultimo codice di errore {1}). Di un elenco senza fine non ci si può fidare, quindi InstallerClean si è fermato. Non è stato rimosso nulla. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer non ha mai segnalato la fine dell'elenco delle patch di un programma: InstallerClean ha rinunciato dopo {0} voci (ultimo codice di errore {1}). Di un elenco senza fine non ci si può fidare, quindi InstallerClean si è fermato. Non è stato rimosso nulla. |
| InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. | InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean non è riuscito a leggere abbastanza dei record di Windows Installer per essere sicuro di che cosa serva ancora: l'elenco dei programmi installati è tornato incompleto, e leggere gli stessi record direttamente dal registro di sistema ha dato errori a sua volta. Un file potrebbe sembrare orfano solo perché il record che lo nomina era fra quelli illeggibili, quindi InstallerClean si è fermato. Non è stato rimosso nulla. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. |
| Invalid destination | Destinazione non valida |
| Could not write to destination | Impossibile scrivere nella destinazione |
| Move failed | Spostamento non riuscito |
| Delete failed | Eliminazione non riuscita |
| Setting not saved | Impostazione non salvata |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Impossibile salvare la modifica. Al prossimo avvio InstallerClean tornerà all'impostazione precedente. |
| The destination cannot be inside the Windows Installer folder. | La destinazione non può trovarsi all'interno della cartella di Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
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
| This file is not directly inside the Windows Installer folder; refused for safety. | Questo file non si trova direttamente all'interno della cartella di Windows Installer; rifiutato per sicurezza. |
| Windows refused access to this file; it was left in place. | Windows ha negato l'accesso a questo file; è stato lasciato al suo posto. |
| Windows refused access to these files; they were left in place. | Windows ha negato l'accesso a questi file; sono stati lasciati al loro posto. |
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. |
| Windows reported a file error; the file was left in place. | Windows ha segnalato un errore sul file; il file è stato lasciato al suo posto. |
| Windows reported file errors; these files were left in place. | Windows ha segnalato errori sui file; questi file sono stati lasciati al loro posto. |
| Something went wrong with this file; it was left in place. | Qualcosa è andato storto con questo file; è stato lasciato al suo posto. |
| Something went wrong with these files; they were left in place. | Qualcosa è andato storto con questi file; sono stati lasciati al loro posto. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Spostamento dei file nella cartella di Windows Installer rifiutato (destinazione: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again. |
| Cannot write to {0}. | Impossibile scrivere in {0}. |
| Could not find a unique filename for '{0}' after 10,000 attempts. | Dopo 10.000 tentativi impossibile trovare un nome file univoco per '{0}'. |

## Update check

| English | Italiano |
| --- | --- |
| Check for updates | Controlla aggiornamenti |
| Checking... | Controllo aggiornamenti... |
| Up to date. | Versione aggiornata. |
| Version {0} is available. | È disponibile la versione {0}. |
| Update available | Aggiornamento disponibile |
| You're running version {0}.<br>Version {1} is available. | La versione in uso è la versione {0}.<br>È disponibile la versione {1}. |
| Couldn't reach GitHub. Check your internet connection and try again. | Impossibile raggiungere GitHub. Controlla la connessione internet e riprova. |
| GitHub returned an error response. Try again in a few minutes. | GitHub ha restituito una risposta di errore. Riprova tra qualche minuto. |
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
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> |

## Tooltips (hover text)

| English | Italiano |
| --- | --- |
| It's thirsty work! | Ci vuole un caffè! |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Annullamento richiesto. InstallerClean sta aspettando che il passaggio in corso arrivi a un punto in cui fermarsi. Può richiedere qualche secondo durante operazioni di I/O intense o una chiamata al database MSI. |
| Close | Chiudi |
| A star helps other people find it. | Una stella aiuta altre persone a trovare InstallerClean. |
| Minimise | Riduci a icona |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working and how much space people are freeing. The next screen lets you see what will be sent before you confirm. | Come preferisci, ma è apprezzato. Invia un riepilogo anonimo che mi fa solo sapere se funziona e quanto spazio le persone stanno liberando. La schermata successiva visualizza cosa verrà inviato prima di confermare. |
| Up to you but appreciated. Sends an anonymous summary that just lets me know if it's working. The next screen lets you see what will be sent before you confirm. | Come preferisci, ma è apprezzato. Invia un riepilogo anonimo che mi fa solo sapere se funziona. La schermata successiva ti mostra cosa verrà inviato prima di confermare. |
| Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. | Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. | Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them. |
| Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. | Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nome soggetto dal certificato Authenticode incorporato. Catena non verificata. |
| Change language. The program will restart. | Cambia lingua. Il programma verrà riavviato. |

## Screen reader labels

| English | Italiano |
| --- | --- |
| Donate | Dona |
| Buy me a cuppa | Offrimi un caffè |
| Cancel operation | Annulla operazione |
| Cancel scan | Annulla scansione |
| Cancel startup scan | Annulla scansione all'avvio |
| Close | Chiudi |
| Close window | Chiudi finestra |
| Close result and return to main window | Chiudi finestra risultato e torna alla finestra principale |
| Leave a star on github | Lascia una stella su github |
| Minimise | Riduci a icona |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Delete permanently removes the unneeded files. Cancel closes without deleting. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | 'Sposta' colloca i file non necessari nella cartella destinazione scelta. 'Annulla' li lascia dove sono. |
| Say thanks | Per ringraziarmi |
| Send posts the report shown to No Faff. Cancel sends nothing. | 'Invia' trasmette a No Faff il rapporto mostrato. Annulla non invia nulla. |
| Check for updates | Controlla aggiornamenti |
| Checks github's releases page for a newer version. | Verifica sulla pagina release di github se esiste una versione più recente. |
| Opens the readme on github in your browser. | Apre readme su github nel browser. |
| Opens the issue tracker on github.com in your browser. | Apre elenco problemi (issue) in github.com nel browser. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Se selezionata, all'avvio InstallerClean verifica su github se è disponibile una versione più recente. |
| Open the release page to download the newer version, or cancel to keep the current version. | Apri pagina release per scaricare la versione più recente, o scegli 'Annulla' per mantenere quella attuale. |
| Opens the licence file on github.com in your browser. | Apre file licenza in github.com nel browser. |
| Backup folder | Backup folder |
| Products | Prodotti |
| Patches | Patch |
| Product details | Dettagli prodotto |
| Backup folder | Backup folder |
| Operation progress | Avanzamento operazione |
| Scan {InstallerFolder} again | Nuova scansione di {InstallerFolder} |
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
| Product details | Product details |
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
| Error: unknown argument '{0}' | Error: unknown argument '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Errore: argomento aggiuntivo imprevisto '{0}'. Se la cartella di destinazione contiene uno spazio, racchiudi l'intero percorso tra virgolette: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. |
| Cancelling... | Annullamento... |
| Cancelled. | Operazione annullata. |
| Error: unexpected failure ({0}). Details written to {1}. | Error: unexpected failure ({0}). Details written to {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Error: unexpected failure ({0}). The crash log could not be written. |
| Scanning {InstallerFolder}... | Scansione di {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Found {0} unneeded {1} to clean up ({2}). |
| Found no unneeded files. | Found no unneeded files. |
| {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. | {0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it. |
| {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. | {0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them. |
| Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. | Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up. |
| Deleting {0} unneeded {1}... | Deleting {0} unneeded {1}... |
| Permanently deleted {0} unneeded {1}. | Permanently deleted {0} unneeded {1}. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Errore: nessuna destinazione di spostamento specificata. Usa /m PERCORSO. (Una destinazione predefinita impostata nella GUI è specifica per utente e non si applica alle esecuzioni pianificate o con account di servizio.) |
| Error: destination cannot be inside the Windows Installer folder. | Errore: la destinazione non può trovarsi all'interno della cartella di Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Errore: la destinazione deve essere un percorso completo. Ricevuto: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. |
| Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. |
| Moving {0} unneeded {1} to {2}... | Moving {0} unneeded {1} to {2}... |
| Moved {0} unneeded {1}. | Moved {0} unneeded {1}. |
| The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again. | The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Un altro processo InstallerClean mantiene il blocco a istanza singola (la GUI o un'altra esecuzione della CLI). Codice di uscita 75 (transitorio); è sicuro riprovare più tardi. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Nota: scrittura nel registro eventi non riuscita. Controlla i permessi del registro Applicazione o i Criteri di gruppo. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - pulizia di {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Removes cached .msi and .msp files that no installed program still needs. |
| Needs an elevated (administrator) prompt; Windows will not start it. | Needs an elevated (administrator) prompt; Windows will not start it. |
| Usage: | Utilizzo: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help       Mostra questa guida (anche /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version    Mostra la versione (anche -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s         Scan only - list unneeded files |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d         Delete unneeded files permanently |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m         Move to the saved backup folder |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PERCORSO  Sposta nel percorso specificato |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. |
| Exit codes: | Codici di uscita: |
|   0   success: the run finished with nothing left to do |   0   success: the run finished with nothing left to do |
|   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |   1   failure: nothing processed (bad arguments, a bad destination, a<br>       failed scan or every file failed) |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   partial: some processed, some not (a failure or a Ctrl+C part way) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitorio: qualcosa ha bloccato l'esecuzione (vedi il messaggio) |
|   130 cancelled (Ctrl+C) |   130 annullato (Ctrl+C) |
