#!/usr/bin/env node
// check-translation-freshness.mjs: fail when a satellite is still carrying a
// translation of a SUPERSEDED English value.
//
// WHY THIS EXISTS AND WHY NOTHING ELSE CATCHES IT. Three gates guard the
// satellites and none of them asks whether a translation is out of date.
// check-still-english.mjs fires when a satellite value EQUALS the current
// neutral, check-resx-parity.mjs compares key presence and NUMERIC placeholder
// arity, and check-cross-key-rules.mjs enforces named-token and heading rules.
// A satellite holding the PREVIOUS English of a key whose neutral has since
// been rewritten equals nothing any of them compares it against: the key is
// present, the arity usually matches, and the value is not the current English.
// It passes all three silently, in every language, for as long as nobody looks.
//
// flag-retranslation.mjs's own header has said so since it was written: "a
// STALE translation (the old wording of a key whose English changed) passes it
// silently". The mechanism was understood. What was missing was enforcement:
// the tool has to be RUN when a neutral value moves, and when nobody ran it
// there was nothing to notice. This is that enforcement.
//
// WHAT IT CANNOT DO, STATED HERE SO NOBODY READS A PASS AS MORE THAN IT IS.
// Which neutral value a given translation was actually made from is not
// recorded anywhere: a generator MAP entry is 'Key': `translation` and carries
// no source English. So this check compares against a LEDGER of what was true
// when each entry was last recorded, and it can only speak about drift from
// that moment forward. It makes NO claim about any translation predating its
// own seed. A key seeded "unverified" is reported as unverified and is not a
// pass.
//
// DO NOT REPLACE THIS WITH A DATE COMPARISON, AND THE NUMBER IS WHY. The cheap
// version of this check asks whether a satellite's value last moved before the
// neutral's did. Over the four pre-August keys that comparison flags, it reports
// one genuine key-slot against thirteen false positives. It flags Status.Done in
// Spanish and Russian, where the neutral says "Ready" and the satellites say
// "Listo" and "Готово", which are correct; flagging destroys two good
// translations for a human to redo. It flags the two Cli.Help lines in seven
// languages, where the only thing that moved was column padding that
// check-cli-help-alignment.mjs already owns. A date is a screen for a human to
// read, never a gate. flag-retranslation.mjs is destructive with no undo.
//
// THE PARSE CONTROL ABOVE readResx IS NOT DEFENSIVE PROGRAMMING AND MUST NOT BE
// SIMPLIFIED INTO A WARNING. This file's regex wants <value> on the same
// whitespace run as <data>, so anything else landing between them drops that
// entry silently: a <comment> moved above its <value> is valid resx, is what the
// Visual Studio editor emits, and costs one entry of the 386. A silent zero over
// an incomplete set reads exactly like a clean result, so refusing with exit 2 is
// the correct answer.
//
// IT HAS TWO LEGS AND BOTH ARE LOAD-BEARING. `parsed !== raw` catches a neutral
// the regex read only part of; `raw === 0` catches one it found no entries in at
// all, which the first cannot see, both of its counts being zero. Neither figure
// is written down, so adding a string cannot make either go stale.
//
// AND THE LEDGER IS TRACKED, IN THE REPOSITORY, DELIBERATELY. A gate reads what
// CI checks out, so the ledger has to sit where CI can read it. Anything kept
// outside the repository can be a working record and can never be a gate. Do not
// move this file out of it.
//
// Usage (from the repo root):
//   node scripts/check-translation-freshness.mjs            check, exit 1 on stale
//   node scripts/check-translation-freshness.mjs --record <Key> [<Key> ...]
//                                                           stamp keys as translated now
//   node scripts/check-translation-freshness.mjs --record-all-current
//                                                           seed every key/language pair
import { readFileSync, writeFileSync, readdirSync, existsSync } from 'node:fs';
import { createHash } from 'node:crypto';

const RES = 'src/InstallerClean.Core/Resources';
const NEUTRAL = `${RES}/Strings.resx`;
const LEDGER = 'scripts/translations/translation-provenance.json';
const UNVERIFIED = 'unverified';

const digest = (s) => createHash('sha256').update(s, 'utf8').digest('hex').slice(0, 16);

