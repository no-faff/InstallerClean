#!/usr/bin/env node
// Fails (exit 1) when a satellite resx still carries the English neutral for a
// key that is meant to be translated. This turns the pre-ship translation gate
// from "remember to run the batch" into "CI will not let you forget": when a key
// is added or its English is reworded, flag-retranslation.mjs sets it to English
// in every satellite until the per-language batch translates it, and this gate is
// what makes that visible on every push rather than only to whoever reads a
// generator's self-check.
//
// It is the CI-runnable aggregate of each gen-strings-<code>.mjs self-check.
// Nothing in CI runs the generators, and it is the committed resx that ships, so
// this reads those directly and reproduces the generators' "still English" test:
// a required key whose satellite value equals the neutral value is untranslated,
// unless it is a legitimate keep (still-english-keeps.mjs). Keep the keep lists in step with the
// generators' KEEP_ENGLISH / ALSO_KEEP; adding a language means adding its
// ALSO_KEEP here too.
//
// It also holds the satellite-only CLDR plural overrides (Key.One/.Few/.Many),
// which exist in no neutral file and which nothing anywhere used to measure. An
// override is another count form of the sentence its base carries, so the two
// go stale together, and the loop above cannot see one: it walks the neutral's
// keys and an override has no neutral counterpart. Nine languages carry 70, and
// the release that removed the Recycle Bin reworded the base of five of them and
// left five satellites telling the user, in shipped copy, that the files had not
// gone to a bin the app no longer has. Two rules below close it, and they answer
// different questions: whether an override was translated at all, and whether it
// was left behind when its base moved.
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
import {
  ALSO_KEEP, JUDGED_ENGLISH, KEEP_ENGLISH, isMachineCliKey,
} from './still-english-keeps.mjs';

const dir = 'src/InstallerClean.Core/Resources';

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
// AN UNREADABLE FILE IS WHAT THE CONTROL IS FOR. The outer loop walks the
// NEUTRAL's keys, and a key absent from a satellite is check-resx-parity's finding
// rather than this one's, so a satellite that cannot be read is one in which every
// key is absent. Without the control in front of it that arrives as
// "OK (no untranslated keys)", byte for byte what a clean run prints.
const parseControl = (file, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${file}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this check cannot show it read.');
  process.exit(2);
};

const parse = (file) => {
  const xml = readFileSync(`${dir}/${file}`, 'utf8');
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>[\s\S]*?<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  // The RETAINED size rather than a match counter, and the difference is real: two
  // entries sharing a key name both match, and the second silently overwrites the
  // first, so the check would go on to reason about one value fewer than the file
  // declares. Comparing what survives against what the file declares catches the
  // dropped entry and the overwritten one in one comparison.
  parseControl(`${dir}/${file}`, xml, map.size);
  return map;
};

// The keys held in English on purpose, and the English each was judged against, are in
// still-english-keeps.mjs, which check-french-spacing reads as well.

const neutral = parse('Strings.resx');
const satellites = readdirSync(dir)
  .filter((f) => /^Strings\.[A-Za-z-]+\.resx$/.test(f) && f !== 'Strings.resx')
  .sort();

if (satellites.length === 0) {
  console.log('No satellite resx present yet; nothing to check.');
  process.exit(0);
}

// The neutral key an override overrides, by the same rule the generators'
// self-check resolves one with: the <Prefix>.Plural sibling when the neutral has
// it, else the flat key. Null when the neutral holds neither, which is
// check-resx-parity's finding rather than this one.
const overrideBase = (key) => {
  const prefix = key.replace(/\.(?:One|Few|Many)$/, '');
  if (neutral.has(`${prefix}.Plural`)) return `${prefix}.Plural`;
  return neutral.has(prefix) ? prefix : null;
};

const judged = [];
{
  const kept = new Set([...KEEP_ENGLISH, ...Object.values(ALSO_KEEP).flat()]);
  for (const key of [...kept].sort()) {
    if (!Object.hasOwn(JUDGED_ENGLISH, key)) judged.push(`${key} is kept and has no pin in JUDGED_ENGLISH`);
    else if (!neutral.has(key)) judged.push(`${key} is kept and the English no longer holds it`);
    else if (neutral.get(key) !== JUDGED_ENGLISH[key])
      judged.push(`${key} is kept as judged against ${JSON.stringify(JUDGED_ENGLISH[key])}, and the English is now ${JSON.stringify(neutral.get(key))}: read each language keeping it against the new wording, then move the pin`);
  }
  for (const key of Object.keys(JUDGED_ENGLISH))
    if (!kept.has(key)) judged.push(`JUDGED_ENGLISH pins ${key}, which no keep names`);
  console.log(`Keeps: ${kept.size} kept key(s), each pinned to the English it was judged against; ${judged.length} out of step.`);
}

let totalStillEnglish = 0;
let totalStranded = 0;
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
  // The overrides, which the loop above cannot reach.
  const stranded = [];
  for (const key of sat.keys()) {
    if (neutral.has(key) || !/\.(?:One|Few|Many)$/.test(key)) continue;
    const base = overrideBase(key);
    if (base === null) continue;
    const english = neutral.get(base);
    // Untranslated: the override carries the base's English, which is what
    // flag-retranslation.mjs puts there and what a translator replaces.
    if (sat.get(key) === english) { stillEnglish.push(key); continue; }
    // Stranded: the base is flagged and this was left holding the wording the
    // base has just lost, so it is the OLD sentence in a count form nobody
    // looked at. Silent until somebody reads the satellite, and reachable: the
    // category branches pick it at 1, or at 2 to 4 in the Slavic languages.
    if (sat.has(base) && sat.get(base) === english) stranded.push(key);
  }
  perLang.push([code, stillEnglish, stranded]);
  totalStillEnglish += stillEnglish.length;
  totalStranded += stranded.length;
}

for (const [code, keys, stranded] of perLang) {
  if (keys.length === 0 && stranded.length === 0) {
    console.log(`${code}: OK (no untranslated keys)`);
    continue;
  }
  if (keys.length)
    console.log(`${code}: ${keys.length} still English: ${keys.sort().join(', ')}`);
  if (stranded.length)
    console.log(`${code}: ${stranded.length} plural override(s) left on a flagged base: ${stranded.sort().join(', ')}`);
}

if (totalStranded > 0) {
  console.error(`\nStill-English gate: ${totalStranded} plural override(s) hold the wording their base key has just lost.`);
  console.error('Rewrite each in its generator\'s OVERRIDES block, or reset it to the English with');
  console.error('flag-retranslation.mjs so it joins the batch. It is reachable copy either way.');
}
if (totalStillEnglish > 0) {
  const distinct = new Set(perLang.flatMap(([, keys]) => keys)).size;
  console.error(`\nStill-English gate: ${totalStillEnglish} untranslated key-slot(s) across ${satellites.length} satellites (${distinct} distinct keys).`);
  console.error('These clear when each is translated in its gen-strings-<code>.mjs MAP and');
  console.error('the satellites are regenerated.');
  console.error('This is a required CI step, so translate these before the branch can go green.');
}
if (judged.length) {
  console.error('\nStill-English gate: a kept key and the English it was judged against are out of step.');
  for (const j of judged) console.error(`  ${j}`);
}
if (totalStillEnglish > 0 || totalStranded > 0 || judged.length) process.exit(1);
console.log(`\nStill-English gate: clean. Every satellite has a real translation for all required keys,`);
console.log(`including its plural overrides.`);
