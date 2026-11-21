namespace InstallerClean.Services;

/// <summary>
/// Thin abstraction over the app's message dialog so ViewModels can raise one
/// without taking a direct dependency on a WPF Window. Lets tests assert that
/// the right warning was shown without spawning a real window.
/// </summary>
public interface IDialogService
{
    void ShowWarning(string message, string caption);

    /// <summary>
    /// A warning whose phrase in <c>[ ]</c> is a link to <paramref name="url"/>. A
    /// message with no complete pair is shown as plain text.
    /// </summary>
    void ShowWarningWithLink(string message, string caption, string url);

    void ShowError(string message, string caption);
}
