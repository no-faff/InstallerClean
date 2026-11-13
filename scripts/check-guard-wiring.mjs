#!/usr/bin/env node
// Fails (exit 1) when a guard a workflow has to invoke by name has lost that
// invocation. Every other guard here is check-*.mjs, which the release workflow
// picks up as a glob, so a new one is run by the release on the day it is
// written and no list can fall out of step.
//
// The guards held here read built files, so none can be in that glob, which runs
// before anything is built. Each is invoked by name after the publish steps
// instead, and a line invoked by name is a line that can be deleted or renamed
// away while everything stays green. This is what makes those invocations a rule
// rather than a habit.
//
// Each names the workflows that run it. check-native-imports.ps1 and
// check-framework-references.ps1 read the two merged exes build-and-test builds and
// are run by ci.yml alone: the release builds the same projects with the same SDK and
// merges them the same way.
//
// THE INVOCATION HAS TO OPEN THE COMMAND, after the indent and an optional
// "run:". A line carrying the name anywhere else is text about the guard rather
// than a step that runs it: a commented-out step, a step name, an environment
// value, a string another command echoes. Anchoring it there is what lets this
// file leave YAML's comment rules alone, since it never has to work out whether
// a # opens a comment or sits inside a quoted string.
//
// THE STEP AROUND IT HAS TO RUN IT AND FAIL WITH IT. The step holding the invocation,
// and the job holding that step, are read for the keys that stop a failing guard from
// failing the run: continue-on-error, which passes the run whatever the guard says,
// unless it is false; and if:, which can leave the step out on a push, unless it is
// one of RUNS_ANYWAY, each of which runs the step on every run that is not cancelled
// or after one that has not failed. The guard also has to be the last command of its
// run:. GitHub runs a pwsh step as a script ending in an exit with $LASTEXITCODE, so a
// native command after the guard sets the step's exit code in its place.
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
  {
    command: './scripts/check-one-file.ps1',
    workflows: ['.github/workflows/ci.yml', '.github/workflows/release.yml'],
  },
  {
    command: './scripts/check-framework-references.ps1',
    workflows: ['.github/workflows/ci.yml'],
  },
  {
    command: './scripts/check-architecture.ps1',
    workflows: ['.github/workflows/ci.yml', '.github/workflows/release.yml'],
  },
  {
    command: './scripts/check-report-bytes.ps1',
    workflows: ['.github/workflows/ci.yml'],
  },
];

// The if: values a guard's step or job may carry: each runs the step on every run the
// default would, and success() || failure() on the runs where an earlier step failed
// as well. Compared with the spaces taken out.
const RUNS_ANYWAY = ['success()', 'success()||failure()', 'always()', '!cancelled()'];

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

const indentOf = (text) => text.length - text.trimStart().length;
const meaningful = (text) => text.trim() !== '' && !text.trim().startsWith('#');

