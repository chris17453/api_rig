using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces;
using Core.Models;
using Data.Exporters;

namespace App.ViewModels;

/// <summary>
/// ViewModel for import/export dialogs for Postman collection JSON.
/// </summary>
public partial class import_export_view_model : ObservableObject
{
    private readonly i_collection_repository _collection_repository;

    [ObservableProperty]
    private string _importFilePath = string.Empty;

    [ObservableProperty]
    private string _exportFilePath = string.Empty;

    [ObservableProperty]
    private string _importPreview = string.Empty;

    [ObservableProperty]
    private bool _isImporting;

    [ObservableProperty]
    private bool _isExporting;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private postman_collection_model? _selectedCollectionForExport;

    // Multi-collection export support
    [ObservableProperty]
    private ObservableCollection<export_collection_item> _collectionsForExport = new();

    [ObservableProperty]
    private bool _exportAll;

    [ObservableProperty]
    private bool _exportToSingleFile = true;

    public bool HasSelectedCollections => CollectionsForExport.Any(c => c.IsSelected);

    public int SelectedCollectionCount => CollectionsForExport.Count(c => c.IsSelected);

    public import_export_view_model(i_collection_repository collection_repository)
    {
        _collection_repository = collection_repository;
    }

    public async Task LoadCollectionsForExportAsync()
    {
        CollectionsForExport.Clear();
        var collections = await _collection_repository.list_all_async(CancellationToken.None);

        foreach (var collection in collections)
        {
            var item = new export_collection_item
            {
                Collection = collection,
                IsSelected = false
            };
            item.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(HasSelectedCollections));
                OnPropertyChanged(nameof(SelectedCollectionCount));
            };
            CollectionsForExport.Add(item);
        }
    }

    partial void OnExportAllChanged(bool value)
    {
        foreach (var item in CollectionsForExport)
        {
            item.IsSelected = value;
        }
    }

    public event EventHandler<postman_collection_model>? collection_imported;
    public event EventHandler? import_cancelled;
    public event EventHandler? export_completed;

    [RelayCommand]
    private async Task ImportCollection()
    {
        if (string.IsNullOrWhiteSpace(ImportFilePath))
        {
            StatusMessage = "Please select a file to import.";
            HasError = true;
            return;
        }

        IsImporting = true;
        HasError = false;
        StatusMessage = string.Empty;

        try
        {
            // Use real import from collection_repository
            var collection = await _collection_repository.import_from_file_async(ImportFilePath, CancellationToken.None);
            
            StatusMessage = $"Successfully imported '{collection.name}' with {collection.items.Count} items.";
            HasError = false;
            
            collection_imported?.Invoke(this, collection);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Import failed: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsImporting = false;
        }
    }

    [RelayCommand]
    private async Task ExportCollection()
    {
        // Support for both single collection (legacy) and multi-collection export
        var selectedCollections = CollectionsForExport.Where(c => c.IsSelected).Select(c => c.Collection).ToList();

        // If no multi-select items, fall back to single selection
        if (selectedCollections.Count == 0 && SelectedCollectionForExport != null)
        {
            selectedCollections.Add(SelectedCollectionForExport);
        }

        if (selectedCollections.Count == 0)
        {
            StatusMessage = "Please select at least one collection to export.";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(ExportFilePath))
        {
            StatusMessage = "Please specify an export file path.";
            HasError = true;
            return;
        }

        IsExporting = true;
        HasError = false;
        StatusMessage = string.Empty;

        try
        {
            var exporter = new collection_exporter();

            if (selectedCollections.Count == 1)
            {
                // Single collection - export directly to file
                await Task.Run(() => exporter.export_to_file(selectedCollections[0], ExportFilePath));
                StatusMessage = $"Successfully exported '{selectedCollections[0].name}' to {ExportFilePath}";
            }
            else if (ExportToSingleFile)
            {
                // Multiple collections to single file - export as array
                await Task.Run(() => exporter.export_multiple_to_file(selectedCollections, ExportFilePath));
                StatusMessage = $"Successfully exported {selectedCollections.Count} collections to {ExportFilePath}";
            }
            else
            {
                // Multiple collections to separate files
                var directory = Path.GetDirectoryName(ExportFilePath) ?? ".";
                var extension = Path.GetExtension(ExportFilePath);
                var baseName = Path.GetFileNameWithoutExtension(ExportFilePath);

                foreach (var collection in selectedCollections)
                {
                    var safeName = string.Join("_", collection.name.Split(Path.GetInvalidFileNameChars()));
                    var filePath = Path.Combine(directory, $"{safeName}{extension}");
                    await Task.Run(() => exporter.export_to_file(collection, filePath));
                }

                StatusMessage = $"Successfully exported {selectedCollections.Count} collections to {directory}";
            }

            HasError = false;
            export_completed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Export failed: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsExporting = false;
        }
    }

    [RelayCommand]
    private async Task PreviewImport()
    {
        if (string.IsNullOrWhiteSpace(ImportFilePath) || !File.Exists(ImportFilePath))
        {
            ImportPreview = "No file selected or file not found.";
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(ImportFilePath);
            
            // Show first 2000 characters as preview
            ImportPreview = json.Length > 2000 
                ? json.Substring(0, 2000) + "\n... (truncated)"
                : json;
        }
        catch (Exception ex)
        {
            ImportPreview = $"Error reading file: {ex.Message}";
        }
    }

    [RelayCommand]
    private void CancelImport()
    {
        ImportFilePath = string.Empty;
        ImportPreview = string.Empty;
        StatusMessage = string.Empty;
        HasError = false;
        import_cancelled?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>
/// Wrapper for collections in export dialog with selection state.
/// </summary>
public partial class export_collection_item : ObservableObject
{
    [ObservableProperty]
    private postman_collection_model _collection = null!;

    [ObservableProperty]
    private bool _isSelected;

    public string Name => Collection?.name ?? string.Empty;
    public int ItemCount => Collection?.items?.Count ?? 0;
}
