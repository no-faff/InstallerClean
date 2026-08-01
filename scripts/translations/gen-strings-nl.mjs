#!/usr/bin/env node
// Dutch (nl) satellite generator for InstallerClean. Copied from
// gen-strings-template.mjs (the full-resx template) and filled with Dutch.
// It works FROM THE ENGLISH SOURCE (Strings.resx): it reads the neutral as the
// structural base, strips ONLY the 21 machine-contract Cli.* keys, replaces the
// inner <value> of every other key from the MAP below, appends the satellite-only
// .One override(s), writes LF/UTF-8 and self-verifies against the neutral.
//
// Provenance: seeded from the complete Dutch translation RijckAlex contributed
// in PR #54, a hand-translated copy of the docs/translations review table, then
// reconciled to the neutral key by key before it shipped. The register is je
// throughout (modern Windows Dutch and the app's own voice); the installer
// overrides in InstallerClean_Languages.iss stay u-form to match the official
// Dutch.isl around them, the same split German ships with Sie.
//
// Dutch plural class: CategoryFor returns One only at n==1, else Other (the
// "default" branch, same selector as de/es/it). Dutch past participles do NOT
// inflect for number (gevonden / verwijderd / verplaatst are identical at 1 and
// many), and the four count-bearing progress lines use the verbal-noun "Bezig
// met ..." construction, which carries no number at all, so the four progress
// overrides German needs (wird/werden) have no Dutch counterpart. Dutch DOES
// inflect the attributive adjective ("1 geregistreerd pakket" vs "120
// geregistreerde pakketten": indefinite singular before a het-word drops the
// -e, and both counted nouns, pakket and product, are het-words), and the
// pronoun standing for the files in Completion.ReverifySkipped is het at one
// file and ze at many. Hence TWO .One overrides:
// Status.RegisteredPackagesFound.One and Completion.ReverifySkipped.One.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.nl.resx`;

// Universal keeps: values identical in every language (brand names, the pure-
// placeholder string, the size/elapsed format strings). Do NOT edit per language.
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

// Keys whose Dutch value is deliberately identical to the English: patch is the
// Dutch word for a software patch (De Prullenbak naast de patch), product keeps
// its Latin form in the singular, and Details is the Dutch noun too.
const ALSO_KEEP = [
  'Section.Registered.Patches',
  'Field.Patches',
  'Action.Details',
  'Automation.Section.Patches',
  'Plural.Product.Singular',
  'Plural.Patch.Singular',
  'Plural.Patch.Plural',
];

