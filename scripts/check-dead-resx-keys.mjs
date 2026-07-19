#!/usr/bin/env node
// Fails (exit 1) when a neutral Strings.resx key has no consumer in the app
// source, so a string that has fallen out of use is caught before it accretes as
// dead weight the fifteen languages all still carry a translation for.
//
// This is the gate half of scripts/list-unused-resources.sh, which is a read-only
// report: this guard reuses that script's exact detection rules and adds a
// pass/fail with a small, explicit allowlist. The two are meant to agree on the
// raw orphan set; if the way a key is consumed ever changes, update both.
//
// A resx key is consumed in one of two forms, and both are searched:
//   - C#:   the underscored accessor Strings.Key_Name. scripts/regenerate-
//           strings-designer.sh turns each dot into an underscore, so Foo.Bar
//           becomes Foo_Bar. Matched as a whole word, so a trailing dot
//           (Strings.Foo_Bar.Trim()) still counts while Foo_Bar does not match
//           inside Foo_Bar_Extra.
//   - XAML/C#: the dotted key Foo.Bar, via {loc:Translate Foo.Bar}. Matched with
//           a boundary that rejects a leading or trailing dot, so Type.Body does
//           not match inside Type.Body.Strong.
//
// Strings.Designer.cs is excluded from the corpus: it generates a property and a
// Get("Foo.Bar") call for every key, so including it would make every key look
// consumed. bin/ and obj/ are excluded because build output mirrors source.
//
// A key built at runtime by string concatenation cannot be seen by a static
// search. The one such mechanism is DisplayHelpers.Pluralise, which reads
// {prefix}.One / .Few / .Many override keys by name; those are SATELLITE-ONLY
// (never in the neutral resx this guard reads), and each prefix's neutral base
// (Plural.File.Singular, Status.RegisteredPackagesFound, ...) is consumed through
// its static Strings.* symbol at the call site, so no plural key is a false
// orphan here. That is why the allowlist below needs no Pluralise entry: verified
// by cross-checking against list-unused-resources.sh, which reports the same two
// keys and no plural key.
//
// Run from the repo root: node scripts/check-dead-resx-keys.mjs
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';

const SRC = 'src';
const RESX = `${SRC}/InstallerClean.Core/Resources/Strings.resx`;

// Neutral keys deliberately kept though nothing consumes them today. Each earns
// its place with a reason; an orphan NOT listed here fails the guard.
const ALLOWLIST = new Set([
  // Both were the body-row status line for a cancelled Move/Delete. That copy
  // moved to the completion overlay once a cancelled run's partial tally was
  // made to survive, leaving these two with no code consumer. The owner kept
  // them: they are between uses, not dead, so the guard passes them rather than
  // the resx losing a string a later state might want back.
  'Status.MoveCancelled.Partial',
  'Status.DeleteCancelled.Partial',
]);

// Every .cs / .xaml under src/, minus bin/ and obj/ (build output mirrors source)
// and minus the generated Designer (it defines an accessor for every key).
function collect(dir, exts, out = []) {
  for (const name of readdirSync(dir)) {
    if (name === 'bin' || name === 'obj') continue;
    const p = join(dir, name);
    if (statSync(p).isDirectory()) collect(p, exts, out);
    else if (exts.some((e) => name.endsWith(e))) out.push(p);
  }
  return out;
}

const csFiles = collect(SRC, ['.cs']).filter((f) => !f.endsWith('Strings.Designer.cs'));
const xamlFiles = collect(SRC, ['.xaml']);
// One corpus, joined by newline so a file boundary is also a token boundary.
const corpus = [...csFiles, ...xamlFiles].map((f) => readFileSync(f, 'utf8')).join('\n');

const keys = [...readFileSync(RESX, 'utf8')
  .matchAll(/<data\s+name="([A-Za-z][A-Za-z0-9._]+)"/g)].map((m) => m[1]);

// Keys contain only letters, digits, dots and underscores, so the dot is the only
// ERE metacharacter to escape.
const escapeRe = (s) => s.replace(/[.]/g, '\\.');
// Underscored C# accessor, whole word: a preceding/following identifier char
// rules it out, but a trailing dot (Foo_Bar.Trim()) does not.
const consumedUnderscored = (k) =>
  new RegExp(`(?<![A-Za-z0-9_])${k.replace(/\./g, '_')}(?![A-Za-z0-9_])`).test(corpus);
// Dotted key: a leading or trailing dot rules it out, so Type.Body is not matched
// inside Type.Body.Strong.
const consumedDotted = (k) =>
  new RegExp(`(?<![A-Za-z0-9_.])${escapeRe(k)}(?![A-Za-z0-9_.])`).test(corpus);

const orphans = keys.filter((k) => !consumedUnderscored(k) && !consumedDotted(k));
const unexpected = orphans.filter((k) => !ALLOWLIST.has(k));

if (orphans.length) {
  console.log(`Dead-resx-key guard: ${orphans.length} neutral key(s) with no static consumer:`);
  for (const k of orphans.sort()) console.log(`  ${ALLOWLIST.has(k) ? '(allowlisted) ' : ''}${k}`);
}
if (unexpected.length) {
  console.error(`\nFAILED: ${unexpected.length} unexpected orphaned key(s), not in the allowlist:`);
  for (const k of unexpected.sort()) console.error(`  ${k}`);
  console.error('\nEither the key is genuinely dead (remove it from Strings.resx and every');
  console.error('satellite), or it is consumed in a way this guard cannot see (add it to the');
  console.error('allowlist with a one-line reason). Do not silence it without deciding which.');
  process.exit(1);
}
console.log(orphans.length
  ? `\nOK: all ${orphans.length} unconsumed key(s) are allowlisted.`
  : 'OK: every neutral resx key has a consumer.');
