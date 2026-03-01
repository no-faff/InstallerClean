using System.Windows;
using SimpleWindowsInstallerCleaner.Helpers;

namespace SimpleWindowsInstallerCleaner;

public partial class ConfirmDeleteWindow : Window
{
    public ConfirmDeleteWindow(int fileCount, string sizeDisplay)
    {
        InitializeComponent();
        var label = DisplayHelpers.Pluralise(fileCount, "file", "files");
        MessageText.Text = $"Delete {fileCount} {label} ({sizeDisplay})?";
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
