namespace InstallerClean.Services;

/// <summary>Probes whether a named system mutex is currently held, without creating it.</summary>
public interface IMutexProbe
{
    /// <summary>
    /// True when the named mutex exists and is currently owned by some
    /// thread. Never creates the mutex. An existing mutex whose DACL
    /// refuses the probe counts as held (held cannot be ruled out);
    /// a missing mutex and other failures count as not held.
    /// </summary>
    bool IsHeld(string name);
}
