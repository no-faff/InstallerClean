#!/usr/bin/env node
// Fails (exit 1) when any line of any Cli.Help.* value, in the neutral resx or a
// satellite, is wider than the help screen's column budget.
//
// The budget is 74 and it is the English text's own: every Cli.Help.* line in
// Strings.resx fits 74 and the longest (Cli.Help.ExitCodeError) is exactly 74,
// which is what makes it the measured budget rather than a number picked here.
// Program.cs prints each value with Console.WriteLine and wraps nothing, so a
// value the console cannot fit is broken across two rows by the terminal, the
// second row starting hard against the left margin under a two-column layout.
// The audience for the CLI is the audience most likely to read that as a broken
// program. Translations are where it goes wrong: nothing in the resx says how
// wide a line may be, and a faithful translation of a line already near the
// budget lands well over it. Russian reached 149 before this guard existed.
//
// WIDTH IS COUNTED IN TERMINAL COLUMNS, NOT CHARACTERS. A CJK glyph occupies two
// cells in a fixed-pitch console, so the Japanese and Korean help lines were the
// widest of the sixteen languages while being among the shortest in characters:
// counting characters would pass, at 101 and 97 columns, the two lines that
// overflow an 80-column console worst. East Asian Wide and Fullwidth (UAX #11)
// count 2, everything else 1, which is the same number as a character count for
// any all-Latin or all-Cyrillic value.
//
// The two-column command lines carry a second rule this guard does NOT enforce,
// because it is per-language: the description column is set by the longest
// "installerclean-cli <flag> <METAVAR>" prefix in that language, so a language
// with a long metavariable (Italian PERCORSO, Vietnamese ĐƯỜNG_DẪN) sits further
// right and has less room for the description. Pad with SPACES to that column
// measured in the same terminal columns; padding a CJK metavariable as though it
// were ASCII pushes its description two cells out of line with every other row.
//
// Run from the repo root: node scripts/check-cli-help-width.mjs
import { readdirSync, readFileSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const BUDGET = 74;

// UAX #11 East Asian Wide (W) and Fullwidth (F) ranges: the code points a
// fixed-pitch console draws two cells wide. Everything else, Latin, Cyrillic,
// Greek, Vietnamese diacritics and the halfwidth kana, is one cell.
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

const parse = (file) => {
  const xml = readFileSync(`${dir}/${file}`, 'utf8');
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  const out = [];
  let m;
  while ((m = re.exec(xml)) !== null)
    if (m[1].startsWith('Cli.Help.')) out.push([m[1], m[2]]);
  return out;
};

const files = ['Strings.resx', ...readdirSync(dir)
  .filter((f) => /^Strings\.[A-Za-z-]+\.resx$/.test(f) && f !== 'Strings.resx')
  .sort()];

const failures = [];
let lines = 0;
for (const file of files) {
  for (const [key, value] of parse(file)) {
    // A value may hold several printed lines; each is measured on its own.
    for (const line of value.split('\n')) {
      lines++;
      const w = columns(line);
      if (w > BUDGET) failures.push({ file, key, w, line });
    }
  }
}

if (failures.length) {
  console.error(`CLI help width FAILED (${failures.length} over ${BUDGET} columns):`);
  for (const f of failures) {
    console.error(`  ${f.file} ${f.key}: ${f.w} columns`);
    console.error(`    ${f.line}`);
  }
  console.error(`\nShorten the wording. The budget is the English text's own maximum, and`);
  console.error(`a line over it wraps in the console instead of sitting in its column.`);
  process.exit(1);
}
console.log(`CLI help width OK: ${lines} Cli.Help lines across ${files.length} resx files, all within ${BUDGET} terminal columns.`);
