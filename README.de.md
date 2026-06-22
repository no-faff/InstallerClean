<p align="center">
  <a href="README.md">English</a> · <a href="README.zh-CN.md">简体中文</a> · <a href="README.ru.md">Русский</a> · <a href="README.es.md">Español</a> · <a href="README.pt-BR.md">Português (BR)</a> · <a href="README.fr.md">Français</a> · <a href="README.ja.md">日本語</a> · <a href="README.ko.md">한국어</a> · <strong>Deutsch</strong> · <a href="README.it.md">Italiano</a>
</p>

<p align="center"><em>Diese Seite ist übersetzt, aber die Programmoberfläche ist derzeit nur auf Englisch verfügbar.</em></p>

<p align="center">
  <img src="docs/icon.png" width="280" alt="InstallerClean">
</p>

<p align="center"><em>🎶 What's my line? I'm happy <a href="https://www.youtube.com/watch?v=HM-jHhUZfFI">cleaning Windows</a></em></p>

<h1 align="center">InstallerClean</h1>

<p align="center"><strong>Ein quelloffenes Tool, um <code>C:\Windows\Installer</code> sicher aufzuräumen, den versteckten Windows-Ordner, der still und leise deinen Speicherplatz auffrisst.</strong></p>

<p align="center"><em>Einmal benutzen. Vielleicht etwas Platz schaffen. Weg damit.</em></p>

<p align="center">
  <a href="LICENSE"><img src="https://img.shields.io/badge/licence-MIT-blue.svg" alt="Lizenz: MIT"></a>
  <a href="https://dotnet.microsoft.com/download/dotnet/10.0"><img src="https://img.shields.io/badge/.NET-10.0-purple.svg" alt=".NET 10"></a>
  <a href="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml"><img src="https://github.com/no-faff/InstallerClean/actions/workflows/ci.yml/badge.svg" alt="CI"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg" alt="Windows 10/11"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases/latest"><img src="https://img.shields.io/badge/release-v1.9.2-blue" alt="GitHub-Release"></a>
  <a href="https://github.com/no-faff/InstallerClean/releases"><img src="https://img.shields.io/badge/downloads-23k-brightgreen" alt="Downloads insgesamt"></a>
</p>

![Screenshot von InstallerClean nach einem erfolgreichen Aufräumen: 1,28 GB freigegeben, 69 Dateien in den Papierkorb verschoben](docs/screenshots/06-freed-success-done.webp)

