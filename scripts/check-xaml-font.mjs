#!/usr/bin/env node
// Fails (exit 1) when text can be drawn under a root that does not carry the
// app's font, Type.FontFamily.
//
// TEXT TAKES ITS FONT FROM THE ROOT IT IS DRAWN UNDER. FontFamily is inherited,
// and inheritance carries a value set on the parent, never a default, so an
// element with no font of its own draws in whatever its root was given. A root
// given nothing draws in the system font. The implicit TextBlock style in
// Themes/Components.xaml does not reach every element: a TextBlock carrying a
// Style of its own does not take it, and a TextBox draws its text itself rather
// than through a TextBlock. So the font is held at the roots.
//
// THE ROOTS ARE WINDOWS AND POPUPS, and each has its own rule.
//
//   A window. Every XAML file whose root element is Window sets
//   FontFamily="{DynamicResource Type.FontFamily}" on that root tag. An implicit
//   Window style cannot do it: an implicit style matches its exact type, and
//   every window in the app is a class derived from Window. A window built in C# alone has
//   no root tag to read, so every C# class derived from Window, directly or
//   through another, must be the x:Class of such a file, and C# constructs no
//   Window itself.
//
//   A popup. A ToolTip and a ContextMenu open in a popup with no parent, so
//   they inherit nothing from the window that opens them. Every Style targeting
//   either sets the font, by a Setter of its own or through BasedOn. Each of
//   the two has an implicit style, which is what a ToolTip="..." attribute and
//   a ContextMenu built in code take. A ToolTip or ContextMenu element naming a
//   Style names one of those, and C# sets no Style on either.
//
//   A UserControl. It is not a root of its own: its text takes the font of the window
//   it is placed in, through inheritance. One placed in a ToolTip or a ContextMenu,
//   or in a Style targeting either, would draw in the system font, so such a use
//   has to carry a FontFamily of its own and fails otherwise.
//
// EVERY REFERENCE TO THE TOKEN IS A DynamicResource. App.OnStartup writes the
// displayed language's family over Type.FontFamily in the application's own
// resources. A StaticResource inside Components.xaml resolves against the
// dictionaries the style was written in, Tokens.xaml among them, and never sees
// that entry, so a StaticResource anywhere in the XAML fails.
//
// EVERY TABLE FAILS CLOSED. A XAML root element this file does not know, and a
// Style= in C# outside an object initializer whose type it can read, stop the
// build rather than pass unread.
//
// XML and C# comments are blanked before anything is matched, newlines kept, so
// a reported line number is the line on disk and a tag quoted in prose is not
// read as markup. C# is read through csharp-source.mjs and XAML through
// xml-source.mjs, and a file either cannot follow to its end stops the run (exit
// 2), as does a XAML file holding a declaration or a tag that does not close.
//
// Run from the repo root: node scripts/check-xaml-font.mjs
import { readFileSync } from 'node:fs';
import { readCSharp } from './csharp-source.mjs';
import { lineIndex, sourceFiles } from './source-files.mjs';
import { blankXmlComments } from './xml-source.mjs';

const SRC = 'src';
const FONT = '{DynamicResource Type.FontFamily}';
const POPUPS = ['ToolTip', 'ContextMenu'];
// Roots that draw nothing themselves: Application hosts resources, and a
// ResourceDictionary's styles are checked where they target a popup.
const NON_DRAWING_ROOTS = new Set(['Application', 'ResourceDictionary']);
// A UserControl's text is drawn under the element it is placed in.
const HOSTED_ROOTS = new Set(['UserControl']);

// XAML with its comments blanked.
const xamlText = (file) => {
  try {
    return blankXmlComments(readFileSync(file, 'utf8'));
  } catch (e) {
    console.error(`check-xaml-font: ${file} cannot be read to its end (${e.message}).`);
    console.error('Refusing to report on a file whose markup cannot be told from its comments.');
    process.exit(2);
  }
};
// C# with its comments taken out, string literals left in place.
const csCode = (file) => {
  try {
    return readCSharp(readFileSync(file, 'utf8')).code;
  } catch (e) {
    console.error(`check-xaml-font: ${file} cannot be read to its end (${e.message}).`);
    console.error('Refusing to report on a file whose code cannot be told from its comments.');
    process.exit(2);
  }
};

