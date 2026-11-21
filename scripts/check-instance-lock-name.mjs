#!/usr/bin/env node
// Fails (exit 1) when the single-instance mutex is named anywhere but InstanceLock.Name,
// or when the setup's AppMutexName define names a different one, or when the setup
// spells the name anywhere but that define.
//
// The window, a command-line Delete or Move, a command-line update and the setup exclude
// each other only through that one name. A second spelling in the source that drifts
// lets two of them run at once; an AppMutex that drifts lets the setup replace files a
// running copy has open. Inno Setup cannot read the C# constant, so the setup spells the
// name itself, once, in a #define that AppMutex and [Code] both use, and this check holds
// that define and the constant together.
//
// The name is read from InstanceLock.Name, and everything else is held to that, so a
// rename that reaches every site needs no change here. A spelling of it elsewhere is
// found by likeness, not by one fixed string: the part after Global\ is folded to its
// letters and digits in lower case, and every string literal in the C#, and every line
// of the setup scripts, is searched for a stretch within a few edits of it, the
// allowance growing with its length. So the exact name and a slip of a letter or two in
// it are both found, and a literal that drifts from the constant cannot open a second
// mutex unnoticed. The closest literal of all is
// printed, so a run that finds nothing shows how far the nearest one was.
//
// The C# is read through csharp-source.mjs, so a comment naming the mutex is not counted
// as a spelling of it and only the text inside strings is searched. The setup scripts
// are read with their comments blanked the same way, through iss-source.mjs.
//
// Run from the repo root: node scripts/check-instance-lock-name.mjs
import { readFileSync } from 'node:fs';
import { readCSharp } from './csharp-source.mjs';
import { readIss } from './iss-source.mjs';
import { lineIndex, sourceFiles } from './source-files.mjs';

const LOCK_FILE = 'src/InstallerClean.Core/Services/InstanceLock.cs';
const SETUP_FILE = 'installer/InstallerClean.iss';
// The test pinning the name spells it on purpose, so a change to the constant fails there.
const PIN_FILE = 'src/InstallerClean.Tests/Services.Integration/InstanceLockTests.cs';

const read = (p) => {
  try {
    return readFileSync(p, 'utf8');
  } catch {
    console.error(`check-instance-lock-name: cannot read ${p}`);
    process.exit(1);
  }
};

const problems = [];

const lockCode = readCSharp(read(LOCK_FILE)).code;
const declared = [...lockCode.matchAll(/const\s+string\s+Name\s*=\s*@"([^"]*)"/g)].map((m) => m[1]);
if (declared.length !== 1) {
  problems.push(`${LOCK_FILE}: expected one 'const string Name = @"..."', found ${declared.length}`);
}
const name = declared[0];

const setupLines = readIss(read(SETUP_FILE))
  .split(/\r?\n/)
  .map((text, i) => ({ line: i + 1, text: text.trim() }))
  .filter((l) => l.text !== '');
