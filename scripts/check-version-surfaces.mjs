#!/usr/bin/env node
// Fails (exit 1) when Directory.Build.props <Version> and the two app.manifest
// assemblyIdentity versions have drifted apart. bump-version.sh stamps all
// three from one argument; this is what makes that a rule rather than a habit.
//
// The manifest identity looks droppable and is not. Nothing at runtime reads
// it, but the loader validates the element at process start, so it must hold
// some version, and it is embedded in the shipped exe where sigcheck -m and any
// resource viewer show it. A version that no longer matches the release is a
// false claim inside the binary, read by whoever is deciding whether to trust
// an unsigned executable.
//
// This belongs on every push, not in the release scripts. The manifests live
// under src/, so once a release has built its binaries, correcting the version
// invalidates the build-provenance check, requires a rebuild, and changes every
// hash the release was scanned and cleared under. Before a build the same drift
// costs one bump-version.sh run.
//
// Run from the repo root: node scripts/check-version-surfaces.mjs
import { readFileSync } from 'node:fs';

const PROPS = 'Directory.Build.props';
const MANIFESTS = [
  ['src/InstallerClean/app.manifest', 'InstallerClean.app'],
  ['src/InstallerClean.Cli/app.manifest', 'InstallerClean.cli'],
];

const read = (p) => {
  try {
    return readFileSync(p, 'utf8');
  } catch {
    console.error(`check-version-surfaces: cannot read ${p}`);
    process.exit(1);
  }
};

const propsMatch = read(PROPS).match(/<Version>(\d+\.\d+\.\d+)<\/Version>/);
if (!propsMatch) {
  console.error(`check-version-surfaces: no <Version>X.Y.Z</Version> in ${PROPS}`);
  process.exit(1);
}
const version = propsMatch[1];

// The manifests carry a four-part version; the props carry three. The fourth is
// always 0, which is what bump-version.sh writes.
const expected = `${version}.0`;
const problems = [];

for (const [path, name] of MANIFESTS) {
  // Anchored on the name attribute so a manifest gaining a dependentAssembly
  // (which would bring its own assemblyIdentity) still matches the right one.
  const re = new RegExp(`<assemblyIdentity version="([\\d.]+)" name="${name.replace('.', '\\.')}"`);
  const m = read(path).match(re);
  if (!m) {
    problems.push(`${path}: no <assemblyIdentity version="..." name="${name}"/> found`);
  } else if (m[1] !== expected) {
    problems.push(`${path}: assemblyIdentity is ${m[1]}, expected ${expected}`);
  }
}

if (problems.length) {
  console.error(`check-version-surfaces: ${PROPS} says ${version}, so the manifests should say ${expected}.\n`);
  for (const p of problems) console.error(`  ${p}`);
  console.error(`\nFix: ./non-repo-files/bump-version.sh ${version}`);
  console.error('It stamps the props and both manifests from one argument.');
  process.exit(1);
}

console.log(`check-version-surfaces: OK (${version} / ${expected})`);
