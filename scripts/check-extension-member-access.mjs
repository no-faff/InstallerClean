#!/usr/bin/env node
// Fails (exit 1) when a member declared inside a C# 14 extension block in Core is not
// public.
//
// Core's extension members are called from the window, the command line and the tests,
// which reach Core's internals through InternalsVisibleTo. The C# compiler of the .NET
// 10 SDK binds an extension member from another assembly only where the member's
// accessibility matches the block's, which it always emits public, so an internal member
// is refused in those assemblies (CS0570) or, where the framework has a member of the
// same name, the call binds the framework's instead. A public member inside an internal
// class is still reachable only from Core and the assemblies it names.
//
// Every member at the top level of each `extension(...)` or `extension<...>(...)` block
// is read, from its first word to the '{', '=>' or ';' that ends its head, with the
// source's comments and string text blanked through csharp-source.mjs. Its modifiers,
// after any attributes, must include `public`. Each member is printed with its file and
// line.
//
// Run from the repo root: node scripts/check-extension-member-access.mjs
import { readFileSync } from 'node:fs';
import { readCSharp } from './csharp-source.mjs';
import { lineIndex, sourceFiles } from './source-files.mjs';

const ROOT = 'src/InstallerClean.Core';

const problems = [];
let blocks = 0;
let members = 0;

for (const file of sourceFiles(ROOT, '.cs')) {
  const { bare } = readCSharp(readFileSync(file, 'utf8'));
  const lineOf = lineIndex(bare);
  const opener = /\bextension\s*(<[^>{};]*>)?\s*\(/g;
  let m;
  while ((m = opener.exec(bare)) !== null) {
    const open = bare.indexOf('{', m.index);
    if (open < 0) {
      problems.push(`${file}:${lineOf(m.index)}: extension block with no body`);
      continue;
    }
    blocks++;
    let depth = 0;
    let head = open + 1;
    let i = open;
    for (; i < bare.length; i++) {
      const c = bare[i];
      if (c === '{') {
        if (depth === 1) checkHead(file, lineOf, bare, head, i);
        depth++;
      } else if (c === '}') {
        depth--;
        if (depth === 0) break;
        if (depth === 1) head = i + 1;
      } else if (depth === 1 && c === ';') {
        checkHead(file, lineOf, bare, head, i);
        head = i + 1;
      } else if (depth === 1 && c === '=' && bare[i + 1] === '>') {
        checkHead(file, lineOf, bare, head, i);
        // The expression body runs to its ';' at this depth; skip to it.
        let j = i + 2;
        let d = 0;
        for (; j < bare.length; j++) {
          if (bare[j] === '(' || bare[j] === '[' || bare[j] === '{') d++;
          else if (bare[j] === ')' || bare[j] === ']' || bare[j] === '}') d--;
          else if (bare[j] === ';' && d === 0) break;
        }
        i = j;
        head = j + 1;
      }
    }
    if (depth !== 0) problems.push(`${file}:${lineOf(open)}: extension block is not closed`);
    opener.lastIndex = i;
  }
}

function checkHead(file, lineOf, bare, from, to) {
  const text = bare.slice(from, to);
  const start = from + text.search(/\S/);
  // Attributes first, then the modifiers up to the first word that is not one.
  const words = text.replace(/\[[^\]]*\]/g, ' ').trim().split(/\s+/);
  if (words.length === 0 || words[0] === '') return;
  members++;
  const modifiers = [];
  for (const w of words) {
    if (/^(public|internal|private|protected|static|readonly|unsafe|extern|new|partial)$/.test(w)) modifiers.push(w);
    else break;
  }
  const where = `${file}:${lineOf(start)}`;
  const shown = words.slice(0, modifiers.length + 3).join(' ');
  if (modifiers.includes('public') && !modifiers.includes('protected')) {
    console.log(`${where}: ${shown}`);
  } else {
    problems.push(`${where}: not public: ${shown}`);
  }
}

console.log(`${blocks} extension blocks, ${members} members read`);
if (blocks === 0) problems.push(`no extension block found under ${ROOT}`);

if (problems.length) {
  for (const p of problems) console.error(`check-extension-member-access: ${p}`);
  console.error('A member of an extension block in Core must be declared public; its class stays internal.');
  process.exit(1);
}