// The keys of a YAML mapping that opens at line `from`, at the indent its first key
// has, read to the first meaningful line at a lower indent or at the same indent that
// opens a new list item. A key on the "- " line itself counts as one of the item's.
const keysOf = (lines, from, indent) => {
  const keys = new Map();
  for (let i = from; i < lines.length; i++) {
    const text = lines[i];
    if (!meaningful(text)) continue;
    let at = indentOf(text);
    let rest = text.trimStart();
    if (i > from && (at < indent || (at === indent - 2 && rest.startsWith('- ')))) break;
    if (i === from && rest.startsWith('- ')) {
      rest = rest.slice(2).trimStart();
      at = indent;
    }
    if (at !== indent) continue;
    const m = /^([\w-]+)\s*:\s*(.*)$/.exec(rest);
    if (m) keys.set(m[1], { line: i + 1, value: m[2].replace(/\s+#.*$/, '').trim() });
  }
  return keys;
};

// The step and the job around a line: the nearest "- " item above it at a lower
// indent, and the nearest key above that at the job's indent, under jobs:.
const enclosing = (lines, index) => {
  const own = indentOf(lines[index]);
  let step = -1;
  for (let i = index; i >= 0; i--) {
    if (meaningful(lines[i]) && indentOf(lines[i]) < own && lines[i].trimStart().startsWith('- ')) {
      step = i;
      break;
    }
  }
  let job = -1;
  let jobs = -1;
  for (let i = step; i >= 0; i--) if (/^jobs\s*:/.test(lines[i])) { jobs = i; break; }
  if (step !== -1 && jobs !== -1) {
    for (let i = step; i > jobs; i--) {
      if (meaningful(lines[i]) && indentOf(lines[i]) < indentOf(lines[step]) && /^\s*[\w-]+\s*:\s*$/.test(lines[i])) {
        const first = lines.slice(jobs + 1).find(meaningful);
        if (first && indentOf(lines[i]) === indentOf(first)) { job = i; break; }
      }
    }
  }
  return { step, job };
};

// What is wrong with a step or a job's keys, for a guard it holds.
const keyProblems = (keys, what) => {
  const out = [];
  const coe = keys.get('continue-on-error');
  if (coe && coe.value !== 'false')
    out.push(`its ${what} has continue-on-error: ${coe.value} (line ${coe.line}), so a failing guard passes the run`);
  const cond = keys.get('if');
  if (cond && !RUNS_ANYWAY.includes(cond.value.replace(/^\$\{\{\s*|\s*\}\}$/g, '').replace(/\s+/g, '')))
    out.push(`its ${what} has if: ${cond.value} (line ${cond.line}), which can leave the guard out of a run`);
  return out;
};

const invocations = (yaml, command) => {
  const lines = yaml.split('\n');
  const out = [];
  lines.forEach((text, index) => {
    if (!runs(command).test(text)) return;
    const issues = [];
    const { step, job } = enclosing(lines, index);
    if (step === -1) {
      issues.push('no step is found around it');
    } else {
      const stepIndent = indentOf(lines[step]) + 2;
      const stepKeys = keysOf(lines, step, stepIndent);
      issues.push(...keyProblems(stepKeys, 'step'));
      if (job === -1) issues.push('no job is found around its step');
      else issues.push(...keyProblems(keysOf(lines, job + 1, indentOf(lines.slice(job + 1).find(meaningful))), 'job'));
      // Inside a block scalar, the guard has to be its last meaningful line. On the
      // run: line itself it is the whole command.
      if (!/^\s*run:/.test(text)) {
        for (let i = index + 1; i < lines.length; i++) {
          if (!meaningful(lines[i])) continue;
          if (indentOf(lines[i]) < indentOf(text)) break;
          issues.push(`line ${i + 1} runs after it in the same step, and its exit code is the step's: ${lines[i].trim()}`);
          break;
        }
      }
    }
    out.push({ line: index + 1, text, issues });
  });
  return out;
};

const problems = [];
const wired = [];

for (const { command, workflows } of GUARDS) {
  for (const path of workflows) {
    const found = invocations(read(path), command);
    if (found.length === 0) problems.push(`${path}: no step runs "${command}"`);
    for (const { line, text, issues } of found) {
      wired.push(`  ${path}:${line}: ${text.trim()}${issues.length ? '  NOT RUN AS A GUARD' : ''}`);
      for (const issue of issues) problems.push(`${path}:${line}: "${command}": ${issue}`);
    }
  }
}

for (const w of wired) console.log(w);
if (problems.length) {
  console.error('check-guard-wiring: a guard that reads built files is not run where it has to be, or cannot fail its run.\n');
  for (const p of problems) console.error(`  ${p}`);
  console.error('\nFix: add a step running the command above after the publish steps in the');
  console.error('workflow named. The guard reads built files, so it has to run after they');
  console.error('exist, which is why it is invoked by name rather than picked up with the');
  console.error('check-*.mjs guards that run before the build. The command has to open the');
  console.error('step, either straight after "run:" or alone on a line in a block scalar,');
  console.error('be its last command, and run in a step and a job that neither skip it nor');
  console.error('pass the run when it fails.');
  process.exit(1);
}

console.log('check-guard-wiring: OK, every guard that reads built files is invoked, and fails its run when it fails.');
