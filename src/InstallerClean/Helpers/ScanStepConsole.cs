using System.IO;
using System.Runtime.InteropServices;

namespace InstallerClean.Helpers;

/// <summary>
/// Developer-only aid for screenshotting the startup scan. With the
/// environment variable <c>IC_SCAN_STEP</c> set to <c>1</c>, the scan
/// pauses at every milestone (driven by <see cref="ScanStepGate"/>) and
/// waits for Enter on a console, so each phase can be captured before it
/// advances. The switch is read once, at first use; unset, none of this
/// runs and the scan is byte-for-byte unchanged, so a shipped run is
/// never affected.
/// </summary>
internal static partial class ScanStepConsole
{
    // ATTACH_PARENT_PROCESS: attach to the console of the launching
    // process (the terminal behind "dotnet run") rather than open a new
    // window. Win32 returns BOOL (4 bytes); surfaced as int so the
    // signatures stay blittable and need no marshalling.
    private const uint AttachParentProcess = 0xFFFFFFFF;

    private static bool _consoleReady;

    public static bool IsEnabled { get; } =
        Environment.GetEnvironmentVariable("IC_SCAN_STEP") == "1";

    /// <summary>
    /// Connects stdout/stdin to a console so the step prompts are visible
    /// and Enter can advance the scan. A WinExe (the GUI subsystem) is
    /// launched without a console of its own, so without this the prompts
    /// would go nowhere. Attaches to the launching terminal where there is
    /// one, otherwise allocates a console window. Idempotent.
    /// </summary>
    public static void EnsureConsole()
    {
        if (_consoleReady) return;
        _consoleReady = true;

        if (GetConsoleWindow() != IntPtr.Zero) return; // a console is already attached

        if (AttachConsole(AttachParentProcess) == 0)
            AllocConsole(); // no parent console (not launched from a terminal): use a fresh window

        // The runtime binds its console streams on first use, which here
        // is before the console existed, so rebind them to the attached
        // console or the prompts and Enter would not reach it.
        Console.SetOut(new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true });
        Console.SetIn(new StreamReader(Console.OpenStandardInput()));
    }

    /// <summary>
    /// Prints the milestone and blocks until Enter. The caller guarantees
    /// this runs off the UI thread (so the window stays painted and
    /// responsive while it waits).
    /// </summary>
    public static void Pause(int step, string message)
    {
        Console.WriteLine();
        Console.WriteLine($"[scan step {step}/7] {message}");
        Console.Write("    the window is showing this phase now. Press Enter for the next... ");
        Console.ReadLine();
    }

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetConsoleWindow();

    [LibraryImport("kernel32.dll")]
    private static partial int AttachConsole(uint dwProcessId);

    [LibraryImport("kernel32.dll")]
    private static partial int AllocConsole();
}
