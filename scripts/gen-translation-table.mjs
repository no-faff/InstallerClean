#!/usr/bin/env node
// Generate the readable English-beside-translation review table for one language's
// satellite resx, written to docs/translations/<code>.md. That table is the friendly
// surface a native-speaker reviewer reads: the invitation in each translated README
// links to it, so a helper sees the English UI string beside its translation, grouped
// by where each line shows up in the app, and never has to read raw resx XML or the
// internal key-names. It is generated, never hand-edited, so it cannot drift from the
// resx; re-run it whenever a satellite changes.
//
// USAGE  node scripts/gen-translation-table.mjs <code>
//   <code> is a shipped satellite code: zh-Hans, de, ko, es, ja, pt-BR, ru, fr.
// It reads the English neutral Strings.resx and Strings.<code>.resx, pairs them by
// key, and writes docs/translations/<code>.md (LF). The 60 Cli.* keys are CLI-only
// and English by contract (the CLI's stdout is a machine-readable interface), so they
// are skipped here exactly as the satellite omits them.
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';

const LANGS = {
  'zh-Hans': { en: 'Simplified Chinese',    endo: '简体中文',          readme: 'README.zh-CN.md' },
  'de':      { en: 'German',                endo: 'Deutsch',           readme: 'README.de.md' },
  'ko':      { en: 'Korean',                endo: '한국어',             readme: 'README.ko.md' },
  'es':      { en: 'Spanish',               endo: 'Español',           readme: 'README.es.md' },
  'ja':      { en: 'Japanese',              endo: '日本語',             readme: 'README.ja.md' },
  'pt-BR':   { en: 'Brazilian Portuguese',  endo: 'Português (Brasil)', readme: 'README.pt-BR.md' },
  'ru':      { en: 'Russian',               endo: 'Русский',           readme: 'README.ru.md' },
  'fr':      { en: 'French',                endo: 'Français',          readme: 'README.fr.md' },
};

const code = process.argv[2];
if (!code || !LANGS[code]) {
  console.error('usage: node scripts/gen-translation-table.mjs <code>');
  console.error('  <code> is one of: ' + Object.keys(LANGS).join(', '));
  process.exit(1);
}
const lang = LANGS[code];

const dir = 'src/InstallerClean.Core/Resources';
const BASE = `${dir}/Strings.resx`;             // English neutral
const TARGET = `${dir}/Strings.${code}.resx`;   // the satellite under review
const OUT = `docs/translations/${code}.md`;

// Same <data><value> capture the generator and its self-check use: the key name,
// then the inner value (non-greedy to the first </value>). The captured value is RAW,
// with its XML entities intact.
const parse = (xml) => {
  const map = new Map();
  const re = /<data\s+name="([^"]+)"[^>]*>\s*<value>([\s\S]*?)<\/value>/g;
  let m;
  while ((m = re.exec(xml)) !== null) map.set(m[1], m[2]);
  return map;
};

const neutral = parse(readFileSync(BASE, 'utf8'));
const target = parse(readFileSync(TARGET, 'utf8'));

// Groups in the order a user meets them: visible UI first, then the hover/tooltip and
// screen-reader text, then internals. A key joins the FIRST group whose prefix it
// carries; order within a group follows the neutral resx. The trailing '' prefix is a
// catch-all so a future key without a home still appears rather than vanishing.
const GROUPS = [
  ['Window titles',                    ['Window.']],
  ['Section headings',                 ['Section.']],
  ['Buttons and actions',              ['Action.']],
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

const keys = [...neutral.keys()].filter((k) => !k.startsWith('Cli.'));
const buckets = GROUPS.map(() => []);
for (const k of keys) buckets[groupOf(k)].push(k);

let missing = 0;
let md = `# InstallerClean UI in ${lang.endo} (${lang.en})\n\n`;
md += `The text of InstallerClean's interface in English on the left, with the ${lang.en} translation beside it, grouped by where each line appears in the app. It is here so someone who really knows ${lang.en} can read through the translation and flag anything that doesn't read well. See [Can you help translate the GUI?](../../${lang.readme}#can-you-help-translate-the-gui) for how to suggest a change, whether an issue or a pull request.\n\n`;
md += `A few lines (the app name, version and file-size formats) are meant to stay the same in every language, so leave those as they are. The translation file itself is [\`Strings.${code}.resx\`](../../${dir}/Strings.${code}.resx). This page is generated from it by \`scripts/gen-translation-table.mjs\`, so do not edit it by hand.\n`;

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

mkdirSync('docs/translations', { recursive: true });
writeFileSync(OUT, md.endsWith('\n') ? md : md + '\n', 'utf8');

const used = buckets.filter((b) => b.length).length;
console.log(`wrote ${OUT}: ${keys.length} strings across ${used} groups` +
  (missing ? `  !! ${missing} MISSING translations` : ''));