const refuseMarkup = (file, line, what) => {
  console.error(`check-xaml-font: ${file}:${line}: ${what}.`);
  console.error('Refusing to report on a file whose markup cannot be read to its end.');
  process.exit(2);
};

// Tags in document order, quotes respected, so a '>' inside an attribute value
// does not end the tag. A processing instruction or a declaration is skipped. One
// with no '>' after it, or a tag with none, stops the run.
function tags(file, text, lineAt) {
  const out = [];
  let i = 0;
  while ((i = text.indexOf('<', i)) !== -1) {
    if (text[i + 1] === '?' || text[i + 1] === '!') {
      const end = text.indexOf('>', i);
      if (end === -1) refuseMarkup(file, lineAt(i), 'a declaration does not close');
      i = end + 1;
      continue;
    }
    let j = i + 1;
    let quote = null;
    for (; j < text.length; j++) {
      const c = text[j];
      if (quote) {
        if (c === quote) quote = null;
      } else if (c === '"' || c === "'") quote = c;
      else if (c === '>') break;
    }
    if (j === text.length) refuseMarkup(file, lineAt(i), 'a tag does not close');
    const raw = text.slice(i, j + 1);
    const m = raw.match(/^<(\/?)([\w:.]+)/);
    if (m) {
      out.push({
        name: m[2],
        closing: m[1] === '/',
        selfClosing: raw.endsWith('/>'),
        raw,
        line: lineAt(i),
      });
    }
    i = j + 1;
  }
  return out;
}

const attr = (raw, name) => {
  const m = raw.match(new RegExp(`\\s${name.replace('.', '\\.')}="([^"]*)"`));
  return m ? m[1] : null;
};

// The key a {StaticResource ...} names: a plain key, or {x:Type T}, which is
// the key an implicit style for T is stored under.
const styleKeyOf = (value) =>
  value.match(/^\{StaticResource\s+([\w.]+|\{x:Type\s+[\w:]+\})\}$/)?.[1]?.replace(/\s+/g, ' ') ?? null;

// TargetType="ToolTip" and TargetType="{x:Type ToolTip}" name the same type.
const targetOf = (raw) => {
  const t = attr(raw, 'TargetType');
  if (t === null) return null;
  const m = t.match(/^\{x:Type\s+([\w:]+)\}$/);
  return m ? m[1] : t;
};

const problems = [];
const matched = { windows: [], subclasses: [], popupStyles: [], popupElements: [], csStyles: [], tokenRefs: [] };

const xamlFiles = sourceFiles(SRC, '.xaml');
const csFiles = sourceFiles(SRC, '.cs').filter((f) => !f.endsWith('.Designer.cs'));

if (xamlFiles.length === 0 || csFiles.length === 0) {
  console.error(`check-xaml-font: found ${xamlFiles.length} XAML and ${csFiles.length} C# files under ${SRC}/.`);
  console.error('Both are expected, so the walk is reading the wrong place. Run from the repo root.');
  process.exit(1);
}

// --- XAML -------------------------------------------------------------------

const windowClasses = new Set(); // x:Class of every Window root carrying the font
const popupStyles = []; // { file, line, target, key, basedOn, setsFont, fontValue }
const popupElements = []; // { file, line, name, style }
const userControls = new Set(); // simple class name of every XAML file whose root is a UserControl
const elementUses = []; // { file, line, local, inPopup, hasFont } for every element, to find UserControls in popups