// Satellite-only CLDR plural overrides (see the header note): the indefinite
// singular adjective before a het-word ("1 geregistreerd pakket"), and the
// singular pronoun for the kept-back file (het, where the base says ze).
const OVERRIDES = {
  'Status.RegisteredPackagesFound.One': `{0} geregistreerd {1} gevonden.`,
  'Completion.ReverifySkipped.One': `{0} {1} behouden, omdat een programma het na de scan weer nodig bleek te hebben.`,
};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Over`,
  'Window.Registered.Title': `Geregistreerde bestanden die niet mogen worden verwijderd`,
  'Window.Orphaned.Title': `Overbodige bestanden die veilig kunnen worden verwijderd`,

  // Section headings
  'Section.Registered.Products': `PRODUCTEN`,
  'Section.Registered.Patches': `PATCHES`,
  'Section.Registered.Details': `PRODUCTGEGEVENS`,
  'Section.Backup.Folder': `VERPLAATSLOCATIE`,
  'Section.SayThanks': `ZEG BEDANKT`,

  // Field labels
  'Field.Reason': `Reden`,
  'Field.Author': `Auteur`,
  'Field.Application': `Toepassing`,
  'Field.Title': `Titel`,
  'Field.Subject': `Onderwerp`,
  'Field.Keywords': `Trefwoorden`,
  'Field.SigningCertificate': `Ondertekeningscertificaat`,
  'Field.FileSize': `Bestandsgrootte`,
  'Field.Comment': `Opmerking`,
  'Field.ProductName': `Productnaam`,
  'Field.File': `Bestand`,
  'Field.Size': `Grootte`,
  'Field.Patches': `Patches`,
  'Field.UnknownProductName': `(onbekend)`,
  'Field.PatchesOnly': `(alleen patches)`,
  'Field.Missing': `ontbreekt`,

  // Actions (button labels; underscore prefixes are WPF mnemonics)
  'Action.About': `_Over`,
  'Action.Copy': `Kopiëren`,
  'Action.Cut': `Knippen`,
  'Action.Paste': `Plakken`,
  'Action.SelectAll': `Alles selecteren`,
  'Action.Browse': `_Bladeren...`,
  'Action.Cancel': `_Annuleren`,
  'Action.CheckForUpdates': `Controleren op _updates`,
  'Action.Close': `_Sluiten`,
  'Action.DeletePermanently': `_Definitief verwijderen`,
  'Action.Done': `_Klaar`,
  'Action.Details': `Details`,
  'Action.BuyMeACuppa': `Trakteer me op een kop _koffie`,
  'Action.LeaveStarOnGitHub': `Geef een s_ter op GitHub`,
  'Action.Licence': `Apache 2.0-licentie`,
  'Action.Move': `Ver_plaatsen`,
  'Action.BackupFolderPlaceholder': `Pad naar de map als je kiest voor Verplaatsen in plaats van Verwijderen`,
  'Action.OpenReleasePage': `_Releasepagina openen`,
  'Action.Rescan': `Opnieuw _scannen`,
  'Action.ScanAgain': `_Opnieuw scannen`,
  'Action.SendResultLog': `Rapport verzenden`,
  'Action.SendResultLogConfirm': `_Verzenden`,
  'About.Link.Guide': `Handleiding en FAQ`,
  'About.Link.ReportProblem': `Een probleem melden`,
  'About.AutoUpdateCheck': `Automatisch controleren op updates`,

  // Automation names (screen reader / accessibility)
  'Automation.BuyMeACuppa': `Doneren`,
  'Automation.BuyMeACuppa.About': `Trakteer me op een kop koffie`,
  'Automation.CancelOperation': `Bewerking annuleren`,
  'Automation.CancelScan': `Scan annuleren`,
  'Automation.CancelStartupScan': `Opstartscan annuleren`,
  'Automation.Close': `Sluiten`,
  'Automation.CloseWindow': `Venster sluiten`,
  'Automation.CloseResult': `Resultaat sluiten en terug naar het hoofdvenster`,
  'Automation.LeaveStarOnGitHub.About': `Geef een ster op github`,
  'Automation.Minimise': `Minimaliseren`,
  'Automation.ConfirmDelete': `Verwijderen verplaatst de overbodige bestanden naar de Prullenbak. Annuleren sluit het venster zonder te verwijderen.`,
  'Automation.ConfirmMove': `Verplaatsen zet de overbodige bestanden in de gekozen doelmap. Annuleren laat ze staan waar ze staan.`,
  'Automation.SayThanks': `Zeg bedankt`,
  'Automation.ConfirmSendResultLog': `Verzenden stuurt het getoonde rapport naar No Faff. Annuleren verstuurt niets.`,
  'Automation.CheckForUpdates': `Controleren op updates`,
  'Automation.CheckForUpdates.HelpText': `Kijkt op de releasepagina van github of er een nieuwere versie is.`,
  'Automation.About.Guide.HelpText': `Opent het readme-bestand op github in je browser.`,
  'Automation.About.ReportProblem.HelpText': `Opent de issue-tracker op github.com in je browser.`,
  'Automation.AutoUpdateCheck.HelpText': `Als dit is aangevinkt, kijkt InstallerClean bij het starten op github of er een nieuwere versie is.`,
  'Automation.UpdateAvailable.HelpText': `Open de releasepagina om de nieuwere versie te downloaden, of annuleer om de huidige versie te houden.`,
  'Automation.Licence.HelpText': `Opent het licentiebestand op github.com in je browser.`,
  'Automation.Section.BackupFolder': `Verplaatslocatie`,
  'Automation.Section.Products': `Producten`,
  'Automation.Section.Patches': `Patches`,
  'Automation.Section.ProductDetails': `Productgegevens`,
  'Automation.BackupFolder': `Verplaatslocatie`,
  'Automation.OperationProgress': `Voortgang van de bewerking`,
  'Automation.RescanInstaller': `{InstallerFolder} opnieuw scannen`,
  'Automation.ScanningProgress': `Voortgang van de scan`,
  'Automation.StartupScanProgress': `Voortgang van de opstartscan`,
  'Automation.ViewOrphanedFiles': `Details, overbodige bestanden`,
  'Automation.ViewOrphanedFiles.HelpText': `Beschikbaar om op te ruimen.`,
  'Automation.ViewRegisteredFiles': `Details, geregistreerde bestanden`,
  'Automation.ViewRegisteredFiles.HelpText': `Alleen-lezen overzicht.`,
  'Automation.SortStatus.Ascending': `Gesorteerd op {0}, oplopend`,
  'Automation.SortStatus.Descending': `Gesorteerd op {0}, aflopend`,
  'Automation.Scroll.ScanResults': `Scanresultaten`,
  'Automation.Scroll.ResultDetails': `Resultaatdetails`,
  'Automation.Scroll.FileDetails': `Bestandsdetails`,
  'Automation.Scroll.DialogBody': `Dialoogtekst`,
  'Automation.ScanResultAnnouncement': `{0} ({1})`,
  'Automation.CompletionErrors': `Bestanden die niet konden worden verwerkt`,
  'Automation.RegisteredMissingSeeAlso': `Legt uit wat deze map is en hoe je een bestand terugzet, in het README-bestand`,

  // Tooltips
  'Tooltip.BuyMeACuppa.About': `Het is dorstig werk!`,
  'Tooltip.CancellingPending': `Annulering aangevraagd. InstallerClean wacht tot de huidige stap een stoppunt bereikt. Dit kan enkele seconden duren bij zware I/O of een aanroep naar de MSI-database.`,
  'Tooltip.Close': `Sluiten`,
  'Tooltip.LeaveStarOnGitHub.About': `Een ster helpt anderen het te vinden.`,
  'Tooltip.Minimise': `Minimaliseren`,
  'Tooltip.SendResultLog': `Dat is aan jou, maar ik zou het op prijs stellen. Er wordt een anonieme samenvatting verstuurd die me alleen laat weten of het werkt en hoeveel ruimte mensen vrijmaken. Op het volgende scherm zie je precies wat er wordt verstuurd voordat je bevestigt.`,
  'Tooltip.SendResultLog.NothingFound': `Dat is aan jou, maar ik zou het op prijs stellen. Er wordt een anonieme samenvatting verstuurd die me alleen laat weten of het werkt. Op het volgende scherm zie je precies wat er wordt verstuurd voordat je bevestigt.`,
  'Tooltip.Move': `Verplaatst de overbodige bestanden naar de verplaatslocatie.`,
  'Tooltip.MoveNeedsDestination': `Zet de overbodige bestanden op een veilige plek. De map kies je hierna.`,
  'Tooltip.Delete': `Verplaatst de overbodige bestanden naar de Prullenbak.`,
  'Tooltip.SigningCertificate': `Naam van het onderwerp uit het ingesloten Authenticode-certificaat. Niet gecontroleerd via de certificaatketen.`,

  // Body copy
  'Body.MainExplanation.Lead': `Overbodige bestanden hieronder kun je veilig verwijderen.`,
  'Body.MainExplanation.Why': `Ze staan in {InstallerFolder}, achtergebleven toen een programma werd verwijderd ({0}), een nieuwere patch een oudere verving ({1}) of de uitgever hem introk ({2}). InstallerClean toont alleen bestanden waarvan Windows zelf aangeeft dat het ermee klaar is.`,
  'Body.MainExplanation.Action': `Verwijder ze naar de Prullenbak, of gebruik Verplaatsen om een back-up te houden. Zet je de bestanden terug in {InstallerFolder}, dan is alles weer precies zoals het was.`,
  'Body.NotScanned.Lead': `Nog niets gescand.`,
  'Body.NotScanned.Why': `Klik op Opnieuw scannen om {InstallerFolder} te doorzoeken op installatiebestanden die geen enkel programma nog nodig heeft.`,
  'Body.PendingReboot.Lead': `Deze bestanden kunnen nu niet worden opgeruimd.`,
  'Body.PendingReboot.MsiExecuteMutex': `Er is op dit moment iets met Windows Installer bezig, meestal een Windows-update of een programma dat op de achtergrond wordt geïnstalleerd. Verplaatsen en Verwijderen staan zolang stil, zodat InstallerClean niet aan de installatiecache komt terwijl die verandert. Zodra het klaar is: opnieuw scannen en ze zijn er weer.`,
  'Body.PendingReboot.InstallerInProgress': `Een eerdere Windows Installer-transactie is op deze computer opgeschort. Hervat die installatie of draai haar terug (of herstart Windows) voordat je de cache opschoont.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows heeft voor de volgende herstart een bestandshernoeming in de wachtrij staan die de installatiecache raakt. Herstart Windows voordat je opschoont.`,
  'Body.NoFileSelected': `Selecteer een bestand om de details te zien.`,
  'Body.NoProductSelected': `Selecteer een product om de details te zien.`,
  'Body.NoMetadata': `Geen metadata beschikbaar.`,
  'Body.RegisteredMissingFromDisk': `Dit installatiebestand is verwijderd. Dat heeft InstallerClean niet gedaan, het verwijdert nooit een bestand dat een programma nog nodig heeft; iets anders heeft dit bestand verwijderd voordat je InstallerClean startte.&#10;&#10;Het geeft nu geen problemen, en dat blijft zo tot de dag dat je het programma waar het bij hoort probeert te herstellen, bij te werken of te verwijderen. Die stap kan dan mislukken, omdat Windows dit bestand zoekt en het er niet is.&#10;&#10;Om het te proberen te herstellen: download de installer van dat programma bij de maker en voer hem uit over je bestaande installatie heen (niet eerst deïnstalleren; deïnstalleren is zelf een stap die dit bestand nodig heeft). Gebruik zo mogelijk de versie die je nu hebt geïnstalleerd, want Windows kan een andere weigeren. Meestal is het bestand daarmee terug en blijven je instellingen ongemoeid, maar Microsoft garandeert het niet; zijn eigen laatste redmiddel is het programma opnieuw installeren, of Windows zelf.`,
  'Body.RegisteredMissingFromDisk.SeeAlso': `Het README-bestand [legt uit wat deze map is], en hoe je een bestand terugzet, in Microsofts eigen woorden.`,
  'Body.NoPatches': `(geen)`,

  // Reasons (Reason column in the orphaned-files list)
  'Reason.Orphaned': `Verweesd`,
  'Reason.Superseded': `Vervangen`,
  'Reason.Obsoleted': `Verouderd`,

  // Status / progress text
  'Status.Scanning': `Scannen...`,
  'Status.Cancelling': `Annuleren...`,
  'Status.StartingScan': `Scan starten...`,
  'Status.QueryingApi': `Windows vragen naar geïnstalleerde software...`,
  'Status.ScanningCache': `Installatiecache scannen...`,
  'Status.EnumeratingProducts': `Geïnstalleerde producten doorlopen...`,
  'Status.CheckingRegistry': `Register controleren op extra pakketten...`,
  'Status.RegisteredPackagesFound': `{0} geregistreerde {1} gevonden.`,
  'Status.ScanComplete': `Scan voltooid ({0})`,
  'Status.FoundProducts': `Lokale pakketten scannen...`,
  'Status.FoundUnused': `Je kunt {0} {1} veilig verwijderen.`,
  'Status.PreparingDestination': `Doelmap voorbereiden...`,
  'Status.Moving': `Bezig met verplaatsen van {0} {1}...`,
  'Status.Deleting': `Bezig met verwijderen van {0} {1}...`,
  'Status.MoveCancelled.Partial': `Verplaatsen geannuleerd. {0} van {1} {2} verwerkt.`,
  'Status.DeleteCancelled.Partial': `Verwijderen geannuleerd. {0} van {1} {2} verwerkt.`,
  'Status.MoveFailed': `Verplaatsen mislukt ({0}). Details in {1}.`,
  'Status.MoveFailed.NoLog': `Verplaatsen mislukt ({0}). Het crashlog kon niet worden geschreven.`,
  'Status.DeleteFailed': `Verwijderen mislukt ({0}). Details in {1}.`,
  'Status.DeleteFailed.NoLog': `Verwijderen mislukt ({0}). Het crashlog kon niet worden geschreven.`,
  'Status.ScanAccessDenied': `Toegang geweigerd. Windows heeft de scan geweigerd.`,
  'Status.ScanFailedDb': `Scan mislukt: de Windows Installer-records konden niet worden gelezen.`,
  'Status.ScanCancelled': `Scan geannuleerd.`,
  'Status.Done': `Gereed`,
  'Status.ScanFailedDetails': `Scan mislukt ({0}). Details in {1}.`,
  'Status.ScanFailedDetails.NoLog': `Scan mislukt ({0}). Het crashlog kon niet worden geschreven.`,

  // Completion screen
  'Completion.AllClean': `Alles schoon`,
  'Completion.NothingToCleanUp': `Niets op te ruimen in {InstallerFolder}`,
  'Completion.NothingToCleanUpReceipt': `{0} {1} gescand in {2}`,
  'Completion.Freed': `{0} vrijgemaakt`,
  'Completion.Moved': `{0} verplaatst`,
  'Completion.NothingMoved': `Er is niets verplaatst`,
  'Completion.NothingDeleted': `Er is niets verwijderd`,
  'Completion.FailedCount.Singular': `{0} van {1} kon niet worden verplaatst.`,
  'Completion.FailedCount.Plural': `{0} van {1} konden niet worden verplaatst.`,
  'Completion.FailedCountDelete.Singular': `{0} van {1} kon niet worden verwijderd.`,
  'Completion.FailedCountDelete.Plural': `{0} van {1} konden niet worden verwijderd.`,
  'Completion.MoveSummary.Singular': `{0} {1} verplaatst naar: {2}`,
  'Completion.MoveSummary.Plural': `{0} {1} verplaatst naar: {2}`,
  'Completion.ReverifySkipped': `{0} {1} behouden, omdat een programma ze na de scan weer nodig bleek te hebben.`,
  'Completion.ReverifyIncomplete': `{0} {1} behouden, omdat de Windows Installer-records bij de herhaalde controle niet volledig konden worden gelezen.`,
  'Completion.MoveCancelledSummary': `{0} van {1} {2} verplaatst voordat je annuleerde.`,
  'Completion.PermanentDeleteCancelledSummary': `{0} van {1} {2} definitief verwijderd voordat je annuleerde.`,
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} definitief verwijderd. Het is niet in de Prullenbak beland.`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} definitief verwijderd. Ze zijn niet in de Prullenbak beland.`,
  'Completion.DonateAsk': `Graag gedaan. Er staat een fooienpot klaar, mocht je je gul voelen.`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} bestand nog nodig`,
  'Summary.RegisteredStillUsed.Plural': `{0} bestanden nog nodig`,
  'Summary.OrphanedToCleanUp.Singular': `{0} overbodig bestand om op te ruimen`,
  'Summary.OrphanedToCleanUp.Plural': `{0} overbodige bestanden om op te ruimen`,
  'Summary.MissingFromDisk.Singular': `{0} geregistreerd bestand ontbreekt (niet door InstallerClean verwijderd). Nu geen probleem, maar een toekomstige herstel-, update- of verwijderactie van dat programma kan mislukken. Open Details voor wat je kunt doen.`,
  'Summary.MissingFromDisk.Plural': `{0} geregistreerde bestanden ontbreken (niet door InstallerClean verwijderd). Nu geen probleem, maar een toekomstige herstel-, update- of verwijderactie van die programma's kan mislukken. Open Details voor wat je kunt doen.`,
  'Summary.ProgramsUnreadable.Singular': `{0} geïnstalleerd programma kon tijdens deze scan niet worden gelezen; vervangen patches zijn daarom behouden. Voor verweesde bestanden maakt dit niets uit.`,
  'Summary.ProgramsUnreadable.Plural': `{0} geïnstalleerde programma's konden tijdens deze scan niet worden gelezen; vervangen patches zijn daarom behouden. Voor verweesde bestanden maakt dit niets uit.`,
  'Summary.OperationFiles': `{0} van {1} {2}`,
  'Summary.OrphanedWindow': `{0} verweesd, {1} vervangen, {2} verouderd ({3})`,
  'Summary.RegisteredWindow.Singular': `{0} geregistreerd bestand dat nog nodig is ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} geregistreerde bestanden die nog nodig zijn ({1})`,

  // Confirmation dialogs
  'Confirm.MoveTitle': `{0} {1} verplaatsen ({2})?`,
  'Confirm.MoveDestination': `De bestanden gaan naar:`,
  'Confirm.DeleteTitle': `{0} {1} verwijderen ({2})?`,
  'Confirm.MoveSameDrive': `Deze map staat op dezelfde schijf, dus het verplaatsen maakt op zichzelf geen ruimte vrij. De ruimte komt terug zodra je de bestanden daaruit verwijdert, of kies een map op een andere schijf.`,

  // Error messages
  'Error.AdminRequiredTitle': `Toegang geweigerd`,
  'Error.AdminRequiredBody': `Windows heeft InstallerClean de toegang geweigerd, dus het is gestopt. Er is niets verwijderd.\n\nInstallerClean draaide al als administrator, dus opnieuw starten op die manier helpt niet. Windows zegt er niet bij wat er precies weigerde, dus er valt niets gerichts te proberen.`,
  'Error.InstallerDbUnavailableTitle': `Kon de Windows Installer-records niet lezen`,
  'Error.ScanFailedTitle': `Scan mislukt`,
  'Error.InstallerDbEmpty': `De Windows Installer-records kwamen volledig leeg terug: geen enkel geïnstalleerd programma of geen enkele update maakt aanspraak op een installatiebestand in de cache. Dat komt op een werkende computer niet voor (zelfs een verse Windows-installatie heeft er een paar), dus of de records zijn beschadigd, of ze konden niet worden gelezen, en een scan die dit antwoord zou geloven, zou elk bestand in {InstallerFolder} ten onrechte verweesd noemen. InstallerClean is daarom gestopt. Er is niets verwijderd.`,
  'Error.MsiAccessDenied': `Windows Installer weigerde InstallerClean te laten zien wat er geïnstalleerd is. InstallerClean draaide al als administrator, dus het nogmaals als administrator uitvoeren verandert niets. Zonder die lijst is er geen veilige manier om te bepalen welke bestanden in de cache nog nodig zijn, dus InstallerClean is gestopt. Er is niets verwijderd.`,
  'Error.MsiNonSuccess': `Windows Installer kon InstallerClean geen leesbare lijst van de geïnstalleerde programma's geven: {0} vermeldingen op rij kwamen onleesbaar terug (laatste foutcode {1}). In plaats van met een half gelezen lijst te werken is InstallerClean gestopt. Er is niets verwijderd.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer heeft het einde van de lijst met geïnstalleerde programma's nooit gemeld: InstallerClean heeft het na {0} vermeldingen opgegeven (laatste foutcode {1}). Een lijst zonder einde is niet te vertrouwen, dus InstallerClean is gestopt. Er is niets verwijderd.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer heeft het einde van de patchlijst van één programma nooit gemeld: InstallerClean heeft het na {0} vermeldingen opgegeven (laatste foutcode {1}). Een lijst zonder einde is niet te vertrouwen, dus InstallerClean is gestopt. Er is niets verwijderd.`,
  'Error.ScanCorrelationFailed': `InstallerClean kreeg deze scan niet te rijmen met de Windows Installer-records: elk bestand dat Windows nog als nodig aanmerkt, ontbreekt in {InstallerFolder}, terwijl de bestanden die echt in de map staan met niets in de records overeenkomen. Zo ziet geen echte computer eruit, dus dit wijst op een probleem met het lezen van de records, niet op bestanden die je veilig kunt verwijderen. Er is niets voor opruiming aangeboden en er is niets verwijderd.`,
  'Error.ScanRecordsUnreadable': `InstallerClean kon niet genoeg van de Windows Installer-records lezen om zeker te weten wat er nog nodig is: de lijst met geïnstalleerde programma's kwam te kort terug, en dezelfde records rechtstreeks uit het register lezen leverde ook fouten op. Een bestand kon er verweesd uitzien enkel doordat het record dat het benoemt een van de onleesbare was, dus InstallerClean is gestopt. Er is niets verwijderd.`,
  'Error.InvalidDestinationTitle': `Ongeldige bestemming`,
  'Error.DestinationWriteFailedTitle': `Kon niet naar de bestemming schrijven`,
  'Error.MoveFailedTitle': `Verplaatsen mislukt`,
  'Error.DeleteFailedTitle': `Verwijderen mislukt`,
  'Error.SettingNotSavedTitle': `Instelling niet opgeslagen`,
  'Error.SettingNotSavedBody': `De wijziging kon niet worden opgeslagen. InstallerClean gaat bij de volgende start terug naar de vorige instelling.`,
  'Error.DestinationInsideInstaller': `De bestemming mag niet in de Windows Installer-map liggen.`,
  'Error.DestinationInSystemFolder': `De bestemming {0} komt uit onder een Windows-systeemmap. Kies een pad buiten %SystemRoot%, %ProgramFiles% en %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Onvoldoende ruimte`,
  'Error.NotEnoughSpaceBody': `Onvoldoende ruimte op {0}\n\nNodig: {1}\nBeschikbaar: {2}`,
  'Error.AccessDeniedDestination': `Je hebt geen rechten om naar {0} te schrijven.\nProbeer een map in je gebruikersprofiel of op een schijf die van jou is.`,
  'Error.PathTooLong': `Het pad {0} is te lang voor Windows. Kies een korter pad.`,
  'Error.DestinationMissing': `De map {0} bestaat niet en kon niet worden aangemaakt. Controleer de stationsletter of het netwerkpad.`,
  'Error.IOWriteDestination': `Windows kan niet schrijven naar {0}.\nDetails in {1}.`,
  'Error.IOWriteDestination.NoLog': `Windows kan niet schrijven naar {0}. Het crashlog kon niet worden geschreven.`,
  'Error.WriteDestination': `Kan niet schrijven naar {0}.\nDetails in {1}.`,
  'Error.WriteDestination.NoLog': `Kan niet schrijven naar {0}. Het crashlog kon niet worden geschreven.`,
  'Error.MissingSourceFile': `Bestand bestaat niet meer.`,
  'Error.SourceIsReparsePoint': `Bronbestand is een symlink of junction; voor de veiligheid geweigerd.`,
  'Error.CandidateOutsideCache': `Dit bestand staat niet direct in de Windows Installer-map; voor de veiligheid geweigerd.`,
  'Error.AccessDenied.Singular': `Windows heeft de toegang tot dit bestand geweigerd; het is blijven staan.`,
  'Error.AccessDenied.Plural': `Windows heeft de toegang tot deze bestanden geweigerd; ze zijn blijven staan.`,
  'Error.FileInUse.Singular': `Dit bestand is geopend of vergrendeld door een ander programma, dus niets kan het nu verplaatsen. Het is blijven staan; probeer het later opnieuw.`,
  'Error.FileInUse.Plural': `Deze bestanden zijn geopend of vergrendeld door een ander programma, dus niets kan ze nu verplaatsen. Ze zijn blijven staan; probeer het later opnieuw.`,
  'Error.IOFailure.Singular': `Windows meldde een bestandsfout; het bestand is blijven staan.`,
  'Error.IOFailure.Plural': `Windows meldde bestandsfouten; deze bestanden zijn blijven staan.`,
  'Error.UnknownError.Singular': `Er ging iets mis met dit bestand; het is blijven staan.`,
  'Error.UnknownError.Plural': `Er ging iets mis met deze bestanden; ze zijn blijven staan.`,
  'Error.MoveIntoInstaller': `Weigert bestanden naar de Windows Installer-map te verplaatsen (bestemming: {0}).`,
  'Error.DestinationNotFullyQualified': `De verplaatslocatie moet een volledig pad naar een map zijn, beginnend met een stationsletter of een netwerkshare (bijvoorbeeld D:\\Backup of \\\\server\\backup). InstallerClean kan hier niets mee: {0}`,
  'BrowserLaunch.FailedTitle': `Kon je browser niet openen`,
  'UpdateCheck.Title': `Controleren op updates`,
  'UpdateCheck.Status.Checking': `Controleren...`,
  'UpdateCheck.Status.UpToDate': `Je bent bij.`,
  'UpdateCheck.Status.UpdateAvailable': `Versie {0} is beschikbaar.`,
  'UpdateCheck.UpdateAvailable.Title': `Update beschikbaar`,
  'UpdateCheck.UpdateAvailable.Body': `Je draait versie {0}.&#10;Versie {1} is beschikbaar.`,
  'UpdateCheck.Failed.NetworkUnavailable': `Kon GitHub niet bereiken. Controleer je internetverbinding en probeer het opnieuw.`,
  'UpdateCheck.Failed.ServerError': `GitHub gaf een foutmelding terug. Probeer het over een paar minuten opnieuw.`,
  'UpdateCheck.Failed.ResponseParseError': `In het antwoord van GitHub was geen release te herkennen. Probeer het later opnieuw, of open de releasepagina rechtstreeks.`,
  'UpdateCheck.Failed.Timeout': `De controle duurde te lang. Je verbinding met GitHub is misschien traag; probeer het opnieuw.`,
  'UpdateCheck.Failed.Unknown': `De controle is om een onbekende reden mislukt. De details staan in crash.log, mocht je het willen melden.`,
  'BrowserLaunch.ClipboardOk': `InstallerClean kon je browser niet openen. De link staat op je klembord, dus je kunt hem zelf plakken:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean kon je browser niet openen, en kon de link ook niet naar je klembord kopiëren. De link is:&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `De verplaatslocatie veranderde terwijl de bestanden werden verplaatst (iets heeft de map vervangen of omgeleid), dus InstallerClean is gestopt om niet op de verkeerde plek te schrijven. Controleer {0}, scan opnieuw en probeer het nog eens.`,
  'Error.CannotWriteFolder': `Kan niet schrijven naar {0}.`,
  'Error.NoUniqueFilename': `Kon na 10.000 pogingen geen unieke bestandsnaam vinden voor '{0}'.`,

  // Result log (post-cleanup diagnostic send)
  'ResultLog.Sending': `Verzenden...`,
  'ResultLog.Sent': `Bedankt! Rapport verzonden.`,
  'ResultLog.Failed': `Verzenden mislukt. Probeer het later opnieuw.`,
  'ResultLog.NothingToSend': `Geen rapport om te verzenden.`,
  'ConfirmSendResultLog.Title': `Dit versturen?`,
  'ConfirmSendResultLog.Reassurance': `Het gaat naar nofaff.netlify.app/api/result-log. Niets identificeert jou of je computer; het laat me alleen weten dat InstallerClean werkt en [hoeveel ruimte mensen vrijmaken].`,
  'Automation.ResultLogPreview': `Rapportvoorbeeld`,

  // Single instance / startup / crash
  'Startup.AlreadyRunningTitle': `InstallerClean`,
  'Startup.AlreadyRunningBody': `InstallerClean is al actief.`,
  'Startup.UnhandledTitle': `InstallerClean`,
  'Startup.UnhandledBody': `Er is een onverwachte fout opgetreden en InstallerClean moet afsluiten.\n\n{0}\n\nDetails weggeschreven naar:\n{1}`,
  'Startup.UnhandledBody.NoLog': `Er is een onverwachte fout opgetreden en InstallerClean moet afsluiten.\n\n{0}\n\nHet crashlog kon niet worden geschreven.`,
  'Startup.ErrorTitle': `Opstartfout`,
  'Startup.FailedToStart': `Starten mislukt ({0}). Details weggeschreven naar:\n{1}`,
  'Startup.FailedToStart.NoLog': `Starten mislukt ({0}). Het crashlog kon niet worden geschreven.`,

  // File picker
  'FilePicker.ChooseDestinationTitle': `Kies de doelmap voor de verplaatste bestanden`,

  // Version display
  'Version.Display': `Versie {0}`,
  'Plural.File.Singular': `bestand`,
  'Plural.File.Plural': `bestanden`,
  'Plural.Error.Singular': `fout`,
  'Plural.Error.Plural': `fouten`,
  'Plural.Package.Singular': `pakket`,
  'Plural.Package.Plural': `pakketten`,
  'Plural.Product.Singular': `product`,
  'Plural.Product.Plural': `producten`,
  'Plural.Patch.Singular': `patch`,
  'Plural.Patch.Plural': `patches`,
  'Display.Size.GB': `{0:F2} GB`,
  'Display.Size.MB': `{0:F1} MB`,
  'Display.Size.KB': `{0:F1} KB`,
  'Display.Size.B': `{0} B`,
  'Display.Elapsed.Ms': `{0:F0}ms`,
  'Display.Elapsed.S': `{0:F1}s`,
  'Display.ElapsedLong.LessThanASecond': `minder dan een seconde`,
  'Display.ElapsedLong.Seconds': `{0:F1} seconden`,
  'CrashLog.PrivacyHeader': `# crash.log legt onafgevangen fouten van InstallerClean vast.\n# Met verhoogde rechten kunnen de foutmeldingen van het framework\n# bestandspaden uit de lopende sessie bevatten (ook uit profielen van\n# andere gebruikers die Windows Installer-query's doorlopen). Meldingen\n# over netwerkfouten van de updatecontrole of de rapport-POST kunnen de\n# bestemmings-URL en het opgeloste IP-adres of proxyadres bevatten.\n# Haal beide soorten details weg voordat je dit bestand bij een openbaar\n# bugrapport voegt.\n`,
  'Tooltip.ChangeLanguage': `Taal wijzigen. Het programma start opnieuw.`,
  'Automation.ChangeLanguage': `Taal wijzigen`,
  'Automation.ChangeLanguage.HelpText': `Het programma start opnieuw.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys. The
  // 21 machine-contract Cli.EventLog* keys (bar Cli.EventLogUnavailable) are NOT
  // here and are stripped from the output; they stay English at runtime. In the
  // help lines translate the DESCRIPTION only: keep the command tokens, flags,
  // the {InstallerFolder} token and the exit-code numbers verbatim, keep the
  // leading spaces and column alignment (the screen is column-aligned for a
  // monospace terminal; PAD is one character shorter than PATH, so its line
  // carries one more space), and translate the PATH metavariable to PAD (it
  // names the argument, as es/pt-BR/it did).
  'Cli.UnknownArgument': `Onbekend argument: '{0}'`,
  'Cli.TooManyArguments': `Fout: onverwacht extra argument '{0}'. Staat er een spatie in je verplaatsingsmap, zet dan aanhalingstekens om het hele pad: /m "D:\\My Backup"`,
  'Cli.Cancelling': `Annuleren...`,
  'Cli.Cancelled': `Geannuleerd.`,
  'Cli.GenericError': `Fout: {0}. Details weggeschreven naar {1}.`,
  'Cli.GenericError.NoLog': `Fout: {0}. Het crashlog kon niet worden geschreven.`,
  'Cli.ScanningInstaller': `{InstallerFolder} scannen...`,
  'Cli.FoundOrphans': `{0} {1} gevonden om op te ruimen ({2}).`,
  'Cli.NothingToDo': `Niets te doen.`,
  'Cli.DeletingFiles': `Bezig met verwijderen van {0} {1}...`,
  'Cli.DeletedFiles': `{0} {1} verwijderd.`,
  'Cli.NoMoveDestination': `Fout: geen verplaatsbestemming opgegeven. Gebruik /m PAD. (Een standaard die in de GUI is ingesteld, geldt per gebruiker en niet voor geplande taken of serviceaccounts.)`,
  'Cli.MoveDestinationInsideInstaller': `Fout: de bestemming mag niet in de Windows Installer-map liggen.`,
  'Cli.MoveDestinationRelative': `Fout: de bestemming moet een volledig gekwalificeerd pad zijn. Gekregen: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Fout: de bestemming {0} komt uit onder een Windows-systeemmap. Kies een pad buiten %SystemRoot%, %ProgramFiles% en %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Fout: er is op dit moment iets met Windows Installer bezig, meestal een Windows-update of een programma dat op de achtergrond wordt geïnstalleerd. Verplaatsen en Verwijderen zijn zolang geblokkeerd. Probeer het opnieuw zodra het klaar is.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Fout: een eerdere Windows Installer-transactie is op deze computer opgeschort. Hervat die installatie of draai haar terug (of herstart Windows) voordat je de cache opschoont.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Fout: een bestandsbewerking die na de herstart in de wachtrij staat, richt zich op de installatiecache ({0}). Herstart Windows om die bewerking te voltooien voordat je opschoont.`,
  'Cli.MovingFiles': `Bezig met verplaatsen van {0} {1} naar {2}...`,
  'Cli.MovedFiles': `{0} {1} verplaatst.`,
  'Cli.MutexBlocked': `Een ander InstallerClean-proces houdt de single-instance-vergrendeling vast (de GUI of een andere CLI-run). Exit 75 (tijdelijk); later opnieuw proberen kan veilig.`,
  'Cli.EventLogUnavailable': `Let op: schrijven naar het gebeurtenislogboek is mislukt. Controleer de rechten op het logboek Toepassing of het groepsbeleid.`,
  'Cli.Help.Header': `InstallerClean - {InstallerFolder} opschonen`,
  'Cli.Help.Usage': `Gebruik:`,
  'Cli.Help.Help': `  installerclean-cli --help     Deze hulp tonen (ook /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  De versie tonen (ook -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Alleen scannen - verwijderbare bestanden`,
  'Cli.Help.Delete': `  installerclean-cli /d         Verwijderbare bestanden (Prullenbak)`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Naar de opgeslagen standaardlocatie`,
  'Cli.Help.MovePath': `  installerclean-cli /m PAD     Naar het opgegeven pad`,
  'Cli.Help.NoteLine1': `installerclean-cli is een echt consoleproces en blokkeert de prompt`,
  'Cli.Help.NoteLine2': `tot het klaar is; leid de uitvoer om of door (pipe) zoals bij elke`,
  'Cli.Help.NoteLine3': `andere console-exe. De GUI zit ernaast, in InstallerClean.exe.`,
  'Cli.Help.MoveScheduledNote': `De opgeslagen standaard is per gebruiker; geplande of SYSTEM-runs: /m PAD.`,
  'Cli.Help.ExitCodesHeader': `Afsluitcodes:`,
  'Cli.Help.ExitCodeOk': `  0   gelukt: elk aangemerkt bestand is verwerkt`,
  'Cli.Help.ExitCodeError': `  1   mislukt: niets verwerkt (foute argumenten, scan of alle bestanden)`,
  'Cli.Help.ExitCodePartial': `  2   gedeeltelijk: sommige bestanden verwerkt, sommige mislukt`,
  'Cli.Help.ExitCodeTransient': `  75  tijdelijk: iets blokkeerde de run (zie de melding)`,
  'Cli.Help.ExitCodeCancelled': `  130 geannuleerd (Ctrl+C)`,
};

let text = readFileSync(BASE, 'utf8');

// Remove ONLY the 21 machine-contract Cli.* <data> elements BY NAME (the
// Cli.EventLog* set bar Cli.EventLogUnavailable): each is matched non-greedy to
// its own </data>. The human-facing Cli keys are KEPT, and their value is
// replaced from the MAP like any other key. Same predicate as
// scripts/check-resx-parity.mjs. The section comments left orphaned by a removed
// machine key are left in place deliberately: removing them needs fragile
// anchors that name specific keys, the exact step that broke before. They are
// harmless XML comments. Do NOT reintroduce comment surgery to "tidy" them.
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
let cliMachineRemoved = 0;
text = text.replace(/[^\S\n]*<data name="(Cli\.[^"]*)"[\s\S]*?<\/data>\n?/g,
  (m, name) => { if (isMachineCliKey(name)) { cliMachineRemoved++; return ''; } return m; });

