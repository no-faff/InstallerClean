#!/usr/bin/env node
// Fails (exit 1) when a satellite resx still carries the English neutral for a
// key that is meant to be translated. This turns the pre-ship translation gate
// from "remember to run the batch" into "CI will not let you forget": when a key
// is added or its English is reworded, flag-retranslation.mjs sets it to English
// in every satellite until the per-language batch translates it, and this gate is
// what makes that visible on every push rather than only to whoever reads a
// generator's self-check.
//
// It is the CI-runnable aggregate of each gen-strings-<code>.mjs self-check. The
// generators live under the gitignored non-repo-files/ and are absent in a CI
// checkout, so this reads only the committed resx and reproduces their "still
// English" test directly: a required key whose satellite value equals the neutral
// value is untranslated, unless it is a legitimate keep (below). Keep the keep
// lists in step with the generators' KEEP_ENGLISH / ALSO_KEEP; adding a language
// means adding its ALSO_KEEP here too.
//
// WIRING: a required CI step (ci.yml). So a new or reworded key must be
// translated before its branch can go green, which is the point: it is the only
// check that catches a satellite still holding English, parity seeing presence
// and arity alone. Adding a key and translating it in the same session keeps
// this green; leaving it for a later batch is what made it red for a fortnight
// in 2026-07.
//
// Run from the repo root: node scripts/check-still-english.mjs
import { readdirSync, readFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';

const parse = (file) => {
  const xml = readFileSync(`${dir}/${file}`, 'utf8');
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>[\s\S]*?<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  return map;
};

// Machine-read CLI lines (the Application-log phrases an RMM tool greps for) are
// forced English at the emit site, so a satellite legitimately keeps them; they
// are not a translation target. Exactly the Cli.EventLog* keys bar
// Cli.EventLogUnavailable, which despite its prefix is an operator-facing warning.
const isMachineCliKey = (key) =>
  key.startsWith('Cli.') && key.includes('EventLog') && key !== 'Cli.EventLogUnavailable';

// Universal keeps: format templates and the product name, byte-identical to
// English in every language on purpose. Mirrors KEEP_ENGLISH in every
// gen-strings-<code>.mjs (they are identical across all fifteen).
const KEEP_ENGLISH = new Set([
  'Window.Main.Title', 'Startup.AlreadyRunningTitle', 'Startup.UnhandledTitle',
  'Automation.ScanResultAnnouncement',
  'Display.Size.GB', 'Display.Size.MB', 'Display.Size.KB', 'Display.Size.B',
  'Display.Elapsed.Ms', 'Display.Elapsed.S',
]);

// Per-language keeps: a word a language deliberately renders identically to
// English (a naturalised loanword such as German "Patches" or "Details"). Mirrors
// each generator's ALSO_KEEP. Languages with an empty ALSO_KEEP are omitted.
const ALSO_KEEP = {
  de: ['Section.Registered.Patches', 'Field.Patches', 'Automation.Section.Patches', 'Action.Details', 'Version.Display'],
  fr: ['Field.Application', 'Version.Display'],
  id: ['Plural.File.Singular', 'Plural.Patch.Singular', 'Field.File'],
  es: ['Plural.Error.Singular'],
  it: ['Field.File', 'Plural.File.Singular', 'Plural.Patch.Singular'],
  nl: ['Section.Registered.Patches', 'Field.Patches', 'Automation.Section.Patches', 'Action.Details', 'Plural.Product.Singular', 'Plural.Patch.Singular', 'Plural.Patch.Plural'],
  'pt-BR': ['Plural.Patch.Singular', 'Plural.Patch.Plural', 'Field.Patches', 'Section.Registered.Patches', 'Automation.Section.Patches'],
};

const neutral = parse('Strings.resx');
const satellites = readdirSync(dir)
  .filter((f) => /^Strings\.[A-Za-z-]+\.resx$/.test(f) && f !== 'Strings.resx')
  .sort();

if (satellites.length === 0) {
  console.log('No satellite resx present yet; nothing to check.');
  process.exit(0);
}

let totalStillEnglish = 0;
const perLang = [];
for (const file of satellites) {
  const code = file.replace(/^Strings\./, '').replace(/\.resx$/, '');
  const sat = parse(file);
  const keep = new Set([...KEEP_ENGLISH, ...(ALSO_KEEP[code] ?? [])]);
  const stillEnglish = [];
  for (const [key, value] of neutral) {
    if (isMachineCliKey(key)) continue;        // legitimately English, forced at emit
    if (keep.has(key)) continue;               // a deliberate keep
    if (!sat.has(key)) continue;               // absent is check-resx-parity's job, not this
    if (sat.get(key) === value) stillEnglish.push(key);
  }
  perLang.push([code, stillEnglish]);
  totalStillEnglish += stillEnglish.length;
}

for (const [code, keys] of perLang) {
  if (keys.length === 0) {
    console.log(`${code}: OK (no untranslated keys)`);
  } else {
    console.log(`${code}: ${keys.length} still English: ${keys.sort().join(', ')}`);
  }
}

if (totalStillEnglish > 0) {
  const distinct = new Set(perLang.flatMap(([, keys]) => keys)).size;
  console.error(`\nStill-English gate: ${totalStillEnglish} untranslated key-slot(s) across ${satellites.length} satellites (${distinct} distinct keys).`);
  console.error('These clear when the per-language translation batch runs (CHANGING-A-TRANSLATED-STRING.md).');
  console.error('This is a required CI step, so translate these before the branch can go green.');
  process.exit(1);
}
console.log(`\nStill-English gate: clean. Every satellite has a real translation for all required keys.`);
