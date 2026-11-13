#!/usr/bin/env node
// check-download-names.mjs: fails where the file names a release publishes and the file
// names the app downloads from it have come apart.
//
// THE APP. AppCopy.DownloadFileName answers, for each kind of copy that updates itself,
// the name of the file it downloads from the release's tag. {version} in an interpolated
// name is the release's version; in a plain string it is the text {version}. A name given
// as a constant, such as CommandLineFileName, is read from the constant.
//
// THE RELEASE. release.yml names the four downloads in four places: the copies the
// "Collect the four downloads" step makes into publish/, the list that step checks the
// signatures of, the table that records their hashes, and the paths the "Upload the
// release folder" step uploads. $version in an unquoted or double-quoted name is the
// release's version, and in a single-quoted name it is the text $version, as PowerShell
// reads it. In an upload path the setup's InstallerClean-*-setup.exe is the setup of any
// version, and any other * is read as the character, so a looser pattern is a difference.
//
// THE EARLIER COPIES. Copies of earlier versions also update from InstallerClean-slim.exe,
// the slim download, which this app does not ask for: the slim is the portable's file
// under that name. EARLIER lists it, and the release has to publish it as it does the
// app's own names.
//
// The copies are also held to the folder each is copied from: the portable from the
// merged window exe, the slim from that same exe signed under its own name in the
// signing folder, the command line from its own publish and the setup from the signed
// setup, so two names swapped between files are a difference too.
//
// Each of the four places has to name exactly the files the app downloads and the ones
// in EARLIER. A copy installed on somebody's PC asks for one of these names under the
// tag of the version it is updating to, so a name on either side that the other does
// not have leaves every installed copy of that kind unable to update.
//
// THE CHECK. The comparison is run on the real files, then on copies with one thing
// changed each, and has to refuse every one. A comparison that accepted those would pass
// the real files without having compared anything. Each change alters the one rule it is
// named for and nothing else, so that rule failing is what lets the change through.
//
// Nothing here reads the network.
//
// Run from the repo root: node scripts/check-download-names.mjs
import { readFileSync } from 'node:fs';
import { readCSharp } from './csharp-source.mjs';

const name = 'check-download-names';
const APP = 'src/InstallerClean.Core/Services/AppCopy.cs';
const RELEASE = '.github/workflows/release.yml';
// The kinds that update themselves from a download, as AppCopyKind names them.
const KINDS = ['Setup', 'Portable', 'CommandLineDownload'];
// The downloads copies of earlier versions update from that the app does not ask for,
// each by the name of its kind in those versions. Do not take one out: every copy of
// that kind already installed asks for it by this name.
const EARLIER = new Map([['Slim', 'InstallerClean-slim.exe']]);

// Read with LF line ends whatever the file was written with.
const read = (path) => {
  try {
    return readFileSync(path, 'utf8').replaceAll('\r\n', '\n');
  } catch (error) {
    console.error(`${name}: cannot read ${path}: ${error.message}`);
    process.exit(1);
  }
};

// The release's version in a name. In C# it is {version} in an interpolated string; in
// PowerShell it is $version in an unquoted or double-quoted name and nothing in a
// single-quoted one.
const csharpName = (text, interpolated) => (interpolated ? text.replaceAll('{version}', '<version>') : text);
const powershellName = (text, quote) => (quote === "'" ? text : text.replaceAll('$version', '<version>'));

// The names the app downloads, from DownloadFileName's arms.
function appNames(source) {
  const code = readCSharp(source).code;
  const constants = new Map(
    [...code.matchAll(/\bconst\s+string\s+(\w+)\s*=\s*"([^"]*)"\s*;/g)].map((m) => [m[1], m[2]]),
  );
  const at = code.search(/\bDownloadFileName\s*\(\s*AppCopyKind\s+\w+\s*,\s*string\s+\w+\s*\)/);
  const end = at < 0 ? -1 : code.indexOf('};', at);
  if (at < 0 || end < 0) return { error: `${APP}: DownloadFileName is not found.` };
  const member = code.slice(at, end);
  const names = new Map();
  const arms = (member.match(/\bAppCopyKind\.\w+\s*=>/g) ?? []).length;
  if (arms !== KINDS.length)
    return { error: `${APP}: DownloadFileName has ${arms} arms before its default and this check compares ${KINDS.length} (${KINDS.join(', ')}). A kind added to the app has to be added here and to release.yml.` };
  for (const kind of KINDS) {
    const arm = member.match(new RegExp(`\\bAppCopyKind\\.${kind}\\s*=>\\s*(\\$?"[^"]*"|\\w+)\\s*,`));
    if (!arm) return { error: `${APP}: DownloadFileName has no arm for ${kind} that names a string or a constant.` };
    let value = arm[1];
    let interpolated = false;
    if (value.startsWith('"') || value.startsWith('$"')) {
      interpolated = value.startsWith('$');
      value = value.slice(value.indexOf('"') + 1, -1);
    } else if (constants.has(value)) value = constants.get(value);
    else return { error: `${APP}: the ${kind} arm names ${value}, which is not a string constant in the file.` };
    names.set(kind, csharpName(value, interpolated));
  }
  return { names };
}

