#!/usr/bin/env node
// Fails (exit 1) when a satellite resx holds a value the English neutral USED to
// have. That is a translation that was never done, sitting behind wording the app
// has since abandoned, and no other check in this directory can see it.
//
// WHY IT EXISTS, AND IT IS NOT A DUPLICATE OF check-still-english.mjs. That gate
// asks whether a satellite value EQUALS the current neutral. It therefore sees a
// key whose English has not moved since it was flagged, and is structurally blind
// to a key whose English moved AFTERWARDS: the satellite then differs from the
// neutral, so it reads as translated, while what it actually holds is the old
// English. The value looks done and is not.
//
// A PLURAL OVERRIDE FALLS THROUGH BOTH OF THAT GATE'S ARMS AT ONCE, which is the
// same gap arriving twice over: not equal to the current neutral, so the
// untranslated arm misses it, and its base not equal either, so the stranded arm
// misses it too.
//
// WHAT IT IS NOT. It is not a freshness ledger and needs no seed file. It reads
// the value history out of git, which already records every wording the neutral
// has ever had, so it cannot drift out of step with the thing it measures and
// there is no state to keep current. check-translation-freshness.mjs holds the
// ledger idea and reports on an empty one; this holds the answer.
//
// NO FALSE POSITIVES BY CONSTRUCTION, which is why it can be a hard gate. A
// satellite value that is byte-identical to a former English value is either the
// untranslated old English or a coincidence between a real translation and a
// sentence the app once wrote in English, and the second does not happen.
//
// --drift is a SECOND and much weaker pass, and it is triage rather than a gate.
// It compares WHEN each value last changed, per key, in the satellite against the
// neutral, and lists the ones the English overtook. That catches a real
// translation of superseded English, which nothing else can, and it also catches
// every cosmetic edit a translation never needed to follow. It never fails the
// build and its output is a reading list, not a verdict. Read them; do not act
// on the count.
//
// Run from the repo root: node scripts/check-superseded-english.mjs [--drift]
import { readdirSync, readFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';

const dir = 'src/InstallerClean.Core/Resources';
const NEUTRAL = `${dir}/Strings.resx`;
const drift = process.argv.includes('--drift');

const git = (...args) => execFileSync('git', args, { encoding: 'utf8', maxBuffer: 1 << 28 });

// SHALLOW CLONE REFUSAL, AND IT IS THE ONE CONTROL THIS GATE CANNOT DO WITHOUT.
// Every finding here is a satellite value matching a wording the neutral has held,
// read out of git, so this gate's answer is only as complete as the history it is
// run against. On a single commit the set of former wordings is the current one, and
// a comparison against it can only ever come back empty, which is the same output a
// genuinely clean tree gives.
//
// actions/checkout defaults to a one-commit fetch, so a CI workflow has to ask for
// the history explicitly. The refusal is here rather than left to that line, because
// a workflow file can be edited and a local clone can be shallow for reasons of its
// own, and this is the only place that can tell.
//
// Exit 2, with the parse controls below, because all three are the same answer:
// refusing to report rather than reporting nothing.
if (git('rev-parse', '--is-shallow-repository').trim() === 'true') {
  console.error('SHALLOW CLONE: this gate reads the neutral resx value history out of git, and a');
  console.error('shallow clone does not have it. It would find nothing and print "clean", which is');
  console.error('indistinguishable from a real pass. Refusing to report on it.');
  console.error('');
  console.error('Fetch the full history first. In a workflow that is fetch-depth: 0 on the checkout');
  console.error('step; locally it is `git fetch --unshallow`.');
  process.exit(2);
}

// The parse control's two legs, applied to one historical revision. Separate from
// parse() below only because a revision is named by commit as well as by path, and
// because the message has to say which commit so the reader can go and look at it.
const historyControl = (where, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${where}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('The value history this gate compares against would be incomplete, and an');
  console.error('incomplete history reports every satellite clean. Refusing to report on it.');
  process.exit(2);
};

// Parse control, the same one refresh-template-english.mjs carries: a regex that
// has stopped matching reports a clean sweep over a read that never happened.
//
// TWO LEGS, AND IT ONLY HAD ONE UNTIL 3.0.0. `map.size !== raw` cannot fire when
// both are zero, so this gate read a neutral truncated to its XML header and
// printed "Superseded-English gate: clean. No satellite holds a wording the English
// has replaced." over a file with nothing in it, exit 0. The word clean, over
// nothing. `raw === 0` is the missing half. Counted with <data\b rather than
// '<data ' so a tab after the tag name is not read as an empty file, and neither
// figure is written down, so adding a string cannot make either go stale.
const parse = (xml, where) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>[\s\S]*?<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  if (raw === 0 || map.size !== raw) {
    console.error(`PARSE CONTROL FAILED for ${where}: ${raw} '<data' occurrence(s), ${map.size} parsed.`);
    process.exit(2);
  }
  return map;
};

