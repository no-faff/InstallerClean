using System.Text.Json;

namespace InstallerClean.Services;

/// <summary>Why this copy does not install a newer version that its release's update.json rules out.</summary>
public enum UpdateRefusal
{
    /// <summary>
    /// Anything the copy cannot act on: a schema or a requirement it does not know, this
    /// copy's download not listed, a file that does not read as the schema, or no file
    /// where the version is one that has to carry it.
    /// </summary>
    CannotInstallFromThisCopy,

    /// <summary>The download needs .NET Framework 4.8, and this PC does not have it.</summary>
    NeedsNetFramework48,
}

/// <summary>
/// The update.json a release publishes beside its downloads, saying what each download
/// needs on the PC, and the answer it gives a copy about to update to that release.
/// </summary>
/// <remarks>
/// <para>
/// The file is <c>{"schema": 1, "downloads": {"setup": {"requires": []}, ...}}</c>, keyed by
/// the download (<see cref="DownloadKey"/>). A requirement is a name. Fields this schema
/// does not name are ignored, so a later release can add information that this copy
/// passes over.
/// </para>
/// <para>
/// The file can only stop an update. Nothing in it names a file, an address or a line to
/// show, and every check a download goes through (its first bytes, its length, the
/// version it carries, the newer version starting) still applies to a release it allows.
/// Every later release carries the file in this shape, or this copy refuses it.
/// </para>
/// </remarks>
public static class UpdateManifest
{
    /// <summary>The file's name in each release.</summary>
    public const string FileName = "update.json";

    /// <summary>The largest file read; a larger one is refused as a failed read.</summary>
    public const int MaxBytes = 64 * 1024;

    /// <summary>The one schema this copy reads.</summary>
    public const int Schema = 1;

    /// <summary>The requirement naming .NET Framework 4.8.</summary>
    public const string NetFramework48 = "netfx-4.8";

    /// <summary>
    /// The newest version a release may be without the file: the releases before the file
    /// existed. Every newer one carries it, so a newer release with no file is refused.
    /// </summary>
    public static readonly Version LastVersionWithoutManifest = new(3, 5, 0);

    /// <summary>
    /// The key and value Microsoft documents for the installed .NET Framework 4.x, and the
    /// smallest Release value that is 4.8.
    /// </summary>
    internal const string NetFrameworkKeyPath = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full";
    internal const string NetFrameworkReleaseValueName = "Release";
    internal const int NetFramework48Release = 528040;

    /// <summary>
    /// Microsoft's link to its .NET Framework 4.8 offline installer, as its support article
    /// for that installer gives it. It answers with the installer file itself, so following
    /// it downloads the file.
    /// </summary>
    public const string NetFramework48DownloadUrl = "https://go.microsoft.com/fwlink/?linkid=2088631";

    /// <summary>Whether <paramref name="body"/> parses as JSON at all, whatever its shape.</summary>
    public static bool IsJson(byte[] body)
    {
        try
        {
            using var _ = JsonDocument.Parse(body);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>Where the release of <paramref name="version"/> publishes the file.</summary>
    public static Uri Address(string version) => new($"{AppCopy.DownloadRoot}v{version}/{FileName}");

    /// <summary>
    /// The key a kind's download has in the file, or null for a kind with no download of
    /// its own.
    /// </summary>
    public static string? DownloadKey(AppCopyKind kind) => kind switch
    {
        AppCopyKind.Setup => "setup",
        AppCopyKind.Portable => "portable",
        AppCopyKind.CommandLineDownload => "cli",
        _ => null,
    };

    /// <summary>
    /// Whether the 64-bit view of the registry says .NET Framework 4.8 or later is installed.
    /// A value that is absent, not a number or cannot be read says it is not.
    /// </summary>
    public static bool HasNetFramework48(IRegistryReader registry) =>
        registry.LocalMachineDwordValue(NetFrameworkKeyPath, NetFrameworkReleaseValueName)
            is { State: RegistryDwordState.Read, Value: >= NetFramework48Release };

    /// <summary>
    /// The answer for a <paramref name="kind"/> copy about to update to
    /// <paramref name="version"/>, given the file's bytes, or null where the release has no
    /// file: null where it may install it, or why it does not.
    /// </summary>
    /// <param name="manifest">The file's bytes, or null where the release has none.</param>
    /// <param name="version">The version the update check found.</param>
    /// <param name="kind">The kind of copy updating.</param>
    /// <param name="hasNetFramework48">
    /// Answers whether this PC has .NET Framework 4.8, asked only where a download needs it.
    /// </param>
    public static UpdateRefusal? Judge(byte[]? manifest, string version, AppCopyKind kind, Func<bool> hasNetFramework48)
    {
        if (manifest is null)
        {
            return Version.TryParse(version, out var parsed) && parsed.Revision <= 0
                && new Version(parsed.Major, parsed.Minor, Math.Max(parsed.Build, 0)) <= LastVersionWithoutManifest
                    ? null
                    : UpdateRefusal.CannotInstallFromThisCopy;
        }
        if (DownloadKey(kind) is not { } key) return UpdateRefusal.CannotInstallFromThisCopy;

        try
        {
            using var document = JsonDocument.Parse(manifest);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("schema", out var schema)
                || schema.ValueKind != JsonValueKind.Number
                || !schema.TryGetInt32(out var schemaNumber) || schemaNumber != Schema
                || !root.TryGetProperty("downloads", out var downloads)
                || downloads.ValueKind != JsonValueKind.Object
                || !downloads.TryGetProperty(key, out var download)
                || download.ValueKind != JsonValueKind.Object
                || !download.TryGetProperty("requires", out var requires)
                || requires.ValueKind != JsonValueKind.Array)
            {
                return UpdateRefusal.CannotInstallFromThisCopy;
            }

            UpdateRefusal? refusal = null;
            foreach (var requirement in requires.EnumerateArray())
            {
                if (requirement.ValueKind != JsonValueKind.String) return UpdateRefusal.CannotInstallFromThisCopy;
                if (requirement.GetString() != NetFramework48) return UpdateRefusal.CannotInstallFromThisCopy;
                if (refusal is null && !hasNetFramework48()) refusal = UpdateRefusal.NeedsNetFramework48;
            }
            return refusal;
        }
        catch (JsonException)
        {
            return UpdateRefusal.CannotInstallFromThisCopy;
        }
    }
}