for (const file of xamlFiles) {
  const text = xamlText(file);
  const lineAt = lineIndex(text);

  for (const m of text.matchAll(/\{(StaticResource|DynamicResource)\s+Type\.FontFamily\s*\}/g)) {
    const line = lineAt(m.index);
    matched.tokenRefs.push(`${file}:${line} ${m[1]}`);
    if (m[1] === 'StaticResource') {
      problems.push(`${file}:${line}: names Type.FontFamily through StaticResource, which cannot see the family App.OnStartup writes; expected ${FONT}`);
    }
  }
  const all = tags(file, text, lineAt);
  const root = all.find((t) => !t.closing);
  if (!root) {
    problems.push(`${file}: no root element found`);
    continue;
  }

  if (root.name === 'Window') {
    const font = attr(root.raw, 'FontFamily');
    const cls = attr(root.raw, 'x:Class');
    matched.windows.push(`${file}:${root.line} ${cls ?? '(no x:Class)'} FontFamily=${font ?? '(none)'}`);
    if (font !== FONT) {
      problems.push(`${file}:${root.line}: the Window root sets ${font === null ? 'no FontFamily' : `FontFamily="${font}"`}, expected FontFamily="${FONT}"`);
    } else if (cls) {
      windowClasses.add(cls);
    }
  } else if (HOSTED_ROOTS.has(root.name)) {
    const cls = attr(root.raw, 'x:Class');
    if (cls) userControls.add(cls.split('.').pop());
    else problems.push(`${file}:${root.line}: the <${root.name}> root has no x:Class, so its uses cannot be found`);
  } else if (!NON_DRAWING_ROOTS.has(root.name)) {
    problems.push(`${file}:${root.line}: root element <${root.name}> is not one this check knows. Teach it where text under that root takes its font.`);
  }

  // Styles targeting a popup: collect each one's direct Setters by depth.
  const stack = [];
  for (const t of all) {
    if (t.closing) {
      const open = stack.pop();
      if (open?.style) popupStyles.push(open.style);
      continue;
    }
    const parent = stack[stack.length - 1];
    if (parent?.style && t.name === 'Setter' && attr(t.raw, 'Property') === 'FontFamily') {
      parent.style.setsFont = attr(t.raw, 'Value') === FONT;
      parent.style.fontValue = attr(t.raw, 'Value');
    }
    let style = null;
    if (t.name === 'Style' && POPUPS.includes(targetOf(t.raw))) {
      const based = attr(t.raw, 'BasedOn');
      style = {
        file,
        line: t.line,
        target: targetOf(t.raw),
        key: attr(t.raw, 'x:Key'),
        basedOn: based ? (styleKeyOf(based) ?? `unreadable: ${based}`) : null,
        setsFont: false,
        fontValue: null,
      };
    }
    elementUses.push({
      file,
      line: t.line,
      local: t.name.split(':').pop(),
      inPopup: stack.some((e) => POPUPS.includes(e.name) || e.style),
      hasFont: attr(t.raw, 'FontFamily') === FONT,
    });
    if (POPUPS.includes(t.name)) {
      popupElements.push({ file, line: t.line, name: t.name, style: attr(t.raw, 'Style') });
    }
    if (t.selfClosing) {
      if (style) popupStyles.push(style);
    } else {
      stack.push({ name: t.name, style });
    }
  }
}

for (const u of elementUses) {
  if (userControls.has(u.local) && u.inPopup && !u.hasFont) {
    problems.push(`${u.file}:${u.line}: <${u.local}> is a UserControl placed in a popup, which inherits no font from a window; expected FontFamily="${FONT}" on it`);
  }
}

// A style sets the font through its own Setter or through the chain it is
// BasedOn. Keys are looked up among the popup styles, so a BasedOn naming any
// other style is unreadable here and fails.
const byKey = new Map(popupStyles.map((s) => [s.key ?? `{x:Type ${s.target}}`, s]));
const carriesFont = (s, seen = new Set()) => {
  if (s.setsFont) return true;
  if (!s.basedOn || seen.has(s)) return false;
  seen.add(s);
  const base = byKey.get(s.basedOn);
  return base ? carriesFont(base, seen) : false;
};

for (const s of popupStyles) {
  const ok = carriesFont(s);
  matched.popupStyles.push(`${s.file}:${s.line} ${s.target} ${s.key ? `x:Key=${s.key}` : '(implicit)'}${s.basedOn ? ` BasedOn=${s.basedOn}` : ''} font=${ok ? 'yes' : 'no'}`);
  if (!ok) {
    const why = s.fontValue !== null ? `its FontFamily Setter has Value="${s.fontValue}"` : s.basedOn ? `neither it nor ${s.basedOn} sets FontFamily` : 'it sets no FontFamily';
    problems.push(`${s.file}:${s.line}: a ${s.target} style${s.key ? ` (${s.key})` : ''} does not carry the font: ${why}, expected Value="${FONT}"`);
  }
}

