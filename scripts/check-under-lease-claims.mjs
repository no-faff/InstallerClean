#!/usr/bin/env node
// check-under-lease-claims.mjs: fails where a production call to an action service
// passes anything but the claims the pre-lease re-verify produced.
//
// Both action services take the claims as a required, non-nullable argument, so the
// compiler holds every caller to passing something and not to passing the right
// thing. UnderLeaseClaims.None is a legal value, and RecheckUnderLease returns
// before asking anything for an empty batch, so a caller passing None gets a pass
// from the last check in front of a permanent delete.
//
// What is matched: every use of the name DeleteFilesAsync or MoveFilesAsync followed by
// a bracket, under src/ outside the test project, read through csharp-source.mjs with
// the comments and the text inside strings taken out. A call with no receiver, as in a
// default interface method or a call inside the service, is matched like any other.
// One whose argument list declares a parameter of type UnderLeaseClaims is a
// declaration: it gives the position of the claims parameter and is not a call. The
// interfaces and the services must agree on that position, and the run refuses if
// they do not.
//
// The claims argument of each call is the one named underLeaseClaims:, or else the one
// at that position, split at the commas outside any bracket. It must be the whole
// expression UnderLeaseClaims.From(...), or a local whose one assignment in the file is
// exactly that. So a conditional with None on one arm, a From buried inside another
// expression, and a local assigned anything else all fail. What From hands the service
// is held by CliUnderLeaseClaimsTests and MainViewModelTests, which drive the call sites
// and read what the service received.
//
// The test project is not read. Its tests hand the services an empty batch on
// purpose, to test what a service does with one.
//
// Every call site is printed with its claims argument. Exit 1 where one is not From;
// exit 2 where a file cannot be read to its end, an argument list does not close, a
// method's claims position cannot be found or is declared two ways, or fewer than 4 call
// sites are found.
//
// Usage (from the repo root):
//   node scripts/check-under-lease-claims.mjs
import { readFileSync } from 'node:fs';
import { argumentSpan, readCSharp } from './csharp-source.mjs';
import { lineIndex, sourceFiles } from './source-files.mjs';

