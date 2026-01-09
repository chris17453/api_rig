using Avalonia.Controls;
using Avalonia.Interactivity;
using PostmanClone.App.ViewModels;

namespace PostmanClone.App.Views;

public partial class collection_runner_dialog : Window
{
    public collection_runner_dialog()
    {
        InitializeComponent();
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        
        if (DataContext is collection_runner_view_model vm)
        {
            await vm.LoadCollections(default);
        }
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
