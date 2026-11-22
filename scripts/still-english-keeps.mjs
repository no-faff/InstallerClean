// The keys a satellite may hold in English on purpose, shared by check-still-english,
// which fails on any other key whose translation is the English, and
// check-french-spacing, which reads French typography into every French value
// except the ones check-still-english would report or that are machine-read.
// Machine-read CLI lines (the Application-log phrases an RMM tool greps for) are
// forced English at the emit site, so a satellite legitimately keeps them; they
// are not a translation target. Exactly the Cli.EventLog* keys bar
// Cli.EventLogUnavailable, which despite its prefix is an operator-facing warning.
export const isMachineCliKey = (key) =>
  key.startsWith('Cli.') && key.includes('EventLog') && key !== 'Cli.EventLogUnavailable';

// Universal keeps: the product name and the pure-placeholder announcement string,
// byte-identical to English in every language on purpose. Mirrors KEEP_ENGLISH in
// every gen-strings-<code>.mjs (they are identical across all of them; the template
// carries seven more and says why in its own comment).
//
// The five size suffixes and the two elapsed suffixes are not here and are not
// universal: French writes To/Go/Mo/Ko/o and Russian and Ukrainian write
// ТБ/ГБ/МБ/КБ/Б and мс/с. They are a per-language keep below, so a language that
// abbreviates as English does still passes while a language that has its own forms
// and has not taken them FAILS, which is what this check is for.
export const KEEP_ENGLISH = new Set([
  'Window.Main.Title', 'Startup.AlreadyRunningTitle', 'Startup.UnhandledTitle',
  'Automation.ScanResultAnnouncement',
]);

// Per-language keeps: a word a language deliberately renders identically to
// English (a naturalised loanword such as German "Patches" or "Details"). Mirrors
// each generator's ALSO_KEEP. Languages with an empty ALSO_KEEP are omitted.
// Display.ListSeparator is the odd one and is in most of them: it is ", ",
// a punctuation mark rather than a word, and most languages separate a list
// exactly as English does. It could neither be translated nor left failing, so
// it is a keep everywhere except ja, zh-Hans and zh-Hant, which take the
// ideographic comma and have a real value.
//
// Display.SentenceSeparator is the same shape and sits beside it in the same
// languages. It is the stop that joins a dialog heading to its body in the single
// line a screen reader announces, ". " in the languages that end a sentence as
// English does, and the ideographic full stop with no space after it in ja,
// zh-Hans and zh-Hant, which is why those three are absent here and carry a real
// value.
//
// Cli.UpdateDownloadLink is the same shape again: "{0}: {1}", a link's words, a colon
// and its address on the command line. It is a keep where the colon is English's, and
// fr, ja, zh-Hans and zh-Hant carry a real value (a narrow no-break space before it in
// French, the full-width colon in the other three).
//
// The command line's answer letters are here where a language's letter is the English
// one: N for no wherever its word for no starts with N, Y for yes in Indonesian, and both
// in Japanese, Korean and Chinese, whose consoles take Y and N.
//
// The unit suffixes are here in the same shape. Most languages
// abbreviate a size exactly as English does and keep all seven; French keeps only
// the two elapsed ones, "ms" and "s" being the SI symbols it writes unchanged
// while To/Go/Mo/Ko/o are abbreviated French words; Russian and Ukrainian keep none,
// taking ТБ/ГБ/МБ/КБ/Б and мс/с. So ja, zh-Hans and zh-Hant keep only the units,
// and ru and uk keep only the separators.
export const SIZE_UNITS = ['Display.Size.TB', 'Display.Size.GB', 'Display.Size.MB', 'Display.Size.KB', 'Display.Size.B'];
export const ELAPSED_UNITS = ['Display.Elapsed.Ms', 'Display.Elapsed.S'];
export const UNITS = [...SIZE_UNITS, ...ELAPSED_UNITS];

