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
// (Plural.File.Singular, Cli.DeletingFiles, ...) is consumed through
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
  // made to survive, leaving these two with no code consumer. They are kept
  // deliberately, being between uses rather than dead, so the guard passes them
  // rather than the resx losing a string a later state might want back.
  'Status.MoveCancelled.Partial',
  'Status.DeleteCancelled.Partial',

  // ---- Retired in 3.0.0 with the identity check, and RETIRED IS NOT DEAD ----
  // THE TWO BELOW ARE IN ALL FIFTEEN SATELLITES, which is what makes keeping them
  // worth something and is the same reason as the pair above: deleting the English
  // would throw away fifteen translations of the only wording this project has for
  // the condition they describe.

  // Two of the five held-back causes on the completion overlay, both produced only
  // by the identity re-check at action time: a record existing under the code the
  // FILE declares about itself, and a file that yielded no code to ask about.
  'Completion.ReverifyIdentityClaimed',
  'Completion.ReverifyIdentityUnreadable',
]);

// A KEY THAT NAMES A CAUSE LEAVES THE RESX WITH ITS MECHANISM RATHER THAN JOINING
// THIS LIST. Kept, it can only be re-read as licence to name that cause again. A
// heading can wait for a new condition; a sentence about a condition cannot
// outlive it.

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
// The key pattern here is NARROWER than the one every other reader in this
// directory uses: it requires a leading letter and at least two characters, so a
// key outside that shape is dropped rather than reported, and a key this guard
// never saw can never be named an orphan. The control is what turns that silent
// drop into a stop.
const parseControl = (file, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${file}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to report on a file this check cannot show it read.');
  process.exit(2);
};

const resxXml = readFileSync(RESX, 'utf8');
const keys = [...resxXml
  .matchAll(/<data\s+name="([A-Za-z][A-Za-z0-9._]+)"/g)].map((m) => m[1]);
parseControl(RESX, resxXml, new Set(keys).size);

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

// An allowlist entry that is NOT an orphan, reported first and separately: it is
// a claim this file makes about a key, and a false one says nothing about any
// key while reading as though it had been checked.
//
// IT WENT UNCAUGHT FOR AS LONG AS IT EXISTED, which is why the check is here.
// 'Details.GroupUnsure' sat in the list carrying a reason that had stopped being
// true: the interface it described was restored, the key was consumed again, and
// the guard's own output stopped naming it, because an entry only ever suppresses
// a finding and there was no finding left to suppress. Nothing anywhere printed a
// word about it.
//
// Two ways an entry goes stale and the message names both, because the fix
// differs: the key is consumed again (delete the entry, the string is in use), or
// the key has left the neutral resx entirely (delete the entry, there is nothing
// to allow).
const orphanSet = new Set(orphans);
const stale = [...ALLOWLIST].filter((k) => !orphanSet.has(k));

if (orphans.length) {
  console.log(`Dead-resx-key guard: ${orphans.length} neutral key(s) with no static consumer:`);
  for (const k of orphans.sort()) console.log(`  ${ALLOWLIST.has(k) ? '(allowlisted) ' : ''}${k}`);
}
if (stale.length) {
  console.error(`\nFAILED: ${stale.length} stale allowlist entr(ies), allowing nothing:`);
  for (const k of stale.sort()) {
    const why = keys.includes(k)
      ? 'the key is consumed again, so it is not an orphan'
      : 'the key is no longer in the neutral resx at all';
    console.error(`  ${k} - ${why}`);
  }
  console.error('\nRemove the entry and its reason. An allowlist entry that allows nothing');
  console.error('reads as a checked decision and is not one.');
}
if (unexpected.length) {
  console.error(`\nFAILED: ${unexpected.length} unexpected orphaned key(s), not in the allowlist:`);
  for (const k of unexpected.sort()) console.error(`  ${k}`);
  console.error('\nEither the key is genuinely dead (remove it from Strings.resx and every');
  console.error('satellite), or it is consumed in a way this guard cannot see (add it to the');
  console.error('allowlist with a one-line reason). Do not silence it without deciding which.');
}
if (stale.length || unexpected.length) process.exit(1);

console.log(orphans.length
  ? `\nOK: all ${orphans.length} unconsumed key(s) are allowlisted, and all `
    + `${ALLOWLIST.size} allowlist entr(ies) are allowing one.`
  : `OK: every neutral resx key has a consumer, and the allowlist is empty.`);
