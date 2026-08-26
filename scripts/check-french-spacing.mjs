#!/usr/bin/env node
// Fails (exit 1) when a French resx value has a plain ASCII space (U+0020) before
// a high punctuation mark (! ? : ;) where French typography requires a narrow
// no-break space (U+202F). An ASCII space there both reads wrong and lets the
// mark wrap onto the next line on its own. Of the spacing-and-punctuation rules
// the fifteen languages between them need, this is the one a machine can check
// without drowning the result in false positives.
//
// It also guards a live hazard: plenty of editors and text tooling silently
// normalise U+202F to a plain space, so a French value can lose the narrow space
// it needs on any edit that passes through one. This catches exactly that.
//
// SCOPE (deliberate): French only, and only the space-before-punctuation rule.
// The CJK equivalents (ASCII vs full-width parentheses and quotes in zh/ja) are
// NOT linted here: on a correct resx a naive "ASCII punctuation next to a CJK
// character" rule produces ~100 false positives, all legitimate (the (_X)
// access-key accelerators, the ASCII ellipsis, {N} placeholders, product names
// and paths, and the half-width parentheses Japanese uses on purpose). Telling
// those from a real slip needs per-language, per-context rules, so that half was
// assessed and left out rather than shipped noisy.
//
// Run from the repo root: node scripts/check-french-spacing.mjs
import { readFileSync } from 'node:fs';

const FILE = 'src/InstallerClean.Core/Resources/Strings.fr.resx';

const xml = readFileSync(FILE, 'utf8');
const dataRe = /<data\s+name="([^"]+)"[^>]*>[\s\S]*?<value>([\s\S]*?)<\/value>/g;
// A plain ASCII space ( ) immediately before ! ? : ;. The correct U+202F
// and U+00A0 are not in the class, so a properly spaced value is clean.
const badSpaceRe = / [!?:;]/g;

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
// This check reads ONE file and names it in its own success line, which is what
// made it the round's worst instance: with every entry of Strings.fr.resx made
// unreadable it printed "French spacing OK: no plain ASCII space before ! ? : ; in
// Strings.fr.resx", byte for byte the clean run's output, and exited 0.
const parseControl = (file, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${file}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this check cannot show it read.');
  process.exit(2);
};

const problems = [];
const parsed = new Set();
let m;
while ((m = dataRe.exec(xml)) !== null) {
  const [, key, value] = m;
  parsed.add(key);
  let h;
  while ((h = badSpaceRe.exec(value)) !== null) {
    const around = value.slice(Math.max(0, h.index - 20), h.index + 2).replace(/\n/g, ' ');
    problems.push({ key, mark: value[h.index + 1], around });
  }
}

parseControl(FILE, xml, parsed.size);

if (problems.length) {
  console.error(`French spacing FAILED (${problems.length}): plain space before a mark that wants U+202F:`);
  for (const p of problems)
    console.error(`  ${p.key}: "...${p.around}" (before '${p.mark}')`);
  console.error('\nReplace the space before ! ? : ; with a narrow no-break space (U+202F), written');
  console.error('with python3/printf, since many editors normalise it to a plain space.');
  process.exit(1);
}
console.log(`French spacing OK: ${parsed.size} value(s) read from ${FILE.split('/').pop()}, no plain ASCII space before ! ? : ;.`);