for (const p of POPUPS) {
  const implicit = popupStyles.filter((s) => s.target === p && !s.key);
  if (implicit.length !== 1) {
    problems.push(`expected exactly one implicit ${p} style, found ${implicit.length}: a ${p} with no Style of its own has to take one that carries the font`);
  }
}

for (const e of popupElements) {
  matched.popupElements.push(`${e.file}:${e.line} <${e.name}>${e.style ? ` Style=${e.style}` : ''}`);
  if (e.style === null) continue;
  const key = styleKeyOf(e.style);
  const s = key ? byKey.get(key) : null;
  if (!s || s.target !== e.name || !carriesFont(s)) {
    problems.push(`${e.file}:${e.line}: <${e.name} Style="${e.style}"> names no ${e.name} style carrying the font`);
  }
}

// --- C# ---------------------------------------------------------------------

// The first type in a class's base list, read from just after its name: past a
// type-parameter list and a primary constructor's parameter list, each balanced to
// its closer, to the colon. Null where the class names no base.
const baseAfter = (text, at) => {
  let i = at;
  const skipSpace = () => { while (/\s/.test(text[i] ?? '')) i++; };
  const skipBalanced = (open, close) => {
    let depth = 0;
    for (; i < text.length; i++) {
      if (text[i] === open) depth++;
      else if (text[i] === close && --depth === 0) { i++; return; }
    }
  };
  skipSpace();
  if (text[i] === '<') { skipBalanced('<', '>'); skipSpace(); }
  if (text[i] === '(') { skipBalanced('(', ')'); skipSpace(); }
  if (text[i] !== ':') return null;
  i++;
  skipSpace();
  return /^(?:global::)?[\w.]+/.exec(text.slice(i))?.[0] ?? null;
};

// Every class whose base chain reaches Window, from `class X : Base` across
// every file, the namespace taken from the file's own declaration. A primary
// constructor, class X(string m) : Base, is read the same way.
const bases = new Map(); // full name -> { base, file, line }
const simpleToFull = new Map();
const csTexts = [];
for (const file of csFiles) {
  const text = csCode(file);
  const lineAt = lineIndex(text);
  csTexts.push([file, text, lineAt]);
  const ns = text.match(/^\s*namespace\s+([\w.]+)/m)?.[1] ?? '';
  for (const m of text.matchAll(/\bclass\s+(\w+)/g)) {
    const base = baseAfter(text, m.index + m[0].length);
    if (base === null) continue;
    const full = ns ? `${ns}.${m[1]}` : m[1];
    bases.set(full, { base: base.replace(/^(?:global::)?System\.Windows\./, ''), file, line: lineAt(m.index) });
    if (!simpleToFull.has(m[1])) simpleToFull.set(m[1], []);
    simpleToFull.get(m[1]).push(full);
  }
}

const reachesWindow = (full, seen = new Set()) => {
  const entry = bases.get(full);
  if (!entry || seen.has(full)) return false;
  seen.add(full);
  if (entry.base === 'Window') return true;
  const next = bases.has(entry.base) ? [entry.base] : simpleToFull.get(entry.base.split('.').pop()) ?? [];
  return next.some((n) => reachesWindow(n, seen));
};

for (const [full, entry] of bases) {
  if (!reachesWindow(full)) continue;
  const ok = windowClasses.has(full);
  matched.subclasses.push(`${entry.file}:${entry.line} ${full} : ${entry.base} xaml-root-with-font=${ok ? 'yes' : 'no'}`);
  if (!ok) {
    problems.push(`${entry.file}:${entry.line}: ${full} derives from Window and is not the x:Class of a XAML Window root carrying FontFamily="${FONT}"`);
  }
}

const isWindowType = (type) =>
  type.replace(/\?$/, '').replace(/^(?:global::)?System\.Windows\./, '') === 'Window';

