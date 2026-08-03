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
// many), and the progress lines use the verbal-noun "Bezig met ..."
// construction, which carries no number at all, so the progress overrides
// German needs for wird/werden have no Dutch counterpart. Dutch DOES
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
  'Section.Backup.Folder': `BACKUP FOLDER`,
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
  'Action.BackupFolderPlaceholder': `Path to folder if you move rather than delete.`,
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
  'Automation.ConfirmDelete': `Delete permanently removes the unneeded files. Cancel closes without deleting.`,
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
  'Automation.Section.BackupFolder': `Backup folder`,
  'Automation.Section.Products': `Producten`,
  'Automation.Section.Patches': `Patches`,
  'Automation.Section.ProductDetails': `Productgegevens`,
  'Automation.BackupFolder': `Backup folder`,
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
  'Tooltip.Move': `Move the unneeded files to the backup folder. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.MoveNeedsDestination': `Move the unneeded files to a backup folder. You'll choose it next. Delete that folder whenever you're satisfied nothing needs them.`,
  'Tooltip.Delete': `Delete the unneeded files permanently. They're safe to remove, and you'll reclaim the space straight away.`,
  'Tooltip.SigningCertificate': `Naam van het onderwerp uit het ingesloten Authenticode-certificaat. Niet gecontroleerd via de certificaatketen.`,

  // Body copy
  'Body.MainExplanation.Lead': `Any unneeded files below are [safe to delete].`,
  'Body.MainExplanation.Why': `Ze staan in {InstallerFolder}, achtergebleven toen een programma werd verwijderd ({0}), een nieuwere patch een oudere verving ({1}) of de uitgever hem introk ({2}). InstallerClean toont alleen bestanden waarvan Windows zelf aangeeft dat het ermee klaar is.`,
  'Body.MainExplanation.Action': `Delete them permanently, or move them to a backup folder until you're satisfied nothing needs them. Put them back into {InstallerFolder} and everything is restored.`,
  'Body.NotScanned.Lead': `Nog niets gescand.`,
  'Body.NotScanned.Why': `Klik op Opnieuw scannen om {InstallerFolder} te doorzoeken op installatiebestanden die geen enkel programma nog nodig heeft.`,
  'Body.PendingReboot.Lead': `Deze bestanden kunnen nu niet worden opgeruimd.`,
  'Body.PendingReboot.MsiExecuteMutex': `Something is using Windows Installer right now, usually a Windows Update or a program installing in the background. Move and Delete are paused while that runs, so InstallerClean won't touch {InstallerFolder} while it's changing. Once it's done, Re-scan and they come back.`,
  'Body.PendingReboot.InstallerInProgress': `A previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Body.PendingReboot.PendingRenameInCache': `Windows has a file rename queued for the next restart that affects {InstallerFolder}. Restart Windows before cleaning.`,
  'Body.NoFileSelected': `Selecteer een bestand om de details te zien.`,
  'Body.NoProductSelected': `Selecteer een product om de details te zien.`,
  'Body.NoMetadata': `Geen metadata beschikbaar.`,
  'Body.RegisteredMissingFromDisk': `This installer file is missing. InstallerClean didn't remove it, it never removes a file a program still needs; it was already gone before you ran InstallerClean.&#10;&#10;It causes no trouble now, and won't until the day you try to repair, update or uninstall the program it belongs to. That step can then fail, because Windows looks for this file and it isn't there.&#10;&#10;To try and fix it, download that program's installer from its maker and run it over your existing copy (don't uninstall first, uninstalling is itself a step that needs this file). Use the version you have installed if you can get it, as Windows may reject a different one. This usually restores the file, and your settings are normally untouched, but Microsoft doesn't guarantee it, its own last resort is reinstalling the program, or Windows itself.`,
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
  'Status.Moving': `Moving unneeded files...`,
  'Status.Deleting': `Deleting unneeded files...`,
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
  'Completion.PermanentDeleteSummary.Singular': `{0} {1} permanently deleted`,
  'Completion.PermanentDeleteSummary.Plural': `{0} {1} permanently deleted`,
  'Completion.DonateAsk': `Graag gedaan. Er staat een fooienpot klaar, mocht je je gul voelen.`,

  // Summaries
  'Summary.RegisteredStillUsed.Singular': `{0} bestand nog nodig`,
  'Summary.RegisteredStillUsed.Plural': `{0} bestanden nog nodig`,
  'Summary.OrphanedToCleanUp.Singular': `{0} overbodig bestand om op te ruimen`,
  'Summary.OrphanedToCleanUp.Plural': `{0} overbodige bestanden om op te ruimen`,
  'Summary.MissingFromDisk.Singular': `{0} geregistreerd bestand ontbreekt (niet door InstallerClean verwijderd). Nu geen probleem, maar een toekomstige herstel-, update- of verwijderactie van dat programma kan mislukken. Open Details voor wat je kunt doen.`,
  'Summary.MissingFromDisk.Plural': `{0} geregistreerde bestanden ontbreken (niet door InstallerClean verwijderd). Nu geen probleem, maar een toekomstige herstel-, update- of verwijderactie van die programma's kan mislukken. Open Details voor wat je kunt doen.`,
  'Summary.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Summary.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. Everything listed is still safe to remove, but there may be more that aren't shown. Re-scan to try again.`,
  'Summary.OperationFiles': `{0} van {1} {2}`,
  'Summary.OrphanedWindow': `{0} verweesd, {1} vervangen, {2} verouderd ({3})`,
  'Summary.RegisteredWindow.Singular': `{0} geregistreerd bestand dat nog nodig is ({1})`,
  'Summary.RegisteredWindow.Plural': `{0} geregistreerde bestanden die nog nodig zijn ({1})`,

  // Confirmation dialogs
  'Confirm.MoveTitle': `{0} {1} verplaatsen ({2})?`,
  'Confirm.MoveDestination': `De bestanden gaan naar:`,
  'Confirm.DeleteTitle': `{0} {1} verwijderen ({2})?`,
  'Confirm.MoveSameDrive': `That folder is on the same drive, so the space won't come back until you delete it. Pick a folder on another drive instead if you want the space straight away.`,

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
  'Error.ScanCorrelationFailed': `InstallerClean couldn't square this scan with the Windows Installer records: nearly every file Windows still lists as needed is missing from {InstallerFolder}, while what is actually in the folder matches almost nothing in the records. No real machine looks like that, so it points to a problem reading the records, not to files you can safely remove. Nothing has been offered for cleanup and nothing has been removed.`,
  'Error.ScanRecordsUnreadable': `InstallerClean kon niet genoeg van de Windows Installer-records lezen om zeker te weten wat er nog nodig is: de lijst met geïnstalleerde programma's kwam te kort terug, en dezelfde records rechtstreeks uit het register lezen leverde ook fouten op. Een bestand kon er verweesd uitzien enkel doordat het record dat het benoemt een van de onleesbare was, dus InstallerClean is gestopt. Er is niets verwijderd.`,
  'Error.InvalidDestinationTitle': `Ongeldige bestemming`,
  'Error.DestinationWriteFailedTitle': `Kon niet naar de bestemming schrijven`,
  'Error.MoveFailedTitle': `Verplaatsen mislukt`,
  'Error.DeleteFailedTitle': `Verwijderen mislukt`,
  'Error.SettingNotSavedTitle': `Instelling niet opgeslagen`,
  'Error.SettingNotSavedBody': `De wijziging kon niet worden opgeslagen. InstallerClean gaat bij de volgende start terug naar de vorige instelling.`,
  'Error.DestinationInsideInstaller': `De bestemming mag niet in de Windows Installer-map liggen.`,
  'Error.DestinationInSystemFolder': `The destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
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
  'Error.FileInUse.Singular': `This file is open or locked by another program, so nothing can remove it just now. It was left in place; try again later.`,
  'Error.FileInUse.Plural': `These files are open or locked by another program, so nothing can remove them just now. They were left in place; try again later.`,
  'Error.IOFailure.Singular': `Windows meldde een bestandsfout; het bestand is blijven staan.`,
  'Error.IOFailure.Plural': `Windows meldde bestandsfouten; deze bestanden zijn blijven staan.`,
  'Error.UnknownError.Singular': `Er ging iets mis met dit bestand; het is blijven staan.`,
  'Error.UnknownError.Plural': `Er ging iets mis met deze bestanden; ze zijn blijven staan.`,
  'Error.MoveIntoInstaller': `Weigert bestanden naar de Windows Installer-map te verplaatsen (bestemming: {0}).`,
  'Error.DestinationNotFullyQualified': `The backup folder needs to be a full path to a folder, starting with a drive letter or a network share (for example D:\\Backup, or \\\\server\\backup). InstallerClean can't use this one: {0}`,
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
  'Error.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then Re-scan and try again.`,
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
  'CrashLog.PrivacyHeader': `# crash.log captures unhandled exceptions from InstallerClean.\n# Under elevation the framework's exception messages can include\n# file paths from the running session (including other users'\n# profiles enumerated by Windows Installer queries). Network-\n# failure messages from the update check or result-log POST can\n# include the destination URL and the resolved IP / proxy address.\n# Entries about unreadable Windows Installer records can include a\n# Windows account SID (S-1-5-21-...) and the product codes of\n# installed software.\n# Redact all three classes of detail before attaching this file to\n# a public bug report.\n`,
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
  'Cli.UnknownArgument': `Error: unknown argument '{0}'`,
  'Cli.TooManyArguments': `Fout: onverwacht extra argument '{0}'. Staat er een spatie in je verplaatsingsmap, zet dan aanhalingstekens om het hele pad: /m "D:\\My Backup"`,
  'Cli.Cancelling': `Annuleren...`,
  'Cli.Cancelled': `Geannuleerd.`,
  'Cli.GenericError': `Error: unexpected failure ({0}). Details written to {1}.`,
  'Cli.GenericError.NoLog': `Error: unexpected failure ({0}). The crash log could not be written.`,
  'Cli.ScanningInstaller': `{InstallerFolder} scannen...`,
  'Cli.FoundOrphans': `Found {0} unneeded {1} to clean up ({2}).`,
  'Cli.DeletingFiles': `Deleting {0} unneeded {1}...`,
  'Cli.DeletedFiles': `Permanently deleted {0} unneeded {1}.`,
  'Cli.NoMoveDestination': `Fout: geen verplaatsbestemming opgegeven. Gebruik /m PAD. (Een standaard die in de GUI is ingesteld, geldt per gebruiker en niet voor geplande taken of serviceaccounts.)`,
  'Cli.MoveDestinationInsideInstaller': `Fout: de bestemming mag niet in de Windows Installer-map liggen.`,
  'Cli.MoveDestinationRelative': `Fout: de bestemming moet een volledig gekwalificeerd pad zijn. Gekregen: {0}`,
  'Cli.MoveDestinationInSystemFolder': `Error: destination {0} resolves under a Windows system folder. Pick a path outside %SystemRoot%, %ProgramFiles%, %ProgramFiles(x86)% and %ProgramData%.`,
  'Cli.PendingRebootBlocked.MsiExecuteMutex': `Error: something is using Windows Installer right now, usually a Windows Update or a program installing in the background. /m and /d are blocked while that runs. Try again once it finishes.`,
  'Cli.PendingRebootBlocked.InstallerInProgress': `Error: a previous Windows Installer transaction is suspended on this machine. Resume or roll back that install (or restart Windows) before cleaning {InstallerFolder}.`,
  'Cli.PendingRebootBlocked.PendingRenameInCache': `Error: a queued post-reboot file operation targets {InstallerFolder} ({0}). Restart Windows to complete that operation before cleaning.`,
  'Cli.MovingFiles': `Moving {0} unneeded {1} to {2}...`,
  'Cli.MovedFiles': `Moved {0} unneeded {1}.`,
  'Cli.MutexBlocked': `Een ander InstallerClean-proces houdt de single-instance-vergrendeling vast (de GUI of een andere CLI-run). Exit 75 (tijdelijk); later opnieuw proberen kan veilig.`,
  'Cli.EventLogUnavailable': `Let op: schrijven naar het gebeurtenislogboek is mislukt. Controleer de rechten op het logboek Toepassing of het groepsbeleid.`,
  'Cli.Help.Header': `InstallerClean - {InstallerFolder} opschonen`,
  'Cli.Help.Usage': `Gebruik:`,
  'Cli.Help.Help': `  installerclean-cli --help     Deze hulp tonen (ook /?, -h)`,
  'Cli.Help.Version': `  installerclean-cli --version  De versie tonen (ook -v)`,
  'Cli.Help.ScanOnly': `  installerclean-cli /s         Scan only - list unneeded files`,
  'Cli.Help.Delete': `  installerclean-cli /d         Delete unneeded files permanently`,
  'Cli.Help.MoveDefault': `  installerclean-cli /m         Move to the saved backup folder`,
  'Cli.Help.MovePath': `  installerclean-cli /m PAD     Naar het opgegeven pad`,
  'Cli.Help.NoteLine1': `installerclean-cli blocks the prompt until it finishes, so a script or&#10;scheduled task can wait on it.`,
  'Cli.Help.MoveScheduledNote': `That folder is saved per-user; scheduled or SYSTEM runs need /m PATH.`,
  'Cli.Help.ExitCodesHeader': `Afsluitcodes:`,
  'Cli.Help.ExitCodeOk': `  0   success: the run finished with nothing left to do`,
  'Cli.Help.ExitCodeError': `  1   failure: nothing processed (bad arguments, a bad destination, a&#10;       failed scan or every file failed)`,
  'Cli.Help.ExitCodePartial': `  2   partial: some processed, some not (a failure or a Ctrl+C part way)`,
  'Cli.Help.ExitCodeTransient': `  75  tijdelijk: iets blokkeerde de run (zie de melding)`,
  'Cli.Help.ExitCodeCancelled': `  130 geannuleerd (Ctrl+C)`,
  'Tooltip.MoveSameDrive': `Move the unneeded files to the backup folder. It's on the same drive, so you won't reclaim the space until you delete that folder or move it to another drive. You can do that whenever you're satisfied nothing needs them.`,
  'Completion.MoveRestoreHint.Singular': `The file in that folder is [safe to remove], so delete the folder whenever you want. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHint.Plural': `The files in that folder are [safe to remove], so delete it whenever you want. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Singular': `The file in that folder is [safe to remove], so delete the folder or move it to another drive whenever you want to actually reclaim the space. Until then, you can put it back into {InstallerFolder} if a program ever turns out to need it (extremely unlikely).`,
  'Completion.MoveRestoreHintSameDrive.Plural': `The files in that folder are [safe to remove], so delete it or move it to another drive whenever you want to actually reclaim the space. Until then, you can put them back into {InstallerFolder} if a program ever turns out to need one (extremely unlikely).`,
  'Confirm.DeletePermanently.Singular': `This file will be deleted permanently. It's [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Confirm.DeletePermanently.Plural': `Files will be deleted permanently. They're [safe to delete], but if you'd like a backup, use the Move button instead.`,
  'Error.ScanCacheRootUnresolved': `InstallerClean couldn't get Windows to resolve the true path of {InstallerFolder}, so no file could be shown to be inside it and none was offered for cleanup. This scan found nothing because that check failed, not because the folder is clean. Nothing has been removed.`,
  'Automation.Scroll.ProductDetails': `Product details`,
  'Body.PendingReboot.Other': `Windows Installer has something in progress, so Move and Delete are paused. InstallerClean won't touch {InstallerFolder} while it's changing. Once it's finished, Re-scan and they come back.`,
  'Cli.TooManyArgumentsNoPath': `Error: unexpected extra argument '{0}'. /s and /d take no further arguments, and only one flag can be used per run.`,
  'Cli.MissingFromDisk.Singular': `{0} registered file is missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of that program could fail. Running that program's installer again, the same version, usually restores it.`,
  'Cli.MissingFromDisk.Plural': `{0} registered files are missing from {InstallerFolder} (not removed by InstallerClean). No trouble now, but a future repair, update or uninstall of those programs could fail. Running each program's installer again, the same version, usually restores them.`,
  'Cli.MoveNotEnoughSpace': `Error: not enough space at {0}. Moving these files needs {1} and {2} is free. Nothing has been moved.`,
  'Cli.PendingRebootBlocked.Other': `Error: Windows Installer has something in progress, so /m and /d are blocked. InstallerClean won't touch {InstallerFolder} while it's changing. Try again once it finishes.`,
  'Cli.FoundNoOrphans': `Found no unneeded files.`,
  'Cli.DestinationChangedMidBatch': `The backup folder changed while the files were being moved (something replaced or redirected the folder), so InstallerClean stopped rather than write into the wrong place. Check {0}, then run the command again.`,
  'Cli.ProgramsUnreadable.Singular': `Windows couldn't fully read the records for one installed program, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Cli.ProgramsUnreadable.Plural': `Windows couldn't fully read the records for {0} installed programs, so this scan left out the superseded and obsoleted patches. What it did find is still safe to remove, but there may be more that aren't shown. Running it again may pick them up.`,
  'Cli.Help.Summary': `Removes cached .msi and .msp files that no installed program still needs.`,
  'Cli.Help.Elevation': `Needs an elevated (administrator) prompt; Windows will not start it.`,
  'Error.InstallerLockUnavailableTitle': `Er is niets verwijderd`,
  'Error.InstallerLockUnavailable': `InstallerClean couldn't take the lock Windows Installer uses to stop two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through, and nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
  'Cli.InstallerLockUnavailable': `Error: InstallerClean couldn't take the Windows Installer lock that stops two programs changing installed software at once, so it couldn't rule out a file becoming needed part-way through. Nothing has been deleted. Try again, and restart Windows if it keeps happening.`,
};

let text = readFileSync(BASE, 'utf8');

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
const parse = (xml) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  return map;
};
const neutral = parse(readFileSync(BASE, 'utf8'));
// Derived, never pinned: the machine set grows whenever the command line
// gains an event-log string, and a literal here would fail every generator
// at once while asserting nothing about what was actually stripped.
const cliMachineExpected = [...neutral.keys()].filter(isMachineCliKey).length;
const written = readFileSync(OUT, 'utf8');
const output = parse(written);
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
