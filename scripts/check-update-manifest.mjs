#!/usr/bin/env node
// check-update-manifest.mjs: fails where the writer of a release's update.json and the
// app that reads it have come apart, or where the writer's check could not tell a wrong
// file from a right one.
//
// THE APP. UpdateManifest.cs names the downloads (DownloadKey's results), the schema
// (Schema) and the requirement .NET Framework 4.8 goes by (NetFramework48). The writer's
// KINDS, less EARLIER_KINDS, have to be exactly those downloads, EARLIER_KINDS none of
// them, SCHEMA that schema and REQUIRES that one requirement, read from the code of that
// file, so a download added to the app and not to the writer, or the reverse, stops the
// run. The names read are printed.
//
// THE WRITER. write-update-manifest.mjs is run, as the release script runs it, for all
// four downloads and for the three without the slim, and each file is compared with the
// text written out below, which is the shape the app reads: every download requiring
// .NET Framework 4.8. The text has LF line ends, no byte order mark and a newline at the
// end.
//
// THE CHECK. --check has to accept each file against the list it was written for, and
// refuse: each file against the other list, a file that is not there, one with CRLF line
// ends, one with a byte order mark, one with a download added or taken out, one with
// another schema, one with no requirement, one with another requirement, and one that is
// not JSON. Each refusal is a
// control: a check that accepted everything would pass the first two comparisons alone.
// The command is also refused, with exit code 2, for no download, an unknown one and one
// named twice.
//
// Nothing here reads the network. The files it writes are in a folder of their own in the
// system's temporary folder, which it removes.
//
// Run from the repo root: node scripts/check-update-manifest.mjs
import { execFileSync } from 'node:child_process';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { readCSharp } from './csharp-source.mjs';
import { EARLIER_KINDS, KINDS, REQUIRES, SCHEMA } from './update-manifest.mjs';

const name = 'check-update-manifest';
const SOURCE = 'src/InstallerClean.Core/Services/UpdateManifest.cs';
const WRITER = 'scripts/write-update-manifest.mjs';
const problems = [];
const fail = (text) => problems.push(text);

// What the app reads, from its own code.
let code;
try {
  code = readCSharp(readFileSync(SOURCE, 'utf8')).code;
} catch (error) {
  console.error(`${name}: cannot read ${SOURCE}: ${error.message}`);
  process.exit(1);
}
const schemaMatch = code.match(/\bpublic\s+const\s+int\s+Schema\s*=\s*(\d+)\s*;/);
const requirementMatch = code.match(/\bpublic\s+const\s+string\s+NetFramework48\s*=\s*"([^"]+)"\s*;/);
const memberAt = code.search(/\bDownloadKey\s*\(\s*AppCopyKind\s+\w+\s*\)/);
const memberEnd = memberAt < 0 ? -1 : code.indexOf('};', memberAt);
const member = memberEnd < 0 ? '' : code.slice(memberAt, memberEnd);
const keys = [...member.matchAll(/\bAppCopyKind\.\w+\s*=>\s*"([^"]+)"/g)].map((m) => m[1]);
// Every arm but the last, `_ => null`, has to be of the form read above. The member's own
// `=>` is not an arm.
const arms = (member.match(/=>/g) ?? []).length - 2;
if (!schemaMatch) fail(`${SOURCE}: no "public const int Schema = N;" found.`);
if (keys.length === 0) fail(`${SOURCE}: no download name found in DownloadKey.`);
if (keys.length !== arms)
  fail(`${SOURCE}: DownloadKey has ${arms} arms before its default and ${keys.length} of them are AppCopyKind.Name => "download".`);
if (schemaMatch && Number(schemaMatch[1]) !== SCHEMA)
  fail(`${SOURCE} reads schema ${schemaMatch[1]}; update-manifest.mjs writes schema ${SCHEMA}.`);
if (!requirementMatch) fail(`${SOURCE}: no "public const string NetFramework48 = "...";" found.`);
else if (REQUIRES.length !== 1 || REQUIRES[0] !== requirementMatch[1])
  fail(`${SOURCE} names .NET Framework 4.8 "${requirementMatch[1]}"; update-manifest.mjs requires ${JSON.stringify(REQUIRES)}.`);
const current = KINDS.filter((k) => !EARLIER_KINDS.includes(k));
const strayEarlier = EARLIER_KINDS.filter((k) => !KINDS.includes(k));
const earlierRead = EARLIER_KINDS.filter((k) => keys.includes(k));
if (strayEarlier.length) fail(`update-manifest.mjs lists ${strayEarlier.join(' ')} among EARLIER_KINDS and not among KINDS.`);
if (earlierRead.length) fail(`${SOURCE} names ${earlierRead.join(' ')}, which update-manifest.mjs lists as read only by earlier copies.`);
const missing = keys.filter((k) => !current.includes(k));
const extra = current.filter((k) => !keys.includes(k));
const repeated = keys.filter((k, i) => keys.indexOf(k) !== i);
if (missing.length) fail(`${SOURCE} names ${missing.join(' ')}, which update-manifest.mjs does not write.`);
if (extra.length) fail(`update-manifest.mjs writes ${extra.join(' ')}, which ${SOURCE} does not name.`);
if (repeated.length) fail(`${SOURCE} names ${[...new Set(repeated)].join(' ')} more than once.`);
console.log(`  the app reads schema ${schemaMatch?.[1]}, the downloads ${keys.join(' ')} and the requirement ${requirementMatch?.[1]}`);
console.log(`  the writer lists ${KINDS.join(' ')}, of which earlier copies alone read ${EARLIER_KINDS.join(' ')}, each requiring ${REQUIRES.join(' ')}`);

