#!/usr/bin/env node
// Fails (exit 1) when a built artefact does not carry every language the app
// ships, when it does not ask for administrator, or when it is older than the
// sources it was built from. Reads the built files themselves rather than the
// tree they came from, which is the only place these three can be answered.
//
// WHY A BUILT FILE AND NOT THE SOURCE. A satellite that never reaches the
// artefact costs nothing at build time and nothing at startup: resource lookup
// falls back to the neutral, so the window opens in English on a machine set to
// that language and says nothing about it. The source resx is present and
// correct in that state, so every check that reads the tree passes.
//
// WHY IT IS NOT A check-*.mjs. The release workflow runs that glob before it
// builds anything, which is the right place for a guard over sources and the
// wrong one for a guard over outputs. This is invoked by name after the publish
// steps in both workflows instead, and check-guard-wiring.mjs is what holds
// those invocations in place.
//
// AN ABSENT ARTEFACT IS A FAILURE AND NEVER A PASS. A guard that skips when it
// finds nothing to read reports the same clean line over a publish that never
// happened, which is the shape it exists to catch.
//
// Usage, from the repo root:
//   node scripts/verify-shipped-artefacts.mjs [artefact-dir ...]
// With no arguments it reads the three directories a release publishes.
import { readFileSync, readdirSync, statSync } from 'node:fs';

const RESOURCE_DIR = 'src/InstallerClean.Core/Resources';

// The satellite assembly is named for the project the resx lives in, which is
// Core rather than either host. Anchoring on the host name finds nothing in a
// correct artefact and reads as an app with no translations at all.
const SATELLITE = 'InstallerClean.Core.resources.dll';

const REQUIRED_LEVEL = 'requireAdministrator';

const DEFAULT_ARTEFACTS = [
  'publish/self-contained',
  'publish/framework-dependent',
  'publish/cli',
];

const fail = (lines) => {
  console.error('verify-shipped-artefacts: what shipped is not what the tree says.\n');
  for (const line of lines) console.error(`  ${line}`);
  console.error('\nFix: rebuild the artefact from a clean tree, then run this again.');
  console.error('Clear obj/ and bin/ first. Removing a source file does not remove its');
  console.error('output: the previous copy is still there to be bundled and the build');
  console.error('stays green, so a rebuild over stale intermediates can carry a language');
  console.error('the tree no longer has. Note that --no-incremental is a dotnet build');
  console.error('switch and dotnet publish rejects it.');
  process.exit(1);
};

// Every language with a satellite resx. The neutral ships inside the main
// assembly and has no satellite of its own, so it is not in this set.
const sourceCultures = () => {
  let names;
  try {
    names = readdirSync(RESOURCE_DIR);
  } catch {
    fail([`cannot read ${RESOURCE_DIR}`]);
  }
  const found = names
    .map((n) => n.match(/^Strings\.([A-Za-z-]+)\.resx$/))
    .filter(Boolean)
    .map((m) => m[1])
    .sort();
  if (found.length === 0) {
    fail([`no Strings.<culture>.resx files in ${RESOURCE_DIR}`]);
  }
  return found;
};

const newestSourceMtime = () => {
  let newest = 0;
  for (const name of readdirSync(RESOURCE_DIR)) {
    if (!/^Strings\.[A-Za-z-]*\.?resx$/.test(name)) continue;
    const m = statSync(`${RESOURCE_DIR}/${name}`).mtimeMs;
    if (m > newest) newest = m;
  }
  return newest;
};

// The single executable a publish directory holds. A single-file publish leaves
// one, so anything else means the directory is not what this expects.
const executableIn = (dir) => {
  let entries;
  try {
    entries = readdirSync(dir);
  } catch {
    return { error: `${dir}: not present, so nothing was published there` };
  }
  const exes = entries.filter((n) => n.toLowerCase().endsWith('.exe'));
  if (exes.length === 0) return { error: `${dir}: no .exe` };
  if (exes.length > 1) return { error: `${dir}: ${exes.length} .exe files (${exes.join(', ')})` };
  return { path: `${dir}/${exes[0]}` };
};

// Bundled entry names are stored as plain text, so latin1 gives one JavaScript
// character per byte and leaves the names intact for matching. The culture is
// read from the entry's own path rather than looked for one language at a time,
// so an unexpected one is as visible as a missing one.
const culturesIn = (bytes) => {
  const text = bytes.toString('latin1');
  const re = new RegExp(`([A-Za-z][A-Za-z-]{1,9})/${SATELLITE.replace(/\./g, '\\.')}`, 'g');
  return [...new Set([...text.matchAll(re)].map((m) => m[1]))].sort();
};

// The Win32 manifest is a resource on the executable rather than an entry in the
// bundle, so it is readable whether or not the bundle was compressed.
const manifestLevelIn = (bytes) => {
  const text = bytes.toString('latin1');
  const tags = text.match(/<requestedExecutionLevel\b[^>]*>/g) ?? [];
  if (tags.length === 0) return null;
  const level = tags[0].match(/\blevel="([^"]*)"/);
  return level ? level[1] : null;
};

const artefacts = process.argv.slice(2).length ? process.argv.slice(2) : DEFAULT_ARTEFACTS;
const expected = sourceCultures();
const newestSource = newestSourceMtime();
const problems = [];
const report = [];

for (const dir of artefacts) {
  const exe = executableIn(dir);
  if (exe.error) {
    problems.push(exe.error);
    continue;
  }

  const built = statSync(exe.path).mtimeMs;
  if (built < newestSource) {
    problems.push(`${exe.path}: older than a source resx, so it is not this tree's build`);
    continue;
  }

  const bytes = readFileSync(exe.path);

  const cultures = culturesIn(bytes);
  const missing = expected.filter((c) => !cultures.includes(c));
  const extra = cultures.filter((c) => !expected.includes(c));

  if (cultures.length === 0) {
    problems.push(`${exe.path}: no ${SATELLITE} entries at all`);
  } else {
    if (missing.length) problems.push(`${exe.path}: missing ${missing.join(', ')}`);
    if (extra.length) problems.push(`${exe.path}: carries ${extra.join(', ')}, which no source resx provides`);
  }

  const level = manifestLevelIn(bytes);
  if (level === null) {
    problems.push(`${exe.path}: no requestedExecutionLevel in the embedded manifest`);
  } else if (level !== REQUIRED_LEVEL) {
    problems.push(`${exe.path}: asks for "${level}", expected "${REQUIRED_LEVEL}"`);
  }

  report.push(`${exe.path}: ${cultures.length} languages, level="${level ?? 'none'}"`);
}

if (problems.length) fail(problems);

console.log(`verify-shipped-artefacts: OK (${artefacts.length} artefacts, ${expected.length} languages each)`);
for (const line of report) console.log(`  ${line}`);
