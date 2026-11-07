#!/usr/bin/env node
// Fails (exit 1) when a member declared inside a C# 14 extension block in Core, the
// window or the command line is not public.
//
// Core's extension members are called from the window, the command line and the tests,
// and the window's and the command line's from the tests, each reaching the other's
// internals through InternalsVisibleTo. The C# compiler of the .NET 10 SDK pairs an
// extension member from another assembly with its implementation only where the
// member's accessibility equals that of the block's marker type, which it always emits
// public. So an internal member is refused in the other assembly (CS0570), or the call
// binds another member of that name instead. A public member inside an internal class
// is still reachable only from its own assembly and the ones it names.
//
// A block is `extension(...)` or `extension<...>(...)` standing where a declaration
// starts (after '{', '}' or ';'), its brackets followed by '{'. Every member at the top
// level of the block is read, from its first word to the '{', '=>' or ';' that ends its
// head, with the source's comments and string text blanked through csharp-source.mjs.
// Its modifiers, after any leading attributes, must include `public`. Each member is
// printed with its file, line and head as written. A block holding a preprocessor line
// is refused, since its branches cannot be read as one body.
//
// Run from the repo root: node scripts/check-extension-member-access.mjs
import { readFileSync } from 'node:fs';
import { readCSharp } from './csharp-source.mjs';
import { lineIndex, sourceFiles } from './source-files.mjs';

const ROOTS = ['src/InstallerClean.Core', 'src/InstallerClean', 'src/InstallerClean.Cli'];
const MODIFIERS = new Set([
  'public', 'internal', 'private', 'protected', 'static', 'readonly', 'unsafe', 'extern',
  'new', 'partial', 'async', 'virtual', 'abstract', 'sealed', 'override', 'required',
  'volatile', 'ref', 'file',
]);

const problems = [];
let blocks = 0;
let members = 0;

// The offset of the bracket closing the one at `open`, or -1.
function closing(bare, open, left, right) {
  let depth = 0;
  for (let i = open; i < bare.length; i++) {
    if (bare[i] === left) depth++;
    else if (bare[i] === right && --depth === 0) return i;
  }
  return -1;
}

const nextNonSpace = (bare, from) => {
  let i = from;
  while (i < bare.length && /\s/.test(bare[i])) i++;
  return i;
};

for (const file of ROOTS.flatMap((root) => sourceFiles(root, '.cs'))) {
  const { bare, directives } = readCSharp(readFileSync(file, 'utf8'));
  const lineOf = lineIndex(bare);
  const opener = /\bextension\s*(<[^>{};]*>)?\s*\(/g;
  let m;
  while ((m = opener.exec(bare)) !== null) {
    const before = bare.slice(0, m.index).trimEnd();
    if (before !== '' && !/[{};]$/.test(before)) continue;
    const paren = m.index + m[0].length - 1;
    const parenEnd = closing(bare, paren, '(', ')');
    if (parenEnd < 0) continue;
    const open = nextNonSpace(bare, parenEnd + 1);
    if (bare[open] !== '{') continue;
    const end = closing(bare, open, '{', '}');
    if (end < 0) {
      problems.push(`${file}:${lineOf(open)}: extension block is not closed`);
      continue;
    }
    blocks++;
    const firstLine = lineOf(open);
    const lastLine = lineOf(end);
    const inside = [...directives.keys()].filter((line) => line >= firstLine && line <= lastLine);
    if (inside.length) {
      problems.push(`${file}:${inside[0]}: preprocessor line inside an extension block`);
      opener.lastIndex = end;
      continue;
    }
    readMembers(file, lineOf, bare, open, end);
    opener.lastIndex = end;
  }
}

function readMembers(file, lineOf, bare, open, end) {
  let depth = 0;
  let head = open + 1;
  for (let i = open; i < end; i++) {
    const c = bare[i];
    if (c === '{') {
      if (depth === 1) checkHead(file, lineOf, bare, head, i);
      depth++;
    } else if (c === '}') {
      depth--;
      if (depth === 1) head = i + 1;
    } else if (depth === 1 && c === ';') {
      checkHead(file, lineOf, bare, head, i);
      head = i + 1;
    } else if (depth === 1 && c === '=' && bare[i + 1] === '>') {
      checkHead(file, lineOf, bare, head, i);
      // The expression body runs to its ';' at this depth.
      let d = 0;
      let j = i + 2;
      for (; j < end; j++) {
        if ('([{'.includes(bare[j])) d++;
        else if (')]}'.includes(bare[j])) d--;
        else if (bare[j] === ';' && d === 0) break;
      }
      i = j;
      head = j + 1;
    }
  }
}

function checkHead(file, lineOf, bare, from, to) {
  let text = bare.slice(from, to);
  // Leading attributes only; brackets later in the head are array or indexer brackets.
  for (;;) {
    const t = text.trimStart();
    if (!t.startsWith('[')) break;
    const close = closing(t, 0, '[', ']');
    if (close < 0) break;
    text = t.slice(close + 1);
  }
  const start = from + (bare.slice(from, to).length - text.length) + text.search(/\S|$/);
  const words = text.trim().split(/\s+/).filter(Boolean);
  if (words.length === 0) return;
  members++;
  const modifiers = [];
  for (const w of words) {
    if (MODIFIERS.has(w)) modifiers.push(w);
    else break;
  }
  const where = `${file}:${lineOf(start)}`;
  const shown = text.trim().replace(/\s+/g, ' ');
  if (modifiers.includes('public') && !modifiers.includes('protected')) {
    console.log(`${where}: ${shown}`);
  } else {
    problems.push(`${where}: not public: ${shown}`);
  }
}

console.log(`${blocks} extension blocks, ${members} members read`);
if (blocks === 0) problems.push(`no extension block found under ${ROOTS.join(', ')}`);

if (problems.length) {
  for (const p of problems) console.error(`check-extension-member-access: ${p}`);
  console.error('A member of an extension block must be declared public; its class stays internal.');
  process.exit(1);
}
