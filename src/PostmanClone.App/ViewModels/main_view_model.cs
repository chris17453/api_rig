using Avalonia;
using Avalonia.Styling;
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
    private tabs_view_model _tabs;

    [ObservableProperty]
    private string _title = "PostmanClone";

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    [ObservableProperty]
    private bool _isDarkTheme = true;

    public main_view_model(
        request_editor_view_model request_editor,
        response_viewer_view_model response_viewer,
        sidebar_view_model sidebar,
        environment_selector_view_model environment_selector,
        script_editor_view_model script_editor,
        test_results_view_model test_results,
        tabs_view_model tabs,
        i_collection_repository collection_repository)
    {
        _requestEditor = request_editor;
        _responseViewer = response_viewer;
        _sidebar = sidebar;
        _environmentSelector = environment_selector;
        _scriptEditor = script_editor;
        _testResults = test_results;
        _tabs = tabs;
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
            {
                _requestEditor.PreRequestScript = _scriptEditor.PreRequestScript;
                sync_editor_to_active_tab();
            }
            else if (e.PropertyName == nameof(script_editor_view_model.PostResponseScript))
            {
                _requestEditor.PostResponseScript = _scriptEditor.PostResponseScript;
                sync_editor_to_active_tab();
            }
        };
        _sidebar.request_with_collection_selected += on_request_with_collection_selected;
        
        // Wire up tab events
        _tabs.tab_activated += on_tab_activated;
        _tabs.tab_closed += on_tab_closed;
        
        // Wire up request editor property changes to track unsaved changes
        _requestEditor.PropertyChanged += on_request_editor_property_changed;
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
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = IsDarkTheme 
                ? ThemeVariant.Dark 
                : ThemeVariant.Light;
        }
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
            Tabs.set_active_tab_response(result.response);
            ScriptEditor.AppendConsoleOutput($"  Status: {result.response.status_code} {result.response.status_description}");
            ScriptEditor.AppendConsoleOutput($"  Time: {result.response.elapsed_ms}ms");
            ScriptEditor.AppendConsoleOutput($"  Size: {result.response.size_bytes} bytes");
        }
        else
        {
            Tabs.set_active_tab_response(null);
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
        // Open in a new tab (history items don't have collection info)
        var tab = Tabs.open_request(request, null, null);
        load_tab_into_editor(tab);
    }

    private void on_request_with_collection_selected(object? sender, (http_request_model request, string collectionId, string collectionItemId) data)
    {
        // Open in a new tab or activate existing tab
        var tab = Tabs.open_request(data.request, data.collectionId, data.collectionItemId);
        load_tab_into_editor(tab);
    }

    private async void on_request_saved(object? sender, EventArgs e)
    {
        // Mark the active tab as saved
        Tabs.mark_active_tab_saved(RequestEditor.CurrentCollectionId, RequestEditor.CurrentRequestId);
        await Sidebar.load_data_async(CancellationToken.None);
    }
    
    private void on_tab_activated(object? sender, tab_state tab)
    {
        load_tab_into_editor(tab);
    }
    
    private void on_tab_closed(object? sender, tab_state tab)
    {
        // If there are remaining tabs, the tabs_view_model will activate another one
        // which will trigger on_tab_activated
    }
    
    private void on_request_editor_property_changed(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Track changes for unsaved indicator
        var trackableProperties = new[] 
        { 
            nameof(request_editor_view_model.RequestName),
            nameof(request_editor_view_model.Url),
            nameof(request_editor_view_model.SelectedMethod),
            nameof(request_editor_view_model.RequestBody)
        };
        
        if (trackableProperties.Contains(e.PropertyName))
        {
            sync_editor_to_active_tab();
        }
    }
    
    private void sync_editor_to_active_tab()
    {
        if (Tabs.ActiveTab == null) return;
        
        var headers = RequestEditor.Headers
            .Where(h => !string.IsNullOrWhiteSpace(h.Key))
            .Select(h => new key_value_pair_model { key = h.Key, value = h.Value, enabled = h.IsEnabled })
            .ToList();
        
        Tabs.update_active_tab_state(
            RequestEditor.RequestName,
            RequestEditor.Url,
            RequestEditor.SelectedMethod,
            RequestEditor.RequestBody,
            RequestEditor.PreRequestScript,
            RequestEditor.PostResponseScript,
            headers);
    }
    
    private void load_tab_into_editor(tab_state tab)
    {
        var request = tab.to_request_model();
        RequestEditor.load_request(request, tab.CollectionId, tab.CollectionItemId);
        ScriptEditor.LoadScriptsFromRequest(request);
        
        // Load response if available
        if (tab.LastResponse != null)
        {
            ResponseViewer.load_response(tab.LastResponse);
        }
        else
        {
            ResponseViewer.clear();
        }
        
        TestResults.ClearResultsCommand.Execute(null);
    }

    public import_export_view_model CreateImportExportViewModel()
    {
        return new import_export_view_model(_collection_repository);
    }
}
