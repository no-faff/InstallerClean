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
// another count form and go stale by the same edit. An override has no neutral
// counterpart of its own to be measured against, so check-still-english.mjs
// measures one against the neutral value it overrides, which is what makes this
// half of the pair enforceable rather than merely intended.
//
// IT SHOWS BEFORE IT ACTS, AND THAT IS THE DEFAULT RATHER THAN A FLAG. The real
// hazard here is judgement and not mechanism: every flag throws away a translation
// a human then has to redo, and the signal that suggests a key needs flagging is
// wrong far more often than it is right, the cheap version of it most of all.
// An undo lets you back out after you have seen the damage; a preview
// lets you not do it. A flag you have to remember is a flag you forget on the run
// that matters, so the safe mode is the one you get by typing nothing.
//
// THE UNDO IS GIT, AND THIS SCRIPT'S JOB IS TO KEEP IT ONE. The fifteen generators
// are tracked, so from a clean tree `git checkout -- scripts/translations/` restores
// every translation exactly. That holds only while those files match the last
// commit: with a half-finished translation already in them, restoring takes the
// human's work out along with what this run wrote. So --apply refuses on a dirty
// tree and names the files, and --force copies them aside first and says where.
// A hand-rolled backup beside git would go stale, get committed by accident, or be
// trusted when it came from a different run.
//
// Usage (from the repo root):
//   node scripts/flag-retranslation.mjs Action.Cancel Action.Close ...
//       show what would change and write nothing
//   node scripts/flag-retranslation.mjs --apply Action.Cancel ...
//       make the change, refusing if any generator has uncommitted changes
//   node scripts/flag-retranslation.mjs --apply --force Action.Cancel ...
//       make the change anyway, copying every generator aside first
//
// After running: translate each flagged key in each gen-strings-<code>.mjs MAP,
// regenerate (the self-check returns to GENERATION OK) and run
// check-resx-parity.mjs. Nothing here keeps a list of what a run flagged, so that
// is a note to keep yourself. A plural override is not a neutral key and no
// self-check compares one, so check-still-english.mjs is what says whether the
// overrides are done. The template is not one of the files this rewrites, and every
// run prints where it stands on the keys it was given: refresh-template-english.mjs
// is what brings that one forward.
import { readFileSync, writeFileSync, readdirSync, copyFileSync, mkdtempSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const argv = process.argv.slice(2);
const APPLY = argv.includes('--apply');
const FORCE = argv.includes('--force');
const keys = argv.filter((a) => !a.startsWith('--'));

// An unrecognised flag is refused rather than read as a key name. Silently taking
// --dry-run as a key would report it "not found in the neutral resx (typo?)", which
// is a true sentence that sends the reader looking in entirely the wrong place.
const strayFlags = argv.filter((a) => a.startsWith('--') && a !== '--apply' && a !== '--force');
if (strayFlags.length) {
  console.error(`Unrecognised option(s): ${strayFlags.join(', ')}`);
  console.error('The options are --apply and --force. Everything else is a key name.');
  process.exit(2);
}
if (FORCE && !APPLY) {
  console.error('--force does nothing on its own: it only relaxes the check --apply makes.');
  console.error('A run without --apply writes nothing, so there is nothing to force.');
  process.exit(2);
}
if (keys.length === 0) {
  console.error('Usage: node scripts/flag-retranslation.mjs [--apply [--force]] <Key.Name> [<Key.Name> ...]');
  console.error('Without --apply it shows what would change and writes nothing.');
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
// THE PROBE BELOW USES THIS SCRIPT'S OWN SHAPE, '<data name="' with one space and
// no \s+, rather than the shape its neighbours use. A control that exercises a
// pattern the reader does not use proves the file has structure and proves nothing
// about whether this reader can reach it.
//
// The unknown-key refusal below is not this and does not cover it: it is per-key,
// so it stops a key that could not be found and stays silent about every key the
// run did not ask for. This tool rewrites fifteen generators with no undo, so a
// neutral it cannot wholly read is one it must not act on at all.
const parseControl = (file, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${file}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to act on a file this script cannot show it read.');
  process.exit(2);
};

const readable = new Set(
  [...neutralXml.matchAll(/<data name="([^"]+)"[^>]*>\s*<value>/g)].map((m) => m[1]));
parseControl(NEUTRAL, neutralXml, readable.size);

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

// EVERY SATELLITE GENERATOR, AND THE TEMPLATE IS NOT ONE OF THEM.
// gen-strings-template.mjs holds the English a new language is copied from rather
// than a translation of it, so setting a value there back to the neutral is not a
// flag: it is that file being current. It still moves when the neutral moves, and
// refresh-template-english.mjs is what moves it, which every run of this prints at
// the end.
const files = readdirSync(GENDIR)
  .filter((f) => /^gen-strings-.+\.mjs$/.test(f) && f !== 'gen-strings-template.mjs')
  .sort();

if (files.length === 0) {
  console.error(`No gen-strings-<code>.mjs found in ${GENDIR}`);
  process.exit(1);
}

// ONE PASS OVER ONE GENERATOR, PRODUCING THE NEW TEXT AND THE LIST OF CHANGES
// TOGETHER. Both modes go through here, so what the preview shows is by
// construction what a write would do. A preview that found its own way to the
// values would be a second instrument reading the same file, and two instruments
// disagree eventually: the one that is only ever read by a human is the one that
// drifts, because nothing fails when it does.
//
// Pure: it takes the text and returns new text. Nothing here writes.
const planFile = (file, original) => {
  let text = original;
  const changes = [];
  const skipped = [];

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
        const m = re.exec(text);
        if (m === null) continue;
        changes.push({ kind: 'override', key: name, before: m[2] });
        text = text.replace(re, (_m, p1, _body, p3) => p1 + english + p3);
      }
    }

    // An existing 'Key': `template-literal`, entry (the inner group tolerates
    // escaped chars so a value containing \` or \\ does not end the match early).
    const entryRe = new RegExp("('" + reEsc(key) + "':\\s*`)((?:\\\\.|[^`\\\\])*)(`)");
    const m = entryRe.exec(text);
    if (m !== null) {
      changes.push({ kind: 'reset', key, before: m[2] });
      text = text.replace(entryRe, (_m, p1, _body, p3) => p1 + english + p3);
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
      if (objStart < 0) { skipped.push(`no "${marker}" found; skipped ${key}`); continue; }
      const rel = text.slice(objStart).search(/\n};/);
      if (rel < 0) { skipped.push(`${marker} close not found; skipped ${key}`); continue; }
      const at = objStart + rel;
      text = text.slice(0, at) + `\n  '${key}': \`${english}\`,` + text.slice(at);
      changes.push({ kind: 'add', key, before: null });
    }
  }
  return { file, path: `${GENDIR}/${file}`, text, changes, skipped };
};

