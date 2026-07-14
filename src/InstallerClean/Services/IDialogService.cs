namespace InstallerClean.Services;

/// <summary>
/// Thin abstraction over the app's message dialog so ViewModels can raise one
/// without taking a direct dependency on a WPF Window. Lets tests assert that
/// the right warning was shown without spawning a real window.
/// </summary>
public interface IDialogService
{
    void ShowWarning(string message, string caption);
    void ShowError(string message, string caption);
}
