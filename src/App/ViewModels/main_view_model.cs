using Avalonia;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces;
using Core.Models;
using Data.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace App.ViewModels;

public record MainViewDependencies(
    request_editor_view_model request_editor,
    response_viewer_view_model response_viewer,
    sidebar_view_model sidebar,
    environment_selector_view_model environment_selector,
    script_editor_view_model script_editor,
    test_results_view_model test_results,
    tabs_view_model tabs,
    top_bar_view_model top_bar,
    icon_bar_view_model icon_bar,
    side_panel_view_model side_panel,
    bottom_bar_view_model bottom_bar,
    environment_editor_view_model environment_editor,
    console_panel_view_model console,
    vault_editor_view_model vault_editor,
    environments_panel_view_model environments_panel,
    history_panel_view_model history_panel,
    vault_panel_view_model vault_panel);

public partial class main_view_model : ObservableObject
{
    private const string heavy_separator = "═══════════════════════════════════════";
    private const string light_separator = "───────────────────────────────────────";

    private readonly i_collection_repository _collection_repository;
    private readonly i_environment_store _environment_store;

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
    private top_bar_view_model _topBar;

    [ObservableProperty]
    private icon_bar_view_model _iconBar;

    [ObservableProperty]
    private side_panel_view_model _sidePanel;

    [ObservableProperty]
    private bottom_bar_view_model _bottomBar;

    [ObservableProperty]
    private environment_editor_view_model _environmentEditor;

    [ObservableProperty]
    private console_panel_view_model _consolePanel;

    [ObservableProperty]
    private vault_editor_view_model _vaultEditor;

    private readonly environments_panel_view_model _environmentsPanel;
    private readonly vault_panel_view_model _vaultPanel;

    [ObservableProperty]
    private string _title = "API RIG";

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    [ObservableProperty]
    private bool _isDarkTheme = true;

    public event EventHandler? import_requested;
    public event EventHandler? export_requested;
    public event EventHandler? about_requested;
    public event EventHandler? settings_requested;

    public main_view_model(
        MainViewDependencies dependencies,
        i_collection_repository collection_repository,
        i_environment_store environment_store)
    {
        _requestEditor = dependencies.request_editor;
        _responseViewer = dependencies.response_viewer;
        _sidebar = dependencies.sidebar;
        _environmentSelector = dependencies.environment_selector;
        _scriptEditor = dependencies.script_editor;
        _testResults = dependencies.test_results;
        _tabs = dependencies.tabs;
        _topBar = dependencies.top_bar;
        _iconBar = dependencies.icon_bar;
        _sidePanel = dependencies.side_panel;
        _bottomBar = dependencies.bottom_bar;
        _environmentEditor = dependencies.environment_editor;
        _consolePanel = dependencies.console;
        _vaultEditor = dependencies.vault_editor;
        _environmentsPanel = dependencies.environments_panel;
        _vaultPanel = dependencies.vault_panel;
        _collection_repository = collection_repository;
        _environment_store = environment_store;

        // Wire up the side panel to use all sub-panels
        _sidePanel.CollectionsPanel = _sidebar;
        _sidePanel.EnvironmentsPanel = _environmentsPanel;
        _sidePanel.HistoryPanel = dependencies.history_panel;
        _sidePanel.VaultPanel = _vaultPanel;

        // Wire up events
        _requestEditor.execution_started += (_, _) => on_execution_started();
        _requestEditor.execution_completed += async (_, result) => await on_execution_completed(result);
        _requestEditor.request_saved += async (_, _) => await on_request_saved_async();
        _sidebar.request_selected += (_, data) => on_request_selected(data);

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
        _sidebar.collection_changed += async (_, _) => await _requestEditor.LoadCollectionsAsync();

        // Wire up tab events
        _tabs.tab_activated += (_, tab) => on_tab_activated(tab);
        _tabs.tab_closed += (_, tab) => on_tab_closed(tab);

        // Wire up request editor property changes to track unsaved changes
        _requestEditor.PropertyChanged += (_, e) => on_request_editor_property_changed(e);

        // Wire up icon bar events for panel switching
        _iconBar.panel_changed += (_, panelType) => _sidePanel.SwitchPanel(panelType);
        _iconBar.theme_toggled += (_, _) => ToggleTheme();
        _iconBar.about_requested += (_, _) => about_requested?.Invoke(this, EventArgs.Empty);

        // Wire up top bar events
        _topBar.import_requested += (_, _) => import_requested?.Invoke(this, EventArgs.Empty);
        _topBar.export_requested += (_, _) => export_requested?.Invoke(this, EventArgs.Empty);
        _topBar.settings_requested += (_, _) => settings_requested?.Invoke(this, EventArgs.Empty);

        // Wire up environments panel events
        _environmentsPanel.create_environment_requested += (_, _) => on_create_environment_requested();
        _environmentsPanel.environment_selected += (_, env) => on_environment_selected(env);

        // Wire up environment editor save to refresh the list and update tab title
        _environmentEditor.environment_saved += async (_, _) =>
        {
            await _environmentsPanel.LoadEnvironmentsAsync();
            // Update tab title to match the saved environment name
            if (Tabs.ActiveTab?.ContentType == TabContentType.Environment)
            {
                Tabs.ActiveTab.Title = _environmentEditor.EnvironmentName;
            }
        };

        // Wire up vault panel events
        _vaultPanel.create_secret_requested += (_, _) => on_create_secret_requested();
        _vaultPanel.secret_selected += (_, secret) => on_secret_selected(secret);

        // Wire up vault editor save to refresh the list
        _vaultEditor.secret_saved += async (_, _) =>
        {
            await _vaultPanel.LoadSecretsCommand.ExecuteAsync(null);
        };

        // Wire up history panel events
        dependencies.history_panel.history_entry_selected += (_, entry) => on_history_entry_selected(entry);

        // IMPORTANT: Load the initial tab immediately so CurrentTabId is set BEFORE any user action
        // This must happen synchronously in the constructor, not in async initialization
        if (_tabs.ActiveTab != null)
        {
            Console.WriteLine($"[MAIN] Constructor: Loading initial tab {_tabs.ActiveTab.Id}");
            load_tab_into_editor(_tabs.ActiveTab);
        }
    }

