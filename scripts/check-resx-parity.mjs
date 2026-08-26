#!/usr/bin/env node
// Checks every Strings.<code>.resx satellite against the neutral Strings.resx.
//
// For each satellite, fails (exit 1) if a required key is missing a translation,
// if a key is stray (present in the satellite but not the neutral), or if a
// translated key's {N} placeholder index set differs from the neutral's.
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
// Exception: a satellite may carry Plural.<Noun>.Few / Plural.<Noun>.Many keys
// that the neutral lacks. These are the extra CLDR plural categories some
// languages need (Russian's 2-4 "few" form); they are optional and language-
// specific, so they are allowed as satellite-only keys rather than flagged stray.
//
// Run from the repo root: node scripts/check-resx-parity.mjs
import { readdirSync, readFileSync } from 'node:fs';

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

const arityMismatch = (neutralPh, satPh) =>
  neutralPh.size !== satPh.size || [...neutralPh].some((i) => !satPh.has(i));

// Optional per-language CLDR-category overrides. A language whose plural rules need
// more than the neutral's one/other pair, or a correct n==1 form for a flat count
// string, adds these as satellite-only keys: a noun fragment (Plural.File.Few), a
// whole count template whose noun is baked in (Summary.RegisteredStillUsed.Few), or
// a one-form override for a flat string (Status.RegisteredPackagesFound.One). They
// are read by name via the ResourceManager, never generated into the Designer, so
// they live only in the satellites that use them. Allowed when the base key (its
// .Plural sibling, or the flat key itself) is in the neutral, which ties each
// override to a real string and still catches a typo'd key.
//
// Returns the neutral base key an override inflects (its .Plural sibling, else the
// flat key), or null when the key is not a well-formed override of a real neutral
// key. The base is what the override's {N} arity is validated against below.
const overrideBaseKey = (key) => {
  const m = key.match(/^(.+)\.(?:One|Few|Many)$/);
  if (m === null) return null;
  if (neutral.has(`${m[1]}.Plural`)) return `${m[1]}.Plural`;
  if (neutral.has(m[1])) return m[1];
  return null;
};
const isOptionalPlural = (key) => overrideBaseKey(key) !== null;
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
    if (arityMismatch(ph, sat.get(key)))
      errors.push(`PLACEHOLDER mismatch ${key}: neutral {${[...ph].sort()}} vs satellite {${[...sat.get(key)].sort()}}`);
  }

  for (const key of sat.keys()) {
    if (neutral.has(key)) continue;
    const base = overrideBaseKey(key);
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

console.log(
  `TOTALS: ${satellites.length} satellite(s) checked, ${tally.files} failed, `
  + `${tally.missing} missing, ${tally.placeholder} placeholder mismatch(es), `
  + `${tally.stray} stray, ${tally.override} placeholder override(s).`);

process.exit(failed ? 1 : 0);
