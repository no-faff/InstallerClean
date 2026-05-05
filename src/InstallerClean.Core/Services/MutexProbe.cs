namespace InstallerClean.Services;

/// <summary>Production IMutexProbe via Mutex.OpenExisting.</summary>
internal sealed class MutexProbe : IMutexProbe
{
    public bool Exists(string name)
    {
        try
        {
            using var m = System.Threading.Mutex.OpenExisting(name);
            return true;
        }
        catch (System.Threading.WaitHandleCannotBeOpenedException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            // Object exists, access denied: counts as exists.
            return true;
        }
        catch
        {
            return false;
        }
    }
}
