#!/usr/bin/env node
// Fails (exit 1) when the setup's [Code] and the app drift apart on the files the setup
// renames aside while it writes:
// - AppFile, the setup's one list of the files it writes to {app}, leaves one out, names
//   one [Files] does not write, skips a number or does not answer '' past its end, or
//   AppFileCount does not count it;
// - HoldAllAside does not hold aside each in turn, stopping at the first it cannot, as a
//   loop that is not the body of a condition, or its answer is not acted on;
// - the completed install's ssPostInstall branch does not clear what earlier setups
//   left of each, or DeleteLeftAside is called anywhere else;
// - a rename builds its name from anything but the file's own name in {app};
// - the done suffix differs from ExeReplacement.SetupDoneSuffix, the C# names the held
//   suffix, or the setup holds no mutex of its own;
// - the window is opened anywhere but DeinitializeSetup after its exit for a completed
//   install, or [Code] starts any other program through an Exec or ShellExec call or
//   declares any external but MoveFileW and DeleteFileW, or the switch is not
//   HandOverContract.UpdateFailedSwitch, left out where the person cancelled and set
//   before the start, or a [Run] entry in this file carries Parameters;
// - CancelButtonClick does not ask Setup's own question and record only a yes.
//
// Before any file is written, the setup renames each file it writes to {app} aside, and
// moves each back where the install fails or where one cannot be renamed, so a file a
// running program holds open never leaves {app} part old and part new. A file left out
// of AppFile is written over in place, which is not an error anywhere. Where a held
// file is still running once the install completes, the setup renames it to a done
// name, which the window and the command line delete beside their own exe at their next
// start. A held file can be the only copy of an earlier one after a failed install, so
// the app never names the held suffix. Inno Setup cannot read the C# constants, so this
// check holds them together.
//
// The setup, ExeReplacement.cs and HandOverContract.cs are read with their comments
// blanked, through csharp-source.mjs and iss-source.mjs. Each [Files] destination, each
// AppFile entry and each rename [Code] builds a name for is printed with its line.
//
// Run from the repo root: node scripts/check-setup-held-copies.mjs
import { readFileSync } from 'node:fs';
import { readCSharp } from './csharp-source.mjs';
import { readIss } from './iss-source.mjs';
import { lineIndex } from './source-files.mjs';

const SETUP_FILE = 'installer/InstallerClean.iss';
const CODE_FILE = 'src/InstallerClean.Core/Services/ExeReplacement.cs';
const CONTRACT_FILE = 'src/InstallerClean.Core/Helpers/HandOverContract.cs';

const read = (p) => {
  try {
    return readFileSync(p, 'utf8');
  } catch {
    console.error(`check-setup-held-copies: cannot read ${p}`);
    process.exit(1);
  }
};

const problems = [];

const code = readCSharp(read(CODE_FILE)).code;
const constant = (name) => {
  const found = [...code.matchAll(new RegExp(`const\\s+string\\s+${name}\\s*=\\s*@?"([^"]*)"`, 'g'))].map((m) => m[1]);
  if (found.length !== 1) problems.push(`${CODE_FILE}: expected one 'const string ${name}', found ${found.length}`);
  return found[0];
};
const csDone = constant('SetupDoneSuffix');

const setupSource = readIss(read(SETUP_FILE));
const lines = setupSource
  .split(/\r?\n/)
  .map((text, i) => ({ line: i + 1, text: text.trim() }));

const define = (name) => {
  const found = lines.filter((l) => new RegExp(`^#define\\s+${name}\\b`).test(l.text));
  if (found.length !== 1) {
    problems.push(`${SETUP_FILE}: expected one #define ${name}, found ${found.length}`);
    return undefined;
  }
  console.log(`${SETUP_FILE}:${found[0].line}: ${found[0].text}`);
  const m = found[0].text.match(new RegExp(`^#define\\s+${name}\\s+"([^"]*)"\\s*$`));
  if (!m) problems.push(`${SETUP_FILE}:${found[0].line}: ${name} is not a plain quoted string`);
  return m ? m[1] : undefined;
};
const issHeld = define('HeldSuffix');
const issDone = define('DoneSuffix');
if (csDone !== undefined && issDone !== undefined && csDone !== issDone) {
  problems.push(`DoneSuffix is '${issDone}' in ${SETUP_FILE}, SetupDoneSuffix is '${csDone}' in ${CODE_FILE}`);
}
// A held file can be the only copy of an earlier one, so the app never deletes one.
if (issHeld !== undefined && issDone !== undefined && (issHeld === issDone || issDone.startsWith(issHeld) || issHeld.startsWith(issDone))) {
  problems.push(`${SETUP_FILE}: DoneSuffix '${issDone}' does not tell a done file from a held one ('${issHeld}')`);
}
if (issHeld !== undefined && code.includes(issHeld)) {
  problems.push(`${CODE_FILE}: names the held suffix '${issHeld}', which only the setup moves and only the uninstaller deletes`);
}

