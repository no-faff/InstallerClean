#!/usr/bin/env node
// Fails (exit 1) when a rule that lives BETWEEN two strings, or between a string
// and the code that shows it, is broken in any language in LANGS. The rules are
// numbered below: a control's spoken name against its visible label, a sentence
// against the button it names, a word a string must not repeat, how github is cased
// for the surface it is on, the folder token and the link phrase a translation has
// to keep, a window title written in code as well as in XAML, and a resource read
// that goes round Strings. Exit 2 where Strings.resx cannot be read, a resx file's
// entries cannot all be parsed, a C# file cannot be read to its end, or a XAML file
// cannot be read or has a comment that does not close.
//
// Run from the repo root: node scripts/check-cross-key-rules.mjs
import { readFileSync, existsSync } from 'node:fs';
import { standsInFor } from './plural-overrides.mjs';
import { readLedger, englishFor, recordedFreshness } from './translation-ledger.mjs';
import { readCSharp, argumentSpan } from './csharp-source.mjs';
import { sourceFiles, lineIndex } from './source-files.mjs';
import { blankXmlComments } from './xml-source.mjs';

const RESX_DIR = 'src/InstallerClean.Core/Resources';
const GUI = 'src/InstallerClean';

// Priority order, matching SupportedLanguages.CultureNames. The neutral is
// checked too: en-GB's own pairs were only ever settled by hand, and it is the
// language every satellite was generated from.
const LANGS = ['en-GB', 'zh-Hans', 'ru', 'es', 'ja', 'pt-BR', 'pl', 'tr',
  'ko', 'fr', 'it', 'de', 'id', 'vi', 'uk', 'nl', 'zh-Hant'];

// ---------------------------------------------------------------------------
// Rule 1. A control's spoken name says the same thing as its visible label.
// Microsoft states it too: the name "should be the same as the label text on
// screen" and must not carry the access-key marker (UWP
// AutomationProperties.Name, Remarks).
//
// Membership is declared, never inferred, because most label/name pairs here are
// SUPPOSED to disagree: a name that tells several identical controls apart has to
// elaborate ("Cancel scan" over a bare "Cancel"), and nothing in the two strings
// tells that apart from a name that has drifted. The members are the names that
// RESTATE their label rather than disambiguate it; the ones that elaborate have
// their own set below and a weaker rule. Rule 2 is what stops a new control
// joining neither list.
const MUST_AGREE = [
  // Content is a StackPanel (icon + AccessText), so there is no string for WPF
  // to derive a name from and the override IS the label.
  { label: 'Action.LeaveATip', name: 'Automation.LeaveATip.About' },
  { label: 'Action.Donate', name: 'Automation.Donate' },
  { label: 'Action.DonateSmall', name: 'Automation.DonateSmall' },
  { label: 'Action.LeaveStarOnGitHub', name: 'Automation.LeaveStarOnGitHub.About' },
  // Content is a plain string, so the override restates what WPF derives, and can
  // still drift from it in any one language.
  { label: 'Action.CheckForUpdates', name: 'Automation.CheckForUpdates' },
  // A link whose Run is the label, so the override restates it, cased for speech.
  { label: 'UpdateNotice.Download', name: 'Automation.UpdateNotice.Download' },
  { label: 'UpdateNotice.DownloadNetFramework48', name: 'Automation.UpdateNotice.DownloadNetFramework48' },
  // Section headings drawn in SmallCaps: the resx value is pre-uppercased so a
  // screen reader never meets the glyph mapping, and the name is the same words
  // in ordinary case. It exists for the casing alone.
  { label: 'Section.SayThanks', name: 'Automation.SayThanks' },
  { label: 'Section.Registered.Patches', name: 'Automation.Section.Patches' },
  { label: 'Section.Registered.Details', name: 'Automation.Section.ProductDetails' },
  { label: 'Section.Backup.Folder', name: 'Automation.Section.BackupFolder' },
];

// ---------------------------------------------------------------------------
// Rule 2. Every automation name resolved in XAML is classified below, so a new
// control fails this guard until somebody decides which list it belongs in.
//
// Names set from code-behind are out of a static check's reach, being built from
// data or reassembled from the sentence a hyperlink was split out of. The one
// that looks like a label/name split and is not is the splash window's Cancel,
// whose Content is set to the same string on the line above.

// A field whose visible text is a VALUE, named for the field it holds. There is
// no rule to write: a path, a count and a version have nothing in common with
// the words that name them. The Field.* keys label the details panes' values;
// Window.Main.Title names the About window's version box, whose text is the
// version; Automation.UpdateNotice.ScoopCommand names the box whose text is Scoop's
// update command.
const LABELS_A_VALUE = new Set([
  'Automation.BackupFolder',
  'Automation.CompletionErrors',
  'Automation.UpdateNotice.ScoopCommand',
  'Window.Main.Title',
  'Field.Application', 'Field.Author', 'Field.Comment', 'Field.FileSize',
  'Field.Keywords', 'Field.Reason', 'Field.SigningCertificate',
  'Field.Subject', 'Field.Title',
]);

// One label, several controls: Cancel and Details buttons that read identically
// until the name says which is which. The rule they owe is containment: WCAG 2.5.3
// Label in Name asks that the accessible name contain the visible label, so speech
// input reaches the control by the word the user can see. KEEP THIS SET APART FROM
// LABELS_A_VALUE. Rule 2 asks only that a control be classified, so a control filed
// under the set that measures nothing satisfies it and is never measured.
const ELABORATES_A_LABEL = [
  { label: 'Action.Cancel', name: 'Automation.CancelScan' },
  { label: 'Action.Cancel', name: 'Automation.CancelOperation' },
  { label: 'Action.Cancel', name: 'Automation.CancelStartupScan' },
  { label: 'Action.Details', name: 'Automation.ViewOrphanedFiles' },
  { label: 'Action.Details', name: 'Automation.ViewRegisteredFiles' },
  // Copy beside Scoop's update command, whose name says what it copies.
  { label: 'Action.Copy', name: 'Automation.UpdateNotice.CopyCommand' },
];

// The same rule for a name built in code rather than resolved in the XAML. The
// stop-waiting button's spoken name names the drive or share the line above it is
// waiting for, so it is composed with that root (DisplayHelpers.StopWaitingFor) and
// bound, and rule 2 below, which reads the XAML, cannot see it. Each name owes
// containment in every language, as above, and is held to being read from the app's
// C# and to naming no control in the XAML, where the lists above would own it.
const ELABORATES_A_LABEL_IN_CODE = [
  { label: 'Action.StopWaiting', name: 'Automation.StopWaitingForDrive' },
  { label: 'Action.StopWaiting', name: 'Automation.StopWaitingForPath' },
];

// A control whose automation name resolves to THE SAME KEY as the visible
// heading that labels it, through AutomationProperties.LabeledBy pointing at
// that heading plus an explicit Name resolving to the heading's own key.
//
// The list only ever admits a key to a classification, and rule 2 below fails on
// any automation name in none of the five lists, so an empty list makes the guard
// stricter and never looser.
//
// Do not put such a key in MUST_AGREE. That list measures whether two keys' values
// agree in every language, and one key cannot disagree with itself, so the entry
// would pass whatever anybody wrote. The rule it owes is KEY IDENTITY, checked in
// rule 2a below: the key also appears as visible Text in the same XAML file.
// Repoint the Name at a different key, or delete the heading, and the guard fails
// until the control is classified again.
const NAME_IS_THE_LABEL = new Set([
  'Window.Report.Title',
]);

// Nothing visible to agree with: an icon-only button, a scroll region, a
// progress bar. The name is the control's only text.
const NO_VISIBLE_LABEL = new Set([
  'Automation.ChangeLanguage',
  'Automation.Close',
  'Automation.CloseWindow',
  'Automation.Minimise',
  'Automation.OperationProgress',
  'Automation.ReportInfo',
  'Automation.ScanningProgress',
  'Automation.StartupScanProgress',
  'Automation.Scroll.DialogBody',
  'Automation.Scroll.FileDetails',
  'Automation.Scroll.ProductDetails',
  'Automation.Scroll.ReportContents',
  'Automation.Scroll.ResultDetails',
  'Automation.Scroll.ScanResults',
  'Automation.Scroll.Waiting',
]);

