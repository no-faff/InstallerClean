#!/usr/bin/env node
// Fails (exit 1) when two controls that are live on the same window at the same
// moment carry the same Alt access key, in any language the app ships. WPF
// answers a duplicate by cycling focus between the matches instead of invoking
// either, so a clash silently retires BOTH accelerators. It only bites a
// keyboard or screen-reader user, and only in the language it happens in, which
// is why it needs a machine to find it: an eye-verify pass runs in one language
// and reads the English accelerator.
//
// The sets below are the load-bearing part, and the part that goes stale: they
// are this file's model of which controls share a window. Moving a button
// between windows without moving its key here leaves the check reporting clean
// about a window it is no longer describing. A control that moves moves here too.
//
// LIVE MEANS ENABLED, NOT MERELY ON SCREEN. AccessKeyManager only targets an
// element that is both visible and enabled, and the main window's bottom bar is
// IsEnabled-bound to IsMainContentInteractive (MainWindow.xaml), which is false
// whenever any of the window's overlays is up. So Re-scan,
// About and Check for updates are dead behind an overlay and cannot clash with
// the Cancel or Done on top of it. Modelling them as live instead would
// manufacture clashes (Italian and Polish both put Alt+A on Cancel and on Check
// for updates) and cost real accelerators to fix something that cannot fire.
//
// A window carrying one access key cannot clash, so the message and two details
// windows are not listed.
//
// It also fails when a string carrying an access key is shown by XAML that does
// not turn the marker into one: a TextBlock's Text, say, draws the underscore and
// the key does nothing. See the consumers pass at the end.
//
// Run from the repo root: node scripts/check-accelerators.mjs
import { readFileSync, existsSync, readdirSync } from 'node:fs';
import { join } from 'node:path';

const DIR = 'src/InstallerClean.Core/Resources';

// Priority order, matching SupportedLanguages.CultureNames. The neutral resx is
// checked too: en-GB's own accelerators were only ever settled by hand.
const LANGS = ['en-GB', 'zh-Hans', 'ru', 'es', 'ja', 'pt-BR', 'pl', 'tr',
  'ko', 'fr', 'it', 'de', 'id', 'vi', 'uk', 'nl', 'zh-Hant'];

// Every control on the window in that state, INCLUDING the ones carrying no
// accelerator today: listing them is what catches a translation that invents one
// and lands it on a letter already spoken for.
const SETS = {
  // Body states of the main window. notScanned and pendingReboot are subsets of
  // results today (a pending reboot only disables Move and Delete; before the
  // first scan the whole action zone is absent), so only results can fail. They
  // are listed separately so a control that later appears in one state alone
  // has a set to be added to.
  // The notice saying a newer version is available can show over any of them, with
  // its Update now.
  'MainWindow-results': ['Action.Details', 'Action.Browse', 'Action.DeletePermanently',
    'Action.Move', 'Action.Rescan', 'Action.About', 'Action.CheckForUpdates',
    'UpdateNotice.UpdateNow'],
  'MainWindow-notScanned': ['Action.Rescan', 'Action.About', 'Action.CheckForUpdates',
    'UpdateNotice.UpdateNow'],
  'MainWindow-pendingReboot': ['Action.Details', 'Action.Browse', 'Action.Rescan',
    'Action.About', 'Action.CheckForUpdates', 'UpdateNotice.UpdateNow'],
  // Overlays. The bar behind each is disabled, so each set is only its own
  // controls. The scanning and operating overlays carry Cancel, and while a line
  // on them names a drive or share the app is waiting for, the button that stops
  // that wait.
  'MainWindow-scanning': ['Action.Cancel', 'Action.StopWaiting'],
  'MainWindow-operating': ['Action.Cancel', 'Action.StopWaiting'],
  // The card the window opens with when the check made as the app starts finds a
  // newer version carries Update now and Continue without updating.
  'MainWindow-updateCard': ['UpdateNotice.UpdateNow', 'UpdateNotice.ContinueWithoutUpdating'],
  // The card shown while an update downloads carries Cancel alone.
  'MainWindow-updating': ['Action.Cancel'],
  // The completion card carries Done with the small tip button under it, or, after a
  // Move or Delete that moved or deleted files, the large tip button over Close
  // without tipping. Never both pairs. Either card, where it is the PC's first, carries
  // the report box under them.
  'MainWindow-completion': ['Action.Done', 'Action.DonateSmall', 'Completion.SendReport'],
  'MainWindow-completionDonate': ['Action.Donate', 'Action.CloseWithoutDonating', 'Completion.SendReport'],

  // The window the startup scan runs in: Cancel, and the same stop-waiting button
  // while its step line names a wait.
  'Splash': ['Action.Cancel', 'Action.StopWaiting'],

  'About': ['About.AutoUpdateCheck', 'Action.Licence', 'Action.LeaveStarOnGitHub',
    'Action.LeaveATip', 'Action.Close'],
  'ConfirmDelete': ['Action.Cancel', 'Action.DeletePermanently'],
  'ConfirmMove': ['Action.Cancel', 'Action.Move'],
};

