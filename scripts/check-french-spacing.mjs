#!/usr/bin/env node
// Fails (exit 1) when a French resx value has a plain ASCII space (U+0020) before
// a high punctuation mark (! ? : ;) where French typography requires a narrow
// no-break space (U+202F). This is the one mechanically-clean member of the
// spacing-and-punctuation class the localisation audit found: an ASCII space
// there both reads wrong and lets the mark wrap onto the next line on its own.
//
// It also guards a live tooling hazard: the Edit/Write tooling flattens U+202F to
// a plain space, so a French value edited through it silently loses the narrow
// space it needs. This catches exactly that.
//
// SCOPE (deliberate): French only, and only the space-before-punctuation rule.
// The CJK members of the same audit class (ASCII vs full-width parentheses and
// quotes in zh/ja) are NOT linted here: on the current, corrected resx a naive
// "ASCII punctuation next to a CJK character" rule produces ~100 false positives,
// all legitimate (the (_X) access-key accelerators, the ASCII ellipsis, {N}
// placeholders, product names and paths, and the half-width parentheses Japanese
// uses on purpose). Telling those from a real slip needs per-language, per-context
// rules, so that half was assessed and left out rather than shipped noisy.
//
// Run from the repo root: node scripts/check-french-spacing.mjs
import { readFileSync } from 'node:fs';

const FILE = 'src/InstallerClean.Core/Resources/Strings.fr.resx';

const xml = readFileSync(FILE, 'utf8');
const dataRe = /<data\s+name="([^"]+)"[^>]*>[\s\S]*?<value>([\s\S]*?)<\/value>/g;
// A plain ASCII space ( ) immediately before ! ? : ;. The correct U+202F
// and U+00A0 are not in the class, so a properly spaced value is clean.
const badSpaceRe = / [!?:;]/g;

const problems = [];
let m;
while ((m = dataRe.exec(xml)) !== null) {
  const [, key, value] = m;
  let h;
  while ((h = badSpaceRe.exec(value)) !== null) {
    const around = value.slice(Math.max(0, h.index - 20), h.index + 2).replace(/\n/g, ' ');
    problems.push({ key, mark: value[h.index + 1], around });
  }
}

if (problems.length) {
  console.error(`French spacing FAILED (${problems.length}): plain space before a mark that wants U+202F:`);
  for (const p of problems)
    console.error(`  ${p.key}: "...${p.around}" (before '${p.mark}')`);
  console.error('\nReplace the space before ! ? : ; with a narrow no-break space (U+202F), written');
  console.error('with python3/printf (the Edit tool flattens it to a plain space).');
  process.exit(1);
}
console.log(`French spacing OK: no plain ASCII space before ! ? : ; in ${FILE.split('/').pop()}.`);