// Replace each key's inner <value> from MAP. The capture keeps the <data> tag,
// its attributes and the whitespace before <value>; any <comment> child and the
// </data> close sit outside the match. The closing quote anchors the name, so
// Status.MoveFailed never matches Status.MoveFailed.NoLog. A function replacement
// keeps $-sequences in a value from being read as backreferences.
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
// Required = everything a satellite must carry: the non-Cli keys plus the
// human-facing Cli keys. The 21 machine Cli keys are the complement; they must be
// absent from the output (isMachineCliKey is defined up in the strip section).
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
  if (!output.has(k)) return false; // already counted by missingFromOutput
  const a = placeholders(neutral.get(k)), b = placeholders(output.get(k));
  return a.size !== b.size || [...a].some((i) => !b.has(i));
});
const crlf = (written.match(/\r/g) || []).length;

// Untranslated-phrase gate (KEY-based, HARD): a value still byte-identical to the
// English neutral is a miss, UNLESS its key is a universal keep or in ALSO_KEEP.
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

// ALSO_KEEP audit roster, so a lazy "force it green" dump is visible at a glance.
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
  if (untranslated.length > 50)
    console.log('   (that is most of the file: this is the untranslated template. Translate the MAP values, then a real miss is listed on its own.)');
}

const structuralOk = !notApplied.length && !missingFromMap.length && !strayMapKeys.length &&
  !missingFromOutput.length && !arityMismatch.length && !machineLeaked.length &&
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === 21 && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
