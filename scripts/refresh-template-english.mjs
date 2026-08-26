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
// test somebody wrote deliberately. Its own header then says it rewrites "each
// gen-strings-<code>.mjs", which is true only if you already know the template
// is not one. So a flag run left the template on the old English, and a
// translator adding the seventeenth language would have worked from wording the
// app stopped using on 11 August.
//
// THE THIRTEEN KEYS THE TEMPLATE'S SELF-CHECK NAMES ARE NOT A SEPARATE BACKLOG.
// Measured 2026-08-21 at 52c31d63: they are SEVEN of the eight stale keys this
// script's sibling job flags, plus SIX belonging to the coordinator's app-string
// batch, with nothing left over. Cli.EventLogUnavailable is in that eight and
// not in the thirteen, because the template's English for it already matches the
// neutral and its staleness is Japanese-only.
//
// SO THE COUNT GOES FROM THIRTEEN TO SIX AND THAT IS THE TOOL WORKING, NOT A
// HALF-FIX. The remaining six are HELD DELIBERATELY: their English is being
// rewritten and nobody may put a wording into the template that the owner has
// not ruled on. What releases them is the app-string batch, not this script.
// Read six where a document said thirteen and you are reading progress, not a
// regression, and "fixing" the six by inventing English here is the specific
// mistake this paragraph exists to prevent.
//
// Usage (from the repo root):
//   node scripts/refresh-template-english.mjs <Key.Name> [<Key.Name> ...]
//   node scripts/refresh-template-english.mjs --list     what differs, change nothing
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

const differing = [];
for (const [k, english] of neutral) {
  const m = text.match(entryRe(k));
  if (m && m[2] !== esc(english)) differing.push(k);
}

const args = process.argv.slice(2);
if (args.includes('--list') || args.length === 0) {
  for (const k of differing) console.log(`  ${k}`);
  console.log(`TOTALS: ${neutral.size} neutral key(s); ${differing.length} template entr(ies) differ from the current English; ${neutral.size - differing.length} match.`);
  if (args.length === 0) console.error('\nUsage: node scripts/refresh-template-english.mjs <Key.Name> [...] , or --list');
  process.exit(args.length === 0 ? 2 : 0);
}

const unknown = args.filter((k) => !neutral.has(k));
const absent = args.filter((k) => neutral.has(k) && !entryRe(k).test(text));
if (unknown.length || absent.length) {
  if (unknown.length) console.error(`Not in the neutral resx (typo?): ${unknown.join(', ')}`);
  if (absent.length) console.error(`No MAP entry in the template: ${absent.join(', ')}`);
  process.exit(2);
}

const changed = [], already = [];
for (const k of args) {
  const english = esc(neutral.get(k));
  const before = text;
  text = text.replace(entryRe(k), (_m, p1, _body, p3) => p1 + english + p3);
  (text === before ? already : changed).push(k);
}
writeFileSync(TEMPLATE, text, 'utf8');

if (changed.length) console.log('Brought back to the current English:');
for (const k of changed) console.log(`  ${k}`);
for (const k of already) console.log(`  (already current) ${k}`);
// The totals line prints always, beside the list and never instead of it: a
// silent zero over an empty set reads exactly like a clean result.
console.log(`TOTALS: ${args.length} key(s) asked for, ${changed.length} rewritten, ${already.length} already current; ${differing.length - changed.length} template entr(ies) still differ from the neutral and are held for the app-string batch.`);
