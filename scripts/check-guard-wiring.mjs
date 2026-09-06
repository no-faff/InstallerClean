#!/usr/bin/env node
// Fails (exit 1) when a guard that both workflows have to invoke by name has
// lost one of those invocations. Only verify-shipped-artefacts.mjs is held this
// way, and the reason is its name: every other guard here is check-*.mjs, which
// the release workflow picks up as a glob, so a new one is run by the release
// on the day it is written and no list can fall out of step.
//
// verify-shipped-artefacts.mjs cannot be in that glob, because the glob runs
// before anything is built and that guard reads built files. It is invoked by
// name after the publish steps in each workflow instead, and a line invoked by
// name is a line that can be deleted or renamed away while everything stays
// green. This is what makes the two invocations a rule rather than a habit.
//
// YAML COMMENTS ARE STRIPPED FIRST, so a commented-out invocation reads as the
// absence it is rather than as the line it used to be.
//
// Run from the repo root: node scripts/check-guard-wiring.mjs
import { readFileSync } from 'node:fs';

const GUARD = 'scripts/verify-shipped-artefacts.mjs';

const WORKFLOWS = [
  '.github/workflows/ci.yml',
  '.github/workflows/release.yml',
];

const read = (p) => {
  try {
    return readFileSync(p, 'utf8');
  } catch {
    console.error(`check-guard-wiring: cannot read ${p}`);
    process.exit(1);
  }
};

// A run: step may carry the guard anywhere on its line, so the test is on the
// line rather than on the whole file, and a line whose first non-space
// character is # is not an invocation of anything.
const invocations = (yaml) =>
  yaml
    .split('\n')
    .filter((line) => !/^\s*#/.test(line))
    .filter((line) => line.includes(`node ${GUARD}`));

const problems = [];

for (const path of WORKFLOWS) {
  const found = invocations(read(path));
  if (found.length === 0) {
    problems.push(`${path}: does not run "node ${GUARD}" outside a comment`);
  }
}

if (problems.length) {
  console.error(`check-guard-wiring: ${GUARD} has to be invoked by both workflows.\n`);
  for (const p of problems) console.error(`  ${p}`);
  console.error(`\nFix: add a step running "node ${GUARD}" after the publish steps`);
  console.error('in the workflow named above. It reads built files, so it has to run after');
  console.error('they exist, which is why it is invoked by name rather than picked up with');
  console.error('the check-*.mjs guards that run before the build.');
  process.exit(1);
}

console.log(`check-guard-wiring: OK (${WORKFLOWS.length} workflows invoke ${GUARD})`);
