namespace InstallerClean.Tests.Helpers;

/// <summary>
/// Runs a clean-up once, as the AppDomain the tests run in ends. On .NET Framework xUnit runs the
/// test assembly in an AppDomain of its own, which sees DomainUnload when the run ends and never
/// ProcessExit; the test process's default domain, and any domain still loaded as the process
/// ends, sees ProcessExit and never DomainUnload. So the clean-up listens for both and runs on
/// whichever comes, once.
/// </summary>
internal static class DomainEnd
{
    internal static void Then(Action cleanUp)
    {
        var done = 0;
        void Run(object? sender, EventArgs e)
        {
            if (Interlocked.Exchange(ref done, 1) != 0) return;
            // A clean-up that fails leaves its folder in the temp folder; the reason goes to the
            // debug output (Trace's default listener) rather than out of the domain's unload.
            try { cleanUp(); } catch (Exception ex) { System.Diagnostics.Trace.WriteLine($"DomainEnd: {ex}"); }
        }
        AppDomain.CurrentDomain.DomainUnload += Run;
        AppDomain.CurrentDomain.ProcessExit += Run;
    }

    /// <summary>Deletes <paramref name="folder"/> and everything in it as the domain ends.</summary>
    internal static void DeleteFolder(string folder) => Then(() => Directory.Delete(folder, recursive: true));
}
