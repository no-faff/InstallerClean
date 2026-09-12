#!/usr/bin/env node
// Fails (exit 1) when a XAML resource reference names a key nothing defines,
// when it names one the consumer cannot reach, when a defined key has no
// consumer, or when a resource's runtime type cannot satisfy the property
// consuming it.
//
// THE DANGLING REFERENCE. A {StaticResource X} naming a key that does not exist
// is not a compile error: XAML resource lookup happens when the consuming
// template is first realised, so the failure surfaces as a XamlParseException
// the first time a user opens the affected window. The build stays green, and
// the tests that do read this markup read it for the theme's cross-file ties
// rather than to resolve a reference, so nothing in the ordinary loop catches
// it. Renaming a token is the operation that produces it: a rename that misses
// one consumer ships a window that throws on open.
//
// THE TYPE MATCH. Resolution is by name and runs no TypeConverter, so a
// resource's runtime type has to satisfy the consuming property's declared type
// on its own: a Double will not fill a Thickness, and a Brush will not fill a
// Style. The compiler sees none of it. The markup is well formed and the key
// resolves, so this fails on the same trigger as a dangling reference and looks
// the same to everything upstream of the user. Inline attribute syntax
// (BorderThickness="1") does run the converter, which is why the strict rule
// belongs to resource lookups alone and why a numeric token consumed as both a
// Thickness and a Double needs parallel resources of each type.
//
// The dead-key half is the same reasoning as scripts/check-dead-resx-keys.mjs: a
// token nothing consumes is dead weight that reads as load-bearing.
//
// This is the XAML-key gate half of scripts/list-unused-resources.sh, whose
// x:Key report is read-only. The two differ deliberately in one way: this guard
// strips XML comments before it reads anything (below), where that script scans
// the raw text. So this guard sees a key named only in prose as dead, and that
// script sees it as alive. Where the two disagree, the comment is the difference.
//
// ONE PARSE ANSWERS BOTH QUESTIONS, AND THAT IS THE POINT OF ITS SHAPE. The type
// half needs the element a reference sits in, because a <Setter>'s target is a
// sibling attribute and a tag runs across as many lines as its author wanted. A
// sweep that reads one line at a time can find the key and cannot find the
// property. So the file is walked as tags, and the name half is fed from the
// same walk rather than from a sweep of its own. Two sweeps over these files
// would be two copies of the collection, the comment stripping and the
// reference pattern, free to answer about different sets of sites while both
// report success.
//
// WHAT COUNTS AS A DEFINITION
//   x:Key="Name" in any .xaml under src/.
//
//   x:Key="{x:Static ...}" is skipped. A markup-extension key resolves to a
//   value at runtime, not to a name a static parse could match, and the one in
//   the repo (SystemParameters.FocusVisualStyleKey) is claimed by WPF itself.
//
// WHERE A DEFINITION CAN BE REACHED FROM, WHICH IS A DIFFERENT QUESTION FROM
// WHETHER IT EXISTS. A StaticResource lookup walks up from the consuming element,
// so it reaches the dictionaries above that element and no others. Everything
// App.xaml merges is application scope and every file can see it; a key in a
// window's own Resources can be reached from that window and nowhere else. A
// reference to one from another file resolves for a reader, and for a check that
// pools every declaration into one namespace, and throws when the template is
// realised.
//
//   The merge chain is followed from App.xaml rather than listed, so a dictionary
//   added to it becomes application scope without this file being told, and a
//   Source naming something not here stops the run rather than leaving the answer
//   half built.
//
//   Reachability is worked out one file at a time, which is the same question as
//   the real one exactly while no resource is declared below the root element's
//   own Resources. One inside a Grid or a ControlTemplate is narrower than its
//   file, so the guard checks that condition instead of resting on it.
//
//   A lookup in C# runs from an element this parse cannot identify, so it is held
//   to the narrower rule that a file-local key is reachable only from the
//   code-behind of the file declaring it.
//
// WHAT COUNTS AS A REFERENCE
//   XAML: {StaticResource Key} and {DynamicResource Key}, wherever they sit in
//   an attribute value. That includes one nested inside another markup
//   extension, which is how every converter in this app is reached: the slot
//   being filled there is the extension's own property rather than the
//   attribute's, and the type half needs to know which.
//
//   {StaticResource {x:Type Foo}} is a reference to an implicit style keyed by
//   type. It is recognised and set aside before the by-name pattern runs, so
//   that setting it aside is a decision this file makes rather than a shape the
//   pattern happens to miss. There is nothing for either half to check: no name
//   to resolve, and an implicit style is a Style by construction.
//
//   C#: only the explicit lookup sites, FindResource / TryFindResource /
//   SetResourceReference / Resources["..."] with a literal key. A C# string
//   literal is NOT read as a reference merely for looking like a token name:
//   resx keys share the Action.* and Status.* prefixes with theme tokens
//   ("Status.Moving" is a resx plural prefix, "Status.Warning" is a brush), and
//   dotted literals in this codebase are mostly type and member names. Matching
//   on shape would flag those as dangling theme references.
//
//   The one lookup built from a name rather than written at the site is App's
//   TypeSizeTokenKeys, whose literals reach Resources[key] in ApplyTextScaling.
//   A static parse cannot follow a variable through an indexer, so the array is
//   read directly, by name, below. It is the exact bug this guard is for: a
//   Type.* rename that misses that array silently stops text scaling (the
//   indexer returns null, the `is double` test fails and the token is skipped),
//   with no exception to notice. The parse fails loudly if the array stops
//   matching, so it cannot rot into a check that quietly covers nothing.
//
//   A C# lookup site is a name and nothing else, so it feeds the name half
//   alone. What the retrieved object is cast to sits in the C# type system,
//   where the compiler already has it.
//
// EVERY TABLE BELOW FAILS CLOSED. An element type this file cannot name, a
// property it cannot type, or a markup-extension slot it does not know stops the
// build and asks for a decision. The alternative is a gate that waves through
// whatever it has not been taught, stays green, and covers less of the app each
// time one is added.
//
// COMMENTS ARE STRIPPED FIRST, both sides. A comment is not a reference: WPF
// never resolves one. Themes/Primitives.xaml explains the type-matching rule
// using "{StaticResource X}" in prose, which without stripping is a reference to
// a key named X that nothing defines, i.e. an instant false failure.
//
// bin/ and obj/ are excluded because build output mirrors source.
//
// Run from the repo root: node scripts/check-xaml-resources.mjs
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join, sep } from 'node:path';

