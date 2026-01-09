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
        _requestEditor.execution_started += on_execution_started;
        _requestEditor.execution_completed += on_execution_completed;
        _requestEditor.request_saved += on_request_saved;
        _sidebar.request_selected += on_request_selected;
        
        // Sync scripts from ScriptEditor to RequestEditor when they change
        _scriptEditor.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(script_editor_view_model.PreRequestScript))
                _requestEditor.PreRequestScript = _scriptEditor.PreRequestScript;
            else if (e.PropertyName == nameof(script_editor_view_model.PostResponseScript))
                _requestEditor.PostResponseScript = _scriptEditor.PostResponseScript;
        };
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

    private void on_execution_started(object? sender, EventArgs e)
    {
        // Clear console and test results before new execution
        ScriptEditor.ClearConsoleCommand.Execute(null);
        TestResults.ClearResultsCommand.Execute(null);
        ResponseViewer.clear();
        
        ScriptEditor.AppendConsoleOutput("═══════════════════════════════════════");
        ScriptEditor.AppendConsoleOutput("Starting request execution...");
        ScriptEditor.AppendConsoleOutput("═══════════════════════════════════════");
    }

    private void on_execution_completed(object? sender, request_execution_result result)
    {
        // Show pre-script results
        if (result.pre_script_result != null)
        {
            ScriptEditor.AppendConsoleOutput("");
            ScriptEditor.AppendConsoleOutput("▶ PRE-REQUEST SCRIPT");
            ScriptEditor.AppendConsoleOutput("───────────────────────────────────────");
            
            foreach (var log in result.pre_script_result.logs)
            {
                ScriptEditor.AppendConsoleOutput($"  {log}");
            }
            
            foreach (var error in result.pre_script_result.errors)
            {
                ScriptEditor.AppendConsoleOutput($"  [ERROR] {error}");
            }
            
            var pre_status = result.pre_script_result.success ? "✓ completed" : "✗ failed";
            ScriptEditor.AppendConsoleOutput($"  Script {pre_status} ({result.pre_script_result.execution_time_ms}ms)");
        }
        
        // Show HTTP request result
        ScriptEditor.AppendConsoleOutput("");
        ScriptEditor.AppendConsoleOutput("▶ HTTP REQUEST");
        ScriptEditor.AppendConsoleOutput("───────────────────────────────────────");
        
        if (result.response != null)
        {
            ResponseViewer.load_response(result.response);
            ScriptEditor.AppendConsoleOutput($"  Status: {result.response.status_code} {result.response.status_description}");
            ScriptEditor.AppendConsoleOutput($"  Time: {result.response.elapsed_ms}ms");
            ScriptEditor.AppendConsoleOutput($"  Size: {result.response.size_bytes} bytes");
        }
        else
        {
            ScriptEditor.AppendConsoleOutput("  [ERROR] No response received");
        }
        
        // Show post-script results
        if (result.post_script_result != null)
        {
            ScriptEditor.AppendConsoleOutput("");
            ScriptEditor.AppendConsoleOutput("▶ POST-RESPONSE SCRIPT");
            ScriptEditor.AppendConsoleOutput("───────────────────────────────────────");
            
            foreach (var log in result.post_script_result.logs)
            {
                ScriptEditor.AppendConsoleOutput($"  {log}");
            }
            
            foreach (var error in result.post_script_result.errors)
            {
                ScriptEditor.AppendConsoleOutput($"  [ERROR] {error}");
            }
            
            var post_status = result.post_script_result.success ? "✓ completed" : "✗ failed";
            ScriptEditor.AppendConsoleOutput($"  Script {post_status} ({result.post_script_result.execution_time_ms}ms)");
        }
        
        // Show test results summary
        if (result.all_test_results.Count > 0)
        {
            ScriptEditor.AppendConsoleOutput("");
            ScriptEditor.AppendConsoleOutput("▶ TEST RESULTS");
            ScriptEditor.AppendConsoleOutput("───────────────────────────────────────");
            
            foreach (var test in result.all_test_results)
            {
                var icon = test.passed ? "✓" : "✗";
                ScriptEditor.AppendConsoleOutput($"  {icon} {test.name}");
                if (!test.passed && !string.IsNullOrEmpty(test.error_message))
                {
                    ScriptEditor.AppendConsoleOutput($"    Error: {test.error_message}");
                }
            }
        }
        
        // Update Test Results panel
        foreach (var test in result.all_test_results)
        {
            TestResults.AddTestResult(test.name, test.passed, test.error_message);
        }
        
        ScriptEditor.AppendConsoleOutput("");
        ScriptEditor.AppendConsoleOutput("═══════════════════════════════════════");
        ScriptEditor.AppendConsoleOutput("Execution completed");
        ScriptEditor.AppendConsoleOutput("═══════════════════════════════════════");
        
        _ = Sidebar.refresh_history_async(CancellationToken.None);
    }

    private void on_request_selected(object? sender, http_request_model request)
    {
        RequestEditor.load_request(request);
        ScriptEditor.LoadScriptsFromRequest(request);
        ResponseViewer.clear();
        TestResults.ClearResultsCommand.Execute(null);
    }

    private void on_request_with_collection_selected(object? sender, (http_request_model request, string collectionId, string collectionItemId) data)
    {
        RequestEditor.load_request(data.request, data.collectionId, data.collectionItemId);
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
