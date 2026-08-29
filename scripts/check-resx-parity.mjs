#!/usr/bin/env node
// Checks every Strings.<code>.resx satellite against the neutral Strings.resx.
//
// For each satellite, fails (exit 1) if a required key is missing a translation,
// if a key is stray (present in the satellite but not the neutral), or if a
// translated key uses a {N} index the neutral does not provide.
//
// THE PLACEHOLDER RULE IS ONE-WAY, BECAUSE THE RUNTIME IS. A satellite naming an
// index the neutral does not provide is handed too few arguments and throws
// FormatException, in that language only, on exactly the screen that uses it. The
// other way round is harmless: string.Format ignores an argument no placeholder
// asks for, so the satellite renders its own sentence and the extra fact is simply
// not in it. That state is the intended one between an English change and the
// translation round it lands in, so it is reported and not failed.
//
// WHAT THIS GATE DOES NOT FAIL ON, AND WHERE THAT GOES INSTEAD. A satellite
// carrying FEWER indices than the neutral is reported rather than failed, and that
// covers two things it cannot tell apart: English that has moved ahead of its
// translation, and a placeholder a translation dropped. Both are listed by key and
// language under NEUTRAL-ONLY below, and both reach check-translation-freshness the
// moment the English moves, so the translation round has them either way.
//
// CLI keys (Cli.*) follow a two-tier rule. A satellite either ships the CLI
// surface or it does not: until it carries its first Cli. key it is skipped for
// CLI (the CLI falls back to neutral English), and once it carries any Cli. key
// it must carry every HUMAN-facing one (a half-translated CLI would render some
// lines in the OS language and some in English). The MACHINE-read keys, the
// Application-channel event-log lines an RMM tool greps for fixed English
// phrases, stay English at runtime via a culture scope at the emit site, so a
// satellite may carry one (ja carries all but one, from coolvitto's
// contribution) or omit it (it.resx omits the lot); either is correct. The machine set is exactly the Cli.EventLog*
// keys minus Cli.EventLogUnavailable, which despite its prefix is an operator-
// facing stdout warning and so is human.
//
// Exception: a satellite may carry a plural override the neutral has not got. An
// override is a key ending .One, .Few or .Many for which the neutral holds the form
// it inflects: the .Singular sibling for a one-form, the .Plural sibling for a few-
// or many-form, or the flat key itself where one neutral string serves every count.
// It can be a noun fragment (Plural.File.Few), a whole count template with its noun
// baked in (Summary.RegisteredStillUsed.Few), or a one-form override for a flat
// string (Status.RegisteredPackagesFound.One). These are the extra CLDR categories
// some languages need, Russian's 2-4 "few" form among them, and a correct n==1 form
// for a count string the neutral keeps flat; they are optional and language-specific,
// so they are allowed as satellite-only keys rather than flagged stray. Which form
// each answers for comes from scripts/lib/plural-overrides.mjs, so this gate and
// check-cross-key-rules measure an override against the same neutral key.
//
// Run from the repo root: node scripts/check-resx-parity.mjs
import { readdirSync, readFileSync } from 'node:fs';
import { standsInFor } from './plural-overrides.mjs';

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
// This gate catches an unreadable file through its own comparison in every case
// but ONE, and the exception is the reason the control is here rather than left
// to luck: when all sixteen files go together an empty set is in parity with an
// empty set, and it reported every satellite OK and exited 0.
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
  while ((m = re.exec(xml)) !== null) {
    const placeholders = new Set([...m[2].matchAll(/\{(\d+)\}/g)].map((p) => p[1]));
    map.set(m[1], placeholders);
  }
  // The RETAINED size rather than a match counter, and the difference is real: two
  // entries sharing a key name both match, and the second silently overwrites the
  // first, so the check would go on to reason about one value fewer than the file
  // declares. Comparing what survives against what the file declares catches the
  // dropped entry and the overwritten one in one comparison.
  parseControl(`${dir}/${file}`, xml, map.size);
  return map;
};

const neutral = parse('Strings.resx');

const isCliKey = (key) => key.startsWith('Cli.');
// Machine-read CLI lines are forced English at the emit site, so a satellite may
// carry or omit them: exactly the Cli.EventLog* keys bar Cli.EventLogUnavailable.
const isMachineCliKey = (key) =>
  isCliKey(key) && key.includes('EventLog') && key !== 'Cli.EventLogUnavailable';
const isHumanCliKey = (key) => isCliKey(key) && !isMachineCliKey(key);

// The failing direction: indices the satellite uses and the neutral does not
// provide. Every one of these is a FormatException waiting for that language.
const unprovidedIndices = (neutralPh, satPh) =>
  [...satPh].filter((i) => !neutralPh.has(i)).sort();

// The reporting direction: indices the neutral provides that this satellite's
// sentence does not use. Harmless at runtime and worth a name, since it is either
// an English change the round has not reached yet or a placeholder a translation
// dropped, and this gate cannot tell those apart.
const unusedIndices = (neutralPh, satPh) =>
  [...neutralPh].filter((i) => !satPh.has(i)).sort();

