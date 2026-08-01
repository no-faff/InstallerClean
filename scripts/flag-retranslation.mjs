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
// A key's satellite-only CLDR plural overrides (Key.One/.Few/.Many, which exist
// in no neutral file) are reset with it, because they are the same sentence in
// another count form and go stale by the same edit. Nine languages carry 70 of
// them and one release rewrote the base of five, in five languages, leaving copy
// on screen that the neutral had just had cut. Every guard was blind to it by
// construction, an override having no neutral counterpart to be compared with;
// check-still-english.mjs now compares one against the neutral value it
// overrides, which is what makes this half of the pair enforceable rather than
// merely intended.
//
// Usage (from the repo root):
//   node scripts/flag-retranslation.mjs Completion.CleanedUp Completion.DeleteRestoreHint ...
//
// After running: translate each flagged key in each gen-strings-<code>.mjs MAP,
// regenerate (the self-check returns to GENERATION OK), run
// check-resx-parity.mjs, and clear the key from PENDING-RETRANSLATION.md.
import { readFileSync, writeFileSync, readdirSync } from 'node:fs';

const keys = process.argv.slice(2);
if (keys.length === 0) {
  console.error('Usage: node scripts/flag-retranslation.mjs <Key.Name> [<Key.Name> ...]');
  process.exit(2);
}

const NEUTRAL = 'src/InstallerClean.Core/Resources/Strings.resx';
const GENDIR = 'scripts/translations';

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

// The prefix whose .One/.Few/.Many overrides take THIS key as their base,
// inverting the rule the generators' own self-check resolves an override with
// (base = the <Prefix>.Plural sibling if the neutral has one, else the flat
// key). A .Singular is never that base, so flagging one drives no override; its
// .Plural sibling, flagged in the same run, is what does.
const overridePrefix = (key) => {
  if (key.endsWith('.Plural')) return key.slice(0, -'.Plural'.length);
  if (key.endsWith('.Singular')) return null;
  return neutralValue(`${key}.Plural`) === null ? key : null;
};
const CATEGORIES = ['One', 'Few', 'Many'];

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

let totalReset = 0, totalAdded = 0, totalOverrides = 0;
for (const file of files) {
  const path = `${GENDIR}/${file}`;
  let text = readFileSync(path, 'utf8');
  const reset = [], added = [], overrides = [];

  for (const key of keys) {
    const english = esc(neutralValue(key));

    // Overrides first, and RESET ONLY: an override is a form the language chose
    // to declare, so a language that declared none for this key must not gain
    // one here. The base's English is what goes in, that being what the
    // override overrides and the only English there is for it.
    const prefix = overridePrefix(key);
    if (prefix !== null) {
      for (const category of CATEGORIES) {
        const name = `${prefix}.${category}`;
        const re = new RegExp("('" + reEsc(name) + "':\\s*`)((?:\\\\.|[^`\\\\])*)(`)");
        if (!re.test(text)) continue;
        text = text.replace(re, (_m, p1, _body, p3) => p1 + english + p3);
        overrides.push(name);
      }
    }

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
  totalOverrides += overrides.length;
  const parts = [];
  if (reset.length) parts.push(`reset ${reset.length}`);
  if (added.length) parts.push(`appended ${added.length}`);
  if (overrides.length) parts.push(`reset ${overrides.length} override (${overrides.join(', ')})`);
  console.log(`${file}: ${parts.join(', ') || 'no change'}`);
}

console.log(`\nFlagged ${keys.length} key(s) across ${files.length} generator(s): ` +
  `${totalReset} reset in place, ${totalAdded} appended, ${totalOverrides} plural override(s) reset.`);
console.log('Each is now the English neutral value, so every generator will report it as ' +
  '"still English (untranslated)" until translated.');
// This script writes to the generators and nothing else, so the sign-off says
// so outright. PENDING-RETRANSLATION.md is a manual step; a closing line that
// merely implies the log was written gets taken at its word, and the debt goes
// unrecorded.
console.log('Next: log the key(s) in PENDING-RETRANSLATION.md by hand (this script does not),');
console.log('then translate each in the gen MAPs and regenerate.');
console.log('Any plural override listed above needs translating too, and has no neutral key of');
console.log('its own to appear under: log it by language as well as by key.');
