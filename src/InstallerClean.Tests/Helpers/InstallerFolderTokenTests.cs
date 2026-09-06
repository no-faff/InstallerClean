using System.Collections;
using System.Globalization;
using InstallerClean.Cli;
using InstallerClean.Helpers;
using InstallerClean.Resources;
using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

/// <summary>
/// The installer-folder token, and the contract that makes it worth having: a
/// string that names the cache folder writes <c>{InstallerFolder}</c> and every
/// consumer receives the resolved path, so a machine whose Windows is not on C:
/// is told the truth about the folder the app is working in.
///
/// The end-to-end assertions go through <c>Strings.Get</c> and <c>Strings.Find</c>
/// rather than the helper alone, because the design rests on those two being the
/// only doors: a consumer that resolved a resource some other way would read a raw
/// token and nothing else here would notice. What holds the "only" is
/// <c>scripts/check-cross-key-rules.mjs</c> rather than a test, this suite reading
/// raw itself, so a test forbidding a raw read would have to exempt its own.
/// </summary>
public class InstallerFolderTokenTests
{
    private const string Literal = "C:\\Windows\\Installer";

    [Fact]
    public void Resolve_substitutes_the_folder_the_scan_uses()
    {
        var resolved = InstallerFolderToken.Resolve($"Scan {InstallerFolderToken.Token} again");

        Assert.Equal($"Scan {InstallerCacheHelpers.InstallerFolder} again", resolved);
    }

    [Fact]
    public void Resolve_substitutes_every_occurrence()
    {
        var resolved = InstallerFolderToken.Resolve(
            $"{InstallerFolderToken.Token} and {InstallerFolderToken.Token}");

        var folder = InstallerCacheHelpers.InstallerFolder;
        Assert.Equal($"{folder} and {folder}", resolved);
    }

    [Fact]
    public void Resolve_returns_a_string_carrying_no_token_unchanged()
    {
        const string plain = "Nothing to clean up.";

        Assert.Same(plain, InstallerFolderToken.Resolve(plain));
    }

    [Fact]
    public void A_key_naming_the_folder_reaches_a_consumer_resolved()
    {
        var value = Strings.Automation_RescanInstaller;

        Assert.DoesNotContain(InstallerFolderToken.Token, value, StringComparison.Ordinal);
        Assert.Contains(InstallerCacheHelpers.InstallerFolder, value, StringComparison.Ordinal);
    }

    [Fact]
    public void A_key_resolved_by_name_reaches_its_consumer_resolved_too()
    {
        // The second door, which exists because a satellite-only plural override
        // has no accessor to come through and so is looked up by name. Asserted on
        // a key that does carry the token, no override naming the folder existing
        // to assert on instead.
        var value = Strings.Find("Automation.RescanInstaller");

        Assert.NotNull(value);
        Assert.DoesNotContain(InstallerFolderToken.Token, value, StringComparison.Ordinal);
        Assert.Contains(InstallerCacheHelpers.InstallerFolder, value, StringComparison.Ordinal);
    }

    [Fact]
    public void A_key_no_resx_defines_comes_back_absent_rather_than_echoed()
    {
        // The one way the two doors differ, and what the override lookup rests on:
        // absent means "this language declines the override, use the neutral form".
        // A key echoed back the way Get echoes one would be rendered as the count
        // noun, so "3 Plural.File.Few" would reach a screen.
        Assert.Null(Strings.Find("Plural.File.NoSuchCategory"));
    }

    [Fact]
    public void The_token_is_spent_before_string_format_sees_the_value()
    {
        // The one key carrying both the token and positional arguments. The
        // token is spent by the time Format runs, so the two placeholder systems
        // never meet; Format throwing here is the failure this pins.
        var formatted = string.Format(
            CultureInfo.InvariantCulture,
            Strings.Body_MainExplanation_Why,
            Strings.Reason_Orphaned, Strings.Reason_Superseded, Strings.Reason_Obsoleted);

        Assert.Contains(InstallerCacheHelpers.InstallerFolder, formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("{0}", formatted, StringComparison.Ordinal);
    }

    [Fact]
    public void The_machine_read_event_log_line_names_the_folder_in_words()
    {
        // Neither a spelled path nor the token: the folder in words, so the one
        // entry on this channel that names it reads the same on every machine
        // without resolving anything. The words are asserted beside the two
        // absences, which a line that had stopped naming the folder at all would
        // otherwise satisfy.
        //
        // Read through the en-GB scope the emit site wraps this line in, so
        // the value asserted is the one a machine reads.
        var value = MachineContract.English(
            () => Strings.Cli_EventLogMoveDestinationInsideInstaller);

        Assert.DoesNotContain(Literal, value, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(InstallerFolderToken.Token, value, StringComparison.Ordinal);
        Assert.Contains("the Windows Installer folder", value, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(EveryShippedCulture))]
    public void No_shipped_string_hardcodes_the_folder(string cultureName)
    {
        // The gate on the next string to name the folder: a hardcoded path is
        // correct on the machine that wrote it and silently wrong on any other,
        // so it is the kind of defect that draws no report. Read from the
        // compiled resources, so it is the shipped satellite being asserted
        // rather than a resx on disk.
        var culture = cultureName.Length == 0
            ? CultureInfo.InvariantCulture
            : CultureInfo.GetCultureInfo(cultureName);
        var set = Strings.ResourceManager.GetResourceSet(
            culture, createIfNotExists: true, tryParents: cultureName.Length == 0);

        Assert.NotNull(set);

        var offenders = set!.Cast<DictionaryEntry>()
            .Where(e => e.Value is string v
                        && v.Contains(Literal, StringComparison.OrdinalIgnoreCase))
            .Select(e => (string)e.Key)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        Assert.True(offenders.Count == 0,
            $"{offenders.Count} {cultureName} string(s) write the installer folder as a literal "
            + $"instead of {InstallerFolderToken.Token}: {string.Join(", ", offenders)}");
    }

    public static TheoryData<string> EveryShippedCulture()
    {
        // The empty name is the neutral, read with tryParents so the invariant
        // resource set resolves.
        var data = new TheoryData<string> { string.Empty };
        foreach (var name in SupportedLanguages.CultureNames)
        {
            if (!string.Equals(name, SupportedLanguages.Neutral, StringComparison.OrdinalIgnoreCase))
                data.Add(name);
        }

        return data;
    }
}
