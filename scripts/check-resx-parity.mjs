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

const parse = (file) => {
  const xml = readFileSync(`${dir}/${file}`, 'utf8');
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>[\s\S]*?<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) {
    const placeholders = new Set([...m[2].matchAll(/\{(\d+)\}/g)].map((p) => p[1]));
    map.set(m[1], placeholders);
  }
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
    console.error(`resx parity FAILED for ${file}:\n  ${errors.join('\n  ')}`);
  } else {
    const detail = shipsCli
      ? `${nonCli} non-Cli + ${humanCli} human Cli keys translated`
      : `${nonCli} non-Cli keys translated, CLI not shipped here`;
    console.log(`${file}: OK (${detail}, placeholder arity matches)`);
  }
}

process.exit(failed ? 1 : 0);
