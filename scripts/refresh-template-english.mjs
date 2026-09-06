#!/usr/bin/env node
// refresh-template-english.mjs: bring gen-strings-template.mjs's MAP back to the
// current English neutral, for named keys.
//
// THIS IS NOT flag-retranslation.mjs AND IT IS NOT THE SAME OPERATION. The
// fifteen gen-strings-<code>.mjs files hold TRANSLATIONS, which go stale when
// the English moves and which only a human can bring forward; flagging one sets
// it back to English on purpose, so the generator's self-check fails loudly
// until somebody translates it. gen-strings-template.mjs holds THE ENGLISH
// ITSELF. It is the file a new language is copied from. It cannot be stale in
// the same sense and it cannot be "flagged": it is either the current neutral or
// it is out of date, and the fix is to copy the neutral in. One tool doing both
// under one name is a tool whose next reader misunderstands it.
//
// WHY IT HAD TO EXIST AT ALL. flag-retranslation.mjs discovers its targets with
//   .filter((f) => /^gen-strings-.+\.mjs$/.test(f) && f !== 'gen-strings-template.mjs')
// so it reaches fifteen generators and never the sixteenth, by an explicit name
// test somebody wrote deliberately. So a flag run resets the fifteen and leaves
// the template holding whatever English it held before, and the template is what
// a seventeenth language would be copied from. This is what puts the current
// English into it.
//
// TWO STATES, AND THEY ARE NOT THE SAME JOB. A key whose English was reworded has a
// MAP entry holding the superseded wording, and bringing it forward is a rewrite in
// place. A key the neutral has GAINED has no entry at all, and nothing else puts one
// there: flag-retranslation.mjs appends a new key to the satellite generators and
// skips this file by the name test above, so the entry arrives here or it is typed
// by hand.
// Both are handled, and --list names them separately because a superseded entry is a
// wording somebody may want to look at while an absent one takes the neutral's value
// verbatim.
//
// THE KEYS THE TEMPLATE'S SELF-CHECK NAMES ARE NOT A SEPARATE BACKLOG. They are
// the stale keys this script's sibling job flags, plus the ones whose English is
// still being rewritten, with nothing left over. A key can be in the sibling's
// set and not in the template's: Cli.EventLogUnavailable is one, the template's
// English for it already matching the neutral and its staleness being
// Japanese-only.
//
// SO THE SET SHRINKS AS THE ENGLISH SETTLES, AND THAT IS THE TOOL WORKING RATHER
// THAN A HALF-FIX. What is left is HELD DELIBERATELY: nobody may put a wording
// into the template that has not been ruled on, and what releases those keys is
// the English being settled rather than this script. "Fixing" them by inventing
// English here is the specific mistake this paragraph exists to prevent.
//
// Usage (from the repo root):
//   node scripts/refresh-template-english.mjs <Key.Name> [<Key.Name> ...]
//       each named key's MAP value set to the current English, and an entry
//       appended for a key the MAP does not hold yet
//   node scripts/refresh-template-english.mjs --list
//       what is not at the current English, changing nothing
import { readFileSync, writeFileSync } from 'node:fs';

const NEUTRAL = 'src/InstallerClean.Core/Resources/Strings.resx';
const TEMPLATE = 'scripts/translations/gen-strings-template.mjs';

const reEsc = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
// Escape for a single-line JS template literal, backslash first so the escapes
// added for $, CR and LF are not doubled. Same order as flag-retranslation.mjs.
const esc = (v) => v
  .replace(/\\/g, '\\\\').replace(/`/g, '\\`')
  .replace(/\$/g, () => '\\$').replace(/\r/g, '\\r').replace(/\n/g, '\\n');

// Parse control: this file's regex wants <value> on the same whitespace run as
// <data>, so a <comment> moved above its <value> drops that entry silently. Refuse
// rather than act on a partial read, which matters more here than in a gate: this
// script WRITES to the template.
//
// TWO LEGS, AND IT ONLY HAD ONE. `neutral.size !== rawCount` cannot fire when both
// are zero, so this reported "0 neutral key(s); 0 template entr(ies) differ" over a
// neutral truncated to its XML header and exited 0. `rawCount === 0` is the missing
// half. Counted with <data\b rather than '<data ' so a tab after the tag name is
// not read as an empty file, and neither figure is written down anywhere, so adding
// a string cannot make either go stale.
const neutralXml = readFileSync(NEUTRAL, 'utf8');
const rawCount = (neutralXml.match(/<data\b/g) || []).length;
const neutral = new Map();
{
  const re = /<data name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(neutralXml)) !== null) neutral.set(m[1], m[2]);
}
if (rawCount === 0 || neutral.size !== rawCount) {
  console.error(`PARSE CONTROL FAILED for ${NEUTRAL}: ${rawCount} '<data' occurrence(s), ${neutral.size} parsed. Refusing to act on a partial read.`);
  process.exit(2);
}

