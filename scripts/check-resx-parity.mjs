#!/usr/bin/env node
// Checks every Strings.<code>.resx satellite against the neutral Strings.resx.
//
// For each satellite, fails (exit 1) if a non-Cli. key is missing a translation,
// if a key is stray (present in the satellite but not the neutral), if a Cli. key
// is translated (the CLI ships English, so those keys are skipped in the
// satellite), or if a translated key's {N} placeholder index set differs from the
// neutral's. Cli. keys are the command-line surface, kept English by a culture
// pin, so the translator omits them; this script enforces that.
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

// Optional per-language plural fragments (CLDR "few"/"many"). A language whose
// plural rules need more than the neutral's one/other pair adds these; they are
// read by name via the ResourceManager, never generated into the Designer, so
// they live only in the satellites that use them.
const isOptionalPlural = (key) => /^Plural\.[A-Za-z]+\.(Few|Many)$/.test(key);

const neutral = parse('Strings.resx');
const satellites = readdirSync(dir)
  .filter((f) => /^Strings\.[A-Za-z-]+\.resx$/.test(f) && f !== 'Strings.resx')
  .sort();

if (satellites.length === 0) {
  console.log('No satellite resx present yet; nothing to check.');
  process.exit(0);
}

let failed = false;
const nonCli = [...neutral.keys()].filter((k) => !k.startsWith('Cli.')).length;

for (const file of satellites) {
  const sat = parse(file);
  const errors = [];

  for (const [key, ph] of neutral) {
    if (key.startsWith('Cli.')) continue; // CLI is English-only; not expected in a satellite
    if (!sat.has(key)) {
      errors.push(`MISSING: ${key}`);
      continue;
    }
    const sPh = sat.get(key);
    if (ph.size !== sPh.size || [...ph].some((i) => !sPh.has(i)))
      errors.push(`PLACEHOLDER mismatch ${key}: neutral {${[...ph].sort()}} vs satellite {${[...sPh].sort()}}`);
  }

  for (const key of sat.keys()) {
    if (neutral.has(key)) {
      if (key.startsWith('Cli.')) errors.push(`Cli. key should not be translated: ${key}`);
    } else if (!isOptionalPlural(key)) {
      errors.push(`STRAY (not in neutral): ${key}`);
    }
  }

  if (errors.length) {
    failed = true;
    console.error(`resx parity FAILED for ${file}:\n  ${errors.join('\n  ')}`);
  } else {
    console.log(`${file}: OK (${nonCli} non-Cli keys translated, placeholder arity matches)`);
  }
}

process.exit(failed ? 1 : 0);
