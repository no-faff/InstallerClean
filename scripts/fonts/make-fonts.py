#!/usr/bin/env python3
"""Makes the eight font files in src/InstallerClean/Fonts from the fonts' own releases.

The window draws in Poppins, and draws Russian, Ukrainian and Vietnamese in Montserrat, which also
comes second after Poppins for the other Latin-script languages. WPF cannot select a weight from a
variable font, so each of the four weights the app uses (Regular, Medium, SemiBold, Bold) is a
static file.

Poppins comes from github.com/google/fonts at commit 7dc16b7de42db624b902f2292b68ed9e489e5053,
ofl/poppins/, version 4.004. Montserrat comes from github.com/JulietaUla/Montserrat at commit
555facfb2a18c72c3c0380f0d9c0f060453a9058, fonts/ttf/, version 9.000, static and hinted. The sha256
of every file is checked before anything is read.

Montserrat gets two changes before it is subset:

- The vertical metrics become Poppins's: ascender 1050, descender -350 and line gap 100 on a
  1000-unit em, in the OS/2 typo fields and in hhea. WPF sets a line at (ascender + descender +
  line gap) / em and the baseline at (ascender + line gap / 2) / em, reading the typo values
  because USE_TYPO_METRICS is set, which it is in all four files and stays set. That gives 1.5
  and 1.1, Poppins's own, so a line in Montserrat is as tall as one in Poppins and its text sits
  at the same depth. As published, Montserrat sets 1.219 and 0.968. The win values are left as
  published.
- The Greek block, U+0370 to U+03FF, leaves every character map. Montserrat draws eight Greek
  letters and Poppins one, so with Montserrat named after Poppins a Greek word would take letters
  from three fonts. Without them Greek falls to Segoe UI whole, apart from Poppins's pi. The OS/2
  bit declaring the Greek block is cleared with them.

Then both families are subset, keeping:

- Every character in the character map, except Poppins's Devanagari block, U+0900 to U+097F. No
  language of the app is written in Devanagari, and a Devanagari product name or path falls to
  the Devanagari face Windows ships. Every Latin, Cyrillic and symbol block stays, because the
  window draws product names and paths in these fonts as well as its own text.
- The glyphs those characters reach through the OpenType features in FEATURES, and no others.
  WPF asks DirectWrite for liga, clig, calt and kern on every run of text, the defaults of
  System.Windows.Documents.Typography, and the app sets no other Typography property. DirectWrite
  applies ccmp, locl, rlig, dist, mark, mkmk and rvrn itself where a font has them. The other
  features (small capitals, old-style or tabular figures, fractions, superiors, stylistic sets)
  are applied only when asked for, so a glyph reached only through one of them is never drawn by
  the app, and goes. Do not narrow FEATURES: a glyph a kept feature reaches would be drawn as its
  unsubstituted form.
- The hinting, the names and every metric. Glyph names are not kept: nothing reads them.

The licence is the SIL Open Font License 1.1, under which a subset is a modified version. Neither
font has a Reserved Font Name, so the files keep their names.

Output is byte for byte the same on every run with the same fontTools: head.modified is kept as
published, and nothing else in the file depends on the time or the machine. The files in the tree
were made with fontTools 4.63.0.

scripts/check-font-coverage.mjs asserts the result in CI: every language's characters in every
face of its family, the 1.5 and 1.1, USE_TYPO_METRICS and the absence of Greek.

Needs fontTools (pip install fonttools==4.63.0). Run from the repository root, with the eight
upstream files in one folder:

    python3 scripts/fonts/make-fonts.py <folder>
"""
import hashlib
import pathlib
import sys

from fontTools import subset
from fontTools.ttLib import TTFont

