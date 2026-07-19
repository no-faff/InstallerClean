#!/usr/bin/env node
// flag-retranslation.mjs: mark one or more neutral string keys as needing
// re-translation across every satellite generator.
//
// For each gen-strings-<code>.mjs (all satellites, the native it and ja
// included), it sets the given key(s) to the CURRENT English neutral value: an
// existing MAP entry is overwritten in place, and a key not yet in the MAP (a
// newly added neutral key) is appended to the MAP. Each generator's self-check
// fails when a value still equals the English neutral ("still English
// (untranslated)"), so regenerating afterwards reports GENERATION HAS ISSUES
// for that key until a human translates it.
//
// That loud, visible gate is the whole point. check-resx-parity.mjs only checks
// key PRESENCE and placeholder arity, so a STALE translation (the old wording
// of a key whose English changed) passes it silently: the key is present and
// the arity matches. Setting the value back to English converts that silent
// staleness into a hard generation failure that cannot be missed.
//
// Usage (from the repo root):
//   node scripts/flag-retranslation.mjs Completion.CleanedUp Completion.DeleteRestoreHint ...
//
// After running: translate each flagged key in each gen-strings-<code>.mjs MAP,
// regenerate (the self-check returns to GENERATION OK), run
// check-resx-parity.mjs, and clear the key from PENDING-RETRANSLATION.md. The
// full process is in
// non-repo-files/0-claude/translation/CHANGING-A-TRANSLATED-STRING.md.
import { readFileSync, writeFileSync, readdirSync } from 'node:fs';

const keys = process.argv.slice(2);
if (keys.length === 0) {
  console.error('Usage: node scripts/flag-retranslation.mjs <Key.Name> [<Key.Name> ...]');
  process.exit(2);
}

const NEUTRAL = 'src/InstallerClean.Core/Resources/Strings.resx';
const GENDIR = 'non-repo-files/0-claude/translation';

const reEsc = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

// English neutral value for a key (raw <value> body, entities intact).
const neutralXml = readFileSync(NEUTRAL, 'utf8');
const neutralValue = (key) => {
  const m = neutralXml.match(
    new RegExp('<data name="' + reEsc(key) + '"[^>]*>\\s*<value>([\\s\\S]*?)</value>'));
  return m ? m[1] : null;
};

const unknown = keys.filter((k) => neutralValue(k) === null);
if (unknown.length) {
  console.error('Not found in the neutral resx (typo?): ' + unknown.join(', '));
  process.exit(2);
}

// Escape a raw value for a single-line JS template literal (backslash first so
// the escapes added for $, CR and LF are not doubled).
const esc = (v) => v
  .replace(/\\/g, '\\\\')
  .replace(/`/g, '\\`')
  .replace(/\$/g, () => '\\$')
  .replace(/\r/g, '\\r')
  .replace(/\n/g, '\\n');

const files = readdirSync(GENDIR)
  .filter((f) => /^gen-strings-.+\.mjs$/.test(f) && f !== 'gen-strings-template.mjs')
  .sort();

if (files.length === 0) {
  console.error(`No gen-strings-<code>.mjs found in ${GENDIR}`);
  process.exit(1);
}

let totalReset = 0, totalAdded = 0;
for (const file of files) {
  const path = `${GENDIR}/${file}`;
  let text = readFileSync(path, 'utf8');
  const reset = [], added = [];

  for (const key of keys) {
    const english = esc(neutralValue(key));
    // An existing 'Key': `template-literal`, entry (the inner group tolerates
    // escaped chars so a value containing \` or \\ does not end the match early).
    const entryRe = new RegExp("('" + reEsc(key) + "':\\s*`)((?:\\\\.|[^`\\\\])*)(`)");
    if (entryRe.test(text)) {
      text = text.replace(entryRe, (_m, p1, _body, p3) => p1 + english + p3);
      reset.push(key);
    } else {
      // A newly added neutral key: append to the target object's close, which is
      // the first line-leading "};" after its "const X = {" (values are
      // single-line, so no earlier "\n};" occurs inside them). The target is
      // normally MAP, but a Cli.* key in a generator that keeps its human Cli.*
      // strings in a separate CLI object (the ru generator strips every Cli.* key
      // from the neutral base and rebuilds that set from CLI) must go into CLI, or
      // the generator strips it by name and it never reaches the satellite resx.
      const marker = (key.startsWith('Cli.') && text.includes('const CLI = {'))
        ? 'const CLI = {'
        : 'const MAP = {';
      const objStart = text.indexOf(marker);
      if (objStart < 0) { console.error(`  ${file}: no "${marker}" found; skipped ${key}`); continue; }
      const rel = text.slice(objStart).search(/\n};/);
      if (rel < 0) { console.error(`  ${file}: ${marker} close not found; skipped ${key}`); continue; }
      const at = objStart + rel;
      text = text.slice(0, at) + `\n  '${key}': \`${english}\`,` + text.slice(at);
      added.push(key);
    }
  }

  writeFileSync(path, text, 'utf8');
  totalReset += reset.length;
  totalAdded += added.length;
  const parts = [];
  if (reset.length) parts.push(`reset ${reset.length}`);
  if (added.length) parts.push(`appended ${added.length}`);
  console.log(`${file}: ${parts.join(', ') || 'no change'}`);
}

console.log(`\nFlagged ${keys.length} key(s) across ${files.length} generator(s): ` +
  `${totalReset} reset in place, ${totalAdded} appended.`);
console.log('Each is now the English neutral value, so every generator will report it as ' +
  '"still English (untranslated)" until translated.');
// This script writes to the generators and nothing else, so the sign-off says
// so outright. PENDING-RETRANSLATION.md is a manual step; a closing line that
// merely implies the log was written gets taken at its word, and the debt goes
// unrecorded.
console.log('Next: log the key(s) in PENDING-RETRANSLATION.md by hand (this script does not),');
console.log('then translate each in the gen MAPs and regenerate.');
console.log('Satellite-only plural overrides (Key.One/.Few/.Many) are NOT touched here and');
console.log('the still-English gate cannot see them: find and rewrite any by hand.');
