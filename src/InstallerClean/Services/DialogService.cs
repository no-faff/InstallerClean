using InstallerClean.Helpers;

namespace InstallerClean.Services;

public sealed class DialogService : IDialogService
{
    // MessageDialog owns the UI-thread marshalling (these are called from
    // view-model paths that can be on a worker) and the fallback for a themed
    // window it cannot build.
    public void ShowWarning(string message, string caption) =>
        MessageDialog.Show(message, caption, MessageKind.Warning);

    public void ShowError(string message, string caption) =>
        MessageDialog.Show(message, caption, MessageKind.Error);
}
