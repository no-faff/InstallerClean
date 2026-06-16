#!/usr/bin/env node
// Checks Strings.it.resx against the neutral Strings.resx.
//
// Fails (exit 1) if a non-Cli. key is missing a translation, if a key is stray
// (present in it.resx but not the neutral), if a Cli. key is translated (the CLI
// ships English, so those keys are skipped in the satellite), or if a translated
// key's {N} placeholder index set differs from the neutral's. Cli. keys are the
// command-line surface, kept English by a culture pin, so the translator omits
// them; this script enforces that.
//
// Run from the repo root: node scripts/check-resx-parity.mjs
import { readFileSync, existsSync } from 'node:fs';

const dir = 'src/InstallerClean.Core/Resources';
const itPath = `${dir}/Strings.it.resx`;

if (!existsSync(itPath)) {
  console.log('Strings.it.resx not present yet; nothing to check.');
  process.exit(0);
}

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
const italian = parse('Strings.it.resx');
const errors = [];

for (const [key, ph] of neutral) {
  if (key.startsWith('Cli.')) continue; // CLI is English-only; not expected in it.resx
  if (!italian.has(key)) {
    errors.push(`MISSING in it.resx: ${key}`);
    continue;
  }
  const itPh = italian.get(key);
  if (ph.size !== itPh.size || [...ph].some((i) => !itPh.has(i)))
    errors.push(`PLACEHOLDER mismatch ${key}: neutral {${[...ph].sort()}} vs it {${[...itPh].sort()}}`);
}
for (const key of italian.keys()) {
  if (!neutral.has(key)) errors.push(`STRAY in it.resx (not in neutral): ${key}`);
  else if (key.startsWith('Cli.')) errors.push(`Cli. key should not be translated: ${key}`);
}

if (errors.length) {
  console.error(`resx parity FAILED:\n${errors.join('\n')}`);
  process.exit(1);
}
const translated = [...neutral.keys()].filter((k) => !k.startsWith('Cli.')).length;
console.log(`resx parity OK: ${translated} non-Cli keys all translated, placeholder arity matches.`);
