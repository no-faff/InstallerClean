#!/usr/bin/env node
// check-debug-only-switches.mjs: fails where a Debug-only switch can reach a Release
// build.
//
// A switch here is an environment variable a Debug build reads to change what the app
// does on the developer's own PC. In the window's start check, one leaves the
// Application log unread. In the update check, one compares the newest release with a
// version it is given, one makes the check wait out its own bound and answer as timed
// out, and one answers with a release tag it is given without asking GitHub. In the
// update itself, one has the app take itself for another kind of copy, one runs a
// setup already on disk in place of downloading one, one copies a newer file already
// on disk in place of downloading it, and one holds either of those two local files
// part way until a gate file appears. Code between #if DEBUG and its #endif is compiled
// only where DEBUG is defined, so a switch's name and its read sit there, and nothing
// that builds a Release defines DEBUG. This checks both.
//
// THE NAME. Each switch's name, in the code of any C# file under src/ (strings
// included), has to sit in the #if DEBUG branch itself: not under a compound condition,
// #if (DEBUG), #elif or #else. A name in a comment is printed and passes, a comment not
// being compiled. The name has to be found in code inside the branch under the switch's
// home folder, so a switch renamed in the code and not here stops the run.
//
// THE READS. Anything in the code of the shipped projects (everything under src/ but
// the tests), strings included, that names the environment: an identifier holding
// "environ" in any case, so a P/Invoke entry point, a reflection name, a registry path
// and a nameof count. With them, the reads whose names do not say so: the C runtime's
// getenv, _wgetenv, getenv_s, _wgetenv_s, _dupenv_s and _wdupenv_s, the generic host
// builders, Path.GetTempPath and GetTempFileName, Registry.GetValue, and
// RegistryValueOptions.None, the option that expands the %NAME% in a registry value. A
// write (SetEnvironmentVariable, _putenv) counts as a read and is printed as a write.
// NAMES_NO_VARIABLE lists, by exact text, what holds the word and names no environment
// variable, such as Environment.NewLine. Anything else holding the word is a read, a
// message in a string that says "environment" among them, until NAMES_NO_VARIABLE or
// ALLOWED names it. An entry in NAMES_NO_VARIABLE that nothing in the shipped projects
// holds stops the run. In Environment.GetEnvironmentVariable the read is the member, so
// the line holds one. One outside the branch fails unless ALLOWED names it by file and
// line text; each entry admits one read and stops the run where it finds none. The
// switch's own read, the line under its home holding its constant, has to be found
// inside the branch.
//
// WHAT DEFINES DEBUG. The token DEBUG in a project, props, targets or publish profile
// file git tracks fails unless an element carrying it, or one around it, is conditioned
// on the Debug configuration; one in an XML comment is printed. A response file (.rsp)
// is read whole: DEBUG fails there, and so does any configuration but Release. A
// #define DEBUG in a shipped C# file fails.
//
// THE WORKFLOWS. DEBUG anywhere in a tracked workflow outside a YAML comment fails,
// a command line and an environment value alike, since MSBuild takes an environment
// variable as a property. Every dotnet publish in release.yml, which builds what ships,
// has to name its configuration, and every configuration it names has to be Release,
// since the SDK defines DEBUG for a configuration called Debug in any case. A publish
// there taking a response file (an @ argument) stops the run: the switches in that file
// reach MSBuild as if they were on the command line.
//
// Exit 1 where a switch can reach a Release build, 2 where the run refuses: a file it
// cannot read to its end, a control not found, an ALLOWED or NAMES_NO_VARIABLE entry
// that matches nothing, git missing. Every finding is printed, passing ones included.
//
// Usage (from the repo root):
//   node scripts/check-debug-only-switches.mjs
import { readFileSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { readCSharp } from './csharp-source.mjs';
import { lineIndex, sourceFiles } from './source-files.mjs';

// Each switch, the constant holding its name, and the folder its read must be found in.
const SWITCHES = [
  {
    name: 'INSTALLERCLEAN_DEBUG_SKIP_APPLICATION_LOG',
    constant: 'SkipApplicationLogVariable',
    home: 'src/InstallerClean.Core/',
  },
  {
    name: 'INSTALLERCLEAN_DEBUG_PRETEND_VERSION',
    constant: 'PretendVersionVariable',
    home: 'src/InstallerClean.Core/',
  },
  {
    name: 'INSTALLERCLEAN_DEBUG_SLOW_UPDATE_CHECK',
    constant: 'SlowCheckVariable',
    home: 'src/InstallerClean.Core/',
  },
  {
    name: 'INSTALLERCLEAN_DEBUG_PRETEND_COPY',
    constant: 'PretendCopyVariable',
    home: 'src/InstallerClean.Core/',
  },
  {
    name: 'INSTALLERCLEAN_DEBUG_LOCAL_SETUP',
    constant: 'LocalSetupVariable',
    home: 'src/InstallerClean.Core/',
  },
  {
    name: 'INSTALLERCLEAN_DEBUG_PRETEND_LATEST',
    constant: 'PretendLatestVariable',
    home: 'src/InstallerClean.Core/',
  },
  {
    name: 'INSTALLERCLEAN_DEBUG_LOCAL_DOWNLOAD',
    constant: 'LocalDownloadVariable',
    home: 'src/InstallerClean.Core/',
  },
  {
    name: 'INSTALLERCLEAN_DEBUG_DOWNLOAD_GATE',
    constant: 'DownloadGateVariable',
    home: 'src/InstallerClean.Core/',
  },
  {
    name: 'INSTALLERCLEAN_DEBUG_LOCAL_MANIFEST',
    constant: 'LocalManifestVariable',
    home: 'src/InstallerClean.Core/',
  },
];

// Reads of the environment a Release build may make, each by its file and the text of
// its line with the comments taken off and the ends trimmed.
const ALLOWED = [
  {
    file: 'src/InstallerClean.Core/Services/InstallerCacheHelpers.cs',
    text: 'Environment.ExpandEnvironmentVariables(value);',
    what: 'ExpandRecordedPath, expanding a path Windows Installer recorded',
  },
  {
    file: 'src/InstallerClean.Core/Services/InstallerQueryService.cs',
    text: 'var raw = key.GetValue(valueName, null, Microsoft.Win32.RegistryValueOptions.None);',
    what: 'TryReadLocalPackage, expanding the LocalPackage path Windows Installer recorded',
  },
  {
    file: 'src/InstallerClean.Core/Services/AppUpdater.cs',
    text: 'private static string? ScoopVariable(string name) => Environment.GetEnvironmentVariable(name);',
    what: 'ScoopVariable, answering the variables Scoop itself reads to find its global root',
  },
  {
    file: 'src/InstallerClean.Core/Helpers/KnownFolders.cs',
    text: 'GetPath(folder, Shell32.SHGetKnownFolderPath, Ole32.CoTaskMemFree, Environment.GetEnvironmentVariable);',
    what: 'KnownFolders.GetPath, the variable .NET falls back to where Windows does not answer for a known folder',
  },
];

// What holds the word and names no environment variable, by its text with the spaces
// taken out. Environment.X is the class System.Environment and its member.
const NAMES_NO_VARIABLE = new Map([
  ['Environment.CurrentManagedThreadId', 'the managed thread'],
  ['Environment.NewLine', 'the line ending'],
  ['Environment.OSVersion', 'the version of Windows'],
  ['Environment.ProcessId', 'the process'],
  ['Environment.ProcessPath', 'the executable'],
  ['Environment.SpecialFolder', 'the known folders'],
  ['Environment.SystemDirectory', 'the system folder'],
  ['DoNotExpandEnvironmentNames', 'the registry option that leaves a %NAME% as written'],
]);

const READ = new RegExp([
  String.raw`\b\w*environ\w*`,
  String.raw`\b(?:_?w?getenv(?:_s)?|_?w?dupenv_s|_?w?putenv(?:_s)?)\b`,
  String.raw`\b(?:CreateDefaultBuilder|CreateApplicationBuilder|CreateSlimBuilder|HostApplicationBuilder|WebApplication)\b`,
  String.raw`\b(?:GetTempPath|GetTempFileName)\w*`,
  String.raw`\bRegistry\s*\.\s*GetValue\b`,
  String.raw`\bRegistryValueOptions\s*\.\s*None\b`,
].join('|'), 'gi');
const WRITE = /^(?:Rtl)?Set\w*Environment|putenv/i;
const TESTS = 'src/InstallerClean.Tests/';
const SHIPPED = ['InstallerClean', 'InstallerClean.Cli', 'InstallerClean.Core'];
const RELEASE_WORKFLOW = '.github/workflows/release.yml';

const failures = [];
const refusals = [];

const escape = (s) => s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');

// --- C# under src/ ------------------------------------------------------------------

// For each line of a file, whether it sits in the branch of an #if whose condition is
// exactly DEBUG, at any depth. An #elif or #else ends that branch. A directive is
// taken by its keyword, so "#  else" is an #else.
const debugLines = (directives, lineCount) => {
  const inside = new Array(lineCount + 2).fill(false);
  const stack = [];
  for (let line = 1; line <= lineCount; line++) {
    const text = directives.get(line);
    if (text !== undefined) {
      const [, keyword = '', rest = ''] = /^#\s*(\w*)(.*)$/.exec(text);
      if (keyword === 'if') stack.push({ debug: rest.trim() === 'DEBUG', active: true });
      else if (keyword === 'elif' || keyword === 'else') {
        if (!stack.length) throw new Error(`line ${line}: #${keyword} with no #if open`);
        stack[stack.length - 1].active = false;
      } else if (keyword === 'endif') {
        if (!stack.length) throw new Error(`line ${line}: #endif with no #if open`);
        stack.pop();
      }
    }
    inside[line] = stack.some((f) => f.debug && f.active);
  }
  if (stack.length) throw new Error(`the file ends inside an #if (${stack.length} open)`);
  return inside;
};

const files = [];
for (const file of sourceFiles('src', '.cs').sort()) {
  const source = readFileSync(file, 'utf8');
  try {
    const read = readCSharp(source);
    const lineCount = source.split('\n').length;
    // One index serves code and comments, which keep every newline where the source has it.
    files.push({ file, ...read, lineAt: lineIndex(source), inside: debugLines(read.directives, lineCount) });
  } catch (e) {
    refusals.push(`${file}: cannot be read to its end (${e.message})`);
  }
}

const shipped = files.filter((f) => !f.file.startsWith(TESTS));

// Names.
for (const { name, home } of SWITCHES) {
  console.log(`${name}:`);
  let foundAtHome = false;
  for (const { file, code, comments, lineAt, inside } of files) {
    const re = new RegExp(`\\b${escape(name)}\\b`, 'g');
    for (const m of code.matchAll(re)) {
      const line = lineAt(m.index);
      console.log(`  ${file}:${line}  ${inside[line] ? 'inside #if DEBUG' : 'OUTSIDE #if DEBUG'}`);
      if (!inside[line]) failures.push(`${file}:${line} names ${name} outside #if DEBUG`);
      else if (file.startsWith(home)) foundAtHome = true;
    }
    for (const m of comments.matchAll(re))
      console.log(`  ${file}:${lineAt(m.index)}  comment`);
  }
  if (!foundAtHome) refusals.push(`${name} is not found in code inside #if DEBUG under ${home}`);
}

// Reads.
console.log('\nReads of the environment in the shipped projects:');
const perProject = new Map(SHIPPED.map((p) => [p, 0]));
for (const { file } of shipped) {
  const project = file.split('/')[1];
  perProject.set(project, (perProject.get(project) ?? 0) + 1);
}
const allowedUsed = new Map(ALLOWED.map((a) => [a, 0]));
const namesUsed = new Map([...NAMES_NO_VARIABLE.keys()].map((n) => [n, 0]));
const ownRead = new Map(SWITCHES.map((s) => [s, 0]));
for (const { file, code, comments, lineAt, inside } of shipped) {
  const codeLines = code.split('\n');
  for (const m of code.matchAll(READ)) {
    let name = m[0].replace(/\s+/g, '');
    // The class itself, written bare or as System.Environment, takes its member's name.
    // Where the member names the environment too, it is the match that counts.
    if (m[0] === 'Environment') {
      const before = code.slice(Math.max(0, m.index - 80), m.index);
      const member = /^\s*\.\s*(\w+)/.exec(code.slice(m.index + m[0].length));
      const qualified = !/\.\s*$/.test(before)
        || /(?:^|[^\w.])(?:global\s*::\s*)?System\s*\.\s*$/.test(before);
      if (qualified && member) {
        if (/environ/i.test(member[1])) continue;
        name = `Environment.${member[1]}`;
      }
    }
    const line = lineAt(m.index);
    const at = `  ${file}:${line}  ${name}`;
    if (NAMES_NO_VARIABLE.has(name)) {
      namesUsed.set(name, namesUsed.get(name) + 1);
      console.log(`${at}  names no variable: ${NAMES_NO_VARIABLE.get(name)}`);
      continue;
    }
    const kind = WRITE.test(name) ? 'write' : 'read';
    const text = codeLines[line - 1].trim();
    const own = SWITCHES.find((s) => file.startsWith(s.home)
      && new RegExp(`\\b${escape(s.constant)}\\b`).test(text));
    if (inside[line]) {
      if (own) ownRead.set(own, ownRead.get(own) + 1);
      console.log(`${at}  ${kind} inside #if DEBUG${own ? `, the read of ${own.name}` : ''}`);
      continue;
    }
    const entry = ALLOWED.find((a) => a.file === file && a.text === text);
    if (entry && allowedUsed.get(entry) === 0) {
      allowedUsed.set(entry, 1);
      console.log(`${at}  ${kind} allowed: ${entry.what}`);
      continue;
    }
    console.log(`${at}  ${kind} OUTSIDE #if DEBUG`);
    failures.push(`${file}:${line} ${kind === 'write' ? 'writes' : 'reads'} the environment (${name}) outside #if DEBUG`
      + (entry ? ', a second read on a line ALLOWED admits once' : ''));
  }
  const mentions = [...comments.matchAll(READ)].length;
  if (mentions) console.log(`  ${file}  ${mentions} in comments`);
}
console.log('Files read, by project: '
  + [...perProject].map(([p, n]) => `${p} ${n}`).join(', '));
for (const p of SHIPPED)
  if (!perProject.get(p)) refusals.push(`no C# file read under src/${p}/`);
for (const [entry, used] of allowedUsed)
  if (!used) refusals.push(`ALLOWED names ${entry.file} "${entry.text}" and no such read is found`);
for (const [name, used] of namesUsed)
  if (!used) refusals.push(`NAMES_NO_VARIABLE names ${name} and nothing in the shipped projects holds it`);
for (const [s, n] of ownRead)
  if (!n) refusals.push(`the read of ${s.name} (a line under ${s.home} holding ${s.constant}) is not found inside #if DEBUG`);

// #define.
console.log('\n#define lines under src/:');
let defines = 0;
for (const { file, directives } of files) {
  for (const [line, text] of directives) {
    const m = /^#\s*define\s+(\w+)/.exec(text);
    if (!m) continue;
    defines++;
    console.log(`  ${file}:${line}  ${text}`);
    if (m[1] === 'DEBUG' && !file.startsWith(TESTS))
      failures.push(`${file}:${line} defines DEBUG`);
  }
}
if (!defines) console.log('  none');

// --- Project files ------------------------------------------------------------------

let tracked = [];
try {
  tracked = execFileSync('git', ['ls-files', '-z'], { encoding: 'utf8', maxBuffer: 1 << 26 })
    .split('\0').filter(Boolean);
} catch (e) {
  refusals.push(`git ls-files did not run (${e.message.split('\n')[0]}), so no project file is read`);
}

const decode = (s) => s
  .replace(/&#x([0-9a-f]+);/gi, (_, h) => String.fromCodePoint(parseInt(h, 16)))
  .replace(/&#(\d+);/g, (_, d) => String.fromCodePoint(Number(d)))
  .replace(/&quot;/g, '"').replace(/&apos;/g, "'").replace(/&lt;/g, '<')
  .replace(/&gt;/g, '>').replace(/&amp;/g, '&');

// A Condition naming the Debug configuration, alone or with a platform. MSBuild
// compares property names and strings without regard to case.
const isDebugCondition = (condition) => {
  const c = condition.replace(/\s+/g, '').toLowerCase();
  return c === "'$(configuration)'=='debug'"
    || /^'\$\(configuration\)\|\$\(platform\)'=='debug\|[^']*'$/.test(c);
};

// Every DEBUG in an MSBuild XML file, each with whether a Debug condition encloses it.
const debugInXml = (path, xml) => {
  const lineOf = lineIndex(xml);
  const found = [];
  const stack = [];
  // A match in the raw text is placed on its own line; one an entity spells out is
  // placed where its text starts.
  const note = (text, at, where) => {
    const raw = [...text.matchAll(/\bDEBUG\b/g)].map((m) => at + m.index);
    const decoded = [...decode(text).matchAll(/\bDEBUG\b/g)].length;
    while (raw.length < decoded) raw.push(at);
    for (const offset of raw)
      found.push({ line: lineOf(offset), where, conditioned: stack.some((e) => e.debug) });
  };
  let i = 0;
  while (i < xml.length) {
    const lt = xml.indexOf('<', i);
    const textEnd = lt === -1 ? xml.length : lt;
    note(xml.slice(i, textEnd), i, 'text');
    if (lt === -1) break;
    if (xml.startsWith('<!--', lt)) {
      const end = xml.indexOf('-->', lt);
      if (end === -1) throw new Error(`line ${lineOf(lt)}: a comment does not close`);
      for (const m of xml.slice(lt, end).matchAll(/\bDEBUG\b/g))
        found.push({ line: lineOf(lt + m.index), where: 'comment' });
      i = end + 3;
      continue;
    }
    if (xml.startsWith('<![CDATA[', lt)) {
      const end = xml.indexOf(']]>', lt);
      if (end === -1) throw new Error(`line ${lineOf(lt)}: a CDATA section does not close`);
      for (const m of xml.slice(lt, end).matchAll(/\bDEBUG\b/g))
        found.push({ line: lineOf(lt + m.index), where: 'text', conditioned: stack.some((e) => e.debug) });
      i = end + 3;
      continue;
    }
    if (xml.startsWith('<?', lt) || xml.startsWith('<!', lt)) {
      const end = xml.indexOf('>', lt);
      if (end === -1) throw new Error(`line ${lineOf(lt)}: a declaration does not close`);
      i = end + 1;
      continue;
    }
    // A tag. Attribute values are quoted, and a '>' inside one is text.
    let j = lt + 1;
    let quote = null;
    while (j < xml.length && (quote || xml[j] !== '>')) {
      if (quote) { if (xml[j] === quote) quote = null; }
      else if (xml[j] === '"' || xml[j] === "'") quote = xml[j];
      j++;
    }
    if (j >= xml.length) throw new Error(`line ${lineOf(lt)}: a tag does not close`);
    const tag = xml.slice(lt + 1, j);
    i = j + 1;
    if (tag.startsWith('/')) {
      if (!stack.length) throw new Error(`line ${lineOf(lt)}: a closing tag with nothing open`);
      stack.pop();
      continue;
    }
    const selfClosing = tag.endsWith('/');
    // Each value is placed where its text starts, after the opening quote.
    const attributes = [...tag.matchAll(/([\w:.-]+)\s*=\s*("([^"]*)"|'([^']*)')/g)]
      .map((m) => ({ name: m[1], value: m[3] ?? m[4], at: lt + 1 + m.index + m[0].length - m[2].length + 1 }));
    const condition = attributes.find((a) => a.name.toLowerCase() === 'condition');
    stack.push({ debug: condition ? isDebugCondition(decode(condition.value)) : false, line: lineOf(lt) });
    for (const a of attributes)
      if (a !== condition) note(a.value, a.at, `attribute ${a.name}`);
    if (selfClosing) stack.pop();
  }
  if (stack.length) throw new Error(`line ${stack[stack.length - 1].line}: an element opened here does not close`);
  return found;
};

// The configurations a command line names, in order: -c and --configuration, and
// Configuration set through -p, /p, -property, /property or --property, whose lists
// MSBuild splits at ';' and ','.
const configurations = (command) => {
  const tokens = command.split(/\s+/).map((t) => t.replace(/^["']|["']$/g, ''));
  const out = [];
  for (let k = 0; k < tokens.length; k++) {
    const t = tokens[k];
    let m = /^(?:-c|--configuration)(?:[:=](.*))?$/i.exec(t);
    if (m) {
      out.push((m[1] ?? tokens[k + 1] ?? '').replace(/^["']|["']$/g, ''));
      continue;
    }
    m = /^(?:-p|\/p|-property|\/property|--property)(?::(.*))?$/i.exec(t);
    if (m) {
      const list = (m[1] ?? tokens[k + 1] ?? '').replace(/^["']|["']$/g, '');
      for (const pair of list.split(/[;,]/)) {
        const eq = pair.indexOf('=');
        if (eq !== -1 && pair.slice(0, eq).trim().toLowerCase() === 'configuration')
          out.push(pair.slice(eq + 1).trim());
      }
    }
  }
  return out;
};

const XML_PROJECT = /\.(?:csproj|props|targets|pubxml)$/i;
const projectFiles = tracked.filter((p) => XML_PROJECT.test(p) || /\.rsp$/i.test(p)).sort();
console.log('\nProject files read for DEBUG:');
for (const path of projectFiles) {
  const text = readFileSync(path, 'utf8');
  if (/\.rsp$/i.test(path)) {
    const lines = text.split('\n');
    const held = [];
    lines.forEach((raw, n) => {
      const line = raw.trim();
      if (!line || line.startsWith('#')) return;
      if (/\bDEBUG\b/.test(line)) {
        held.push(`DEBUG at ${n + 1}`);
        failures.push(`${path}:${n + 1} names DEBUG`);
      }
      for (const c of configurations(line)) {
        held.push(`configuration ${c} at ${n + 1}`);
        if (c.toLowerCase() !== 'release')
          failures.push(`${path}:${n + 1} sets the configuration to ${c}`);
      }
    });
    console.log(`  ${path}  ${held.length ? held.join(', ') : 'no DEBUG, no configuration'}`);
    continue;
  }
  let found;
  try {
    found = debugInXml(path, text);
  } catch (e) {
    refusals.push(`${path}: cannot be read to its end (${e.message})`);
    continue;
  }
  console.log(`  ${path}  ${found.length ? '' : 'no DEBUG'}`);
  for (const f of found) {
    const state = f.where === 'comment' ? 'comment'
      : f.conditioned ? `${f.where}, conditioned on Debug` : `${f.where}, NOT conditioned on Debug`;
    console.log(`    ${path}:${f.line}  DEBUG, ${state}`);
    if (f.where !== 'comment' && !f.conditioned)
      failures.push(`${path}:${f.line} names DEBUG outside an element conditioned on the Debug configuration`);
  }
}
for (const csproj of SHIPPED.map((p) => `src/${p}/${p}.csproj`))
  if (!projectFiles.includes(csproj)) refusals.push(`${csproj} is not among the project files read`);

// --- Workflows ----------------------------------------------------------------------

// A line with its YAML comment taken off: a line whose first character past the indent
// is '#', and anything from a '#' after a space or a tab outside quotes. Inside double
// quotes a backslash or a backtick takes the next character with it, as it does in
// YAML, bash and PowerShell, so a '#' after an escaped quote stays in the quotes.
const withoutComment = (line) => {
  if (/^\s*#/.test(line)) return '';
  let quote = null;
  for (let k = 0; k < line.length; k++) {
    const c = line[k];
    if (quote === '"' && (c === '\\' || c === '`')) { k++; continue; }
    if (quote) { if (c === quote) quote = null; continue; }
    if (c === '"' || c === "'") quote = c;
    else if (c === '#' && (k === 0 || line[k - 1] === ' ' || line[k - 1] === '\t'))
      return line.slice(0, k);
  }
  return line;
};

const workflows = tracked.filter((p) => /^\.github\/workflows\/[^/]+\.ya?ml$/i.test(p)).sort();
console.log('\nWorkflows read for DEBUG:');
let publishes = 0;
for (const path of workflows) {
  const lines = readFileSync(path, 'utf8').split('\n').map(withoutComment);
  const named = [];
  lines.forEach((line, n) => {
    if (/\bDEBUG\b/.test(line)) {
      named.push(n + 1);
      failures.push(`${path}:${n + 1} names DEBUG`);
    }
  });
  console.log(`  ${path}  ${named.length ? `DEBUG at ${named.join(', ')}` : 'no DEBUG'}`);
  if (path !== RELEASE_WORKFLOW) continue;
  // A command continued with '\', '`' or '^' at the end of its line is joined first.
  for (let n = 0; n < lines.length; n++) {
    if (!/\bdotnet\s+publish\b/.test(lines[n])) continue;
    let command = lines[n];
    let last = n;
    while (/[\\`^]\s*$/.test(command) && last + 1 < lines.length)
      command = command.replace(/[\\`^]\s*$/, ' ') + lines[++last];
    publishes++;
    const args = command.slice(command.search(/\bdotnet\s+publish\b/));
    const responseFiles = args.split(/\s+/).filter((t) => /^["']?@/.test(t));
    for (const r of responseFiles)
      refusals.push(`${path}:${n + 1} publishes with the response file ${r}, which this does not read`);
    const named = configurations(args);
    console.log(`    ${path}:${n + 1}  dotnet publish, configuration ${named.length ? named.join(', then ') : 'NOT NAMED'}`);
    if (!named.length)
      failures.push(`${path}:${n + 1} publishes without naming a configuration`);
    for (const c of named)
      if (c.toLowerCase() !== 'release')
        failures.push(`${path}:${n + 1} publishes with the configuration ${c}`);
  }
}
if (!workflows.includes(RELEASE_WORKFLOW)) refusals.push(`${RELEASE_WORKFLOW} is not among the workflows read`);
else if (!publishes) refusals.push(`${RELEASE_WORKFLOW} holds no dotnet publish to read`);

// --- Verdict ------------------------------------------------------------------------

if (failures.length) {
  console.error('\ncheck-debug-only-switches: a Debug-only switch can reach a Release build.\n');
  for (const f of failures) console.error(`  ${f}`);
}
if (refusals.length) {
  console.error('\ncheck-debug-only-switches: REFUSING, because the run cannot vouch for what it read.\n');
  for (const r of refusals) console.error(`  ${r}`);
}
if (refusals.length) process.exit(2);
if (failures.length) process.exit(1);
console.log('\ncheck-debug-only-switches: OK');
