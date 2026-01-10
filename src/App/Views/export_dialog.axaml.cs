using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using App.ViewModels;

namespace App.Views;

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

        var vm = DataContext as import_export_view_model;
        if (vm == null) return;

        if (vm.ExportToSingleFile || vm.SelectedCollectionCount == 1)
        {
            // Single file export - use save file picker
            var suggestedName = vm.SelectedCollectionCount == 1
                ? $"{vm.CollectionsForExport.FirstOrDefault(c => c.IsSelected)?.Name ?? "collection"}.json"
                : "collections.json";

            var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Collection",
                SuggestedFileName = suggestedName,
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("JSON files") { Patterns = new[] { "*.json" } }
                }
            });

            if (file != null)
            {
                vm.ExportFilePath = file.Path.LocalPath;
            }
        }
        else
        {
            // Separate files export - use folder picker
            var folder = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Export Directory",
                AllowMultiple = false
            });

            if (folder.Count > 0)
            {
                // Set a placeholder file path in the directory
                vm.ExportFilePath = System.IO.Path.Combine(folder[0].Path.LocalPath, "collection.json");
            }
        }
    }

    private void SeparateFilesRadio_Click(object? sender, RoutedEventArgs e)
    {
        var vm = DataContext as import_export_view_model;
        if (vm != null)
        {
            vm.ExportToSingleFile = false;
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
