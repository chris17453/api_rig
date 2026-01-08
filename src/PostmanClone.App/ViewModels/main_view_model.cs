using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using PostmanClone.Data.Services;

namespace PostmanClone.App.ViewModels;

public partial class main_view_model : ObservableObject
{
    private readonly i_collection_repository _collection_repository;

    [ObservableProperty]
    private request_editor_view_model _requestEditor;

    [ObservableProperty]
    private response_viewer_view_model _responseViewer;

    [ObservableProperty]
    private sidebar_view_model _sidebar;

    [ObservableProperty]
    private environment_selector_view_model _environmentSelector;

    [ObservableProperty]
    private script_editor_view_model _scriptEditor;

    [ObservableProperty]
    private test_results_view_model _testResults;

    [ObservableProperty]
    private string _title = "PostmanClone";

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    public main_view_model(
        request_editor_view_model request_editor,
        response_viewer_view_model response_viewer,
        sidebar_view_model sidebar,
        environment_selector_view_model environment_selector,
        script_editor_view_model script_editor,
        test_results_view_model test_results,
        i_collection_repository collection_repository)
    {
        _requestEditor = request_editor;
        _responseViewer = response_viewer;
        _sidebar = sidebar;
        _environmentSelector = environment_selector;
        _scriptEditor = script_editor;
        _testResults = test_results;
        _collection_repository = collection_repository;

        // Wire up events
        _requestEditor.execution_completed += on_execution_completed;
        _requestEditor.request_saved += on_request_saved;
        _sidebar.request_selected += on_request_selected;
        _sidebar.request_with_collection_selected += on_request_with_collection_selected;
    }

    [RelayCommand]
    public async Task initialize_async(CancellationToken cancellation_token)
    {
        await Sidebar.load_data_async(cancellation_token);
        await EnvironmentSelector.load_environments_async(cancellation_token);
    }

    [RelayCommand]
    private void toggle_sidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
    }

    [RelayCommand]
    private async Task OpenImportDialog()
    {
        // Dialog will be shown from the view
    }

    [RelayCommand]
    private async Task OpenExportDialog()
    {
        // Dialog will be shown from the view
    }

    private void on_execution_completed(object? sender, request_execution_result result)
    {
        if (result.response != null)
        {
            ResponseViewer.load_response(result.response);
        }
        
        // Update Test Results
        TestResults.ClearResultsCommand.Execute(null);
        foreach (var test in result.all_test_results)
        {
            TestResults.AddTestResult(test.name, test.passed, test.error_message);
        }
        
        _ = Sidebar.refresh_history_async(CancellationToken.None);
    }

    private void on_request_selected(object? sender, http_request_model request)
    {
        RequestEditor.load_request(request);
        ScriptEditor.LoadScriptsFromRequest(request);
        ResponseViewer.clear();
        TestResults.ClearResultsCommand.Execute(null);
    }

    private void on_request_with_collection_selected(object? sender, (http_request_model request, string collectionId) data)
    {
        RequestEditor.load_request(data.request, data.collectionId);
        ScriptEditor.LoadScriptsFromRequest(data.request);
        ResponseViewer.clear();
        TestResults.ClearResultsCommand.Execute(null);
    }

    private async void on_request_saved(object? sender, EventArgs e)
    {
        await Sidebar.load_data_async(CancellationToken.None);
    }

    public import_export_view_model CreateImportExportViewModel()
    {
        return new import_export_view_model(_collection_repository);
    }
}
