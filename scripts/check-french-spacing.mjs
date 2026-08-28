#!/usr/bin/env node
// Fails (exit 1) on either of two French typography faults in the French resx.
// Both are a space that is missing or of the wrong kind, and neither is visible to
// a reader who is not looking for it.
//
// ONE, a plain ASCII space (U+0020) before a high punctuation mark (! ? : ;) where
// French typography requires a narrow no-break space (U+202F). An ASCII space there
// both reads wrong and lets the mark wrap onto the next line on its own.
//
// TWO, the space INSIDE a pair of guillemets, which French requires and which has
// to be a no-break space for the same reason: the mark belongs to the words it
// encloses and must not be left stranded at the end of a line.
//
// Of the spacing-and-punctuation rules the fifteen languages between them need,
// these are the ones a machine can check without drowning the result in false
// positives.
//
// TWO RULES, BECAUSE FRENCH SETS THIS SPACE IN TWO PLACES AND ONE EDIT REACHES
// BOTH. The same tooling that flattens U+202F before a high punctuation mark
// flattens it inside a guillemet pair, so the two rules answer one condition
// between them, and reading both is what lets a clean report here stand for a
// clean file.
//
// It also guards a live hazard: plenty of editors and text tooling silently
// normalise U+202F to a plain space, so a French value can lose the narrow space
// it needs on any edit that passes through one. This catches exactly that.
//
// SCOPE (deliberate): French only, and only these two rules. French sets a narrow
// no-break space in several other places, the thousands separator among them, and
// none of the rest is linted: a digit group can legitimately be written several
// ways and a rule over them would spend its output on false positives, which is the
// same reasoning that keeps the CJK half out below.
//
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

// The two characters that are a correct space inside guillemets: the narrow
// no-break space French typography asks for, and the ordinary no-break space,
// which is the older answer to the same question and is still what a good deal of
// tooling emits. Either keeps the mark welded to its text, which is the whole
// point, so both pass and everything else fails.
//
// WRITTEN AS ESCAPES ON PURPOSE. The hazard this file guards is editors and text
// tooling silently normalising U+202F to a plain space; a literal U+202F in the
// CHECKER would be normalised by the same edit that broke the resx, and the check
// would then quietly start accepting what it exists to refuse.
const INNER_SPACE = '\u202f\u00a0';

// THE RULE HAS TWO ENDS AND BOTH ARE CHECKED, because a fix aimed at one of them
// leaves the same hole in the other half. The space that matters is the one INSIDE
// the pair, so it is the character AFTER an opening mark and the character BEFORE a
// closing one. A mark at the very edge of a value has no character on its inner
// side at all, which is the "the space was deleted" case at a boundary and fails
// like any other rather than being skipped for want of something to look at.
const GUILLEMETS = { '\u00ab': 1, '\u00bb': -1 };

// What was found where a no-break space was wanted, for the failure line. A plain
// space and a deleted space are different edits with the same cause and read
// identically in a terminal, so the report names which it was rather than printing
// the value and leaving a reader to measure it.
const describeInner = (ch) =>
  ch === undefined ? 'the edge of the value'
    : ch === ' ' ? 'a plain ASCII space'
      : `U+${ch.codePointAt(0).toString(16).toUpperCase().padStart(4, '0')} '${ch}'`;

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
// AND IT CARRIES THE WHOLE WEIGHT HERE, BECAUSE THIS CHECK READS ONE FILE AND
// NAMES IT IN ITS OWN SUCCESS LINE. That line reports the file by name and the
// counts read from it, so the control is what lets it stand for the file's
// contents rather than for the reader having reached them.
const parseControl = (file, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${file}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this check cannot show it read.');
  process.exit(2);
};

const problems = [];
const guillemetProblems = [];
// Counted so the success line can say how many marks the rule actually looked at.
// A resx with no guillemets in it at all is a legitimate state and reads exactly
// like a rule that has stopped matching, and this is the figure that tells them
// apart without pinning a number anything can go stale against.
let guillemets = 0;
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

  for (let i = 0; i < value.length; i++) {
    const side = GUILLEMETS[value[i]];
    if (side === undefined) continue;
    guillemets++;
    const inner = value[i + side];
    if (inner !== undefined && INNER_SPACE.includes(inner)) continue;
    const around = value.slice(Math.max(0, i - 20), i + 22).replace(/\n/g, ' ');
    guillemetProblems.push({ key, mark: value[i], found: describeInner(inner), around });
  }
}

parseControl(FILE, xml, parsed.size);

// The two rules are reported separately and both are printed before the exit, so a
// run that broke both says so once rather than sending somebody back for a second
// look after they have fixed the first.
if (problems.length) {
  console.error(`French spacing FAILED (${problems.length}): plain space before a mark that wants U+202F:`);
  for (const p of problems)
    console.error(`  ${p.key}: "...${p.around}" (before '${p.mark}')`);
  console.error('\nReplace the space before ! ? : ; with a narrow no-break space (U+202F), written');
  console.error('with python3/printf, since many editors normalise it to a plain space.');
}

if (guillemetProblems.length) {
  console.error(`French guillemets FAILED (${guillemetProblems.length}): no no-break space inside the mark:`);
  for (const g of guillemetProblems)
    console.error(`  ${g.key}: "...${g.around}..." (inside '${g.mark}' found ${g.found})`);
  console.error('\nFrench sets a no-break space inside a pair of guillemets: \u00ab\u202fcomme ceci\u202f\u00bb.');
  console.error('Write it with python3/printf as U+202F, since many editors normalise it to a plain');
  console.error('space and some strip it altogether.');
}

if (problems.length || guillemetProblems.length) process.exit(1);

console.log(`French spacing OK: ${parsed.size} value(s) read from ${FILE.split('/').pop()}, no plain ASCII space before ! ? : ;, and a no-break space inside each of the ${guillemets} guillemet mark(s) found.`);
