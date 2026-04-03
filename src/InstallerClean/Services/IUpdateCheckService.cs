namespace InstallerClean.Services;

public interface IUpdateCheckService
{
    Task<string?> GetLatestVersionAsync();
}