const SRC = 'src';
const APP_XAML = `${SRC}/InstallerClean/App.xaml`;
const APP_XAML_CS = `${SRC}/InstallerClean/App.xaml.cs`;

const WPF_NS = 'http://schemas.microsoft.com/winfx/2006/xaml/presentation';
const XAML_NS = 'http://schemas.microsoft.com/winfx/2006/xaml';

// Keys defined on purpose with no consumer in the source. Each earns its place
// with a reason; an unconsumed key NOT listed here fails the guard.
const UNCONSUMED_ALLOWLIST = new Set([
  // The red pill. Kept in the theme with nothing built on it, deliberately:
  // the app's one irreversible-looking action stopped being framed as a hazard
  // when Delete became an ordinary permanent delete, and the screen that offers
  // it opens by saying the files are safe to delete, so red would be the single
  // element arguing with that. It stays because the decision is about this
  // screen rather than about the token, and a future screen with a genuine
  // hazard on it should not have to reinvent the style.
  'DangerPill',
]);

// Referenced keys that nothing in this repo defines: a resource WPF or the
// system supplies. Each earns its place with a reason; a dangling reference NOT
// listed here fails the guard, because that is the missed-rename crash.
const EXTERNAL_ALLOWLIST = new Set([]);

// The runtime type each resource element produces, and the declared types that
// type can satisfy. A resource is written as an element, so the element's name
// IS its runtime type; the list beside it is that type plus the bases and
// interfaces a consuming property in this app actually declares. The list is
// deliberately not a full ancestry: a base nothing here consumes would be an
// assertion this file cannot check.
//
// Keyed by resolved XML namespace rather than by the prefix as written, because
// a prefix is a local nickname. Renaming xmlns:sys would otherwise fail the
// build over a spelling.
const RESOURCE_TYPES = new Map([
  [`{${WPF_NS}}SolidColorBrush`, ['SolidColorBrush', 'Brush']],
  [`{${WPF_NS}}Color`, ['Color']],
  [`{${WPF_NS}}Style`, ['Style']],
  [`{${WPF_NS}}Thickness`, ['Thickness']],
  [`{${WPF_NS}}CornerRadius`, ['CornerRadius']],
  [`{${WPF_NS}}FontFamily`, ['FontFamily']],
  [`{${WPF_NS}}PathGeometry`, ['PathGeometry', 'Geometry']],
  [`{${WPF_NS}}BooleanToVisibilityConverter`, ['BooleanToVisibilityConverter', 'IValueConverter']],
  ['{clr-namespace:System;assembly=mscorlib}Double', ['Double']],
  ['{clr-namespace:InstallerClean.Helpers}InstallerPathTextConverter',
    ['InstallerPathTextConverter', 'IValueConverter']],
]);

// The declared type of every property in this app that is filled from a
// resource, written as it appears in the markup. A <Setter Property="X"> names
// the same property as an X="..." attribute and reads from the same table.
//
// Qualified names (an attached property, or Control.Template) are held under
// their full spelling. Trimming the qualifier off would let a lookup for an
// attached property answer with the plain property's type, which is a different
// property of a different type wearing a similar name.
const PROPERTY_TYPES = new Map([
  ['Background', 'Brush'],
  ['BorderBrush', 'Brush'],
  ['CaretBrush', 'Brush'],
  ['Data', 'Geometry'],
  ['Fill', 'Brush'],
  ['Foreground', 'Brush'],
  ['Stroke', 'Brush'],
  ['BorderThickness', 'Thickness'],
  ['Margin', 'Thickness'],
  ['Padding', 'Thickness'],
  ['CornerRadius', 'CornerRadius'],
  ['Color', 'Color'],
  ['FontFamily', 'FontFamily'],
  ['FontSize', 'Double'],
  ['BasedOn', 'Style'],
  ['FocusVisualStyle', 'Style'],
  ['HeaderContainerStyle', 'Style'],
  ['Style', 'Style'],
]);

