#!/usr/bin/env node
// Fails (exit 1) when a rule that lives BETWEEN two strings, or between a string
// and the code that shows it, is broken in any of the sixteen languages.
//
// Every other check in scripts/ holds one file's structure or one key's sameness
// across files, so none of them can see a rule of this shape. That is where the
// rules the UI depends on live, and they were held by a human reading the
// English resx: the About window's cuppa button showed one phrase and spoke
// another in Japanese and Indonesian across four releases, and the say-thanks
// heading had drifted the same way in Brazilian Portuguese and Vietnamese.
//
// Run from the repo root: node scripts/check-cross-key-rules.mjs
import { readFileSync, existsSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { standsInFor } from './plural-overrides.mjs';
import { readLedger, englishFor, recordedFreshness } from './translation-ledger.mjs';

const RESX_DIR = 'src/InstallerClean.Core/Resources';
const GUI = 'src/InstallerClean';

// Priority order, matching SupportedLanguages.CultureNames. The neutral is
// checked too: en-GB's own pairs were only ever settled by hand, and it is the
// language every satellite was generated from.
const LANGS = ['en-GB', 'zh-Hans', 'ru', 'es', 'ja', 'pt-BR', 'pl', 'tr',
  'ko', 'fr', 'it', 'de', 'id', 'vi', 'uk', 'nl'];

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
  { label: 'Action.BuyMeACuppa', name: 'Automation.BuyMeACuppa.About' },
  { label: 'Action.LeaveStarOnGitHub', name: 'Automation.LeaveStarOnGitHub.About' },
  // Content is a plain string, so the override only restates what WPF derives.
  // It can still drift per language, which is the whole point.
  { label: 'Action.CheckForUpdates', name: 'Automation.CheckForUpdates' },
  // Section headings drawn in SmallCaps: the resx value is pre-uppercased so a
  // screen reader never meets the glyph mapping, and the name is the same words
  // in ordinary case. It exists for the casing alone.
  { label: 'Section.SayThanks', name: 'Automation.SayThanks' },
  { label: 'Section.Registered.Patches', name: 'Automation.Section.Patches' },
  { label: 'Section.Registered.Details', name: 'Automation.Section.ProductDetails' },
  { label: 'Section.Backup.Folder', name: 'Automation.Section.BackupFolder' },
];

// ---------------------------------------------------------------------------
// Rule 2. Every automation name resolved in XAML is classified below, which is
// what keeps rule 1 honest: a new control fails this guard until somebody
// decides which list it belongs in.
//
// Names set from code-behind are out of a static check's reach, being built from
// data or reassembled from the sentence a hyperlink was split out of. The one
// that looks like a label/name split and is not is the splash window's Cancel,
// whose Content is set to the same string on the line above.

// A field whose visible text is a VALUE, named for the field it holds. There is
// no rule to write: a path, a count and a version have nothing in common with
// the words that name them. The Field.* keys label the details panes' values;
// Window.Main.Title names the About window's version box, whose text is the
// version.
const LABELS_A_VALUE = new Set([
  'Automation.BackupFolder',
  'Automation.CompletionErrors',
  'Window.Main.Title',
  'Field.Application', 'Field.Author', 'Field.Comment', 'Field.FileSize',
  'Field.Keywords', 'Field.Reason', 'Field.SigningCertificate',
  'Field.Subject', 'Field.Title',
]);

// One label, several controls: three Cancels and two Details buttons that read
// identically until the name says which is which. These DO label text, so
// equality is the wrong rule and no rule is the wrong answer. The rule they owe
// is containment: WCAG 2.5.3 Label in Name asks that the accessible name
// contain the visible label, so speech input reaches the control by the word
// the user can see. KEEP THE TWO SETS APART. Rule 2 stops a control being
// unclassified and nothing more, so a control filed under the set that measures
// nothing satisfies it and is then never measured against anything.
const ELABORATES_A_LABEL = [
  { label: 'Action.Cancel', name: 'Automation.CancelScan' },
  { label: 'Action.Cancel', name: 'Automation.CancelOperation' },
  { label: 'Action.Cancel', name: 'Automation.CancelStartupScan' },
  { label: 'Action.Details', name: 'Automation.ViewOrphanedFiles' },
  { label: 'Action.Details', name: 'Automation.ViewRegisteredFiles' },
];