const ROOT = 'src';
const TESTS = `${ROOT}/InstallerClean.Tests/`;
const METHODS = ['DeleteFilesAsync', 'MoveFilesAsync'];
const PARAMETER = 'underLeaseClaims';
const FROM = /^UnderLeaseClaims\s*\.\s*From\s*\(/;

const problems = [];
const refusals = [];

// The arguments of a call, split at the commas outside any bracket, each with its
// offset in the file. Angle brackets are counted only when asked, for a parameter
// list, where a comma inside Func<string, bool> is not a separator; in an argument
// list a '<' can be a comparison.
const splitArguments = (code, [from, to], angles) => {
  const out = [];
  let depth = 0;
  let start = from;
  for (let i = from; i < to; i++) {
    const c = code[i];
    if ('([{'.includes(c) || (angles && c === '<')) depth++;
    else if (')]}'.includes(c) || (angles && c === '>')) depth--;
    else if (c === ',' && depth === 0) {
      out.push({ text: code.slice(start, i), at: start });
      start = i + 1;
    }
  }
  out.push({ text: code.slice(start, to), at: start });
  return out;
};

// The whole of a From(...) expression: From and its brackets, nothing after them.
const isWholeFrom = (code, text, at) => {
  const trimmed = text.trim();
  const m = FROM.exec(trimmed);
  if (!m) return false;
  const lead = text.length - text.trimStart().length;
  const open = at + lead + m[0].length - 1;
  const span = argumentSpan(code, open);
  return span !== null && code.slice(span[1] + 1, at + text.length).trim() === '';
};

const files = [];
for (const file of sourceFiles(ROOT, '.cs').filter((f) => !f.startsWith(TESTS))) {
  try {
    files.push({ file, code: readCSharp(readFileSync(file, 'utf8')).bare });
  } catch (e) {
    refusals.push(`${file}: cannot be read to its end (${e.message})`);
  }
}

// Declarations first, for the position of the claims parameter in each method.
const positions = new Map(METHODS.map((m) => [m, new Set()]));
const calls = [];
for (const { file, code } of files) {
  const lineAt = lineIndex(code);
  for (const method of METHODS) {
    for (const m of code.matchAll(new RegExp(`\\b${method}\\s*\\(`, 'g'))) {
      const line = lineAt(m.index);
      const span = argumentSpan(code, code.indexOf('(', m.index));
      if (span === null) {
        refusals.push(`${file}:${line} ${method} with an argument list that does not close`);
        continue;
      }
      const parameters = splitArguments(code, span, true);
      const claims = parameters.findIndex((p) => /\bUnderLeaseClaims\s+@?\w+\s*(=|$)/.test(p.text.trim()));
      if (claims !== -1) {
        positions.get(method).add(claims);
        console.log(`  declared: ${file}:${line}  ${method}, claims at argument ${claims + 1}`);
      } else {
        calls.push({ file, code, line, method, span });
      }
    }
  }
}

for (const [method, at] of positions) {
  if (at.size === 0) refusals.push(`no declaration of ${method} with an UnderLeaseClaims parameter was found`);
  if (at.size > 1) refusals.push(`${method} declares its claims parameter at more than one position: ${[...at].map((n) => n + 1).join(', ')}`);
}
for (const r of refusals) console.error(`  REFUSING: ${r}`);
if (refusals.length) process.exit(2);

console.log('Production calls to the action services:');
for (const { file, code, line, method, span } of calls) {
  const position = [...positions.get(method)][0];
  const args = splitArguments(code, span, false);
  const named = args.find((a) => new RegExp(`^\\s*${PARAMETER}\\s*:(?!:)`).test(a.text));
  let claims;
  if (named) {
    const colon = named.text.indexOf(':');
    claims = { text: named.text.slice(colon + 1), at: named.at + colon + 1 };
  } else {
    // Positional arguments come before any named one, so the position counts them.
    claims = args[position] && !/^\s*@?\w+\s*:(?!:)/.test(args[position].text) ? args[position] : null;
  }
  const shown = claims ? claims.text.trim().replace(/\s+/g, ' ') : '(none)';
  let passes = false;
  if (claims && isWholeFrom(code, claims.text, claims.at)) {
    passes = true;
  } else if (claims && /^@?\w+$/.test(claims.text.trim())) {
    // A local holding the claims: its one assignment in the file must be the whole From.
    const name = claims.text.trim().replace(/^@/, '');
    const assignments = [...code.matchAll(new RegExp(`(?<![\\w.])@?${name}\\s*(=(?![=>])|\\+=|\\?\\?=)`, 'g'))];
    const byReference = new RegExp(`\\b(ref|out)\\s+(var\\s+|UnderLeaseClaims\\s+)?@?${name}\\b`).test(code);
    if (assignments.length === 1 && assignments[0][1] === '=' && !byReference) {
      const valueAt = assignments[0].index + assignments[0][0].length;
      const end = code.indexOf(';', valueAt);
      passes = end !== -1 && isWholeFrom(code, code.slice(valueAt, end), valueAt);
    }
  }
  console.log(`  ${file}:${line}  ${method}  claims: ${shown}  ${passes ? 'is From' : 'NOT From'}`);
  if (!passes)
    problems.push(
      `${file}:${line} ${method} is passed ${shown} as its claims. The claims the `
      + 'pre-lease re-verify produced, UnderLeaseClaims.From(...), are what the under-lease '
      + 're-read is for; anything else can hand it an empty batch, and it returns a pass '
      + 'without asking.');
}

for (const p of problems) console.error(`  ${p}`);

// A run matching no call site would print no problem, and renaming the methods or
// changing how the calls are written is what would cause it. A floor rather than a
// count, so that adding a caller does not fail this.
if (calls.length < 4) {
  console.error(`FLOOR FAILED: ${calls.length} production call site(s) found, expected at least 4.`);
  console.error('Refusing to report clean over a set this small: the methods have been renamed, moved or rewritten.');
  process.exit(2);
}

console.log(`TOTALS: ${calls.length} production call site(s) checked, ${problems.length} not passed UnderLeaseClaims.From(...).`);
process.exit(problems.length ? 1 : 0);