// The text of one step of release.yml: from its `- name:` line to the first later line
// that is indented no deeper than that line, which is the next step or the end of the job.
function stepText(workflow, stepName) {
  const lines = workflow.split('\n');
  const start = lines.findIndex((l) => l.trimStart().startsWith(`- name: ${stepName}`));
  if (start < 0) return null;
  const indent = (l) => l.length - l.trimStart().length;
  const depth = indent(lines[start]);
  let end = start + 1;
  while (end < lines.length && (lines[end].trim() === '' || indent(lines[end]) > depth)) end++;
  return lines.slice(start, end).join('\n');
}

// The four places release.yml names the downloads, each as a sorted list of names.
function releaseNames(workflow) {
  const places = new Map();
  const collect = stepText(workflow, 'Collect the four downloads');
  const upload = stepText(workflow, 'Upload the release folder');
  const record = stepText(workflow, 'Record what was built');
  if (!collect) return { error: `${RELEASE}: no step named "Collect the four downloads".` };
  if (!upload) return { error: `${RELEASE}: no step named "Upload the release folder".` };
  if (!record) return { error: `${RELEASE}: no step named "Record what was built".` };

  const code = collect.split('\n').filter((line) => !/^\s*#/.test(line)).join('\n');
  // A path as written in PowerShell, to its file name and the quote it was written in.
  const unquote = (path) => {
    const quote = /^["']/.test(path) ? path[0] : '';
    return { name: path.replace(/^["']|["']$/g, '').replace(/^publish\//, ''), quote };
  };
  const named = ({ name, quote }) => powershellName(name, quote);
  const copies = [...code.matchAll(/Copy-Item\s+(\S+)\s+(\S+)(?:\s+-Force)?\s*$/gm)].map((m) => ({
    source: m[1],
    name: named(unquote(m[2])),
  }));
  places.set('the copies made into publish/', copies.map((c) => c.name));
  const loop = code.match(/foreach\s*\(\s*\$f\s+in\s+@\(([\s\S]*?)\)\)/);
  places.set(
    'the signature check',
    loop ? [...loop[1].matchAll(/["'][^"']+["']/g)].map((m) => named(unquote(m[0]))) : [],
  );
  const table = record.match(/\$artefacts\s*=\s*\[ordered\]@\{([\s\S]*?)\n\s*\}/);
  places.set(
    'the hash table',
    table ? [...table[1].matchAll(/=\s*(["'][^"']+["'])/g)].map((m) => named(unquote(m[1]))) : [],
  );
  const paths = upload.match(/path:\s*\|\n([\s\S]*?)\n\s*if-no-files-found/);
  places.set(
    'the upload paths',
    paths
      ? paths[1]
          .split('\n')
          .map((l) => l.trim())
          .filter((l) => l && l !== 'publish/build-provenance.txt')
          .map((l) => l.replace(/^publish\//, '').replace(/^InstallerClean-\*-setup\.exe$/, 'InstallerClean-<version>-setup.exe'))
      : [],
  );
  for (const [place, names] of places) places.set(place, [...names].sort());
  return { places, copies };
}

// The folder each kind's copy is made from, as a part of the source path. The
// portable is the merged window exe; the slim is that same exe signed under its
// own name, which sits in the signing folder rather than a publish one.
const SOURCES = new Map([
  ['Setup', 'signed-setup'],
  ['Portable', 'window'],
  ['Slim', 'signed-executables'],
  ['CommandLineDownload', 'cli'],
]);

// What the app downloads, what the release names and every problem between them.
function compare(appSource, workflow) {
  const app = appNames(appSource);
  if (app.error) return { problems: [app.error] };
  const release = releaseNames(workflow);
  if (release.error) return { problems: [release.error], app };
  const wanted = [...app.names.values(), ...EARLIER.values()].sort();
  const problems = [];
  for (const [place, names] of release.places) {
    const absent = wanted.filter((n) => !names.includes(n));
    const unknown = names.filter((n) => !wanted.includes(n));
    const repeated = names.filter((n, i) => names.indexOf(n) !== i);
    if (names.length === 0) problems.push(`${RELEASE}: ${place} names no download.`);
    if (absent.length) problems.push(`${RELEASE}: ${place} does not name ${absent.join(', ')}, which the app or earlier copies download.`);
    if (unknown.length) problems.push(`${RELEASE}: ${place} names ${unknown.join(', ')}, which neither the app nor earlier copies download.`);
    if (repeated.length) problems.push(`${RELEASE}: ${place} names ${[...new Set(repeated)].join(', ')} more than once.`);
  }
  for (const [kind, file] of [...app.names, ...EARLIER]) {
    const copy = release.copies.find((c) => c.name === file);
    if (copy && !copy.source.includes(SOURCES.get(kind)))
      problems.push(`${RELEASE}: ${file} is copied from ${copy.source}, not from the ${SOURCES.get(kind)} publish.`);
  }
  return { problems, app, release };
}

const appSource = read(APP);
const workflow = read(RELEASE);
const { problems, app, release } = compare(appSource, workflow);

if (app?.names) {
  for (const [kind, file] of app.names) console.log(`  the app downloads ${file} for ${kind}`);
  for (const [kind, file] of EARLIER) console.log(`  earlier copies download ${file} for ${kind}`);
}
if (release?.places) {
  for (const [place, names] of release.places) console.log(`  ${place}: ${names.join(' ')}`);
}

// The controls, run once the real files agree: a comparison that accepted these would
// accept anything. The workflow is changed by what a line says and not by how it is
// indented.
const reline = (pattern, replacement) => workflow.replace(pattern, replacement);
const controls = [
  [
    'the portable copied under a versioned name',
    appSource,
    reline(/(Copy-Item\s+publish\/window\/InstallerClean\.exe\s+)publish\/InstallerClean-portable\.exe/, '$1"publish/InstallerClean-$$version-portable.exe"'),
  ],
  [
    'the portable versioned in the signature check',
    appSource,
    reline(/'publish\/InstallerClean-portable\.exe',/, '"publish/InstallerClean-$$version-portable.exe",'),
  ],
  [
    'the portable versioned in the hash table',
    appSource,
    reline(/portable(\s*)=\s*'InstallerClean-portable\.exe'/, 'portable$1= "InstallerClean-$$version-portable.exe"'),
  ],
  ['the slim left out of the upload paths', appSource, reline(/^[ \t]*publish\/InstallerClean-slim\.exe[ \t]*\n/m, '')],
  [
    'the setup upload path written as a pattern that is not the setup of any version',
    appSource,
    reline(/InstallerClean-\*-setup\.exe/, 'InstallerClean-*-setup*.exe'),
  ],
  [
    'the setup in the signature check written in single quotes, where PowerShell reads $version as text',
    appSource,
    reline(/"publish\/InstallerClean-\$version-setup\.exe",\n/, "'publish/InstallerClean-$$version-setup.exe',\n"),
  ],
  [
    'the portable copied from the slim\'s folder',
    appSource,
    reline(/Copy-Item\s+publish\/window\/InstallerClean\.exe(\s+)publish\/InstallerClean-portable\.exe/, 'Copy-Item signing/signed-executables/InstallerClean-slim.exe$1publish/InstallerClean-portable.exe'),
  ],
  [
    'the app asking for a versioned portable',
    appSource.replace('"InstallerClean-portable.exe"', '$"InstallerClean-{version}-portable.exe"'),
    workflow,
  ],
  [
    'the app asking for a setup written with {version} in a plain string',
    appSource.replace('$"InstallerClean-{version}-setup.exe"', '"InstallerClean-{version}-setup.exe"'),
    workflow,
  ],
  [
    'the app asking for a fifth download',
    appSource.replace('        AppCopyKind.CommandLineDownload => CommandLineFileName,', '        AppCopyKind.CommandLineDownload => CommandLineFileName,\n        AppCopyKind.Scoop => "InstallerClean-scoop.exe",'),
    workflow,
  ],
];
for (const [label, changedApp, changedWorkflow] of problems.length === 0 ? controls : []) {
  if (changedApp === appSource && changedWorkflow === workflow)
    problems.push(`the control "${label}" did not change anything, so it could not test anything.`);
  else if (compare(changedApp, changedWorkflow).problems.length === 0) problems.push(`the comparison accepted ${label}.`);
}

if (problems.length) {
  for (const p of problems) console.error(`${name}: ${p}`);
  process.exit(1);
}
console.log(`${name}: release.yml names the files the app and earlier copies download, in all four places.`);
