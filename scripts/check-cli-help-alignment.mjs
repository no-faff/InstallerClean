#!/usr/bin/env node
// Fails (exit 1) when the two-column command rows of the help screen do not all
// start their description at the same terminal column, in any language the app
// ships.
//
// check-cli-help-width.mjs holds the other half of that layout and states in its
// own header that it does NOT hold this one, because the column is per-language:
// it is set by the longest "installerclean-cli <flag> <METAVAR>" prefix in that
// language, and a translated metavariable is not the width of PATH. Italian's
// PERCORSO and Vietnamese's ĐƯỜNG_DẪN are both wider, so those languages'
// descriptions sit further right and there is no single number any check could
// compare against. What is comparable is a language against itself.
//
// It bites during a translation round rather than at rest, which is why nothing
// caught it before: a round reflags some of the rows and leaves the others
// translated, so the flagged ones carry English padded to English's column while
// their neighbours keep the language's own. Every value is then correct read on
// its own and the screen is ragged. Program.cs prints each with Console.WriteLine
// and pads nothing, so what is in the resx is what lands on the terminal.
//
// A ROW STILL HOLDING THE NEUTRAL'S ENGLISH IS NOT MEASURED, and the reason is
// not tidiness. check-still-english.mjs finds an untranslated key by testing the
// satellite value for EQUALITY with the neutral's, so re-padding an English row
// to its language's column would make the two differ and take that key out of
// that gate's sight: the row would look translated, and the batch would never be
// told to do it. The padding therefore has to be written as the row is
// translated, never before, and this check waits for it. Until then the row is
// check-still-english's, and a language whose rows are all English is reported
// as unmeasurable rather than clean, an empty comparison being indistinguishable
// from an aligned one.
//
// WIDTH IS COUNTED IN TERMINAL COLUMNS, NOT CHARACTERS, for the reason its
// sibling gives: a CJK glyph occupies two cells, so padding a Japanese
// metavariable as though it were ASCII pushes its description two cells out of
// line with every other row. The UAX #11 table and the entity decoding below are
// that script's, duplicated because these scripts share no module; the two move
// together.
//
// Run from the repo root: node scripts/check-cli-help-alignment.mjs
import { readdirSync, readFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';

// Priority order, matching SupportedLanguages.CultureNames. The neutral is
// checked too, and is also this file's control: see below.
const LANGS = ['en-GB', 'zh-Hans', 'ru', 'es', 'ja', 'pt-BR', 'pl', 'tr',
  'ko', 'fr', 'it', 'de', 'id', 'vi', 'uk', 'nl'];

// UAX #11 East Asian Wide (W) and Fullwidth (F): the code points a fixed-pitch
// console draws two cells wide.
const WIDE = [
  [0x1100, 0x115f], [0x2e80, 0x303e], [0x3041, 0x33ff], [0x3400, 0x4dbf],
  [0x4e00, 0x9fff], [0xa000, 0xa4cf], [0xa960, 0xa97f], [0xac00, 0xd7a3],
  [0xf900, 0xfaff], [0xfe10, 0xfe19], [0xfe30, 0xfe6f], [0xff00, 0xff60],
  [0xffe0, 0xffe6], [0x1f300, 0x1f64f], [0x1f900, 0x1f9ff],
  [0x20000, 0x2fffd], [0x30000, 0x3fffd],
];
const columns = (s) => {
  let n = 0;
  for (const ch of s) {
    const cp = ch.codePointAt(0);
    n += WIDE.some(([lo, hi]) => cp >= lo && cp <= hi) ? 2 : 1;
  }
  return n;
};

// The value as the console draws it rather than as the XML holds it. &amp; is
// decoded last so that &amp;lt; stays &lt; rather than becoming <, and the
// installer-folder token is spent because Strings.Get spends it on the way out.
const FOLDER = 'C:\\Windows\\Installer';
const printed = (value) => value
  .replace(/&#(\d+);/g, (_, n) => String.fromCodePoint(Number(n)))
  .replace(/&#x([0-9a-fA-F]+);/g, (_, n) => String.fromCodePoint(parseInt(n, 16)))
  .replaceAll('{InstallerFolder}', FOLDER)
  .replaceAll('&lt;', '<').replaceAll('&gt;', '>').replaceAll('&quot;', '"')
  .replaceAll('&apos;', "'").replaceAll('&amp;', '&');

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
// The controls further down are about the LAYOUT model, not about the reading:
// they ask whether the neutral still yields command rows and whether its own rows
// agree. Neither can speak for a satellite, and a satellite this function failed
// to read reaches them as a file in which every row is missing.
const parseControl = (file, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${file}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this check cannot show it read.');
  process.exit(2);
};

const values = (file) => {
  const xml = readFileSync(`${dir}/${file}`, 'utf8');
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], printed(m[2]));
  // The RETAINED size rather than a match counter, and the difference is real: two
  // entries sharing a key name both match, and the second silently overwrites the
  // first, so the check would go on to reason about one value fewer than the file
  // declares. Comparing what survives against what the file declares catches the
  // dropped entry and the overwritten one in one comparison.
  parseControl(`${dir}/${file}`, xml, map.size);
  return map;
};

// A command row is the exe name, then at most a flag and a metavariable, then
// the gap, then the description. The token count is bounded rather than open so
// that a row whose gap has collapsed to one space fails to parse here instead of
// being measured at the first wide gap inside its own description, which would
// report a column that is not the one on screen.
const ROW = /^(\s+installerclean-cli(?:\s+\S+){0,2}?)(\s{2,})(\S.*)$/;

const file = (lang) => (lang === 'en-GB' ? 'Strings.resx' : `Strings.${lang}.resx`);

const neutral = values('Strings.resx');
// Membership is the neutral's decision, as it is for the folder token and the
// link phrase: whichever Cli.Help.* values are laid out as command rows. A
// hardcoded list here would keep reporting clean about a row the help screen no
// longer has, or miss one it has gained.
const ROW_KEYS = [...neutral].filter(([k, v]) => k.startsWith('Cli.Help.') && ROW.test(v))
  .map(([k]) => k).sort();

// --- The controls, and they are about this file rather than about any language.
//
// A detector that has stopped matching reports every language clean, which reads
// exactly like a screen that is correctly aligned. Two things have to hold before
// any measurement below means anything: the neutral must still yield rows to
// compare (it cannot, if the exe name or the layout changes), and the neutral's
// own rows must already agree, en-GB's help screen being laid out by hand and
// being the shape every translation is padded against. Either failing is a fault
// in this check, not in a translation, and says so.
const control = [];
if (ROW_KEYS.length < 2)
  control.push(`Strings.resx yields ${ROW_KEYS.length} command row(s), so there is nothing to `
    + 'compare. The row pattern in this file no longer matches the help screen.');
else {
  const neutralCols = new Set(ROW_KEYS.map((k) => {
    const m = ROW.exec(neutral.get(k));
    return columns(m[1] + m[2]);
  }));
  if (neutralCols.size !== 1)
    control.push('en-GB\'s own command rows start at '
      + `${[...neutralCols].sort((a, b) => a - b).join(' and ')}, so this file's model of the `
      + 'layout is wrong. The neutral is hand-laid-out and is what the translations are padded '
      + 'against, so fix the model before reading anything below as a translation fault.');
}
if (control.length) {
  console.error(`CLI help alignment: THE CHECK ITSELF IS FAULTY (${control.length}).`);
  for (const c of control) console.error(`  ${c}`);
  process.exit(1);
}

const shipped = new Set(readdirSync(dir));
const problems = [];
let measured = 0;
for (const lang of LANGS) {
  const name = file(lang);
  if (!shipped.has(name)) {
    problems.push(`${lang}: ${name} is missing.`);
    continue;
  }
  const map = values(name);
  const found = [];
  const failures = [];
  let pending = 0;
  for (const key of ROW_KEYS) {
    if (!map.has(key)) {
      // check-resx-parity owns a satellite short of a key. Refuse to measure
      // round it either way rather than report a missing row as agreement.
      failures.push(`${key} is missing from this satellite (run check-resx-parity)`);
      continue;
    }
    const value = map.get(key);
    if (lang !== 'en-GB' && value === neutral.get(key)) { pending++; continue; }
    const m = ROW.exec(value);
    if (!m) {
      failures.push(`${key} is not laid out as a command row: its description does not follow `
        + 'a gap of two or more spaces, so it has no column to share');
      continue;
    }
    found.push([key, columns(m[1] + m[2]), m[3]]);
  }
  // Two rows are the fewest that can disagree, so anything less is a run that
  // measured nothing and must say so rather than pass.
  if (!failures.length && found.length < 2) {
    console.log(`      ${lang.padEnd(7)} not measured: ${found.length} of ${ROW_KEYS.length} rows `
      + `translated, ${pending} still awaiting the round (check-still-english holds those)`);
    continue;
  }
  measured += found.length;
  const starts = [...new Set(found.map(([, c]) => c))].sort((a, b) => a - b);
  if (starts.length > 1) {
    const widest = Math.max(...found.map(([, , d]) => columns(d)));
    failures.push(`the description column is ragged, at ${starts.join(' and ')}: `
      + found.filter(([, c]) => c !== starts.at(-1)).map(([k, c]) => `${k} at ${c}`).join(', ')
      + `, against ${starts.at(-1)} elsewhere. Pad every row to ${starts.at(-1)} with SPACES, `
      + `measured in terminal columns; the longest description here is ${widest}, so the widest `
      + `row would be ${starts.at(-1) + widest} columns`);
  }
  if (failures.length) {
    console.error(`FAIL  ${lang.padEnd(7)} ${failures.join('; ')}`);
    problems.push(...failures.map((f) => `${lang}: ${f}`));
  } else {
    console.log(`clean ${lang.padEnd(7)} column ${starts[0]}`
      + (pending ? `, ${found.length} of ${ROW_KEYS.length} rows measured `
        + `(${pending} awaiting the translation round)` : ''));
  }
}

if (problems.length) {
  console.error(`\nCLI help alignment FAILED (${problems.length}):`);
  for (const p of problems) console.error(`  ${p}`);
  console.error('\nThe column belongs to the language, not to English: it is set by that '
    + "language's\nlongest installerclean-cli prefix, so a row still carrying English padding "
    + 'sits at\nEnglish\'s column and not at its neighbours\'. The translated resx files are '
    + 'generated\nfrom scripts/translations/gen-strings-<code>.mjs and are never hand-edited, so '
    + "a fix\ngoes into that language's generator and the file is regenerated.");
  process.exit(1);
}

// The measured total is reported rather than the possible one: a summary naming
// only the rows that exist reads as a run that checked them all, and during a
// translation round most of them are not this check's to read yet.
console.log(`\nCLI help alignment OK: ${LANGS.length} languages, ${ROW_KEYS.length} command rows `
  + `each, ${measured} of ${LANGS.length * ROW_KEYS.length} measured, every description column `
  + 'shared within its language.');
