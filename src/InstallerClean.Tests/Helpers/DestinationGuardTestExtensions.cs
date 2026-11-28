using InstallerClean.Services;

namespace InstallerClean.Tests.Helpers;

internal static class DestinationGuardTestExtensions
{
    /// <summary>
    /// Discards the copy at <paramref name="destinationPath"/> by opening the folder it is in
    /// and discarding the file there by name, answering false where the path has no folder
    /// or no name, or the folder will not open.
    /// </summary>
    internal static bool DiscardAt(this IDestinationGuard guard, string destinationPath, out int failureCode)
    {
        var folder = System.IO.Path.GetDirectoryName(destinationPath);
        var name = System.IO.Path.GetFileName(destinationPath);
        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(name))
        {
            failureCode = 0;
            return false;
        }
        using var opened = guard.Open(folder, out failureCode);
        if (opened is null) return false;
        return guard.TryDiscardCopy(opened, name, out failureCode);
    }
}
