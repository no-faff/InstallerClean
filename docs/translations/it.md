# InstallerClean in Italiano (Italian)

The text of InstallerClean's interface and command-line tool in English on the left, with the Italian translation beside it, grouped by where each line appears in the app. It is here so someone who really knows Italian can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.

A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [`Strings.it.resx`](../../src/InstallerClean.Core/Resources/Strings.it.resx), so do not edit it by hand. The Italian translation itself lives in [`gen-strings-it.mjs`](../../scripts/translations/gen-strings-it.mjs).

`{InstallerFolder}` and the numbered slots (`{0}`, `{1}`) are filled in by the app when it runs, so keep them exactly as they are. `{InstallerFolder}` becomes the real installer folder on that machine, usually `C:\Windows\Installer`. Move them within the sentence if the grammar needs it; do not translate them.

## Window titles

| English | Italiano |
| --- | --- |
| InstallerClean | InstallerClean |
| About | Informazioni |
| Files left alone | File lasciati stare |
| Unneeded files that are safe to delete | File non necessari, sicuri da eliminare |
| The anonymous report | Il rapporto anonimo |
| InstallerClean couldn't read the report. | InstallerClean non è riuscito a leggere il rapporto. |

## Section headings

| English | Italiano |
| --- | --- |
| PATCHES | PATCH |
| PRODUCT DETAILS | DETTAGLI PRODOTTO |
| BACKUP FOLDER | CARTELLA DI BACKUP |
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
| Carry on _without it | Continua _senza attendere |
| Check for _updates | Controlla _aggiornamenti |
| _Close | _Chiudi |
| _Close without tipping | _Chiudi senza lasciare la mancia |
| _Delete permanently | _Elimina definitivamente |
| _Done | _Fatto |
| Details | Dettagli |
| _Leave a tip | _Lascia una mancia |
| Leave a tip | Lascia una mancia |
| _Leave a tip | _Lascia una mancia |
| Leave a _star on GitHub | Lascia una _stella su GitHub |
| Apache 2.0 licence | Licenza Apache 2.0 |
| _Move | _Sposta |
| Path to folder if you move rather than delete. | Percorso della cartella se sposti anziché eliminare. |
| _Re-scan | _Ripeti scansione |

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
| (no program) | (nessun programma) |
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
| Checking registry for additional packages... | Controllo registro per altri pacchetti... |
| Matching files against installed programs... | Confronto file con programmi installati... |
| {0} of {1} | {0} di {1} |
| Checking remaining files... | Controllo file rimanenti... |
| Waiting for drive {0} to respond... | In attesa che l'unità {0} risponda... |
| Waiting for {0} to respond... | In attesa che {0} risponda... |
| Any file still to be checked against it will be left alone. | Ogni file ancora da confrontare con ciò che vi si trova verrà lasciato stare. |
| Scan complete ({0}) | Scansione completata ({0}) |
| Found {0} {1} you can safely delete. | {0} {1} che puoi eliminare in sicurezza. |
| Preparing destination folder... | Preparazione cartella destinazione... |
| Moving unneeded files... | Spostamento dei file non necessari... |
| Deleting unneeded files... | Eliminazione dei file non necessari... |
| Move cancelled. {0} of {1} {2} processed. | Spostamento annullato dopo {0} di {1} {2}. |
| Delete cancelled. {0} of {1} {2} processed. | Eliminazione annullata dopo {0} di {1} {2}. |
| {0}. Details are in {1}. | {0}. Dettagli in {1}. |
| {0}. The crash log could not be written. | {0}. Non è stato possibile scrivere il file crash.log. |
| {0}. Details are in {1}. | {0}. Dettagli in {1}. |
| {0}. The crash log could not be written. | {0}. Non è stato possibile scrivere il file crash.log. |
| Access denied. Windows refused the scan. | Accesso negato. Windows ha rifiutato la scansione. |
| Scan stopped. | Scansione interrotta. |
| Scan cancelled. | Scansione annullata. |
| Ready | Pronto |
| Scan failed ({0}). Details in {1}. | Scansione non riuscita ({0}). Dettagli in {1}. |
| Scan failed ({0}). The crash log could not be written. | Scansione non riuscita ({0}). Non è stato possibile scrivere il file crash.log. |

## Main screen text

