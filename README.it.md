<p align="center">
  <a href="README.md">English</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.es.md">Español</a> · <a href="README.ja.md">日本語</a> · <a href="README.pt-BR.md">Português (BR)</a> · <a href="README.ru.md">Русский</a> · <a href="README.fr.md">Français</a> · <strong>Italiano</strong>
</p>

<p align="center"><em>Anche l'interfaccia dell'app è disponibile in italiano: segue la lingua di Windows e si può cambiare dal menu della lingua nell'app.</em></p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<p align="center"><em>🎶 What's my line? I'm happy <a href="https://www.youtube.com/watch?v=HM-jHhUZfFI">cleaning Windows</a></em></p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong>Uno strumento open source per pulire in sicurezza <code>C:\Windows\Installer</code>, la cartella nascosta di Windows che si mangia in silenzio il tuo spazio su disco.</strong></p>

<p align="center"><em>Usala una volta. Magari liberi un po' di spazio. Buttala via.</em></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-blue.svg" alt="Licenza: MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/badge/release-v1.9.2-blue" alt="Versione di GitHub"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/downloads-23k-brightgreen" alt="Download totali"></a>
</p>

![Schermata di InstallerClean dopo una pulizia riuscita: 1,28 GB liberati, 69 file inviati al Cestino](docs/screenshots/06-freed-success-done.webp)

