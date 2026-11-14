#!/usr/bin/env node
// Fails (exit 1) when a built artefact is missing the strings of a language the
// source tree provides, when it does not ask for administrator, when it holds a
// Debug-only switch's name, or when it is older than the sources it was built
// from. Reads the built files themselves
// rather than the tree they came from, which is the only place these three can
// be answered.
//
// WHAT A GREEN LINE HERE SAYS ABOUT A LANGUAGE, AND WHAT IT DOES NOT. It says
// the language's resource reached the artefact. Whether that resource holds
// every key the neutral holds is a question about the source, and
// check-resx-parity.mjs answers it there, in the guard glob, on every push. A
// language short of a key still compiles and still ships, so the culture named
// below is a statement about a resource arriving rather than about a screen
// being translated.
//
// WHY A BUILT FILE AND NOT THE SOURCE. A language that never reaches the
// artefact costs nothing at build time and nothing at startup: resource lookup
// falls back to the neutral, so the window opens in English on a machine set to
// that language and says nothing about it. The source resx is present and
// correct in that state, so every check that reads the tree passes.
//
// WHY IT IS NOT A check-*.mjs. The release workflow runs that glob before it
// builds anything, which is the right place for a guard over sources and the
// wrong one for a guard over outputs. This is invoked by name after the merged
// builds in both workflows instead, and check-guard-wiring.mjs is what holds
// those invocations in place.
//
// AN ABSENT ARTEFACT IS A FAILURE AND NEVER A PASS. A guard that skips when it
// finds nothing to read reports the same clean line over a build that never
// happened, which is the shape it exists to catch.
//
// Usage, from the repo root:
//   node scripts/verify-shipped-artefacts.mjs [artefact-dir ...]
// With no arguments it reads the two directories a build with OneFile writes.
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { blankXmlComments } from './xml-source.mjs';

const RESOURCE_DIR = 'src/InstallerClean.Core/Resources';

// Every language is a resource inside the exe, named for the namespace the resx
// files are compiled under: the neutral strings as <base>.resources and each
// other language as <base>.<culture>.resources. The names are the ones
// InstallerClean.Core.csproj gives them and EmbeddedCultureResources reads.
const RESOURCE_BASE = 'InstallerClean.Resources.Strings';
const RESOURCE_EXTENSION = '.resources';

const REQUIRED_LEVEL = 'requireAdministrator';

// What every Debug-only switch's name starts with. The names sit only inside #if
// DEBUG (check-debug-only-switches.mjs), so a Release build holds none, and a merged
// Debug build, which Directory.Build.targets makes only on -p:OneFileDebug=true, holds
// them. A C# string is stored in UTF-16, little-endian.
const DEBUG_SWITCH_PREFIX = Buffer.from('INSTALLERCLEAN_DEBUG_', 'utf16le');

const DEFAULT_ARTEFACTS = [
  'publish/window',
  'publish/cli',
];

const fail = (lines) => {
  console.error('verify-shipped-artefacts: what shipped is not what the tree says.\n');
  for (const line of lines) console.error(`  ${line}`);
  console.error('\nFix: rebuild the artefact from a clean tree, then run this again.');
  console.error('Clear obj/ and bin/ first. Removing a source file does not remove its');
  console.error('output: the previous copy is still there to be merged and the build');
  console.error('stays green, so a rebuild over stale intermediates can carry a language');
  console.error('the tree no longer has.');
  process.exit(1);
};

// Every language with a satellite resx. The neutral is not in this set: its
// resource carries no culture in its name and is looked for by itself.
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

// The single executable an artefact directory holds. The merge writes one file,
// so anything else means the directory is not what this expects.
const executableIn = (dir) => {
  let entries;
  try {
    entries = readdirSync(dir);
  } catch {
    return { error: `${dir}: not present, so nothing was built there` };
  }
  const exes = entries.filter((n) => n.toLowerCase().endsWith('.exe'));
  if (exes.length === 0) return { error: `${dir}: no .exe` };
  if (exes.length > 1) return { error: `${dir}: ${exes.length} .exe files (${exes.join(', ')})` };
  return { path: `${dir}/${exes[0]}` };
};

