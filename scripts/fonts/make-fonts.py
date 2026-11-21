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
  applies ccmp, locl, rlig, dist, mark, mkmk and rvrn itself where a font has them. The OpenType
  feature registry has a shaping engine apply rclt, curs, ltra, ltrm, abvm and blwm by default as
  well; neither font uses those six, and they are kept so that a later release of either font
  that does keeps them. The other features (small capitals, old-style or tabular figures,
  fractions, superiors, stylistic sets) are applied only when asked for, so a glyph reached only
  through one of them is never drawn by the app, and goes. Do not narrow FEATURES: a glyph a kept
  feature reaches would be drawn as its unsubstituted form.
- The hinting, the names and the vertical metrics a line is set from (the OS/2 typo and win
  values, hhea's ascender, descender and line gap). The bounding box in head and the extents in
  hhea are recalculated for the glyphs that remain. Glyph names are not kept: nothing reads them.

The licence is the SIL Open Font License 1.1, under which a subset is a modified version. Neither
font has a Reserved Font Name, so the files keep their names.

Output is byte for byte the same on every run with the same fontTools and uharfbuzz:
head.modified is kept as published, and nothing else in the file depends on the time or the
machine. fontTools packs the layout tables with HarfBuzz's repacker when uharfbuzz is installed,
and another version of either can write other bytes, so the script refuses any but FONTTOOLS and
UHARFBUZZ, and checks each file it makes against its sha256 in OUTPUTS, the files in the tree. It
checks all eight inputs before it makes anything, and writes nothing unless all eight outputs are
made and each is as in OUTPUTS; --write-new writes outputs that differ, for a change meant to
change them, whose new sha256 values then go into OUTPUTS.

scripts/check-font-coverage.mjs asserts in CI that every language's characters are in every face
of its family, the 1.5 and 1.1, USE_TYPO_METRICS and the absence of Greek. That the files in the
tree are this script's output is checked by running the script.

Needs fontTools and uharfbuzz (pip install fonttools==4.63.0 uharfbuzz==0.57.0). Run from the
repository root, with the eight upstream files in one folder:

    python3 scripts/fonts/make-fonts.py <folder>
"""
import hashlib
import io
import pathlib
import sys

import fontTools
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
FEATURES = ['liga', 'clig', 'calt', 'kern', 'ccmp', 'locl', 'rlig', 'dist', 'mark', 'mkmk', 'rvrn',
            'rclt', 'curs', 'ltra', 'ltrm', 'abvm', 'blwm']
FONTTOOLS = '4.63.0'
UHARFBUZZ = '0.57.0'
# The sha256 of each file this script makes, which are the files in the tree.
OUTPUTS = {
    'Poppins-Regular.ttf': '53e9d1828e1181697fdc1f617ba809de5515254dc47daec6adf604d2f47c884a',
    'Poppins-Medium.ttf': 'ffbe5a937a97e4d2b593fb3aa272a7a7441fa34b262e311b44ce00fdae16e8f4',
    'Poppins-SemiBold.ttf': '284ca449b89a7f20c45a18a57034e5f04e18922b57b3986e89c1446926f42b21',
    'Poppins-Bold.ttf': 'beacc9eb037abb437e357deeecbc8d6854a755ce6f79d91dabf177fdbc85d868',
    'Montserrat-Regular.ttf': '488c0a9a536842390a8a5d1fb61c2b1ac04c07e393ebfcd77cc66ce22570f5ae',
    'Montserrat-Medium.ttf': 'd257db3e8c443a1c8aebb3c6f235f147f25a9005652fc7884a3025436eb12e31',
    'Montserrat-SemiBold.ttf': '4fa1c99df514730c84a788ed584b2b3471db2ba9aa14d4eb5d3373d0f79e3cd7',
    'Montserrat-Bold.ttf': 'b34e448808a9663ebbfcdf93446e8332d25ee15dec8fcddb010d0737bc4a5956',
}


def load(source, name, expected):
    path = source / name
    if not path.is_file():
        sys.exit(f'{path} not found: the folder needs all eight upstream files')
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
    args = [a for a in sys.argv[1:] if a != '--write-new']
    write_new = '--write-new' in sys.argv[1:]
    if len(args) != 1:
        sys.exit(__doc__.rstrip().rsplit('\n', 1)[-1].strip())
    source = pathlib.Path(args[0])
    if not OUT.is_dir():
        sys.exit(f'{OUT} not found: run from the repository root')
    if fontTools.version != FONTTOOLS:
        sys.exit(f'fontTools {fontTools.version} is installed; the files are made with {FONTTOOLS} '
                 f'(pip install fonttools=={FONTTOOLS})')
    try:
        import uharfbuzz
    except ImportError:
        sys.exit(f'uharfbuzz is not installed; the files are made with {UHARFBUZZ} '
                 f'(pip install uharfbuzz=={UHARFBUZZ})')
    if uharfbuzz.__version__ != UHARFBUZZ:
        sys.exit(f'uharfbuzz {uharfbuzz.__version__} is installed; the files are made with '
                 f'{UHARFBUZZ} (pip install uharfbuzz=={UHARFBUZZ})')

    fonts = {}
    for family, files in (('Poppins', POPPINS), ('Montserrat', MONTSERRAT)):
        for name, expected in files.items():
            fonts[name] = (family, load(source, name, expected))

    made = {}
    for name, (family, font) in fonts.items():
        if family == 'Montserrat':
            adjust_montserrat(font, name)
        keep = sorted(c for c in font.getBestCmap() if c not in DEVANAGARI)
        subset_font(font, keep)
        if set(font.getBestCmap()) != set(keep):
            sys.exit(f'{name}: the subset maps other characters than the ones asked for')
        buffer = io.BytesIO()
        font.save(buffer)
        made[name] = buffer.getvalue()
        digest = hashlib.sha256(made[name]).hexdigest()
        same = 'as in OUTPUTS' if digest == OUTPUTS[name] else 'NOT as in OUTPUTS'
        print(f'{name}: {len(keep)} characters, {font["maxp"].numGlyphs} glyphs, '
              f'{len(made[name])} bytes, sha256 {digest} ({same})')

    changed = [n for n, data in made.items() if hashlib.sha256(data).hexdigest() != OUTPUTS[n]]
    if changed and not write_new:
        sys.exit('Nothing written: ' + ', '.join(changed) + ' would differ from OUTPUTS. If the '
                 'change is meant, run again with --write-new and put the new sha256 values in '
                 'OUTPUTS.')
    for name, data in made.items():
        (OUT / name).write_bytes(data)
    print(f'Wrote the eight files to {OUT}.')


if __name__ == '__main__':
    main()