const satellites = readdirSync(dir)
  .filter((f) => /^Strings\.[A-Za-z-]+\.resx$/.test(f) && f !== 'Strings.resx')
  .sort();

if (satellites.length === 0) {
  console.log('No satellite resx present yet; nothing to check.');
  process.exit(0);
}

let failed = false;
const nonCli = [...neutral.keys()].filter((k) => !isCliKey(k)).length;
const humanCli = [...neutral.keys()].filter(isHumanCliKey).length;

// A COUNT OF WHAT WAS REPORTED, PRINTED BESIDE THE LIST AND NOT INSTEAD OF IT.
// Everything this gate says is a filtered list, and a filtered read of a list cannot
// be checked against anything: a grep that matches nothing and a run that found
// nothing produce the same empty output, and the second is the answer everybody
// hopes for. The totals below are what a reader compares their own count against.
//
// ADDITIVE ONLY. This changes nothing about what passes or fails. The exit code is
// still decided by `failed`, set exactly where it always was.
const tally = { files: 0, missing: 0, placeholder: 0, stray: 0, override: 0 };

// NAMED, NOT COUNTED. This half is reported rather than failed, so the list is its
// whole trace, and a number on its own is compatible with the check having answered
// about nothing. Printed as key and language so it reads as a worklist.
const neutralOnly = [];

for (const file of satellites) {
  const sat = parse(file);
  const errors = [];
  const shipsCli = [...sat.keys()].some(isCliKey);

  for (const [key, ph] of neutral) {
    // A satellite that has not started the CLI surface omits every Cli. key.
    if (isCliKey(key) && !shipsCli) continue;
    // Machine CLI keys are optional even in a Cli-shipping satellite.
    const required = !isMachineCliKey(key);
    if (!sat.has(key)) {
      if (required) errors.push(`MISSING: ${key}`);
      continue;
    }
    const unprovided = unprovidedIndices(ph, sat.get(key));
    if (unprovided.length)
      errors.push(`PLACEHOLDER unprovided ${key}: satellite uses {${unprovided}}, neutral provides {${[...ph].sort()}}`);
    const unused = unusedIndices(ph, sat.get(key));
    if (unused.length) neutralOnly.push(`${file}  ${key}  neutral provides {${unused}} that this value does not use`);
  }

  for (const key of sat.keys()) {
    if (neutral.has(key)) continue;
    // Tying an override to a form the neutral really holds is what tells one from
    // a stray, and what still catches a key whose prefix is a typo.
    const base = standsInFor(key, neutral);
    if (base === null) {
      errors.push(`STRAY (not in neutral): ${key}`);
      continue;
    }
    // A satellite-only override is checked for presence and shape above but its
    // {N} arity is not, because the neutral has no key of that name to compare
    // it to. It is still passed to string.Format with the base key's arguments,
    // so an index the base never provides throws FormatException at runtime for
    // exactly the count that selects this form. Validate the override references
    // no placeholder its base does not.
    const basePh = neutral.get(base);
    const extra = [...sat.get(key)].filter((i) => !basePh.has(i)).sort();
    if (extra.length)
      errors.push(`PLACEHOLDER override ${key}: {${extra}} not provided by base ${base} {${[...basePh].sort()}}`);
  }

  if (errors.length) {
    failed = true;
    tally.files += 1;
    for (const e of errors) {
      const kind = e.startsWith('MISSING') ? 'missing'
        : e.startsWith('STRAY') ? 'stray'
        : e.startsWith('PLACEHOLDER override') ? 'override'
        : 'placeholder';
      tally[kind] += 1;
    }
    console.error(`resx parity FAILED for ${file}:\n  ${errors.join('\n  ')}`);
  } else {
    const detail = shipsCli
      ? `${nonCli} non-Cli + ${humanCli} human Cli keys translated`
      : `${nonCli} non-Cli keys translated, CLI not shipped here`;
    console.log(`${file}: OK (${detail}, placeholder arity matches)`);
  }
}

if (neutralOnly.length) {
  console.log(`\nNEUTRAL-ONLY PLACEHOLDERS (${neutralOnly.length}), reported and not failed:`);
  for (const line of neutralOnly.sort()) console.log(`  ${line}`);
  console.log('Each is either English that has moved ahead of its translation, which the round');
  console.log('clears, or a placeholder a translation dropped. This gate cannot tell them apart,');
  console.log('so it names every one rather than deciding.');
}

console.log(
  `TOTALS: ${satellites.length} satellite(s) checked, ${tally.files} failed, `
  + `${tally.missing} missing, ${tally.placeholder} unprovided placeholder(s), `
  + `${tally.stray} stray, ${tally.override} placeholder override(s), `
  + `${neutralOnly.length} neutral-only placeholder(s) reported.`);

process.exit(failed ? 1 : 0);
