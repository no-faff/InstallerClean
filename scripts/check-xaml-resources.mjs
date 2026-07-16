#!/usr/bin/env node
// Fails (exit 1) when a XAML resource reference names a key nothing defines, or
// when a defined key has no consumer.
//
// The dangling reference is the reason this guard exists. A {StaticResource X}
// naming a key that does not exist is not a compile error: XAML resource lookup
// happens when the consuming template is first realised, so the failure surfaces
// as a XamlParseException the first time a user opens the affected window. The
// build stays green, `dotnet test` never touches XAML, and Core-only logic runs
// on Linux, so nothing in the ordinary loop catches it. Renaming a token is the
// operation that produces it: a rename that misses one consumer ships a window
// that throws on open.
//
// The dead-key half is the same reasoning as scripts/check-dead-resx-keys.mjs: a
// token nothing consumes is dead weight that reads as load-bearing.
//
// This is the XAML-key gate half of scripts/list-unused-resources.sh, whose
// x:Key report is read-only. The two differ deliberately in one way: this guard
// strips XML comments before it reads anything (below), where that script scans
// the raw text. So this guard sees a key named only in prose as dead, and that
// script sees it as alive. Where the two disagree, the comment is the difference.
//
// WHAT COUNTS AS A DEFINITION
//   x:Key="Name" in any .xaml under src/. Both the theme dictionaries and the
//   three window-local BoolToVis converters are read as one namespace. That is a
//   simplification: WPF resolves a StaticResource up the tree from the consuming
//   element, so a window-local key referenced from a DIFFERENT window would pass
//   here and throw at runtime. It is sound as long as window-local keys stay
//   defined in the window that uses them (verified: each of the three BoolToVis
//   definitions is referenced only inside its own file).
//
//   x:Key="{x:Static ...}" is skipped. A markup-extension key resolves to a
//   value at runtime, not to a name a static parse could match, and the one in
//   the repo (SystemParameters.FocusVisualStyleKey) is claimed by WPF itself.
//
// WHAT COUNTS AS A REFERENCE
//   XAML: {StaticResource Key} and {DynamicResource Key}. The
//   {StaticResource {x:Type Foo}} form is skipped for the same reason as the
//   markup-extension key above: it names an implicit style keyed by type.
//
//   C#: only the explicit lookup sites, FindResource / TryFindResource /
//   SetResourceReference / Resources["..."] with a literal key. A C# string
//   literal is NOT read as a reference merely for looking like a token name:
//   resx keys share the Action.* and Status.* prefixes with theme tokens
//   ("Status.Moving" is a resx plural prefix, "Status.Warning" is a brush), and
//   dotted literals in this codebase are mostly type and member names. Matching
//   on shape would flag those as dangling theme references.
//
//   The one lookup built from a name rather than written at the site is App's
//   TypeSizeTokenKeys, whose literals reach Resources[key] in ApplyTextScaling.
//   A static parse cannot follow a variable through an indexer, so the array is
//   read directly, by name, below. It is the exact bug this guard is for: a
//   Type.* rename that misses that array silently stops text scaling (the
//   indexer returns null, the `is double` test fails and the token is skipped),
//   with no exception to notice. The parse fails loudly if the array stops
//   matching, so it cannot rot into a check that quietly covers nothing.
//
// COMMENTS ARE STRIPPED FIRST, both sides. A comment is not a reference: WPF
// never resolves one. Themes/Primitives.xaml explains the type-matching rule
// using "{StaticResource X}" in prose, which without stripping is a reference to
// a key named X that nothing defines, i.e. an instant false failure.
//
// bin/ and obj/ are excluded because build output mirrors source.
//
// Run from the repo root: node scripts/check-xaml-resources.mjs
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, sep } from 'node:path';

const SRC = 'src';
const APP_XAML_CS = `${SRC}/InstallerClean/App.xaml.cs`;

// Keys defined on purpose with no consumer in the source. Each earns its place
// with a reason; an unconsumed key NOT listed here fails the guard.
const UNCONSUMED_ALLOWLIST = new Set([]);

// Referenced keys that nothing in this repo defines: a resource WPF or the
// system supplies. Each earns its place with a reason; a dangling reference NOT
// listed here fails the guard, because that is the missed-rename crash.
const EXTERNAL_ALLOWLIST = new Set([]);

// Paths are normalised to forward slashes. CI runs this on Windows, where
// path.join yields backslashes, and the theme-file test below matches on a path
// fragment: without this the collision check would quietly pass everything on the
// one machine that gates the build.
function collect(dir, ext, out = []) {
  for (const name of readdirSync(dir)) {
    if (name === 'bin' || name === 'obj') continue;
    const p = join(dir, name);
    if (statSync(p).isDirectory()) collect(p, ext, out);
    else if (name.endsWith(ext)) out.push(p.split(sep).join('/'));
  }
  return out;
}

// Blanked rather than deleted, newlines kept, so every reported line number is
// the line number in the file on disk.
const blank = (m) => m.replace(/[^\n]/g, ' ');

const stripXmlComments = (s) => s.replace(/<!--[\s\S]*?-->/g, blank);
// Line and block comments. The string-literal alternative is matched FIRST and
// put back verbatim, so a // or /* inside a string is not read as a comment.
const stripCsComments = (s) =>
  s.replace(/"(?:\\.|[^"\\])*"|\/\/[^\n]*|\/\*[\s\S]*?\*\//g, (m) =>
    m.startsWith('"') ? m : blank(m));

const xamlFiles = collect(SRC, '.xaml');
const csFiles = collect(SRC, '.cs').filter((f) => !f.endsWith('Strings.Designer.cs'));