| English | Italiano |
| --- | --- |
| Any unneeded files below are safe to delete. | Tutti i file non necessari qui sotto sono sicuri da eliminare. |
| They sit in {InstallerFolder}. InstallerClean asks Windows about every installed program: a file is listed when no program claims it ({0}), or when a newer patch has replaced it and no program could roll back to it ({1}). | Si trovano in {InstallerFolder}. InstallerClean interroga Windows su ogni programma installato: un file compare nell'elenco quando nessun programma lo rivendica ({0}), oppure quando una patch più recente lo ha sostituito e nessun programma potrebbe tornare a esso ({1}). |
| Move them to a backup folder you choose, then delete that folder when you're satisfied your programs still update and uninstall as normal. Putting them back into {InstallerFolder} restores everything. Or delete them permanently now. | Spostali in una cartella di backup che scegli tu, poi elimina quella cartella quando sei convinto che i tuoi programmi si aggiornino e si disinstallino ancora normalmente. Rimetterli in {InstallerFolder} ripristina tutto. Oppure eliminali definitivamente adesso. |
| Nothing scanned yet. | Ancora nessuna scansione. |
| Press Re-scan to look through {InstallerFolder} for installer files that no program still needs. | Premi Ripeti scansione per cercare in {InstallerFolder} i file di installazione che nessun programma usa più. |
| Nothing to show until the next scan. | Niente da mostrare fino alla prossima scansione. |
| The scan that runs after a Move or Delete didn't finish, so there's no list to show. Press Re-scan to scan {InstallerFolder} again. | La scansione eseguita dopo Sposta o Elimina non è terminata, quindi non c'è nessun elenco da mostrare. Premi Ripeti scansione per eseguire di nuovo la scansione di {InstallerFolder}. |
| What stopped it is recorded in {0}. | Il motivo dell'interruzione è registrato in {0}. |
| These files can't be cleaned up right now. | Al momento questi file non si possono ripulire. |
| Something is using Windows Installer right now, such as a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back. | Qualcosa sta usando Windows Installer in questo momento, ad esempio un aggiornamento di Windows o un programma che si installa in background. Sposta ed Elimina sono in pausa mentre accade, così InstallerClean non tocca {InstallerFolder} mentre cambia. Quando ha finito, ripeti la scansione e tornano disponibili. |
| Windows refused InstallerClean permission to check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If Windows still refuses, this isn't a machine InstallerClean can clean. | Windows ha negato a InstallerClean il permesso di controllare se Windows Installer è occupato, quindi Sposta ed Elimina sono in pausa e InstallerClean non tocca {InstallerFolder}. Riavvia Windows e usa Ripeti scansione. Se Windows continua a negarlo, questo non è un computer che InstallerClean possa pulire. |
| InstallerClean couldn't check whether Windows Installer is busy, so Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean non è riuscito a controllare se Windows Installer è occupato, quindi Sposta ed Elimina sono in pausa e InstallerClean non tocca {InstallerFolder}. Riavvia Windows e usa Ripeti scansione. Se continua a non riuscire a controllarlo, questo non è un computer che InstallerClean possa pulire. |
| InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}). Move and Delete are paused and InstallerClean won't touch {InstallerFolder}. Restart Windows and Re-scan. If it still can't check, this isn't a machine InstallerClean can clean. | InstallerClean non è riuscito a controllare se Windows Installer è occupato, perché Windows ha restituito un errore: {0} (errore {1}). Sposta ed Elimina sono in pausa e InstallerClean non tocca {InstallerFolder}. Riavvia Windows e usa Ripeti scansione. Se continua a non riuscire a controllarlo, questo non è un computer che InstallerClean possa pulire. |
| A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Su questo computer c'è una transazione di Windows Installer sospesa. Riprendi o annulla quell'installazione (o riavvia Windows) prima di ripulire {InstallerFolder}. |
| Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning. | Windows ha in coda per il prossimo riavvio una ridenominazione di file che riguarda {InstallerFolder}. Riavvia Windows prima di ripulire. |
| A file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out that they're in {InstallerFolder}. Restart Windows before cleaning. | Un'operazione sui file è in coda per il prossimo riavvio e InstallerClean non riesce a sapere quali file nomina, quindi non può escludere che siano in {InstallerFolder}. Riavvia Windows prima di pulire. |
| InstallerClean couldn't read one of the Windows settings it checks before touching {InstallerFolder}, so it can't tell whether an installer operation is running or waiting for a restart. Restart Windows and Re-scan. If the setting still won't read, this isn't a machine InstallerClean can clean. | InstallerClean non è riuscito a leggere una delle impostazioni di Windows che controlla prima di toccare {InstallerFolder}, quindi non può sapere se un'operazione di installazione è in corso o in attesa di un riavvio. Riavvia Windows e usa Ripeti scansione. Se l'impostazione continua a non essere leggibile, questo non è un computer che InstallerClean possa pulire. |
| Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back. | Windows Installer ha qualcosa in corso, quindi Sposta ed Elimina sono in pausa. InstallerClean non tocca {InstallerFolder} mentre cambia. Quando ha finito, ripeti la scansione e tornano disponibili. |
| Select a file to view details. | Seleziona un file per vederne i dettagli. |
| Select a product to view details. | Seleziona un prodotto per vederne i dettagli. |
| No metadata available. | Nessun metadato disponibile. |
| This installer file is missing. It causes no trouble now, and won't until the day you try to update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.<br><br>To put it back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Questo file di installazione manca. Ora non causa alcun problema, e non ne causerà fino al giorno in cui proverai ad aggiornare o disinstallare il programma a cui appartiene. Quel passaggio può allora fallire, perché Windows cerca questo file e non lo trova.<br><br>Per rimetterlo, ti serve il programma di installazione della versione che hai già. Procuratelo dal produttore del programma ed eseguilo sopra la copia esistente. Una versione più recente non va bene: dovrebbe prima rimuovere quella che hai, ed è proprio il passaggio che ha bisogno di questo file. Nemmeno disinstallare prima funziona, per lo stesso motivo. Questo dovrebbe ripristinare il file e lasciare intatte le tue impostazioni, ma Microsoft non lo garantisce. |
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
| Nothing offered on this PC | Nulla proposto su questo PC |
| One file ({2}) was held back because Windows shows it was changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check it again. | Un file ({2}) è stato trattenuto perché Windows indica che è stato modificato dopo che InstallerClean ha iniziato la scansione. Se un programma si stava installando o aggiornando, ripeti la scansione quando ha finito e InstallerClean lo controllerà di nuovo. |
| {0} {1} ({2}) were held back because Windows shows they were changed after InstallerClean started scanning. If a program was installing or updating, scan again once it has finished and InstallerClean will check them again. | {0} {1} ({2}) sono stati trattenuti perché Windows indica che sono stati modificati dopo che InstallerClean ha iniziato la scansione. Se un programma si stava installando o aggiornando, ripeti la scansione quando ha finito e InstallerClean li controllerà di nuovo. |
| Delete that folder when you're satisfied all is well. | Elimina quella cartella quando sei convinto che vada tutto bene. |
| Delete that folder when you're satisfied all is well. You won't actually reclaim the space until you do. | Elimina quella cartella quando sei convinto che vada tutto bene. Solo allora recuperi davvero lo spazio. |
| {0} freed | Liberati {0} |
| {0} moved | Spostati {0} |
| Nothing was moved | Nessun file spostato |
| Nothing was deleted | Nessun file eliminato |
| {0} file could not be moved. | {0} file non è stato spostato. |
| {0} files could not be moved. | {0} file non sono stati spostati. |
| {0} file could not be deleted. | {0} file non è stato eliminato. |
| {0} files could not be deleted. | {0} file non sono stati eliminati. |
| {0} {1} moved to: {2} | Spostato {0} {1} in: {2} |
| {0} {1} moved to: {2} | Spostati {0} {1} in: {2} |
| {0} file held back. The scan said it was unneeded. The final check couldn't confirm that. | {0} file trattenuto. La scansione lo dava per non necessario. Il controllo finale non ha potuto confermarlo. |
| {0} files held back. The scan said these were unneeded. The final check couldn't confirm that. | {0} file trattenuti. La scansione li dava per non necessari. Il controllo finale non ha potuto confermarlo. |
| {0} {1} kept in place, because Windows has a record of the program named inside. | {0} {1} lasciati al loro posto, perché Windows ha un record del programma nominato all'interno. |
| {0} {1} kept in place, because InstallerClean couldn't find a program named inside. | {0} {1} lasciati al loro posto, perché InstallerClean non ha trovato alcun programma nominato all'interno. |
| Moved {0} of {1} {2} to {3} before you cancelled. | Spostati {0} di {1} {2} in {3} prima dell'annullamento. |
| Permanently deleted {0} of {1} {2} before you cancelled. | Eliminati definitivamente {0} di {1} {2} prima dell'annullamento. |
| It's simple to undo. Move them back into {InstallerFolder} and everything will be back to how it was. | Annullarlo è semplice. Rispostali in {InstallerFolder} e tornerà tutto com'era. |
| {0} {1} permanently deleted | {0} {1} eliminato definitivamente |
| {0} {1} permanently deleted | {0} {1} eliminati definitivamente |
| Any tip is much appreciated | Ogni mancia è molto apprezzata |
| Send _anonymous report | Invia rapporto _anonimo |
| The report adds your result to [this chart] on GitHub, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Il rapporto aggiunge il tuo risultato a [questo grafico] su GitHub, che mostra quanto spazio le persone stanno recuperando con InstallerClean. Contiene anche questi dettagli tecnici, che mi aiutano a individuare eventuali bug: |
| How much space was freed, and how many files were moved or deleted | Quanto spazio è stato liberato e quanti file sono stati spostati o eliminati |
| Whether a Move went to the same drive, another drive, a removable drive or a network share | Se Sposta ha portato i file sulla stessa unità, su un'altra unità, su un'unità rimovibile o su una condivisione di rete |
| How long the scan and the Move or Delete took | Quanto sono durati la scansione e Sposta o Elimina |
| How many installer files there are of each kind, and how much space they take | Quanti file di installazione ci sono di ogni tipo e quanto spazio occupano |
| How many files were left alone, and why | Quanti file sono stati lasciati stare e perché |
| How often InstallerClean waited for a slow drive or network share, and how many it stopped waiting for | Quante volte InstallerClean ha atteso un'unità o una condivisione di rete lenta e quante ha smesso di attendere |
| How many programs and patches Windows has a record of | Di quanti programmi e patch Windows ha un record |
| Whether Windows still makes old-style short file names | Se Windows crea ancora i nomi di file brevi vecchio stile |
| Whether Windows is 10 or 11 and x64 or Arm, and which language and country or region it's set to | Se Windows è 10 o 11 e x64 o Arm, e in quale lingua e con quale paese o area geografica è impostato |
| InstallerClean's version, how you got it (setup, portable, slim, Scoop, the command-line tool on its own, built from source, or not known), whether the report was sent from InstallerClean or the command-line tool, and the language it was showing | La versione di InstallerClean, come l'hai ottenuto (setup, portable, slim, Scoop, lo strumento da riga di comando da solo, compilato dal codice sorgente, o sconosciuto), se il rapporto è stato inviato da InstallerClean o dallo strumento da riga di comando, e la lingua in cui era visualizzato |
| Counts of anything that went wrong | Il conteggio di qualsiasi cosa sia andata storta |
| Each of these is a number or a label. Nothing in the report names you, your PC, your files or your programs, and I never see your IP address. | Ognuno di questi è un numero o un'etichetta. Niente nel rapporto indica te, il tuo PC, i tuoi file o i tuoi programmi, e non vedo mai il tuo indirizzo IP. |
| See exactly what's sent | Vedi esattamente cosa viene inviato |

## Summaries and counts