const plans = files.map((f) => planFile(f, readFileSync(`${GENDIR}/${f}`, 'utf8')));
const langOf = (file) => file.replace(/^gen-strings-/, '').replace(/\.mjs$/, '');

for (const p of plans) for (const s of p.skipped) console.error(`  ${p.file}: ${s}`);

// --- What is about to go, or what has just gone. One block per key, because the
// English replacing it is the same in every language and printing it fifteen times
// says nothing the once does not.
//
// THE OLD VALUE IS PRINTED, NOT JUST THE KEY NAME. Then this output is itself a
// written record of what was replaced, readable with no git and no copy aside, and
// it is the only such record a run leaves.
const report = () => {
  for (const key of keys) {
    const rows = plans.flatMap((p) => p.changes
      .filter((c) => c.key === key || c.key.startsWith(`${key.replace(/\.Plural$/, '')}.`))
      .map((c) => ({ lang: langOf(p.file), ...c })));
    if (!rows.length) continue;
    console.log(`\n${key}`);
    console.log(`  English going in:  ${esc(neutralValue(key))}`);
    // A language's own entry first, then any plural override it declares, so the
    // two sit together and the override reads as a form of the line above it. An
    // override is labelled by its category alone: the key is the block heading
    // three lines up, and repeating it in full pushed the value off its column.
    const order = (r) => (r.kind === 'override' ? 1 : 0);
    for (const r of [...rows].sort((a, b) => a.lang.localeCompare(b.lang) || order(a) - order(b))) {
      const label = r.kind === 'override' ? `${r.lang}  .${r.key.split('.').pop()}` : r.lang;
      console.log(`    ${label.padEnd(20)}${r.before === null ? '(no entry yet, one will be added)' : r.before}`);
    }
  }
};