    [RelayCommand]
    public async Task initialize_async(CancellationToken cancellation_token)
    {
        await Sidebar.load_data_async(cancellation_token);
        await EnvironmentSelector.load_environments_async(cancellation_token);
        await RequestEditor.LoadCollectionsAsync(cancellation_token);
        await _environmentsPanel.LoadEnvironmentsAsync(cancellation_token);
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
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task OpenExportDialog()
    {
        // Dialog will be shown from the view
        await Task.CompletedTask;
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

    private void on_request_selected((http_request_model request, string? sourceTabId, string? collectionId, string? collectionItemId) data)
    {
        // Open in a new tab or activate existing tab - prioritize sourceTabId for history items
        Console.WriteLine($"[MAIN] on_request_selected: sourceTabId={data.sourceTabId ?? "null"}, collectionId={data.collectionId ?? "null"}, collectionItemId={data.collectionItemId ?? "null"}");
        var tab = Tabs.open_request(data.request, data.sourceTabId, data.collectionId, data.collectionItemId);
        load_tab_into_editor(tab);
    }

    private void on_request_with_collection_selected((http_request_model request, string collectionId, string collectionItemId, string collectionName) data)
    {
        // Open in a new tab or activate existing tab - no sourceTabId since this comes from collections
        var tab = Tabs.open_request(data.request, null, data.collectionId, data.collectionItemId, data.collectionName);
        load_tab_into_editor(tab);
    }

    private async Task on_request_saved_async()
    {
        // Mark the active tab as saved with collection info
        Tabs.mark_active_tab_saved(RequestEditor.CurrentCollectionId, RequestEditor.CurrentRequestId);
        await Sidebar.load_data_async(CancellationToken.None);

        // Sync sidebar selection to highlight the saved item
        Sidebar.SelectItemById(RequestEditor.CurrentCollectionId, RequestEditor.CurrentRequestId);
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
        Console.WriteLine($"[MAIN] load_tab_into_editor: tab.Id={tab.Id}, ContentType={tab.ContentType}, CollectionId={tab.CollectionId ?? "null"}");

        // Clear selections in all panels first, then select the appropriate one
        Sidebar.SelectItemById(null, null);
        _environmentsPanel.SelectById(null);
        _vaultPanel.SelectById(null);

        switch (tab.ContentType)
        {
            case TabContentType.Request:
                var request = tab.to_request_model();
                // Pass the tab ID so history entries can reference back to this tab
                RequestEditor.load_request(request, tab.CollectionId, tab.CollectionItemId, tab.Id, tab.CollectionName);
                ScriptEditor.LoadScriptsFromRequest(request);

                // Sync sidebar selection to highlight the correct collection item
                Sidebar.SelectItemById(tab.CollectionId, tab.CollectionItemId);

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
                break;

            case TabContentType.Environment:
                if (tab.Environment != null)
                {
                    EnvironmentEditor.LoadFromEnvironment(tab.Environment);
                }
                // Sync environment panel selection
                _environmentsPanel.SelectById(tab.EnvironmentId);
                break;

            case TabContentType.VaultSecret:
                // Sync vault panel selection
                _vaultPanel.SelectById(tab.VaultSecretId);
                break;

            case TabContentType.Settings:
                // Settings editor not implemented yet
                break;
        }
    }

    public import_export_view_model CreateImportExportViewModel()
    {
        return new import_export_view_model(_collection_repository);
    }

    private void on_create_environment_requested()
    {
        // Create a new environment and open it in a tab
        var newEnv = new environment_model
        {
            id = Guid.NewGuid().ToString(),
            name = "New Environment",
            variables = new Dictionary<string, string>()
        };

        // Open in environment editor tab
        var tab = Tabs.open_environment(newEnv);
        EnvironmentEditor.LoadFromEnvironment(newEnv);
    }

    private async void on_environment_selected(environment_list_item env)
    {
        // Fetch full environment from store
        var environment = await _environment_store.get_by_id_async(env.Id, CancellationToken.None);
        if (environment == null) return;

        // Open in environment editor tab
        var tab = Tabs.open_environment(environment);
        EnvironmentEditor.LoadFromEnvironment(environment);
    }

    private void on_create_secret_requested()
    {
        // Create a new secret tab
        Tabs.CreateNewVaultSecretTab();
        VaultEditor.CreateNew();
    }

    private void on_secret_selected(vault_secret_item secret)
    {
        // Load secret into editor tab
        var model = secret.ToModel();
        Tabs.CreateNewVaultSecretTab();
        VaultEditor.LoadFromSecret(model);
    }

    private void on_history_entry_selected(history_entry_model entry)
    {
        // Load the request from history into a new tab
        if (entry.request_snapshot != null)
        {
            var tab = Tabs.open_request(entry.request_snapshot, entry.source_tab_id, entry.collection_id, entry.collection_item_id);
            load_tab_into_editor(tab);
        }
    }
}
