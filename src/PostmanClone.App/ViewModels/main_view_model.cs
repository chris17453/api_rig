using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using PostmanClone.Data.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PostmanClone.App.ViewModels;

public record MainViewDependencies(
    request_editor_view_model request_editor,
    response_viewer_view_model response_viewer,
    sidebar_view_model sidebar,
    environment_selector_view_model environment_selector,
    script_editor_view_model script_editor,
    test_results_view_model test_results,
    tabs_view_model tabs);

public partial class main_view_model : ObservableObject
{
    private const string heavy_separator = "═══════════════════════════════════════";
    private const string light_separator = "───────────────────────────────────────";

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
        MainViewDependencies dependencies,
        i_collection_repository collection_repository)
    {
        _requestEditor = dependencies.request_editor;
        _responseViewer = dependencies.response_viewer;
        _sidebar = dependencies.sidebar;
        _environmentSelector = dependencies.environment_selector;
        _scriptEditor = dependencies.script_editor;
        _testResults = dependencies.test_results;
        _tabs = dependencies.tabs;
        _collection_repository = collection_repository;

        // Wire up events
        _requestEditor.execution_started += (_, _) => on_execution_started();
        _requestEditor.execution_completed += async (_, result) => await on_execution_completed(result);
        _requestEditor.request_saved += async (_, _) => await on_request_saved_async();
        _sidebar.request_selected += (_, request) => on_request_selected(request);
        
        // Sync scripts from ScriptEditor to RequestEditor when they change
        _scriptEditor.PropertyChanged += (_, e) =>
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
        _sidebar.request_with_collection_selected += (_, data) => on_request_with_collection_selected(data);
        
        // Wire up tab events
        _tabs.tab_activated += (_, tab) => on_tab_activated(tab);
        _tabs.tab_closed += (_, tab) => on_tab_closed(tab);
        
        // Wire up request editor property changes to track unsaved changes
        _requestEditor.PropertyChanged += (_, e) => on_request_editor_property_changed(e);
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

    private void on_execution_started()
    {
        // Clear console and test results before new execution
        ScriptEditor.ClearConsoleCommand.Execute(null);
        TestResults.ClearResultsCommand.Execute(null);
        ResponseViewer.clear();
        
        ScriptEditor.AppendConsoleOutput(heavy_separator);
        ScriptEditor.AppendConsoleOutput("Starting request execution...");
        ScriptEditor.AppendConsoleOutput(heavy_separator);
    }

    private async Task on_execution_completed(request_execution_result result)
    {
        log_pre_request(result.pre_script_result);
        log_http_request(result.response);
        log_post_request(result.post_script_result);
        log_test_results(result.all_test_results);
        update_test_results_panel(result.all_test_results);
        log_completion();

        await Sidebar.refresh_history_async(CancellationToken.None);
    }

    private void log_pre_request(script_execution_result_model? pre_script_result)
    {
        if (pre_script_result == null) return;

        ScriptEditor.AppendConsoleOutput(string.Empty);
        ScriptEditor.AppendConsoleOutput("▶ PRE-REQUEST SCRIPT");
        ScriptEditor.AppendConsoleOutput(light_separator);

        foreach (var log in pre_script_result.logs)
        {
            ScriptEditor.AppendConsoleOutput($"  {log}");
        }

        foreach (var error in pre_script_result.errors)
        {
            ScriptEditor.AppendConsoleOutput($"  [ERROR] {error}");
        }

        var pre_status = pre_script_result.success ? "✓ completed" : "✗ failed";
        ScriptEditor.AppendConsoleOutput($"  Script {pre_status} ({pre_script_result.execution_time_ms}ms)");
    }

    private void log_http_request(http_response_model? response)
    {
        ScriptEditor.AppendConsoleOutput(string.Empty);
        ScriptEditor.AppendConsoleOutput("▶ HTTP REQUEST");
        ScriptEditor.AppendConsoleOutput(light_separator);

        if (response != null)
        {
            ResponseViewer.load_response(response);
            Tabs.set_active_tab_response(response);
            ScriptEditor.AppendConsoleOutput($"  Status: {response.status_code} {response.status_description}");
            ScriptEditor.AppendConsoleOutput($"  Time: {response.elapsed_ms}ms");
            ScriptEditor.AppendConsoleOutput($"  Size: {response.size_bytes} bytes");
        }
        else
        {
            Tabs.set_active_tab_response(null);
            ScriptEditor.AppendConsoleOutput("  [ERROR] No response received");
        }
    }

    private void log_post_request(script_execution_result_model? post_script_result)
    {
        if (post_script_result == null) return;

        ScriptEditor.AppendConsoleOutput(string.Empty);
        ScriptEditor.AppendConsoleOutput("▶ POST-RESPONSE SCRIPT");
        ScriptEditor.AppendConsoleOutput(light_separator);

        foreach (var log in post_script_result.logs)
        {
            ScriptEditor.AppendConsoleOutput($"  {log}");
        }

        foreach (var error in post_script_result.errors)
        {
            ScriptEditor.AppendConsoleOutput($"  [ERROR] {error}");
        }

        var post_status = post_script_result.success ? "✓ completed" : "✗ failed";
        ScriptEditor.AppendConsoleOutput($"  Script {post_status} ({post_script_result.execution_time_ms}ms)");
    }

    private void log_test_results(IReadOnlyList<test_result_model> all_test_results)
    {
        if (all_test_results.Count == 0) return;

        ScriptEditor.AppendConsoleOutput(string.Empty);
        ScriptEditor.AppendConsoleOutput("▶ TEST RESULTS");
        ScriptEditor.AppendConsoleOutput(light_separator);

        foreach (var test in all_test_results)
        {
            var icon = test.passed ? "✓" : "✗";
            ScriptEditor.AppendConsoleOutput($"  {icon} {test.name}");
            if (!test.passed && !string.IsNullOrEmpty(test.error_message))
            {
                ScriptEditor.AppendConsoleOutput($"    Error: {test.error_message}");
            }
        }
    }

    private void update_test_results_panel(IReadOnlyList<test_result_model> all_test_results)
    {
        foreach (var test in all_test_results)
        {
            TestResults.AddTestResult(test.name, test.passed, test.error_message);
        }
    }

    private void log_completion()
    {
        ScriptEditor.AppendConsoleOutput(string.Empty);
        ScriptEditor.AppendConsoleOutput(heavy_separator);
        ScriptEditor.AppendConsoleOutput("Execution completed");
        ScriptEditor.AppendConsoleOutput(heavy_separator);
    }

    private void on_request_selected(http_request_model request)
    {
        // Open in a new tab (history items don't have collection info)
        var tab = Tabs.open_request(request, null, null);
        load_tab_into_editor(tab);
    }

    private void on_request_with_collection_selected((http_request_model request, string collectionId, string collectionItemId) data)
    {
        // Open in a new tab or activate existing tab
        var tab = Tabs.open_request(data.request, data.collectionId, data.collectionItemId);
        load_tab_into_editor(tab);
    }

    private async Task on_request_saved_async()
    {
        // Mark the active tab as saved
        Tabs.mark_active_tab_saved(RequestEditor.CurrentCollectionId, RequestEditor.CurrentRequestId);
        await Sidebar.load_data_async(CancellationToken.None);
    }
    
    private void on_tab_activated(tab_state tab)
    {
        load_tab_into_editor(tab);
    }
    
    private void on_tab_closed(tab_state tab)
    {
        // If there are remaining tabs, the tabs_view_model will activate another one
        // which will trigger on_tab_activated
    }
    
    private void on_request_editor_property_changed(System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Track changes for unsaved indicator
        var trackableProperties = new[] 
        { 
            nameof(request_editor_view_model.RequestName),
            nameof(request_editor_view_model.Url),
            nameof(request_editor_view_model.SelectedMethod),
            nameof(request_editor_view_model.RequestBody),
            nameof(request_editor_view_model.Headers)
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