// ---------------------------------------------------------------------------
// Rule 3. A sentence that quotes a button quotes that button's own label.
//
// The not-yet-scanned line tells the reader to press Re-scan, and its resx
// comment tells the translator to use whatever Action.Rescan says in their
// language. Reword the button in one language without the sentence and that
// language names a button it does not have.
//
// Membership is every neutral sentence naming a button whose label the language
// can quote uninflected. Not every sentence that mentions one: the
// pending-reboot family says "Move and Delete are paused", which several
// languages have to inflect, so a rule there would fault a correct translation.
//
// A sentence whose satellites still carry English goes in rule 3a below instead,
// with the condition it is out under.
const QUOTES_A_LABEL = [
  { sentence: 'Body.NotScanned.Why', label: 'Action.Rescan' },
  // Each confirmation dialog's spoken help names both of its own buttons, which
  // is the whole of what it says: "Move puts the unneeded files in the chosen
  // destination folder. Cancel leaves them where they are."
  { sentence: 'Automation.ConfirmMove', label: 'Action.Move' },
  { sentence: 'Automation.ConfirmMove', label: 'Action.Cancel' },
  { sentence: 'Automation.ConfirmDelete', label: 'Action.DeletePermanently' },
  { sentence: 'Automation.ConfirmDelete', label: 'Action.Cancel' },
  // The sentence the delete dialog introduces offers the other way of doing it
  // by name, so the offer is worth exactly what the button it points at is
  // called, in both of its forms.
  { sentence: 'Confirm.DeletePermanently.Singular', label: 'Action.Move' },
  { sentence: 'Confirm.DeletePermanently.Plural', label: 'Action.Move' },
  // The registered-files row's own button, named by the sentence that sends the
  // reader to it.
  { sentence: 'Summary.MissingFromDisk.Singular', label: 'Action.Details' },
  { sentence: 'Summary.MissingFromDisk.Plural', label: 'Action.Details' },
  // A batch that stopped because the backup folder would no longer resolve says
  // which button starts the scan again.
  { sentence: 'Error.DestinationChangedMidBatch', label: 'Action.Rescan' },
  // The line naming a drive or share the app carried on without ends by saying
  // which button to press once it responds normally.
  { sentence: 'Summary.SourceGivenUp.Drive', label: 'Action.Rescan' },
  { sentence: 'Summary.SourceGivenUp.Path', label: 'Action.Rescan' },
  { sentence: 'Summary.SourcesGivenUp', label: 'Action.Rescan' },
  // The line saying why a scan held back the files no record names ends, for every
  // reason, by saying which button to press.
  ...['Busy', 'Suspended', 'RenameInCache', 'RenameUnresolved', 'AccessRefused',
    'RegistryUnreadable', 'LockUnreadable', 'LockUnreadableWindowsError', 'Other',
    'ClockSetBack'].map((reason) => ({ sentence: `Summary.ScanSignal.${reason}`, label: 'Action.Rescan' })),
];

// ---------------------------------------------------------------------------
// Rule 3a. The same rule, on the sentences whose satellites cannot meet it yet.
//
// A satellite still carrying English cannot quote a translated button, so a
// sentence in that state would fail rule 3 in every such language, for a reason
// check-still-english and check-superseded-english already name in the place the
// fix goes. It is declared here with the condition it is out under, and it is
// checked in each language as soon as that condition lifts there.
//
// TWO LEGS, AND NEITHER IS SPARE, BECAUSE A SATELLITE HOLDS ENGLISH IN TWO SHAPES.
// The value equalling the English it answers for catches a satellite carrying the
// CURRENT neutral, which is what a key flagged for re-translation holds until the
// round reaches it. The ledger recording the slot stale catches a satellite
// carrying a wording the neutral has SINCE REPLACED, which is equal to nothing the
// first leg compares against. And a slot the ledger records as unverified is a
// claim nobody has made rather than a translation, so the ledger has no answer for
// it and the value comparison is the only thing that can hold it.
//
// Either leg holds a slot out; a slot neither holds is checked. Every slot held
// out is printed with the leg that held it, and an entry no language holds out any
// more fails the run as something to move up into rule 3.
const QUOTES_A_LABEL_ONCE_TRANSLATED = [];

// ---------------------------------------------------------------------------
// Rule 4. A string must not repeat a word the control it hangs on already says.
//
// The star pill's tooltip doubles as its screen-reader help, and the button it
// sits on already names GitHub, so naming it again says the same word twice in
// one glance and twice in one announcement. The key's own resx comment says so.
const MUST_NOT_NAME = [
  { key: 'Tooltip.LeaveStarOnGitHub.About', word: 'github' },
];

// ---------------------------------------------------------------------------
// Rule 5. github is cased for the surface it is on.
//
// Narrator reads the CamelCase form letter by letter, "G I T hub", so every
// string that is ONLY ever spoken lower-cases it; every string that is drawn
// keeps the company's own capitalisation, because a reader sees it. Rule 1
// compares case-insensitively, so the star pill needs no exception there.
const GITHUB_SPOKEN = [
  'Automation.LeaveStarOnGitHub.About',
  'Automation.CheckForUpdates.HelpText',
  'Automation.About.Guide.HelpText',
  'Automation.About.ReportProblem.HelpText',
  'Automation.AutoUpdateCheck.HelpText',
  'Automation.Licence.HelpText',
  'Automation.UpdateNotice.Download',
];
const GITHUB_DRAWN = [
  'Action.LeaveStarOnGitHub',
  'UpdateCheck.Failed.NetworkUnavailable',
  'UpdateCheck.Failed.ServerError',
  'UpdateCheck.Failed.ResponseParseError',
  'UpdateCheck.Failed.Timeout',
  'UpdateNotice.Download',
];

// ---------------------------------------------------------------------------
// Rule 6. The installer-folder token survives translation.
//
// Every string that names the installer cache folder writes {InstallerFolder}
// and Strings.Get substitutes the resolved path, so a machine whose Windows
// lives somewhere other than C:\Windows is told the truth. A translator who
// renders the token into their own language, or drops it, gets a sentence with
// a hole in it. check-resx-parity cannot see this: it matches {N} only.
const FOLDER_TOKEN = '{InstallerFolder}';

// ---------------------------------------------------------------------------
// Rule 9. A bracketed link phrase survives translation.
//
// A sentence that carries a link holds it as a phrase inside itself rather than
// as a line of its own. The resx marks the phrase with [square brackets] and
// CompositionParsing.SplitAtBracketedPhrase turns the pair into a Hyperlink as
// the window is built. A value with no pair renders as plain prose, which is the
// right fallback and is also completely silent: a translator who drops the
// brackets takes a link off a screen and nothing anywhere says so.
//
// Which keys carry a pair is the neutral's decision, as with rule 6, so there
// is no list here to go stale as screens gain and lose links. Both directions
// are faulted: a satellite that has LOST its pair silently drops a link, and
// one that has GAINED brackets on a key nothing splits paints the brackets.
const bracketCounts = (value) => ({
  pairs: (value.match(/\[[^[\]]*\]/g) ?? []).length,
  chars: (value.match(/[[\]]/g) ?? []).length,
});

