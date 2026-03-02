using System.Diagnostics;
using System.Reflection;
using System.Windows;

namespace SimpleWindowsInstallerCleaner;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is not null ? $"Version {version.Major}.{version.Minor}.{version.Build}" : string.Empty;
    }

    private void Hyperlink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Documents.Hyperlink link && link.NavigateUri is not null)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = link.NavigateUri.AbsoluteUri,
                UseShellExecute = true
            });
        }
    }
}