// Where a row above claims a resource satisfies something OTHER than its own
// type, that claim comes from one of two places, and they keep differently.
//
// A type declared in this repository can move under us, so its claims are read
// off its own declaration below rather than recorded here. A framework type
// cannot: its hierarchy is fixed by the framework and is part of the contract the
// app compiles against. Those claims are recorded here instead, by name.
//
// The point of naming them is that the list is CHECKED both ways. A framework row
// claiming something not recorded here stops the build, so the set of claims taken
// on the framework's word cannot grow without somebody writing it down; and a name
// here that no longer matches a claim stops the build too, so the list cannot keep
// entries for rows that have gone. Identity rows, where a resource satisfies its
// own type and nothing else, make no claim and appear in neither place.
const FRAMEWORK_INHERITANCE = new Map([
  [`{${WPF_NS}}SolidColorBrush`, ['Brush']],
  [`{${WPF_NS}}BooleanToVisibilityConverter`, ['IValueConverter']],
  [`{${WPF_NS}}PathGeometry`, ['Geometry']],
]);

// A resource reached from inside another markup extension fills that
// extension's property, not the attribute's. Every converter in the app arrives
// this way, as {Binding ..., Converter={StaticResource ...}}, and the attribute
// around it is typically Visibility, which a converter could never satisfy: read
// the outer property and the guard would report a fault that is not there.
const EXTENSION_SLOTS = new Map([
  ['Binding.Converter', 'IValueConverter'],
]);

// Paths are normalised to forward slashes. CI runs this on Windows, where
// path.join yields backslashes, and the theme-file test below matches on a path
// fragment: without this the collision check would quietly pass everything on the
// one machine that gates the build.
function collect(dir, ext, out = []) {
  for (const name of readdirSync(dir)) {
    if (name === 'bin' || name === 'obj') continue;
    const p = join(dir, name);
    if (statSync(p).isDirectory()) collect(p, ext, out);
    else if (name.endsWith(ext)) out.push(p.split(sep).join('/'));
  }
  return out;
}

// Blanked rather than deleted, newlines kept, so every reported line number is
// the line number in the file on disk.
const blank = (m) => m.replace(/[^\n]/g, ' ');

// Reports read as prose, and "a IValueConverter" reads as a typo in the guard
// rather than as the fault it is describing.
const article = (t) => (/^[aeiou]/i.test(t) ? 'an' : 'a');

const stripXmlComments = (s) => s.replace(/<!--[\s\S]*?-->/g, blank);
// Line and block comments. The string-literal alternative is matched FIRST and
// put back verbatim, so a // or /* inside a string is not read as a comment.
const stripCsComments = (s) =>
  s.replace(/"(?:\\.|[^"\\])*"|\/\/[^\n]*|\/\*[\s\S]*?\*\//g, (m) =>
    m.startsWith('"') ? m : blank(m));

const xamlFiles = collect(SRC, '.xaml');
const csFiles = collect(SRC, '.cs').filter((f) => !f.endsWith('Strings.Designer.cs'));

const xaml = xamlFiles.map((f) => [f, stripXmlComments(readFileSync(f, 'utf8'))]);
const cs = csFiles.map((f) => [f, stripCsComments(readFileSync(f, 'utf8'))]);

// --- the XAML walk -----------------------------------------------------------
// Element tags, whole, however many lines they span. The attribute blob skips
// over quoted runs so that a > inside an attribute value does not end the tag.
const TAG = /<(\/?)([A-Za-z_][\w:.\-]*)((?:[^<>"']|"[^"]*"|'[^']*')*?)(\/?)>/g;
const ATTR = /([\w:.\-]+)\s*=\s*(?:"([^"]*)"|'([^']*)')/g;
// The implicit-style form, recognised so it can be set aside before the by-name
// pattern runs.
const TYPE_KEYED = /\{\s*(?:Static|Dynamic)Resource\s+\{\s*x:Type\s+[^{}]*\}\s*\}/g;
const RESOURCE = /\{\s*(Static|Dynamic)Resource\s+([^}\s]+)\s*\}/g;

const lineCounter = (text) => (offset) => {
  let n = 1;
  for (let i = 0; i < offset; i++) if (text[i] === '\n') n++;
  return n;
};

// The prefix-to-namespace map for one file. Declarations sit on the root element
// in every file here, but any element may carry one, so all of them are read. A
// prefix bound twice to different namespaces would make every lookup under it
// ambiguous, so it stops the run rather than picking one.
function namespaces(file, text) {
  const map = new Map();
  for (const [, decl, dq, sq] of text.matchAll(
    /(xmlns(?::[\w.\-]+)?)\s*=\s*(?:"([^"]*)"|'([^']*)')/g
  )) {
    const prefix = decl.includes(':') ? decl.slice(decl.indexOf(':') + 1) : '';
    const uri = dq ?? sq;
    const seen = map.get(prefix);
    if (seen !== undefined && seen !== uri) {
      console.error(`FAILED: ${file} binds the prefix "${prefix}" to two namespaces.`);
      console.error(`  ${seen}`);
      console.error(`  ${uri}`);
      console.error('Every element name under it would resolve two ways. Use one binding.');
      process.exit(1);
    }
    map.set(prefix, uri);
  }
  // The two a XAML file may use without declaring.
  if (!map.has('')) map.set('', WPF_NS);
  if (!map.has('x')) map.set('x', XAML_NS);
  return map;
}

