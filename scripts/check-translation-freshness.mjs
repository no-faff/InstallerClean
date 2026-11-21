#!/usr/bin/env node
// check-translation-freshness.mjs: fail when a satellite is still carrying a
// translation of a SUPERSEDED English value.
//
// WHY THIS EXISTS AND WHY NOTHING ELSE CATCHES IT. The other gates on the
// satellites do not ask whether a translation is out of date.
// check-still-english.mjs fires when a satellite value EQUALS the current
// neutral, check-superseded-english.mjs when it equals a FORMER one,
// check-resx-parity.mjs compares key presence and NUMERIC placeholder arity, and
// check-cross-key-rules.mjs enforces named-token and heading rules. A satellite
// holding a TRANSLATION of the previous English of a key whose neutral has since
// been rewritten equals nothing any of them compares it against: the key is
// present, the arity usually matches, and the value is neither the current
// English nor a former one. It passes all of them silently, in every language.
//
// flag-retranslation.mjs is what sets a key's translations back to the English once
// its English has moved, and it acts only when somebody runs it. This is the
// enforcement, asking on every push rather than when somebody remembers.
//
// WHAT A PASS DOES NOT SAY. Which neutral value a given translation was actually
// made from is not recorded anywhere: a generator MAP entry is 'Key': `translation`
// and carries no source English. So this check compares against a LEDGER of what
// was true when each entry was last recorded, and it can only speak about drift
// from that moment forward. It makes NO claim about any translation predating its
// own seed. A key seeded "unverified" is reported as unverified and is not a pass.
//
// DO NOT REPLACE THIS WITH A DATE COMPARISON. The date version of this check asks
// whether a satellite's value last moved before the neutral's did, and the English
// moves for reasons that leave a translation correct. It flags Status.Ready in
// Spanish and Russian, where the neutral says "Ready" and the satellites say
// "Listo" and "Готово", which are correct; flagging destroys two good
// translations for a human to redo. It flags the two Cli.Help lines where the only
// thing that moved was column padding that check-cli-help-alignment.mjs already
// owns. A date is a screen for a human to read, never a gate. flag-retranslation.mjs
// replaces a translation with the English, and a run that writes prints its own way
// back when it finishes.
//
// THE PARSE CONTROL IN readResx IS NOT DEFENSIVE PROGRAMMING AND MUST NOT BE
// SIMPLIFIED INTO A WARNING. This file's regex wants <value> on the same
// whitespace run as <data>, so anything else landing between them drops that
// entry silently: a <comment> moved above its <value> is valid resx, is what the
// Visual Studio editor emits, and costs that entry. A silent zero over an
// incomplete set reads exactly like a clean result, so refusing with exit 2 is
// the correct answer.
//
// IT HAS TWO LEGS AND BOTH ARE LOAD-BEARING. `out.size !== raw` catches a file
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
//   node scripts/check-translation-freshness.mjs            check, exit 1 on a stale,
//                                                           GONE, ORPHANED or
//                                                           UNRECORDED slot
//   node scripts/check-translation-freshness.mjs --record <Key> [<Key> ...]
//                                                           stamp keys as translated now
//   node scripts/check-translation-freshness.mjs --record-unverified <Key> [<Key> ...]
//                                                           stamp keys as never established
//   node scripts/check-translation-freshness.mjs --record-all-current
//                                                           seed every key/language pair
import { readFileSync, writeFileSync, readdirSync } from 'node:fs';
import { standsInFor } from './plural-overrides.mjs';
import { LEDGER, UNVERIFIED, digest, readLedger, englishFor, recordedFreshness } from './translation-ledger.mjs';

const RES = 'src/InstallerClean.Core/Resources';
const NEUTRAL = `${RES}/Strings.resx`;

// Parse a resx into key -> raw <value> body. An entry the regex cannot match is
// still a '<data' occurrence, so a partial read shows as the two counts differing.
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
const ledger = readLedger();

