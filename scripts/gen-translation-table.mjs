#!/usr/bin/env node
// Generate the readable English-beside-translation table for one language's
// satellite resx, written to docs/translations/<code>.md. That table is the friendly
// surface for anyone suggesting improvements to a shipped translation: the
// translation-feedback issue template and CONTRIBUTING.md point here, so a helper
// sees the English UI string beside its translation, grouped by where each line
// shows up in the app, and never has to read raw resx XML or the internal
// key-names. It is generated, never hand-edited, so it cannot drift from the
// resx; re-run it whenever a satellite changes.
//
// USAGE  node scripts/gen-translation-table.mjs <code>       write one table
//        node scripts/gen-translation-table.mjs --check      verify all fifteen
//        node scripts/gen-translation-table.mjs --check <code>
//   <code> is a satellite code: zh-Hans, de, ko, es, it, ja, pt-BR, ru, fr, pl,
//   tr, id, vi, uk, nl.
// It reads the English neutral Strings.resx and Strings.<code>.resx, pairs them by
// key, and writes docs/translations/<code>.md (LF). The human-facing Cli.* keys are
// translated and shown in their own group; the 20 machine-contract Cli.EventLog*
// keys (bar Cli.EventLogUnavailable) stay English by contract, so they are skipped
// here whether or not the satellite carries them (ja does). Only the machine set
// is counted, because it is closed by that contract while the human set grows
// with every string the CLI gains.
//
// --check is what makes "generated, never hand-edited" enforceable rather than
// merely stated. It rebuilds every table in memory and fails on any difference
// from the committed file, so a resx change that skips this script is caught on
// the commit that causes it instead of by whoever next reads the public page.
// Without it a table goes stale silently and reaches the public pages still
// quoting a string the app no longer has, on the very pages the project invites
// native speakers to review.
import { readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';

const LANGS = {
  'zh-Hans': { en: 'Simplified Chinese',    endo: '简体中文' },
  'de':      { en: 'German',                endo: 'Deutsch' },
  'ko':      { en: 'Korean',                endo: '한국어' },
  'es':      { en: 'Spanish',               endo: 'Español' },
  'it':      { en: 'Italian',               endo: 'Italiano' },
  'ja':      { en: 'Japanese',              endo: '日本語' },
  'pt-BR':   { en: 'Brazilian Portuguese',  endo: 'Português (Brasil)' },
  'ru':      { en: 'Russian',               endo: 'Русский' },
  'fr':      { en: 'French',                endo: 'Français' },
  'pl':      { en: 'Polish',                endo: 'Polski' },
  'tr':      { en: 'Turkish',               endo: 'Türkçe' },
  'id':      { en: 'Indonesian',            endo: 'Bahasa Indonesia' },
  'vi':      { en: 'Vietnamese',            endo: 'Tiếng Việt' },
  'uk':      { en: 'Ukrainian',             endo: 'Українська' },
  'nl':      { en: 'Dutch',                 endo: 'Nederlands' },
};

const args = process.argv.slice(2);
const checkMode = args[0] === '--check';
const code = checkMode ? args[1] : args[0];
if ((code && !LANGS[code]) || (!code && !checkMode)) {
  console.error('usage: node scripts/gen-translation-table.mjs <code>');
  console.error('       node scripts/gen-translation-table.mjs --check [<code>]');
  console.error('  <code> is one of: ' + Object.keys(LANGS).join(', '));
  process.exit(1);
}

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;             // English neutral

// Same <data><value> capture the generator and its self-check use: the key name,
// then the inner value (non-greedy to the first </value>). The captured value is RAW,
// with its XML entities intact.
// PARSE CONTROL. About the READING and not about the content: a regex that has
// stopped matching yields an empty set, and a silent zero over an empty set reads
// exactly like a clean result. BOTH legs are load-bearing. raw === 0 catches a
// file that declares no entry at all, which the equality cannot see on its own
// because 0 === 0 holds; parsed !== raw catches entries the reader dropped, which
// one <comment> moved above its <value> does to every regex wanting <value> on the
// same whitespace run as <data>, and the Visual Studio resx editor writes that
// shape. Counted with <data\b rather than '<data ' so a tab after the tag name is
// not read as an empty file. Neither figure is written down here, so adding a
// string to the resx cannot make this go stale.
//
// It gates the WRITE as well as the report, which is the half that mattered: this
// script is the only reader here that produces a public artefact. Reading the file
// in here rather than at the call site is what puts the control ahead of every
// writeFileSync, buildTable having to parse both files before it can return a page.
const parseControl = (file, xml, parsed) => {
  const raw = (xml.match(/<data\b/g) || []).length;
  if (raw !== 0 && parsed === raw) return;
  console.error(`PARSE CONTROL FAILED for ${file}: ${raw} '<data' occurrence(s), ${parsed} parsed.`);
  console.error('Refusing to act on a file this script cannot show it read.');
  process.exit(2);
};

const parse = (path) => {
  const xml = readFileSync(path, 'utf8');
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  parseControl(path, xml, map.size);
  return map;
};

const neutral = parse(BASE);

// Groups in the order a user meets them: visible UI first, then the hover/tooltip and
// screen-reader text, then internals. A key joins the FIRST group whose prefix it
// carries; order within a group follows the neutral resx. The trailing '' prefix is a
// catch-all so a future key without a home still appears rather than vanishing.
const GROUPS = [
  ['Window titles',                    ['Window.']],
  ['Section headings',                 ['Section.']],
  ['Buttons and actions',              ['Action.']],
  ['About window',                     ['About.']],
  ['Field labels',                     ['Field.']],
  ['Status and progress',              ['Status.']],
  ['Main screen text',                 ['Body.']],
  ['Reasons a file is unneeded',       ['Reason.']],
  ['Completion screen',                ['Completion.']],
  ['Recycle Bin unavailable',          ['RecycleUnavailable.']],
  ['Summaries and counts',             ['Summary.']],
  ['Confirmation dialogs',             ['Confirm.']],
  ['Error messages',                   ['Error.']],
  ['Update check',                     ['UpdateCheck.']],
  ['Opening links in your browser',    ['BrowserLaunch.']],
  ['Sending the summary',              ['ResultLog.', 'ConfirmSendResultLog.']],
  ['Startup and crashes',              ['Startup.', 'CrashLog.']],
  ['Tooltips (hover text)',            ['Tooltip.']],
  ['Screen reader labels',             ['Automation.']],
  ['File picker',                      ['FilePicker.']],
  ['Version',                          ['Version.']],
  ['Word forms (singular and plural)', ['Plural.']],
  ['Sizes and times',                  ['Display.']],
  ['Command-line tool (installerclean-cli)', ['Cli.']],
  ['Other',                            ['']],
];
const groupOf = (key) => GROUPS.findIndex(([, prefixes]) => prefixes.some((p) => key.startsWith(p)));

// Render a raw resx value into one markdown table cell. Two things would break the
// table: a literal newline (ends the row) and a literal pipe (splits the column), so
// newlines become <br> and pipes are escaped. resx text cannot contain a raw < or &
// (they must be entities), and the entities it does carry (&amp; &lt; &gt; &#39;) are
// valid HTML that GitHub renders natively, so nothing else needs sanitising.
const cell = (raw) => raw
  .replace(/&#10;/g, '<br>')         // the resx newline entity
  .replace(/\r\n|\r|\n/g, '<br>')    // a literal newline in a multi-line value
  .replace(/\|/g, '\\|');

// Drop only the 20 machine-contract CLI keys (the Cli.EventLog* set bar
// Cli.EventLogUnavailable, the one operator-facing warning); every other Cli.*
// key is translated and belongs in the table.
const isMachineCliKey = (k) => k.startsWith('Cli.') && k.includes('EventLog') && k !== 'Cli.EventLogUnavailable';
const keys = [...neutral.keys()].filter((k) => !isMachineCliKey(k));

// Builds one language's whole page in memory. Both modes go through it, so
// --check cannot drift from what a write would produce.
const buildTable = (c) => {
  const lang = LANGS[c];
  const target = parse(`${dir}/Strings.${c}.resx`);
  const buckets = GROUPS.map(() => []);
  for (const k of keys) buckets[groupOf(k)].push(k);

  let missing = 0;
  let md = `# InstallerClean in ${lang.endo} (${lang.en})\n\n`;
  md += `The text of InstallerClean's interface and command-line tool in English on the left, with the ${lang.en} translation beside it, grouped by where each line appears in the app. It is here so someone who really knows ${lang.en} can read through the translation and flag anything that could read better: [open an issue](https://github.com/no-faff/InstallerClean/issues/new?template=translation_review.md) or a pull request, with as few or as many changes as you like.\n\n`;
  md += `A few lines (the app name, version, file-size formats, and the command-line tool's flags and command names) are meant to stay the same in every language, so leave those as they are. This page is generated from [\`Strings.${c}.resx\`](../../${dir}/Strings.${c}.resx), so do not edit it by hand. The ${lang.en} translation itself lives in [\`gen-strings-${c}.mjs\`](../../scripts/translations/gen-strings-${c}.mjs).\n\n`;
  // The token is the one piece of markup a reviewer meets that is not obviously
  // machinery, and translating it is the natural mistake: it reads like a word.
  // Said here rather than only in the maintainer notes, because this page is
  // what the README invites people to work from.
  md += `\`{InstallerFolder}\` and the numbered slots (\`{0}\`, \`{1}\`) are filled in by the app when it runs, so keep them exactly as they are. \`{InstallerFolder}\` becomes the real installer folder on that machine, usually \`C:\\Windows\\Installer\`. Move them within the sentence if the grammar needs it; do not translate them.\n`;

  for (let i = 0; i < GROUPS.length; i++) {
    const list = buckets[i];
    if (!list.length) continue;
    md += `\n## ${GROUPS[i][0]}\n\n`;
    md += `| English | ${lang.endo} |\n| --- | --- |\n`;
    for (const k of list) {
      const en = cell(neutral.get(k) ?? '');
      let tr;
      if (target.has(k)) tr = cell(target.get(k));
      else { tr = '_(missing)_'; missing++; }
      md += `| ${en} | ${tr} |\n`;
    }
  }

  return {
    md: md.endsWith('\n') ? md : md + '\n',
    missing,
    groups: buckets.filter((b) => b.length).length,
  };
};

const outPath = (c) => `docs/translations/${c}.md`;

if (!checkMode) {
  const { md, missing, groups } = buildTable(code);
  mkdirSync('docs/translations', { recursive: true });
  writeFileSync(outPath(code), md, 'utf8');
  console.log(`wrote ${outPath(code)}: ${keys.length} strings across ${groups} groups` +
    (missing ? `  !! ${missing} MISSING translations` : ''));
  process.exit(0);
}

// --check: rebuild and compare, never write. The summary counts differing lines
// rather than printing a diff, because every line here is a full UI string and
// fifteen tables' worth would bury the one fact that matters, which language
// and how far out of step.
const codes = code ? [code] : Object.keys(LANGS);
let stale = 0;

// Multiset line comparison. An LCS diff would report the same totals for the
// edits these files actually take (a row's value changing, a row appearing or
// retiring) and this cannot get the counts subtly wrong on a 400-row table.
const lineDelta = (committed, generated) => {
  const counts = new Map();
  for (const l of committed.split('\n')) counts.set(l, (counts.get(l) ?? 0) + 1);
  let added = 0;
  for (const l of generated.split('\n')) {
    const n = counts.get(l) ?? 0;
    if (n > 0) counts.set(l, n - 1); else added++;
  }
  let removed = 0;
  for (const n of counts.values()) removed += n;
  return { added, removed };
};

for (const c of codes) {
  const path = outPath(c);
  const { md } = buildTable(c);
  if (!existsSync(path)) {
    console.error(`${path}: MISSING. Run: node scripts/gen-translation-table.mjs ${c}`);
    stale++;
    continue;
  }
  const committed = readFileSync(path, 'utf8');
  if (committed === md) continue;
  const { added, removed } = lineDelta(committed, md);
  console.error(`${path}: STALE (${added} line(s) to add, ${removed} to remove).`);
  stale++;
}

if (stale) {
  console.error('');
  console.error(`Translation-table gate: ${stale} of ${codes.length} table(s) out of step with the resx.`);
  console.error('These pages are what the project invites native speakers to review, so a stale one');
  console.error('asks for corrections to text the app no longer has. Regenerate and commit:');
  console.error('  for c in ' + Object.keys(LANGS).join(' ') + '; do node scripts/gen-translation-table.mjs $c; done');
  process.exit(1);
}

console.log(`Translation tables: all ${codes.length} in step with the resx.`);