// ---------------------------------------------------------------------------
// Rule 10. A tooltip that is one sentence carries no closing full stop.
//
// A tooltip is a label for a control, and a one-sentence one reads as a caption
// with a stop after it. A tooltip of two or more sentences is left alone. A value
// is judged on its own wording in its own language: it is one sentence when
// nothing before its final stop is a sentence break, a break being . ! or ?
// followed by a space, or one of the ideographic 。 ！ ？ followed by more text.
// The stop that is forbidden is the full stop of the language: a single . (an
// ellipsis is not one) or the ideographic 。, read through any closing bracket or
// quote after it.
//
// Which keys are tooltips is read from the code and not listed here: every
// Tooltip.* key in the neutral, every key a XAML ToolTip attribute, setter or
// element draws, and every key a C# file assigns to a ToolTip through Strings.
// A tooltip text that a view model returns is found by its Tooltip.* name. A
// satellite is held to the rule as the neutral is, because a translation can
// join two English sentences into one, or split one into two, and its stops
// follow its own sentences.
const STOP_CLOSERS = '\\)\\]}\u300D\u300F\uFF09\u3011\u201D\u2019"\'';
const TAIL = new RegExp(`[${STOP_CLOSERS}]*$`, 'u');
const SENTENCE_BREAK = new RegExp(`[.!?](?=\\s)|[\u3002\uFF01\uFF1F][${STOP_CLOSERS}]*[^${STOP_CLOSERS}]`, 'u');
const judgeTooltip = (value) => {
  const core = value.trim().replace(TAIL, '');
  const stopped = /(?<!\.)\.$|\u3002$/u.test(core);
  const body = stopped ? core.slice(0, -1) : core;
  return { oneSentence: !SENTENCE_BREAK.test(body), stopped };
};
// The judge is run on known values first, and a run that cannot tell them apart
// stops, so that a rule which can no longer fault anything does not print a pass.
for (const [value, oneSentence, stopped] of [
  ['Open it.', true, true], ['Open it', true, false], ['Open it. Then close it.', false, true],
  ['Open it. Then close it', false, false], ['\u8981\u5F00\u542F\u3002', true, true],
  ['\u958B\u304F\u3002\u9589\u3058\u308B\u3002', false, true],
  ['Loading...', true, false], ['\u300C\u958B\u304F\u3002\u300D', true, true],
  ['\uFF08\u958B\u304F\u3002\uFF09', true, true],
  ['\u300C\u958B\u304F\u3002\u300D\u9589\u3058\u308B', false, false]]) {
  const got = judgeTooltip(value);
  if (got.oneSentence !== oneSentence || got.stopped !== stopped) {
    console.error(`Rule 10 control failed on "${value}": read ${JSON.stringify(got)}.`);
    process.exit(2);
  }
}

// ---------------------------------------------------------------------------
// Rule 7. A window title resolved in XAML is never written in code.
//
// Every dialog here is ShowInTaskbar=False under custom chrome, so Title is
// never painted and exists only for the announcement a screen reader makes when
// the window opens. A window takes its title from one key in XAML or composes it
// in code (the heading and the question, not a category), never both. A write in
// code replaces the value the XAML resolved, and one made before the window shows
// leaves the key referenced and never read, which check-dead-resx-keys does not
// report.
//
// A write is an assignment by any operator, a SetValue, SetCurrentValue,
// SetBinding or ClearValue on TitleProperty, the same through BindingOperations,
// or a Title set in an object initializer. A receiver typed as Window can hold any
// window, so a write through one counts against every window whose XAML resolves
// Title, as does one on an object made by new() with no type written. A Title not
// read as a declaration or as another object's member is a write.

// ---------------------------------------------------------------------------
// Rule 8. A resx value reaches a user through Strings and nothing else.
//
// Strings.Get and Strings.Find substitute rule 6's token on the way out.
// ResourceManager answers any key by name, and a read that goes round the two
// doors hands a user a literal {InstallerFolder} on screen, in a console or through
// a screen reader. A satellite-only plural override has no typed accessor, and
// Strings.Find is its door.
//
// The two doors are internal to the Core assembly and so is the manager behind
// them, and every project in the solution holds InternalsVisibleTo, so a compiler
// cannot tell a sanctioned read from a bypass.
//
// Comments come off before the search and strings stay, so a file that discusses
// the manager in a comment is not a finding and a name in a string is.
const RAW_READ = /\bResourceManager\b/;

// The files that read raw, each with why. Adding one is a decision about whether a
// value can reach a user with its token unspent, so an entry says how it cannot.
const RAW_READ_ALLOWED = [
  {
    file: 'src/InstallerClean.Core/Resources/Strings.Designer.cs',
    reason: 'The doors themselves. The manager is private here and Get and Find are '
      + 'what hold it.',
  },
  {
    file: 'src/InstallerClean.Tests/Resources/SatelliteResxParityTests.cs',
    reason: 'Enumerates a whole resource set per culture to prove each shipped '
      + 'satellite carries what the neutral does. A door that answers one named key '
      + 'cannot list a set.',
  },
  {
    file: 'src/InstallerClean.Tests/Helpers/InstallerFolderTokenTests.cs',
    reason: 'Audits every shipped culture for a hardcoded installer path, which needs '
      + 'the values raw: substituted, a token-carrying string holds neither the token '
      + 'nor the literal, so the audit would pass by construction.',
  },
  {
    file: 'src/InstallerClean.Tests/Helpers/CountedStringTests.cs',
    reason: 'Enumerates a whole resource set per culture to prove every CLDR category '
      + 'override names a prefix the code actually passes to Pluralise. A door that '
      + 'answers one named key cannot list a set. Every other read in the file goes '
      + 'through Find or Get.',
  },
  {
    file: 'src/InstallerClean.Tests/Helpers/InstallerPathTextCopyingTests.cs',
    reason: 'Enumerates a whole resource set per culture to prove no shipped value '
      + 'carries a word joiner or a zero-width space, which a copy then removes. A door '
      + 'that answers one named key cannot list a set, and the values have to be the '
      + 'raw ones the drawing transform starts from.',
  },
  {
    file: 'src/InstallerClean.Tests/Helpers/LocalisationOverrideTests.cs',
    reason: 'Reads one key at a named culture to prove an explicit language pick '
      + 'drives the typed accessor. The expectation has to come from the satellite '
      + 'rather than from the door under test.',
  },
  {
    file: 'src/InstallerClean.Tests/Helpers/DeleteConfirmationCompositionTests.cs',
    reason: 'Enumerates one culture\'s own resource set, with tryParents false, to '
      + 'find the plural overrides that language declares. A door that answers one '
      + 'named key cannot list a set, and which forms exist is the language\'s '
      + 'decision rather than a list this file could hold. Every value it goes on to '
      + 'assert against is read through Strings.Get.',
  },
  {
    file: 'src/InstallerClean.Tests/Helpers/LinkPhraseCompositionTests.cs',
    reason: 'Enumerates the neutral resource set to derive which sentences carry a '
      + 'link phrase from the English punctuation, the way linkKeys below is built. A '
      + 'door that answers one named key cannot list a set, and a list written by hand '
      + 'would answer for the sentences that linked on the day it was written. Every '
      + 'value it goes on to assert against is read through Strings.Get.',
  },
];

// ---------------------------------------------------------------------------

// PARSE CONTROL. About the READING and not about the content: a regex that has
// stopped matching yields an empty set, and a silent zero over an empty set reads
// exactly like a clean result. BOTH legs are load-bearing. raw === 0 catches a
// file that declares no entry at all, which the equality cannot see on its own
// because 0 === 0 holds; parsed !== raw catches entries the reader dropped, which
// one <comment> moved above its <value> does to every regex wanting <value> on the
// same whitespace run as <data>, and the Visual Studio resx editor writes that
// shape. Counted with <data\b rather than '<data ' so a tab after the tag name is
// not read as an empty file. Neither figure is written down here, so adding a
// string to the resx cannot make this go stale.
//
// It stops the run naming the file, ahead of the stale-declaration block below,
// whose message would name keys rather than the file it could not read.
const parseControl = (file, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${file}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this check cannot show it read.');
  process.exit(2);
};
// Reads the file itself rather than taking its text, so the control can name the
// path in its message: a failure here is about one file and the reader needs to
// be told which.
const values = (path) => {
  let xml;
  try {
    xml = readFileSync(path, 'utf8');
  } catch (e) {
    console.error(`${path} cannot be read (${e.code ?? e.message}). Refusing to report on it.`);
    process.exit(2);
  }
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  // The RETAINED size rather than a match counter, and the difference is real: two
  // entries sharing a key name both match, and the second silently overwrites the
  // first, so the check would go on to reason about one value fewer than the file
  // declares. Comparing what survives against what the file declares catches the
  // dropped entry and the overwritten one in one comparison.
  parseControl(path, xml, map.size);
  return map;
};

// The wording, with the access-key apparatus taken off. The Latin languages
// underline a letter inside the word ("Buy me a _cuppa"); ja, ko and Chinese
// append a parenthesised Latin letter ("...(_B)"), their scripts having no
// letter to underline. That group is drawn but never spoken, so it comes off
// whole, while the inline marker leaves its letter behind. A doubled underscore
// is WPF's escape for a literal one, and only ASCII parentheses are recognised:
// a fullwidth pair fails this guard rather than passing quietly.
const wording = (value) => {
  let v = value.replace(/\s*\(_[^_)]\)/g, '');
  let out = '';
  for (let i = 0; i < v.length; i++) {
    if (v[i] === '_') {
      if (v[i + 1] === '_') { out += '_'; i++; continue; }
      continue;
    }
    out += v[i];
  }
  return out;
};

