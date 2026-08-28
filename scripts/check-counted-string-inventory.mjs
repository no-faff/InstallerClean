#!/usr/bin/env node
// Checks the test inventory of counted strings against the switch that classifies
// them.
//
// A counted string is one whose wording changes with a number. Every one of them
// reaches DisplayHelpers.Pluralise with a keyPrefix, and DisplayHelpers.QuestionFor
// decides which question that prefix's one-form answers. The tests that render
// every counted string in every language are driven off one array,
// CountedStringTests.CountedPrefixes, and a theory walks exactly what its array
// holds: a prefix absent from the array is absent from every one of those tests at
// once, and they all still pass.
//
// So the two lists have to hold the same set, and nothing in the build could say
// so. A switch's arms are not enumerable at runtime, which is why this reads the
// source of both rather than reflecting over the assembly.
//
// Checked both ways:
//   - every prefix QuestionFor classifies is in the test inventory
//   - every prefix in the test inventory is classified by QuestionFor
//
// The second direction is not symmetrical decoration. An inventory entry with no
// arm reaches the switch's default and throws, which the tests do catch; this
// names it at the source in seconds instead.
//
// Run from the repo root: node scripts/check-counted-string-inventory.mjs
import { readFileSync } from 'node:fs';

const switchPath = 'src/InstallerClean.Core/Helpers/DisplayHelpers.cs';
const inventoryPath = 'src/InstallerClean.Tests/Helpers/CountedStringTests.cs';

// Comments are stripped before any literal is read. Both regions carry prose in
// double quotes explaining the classification, and a reader that does not drop it
// returns whole sentences as though they were prefixes. A // is only a comment
// where it is not inside a string, so the cut is taken at the first one with an
// even number of quotes before it.
function withoutComments(region) {
  return region
    .split('\n')
    .map((line) => {
      let quotes = 0;
      for (let i = 0; i < line.length - 1; i++) {
        if (line[i] === '"') quotes++;
        else if (line[i] === '/' && line[i + 1] === '/' && quotes % 2 === 0) return line.slice(0, i);
      }
      return line;
    })
    .join('\n');
}

const literals = (region) => [...withoutComments(region).matchAll(/"([^"\\]+)"/g)].map((m) => m[1]);

// The classifier's arms, taken from the head of the switch to its default arm.
// Stopping at the default matters: the throw below it holds prose in double quotes
// that would otherwise read as prefixes.
function classifiedPrefixes(source) {
  const head = source.indexOf('QuestionFor(string keyPrefix) => keyPrefix switch');
  if (head === -1) fail(`${switchPath}: could not find the QuestionFor switch. Has it been renamed?`);
  const tail = source.indexOf('_ =>', head);
  if (tail === -1) fail(`${switchPath}: the QuestionFor switch has no default arm, so its end cannot be located.`);
  return literals(source.slice(head, tail));
}

// The array the tests walk. Read to its closing brace rather than to the next
// array, because a second inventory sits directly below this one and the two hold
// deliberately different sets.
function inventoryPrefixes(source) {
  const head = source.indexOf('private static readonly string[] CountedPrefixes =');
  if (head === -1) fail(`${inventoryPath}: could not find CountedPrefixes. Has it been renamed?`);
  // Both closers, because the array's syntax is not this check's to pin: written as
  // a collection expression it closes with ]; instead, and a reader that knew only
  // one form would run on into the next array and report its members as duplicates
  // of nothing. Whichever comes first is this array's end.
  const braced = source.indexOf('};', head);
  const bracketed = source.indexOf('];', head);
  const ends = [braced, bracketed].filter((i) => i !== -1);
  if (ends.length === 0) fail(`{inventoryPath}: CountedPrefixes is not closed, so its end cannot be located.`);
  return literals(source.slice(head, Math.min(...ends)));
}

function fail(message) {
  console.error(message);
  process.exit(1);
}

const classified = classifiedPrefixes(readFileSync(switchPath, 'utf8'));
const inventory = inventoryPrefixes(readFileSync(inventoryPath, 'utf8'));

// PARSE CONTROL, about the READING rather than about the content. A regex that has
// stopped matching yields an empty set, and two empty sets agree with each other,
// so the comparison below would report a clean result over nothing at all. Neither
// figure is written down here, so adding a counted string cannot make this stale.
if (classified.length === 0) fail(`${switchPath}: no prefixes parsed out of QuestionFor, so this run establishes nothing.`);
if (inventory.length === 0) fail(`${inventoryPath}: no prefixes parsed out of CountedPrefixes, so this run establishes nothing.`);

// MUST-HIT CONTROL. A pair every counted-string arrangement has to carry, named so
// that a reader can see the instrument found a real member of each set rather than
// only a count of them.
const mustHit = 'Plural.File';
if (!classified.includes(mustHit)) fail(`${switchPath}: the parse did not find ${mustHit}, so it is not reading the switch.`);
if (!inventory.includes(mustHit)) fail(`${inventoryPath}: the parse did not find ${mustHit}, so it is not reading the array.`);

const missingFromInventory = classified.filter((p) => !inventory.includes(p)).sort();
const missingFromSwitch = inventory.filter((p) => !classified.includes(p)).sort();

const duplicates = (list) => [...new Set(list.filter((p, i) => list.indexOf(p) !== i))].sort();
const classifiedTwice = duplicates(classified);
const listedTwice = duplicates(inventory);

let bad = false;

if (missingFromInventory.length > 0) {
  bad = true;
  console.error(`NOT IN THE TEST INVENTORY (${missingFromInventory.length}), so no test renders them:`);
  for (const p of missingFromInventory) console.error(`  ${p}`);
  console.error(`Add each to CountedPrefixes in ${inventoryPath}.\n`);
}

if (missingFromSwitch.length > 0) {
  bad = true;
  console.error(`NOT CLASSIFIED (${missingFromSwitch.length}), so Pluralise throws on them:`);
  for (const p of missingFromSwitch) console.error(`  ${p}`);
  console.error(`Add an arm for each to QuestionFor in ${switchPath}.\n`);
}

for (const [what, list, where] of [
  ['classified twice', classifiedTwice, switchPath],
  ['listed twice', listedTwice, inventoryPath],
]) {
  if (list.length > 0) {
    bad = true;
    console.error(`${what.toUpperCase()} in ${where}: ${list.join(', ')}\n`);
  }
}

if (bad) {
  console.error(
    'A counted string is classified in QuestionFor and listed in CountedPrefixes, in the same\n'
    + 'edit that adds its resx keys. The tests walk the array, so a prefix that is only in the\n'
    + 'switch is rendered by nothing and every one of those tests still passes.');
  process.exit(1);
}

console.log(`Counted-string inventory OK: ${classified.length} prefix(es) classified in QuestionFor and the same ${inventory.length} listed in CountedPrefixes.`);
console.log(`Both parses found the control ${mustHit}, and neither side holds a duplicate.`);
console.log(`Classified: ${classified.slice().sort().join(', ')}`);
