using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PostmanClone.App.Views;

public partial class settings_dialog : Window
{
    public settings_dialog()
    {
        InitializeComponent();
    }

    private void SaveButton_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Save settings to configuration
        Close();
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