// --- WHERE THE TEMPLATE STANDS, WHICH THIS RUN LEAVES EXACTLY AS IT FOUND IT. Read
// and never written: the English source is brought forward by
// refresh-template-english.mjs, for the reason given at the file list above. It has
// to be brought forward, because the template is what a new language is copied from,
// so a run that said nothing about it would leave the previous wording in the file
// that language starts from.
//
// IT ANSWERS FOR THE KEYS OF THIS RUN AND FOR NOTHING ELSE. Where the template
// stands across the rest of the neutral is its own self-check's answer, and this
// prints nothing about it.
const TEMPLATE = `${GENDIR}/gen-strings-template.mjs`;
const templateReport = () => {
  let tmpl;
  try {
    tmpl = readFileSync(TEMPLATE, 'utf8');
  } catch {
    console.log(`\nTHE TEMPLATE: ${TEMPLATE} could not be read, so where it stands is unreported.`);
    return;
  }
  const rows = keys.map((key) => {
    const m = new RegExp("('" + reEsc(key) + "':\\s*`)((?:\\\\.|[^`\\\\])*)(`)").exec(tmpl);
    if (m === null) return { key, state: 'no entry', held: null };
    return { key, state: m[2] === esc(neutralValue(key)) ? 'current' : 'superseded', held: m[2] };
  });
  console.log('\nTHE TEMPLATE, which this run does not touch. A new language is copied from it:');
  for (const r of rows)
    console.log(`  ${r.state.padEnd(12)}${r.key}${r.state === 'superseded' ? `  ${r.held}` : ''}`);
  // A key with no entry there is one the template never gained rather than one that
  // has gone stale, and the same command answers for both.
  const behind = rows.filter((r) => r.state !== 'current');
  if (behind.length === 0) {
    console.log('  Every key above already holds the current English there.');
    return;
  }
  console.log(`  ${behind.length} of ${keys.length} key(s) above are not at the current English there.`);
  console.log('  Bring them forward with:');
  console.log(`    node scripts/refresh-template-english.mjs ${behind.map((r) => r.key).join(' ')}`);
};

const totalReset = plans.reduce((n, p) => n + p.changes.filter((c) => c.kind === 'reset').length, 0);
const totalAdded = plans.reduce((n, p) => n + p.changes.filter((c) => c.kind === 'add').length, 0);
const totalOverrides = plans.reduce((n, p) => n + p.changes.filter((c) => c.kind === 'override').length, 0);

