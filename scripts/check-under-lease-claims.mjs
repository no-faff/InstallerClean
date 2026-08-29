#!/usr/bin/env node
// check-under-lease-claims.mjs: fail when a production call to an action service
// passes anything but the claims the pre-lease re-verify produced.
//
// WHY THIS EXISTS AND WHY THE COMPILER CANNOT DO IT. Both action services take the
// claims as a required, non-nullable argument, so the compiler proves every caller
// passes something. It cannot prove they pass the right thing.
// UnderLeaseClaims.None is a legal value and RecheckUnderLease returns at its first
// line for an empty batch, so a caller passing None receives a pass from the last
// check standing in front of a permanent delete without anything having been asked.
//
// AND WHY A TEST IS NOT ENOUGH ON ITS OWN. CliUnderLeaseClaimsTests and the two
// assertions in MainViewModelTests drive the call sites that exist and read what the
// service was handed, which is the stronger evidence and is not replaced by this. What
// they cannot reach is a call site somebody adds tomorrow. This fails on the day that
// one is written rather than when somebody thinks to test it.
//
// NEITHER COVERS THE OTHER AND THE DIVISION IS EXACT. This proves the call sites pass
// UnderLeaseClaims.From(...) and says nothing whatever about what came out of it: a
// re-verify that produced nothing would satisfy this check. The tests prove the value
// is the re-verify's own claims and not the empty one, over the sites that exist. Read
// as more than that, this becomes a licence to delete the tests.
//
// SCOPE IS PRODUCTION ONLY, DELIBERATELY. The test project passes None at sixty-one
// sites and a matcher at sixty more, all correctly: a test of what a service does with
// an empty batch has to be able to hand it one. Widening this to the test project
// would report those as faults and be turned off.
//
// THE ARGUMENT LIST IS PARSED RATHER THAN MATCHED. A regex over one line cannot see a
// call wrapped across four of them, and this project's call sites are wrapped. The
// scan takes the balanced parenthesis span after the method name, so a call spelled
// differently is read or reported, never skipped.
//
// Usage (from the repo root):
//   node scripts/check-under-lease-claims.mjs
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';

const ROOT = 'src';
const TESTS = 'src/InstallerClean.Tests';
const METHODS = ['DeleteFilesAsync', 'MoveFilesAsync'];
const REQUIRED = 'UnderLeaseClaims.From(';

// Comments come off before the search, so a file that merely discusses a call is not
// a finding and a call commented out is not one either.
const codeOnly = (s) =>
  s.replace(/\/\*[\s\S]*?\*\//g, ' ').replace(/^[ \t]*\/\/.*$/gm, ' ').replace(/\/\/.*$/gm, ' ');

function* csFiles(dir) {
  for (const name of readdirSync(dir)) {
    const path = join(dir, name);
    if (path === TESTS || name === 'bin' || name === 'obj') continue;
    if (statSync(path).isDirectory()) yield* csFiles(path);
    else if (name.endsWith('.cs')) yield path;
  }
}

// The balanced span after an opening parenthesis, or null where the file ends first.
function argumentSpan(text, openIndex) {
  let depth = 0;
  for (let i = openIndex; i < text.length; i++) {
    if (text[i] === '(') depth++;
    else if (text[i] === ')' && --depth === 0) return text.slice(openIndex + 1, i);
  }
  return null;
}

const problems = [];
let callSites = 0;

for (const file of csFiles(ROOT)) {
  const code = codeOnly(readFileSync(file, 'utf8'));
  for (const method of METHODS) {
    const re = new RegExp(`\\.${method}\\s*\\(`, 'g');
    let m;
    while ((m = re.exec(code)) !== null) {
      const open = code.indexOf('(', m.index);
      const args = argumentSpan(code, open);
      if (args === null) {
        problems.push(`${file}: a call to ${method} whose argument list does not close`);
        continue;
      }
      callSites++;
      if (!args.includes(REQUIRED))
        problems.push(
          `${file}:${code.slice(0, m.index).split('\n').length} ${method} is not passed `
          + `${REQUIRED}...). The claims the pre-lease re-verify produced are what the `
          + 'under-lease re-read is for; anything else hands it an empty batch and it '
          + 'returns a pass without asking.');
    }
  }
}

for (const p of problems) console.error(`  ${p}`);

// THE FLOOR IS NOT OPTIONAL. A scan that found no call sites reports no problems and
// reads exactly like a clean run, and the two things most likely to cause it are a
// rename and a change to how these calls are written. The figure is a floor rather
// than a pinned count so that adding a caller does not fail this for the wrong reason.
if (callSites < 4) {
  console.error(`FLOOR FAILED: ${callSites} production call site(s) found, expected at least 4.`);
  console.error('Refusing to report clean over a set this small: the methods have been renamed, moved or rewritten.');
  process.exit(2);
}

console.log(`TOTALS: ${callSites} production call site(s) checked, ${problems.length} not passing ${REQUIRED}...).`);
process.exit(problems.length ? 1 : 0);
