using System.Globalization;
using System.Runtime.CompilerServices;
using InstallerClean.Helpers;

// Tests run one at a time. The app's chosen language is process-global state:
// Localisation holds a static override, and every Strings lookup and every
// DisplayHelpers format call reads it, so a test that sets a language rewrites
// what every other running test sees. The command-line tests that swap
// Console.Out and read it back rely on this too, and so does EventLogRecorder's
// one list. Under xUnit's default per-class parallelism one test's language and
// output would reach another's run.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]

// The tests' own imports name System32 as the place to look for the libraries they load.
[assembly: System.Runtime.InteropServices.DefaultDllImportSearchPaths(
    System.Runtime.InteropServices.DllImportSearchPath.System32)]

namespace InstallerClean.Tests;

/// <summary>
/// Every test runs in en-GB, its culture and its UI culture alike, whatever the
/// language of the PC running it. The two defaults set here apply to every thread that
/// has not set its own: the test threads, async continuations and the thread pool, and
/// a thread already running when the assembly loads. A test that needs another language
/// sets its own for its run and puts the previous one back. TestCultureTests reads both
/// from each kind of thread and fails where this is gone.
/// </summary>
internal static class TestCulture
{
    [ModuleInitializer]
    internal static void Pin()
    {
        var english = CultureInfo.GetCultureInfo("en-GB");
        CultureInfo.DefaultThreadCurrentCulture = english;
        CultureInfo.DefaultThreadCurrentUICulture = english;
    }
}

/// <summary>
/// Every test that writes the crash log writes it to a folder of this run's own under the
/// temp folder, never to the log of the PC running the suite. The folder is set before any
/// test runs, so a test reaching the log by a path nobody planned for is covered too, and
/// it is deleted when the test process exits, or the AppDomain it was set in is unloaded,
/// along with the shared InstallerClean.Tests folder above it where no other run's folder is
/// left in it. A test that changes the
/// folder for its run, through <see cref="OwnFolder"/> or by hand as CrashLogTests does,
/// puts this one back.
/// </summary>
internal static class TestCrashLog
{
    private static readonly string Shared = Path.Combine(Path.GetTempPath(), "InstallerClean.Tests");

    /// <summary>A folder of its own under the shared one, not yet made.</summary>
    internal static string NewFolder() => Path.Combine(Shared, Guid.NewGuid().ToString("N"));

    /// <summary>
    /// Points the crash log at a new folder until the answer is disposed, which puts back the
    /// folder it replaced and deletes the new one. A test class takes one where nothing after
    /// it in the class's construction can throw, since a test class whose constructor throws
    /// is never disposed, and each of its tests then reads <see cref="Text"/> and sees only
    /// what its own work wrote.
    /// </summary>
    internal static IDisposable OwnFolder() => new Folder();

    /// <summary>
    /// Folders are put back in the reverse of the order they were taken. Disposing one while
    /// the crash log points anywhere else throws and changes nothing: putting its previous
    /// folder back then would send the writes of whatever set the current folder elsewhere,
    /// and the later put-back of that one would point the crash log at this folder after it
    /// is deleted.
    /// </summary>
    private sealed class Folder : IDisposable
    {
        private readonly string? _previous = CrashLog.FolderForTests;
        private readonly string _folder = NewFolder();
        private bool _disposed;

        public Folder() => CrashLog.FolderForTests = _folder;

        public void Dispose()
        {
            if (_disposed) return;
            if (CrashLog.FolderForTests != _folder)
                throw new InvalidOperationException(
                    $"The crash log's folder is {CrashLog.FolderForTests}, not {_folder}, which is being put back.");
            _disposed = true;
            CrashLog.FolderForTests = _previous;
            TryDelete(_folder);
        }
    }

    [ModuleInitializer]
    internal static void Redirect()
    {
        var folder = NewFolder();
        CrashLog.FolderForTests = folder;
        void Remove(object? sender, EventArgs e)
        {
            TryDelete(folder);
            // Not recursive, so a folder another run is still using stays.
            try { Directory.Delete(Shared); } catch (Exception) { }
        }
        // ProcessExit for the test process's own domain; DomainUnload for a domain a test
        // makes and unloads, which never sees ProcessExit.
        AppDomain.CurrentDomain.ProcessExit += Remove;
        AppDomain.CurrentDomain.DomainUnload += Remove;
    }

    /// <summary>
    /// The crash log as it stands, after the copy it was last moved aside to, or empty where
    /// there is neither. Read whole, so a test sees every entry its work wrote even where a
    /// write moved a full log aside.
    /// </summary>
    internal static string Text()
    {
        var archive = ReadIfThere(CrashLog.LogPath + CrashLog.ArchiveSuffix);
        if (archive.Length > 0 && !archive.EndsWith('\n')) archive += "\n";
        return archive + ReadIfThere(CrashLog.LogPath);
    }

    /// <summary>
    /// The entries in the crash log in the order they were written, each from its opening
    /// "---- " line to the line before the next.
    /// </summary>
    internal static IReadOnlyList<string> Entries()
    {
        var entries = new List<string>();
        foreach (var line in Text().Split('\n'))
        {
            if (line.StartsWith("---- ", StringComparison.Ordinal)) entries.Add(line);
            else if (entries.Count > 0) entries[^1] += "\n" + line;
        }
        return entries;
    }

    /// <summary>The one entry in the crash log. A log holding none, or more than one, fails the test.</summary>
    internal static string OnlyEntry() => Assert.Single(Entries());

    /// <summary>
    /// How many entries in the crash log are a start of <paramref name="file"/> that Windows
    /// refused, counted by the line in each that opens with the Win32Exception and names the
    /// file.
    /// </summary>
    internal static int FailedStartsOf(string file) =>
        Text().Split('\n').Count(line =>
            line.StartsWith("System.ComponentModel.Win32Exception", StringComparison.Ordinal)
            && line.Contains(file, StringComparison.OrdinalIgnoreCase));

    private static string ReadIfThere(string path) => File.Exists(path) ? File.ReadAllText(path) : string.Empty;

    /// <summary>
    /// Deletes <paramref name="folder"/> and everything in it, and throws nothing where the
    /// delete is refused: a file an antivirus holds open, or a read-only one on Windows,
    /// leaves the folder in place and the test that made it passing.
    /// </summary>
    internal static void TryDelete(string folder)
    {
        try { Directory.Delete(folder, recursive: true); } catch (Exception) { }
    }
}