export const ALSO_KEEP = {
  de: ['Section.Registered.Patches', 'Field.Patches', 'Automation.Section.Patches', 'Action.Details', 'Version.Display', 'Display.ListSeparator', 'Display.SentenceSeparator', 'Cli.UpdateDownloadLink', ...UNITS, 'Cli.UpdateAnswerNo'],
  es: ['Plural.Error.Singular', 'Display.ListSeparator', 'Display.SentenceSeparator', 'Cli.UpdateDownloadLink', ...UNITS, 'Cli.UpdateAnswerNo'],
  fr: ['Field.Application', 'Version.Display', 'Display.ListSeparator', 'Display.SentenceSeparator', ...ELAPSED_UNITS, 'Cli.UpdateAnswerNo'],
  id: ['Plural.File.Singular', 'Plural.Patch.Singular', 'Field.File', 'Display.ListSeparator', 'Display.SentenceSeparator', 'Cli.UpdateDownloadLink', 'Display.DriveName', ...UNITS, 'Cli.UpdateAnswerYes'],
  it: ['Field.File', 'Plural.File.Singular', 'Plural.Patch.Singular', 'Display.ListSeparator', 'Display.SentenceSeparator', 'Cli.UpdateDownloadLink', ...UNITS, 'Cli.UpdateAnswerNo'],
  ja: [...UNITS, 'Cli.UpdateAnswerYes', 'Cli.UpdateAnswerNo'],
  ko: ['Display.ListSeparator', 'Display.SentenceSeparator', 'Cli.UpdateDownloadLink', ...UNITS, 'Cli.UpdateAnswerYes', 'Cli.UpdateAnswerNo'],
  nl: ['Section.Registered.Patches', 'Field.Patches', 'Automation.Section.Patches', 'Action.Details', 'Plural.Product.Singular', 'Plural.Patch.Singular', 'Plural.Patch.Plural', 'Display.ListSeparator', 'Display.SentenceSeparator', 'Cli.UpdateDownloadLink', ...UNITS, 'Cli.UpdateAnswerNo'],
  pl: ['Display.ListSeparator', 'Display.SentenceSeparator', 'Cli.UpdateDownloadLink', ...UNITS, 'Cli.UpdateAnswerNo'],
  'pt-BR': ['Plural.Patch.Singular', 'Plural.Patch.Plural', 'Field.Patches', 'Section.Registered.Patches', 'Automation.Section.Patches', 'Display.ListSeparator', 'Display.SentenceSeparator', 'Cli.UpdateDownloadLink', ...UNITS, 'Cli.UpdateAnswerNo'],
  ru: ['Display.ListSeparator', 'Display.SentenceSeparator', 'Cli.UpdateDownloadLink'],
  tr: ['Display.ListSeparator', 'Display.SentenceSeparator', 'Cli.UpdateDownloadLink', ...UNITS],
  uk: ['Display.ListSeparator', 'Display.SentenceSeparator', 'Cli.UpdateDownloadLink'],
  vi: ['Display.ListSeparator', 'Display.SentenceSeparator', 'Cli.UpdateDownloadLink', ...UNITS],
  'zh-Hans': [...UNITS, 'Cli.UpdateAnswerYes', 'Cli.UpdateAnswerNo'],
  'zh-Hant': [...UNITS, 'Cli.UpdateAnswerYes', 'Cli.UpdateAnswerNo'],
};

// A KEEP IS A JUDGEMENT OF ONE ENGLISH WORDING, so each kept key is pinned here to the
// English it was judged against, raw as the resx holds it. A key's English reworded
// fails the run until somebody reads each language keeping it against the new
// wording and moves the pin, since a keep by key name alone would pass the new
// English in every language keeping it, the translations flag-retranslation.mjs sets
// back to English included. A kept key with no pin, and a pin no keep uses, fail too.
export const JUDGED_ENGLISH = {
  'Action.Details': 'Details',
  'Automation.ScanResultAnnouncement': '{0} ({1})',
  'Automation.Section.Patches': 'Patches',
  'Cli.UpdateAnswerNo': 'N',
  'Cli.UpdateAnswerYes': 'Y',
  'Cli.UpdateDownloadLink': '{0}: {1}',
  'Display.DriveName': 'drive {0}',
  'Display.Elapsed.Ms': '{0:F0}ms',
  'Display.Elapsed.S': '{0:F1}s',
  'Display.ListSeparator': ', ',
  'Display.SentenceSeparator': '. ',
  'Display.Size.B': '{0} B',
  'Display.Size.GB': '{0} GB',
  'Display.Size.KB': '{0} KB',
  'Display.Size.MB': '{0} MB',
  'Display.Size.TB': '{0} TB',
  'Field.Application': 'Application',
  'Field.File': 'File',
  'Field.Patches': 'Patches',
  'Plural.Error.Singular': 'error',
  'Plural.File.Singular': 'file',
  'Plural.Patch.Plural': 'patches',
  'Plural.Patch.Singular': 'patch',
  'Plural.Product.Singular': 'product',
  'Section.Registered.Patches': 'PATCHES',
  'Startup.AlreadyRunningTitle': 'InstallerClean',
  'Startup.UnhandledTitle': 'InstallerClean',
  'Version.Display': 'Version {0}',
  'Window.Main.Title': 'InstallerClean',
};

// Whether `lang` keeps `key` identical to the English as its translation: a word or a
// mark the language writes exactly as English does. A machine-read line is not one of
// these, being English wherever it is written (isMachineCliKey).
export const isKeptAsTranslation = (lang, key) =>
  KEEP_ENGLISH.has(key) || (ALSO_KEEP[lang] ?? []).includes(key);
