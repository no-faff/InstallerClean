#!/usr/bin/env node
// Fails (exit 1) when a guard a workflow has to invoke by name has lost that
// invocation. Every other guard here is check-*.mjs, which the release workflow
// picks up as a glob, so a new one is run by the release on the day it is
// written and no list can fall out of step.
//
// The two held here read built files, so neither can be in that glob, which runs
// before anything is built. Each is invoked by name after the publish steps
// instead, and a line invoked by name is a line that can be deleted or renamed
// away while everything stays green. This is what makes those invocations a rule
// rather than a habit.
//
// verify-shipped-artefacts.mjs is run by both workflows. check-native-imports.ps1
// reads the two merged exes build-and-test builds and is run by ci.yml alone: the
// release builds the same projects with the same SDK and merges them the same way.
//
// THE INVOCATION HAS TO OPEN THE COMMAND, after the indent and an optional
// "run:". A line carrying the name anywhere else is text about the guard rather
// than a step that runs it: a commented-out step, a step name, an environment
// value, a string another command echoes. Anchoring it there is what lets this
// file leave YAML's comment rules alone, since it never has to work out whether
// a # opens a comment or sits inside a quoted string.
//
// Run from the repo root: node scripts/check-guard-wiring.mjs
import { readFileSync } from 'node:fs';

const GUARDS = [
  {
    command: 'node scripts/verify-shipped-artefacts.mjs',
    workflows: ['.github/workflows/ci.yml', '.github/workflows/release.yml'],
  },
  {
    command: './scripts/check-native-imports.ps1',
    workflows: ['.github/workflows/ci.yml'],
  },
];

const read = (p) => {
  try {
    return readFileSync(p, 'utf8');
  } catch {
    console.error(`check-guard-wiring: cannot read ${p}`);
    process.exit(1);
  }
};

// The test is on the line rather than on the whole file, and it takes both
// shapes these workflows use for a step: straight after "run:", and alone on its
// own line inside a block scalar. Whatever follows the command is arguments or a
// trailing comment, and the step runs the guard on either reading. The command is
// a literal, so every character a regular expression would read as anything but
// itself is escaped.
const runs = (command) =>
  new RegExp(String.raw`^\s*(?:run:\s+)?${command.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}(?:\s|$)`);

const invocations = (yaml, command) =>
  yaml
    .split('\n')
    .map((text, i) => ({ line: i + 1, text }))
    .filter(({ text }) => runs(command).test(text));

const problems = [];
const wired = [];

for (const { command, workflows } of GUARDS) {
  for (const path of workflows) {
    const found = invocations(read(path), command);
    if (found.length === 0) problems.push(`${path}: no step runs "${command}"`);
    for (const { line, text } of found) wired.push(`  ${path}:${line}: ${text.trim()}`);
  }
}

if (problems.length) {
  console.error('check-guard-wiring: a guard that reads built files is not invoked where it has to be.\n');
  for (const p of problems) console.error(`  ${p}`);
  console.error('\nFix: add a step running the command above after the publish steps in the');
  console.error('workflow named. The guard reads built files, so it has to run after they');
  console.error('exist, which is why it is invoked by name rather than picked up with the');
  console.error('check-*.mjs guards that run before the build. The command has to open the');
  console.error('step, either straight after "run:" or alone on a line in a block scalar.');
  process.exit(1);
}

console.log('check-guard-wiring: OK, every guard that reads built files is invoked:');
for (const w of wired) console.log(w);
