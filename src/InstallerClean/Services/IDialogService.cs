namespace InstallerClean.Services;

/// <summary>
/// Thin abstraction over MessageBox so ViewModels can raise dialogs without
/// taking a direct dependency on WPF's MessageBox. Lets tests assert that
/// the right warning was shown without spawning a real window.
/// </summary>
public interface IDialogService
{
    void ShowWarning(string message, string caption);
    void ShowError(string message, string caption);
}
