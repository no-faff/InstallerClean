namespace InstallerClean.Services;

/// <summary>Probes whether a named system mutex exists, without creating it.</summary>
public interface IMutexProbe
{
    /// <summary>True when the named mutex currently exists. Never creates the mutex. Access-denied counts as exists; other failures count as not exists.</summary>
    bool Exists(string name);
}