| English | Italiano |
| --- | --- |
| {0} file left alone | {0} file lasciato stare |
| {0} files left alone | {0} file lasciati stare |
| {0} unneeded file to clean up | {0} file non necessario da eliminare |
| {0} unneeded files to clean up | {0} file non necessari da eliminare |
| Windows has a record for {0} file that isn't in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. Open Details for what to do. | Windows ha un record per {0} file che non è in {InstallerFolder}: {1}. Nell'uso quotidiano non crea problemi, ma un aggiornamento o una disinstallazione di quel programma può fallire. Apri Dettagli per sapere cosa fare. |
| Windows has records for {0} files that aren't in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. Open Details for what to do. | Windows ha record per {0} file che non sono in {InstallerFolder}: {1}. Nell'uso quotidiano non creano problemi, ma un aggiornamento o una disinstallazione di quei programmi può fallire. Apri Dettagli per sapere cosa fare. |
| {0} other program | {0} altro programma |
| {0} other programs | {0} altri programmi |
| {0} file with no program named in the records | {0} file senza alcun programma nominato nei record |
| {0} files with no program named in the records | {0} file senza alcun programma nominato nei record |
| InstallerClean carried on without drive {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean è andato avanti senza l'unità {0} e ha lasciato stare ogni file ancora da confrontare con essa. Quando torna a rispondere normalmente, premi Ripeti scansione. |
| InstallerClean carried on without {0} and left alone any file still to be checked against it. Once it's responding normally, Re-scan. | InstallerClean è andato avanti senza {0} e ha lasciato stare ogni file ancora da confrontare con ciò che vi si trova. Quando torna a rispondere normalmente, premi Ripeti scansione. |
| InstallerClean carried on without more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, Re-scan. | InstallerClean è andato avanti senza più di un'unità o condivisione ({0}) e ha lasciato stare ogni file ancora da confrontare con una di esse. Quando tornano a rispondere normalmente, premi Ripeti scansione. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has finished. | Qualcosa stava usando Windows Installer all'avvio della scansione, ad esempio un aggiornamento di Windows o un programma che si installava, perciò InstallerClean ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Premi Ripeti scansione quando ha finito. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then Re-scan. | All'avvio della scansione, su questo computer c'era una transazione di Windows Installer sospesa, perciò InstallerClean ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Riprendi o annulla quell'installazione, oppure riavvia Windows, poi premi Ripeti scansione. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | All'avvio della scansione, Windows aveva in coda per il prossimo riavvio una ridenominazione di file che riguarda {InstallerFolder}, perciò InstallerClean ha trattenuto i file lì presenti non nominati nei record di alcun programma installato, invece di proporli. Riavvia Windows, poi premi Ripeti scansione. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then Re-scan. | All'avvio della scansione, un'operazione sui file era in coda per il prossimo riavvio e InstallerClean non riusciva a sapere quali file nomina, perciò ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Riavvia Windows, poi premi Ripeti scansione. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | All'avvio della scansione, Windows ha negato a InstallerClean il permesso di controllare se Windows Installer era occupato, perciò ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Riavvia Windows e premi Ripeti scansione. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | All'avvio della scansione, InstallerClean non è riuscito a leggere una delle impostazioni di Windows che controlla, quindi non poteva sapere se un'operazione di installazione era in corso o in attesa di un riavvio, e ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Riavvia Windows e premi Ripeti scansione. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | All'avvio della scansione, InstallerClean non è riuscito a controllare se Windows Installer era occupato, perciò ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Riavvia Windows e premi Ripeti scansione. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and Re-scan. | All'avvio della scansione, InstallerClean non è riuscito a controllare se Windows Installer era occupato, perché Windows ha restituito un errore: {0} (errore {1}). Perciò ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Riavvia Windows e premi Ripeti scansione. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan once that has cleared. | Windows Installer aveva qualcosa in corso o in attesa all'avvio della scansione, perciò InstallerClean ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Premi Ripeti scansione quando si è risolto. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Re-scan. | L'orologio di questo PC è tornato indietro o ha perso tempo durante la scansione, perciò InstallerClean ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Premi Ripeti scansione. |
| {0} of {1} {2} | {0} di {1} {2} |
| {0} unneeded {1} ({2}) | {0} {1} da eliminare ({2}) |
| {0} file left alone ({1}) | {0} file lasciato stare ({1}) |
| {0} files left alone ({1}) | {0} file lasciati stare ({1}) |
| {0} missing | {0} mancante |
| {0} missing | {0} mancanti |

## Confirmation dialogs

| English | Italiano |
| --- | --- |
| Move {0} {1} ({2})? | Vuoi spostare {0} {1} ({2})? |
| This file will be moved to: | Questo file verrà spostato in: |
| These files will be moved to: | Questi file verranno spostati in: |
| Delete {0} {1} ({2})? | Vuoi eliminare {0} {1} ({2})? |
| This file will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Questo file verrà eliminato definitivamente. È un'operazione sicura, ma se vuoi una copia di riserva usa invece Sposta. |
| These files will be deleted permanently. It's safe to do but if you'd like a backup, use Move instead. | Questi file verranno eliminati definitivamente. È un'operazione sicura, ma se vuoi una copia di riserva usa invece Sposta. |
| That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away. | Quella cartella è sulla stessa unità, quindi lo spazio non torna finché non la elimini. Scegli invece una cartella su un'altra unità se vuoi lo spazio subito. |

## Error messages

| English | Italiano |
| --- | --- |
| This is also recorded in {0}. | Questo viene registrato anche in {0}. |
| Access denied | Accesso negato |
| Windows refused InstallerClean access, so it stopped. Nothing has been removed.<br><br>InstallerClean was already running as administrator, so starting it again that way won't help. Windows doesn't say any more about what refused, so there's nothing specific to try. | Windows ha negato l'accesso a InstallerClean, che si è quindi fermato. Non è stato rimosso nulla.<br><br>InstallerClean era già in esecuzione come amministratore, quindi riavviarlo in quel modo non serve. Windows non dice altro su che cosa abbia negato l'accesso, quindi non c'è nulla di specifico da provare. |
| InstallerClean stopped | InstallerClean si è fermato |
| Scan failed | Scansione non riuscita |
| The Windows Installer records came back completely empty: not one installed program or update claims a cached installer file. That doesn't happen on a working machine (even a fresh Windows install has some), so either the records are damaged or they couldn't be read, and a scan that believed this answer would wrongly call every file in {InstallerFolder} orphaned. InstallerClean stopped instead. Nothing has been removed. | I record di Windows Installer sono tornati completamente vuoti: nemmeno un programma installato o un aggiornamento rivendica un file di installazione nella cache. Su una macchina funzionante questo non succede (perfino un'installazione di Windows appena fatta ne ha qualcuno), quindi o i record sono danneggiati o non è stato possibile leggerli, e una scansione che credesse a questa risposta classificherebbe erroneamente come orfano ogni file in {InstallerFolder}. InstallerClean si è fermato invece di farlo. Non è stato rimosso nulla. |
| Windows Installer refused to let InstallerClean list what's installed. InstallerClean was already running as administrator, so running it again as administrator won't change anything. Without that list there is no safe way to tell which cached files are still needed, so InstallerClean stopped. Nothing has been removed. | Windows Installer ha impedito a InstallerClean di elencare ciò che è installato. InstallerClean era già in esecuzione come amministratore, quindi eseguirlo di nuovo come amministratore non cambia nulla. Senza quell'elenco non c'è modo sicuro di sapere quali file nella cache servono ancora, quindi InstallerClean si è fermato. Non è stato rimosso nulla. |
| Windows Installer couldn't give InstallerClean a readable list of the installed programs: it read {1} {2}, then an entry came back unreadable (error code {0}). Rather than work from a part-read list, InstallerClean stopped. Nothing has been removed. | Windows Installer non è riuscito a fornire a InstallerClean un elenco leggibile dei programmi installati: ha letto {1} {2}, poi una voce è tornata illeggibile (codice di errore {0}). Invece di lavorare su un elenco letto solo in parte, InstallerClean si è fermato. Non è stato rimosso nulla. |
| Windows Installer never signalled the end of the list of installed programs: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer non ha mai segnalato la fine dell'elenco dei programmi installati: InstallerClean ha letto {2} {3}, poi ha rinunciato dopo {0} voci (ultimo codice di errore {1}). Di un elenco senza fine non ci si può fidare, quindi InstallerClean si è fermato. Non è stato rimosso nulla. |
| Windows Installer never signalled the end of one program's patch list: InstallerClean read {2} {3}, then gave up after {0} entries (last error code {1}). A list with no end can't be trusted, so InstallerClean stopped. Nothing has been removed. | Windows Installer non ha mai segnalato la fine dell'elenco delle patch di un programma: InstallerClean ha letto {2} {3}, poi ha rinunciato dopo {0} voci (ultimo codice di errore {1}). Di un elenco senza fine non ci si può fidare, quindi InstallerClean si è fermato. Non è stato rimosso nulla. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. Almost nothing the records point at is actually there, and almost nothing that's there is named by any record, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean non è riuscito a far corrispondere i record di Windows Installer con il contenuto di {InstallerFolder}. Quasi nulla di ciò che i record indicano si trova davvero lì, e quasi nulla di ciò che è lì è nominato da un record, quindi non si è potuto dimostrare che qualche file fosse non necessario. Non è stato proposto nulla e non è stato rimosso nulla. |
| InstallerClean couldn't match the Windows Installer records against what's in {InstallerFolder}. The folder has files in it, but not one record points at anything in there, so nothing could be shown to be unneeded. Nothing has been offered and nothing has been removed. | InstallerClean non è riuscito a far corrispondere i record di Windows Installer con il contenuto di {InstallerFolder}. La cartella contiene file, ma nemmeno un record indica qualcosa al suo interno, quindi non si è potuto dimostrare che qualche file fosse non necessario. Non è stato proposto nulla e non è stato rimosso nulla. |
| InstallerClean can't scan {InstallerFolder} on this PC. Windows reports that folder as {0}, and InstallerClean only scans an Installer folder that Windows reports at its own path. Nothing has been offered and nothing has been removed. | InstallerClean non può eseguire la scansione di {InstallerFolder} su questo PC. Windows indica questa cartella come {0}, e InstallerClean esegue la scansione solo di una cartella Installer che Windows indica nel suo stesso percorso. Non è stato proposto nulla e non è stato rimosso nulla. |
| Windows reports that {InstallerFolder} doesn't exist on this PC, so InstallerClean stopped. Nothing has been offered and nothing has been removed. | Windows segnala che {InstallerFolder} non esiste su questo PC, quindi InstallerClean si è fermato. Non è stato proposto nulla e non è stato rimosso nulla. |
| Windows reported an error when InstallerClean tried to list the files in {InstallerFolder}, so it couldn't get a complete list and stopped. Nothing has been offered and nothing has been removed. | Windows ha segnalato un errore quando InstallerClean ha provato a elencare i file in {InstallerFolder}, quindi non è riuscito a ottenerne l'elenco completo e si è fermato. Non è stato proposto nulla e non è stato rimosso nulla. |
| InstallerClean couldn't read enough of the Windows Installer records to be sure what's still needed: the list of installed programs came back short, and reading the same records straight from the registry hit errors too. A file could look orphaned just because the record naming it was one of the unreadable ones, so InstallerClean stopped. Nothing has been removed. | InstallerClean non è riuscito a leggere abbastanza dei record di Windows Installer per essere sicuro di che cosa serva ancora: l'elenco dei programmi installati è tornato incompleto, e leggere gli stessi record direttamente dal registro di sistema ha dato errori a sua volta. Un file potrebbe sembrare orfano solo perché il record che lo nomina era fra quelli illeggibili, quindi InstallerClean si è fermato. Non è stato rimosso nulla. |
| InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed. | InstallerClean non è riuscito a far risolvere a Windows il vero percorso di {InstallerFolder}, quindi non si è potuto dimostrare che qualche file fosse al suo interno e nessuno è stato proposto per la pulizia. Questa scansione non ha trovato nulla perché quel controllo non è riuscito, non perché la cartella sia pulita. Non è stato rimosso nulla. |
| Nothing was deleted | Nessun file eliminato |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. | Windows ha negato a InstallerClean il permesso di controllare se Windows Installer fosse occupato, quindi non ha potuto escludere che un file servisse a metà strada, e non è stato eliminato nulla. |
| Nothing was moved | Nessun file spostato |
| Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. | Windows ha negato a InstallerClean il permesso di controllare se Windows Installer fosse occupato, quindi non ha potuto escludere che un file servisse a metà strada, e non è stato spostato nulla. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening. | InstallerClean non è riuscito a prendere il blocco che Windows Installer usa per impedire a due programmi di modificare il software installato nello stesso momento, quindi non ha potuto escludere che un file diventasse necessario a metà strada, e non è stato eliminato nulla. Riprova, e riavvia Windows se continua a succedere. |
| InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been moved. Try again, and restart Windows if it keeps happening. | InstallerClean non è riuscito a prendere il blocco che Windows Installer usa per impedire a due programmi di modificare il software installato nello stesso momento, quindi non ha potuto escludere che un file diventasse necessario a metà strada, e non è stato spostato nulla. Riprova, e riavvia Windows se continua a succedere. |
| Invalid destination | Destinazione non valida |
| Move stopped | Spostamento interrotto |
| Couldn't use that backup folder | Impossibile usare quella cartella di backup |
| Move failed | Spostamento non riuscito |
| Delete failed | Eliminazione non riuscita |
| Setting not saved | Impostazione non salvata |
| The change could not be saved. InstallerClean will go back to the previous setting next time it starts. | Impossibile salvare la modifica. Al prossimo avvio InstallerClean tornerà all'impostazione precedente. |
| The destination cannot be inside the Windows Installer folder. | La destinazione non può trovarsi all'interno della cartella di Windows Installer. |
| The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | La destinazione {0} si risolve dentro una cartella di sistema di Windows. Scegli un percorso fuori da %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% e %ProgramData%. |
| Not enough space | Spazio insufficiente |
| There isn't room at {0}<br><br>Required: {1}<br>Available: {2} | Non c'è abbastanza posto in {0}<br><br>Necessario: {1}<br>Disponibile: {2} |
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
| This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later. | Questo file è aperto o bloccato da un altro programma, quindi al momento nulla può rimuoverlo. È stato lasciato al suo posto; riprova più tardi. |
| These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later. | Questi file sono aperti o bloccati da un altro programma, quindi al momento nulla può rimuoverli. Sono stati lasciati al loro posto; riprova più tardi. |
| Windows reported a file error; the file was left in place. | Windows ha segnalato un errore sul file; il file è stato lasciato al suo posto. |
| Windows reported file errors; these files were left in place. | Windows ha segnalato errori sui file; questi file sono stati lasciati al loro posto. |
| Something went wrong with this file; it was left in place. | Qualcosa è andato storto con questo file; è stato lasciato al suo posto. |
| Something went wrong with these files; they were left in place. | Qualcosa è andato storto con questi file; sono stati lasciati al loro posto. |
| Refusing to move files into the Windows Installer folder (destination: {0}). | Spostamento dei file nella cartella di Windows Installer rifiutato (destinazione: {0}). |
| The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\Backup, or \\server\backup). InstallerClean can't use this one: {0} | La cartella di backup deve essere un percorso completo a una cartella, che inizia con una lettera di unità o una condivisione di rete (ad esempio D:\Backup, oppure \\server\backup). InstallerClean non può usare questo: {0} |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then Re-scan and try again. | InstallerClean non è più riuscito a confermare la cartella di backup, quindi si è fermato. Controlla {0}, poi Ripeti scansione e riprova. |
| Cannot write to {0}. | Impossibile scrivere in {0}. |
| You can save files in {0} but not open them again, so InstallerClean won't move files there. | Puoi salvare file in {0} ma non riaprirli, quindi InstallerClean non sposterà file lì. |
| A file called '{0}' is already in the backup folder. | Un file di nome '{0}' è già nella cartella di backup. |

## Update check

| English | Italiano |
| --- | --- |
| Check for updates | Controlla aggiornamenti |
| Checking... | Controllo aggiornamenti... |
| Up to date. | Versione aggiornata. |
| InstallerClean {0} is available. | È disponibile InstallerClean {0}. |
| It's strongly recommended to use the latest version. | Si consiglia vivamente di usare la versione più recente. |
| Download it from GitHub | Scaricalo da GitHub |
| Download .NET Framework 4.8 from Microsoft | Scarica .NET Framework 4.8 da Microsoft |
| Update now | Aggiorna ora |
| This copy was installed by Scoop. To update it, close InstallerClean and run: | Questa copia è stata installata da Scoop. Per aggiornarla, chiudi InstallerClean ed esegui: |
| This copy was installed by Scoop for all users. To update it, close InstallerClean, open a terminal as administrator and run: | Questa copia è stata installata da Scoop per tutti gli utenti. Per aggiornarla, chiudi InstallerClean, apri un terminale come amministratore ed esegui: |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then open InstallerClean to update. | Questa versione richiede .NET Framework 4.8. Installalo dal sito di Microsoft, riavvia il PC, poi apri InstallerClean per aggiornare. |
| InstallerClean {0} can't be installed automatically. | InstallerClean {0} non può essere installato automaticamente. |
| InstallerClean can't tell whether its setup installed it, so it can't update itself. | InstallerClean non riesce a stabilire se è stato installato dal suo programma di installazione, quindi non può aggiornarsi. |
| Copied | Copiato |
| Couldn't copy it. Select the command and copy it yourself. | Impossibile copiarlo. Seleziona il comando e copialo a mano. |
| _Continue without updating | _Continua senza aggiornare |
| Downloading InstallerClean {0}... | Download di InstallerClean {0}... |
| Checking the download... | Verifica del download... |
| The update couldn't be downloaded. | Non è stato possibile scaricare l'aggiornamento. |
| The update couldn't be installed. | Non è stato possibile installare l'aggiornamento. |
| Couldn't reach GitHub. Check your internet connection and try again. | Impossibile raggiungere GitHub. Controlla la connessione internet e riprova. |
| GitHub returned an error response. Try again in a few minutes. | GitHub ha restituito una risposta di errore. Riprova tra qualche minuto. |
| GitHub's response did not contain a recognised release. Try again later, or open the releases page directly. | La risposta di GitHub non conteneva una release riconoscibile. Riprova più tardi, o apri direttamente la pagina delle release. |
| The check timed out. Your connection to GitHub may be slow; try again. | Il controllo è scaduto. La connessione a GitHub potrebbe essere lenta; riprova. |
| The check failed for an unknown reason. Details are in {0} if you need to report it. | Il controllo non è riuscito per un motivo sconosciuto. I dettagli sono in {0}, se devi segnalarlo. |
| The check failed for an unknown reason. The crash log could not be written. | Il controllo non è riuscito per un motivo sconosciuto. Non è stato possibile scrivere il file crash.log. |

## Opening links in your browser

| English | Italiano |
| --- | --- |
| Couldn't open your browser | Impossibile aprire il browser |
| The link is on your clipboard, so you can paste it in yourself:<br><br>{0} | Il link è negli appunti, così puoi incollarlo tu:<br><br>{0} |
| InstallerClean couldn't copy the link to your clipboard either, so here it is:<br><br>{0} | InstallerClean non è riuscito nemmeno a copiare il link negli appunti, eccolo:<br><br>{0} |

## Startup and crashes

| English | Italiano |
| --- | --- |
| InstallerClean | InstallerClean |
| It's already running. | È già in esecuzione. |
| InstallerClean | InstallerClean |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>Details written to:<br>{1} | Si è verificato un errore imprevisto e InstallerClean verrà chiuso.<br><br>{0}<br><br>Dettagli salvati in:<br>{1} |
| An unexpected error occurred and InstallerClean needs to close.<br><br>{0}<br><br>The crash log could not be written. | Si è verificato un errore imprevisto e InstallerClean verrà chiuso.<br><br>{0}<br><br>Non è stato possibile scrivere il file crash.log. |
| Startup error | Errore in avvio |
| Failed to start ({0}). Details written to:<br>{1} | Avvio non riuscito ({0}). Dettagli salvati in:<br>{1} |
| Failed to start ({0}). The crash log could not be written. | Avvio non riuscito ({0}). Non è stato possibile scrivere il file crash.log. |
| Administrator rights needed | Servono i diritti di amministratore |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed.<br><br>Run InstallerClean again as administrator. | InstallerClean ha bisogno dei diritti di amministratore per vedere tutti i programmi installati su questo PC e non è in esecuzione come amministratore, quindi non eseguirà la scansione. Non è stato modificato nulla.<br><br>Esegui di nuovo InstallerClean come amministratore. |
| # crash.log captures unhandled exceptions from InstallerClean.<br># Under elevation the framework's exception messages can include<br># file paths from the running session (including other users'<br># profiles enumerated by Windows Installer queries). Network-<br># failure messages from the update check or result-log POST can<br># include the destination URL and the resolved IP / proxy address.<br># Entries about unreadable Windows Installer records can include a<br># Windows account SID (S-1-5-21-...) and the product codes of<br># installed software.<br># Redact all three classes of detail before attaching this file to<br># a public bug report.<br> | # crash.log raccoglie le eccezioni non gestite di InstallerClean.<br># Con privilegi elevati, i messaggi di eccezione del framework possono<br># includere percorsi di file della sessione in corso (compresi i<br># profili di altri utenti enumerati dalle query di Windows Installer).<br># I messaggi di errore di rete del controllo aggiornamenti o dell'invio<br># del registro dei risultati possono includere l'URL di destinazione e<br># l'indirizzo IP o proxy risolto. Le voci sui record di Windows<br># Installer illeggibili possono includere un SID di account Windows<br># (S-1-5-21-...) e i codici prodotto del software installato.<br># Rimuovi tutte e tre le categorie di dati prima di allegare questo<br># file a una segnalazione di bug pubblica.<br> |

## Tooltips (hover text)

| English | Italiano |
| --- | --- |
| Any tip is much appreciated | Ogni mancia è molto apprezzata |
| Cancellation requested. InstallerClean is waiting for the current step to reach a stopping point. This can take a few seconds during heavy I/O or an MSI database call. | Annullamento richiesto. InstallerClean sta aspettando che il passaggio in corso arrivi a un punto in cui fermarsi. Può richiedere qualche secondo durante operazioni di I/O intense o una chiamata al database MSI. |
| Close | Chiudi |
| I really should charge for InstallerClean - because it's been a huge amount of work - but I want to keep it free so it helps as many people as possible. Your donation makes it worthwhile and is very much appreciated. | Dovrei proprio far pagare InstallerClean, perché mi è costato un'enorme quantità di lavoro, ma voglio mantenerlo gratuito perché aiuti quante più persone possibile. La tua donazione ripaga lo sforzo ed è molto apprezzata. |
| A star helps other people find it | Una stella aiuta altre persone a trovare InstallerClean |
| Minimise | Riduci a icona |
| Move the unneeded files to the backup folder | Sposta i file non necessari nella cartella di backup |
| Move the unneeded files to a backup folder. You'll choose it next. | Sposta i file non necessari in una cartella di backup. La scegli subito dopo. |
| Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder. | Sposta i file non necessari nella cartella di backup. È sullo stesso disco, quindi non recuperi lo spazio finché non elimini quella cartella. |
| Delete the unneeded files permanently. Use Move instead if you'd like a chance to satisfy yourself all is well. | Elimina definitivamente i file non necessari. Usa invece Sposta se vuoi la possibilità di convincerti che vada tutto bene. |
| Subject name from the embedded Authenticode certificate. Not chain-verified. | Nome soggetto dal certificato Authenticode incorporato. Catena non verificata. |
| Change language. The program will restart. | Cambia lingua. Il programma verrà riavviato. |

## Screen reader labels

| English | Italiano |
| --- | --- |
| Leave a tip | Lascia una mancia |
| Leave a tip | Lascia una mancia |
| Leave a tip | Lascia una mancia |
| Cancel operation | Annulla operazione |
| Cancel scan | Annulla scansione |
| Cancel startup scan | Annulla scansione all'avvio |
| Carry on without it, drive {0} | Continua senza attendere, unità {0} |
| Carry on without it, {0} | Continua senza attendere, {0} |
| Close | Chiudi |
| Close window | Chiudi finestra |
| Close result and return to main window | Chiudi finestra risultato e torna alla finestra principale |
| Leave a star on github | Lascia una stella su github |
| Minimise | Riduci a icona |
| Delete permanently removes the unneeded files. Cancel closes without deleting. | Elimina definitivamente rimuove i file non necessari. Annulla chiude senza eliminare nulla. |
| Move puts the unneeded files in the chosen destination folder. Cancel leaves them where they are. | 'Sposta' colloca i file non necessari nella cartella destinazione scelta. 'Annulla' li lascia dove sono. |
| Say thanks | Per ringraziarmi |
| Check for updates | Controlla aggiornamenti |
| Checks github's releases page for a newer version. | Verifica sulla pagina release di github se esiste una versione più recente. |
| Opens the readme on github in your browser. | Apre readme su github nel browser. |
| Opens the issue tracker on github.com in your browser. | Apre elenco problemi (issue) in github.com nel browser. |
| If ticked, InstallerClean checks github for a newer version when you run it. | Se selezionata, all'avvio InstallerClean verifica su github se è disponibile una versione più recente. |
| Download it from github | Scaricalo da github |
| Download .NET Framework 4.8 from Microsoft | Scarica .NET Framework 4.8 da Microsoft |
| Copy the command | Copia il comando |
| Scoop's update command | Comando di aggiornamento di Scoop |
| Opens the licence file on github.com in your browser. | Apre file licenza in github.com nel browser. |
| Backup folder | Cartella di backup |
| Patches | Patch |
| Product details | Dettagli prodotto |
| Backup folder | Cartella di backup |
| Operation progress | Avanzamento operazione |
| Scan {InstallerFolder} again | Nuova scansione di {InstallerFolder} |
| Scanning progress | Avanzamento scansione |
| Startup scan progress | Avanzamento scansione all'avvio |
| Details, unneeded files | Dettagli, file non necessari |
| Available for cleanup. | Disponibili per la pulizia. |
| Details, files left alone | Dettagli, file lasciati stare |
| Read-only inventory. | Elenco sola lettura. |
| Sorted by {0}, ascending | Ordinati per {0}, crescente |
| Sorted by {0}, descending | Ordinati per {0}, decrescente |
| Scan results | Risultati scansione |
| Result details | Dettagli risultato |
| File details | Dettagli file |
| Product details | Dettagli del prodotto |
| Dialog text | Testo finestra di dialogo |
| Waiting for a drive or share | In attesa di un'unità o condivisione |
| {0} ({1}) | {0} ({1}) |
| Files that could not be processed | File che non è stato possibile elaborare |
| Explains this folder, and how to recover a file, in the README | Spiega questa cartella, e come recuperare un file, nel README |
| {0} Press Alt+{1} to carry on without it. {2} | {0} Premi Alt+{1} per continuare senza attendere. {2} |
| What the report holds | Cosa contiene il rapporto |
| What the report holds | Cosa contiene il rapporto |
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
| ,  | ,  |
| drive {0} | unità {0} |
| .  | .  |
| {0} TB | {0} TB |
| {0} GB | {0} GB |
| {0} MB | {0} MB |
| {0} KB | {0} KB |
| {0} B | {0} B |
| {0:F0}ms | {0:F0}ms |
| {0:F1}s | {0:F1}s |
| less than a second | meno di un secondo |
| {0:F1} seconds | {0:F1} secondi |

## Command-line tool (installerclean-cli)

| English | Italiano |
| --- | --- |
| Error: unknown argument '{0}' | Errore: argomento sconosciuto '{0}' |
| Error: unexpected extra argument '{0}'. If your move folder has a space in it, put quotes around the whole path: /m "D:\My Backup" | Errore: argomento aggiuntivo imprevisto '{0}'. Se la cartella di destinazione contiene uno spazio, racchiudi l'intero percorso tra virgolette: /m "D:\My Backup" |
| Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run. | Errore: argomento aggiuntivo inatteso '{0}'. /s e /d non accettano altri argomenti, e si può usare una sola opzione per esecuzione. |
| Cancelling... | Annullamento... |
| Cancelled. | Operazione annullata. |
| Error: unexpected failure ({0}). Details written to {1}. | Errore: crash inatteso ({0}). Dettagli scritti in {1}. |
| Error: unexpected failure ({0}). The crash log could not be written. | Errore: crash inatteso ({0}). Non è stato possibile scrivere il registro dei crash. |
| Scanning {InstallerFolder}... | Scansione di {InstallerFolder}... |
| Found {0} unneeded {1} to clean up ({2}). | Trovati {0} {1} non necessari da eliminare ({2}). |
| Found no unneeded files. | Nessun file non necessario trovato. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean non è riuscito a stabilire con certezza quali file nella cache appartengono ai programmi installati qui, perciò ha trattenuto l'unico file ({2}) invece di proporlo. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean non è riuscito a stabilire con certezza quali file nella cache appartengono ai programmi installati qui, perciò ha trattenuto {0} {1} ({2}) invece di proporli. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean non è riuscito ad accertare che un file non sia necessario, perciò lo ha trattenuto ({2}) invece di proporlo. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean non è riuscito ad accertare che {0} {1} non siano necessari, perciò li ha trattenuti ({2}) invece di proporli. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back the one file ({2}) rather than offering it. | InstallerClean non è riuscito a stabilire con certezza quali file nella cache appartengono ai programmi installati qui, perciò ha trattenuto l'unico file ({2}) invece di proporlo. |
| InstallerClean couldn't be certain which cached files belong to the programs installed here, so it has held back {0} {1} ({2}) rather than offering them. | InstallerClean non è riuscito a stabilire con certezza quali file nella cache appartengono ai programmi installati qui, perciò ha trattenuto {0} {1} ({2}) invece di proporli. |
| InstallerClean couldn't establish that one file is unneeded, so it has held it back ({2}) rather than offering it. | InstallerClean non è riuscito ad accertare che un file non sia necessario, perciò lo ha trattenuto ({2}) invece di proporlo. |
| InstallerClean couldn't establish that {0} {1} are unneeded, so it has held them back ({2}) rather than offering them. | InstallerClean non è riuscito ad accertare che {0} {1} non siano necessari, perciò li ha trattenuti ({2}) invece di proporli. |
| Why it couldn't be certain: | Perché non è stato possibile averne certezza: |
|   A file path in Windows Installer's own records wouldn't resolve, so nothing could be matched to it. |   Un percorso di file negli archivi di Windows Installer non si è risolto, perciò non è stato possibile associarvi nulla. |
|   A file Windows has a record of couldn't be identified, so it couldn't be matched to what's in the folder. |   Non è stato possibile identificare un file di cui Windows ha un archivio, perciò non è stato possibile confrontarlo con il contenuto della cartella. |
|   A program may be installed more than once on this PC, and the records can't say which copy a file belongs to. |   Un programma potrebbe essere installato più di una volta su questo PC, e gli archivi non possono dire a quale copia appartenga un file. |
|   Part of Windows Installer's records wouldn't read, so a file could belong to a program recorded there. |   Non è stato possibile leggere una parte degli archivi di Windows Installer, perciò un file potrebbe appartenere a un programma registrato lì. |
|   A file in the folder couldn't be identified, so it couldn't be matched against the records. |   Non è stato possibile identificare un file nella cartella, perciò non è stato possibile confrontarlo con gli archivi. |
|   Either a file wouldn't say which program it belongs to, or Windows wouldn't give a clear answer about that program. |   O un file non ha indicato a quale programma appartiene, oppure Windows non ha dato una risposta chiara riguardo a quel programma. |
|   A check on which programs the files belong to gave answers that didn't line up with the files it was handed. |   Un controllo su a quali programmi appartengono i file ha dato risposte che non corrispondevano ai file ricevuti. |
|   Either a patch file wouldn't say which patch it is, or it wouldn't say which programs it's for, or Windows wouldn't say in full which programs have that patch. |   O un file di patch non ha indicato di quale patch si tratta, oppure non ha indicato a quali programmi è destinato, oppure Windows non ha indicato in modo completo quali programmi hanno quella patch. |
| Windows has a record for {0} file that is not in {InstallerFolder}: {1}. It causes no trouble day to day, but an update or uninstall of that program can fail. To put the file back, you need the installer for the version you already have. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs this file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows ha un record per {0} file che non è in {InstallerFolder}: {1}. Nell'uso quotidiano non crea problemi, ma un aggiornamento o una disinstallazione di quel programma può fallire. Per rimettere il file, ti serve il programma di installazione della versione che hai già. Procuratelo dal produttore del programma ed eseguilo sopra la copia esistente. Una versione più recente non va bene: dovrebbe prima rimuovere quella che hai, ed è proprio il passaggio che ha bisogno di questo file. Nemmeno disinstallare prima funziona, per lo stesso motivo. Questo dovrebbe ripristinare il file e lasciare intatte le tue impostazioni, ma Microsoft non lo garantisce. |
| Windows has records for {0} files that are not in {InstallerFolder}: {1}. They cause no trouble day to day, but an update or uninstall of those programs can fail. To put a file back, you need the installer for the version you already have of that program. Get it from the program's maker and run it over your existing copy. A newer version won't do: it has to remove the one you've got first, and that's the step that needs the file. Uninstalling first won't work either, for the same reason. This should restore the file and leave your settings alone, but Microsoft doesn't guarantee it. | Windows ha record per {0} file che non sono in {InstallerFolder}: {1}. Nell'uso quotidiano non creano problemi, ma un aggiornamento o una disinstallazione di quei programmi può fallire. Per rimettere un file, ti serve il programma di installazione della versione di quel programma che hai già. Procuratelo dal produttore del programma ed eseguilo sopra la copia esistente. Una versione più recente non va bene: dovrebbe prima rimuovere quella che hai, ed è proprio il passaggio che ha bisogno del file. Nemmeno disinstallare prima funziona, per lo stesso motivo. Questo dovrebbe ripristinare il file e lasciare intatte le tue impostazioni, ma Microsoft non lo garantisce. |
| InstallerClean couldn't be certain that the one superseded file is no longer needed, so it has held it back. | InstallerClean non è riuscito a stabilire con certezza che l'unico file sostituito non serva più, perciò l'ha trattenuto. |
| InstallerClean couldn't be certain that {0} superseded files are no longer needed, so it has held them back. | InstallerClean non è riuscito a stabilire con certezza che {0} file sostituiti non servano più, perciò li ha trattenuti. |
| InstallerClean stopped waiting for drive {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean ha smesso di attendere l'unità {0} e ha lasciato stare ogni file ancora da confrontare con essa. Quando torna a rispondere normalmente, esegui di nuovo il comando. |
| InstallerClean stopped waiting for {0} and left alone any file still to be checked against it. Once it's responding normally, run the command again. | InstallerClean ha smesso di attendere {0} e ha lasciato stare ogni file ancora da confrontare con ciò che vi si trova. Quando torna a rispondere normalmente, esegui di nuovo il comando. |
| InstallerClean stopped waiting for more than one drive or share ({0}) and left alone any file still to be checked against one of them. Once they're responding normally, run the command again. | InstallerClean ha smesso di attendere più di un'unità o condivisione ({0}) e ha lasciato stare ogni file ancora da confrontare con una di esse. Quando tornano a rispondere normalmente, esegui di nuovo il comando. |
| Deleting {0} unneeded {1}... | Eliminazione di {0} {1} non necessari... |
| Permanently deleted {0} unneeded {1}. | Eliminati definitivamente {0} {1} non necessari. |
| Error: no move destination specified. Use /m PATH. (A default set in the GUI is per-user and does not apply to scheduled or service-account runs.) | Errore: nessuna destinazione di spostamento specificata. Usa /m PERCORSO. (Una destinazione predefinita impostata nella GUI è specifica per utente e non si applica alle esecuzioni pianificate o con account di servizio.) |
| Error: destination cannot be inside the Windows Installer folder. | Errore: la destinazione non può trovarsi all'interno della cartella di Windows Installer. |
| Error: destination must be a fully qualified path. Got: {0} | Errore: la destinazione deve essere un percorso completo. Ricevuto: {0} |
| Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%. | Errore: la destinazione {0} si risolve dentro una cartella di sistema di Windows. Scegli un percorso fuori da %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% e %ProgramData%. |
| Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved. | Errore: spazio insufficiente in {0}. Spostare questi file richiede {1} e ne sono liberi {2}. Non è stato spostato nulla. |
| Error: something is using Windows Installer right now, such as a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes. | Errore: qualcosa sta usando Windows Installer in questo momento, ad esempio un aggiornamento di Windows o un programma che si installa in background. /m e /d sono bloccati mentre accade. Riprova quando ha finito. |
| Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}. | Errore: su questo computer c'è una transazione di Windows Installer sospesa. Riprendi o annulla quell'installazione (o riavvia Windows) prima di ripulire {InstallerFolder}. |
| Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning. | Errore: un'operazione su file in coda dopo il riavvio riguarda {InstallerFolder} ({0}). Riavvia Windows per completare quell'operazione prima di ripulire. |
| Error: a file operation is queued for the next restart and InstallerClean can't tell which files it names, so it can't rule out {InstallerFolder}. Restart Windows before cleaning. | Errore: un'operazione sui file è in coda per il prossimo riavvio e InstallerClean non riesce a sapere quali file nomina, quindi non può escludere {InstallerFolder}. Riavvia Windows prima di pulire. |
| Error: InstallerClean couldn't read one of the registry values it checks before touching {InstallerFolder}, so it can't rule out a Windows Installer operation in flight or queued for the next restart. /m and /d are blocked. Restart Windows and try again. If the read still fails, this isn't a machine InstallerClean can clean. | Errore: InstallerClean non è riuscito a leggere uno dei valori di registro che controlla prima di toccare {InstallerFolder}, quindi non può escludere un'operazione di Windows Installer in corso o in coda per il prossimo riavvio. /m e /d sono bloccati. Riavvia Windows e riprova. Se la lettura continua a non riuscire, questo non è un computer che InstallerClean possa pulire. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Errore: InstallerClean non è riuscito a controllare se Windows Installer è occupato, quindi non può escludere un'installazione in corso. /m e /d sono bloccati. Riavvia Windows e riprova. Se il controllo continua a non riuscire, questo non è un computer che InstallerClean possa pulire. |
| Error: InstallerClean couldn't check whether Windows Installer is busy, because Windows gave an error: {0} (error {1}), so it can't rule out an installation in progress. /m and /d are blocked. Restart Windows and try again. If the check still fails, this isn't a machine InstallerClean can clean. | Errore: InstallerClean non è riuscito a controllare se Windows Installer è occupato, perché Windows ha restituito un errore: {0} (errore {1}), quindi non può escludere un'installazione in corso. /m e /d sono bloccati. Riavvia Windows e riprova. Se il controllo continua a non riuscire, questo non è un computer che InstallerClean possa pulire. |
| Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes. | Errore: Windows Installer ha qualcosa in corso, quindi /m e /d sono bloccati. InstallerClean non tocca {InstallerFolder} mentre cambia. Riprova quando ha finito. |
| Something was using Windows Installer when the scan started, such as a Windows Update or a program installing, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has finished. | Qualcosa stava usando Windows Installer all'avvio della scansione, ad esempio un aggiornamento di Windows o un programma che si installava, perciò InstallerClean ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Esegui di nuovo il comando quando ha finito. |
| A Windows Installer transaction was suspended on this machine when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Resume or roll back that install, or restart Windows, then run it again. | All'avvio della scansione, su questo computer c'era una transazione di Windows Installer sospesa, perciò InstallerClean ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Riprendi o annulla quell'installazione, oppure riavvia Windows, poi esegui di nuovo il comando. |
| When the scan started, Windows had a file rename queued for the next restart that affects {InstallerFolder}, so InstallerClean held back the files there that no installed program's records name, rather than offering them. Restart Windows, then run it again. | All'avvio della scansione, Windows aveva in coda per il prossimo riavvio una ridenominazione di file che riguarda {InstallerFolder}, perciò InstallerClean ha trattenuto i file lì presenti non nominati nei record di alcun programma installato, invece di proporli. Riavvia Windows, poi esegui di nuovo il comando. |
| When the scan started, a file operation was queued for the next restart and InstallerClean couldn't tell which files it names, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows, then run it again. | All'avvio della scansione, un'operazione sui file era in coda per il prossimo riavvio e InstallerClean non riusciva a sapere quali file nomina, perciò ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Riavvia Windows, poi esegui di nuovo il comando. |
| When the scan started, Windows refused InstallerClean permission to check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | All'avvio della scansione, Windows ha negato a InstallerClean il permesso di controllare se Windows Installer era occupato, perciò ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Riavvia Windows ed esegui di nuovo il comando. |
| When the scan started, InstallerClean couldn't read one of the Windows settings it checks, so it couldn't tell whether an installer operation was running or waiting for a restart, and it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | All'avvio della scansione, InstallerClean non è riuscito a leggere una delle impostazioni di Windows che controlla, quindi non poteva sapere se un'operazione di installazione era in corso o in attesa di un riavvio, e ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Riavvia Windows ed esegui di nuovo il comando. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, so it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | All'avvio della scansione, InstallerClean non è riuscito a controllare se Windows Installer era occupato, perciò ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Riavvia Windows ed esegui di nuovo il comando. |
| When the scan started, InstallerClean couldn't check whether Windows Installer was busy, because Windows gave an error: {0} (error {1}). So it held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Restart Windows and run it again. | All'avvio della scansione, InstallerClean non è riuscito a controllare se Windows Installer era occupato, perché Windows ha restituito un errore: {0} (errore {1}). Perciò ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Riavvia Windows ed esegui di nuovo il comando. |
| Windows Installer had something in progress or waiting when the scan started, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again once that has cleared. | Windows Installer aveva qualcosa in corso o in attesa all'avvio della scansione, perciò InstallerClean ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Esegui di nuovo il comando quando si è risolto. |
| This PC's clock went back or lost time while the scan was running, so InstallerClean held back the files in {InstallerFolder} that no installed program's records name, rather than offering them. Run it again. | L'orologio di questo PC è tornato indietro o ha perso tempo durante la scansione, perciò InstallerClean ha trattenuto i file in {InstallerFolder} non nominati nei record di alcun programma installato, invece di proporli. Esegui di nuovo il comando. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening. | Errore: InstallerClean non è riuscito a prendere il blocco di Windows Installer che impedisce a due programmi di modificare il software installato nello stesso momento, quindi non ha potuto escludere che un file diventasse necessario a metà strada. Non è stato eliminato nulla. Riprova, e riavvia Windows se continua a succedere. |
| Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. Try again, and restart Windows if it keeps happening. | Errore: InstallerClean non è riuscito a prendere il blocco di Windows Installer che impedisce a due programmi di modificare il software installato nello stesso momento, quindi non ha potuto escludere che un file diventasse necessario a metà strada. Non è stato spostato nulla. Riprova, e riavvia Windows se continua a succedere. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. | Errore: Windows ha negato a InstallerClean il permesso di controllare se Windows Installer fosse occupato, quindi non ha potuto escludere che un file servisse a metà strada. Non è stato eliminato nulla. |
| Error: Windows refused InstallerClean permission to check whether Windows Installer was busy, so it couldn't rule out a file becoming needed part-way through. Nothing has been moved. | Errore: Windows ha negato a InstallerClean il permesso di controllare se Windows Installer fosse occupato, quindi non ha potuto escludere che un file servisse a metà strada. Non è stato spostato nulla. |
| Moving {0} unneeded {1} to {2}... | Spostamento di {0} {1} non necessari in {2}... |
| Moved {0} unneeded {1}. | Spostati {0} {1} non necessari. |
| Check that your programs still update and uninstall as normal, then delete {0}. | Verifica che i tuoi programmi si aggiornino e si disinstallino ancora normalmente, poi elimina {0}. |
| InstallerClean is free. If it helped, a tip is much appreciated: {0} | InstallerClean è gratuito. Se ti è stato utile, una mancia è molto apprezzata: {0} |
| Update now? It's strongly recommended to use the latest version. [{0}/{1}] | Aggiornare ora? Si consiglia vivamente di usare la versione più recente. [{0}/{1}] |
| Y | S |
| N | N |
| No answer after a minute, so InstallerClean updates without asking, as it does when nobody is there to answer. | Nessuna risposta dopo un minuto, quindi InstallerClean si aggiorna senza chiedere, come quando non c'è nessuno a rispondere. |
| Send an anonymous report of this run? It is saved at {0}. Type ? to see what it holds. [{1}/{2}/?] | Inviare un rapporto anonimo di questa esecuzione? È salvato in {0}. Digita ? per vedere cosa contiene. [{1}/{2}/?] |
| the chart at {0} | questo grafico all'indirizzo {0} |
| The report adds your result to {0}, which shows how much space people are reclaiming with InstallerClean. It also holds these technical details, to help me spot any bugs: | Il rapporto aggiunge il tuo risultato a {0}, che mostra quanto spazio le persone stanno recuperando con InstallerClean. Contiene anche questi dettagli tecnici, che mi aiutano a individuare eventuali bug: |
| The full report is printed below. | Il rapporto completo è riportato qui sotto. |
| Report sent. Thank you. | Rapporto inviato. Grazie. |
| Report not sent. | Rapporto non inviato. |
| No answer after a minute. | Nessuna risposta dopo un minuto. |
| The report could not be sent. It is kept in {0}. | Non è stato possibile inviare il rapporto. È conservato in {0}. |
| Sending a report left waiting by an earlier run. | Invio di un rapporto in attesa da un'esecuzione precedente. |
| The new version will scan and list what it finds, without deleting anything. To delete, run /d again afterwards. | La nuova versione eseguirà la scansione ed elencherà ciò che trova, senza eliminare nulla. Per eliminare, esegui di nuovo /d al termine. |
| The new version will scan and list what it finds, without moving anything. To move, run /m again afterwards. | La nuova versione eseguirà la scansione ed elencherà ciò che trova, senza spostare nulla. Per spostare, esegui di nuovo /m al termine. |
| InstallerClean is in use, so it can't update itself now. | InstallerClean è in uso, quindi ora non può aggiornarsi. |
| Running {0} with InstallerClean {1} instead. | Si esegue invece {0} con InstallerClean {1}. |
| The newer version couldn't be run. | Non è stato possibile eseguire la versione più recente. |
| Running {0} with InstallerClean {1}. It takes this copy's place once the run ends. | Si esegue {0} con InstallerClean {1}. Prenderà il posto di questa copia al termine dell'esecuzione. |
| {0}: {1} | {0}: {1} |
| It needs .NET Framework 4.8. Install it from Microsoft, restart the PC, then run InstallerClean again to update. | Questa versione richiede .NET Framework 4.8. Installalo dal sito di Microsoft, riavvia il PC, poi esegui di nuovo InstallerClean per aggiornare. |
| It's simple to undo. Move them back from {0} into {InstallerFolder} and everything will be back to how it was. | Annullarlo è semplice. Rispostali da {0} in {InstallerFolder} e tornerà tutto com'era. |
| InstallerClean could no longer confirm the backup folder, so it went no further. Check {0}, then run the command again. | InstallerClean non è più riuscito a confermare la cartella di backup, quindi si è fermato. Controlla {0}, poi esegui di nuovo il comando. |
| Another InstallerClean process holds the single-instance lock (GUI or another CLI run). Exit 75 (transient); safe to retry later. | Un altro processo InstallerClean mantiene il blocco a istanza singola (la GUI o un'altra esecuzione della CLI). Codice di uscita 75 (transitorio); è sicuro riprovare più tardi. |
| Stopped after deleting {0} of {1} {2}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Interrotto dopo aver eliminato {0} di {1} {2}: InstallerClean non poteva più essere certo che nessun altro InstallerClean stesse agendo. Esegui di nuovo lo stesso comando. Codice di uscita 2 (parziale). |
| Stopped after moving {0} of {1} {2} to {3}: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 2 (partial). | Interrotto dopo aver spostato {0} di {1} {2} in {3}: InstallerClean non poteva più essere certo che nessun altro InstallerClean stesse agendo. Esegui di nuovo lo stesso comando. Codice di uscita 2 (parziale). |
| Stopped before deleting any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Interrotto prima di eliminare qualsiasi file: InstallerClean non poteva più essere certo che nessun altro InstallerClean stesse agendo. Esegui di nuovo lo stesso comando. Codice di uscita 75 (transitorio). |
| Stopped before moving any file: InstallerClean could no longer be sure that no other InstallerClean was acting. Run the same command again. Exit 75 (transient). | Interrotto prima di spostare qualsiasi file: InstallerClean non poteva più essere certo che nessun altro InstallerClean stesse agendo. Esegui di nuovo lo stesso comando. Codice di uscita 75 (transitorio). |
| InstallerClean needs administrator rights to see every program installed on this PC, and it isn't running as administrator, so it won't scan. Nothing has been changed. Run it again from an administrator prompt. | InstallerClean ha bisogno dei diritti di amministratore per vedere tutti i programmi installati su questo PC e non è in esecuzione come amministratore, quindi non eseguirà la scansione. Non è stato modificato nulla. Eseguilo di nuovo da un prompt come amministratore. |
| Note: Event Log writing failed. Check Application log permissions or Group Policy. | Nota: scrittura nel registro eventi non riuscita. Controlla i permessi del registro Applicazione o i Criteri di gruppo. |
| InstallerClean - clean up {InstallerFolder} | InstallerClean - pulizia di {InstallerFolder} |
| Removes cached .msi and .msp files that no installed program still needs. | Rimuove i file .msi/.msp in cache che nessun programma installato usa più. |
| Needs an elevated (administrator) prompt and won't scan without one. | Richiede un prompt come amministratore; senza, non esegue la scansione. |
| Usage: | Utilizzo: |
|   installerclean-cli --help     Show this help (also accepts /?, -h) |   installerclean-cli --help       Mostra questa guida (anche /?, -h) |
|   installerclean-cli --version  Print the version (also accepts -v) |   installerclean-cli --version    Mostra la versione (anche -v) |
|   installerclean-cli /s         Scan only - list unneeded files |   installerclean-cli /s           Solo scansione - elenca i superflui |
|   installerclean-cli /d         Delete unneeded files permanently |   installerclean-cli /d           Elimina definitivamente i superflui |
|   installerclean-cli /m         Move to the saved backup folder |   installerclean-cli /m           Sposta nella cartella salvata |
|   installerclean-cli /m PATH    Move to specified path |   installerclean-cli /m PERCORSO  Sposta nel percorso specificato |
| installerclean-cli blocks the prompt until it finishes, so a script or<br>scheduled task can wait on it. | installerclean-cli blocca il prompt finché non termina, così uno script<br>o un'operazione pianificata può attenderlo. |
| That folder is saved per-user; scheduled or SYSTEM runs need /m PATH. | Cartella per utente; esecuzioni pianificate o SYSTEM: /m PERCORSO. |
| Before it scans, it checks for a newer version of InstallerClean.<br>When you run it yourself, it asks before updating, and updates if nobody<br>answers within a minute. When it runs on its own, in a scheduled task or<br>a script, it updates without asking.<br>Add /noupdate to skip the check. | Prima della scansione controlla se esiste una versione più recente di<br>InstallerClean. Se lo avvii tu, chiede prima di aggiornare, e si aggiorna<br>se nessuno risponde entro un minuto. Se viene eseguito da solo, in<br>un'operazione pianificata o in uno script, si aggiorna senza chiedere.<br>Aggiungi /noupdate per saltare il controllo. |
| Exit codes: | Codici di uscita: |
|   0   success: the run did what it was asked |   0   riuscito: ha fatto quanto richiesto |
|   1   nothing processed: the run failed or was refused |   1   nulla elaborato: l'esecuzione è fallita o è stata rifiutata |
|   2   partial: some processed, some not (a failure or a Ctrl+C part way) |   2   parziale: alcuni elaborati, altri no (un errore o un Ctrl+C) |
|   75  transient: a temporary condition blocked the run (see the message) |   75  transitorio: qualcosa ha bloccato l'esecuzione (vedi il messaggio) |
|   130 cancelled (Ctrl+C) |   130 annullato (Ctrl+C) |
