# Fails (exit 1) where .NET Framework's string.IndexOf or LastIndexOf under
# StringComparison.OrdinalIgnoreCase finds an ASCII value in ASCII text at another index
# than an ordinal search folding a-z does, which is the search .NET 10 makes on ASCII text.
#
# .NET Framework hands ASCII text under OrdinalIgnoreCase to the invariant culture's
# CompareInfo, and the shipped projects call FrameworkStringExtensions.IndexOfOrdinal in
# its place (BannedSymbols.txt). The searches made: every ASCII character in "a?b" against
# "ab", in "x?y" as the value, and in "ab" against "a?b", and every ordered pair of ASCII
# characters in "q??q" against the pair with its first character upper-cased and against
# the pair lower-cased, and in "q?q?" against the pair. Each search that differs is printed, the first 400 of them.
#
# The run checks itself against the same searches with a fault planted in the ordinal
# reference, '[' in the text folded as '{': the control has to find a difference, or the
# step fails, so a run whose comparison sees nothing cannot pass.
#
# Runs on .NET Framework only, so it is started by Windows PowerShell 5.1 (shell:
# powershell), and fails under PowerShell 7, which runs on .NET.
#
# Run from the repo root: powershell -File scripts/check-framework-ascii-search.ps1
$ErrorActionPreference = 'Stop'

if ($PSVersionTable.PSEdition -ne 'Desktop') {
  Write-Host "::error::This runs on PowerShell $($PSVersionTable.PSVersion), edition $($PSVersionTable.PSEdition), which is not .NET Framework. Run it with Windows PowerShell 5.1."
  exit 1
}

Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Text;
public static class FrameworkAsciiSearch
{
    static char Fold(char c, bool planted)
    {
        if (planted && c == '[') return '{';
        return c >= 'a' && c <= 'z' ? (char)(c - 32) : c;
    }
    static bool At(string t, int i, string v, bool planted)
    {
        for (int j = 0; j < v.Length; j++) if (Fold(t[i + j], planted) != Fold(v[j], false)) return false;
        return true;
    }
    static int OrdinalIndexOf(string t, string v, bool planted)
    {
        for (int i = 0; i + v.Length <= t.Length; i++) if (At(t, i, v, planted)) return i;
        return -1;
    }
    static int OrdinalLastIndexOf(string t, string v, bool planted)
    {
        for (int i = t.Length - v.Length; i >= 0; i--) if (At(t, i, v, planted)) return i;
        return -1;
    }
    static string Show(string s)
    {
        var b = new StringBuilder();
        foreach (char c in s) b.Append(c >= 0x20 && c < 0x7F ? c.ToString() : string.Format("<{0:X2}>", (int)c));
        return b.ToString();
    }
    // The searches made, then each one that differs, the first 400 of them.
    public static List<string> Run(bool planted, out int searches, out int differ)
    {
        var lines = new List<string>();
        int made = 0, found = 0;
        Action<string, string> check = (t, v) =>
        {
            made++;
            int got = t.IndexOf(v, StringComparison.OrdinalIgnoreCase), want = OrdinalIndexOf(t, v, planted);
            int gotLast = t.LastIndexOf(v, StringComparison.OrdinalIgnoreCase), wantLast = OrdinalLastIndexOf(t, v, planted);
            if (got != want || gotLast != wantLast)
            {
                found++;
                if (lines.Count < 400)
                    lines.Add(string.Format("text \"{0}\" value \"{1}\": IndexOf {2} (ordinal {3}), LastIndexOf {4} (ordinal {5})",
                        Show(t), Show(v), got, want, gotLast, wantLast));
            }
        };
        for (int a = 0; a < 128; a++)
        {
            char ca = (char)a;
            check("a" + ca + "b", "ab");
            check("x" + ca + "y", ca.ToString());
            check("ab", "a" + ca + "b");
            for (int b = 0; b < 128; b++)
            {
                char cb = (char)b;
                check("q" + ca + cb + "q", new string(new[] { Fold(ca, false), cb }));
                check("q" + ca + cb + "q", new string(new[] { ca, cb }).ToLowerInvariant());
                check("q" + ca + "q" + cb, ca.ToString() + cb);
            }
        }
        searches = made;
        differ = found;
        return lines;
    }
}
'@

$release = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full').Release
$build = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion').CurrentBuild
"Windows PowerShell $($PSVersionTable.PSVersion), CLR $([Environment]::Version), .NET Framework release $release, Windows build $build, culture $([Globalization.CultureInfo]::CurrentCulture.Name)"

$failed = $false
$searches = 0; $differ = 0
$lines = [FrameworkAsciiSearch]::Run($false, [ref]$searches, [ref]$differ)
"$searches searches, $differ differ from the ordinal search"
foreach ($line in $lines) { "  $line" }
if ($searches -eq 0) { Write-Host '::error::No search was made.'; $failed = $true }
if ($differ -gt 0) { Write-Host "::error::$differ searches under OrdinalIgnoreCase differ from the ordinal search on ASCII text."; $failed = $true }

$controlSearches = 0; $controlDiffer = 0
$null = [FrameworkAsciiSearch]::Run($true, [ref]$controlSearches, [ref]$controlDiffer)
"Control, '[' in the text folded as '{' in the ordinal search: $controlSearches searches, $controlDiffer differ"
if ($controlSearches -ne $searches) { Write-Host "::error::The control made $controlSearches searches, not $searches."; $failed = $true }
if ($controlDiffer -eq 0) { Write-Host '::error::The control found no difference, so this run could not have seen one.'; $failed = $true }

if ($failed) { exit 1 }
'Every ASCII search under OrdinalIgnoreCase finds what the ordinal search finds.'