// Each [Files] entry writing under {app}, by the path it writes relative to {app}. A
// field's value may be quoted or not, and {app} is matched in any case, as Inno Setup
// reads them.
let section = '';
const written = new Map();
for (const l of lines) {
  const heading = l.text.match(/^\[([^\]]+)\]$/);
  if (heading) {
    section = heading[1].toLowerCase();
    continue;
  }
  if (section !== 'files' || l.text === '') continue;
  const field = (name) => {
    const m = l.text.match(new RegExp(`(?:^|;)\\s*${name}\\s*:\\s*("([^"]*)"|[^;]*)`, 'i'));
    return m ? (m[2] ?? m[1]).trim() : undefined;
  };
  const destDir = field('DestDir');
  if (destDir === undefined) {
    problems.push(`${SETUP_FILE}:${l.line}: a [Files] entry with no DestDir the check can read`);
    continue;
  }
  const under = destDir.match(/^\{app\}(?:\\(.*))?$/i);
  if (!under) continue;
  const leaf = field('DestName') ?? field('Source')?.split('\\').pop();
  if (!leaf) {
    problems.push(`${SETUP_FILE}:${l.line}: a [Files] entry for {app} with no Source or DestName the check can read`);
    continue;
  }
  const sub = (under[1] ?? '').replace(/\\+$/, '');
  const name = sub ? `${sub}\\${leaf}` : leaf;
  console.log(`${SETUP_FILE}:${l.line}: writes {app}\\${name}`);
  written.set(name.toLowerCase(), { name, line: l.line });
}

// AppFile is the setup's one list of the files it writes to {app}: HoldAllAside holds
// each aside and the completed install clears what earlier setups left of each. Its
// entries run from 0 with no gap, and both loops run to AppFileCount, which counts them.
const text = lines.map((l) => l.text).join('\n');
const lineAt = lineIndex(setupSource);
const held = new Map();
const appFile = setupSource.match(/function\s+AppFile\s*\(\s*I\s*:\s*Integer\s*\)\s*:\s*String\s*;[\s\S]*?\nend;/i);
if (!appFile) problems.push(`${SETUP_FILE}: no 'function AppFile(I: Integer): String;' found`);
else {
  const entries = [...appFile[0].matchAll(/(\d+)\s*:\s*Result\s*:=\s*'([^']*)'\s*;/g)];
  entries.forEach((m, n) => {
    const line = lineAt(appFile.index + m.index);
    console.log(`${SETUP_FILE}:${line}: AppFile ${m[1]} is ${m[2]}`);
    if (Number(m[1]) !== n) problems.push(`${SETUP_FILE}:${line}: AppFile's entry ${m[1]} is not number ${n}`);
    if (held.has(m[2].toLowerCase())) problems.push(`${SETUP_FILE}:${line}: AppFile names ${m[2]} a second time`);
    held.set(m[2].toLowerCase(), { name: m[2], line });
  });
  if (!/else\s+Result\s*:=\s*''\s*;/i.test(appFile[0])) problems.push(`${SETUP_FILE}: AppFile does not answer '' past its last entry`);
}
if (!/function\s+AppFileCount\s*:\s*Integer\s*;\s*begin\s*Result\s*:=\s*0\s*;\s*while\s+AppFile\s*\(\s*Result\s*\)\s*<>\s*''\s+do\s+Result\s*:=\s*Result\s*\+\s*1\s*;\s*end\s*;/i.test(setupSource)) {
  problems.push(`${SETUP_FILE}: AppFileCount does not count AppFile's entries up to the first ''`);
}
// A loop over every AppFile entry that follows a ';' or a routine's own 'begin', not a
// 'then', 'do' or 'else' or a 'begin' after one, so it is not itself the body of a
// condition or loop. A loop body of more than one statement is written in begin and end.
const everyFile = (call, within, block) => {
  const body = block ? String.raw`begin\s*${call}\s*end\s*;` : String.raw`(?:begin\s*${call}\s*end\s*;|${call})`;
  const m = within.match(new RegExp(String.raw`for\s+I\s*:=\s*0\s+to\s+AppFileCount\s*-\s*1\s+do\s+${body}`, 'i'));
  if (!m) return false;
  const before = within.slice(0, m.index).replace(/\s+$/, '');
  if (before.endsWith(';')) return true;
  if (!/\bbegin$/i.test(before)) return false;
  return !/\b(?:then|do|else)$/i.test(before.slice(0, -5).replace(/\s+$/, ''));
};
const holdAll = setupSource.match(/function\s+HoldAllAside\s*:\s*Boolean\s*;[\s\S]*?\nend;/i);
if (!holdAll || !everyFile(String.raw`Result\s*:=\s*HoldAside\s*\(\s*AppFile\s*\(\s*I\s*\)\s*\)\s*;\s*if\s+not\s+Result\s+then\s+Exit\s*;`, holdAll[0], true)) {
  problems.push(`${SETUP_FILE}: HoldAllAside does not hold aside every AppFile entry, stopping at the first that cannot be`);
}