// The keys a satellite legitimately keeps in English, mirroring the two gates
// that already scope them out. Kept short on purpose: this check only ever asks
// whether a value is an OLD English one, and a deliberate keep is the CURRENT
// English, so only the machine keys and the format templates can collide.
const isMachineCliKey = (key) =>
  key.startsWith('Cli.') && key.includes('EventLog') && key !== 'Cli.EventLogUnavailable';

const neutral = parse(readFileSync(NEUTRAL, 'utf8'), NEUTRAL);

// Every value each key has ever held in the neutral, oldest commit to newest.
//
// CONTROLLED PER REVISION, and it is the same control parse() carries rather than a
// second idea. A revision read as empty contributes no former wording, so no
// satellite value can match one, so this gate prints "clean" for the same reason it
// would over a resx it never opened. The whole finding rests on this map being
// complete.
//
// THE catch BELOW IS WHAT MAKES THE CONTROL SAFE AND IT IS LOAD-BEARING. A revision
// from before this file existed makes `git show` fail, and that revision is skipped
// without ever reaching the control: absent at this commit and present-but-read-as-
// empty are different things and only the second is a fault. So a red here is a
// real change of shape rather than a revision the parser could not reach.
const revs = git('rev-list', 'HEAD', '--', NEUTRAL).split('\n').filter(Boolean);
const history = new Map();
for (const rev of revs) {
  let xml;
  try { xml = git('show', `${rev}:${NEUTRAL}`); } catch { continue; }
  const re = /<data\s+name="([^"]+)"[^>]*>[\s\S]*?<value>([\s\S]*?)<\/value>/g;
  const seen = new Set();
  let m;
  while ((m = re.exec(xml)) !== null) {
    seen.add(m[1]);
    if (!history.has(m[1])) history.set(m[1], new Set());
    history.get(m[1]).add(m[2]);
  }
  historyControl(`${NEUTRAL} at ${rev.slice(0, 8)}`, xml, seen.size);
}

const satellites = readdirSync(dir)
  .filter((f) => /^Strings\.[A-Za-z-]+\.resx$/.test(f) && f !== 'Strings.resx')
  .sort();

// The neutral key an override overrides, by the generators' own rule.
const overrideBase = (key) => {
  const prefix = key.replace(/\.(?:One|Few|Many)$/, '');
  if (neutral.has(`${prefix}.Plural`)) return `${prefix}.Plural`;
  return neutral.has(prefix) ? prefix : null;
};