// One string, two buttons on the same window (the unneeded and still-needed
// rows each get a Details). An accelerator on it would clash with itself, which
// the per-set comparison cannot see because both buttons read the same key.
const SHARED_LABELS = new Set(['Action.Details']);

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
// Every case this check catches today it catches through a DIFFERENT assertion:
// an unreadable neutral fails the stale-set check, an unreadable satellite fails
// as a file short of every key. Both are luck about what this file happens to
// compare and neither is a statement that it read anything, which is why the
// control is here in its own right.
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

// WPF's rule: a doubled underscore is an escape for a literal underscore and
// marks nothing, so the accelerator is the first single underscore left once
// those pairs are consumed left to right. Consuming them is the part that is
// easy to lose: "__X" carries no accelerator while "___X" marks X, and a match
// on the first underscore not followed by another reads the second half of a
// doubled pair as the marker.
const accelerator = (value, lang) => {
  for (let i = 0; i < value.length - 1; i++) {
    if (value[i] !== '_') continue;
    if (value[i + 1] === '_') { i++; continue; }
    return value[i + 1].toLocaleUpperCase(lang);
  }
  return null;
};

const problems = [];
const ALL_KEYS = [...new Set(Object.values(SETS).flat())];

// Validate the sets against the neutral first, once. A key the resx no longer
// holds is a fact about the sets above, not about any language. Dropping it from
// the comparison instead reads a renamed key as "this control has no
// accelerator", so a set quietly loses a control while the run still reports
// clean.
const neutral = values(`${DIR}/Strings.resx`);
for (const [window, keys] of Object.entries(SETS))
  for (const key of keys)
    if (!neutral.has(key))
      problems.push(`${window} lists ${key}, which Strings.resx does not hold. `
        + 'Renamed or removed: update the set above.');
if (problems.length) {
  console.error(`Accelerators FAILED (${problems.length}): the window sets are stale.`);
  for (const p of problems) console.error(`  ${p}`);
  process.exit(1);
}

const neutralHasAccelerator = new Map(
  ALL_KEYS.map((key) => [key, accelerator(neutral.get(key), 'en-GB') !== null]));
for (const key of SHARED_LABELS)
  if (neutralHasAccelerator.get(key))
    problems.push(`en-GB: ${key} labels more than one control on the same window, `
      + 'so it must carry no accelerator.');

for (const lang of LANGS) {
  const path = lang === 'en-GB' ? `${DIR}/Strings.resx` : `${DIR}/Strings.${lang}.resx`;
  if (!existsSync(path)) {
    problems.push(`${lang}: ${path} is missing.`);
    continue;
  }
  const map = values(path);
  const letters = new Map();

  for (const key of ALL_KEYS) {
    if (!map.has(key)) {
      // The sets are known good against the neutral by now, so this is a
      // satellite short of a key rather than a stale set. check-resx-parity is
      // the guard that owns that; refuse to measure round it either way.
      problems.push(`${lang}: ${key} is missing from this satellite (run check-resx-parity).`);
      continue;
    }
    const letter = accelerator(map.get(key), lang);
    letters.set(key, letter);

    // Whether a control carries an accelerator at all is the English original's
    // decision. A translation that drops one leaves a keyboard user without the
    // route English has; one that adds a new one puts an unreviewed letter on
    // the window, where it can land on top of a neighbour's.
    if (neutralHasAccelerator.get(key) !== (letter !== null))
      problems.push(letter === null
        ? `${lang}: ${key} has no accelerator, but en-GB gives it one.`
        : `${lang}: ${key} carries an accelerator (${letter}) that en-GB does not give it.`);
  }

  const clashes = [];
  for (const [window, keys] of Object.entries(SETS)) {
    const seen = new Map();
    for (const key of keys) {
      const letter = letters.get(key);
      if (!letter) continue;
      if (seen.has(letter)) clashes.push(`${window}: ${letter} on both ${seen.get(letter)} and ${key}`);
      else seen.set(letter, key);
    }
  }

  if (clashes.length) {
    console.error(`CLASH ${lang.padEnd(7)} ${clashes.join('; ')}`);
    problems.push(...clashes.map((c) => `${lang}: ${c}`));
  } else {
    console.log(`clean ${lang.padEnd(7)}`);
  }
}

