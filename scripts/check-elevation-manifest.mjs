#!/usr/bin/env node
// Fails (exit 1) when either app.manifest stops asking for administrator, or
// asks in a way the loader will not honour. Both hosts must declare
// <requestedExecutionLevel level="requireAdministrator"/>, and this is what
// makes that a rule rather than a line nobody has cause to look at.
//
// THE LEVEL IS PART OF THE SCAN'S CORRECTNESS, NOT ONLY ITS WRITE ACCESS, which
// is the half that would not be guessed from the element. The scan asks Windows
// Installer whether a product a patch file declares is installed, and takes
// ERROR_UNKNOWN_PRODUCT as a positive answer that no installation of it is there,
// wherever the product enumerations returned none; a product the registry names
// that they returned no installation of is read off the registry. Only a caller
// entitled to see every user's installs gets that answer: an administrator may
// query product and patch data for any instance and any user on the computer, and
// a non-administrator may not. A caller that cannot see a per-user product
// belonging to another account is told exactly that about a product which is in
// fact there, so the same answer stops meaning the same thing. Lowering the level
// would therefore change what the scan concludes about whether a cached patch is
// still needed, with the build green and every test still passing, because no test
// starts a process at a different level.
//
// The element carries the same weight in both hosts and neither is the lesser
// case: the command-line tool runs unattended from scheduled tasks, where a
// silent change of meaning has nobody watching it.
//
// XML COMMENTS ARE BLANKED BEFORE ANYTHING IS MATCHED, through xml-source.mjs. Both
// manifests explain the level in a comment beside it, and those comments name both
// this level and the one it must not become, so a check reading the raw file would
// find either spelling whatever the element says. A comment that does not close is
// reported with its line, the rest of that file being unreadable as markup.
//
// Run from the repo root: node scripts/check-elevation-manifest.mjs
import { readFileSync } from 'node:fs';
import { blankXmlComments } from './xml-source.mjs';

const REQUIRED = 'requireAdministrator';

const MANIFESTS = [
  'src/InstallerClean/app.manifest',
  'src/InstallerClean.Cli/app.manifest',
];

const read = (p) => {
  try {
    return readFileSync(p, 'utf8');
  } catch {
    console.error(`check-elevation-manifest: cannot read ${p}`);
    process.exit(1);
  }
};

const problems = [];
// A manifest whose comments cannot be told from its markup, kept apart from the
// problems with the level, since the fix for it is a different one.
const unreadable = [];

for (const path of MANIFESTS) {
  let xml;
  try {
    xml = blankXmlComments(read(path));
  } catch (e) {
    unreadable.push(`${path}: ${e.message}`);
    continue;
  }

  const tags = xml.match(/<requestedExecutionLevel\b[^>]*>/g) ?? [];

  if (tags.length === 0) {
    problems.push(`${path}: no <requestedExecutionLevel/> element outside a comment`);
    continue;
  }
  // Two of them would leave which one applies to the reader's eye rather than to
  // a rule, so the count is part of what is being held.
  if (tags.length > 1) {
    problems.push(`${path}: ${tags.length} <requestedExecutionLevel/> elements, expected exactly one`);
    continue;
  }

  const level = tags[0].match(/\blevel="([^"]*)"/);
  if (!level) {
    problems.push(`${path}: <requestedExecutionLevel/> carries no level attribute`);
  } else if (level[1] !== REQUIRED) {
    problems.push(`${path}: level is "${level[1]}", expected "${REQUIRED}"`);
  }
}

if (unreadable.length) {
  console.error('check-elevation-manifest: a manifest cannot be read to its end.\n');
  for (const u of unreadable) console.error(`  ${u}`);
  console.error("\nFix: close the comment with '-->'. Everything after an unclosed '<!--' is");
  console.error('part of the comment, so the level this check holds cannot be read.');
  if (problems.length) console.error('');
}

if (problems.length) {
  console.error('check-elevation-manifest: both hosts must ask for administrator.\n');
  for (const p of problems) console.error(`  ${p}`);
  console.error(`\nFix: set level="${REQUIRED}" on the <requestedExecutionLevel/>`);
  console.error('element in each manifest. The scan reads a per-user product it is not');
  console.error('entitled to see as a product that is not installed, so the level decides');
  console.error('what the scan concludes, not merely whether a later write succeeds.');
}

if (unreadable.length || problems.length) process.exit(1);

console.log(`check-elevation-manifest: OK (${MANIFESTS.length} manifests, level="${REQUIRED}")`);
