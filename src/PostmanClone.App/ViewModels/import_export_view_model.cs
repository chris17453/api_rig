using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;

namespace PostmanClone.App.ViewModels;

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
            // Read the file content
            if (!File.Exists(ImportFilePath))
            {
                StatusMessage = "File not found.";
                HasError = true;
                return;
            }

            var json = await File.ReadAllTextAsync(ImportFilePath);
            
            // Parse and validate (mock for now - will use real parser from Data team)
            var collection = ParsePostmanCollection(json);
            
            if (collection is not null)
            {
                // Save to repository
                await _collection_repository.save_async(collection, CancellationToken.None);
                
                StatusMessage = $"Successfully imported '{collection.name}' with {collection.items.Count} items.";
                HasError = false;
                
                collection_imported?.Invoke(this, collection);
            }
            else
            {
                StatusMessage = "Invalid Postman collection format.";
                HasError = true;
            }
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
            // Convert to Postman format (mock for now)
            var json = ConvertToPostmanFormat(SelectedCollectionForExport);
            
            // Write to file
            await File.WriteAllTextAsync(ExportFilePath, json);
            
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

    /// <summary>
    /// Mock parser - will be replaced with real implementation from Data team.
    /// </summary>
    private postman_collection_model? ParsePostmanCollection(string json)
    {
        try
        {
            // Basic validation - check if it looks like a Postman collection
            if (!json.Contains("\"info\"") || !json.Contains("\"item\""))
            {
                return null;
            }

            // Create a mock parsed collection
            return new postman_collection_model
            {
                name = "Imported Collection",
                description = "Imported from Postman",
                items = new List<collection_item_model>
                {
                    new collection_item_model
                    {
                        name = "Sample Request",
                        is_folder = false,
                        request = new http_request_model
                        {
                            name = "Sample Request",
                            method = http_method.get,
                            url = "https://api.example.com/data"
                        }
                    }
                }
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Mock converter - will be replaced with real implementation from Data team.
    /// </summary>
    private string ConvertToPostmanFormat(postman_collection_model collection)
    {
        // Generate Postman collection v2.1 format
        return $$"""
        {
            "info": {
                "_postman_id": "{{collection.id}}",
                "name": "{{collection.name}}",
                "description": "{{collection.description ?? ""}}",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                {{string.Join(",\n        ", collection.items.Where(i => i.request != null).Select(i => $$"""
                {
                    "name": "{{i.name}}",
                    "request": {
                        "method": "{{i.request!.method.ToString().ToUpper()}}",
                        "url": "{{i.request.url}}"
                    }
                }
                """))}}
            ]
        }
        """;
    }
}
