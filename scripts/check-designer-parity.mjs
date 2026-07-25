#!/usr/bin/env node
// Checks Strings.Designer.cs against the neutral Strings.resx.
//
// The Designer is hand-managed (scripts/regenerate-strings-designer.sh), because
// the MSBuild ResX generator runs after MarkupCompilePass1 in this layout and so
// orders itself wrong. Hand-managed means the regenerate step can be forgotten,
// and the two drift silently: an accessor whose key has been removed from the
// resx still COMPILES, and Get() falls back to returning the key itself, so the
// first sign of it is a screen showing "Body.MainExplanation.Lead" to a user.
// Every other drift pair in this project has a guard (resx to satellites, resx to
// consumers, installer to SupportedLanguages, translation tables); this closes
// the last one.
//
// Checked both ways, plus the naming rule the generator applies:
//   - every resx key has an accessor
//   - every accessor names a key the resx defines
//   - the property name is the key with dots replaced by underscores
//
// This compares the two files rather than re-running the generator, so it costs
// milliseconds in CI instead of the regenerate script's two-plus minutes, and it
// fails on the drift that actually bites rather than on formatting.
//
// Run from the repo root: node scripts/check-designer-parity.mjs
import { readFileSync } from 'node:fs';

const resxPath = 'src/InstallerClean.Core/Resources/Strings.resx';
const designerPath = 'src/InstallerClean.Core/Resources/Strings.Designer.cs';

const resx = readFileSync(resxPath, 'utf8');
const designer = readFileSync(designerPath, 'utf8');

const resxKeys = new Set(
    [...resx.matchAll(/<data\s+name="([^"]+)"/g)].map((m) => m[1]),
);

// The generated accessor shape, and the only one this guard recognises:
//     public static string Action_About => Get("Action.About");
const accessors = [...designer.matchAll(
    /public\s+static\s+string\s+(\w+)\s*=>\s*Get\("([^"]+)"\)\s*;/g,
)].map((m) => ({ property: m[1], key: m[2] }));

const accessorKeys = new Set(accessors.map((a) => a.key));

const problems = [];

for (const key of resxKeys) {
    if (!accessorKeys.has(key))
        problems.push(`${key}: in Strings.resx with no accessor in Strings.Designer.cs`);
}

for (const { property, key } of accessors) {
    if (!resxKeys.has(key)) {
        problems.push(`${property}: accessor for "${key}", which Strings.resx does not define`);
        continue;
    }
    const expected = key.replaceAll('.', '_');
    if (property !== expected)
        problems.push(`${key}: accessor is named ${property}, expected ${expected}`);
}

if (accessors.length === 0)
    problems.push(
        'No accessors matched in Strings.Designer.cs. Either the file is truncated ' +
        '(the regenerate script was interrupted) or the generated shape changed and ' +
        'this guard needs updating with it.',
    );

if (problems.length > 0) {
    console.error(
        `Strings.Designer.cs is out of step with Strings.resx (${problems.length} problem(s)):\n`,
    );
    for (const p of problems.sort()) console.error(`  ${p}`);
    console.error(
        '\nRegenerate with: bash scripts/regenerate-strings-designer.sh' +
        '\nIt finishes in well under a second and says so, printing "Wrote ... (N lines)".' +
        '\nA run that ends any other way leaves a truncated file that breaks the build.',
    );
    process.exit(1);
}

console.log(
    `Strings.Designer.cs matches Strings.resx (${resxKeys.size} keys, ${accessors.length} accessors).`,
);
