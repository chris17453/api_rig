using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using System.Collections.ObjectModel;

namespace App.ViewModels;

/// <summary>
/// Event args for close confirmation request.
/// </summary>
public class CloseConfirmationEventArgs : EventArgs
{
    public tab_state Tab { get; }
    public bool AllowClose { get; set; } = true;
    public TaskCompletionSource<bool> Confirmation { get; } = new();

    public CloseConfirmationEventArgs(tab_state tab)
    {
        Tab = tab;
    }
}

/// <summary>
/// ViewModel for managing multiple request tabs.
/// </summary>
public partial class tabs_view_model : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<tab_state> _tabs = new();

    [ObservableProperty]
    private tab_state? _activeTab;

    public event EventHandler<tab_state>? tab_activated;
    public event EventHandler<tab_state>? tab_closed;
    public event EventHandler<tab_state>? tab_state_changed;
    public event EventHandler<CloseConfirmationEventArgs>? close_confirmation_requested;

    public tabs_view_model()
    {
        // Start with one empty tab
        var initialTab = tab_state.create_new();
        initialTab.IsActive = true;
        Tabs.Add(initialTab);
        ActiveTab = initialTab;
    }

    /// <summary>
    /// Opens a request in a new tab or activates existing tab if already open.
    /// </summary>
    public tab_state open_request(http_request_model request, string? sourceTabId = null, string? collectionId = null, string? collectionItemId = null, string? collectionName = null)
    {
        Console.WriteLine($"[TABS] open_request called: sourceTabId={sourceTabId ?? "null"}, collectionId={collectionId ?? "null"}, collectionItemId={collectionItemId ?? "null"}, url={request.url}");

        // First priority: Check if the source tab still exists (for history items)
        if (!string.IsNullOrEmpty(sourceTabId))
        {
            Console.WriteLine($"[TABS] Looking for tab by ID: {sourceTabId}");
            var tabById = find_tab_by_id(sourceTabId);
            if (tabById != null)
            {
                Console.WriteLine($"[TABS] Found tab by ID! Activating.");
                ActivateTab(tabById);
                return tabById;
            }
            Console.WriteLine($"[TABS] Tab with ID {sourceTabId} not found.");
        }

        // Second priority: Check if this request is already open in a tab (for collection items)
        if (!string.IsNullOrEmpty(collectionId) && !string.IsNullOrEmpty(collectionItemId))
        {
            Console.WriteLine($"[TABS] Searching for existing tab with collectionId={collectionId}, collectionItemId={collectionItemId}");
            var existingTab = find_tab_by_request(collectionId, collectionItemId);
            if (existingTab != null)
            {
                Console.WriteLine($"[TABS] Found existing tab by collection! Activating.");
                ActivateTab(existingTab);
                return existingTab;
            }
            Console.WriteLine($"[TABS] No existing tab found by collection.");
        }

        // Third priority: Find any tab with the same URL and method (prevents duplicate tabs)
        var matchingTab = find_tab_by_url_and_method(request.url, request.method);
        if (matchingTab != null)
        {
            Console.WriteLine($"[TABS] Found existing tab by URL+method! Activating tab {matchingTab.Id}");
            ActivateTab(matchingTab);
            return matchingTab;
        }

        // Check if we can reuse the current empty/new tab
        if (ActiveTab != null &&
            string.IsNullOrEmpty(ActiveTab.Url) &&
            !ActiveTab.HasUnsavedChanges &&
            string.IsNullOrEmpty(ActiveTab.CollectionId))
        {
            // Reuse the empty tab
            ActiveTab.RequestName = request.name;
            ActiveTab.Url = request.url;
            ActiveTab.SelectedMethod = request.method;
            ActiveTab.RequestBody = request.body?.raw_content ?? string.Empty;
            ActiveTab.PreRequestScript = request.pre_request_script ?? string.Empty;
            ActiveTab.PostResponseScript = request.post_response_script ?? string.Empty;
            ActiveTab.Headers = request.headers?.ToList() ?? new List<key_value_pair_model>();
            ActiveTab.QueryParams = request.query_params?.ToList() ?? new List<key_value_pair_model>();
            ActiveTab.CollectionId = collectionId;
            ActiveTab.CollectionItemId = collectionItemId;
            ActiveTab.CollectionName = collectionName;
            ActiveTab.save_original_state();

            return ActiveTab;
        }

        // Create new tab
        var newTab = tab_state.from_request(request, collectionId, collectionItemId, collectionName);
        Tabs.Add(newTab);
        ActivateTab(newTab);

        return newTab;
    }

    /// <summary>
    /// Creates a new empty tab.
    /// </summary>
    [RelayCommand]
    public void CreateNewTab()
    {
        var newTab = tab_state.create_new();
        Tabs.Add(newTab);
        ActivateTab(newTab);
    }

    /// <summary>
    /// Opens an environment in a new tab or activates existing tab if already open.
    /// </summary>
    public tab_state open_environment(environment_model environment)
    {
        // Check if environment is already open
        var existingTab = Tabs.FirstOrDefault(t =>
            t.ContentType == TabContentType.Environment &&
            t.EnvironmentId == environment.id);

        if (existingTab != null)
        {
            ActivateTab(existingTab);
            return existingTab;
        }

        // Create new environment tab
        var newTab = tab_state.from_environment(environment);
        Tabs.Add(newTab);
        ActivateTab(newTab);

        return newTab;
    }

    /// <summary>
    /// Creates a new environment tab.
    /// </summary>
    [RelayCommand]
    public void CreateNewEnvironmentTab()
    {
        var newTab = tab_state.create_new_environment();
        Tabs.Add(newTab);
        ActivateTab(newTab);
    }

    /// <summary>
    /// Creates a new vault secret tab.
    /// </summary>
    [RelayCommand]
    public void CreateNewVaultSecretTab()
    {
        var newTab = tab_state.create_vault_secret();
        Tabs.Add(newTab);
        ActivateTab(newTab);
    }

    /// <summary>
    /// Activates the specified tab.
    /// </summary>
    [RelayCommand]
    public void ActivateTab(tab_state tab)
    {
        if (tab == null || ActiveTab == tab) return;

        // Deactivate current tab
        if (ActiveTab != null)
        {
            ActiveTab.IsActive = false;
        }

        // Activate new tab
        tab.IsActive = true;
        ActiveTab = tab;
        
        tab_activated?.Invoke(this, tab);
    }

    /// <summary>
    /// Closes the specified tab with confirmation if unsaved.
    /// </summary>
    [RelayCommand]
    public async Task CloseTab(tab_state tab)
    {
        if (tab == null) return;

        var index = Tabs.IndexOf(tab);
        if (index < 0) return;

        // Check for unsaved changes and request confirmation
        if (tab.HasUnsavedChanges)
        {
            var args = new CloseConfirmationEventArgs(tab);
            close_confirmation_requested?.Invoke(this, args);

            // Wait for the UI to respond with confirmation
            var confirmed = await args.Confirmation.Task;
            if (!confirmed) return;
        }

        Tabs.Remove(tab);
        tab_closed?.Invoke(this, tab);

        // If we closed the active tab, activate another one
        if (tab.IsActive && Tabs.Count > 0)
        {
            // Activate the tab at the same index, or the last one if we closed the last tab
            var newIndex = Math.Min(index, Tabs.Count - 1);
            ActivateTab(Tabs[newIndex]);
        }
        else if (Tabs.Count == 0)
        {
            // Create a new empty tab if all tabs are closed
            CreateNewTab();
        }
    }

    /// <summary>
    /// Force closes a tab without confirmation (used after user confirms).
    /// </summary>
    public void ForceCloseTab(tab_state tab)
    {
        if (tab == null) return;

        var index = Tabs.IndexOf(tab);
        if (index < 0) return;

        Tabs.Remove(tab);
        tab_closed?.Invoke(this, tab);

        if (tab.IsActive && Tabs.Count > 0)
        {
            var newIndex = Math.Min(index, Tabs.Count - 1);
            ActivateTab(Tabs[newIndex]);
        }
        else if (Tabs.Count == 0)
        {
            CreateNewTab();
        }
    }

    /// <summary>
    /// Finds a tab by its unique ID.
    /// </summary>
    public tab_state? find_tab_by_id(string? tabId)
    {
        if (string.IsNullOrEmpty(tabId))
            return null;

        return Tabs.FirstOrDefault(t => t.Id == tabId);
    }

    /// <summary>
    /// Finds a tab by its collection and item IDs.
    /// </summary>
    public tab_state? find_tab_by_request(string? collectionId, string? collectionItemId)
    {
        if (string.IsNullOrEmpty(collectionId) || string.IsNullOrEmpty(collectionItemId))
            return null;

        return Tabs.FirstOrDefault(t =>
            t.CollectionId == collectionId &&
            t.CollectionItemId == collectionItemId);
    }

    /// <summary>
    /// Finds a tab by URL and HTTP method (fallback matching for history items).
    /// </summary>
    public tab_state? find_tab_by_url_and_method(string? url, http_method method)
    {
        if (string.IsNullOrEmpty(url))
            return null;

        return Tabs.FirstOrDefault(t =>
            t.Url == url &&
            t.SelectedMethod == method);
    }

    /// <summary>
    /// Updates the active tab's state from the request editor.
    /// </summary>
    public void update_active_tab_state(
        string requestName,
        string url,
        http_method method,
        string requestBody,
        string preRequestScript,
        string postResponseScript,
        List<key_value_pair_model> headers)
    {
        if (ActiveTab == null) return;

        ActiveTab.RequestName = requestName;
        ActiveTab.Url = url;
        ActiveTab.SelectedMethod = method;
        ActiveTab.RequestBody = requestBody;
        ActiveTab.PreRequestScript = preRequestScript;
        ActiveTab.PostResponseScript = postResponseScript;
        ActiveTab.Headers = headers;
        ActiveTab.Title = requestName;

        ActiveTab.check_for_changes();
        tab_state_changed?.Invoke(this, ActiveTab);
    }

    /// <summary>
    /// Marks the active tab as saved (no unsaved changes).
    /// </summary>
    public void mark_active_tab_saved(string? collectionId = null, string? collectionItemId = null)
    {
        if (ActiveTab == null) return;

        if (!string.IsNullOrEmpty(collectionId))
            ActiveTab.CollectionId = collectionId;
        
        if (!string.IsNullOrEmpty(collectionItemId))
            ActiveTab.CollectionItemId = collectionItemId;

        ActiveTab.save_original_state();
    }

    /// <summary>
    /// Updates the response for the active tab.
    /// </summary>
    public void set_active_tab_response(http_response_model? response)
    {
        if (ActiveTab == null) return;
        ActiveTab.LastResponse = response;
    }

    /// <summary>
    /// Gets whether any tab has unsaved changes.
    /// </summary>
    public bool has_any_unsaved_changes()
    {
        return Tabs.Any(t => t.HasUnsavedChanges);
    }

    /// <summary>
    /// Gets all tabs with unsaved changes.
    /// </summary>
    public IEnumerable<tab_state> get_unsaved_tabs()
    {
        return Tabs.Where(t => t.HasUnsavedChanges);
    }
}
