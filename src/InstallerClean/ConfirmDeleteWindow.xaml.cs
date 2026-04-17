using System.Windows;
using InstallerClean.Helpers;

namespace InstallerClean;

public partial class ConfirmDeleteWindow : Window
{
    private const long TotalWarnThreshold = 1_073_741_824;   // 1 GB
    private const long SingleFileWarnThreshold = 524_288_000; // 500 MB

    public ConfirmDeleteWindow(int fileCount, string sizeDisplay, long totalBytes = 0, long maxSingleFileBytes = 0)
    {
        InitializeComponent();
        var label = DisplayHelpers.Pluralise(fileCount, "file", "files");
        MessageText.Text = $"Delete {fileCount} {label} ({sizeDisplay})?";

        // Windows silently skips the Recycle Bin when either a single file or
        // the total exceeds the per-drive quota.
        if (totalBytes > TotalWarnThreshold || maxSingleFileBytes > SingleFileWarnThreshold)
            LargeSizeWarning.Visibility = Visibility.Visible;

        this.EnableAltSpaceSystemMenu();
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