// Case folds in the language's own rules, not invariantly: Turkish İ folds to i
// only under tr, so an invariant comparison reports every Turkish heading as a
// mismatch.
//
// Whitespace is normalised out. Japanese sets github off from the following kana
// with a space in the star pill's spoken name and not in its label, both
// coolvitto's own. A space there is typography rather than a word, so the
// comparison asks neither string to change.
const compare = (value, lang) => wording(value).replace(/\s+/g, '').toLocaleLowerCase(lang);

// A C# file through csharp-source.mjs: its code, with the comments taken out and the
// string literals left in place, and its bare, with the string text taken out as well.
// A file that cannot be read to its end stops the run.
const readCode = (file) => {
  try {
    return readCSharp(readFileSync(file, 'utf8'));
  } catch (e) {
    console.error(`${file}: cannot be read to its end (${e.message}). Refusing to report on it.`);
    process.exit(2);
  }
};

// A XAML file with every comment blanked, so markup inside a comment is not read as
// markup. A file that cannot be read, or whose comment does not close, stops the run.
const readXaml = (file) => {
  let text;
  try {
    text = readFileSync(file, 'utf8');
  } catch (e) {
    console.error(`${file} cannot be read (${e.code ?? e.message}). Refusing to report on it.`);
    process.exit(2);
  }
  try {
    return blankXmlComments(text);
  } catch (e) {
    console.error(`${file}: ${e.message}. Refusing to report on it.`);
    process.exit(2);
  }
};

const problems = [];
// Kept apart from the findings below and reported first: a declaration this file
// makes about a key or a control that no longer exists says nothing about any
// language and makes every measurement under it unreliable.
const stale = [];

// --- Rule 2, once: the classification lists cover every automation name in the
// XAML, and claim none the XAML no longer has. Run before the per-language
// work, because a stale list is a fact about this file rather than about any
// language.
// Each XAML file is read once, here, for Rules 2 and 7, with its comments blanked.
const xamlText = new Map(sourceFiles(GUI, '.xaml').map((file) => [file, readXaml(file)]));
const xamlFiles = [...xamlText.keys()];
const namedInXaml = new Set();
const visibleTextInXaml = {};
for (const [file, xaml] of xamlText) {
  for (const m of xaml.matchAll(/AutomationProperties\.Name="\{loc:Translate ([A-Za-z0-9._]+)\}"/g))
    namedInXaml.add(m[1]);
  for (const m of xaml.matchAll(/Text="\{loc:Translate ([A-Za-z0-9._]+)\}"/g))
    (visibleTextInXaml[file] ??= new Set()).add(m[1]);
}

// Rule 2a. A NAME_IS_THE_LABEL key must still BE the label: the same key drawn as
// visible Text in the same file. The list's whole rule is this identity, so a
// name repointed at another key, or a heading deleted, fails here rather than
// passing an equality test it could never fail.
for (const key of [...NAME_IS_THE_LABEL].sort()) {
  const drawnIn = Object.entries(visibleTextInXaml)
    .filter(([, keys]) => keys.has(key)).map(([file]) => file);
  const namedIn = xamlFiles.filter((file) => xamlText.get(file)
    .includes(`AutomationProperties.Name="{loc:Translate ${key}}"`));
  if (!namedIn.length)
    stale.push(`${key} is in NAME_IS_THE_LABEL and names no control in the XAML. `
      + 'Renamed, removed, or moved to code-behind: update the lists above.');
  else if (!drawnIn.some((file) => namedIn.includes(file)))
    stale.push(`${key} is in NAME_IS_THE_LABEL but is not drawn as visible Text in `
      + `${namedIn.join(', ')}. Its whole rule is that the name and the label are one key, `
      + 'so either restore the heading or classify the control into another list.');
}
const classified = new Set([
  ...MUST_AGREE.map((p) => p.name), ...ELABORATES_A_LABEL.map((p) => p.name),
  ...LABELS_A_VALUE, ...NO_VISIBLE_LABEL, ...NAME_IS_THE_LABEL,
]);
for (const key of [...namedInXaml].sort())
  if (!classified.has(key))
    stale.push(`${key} names a control in the XAML and is in none of this file's five lists. `
      + 'Decide whether its name restates a visible label (MUST_AGREE), elaborates one to tell '
      + 'identical controls apart (ELABORATES_A_LABEL), names the field behind a value '
      + '(LABELS_A_VALUE), labels a control with no visible text (NO_VISIBLE_LABEL), or is the '
      + 'same key as the visible label itself (NAME_IS_THE_LABEL).');
for (const key of [...classified].sort())
  if (!namedInXaml.has(key))
    stale.push(`${key} is classified in this file but names no control in the XAML. `
      + 'Renamed, removed, or moved to code-behind: update the lists above.');

// Each C# file is read once, here, for Rules 7 and 8. Compared with the paths written
// by hand in RAW_READ_ALLOWED, which sourceFiles writes the same way, with forward
// slashes.
const csFiles = sourceFiles('src', '.cs');
const csRead = new Map(csFiles.map((file) => [file, readCode(file)]));

// --- Rule 7, once: source shape, not language.
// The windows whose XAML resolves Title, by the class each XAML declares, and every C#
// file of the app searched in its bare, so a Title in a comment or a string is not a
// write. The test project does not ship, and the other projects cannot name a window.
const titledWindows = new Map();
for (const [file, xaml] of xamlText) {
  const title = xaml.match(/\bTitle="\{loc:Translate ([A-Za-z0-9._]+)\}"/);
  if (!title) continue;
  const declared = xaml.match(/\bx:Class="(?:[A-Za-z_][\w.]*\.)?([A-Za-z_]\w*)"/);
  if (!declared) {
    problems.push(`${file} resolves Title from ${title[1]} and declares no x:Class, so this `
      + 'check cannot tell which class to look for writes to its title. Give the window its x:Class.');
    continue;
  }
  titledWindows.set(declared[1], { xaml: file, key: title[1] });
}

const ID = '@?[A-Za-z_]\\w*';
const QUALIFIED = '(?:[A-Za-z_]\\w*\\s*\\.\\s*)*[A-Za-z_]\\w*';
// Every assignment operator. ==, !=, <=, >= and => are not among them.
const ASSIGN = '(?:>>>|>>|<<|\\?\\?|[-+*/%&|^])?=(?![=>])';
const TYPE_KEYWORDS = new Set(['bool', 'byte', 'sbyte', 'char', 'decimal', 'double', 'float', 'int',
  'uint', 'nint', 'nuint', 'long', 'ulong', 'short', 'ushort', 'object', 'string', 'var', 'dynamic']);
const KEYWORDS = new Set(['abstract', 'as', 'base', 'break', 'case', 'catch', 'checked', 'class',
  'const', 'continue', 'default', 'delegate', 'do', 'else', 'enum', 'event', 'explicit', 'extern',
  'false', 'finally', 'fixed', 'for', 'foreach', 'goto', 'if', 'implicit', 'in', 'interface',
  'internal', 'is', 'lock', 'namespace', 'new', 'null', 'operator', 'out', 'override', 'params',
  'private', 'protected', 'public', 'readonly', 'ref', 'return', 'sealed', 'sizeof', 'stackalloc',
  'static', 'struct', 'switch', 'this', 'throw', 'true', 'try', 'typeof', 'unchecked', 'unsafe',
  'using', 'virtual', 'void', 'volatile', 'while', 'await', 'yield', 'when', 'and', 'or', 'not',
  'with', 'init', 'required', 'scoped', 'file', 'record', 'async', 'partial', 'get', 'set']);
const MODIFIERS = new Set(['public', 'private', 'protected', 'internal', 'static', 'readonly',
  'const', 'new', 'override', 'virtual', 'sealed', 'abstract', 'extern', 'unsafe', 'volatile',
  'required', 'partial', 'ref', 'scoped', 'fixed', 'async', 'file']);
const OPENERS = '([{';
const CLOSERS = ')]}';
const isWord = (c) => c !== undefined && /[\w@]/.test(c);