// The English a key answers for, and the freshness of one slot, both come from
// translation-ledger.mjs, so this gate and check-cross-key-rules read the ledger
// the same way. englishFor is what measures an override against the base form it
// inflects rather than against a key the neutral was never going to hold.

// EVERY KEY THIS LANGUAGE HAS A CLAIM TO MAKE ABOUT, and it is three sets rather than
// the neutral's alone. The neutral's keys are what every satellite is measured against.
// A satellite's own overrides are keys the neutral never holds, so a walk over the
// neutral alone would not reach them. And an override this language is STAMPED for is
// walked whether the satellite still holds it or not, which is what lets a deleted one
// be reported as GONE rather than disappearing with the walk that would have found it.
//
// A stamped key that answers for no form the neutral holds is not walked. There is no
// English to measure it against, and no sentence the English holds is left without its
// translation, so it is neither stale nor GONE. The pass over the ledger after the walk
// reports it as ORPHANED.
const keysFor = (sat, lang) => {
  const out = new Set(neutral.keys());
  for (const k of sat.keys())
    if (!neutral.has(k) && standsInFor(k, neutral) !== null) out.add(k);
  for (const [k, langs] of Object.entries(ledger.keys ?? {}))
    if (!neutral.has(k) && langs?.[lang] !== undefined && standsInFor(k, neutral) !== null)
      out.add(k);
  return out;
};

const args = process.argv.slice(2);
const recordIdx = args.indexOf('--record');
const unverifiedIdx = args.indexOf('--record-unverified');
const recordAll = args.includes('--record-all-current');

// A SLOT WRITTEN BY HAND WANTS THE SENTINEL AND NOT A DIGEST. Stamping the
// current English says a translation was made from it; that is the claim the
// digest carries and the whole basis of the drift report. Where a satellite was
// edited directly, the value is there and nothing recorded which English it
// answers, so the honest entry is the one the check already understands as a
// claim nobody has made. The two modes write the same slot and differ only in
// what they write into it. indexOf matches an argument whole, so the longer
// flag is never read as the shorter one.
if (recordIdx !== -1 || unverifiedIdx !== -1 || recordAll) {
  const asUnverified = unverifiedIdx !== -1;
  // --record-all-current TAKES THE OVERRIDES TOO, which the neutral's keys never
  // include. The union is built across every satellite because which language
  // declares which override is that language's own decision.
  //
  // TWO SETS AND NOT ONE, BECAUSE THEY ARE DIFFERENT QUANTITIES. The key list wants
  // DISTINCT KEYS, one per name, and the figure printed at the end wants KEY-SLOTS,
  // one per language that declares it. Reported as one number they would be compared
  // against each other, and the gap between them would read as a shortfall.
  const overrides = new Set();
  const overrideSlots = new Set();
  for (const f of satFiles)
    for (const k of readResx(`${RES}/${f}`).keys())
      if (!neutral.has(k) && standsInFor(k, neutral) !== null) {
        overrides.add(k);
        overrideSlots.add(`${langOf(f)}\u0000${k}`);
      }

  const keys = recordAll
    ? [...neutral.keys(), ...overrides]
    : args.slice((asUnverified ? unverifiedIdx : recordIdx) + 1);

  // A NAMED KEY IS ACCEPTED WHERE THE NEUTRAL HOLDS IT OR WHERE IT ANSWERS FOR A FORM
  // THE NEUTRAL HOLDS. The second is what an override is: satellite-only by
  // construction, so neutral.has is false for every one of them, and a test on
  // neutral.has alone would refuse every override by name.
  const unknown = keys.filter((k) => englishFor(k, neutral) === undefined);
  if (!keys.length || unknown.length) {
    console.error(unknown.length ? `Neither in the neutral resx nor answering for a form that is: ${unknown.join(', ')}` : 'Usage: --record <Key> [<Key> ...] | --record-unverified <Key> [<Key> ...]');
    process.exit(2);
  }
  let stamped = 0;
  let overridesStamped = 0;
  for (const f of satFiles) {
    const lang = langOf(f);
    const sat = readResx(`${RES}/${f}`);
    for (const k of keys) {
      if (!sat.has(k)) continue;
      ledger.keys[k] ??= {};
      // The digest is of the English this key answers for, which for an override is
      // the base form's. See englishFor.
      ledger.keys[k][lang] = asUnverified ? UNVERIFIED : digest(englishFor(k, neutral));
      stamped++;
      if (!neutral.has(k)) overridesStamped++;
    }
  }
  writeFileSync(LEDGER, JSON.stringify(ledger, null, 2) + '\n', 'utf8');
  // BOTH POPULATIONS SIDE BY SIDE. A run that stamped fewer overrides than the
  // satellites hold would report a smaller number and read exactly like a clean
  // run, so the count of overrides available is printed beside the count stamped.
  console.log(`RECORDED: ${keys.length} key(s), ${stamped} key-slot(s) stamped ${asUnverified ? 'as never established' : 'against the current neutral'}, across ${satFiles.length} satellite(s).`);
  console.log(`  of those, ${overridesStamped} override key-slot(s) stamped, out of ${overrideSlots.size} the satellites declare (${overrides.size} distinct override key(s)).`);
  process.exit(0);
}