// Each name [Code] renames a file to is built from the file's own name in {app}, which is
// what the app matches: a held name from the path being held aside, a done name from the
// path the file was held aside from.
const renames = [...text.matchAll(/RenameAside\s*\(\s*([^,()]+?)\s*,\s*([^,()]+?)\s*,\s*'\{#(\w+)\}'\s*\)/g)];
if (renames.length === 0) problems.push(`${SETUP_FILE}: no RenameAside(..., ..., '{#...}') call found; the check has read nothing`);
const textLineAt = lineIndex(text);
for (const m of renames) {
  const line = textLineAt(m.index);
  console.log(`${SETUP_FILE}:${line}: renames ${m[1]} to a free ${m[3]} name of ${m[2]}`);
  const base = m[2].replace(/\s+/g, '');
  if (m[3] === 'HeldSuffix' && base !== m[1].replace(/\s+/g, '')) {
    problems.push(`${SETUP_FILE}:${line}: a held name is built from ${m[2]}, not from the path being held aside`);
  } else if (m[3] === 'DoneSuffix' && base !== 'HeldFrom[I]') {
    problems.push(`${SETUP_FILE}:${line}: a done name is built from ${m[2]}, not from HeldFrom[I], the file's own name in {app}`);
  } else if (m[3] !== 'HeldSuffix' && m[3] !== 'DoneSuffix') {
    problems.push(`${SETUP_FILE}:${line}: renames to an unknown suffix {#${m[3]}}`);
  }
}
const setupMutex = lines.filter((l) => /^SetupMutex\s*=/i.test(l.text));
if (setupMutex.length !== 1 || setupMutex[0].text.replace(/\s+/g, '') !== 'SetupMutex={#SetupMutexName}') {
  problems.push(`${SETUP_FILE}: expected one 'SetupMutex={#SetupMutexName}' line, so a second setup does not run while one is renaming`);
}

