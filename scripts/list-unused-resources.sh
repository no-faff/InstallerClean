#!/usr/bin/env bash
#
# Lists localisation resx keys and XAML x:Key resources that no source file
# consumes. Read-only: it reports candidates, it never edits or deletes. A key
# can be retired safely only once a human has confirmed nothing builds it at
# runtime, so this is a pointer, not an authority.
#
# A resx key is consumed in one of two forms, so both are searched:
#   - C#:   the underscored accessor Strings.Key_Name (dots become underscores
#           in scripts/regenerate-strings-designer.sh, so Foo.Bar -> Foo_Bar).
#   - XAML:  the dotted key Foo.Bar, via {loc:Translate Foo.Bar}.
# A XAML x:Key resource is consumed via {StaticResource Key} / {DynamicResource
# Key} in XAML or FindResource("Key") / Resources["Key"] in C#.
#
# Excluded from the consumer corpus:
#   - Strings.Designer.cs: the generated accessor defines a property and a
#     Get("Foo.Bar") call for every key, so including it would make every key
#     look consumed.
#   - bin/ and obj/: build output mirrors source, so scanning it adds nothing
#     and makes the result depend on build state.
#
# Boundary handling differs by form on purpose. The underscored C# accessor may
# be followed by a dot (Strings.Foo_Bar.Trim()), so it is matched with a
# word boundary that treats a dot as a terminator. The dotted forms (resx
# {loc:Translate Foo.Bar} and XAML keys like Type.Body) must NOT match inside a
# longer dotted key (Type.Body.Strong), so they are matched with a stricter
# boundary that rejects a trailing or leading dot.
#
# Markup-extension keys such as x:Key="{x:Type Button}" are skipped: they are
# matched by type at runtime, not by a name a static search could verify.

set -euo pipefail

ROOT=$(cd "$(dirname "$0")/.." && pwd)
SRC=$ROOT/src
RESX=$SRC/InstallerClean.Core/Resources/Strings.resx

[[ -f $RESX ]] || { echo "Missing $RESX" >&2; exit 1; }

mapfile -t ALL_CS  < <(find "$SRC" -type f -name '*.cs'   -not -path '*/obj/*' -not -path '*/bin/*')
mapfile -t XAML    < <(find "$SRC" -type f -name '*.xaml' -not -path '*/obj/*' -not -path '*/bin/*')
mapfile -t CS_NOGEN < <(printf '%s\n' "${ALL_CS[@]}" | grep -v '/Strings\.Designer\.cs$' || true)

# Built once and searched in memory rather than re-reading files per key.
RESX_CORPUS=$(cat "${CS_NOGEN[@]}" "${XAML[@]}")
# XAML key definitions are stripped so a key's own x:Key="..." line is not
# mistaken for a reference; a real reference is never written as x:Key="...".
XAML_CORPUS=$( { sed -E 's/x:Key="[^"]*"//g' "${XAML[@]}"; cat "${ALL_CS[@]}"; } )

# Regex-escape the only ERE metacharacter that appears in a key: the dot.
esc() { printf '%s' "$1" | sed 's/[.]/\\./g'; }
# Strict boundary: the token is bounded by start/end or a char that is neither
# an identifier char nor a dot, so Type.Body does not match Type.Body.Strong.
bounded_strict() { grep -qE "(^|[^A-Za-z0-9_.])$(esc "$1")([^A-Za-z0-9_.]|$)"; }

# resx keys
mapfile -t RESX_KEYS < <(grep -oE '<data name="[A-Za-z][A-Za-z0-9._]+' "$RESX" \
                           | sed 's|<data name="||' | sort -u)
resx_unused=()
for k in "${RESX_KEYS[@]}"; do
    under=${k//./_}
    # Underscored C# accessor (word boundary permits a trailing dot for .Trim()
    # etc.), or the dotted XAML form (strict boundary).
    if grep -wqF -- "$under" <<<"$RESX_CORPUS" || bounded_strict "$k" <<<"$RESX_CORPUS"; then
        continue
    fi
    resx_unused+=("$k")
done

# XAML x:Key resources
mapfile -t XAML_KEYS < <(grep -hoE 'x:Key="[^"]+"' "${XAML[@]}" \
                           | sed -E 's/x:Key="//; s/"$//' | sort -u)
xaml_unused=()
for k in "${XAML_KEYS[@]}"; do
    [[ $k == \{* ]] && continue   # markup-extension key, matched by type not name
    if bounded_strict "$k" <<<"$XAML_CORPUS"; then
        continue
    fi
    xaml_unused+=("$k")
done

# report
echo "Unused-resource report (read-only; nothing is modified)."
echo
echo "Resx keys in Strings.resx with no consumer (${#resx_unused[@]}):"
if ((${#resx_unused[@]})); then printf '  %s\n' "${resx_unused[@]}"; else echo "  (none)"; fi
echo
echo "XAML x:Key resources with no consumer (${#xaml_unused[@]}):"
if ((${#xaml_unused[@]})); then printf '  %s\n' "${xaml_unused[@]}"; else echo "  (none)"; fi
echo
cat <<'NOTE'
Notes
  - Consumers scanned: src/**/*.cs (minus the generated Strings.Designer.cs)
    and src/**/*.xaml, excluding bin/ and obj/.
  - A key built at runtime by string concatenation cannot be detected here;
    confirm a flagged key is genuinely unused before removing it.
  - Markup-extension keys such as x:Key="{x:Type Button}" are skipped: they are
    matched by type, not by name.
NOTE