const defines = setupLines.filter((l) => /^#define\s+AppMutexName\b/.test(l.text));
const appMutex = setupLines.filter((l) => /^AppMutex\s*=/i.test(l.text));
if (defines.length !== 1) {
  problems.push(`${SETUP_FILE}: expected one #define AppMutexName, found ${defines.length}`);
} else {
  const m = defines[0].text.match(/^#define\s+AppMutexName\s+"([^"]*)"\s*$/);
  const value = m ? m[1] : null;
  console.log(`${SETUP_FILE}:${defines[0].line}: ${defines[0].text}`);
  if (value === null) {
    problems.push(`${SETUP_FILE}:${defines[0].line}: AppMutexName is not a plain quoted string`);
  } else if (name !== undefined && value !== name) {
    problems.push(`${SETUP_FILE}:${defines[0].line}: AppMutexName is '${value}', InstanceLock.Name is '${name}'`);
  }
}
if (appMutex.length !== 1) {
  problems.push(`${SETUP_FILE}: expected one AppMutex line, found ${appMutex.length}`);
} else {
  console.log(`${SETUP_FILE}:${appMutex[0].line}: ${appMutex[0].text}`);
  if (appMutex[0].text.replace(/\s+/g, '') !== 'AppMutex={#AppMutexName}') {
    problems.push(`${SETUP_FILE}:${appMutex[0].line}: AppMutex does not name {#AppMutexName}`);
  }
}
// The wait in [Code] polls the define.
const waits = setupLines.filter((l) => /CheckForMutexes\s*\(/.test(l.text));
if (waits.length === 0) problems.push(`${SETUP_FILE}: no CheckForMutexes call in [Code]`);
for (const l of waits) {
  console.log(`${SETUP_FILE}:${l.line}: ${l.text}`);
  if (!/CheckForMutexes\s*\(\s*'\{#AppMutexName\}'\s*\)/.test(l.text)) {
    problems.push(`${SETUP_FILE}:${l.line}: CheckForMutexes does not poll '{#AppMutexName}'`);
  }
}
// The name, folded to what a near-miss is compared on: the part after the namespace,
// letters and digits only, in lower case.
const fold = (text) => text.toLowerCase().replace(/[^a-z0-9]/g, '');
if (name === undefined) {
  for (const p of problems) console.error(`check-instance-lock-name: ${p}`);
  process.exit(1);
}
const mark = fold(name.replace(/^(Global|Local)\\/, ''));
if (mark.length < 12) {
  console.error(`check-instance-lock-name: the folded name '${mark}' is too short to search for by likeness`);
  process.exit(2);
}
// Edits a spelling can be from the name and still be counted as a spelling of it: one
// for every nine letters, so a short name is not matched to the product's own name,
// which every name here starts with.
const NEAR = Math.floor(mark.length / 9);

// The fewest edits that turn mark into some stretch of text, both already folded.
const nearest = (text) => {
  let row = new Array(text.length + 1).fill(0);
  for (let i = 1; i <= mark.length; i++) {
    const next = [i];
    for (let j = 1; j <= text.length; j++) {
      next[j] = Math.min(row[j] + 1, next[j - 1] + 1, row[j - 1] + (mark[i - 1] === text[j - 1] ? 0 : 1));
    }
    row = next;
  }
  return Math.min(...row);
};
let closest = { distance: Infinity, where: '(none)' };
const weigh = (distance, where) => {
  if (distance < closest.distance) closest = { distance, where };
};

// The setup spells the name once, in the define, and names the define everywhere else.
for (const path of sourceFiles('installer', '.iss')) {
  const lines = readIss(read(path)).split(/\r?\n/);
  lines.forEach((text, i) => {
    if (path === SETUP_FILE && /^\s*#define\s+AppMutexName\b/.test(text)) return;
    const distance = nearest(fold(text));
    weigh(distance, `${path}:${i + 1}`);
    if (distance <= NEAR) problems.push(`${path}:${i + 1}: names the mutex, or nearly; use {#AppMutexName}`);
  });
}
console.log(`${LOCK_FILE}: InstanceLock.Name = ${name}`);

// Every string literal in the C#, outside the constant itself and the test pinning it.
// A literal is a run of string text, which is where code and bare differ; a space reads
// the same in both and is left out by the folding, so it neither ends a literal nor adds
// to one.
for (const path of sourceFiles('src', '.cs')) {
  if (path === PIN_FILE) continue;
  const { code, bare } = readCSharp(read(path));
  const lineOf = lineIndex(code);
  let literal = '';
  let start = -1;
  const close = () => {
    if (start !== -1) {
      const distance = nearest(literal);
      const exempt = path === LOCK_FILE && literal === fold(name);
      if (!exempt) {
        weigh(distance, `${path}:${lineOf(start)}`);
        if (distance <= NEAR) problems.push(`${path}:${lineOf(start)}: names the mutex, or nearly; use InstanceLock.Name`);
      }
    }
    literal = '';
    start = -1;
  };
  for (let i = 0; i < code.length; i++) {
    if (code[i] !== bare[i]) {
      if (start === -1) start = i;
      literal += fold(code[i]);
    } else if (code[i] !== ' ') {
      close();
    }
  }
  close();
}
console.log(`closest other spelling: ${closest.where}, ${closest.distance} edit(s) from '${mark}' (counted at ${NEAR} or fewer)`);

if (problems.length > 0) {
  for (const p of problems) console.error(`check-instance-lock-name: ${p}`);
  process.exit(1);
}
console.log('check-instance-lock-name: one name for the mutex, and the setup names it through its define');
