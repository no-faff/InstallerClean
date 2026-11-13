#!/usr/bin/env node
// write-update-manifest.mjs: writes the update.json a release publishes, or checks that
// a file is exactly what would be written.
//
//   node scripts/write-update-manifest.mjs <file> <download>...
//   node scripts/write-update-manifest.mjs --check <file> <download>...
//
// The downloads are the ones the release uploads, named setup, portable, slim and cli.
// The file lists those and no others, each requiring .NET Framework 4.8, so an update
// from a copy whose download is not among them is told the release has none, an update
// never goes to a download the release does not have, and a copy on a PC without .NET
// Framework 4.8 is told to install it before it downloads anything. The text is
// update-manifest.mjs's manifestText.
//
// --check reads <file> and compares its bytes with that text, so it answers for the
// file being there, parsing, carrying the schema the app reads, listing exactly these
// downloads and being the bytes a second run would write. It exits 0 and names the
// downloads it found, or exits 1 and says how the file differs.
//
// Exit codes: 0 done or matching, 1 the file differs or cannot be read or written, 2 the
// command was not understood: --check anywhere but first, any other argument starting with
// a hyphen, no download, an unknown download or one named twice.
import { readFileSync, writeFileSync } from 'node:fs';
import { manifestText } from './update-manifest.mjs';

const name = 'write-update-manifest';
const args = process.argv.slice(2);
const check = args[0] === '--check';
if (check) args.shift();
const [file, ...kinds] = args;

if (!file || args.some((a) => a.startsWith('-'))) {
  console.error(`${name}: usage: node scripts/write-update-manifest.mjs [--check] <file> <download>...`);
  process.exit(2);
}

let expected;
try {
  expected = manifestText(kinds);
} catch (error) {
  console.error(`${name}: ${error.message}`);
  process.exit(2);
}

if (!check) {
  try {
    writeFileSync(file, expected, 'utf8');
  } catch (error) {
    console.error(`${name}: cannot write ${file}: ${error.message}`);
    process.exit(1);
  }
  console.log(`${name}: wrote ${file} listing ${kinds.join(' ')}`);
  process.exit(0);
}

let found;
try {
  found = readFileSync(file);
} catch (error) {
  console.error(`${name}: cannot read ${file}: ${error.message}`);
  process.exit(1);
}
if (found.equals(Buffer.from(expected, 'utf8'))) {
  console.log(`${name}: ${file} lists ${kinds.join(' ')}`);
  process.exit(0);
}

let listed = 'is not JSON';
try {
  const parsed = JSON.parse(found.toString('utf8').replace(/^﻿/, ''));
  listed = `lists ${Object.keys(parsed?.downloads ?? {}).join(' ') || 'no downloads'} under schema ${parsed?.schema}`;
} catch {
  // Not JSON: the description above stands.
}
console.error(`${name}: ${file} is not what a release listing ${kinds.join(' ')} publishes. It ${listed}.`);
process.exit(1);