- **Was:** InstallerClean tut eine Sache: Es entfernt nicht benötigte Dateien aus `C:\Windows\Installer`, einem versteckten Ordner, den Windows nie aufräumt. Nach einem fast augenblicklichen Scan sagt es dir, ob du welche hast, zeigt Neugierigen mehr Details und lässt dich die Dateien löschen, um Platz auf deiner C:-Festplatte freizugeben. Du benutzt es einmal und machst weiter.
- **Wie viel Platz:** Die (optionalen) bisher eingesandten Berichte zeigen, dass bei <!-- reports-freedpct-start -->41 %<!-- reports-freedpct-end --> der Rechner nicht benötigte Dateien zum Aufräumen vorhanden waren. Bei diesen wurden im Median <!-- reports-median-start -->22 GB<!-- reports-median-end --> freigegeben. Einige haben Hunderte GB freigeräumt. Bei mir waren es 1,28 GB. Die übrigen <!-- reports-nothingpct-start -->59 %<!-- reports-nothingpct-end --> fanden nichts zu entfernen, was einfach heißt, dass ihr Installer-Ordner bereits sauber war. Mehr Details in den [FAQ](#faq) weiter unten.
- **Ist es sicher:** Ja. Es fragt die Windows-Installer-API selbst, welche Dateien noch benötigt werden, und listet nur die auf, die Windows als erledigt meldet. Es ist quelloffen (MIT) und fragt nichts über dich ab: kein Konto, keine Werbung, kein Tracking, keine Telemetrie, nichts, was im Hintergrund läuft. Es geht nie von selbst online.
- **Holen:** [Lade die neueste Version herunter](../../releases/latest). Führe sie aus; klicke dich durch [die Windows-Warnung](#unknown-publisher) und [die Administrator-Abfrage](#admin). Lösche alle nicht benötigten Dateien. Fertig.

## Inhalt

- [Der Ordner, von dem niemand spricht](#der-ordner-von-dem-niemand-spricht)
- [Die Suche nach Hilfe](#die-suche-nach-hilfe)
- [Was es tut](#was-es-tut)
- [Screenshots](#screenshots)
- [Wie es funktioniert](#wie-es-funktioniert)
- [Ist es sicher?](#ist-es-sicher)
- [Wenn dir doch eine Datei aus C:\Windows\Installer fehlt](#recovery)
- [Barrierefreiheit](#barrierefreiheit)
- [Was es nicht tut](#was-es-nicht-tut)
- [FAQ](#faq)
- [Download](#download)
- [Im Vergleich zu PatchCleaner](#im-vergleich-zu-patchcleaner)
- [Befehlszeile](#befehlszeile)
- [Voraussetzungen](#voraussetzungen)
- [Aus dem Quellcode kompilieren](#aus-dem-quellcode-kompilieren)
- [Mitwirken](#mitwirken)
- [Das Projekt unterstützen](#das-projekt-unterstützen)
- [Sternverlauf](#sternverlauf)
- [Lizenz](#lizenz)

---

## Der Ordner, von dem niemand spricht

Auf jedem Windows-PC gibt es einen versteckten Ordner namens `C:\Windows\Installer`. Jedes Mal, wenn du Software installierst, die das Windows-Installer-System nutzt, oder einen Patch für Microsoft Office, Adobe Acrobat, Visual Studio oder eine andere `.msi`-basierte Anwendung einspielst, landet eine Kopie dieses Installers oder dieser `.msp`-Patchdatei in diesem Ordner, und bleibt dort.

Wenn du die Software deinstallierst, bleiben die Dateien. Wenn ein neuerer Patch einen älteren ersetzt, bleiben beide. Windows räumt sie nie auf. Die Datenträgerbereinigung rührt sie nicht an. DISM ist für einen ganz anderen Ordner zuständig. Mit der Zeit wächst der Ordner: 1 GB, 5 GB, 20 GB, 50 GB. Auf Rechnern mit viel MSI-basierter Software (Acrobat ist ein häufiger Übeltäter) kann er [über 100 GB](https://www.reddit.com/r/sysadmin/comments/1oxcrmh/acrobat_filling_up_the_cwindowsinstaller_folder/) erreichen.

Das sind keine temporären Dateien, die von selbst wiederkommen. Sie sind echter Ballast: alte Installer von Software, die du vor Jahren deinstalliert hast, und Patches, die mehrfach ersetzt wurden. Einmal weg, kommen sie nicht wieder.

**Wenn du eine einfache Möglichkeit suchst, unter Windows Speicherplatz freizugeben, ist dieser Ordner ein guter Anfang.** InstallerClean findet die nicht benötigten Dateien und entfernt sie sicher.

## Die Suche nach Hilfe

Wenn du jemals nach Hilfe zu diesem Ordner gesucht hast, kennst du das wahrscheinlich. Jemand mit 180 GB in `C:\Windows\Installer` fragt, wie man ihn aufräumt. Es wird [empfohlen, die Datenträgerbereinigung auszuführen](https://learn.microsoft.com/en-us/answers/questions/4238108/windows-installer-folder-has-occupied-180gb). Er probiert es. Sie gibt 600 MB frei, nichts davon aus diesem Ordner (denn die Datenträgerbereinigung rührt `C:\Windows\Installer` nicht an). Der Thread verstummt.

> *„Alle Threads, die ich gefunden habe, empfehlen meist die gleichen Dinge, die das Problem nicht lösen, und versanden dann.“*
>
> [ksparks519, r/Windows10](https://www.reddit.com/r/Windows10/comments/1bt8c5p/anyone_ever_figure_out_giant_installer_folders/) (aus dem Englischen übersetzt)

Oder es wird ihnen geraten, ihn gar nicht erst anzufassen. In einem Thread wurde jemandem mit einem 60 GB großen Installer-Ordner gesagt, er solle ihn [„nicht anfassen.“](https://www.reddit.com/r/techsupport/comments/1hw4suq/my_windows_installer_folder_is_like_60gb_so_i/) Als er fragte, was er stattdessen tun solle, lautete die Antwort: *„Das habe ich dir gerade gesagt.“*

Der übliche Rat verwechselt das wahllose Löschen von Dateien (was tatsächlich gefährlich ist) mit dem Entfernen von Dateien, die Windows selbst nicht mehr benötigt (was es nicht ist). InstallerClean tut Letzteres.

## Was es tut

1. **Scannt** `C:\Windows\Installer` nach `.msi`- und `.msp`-Dateien
2. **Fragt** die Windows-Installer-API ab, um zu ermitteln, welche Dateien noch registriert sind
3. **Zeigt**, wie viel du freigeben kannst und wie viel noch benötigt wird, mit optionalen Detailfenstern, die jede Datei auflisten
4. **Entfernt** die nicht benötigten Dateien: in den Papierkorb löschen oder in einen Ordner deiner Wahl verschieben

## Screenshots

<p>
  <img src="docs/screenshots/01-initial-scan.webp" alt="Startbildschirm mit dem InstallerClean-Logo, während der Scan läuft" width="900"><br>
  <em>Erster Scan. Das geht sehr schnell.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/02-main-window.webp" alt="Hauptfenster mit 120 noch benötigten Dateien (2,83 GB) und 69 nicht benötigten Dateien zum Aufräumen (1,28 GB), mit einem Feld für den Verschiebeort und den Schaltflächen Löschen und Verschieben" width="900"><br>
  <em>Ergebnisse: wie viel noch benötigt wird, wie viel entfernt werden kann.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/03-details-registered.webp" alt="Fenster der registrierten Dateien mit den installierten Produkten und Installer-Datenbankdetails zum ausgewählten Produkt" width="900"><br>
  <em>Details zu den noch benötigten Dateien, mit aus der Installer-Datenbank gelesenen Metadaten.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/04-details-safe-to-delete.webp" alt="Fenster der nicht benötigten Dateien mit den entfernbaren .msi-Dateien nach Größe sortiert, mit dem Grund, warum jede entfernbar ist, und Details zur ausgewählten Datei" width="900"><br>
  <em>Details zu den nicht mehr benötigten Dateien.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/05-delete-dialogue.webp" alt="Löschbestätigung, die fragt, ob 69 Dateien (1,28 GB) gelöscht werden sollen, mit dem Hinweis, dass die Dateien in den Papierkorb verschoben werden" width="900"><br>
  <em>Bestätigung vor beiden Aktionen. Löschen verschiebt in den Papierkorb; Verschieben legt die Dateien an einen Ort deiner Wahl.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/06-freed-success-done.webp" alt="Erfolgs-Overlay mit 1,28 GB freigegeben und 69 in den Papierkorb verschobenen Dateien" width="900"><br>
  <em>Nach einem erfolgreichen Löschen.</em>
  <br><br>
</p>

<p>
  <img src="docs/screenshots/07-scanned-again-all-clean.webp" alt="Overlay „Alles sauber“ nach erneutem Scan: nichts aufzuräumen in C:\Windows\Installer" width="900"><br>
  <em>Nach erneutem Scan. Nichts mehr aufzuräumen.</em>
  <br><br>
</p>

## Wie es funktioniert

InstallerClean erkennt drei Arten von nicht benötigten Dateien.

**Verwaiste Dateien** sind die `.msi`-Installer (und etwaige `.msp`-Patches), die nach dem Deinstallieren von Software zurückbleiben. Windows verweist nicht mehr auf sie, aber die Dateien liegen im Ordner und belegen Platz.

**Ersetzte Patches** sind alte `.msp`-Patches, die durch neuere ersetzt wurden. Windows markiert sie in seiner eigenen Datenbank als ersetzt, löscht sie aber nie. Anbieter, die häufig Patches veröffentlichen (Acrobat, Office, große Entwicklungswerkzeuge), sammeln ersetzte Patches endlos an.

**Veraltete Patches** sind `.msp`-Patches, die der Herausgeber zurückgezogen oder für überholt erklärt hat, statt sie durch eine neuere Version zu ersetzen. Windows erfasst auch diesen Zustand und lässt die Datei ebenfalls im Ordner.

Um sie zu finden, ruft InstallerClean die COM-Schnittstelle von Windows Installer direkt über P/Invoke auf:

- `MsiEnumProductsEx`, um jedes installierte Produkt aufzuzählen
- `MsiEnumPatchesEx`, um alle registrierten Patches für jedes Produkt zu finden
- `MsiGetPatchInfoEx`, um den Patch-Zustand zu lesen (angewendet, ersetzt oder veraltet)

Jede `.msi`- oder `.msp`-Datei in `C:\Windows\Installer`, die keinem registrierten Produkt zugeordnet ist, ist verwaist und wird als entfernbar markiert. Ebenso jeder Patch, den die Datenbank als ersetzt oder veraltet markiert und der nicht für die Deinstallation benötigt wird.

Wenn die API unvollständige Daten zurückgibt (selten, aber bei beschädigtem Installer-Zustand möglich), greift die App ersatzweise auf das Lesen der Registrierung zurück. Dabei werden Dateien nur der Menge der „noch benötigten“ hinzugefügt, nie der Menge der „entfernbaren“.

Nach einem abgeschlossenen Verschieben oder Löschen werden leere Unterordner in `C:\Windows\Installer` (die Verzeichnisse, die der Cache zurücklässt, sobald ihr Inhalt weg ist) im selben Durchgang entfernt.

## Ist es sicher?

Ja. InstallerClean fragt dieselbe Windows-Installer-API-Datenbank ab, die Windows selbst verwendet, um nachzuhalten, was installiert ist. Wenn Windows sagt, dass eine Datei nicht mehr benötigt wird, vertraut die App darauf; sie rät nicht anhand von Dateinamen oder Datumsangaben.

**Zu Löschen und Verschieben.** Die Dateien, die InstallerClean löscht, können bedenkenlos dauerhaft gelöscht werden. **Löschen** verschiebt sie in den Papierkorb (du wirst gewarnt, falls er nicht verfügbar ist); den Speicherplatz auf deiner C:-Festplatte bekommst du zurück, wenn du den Papierkorb leerst.

Du musst mir aber nicht glauben, dass die Dateien bedenkenlos gelöscht werden können. Solange sie im Papierkorb liegen, kannst du prüfen, ob die Programme, die diesen Ordner nutzen, Office, Acrobat, Visual Studio und Ähnliches, sich weiterhin problemlos aktualisieren und deinstallieren lassen. Falls etwas kaputt ist (wird es nicht!), stell die Dateien aus dem Papierkorb wieder her, um es zu beheben. Ganz auf Nummer sicher gehst du mit **Verschieben**, um die Dateien in einem Ordner deiner Wahl zu parken (wähle natürlich einen Ordner auf einer anderen Partition/Festplatte, wenn du Platz auf C: freigeben willst). Kopier die Dateien einfach zurück nach `C:\Windows\Installer`, um alles wieder so herzustellen, wie es war (aber das wirst du nicht brauchen!).

Wenn Windows Installer gerade in den Cache schreibt, eine frühere Transaktion ausgesetzt ist oder eine nach dem Neustart auszuführende Umbenennung den Cache betrifft, sind Verschieben und Löschen deaktiviert und der konkrete Grund wird angezeigt.

Die Dienste für Scan, Abfrage, Verschieben, Löschen, Einstellungen und ausstehenden Neustart sind durch eine automatisierte Testsuite abgedeckt, die bei jedem Commit läuft (siehe das CI-Badge oben).

**Die Binärdatei überprüfen.** InstallerClean ist unsigniert, du musst es also nicht auf Treu und Glauben hinnehmen:

- SHA-256-Hashes für jede Version sind auf der [Releases-Seite](../../releases/latest) aufgeführt.
- VirusTotal: sauber bei allen Engines. Live-Links in den Hinweisen zu jeder Version, damit du es erneut prüfen kannst.
- Der Quellcode liegt auf [github.com/no-faff/InstallerClean](https://github.com/no-faff/InstallerClean), und die CI baut und testet jeden Commit (siehe das grüne CI-Badge oben).
- <!-- downloads-start -->23k<!-- downloads-end --> Downloads über GitHub, MajorGeeks und Softpedia.
- [MajorGeeks](https://www.majorgeeks.com/files/details/installerclean.html) testet jede Einreichung in einer virtuellen Maschine und listet sie nur, wenn sie die Prüfung besteht.
- [Softpedia](https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml) prüft jede Version auf Viren, Spyware und Adware.

<a href="https://www.softpedia.com/get/System/Hard-Disk-Utils/InstallerClean.shtml"><img src="docs/badges/softpedia-100-free2.webp" alt="Von Softpedia als 100 % sauber zertifiziert" width="190"></a>

<a id="recovery"></a>
## Wenn dir doch eine Datei aus `C:\Windows\Installer` fehlt

InstallerClean entfernt nur Dateien, die Windows selbst als nicht mehr benötigt meldet, es kann also nie der Grund dafür sein, dass eine Datei fehlt. Aber wenn bereits eine verschwunden ist, erkennt InstallerClean das und weist darauf hin. So behebst du es.

Lade den Installer dieses Programms beim Hersteller herunter und führe ihn über deine bestehende Installation aus; deinstalliere nicht zuerst. Verwende möglichst die Version, die du jetzt hast, denn Windows lehnt eine andere unter Umständen ab. Das setzt die Datei in der Regel wieder ein und lässt deine Einstellungen unangetastet. Scanne in InstallerClean erneut, und die Warnung ist weg, wenn es geklappt hat.

Das funktioniert meistens. Was folgt, ist Microsofts eigene, ausführlichere Darstellung: die offiziellen Details und die schwierigeren Fälle, wenn es nicht so einfach ist. Nichts davon geht auf InstallerClean zurück, und ich kann Microsofts Anleitung nicht verbessern, also gebe ich sie nur weiter.

<details>
<summary>Microsofts ausführlichere Darstellung</summary>

*Die folgenden Microsoft-Zitate stehen im englischen Original.*

Vollständige Anleitung: [Restore missing Windows Installer cache files](https://learn.microsoft.com/en-us/troubleshoot/windows-client/application-management/missing-windows-installer-cache).

*Es zeigt sich möglicherweise nicht sofort:*
> "If the installer cache is compromised, you may not immediately see problems until you take an action such as uninstalling, repairing, or updating a product."

*Die Dateien sind pro Rechner einzigartig, du kannst also keine von einem anderen PC kopieren:*
> "Missing files cannot be copied between computers because the files are unique."

*Du kannst die Datei auch nicht einfach aus einem Backup holen:*
> "To restore the missing files, a full system state restoration is required. It is not possible to replace only the missing files from a previous backup."

*Die empfohlene Wiederherstellung und ihre nüchternen Grenzen:*
> "If application files are missing from the Windows Installer Cache, ask the vendor or support team for the application about the missing files. You must follow the procedures or steps recommended by the application vendor to restore the files. In some cases, you may have to rebuild the operating system and reinstall the application to fix the problem."
>
> "Windows support engineers cannot help you recover missing application files from the Windows Installer cache."

*Warum dieselbe Version wichtig ist:*
> "The upgrade cannot be installed by the Windows Installer service because the program to be upgraded may be missing, or the upgrade may update a different version of the program."

</details>

## Barrierefreiheit

InstallerClean ist so gebaut, dass es vollständig mit der Tastatur und mit einem Screenreader bedienbar ist.

- **Durchgehend per Tastatur bedienbar.** Tab erreicht jedes Element, und die Spalten der Detailfenster lassen sich per Tastatur sortieren, hier braucht nichts eine Maus. Der Tastaturfokus bleibt überall sichtbar, wo er landet.
- **Sprachausgabe und Sprachzugriff.** Jedes Element ist beschriftet, und das sichtbare Wort auf einer Schaltfläche ist das Wort, das sie per Sprache auslöst. Wenn ein Verschieben oder Löschen abgeschlossen ist, wird das Ergebnis vorgelesen.
- **Zum Lesen gemacht.** Der Text erfüllt im gesamten dunklen Design den WCAG-AA-Kontrast.

Wenn dir hier etwas im Weg ist, [erstelle ein Issue](../../issues). Barrierefreiheitsprobleme sind Bugs, keine Randfälle.

## Was es nicht tut

- WinSxS (`C:\Windows\WinSxS`) ist ein anderer Ordner mit anderen Regeln. Dafür führe `Dism /Online /Cleanup-Image /StartComponentCleanup` in einer Eingabeaufforderung mit erhöhten Rechten aus.
- Kein Hintergrunddienst, keine geplante Aufgabe, kein automatisches Aufräumen. Die App läuft, wenn du sie startest.
- Die Registrierung wird nur gelesen. Die App fragt die Windows-Installer-Datenbank ab; sie verändert sie nicht.
- Sie verbindet sich nur mit dem Internet, wenn du es ihr sagst: eine manuelle Update-Prüfung; die optionale anonyme Zusammenfassung (nur damit ich weiß, dass es funktioniert); und Links zur GitHub-Dokumentation und einer Spendenseite, die sich in deinem Browser öffnen, wenn du sie anklickst.
- Keine Symbolleisten, keine gebündelte Software, keine Adware.

## FAQ

**Werde ich wirklich GB an Speicher freigeben?** Das hängt von deinem Rechner ab. Eine saubere Windows-11-Installation ohne zusätzliche Software hat nichts zu entfernen. Eine lange genutzte Entwickler-Workstation oder jeder Rechner mit viel MSI-basierter Software (Acrobat, Office, LibreOffice, große Entwicklungswerkzeuge) kann zig GB haben. So oder so siehst du genau wie viel, sobald du es ausführst.

<!-- reports-stats-start (generated by non-repo-files/refresh-reports-table.mjs; do not hand-edit between these markers) -->
Von den 87 Berichten, die seit der Einführung der Option in v1.8.0 freundlicherweise eingesandt wurden (danke 🙏):

| Ergebnis | Anteil | Minimum | Median | Maximum |
|---|---|---|---|---|
| Nichts zu entfernen | 59 % | - | - | - |
| Freigegebener Speicher | 41 % | 0,1 GB | 22 GB | 327 GB |
<!-- reports-stats-end -->

<details>
<summary>Diese Berichte stammen von der optionalen Schaltfläche „Zusammenfassung senden“. Das siehst du, bevor etwas gesendet wird.</summary>

![Bestätigungsdialog mit dem Titel „Send this to No Faff?“, der den vollständigen Bericht zeigt, der gesendet würde: App-Version, Windows-Version, Scan-Zähler, verarbeitete Dateien und freigegebene Bytes, ohne Dateipfade, Namen oder Geräte-IDs, mit dem Hinweis, dass nichts dich oder deinen Rechner identifiziert, nur ob die App funktioniert hat und wie viel Platz freigegeben wurde, mit den Schaltflächen Abbrechen und Senden.](docs/screenshots/optional-send-summary-confirmation-dialogue.webp)

</details>

<a id="admin"></a>

**Warum will es Administratorrechte?** `C:\Windows\Installer` ist für Administratoren gesperrt. Das Lesen, das Abfragen der Installer-Datenbank und das Verschieben oder Löschen von Dateien erfordern allesamt erhöhte Rechte, also muss die App als Administrator laufen.

<a id="unknown-publisher"></a>

**Warum sagt Windows „Unbekannter Herausgeber“?** Weil InstallerClean nicht signiert ist. Ein Signaturzertifikat kostet jedes Jahr Geld, und ich halte die App lieber kostenlos, als für eines zu zahlen. Wenn du sie also ausführst, zeigt Windows SmartScreen „Der Computer wurde durch Windows geschützt“. Klicke auf **Weitere Informationen** und dann auf **Trotzdem ausführen**. Das ist unbedenklich: Der Quellcode ist öffentlich, und jede Version hat VirusTotal-Links und SHA-256-Hashes, die du vorher prüfen kannst.

**Kann ich ein Löschen rückgängig machen?** Meistens ja. Wenn der Papierkorb für das Laufwerk verfügbar ist, verschiebt Löschen die Dateien dorthin, und du kannst sie aus dem Papierkorb wiederherstellen. Wenn der Papierkorb nicht verfügbar ist, löscht die App von sich aus nie endgültig (siehe [Ist es sicher?](#ist-es-sicher)). Und wenn du lieber einen Rückweg hast, den du selbst kontrollierst, legt Verschieben die Dateien in einen Ordner deiner Wahl; lösche sie von dort, wann immer du zufrieden bist.

**Wird sich Windows beschweren, wenn ich diese Dateien entferne?** Nein. InstallerClean entfernt immer nur die Dateien, die Windows selbst als erledigt meldet, also wird nichts davon zum Reparieren, Aktualisieren oder Deinstallieren eines Programms benötigt. Falls doch auf anderem Weg eine benötigte Datei aus `C:\Windows\Installer` verschwindet, siehe [Wenn dir doch eine Datei aus C:\Windows\Installer fehlt](#recovery).

**Warum kein `Win32_Product` (WMI)?** [`Win32_Product` löst bei der Aufzählung auf jedem Produkt MSI-Reparaturvorgänge aus](https://gregramsey.net/2012/02/20/win32_product-is-evil/), was Minuten dauern und die Festplatte stark belasten kann. InstallerClean ruft die COM-API von Windows Installer direkt und ohne Nebenwirkungen auf.

**Warum nicht einfach ein PowerShell-Skript?** Ein kurzes Skript, das `MsiEnumPatchesEx` aufruft, reicht, um Patches *aufzulisten*, aber die tragenden Teile von InstallerClean sind die, die ein Skript überspringt: die Einordnung verwaist gegen ersetzt, der Rückgriff auf die Registrierung, der Dateien nur der Menge der „noch benötigten“ hinzufügt (nie der der „entfernbaren“), die Sperre bei ausstehendem Neustart, das Sicherheitsnetz des Verschiebens an einen anderen Ort, der Fortschritt pro Datei mit Abbruchmöglichkeit und die Voreinstellung Papierkorb-statt-endgültig-löschen. Randfälle auf echten Rechnern mit viel MSI (beschädigte Registrierungen, Junctions im Cache, Produkte in `HKU\.DEFAULT`, ausgesetzte Installer-Transaktionen) lassen sich in einem improvisierten Skript leicht falsch behandeln. Die `installerclean-cli` ist das Gesicht ohne Oberfläche, wenn du Skripting willst.

**Funktioniert es unter Windows 7 oder 8?** Ungetestet und nicht unterstützt. Ausgelegt auf Windows 10 und 11.

**Ist es für RMM / Massenbereitstellung geeignet?** Ja. Die CLI beendet sich mit eindeutigen Codes je Ergebnis (0 Erfolg, 2 teilweise, 1 schwerer Fehler, 75 vorübergehend, 130 für ein Strg+C, bevor eine Datei verarbeitet wurde; ein Strg+C mitten im Stapel beendet mit 2, da Arbeit ausgeführt wurde), sodass eine geplante Aufgabe bei 75 erneut versuchen kann, ohne es mit schweren Fehlern zu vermengen. Sie schreibt für jede Ausführung eine Zusammenfassung ins Anwendungs-Ereignisprotokoll und respektiert denselben Einzelinstanz-Mutex wie die GUI. Auch der Setup-Installer installiert still mit den Standard-Schaltern von Inno Setup (`/SILENT` oder `/VERYSILENT`); der Start nach der Installation wird bei stillen Installationen übersprungen. Siehe den Abschnitt Befehlszeile.

## Download

Drei Varianten, wähle eine:

- **Setup** (`InstallerClean-setup.exe`): ein normaler Windows-Installer mit gebündelter .NET-10-Laufzeit. Fügt einen Eintrag ins Startmenü ein und lässt sich sauber deinstallieren. Bei den Programmen einsortiert, damit du es in sechs Monaten leicht wiederfindest.
- **Portable** (`InstallerClean-portable.exe`): eine einzelne, eigenständige exe mit gebündelter Laufzeit. Keine Installation, kein Deinstallationsprogramm. Ausführen, benutzen, löschen. Jederzeit wieder ausführen.
- **CLI** (`installerclean-cli.exe`): die Befehlszeilenversion allein, eine einzelne, eigenständige exe. Keine Installation, danach bleibt nichts auf dem Rechner zurück. Leg sie auf einem Client ab, führe einen Scan oder ein Aufräumen aus, lösche sie. Gebaut für Skripting, geplante Aufgaben und Massenbereitstellung, wenn du die Operationen ohne Desktop-App auf dem Client willst. Siehe [Befehlszeile](#befehlszeile) für die Argumente und Exit-Codes.

Lade es von der [Releases-Seite](../../releases/latest) herunter und führe es aus. Es ist unsigniert, daher zeigt Windows eine Warnung „Unbekannter Herausgeber“; die [FAQ](#unknown-publisher) erklären, was du siehst und warum es unbedenklich ist.

Die App scannt beim Start automatisch. Sieh dir die Ergebnisse an und klicke dann auf **Löschen** oder **Verschieben**.

Oder installiere über [winget](https://learn.microsoft.com/windows/package-manager/winget/):

```
winget install NoFaff.InstallerClean
```

Oder installiere über [Scoop](https://scoop.sh):

```
scoop bucket add no-faff https://github.com/no-faff/scoop-bucket
scoop install installerclean
```

## Im Vergleich zu PatchCleaner

Wenn du schon einmal nach diesem Ordner gesucht hast, ist das Tool, auf das du am ehesten gestoßen bist, [PatchCleaner](https://www.homedev.com.au/free/patchcleaner). Es läuft noch immer gut, aber ich habe InstallerClean gemacht, weil PatchCleaner quellgeschlossen ist, seit März 2016 kein Update mehr bekommen hat und Adobe-Produkte standardmäßig nicht anrührt. Seine Prüfung auf verwaiste Dateien markierte Adobes Patches fälschlich, und sie zu entfernen machte Adobes Updates kaputt, also lässt es alle Adobe-Dateien in Ruhe, sofern du den Filter nicht abschaltest. Auf den Rechnern, wo Adobe der größte Übeltäter ist, steckt dort der meiste Platz:

> *„Ich habe PatchCleaner heruntergeladen, um die verwaisten `.msp`-Dateien zu löschen, aber das würde anscheinend nur 250 MB Platz freigeben. 29 GB der Dateien sind ‚durch Filter ausgeschlossen’, PatchCleaner scheint also nicht zu helfen.“*
>
> HeatherBunny1111, [r/techsupport](https://www.reddit.com/r/techsupport/comments/1qc4tcf/how_to_delete_msp_files_safely/) (aus dem Englischen übersetzt)

InstallerClean liest die eigenen Patch-Aufzeichnungen von Windows Installer, kann also erkennen, welche Adobe-Patches wirklich ersetzt sind, und diese sicher entfernen, ohne pauschalen Filter. So vergleichen sich die beiden:

| | **InstallerClean** | **PatchCleaner** |
|---|---|---|
| Zuletzt aktualisiert | 2026 (aktiv) | 3. März 2016 |
| Quellcode | Open Source (MIT) | Quellgeschlossen |
| Laufzeit | .NET 10 (eigenständig) | .NET + VBScript |
| API | Windows Installer COM (im Prozess) | Windows Installer COM (außerhalb des Prozesses, über VBScript) |
| Erkennung ersetzter Patches | Ja | Nein |
| Adobe-Handhabung | Erkennt ersetzte Patches | Schließt standardmäßig aus |
| Oberfläche | Dunkles Design (WPF) | Windows Forms |
| Datenerfassung | Keine | Keine |
| Sicherheit beim Löschen | Papierkorb. Wenn er nicht verfügbar ist, fragt es: stattdessen verschieben oder endgültig löschen | Endgültig, kein Papierkorb |

> **Eine Anmerkung zu `Win32_Product`:** Der verbreitete, aber fehlerhafte Ansatz zum Auflisten installierter Produkte ist `Win32_Product` (WMI), der bei der Aufzählung [auf jedem Produkt MSI-Reparaturvorgänge auslöst](https://gregramsey.net/2012/02/20/win32_product-is-evil/). Sowohl InstallerClean als auch PatchCleaner vermeiden ihn. Beide nutzen die COM-Schnittstelle von Windows Installer. Der Dateiname `WMIProducts.vbs` in PatchCleaners Skript ist irreführend; das Skript nutzt MSI-COM, nicht WMI.

[Ultra Virus Killer (UVK)](https://www.carifred.com/uvk/) bietet die Installer-Bereinigung ebenfalls an, als Teil seines System-Booster-Moduls, aber es ist ein kostenpflichtiges Tool (15-25 USD) und die Bereinigung ist eine kleine Funktion innerhalb einer viel größeren Anwendung. InstallerClean ist kostenlos, fokussiert und quelloffen.

Allzweck-Systemreiniger wie [CCleaner](https://www.ccleaner.com/) und [BleachBit](https://www.bleachbit.org/) rühren `C:\Windows\Installer` nicht an. Der Ordner braucht Abfragen an die Windows-Installer-API, um registrierte Pakete von nicht benötigten zu unterscheiden, und ein generischer Reiniger, der nur den Dateibaum durchläuft, könnte installierte Apps kaputt machen. InstallerClean ist das Werkzeug, zu dem du greifst, wenn genau dieser Ordner aufgeräumt werden soll.

## Befehlszeile

InstallerClean unterstützt den Betrieb ohne Oberfläche, für Skripting und die Nutzung durch Systemadministratoren:

```
Verwendung:
  installerclean-cli --help   Diese Hilfe anzeigen (akzeptiert auch /?, -h)
  installerclean-cli --version  Die Version ausgeben (akzeptiert auch -v)
  installerclean-cli /s       Nur scannen, entfernbare Dateien auflisten
  installerclean-cli /d       Entfernbare Dateien löschen (Papierkorb)
  installerclean-cli /m       In den gespeicherten Standardort verschieben
  installerclean-cli /m PATH  In den angegebenen Pfad verschieben
```

Um die GUI zu starten, führe `InstallerClean.exe` aus (oder nutze die Startmenü-Verknüpfung aus der Setup-Installation).

Wird `installerclean-cli` ohne Argument oder mit einer nicht erkannten Option ausgeführt, gibt es diese Verwendung aus und beendet sich mit `1`, sodass eine geplante Aufgabe, die ihre Option verliert, sichtbar fehlschlägt, statt still zu „gelingen“, ohne etwas zu tun. Ein ausdrückliches `--help`, `/?` oder `-h` gibt dieselbe Verwendung aus und beendet sich mit `0`.

`/s` ist ein Probelauf: Es scannt, listet mit Dateinamen und Größen auf, was es entfernen würde, und beendet sich dann. Nützlich, um vor dem Aufräumen zu prüfen. Der Exit-Code ist `0` bei einem erfolgreichen Scan, `1`, wenn der Scan fehlschlägt, und `130` bei Strg+C. Alle Dateien liegen in `C:\Windows\Installer`.

`/d` und `/m` scannen und handeln dann. `/d` verschiebt entfernbare Dateien in den Papierkorb. `/m` verschiebt sie in einen Ordner (entweder einen, den du auf der Befehlszeile angibst, oder den aus der GUI gespeicherten Standard). Exit-Codes: `0` für vollen Erfolg, `2` für teilweisen (einige Dateien erfolgreich, einige fehlgeschlagen), `1` für völliges Scheitern (Scan fehlgeschlagen, falsche Argumente oder jede Datei im Stapel fehlgeschlagen), `75` für eine vorübergehende Bedingung, die den Lauf blockiert hat (die ausgegebene Meldung erklärt welche und ob ein erneuter Versuch hilft), `130` für ein Strg+C, bevor eine Datei verarbeitet wurde (ein Strg+C mitten im Stapel beendet mit `2`, teilweise, da Arbeit ausgeführt wurde).

Die gesamte Ausgabe der CLI, einschließlich Fehler- und Diagnosemeldungen, geht an stdout; es gibt keinen separaten stderr-Stream. Der Exit-Code ist das maschinenlesbare Signal (und der Eintrag pro Lauf im Anwendungs-Ereignisprotokoll spiegelt ihn wider), ein Skript sollte sich also am Exit-Code orientieren statt den Text zu parsen, und `installerclean-cli /s > audit.txt` erfasst den gesamten Lauf einschließlich etwaiger Fehlerzeile.

Alle drei erfordern eine Eingabeaufforderung mit erhöhten Rechten (Administrator). Wenn eine Gruppenrichtlinie die UAC-Eingabeaufforderung blockiert, verweigert der Prozess den Start und Windows gibt den Fehler 740 an die übergeordnete Shell zurück (`$LASTEXITCODE = 740` in PowerShell). `taskkill /pid <pid>` löst keinen kontrollierten Abbruch aus; der Einzelinstanz-Mutex wird vom nächsten Lauf über den AbandonedMutexException-Pfad wiederhergestellt.

Hinweis: Die Ausgabe der CLI selbst ist auf Englisch. Die obigen Beschreibungen entsprechen den verfügbaren Optionen.

### Warum `installerclean-cli` und nicht `installerclean.exe`?

`InstallerClean.exe` ist die WPF-GUI; sie reagiert nicht auf Befehlszeilenargumente. `installerclean-cli.exe` ist eine separate Konsolenanwendung, die im selben Installationsverzeichnis liegt und dieselben Scan-/Verschiebe-/Löschvorgänge für PowerShell, cmd und geplante Aufgaben bereitstellt. Da es ein echter Konsolenprozess ist, blockiert er die Eingabeaufforderung, bis er fertig ist; leite seine Ausgabe um oder per Pipe weiter wie bei jeder anderen Konsolen-exe.

Der Portable-Download enthält nur die GUI-exe. Wenn du die Befehlszeile ohne die GUI willst, lade `installerclean-cli.exe` von der [Releases-Seite](../../releases/latest) herunter und führe es direkt aus. Auch der Setup-Installer installiert es zusammen mit der GUI.

## Voraussetzungen

- Windows 10 (Version 1607 / Build 14393 oder neuer, die älteste von der .NET-10-Laufzeit unterstützte) oder Windows 11
- Administratorrechte (`C:\Windows\Installer` ist nur für Administratoren)

Siehe [Download](#download) für die Varianten Setup, Portable und CLI.

## Aus dem Quellcode kompilieren

```
git clone https://github.com/no-faff/InstallerClean.git
cd InstallerClean
dotnet build src/InstallerClean.sln
```

Führe die Tests aus:

```
dotnet test src/InstallerClean.Tests/
```

## Mitwirken

Einen Bug gefunden oder einen Vorschlag? [Erstelle ein Issue](../../issues) oder starte eine [Diskussion](../../discussions). Pull Requests sind willkommen. Bitte führe `dotnet test` aus, bevor du etwas einreichst.

## Das Projekt unterstützen

Wenn InstallerClean geholfen hat, denk darüber nach, [No Faff zu unterstützen](https://nofaff.netlify.app/support) oder einen Stern auf GitHub dazulassen.

## Sternverlauf

<a href="https://www.star-history.com/?repos=no-faff%2FInstallerClean&type=date&legend=top-left">
 <picture>
   <source media="(prefers-color-scheme: dark)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&theme=dark&legend=top-left" />
   <source media="(prefers-color-scheme: light)" srcset="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
   <img alt="Diagramm des Sternverlaufs" src="https://api.star-history.com/chart?repos=no-faff/InstallerClean&type=date&legend=top-left" />
 </picture>
</a>

## Lizenz

[MIT](LICENSE)

---

🎶 [George Formby - When I'm Cleaning Windows](https://www.youtube.com/watch?v=sfmAeijj5cM). Viel Spaß!
