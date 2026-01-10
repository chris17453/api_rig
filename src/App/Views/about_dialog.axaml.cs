using Avalonia.Controls;
using Avalonia.Interactivity;

namespace App.Views;

public partial class about_dialog : Window
{
    public about_dialog()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