POPPINS = {
    'Poppins-Regular.ttf': '7e65201e9b79159e2300267cc885e16c8dcef2424cdfa09a29bfb0980a94a7ba',
    'Poppins-Medium.ttf': '90373e7d838d32468438fc3e152dca0bdb12edcab99ea639f158790b1ba1fd05',
    'Poppins-SemiBold.ttf': 'd3bf1bdaf0550e83da9ac0b1d1d9fe6db086835a83aa28578e609a394b9a0286',
    'Poppins-Bold.ttf': '983676516167748b74de6f4771fb384c664fd913acb8b471122ecacf5da5ea6c',
}
MONTSERRAT = {
    'Montserrat-Regular.ttf': '3e8abe50c44c82e2242e97d1ec8c0d385c4890cdc50447bcdb8605c81a38cfb2',
    'Montserrat-Medium.ttf': 'dae47428bb041f9716604e0e07b5b0c8585b3bdd8183362f75c69fe7bb3cfaf4',
    'Montserrat-SemiBold.ttf': 'b4e1563393d73fdff491a869441245aef31add2ec03d9c97a6dae4de07c52fd0',
    'Montserrat-Bold.ttf': 'bc6e854971cea46b463be6f9eef4d9cd52f51cfc1fc0dd90c9d3e6483dc0ec61',
}
OUT = pathlib.Path('src/InstallerClean/Fonts')
ASCENDER, DESCENDER, LINE_GAP = 1050, -350, 100
GREEK = range(0x0370, 0x0400)
DEVANAGARI = range(0x0900, 0x0980)
# OS/2 Unicode range bit 7 is Greek and Coptic, the block the character maps lose.
GREEK_RANGE_BIT = 1 << 7
USE_TYPO_METRICS = 1 << 7
FEATURES = ['liga', 'clig', 'calt', 'kern', 'ccmp', 'locl', 'rlig', 'dist', 'mark', 'mkmk', 'rvrn']


def load(source, name, expected):
    path = source / name
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    if digest != expected:
        sys.exit(f'{path}: sha256 {digest}, expected {expected}')
    # recalcTimestamp=False keeps head.modified as published, which is what makes a re-run
    # give the same bytes.
    font = TTFont(str(path), recalcTimestamp=False)
    if font['head'].unitsPerEm != 1000:
        sys.exit(f'{name}: {font["head"].unitsPerEm} units per em; the metrics here assume 1000')
    return font


def adjust_montserrat(font, name):
    """Gives the file Poppins's line metrics and takes Greek out of its character maps."""
    if 'MVAR' in font:
        sys.exit(f'{name}: has an MVAR table, which could vary the metrics; a static file has none')
    os2, hhea = font['OS/2'], font['hhea']
    if not os2.fsSelection & USE_TYPO_METRICS:
        sys.exit(f'{name}: USE_TYPO_METRICS is not set, so WPF would not read the typo values')
    os2.sTypoAscender, os2.sTypoDescender, os2.sTypoLineGap = ASCENDER, DESCENDER, LINE_GAP
    hhea.ascent, hhea.descent, hhea.lineGap = ASCENDER, DESCENDER, LINE_GAP

    for table in font['cmap'].tables:
        for code in [c for c in table.cmap if c in GREEK]:
            del table.cmap[code]
    os2.ulUnicodeRange1 &= ~GREEK_RANGE_BIT


def subset_font(font, keep):
    options = subset.Options()
    options.layout_features = FEATURES
    options.name_IDs = ['*']
    options.name_languages = ['*']
    options.name_legacy = True
    options.legacy_kern = True
    options.notdef_outline = True
    options.glyph_names = False
    options.hinting = True
    subsetter = subset.Subsetter(options)
    subsetter.populate(unicodes=keep)
    subsetter.subset(font)


def main():
    if len(sys.argv) != 2:
        sys.exit(__doc__.rstrip().rsplit('\n', 1)[-1].strip())
    source = pathlib.Path(sys.argv[1])
    if not OUT.is_dir():
        sys.exit(f'{OUT} not found: run from the repository root')

    for family, files in (('Poppins', POPPINS), ('Montserrat', MONTSERRAT)):
        for name, expected in files.items():
            font = load(source, name, expected)
            if family == 'Montserrat':
                adjust_montserrat(font, name)
            mapped = set(font.getBestCmap())
            keep = sorted(c for c in mapped if c not in DEVANAGARI)
            subset_font(font, keep)
            if set(font.getBestCmap()) != set(keep):
                sys.exit(f'{name}: the subset maps other characters than the ones asked for')

            target = OUT / name
            font.save(str(target))
            digest = hashlib.sha256(target.read_bytes()).hexdigest()
            print(f'{target}: {len(keep)} characters, {font["maxp"].numGlyphs} glyphs, '
                  f'{target.stat().st_size} bytes, sha256 {digest}')


if __name__ == '__main__':
    main()
