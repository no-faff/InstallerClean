#!/usr/bin/env node
// check-machine-contract-scope.mjs: fail when a TEST composes an Application-channel
// value outside the en-GB scope the write site builds it in.
//
// WHAT THE VALUES ARE. The CLI's Application-log entries are machine-read: RMM tooling
// greps them for known English phrases, so MachineContract forces en-GB at the emit
// site whatever the machine's language. Every write site passes a lambda into
// MachineContract.English or MachineContract.WriteEventLog, which applies the scope
// itself rather than trusting the caller to remember.
//
// WHY THIS EXISTS. A test that builds one of these values without the scope gets the
// host's language instead. On an English machine the two agree, so the test passes and
// CI is English. The value under assertion is then not the value the channel carries,
// and which of the two you get depends on the machine the suite happens to run on.
//
// AND WHY A COMMENT WAS NOT ENOUGH. The methods carry a doc line saying to call them
// from inside the scope. The bare form is always available and always compiles, and a
// reader reaching for it gets a green run. This fails on the day one is written.
//
// SCOPE IS THE TEST PROJECT ONLY, DELIBERATELY. The builders' own bodies read these
// resx values outside any scope, which is correct: they are the thing the write site
// wraps. Pointing this at production would report every one of them and be turned off.
//
// WHAT THIS DOES NOT COVER, AND THE RUN SAYS SO EVERY TIME RATHER THAN LEAVING IT HERE.
// Two other ways a test can read a value in the wrong language, neither of them this:
// a displayed-language value asserted against an English literal outside a
// LocalisationScope, and a counted value composed without the plural rule the run uses.
// A green run here is evidence about the machine-contract values and about nothing else.
//
// THE ARGUMENT SPAN IS PARSED RATHER THAN MATCHED. This project's call sites wrap
// across lines, so a regex over one line cannot see which of them sits inside a scope.
// The scan takes the balanced parenthesis span after each scope call and asks whether
// a reference falls inside one.
//
// Usage (from the repo root):
//   node scripts/check-machine-contract-scope.mjs
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';

const TESTS = 'src/InstallerClean.Tests';

// The builders whose result is an Application-channel line, and the raw resx values
// those lines are made of. Both spellings reach the same fault: the first through a
// method that reads the value, the second by reading it directly.
const BUILDERS = [
  'AbortedMoveEventLogLine',
  'InstallerLockUnavailableEventLogLine',
  'PendingRebootEventLogLine',
  'MoveDestinationInsideInstallerEventLogLine',
  'PendingRebootEventLogReason',
];
const RAW_VALUE = 'Strings\\.Cli_EventLog[A-Za-z0-9_]*';
const SCOPES = ['MachineContract.English', 'MachineContract.WriteEventLog'];

// Comments come off first, so a file discussing a call is not a finding and a call
// commented out is not one either.
const codeOnly = (s) =>
  s.replace(/\/\*[\s\S]*?\*\//g, ' ').replace(/^[ \t]*\/\/.*$/gm, ' ').replace(/\/\/.*$/gm, ' ');

function* csFiles(dir) {
  for (const name of readdirSync(dir)) {
    const path = join(dir, name);
    if (name === 'bin' || name === 'obj') continue;
    if (statSync(path).isDirectory()) yield* csFiles(path);
    else if (name.endsWith('.cs')) yield path;
  }
}

// The balanced span after an opening parenthesis, or null where the file ends first.
function argumentSpan(text, open) {
  let depth = 0;
  for (let i = open; i < text.length; i++) {
    if (text[i] === '(') depth++;
    else if (text[i] === ')' && --depth === 0) return [open + 1, i];
  }
  return null;
}

const problems = [];
let inScope = 0;
let unclosed = 0;

for (const file of csFiles(TESTS)) {
  const code = codeOnly(readFileSync(file, 'utf8'));

  const spans = [];
  for (const scope of SCOPES) {
    const re = new RegExp(scope.replace('.', '\\.') + '\\s*\\(', 'g');
    let m;
    while ((m = re.exec(code)) !== null) {
      const span = argumentSpan(code, code.indexOf('(', m.index));
      if (span) spans.push(span);
      else {
        unclosed++;
        problems.push(`${file}: a ${scope} call whose argument list does not close`);
      }
    }
  }

  const needles = BUILDERS.map((b) => [`\\b${b}\\s*\\(`, b]).concat([[RAW_VALUE, 'a Cli.EventLog value']]);
  for (const [pattern, what] of needles) {
    const re = new RegExp(pattern, 'g');
    let m;
    while ((m = re.exec(code)) !== null) {
      if (spans.some(([from, to]) => m.index > from && m.index < to)) { inScope++; continue; }
      const line = code.slice(0, m.index).split('\n').length;
      problems.push(
        `${file}:${line} ${what} is composed outside MachineContract.English. The write `
        + 'site builds this line in en-GB, so a value built here in the host language is '
        + 'not the value the Application channel carries.');
    }
  }
}

for (const p of problems) console.error(`  ${p}`);

// THE FLOOR IS NOT OPTIONAL. A scan finding nothing reports no problems and reads
// exactly like a clean run, and a rename is what would cause it. A floor rather than a
// pinned count, so that adding a test does not fail this for the wrong reason.
const total = inScope + problems.length;
if (total < 12) {
  console.error(`FLOOR FAILED: ${total} reference(s) found, expected at least 12.`);
  console.error('Refusing to report clean over a set this small: the builders or the values have been renamed.');
  process.exit(2);
}

console.log(`TOTALS: ${total} machine-contract reference(s) in the test project, ${inScope} inside the scope, ${problems.length} outside it.`);
console.log('NOT COVERED by this gate: a displayed-language value asserted against an English');
console.log('literal outside a LocalisationScope, and a counted value composed without the');
console.log('plural rule. A green run here says nothing about either.');
process.exit(problems.length ? 1 : 0);