// Parse a resx into key -> raw <value> body. Controlled against a raw count of
// '<data ' occurrences, because a regex requiring <value> on the same line as
// <data> silently drops every multi-line entry and this file has 19 of them.
function readResx(path) {
  const xml = readFileSync(path, 'utf8');
  // <data\b rather than '<data ' so a tab after the tag name is not counted as a
  // file with no entries, which would fail the raw === 0 leg over a readable file.
  const raw = (xml.match(/<data\b/g) || []).length;
  const out = new Map();
  const re = /<data name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) out.set(m[1], m[2]);
  if (raw === 0 || out.size !== raw) {
    console.error(`PARSE CONTROL FAILED for ${path}: ${raw} '<data' occurrence(s), ${out.size} parsed. Refusing to report on a partial read.`);
    process.exit(2);
  }
  return out;
}

const neutral = readResx(NEUTRAL);
const satFiles = readdirSync(RES).filter((f) => /^Strings\.[A-Za-z-]+\.resx$/.test(f)).sort();
const langOf = (f) => f.match(/^Strings\.([A-Za-z-]+)\.resx$/)[1];
const ledger = existsSync(LEDGER) ? JSON.parse(readFileSync(LEDGER, 'utf8')) : { keys: {} };

const args = process.argv.slice(2);
const recordIdx = args.indexOf('--record');
const recordAll = args.includes('--record-all-current');

if (recordIdx !== -1 || recordAll) {
  const keys = recordAll ? [...neutral.keys()] : args.slice(recordIdx + 1);
  const unknown = keys.filter((k) => !neutral.has(k));
  if (!keys.length || unknown.length) {
    console.error(unknown.length ? `Not in the neutral resx: ${unknown.join(', ')}` : 'Usage: --record <Key> [<Key> ...]');
    process.exit(2);
  }
  let stamped = 0;
  for (const f of satFiles) {
    const lang = langOf(f);
    const sat = readResx(`${RES}/${f}`);
    for (const k of keys) {
      if (!sat.has(k)) continue;
      ledger.keys[k] ??= {};
      ledger.keys[k][lang] = digest(neutral.get(k));
      stamped++;
    }
  }
  writeFileSync(LEDGER, JSON.stringify(ledger, null, 2) + '\n', 'utf8');
  console.log(`RECORDED: ${keys.length} key(s), ${stamped} key-slot(s) stamped against the current neutral, across ${satFiles.length} satellite(s).`);
  process.exit(0);
}

const stale = [];
let checked = 0, fresh = 0, unverified = 0, absent = 0, notInLedger = 0;

for (const f of satFiles) {
  const lang = langOf(f);
  const sat = readResx(`${RES}/${f}`);
  for (const [key, englishNow] of neutral) {
    if (!sat.has(key)) { absent++; continue; }
    const recorded = ledger.keys?.[key]?.[lang];
    if (recorded === undefined) { notInLedger++; continue; }
    checked++;
    if (recorded === UNVERIFIED) { unverified++; continue; }
    if (recorded === digest(englishNow)) { fresh++; continue; }
    stale.push({ lang, key });
  }
}

const byLang = new Map();
for (const s of stale) byLang.set(s.lang, [...(byLang.get(s.lang) || []), s.key]);
for (const [lang, keys] of [...byLang].sort()) {
  console.log(`${lang}: ${keys.length} stale (the English moved since this was translated): ${keys.sort().join(', ')}`);
}

// The totals line is printed ALWAYS, beside the filtered list and never instead
// of it. A silent zero over an empty set reads exactly like a clean result, and
// this project has been caught by that shape twice.
console.log(
  `TOTALS: ${satFiles.length} satellite(s), ${neutral.size} neutral key(s); ` +
  `${checked} key-slot(s) checked, ${fresh} fresh, ${stale.length} STALE, ` +
  `${unverified} unverified (recorded as never established), ` +
  `${notInLedger} not in the ledger (no claim made), ${absent} absent from the satellite.`
);
if (notInLedger > 0) {
  console.log(`NOTE: ${notInLedger} key-slot(s) carry no ledger entry, so this run says NOTHING about them. It reports drift from the seed forward and makes no claim about any translation predating it.`);
}
process.exit(stale.length ? 1 : 0);