// Brackets in bare are the code's own, so they pair up.
const openerOf = (bare, close) => {
  let depth = 0;
  for (let i = close; i >= 0; i--) {
    if (CLOSERS.includes(bare[i])) depth++;
    else if (OPENERS.includes(bare[i]) && --depth === 0) return i;
  }
  return -1;
};
const enclosingOpener = (bare, at) => {
  let depth = 0;
  for (let i = at - 1; i >= 0; i--) {
    if (CLOSERS.includes(bare[i])) depth++;
    else if (OPENERS.includes(bare[i])) {
      if (depth === 0) return i;
      depth--;
    }
  }
  return -1;
};
const backOverSpace = (bare, i) => {
  while (i >= 0 && /\s/.test(bare[i])) i--;
  return i;
};
const wordEndingAt = (bare, i) => {
  let j = i;
  while (j >= 0 && isWord(bare[j])) j--;
  return { word: bare.slice(j + 1, i + 1).replace(/^@/, ''), start: j + 1 };
};
// The arguments of a call, split at the commas of its own level.
const callArguments = (bare, open) => {
  const span = argumentSpan(bare, open);
  if (!span) return null;
  const args = [];
  let depth = 0;
  let from = span[0];
  for (let i = span[0]; i < span[1]; i++) {
    if (OPENERS.includes(bare[i])) depth++;
    else if (CLOSERS.includes(bare[i])) depth--;
    else if (bare[i] === ',' && depth === 0) {
      args.push(bare.slice(from, i).trim());
      from = i + 1;
    }
  }
  args.push(bare.slice(from, span[1]).trim());
  return args;
};
const namesTitleProperty = (arg) => new RegExp(`^(?:${QUALIFIED}\\s*\\.\\s*)?TitleProperty$`).test(arg ?? '');

// A declaration context: where a member, a local or a parameter can begin.
const declarationContextBefore = (bare, start) => {
  const i = backOverSpace(bare, start - 1);
  if (i < 0 || ';{}](,'.includes(bare[i])) return true;
  if (!isWord(bare[i])) return false;
  return MODIFIERS.has(wordEndingAt(bare, i).word);
};
// Whether the text ending just before `at` is a type standing where a declaration
// begins, which makes a Title after it a declared name rather than a write.
const declaresTitle = (bare, at) => {
  let i = backOverSpace(bare, at - 1);
  if (i < 0) return false;
  if (bare[i] === '?') {
    i--;
    if (i < 0 || !(isWord(bare[i]) || '>])'.includes(bare[i]))) return false;
  }
  // A qualified type: walk back from its last name over Namespace.Type.
  const qualifiedStart = (from) => {
    let start = from;
    for (let j = backOverSpace(bare, start - 1); j >= 0 && bare[j] === '.';) {
      const k = backOverSpace(bare, j - 1);
      if (!isWord(bare[k])) break;
      start = wordEndingAt(bare, k).start;
      j = backOverSpace(bare, start - 1);
    }
    return start;
  };
  let start;
  if (isWord(bare[i])) {
    const { word, start: s } = wordEndingAt(bare, i);
    if (KEYWORDS.has(word) && !TYPE_KEYWORDS.has(word)) return false;
    start = qualifiedStart(s);
  } else if (bare[i] === '>') {
    let depth = 0;
    let j = i;
    for (; j >= 0; j--) {
      if (bare[j] === '>') depth++;
      else if (bare[j] === '<' && --depth === 0) break;
      else if (';{}='.includes(bare[j])) return false;
    }
    const k = backOverSpace(bare, j - 1);
    if (j < 0 || !isWord(bare[k])) return false;
    start = qualifiedStart(wordEndingAt(bare, k).start);
  } else if (bare[i] === ']') {
    // An array type: its rank brackets hold nothing but commas, after an element type.
    const open = openerOf(bare, i);
    if (open < 0 || !/^[\s,]*$/.test(bare.slice(open + 1, i))) return false;
    return declaresTitle(bare, open);
  } else if (bare[i] === ')') {
    // A tuple type, which stands where a declaration begins; `if (...) Title = x` does not.
    start = openerOf(bare, i);
    if (start < 0) return false;
  } else return false;
  return declarationContextBefore(bare, start);
};