const xaml = xamlFiles.map((f) => [f, stripXmlComments(readFileSync(f, 'utf8'))]);
const cs = csFiles.map((f) => [f, stripCsComments(readFileSync(f, 'utf8'))]);

// --- definitions -------------------------------------------------------------
const defs = new Map(); // key -> [file, ...]
for (const [file, text] of xaml)
  for (const [, key] of text.matchAll(/x:Key="([^"]+)"/g)) {
    if (key.startsWith('{')) continue; // markup-extension key, resolved by type
    if (!defs.has(key)) defs.set(key, []);
    defs.get(key).push(file);
  }

// --- references --------------------------------------------------------------
const refs = new Map(); // key -> [site, ...]
const addRef = (key, site) => {
  if (!refs.has(key)) refs.set(key, []);
  refs.get(key).push(site);
};

for (const [file, text] of xaml) {
  const lines = text.split('\n');
  lines.forEach((line, i) => {
    for (const [, key] of line.matchAll(/\{(?:Static|Dynamic)Resource\s+([^}\s]+)\s*\}/g)) {
      if (key.startsWith('{')) continue; // {StaticResource {x:Type Foo}}
      addRef(key, `${file}:${i + 1}`);
    }
  });
}

for (const [file, text] of cs) {
  const lines = text.split('\n');
  lines.forEach((line, i) => {
    const site = `${file}:${i + 1}`;
    for (const [, key] of line.matchAll(/(?:Try)?FindResource\(\s*"([^"]+)"/g)) addRef(key, site);
    for (const [, key] of line.matchAll(/Resources\[\s*"([^"]+)"\s*\]/g)) addRef(key, site);
    for (const [, key] of line.matchAll(/SetResourceReference\([^,]+,\s*"([^"]+)"/g)) addRef(key, site);
  });
}

// The name-built lookup: App's TypeSizeTokenKeys feeds Resources[key]. Parsed
// here because no lookup site names these keys. A shape change must fail rather
// than silently drop the check.
const appSource = stripCsComments(readFileSync(APP_XAML_CS, 'utf8'));
const typeArray = appSource.match(/TypeSizeTokenKeys\s*=\s*(?:new\s+string\[\]\s*)?\{([^}]*)\}/);
if (!typeArray) {
  console.error(`FAILED: could not read the TypeSizeTokenKeys array in ${APP_XAML_CS}.`);
  console.error('Its literals are resource keys reached through Resources[key], which no');
  console.error('static parse can follow. If the array moved or changed shape, update this');
  console.error('guard to match; if the text-scaling mechanism is gone, delete this block.');
  process.exit(1);
}
const typeKeys = [...typeArray[1].matchAll(/"([^"]+)"/g)].map((m) => m[1]);
if (!typeKeys.length) {
  console.error(`FAILED: TypeSizeTokenKeys in ${APP_XAML_CS} parsed as empty.`);
  process.exit(1);
}
for (const key of typeKeys) addRef(key, `${APP_XAML_CS} (TypeSizeTokenKeys)`);

// --- report ------------------------------------------------------------------
const dangling = [...refs.keys()].filter((k) => !defs.has(k) && !EXTERNAL_ALLOWLIST.has(k));
const unconsumed = [...defs.keys()].filter((k) => !refs.has(k) && !UNCONSUMED_ALLOWLIST.has(k));

// A key defined twice among the theme dictionaries. App.xaml merges
// Components.xaml, which merges Tokens.xaml, which merges Primitives.xaml, so
// all three land in one application-scope namespace and a name defined in two of
// them resolves to whichever merge ran last, silently and correctly-looking. It
// is the failure mode of a rename that lands on a name already in use: the
// consumer keeps painting, in the wrong colour. Window-local keys are excluded
// because each window is its own scope (the three BoolToVis converters are the
// same name three times over, and never collide).
const themeDefs = (files) => files.filter((f) => f.includes('/Themes/'));
const collisions = [...defs.entries()].filter(([, files]) => themeDefs(files).length > 1);

console.log(
  `XAML resource guard: ${defs.size} key(s) defined, ${refs.size} key(s) referenced ` +
    `across ${xamlFiles.length} XAML and ${csFiles.length} C# file(s).`
);

if (dangling.length) {
  console.error(`\nFAILED: ${dangling.length} reference(s) to a key nothing defines:`);
  for (const k of dangling.sort())
    for (const site of refs.get(k)) console.error(`  ${k}  <-  ${site}`);
  console.error('\nEach one throws a XamlParseException when its template is first realised.');
  console.error('Either the key was renamed and this consumer was missed, or the resource');
  console.error('comes from outside the repo (add it to EXTERNAL_ALLOWLIST with a reason).');
}

if (unconsumed.length) {
  console.error(`\nFAILED: ${unconsumed.length} defined key(s) with no consumer:`);
  for (const k of unconsumed.sort()) console.error(`  ${k}  (defined in ${defs.get(k).join(', ')})`);
  console.error('\nEither the key is dead (remove it), or it is consumed in a way this guard');
  console.error('cannot see (add it to UNCONSUMED_ALLOWLIST with a one-line reason). Do not');
  console.error('silence it without deciding which.');
}

if (collisions.length) {
  console.error(`\nFAILED: ${collisions.length} key(s) defined more than once in the theme:`);
  for (const [k, files] of collisions.sort()) console.error(`  ${k}  (${files.join(', ')})`);
  console.error('\nThe theme dictionaries merge into one namespace, so the later definition');
  console.error('silently wins and its consumers paint the wrong value. Rename one of them.');
}

if (dangling.length || unconsumed.length || collisions.length) process.exit(1);
console.log('OK: every reference resolves, every key has a consumer, and no key is defined twice.');