const stale = [];
const unrecorded = [];
const deleted = [];
const visited = new Set();
let checked = 0, fresh = 0, unverified = 0, absent = 0, notInLedger = 0, overridesWalked = 0;

for (const f of satFiles) {
  const lang = langOf(f);
  const sat = readResx(`${RES}/${f}`);
  for (const key of keysFor(sat, lang)) {
    visited.add(`${lang}\u0000${key}`);
    const recorded = ledger.keys?.[key]?.[lang];
    if (!neutral.has(key)) overridesWalked++;
    // AN ENTRY STANDING OVER A KEY THAT IS NOT THERE IS A TRANSLATION THAT HAS
    // GONE, and the ledger is what tells the two absences apart. A slot is only
    // ever stamped while the satellite holds the key, so a stamp with no key
    // means that language had a translation and no longer does; no stamp means
    // the key has never been in that file. The value reverts to English, which
    // is a correct sentence in the wrong language, so nothing else here has
    // anything to compare.
    if (!sat.has(key)) {
      if (recorded !== undefined) deleted.push({ lang, key });
      absent++;
      continue;
    }
    if (recorded === undefined) { notInLedger++; unrecorded.push({ lang, key }); continue; }
    checked++;
    const state = recordedFreshness(ledger, key, lang, neutral);
    if (state === UNVERIFIED) { unverified++; continue; }
    if (state === 'fresh') { fresh++; continue; }
    stale.push({ lang, key });
  }
}

// EVERY LEDGER ENTRY IS READ, AND ONE THE WALK DID NOT VISIT IS ORPHANED. That is an
// entry for a key the neutral does not hold and that answers for no form it holds,
// such as a key taken out of the English, or an entry for a language with no
// satellite. Either is a claim about a translation this gate cannot check. The test is
// what the walk visited rather than a second statement of which keys it takes, so a
// change to keysFor moves both together.
const satLangs = new Set(satFiles.map(langOf));
const orphaned = [];
for (const [key, langs] of Object.entries(ledger.keys ?? {}))
  for (const lang of Object.keys(langs ?? {}))
    if (!visited.has(`${lang}\u0000${key}`)) orphaned.push({ lang, key });

const byLang = new Map();
for (const s of stale) byLang.set(s.lang, [...(byLang.get(s.lang) || []), s.key]);
for (const [lang, keys] of [...byLang].sort()) {
  console.log(`${lang}: ${keys.length} stale (the English moved since this was translated): ${keys.sort().join(', ')}`);
}

