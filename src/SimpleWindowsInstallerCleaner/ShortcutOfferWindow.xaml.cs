using System.Windows;
using System.Windows.Input;

namespace SimpleWindowsInstallerCleaner;

public partial class ShortcutOfferWindow : Window
{
    public bool CreateDesktopShortcut { get; private set; }

    public ShortcutOfferWindow()
    {
        InitializeComponent();
    }

    private void OnAddShortcut(object sender, RoutedEventArgs e)
    {
        CreateDesktopShortcut = DesktopCheckBox.IsChecked == true;
        DialogResult = true;
        Close();
    }

    private void OnNoThanks(object sender, ExecutedRoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void OnNoThanks(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
