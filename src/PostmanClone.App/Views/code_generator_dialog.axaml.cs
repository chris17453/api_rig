using Avalonia.Controls;
using Avalonia.Interactivity;
using PostmanClone.App.ViewModels;

namespace PostmanClone.App.Views;

public partial class code_generator_dialog : Window
{
    public code_generator_dialog()
    {
        InitializeComponent();
    }

    private async void CopyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is request_editor_view_model vm && !string.IsNullOrEmpty(vm.GeneratedCurlCommand))
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(vm.GeneratedCurlCommand);
                
                if (this.FindControl<TextBlock>("CopyFeedback") is TextBlock feedback)
                {
                    feedback.Text = "✓ Copied to clipboard!";
                    
                    // Clear feedback after 2 seconds
                    await Task.Delay(2000);
                    feedback.Text = "";
                }
            }
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