if (!APPLY) {
  console.log(`PREVIEW of ${keys.length} key(s) across ${files.length} generator(s). Nothing has been written.`);
  report();
  // The headline is the TOTAL being replaced, with the split after it. Naming the
  // two parts and no total reads as the smaller number: an override is a
  // translation somebody wrote and it is destroyed exactly as the entry is.
  console.log(`\nTOTALS: ${totalReset + totalOverrides} translation(s) would be replaced with the `
    + `English (${totalReset} entr(ies), ${totalOverrides} plural override(s)), `
    + `${totalAdded} new entr(ies) appended.`);
  // PARTITIONED, one line per kind present, because the two are not the same cost
  // and no sentence is true of both. A line carrying a value is a translation being
  // destroyed; a line reading "no entry yet" is a key the generator never had, so
  // nothing is being done "again" and it needs doing for the first time. Each line
  // is printed only when that kind is actually in the listing, so neither ever
  // states a cost the run does not carry.
  const closing = [];
  if (totalReset + totalOverrides > 0)
    closing.push('Every line above carrying a value is a translation that would have to be done'
      + '\nagain by a human.');
  if (totalAdded > 0)
    // "generators with no entry for it", NOT "keys no generator has". A key can be
    // absent from fourteen generators and present in the fifteenth, which is the
    // ordinary state of a Cli. key part-way through a round, and the second wording
    // is false of that run while every figure in it stays right.
    closing.push(`The ${totalAdded} line(s) reading "no entry yet" are generators that have no entry`
      + '\nfor that key, so those need a FIRST translation rather than a second.');
  for (const c of closing) console.log(`\n${c}`);
  templateReport();
  console.log('\nIf that is what you want: run it again with --apply.');
  process.exit(0);
}

// --- The clean-tree check, and it is what makes git a guaranteed undo rather than
// a conditional one.
//
// SCOPED TO THE FILES THIS RUN WOULD REWRITE, not to the tree: an unrelated edit
// elsewhere is not this script's business and refusing over one would train people
// to reach for --force by reflex.
//
// THE NORMAL STATE AFTER A RUN IS DIRTY, because translating the flagged keys is
// the next step, so flagging a second key mid-translation trips this. That is the
// friction working rather than failing: mid-translation is exactly the case where
// restoring with git would throw the human's work away, so it is the case that most
// needs to be stopped and told why.
//
// NO GIT, OR NOT A REPOSITORY, REFUSES TOO. It is the only undo there is, so a run
// that cannot establish it has one is a run with no way back. --force is the answer
// in both cases and it leaves a real copy behind.
const targets = files.map((f) => `${GENDIR}/${f}`);
let dirty = null;
try {
  dirty = execFileSync('git', ['status', '--porcelain', '--', ...targets], { encoding: 'utf8' })
    .split('\n').filter(Boolean).map((l) => l.slice(3).trim());
} catch {
  dirty = null;
}

if (!FORCE && dirty === null) {
  console.error('REFUSING: cannot ask git whether the generators have uncommitted changes.');
  console.error('Either git is not on the path or this is not a checkout. Git is the only way');
  console.error('back from this script, so a run that cannot check it is a run with no undo.');
  console.error('\nRun it again with --force. That copies all '
    + `${files.length} generator(s) aside first and prints where they went.`);
  process.exit(3);
}
if (!FORCE && dirty.length) {
  console.error(`REFUSING: ${dirty.length} of the ${files.length} file(s) this would rewrite `
    + 'have uncommitted changes.');
  for (const d of dirty) console.error(`  ${d}`);
  console.error('\nThis script replaces a translation with the English neutral and keeps no copy');
  console.error('of what it replaced. Git is the undo, and git can only be the undo while these');
  console.error('files match the last commit: with changes already in them, restoring takes your');
  console.error('work back out along with what this run would write.');
  // NO RESTORE COMMAND HERE, DELIBERATELY, AND IT IS NOT AN OVERSIGHT. A refusal is
  // read by somebody who has just been stopped and is scanning for the remedy, and
  // two lines above they have been told their uncommitted work is what stopped them.
  // A `git checkout` printed underneath that gets run by anybody skimming, and it
  // destroys the very translations this refusal exists to save. It would also be
  // redundant: nothing was written, so there is nothing to restore. The run that
  // DOES write prints it, which is the only run it means anything on.
  console.error('\nCommit or stash them, then run this again.');
  console.error('\nTo go ahead anyway, add --force. It copies all '
    + `${files.length} generator(s) aside first and prints where they went.`);
  process.exit(3);
}