// An assembly's metadata stores each resource's name as plain text ended by a
// zero byte, so latin1 gives one JavaScript character per byte and leaves the
// names intact for matching. The culture is read from the resource's own name
// rather than looked for one language at a time, so an unexpected one is as
// visible as a missing one. The zero byte on each side keeps a longer name that
// merely contains one of these from counting; it is looked at and not consumed,
// since two names stored one after the other share the byte between them.
const escaped = (s) => s.replace(/\./g, '\\.');
const culturesIn = (text) => {
  const re = new RegExp(`(?<=\\0)${escaped(RESOURCE_BASE)}\\.([A-Za-z][A-Za-z-]{1,9})${escaped(RESOURCE_EXTENSION)}(?=\\0)`, 'g');
  return [...new Set([...text.matchAll(re)].map((m) => m[1]))].sort();
};
const holdsNeutral = (text) => text.includes(`\0${RESOURCE_BASE}${RESOURCE_EXTENSION}\0`);

// THE MANIFEST WINDOWS HONOURS IS A PE RESOURCE. Text shaped like a manifest can
// sit elsewhere in an exe, in a string or a resource of a merged assembly, and
// the loader never consults it. Scanning the file for the first thing shaped
// like a manifest cannot tell the two apart, so the level is read out of the
// resource directory instead, which is the same place the loader reads it from.
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

// XML comments go first, through xml-source.mjs, which throws on one that does not
// close. Each manifest explains the level in a comment beside it, and those comments
// name both the level required and the one it must not become, so a reader that
// matched the raw text would find either spelling whatever the element says.
// check-elevation-manifest.mjs guards the source files the same way.
const levelsIn = (xml) => {
  const declared = blankXmlComments(xml);
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

  const text = bytes.toString('latin1');
  const cultures = culturesIn(text);
  const missing = expected.filter((c) => !cultures.includes(c));
  const extra = cultures.filter((c) => !expected.includes(c));

  if (!holdsNeutral(text)) {
    problems.push(`${exe.path}: no ${RESOURCE_BASE}${RESOURCE_EXTENSION}, the neutral strings`);
  }
  if (cultures.length === 0) {
    problems.push(`${exe.path}: no ${RESOURCE_BASE}.<culture>${RESOURCE_EXTENSION} resource at all`);
  } else {
    if (missing.length) problems.push(`${exe.path}: missing ${missing.join(', ')}`);
    if (extra.length) problems.push(`${exe.path}: carries ${extra.join(', ')}, which no source resx provides`);
  }

  if (bytes.includes(DEBUG_SWITCH_PREFIX)) {
    problems.push(`${exe.path}: holds a Debug-only switch's name, so it is a Debug build: build it with -c Release`);
  }

  const embedded = manifestsIn(bytes);
  let level = 'none';
  let asked = null;
  if (embedded.error) {
    problems.push(`${exe.path}: ${embedded.error}`);
  } else {
    // The line is in the manifest resource the build embedded, not in a file on disk.
    try {
      asked = [...new Set(embedded.manifests.flatMap(levelsIn))];
    } catch (e) {
      problems.push(`${exe.path}: the embedded manifest, ${e.message}`);
    }
  }
  if (asked) {
    if (asked.length) level = asked.join(' and ');
    if (asked.length === 0) {
      problems.push(`${exe.path}: no requestedExecutionLevel in the embedded manifest`);
    } else if (asked.length > 1) {
      problems.push(`${exe.path}: copies of the embedded manifest ask for ${asked.map((a) => `"${a}"`).join(' and ')}`);
    } else if (asked[0] !== REQUIRED_LEVEL) {
      problems.push(`${exe.path}: asks for "${asked[0]}", expected "${REQUIRED_LEVEL}"`);
    }
  }

  report.push(`${exe.path}: level="${level}", no Debug-only switch name, the neutral strings and ${cultures.join(' ')}`);
}

if (problems.length) fail(problems);

// The cultures found are printed rather than counted. A total agrees with the
// total it is checked against by the time this line is reached, so it can only
// ever repeat itself; the names are the part a reader can act on.
console.log(`verify-shipped-artefacts: OK (${artefacts.length} artefacts)`);
for (const line of report) console.log(`  ${line}`);