let text = readFileSync(TEMPLATE, 'utf8');
const entryRe = (k) => new RegExp("('" + reEsc(k) + "':\\s*`)((?:\\\\.|[^`\\\\])*)(`)");

// The machine-contract keys, which must never gain a MAP entry. The template strips
// the Cli.EventLog* set bar Cli.EventLogUnavailable out of its output, those being
// Application-channel lines an RMM tool greps for fixed English phrases, so an entry
// for one has nothing left to apply to: the template reports a value not applied and
// stops at TEMPLATE HAS ISSUES. The same test is spelled again here so that answer
// comes before anything is written rather than after.
const isMachineCliKey = (k) =>
  k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';

// Every neutral key the template is required to carry, and the two ways it can be
// behind on one. A key lands in at most one list, so the figures the listing prints
// partition the population it read.
const required = [...neutral.keys()].filter((k) => !isMachineCliKey(k));
const superseded = [];
const absent = [];
for (const k of required) {
  const m = text.match(entryRe(k));
  if (m === null) absent.push(k);
  else if (m[2] !== esc(neutral.get(k))) superseded.push(k);
}

// The MAP's close, found as flag-retranslation.mjs finds a generator's: the first
// line-leading "};" after the object opens, values being single-line so no earlier
// one occurs inside it.
const MARKER = 'const MAP = {';
const mapClose = (s) => {
  const open = s.indexOf(MARKER);
  if (open < 0) return -1;
  const rel = s.slice(open).search(/\n};/);
  return rel < 0 ? -1 : open + rel;
};

const args = process.argv.slice(2);
if (args.includes('--list') || args.length === 0) {
  // Labelled, because the two want different things done about them and an
  // unlabelled list of names would read as one backlog.
  for (const k of superseded) console.log(`  superseded  ${k}`);
  for (const k of absent) console.log(`  no entry    ${k}`);
  console.log(`TOTALS: ${required.length} translatable neutral key(s); `
    + `${superseded.length} template entr(ies) hold English the neutral has replaced; `
    + `${absent.length} key(s) have no MAP entry; `
    + `${required.length - superseded.length - absent.length} are at the current English.`);
  if (args.length === 0) console.error('\nUsage: node scripts/refresh-template-english.mjs <Key.Name> [...] , or --list');
  process.exit(args.length === 0 ? 2 : 0);
}

const unknown = args.filter((k) => !neutral.has(k));
const machine = args.filter((k) => neutral.has(k) && isMachineCliKey(k));
if (unknown.length || machine.length) {
  if (unknown.length) console.error(`Not in the neutral resx (typo?): ${unknown.join(', ')}`);
  if (machine.length) {
    console.error(`Machine-contract key(s): ${machine.join(', ')}`);
    console.error('The template strips those out of its output, so an entry for one would have');
    console.error('nothing to apply to and the template would stop at TEMPLATE HAS ISSUES.');
  }
  process.exit(2);
}

// A key with no entry needs one appended, so the place to put it is resolved before
// anything is written: a file whose MAP cannot be found the end of is refused whole
// rather than half-rewritten.
if (args.some((k) => !entryRe(k).test(text)) && mapClose(text) < 0) {
  console.error(`No "${MARKER}" with a line-leading "};" after it in ${TEMPLATE}.`);
  console.error('At least one key needs a new entry and there is nowhere to put one, so nothing');
  console.error('has been written.');
  process.exit(2);
}

const rewritten = [], added = [], already = [];
for (const k of args) {
  const english = esc(neutral.get(k));
  if (!entryRe(k).test(text)) {
    // The close is resolved again for each one, the previous append having moved it.
    const at = mapClose(text);
    text = text.slice(0, at) + `\n  '${k}': \`${english}\`,` + text.slice(at);
    added.push(k);
    continue;
  }
  const before = text;
  text = text.replace(entryRe(k), (_m, p1, _body, p3) => p1 + english + p3);
  (text === before ? already : rewritten).push(k);
}
writeFileSync(TEMPLATE, text, 'utf8');

// PARTITIONED, one heading per kind present, because no sentence is true of both: a
// rewrite replaces a wording that was there, and an append puts a key into a file
// that did not carry it.
if (rewritten.length) console.log('Brought back to the current English:');
for (const k of rewritten) console.log(`  ${k}`);
if (added.length) console.log('Appended to the MAP, holding the current English:');
for (const k of added) console.log(`  ${k}`);
for (const k of already) console.log(`  (already current) ${k}`);
// The totals line prints always, beside the list and never instead of it: a
// silent zero over an empty set reads exactly like a clean result. The last two
// figures are what the template still holds against the neutral, so they answer for
// the whole file rather than for the keys this run was given.
console.log(`TOTALS: ${args.length} key(s) asked for, ${rewritten.length} rewritten, `
  + `${added.length} appended, ${already.length} already current; `
  + `${superseded.length - rewritten.length} entr(ies) elsewhere hold English the neutral `
  + `has replaced and ${absent.length - added.length} key(s) elsewhere have no MAP entry.`);
