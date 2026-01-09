using Avalonia.Controls;
using Avalonia.Interactivity;
using PostmanClone.App.ViewModels;

namespace PostmanClone.App.Views;

public partial class registration_dialog : Window
{
    public registration_dialog()
    {
        InitializeComponent();
    }

    private async void RegisterButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is registration_view_model viewModel)
        {
            await viewModel.save_registration_async();
            // Wait a moment for the user to see the success message
            await Task.Delay(1000);
            Close(true);
        }
    }

    private async void SkipButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is registration_view_model viewModel)
        {
            await viewModel.skip_registration_async();
            Close(false);
        }
    }
}