// THE CONSUMERS PASS. A marker becomes an access key only where WPF reads it as
// one: the Text of an AccessText, or the string Content of a Button or CheckBox,
// whose templates here hand it to a ContentPresenter that recognises access keys.
// A Content shown through a ContentTemplate keeps its key only when that template
// is an AccessText bound to the string. Every other consumer is refused rather
// than trusted, so a new kind of site has to be added here on purpose. Strings
// read in C# are outside this pass: what they are shown in is decided in code.
const ACCESS_KEY_CONSUMERS = new Set(['AccessText.Text', 'Button.Content', 'CheckBox.Content']);
const XAML_ROOT = 'src/InstallerClean';
const xamlFiles = (dir) => readdirSync(dir, { withFileTypes: true }).flatMap((e) =>
  e.isDirectory() ? (['bin', 'obj'].includes(e.name) ? [] : xamlFiles(join(dir, e.name)))
    : e.name.endsWith('.xaml') ? [join(dir, e.name)] : []);
const blankComments = (t) => t.replace(/<!--[\s\S]*?-->/g, (m) => m.replace(/[^\n]/g, ' '));
const wrappingTemplates = (text) => {
  const ok = new Set();
  for (const m of text.matchAll(/<DataTemplate\s+x:Key="([^"]+)"\s*>([\s\S]*?)<\/DataTemplate>/g))
    if (/^\s*<AccessText\s[^<>]*Text="\{Binding\}"[^<>]*\/>\s*$/.test(m[2])) ok.add(m[1]);
  return ok;
};
let sitesChecked = 0;
const consumerProblems = [];
for (const file of xamlFiles(XAML_ROOT)) {
  const text = blankComments(readFileSync(file, 'utf8'));
  const templates = wrappingTemplates(text);
  for (const tag of text.matchAll(/<([\w:.]+)\b([^<>]*?)\/?>/g)) {
    const [, element, attributes] = tag;
    const line = text.slice(0, tag.index).split('\n').length;
    for (const attr of attributes.matchAll(/([\w:.]+)="([^"]*)"/g)) {
      for (const use of attr[2].matchAll(/\{loc:Translate ([\w.]+)\}/g)) {
        const key = use[1];
        if (!neutral.has(key) || accelerator(neutral.get(key), 'en-GB') === null) continue;
        sitesChecked++;
        const site = `${file}:${line}`;
        const consumer = `${element}.${attr[1]}`;
        if (!ACCESS_KEY_CONSUMERS.has(consumer)) {
          consumerProblems.push(`${key} carries an access key, and ${site} shows it in ${consumer}, `
            + 'which draws the underscore and leaves the key doing nothing.');
          continue;
        }
        const template = /ContentTemplate="\{StaticResource ([^}]+)\}"/.exec(attributes);
        if (attr[1] === 'Content' && /ContentTemplate=/.test(attributes)
            && !(template && templates.has(template[1])))
          consumerProblems.push(`${key} carries an access key, and ${site} shows it through a ContentTemplate `
            + 'that is not an AccessText bound to the string in the same file.');
      }
    }
  }
}
// The control on the pass itself: a reader that matched nothing would report clean.
if (sitesChecked === 0) {
  console.error('CONSUMERS PASS FAILED: no XAML site showing a string with an access key was found.');
  process.exit(2);
}
console.log(`\nConsumers: ${sitesChecked} XAML site(s) showing a string with an access key checked.`);
if (consumerProblems.length) {
  console.error(`\nAccess keys FAILED (${consumerProblems.length}):`);
  for (const p of consumerProblems) console.error(`  ${p}`);
  console.error('\nShow the string in one of the consumers above, as an AccessText or as the'
    + '\nstring Content of a Button or CheckBox, with a ContentTemplate that is an'
    + '\nAccessText bound to it where the label has to wrap.');
  if (!problems.length) process.exit(1);
}

if (problems.length) {
  console.error(`\nAccelerators FAILED (${problems.length}):`);
  for (const p of problems) console.error(`  ${p}`);
  console.error('\nFix a clash by re-picking the marked access key. The translated resx'
    + '\nfiles are generated by maintainer tooling and are never hand-edited, so a'
    + '\nchange that cannot reach that tooling should flag the clash instead.');
  process.exit(1);
}

console.log(`\nAccelerators OK: ${LANGS.length} languages, ${Object.keys(SETS).length} window states, no clashes, and every access key shown where WPF reads it.`);