// After a completed install the ssPostInstall branch clears what earlier setups left of
// every file in AppFile, and nothing else calls DeleteLeftAside.
const post = setupSource.match(/CurStep\s*=\s*ssPostInstall\s+then([\s\S]*?)\nend;/i);
const clears = [...setupSource.matchAll(/(?<!\bprocedure\s+)\bDeleteLeftAside\s*\(/gi)];
if (!post) problems.push(`${SETUP_FILE}: no ssPostInstall branch found in CurStepChanged`);
else {
  const start = post.index;
  const end = post.index + post[0].length;
  if (!everyFile(String.raw`DeleteLeftAside\s*\(\s*AppFile\s*\(\s*I\s*\)\s*\)\s*;`, post[1])) {
    problems.push(`${SETUP_FILE}:${lineAt(start)}: the ssPostInstall branch does not call DeleteLeftAside(AppFile(I)) for every AppFile entry, as a loop that is not itself the body of a condition`);
  }
  for (const m of clears) {
    if (m.index < start || m.index > end) problems.push(`${SETUP_FILE}:${lineAt(m.index)}: DeleteLeftAside is called outside the ssPostInstall branch`);
  }
  if (clears.length !== 1) problems.push(`${SETUP_FILE}: expected one DeleteLeftAside call, found ${clears.length}`);
}

// The window opened after an Update now whose install did not complete is told so with
// the switch HandOverContract reads, from DeinitializeSetup after its exit for a
// completed install, and a completed install's [Run] entry passes no such switch.
const contract = readCSharp(read(CONTRACT_FILE)).code;
const failedSwitch = contract.match(/const\s+string\s+UpdateFailedSwitch\s*=\s*"([^"]*)"/)?.[1];
if (failedSwitch === undefined) problems.push(`${CONTRACT_FILE}: no 'const string UpdateFailedSwitch' found`);
const deinit = setupSource.match(/procedure\s+DeinitializeSetup\s*;[\s\S]*?\nbegin([\s\S]*?)\nend;/i);
// The Exec and ShellExec calls that start a program, every external the script
// declares, and the switch spelled out whole, however the program's path is written.
const setupCode = setupSource.slice(setupSource.search(/^\[Code\]/im));
const opens = [...setupCode.matchAll(/\b(?:ShellExec|ShellExecAsOriginalUser|Exec|ExecAsOriginalUser|ExecAndLogOutput|ExecAndCaptureOutput)\s*\(/gi)]
  .map((m) => ({ index: setupSource.length - setupCode.length + m.index }));
if (opens.length !== 1) problems.push(`${SETUP_FILE}: expected one call in [Code] that starts a program, the window's, found ${opens.length}`);
for (const m of setupCode.matchAll(/external\s+'([^']*)'/gi)) {
  if (!/^(?:MoveFileW|DeleteFileW)@kernel32\.dll\b/i.test(m[1])) {
    problems.push(`${SETUP_FILE}:${lineAt(setupSource.length - setupCode.length + m.index)}: an external other than MoveFileW or DeleteFileW, '${m[1]}', which this check does not read`);
  }
}
const spellings = failedSwitch === undefined ? [] : [...setupCode.matchAll(new RegExp(failedSwitch.replace(/[.*+?^${}()|[\]\\/]/g, '\\$&'), 'gi'))];
if (failedSwitch !== undefined && spellings.length !== 1) {
  problems.push(`${SETUP_FILE}: expected ${failedSwitch} once in [Code], in DeinitializeSetup's Args, found ${spellings.length}`);
}
if (!deinit) problems.push(`${SETUP_FILE}: no DeinitializeSetup found`);
else {
  const body = deinit[1];
  const line = lineAt(deinit.index);
  if (!/^\s*if\s+Installed\s+then\s+Exit\s*;/i.test(body)) {
    problems.push(`${SETUP_FILE}:${line}: DeinitializeSetup does not exit first for a completed install`);
  }
  for (const m of opens) {
    const at = m.index - (deinit.index + deinit[0].indexOf(body));
    if (at < 0 || at > body.length) problems.push(`${SETUP_FILE}:${lineAt(m.index)}: the window is opened outside DeinitializeSetup`);
  }
  const bodyAt = deinit.index + deinit[0].indexOf(body);
  const switchAt = [...body.matchAll(/\bArgs\s*:=\s*'([^']+)'/g)];
  if (switchAt.length !== 1) problems.push(`${SETUP_FILE}:${line}: expected one Args := '...' in DeinitializeSetup, found ${switchAt.length}`);
  // The switch is the else of the cancel, so a setup the person stopped gets none.
  if (!/\bif\s+CancelClicked\s+then\s+Args\s*:=\s*''\s+else\s+Args\s*:=\s*'[^']+'\s*;/i.test(body)) {
    problems.push(`${SETUP_FILE}:${line}: DeinitializeSetup does not leave the switch out where CancelClicked is set`);
  }
  const openAt = body.search(/ShellExec\s*\(\s*'[^']*'\s*,\s*App\s*,\s*Args\s*,/);
  if (openAt < 0) {
    problems.push(`${SETUP_FILE}:${line}: DeinitializeSetup does not open the window with ShellExec('', App, Args, ...)`);
  }
  for (const m of switchAt) {
    if (openAt >= 0 && m.index > openAt) problems.push(`${SETUP_FILE}:${lineAt(bodyAt + m.index)}: Args is set after the window is started`);
  }
  for (const m of switchAt) {
    console.log(`${SETUP_FILE}:${lineAt(bodyAt + m.index)}: DeinitializeSetup opens the window with '${m[1]}'`);
    if (failedSwitch !== undefined && m[1] !== failedSwitch) {
      problems.push(`${SETUP_FILE}:${lineAt(bodyAt + m.index)}: opens the window with '${m[1]}', HandOverContract.UpdateFailedSwitch is '${failedSwitch}'`);
    }
  }
}
// No entry in this file's [Run] section, which runs after a completed install, passes
// the switch in any field order, or carries Parameters at all, so no constant,
// {code:...} or define can carry the switch there.
if (failedSwitch !== undefined) {
  let runSection = '';
  for (const l of lines) {
    const heading = l.text.match(/^\[([^\]]+)\]$/);
    if (heading) {
      runSection = heading[1].toLowerCase();
      continue;
    }
    if (runSection === 'run' && l.text.toLowerCase().includes(failedSwitch.toLowerCase())) {
      problems.push(`${SETUP_FILE}:${l.line}: a [Run] entry passes ${failedSwitch}, which a completed install never does`);
    } else if (runSection === 'run' && /(?:^|;)\s*Parameters\s*:/i.test(l.text)) {
      problems.push(`${SETUP_FILE}:${l.line}: a [Run] entry carries Parameters, which this check does not read; one that opens the window must not pass ${failedSwitch} after a completed install`);
    }
  }
}

