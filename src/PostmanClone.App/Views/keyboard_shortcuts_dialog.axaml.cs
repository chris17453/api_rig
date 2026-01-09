using Avalonia.Controls;
using Avalonia.Interactivity;

namespace PostmanClone.App.Views;

public partial class keyboard_shortcuts_dialog : Window
{
    public keyboard_shortcuts_dialog()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