const resolve = (ns, qualified) => {
  const i = qualified.indexOf(':');
  const prefix = i < 0 ? '' : qualified.slice(0, i);
  const local = i < 0 ? qualified : qualified.slice(i + 1);
  const uri = ns.get(prefix);
  return { uri, local, expanded: uri ? `{${uri}}${local}` : null };
};

// The slot a reference fills, when it sits inside another markup extension:
// the innermost extension still open at that point, and the property of it
// being assigned. Braces are counted rather than matched, which is what
// distinguishes a nested reference from one that merely follows a closed
// extension in the same attribute value.
function enclosingExtension(value, at) {
  const open = [];
  for (let i = 0; i < at; i++) {
    if (value[i] === '{') open.push(i);
    else if (value[i] === '}') open.pop();
  }
  if (!open.length) return null;
  const start = open[open.length - 1];
  const name = /^\{\s*([\w:.\-]+)/.exec(value.slice(start));
  const prop = /([\w.:]+)\s*=\s*$/.exec(value.slice(start, at));
  return {
    extension: name ? name[1] : null,
    property: prop ? prop[1] : null,
  };
}

const definitions = []; // { file, line, key, element, whole }
const references = []; // { file, line, key, slot, describeSlot }
const merged = new Map(); // file -> [file, ...] it merges
let typeKeyedSites = 0;

// The elements a resource may sit under and still belong to the whole file: the
// root's own Resources, and a ResourceDictionary inside it. Anything else, a
// Grid or a Style or a ControlTemplate, scopes the resource to that subtree and
// not to the file.
const SCOPE_WRAPPER = /^(?:[\w:.]+\.Resources|ResourceDictionary)$/;

// Relative to the file holding it, and resolved without the platform's separator:
// CI runs this on Windows, where a resolver that returned backslashes would match
// nothing against the forward-slash paths everything else here uses.
const resolveSource = (fromFile, src) => {
  const out = [];
  for (const part of fromFile.split('/').slice(0, -1).concat(src.split(/[\\/]/))) {
    if (part === '' || part === '.') continue;
    if (part === '..') out.pop();
    else out.push(part);
  }
  return out.join('/');
};

for (const [file, text] of xaml) {
  const ns = namespaces(file, text);
  const lineAt = lineCounter(text);
  const stack = [];
  for (const tag of text.matchAll(TAG)) {
    const element = tag[2];
    const blob = tag[3];
    if (tag[1] === '/') {
      stack.pop();
      continue;
    }
    const selfClosing = tag[4] === '/';
    const blobStart = tag.index + 1 + element.length;
    const attrs = new Map();
    for (const a of blob.matchAll(ATTR)) attrs.set(a[1], a[2] ?? a[3]);
    if (!selfClosing) stack.push(element);
    const path = selfClosing ? stack : stack.slice(0, -1);

    if (element.split(':').pop() === 'ResourceDictionary' && attrs.has('Source')) {
      if (!merged.has(file)) merged.set(file, []);
      merged.get(file).push(resolveSource(file, attrs.get('Source')));
    }

    const keyAttr = attrs.get('x:Key');
    if (keyAttr !== undefined && !keyAttr.startsWith('{'))
      definitions.push({
        file, line: lineAt(tag.index), key: keyAttr, element, ns,
        // Whether this declaration is reachable from anywhere in its own file, or
        // only from part of it. The scope check below reasons per file, which is
        // sound exactly while this holds.
        whole: path.slice(1).every((e) => SCOPE_WRAPPER.test(e)),
        under: path.join(' > '),
      });

    for (const a of blob.matchAll(ATTR)) {
      const name = a[1];
      const raw = a[2] ?? a[3];
      const valueStart = blobStart + a.index + a[0].length - raw.length - 1;
      // Setter's own Property attribute names the target; it is not a value.
      if (element.split(':').pop() === 'Setter' && name === 'Property') continue;

      const value = raw.replace(TYPE_KEYED, (m) => {
        typeKeyedSites++;
        return blank(m);
      });

      for (const ref of value.matchAll(RESOURCE)) {
        const line = lineAt(valueStart + ref.index);
        const nested = enclosingExtension(value, ref.index);
        let slot;
        let describeSlot;
        if (nested) {
          slot = { kind: 'extension', name: `${nested.extension}.${nested.property}` };
          describeSlot = `${nested.extension}.${nested.property} (inside the ${name} attribute)`;
        } else if (element.split(':').pop() === 'Setter') {
          const target = attrs.get('Property');
          slot = { kind: 'property', name: target };
          describeSlot = target === undefined
            ? '<Setter> with no Property attribute'
            : `${target} (set by <Setter>)`;
        } else {
          slot = { kind: 'property', name };
          describeSlot = `${element}.${name}`;
        }
        references.push({ file, line, key: ref[2], slot, describeSlot });
      }
    }
  }
}

// A walk that finds nothing has stopped reading the files rather than found them
// clean, and the two look identical from the exit code.
if (!xamlFiles.length || !references.length || !definitions.length) {
  console.error('FAILED: the XAML walk produced nothing to check.');
  console.error(`  ${xamlFiles.length} XAML file(s), ${definitions.length} definition(s), ` +
    `${references.length} reference(s).`);
  console.error('Nothing in this repo is in that state, so the walk is reading something');
  console.error('other than the source, or the source has moved out from under it.');
  process.exit(1);
}

// --- scope -------------------------------------------------------------------
// A resource is visible to a consumer only if the lookup can walk up to the
// dictionary holding it. Everything App.xaml merges is application scope and is
// reachable from anywhere; everything else is reachable only inside the file that
// declares it. Pooling every declaration into one namespace, which is what a
// name-only check does, answers "does this key exist somewhere" when the question
// is "can this consumer reach it".
//
// The merge chain is followed from App.xaml rather than listed here, so a
// dictionary added to it becomes application scope without this file being told.
const declsByFile = new Map(); // file -> Map(key -> definition)
for (const d of definitions) {
  if (!declsByFile.has(d.file)) declsByFile.set(d.file, new Map());
  if (!declsByFile.get(d.file).has(d.key)) declsByFile.get(d.file).set(d.key, d);
}

const knownXaml = new Set(xamlFiles);
const unresolvedMerges = [];
for (const [file, sources] of merged)
  for (const s of sources) if (!knownXaml.has(s)) unresolvedMerges.push([file, s]);
if (unresolvedMerges.length) {
  console.error(`FAILED: ${unresolvedMerges.length} merged dictionary source(s) that do not name a file here:`);
  for (const [file, s] of unresolvedMerges) console.error(`  ${file} merges ${s}`);
  console.error('Which keys are application scope is worked out by following these, so an');
  console.error('unresolved one leaves that answer incomplete. If the dictionary comes from');
  console.error('outside this repo, this guard needs teaching about it before it can run.');
  process.exit(1);
}

const closure = (file, seen = new Set()) => {
  if (seen.has(file)) return seen;
  seen.add(file);
  for (const s of merged.get(file) ?? []) closure(s, seen);
  return seen;
};

if (!knownXaml.has(APP_XAML)) {
  console.error(`FAILED: ${APP_XAML} is not among the XAML files read.`);
  console.error('It is where application scope starts, so without it every key would read');
  console.error('as local to its own file and the scope check would pass anything.');
  process.exit(1);
}
const appScopeFiles = closure(APP_XAML);
const appKeys = new Set();
for (const f of appScopeFiles)
  for (const k of declsByFile.get(f)?.keys() ?? []) appKeys.add(k);
if (!appKeys.size) {
  console.error(`FAILED: following the merged dictionaries from ${APP_XAML} found no keys.`);
  console.error('Every key would then read as local to its own file, and a check that cannot');
  console.error('tell the two apart passes everything.');
  process.exit(1);
}

// A file-scope model is only equivalent to the real one while no declaration sits
// below the root element's own Resources. One inside a Grid or a ControlTemplate
// is narrower than its file, and a check reasoning per file would pass a
// reference that cannot reach it.
const narrower = definitions.filter((d) => !d.whole);
if (narrower.length) {
  console.error(`FAILED: ${narrower.length} resource(s) declared below the root of their file:`);
  for (const d of narrower) console.error(`  ${d.key}  at ${d.file}:${d.line}  under ${d.under}`);
  console.error('This guard reasons about visibility one file at a time, which stops being');
  console.error('the same question once a declaration is scoped to part of a file. Move it to');
  console.error('the root Resources, or teach this guard to reason about the element tree.');
  process.exit(1);
}

const visibleIn = (file) => {
  const v = new Set(appKeys);
  for (const f of closure(file))
    for (const k of declsByFile.get(f)?.keys() ?? []) v.add(k);
  return v;
};
const visibility = new Map(xamlFiles.map((f) => [f, visibleIn(f)]));

// Where a reference resolves FROM decides which declaration it gets, so the type
// half asks the same question rather than taking whichever declaration was read
// first.
const declarationsFor = (file, key) => {
  const local = [];
  for (const f of closure(file))
    if (!appScopeFiles.has(f)) {
      const d = declsByFile.get(f)?.get(key);
      if (d) local.push(d);
    }
  if (local.length) return local;
  const app = [];
  for (const f of appScopeFiles) {
    const d = declsByFile.get(f)?.get(key);
    if (d) app.push(d);
  }
  return app;
};

// --- definitions -------------------------------------------------------------
const defs = new Map(); // key -> [file, ...]
for (const d of definitions) {
  if (!defs.has(d.key)) defs.set(d.key, []);
  defs.get(d.key).push(d.file);
}
// A key a site could reach at two different element types has no single answer to
// "what type is it". Scope decides which declaration a site gets, so this is only
// reached when one scope holds two of them, and the type half stops rather than
// picking one.
const ambiguous = new Map(); // key -> Set of element spellings

// --- references --------------------------------------------------------------
const refs = new Map(); // key -> [site, ...]
const addRef = (key, site) => {
  if (!refs.has(key)) refs.set(key, []);
  refs.get(key).push(site);
};
for (const r of references) addRef(r.key, `${r.file}:${r.line}`);

const csReferences = []; // { file, site, key }
for (const [file, text] of cs) {
  const lines = text.split('\n');
  lines.forEach((line, i) => {
    const site = `${file}:${i + 1}`;
    const here = (key) => {
      addRef(key, site);
      csReferences.push({ file, site, key });
    };
    for (const [, key] of line.matchAll(/(?:Try)?FindResource\(\s*"([^"]+)"/g)) here(key);
    for (const [, key] of line.matchAll(/Resources\[\s*"([^"]+)"\s*\]/g)) here(key);
    for (const [, key] of line.matchAll(/SetResourceReference\([^,]+,\s*"([^"]+)"/g)) here(key);
  });
}

// The name-built lookup: App's TypeSizeTokenKeys feeds Resources[key]. Parsed
// here because no lookup site names these keys. A shape change must fail rather
// than silently drop the check.
const appSource = stripCsComments(readFileSync(APP_XAML_CS, 'utf8'));
const typeArray = appSource.match(/TypeSizeTokenKeys\s*=\s*(?:new\s+string\[\]\s*)?\{([^}]*)\}/);
if (!typeArray) {
  console.error(`FAILED: could not read the TypeSizeTokenKeys array in ${APP_XAML_CS}.`);
  console.error('Its literals are resource keys reached through Resources[key], which no');
  console.error('static parse can follow. If the array moved or changed shape, update this');
  console.error('guard to match; if the text-scaling mechanism is gone, delete this block.');
  process.exit(1);
}
const typeKeys = [...typeArray[1].matchAll(/"([^"]+)"/g)].map((m) => m[1]);
if (!typeKeys.length) {
  console.error(`FAILED: TypeSizeTokenKeys in ${APP_XAML_CS} parsed as empty.`);
  process.exit(1);
}
for (const key of typeKeys) addRef(key, `${APP_XAML_CS} (TypeSizeTokenKeys)`);

// --- reachability -------------------------------------------------------------
// A reference to a key that exists but is declared in another file's own
// Resources. It resolves for a reader, and for a name-only check, and throws when
// the template is realised.
const outOfScope = [];
for (const r of references) {
  if (visibility.get(r.file)?.has(r.key)) continue;
  if (!defs.has(r.key)) continue; // declared nowhere; the dangling arm has it
  outOfScope.push([r.key, `${r.file}:${r.line}`, defs.get(r.key)]);
}

// A lookup in code runs from an element, and which element is not something this
// parse can know. What it can hold is the narrower rule that a file-local key is
// only reachable from the code-behind of the file declaring it, which is where
// every lookup here already sits.
for (const r of csReferences) {
  if (appKeys.has(r.key)) continue;
  if (!defs.has(r.key)) continue; // declared nowhere; the dangling arm has it
  const owners = defs.get(r.key);
  if (owners.some((owner) => r.file === `${owner}.cs`)) continue;
  outOfScope.push([r.key, r.site, owners]);
}

// --- the type match ----------------------------------------------------------
// A resource declared in a namespace of this assembly is a type in this repo, so
// what it claims to satisfy above can be read at its own declaration rather than
// taken on trust. A framework type cannot be, and is not.
const LOCAL_NS = /^clr-namespace:InstallerClean(?:[.;]|$)/;
const csText = cs.map(([, t]) => t).join('\n');

const untypedResources = new Map(); // key -> resource element this file cannot name
const untypedSlots = []; // consuming slot this file cannot type
const mismatches = []; // resource type cannot satisfy the slot
const unverifiedLocal = []; // local type whose declaration does not bear it out
let typeChecked = 0; // sites actually compared, which is what the summary reports

const tableBoundary = []; // a claim whose keeping is not accounted for
const claimed = new Map(); // expanded -> claims beyond the row's own type
for (const [expanded, satisfies] of RESOURCE_TYPES) {
  const uri = expanded.slice(1, expanded.indexOf('}'));
  const local = expanded.slice(expanded.indexOf('}') + 1);
  // A resource element IS its type, so a row that does not satisfy its own name
  // has been mistyped and every site consuming it is compared against the wrong
  // thing.
  if (!satisfies.includes(local)) {
    tableBoundary.push([expanded, `it does not list ${local}, which is the type the element is`]);
    continue;
  }
  const extra = satisfies.filter((s) => s !== local);
  claimed.set(expanded, extra);
  if (!extra.length) continue; // identity row: nothing is claimed, nothing to keep

  if (LOCAL_NS.test(uri)) {
    const decl = new RegExp(`\\b(?:class|struct|record)\\s+${local}\\b([^{]*)`).exec(csText);
    if (!decl) {
      unverifiedLocal.push([expanded, 'no declaration of this type is in the C# source']);
      continue;
    }
    for (const claim of extra)
      if (!new RegExp(`\\b${claim}\\b`).test(decl[1]))
        unverifiedLocal.push([expanded, `its declaration does not name ${claim}`]);
    continue;
  }

  const recorded = FRAMEWORK_INHERITANCE.get(expanded) ?? [];
  for (const claim of extra)
    if (!recorded.includes(claim))
      tableBoundary.push([expanded, `it satisfies ${claim} on the framework's word, and FRAMEWORK_INHERITANCE does not say so`]);
}
// The other direction, so the list cannot keep entries for claims that have gone.
for (const [expanded, claims] of FRAMEWORK_INHERITANCE) {
  const extra = claimed.get(expanded);
  if (extra === undefined) {
    tableBoundary.push([expanded, 'it is recorded here and RESOURCE_TYPES has no such row']);
    continue;
  }
  for (const claim of claims)
    if (!extra.includes(claim))
      tableBoundary.push([expanded, `it is recorded here as satisfying ${claim} and RESOURCE_TYPES does not claim that`]);
}

for (const r of references) {
  const candidates = declarationsFor(r.file, r.key);
  if (!candidates.length) continue; // a dangling reference; the name half reports it
  if (new Set(candidates.map((d) => d.element)).size > 1) {
    if (!ambiguous.has(r.key))
      ambiguous.set(r.key, new Set(candidates.map((d) => d.element)));
    continue;
  }
  const declared = candidates[0];

  const resolved = resolve(declared.ns, declared.element);
  const satisfies = resolved.expanded ? RESOURCE_TYPES.get(resolved.expanded) : undefined;
  if (!satisfies) {
    // The fault is the declaration, so it is reported once however many sites
    // consume it. The site count is what says how much went unchecked.
    const seen = untypedResources.get(r.key);
    if (seen) seen.sites++;
    else
      untypedResources.set(r.key, {
        element: declared.element,
        uri: resolved.uri ?? 'unbound prefix',
        at: `${declared.file}:${declared.line}`,
        sites: 1,
      });
    continue;
  }

  const wanted = r.slot.kind === 'extension'
    ? EXTENSION_SLOTS.get(r.slot.name)
    : PROPERTY_TYPES.get(r.slot.name);
  if (!wanted) {
    untypedSlots.push([r.describeSlot, `${r.file}:${r.line}`, r.key]);
    continue;
  }

  typeChecked++;
  if (!satisfies.includes(wanted))
    mismatches.push([r.key, declared.element, wanted, r.describeSlot, `${r.file}:${r.line}`]);
}

// --- report ------------------------------------------------------------------
const dangling = [...refs.keys()].filter((k) => !defs.has(k) && !EXTERNAL_ALLOWLIST.has(k));
const unconsumed = [...defs.keys()].filter((k) => !refs.has(k) && !UNCONSUMED_ALLOWLIST.has(k));

// A key defined twice among the theme dictionaries. App.xaml merges
// Components.xaml, which merges Tokens.xaml, which merges Primitives.xaml, so
// all three land in one application-scope namespace and a name defined in two of
// them resolves to whichever merge ran last, silently and correctly-looking. It
// is the failure mode of a rename that lands on a name already in use: the
// consumer keeps painting, in the wrong colour. Window-local keys are excluded
// because each window is its own scope (the three BoolToVis converters are the
// same name three times over, and never collide).
const themeDefs = (files) => files.filter((f) => f.includes('/Themes/'));
const collisions = [...defs.entries()].filter(([, files]) => themeDefs(files).length > 1);

const distinctSlots = new Set(references.map((r) => r.slot.name)).size;
console.log(
  `XAML resource guard: ${defs.size} key(s) defined, ${refs.size} key(s) referenced ` +
    `across ${xamlFiles.length} XAML and ${csFiles.length} C# file(s).`
);
// The first number counts sites this run actually compared, not sites walked.
// A check that stopped checking would go on finding the same markup, so a
// figure taken from the walk would read the same either way.
console.log(
  `Type match: ${typeChecked} of ${references.length} markup site(s) checked across ` +
    `${distinctSlots} property slot(s), plus ${typeKeyedSites} implicit-style ` +
    `reference(s) set aside.`
);

if (dangling.length) {
  console.error(`\nFAILED: ${dangling.length} reference(s) to a key nothing defines:`);
  for (const k of dangling.sort())
    for (const site of refs.get(k)) console.error(`  ${k}  <-  ${site}`);
  console.error('\nEach one throws a XamlParseException when its template is first realised.');
  console.error('Either the key was renamed and this consumer was missed, or the resource');
  console.error('comes from outside the repo (add it to EXTERNAL_ALLOWLIST with a reason).');
}

if (unconsumed.length) {
  console.error(`\nFAILED: ${unconsumed.length} defined key(s) with no consumer:`);
  for (const k of unconsumed.sort()) console.error(`  ${k}  (defined in ${defs.get(k).join(', ')})`);
  console.error('\nEither the key is dead (remove it), or it is consumed in a way this guard');
  console.error('cannot see (add it to UNCONSUMED_ALLOWLIST with a one-line reason). Do not');
  console.error('silence it without deciding which.');
}

if (collisions.length) {
  console.error(`\nFAILED: ${collisions.length} key(s) defined more than once in the theme:`);
  for (const [k, files] of collisions.sort()) console.error(`  ${k}  (${files.join(', ')})`);
  console.error('\nThe theme dictionaries merge into one namespace, so the later definition');
  console.error('silently wins and its consumers paint the wrong value. Rename one of them.');
}

if (mismatches.length) {
  console.error(`\nFAILED: ${mismatches.length} site(s) consuming a resource of the wrong type:`);
  for (const [key, element, wanted, slot, site] of mismatches)
    console.error(`  ${site}\n      ${key} is ${article(element)} <${element}>, and ${slot} takes ${article(wanted)} ${wanted}.`);
  console.error('\nResource lookup runs no TypeConverter, so each of these resolves by name');
  console.error('and fails when the template is realised. Either point the site at a resource');
  console.error('of the right type, or define a parallel resource of that type beside this one.');
}

if (untypedResources.size) {
  console.error(`\nFAILED: ${untypedResources.size} resource(s) of a type this guard cannot name:`);
  for (const [key, r] of untypedResources)
    console.error(`  ${key}  at ${r.at}  is ${article(r.element)} <${r.element}> in ${r.uri}` +
      `, and ${r.sites} site(s) consume it`);
  console.error('\nAdd it to RESOURCE_TYPES with the declared types it satisfies. Leaving it');
  console.error('out is not neutral: every site consuming it would go unchecked.');
}

if (ambiguous.size) {
  console.error(`\nFAILED: ${ambiguous.size} key(s) declared at more than one type:`);
  for (const [key, elements] of ambiguous)
    console.error(`  ${key}  is declared as ${[...elements].map((e) => `<${e}>`).join(' and ')}` +
      `  (in ${defs.get(key).join(', ')})`);
  console.error('\nWhich one a site gets depends on where the site sits, so no site consuming');
  console.error('this key can be checked against a type. Give the two different names, or');
  console.error('declare them at the same type.');
}

if (outOfScope.length) {
  console.error(`\nFAILED: ${outOfScope.length} reference(s) to a key the consumer cannot reach:`);
  for (const [key, site, owners] of outOfScope)
    console.error(`  ${key}  <-  ${site}    (declared in ${owners.join(', ')})`);
  console.error('\nA resource declared in one file\'s own Resources is invisible to another,');
  console.error('so lookup runs off the end of the tree and throws when the template is');
  console.error('realised. Either declare it where the consumer can reach it, or move it into');
  console.error('the theme, which every file can see.');
}

if (untypedSlots.length) {
  console.error(`\nFAILED: ${untypedSlots.length} site(s) whose property this guard cannot type:`);
  for (const [slot, site, key] of untypedSlots)
    console.error(`  ${slot}  <-  ${key}  at ${site}`);
  console.error('\nAdd the property to PROPERTY_TYPES, or the markup-extension slot to');
  console.error('EXTENSION_SLOTS, with the type it declares.');
}

if (tableBoundary.length) {
  console.error(`\nFAILED: ${tableBoundary.length} row(s) in the type tables that do not account for themselves:`);
  for (const [expanded, why] of tableBoundary) console.error(`  ${expanded}: ${why}`);
  console.error('\nEvery claim a resource type makes beyond its own name is kept in one of two');
  console.error('ways: read off its declaration if the type is declared here, or recorded in');
  console.error('FRAMEWORK_INHERITANCE if it comes from the framework. A claim in neither is');
  console.error('one nothing is holding to anything.');
}

if (unverifiedLocal.length) {
  console.error(`\nFAILED: ${unverifiedLocal.length} claim(s) about a type in this repo that its source does not bear out:`);
  for (const [expanded, why] of unverifiedLocal) console.error(`  ${expanded}: ${why}`);
  console.error('\nRESOURCE_TYPES says what this type satisfies and the C# says otherwise.');
  console.error('Whichever moved, the sites consuming it are being checked against the wrong');
  console.error('type until the two agree.');
}

if (dangling.length || unconsumed.length || collisions.length || mismatches.length ||
    untypedResources.size || untypedSlots.length || unverifiedLocal.length ||
    ambiguous.size || outOfScope.length || tableBoundary.length)
  process.exit(1);
console.log('OK: every reference resolves and is of a type its consumer can take, every key');
console.log('has a consumer, and no key is defined twice.');
