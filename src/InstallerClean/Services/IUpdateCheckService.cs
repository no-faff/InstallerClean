using InstallerClean.Models;

namespace InstallerClean.Services;

public interface IUpdateCheckService
{
    Task<UpdateCheckResult> GetLatestVersionAsync();
}