// A control whose automation name resolves to THE SAME KEY as the visible
// heading that labels it, through AutomationProperties.LabeledBy pointing at
// that heading plus an explicit Name resolving to the heading's own key.
//
// IT IS EMPTY, AND EMPTY IS SAFE HERE IN A WAY AN EMPTY ALLOWLIST USUALLY IS
// NOT. Emptying a list that SUPPRESSES a check would quietly stop the checking;
// this one only ever ADMITS a key to a classification, and rule 2 below fails on
// any automation name that is in none of the five lists. So an empty list here
// can make the guard stricter and never looser, and the classification stays
// written down for whoever builds that shape.
//
// IT IS NOT MUST_AGREE, AND PUTTING A KEY THERE WOULD BE THIS FILE'S OWN NAMED
// MISTAKE. That list measures whether two keys' values agree in every language.
// One key cannot disagree with itself, so the comparison would pass in all
// sixteen whatever anybody wrote, and the entry would be a control classified
// into a list that measures nothing, which is exactly what the merge above hid.
//
// The rule it owes instead is KEY IDENTITY, checked below: the key must also
// appear as visible Text in the same XAML file. That is not vacuous. Repoint the
// Name at a different key, or delete the heading, and the guard fails and the
// control has to be classified again by whoever did it.
const NAME_IS_THE_LABEL = new Set([]);

