#!/usr/bin/env node
// Fails (exit 1) when the installer's language set and the app's supported-
// language set have drifted apart. Nothing else connects them: the app validates
// a saved language against SupportedLanguages.CultureNames, while the installer
// lists its languages by Inno name in installer/InstallerClean_Languages.iss, and
// the two sets line up only because someone kept them lined up by hand. That makes
// every language added after the first a trap, and the documented recipe for
// adding a language never mentions the installer. This guard is what connects the
// two.
//
// It checks three things:
//   1. Every SupportedLanguages culture has an installer [Languages] entry, and
//      every installer entry maps to a supported culture. Arabic is the one
//      documented asymmetry: it is README-only (no UI, no installer), so it is in
//      neither set and needs no handling here.
//   2. The Inno-name-to-culture map below covers every installer language. A new
//      installer language with no mapping fails here, which is the point: adding
//      a language means teaching this guard the pairing.
//   3. Every installer language carries its full set of per-language override
//      keys in [Messages] and [CustomMessages]. A language added to [Languages]
//      but left without its welcome/finished/app-running/uninstall/launch strings
//      would otherwise fall back to Inno's English mid-wizard.
//
// Run from the repo root: node scripts/check-installer-languages.mjs
import { readFileSync } from 'node:fs';

const SL = 'src/InstallerClean.Core/Helpers/SupportedLanguages.cs';
const ISS = 'installer/InstallerClean_Languages.iss';

// The pairing between an Inno [Languages] name and a SupportedLanguages culture.
// This is the mapping nothing else in the tree holds; keep it in step when a
// language is added. Arabic is deliberately absent (README-only, no installer
// language and no UI culture).
const INNO_TO_CULTURE = {
  english: 'en-GB',
  indonesian: 'id',
  german: 'de',
  spanish: 'es',
  french: 'fr',
  italian: 'it',
  dutch: 'nl',
  polish: 'pl',
  brazilianportuguese: 'pt-BR',
  vietnamese: 'vi',
  turkish: 'tr',
  russian: 'ru',
  ukrainian: 'uk',
  japanese: 'ja',
  chinesesimplified: 'zh-Hans',
  korean: 'ko',
};

// Every installer language overrides these Inno [Messages] and defines these
// [CustomMessages], the same set for all of them. A language missing any would
// drop to Inno's own English for that line.
const REQUIRED_MESSAGE_KEYS = [
  'WelcomeLabel1', 'WelcomeLabel2', 'FinishedHeadingLabel', 'FinishedLabel',
  'ClickFinish', 'SetupAppRunningError', 'UninstallAppRunningError',
];
const REQUIRED_CUSTOM_KEYS = ['UninstallApp', 'LaunchApp'];

const errors = [];

// --- SupportedLanguages.CultureNames (the neutral const plus the array) ---
const csText = readFileSync(SL, 'utf8');
const neutralMatch = csText.match(/Neutral\s*=\s*"([^"]+)"/);
const arrayMatch = csText.match(/CultureNames\s*=\s*new\[\]\s*\{([\s\S]*?)\}/);
if (!neutralMatch || !arrayMatch) {
  console.error(`Could not parse Neutral / CultureNames from ${SL}.`);
  process.exit(1);
}
const cultures = new Set([
  neutralMatch[1],
  ...[...arrayMatch[1].matchAll(/"([^"]+)"/g)].map((m) => m[1]),
]);

// --- installer file (strip the UTF-8 BOM the #include requires; readFileSync
// leaves it as a leading U+FEFF that would otherwise glue onto "[Languages]") ---
const raw = readFileSync(ISS, 'utf8');
const issText = raw.charCodeAt(0) === 0xfeff ? raw.slice(1) : raw;
// Return every line inside a [Section], by walking the file and toggling on each
// [Header] line. A regex boundary is not used on purpose: the section bodies
// carry arbitrary prose (a stray "[" or capital letter would trip a lazy match).
const section = (name) => {
  const out = [];
  let inSection = false;
  for (const line of issText.split('\n')) {
    const header = line.match(/^\[([A-Za-z]+)\]\s*$/);
    if (header) { inSection = header[1] === name; continue; }
    if (inSection) out.push(line);
  }
  return out.join('\n');
};
const langNames = [...section('Languages').matchAll(/Name:\s*"([^"]+)"/g)].map((m) => m[1]);
// [Messages] and [CustomMessages] entries are <name>.<Key>=..., one per line.
const overrideText = `${section('Messages')}\n${section('CustomMessages')}`;
const overrideKeys = new Set(
  [...overrideText.matchAll(/^([A-Za-z]+)\.([A-Za-z0-9]+)\s*=/gm)].map((m) => `${m[1]}.${m[2]}`),
);

// --- 1 + 2: the two sets line up, and every installer name is mapped ---
const installerCultures = new Set();
for (const name of langNames) {
  const culture = INNO_TO_CULTURE[name];
  if (!culture) {
    errors.push(`installer language "${name}" has no culture in this guard's map; add the pairing (and it must also be a SupportedLanguages culture)`);
    continue;
  }
  installerCultures.add(culture);
  if (!cultures.has(culture))
    errors.push(`installer ships "${name}" (culture ${culture}) but SupportedLanguages.CultureNames does not list ${culture}`);
}
for (const culture of cultures) {
  if (!installerCultures.has(culture))
    errors.push(`SupportedLanguages lists ${culture} but no installer [Languages] entry maps to it`);
}

// --- 3: each installer language carries its full override set ---
for (const name of langNames) {
  for (const key of REQUIRED_MESSAGE_KEYS)
    if (!overrideKeys.has(`${name}.${key}`))
      errors.push(`installer language "${name}" is missing [Messages] override ${name}.${key}`);
  for (const key of REQUIRED_CUSTOM_KEYS)
    if (!overrideKeys.has(`${name}.${key}`))
      errors.push(`installer language "${name}" is missing [CustomMessages] entry ${name}.${key}`);
}

if (errors.length) {
  console.error(`Installer language guard FAILED (${errors.length}):`);
  for (const e of errors) console.error(`  ${e}`);
  process.exit(1);
}
console.log(`Installer languages OK: ${langNames.length} installer languages in step with ${cultures.size} supported cultures, each with its full override set.`);