// Members and not a count, on every list, because a number says nothing about
// which language lost which sentence.
const goneByLang = new Map();
for (const d of deleted) goneByLang.set(d.lang, [...(goneByLang.get(d.lang) || []), d.key]);
for (const [lang, keys] of [...goneByLang].sort()) {
  console.log(`${lang}: ${keys.length} GONE (translated once, now absent from this satellite): ${keys.sort().join(', ')}`);
}
const orphanedByLang = new Map();
for (const o of orphaned) orphanedByLang.set(o.lang, [...(orphanedByLang.get(o.lang) || []), o.key]);
for (const [lang, keys] of [...orphanedByLang].sort()) {
  const why = satLangs.has(lang)
    ? 'the English holds no such key, nor a form it answers for'
    : 'no satellite for this language';
  console.log(`${lang}: ${keys.length} ORPHANED (${why}): ${keys.sort().join(', ')}`);
}

// The totals line is printed ALWAYS, beside the filtered list and never instead
// of it. A silent zero over an empty set reads exactly like a clean result.
console.log(
  `TOTALS: ${satFiles.length} satellite(s), ${neutral.size} neutral key(s); ` +
  `${checked} key-slot(s) checked, ${fresh} fresh, ${stale.length} STALE, ${deleted.length} GONE, ${orphaned.length} ORPHANED, ` +
  `${unverified} unverified (recorded as never established), ` +
  `${notInLedger} UNRECORDED (not in the ledger), ${absent} absent from the satellite.`
);
// PRINTED BESIDE THE WALK'S OWN TOTAL AND NEVER INSTEAD OF IT. The overrides are the
// population a walk over the neutral alone misses, so a walk that reached fewer of them
// than the satellites hold would report a smaller number and look exactly like a run
// over a tree that has fewer. The second figure is counted from the files rather than
// from the walk, which is what makes the pair worth reading.
//
// BOTH ARE KEY-SLOTS, LANGUAGE BY LANGUAGE, AND SAYING SO IS THE POINT. A count of
// distinct KEYS beside a count of slots would be two different quantities, and a
// reader would draw a conclusion from the gap between them. Walked can exceed
// declared by exactly the slots a language is stamped for and no longer holds, which
// is the GONE line above.
const declared = new Set();
for (const f of satFiles)
  for (const k of readResx(`${RES}/${f}`).keys())
    if (!neutral.has(k) && standsInFor(k, neutral) !== null) declared.add(`${langOf(f)}\u0000${k}`);
console.log(`OVERRIDES: ${overridesWalked} key-slot(s) walked, ${declared.size} declared across the satellites.`);
// A SLOT THE SATELLITE HOLDS AND THE LEDGER DOES NOT IS UNRECORDED, and fails the run.
// The check can say nothing about it, and nothing about it later either, since a
// translation that goes stale without ever having been stamped is stale against a
// ledger with no entry to compare. So a key added to the satellites is stamped when
// it is: --record once its translations are made, or --record-unverified where nobody
// has established which English they were made from.
const unrecordedByLang = new Map();
for (const u of unrecorded) unrecordedByLang.set(u.lang, [...(unrecordedByLang.get(u.lang) || []), u.key]);
for (const [lang, keys] of [...unrecordedByLang].sort()) {
  console.log(`${lang}: ${keys.length} UNRECORDED (held by the satellite, no ledger entry): ${keys.sort().join(', ')}`);
}
if (unrecorded.length) {
  console.error(`\n${unrecorded.length} key-slot(s) carry no ledger entry, so this check cannot tell when their English moves. Stamp them with --record <Key> once translated, or --record-unverified <Key>.`);
}
// A STALE, GONE, ORPHANED or UNRECORDED slot fails the run, so on a pass every slot a
// satellite holds is in the ledger, and every entry there is either fresh against the
// English or stamped unverified.
process.exit(stale.length || deleted.length || orphaned.length || unrecorded.length ? 1 : 0);
