#!/usr/bin/env node
// check-machine-contract-scope.mjs: fails where a test composes an Application-channel
// value outside the en-GB scope its write site builds it in.
//
// The command line's Application-log entries are read by machines, so they are built
// in en-GB whatever the PC's language: every write site passes a lambda to
// MachineContract.English or MachineContract.WriteEventLog, which applies the scope
// itself. A test that builds one of these values outside that scope gets it in the
// language the test runs in rather than the en-GB value the channel carries.
//
// What is read: every .cs file in the test project, through csharp-source.mjs, with the
// comments and the text inside strings taken out, so every match is code. What is
// matched: a call to any method in BUILDERS, and any Strings.Cli_EventLog* value but the
// stdout note (RAW_VALUE). What is in scope: anything inside the argument list of a
// MachineContract.English or MachineContract.WriteEventLog call, found by balancing its
// brackets, since the call sites here wrap across lines.
//
// The production code is not read. The builders' own bodies read these values outside
// any scope, being what each write site wraps.
//
// Every reference is printed with its file, line and what it names, in scope or not.
// Exit 1 where one is out of scope. Exit 2 where a file cannot be read to its end, a
// scope call's argument list does not close, or fewer than 12 references are found.
//
// Usage (from the repo root):
//   node scripts/check-machine-contract-scope.mjs
import { readFileSync } from 'node:fs';
import { argumentSpan, readCSharp } from './csharp-source.mjs';
import { lineIndex, sourceFiles } from './source-files.mjs';

const TESTS = 'src/InstallerClean.Tests';

// The builders whose result is an Application-channel line, and the raw resx values
// those lines are made of. Both spellings reach the same fault: the first through a
// method that reads the value, the second by reading it directly.
//
// Cli.EventLogUnavailable is not one of those values. It is the note the command line
// prints to standard output, in the PC's language, saying the Application log refused an
// entry, so a test composes it outside any scope and the pattern leaves it out.
const BUILDERS = [
  'AbortedMoveEventLogLine',
  'BatchEndedEventLogLine',
  'PendingRebootEventLogLine',
  'MoveDestinationInsideInstallerEventLogLine',
  'PendingRebootEventLogReason',
  'AdminRightsNeededEventLogLine',
  'SourcesGivenUpEventLogLine',
  'SourcesGivenUpNoticeEventLogLine',
  'UpdatedEventLogLine',
  'UpdateFailedEventLogLine',
  'UpdateFailedThenCancelledEventLogLine',
];
const RAW_VALUE = 'Strings\\.Cli_EventLog(?!Unavailable\\b)[A-Za-z0-9_]*';
const SCOPES = ['MachineContract.English', 'MachineContract.WriteEventLog'];

const problems = [];
const refusals = [];
const references = [];

for (const file of sourceFiles(TESTS, '.cs')) {
  let code;
  try {
    code = readCSharp(readFileSync(file, 'utf8')).bare;
  } catch (e) {
    refusals.push(`${file}: cannot be read to its end (${e.message})`);
    continue;
  }
  const lineOf = lineIndex(code);

  const spans = [];
  for (const scope of SCOPES) {
    const re = new RegExp(scope.replace('.', '\\.') + '\\s*\\(', 'g');
    for (const m of code.matchAll(re)) {
      const span = argumentSpan(code, code.indexOf('(', m.index));
      if (span) spans.push(span);
      else refusals.push(`${file}:${lineOf(m.index)} a ${scope} call whose argument list does not close`);
    }
  }

  const needles = BUILDERS.map((b) => [`\\b${b}\\s*\\(`, b]).concat([[RAW_VALUE, null]]);
  for (const [pattern, name] of needles) {
    for (const m of code.matchAll(new RegExp(pattern, 'g'))) {
      const inScope = spans.some(([from, to]) => m.index > from && m.index < to);
      const line = lineOf(m.index);
      const what = name ?? m[0];
      references.push(`  ${file}:${line}  ${what}  ${inScope ? 'in scope' : 'OUTSIDE the scope'}`);
      if (!inScope)
        problems.push(
          `${file}:${line} ${what} is composed outside MachineContract.English. The write `
          + 'site builds this line in en-GB; built here, it takes the language the test '
          + 'runs in.');
    }
  }
}

console.log('Machine-contract references in the test project:');
for (const r of references) console.log(r);
for (const p of problems) console.error(`  ${p}`);
for (const r of refusals) console.error(`  REFUSING: ${r}`);
if (refusals.length) process.exit(2);

// A run matching nothing would print no problem, and renaming the builders or the
// values is what would cause it. A floor rather than a count, so that adding a test
// does not fail this.
if (references.length < 12) {
  console.error(`FLOOR FAILED: ${references.length} reference(s) found, expected at least 12.`);
  console.error('Refusing to report clean over a set this small: the builders or the values have been renamed.');
  process.exit(2);
}

console.log(`TOTALS: ${references.length} machine-contract reference(s) in the test project, `
  + `${references.length - problems.length} inside the scope, ${problems.length} outside it.`);
process.exit(problems.length ? 1 : 0);
