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

// THE MANIFEST WINDOWS HONOURS IS A PE RESOURCE, AND A SINGLE-FILE ARTEFACT
// HOLDS OTHERS THAT IT DOES NOT. The apphost carries the app's own manifest in
// its resource directory, and a single-file publish appends a bundle after that
// image. Assemblies inside the bundle can carry manifests of their own asking
// for a different level, and those are payload bytes the loader never consults.
// Scanning the file for the first thing shaped like a manifest cannot tell the
// two apart, so the level is read out of the resource directory instead, which
// is the same place the loader reads it from. Compression decides how many of
// the payload copies are legible as text and decides nothing about which
// manifest starts the process.
const RT_MANIFEST = 24;

// The resource id Windows reserves for an executable's own manifest. A library
// declares its isolation-aware manifest under 2, and nothing here reads one.
const APP_MANIFEST_ID = 1;

// Where the resource table sits among the optional header's data directories.
const RESOURCE_TABLE_INDEX = 2;

// Walks the PE headers to the resource directory and returns every manifest
// stored at RT_MANIFEST under the executable's own id. There is normally one.
// More than one means a copy per language, which is why they are returned as a
// set rather than reduced here: the caller is entitled to know that two copies
// of the same manifest ask for different things.
//
// Anything unreadable is returned as an error rather than as an absence, for
// the same reason an absent artefact fails: a guard that cannot see what it
// came to check has not checked it.
const manifestsIn = (bytes) => {
  try {
    return readManifests(bytes);
  } catch {
    return { error: 'the image is malformed: a header read ran off the end of the file' };
  }
};

const readManifests = (bytes) => {
  const u16 = (o) => bytes.readUInt16LE(o);
  const u32 = (o) => bytes.readUInt32LE(o);
  const problem = (why) => ({ error: why });

  if (bytes.length < 0x40 || u16(0) !== 0x5a4d) return problem('not a PE image: no MZ signature');
  const pe = u32(0x3c);
  if (pe + 24 > bytes.length || u32(pe) !== 0x00004550) return problem('not a PE image: no PE signature');

  const sectionCount = u16(pe + 6);
  const optionalSize = u16(pe + 20);
  const optional = pe + 24;

  // PE32 and PE32+ differ only in the width of a few optional-header fields, so
  // the data directories sit at a different offset in each.
  const magic = u16(optional);
  if (magic !== 0x10b && magic !== 0x20b) return problem(`unrecognised optional header magic 0x${magic.toString(16)}`);
  const wide = magic === 0x20b;

  if (u32(optional + (wide ? 108 : 92)) <= RESOURCE_TABLE_INDEX) {
    return problem('the optional header declares no resource data directory');
  }
  const entry = optional + (wide ? 112 : 96) + RESOURCE_TABLE_INDEX * 8;
  const rootRva = u32(entry);
  if (rootRva === 0 || u32(entry + 4) === 0) return problem('the image carries no resource directory');

  // Resource offsets are addresses in the loaded image, so each one has to be
  // put back through the section table to find the byte in the file.
  const sections = [];
  for (let i = 0; i < sectionCount; i += 1) {
    const s = optional + optionalSize + i * 40;
    if (s + 40 > bytes.length) return problem('the section table runs past the end of the file');
    sections.push({ virtual: u32(s + 12), virtualSize: u32(s + 8), raw: u32(s + 20), rawSize: u32(s + 16) });
  }
  const fileOffsetOf = (rva) => {
    for (const s of sections) {
      const span = Math.max(s.virtualSize, s.rawSize);
      if (rva >= s.virtual && rva < s.virtual + span) return s.raw + (rva - s.virtual);
    }
    return -1;
  };

  const root = fileOffsetOf(rootRva);
  if (root < 0) return problem('the resource directory address falls in no section');

  // Every offset inside the resource tree is relative to its root. The top bit
  // of a name says whether it is a string rather than an id, and the top bit of
  // an offset says whether it points at another directory rather than at data.
  const childrenOf = (offset) => {
    const dir = root + offset;
    if (dir + 16 > bytes.length) return null;
    const total = u16(dir + 12) + u16(dir + 14);
    const out = [];
    for (let i = 0; i < total; i += 1) {
      const e = dir + 16 + i * 8;
      if (e + 8 > bytes.length) return null;
      const name = u32(e);
      const to = u32(e + 4);
      out.push({ named: name >= 0x80000000, id: name & 0x7fffffff, directory: to >= 0x80000000, offset: to & 0x7fffffff });
    }
    return out;
  };
  const withId = (list, id) => (list ?? []).find((c) => !c.named && c.id === id);

  const types = childrenOf(0);
  if (!types) return problem('the resource directory could not be read');
  const manifestType = withId(types, RT_MANIFEST);
  if (!manifestType) return problem('the image holds no manifest resource');
  if (!manifestType.directory) return problem('the manifest resource is not a directory');

  const own = withId(childrenOf(manifestType.offset), APP_MANIFEST_ID);
  if (!own) return problem(`the image holds no manifest at resource id ${APP_MANIFEST_ID}`);

  const leaves = own.directory ? childrenOf(own.offset) : [own];
  if (!leaves) return problem('the manifest language directory could not be read');

  const found = [];
  for (const leaf of leaves) {
    if (leaf.directory) return problem('unexpected directory below the manifest language level');
    const data = root + leaf.offset;
    if (data + 16 > bytes.length) return problem('a manifest data entry runs past the end of the file');
    const start = fileOffsetOf(u32(data));
    const size = u32(data + 4);
    if (start < 0 || start + size > bytes.length) return problem('a manifest lies outside the file');
    found.push(bytes.toString('utf8', start, start + size));
  }
  return { manifests: found };
};

// XML comments go first. Each manifest explains the level in a comment beside
// it, and those comments name both the level required and the one it must not
// become, so a reader that matched the raw text would find either spelling
// whatever the element says. check-elevation-manifest.mjs guards the source
// files the same way.
const levelsIn = (xml) => {
  const declared = xml.replace(/<!--[\s\S]*?-->/g, '');
  const tags = [...declared.matchAll(/<requestedExecutionLevel\b[^>]*?\blevel="([^"]*)"/g)];
  return [...new Set(tags.map((m) => m[1]))];
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

  const embedded = manifestsIn(bytes);
  let level = 'none';
  if (embedded.error) {
    problems.push(`${exe.path}: ${embedded.error}`);
  } else {
    const asked = [...new Set(embedded.manifests.flatMap(levelsIn))];
    if (asked.length) level = asked.join(' and ');
    if (asked.length === 0) {
      problems.push(`${exe.path}: no requestedExecutionLevel in the embedded manifest`);
    } else if (asked.length > 1) {
      problems.push(`${exe.path}: copies of the embedded manifest ask for ${asked.map((a) => `"${a}"`).join(' and ')}`);
    } else if (asked[0] !== REQUIRED_LEVEL) {
      problems.push(`${exe.path}: asks for "${asked[0]}", expected "${REQUIRED_LEVEL}"`);
    }
  }

  report.push(`${exe.path}: ${cultures.length} languages, level="${level}"`);
}

if (problems.length) fail(problems);

console.log(`verify-shipped-artefacts: OK (${artefacts.length} artefacts, ${expected.length} languages each)`);
for (const line of report) console.log(`  ${line}`);