// Nothing visible to agree with: an icon-only button, a scroll region, a
// progress bar. The name is the control's only text.
const NO_VISIBLE_LABEL = new Set([
  'Automation.BuyMeACuppa',
  'Automation.ChangeLanguage',
  'Automation.Close',
  'Automation.CloseWindow',
  'Automation.Minimise',
  'Automation.OperationProgress',
  'Automation.ResultLogPreview',
  'Automation.ScanningProgress',
  'Automation.StartupScanProgress',
  'Automation.Scroll.DialogBody',
  'Automation.Scroll.FileDetails',
  'Automation.Scroll.ProductDetails',
  'Automation.Scroll.ResultDetails',
  'Automation.Scroll.ScanResults',
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
// Three sentences that belong here are out for now, and what keeps them out is
// what their satellites hold rather than anything the sentences say.
// Confirm.DeletePermanently.Plural and both of Summary.MissingFromDisk's neutral
// forms carry English in every satellite, either the current neutral value or a
// wording the neutral has since replaced. A sentence still in English quotes an
// English button while the button beside it has been translated, so listing them
// here would fault fifteen languages for what check-still-english and
// check-superseded-english already name, and name in the place the fix goes.
// Rule 3a below carries them with the condition they are out under, so each one
// rejoins this rule the day its language stops holding English for it rather than
// the day somebody remembers to move it.
const QUOTES_A_LABEL = [
  { sentence: 'Body.NotScanned.Why', label: 'Action.Rescan' },
  // Each confirmation dialog's spoken help names both of its own buttons, which
  // is the whole of what it says: "Move puts the unneeded files in the chosen
  // destination folder. Cancel leaves them where they are."
  { sentence: 'Automation.ConfirmMove', label: 'Action.Move' },
  { sentence: 'Automation.ConfirmMove', label: 'Action.Cancel' },
  { sentence: 'Automation.ConfirmDelete', label: 'Action.DeletePermanently' },
  { sentence: 'Automation.ConfirmDelete', label: 'Action.Cancel' },
  { sentence: 'Automation.ConfirmSendResultLog', label: 'Action.SendResultLogConfirm' },
  { sentence: 'Automation.ConfirmSendResultLog', label: 'Action.Cancel' },
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
];

// ---------------------------------------------------------------------------
// Rule 3a. The same rule, on the sentences whose satellites cannot meet it yet.
//
// A satellite still carrying English cannot quote a translated button, so a
// sentence in that state fails rule 3 in every language for a reason that belongs
// to the translation round and to the generator. Listing it above would mean
// reading that failure fifteen times over; leaving it out of the file altogether
// would mean somebody remembering to put it back. It is declared here instead,
// with the condition it is out under, and it rejoins rule 3 language by language
// as that condition lifts.
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
// Held out is either leg. Checked is neither. Every slot held out is printed with
// the leg that held it, and an entry no language holds out any more is reported as
// something to move up, so this list empties itself rather than outliving the
// round it is waiting for.
const QUOTES_A_LABEL_ONCE_TRANSLATED = [
  // An entry here is a pair whose containment cannot be checked until the
  // translation lands. It moves up into rule 3 when it does.
];

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
// keeps the company's own capitalisation, because a reader sees it. Settled by
// ear on Windows. It is also what lets rule 1 compare case-insensitively rather
// than needing an exception carved for the star pill.
const GITHUB_SPOKEN = [
  'Automation.LeaveStarOnGitHub.About',
  'Automation.CheckForUpdates.HelpText',
  'Automation.About.Guide.HelpText',
  'Automation.About.ReportProblem.HelpText',
  'Automation.AutoUpdateCheck.HelpText',
  'Automation.Licence.HelpText',
];
const GITHUB_DRAWN = [
  'Action.LeaveStarOnGitHub',
  'UpdateCheck.Failed.NetworkUnavailable',
  'UpdateCheck.Failed.ServerError',
  'UpdateCheck.Failed.ResponseParseError',
  'UpdateCheck.Failed.Timeout',
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
// Three screens link into the README's safety section, and each link is a
// phrase inside a sentence rather than a line of its own. The resx marks it
// with [square brackets] and CompositionParsing.SplitAtBracketedPhrase turns
// the pair into a Hyperlink as the window is built. A value with no pair
// renders as plain prose, which is the right fallback and is also completely
// silent: a translator who drops the brackets takes a link off a screen and
// nothing anywhere says so. The delete confirmation's is the one that matters
// most, being the reason a modal carries its own at all.
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
// Rule 7. A window title resolved in XAML is not overwritten before first use.
//
// Every dialog here is ShowInTaskbar=False under custom chrome, so Title is
// never painted and exists only for the announcement a screen reader makes when
// the window opens. A dialog that composes its title in the constructor, which
// is the better announcement (the heading and the question, not a category),
// must not also resolve one in XAML: that attribute would be the key's only
// consumer and it is overwritten before the window can show. check-dead-resx-keys
// cannot see it, the key being referenced and the reference dead.

// ---------------------------------------------------------------------------
// Rule 8. A resx value reaches a user through Strings and nothing else.
//
// Rule 6's token is only worth writing because Strings.Get and Strings.Find spend
// it on the way out. Nothing in the language holds that: ResourceManager answers
// any key by name, and a read that goes round the two doors hands a user a literal
// {InstallerFolder} on screen, in a console or through a screen reader. Such a read
// has already existed here once, the satellite-only plural overrides having no
// typed accessor to come through, and it was caught by somebody reading the diff
// rather than by anything that could fail.
//
// The two doors are internal to the Core assembly and so is the manager behind
// them, which is as far as visibility goes: every project in the solution holds
// InternalsVisibleTo, so a compiler cannot tell a sanctioned read from a bypass.
// This can.
//
// Comments come off before the search, so the rule measures what a file DOES and a
// file that merely discusses the manager is not a finding.
const RAW_READ = /\bResourceManager\b/;

// A test may read raw, and these do it deliberately. Every entry says why, because
// the next one is a decision about whether a value can reach a user unspent, and
// that question has an answer rather than a shrug.
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
// The stale-declaration block below fails on an unreadable neutral, but only
// because this file hard-codes key names to check against it, and it says in its
// own message that a stop there leaves the per-language pass unrun. A control that
// names the file is the better answer than a stale-declaration report that sends
// the reader to the wrong place entirely.
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
  const xml = readFileSync(path, 'utf8');
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
// underline a letter inside the word ("Buy me a _cuppa"); ja, ko and zh-Hans
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
// coolvitto's own. A space there is typography
// rather than a word, and it may earn its place in the spoken string by helping
// an engine take github as a word; neither is testable from here, so the
// comparison forces a change to neither string.
const compare = (value, lang) => wording(value).replace(/\s+/g, '').toLocaleLowerCase(lang);

function collectXaml(dir, out = []) {
  for (const name of readdirSync(dir)) {
    if (name === 'bin' || name === 'obj') continue;
    const p = join(dir, name);
    if (statSync(p).isDirectory()) collectXaml(p, out);
    else if (name.endsWith('.xaml')) out.push(p);
  }
  return out;
}

// Built with a forward slash rather than join(), because these paths are compared
// against the ones written by hand in RAW_READ_ALLOWED and CI runs on Windows,
// where join() would produce a separator no entry could ever match.
function collectCs(dir, out = []) {
  for (const name of readdirSync(dir)) {
    if (name === 'bin' || name === 'obj') continue;
    const p = `${dir}/${name}`;
    if (statSync(p).isDirectory()) collectCs(p, out);
    else if (name.endsWith('.cs')) out.push(p);
  }
  return out;
}

// C# with its comments taken out. String literals are left where they are: a
// literal is code, and the quoted forms (verbatim, interpolated, both at once,
// and char literals) have to be walked anyway to know which slashes open a
// comment and which sit inside a path or a URL. Raw string literals are the one
// form not walked; a """ block carrying a // would hide the rest of that line
// from the search, so a bypass could sit inside one unseen.
const codeOnly = (src) => {
  let out = '';
  let i = 0;
  while (i < src.length) {
    const two = src.slice(i, i + 2);
    if (two === '//') {
      while (i < src.length && src[i] !== '\n') i++;
      out += '\n';
      continue;
    }
    if (two === '/*') {
      i += 2;
      while (i < src.length && src.slice(i, i + 2) !== '*/') i++;
      i += 2;
      out += ' ';
      continue;
    }
    // Verbatim: a backslash is a backslash and the only escape is a doubled quote.
    const three = src.slice(i, i + 3);
    const opener = three === '$@"' || three === '@$"' ? 3 : two === '@"' ? 2 : 0;
    if (opener) {
      out += src.slice(i, i + opener);
      i += opener;
      while (i < src.length) {
        if (src[i] === '"' && src[i + 1] === '"') { out += '""'; i += 2; continue; }
        out += src[i];
        i++;
        if (src[i - 1] === '"') break;
      }
      continue;
    }
    // Regular and interpolated strings, and char literals: backslash escapes.
    if (src[i] === '"' || src[i] === "'") {
      const quote = src[i];
      out += src[i];
      i++;
      while (i < src.length) {
        if (src[i] === '\\') { out += src.slice(i, i + 2); i += 2; continue; }
        out += src[i];
        i++;
        if (src[i - 1] === quote) break;
      }
      continue;
    }
    out += src[i];
    i++;
  }
  return out;
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
const xamlFiles = collectXaml(GUI);
const namedInXaml = new Set();
const visibleTextInXaml = {};
for (const file of xamlFiles) {
  const xaml = readFileSync(file, 'utf8');
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
  const namedIn = xamlFiles.filter((file) => readFileSync(file, 'utf8')
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

// --- Rule 7, once: source shape, not language.
for (const file of xamlFiles) {
  const xaml = readFileSync(file, 'utf8');
  const title = xaml.match(/\bTitle="\{loc:Translate ([A-Za-z0-9._]+)\}"/);
  if (!title) continue;
  const codeBehind = `${file}.cs`;
  if (!existsSync(codeBehind)) continue;
  if (/^\s*(this\.)?Title\s*=[^=]/m.test(readFileSync(codeBehind, 'utf8')))
    problems.push(`${file} resolves Title from ${title[1]} and ${file}.cs assigns over it, `
      + 'so the resx value never reaches a user in any language. Drop the XAML attribute; '
      + 'drop the key too unless something else shows it.');
}

// --- Rule 8, once: source shape, not language.
const csFiles = collectCs('src');
const rawAllowed = new Map(RAW_READ_ALLOWED.map((e) => [e.file, e.reason]));
const rawReaders = new Set(
  csFiles.filter((f) => RAW_READ.test(codeOnly(readFileSync(f, 'utf8')))),
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
  console.error(`\n  Rules 1 and 3 to 6 did NOT run: ${LANGS.length} languages unchecked. `
    + 'Fix the declarations and run again before believing anything about the translations.');
  process.exit(1);
}

for (const lang of LANGS) {
  const path = lang === 'en-GB' ? `${RESX_DIR}/Strings.resx` : `${RESX_DIR}/Strings.${lang}.resx`;
  if (!existsSync(path)) {
    problems.push(`${lang}: ${path} is missing.`);
    continue;
  }
  const map = values(path);
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

  for (const { label, name } of ELABORATES_A_LABEL) {
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
// went unmeasured is not a sentence that passed, and a held-out list nobody sees is
// the exclusion this rule was written to stop being invisible.
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
  + `and ${ELABORATES_A_LABEL.length} measured by containment, `
  + `${tokenKeys.size} keys carrying ${FOLDER_TOKEN}, ${linkKeys.size} carrying a [link phrase], `
  + `${namedInXaml.size} automation names classified (${NAME_IS_THE_LABEL.size} by key identity), `
  + `${csFiles.length} C# files read through Strings bar ${rawAllowed.size} allowed direct.`);
