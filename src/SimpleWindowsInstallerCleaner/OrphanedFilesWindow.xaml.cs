using System.Windows;
using System.Windows.Controls;
using SimpleWindowsInstallerCleaner.ViewModels;

namespace SimpleWindowsInstallerCleaner;

public partial class OrphanedFilesWindow : Window
{
    public OrphanedFilesWindow(OrphanedFilesViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (ActionableList.Items.Count > 0)
        {
            ActionableList.SelectedIndex = 0;
            ActionableList.ScrollIntoView(ActionableList.Items[0]);
            var container = (ListBoxItem?)ActionableList.ItemContainerGenerator
                .ContainerFromIndex(0);
            container?.Focus();
        }
    }

    private void ActionableList_GotFocus(object sender, RoutedEventArgs e)
    {
        ExcludedList.UnselectAll();
    }

    private void ExcludedList_GotFocus(object sender, RoutedEventArgs e)
    {
        ActionableList.UnselectAll();
    }
}