// A Window built in C# by name. A class derived from Window is held above to being
// the x:Class of a XAML root, so building one by name builds that window.
for (const [file, text, lineAt] of csTexts) {
  for (const m of text.matchAll(/\bnew\s+(?:global::)?(?:System\.Windows\.)?Window\s*[({]/g)) {
    problems.push(`${file}:${lineAt(m.index)}: constructs a Window in C#, which has no root tag to carry the font`);
  }
}

// A target-typed new(), which names no type, builds whatever its target is. Its
// target is read from what the new() stands in: a declaration, T x = new(...); a
// property initializer, T X { get; } = new(...); an assignment, x = new(...), with
// x's declaration in the file; an expression-bodied member, T Name(...) => new(...);
// and a return, from the nearest member header above it. A target that is Window
// fails as new Window() does. Only the
// projects building with WPF can construct a Window, and these are read from the
// project files; there a new() whose target none of those shapes gives fails too,
// unless TARGET_NOT_READ names it, by file and the text of its line, as building a
// type that is no Window. Each entry has to match exactly one such new().
const TARGET_NOT_READ = [
  // Arms of a switch expression whose value is a ScanFailureDescription.
  { file: 'src/InstallerClean/ViewModels/ScanViewModel.cs', text: 'new(ex.Message, Strings.Error_AdminRequiredTitle, IsError: false, Strings.Status_ScanAccessDenied),' },
  { file: 'src/InstallerClean/ViewModels/ScanViewModel.cs', text: 'new(Strings.Error_AdminRequiredBody, Strings.Error_AdminRequiredTitle, IsError: false, Strings.Status_ScanAccessDenied),' },
];
const wpfProjects = sourceFiles(SRC, '.csproj')
  .filter((p) => !p.startsWith(`${SRC}/InstallerClean.Tests/`))
  .filter((p) => /<UseWPF>\s*true\s*<\/UseWPF>/i.test(blankXmlComments(readFileSync(p, 'utf8'))))
  .map((p) => `${p.slice(0, p.lastIndexOf('/'))}/`);
if (wpfProjects.length === 0) problems.push('no project outside the tests builds with UseWPF, so the target-typed new() sweep read nothing');
const TYPE = String.raw`(?:global::)?[\w.]+(?:<[^;(){}=]*?>)?\??`;
const newTargetOf = (bare, at) => {
  const before = bare.slice(Math.max(0, at - 600), at);
  let m;
  if ((m = new RegExp(`(${TYPE})\\s+@?\\w+\\s*=\\s*$`).exec(before))) return m[1];
  if ((m = new RegExp(`(${TYPE})\\s+@?\\w+\\s*\\{[^{}]*\\}\\s*=\\s*$`).exec(before))) return m[1];
  if ((m = new RegExp(`(${TYPE})\\s+@?\\w+\\s*(?:\\([^()]*\\))?\\s*=>\\s*$`).exec(before))) return m[1];
  if ((m = /(?<![\w.])(?:this\.)?(@?\w+)\s*(?:=|\?\?=)\s*$/.exec(before))) {
    const name = m[1].replace(/^@/, '');
    const declared = new RegExp(`(${TYPE})\\s+@?${name}\\s*[;=,)]`).exec(bare);
    return declared ? declared[1] : null;
  }
  if (/\breturn\s*$/.test(before)) {
    const headers = [...bare.slice(0, at).matchAll(new RegExp(`(${TYPE})\\s+@?\\w+\\s*(?:<[^<>()]*>)?\\s*\\([^()]*\\)\\s*(?:where\\s[^{;]*)?\\{`, 'g'))];
    const last = headers[headers.length - 1];
    return last && !/^(?:if|while|for|foreach|switch|using|lock|catch|fixed|return|new|else)$/.test(last[1]) ? last[1] : null;
  }
  return null;
};
const notReadUsed = TARGET_NOT_READ.map(() => 0);
let targetTyped = 0;
for (const [file] of csTexts) {
  if (!wpfProjects.some((d) => file.startsWith(d))) continue;
  const bare = readCSharp(readFileSync(file, 'utf8')).bare;
  const lineAt = lineIndex(bare);
  const lines = readFileSync(file, 'utf8').split('\n');
  for (const m of bare.matchAll(/(?<![\w.@])new\s*\(/g)) {
    targetTyped++;
    const line = lineAt(m.index);
    const type = newTargetOf(bare, m.index);
    if (type !== null) {
      if (isWindowType(type)) problems.push(`${file}:${line}: constructs a ${type} with new(), and a Window built in C# has no root tag to carry the font`);
      continue;
    }
    const text = lines[line - 1].trim();
    const k = TARGET_NOT_READ.findIndex((e) => e.file === file && text.includes(e.text));
    if (k !== -1) { notReadUsed[k]++; continue; }
    problems.push(`${file}:${line}: a new() whose target type this check cannot read, so it cannot tell whether it builds a Window: ${text}`);
  }
}
TARGET_NOT_READ.forEach((e, k) => {
  if (notReadUsed[k] !== 1) problems.push(`TARGET_NOT_READ entry for ${e.file} matched ${notReadUsed[k]} new() expressions, not one: ${e.text}`);
});
console.log(`Target-typed new() in ${wpfProjects.join(', ')}: ${targetTyped}, ${TARGET_NOT_READ.length} named as no Window`);
if (targetTyped === 0) problems.push('the target-typed new() sweep matched nothing in the WPF projects, so it is not reading them');

for (const [file, text, lineAt] of csTexts) {

  // A Style set in C#. In an object initializer the type being built is read
  // from the nearest unclosed `new T {` before it; anywhere else the target
  // cannot be read, so it fails.
  for (const m of text.matchAll(/(?<![\w.])(\w+\.)?Style\s*=(?!=)/g)) {
    const line = lineAt(m.index);
    if (m[1]) {
      problems.push(`${file}:${line}: sets ${m[0].replace(/\s*=$/, '')} outside an object initializer, where this check cannot read the type being styled`);
      continue;
    }
    const before = text.slice(0, m.index);
    let depth = 0;
    let open = -1;
    for (let k = before.length - 1; k >= 0; k--) {
      if (before[k] === '}') depth++;
      else if (before[k] === '{') {
        if (depth === 0) { open = k; break; }
        depth--;
      }
    }
    // Constructor arguments may nest, as in new Hyperlink(new Run(text)), so
    // the parentheses are balanced back to their opener before the type is read.
    let head = open === -1 ? null : before.slice(0, open).trimEnd();
    if (head?.endsWith(')')) {
      let d = 0;
      let k = head.length - 1;
      for (; k >= 0; k--) {
        if (head[k] === ')') d++;
        else if (head[k] === '(' && --d === 0) break;
      }
      head = k < 0 ? null : head.slice(0, k);
    }
    const type = head?.match(/\bnew\s+([\w.]+)\s*$/)?.[1];
    matched.csStyles.push(`${file}:${line} Style= in new ${type ?? '(unreadable)'}`);
    if (!type) {
      problems.push(`${file}:${line}: sets a Style where this check cannot read the type being styled`);
    } else if (POPUPS.includes(type.split('.').pop())) {
      problems.push(`${file}:${line}: sets a Style on a ${type} in C#, which this check cannot follow to the font`);
    }
  }
}

// --- report -----------------------------------------------------------------

const print = (title, rows) => {
  console.log(`${title} (${rows.length}):`);
  for (const r of rows) console.log(`  ${r}`);
};
print('Window roots', matched.windows);
print('C# classes derived from Window', matched.subclasses);
print('Popup styles', matched.popupStyles);
print('Popup elements in XAML', matched.popupElements);
print('Styles set in C#', matched.csStyles);
print('References to Type.FontFamily in XAML', matched.tokenRefs);

// A walk that matched no window, or no popup style, has stopped reading the
// files rather than found them all correct.
if (matched.windows.length === 0 || matched.popupStyles.length === 0 || matched.tokenRefs.length === 0) {
  problems.push('the walk matched no Window root, no popup style or no reference to Type.FontFamily, so it is not reading the files it is meant to');
}

if (problems.length) {
  console.error('\ncheck-xaml-font: text can be drawn under a root without the app font.\n');
  for (const p of problems) console.error(`  ${p}`);
  console.error(`\nFix: set FontFamily="${FONT}" on the window's root tag, or a FontFamily`);
  console.error('Setter with that value in the popup style. Without it the text under that');
  console.error('root draws in the system font wherever a TextBlock carries a Style of its');
  console.error('own and wherever a TextBox draws its text.');
  process.exit(1);
}

console.log('\ncheck-xaml-font: OK');
