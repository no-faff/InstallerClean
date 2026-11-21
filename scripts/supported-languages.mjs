// supported-languages.mjs: the languages the app ships, for the checks in this folder
// that read every language's strings.
//
// supportedLanguages() returns { neutral, cultures, satellites }: the neutral culture
// from SupportedLanguages.Neutral, every culture in SupportedLanguages.CultureNames in
// its order, the neutral among them, and the cultures with a Strings.<culture>.resx,
// in the same order. A check reading cultures reads a language added to the app with
// no change of its own.
//
// The two sources have to agree: every culture in CultureNames but the neutral has a
// satellite resx, and every satellite resx has a culture in CultureNames. Where they
// do not, or where either cannot be read, the run stops with exit 2, naming what it
// could not reconcile, since a check reading one of them would leave a language out
// and still print a pass.
import { readdirSync, readFileSync } from 'node:fs';
import { readCSharp } from './csharp-source.mjs';

export const SUPPORTED = 'src/InstallerClean.Core/Helpers/SupportedLanguages.cs';
export const RESX_DIR = 'src/InstallerClean.Core/Resources';

const refuse = (why) => {
  console.error(`supported-languages: REFUSING: ${why}`);
  process.exit(2);
};

// A C# regular string literal's value. The culture names hold no escapes, so one
// that does is refused rather than decoded.
const literal = (text) => {
  if (text.includes('\\')) refuse(`${SUPPORTED}: the culture name ${text} holds an escape`);
  return text.slice(1, -1);
};

export function supportedLanguages() {
  let code;
  try {
    code = readCSharp(readFileSync(SUPPORTED, 'utf8')).code;
  } catch (e) {
    refuse(`${SUPPORTED}: cannot be read (${e.message})`);
  }
  const neutralMatch = code.match(/\bconst\s+string\s+Neutral\s*=\s*("[^"]*")\s*;/);
  if (!neutralMatch) refuse(`${SUPPORTED}: no const string Neutral = "..."`);
  const neutral = literal(neutralMatch[1]);
  const listMatch = code.match(/\bCultureNames\s*=\s*new\s*(?:string\s*)?\[\s*\]\s*\{([^}]*)\}/);
  if (!listMatch) refuse(`${SUPPORTED}: no CultureNames = new[] { ... }`);
  const cultures = [...listMatch[1].matchAll(/"[^"]*"|\bNeutral\b|[^\s,]+/g)].map((m) => {
    if (m[0] === 'Neutral') return neutral;
    if (m[0].startsWith('"')) return literal(m[0]);
    return refuse(`${SUPPORTED}: CultureNames holds ${m[0]}, which is neither a string nor Neutral`);
  });
  if (!cultures.includes(neutral)) refuse(`${SUPPORTED}: CultureNames does not hold the neutral ${neutral}`);
  if (new Set(cultures).size !== cultures.length) refuse(`${SUPPORTED}: CultureNames names a culture twice`);

  let names;
  try {
    names = readdirSync(RESX_DIR);
  } catch (e) {
    refuse(`${RESX_DIR}: cannot be read (${e.message})`);
  }
  const onDisk = names.map((n) => n.match(/^Strings\.(.+)\.resx$/)?.[1]).filter(Boolean);
  if (!names.includes('Strings.resx')) refuse(`${RESX_DIR}: no neutral Strings.resx`);
  const problems = [];
  for (const c of cultures)
    if (c !== neutral && !onDisk.includes(c)) problems.push(`CultureNames has ${c} and there is no ${RESX_DIR}/Strings.${c}.resx`);
  for (const s of onDisk)
    if (!cultures.includes(s)) problems.push(`${RESX_DIR}/Strings.${s}.resx has no culture in CultureNames`);
  if (problems.length) refuse(problems.join('; '));

  return { neutral, cultures, satellites: cultures.filter((c) => c !== neutral) };
}
