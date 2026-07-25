using System.Collections;
using System.Globalization;
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
/// The end-to-end assertions go through <c>Strings.Get</c> rather than the
/// helper alone, because the whole design rests on that being the single door:
/// a consumer that resolved a resource some other way would read a raw token
/// and nothing else here would notice.
/// </summary>
public class InstallerFolderTokenTests
{
    // Written to the Application event log, which monitoring tools match on, so
    // its shape is held stable and English at the emit site (MachineContract).
    // It is the one string naming the folder that keeps the C: literal.
    private const string MachineContractKey = "Cli.EventLogMoveDestinationInsideInstaller";

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
    public void The_machine_read_event_log_line_keeps_its_literal()
    {
        var value = Strings.Cli_EventLogMoveDestinationInsideInstaller;

        Assert.Contains(Literal, value, StringComparison.Ordinal);
        Assert.DoesNotContain(InstallerFolderToken.Token, value, StringComparison.Ordinal);
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
            .Where(e => (string)e.Key != MachineContractKey)
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
