#!/usr/bin/env node
// Dutch (nl) satellite generator for InstallerClean. Copied from
// gen-strings-template.mjs (the full-resx template) and filled with Dutch.
// It works FROM THE ENGLISH SOURCE (Strings.resx): it reads the neutral as the
// structural base, strips ONLY the machine-contract Cli.* keys, replaces the
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
// many), and the progress lines are bare-noun infinitives ("Installatiecache
// scannen..."), which carry no number at all, so the progress overrides
// German needs for wird/werden have no Dutch counterpart. Dutch DOES
// inflect the attributive adjective ("1 geregistreerd pakket" vs "120
// geregistreerde pakketten": indefinite singular before a het-word drops the
// -e, and both counted nouns, pakket and product, are het-words), and the
// counted CLI lines interpolate the file noun after "overbodig", which inflects
// the same way. Hence the .One overrides below, and count the block rather than
// this paragraph. A held-back key carried one more for a pronoun (het at one file,
// ze at many); that override was retired when the English lost its pronoun, and the
// key itself went in the 3.0.0 round that replaced four held-back sentences with
// one Completion.HeldBack pair.
import { readFileSync, writeFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;            // English source (the "neutral")
const OUT = `${dir}/Strings.nl.resx`;

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
  'Window.Main.Title',                 // InstallerClean
  'Startup.AlreadyRunningTitle',       // InstallerClean
  'Startup.UnhandledTitle',            // InstallerClean
  'Automation.ScanResultAnnouncement', // {0} ({1})
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
  // The list separator Dutch uses is the one English uses. A punctuation
  // mark rather than a word, so there is nothing to translate and nothing to
  // get wrong; only ja and zh-Hans differ, taking the ideographic comma.
  'Display.ListSeparator',       // ", "
  // The size and elapsed unit suffixes. Dutch abbreviates them exactly as
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

// Satellite-only CLDR plural overrides. All six are the same point of Dutch
// grammar: an attributive adjective before an indefinite singular het-word
// drops its -e, so "1 overbodig bestand" against "3 overbodige bestanden", and
// no gate can see a missing one (flag-retranslation only ever resets an
// override, never creates one). The CLI's five counted lines each interpolate
// the file noun after "overbodig", so each needs its own singular.
const OVERRIDES = {
  'Status.RegisteredPackagesFound.One': `{0} geregistreerd {1} gevonden.`,
  'Cli.FoundOrphans.One': `{0} overbodig {1} gevonden om op te ruimen ({2}).`,
  'Cli.DeletingFiles.One': `{0} overbodig {1} verwijderen...`,
  'Cli.DeletedFiles.One': `{0} overbodig {1} definitief verwijderd.`,
  'Cli.MovingFiles.One': `{0} overbodig {1} verplaatsen naar {2}...`,
  'Cli.MovedFiles.One': `{0} overbodig {1} verplaatst.`,
};

const MAP = {
  // Window titles
  'Window.Main.Title': `InstallerClean`,
  'Window.About.Title': `Over`,
  'Window.Registered.Title': `Ongemoeid gelaten bestanden`,
  'Window.Orphaned.Title': `Overbodige bestanden die veilig kunnen worden verwijderd`,

  // Section headings
  // Section.Registered.Products and Automation.Section.Products are deliberately
  // not here. They left the neutral resx when the registered-files window stopped
  // having a products group of its own, so a value for either would be
  // regenerated into a key the app cannot use.
  'Section.Registered.Patches': `PATCHES`,
  'Section.Registered.Details': `PRODUCTGEGEVENS`,
  'Section.Backup.Folder': `BACK-UPMAP`,
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
  'Action.BuyMeACuppa': `Trakteer me op een kopje _koffie`,
  'Action.LeaveStarOnGitHub': `Geef een s_ter op GitHub`,
  'Action.Licence': `Apache 2.0-licentie`,
  'Action.Move': `Ver_plaatsen`,
  'Action.BackupFolderPlaceholder': `Pad naar de map als je verplaatst in plaats van verwijdert.`,
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
  'Automation.BuyMeACuppa.About': `Trakteer me op een kopje koffie`,
  'Automation.CancelOperation': `Bewerking annuleren`,
  'Automation.CancelScan': `Scan annuleren`,
  'Automation.CancelStartupScan': `Opstartscan annuleren`,
  'Automation.Close': `Sluiten`,
  'Automation.CloseWindow': `Venster sluiten`,
  'Automation.CloseResult': `Resultaat sluiten en terug naar het hoofdvenster`,
  'Automation.LeaveStarOnGitHub.About': `Geef een ster op github`,
  'Automation.Minimise': `Minimaliseren`,
  'Automation.ConfirmDelete': `Definitief verwijderen haalt de overbodige bestanden weg. Annuleren sluit het venster zonder iets te verwijderen.`,
  'Automation.ConfirmMove': `Verplaatsen zet de overbodige bestanden in de gekozen doelmap. Annuleren laat ze waar ze zijn.`,
  'Automation.SayThanks': `Zeg bedankt`,
  'Automation.ConfirmSendResultLog': `Verzenden stuurt het getoonde rapport naar No Faff. Annuleren stuurt niets.`,
  'Automation.CheckForUpdates': `Controleren op updates`,
  'Automation.CheckForUpdates.HelpText': `Kijkt op de releasepagina van github of er een nieuwere versie is.`,
  'Automation.About.Guide.HelpText': `Opent het readme-bestand op github in je browser.`,
  'Automation.About.ReportProblem.HelpText': `Opent de issue-tracker op github.com in je browser.`,
  'Automation.AutoUpdateCheck.HelpText': `Als dit is aangevinkt, kijkt InstallerClean bij het starten op github of er een nieuwere versie is.`,
  'Automation.UpdateAvailable.HelpText': `Open de releasepagina om de nieuwere versie te downloaden, of annuleer om de huidige versie te behouden.`,
  'Automation.Licence.HelpText': `Opent het licentiebestand op github.com in je browser.`,
  'Automation.Section.BackupFolder': `Back-upmap`,
  'Automation.Section.Patches': `Patches`,
  'Automation.Section.ProductDetails': `Productgegevens`,
  'Automation.BackupFolder': `Back-upmap`,
  'Automation.OperationProgress': `Voortgang van de bewerking`,
  'Automation.RescanInstaller': `{InstallerFolder} opnieuw scannen`,
  'Automation.ScanningProgress': `Voortgang van de scan`,
  'Automation.StartupScanProgress': `Voortgang van de opstartscan`,
  'Automation.ViewOrphanedFiles': `Details, overbodige bestanden`,
  'Automation.ViewOrphanedFiles.HelpText': `Beschikbaar om op te ruimen.`,
  'Automation.ViewRegisteredFiles': `Details, ongemoeid gelaten bestanden`,
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
  'Tooltip.SendResultLog': `Dat is aan jou, maar ik zou het op prijs stellen. Er wordt een anonieme samenvatting verstuurd die me alleen laat weten of het werkt en hoeveel ruimte mensen vrijmaken. Op het volgende scherm kun je zien wat er wordt verstuurd voordat je het bevestigt.`,
  'Tooltip.SendResultLog.NothingFound': `Dat is aan jou, maar ik zou het op prijs stellen. Er wordt een anonieme samenvatting verstuurd die me alleen laat weten of het werkt. Op het volgende scherm kun je zien wat er wordt verstuurd voordat je het bevestigt.`,
  'Tooltip.Move': `De overbodige bestanden naar de back-upmap verplaatsen.`,
  'Tooltip.MoveNeedsDestination': `De overbodige bestanden naar een back-upmap verplaatsen. Die kies je hierna.`,
  'Tooltip.Delete': `De overbodige bestanden definitief verwijderen. Gebruik Verplaatsen als je jezelf eerst wilt overtuigen dat alles goed is.`,
  'Tooltip.SigningCertificate': `Naam van het onderwerp uit het ingebedde Authenticode-certificaat. Niet via de certificaatketen geverifieerd.`,

  // Body copy
  'Body.MainExplanation.Lead': `Overbodige bestanden hieronder zijn [veilig te verwijderen].`,
  'Body.MainExplanation.Why': `Ze staan in {InstallerFolder}. InstallerClean vraagt Windows naar elk geïnstalleerd programma: een bestand komt in de lijst wanneer geen enkel programma het opeist ({0}), of wanneer een nieuwere patch het heeft vervangen en geen enkel programma erop kan terugvallen ({1}).`,
  'Body.MainExplanation.Action': `Verplaats ze naar een back-upmap die je zelf kiest en verwijder die map zodra je zeker weet dat je programma's nog gewoon bijwerken en verwijderen. Terugzetten in {InstallerFolder} herstelt alles. Of verwijder ze nu definitief.`,
  'Body.NotScanned.Lead': `Nog niets gescand.`,
  'Body.NotScanned.Why': `Klik op Opnieuw scannen om {InstallerFolder} te doorzoeken op installatiebestanden die geen enkel programma nog nodig heeft.`,
  'Body.PendingReboot.Lead': `Deze bestanden kunnen op dit moment niet worden opgeruimd.`,
  'Body.PendingReboot.MsiExecuteMutex': `Er is op dit moment iets bezig met Windows Installer, zoals een Windows-update of een programma dat op de achtergrond installeert. Verplaatsen en Verwijderen staan stil zolang dat loopt, zodat InstallerClean {InstallerFolder} niet aanraakt terwijl daar iets verandert. Zodra het klaar is, klik je op Opnieuw scannen en komen ze terug.`,
  'Body.PendingReboot.InstallerInProgress': `Een eerdere Windows Installer-transactie is op deze computer opgeschort. Hervat die installatie of draai haar terug (of herstart Windows) voordat je {InstallerFolder} opruimt.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows heeft voor de volgende herstart een bestandshernoeming in de wachtrij staan die {InstallerFolder} raakt. Herstart Windows voordat je opruimt.`,
  'Body.NoFileSelected': `Selecteer een bestand om de details te bekijken.`,
  'Body.NoProductSelected': `Selecteer een product om de details te bekijken.`,
  'Body.NoMetadata': `Geen metadata beschikbaar.`,
  'Body.RegisteredMissingFromDisk': `Dit installatiebestand ontbreekt. Dat levert nu geen problemen op, en dat blijft zo tot de dag waarop je het bijbehorende programma wilt bijwerken of verwijderen. Die stap kan dan mislukken, omdat Windows dit bestand zoekt en het er niet is.\n\nOm het terug te zetten heb je het installatieprogramma nodig van de versie die je al hebt. Haal het bij de maker van het programma en voer het uit over je bestaande installatie heen. Een nieuwere versie volstaat niet: die moet eerst verwijderen wat je hebt, en juist die stap heeft dit bestand nodig. Eerst verwijderen werkt om dezelfde reden ook niet. Dit zou het bestand moeten herstellen en je instellingen ongemoeid moeten laten, maar Microsoft geeft daar geen garantie op.`,
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
  'Status.EnumeratingProducts': `Geïnstalleerde producten opsommen...`,
  'Status.CheckingRegistry': `Register controleren op extra pakketten...`,
  'Status.RegisteredPackagesFound': `{0} geregistreerde {1} gevonden.`,
  'Status.ScanComplete': `Scan voltooid ({0})`,
  'Status.FoundProducts': `Lokale pakketten scannen...`,
  'Status.FoundUnused': `Je kunt {0} {1} veilig verwijderen.`,
  'Status.PreparingDestination': `Doelmap voorbereiden...`,
  'Status.Moving': `Overbodige bestanden verplaatsen...`,
  'Status.Deleting': `Overbodige bestanden verwijderen...`,
  'Status.MoveCancelled.Partial': `Verplaatsen geannuleerd. {0} van {1} {2} verwerkt.`,
  'Status.DeleteCancelled.Partial': `Verwijderen geannuleerd. {0} van {1} {2} verwerkt.`,
  'Status.MoveFailed': `Verplaatsen mislukt ({0}). Details in {1}.`,
  'Status.MoveFailed.NoLog': `Verplaatsen mislukt ({0}). Het crashlog kon niet worden weggeschreven.`,
  'Status.DeleteFailed': `Verwijderen mislukt ({0}). Details in {1}.`,
  'Status.DeleteFailed.NoLog': `Verwijderen mislukt ({0}). Het crashlog kon niet worden weggeschreven.`,
  'Status.ScanAccessDenied': `Toegang geweigerd. Windows heeft de scan geweigerd.`,
  'Status.ScanFailedDb': `Scan mislukt: de Windows Installer-records konden niet worden gelezen.`,
  'Status.ScanCancelled': `Scan geannuleerd.`,
  'Status.Done': `Klaar`,
  'Status.ScanFailedDetails': `Scan mislukt ({0}). Details in {1}.`,
  'Status.ScanFailedDetails.NoLog': `Scan mislukt ({0}). Het crashlog kon niet worden weggeschreven.`,

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
  'Completion.MoveCancelledSummary': `{0} van {1} {2} verplaatst voordat je annuleerde.`,
  'Completion.PermanentDeleteCancelledSummary': `{0} van {1} {2} definitief verwijderd voordat je annuleerde.`,
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} definitief verwijderd`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} definitief verwijderd`,
  'Completion.DonateAsk': `Graag gedaan. Er staat een fooienpot klaar, mocht je je gul voelen.`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} bestand ongemoeid gelaten`,
  'Summary.RegisteredStillUsed.Plural': `{0} bestanden ongemoeid gelaten`,
  'Summary.OrphanedToCleanUp.Singular': `{0} overbodig bestand om op te ruimen`,
  'Summary.OrphanedToCleanUp.Plural': `{0} overbodige bestanden om op te ruimen`,
  'Summary.NothingListed.Singular': `InstallerClean kon niet met zekerheid vaststellen welke bestanden in de cache bij de hier geïnstalleerde programma's horen, en heeft daarom het ene bestand achtergehouden in plaats van het aan te bieden.`,
  'Summary.NothingListed.Plural': `InstallerClean kon niet met zekerheid vaststellen welke bestanden in de cache bij de hier geïnstalleerde programma's horen, en heeft daarom {0} {1} achtergehouden in plaats van ze aan te bieden.`,
  'Summary.MissingFromDisk.Singular': `Windows heeft een vermelding van {0} bestand dat niet in {InstallerFolder} staat: {1}. In het dagelijks gebruik levert dat geen problemen op, maar bijwerken of verwijderen van dat programma kan mislukken. Open Details voor wat je kunt doen.`,
  'Summary.MissingFromDisk.Plural': `Windows heeft vermeldingen van {0} bestanden die niet in {InstallerFolder} staan: {1}. In het dagelijks gebruik leveren die geen problemen op, maar bijwerken of verwijderen van die programma's kan mislukken. Open Details voor wat je kunt doen.`,
  'Summary.MissingFromDisk.OtherPrograms.Singular': `{0} ander programma`,
  'Summary.MissingFromDisk.OtherPrograms.Plural': `{0} andere programma's`,
  'Summary.MissingFromDisk.Unnamed.Singular': `{0} bestand waarbij de records geen programma noemen`,
  'Summary.MissingFromDisk.Unnamed.Plural': `{0} bestanden waarbij de records geen programma noemen`,
  'Summary.OperationFiles': `{0} van {1} {2}`,
  'Summary.OrphanedWindow': `{0} {1} om op te ruimen ({2})`,
  'Summary.RegisteredWindow.Singular': `{0} bestand ongemoeid gelaten ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} bestanden ongemoeid gelaten ({1})`,

  // Confirmation dialogs
  'Confirm.MoveTitle': `{0} {1} verplaatsen ({2})?`,
  'Confirm.DeleteTitle': `{0} {1} verwijderen ({2})?`,
  'Confirm.MoveSameDrive': `Die map staat op dezelfde schijf, dus de ruimte komt pas vrij als je hem verwijdert. Kies een map op een andere schijf als je de ruimte meteen wilt hebben.`,

  // Error messages
  'Error.AdminRequiredTitle': `Toegang geweigerd`,
  'Error.AdminRequiredBody': `Windows heeft InstallerClean de toegang geweigerd, dus het is gestopt. Er is niets verwijderd.\n\nInstallerClean draaide al als administrator, dus opnieuw starten op die manier helpt niet. Windows zegt er niet bij wat er precies weigerde, dus er valt niets gerichts te proberen.`,
  'Error.InstallerDbUnavailableTitle': `Windows Installer-records konden niet worden gelezen`,
  'Error.ScanFailedTitle': `Scan mislukt`,
  'Error.InstallerDbEmpty': `De Windows Installer-records kwamen volledig leeg terug: geen enkel geïnstalleerd programma of geen enkele update maakt aanspraak op een installatiebestand in de cache. Dat komt op een werkende computer niet voor (zelfs een gloednieuwe Windows-installatie heeft er een paar), dus ofwel zijn de records beschadigd, ofwel konden ze niet worden gelezen, en een scan die dit antwoord zou geloven, zou elk bestand in {InstallerFolder} ten onrechte verweesd noemen. InstallerClean is in plaats daarvan gestopt. Er is niets verwijderd.`,
  'Error.MsiAccessDenied': `Windows Installer weigerde InstallerClean te laten zien wat er geïnstalleerd is. InstallerClean draaide al als administrator, dus het nogmaals als administrator uitvoeren verandert niets. Zonder die lijst is er geen veilige manier om te bepalen welke bestanden in de cache nog nodig zijn, dus InstallerClean is gestopt. Er is niets verwijderd.`,
  'Error.MsiNonSuccess': `Windows Installer kon InstallerClean geen leesbare lijst van de geïnstalleerde programma's geven: het las {2} {3} en daarna kwamen {0} vermeldingen op rij onleesbaar terug (laatste foutcode {1}). In plaats van met een half gelezen lijst te werken is InstallerClean gestopt. Er is niets verwijderd.`,
  'Error.MsiEnumerationNeverEnded': `Windows Installer heeft het einde van de lijst met geïnstalleerde programma's nooit gemeld: InstallerClean las {2} {3} en heeft het daarna na {0} vermeldingen opgegeven (laatste foutcode {1}). Een lijst zonder einde is niet te vertrouwen, dus InstallerClean is gestopt. Er is niets verwijderd.`,
  'Error.MsiPatchEnumerationNeverEnded': `Windows Installer heeft het einde van de patchlijst van een programma nooit gemeld: InstallerClean las {2} {3} en heeft het daarna na {0} vermeldingen opgegeven (laatste foutcode {1}). Een lijst zonder einde is niet te vertrouwen, dus InstallerClean is gestopt. Er is niets verwijderd.`,
  'Error.ScanCorrelationFailed': `InstallerClean kon de Windows Installer-records niet koppelen aan de inhoud van {InstallerFolder}. Bijna niets waarnaar de records verwijzen staat daar werkelijk, en bijna niets van wat daar staat wordt door een record genoemd, waardoor er niets als overbodig kon worden aangemerkt. Er is niets voor opruimen aangeboden en er is niets verwijderd.`,
  'Error.ScanRecordsUnreadable': `InstallerClean kon niet genoeg van de Windows Installer-records lezen om zeker te weten wat er nog nodig is: de lijst met geïnstalleerde programma's kwam onvolledig terug, en dezelfde records rechtstreeks uit het register lezen leverde ook fouten op. Een bestand kon er verweesd uitzien enkel doordat het record dat het benoemt een van de onleesbare was, dus InstallerClean is gestopt. Er is niets verwijderd.`,
  'Error.InvalidDestinationTitle': `Ongeldige bestemming`,
  'Error.DestinationWriteFailedTitle': `Kon niet naar de bestemming schrijven`,
  'Error.MoveFailedTitle': `Verplaatsen mislukt`,
  'Error.DeleteFailedTitle': `Verwijderen mislukt`,
  'Error.SettingNotSavedTitle': `Instelling niet opgeslagen`,
  'Error.SettingNotSavedBody': `De wijziging kon niet worden opgeslagen. InstallerClean gaat bij de volgende start terug naar de vorige instelling.`,
  'Error.DestinationInsideInstaller': `De bestemming mag niet in de Windows Installer-map liggen.`,
  'Error.DestinationInSystemFolder': `De bestemming {0} verwijst naar een locatie onder een Windows-systeemmap. Kies een pad buiten %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% en %ProgramData%.`,
  'Error.NotEnoughSpaceTitle': `Onvoldoende ruimte`,
  'Error.NotEnoughSpaceBody': `Onvoldoende ruimte op {0}\n\nVereist: {1}\nBeschikbaar: {2}`,
  'Error.AccessDeniedDestination': `Je hebt geen schrijfrechten voor {0}.\nProbeer een map in je gebruikersprofiel of op een schijf die van jou is.`,
  'Error.PathTooLong': `Het pad {0} is te lang voor Windows. Kies een korter pad.`,
  'Error.DestinationMissing': `De map {0} bestaat niet en kon niet worden aangemaakt. Controleer de stationsletter of het netwerkpad.`,
  'Error.IOWriteDestination': `Windows kan niet schrijven naar {0}.\nDetails in {1}.`,
  'Error.IOWriteDestination.NoLog': `Windows kan niet schrijven naar {0}. Het crashlog kon niet worden weggeschreven.`,
  'Error.WriteDestination': `Kan niet schrijven naar {0}.\nDetails in {1}.`,
  'Error.WriteDestination.NoLog': `Kan niet schrijven naar {0}. Het crashlog kon niet worden weggeschreven.`,
  'Error.MissingSourceFile': `Bestand bestaat niet meer.`,
  'Error.SourceIsReparsePoint': `Bronbestand is een symlink of junction; om veiligheidsredenen geweigerd.`,
  'Error.CandidateOutsideCache': `Dit bestand bevindt zich niet direct in de Windows Installer-map; om veiligheidsredenen geweigerd.`,
  'Error.AccessDenied.Singular': `Windows heeft de toegang tot dit bestand geweigerd; het is blijven staan.`,
  'Error.AccessDenied.Plural': `Windows heeft de toegang tot deze bestanden geweigerd; ze zijn blijven staan.`,
  'Error.FileInUse.Singular': `Dit bestand is geopend of vergrendeld door een ander programma, dus niets kan het nu verwijderen. Het is blijven staan; probeer het later opnieuw.`,
  'Error.FileInUse.Plural': `Deze bestanden zijn geopend of vergrendeld door een ander programma, dus niets kan ze nu verwijderen. Ze zijn blijven staan; probeer het later opnieuw.`,
  'Error.IOFailure.Singular': `Windows heeft een bestandsfout gemeld; het bestand is blijven staan.`,
  'Error.IOFailure.Plural': `Windows heeft bestandsfouten gemeld; deze bestanden zijn blijven staan.`,
  'Error.UnknownError.Singular': `Er is iets misgegaan met dit bestand; het is blijven staan.`,
  'Error.UnknownError.Plural': `Er is iets misgegaan met deze bestanden; ze zijn blijven staan.`,
  'Error.MoveIntoInstaller': `Weigert bestanden naar de Windows Installer-map te verplaatsen (bestemming: {0}).`,
  'Error.DestinationNotFullyQualified': `De back-upmap moet een volledig pad naar een map zijn, beginnend met een stationsletter of een netwerkshare (bijvoorbeeld D:\\Backup of \\\\server\\backup). InstallerClean kan deze niet gebruiken: {0}`,
  'BrowserLaunch.FailedTitle': `Kon je browser niet openen`,
  'UpdateCheck.Title': `Controleren op updates`,
  'UpdateCheck.Status.Checking': `Controleren...`,
  'UpdateCheck.Status.UpToDate': `Je bent bij.`,
  'UpdateCheck.Status.UpdateAvailable': `Versie {0} is beschikbaar.`,
  'UpdateCheck.UpdateAvailable.Title': `Update beschikbaar`,
  'UpdateCheck.UpdateAvailable.Body': `Je gebruikt versie {0}.&#10;Versie {1} is beschikbaar.`,
  'UpdateCheck.Failed.NetworkUnavailable': `Kon GitHub niet bereiken. Controleer je internetverbinding en probeer het opnieuw.`,
  'UpdateCheck.Failed.ServerError': `GitHub heeft een foutmelding teruggestuurd. Probeer het over een paar minuten opnieuw.`,
  'UpdateCheck.Failed.ResponseParseError': `In het antwoord van GitHub was geen release te herkennen. Probeer het later opnieuw, of open de releasepagina rechtstreeks.`,
  'UpdateCheck.Failed.Timeout': `De controle duurde te lang. Je verbinding met GitHub is mogelijk traag; probeer het opnieuw.`,
  'UpdateCheck.Failed.Unknown': `De controle is om een onbekende reden mislukt. De details staan in crash.log, mocht je het willen melden.`,
  'BrowserLaunch.ClipboardOk': `The link is on your clipboard, so you can paste it in yourself:&#10;&#10;{0}`,
  'BrowserLaunch.ClipboardFailed': `InstallerClean couldn't copy the link to your clipboard either, so here it is:&#10;&#10;{0}`,
  'Error.DestinationChangedMidBatch': `InstallerClean kon de back-upmap niet meer bevestigen en is gestopt in plaats van op de verkeerde plek te schrijven. Controleer {0}, klik daarna op Opnieuw scannen en probeer het opnieuw.`,
  'Error.CannotWriteFolder': `Kan niet schrijven naar {0}.`,
  'Error.DestinationCollision': `Er staat al een bestand met de naam '{0}' in de back-upmap.`,

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
  'Startup.UnhandledBody': `Er is een onverwachte fout opgetreden en InstallerClean moet worden afgesloten.\n\n{0}\n\nDetails weggeschreven naar:\n{1}`,
  'Startup.UnhandledBody.NoLog': `Er is een onverwachte fout opgetreden en InstallerClean moet worden afgesloten.\n\n{0}\n\nHet crashlog kon niet worden weggeschreven.`,
  'Startup.ErrorTitle': `Opstartfout`,
  'Startup.FailedToStart': `Starten mislukt ({0}). Details weggeschreven naar:\n{1}`,
  'Startup.FailedToStart.NoLog': `Starten mislukt ({0}). Het crashlog kon niet worden weggeschreven.`,

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
  'Display.ListSeparator': `, `,
  'Display.ElapsedLong.LessThanASecond': `minder dan een seconde`,
  'Display.ElapsedLong.Seconds': `{0:F1} seconden`,
  'CrashLog.PrivacyHeader': `# crash.log legt onafgevangen fouten van InstallerClean vast.\n# Met verhoogde rechten kunnen de foutmeldingen van het framework\n# bestandspaden uit de lopende sessie bevatten (waaronder profielen\n# van andere gebruikers die door Windows Installer-query's zijn\n# opgesomd). Meldingen over netwerkfouten bij de updatecontrole of\n# de POST van het rapport kunnen de bestemmings-URL en het\n# opgeloste IP- of proxyadres bevatten. Regels over onleesbare\n# Windows Installer-records kunnen een Windows-account-SID\n# (S-1-5-21-...) en de productcodes van geïnstalleerde software\n# bevatten.\n# Verwijder alle drie de soorten gegevens voordat je dit bestand\n# aan een openbaar bugrapport toevoegt.\n`,
  'Tooltip.ChangeLanguage': `Taal wijzigen. Het programma start opnieuw.`,
  'Automation.ChangeLanguage': `Taal wijzigen`,
  'Automation.ChangeLanguage.HelpText': `Het programma start opnieuw.`,

  // Command-line tool (installerclean-cli): the HUMAN-facing Cli.* keys. The
  // machine-contract Cli.EventLog* keys (bar Cli.EventLogUnavailable) are NOT
  // here and are stripped from the output; they stay English at runtime. In the
  // help lines translate the DESCRIPTION only: keep the command tokens, flags,
  // the {InstallerFolder} token and the exit-code numbers verbatim, keep the
  // leading spaces and column alignment (the screen is column-aligned for a
  // monospace terminal; PAD is one character shorter than PATH, so its line
  // carries one more space), and translate the PATH metavariable to PAD (it
  // names the argument, as es/pt-BR/it did).
  'Cli.UnknownArgument': `Fout: onbekend argument '{0}'`,
  'Cli.TooManyArguments': `Fout: onverwacht extra argument '{0}'. Als de verplaatsingsmap een spatie bevat, zet dan het volledige pad tussen aanhalingstekens: /m "D:\\My Backup"`,
  'Cli.Cancelling': `Annuleren...`,
  'Cli.Cancelled': `Geannuleerd.`,
  'Cli.GenericError': `Fout: onverwachte crash ({0}). Details weggeschreven naar {1}.`,
  'Cli.GenericError.NoLog': `Fout: onverwachte crash ({0}). Het crashlog kon niet worden weggeschreven.`,
  'Cli.ScanningInstaller': `{InstallerFolder} scannen...`,
  'Cli.FoundOrphans': `{0} overbodige {1} gevonden om op te ruimen ({2}).`,
  'Cli.DeletingFiles': `{0} overbodige {1} verwijderen...`,
  'Cli.DeletedFiles': `{0} overbodige {1} definitief verwijderd.`,
  'Cli.NoMoveDestination': `Fout: er is geen bestemming voor het verplaatsen opgegeven. Gebruik /m PAD. (Een standaard die in de GUI is ingesteld, geldt per gebruiker en niet voor geplande taken of serviceaccounts.)`,
  'Cli.MoveDestinationInsideInstaller': `Fout: de bestemming mag niet in de Windows Installer-map liggen.`,
  'Cli.MoveDestinationRelative': `Fout: de bestemming moet een volledig gekwalificeerd pad zijn. Ontvangen: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Fout: de bestemming {0} komt uit onder een Windows-systeemmap. Kies een pad buiten %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% en %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Fout: er is op dit moment iets bezig met Windows Installer, zoals een Windows-update of een programma dat op de achtergrond installeert. /m en /d zijn geblokkeerd zolang dat loopt. Probeer het opnieuw zodra het klaar is.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Fout: een eerdere Windows Installer-transactie is op deze computer opgeschort. Hervat die installatie of draai haar terug (of herstart Windows) voordat je {InstallerFolder} opruimt.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Fout: een bestandsbewerking die na de herstart in de wachtrij staat, richt zich op {InstallerFolder} ({0}). Herstart Windows om die bewerking te voltooien voordat je opruimt.`,
  'Cli.MovingFiles': `{0} overbodige {1} verplaatsen naar {2}...`,
  'Cli.MovedFiles': `{0} overbodige {1} verplaatst.`,
  'Cli.MutexBlocked': `Een ander InstallerClean-proces houdt de single-instance-vergrendeling vast (de GUI of een andere CLI-uitvoering). Exit 75 (tijdelijk); je kunt het later veilig opnieuw proberen.`,
  'Cli.EventLogUnavailable': `Opmerking: het schrijven naar het gebeurtenislogboek is mislukt. Controleer de machtigingen voor het logboek Toepassing of het groepsbeleid.`,
  'Cli.Help.Header': `InstallerClean - {InstallerFolder} opschonen`,
  'Cli.Help.Usage': `Gebruik:`,
  'Cli.Help.Help': `  installerclean-cli --help     Deze helptekst weergeven (ook /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  De versie weergeven (ook -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Alleen scannen: overbodige bestanden tonen`,
  'Cli.Help.Delete': `  installerclean-cli /d         Definitief verwijderen wat overbodig is`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Verplaatsen naar de opgeslagen back-upmap`,
  'Cli.Help.MovePath': `  installerclean-cli /m PAD     Verplaatsen naar het opgegeven pad`,
  'Cli.Help.NoteLine1': `installerclean-cli blokkeert de prompt tot het klaar is, zodat een script&#10;of geplande taak erop kan wachten.`,
  'Cli.Help.MoveScheduledNote': `Die map geldt per gebruiker; geplande of SYSTEM-taken vereisen /m PAD.`,
  'Cli.Help.ExitCodesHeader': `Afsluitcodes:`,
  'Cli.Help.ExitCodeOk': `  0   geslaagd: de uitvoering deed wat gevraagd was, zonder fouten`,
  'Cli.Help.ExitCodeError': `  1   mislukt: niets verwerkt (verkeerde argumenten, een verkeerde&#10;       bestemming, een mislukte scan of elk bestand mislukt)`,
  'Cli.Help.ExitCodePartial': `  2   gedeeltelijk: een deel verwerkt, een deel niet (een fout of Ctrl+C)`,
  'Cli.Help.ExitCodeTransient': `  75  tijdelijk: iets tijdelijks blokkeerde de uitvoering (zie de melding)`,
  'Cli.Help.ExitCodeCancelled': `  130 geannuleerd (Ctrl+C)`,
  'Tooltip.MoveSameDrive': `De overbodige bestanden naar de back-upmap verplaatsen. Die staat op dezelfde schijf, dus de ruimte komt pas vrij als je die map verwijdert.`,
  'Confirm.DeletePermanently.Singular': `Dit bestand wordt definitief verwijderd. Dat kan veilig, maar wil je een back-up, gebruik dan Verplaatsen.`,
  'Confirm.DeletePermanently.Plural': `Deze bestanden worden definitief verwijderd. Dat kan veilig, maar wil je een back-up, gebruik dan Verplaatsen.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean kreeg Windows niet zover het echte pad van {InstallerFolder} te bepalen, dus van geen enkel bestand kon worden aangetoond dat het erin staat en er is niets voor opruimen aangeboden. Deze scan vond niets omdat die controle mislukte, niet omdat de map schoon is. Er is niets verwijderd.`,
  'Automation.Scroll.ProductDetails': `Productgegevens`,
  'Body.PendingReboot.Other': `Windows Installer heeft iets lopen, dus Verplaatsen en Verwijderen staan stil. InstallerClean raakt {InstallerFolder} niet aan terwijl daar iets verandert. Zodra het klaar is, klik je op Opnieuw scannen en komen ze terug.`,
  'Cli.TooManyArgumentsNoPath': `Fout: onverwacht extra argument '{0}'. /s en /d nemen geen verdere argumenten, en er kan maar één vlag per uitvoering worden gebruikt.`,
  'Cli.MissingFromDisk.Singular': `Windows heeft een vermelding van {0} bestand dat niet in {InstallerFolder} staat: {1}. In het dagelijks gebruik levert dat geen problemen op, maar bijwerken of verwijderen van dat programma kan mislukken. Om het bestand terug te zetten heb je het installatieprogramma nodig van de versie die je al hebt. Haal het bij de maker van het programma en voer het uit over je bestaande installatie heen. Een nieuwere versie volstaat niet: die moet eerst verwijderen wat je hebt, en juist die stap heeft dit bestand nodig. Eerst verwijderen werkt om dezelfde reden ook niet. Dit zou het bestand moeten herstellen en je instellingen ongemoeid moeten laten, maar Microsoft geeft daar geen garantie op.`,
  'Cli.MissingFromDisk.Plural': `Windows heeft vermeldingen van {0} bestanden die niet in {InstallerFolder} staan: {1}. In het dagelijks gebruik leveren die geen problemen op, maar bijwerken of verwijderen van die programma's kan mislukken. Om een bestand terug te zetten heb je het installatieprogramma nodig van de versie die je van dat programma al hebt. Haal het bij de maker van het programma en voer het uit over je bestaande installatie heen. Een nieuwere versie volstaat niet: die moet eerst verwijderen wat je hebt, en juist die stap heeft het bestand nodig. Eerst verwijderen werkt om dezelfde reden ook niet. Dit zou het bestand moeten herstellen en je instellingen ongemoeid moeten laten, maar Microsoft geeft daar geen garantie op.`,
  'Cli.MoveNotEnoughSpace': `Fout: onvoldoende ruimte op {0}. Deze bestanden verplaatsen vraagt {1} en er is {2} vrij. Er is niets verplaatst.`,
  'Cli.PendingRebootBlocked.Other': `Fout: Windows Installer heeft iets lopen, dus /m en /d zijn geblokkeerd. InstallerClean raakt {InstallerFolder} niet aan terwijl daar iets verandert. Probeer het opnieuw zodra het klaar is.`,
  'Cli.FoundNoOrphans': `Geen overbodige bestanden gevonden.`,
  'Cli.NothingOffered.Singular': `InstallerClean kon niet met zekerheid vaststellen welke bestanden in de cache bij de hier geïnstalleerde programma's horen, en heeft daarom het ene bestand ({2}) achtergehouden in plaats van het aan te bieden.`,
  'Cli.NothingOffered.Plural': `InstallerClean kon niet met zekerheid vaststellen welke bestanden in de cache bij de hier geïnstalleerde programma's horen, en heeft daarom alle {0} {1} ({2}) achtergehouden in plaats van ze aan te bieden.`,
  'Cli.DestinationChangedMidBatch': `InstallerClean kon de back-upmap niet meer bevestigen en is gestopt in plaats van op de verkeerde plek te schrijven. Controleer {0} en voer de opdracht opnieuw uit.`,
  'Cli.Help.Summary': `Verwijdert .msi/.msp die geen geïnstalleerd programma meer nodig heeft.`,
  'Cli.Help.Elevation': `Vereist een prompt als administrator; Windows start het anders niet.`,
  'Error.InstallerLockUnavailableTitle': `Er is niets verwijderd`,
  'Error.MoveInstallerLockUnavailableTitle': `Er is niets verplaatst`,
  'Error.InstallerLockUnavailable': `InstallerClean kon de vergrendeling niet krijgen waarmee Windows Installer voorkomt dat twee programma's tegelijk geïnstalleerde software wijzigen, en kon dus niet uitsluiten dat een bestand halverwege alsnog nodig werd, dus er is niets verwijderd. Probeer het opnieuw, en herstart Windows als het zich blijft voordoen.`,
  'Error.MoveInstallerLockUnavailable': `InstallerClean kon de vergrendeling niet krijgen waarmee Windows Installer voorkomt dat twee programma's tegelijk geïnstalleerde software wijzigen, en kon dus niet uitsluiten dat een bestand halverwege alsnog nodig werd, dus er is niets verplaatst. Probeer het opnieuw, en herstart Windows als het zich blijft voordoen.`,
  'Cli.InstallerLockUnavailable': `Fout: InstallerClean kon de Windows Installer-vergrendeling niet krijgen die voorkomt dat twee programma's tegelijk geïnstalleerde software wijzigen, en kon dus niet uitsluiten dat een bestand halverwege alsnog nodig werd. Er is niets verwijderd. Probeer het opnieuw, en herstart Windows als het zich blijft voordoen.`,
  'Cli.MoveInstallerLockUnavailable': `Fout: InstallerClean kon de Windows Installer-vergrendeling niet krijgen die voorkomt dat twee programma's tegelijk geïnstalleerde software wijzigen, en kon dus niet uitsluiten dat een bestand halverwege alsnog nodig werd. Er is niets verplaatst. Probeer het opnieuw, en herstart Windows als het zich blijft voordoen.`,
  'Completion.ReverifyIdentityClaimed': `{0} {1} behouden, omdat Windows een registratie heeft van het programma dat erin genoemd wordt.`,
  'Completion.ReverifyIdentityUnreadable': `{0} {1} behouden, omdat InstallerClean geen programma kon vinden dat erin genoemd wordt.`,
  'Error.ScanNoRegisteredFileInFolder': `InstallerClean kon de Windows Installer-records niet koppelen aan de inhoud van {InstallerFolder}. De map bevat wel bestanden, maar geen enkel record verwijst naar iets daarin, waardoor er niets als overbodig kon worden aangemerkt. Er is niets voor opruimen aangeboden en er is niets verwijderd.`,
  'Completion.NothingOffered': `Niets aangeboden op deze pc`,
  'Completion.NothingOfferedBody.Singular': `InstallerClean kon niet met zekerheid vaststellen welke bestanden in de cache bij de hier geïnstalleerde programma's horen, en heeft daarom het ene bestand ({2}) achtergehouden in plaats van het aan te bieden.`,
  'Completion.NothingOfferedBody.Plural': `InstallerClean kon niet met zekerheid vaststellen welke bestanden in de cache bij de hier geïnstalleerde programma's horen, en heeft daarom alle {0} {1} ({2}) achtergehouden in plaats van ze aan te bieden.`,
  'Summary.SupersededHeldBack.Singular': `InstallerClean kon niet met zekerheid vaststellen dat het ene vervangen bestand niet meer nodig is, en heeft het daarom achtergehouden.`,
  'Summary.SupersededHeldBack.Plural': `InstallerClean kon niet met zekerheid vaststellen dat {0} vervangen bestanden niet meer nodig zijn, en heeft ze daarom achtergehouden.`,
  'Cli.SupersededHeldBack.Singular': `InstallerClean kon niet met zekerheid vaststellen dat het ene vervangen bestand niet meer nodig is, en heeft het daarom achtergehouden.`,
  'Cli.SupersededHeldBack.Plural': `InstallerClean kon niet met zekerheid vaststellen dat {0} vervangen bestanden niet meer nodig zijn, en heeft ze daarom achtergehouden.`,
  'Completion.HeldBack.Singular': `{0} bestand achtergehouden. De scan noemde het overbodig. De laatste controle kon dat niet bevestigen.`,
  'Completion.HeldBack.Plural': `{0} bestanden achtergehouden. De scan noemde ze overbodig. De laatste controle kon dat niet bevestigen.`,
  'Body.PendingReboot.PendingRenameUnresolved': `Er staat een bestandsbewerking in de wachtrij voor de volgende herstart en InstallerClean kan niet zien welke bestanden die noemt, dus kan het niet uitsluiten dat ze in {InstallerFolder} staan. Start Windows opnieuw op voordat je opruimt.`,
  'Completion.MoveRestoreHint': `Verwijder die map zodra je zeker weet dat alles goed is.`,
  'Completion.MoveRestoreHintSameDrive': `Verwijder die map zodra je zeker weet dat alles goed is. Pas dan komt de ruimte echt vrij.`,
  'Confirm.MoveDestination.Singular': `Dit bestand gaat naar:`,
  'Confirm.MoveDestination.Plural': `Deze bestanden gaan naar:`,
  'Cli.NothingListed.Singular': `InstallerClean kon niet met zekerheid vaststellen welke bestanden in de cache bij de hier geïnstalleerde programma's horen, en heeft daarom het ene bestand ({2}) achtergehouden in plaats van het aan te bieden.`,
  'Cli.NothingListed.Plural': `InstallerClean kon niet met zekerheid vaststellen welke bestanden in de cache bij de hier geïnstalleerde programma's horen, en heeft daarom {0} {1} ({2}) achtergehouden in plaats van ze aan te bieden.`,
  'Cli.WithheldReasons.Header': `Waarom die zekerheid er niet was:`,
  'Cli.WithheldReasons.RecordedPath': `  Een bestandspad uit de eigen administratie van Windows Installer was niet te herleiden, en er kon daarom niets aan worden gekoppeld.`,
  'Cli.WithheldReasons.FileIdentity': `  Een bestand waarvan Windows een administratie heeft, was niet te identificeren, en kon daarom niet worden vergeleken met wat er in de map staat.`,
  'Cli.WithheldReasons.SecondInstance': `  Een programma is mogelijk meer dan één keer op deze pc geïnstalleerd, en de administratie kan niet zeggen bij welke kopie een bestand hoort.`,
  'Cli.PendingRebootBlocked.PendingRenameUnresolved': `Fout: er staat een bestandsbewerking in de wachtrij voor de volgende herstart en InstallerClean kan niet zien welke bestanden die noemt, dus kan het {InstallerFolder} niet uitsluiten. Start Windows opnieuw op voordat je opruimt.`,
  'Cli.MoveRestoreHint': `Controleer of je programma's nog gewoon bijwerken en verwijderen, en verwijder daarna {0}.`,
  'Error.ScanStoppedDetails': `Dit wordt ook vastgelegd in {0}.`,
  'Cli.NothingListedPerFile.Singular': `InstallerClean was niet zeker over een van de gevonden bestanden in de cache, en heeft dat ene ({2}) daarom achtergehouden in plaats van het aan te bieden.`,
  'Cli.NothingListedPerFile.Plural': `InstallerClean was niet zeker over sommige van de gevonden bestanden in de cache, en heeft daarom {0} {1} ({2}) achtergehouden in plaats van ze aan te bieden.`,
  'Cli.NothingOfferedPerFile.Singular': `InstallerClean kon niet vaststellen dat het gevonden bestand in de cache overbodig is, en heeft daarom dat ene bestand ({2}) achtergehouden in plaats van het aan te bieden.`,
  'Cli.NothingOfferedPerFile.Plural': `InstallerClean kon van geen van de gevonden bestanden in de cache vaststellen dat ze overbodig zijn, en heeft daarom alle {0} {1} ({2}) achtergehouden in plaats van ze aan te bieden.`,
  'Completion.NothingOfferedPerFileBody.Singular': `InstallerClean kon niet vaststellen dat het gevonden bestand in de cache overbodig is, en heeft daarom dat ene bestand ({2}) achtergehouden in plaats van het aan te bieden.`,
  'Completion.NothingOfferedPerFileBody.Plural': `InstallerClean kon van geen van de gevonden bestanden in de cache vaststellen dat ze overbodig zijn, en heeft daarom alle {0} {1} ({2}) achtergehouden in plaats van ze aan te bieden.`,
  'Summary.NothingListedPerFile.Singular': `InstallerClean was niet zeker over een van de gevonden bestanden in de cache, en heeft het daarom achtergehouden in plaats van het aan te bieden.`,
  'Summary.NothingListedPerFile.Plural': `InstallerClean was niet zeker over sommige van de gevonden bestanden in de cache, en heeft daarom {0} {1} achtergehouden in plaats van ze aan te bieden.`,
  'Cli.WithheldReasons.CandidateIdentity': `  Een bestand in de map was niet te identificeren, en kon daarom niet met de administratie worden vergeleken.`,
  'Cli.WithheldReasons.DeclaredProductInstalled': `  Een bestand zegt bij een programma te horen dat nog geïnstalleerd is, en is daarom mogelijk nog nodig.`,
  'Cli.WithheldReasons.DeclaredProductUnestablished': `  Ofwel gaf een bestand niet aan bij welk programma het hoort, ofwel gaf Windows geen antwoord over dat programma.`,
  'Cli.WithheldReasons.ScreenUnanswered': `  Een controle op bij welke programma's de bestanden horen, gaf antwoorden die niet overeenkwamen met de bestanden die eraan waren doorgegeven.`,
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

// Remove ONLY the machine-contract Cli.* <data> elements BY NAME (the
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
// Required = everything a satellite must carry: the non-Cli keys plus the
// human-facing Cli keys. The machine Cli keys are the complement; they must be
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

// The one human-facing Cli.EventLog* key, asserted present rather than left to
// the counts: a predicate that stopped discriminating it takes it out of the
// output AND out of the required set, so every figure above still agrees. The
// MAP substitution notices today only through the order the two run in.
const humanCliStripped = !output.has('Cli.EventLogUnavailable');
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
console.log('machine Cli <data> removed:', cliMachineRemoved, `(expect ${cliMachineExpected})`);
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
if (humanCliStripped) console.log('!! Cli.EventLogUnavailable stripped: that key is human-facing and must stay');
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
  !humanCliStripped &&
  !overrideMissing.length && !overrideArityMismatch.length &&
  output.size === neutralRequired.length + overrideKeys.length && cliMachineRemoved === cliMachineExpected && crlf === 0;
const ok = structuralOk && !untranslated.length;
console.log(ok ? '\nGENERATION OK' : '\nGENERATION HAS ISSUES (see above)');