// The shape the app reads, written out by hand.
const entry = (kind) => `    "${kind}": {\n      "requires": [\n        "netfx-4.8"\n      ]\n    }`;
const expectedFor = (kinds) =>
  `{\n  "schema": ${SCHEMA},\n  "downloads": {\n${kinds.map(entry).join(',\n')}\n  }\n}\n`;
const ALL = ['setup', 'portable', 'slim', 'cli'];
const NO_SLIM = ['setup', 'portable', 'cli'];

const folder = mkdtempSync(join(tmpdir(), 'update-manifest-'));
try {
  const run = (...argv) => {
    try {
      const output = execFileSync(process.execPath, [WRITER, ...argv], { encoding: 'utf8', stdio: ['ignore', 'pipe', 'pipe'] });
      return { code: 0, output };
    } catch (error) {
      return { code: error.status ?? -1, output: `${error.stdout ?? ''}${error.stderr ?? ''}` };
    }
  };
  const expectCode = (label, wanted, result) => {
    if (result.code === wanted) console.log(`  ${label}: exit ${result.code}`);
    else fail(`${label}: exited ${result.code}, wanted ${wanted}. ${result.output.trim()}`);
  };

  const files = new Map();
  for (const [label, kinds] of [['all four', ALL], ['without the slim', NO_SLIM]]) {
    const file = join(folder, `${label.replace(/ /g, '-')}.json`);
    // Given in the reverse of the file's order, which the file does not follow.
    expectCode(`writing ${label}`, 0, run(file, ...[...kinds].reverse()));
    const bytes = readFileSync(file);
    if (bytes.equals(Buffer.from(expectedFor(kinds), 'utf8'))) console.log(`  ${label}: the bytes are the expected text`);
    else fail(`${label}: the writer wrote\n${bytes.toString('utf8')}\nwhere\n${expectedFor(kinds)}was expected.`);
    if (bytes.includes(0x0d)) fail(`${label}: the file holds a carriage return.`);
    if (bytes[0] === 0xef) fail(`${label}: the file starts with a byte order mark.`);
    files.set(label, file);
    expectCode(`${label} against its own list`, 0, run('--check', file, ...kinds));
  }
  expectCode('all four against the list without the slim', 1, run('--check', files.get('all four'), ...NO_SLIM));
  expectCode('without the slim against the list of all four', 1, run('--check', files.get('without the slim'), ...ALL));
  expectCode('a file that is not there', 1, run('--check', join(folder, 'absent.json'), ...ALL));

  const good = expectedFor(ALL);
  const variants = [
    ['a file with CRLF line ends', Buffer.from(good.replace(/\n/g, '\r\n'), 'utf8')],
    ['a file with a byte order mark', Buffer.concat([Buffer.from([0xef, 0xbb, 0xbf]), Buffer.from(good, 'utf8')])],
    ['a file with no newline at the end', Buffer.from(good.replace(/\n$/, ''), 'utf8')],
    ['a file with a download taken out', Buffer.from(expectedFor(NO_SLIM), 'utf8')],
    ['a file with another schema', Buffer.from(good.replace(`"schema": ${SCHEMA}`, `"schema": ${SCHEMA + 1}`), 'utf8')],
    ['a file with no requirement', Buffer.from(good.replace(/"requires": \[\n\s*"netfx-4\.8"\n\s*\]/g, '"requires": []'), 'utf8')],
    ['a file with another requirement', Buffer.from(good.replaceAll('"netfx-4.8"', '"netfx-4.7.2"'), 'utf8')],
    ['a file of text that is not JSON', Buffer.from('<html></html>\n', 'utf8')],
    ['an empty file', Buffer.alloc(0)],
  ];
  for (const [label, bytes] of variants) {
    const file = join(folder, 'variant.json');
    writeFileSync(file, bytes);
    expectCode(label, 1, run('--check', file, ...ALL));
  }

  expectCode('no download', 2, run(join(folder, 'none.json')));
  expectCode('an option in the place of the file', 2, run('--verbose', join(folder, 'option.json'), 'setup'));
  expectCode('an option after the file', 2, run(join(folder, 'late.json'), '--check', 'setup'));
  expectCode('an unknown download', 2, run(join(folder, 'unknown.json'), 'setup', 'installer'));
  expectCode('a download named twice', 2, run(join(folder, 'twice.json'), 'setup', 'setup'));
} finally {
  rmSync(folder, { recursive: true, force: true });
}

if (problems.length) {
  console.error(`${name}: ${problems.length} problem${problems.length === 1 ? '' : 's'}:\n`);
  for (const p of problems) console.error(`  ${p}`);
  process.exit(1);
}
console.log(`${name}: OK`);
