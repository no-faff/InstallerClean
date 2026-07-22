#!/usr/bin/env node
// Fails (exit 1) when two controls that are live on the same window at the same
// moment carry the same Alt access key, in any of the fifteen languages. WPF
// answers a duplicate by cycling focus between the matches instead of invoking
// either, so a clash silently retires BOTH accelerators. It only bites a
// keyboard or screen-reader user, and only in the language it happens in, which
// is why it needs a machine to find it: an eye-verify pass runs in one language
// and reads the English accelerator.
//
// The sets below are the load-bearing part, and the part that goes stale: they
// are this file's model of which controls share a window. Moving a button
// between windows without moving its key here leaves the check reporting clean
// about a window it is no longer describing, which is exactly what happened when
// the update button moved from About to the main window's bottom bar.
//
// LIVE MEANS ENABLED, NOT MERELY ON SCREEN. AccessKeyManager only targets an
// element that is both visible and enabled, and the main window's bottom bar is
// IsEnabled-bound to IsMainContentInteractive (MainWindow.xaml), which is false
// whenever the scanning, operating or completion overlay is up. So Re-scan,
// About and Check for updates are dead behind an overlay and cannot clash with
// the Cancel or Done on top of it. Modelling them as live instead would
// manufacture clashes (Italian and Polish both put Alt+A on Cancel and on Check
// for updates) and cost real accelerators to fix something that cannot fire.
//
// A window carrying one access key cannot clash, so the message, splash and two
// details windows are not listed.
//
// Run from the repo root: node scripts/check-accelerators.mjs
import { readFileSync, existsSync } from 'node:fs';

const DIR = 'src/InstallerClean.Core/Resources';

// Priority order, matching SupportedLanguages.CultureNames. The neutral resx is
// checked too: en-GB's own accelerators were only ever settled by hand.
const LANGS = ['en-GB', 'zh-Hans', 'ru', 'es', 'ja', 'pt-BR', 'pl', 'tr',
  'ko', 'fr', 'it', 'de', 'id', 'vi', 'uk'];

// Every control on the window in that state, INCLUDING the ones carrying no
// accelerator today: listing them is what catches a translation that invents one
// and lands it on a letter already spoken for.
const SETS = {
  // Body states of the main window. notScanned and pendingReboot are subsets of
  // results today (a pending reboot only disables Move and Delete; before the
  // first scan the whole action zone is absent), so only results can fail. They
  // are listed separately so a control that later appears in one state alone
  // has a set to be added to.
  'MainWindow-results': ['Action.Details', 'Action.Browse', 'Action.Delete',
    'Action.Move', 'Action.Rescan', 'Action.About', 'Action.CheckForUpdates'],
  'MainWindow-notScanned': ['Action.Rescan', 'Action.About', 'Action.CheckForUpdates'],
  'MainWindow-pendingReboot': ['Action.Details', 'Action.Browse', 'Action.Rescan',
    'Action.About', 'Action.CheckForUpdates'],
  // Overlays. The bar behind each is disabled, so each set is only its own
  // controls; the scanning and operating overlays carry Cancel alone.
  'MainWindow-completion': ['Action.Done', 'Action.SendResultLog', 'Action.ScanAgain'],

  'About': ['About.AutoUpdateCheck', 'Action.Licence', 'Action.LeaveStarOnGitHub',
    'Action.BuyMeACuppa', 'Action.Close'],
  'ConfirmDelete': ['Action.Cancel', 'Action.Delete'],
  'ConfirmMove': ['Action.Cancel', 'Action.Move'],
  'ConfirmSendResultLog': ['Action.Cancel', 'Action.SendResultLogConfirm'],
  'RecycleUnavailable': ['Action.MoveInstead', 'Action.DeletePermanently', 'Action.Cancel'],
  'UpdateAvailable': ['Action.Cancel', 'Action.OpenReleasePage'],
};

// One string, two buttons on the same window (the unneeded and still-needed
// rows each get a Details). An accelerator on it would clash with itself, which
// the per-set comparison cannot see because both buttons read the same key.
const SHARED_LABELS = new Set(['Action.Details']);

const values = (xml) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
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
const neutral = values(readFileSync(`${DIR}/Strings.resx`, 'utf8'));
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
  const map = values(readFileSync(path, 'utf8'));
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

if (problems.length) {
  console.error(`\nAccelerators FAILED (${problems.length}):`);
  for (const p of problems) console.error(`  ${p}`);
  console.error('\nFix a clash by re-picking the marked access key. The translated resx'
    + '\nfiles are generated by maintainer tooling and are never hand-edited, so a'
    + '\nchange that cannot reach that tooling should flag the clash instead.');
  process.exit(1);
}

console.log(`\nAccelerators OK: ${LANGS.length} languages, ${Object.keys(SETS).length} window states, no clashes.`);
