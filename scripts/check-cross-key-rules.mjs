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
// elaborate ("Cancel scan" over a bare "Cancel"), and a rule clever enough to
// admit those by containment fails in six languages. The members are the names
// that RESTATE their label rather than disambiguate it. Rule 2 is what stops a
// new control joining neither list.
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
  { label: 'Section.Registered.Products', name: 'Automation.Section.Products' },
  { label: 'Section.Registered.Patches', name: 'Automation.Section.Patches' },
  { label: 'Section.Registered.Details', name: 'Automation.Section.ProductDetails' },
  { label: 'Section.Move.Location', name: 'Automation.Section.MoveLocation' },
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

// Named to say something the visible text does not, deliberately.
const DIFFERS_ON_PURPOSE = new Set([
  // One label, several controls: three Cancels and two Details buttons that
  // read identically until the name says which is which.
  'Automation.CancelScan',
  'Automation.CancelOperation',
  'Automation.CancelStartupScan',
  'Automation.ViewOrphanedFiles',
  'Automation.ViewRegisteredFiles',
  // A field whose visible text is a value, named for the field it holds. The
  // Field.* keys label the details panes' values; Window.Main.Title names the
  // About window's version box, whose text is the version.
  'Automation.MoveDestinationFolder',
  'Automation.CompletionErrors',
  'Window.Main.Title',
  'Field.Application', 'Field.Author', 'Field.Comment', 'Field.FileSize',
  'Field.Keywords', 'Field.Reason', 'Field.SigningCertificate',
  'Field.Subject', 'Field.Title',
]);

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
const QUOTES_A_LABEL = [
  { sentence: 'Body.NotScanned.Why', label: 'Action.Rescan' },
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

const values = (xml) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
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
for (const file of xamlFiles) {
  const xaml = readFileSync(file, 'utf8');
  for (const m of xaml.matchAll(/AutomationProperties\.Name="\{loc:Translate ([A-Za-z0-9._]+)\}"/g))
    namedInXaml.add(m[1]);
}
const classified = new Set([
  ...MUST_AGREE.map((p) => p.name), ...DIFFERS_ON_PURPOSE, ...NO_VISIBLE_LABEL,
]);
for (const key of [...namedInXaml].sort())
  if (!classified.has(key))
    stale.push(`${key} names a control in the XAML and is in none of this file's three lists. `
      + 'Decide whether its name restates a visible label (MUST_AGREE), says something the '
      + 'visible text does not (DIFFERS_ON_PURPOSE), or labels a control with no visible text '
      + '(NO_VISIBLE_LABEL).');
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

// --- Rules 1 and 3 to 6, per language.
const neutral = values(readFileSync(`${RESX_DIR}/Strings.resx`, 'utf8'));
const declaredKeys = [
  ...MUST_AGREE.flatMap((p) => [p.label, p.name]),
  ...QUOTES_A_LABEL.flatMap((p) => [p.sentence, p.label]),
  ...MUST_NOT_NAME.map((p) => p.key),
  ...GITHUB_SPOKEN, ...GITHUB_DRAWN,
];
for (const key of [...new Set(declaredKeys)].sort())
  if (!neutral.has(key))
    stale.push(`${key} is named by a rule in this file and Strings.resx does not hold it.`);

// Which keys carry the folder token is the neutral's decision; a satellite that
// has gained or lost one has drifted from it.
const tokenKeys = [...neutral].filter(([, v]) => v.includes(FOLDER_TOKEN)).map(([k]) => k);

if (stale.length) {
  console.error(`Cross-key rules FAILED (${stale.length}): the declarations in this file are stale.`);
  for (const s of stale) console.error(`  ${s}`);
  process.exit(1);
}

for (const lang of LANGS) {
  const path = lang === 'en-GB' ? `${RESX_DIR}/Strings.resx` : `${RESX_DIR}/Strings.${lang}.resx`;
  if (!existsSync(path)) {
    problems.push(`${lang}: ${path} is missing.`);
    continue;
  }
  const map = values(readFileSync(path, 'utf8'));
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

  for (const { sentence, label } of QUOTES_A_LABEL) {
    const body = read(sentence), button = read(label);
    if (body === null || button === null) continue;
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

  for (const key of tokenKeys) {
    const value = read(key);
    if (value === null) continue;
    if (!value.includes(FOLDER_TOKEN))
      failures.push(`${key} has lost its ${FOLDER_TOKEN} token, so the sentence names no folder`);
  }

  if (failures.length) {
    console.error(`FAIL  ${lang.padEnd(7)} ${failures.join('; ')}`);
    problems.push(...failures.map((f) => `${lang}: ${f}`));
  } else {
    console.log(`clean ${lang.padEnd(7)}`);
  }
}

if (problems.length) {
  console.error(`\nCross-key rules FAILED (${problems.length}):`);
  for (const p of problems) console.error(`  ${p}`);
  console.error('\nThe translated resx files are generated from the maintainer generators and'
    + '\nare never hand-edited, so a fix goes into the generator for that language and the'
    + '\nfile is regenerated.');
  process.exit(1);
}

console.log(`\nCross-key rules OK: ${LANGS.length} languages, ${MUST_AGREE.length} label/name pairs, `
  + `${tokenKeys.length} keys carrying ${FOLDER_TOKEN}, ${namedInXaml.size} automation names classified.`);
