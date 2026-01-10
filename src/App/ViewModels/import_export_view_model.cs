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

    public import_export_view_model(i_collection_repository collection_repository)
    {
        _collection_repository = collection_repository;
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
        if (SelectedCollectionForExport is null)
        {
            StatusMessage = "Please select a collection to export.";
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
            // Use real collection exporter
            var exporter = new collection_exporter();
            exporter.export_to_file(SelectedCollectionForExport, ExportFilePath);
            
            StatusMessage = $"Successfully exported '{SelectedCollectionForExport.name}' to {ExportFilePath}";
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