// A cancel is recorded only where the person answered yes to the question, so a declined
// cancel, or a stop Setup makes without asking, still has a failed update reported.
const cancelClick = setupSource.match(/procedure\s+CancelButtonClick\s*\([^)]*\)\s*;\s*begin([\s\S]*?)\nend;/i);
if (!cancelClick || !/^\s*if\s+Confirm\s+then\s+begin\s*Cancel\s*:=\s*ExitSetupMsgBox\s*;\s*Confirm\s*:=\s*False\s*;\s*if\s+Cancel\s+then\s+CancelClicked\s*:=\s*True\s*;\s*end\s*;\s*$/i.test(cancelClick[1])) {
  problems.push(`${SETUP_FILE}: CancelButtonClick does not ask ExitSetupMsgBox itself and record only a yes in CancelClicked`);
}
if ([...setupCode.matchAll(/\bCancelClicked\s*:=/gi)].length !== 1) problems.push(`${SETUP_FILE}: CancelClicked is set somewhere other than CancelButtonClick`);

if (written.size === 0) problems.push(`${SETUP_FILE}: no [Files] entry writing to {app} found; the check has read nothing`);
if (appFile && held.size === 0) problems.push(`${SETUP_FILE}: AppFile names no file; the check has read nothing`);
for (const [key, { name, line }] of written) {
  if (!held.has(key)) problems.push(`${SETUP_FILE}:${line}: writes {app}\\${name}, which AppFile does not name`);
}
for (const [key, { name, line }] of held) {
  if (!written.has(key)) problems.push(`${SETUP_FILE}:${line}: AppFile names ${name}, which [Files] does not write to {app}`);
}
if (!/if\s+not\s+HoldAllAside\s+then/i.test(text)) problems.push(`${SETUP_FILE}: HoldAllAside's answer is never acted on`);

if (problems.length > 0) {
  for (const p of problems) console.error(`check-setup-held-copies: ${p}`);
  process.exit(1);
}
console.log(`check-setup-held-copies: AppFile names each of the ${written.size} files the setup writes to {app}, HoldAllAside and the completed install's clean-up cover it, the window is told of a failed Update now only from DeinitializeSetup, and the names match ExeReplacement and HandOverContract`);