let total = 0;
const distinct = new Set();
for (const file of satellites) {
  const code = file.replace(/^Strings\./, '').replace(/\.resx$/, '');
  const sat = parse(readFileSync(`${dir}/${file}`, 'utf8'), file);
  const hits = [];
  for (const [key, value] of sat) {
    if (isMachineCliKey(key)) continue;
    const base = neutral.has(key) ? key : overrideBase(key);
    if (base === null) continue;                       // an override with no neutral counterpart
    if (value === neutral.get(base)) continue;         // check-still-english's job, not this one
    if (history.get(base)?.has(value)) hits.push({ key, base });
  }
  if (hits.length) {
    total += hits.length;
    for (const h of hits) distinct.add(h.key);
    console.log(`${code}: ${hits.length} value(s) holding a superseded English value:`);
    for (const h of hits)
      console.log(`  ${h.key}${h.key === h.base ? '' : ` (against ${h.base})`}`);
  }
}

console.log(`\nTOTALS: ${revs.length} commit(s) of the neutral read, ${history.size} key(s) with a value `
  + `history, ${satellites.length} satellite(s) checked; ${total} key-slot(s) holding superseded English `
  + `across ${distinct.size} distinct key(s).`);

if (drift) {
  // TRIAGE, never a verdict. Ordering all of HEAD's commits once gives a total
  // order in which a LOWER index is NEWER, so "the satellite has not been touched
  // since the English moved" is an integer comparison rather than a date one.
  const order = new Map(git('rev-list', 'HEAD').split('\n').filter(Boolean).map((r, i) => [r, i]));
  const lastChange = (path) => {
    const own = git('rev-list', '--reverse', 'HEAD', '--', path).split('\n').filter(Boolean);
    let prev = new Map();
    const out = new Map();
    for (const rev of own) {
      let xml;
      try { xml = git('show', `${rev}:${path}`); } catch { continue; }
      const cur = new Map();
      const re = /<data\s+name="([^"]+)"[^>]*>[\s\S]*?<value>([\s\S]*?)<\/value>/g;
      let m;
      while ((m = re.exec(xml)) !== null) cur.set(m[1], m[2]);
      // Controlled per revision like the history above. --drift is triage rather than
      // a gate, but a reading list built from a partial read is a reading list with
      // entries missing, and nothing about it would look wrong.
      historyControl(`${path} at ${rev.slice(0, 8)}`, xml, cur.size);
      for (const [k, v] of cur) if (prev.get(k) !== v) out.set(k, order.get(rev));
      prev = cur;
    }
    return out;
  };
  const neutralWhen = lastChange(NEUTRAL);
  console.log('\n--drift: values last touched BEFORE their English last moved. '
    + 'TRIAGE, NOT A VERDICT: a cosmetic edit to the English lands here too, '
    + 'and reads exactly like a real one. Open each before acting on it.');
  let driftTotal = 0;
  for (const file of satellites) {
    const code = file.replace(/^Strings\./, '').replace(/\.resx$/, '');
    const satNow = parse(readFileSync(`${dir}/${file}`, 'utf8'), file);
    const satWhen = lastChange(`${dir}/${file}`);
    // A key that has since been RENAMED away still has a change history in both
    // files, so without this it is reported as drifted for ever and the list
    // fills with keys neither file holds. Only what is in the tree now counts.
    const stale = [...satWhen]
      .filter(([k, when]) => !isMachineCliKey(k) && neutral.has(k) && satNow.has(k)
        && neutralWhen.has(k) && when > neutralWhen.get(k))
      .map(([k]) => k).sort();
    driftTotal += stale.length;
    if (stale.length) console.log(`  ${code}: ${stale.join(', ')}`);
  }
  console.log(`  drift TOTAL: ${driftTotal} key-slot(s) to read.`);
}

if (total > 0) {
  console.error(`\nSuperseded-English gate: ${total} key-slot(s) hold English the neutral has replaced.`);
  console.error('These are untranslated, not stale translations: the value is a wording the app dropped.');
  console.error('Fix each in its generator (scripts/translations/gen-strings-<code>.mjs) and regenerate.');
  process.exit(1);
}
console.log('\nSuperseded-English gate: clean. No satellite holds a wording the English has replaced.');