// What a receiver expression is: windows it names, every titled window when it is typed
// as Window, or null for anything else.
const everyWindow = (what) => ({ any: true, what });
const typeReach = (type) => {
  const name = type.replace(/\s/g, '').replace(/\?$/, '').split('.').pop();
  if (name === 'Window') return everyWindow('a receiver typed as Window');
  if (titledWindows.has(name)) return { classes: [name] };
  return null;
};
const segmentsOf = (expr) => {
  const out = [];
  let depth = 0;
  let from = 0;
  for (let i = 0; i < expr.length; i++) {
    if (OPENERS.includes(expr[i])) depth++;
    else if (CLOSERS.includes(expr[i])) depth--;
    else if (expr[i] === '.' && depth === 0) {
      out.push(expr.slice(from, i).replace(/[?!]\s*$/, '').trim());
      from = i + 1;
    }
  }
  out.push(expr.slice(from).replace(/!\s*$/, '').trim());
  return out;
};
const reach = (expr, names, own) => {
  expr = expr.trim().replace(/!$/, '').trim();
  if (!expr) return null;
  let m = expr.match(new RegExp(`^new\\s+(${QUALIFIED})\\s*(?:<[^<>]*>)?\\s*[({]`));
  if (m) return typeReach(m[1]);
  if (expr[0] === '(' && expr.endsWith(')') && openerOf(expr, expr.length - 1) === 0) {
    const inner = expr.slice(1, -1).trim();
    return reach(inner, names, own);
  }
  m = expr.match(new RegExp(`^\\(\\s*(${QUALIFIED}\\s*\\??)\\s*\\)\\s*\\S`));
  if (m) return typeReach(m[1]);
  m = expr.match(new RegExp(`\\bas\\s+(${QUALIFIED}\\s*\\??)$`));
  if (m) return typeReach(m[1]);
  const segments = segmentsOf(expr).map((s) => s.replace(/^@/, ''));
  const last = segments[segments.length - 1];
  if (/^(?:MainWindow|Owner)$/.test(last) || /^GetWindow\s*\(/.test(last)
    || /^(?:Windows|OwnedWindows)\s*\[/.test(last))
    return everyWindow('a receiver typed as Window');
  if (segments.length === 1 && /^(?:this|base)$/.test(last)) return own.length ? { classes: own } : null;
  if (segments.length === 1 || (segments.length === 2 && segments[0] === 'this'))
    return names.get(last) ?? null;
  return null;
};
// The receiver written before the '.' or '?.' at `dot`.
const receiverBefore = (bare, dot) => {
  let i = backOverSpace(bare, dot - 1);
  if (bare[i] === '?' || bare[i] === '!') i = backOverSpace(bare, i - 1);
  const end = i + 1;
  let start = end;
  for (;;) {
    if (i >= 0 && ')]'.includes(bare[i])) {
      const open = openerOf(bare, i);
      if (open < 0) break;
      start = open;
      i = backOverSpace(bare, open - 1);
      if (isWord(bare[i])) {
        start = wordEndingAt(bare, i).start;
        i = backOverSpace(bare, start - 1);
      }
    } else if (isWord(bare[i])) {
      start = wordEndingAt(bare, i).start;
      i = backOverSpace(bare, start - 1);
    } else break;
    if (bare[i] === '.') {
      i = backOverSpace(bare, i - 1);
      if (bare[i] === '?' || bare[i] === '!') i = backOverSpace(bare, i - 1);
      continue;
    }
    break;
  }
  if (isWord(bare[i]) && wordEndingAt(bare, i).word === 'new') start = wordEndingAt(bare, i).start;
  return bare.slice(start, end);
};
// The type an object initializer opened at `brace` builds, read from what stands before
// it: windows, every window for new() with no type written, null for any other type, or
// 'block' where the brace is not an object initializer.
const initializerReach = (bare, brace, names, own) => {
  let i = backOverSpace(bare, brace - 1);
  let called = false;
  if (bare[i] === ')') {
    const open = openerOf(bare, i);
    if (open < 0) return 'block';
    i = backOverSpace(bare, open - 1);
    called = true;
  }
  const before = bare.slice(Math.max(0, i - 400), i + 1);
  if (/\bnew$/.test(before)) {
    if (!called) return null;
    const head = before.replace(/\bnew$/, '');
    let m = head.match(new RegExp(`(?:^|[^\\w.@])(${QUALIFIED}\\s*\\??)\\s+(${ID})\\s*=\\s*$`));
    if (m && !KEYWORDS.has(m[1].trim())) return typeReach(m[1]);
    m = head.match(new RegExp(`(?:^|[^\\w.@])(?:this\\s*\\.\\s*)?(${ID})\\s*=\\s*$`));
    if (m && names.has(m[1].replace(/^@/, ''))) return names.get(m[1].replace(/^@/, ''));
    return everyWindow('an object made by new() with no type written');
  }
  if (/\bwith$/.test(before)) return null;
  const m = before.match(new RegExp(`\\bnew\\s+(${QUALIFIED})\\s*(?:<[^<>]*>)?\\s*\\??$`));
  if (m) return typeReach(m[1]);
  return 'block';
};
// An attribute's '[' stands where a declaration begins; an indexer's follows an expression.
const insideAttribute = (bare, open) => {
  const outer = enclosingOpener(bare, open);
  if (outer < 0 || bare[outer] !== '[') return false;
  const i = backOverSpace(bare, outer - 1);
  return i < 0 || ';{}]'.includes(bare[i]);
};

const titleWrites = [];
for (const file of csFiles.filter((f) => f.startsWith(`${GUI}/`))) {
  if (!titledWindows.size) break;
  const { bare } = csRead.get(file);
  const lineAt = lineIndex(bare);
  const own = [...titledWindows.keys()].filter((c) => new RegExp(`\\bclass\\s+${c}\\b`).test(bare));
  const found = (at, target) => { if (target) titleWrites.push({ file, line: lineAt(at), target }); };

  // Names this file declares or assigns as a titled window or as a Window.
  const names = new Map();
  for (const type of [...titledWindows.keys(), 'Window'])
    for (const m of bare.matchAll(new RegExp(
      `(?<![\\w.@])(?:[A-Za-z_]\\w*\\s*\\.\\s*)*${type}\\s*\\??\\s+(${ID})\\s*(?=[=;,){]|\\bin\\b)`, 'g')))
      if (!KEYWORDS.has(m[1].replace(/^@/, ''))) names.set(m[1].replace(/^@/, ''), typeReach(type));
  for (let pass = 0; pass < 3; pass++)
    for (const m of bare.matchAll(new RegExp(`(?<![\\w.@])(?:this\\s*\\.\\s*)?(${ID})\\s*=(?![=>])`, 'g'))) {
      const name = m[1].replace(/^@/, '');
      if (names.has(name)) continue;
      let depth = 0;
      let end = m.index + m[0].length;
      for (; end < bare.length; end++) {
        if (OPENERS.includes(bare[end])) depth++;
        else if (CLOSERS.includes(bare[end])) { if (depth-- === 0) break; }
        else if ((bare[end] === ';' || bare[end] === ',') && depth === 0) break;
      }
      const target = reach(bare.slice(m.index + m[0].length, end), names, own);
      if (target) names.set(name, target);
    }

  // R.Title op, R?.Title op.
  for (const m of bare.matchAll(new RegExp(`(?:\\?\\s*)?\\.\\s*@?Title\\s*${ASSIGN}`, 'g')))
    found(m.index, reach(receiverBefore(bare, m.index), names, own));
  // R.SetValue(TitleProperty, ...) and its kin, with a receiver or without one.
  for (const m of bare.matchAll(/(?<![\w@])(SetValue|SetCurrentValue|SetBinding|ClearValue)\s*\(/g)) {
    const args = callArguments(bare, m.index + m[0].length - 1);
    if (!args || !namesTitleProperty(args[0])) continue;
    const i = backOverSpace(bare, m.index - 1);
    if (bare[i] === '.') found(m.index, reach(receiverBefore(bare, i), names, own));
    else found(m.index, own.length ? { classes: own } : null);
  }
  for (const m of bare.matchAll(/\bBindingOperations\s*\.\s*(?:SetBinding|ClearBinding)\s*\(/g)) {
    const args = callArguments(bare, m.index + m[0].length - 1);
    if (args && namesTitleProperty(args[1])) found(m.index, reach(args[0], names, own));
  }
  // Title op with no receiver: this object's own title, a member set in an object
  // initializer, or a declared name.
  for (const m of bare.matchAll(new RegExp(`(?<![\\w.@])@?Title\\s*${ASSIGN}`, 'g'))) {
    if (declaresTitle(bare, m.index)) continue;
    const open = enclosingOpener(bare, m.index);
    if (open >= 0 && bare[open] === '(' && insideAttribute(bare, open)) continue;
    if (open >= 0 && bare[open] === '{') {
      const built = initializerReach(bare, open, names, own);
      if (built !== 'block') {
        found(m.index, built);
        continue;
      }
    }
    found(m.index, own.length ? { classes: own } : null);
  }
}

const seenWrites = new Set();
for (const { file, line, target } of titleWrites) {
  const where = `${file}:${line}`;
  if (target.any) {
    if (seenWrites.has(where)) continue;
    seenWrites.add(where);
    problems.push(`${where} writes the Title of ${target.what}, which can be any of the windows `
      + `whose XAML resolves Title (${[...titledWindows.keys()].join(', ')}). Type it as the window's `
      + 'class, or move the write into the window.');
    continue;
  }
  for (const window of target.classes) {
    if (seenWrites.has(`${where} ${window}`)) continue;
    seenWrites.add(`${where} ${window}`);
    const { xaml, key } = titledWindows.get(window);
    problems.push(`${xaml} resolves Title from ${key} and ${where} writes it, so the window's title `
      + 'has two sources. Compose the whole title in code and drop the XAML attribute, and the key '
      + 'too unless something else shows it; or leave the title to the XAML.');
  }
}

// --- Rule 8, once: source shape, not language.
const rawAllowed = new Map(RAW_READ_ALLOWED.map((e) => [e.file, e.reason]));
const rawReaders = new Set(
  csFiles.filter((f) => RAW_READ.test(csRead.get(f).code)),
);
for (const file of [...rawReaders].sort())
  if (!rawAllowed.has(file))
    problems.push(`${file} reads a resource through ResourceManager rather than `
      + 'Strings.Get or Strings.Find, so a value naming the installer cache folder '
      + 'keeps its raw {InstallerFolder} all the way to a user. Route the read through '
      + "a door, or add the file to this file's RAW_READ_ALLOWED with the reason it "
      + 'cannot.');
for (const file of [...rawAllowed.keys()].sort())
  if (!rawReaders.has(file))
    stale.push(`${file} is allowed a direct ResourceManager read in this file and makes `
      + 'none: renamed, moved or routed through a door since. Drop its entry.');

// A name ELABORATES_A_LABEL_IN_CODE declares is built in the app's own C#: read there
// through its typed accessor, outside the tests, which would otherwise keep a name
// the app no longer speaks looking used.
const appCode = csFiles
  .filter((file) => !file.startsWith('src/InstallerClean.Tests/'))
  .map((file) => csRead.get(file).code)
  .join('\n');
for (const { name } of ELABORATES_A_LABEL_IN_CODE) {
  const accessor = `Strings.${name.replaceAll('.', '_')}`;
  if (!new RegExp(`\\b${accessor.replaceAll('.', '\\.')}\\b`).test(appCode))
    stale.push(`${name} is in ELABORATES_A_LABEL_IN_CODE and the app's C# never reads ${accessor}: `
      + 'renamed, removed, or moved into the XAML. Update the list.');
  if (namedInXaml.has(name))
    stale.push(`${name} is in ELABORATES_A_LABEL_IN_CODE and names a control in the XAML, where `
      + 'the lists above classify it. Move it there.');
}

// Rules 7 and 8 fault the XAML and the C#, where neither a resx nor a generator
// is in reach of the fix. The closing footer sends a reader to the generator for
// a language, so it belongs to the per-language rules alone; each source-shape
// message already carries its own instruction.
const sourceShapeProblems = problems.length;

// --- Rules 1 and 3 to 6, per language.
const neutral = values(`${RESX_DIR}/Strings.resx`);
const declaredKeys = [
  ...MUST_AGREE.flatMap((p) => [p.label, p.name]),
  ...ELABORATES_A_LABEL.flatMap((p) => [p.label, p.name]),
  ...ELABORATES_A_LABEL_IN_CODE.flatMap((p) => [p.label, p.name]),
  ...QUOTES_A_LABEL.flatMap((p) => [p.sentence, p.label]),
  ...MUST_NOT_NAME.map((p) => p.key),
  ...GITHUB_SPOKEN, ...GITHUB_DRAWN,
];
for (const key of [...new Set(declaredKeys)].sort())
  if (!neutral.has(key))
    stale.push(`${key} is named by a rule in this file and Strings.resx does not hold it.`);

// Which keys carry the folder token is the neutral's decision; a satellite that
// has gained or lost one has drifted from it.
const tokenKeys = new Set(
  [...neutral].filter(([, v]) => v.includes(FOLDER_TOKEN)).map(([k]) => k));

// The same for the link phrase. Membership is any bracket at all rather than a
// well-formed pair, so an unbalanced neutral is a finding here and not a key
// that quietly leaves the rule.
//
// Membership is the NEUTRAL's punctuation and not a list of the sites that split
// a value. Every production split runs unconditionally over whatever value it is
// handed, so a satellite value outside this set has been shown to disagree with
// the neutral sentence it answers for and nothing further, which is what its
// message says.
const linkKeys = new Set(
  [...neutral].filter(([, v]) => v.includes('[') || v.includes(']')).map(([k]) => k));

// Rule 10's keys. The XAML is read the way rules 2 and 7 read it, with its comments
// blanked: a ToolTip="{loc:Translate Key}" attribute, a ToolTip property setter, and
// a ToolTip element's Content.
const tooltipPatterns = [
  ['a ToolTip attribute', /\bToolTip="\{loc:Translate ([A-Za-z0-9._]+)\}"/g],
  ['a ToolTip setter', /Property="ToolTip"\s+Value="\{loc:Translate ([A-Za-z0-9._]+)\}"/g],
  ['a ToolTip element', /<ToolTip\b[^>]*?\bContent="\{loc:Translate ([A-Za-z0-9._]+)\}"/g],
];
const tooltipKeysFromXaml = new Set();
const tooltipKeysByPattern = new Map(tooltipPatterns.map(([name]) => [name, new Set()]));
for (const xaml of xamlText.values())
  for (const [name, pattern] of tooltipPatterns)
    for (const m of xaml.matchAll(pattern)) {
      tooltipKeysFromXaml.add(m[1]);
      tooltipKeysByPattern.get(name).add(m[1]);
    }
// A tooltip a C# file gives a Strings accessor: the accessor is the key with its
// dots written as underscores.
const keyOfAccessor = new Map([...neutral.keys()].map((key) => [key.replaceAll('.', '_'), key]));
const tooltipKeysFromCode = new Set();
for (const file of csFiles.filter((f) => f.startsWith(`${GUI}/`)))
  for (const m of csRead.get(file).code.matchAll(/\bToolTip\s*=\s*Strings\.([A-Za-z0-9_]+)/g))
    if (keyOfAccessor.has(m[1])) tooltipKeysFromCode.add(keyOfAccessor.get(m[1]));
const tooltipKeys = new Set([
  ...[...neutral.keys()].filter((key) => key.startsWith('Tooltip.')),
  ...tooltipKeysFromXaml,
  ...tooltipKeysFromCode,
]);
// Each way of reading a tooltip has to find at least one, or it has stopped matching
// and rule 10 would be judging the others alone. The element form is also the only
// one that draws a key not named Tooltip.*.
for (const [name] of tooltipPatterns)
  if (tooltipKeysByPattern.get(name).size === 0)
    stale.push(`Rule 10 found no tooltip through ${name} in the XAML; its reading of the XAML has `
      + 'stopped matching that form.');
if (tooltipKeysFromCode.size === 0)
  stale.push('Rule 10 found no tooltip a C# file assigns through Strings; its reading of the code '
    + 'has stopped matching.');
for (const key of tooltipKeysFromXaml)
  if (!neutral.has(key))
    stale.push(`${key} is drawn as a tooltip in the XAML and Strings.resx does not hold it.`);

// The labels each sentence must quote, keyed by the sentence, so a plural override
// can be measured against the same ones as the form it inflects. Two of the
// sentences name two buttons, which is why the value is a list. Rule 3 and rule 3a
// each get one, because a held-out sentence's overrides are held out with it.
const labelsFor = (pairs) => {
  const out = new Map();
  for (const { sentence, label } of pairs)
    out.set(sentence, [...(out.get(sentence) ?? []), label]);
  return out;
};
const labelsBySentence = labelsFor(QUOTES_A_LABEL);
const heldOutLabelsBySentence = labelsFor(QUOTES_A_LABEL_ONCE_TRANSLATED);

const ledger = readLedger();

// WHICH LEG, RATHER THAN WHETHER, so a run can say why a sentence went unmeasured
// instead of leaving a reader to work it out. Empty is the answer that means the
// sentence is rule 3's to check.
const holdingLegs = (key, value, lang) => {
  const legs = [];
  if (value === englishFor(key, neutral)) legs.push('its value is the English it answers for');
  if (recordedFreshness(ledger, key, lang, neutral) === 'stale')
    legs.push('the ledger records this slot stale');
  return legs;
};

// Held out, key-slot by key-slot, and what each declared entry saw, which is what
// tells an entry still doing its job from one that has nothing left to hold.
const heldOut = [];
const heldOutTally = new Map(
  QUOTES_A_LABEL_ONCE_TRANSLATED.map((entry) => [entry, { read: 0, held: 0 }]));

if (stale.length) {
  console.error(`Cross-key rules FAILED (${stale.length}): the declarations in this file are stale.`);
  for (const s of stale) console.error(`  ${s}`);
  // AND SAY WHAT THIS EXIT DID NOT CHECK. A stale declaration stops the run before
  // the per-language pass, so the list above is what this run reached and never a
  // count of what is wrong: any number of per-language failures can be standing
  // behind it, unseen, until the declarations are fixed and it runs again.
  console.error(`\n  Rules 1, 3 to 6 and 10 did NOT run: ${LANGS.length} languages unchecked. `
    + 'Fix the declarations and run again before believing anything about the translations.');
  process.exit(1);
}

// Rule 10's members, as the neutral judges them, printed on every run.
console.log(`Rule 10, tooltips (${tooltipKeys.size}), as the neutral judges them:`);
for (const key of [...tooltipKeys].sort()) {
  const { oneSentence, stopped } = judgeTooltip(neutral.get(key) ?? '');
  console.log(`  ${key.padEnd(34)} ${oneSentence ? 'one sentence' : 'two or more'}, ${stopped ? 'ends in a full stop' : 'no closing full stop'}`);
}
console.log('');

for (const lang of LANGS) {
  const path = lang === 'en-GB' ? `${RESX_DIR}/Strings.resx` : `${RESX_DIR}/Strings.${lang}.resx`;
  if (!existsSync(path)) {
    problems.push(`${lang}: ${path} is missing.`);
    continue;
  }
  // en-GB is the neutral, already read.
  const map = lang === 'en-GB' ? neutral : values(path);
  const failures = [];

  // A satellite short of a key is check-resx-parity's finding, not this one.
  // Refuse to measure round it either way rather than report a missing key as
  // agreement.
  const read = (key) => {
    if (map.has(key)) return map.get(key);
    failures.push(`${key} is missing from this satellite (run check-resx-parity)`);
    return null;
  };

  for (const { label, name } of MUST_AGREE) {
    const drawn = read(label), spoken = read(name);
    if (drawn === null || spoken === null) continue;
    if (compare(drawn, lang) !== compare(spoken, lang))
      failures.push(`${label} shows "${drawn}" but ${name} speaks "${spoken}"`);
  }

  for (const { label, name } of [...ELABORATES_A_LABEL, ...ELABORATES_A_LABEL_IN_CODE]) {
    const drawn = read(label), spoken = read(name);
    if (drawn === null || spoken === null) continue;
    if (!compare(spoken, lang).includes(compare(drawn, lang)))
      failures.push(`${label} shows "${wording(drawn)}" and ${name} speaks "${wording(spoken)}", `
        + 'which does not contain it, so speech input cannot reach the control by the word on it '
        + '(WCAG 2.5.3)');
  }

  // A PLURAL OVERRIDE IS ANOTHER COUNT FORM OF THE SENTENCE IT INFLECTS, so it
  // owes the same rule. Pluralise hands the override to the same screen the
  // neutral form would have reached, and a few-form naming a button by a word
  // that button does not carry misdirects at exactly the counts that select it.
  // Rules 6 and 9 below fold overrides in on the same ground, and which neutral
  // form each answers for is standsInFor's decision rather than this rule's.
  const overridesOf = (bySentence) => [...map.keys()]
    .filter((key) => !neutral.has(key))
    .flatMap((key) => (bySentence.get(standsInFor(key, neutral)) ?? [])
      .map((label) => ({ sentence: key, label })));

  for (const { sentence, label } of [...QUOTES_A_LABEL, ...overridesOf(labelsBySentence)]) {
    const body = read(sentence), button = read(label);
    if (body === null || button === null) continue;
    if (!compare(body, lang).includes(compare(button, lang)))
      failures.push(`${sentence} does not quote ${label} ("${wording(button)}")`);
  }

  // Rule 3a. The same comparison, run only where neither leg holds the sentence
  // out. An override of a held-out sentence goes through it as well, because a
  // few-form of an untranslated sentence is untranslated in the same way.
  for (const entry of [...QUOTES_A_LABEL_ONCE_TRANSLATED, ...overridesOf(heldOutLabelsBySentence)]) {
    const { sentence, label } = entry;
    const body = read(sentence), button = read(label);
    if (body === null || button === null) continue;
    // Counted for the declared entries alone. An override comes and goes with the
    // language that declares it, so it says nothing about whether the entry above
    // it still has anything to hold.
    const tally = heldOutTally.get(entry);
    // THE NEUTRAL CANNOT BE IN THE STATE THE LEGS DESCRIBE: it IS the English, so
    // the first leg would answer yes for ever and hold this sentence out of its
    // own language's check permanently.
    const legs = lang === 'en-GB' ? [] : holdingLegs(sentence, body, lang);
    if (tally && lang !== 'en-GB') tally.read += 1;
    if (legs.length) {
      if (tally) tally.held += 1;
      heldOut.push({ lang, sentence, legs });
      continue;
    }
    if (!compare(body, lang).includes(compare(button, lang)))
      failures.push(`${sentence} does not quote ${label} ("${wording(button)}")`);
  }

  for (const { key, word } of MUST_NOT_NAME) {
    const value = read(key);
    if (value === null) continue;
    if (value.toLowerCase().includes(word))
      failures.push(`${key} names "${word}", which the control it hangs on already says`);
  }

  for (const key of GITHUB_SPOKEN) {
    const value = read(key);
    if (value === null) continue;
    for (const found of value.match(/github/gi) ?? [])
      if (found !== 'github')
        failures.push(`${key} is spoken only and writes "${found}", which is read out a letter at a time`);
  }
  for (const key of GITHUB_DRAWN) {
    const value = read(key);
    if (value === null) continue;
    for (const found of value.match(/github/gi) ?? [])
      if (found !== 'GitHub')
        failures.push(`${key} is drawn and writes "${found}" rather than GitHub`);
  }

  // A key that names the folder goes on naming it in every language, and a plural
  // override is one of that key's forms rather than a key of its own: Pluralise
  // hands the overridden form to the same host, so a form carrying no token names
  // no folder at whichever count selected it. The neutral's own keys go through
  // read() first, so a satellite that has dropped one is reported rather than
  // passing for agreement, and the overrides are found by which neutral form each
  // answers for, so a language's second and third forms are covered by the same
  // rule as its first.
  const overridesOfTokenKeys = [...map.keys()]
    .filter((key) => !neutral.has(key) && tokenKeys.has(standsInFor(key, neutral)));
  for (const key of [...tokenKeys, ...overridesOfTokenKeys]) {
    const value = read(key);
    if (value === null) continue;
    if (!value.includes(FOLDER_TOKEN))
      failures.push(`${key} has lost its ${FOLDER_TOKEN} token, so the sentence names no folder`);
  }

  // A BRACKET IS MEASURED AGAINST A NEUTRAL SENTENCE, so a key the neutral does
  // not hold has nothing here to disagree with and is check-resx-parity's stray
  // finding rather than this one. The exception is a plural override, which is a
  // form of a key the neutral does hold: folding it in with the strays would take
  // every inflecting language's overridden forms out of this rule with nothing in
  // the output to say so.
  for (const [key, value] of map) {
    const against = neutral.has(key) ? key : standsInFor(key, neutral);
    if (against === null) continue;

    const { pairs, chars } = bracketCounts(value);
    if (linkKeys.has(against)) {
      if (pairs !== 1 || chars !== 2)
        failures.push(`${key} carries ${pairs} balanced [phrase] in ${chars} bracket(s); `
          + 'exactly one pair is what becomes the link, and none renders the sentence plain');
    } else if (chars > 0) {
      const compared = against === key ? 'the neutral' : `the neutral's ${against}`;
      failures.push(`${key} carries a square bracket and ${compared} carries none, `
        + 'so this language and the neutral disagree about whether the sentence '
        + 'holds a link phrase');
    }
  }

  for (const key of tooltipKeys) {
    const value = read(key);
    if (value === null) continue;
    const { oneSentence, stopped } = judgeTooltip(value);
    if (oneSentence && stopped)
      failures.push(`${key} is a tooltip of one sentence and ends in a full stop`);
  }

  if (failures.length) {
    console.error(`FAIL  ${lang.padEnd(7)} ${failures.join('; ')}`);
    problems.push(...failures.map((f) => `${lang}: ${f}`));
  } else {
    console.log(`clean ${lang.padEnd(7)}`);
  }
}

// The count of problems a LANGUAGE raised, taken before rule 3a's own findings are
// added, because the footer below sends a reader to a generator and an entry this
// file has outlived is fixed in this file.
const perLanguageProblems = problems.length - sourceShapeProblems;

// PRINTED ON EVERY RUN, CLEAN OR NOT, AND WITH THE LEG THAT DID IT. A sentence that
// went unmeasured is not a sentence that passed.
if (heldOut.length) {
  const byEntry = new Map();
  for (const { sentence, legs, lang } of heldOut) {
    const byReason = byEntry.get(sentence) ?? new Map();
    const reason = legs.join(' and ');
    byReason.set(reason, [...(byReason.get(reason) ?? []), lang]);
    byEntry.set(sentence, byReason);
  }
  console.log(`\nHELD OUT OF RULE 3 (${heldOut.length} key-slot(s)), and by which leg:`);
  for (const [sentence, byReason] of [...byEntry].sort())
    for (const [reason, langs] of [...byReason].sort())
      console.log(`  ${sentence}, ${reason}: ${langs.sort().join(', ')}`);
}

// AN ENTRY WITH NOTHING LEFT TO HOLD IS A DECLARATION THIS FILE HAS OUTLIVED, and
// it is reported rather than left, because a rule that has quietly stopped applying
// to anything reads exactly like a rule that is working.
for (const [{ sentence, label }, { read, held }] of heldOutTally)
  if (read > 0 && held === 0)
    problems.push(`${sentence} is declared as quoting ${label} once translated, and no satellite `
      + 'holds it out any longer: in none of them is the value the English it answers for, and in '
      + 'none does the ledger record the slot stale. Move the pair into QUOTES_A_LABEL and drop '
      + 'this entry.');

if (problems.length) {
  console.error(`\nCross-key rules FAILED (${problems.length}):`);
  for (const p of problems) console.error(`  ${p}`);
  if (perLanguageProblems > 0)
    console.error('\nThe translated resx files are generated from'
      + '\nscripts/translations/gen-strings-<code>.mjs and are never hand-edited, so a fix'
      + "\ngoes into that language's generator and the file is regenerated.");
  process.exit(1);
}

console.log(`\nCross-key rules OK: ${LANGS.length} languages, ${MUST_AGREE.length} label/name pairs `
  + `and ${ELABORATES_A_LABEL.length + ELABORATES_A_LABEL_IN_CODE.length} measured by containment `
  + `(${ELABORATES_A_LABEL_IN_CODE.length} of them built in code), `
  + `${tokenKeys.size} keys carrying ${FOLDER_TOKEN}, ${linkKeys.size} carrying a [link phrase], `
  + `${tooltipKeys.size} tooltips judged for a closing full stop, `
  + `${namedInXaml.size} automation names classified (${NAME_IS_THE_LABEL.size} by key identity), `
  + `${csFiles.length} C# files read through Strings bar ${rawAllowed.size} allowed direct.`);