// --force copies EVERY generator aside, not only the dirty ones: the point of the
// copy is to be a complete restore, and a partial one is worse than none because it
// looks like a restore. It goes to a fresh directory outside the repository, so it
// cannot be committed by accident and cannot be mistaken for another run's.
let backup = null;
if (FORCE) {
  // TIMESTAMPED AND SORTABLE, because two runs leave two directories and the person
  // who comes back tomorrow has lost the scrollback that named the right one. Local
  // time rather than UTC so it matches the clock they are reading it against, and
  // mkdtemp's random suffix stays on the end for collision safety.
  const p2 = (n) => String(n).padStart(2, '0');
  const d = new Date();
  const stamp = `${d.getFullYear()}${p2(d.getMonth() + 1)}${p2(d.getDate())}`
    + `-${p2(d.getHours())}${p2(d.getMinutes())}${p2(d.getSeconds())}`;
  backup = mkdtempSync(join(tmpdir(), `flag-retranslation-${stamp}-`));
  for (const f of files) copyFileSync(`${GENDIR}/${f}`, join(backup, f));
}

for (const p of plans) writeFileSync(p.path, p.text, 'utf8');

console.log(`Flagged ${keys.length} key(s) across ${files.length} generator(s). `
  + 'What each language held before this run:');
report();
console.log(`\nTOTALS: ${totalReset} reset in place, ${totalAdded} appended, `
  + `${totalOverrides} plural override(s) reset.`);
// PARTITIONED, one line per kind present, on the rule the preview follows: no
// sentence is true of both. An entry is a neutral key, so a generator's own
// self-check compares it against the neutral and names it; an override answers for
// a count form the neutral has no key for, so nothing inside a generator can
// compare it and check-still-english.mjs is what reads it.
const reported = [];
if (totalReset + totalAdded > 0)
  reported.push(`The ${totalReset + totalAdded} entr(ies) now hold the English neutral value, so every generator`
    + '\nreports each of them as "still English (untranslated)" until translated.');
if (totalOverrides > 0)
  reported.push(`The ${totalOverrides} plural override(s) now hold the English their base key holds. No`
    + '\nneutral key exists for an override to be compared with, so no generator reports'
    + '\none as still English: check-still-english.mjs names them until translated.');
for (const c of reported) console.log(`\n${c}`);

// The way back, printed by every run that writes, before the way forward. Nobody
// should have to remember a restore command or go and look one up at the moment
// they have just realised they want it.
if (backup !== null) {
  // Says WHICH case it was in. "git could not be relied on" covers both and tells
  // the reader neither, and the two want different things of them afterwards.
  console.log('\nEvery generator was copied aside before it was written, because --force was');
  console.log(dirty === null
    ? 'given and git could not be asked whether they were clean. The copies from this run are in:'
    : `given and ${dirty.length} of them had uncommitted changes. The copies from this run are in:`);
  console.log(`  ${backup}`);
  console.log('Restore one by copying it back over the file of the same name in '
    + `${GENDIR}/, or all of them at once.`);
  // A backup somebody believes is permanent is a worse artefact than no backup:
  // they stop taking their own, and it is gone the next time the machine tidies up.
  console.log('That is a TEMPORARY directory and the operating system will clear it,');
  console.log('on a reboot or whenever it next tidies up. Copy it somewhere you choose');
  console.log('if you want it to survive.');
} else {
  console.log(`\nThe generators were clean before this run, so git is a complete undo:`);
  console.log(`  git checkout -- ${GENDIR}/`);
}

// This script writes to the generators and nothing else, so the sign-off says so
// outright. Keeping track of what is waiting to be translated is a manual step, and
// a closing line that merely implied this run had recorded it would get taken at its
// word, leaving the debt unrecorded.
console.log('\nNext: this run keeps no list of what it flagged, so note the key(s) above');
console.log('yourself, then translate each in the gen MAPs and regenerate.');
console.log('Any plural override listed above needs translating too, and has no neutral key of');
console.log('its own to appear under: log it by language as well as by key.');
templateReport();