- **Cosa fa:** InstallerClean fa una cosa sola: rimuove i file non necessari da `C:\Windows\Installer`, una cartella nascosta che Windows non pulisce mai. Dopo una scansione quasi istantanea ti dice se ne hai, mostra qualche dettaglio in più per i curiosi e ti lascia eliminarli per liberare spazio sull'unità C:. La usi una volta e passi oltre.
- **Quanto spazio:** I report (opzionali) inviati finora mostrano che il <!-- reports-freedpct-start -->41%<!-- reports-freedpct-end --> dei computer aveva file non necessari da pulire. Di questi, la mediana liberata è di <!-- reports-median-start -->22 GB<!-- reports-median-end -->. Alcuni hanno liberato centinaia di GB. Nel mio caso, 1,28 GB. Il restante <!-- reports-nothingpct-start -->59%<!-- reports-nothingpct-end --> non ha trovato nulla da rimuovere, il che significa solo che la loro cartella Installer era già pulita. Più dettagli nelle [Domande frequenti](#domande-frequenti) più sotto.
- **È sicuro:** Sì. Chiede alla stessa API di Windows Installer quali file servono ancora ed elenca solo quelli che Windows segnala come non più necessari. È open source (MIT) e non chiede nulla su di te: nessun account, nessuna pubblicità, nessun tracciamento, nessuna telemetria, niente che giri in background. Non si collega mai a internet da solo.
- **Come ottenerla:** [Scarica l'ultima versione](../../releases/latest). Eseguila; supera [l'avviso di Windows](#unknown-publisher) e [la richiesta di amministratore](#admin). Elimina i file non necessari. Fatto.

## Indice

- [La cartella di cui nessuno ti parla](#la-cartella-di-cui-nessuno-ti-parla)
- [La ricerca di aiuto](#la-ricerca-di-aiuto)
- [Cosa fa](#cosa-fa)
- [Schermate](#schermate)
- [Come funziona](#come-funziona)
- [È sicuro?](#è-sicuro)
- [Se ti manca un file da C:\Windows\Installer](#recovery)
- [Accessibilità](#accessibilità)
- [Cosa non fa](#cosa-non-fa)
- [Domande frequenti](#domande-frequenti)
- [Download](#download)
- [Confronto con PatchCleaner](#confronto-con-patchcleaner)
- [Riga di comando](#riga-di-comando)
- [Requisiti](#requisiti)
- [Compilare dal codice sorgente](#compilare-dal-codice-sorgente)
- [Contribuire](#contribuire)
- [Sostieni il progetto](#sostieni-il-progetto)
- [Cronologia delle stelle](#cronologia-delle-stelle)
- [Licenza](#licenza)

---

## La cartella di cui nessuno ti parla

Su ogni PC Windows c'è una cartella nascosta chiamata `C:\Windows\Installer`. Ogni volta che installi un software che usa il sistema Windows Installer, o applichi una patch a Microsoft Office, Adobe Acrobat, Visual Studio o a qualunque altra applicazione basata su `.msi`, una copia di quell'installer o di quel file di patch `.msp` finisce in questa cartella, e lì resta.

Quando disinstalli il software, i file restano. Quando una patch più recente ne sostituisce una vecchia, restano entrambe. Windows non li pulisce mai. Pulizia disco non li tocca. DISM si occupa di tutt'altra cartella. Col tempo la cartella cresce: 1 GB, 5 GB, 20 GB, 50 GB. Sui computer con molto software basato su MSI (Acrobat è un colpevole frequente), può [superare i 100 GB](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/).

Non sono file temporanei che ritornano da soli. Sono peso morto a tutti gli effetti: vecchi installer di software che hai disinstallato anni fa e patch sostituite più volte. Una volta spariti, non tornano più.

**Se cerchi un modo semplice per liberare spazio su disco in Windows, questa cartella è un buon punto di partenza.** InstallerClean trova i file non necessari e li rimuove in sicurezza.

## La ricerca di aiuto

Se hai mai cercato aiuto per questa cartella, probabilmente sai come va a finire. Qualcuno con 180 GB in `C:\Windows\Installer` chiede come pulirla. Gli [dicono di eseguire Pulizia disco](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb). Ci prova. Libera 600 MB, nessuno dei quali da quella cartella (perché Pulizia disco non tocca `C:\Windows\Installer`). La discussione si spegne.

> *«Tutte le discussioni che ho trovato tendono a consigliare le stesse cose, che non risolvono il problema, e poi muoiono.»*
>
> [ksparks519, r/Windows10](https://www.reddit.com/r/Windows10/comments/1bt8c5p/anyone_ever_figure_out_giant_installer_folders/) (tradotto dall'inglese)

Oppure gli dicono di non toccarla affatto. In una discussione, a qualcuno con una cartella Installer da 60 GB è stato detto di [«non metterci mano».](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/) Quando ha chiesto cosa avrebbe dovuto fare invece, la risposta è stata: *«Te l'ho appena detto.»*

Il consiglio abituale confonde l'eliminare file a caso (cosa che è davvero pericolosa) con il rimuovere file che Windows stesso dà per non più necessari (cosa che non lo è). InstallerClean fa la seconda.

## Cosa fa

1. **Scansiona** `C:\Windows\Installer` alla ricerca di file `.msi` e `.msp`
2. **Interroga** l'API di Windows Installer per individuare quali file sono ancora registrati
3. **Mostra** quanto puoi liberare e quanto serve ancora, con finestre di dettaglio opzionali che elencano ogni file
4. **Rimuove** i file non necessari: li elimina nel Cestino, oppure li sposta in una cartella che scegli tu

## Schermate

<p>
  <img src="docs/screenshots/01-initial-scan.webp" alt="Schermata iniziale con il logo di InstallerClean mentre la scansione è in corso" width="900"><br>
  <em>Scansione iniziale. È molto rapida.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/02-main-window.webp" alt="Finestra principale con 120 file ancora necessari (2,83 GB) e 69 file non necessari da pulire (1,28 GB), con un campo per la destinazione spostamento e i pulsanti Elimina e Sposta" width="900"><br>
  <em>Risultati: quanto serve ancora, quanto è rimovibile.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/03-details-registered.webp" alt="Finestra dei file registrati con l'elenco dei prodotti installati e i dettagli del database del programma di installazione per il prodotto selezionato" width="900"><br>
  <em>Dettagli dei file ancora necessari, con i metadati letti dal database del programma di installazione.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/04-details-safe-to-delete.webp" alt="Finestra dei file non necessari con i file .msi rimovibili ordinati per dimensione, il motivo per cui ciascuno è rimovibile e i dettagli del file selezionato" width="900"><br>
  <em>Dettagli dei file non più necessari.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/05-delete-dialogue.webp" alt="Conferma di eliminazione che chiede di eliminare 69 file (1,28 GB), segnalando che i file verranno inviati al Cestino" width="900"><br>
  <em>Conferma prima di ogni azione. Elimina invia al Cestino; Sposta colloca i file dove scegli tu.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/06-freed-success-done.webp" alt="Schermata di esito positivo che mostra 1,28 GB liberati, con 69 file inviati al Cestino" width="900"><br>
  <em>Dopo un'eliminazione riuscita.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/07-scanned-again-all-clean.webp" alt="Schermata «Tutto pulito» dopo una nuova scansione: niente da pulire in C:\Windows\Installer" width="900"><br>
  <em>Dopo una nuova scansione. Non resta nulla da pulire.</em>
  <br><br>
</p>

## Come funziona

InstallerClean individua tre tipi di file non necessari.

**I file orfani** sono gli installer `.msi` (e le eventuali patch `.msp`) lasciati indietro dopo aver disinstallato un software. Windows non li referenzia più, ma i file restano nella cartella a occupare spazio.

**Le patch sostituite** sono vecchie patch `.msp` che sono state rimpiazzate da altre più recenti. Windows le contrassegna come sostituite nel proprio database, ma non le elimina mai. I produttori che pubblicano patch di frequente (Acrobat, Office, grandi strumenti di sviluppo) accumulano patch sostituite all'infinito.

**Le patch obsolete** sono patch `.msp` che l'editore ha ritirato o dichiarato obsolete invece di sostituirle con una versione più recente. Windows registra anche questo stato e, allo stesso modo, lascia il file nella cartella.

Per trovarle, InstallerClean chiama direttamente l'interfaccia COM di Windows Installer tramite P/Invoke:

- `MsiEnumProductsEx` per enumerare ogni prodotto installato
- `MsiEnumPatchesEx` per trovare tutte le patch registrate di ogni prodotto
- `MsiGetPatchInfoEx` per leggere lo stato di ogni patch (applicata, sostituita o obsoleta)

Qualunque file `.msi` o `.msp` in `C:\Windows\Installer` che non sia rivendicato da un prodotto registrato è orfano e viene contrassegnato come rimovibile. Lo stesso vale per qualunque patch che il database segni come sostituita o obsoleta e che non serva per la disinstallazione.

Se l'API restituisce dati incompleti (cosa rara, ma che può capitare con uno stato del programma di installazione danneggiato), l'app ripiega sulla lettura del registro di sistema. Questo ripiego aggiunge file solo all'insieme degli «ancora necessari», mai a quello dei «rimovibili».

Una volta completato uno spostamento o un'eliminazione, le sottocartelle vuote dentro `C:\Windows\Installer` (le directory che la cache lascia indietro quando il loro contenuto sparisce) vengono eliminate nella stessa passata.

## È sicuro?

Sì. InstallerClean interroga lo stesso database dell'API di Windows Installer che Windows stesso usa per tenere traccia di ciò che è installato. Se Windows dice che un file non serve più, l'app si fida; non tira a indovinare in base a nomi di file o date.

**Su Elimina e Sposta.** I file che InstallerClean elimina si possono eliminare definitivamente senza rischi. **Elimina** li invia al Cestino (verrai avvisato se non è disponibile); recuperi lo spazio sull'unità C: quando svuoti il Cestino.

Non sei comunque costretto a fidarti di me sul fatto che i file si possano eliminare senza rischi. Finché sono nel Cestino, hai modo di verificare che le app che usano questa cartella, Office, Acrobat, Visual Studio e simili, continuino ad aggiornarsi e a disinstallarsi senza problemi. Se qualcosa non funziona (non succederà!), ripristina i file dal Cestino per sistemare le cose. Per andare ancora più sul sicuro, puoi invece usare **Sposta**, per parcheggiare i file in una cartella che scegli tu (ovviamente scegli una cartella su un'altra partizione o unità se quello che vuoi è liberare spazio su C:). Per tornare com'era basta ricopiare i file in `C:\Windows\Installer` (ma non ti servirà!).

Se Windows Installer in quel momento sta scrivendo nella cache, ha una transazione precedente sospesa o ha in coda per il prossimo riavvio la ridenominazione di un file che riguarda la cache, allora Sposta ed Elimina sono disattivati e viene mostrato il motivo specifico.

I servizi di scansione, interrogazione, spostamento, eliminazione, impostazioni e controllo del riavvio in sospeso sono coperti da una suite di test automatici che viene eseguita a ogni commit (vedi il badge CI qui sopra).

**Verificare il file binario.** InstallerClean non è firmato, quindi non devi fidarti sulla parola:

- Gli hash SHA-256 di ogni versione sono elencati nella [pagina delle release](../../releases/latest).
- VirusTotal: pulito su tutti i motori. Nelle note di ogni versione ci sono link attivi per ricontrollare.
- Il codice sorgente è su [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean) e la CI compila e testa ogni commit (vedi il badge verde della CI qui sopra).
- <!-- downloads-start -->23k<!-- downloads-end --> download tra GitHub, MajorGeeks e Softpedia.
- [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) prova ogni invio in una macchina virtuale e lo pubblica solo se supera la loro revisione.
- [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) analizza ogni versione alla ricerca di virus, spyware e adware.

<a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Certificato 100% pulito da Softpedia" width="190"></a>

<a id="recovery"></a>
## Se ti manca un file da `C:\Windows\Installer`

InstallerClean rimuove solo i file che Windows stesso segnala come non più necessari, quindi non può mai essere la causa di un file mancante. Ma se uno è già sparito, InstallerClean lo rileva e lo segnala. Ecco come rimediare.

Scarica il programma di installazione di quel software dal suo produttore ed eseguilo sopra l'installazione esistente; non disinstallare prima. Usa la versione che hai adesso, se puoi, perché Windows potrebbe rifiutarne una diversa. Di solito questo rimette a posto il file e lascia intatte le tue impostazioni. Esegui di nuovo la scansione in InstallerClean e, se ha funzionato, l'avviso sarà sparito.

Di solito funziona. Quello che segue è il resoconto più completo di Microsoft stessa: il dettaglio ufficiale e i casi più difficili per quando non è così semplice. Niente di tutto questo dipende da InstallerClean, e non posso migliorare le indicazioni di Microsoft, quindi mi limito a riportartele.

<details>
<summary>La posizione più completa di Microsoft</summary>

*Le citazioni di Microsoft qui sotto sono riportate nella loro versione originale in inglese.*

Guida completa: [Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache).

*Potrebbe non manifestarsi subito:*
> "If the installer cache is compromised, you may not immediately see problems until you take an action such as uninstalling, repairing, or updating a product."

*I file sono unici per ogni computer, quindi non puoi copiarne uno da un altro PC:*
> "Missing files cannot be copied between computers because the files are unique."

*E non puoi nemmeno recuperare solo il file da un backup:*
> "To restore the missing files, a full system state restoration is required. It is not possible to replace only the missing files from a previous backup."

*Il ripristino consigliato, e i suoi limiti senza giri di parole:*
> "If application files are missing from the Windows Installer Cache, ask the vendor or support team for the application about the missing files. You must follow the procedures or steps recommended by the application vendor to restore the files. In some cases, you may have to rebuild the operating system and reinstall the application to fix the problem."
>
> "Windows support engineers cannot help you recover missing application files from the Windows Installer cache."

*Perché conta usare la stessa versione:*
> "The upgrade cannot be installed by the Windows Installer service because the program to be upgraded may be missing, or the upgrade may update a different version of the program."

</details>

## Accessibilità

InstallerClean è realizzato per essere pienamente utilizzabile da tastiera e con uno screen reader.

- **Utilizzabile interamente da tastiera.** Il tasto Tab raggiunge ogni controllo, e le colonne delle finestre di dettaglio si ordinano da tastiera: qui niente richiede il mouse. Il focus della tastiera resta visibile ovunque si trovi.
- **Assistente vocale e Accesso vocale.** Ogni controllo è etichettato, e la parola visibile su un pulsante è quella che lo attiva con la voce. Quando uno spostamento o un'eliminazione si conclude, l'esito viene letto ad alta voce.
- **Fatto per essere letto.** Il testo rispetta il contrasto WCAG AA in tutto il tema scuro.

Se qualcosa qui ti ostacola, [apri un issue](../../issues). I problemi di accessibilità sono bug, non casi limite.

## Cosa non fa

- WinSxS (`C:\Windows\WinSxS`) è una cartella diversa con regole diverse. Per quella, esegui `Dism /Online /Cleanup-Image /StartComponentCleanup` da un prompt con privilegi elevati.
- Nessun servizio in background, nessuna attività pianificata, nessuna pulizia automatica. L'app gira quando la avvii tu.
- L'accesso al registro di sistema è in sola lettura. L'app interroga il database di Windows Installer; non lo modifica.
- Si collega a internet solo quando glielo dici tu: un controllo manuale degli aggiornamenti; il riepilogo anonimo opzionale (solo per farmi sapere che funziona); e i link alla documentazione su GitHub e a una pagina per le donazioni, che si aprono nel tuo browser se scegli di cliccarli.
- Niente barre degli strumenti, niente software incluso, niente adware.

## Domande frequenti

**Libererò davvero GB di spazio?** Dipende dal tuo computer. Un'installazione pulita di Windows 11 senza software aggiuntivo non ha nulla da rimuovere. Una postazione di sviluppo usata da tempo, o qualunque computer con molto software basato su MSI (Acrobat, Office, LibreOffice, grandi strumenti di sviluppo), può averne decine di GB. In ogni caso, vedrai esattamente quanto nel momento in cui la esegui.

<!-- reports-stats-start (generated by non-repo-files/refresh-reports-table.mjs; do not hand-edit between these markers) -->
Su 87 report che mi sono stati inviati (grazie 🙏) da quando la v1.8.0 ha aggiunto l'opzione:

| Esito | Quota | Minimo | Mediana | Massimo |
|---|---|---|---|---|
| Niente da rimuovere | 59% | - | - | - |
| Spazio liberato | 41% | 0,1 GB | 22 GB | 327 GB |
<!-- reports-stats-end -->

<details>
<summary>Quei report arrivano dal pulsante opzionale «Invia riepilogo». Ecco cosa vedrai prima che venga inviato qualcosa.</summary>

![Finestra di conferma intitolata «Vuoi inviare questo a No Faff?» che mostra il report completo che verrebbe inviato: versione dell'app, versione di Windows, conteggi della scansione, file elaborati e byte liberati, senza percorsi di file, nomi o identificatori del computer, e con una nota che niente identifica te o il tuo computer, solo se l'app ha funzionato e quanto spazio è stato liberato, con i pulsanti Annulla e Invia.](docs/screenshots/optional-send-summary-confirmation-dialogue.webp)

</details>

<a id="admin"></a>

**Perché richiede i diritti di amministratore?** `C:\Windows\Installer` è riservata agli amministratori. Leggerla, interrogare il database di Windows Installer e spostare o eliminare file lo richiedono tutti, quindi l'app deve essere eseguita come amministratore.

<a id="unknown-publisher"></a>

**Perché Windows dice «Autore sconosciuto»?** Perché InstallerClean non è firmato digitalmente. Un certificato di firma ha un costo annuale, e preferisco tenere l'app gratuita piuttosto che pagarne uno. Così, quando la esegui, Windows SmartScreen mostra «PC protetto da Windows». Clicca su **Ulteriori informazioni**, poi su **Esegui comunque**. Farlo è sicuro: il codice sorgente è pubblico, e ogni versione ha link a VirusTotal e hash SHA-256 che puoi controllare prima.

**Posso annullare un'eliminazione?** Di solito sì. Quando il Cestino è disponibile per l'unità, Elimina ci invia i file e puoi ripristinarli dal Cestino. Se il Cestino non è disponibile, l'app non elimina mai definitivamente di sua iniziativa (vedi [È sicuro?](#è-sicuro)). E se preferisci avere una via di ritorno che controlli tu, Sposta mette i file in una cartella che scegli tu; eliminali da lì quando sei tranquillo.

**Windows si lamenterà se rimuovo questi file?** No. InstallerClean rimuove sempre e solo i file che Windows stesso segnala come non più necessari, quindi niente di ciò che rimuove serve a riparare, aggiornare o disinstallare un programma. Se un file necessario sparisce da `C:\Windows\Installer` per qualche altra via, vedi [Se ti manca un file da C:\Windows\Installer](#recovery).

**Perché non `Win32_Product` (WMI)?** [`Win32_Product` scatena operazioni di riparazione MSI su ogni prodotto durante l'enumerazione](https://gregramsey.net/2012/02/20/win32_product-is-evil/), cosa che può richiedere minuti e mettere sotto sforzo il disco. InstallerClean chiama direttamente l'API COM di Windows Installer, senza effetti collaterali.

**Perché non un semplice script PowerShell?** Un breve script che chiama `MsiEnumPatchesEx` basta a *elencare* le patch, ma le parti portanti di InstallerClean sono quelle che uno script sorvola: la classificazione orfano contro sostituito, il ripiego sul registro che aggiunge file solo all'insieme degli «ancora necessari» (mai a quello dei «rimovibili»), il blocco per riavvio in sospeso, la rete di sicurezza dello spostamento altrove, l'avanzamento per singolo file con annullamento e l'impostazione predefinita Cestino-anziché-eliminazione-definitiva. I casi limite sui computer reali con molto MSI (registrazioni danneggiate, giunzioni dentro la cache, prodotti in `HKU\.DEFAULT`, transazioni del programma di installazione sospese) sono facili da gestire male in uno script improvvisato. La `installerclean-cli` è la versione senza interfaccia, se quello che vuoi è lo scripting.

**Funziona su Windows 7 o 8?** Non testato e non supportato. È pensato per Windows 10 e 11.

**È adatto per RMM o distribuzione di massa?** Sì. La CLI esce con codici distinti per ogni esito (0 successo, 2 parziale, 1 errore grave, 75 transitorio, 130 per un Ctrl+C prima che venga elaborato qualunque file; un Ctrl+C che cade a metà del lotto esce con 2, perché il lavoro era già stato eseguito), così un'attività pianificata può riprovare sul 75 senza confonderlo con un errore grave. Scrive un riepilogo per ogni esecuzione nel registro eventi Applicazione e rispetta lo stesso mutex di istanza singola della GUI. Anche il programma di installazione si installa in modo silenzioso con i parametri standard di Inno Setup (`/SILENT` o `/VERYSILENT`); l'avvio successivo all'installazione viene saltato nelle installazioni silenziose. Vedi la sezione Riga di comando.

## Download

Tre varianti, scegline una:

- **Setup** (`InstallerClean-setup.exe`): un normale programma di installazione di Windows con il runtime .NET 10 incluso. Aggiunge una voce nel menu Start e si disinstalla in modo pulito. Sistemato tra i programmi, così è facile da ritrovare tra sei mesi.
- **Portable** (`InstallerClean-portable.exe`): un singolo exe autonomo con il runtime incluso. Nessuna installazione, nessun programma di disinstallazione. Eseguilo, usalo, eliminalo. Rieseguilo quando vuoi.
- **CLI** (`installerclean-cli.exe`): la versione a riga di comando da sola, un singolo exe autonomo. Nessuna installazione, niente lasciato sulla macchina dopo. Mettilo su un client, esegui una scansione o una pulizia, eliminalo. Pensato per scripting, attività pianificate e distribuzione di massa, quando vuoi le operazioni senza un'app desktop sul client. Vedi [Riga di comando](#riga-di-comando) per gli argomenti e i codici di uscita.

Scaricala dalla [pagina delle release](../../releases/latest), poi eseguila. Non è firmato, quindi Windows mostra un avviso di «autore sconosciuto»; le [Domande frequenti](#unknown-publisher) spiegano cosa vedrai e perché è sicuro.

L'app esegue la scansione automaticamente all'avvio. Esamina i risultati, poi clicca su **Elimina** o **Sposta**.

Oppure installala tramite [winget](https://learn.microsoft.com/windows/package-manager/winget/):

```
winget install NoFaff.InstallerClean
```

Oppure installala tramite [Scoop](https://scoop.sh):

```
scoop bucket add no-faff https://github.com/no-faff/scoop-bucket
scoop install installerclean
```

## Confronto con PatchCleaner

Se hai già cercato questa cartella prima d'ora, lo strumento che con ogni probabilità avrai trovato è [PatchCleaner](https://www.homedev.com.au/free/patchcleaner). Funziona ancora bene, ma ho creato InstallerClean perché PatchCleaner è a codice chiuso, non riceve aggiornamenti da marzo 2016 e, per impostazione predefinita, non tocca i prodotti Adobe. Il suo controllo degli orfani contrassegnava per errore le patch di Adobe, e rimuoverle rompeva gli aggiornamenti di Adobe, quindi lascia in pace tutti i file Adobe a meno che tu non disattivi il filtro. Sui computer dove Adobe è il principale responsabile, è lì che si trova la maggior parte dello spazio:

> *«Ho scaricato PatchCleaner per eliminare i file `.msp` orfani, ma a quanto pare questo libererebbe solo 250 MB di spazio. 29 GB dei file sono "esclusi dai filtri", quindi PatchCleaner non sembra essere d'aiuto.»*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/) (tradotto dall'inglese)

InstallerClean legge i registri delle patch di Windows Installer stesso, quindi sa distinguere quali patch di Adobe sono davvero sostituite ed eliminare quelle in sicurezza, senza un filtro indiscriminato. Ecco come si confrontano i due:

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| Ultimo aggiornamento | 2026 (attivo) | 3 marzo 2016 |
| Codice sorgente | Open source (MIT) | Codice chiuso |
| Runtime | .NET 10 (autonomo) | .NET + VBScript |
| API | Windows Installer COM (nello stesso processo) | Windows Installer COM (in un processo separato, tramite VBScript) |
| Rilevamento delle patch sostituite | Sì | No |
| Gestione di Adobe | Rileva le patch sostituite | Esclude per impostazione predefinita |
| Interfaccia | Tema scuro (WPF) | Windows Forms |
| Raccolta dati | Nessuna | Nessuna |
| Sicurezza dell'eliminazione | Cestino. Se non è disponibile, chiede: spostare invece o eliminare definitivamente | Definitiva, senza Cestino |

> **Una nota su `Win32_Product`:** L'approccio comune ma difettoso per elencare i prodotti installati è `Win32_Product` (WMI), che [scatena operazioni di riparazione MSI](https://gregramsey.net/2012/02/20/win32_product-is-evil/) su ogni prodotto durante l'enumerazione. Sia InstallerClean sia PatchCleaner lo evitano. Entrambi usano l'interfaccia COM di Windows Installer. Il nome di file `WMIProducts.vbs` nello script di PatchCleaner è fuorviante; lo script usa COM di MSI, non WMI.

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) offre anch'esso la pulizia dell'Installer come parte del suo modulo System Booster, ma è uno strumento a pagamento (15-25 USD) e la pulizia è una piccola funzione dentro un'applicazione molto più grande. InstallerClean è gratuito, mirato e open source.

I pulitori di sistema generici come [CCleaner](https://www.ccleaner.com/) e [BleachBit](https://www.bleachbit.org/) non toccano `C:\Windows\Installer`. La cartella richiede interrogazioni all'API di Windows Installer per distinguere i pacchetti registrati da quelli non necessari, e un pulitore generico che si limitasse a percorrere l'albero dei file potrebbe rompere le app installate. InstallerClean è lo strumento a cui rivolgerti quando è proprio quella la cartella che vuoi pulire.

## Riga di comando

InstallerClean supporta il funzionamento senza interfaccia, per scripting e uso da amministratore di sistema:

```
Uso:
  installerclean-cli --help   Mostra questa guida (accetta anche /?, -h)
  installerclean-cli --version  Stampa la versione (accetta anche -v)
  installerclean-cli /s       Solo scansione, elenca i file rimovibili
  installerclean-cli /d       Elimina i file rimovibili (Cestino)
  installerclean-cli /m       Sposta nella posizione predefinita salvata
  installerclean-cli /m PATH  Sposta nel percorso indicato
```

Per avviare la GUI, esegui `InstallerClean.exe` (o usa il collegamento nel menu Start dell'installazione con Setup).

Eseguito senza argomenti, o con un'opzione non riconosciuta, `installerclean-cli` stampa questo testo d'uso ed esce con `1`, così un'attività pianificata che perde la sua opzione fallisce in modo visibile invece di riuscire in silenzio senza fare nulla. Un `--help`, `/?` o `-h` esplicito stampa lo stesso testo d'uso ed esce con `0`.

`/s` è un'esecuzione di prova: scansiona, elenca ciò che rimuoverebbe con nomi di file e dimensioni, poi esce. Utile per controllare prima di pulire. Il codice di uscita è `0` se la scansione riesce, `1` se fallisce e `130` con Ctrl+C. Tutti i file sono in `C:\Windows\Installer`.

`/d` e `/m` scansionano e poi agiscono. `/d` invia i file rimovibili al Cestino. `/m` li sposta in una cartella (quella che indichi sulla riga di comando, oppure quella predefinita salvata dalla GUI). Codici di uscita: `0` per successo completo, `2` per parziale (alcuni file riusciti, altri falliti), `1` per fallimento totale (scansione fallita, argomenti errati o tutti i file del lotto falliti), `75` per una condizione transitoria che ha bloccato l'esecuzione (il messaggio stampato spiega quale e se riprovare servirà), `130` per un Ctrl+C prima che venga elaborato qualunque file (un Ctrl+C che cade a metà del lotto esce con `2`, parziale, perché il lavoro era già stato eseguito).

Tutto l'output della CLI, compresi i messaggi di errore e di diagnostica, va su stdout; non c'è un flusso stderr separato. Il codice di uscita è il segnale leggibile dalla macchina (e la voce per ogni esecuzione nel registro eventi Applicazione lo rispecchia), quindi uno script dovrebbe basarsi sul codice di uscita anziché analizzare il testo, e `installerclean-cli /s > audit.txt` cattura l'intera esecuzione, compresa qualunque riga di errore.

Tutte e tre richiedono un prompt dei comandi con privilegi elevati (amministratore). Se Criteri di gruppo blocca la richiesta di elevazione UAC, il processo si rifiuta di avviarsi e Windows restituisce l'errore 740 alla shell chiamante (`$LASTEXITCODE = 740` in PowerShell). `taskkill /pid <pid>` non provoca un annullamento controllato; il mutex di istanza singola viene recuperato dall'esecuzione successiva tramite il percorso AbandonedMutexException.

Nota: l'output della CLI stessa è in inglese. Le descrizioni qui sopra corrispondono alle opzioni disponibili.

### Perché `installerclean-cli` e non `installerclean.exe`?

`InstallerClean.exe` è la GUI WPF; non risponde agli argomenti da riga di comando. `installerclean-cli.exe` è un eseguibile da console separato che viene distribuito nella stessa cartella di installazione ed espone le stesse operazioni di scansione / spostamento / eliminazione a PowerShell, cmd e attività pianificate. Essendo un vero processo da console, blocca il prompt finché non termina; redirigi o invia in pipe il suo output come faresti con qualunque altro exe da console.

Il download portable contiene solo l'exe della GUI. Se vuoi la riga di comando senza la GUI, scarica `installerclean-cli.exe` dalla [pagina delle release](../../releases/latest) ed eseguilo direttamente. Anche il programma di installazione lo installa insieme alla GUI.

## Requisiti

- Windows 10 (versione 1607 / build 14393 o successiva, la più vecchia supportata dal runtime .NET 10) o Windows 11
- Privilegi di amministratore (`C:\Windows\Installer` è riservata agli amministratori)

Vedi [Download](#download) per le varianti setup, portable e CLI.

## Compilare dal codice sorgente

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean.sln
```

Esegui i test:

```
dotnet test src/InstallerClean.Tests/
```

## Contribuire

Hai trovato un bug o hai un suggerimento? [Apri un issue](../../issues) o avvia una [discussione](../../discussions). Le pull request sono benvenute. Esegui `dotnet test` prima di inviare.

## Sostieni il progetto

Se InstallerClean ti è stato utile, valuta di [sostenere No Faff](https://nofaff.netlify.app/support) o di lasciare una stella su GitHub.

## Cronologia delle stelle

<a href="https://www.star-history.com/?repos=no-faff%2FInstallerClean&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
   <img alt="Grafico della cronologia delle stelle" src="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
 </picture>
</a>

## Licenza

[MIT](LICENSE)

---

🎶 [George Formby - When I'm Cleaning Windows](https://www.youtube.com/watch?v=sfmAeijj5cM). Buon ascolto!
