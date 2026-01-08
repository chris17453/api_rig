using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using PostmanClone.App.ViewModels;

namespace PostmanClone.App.Views;

public partial class export_dialog : Window
{
    public export_dialog()
    {
        InitializeComponent();
    }

    private async void BrowseButton_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel == null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Collection",
            SuggestedFileName = "collection.json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } }
            }
        });

        if (file != null)
        {
            var vm = DataContext as import_export_view_model;
            if (vm != null)
            {
                vm.ExportFilePath = file.Path.LocalPath;
            }
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
