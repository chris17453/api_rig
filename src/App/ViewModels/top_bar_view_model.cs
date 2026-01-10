using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces;
using Core.Models;

namespace App.ViewModels;

public partial class top_bar_view_model : ObservableObject
{
    private i_workspace_store? _workspaceStore;

    [ObservableProperty]
    private string _currentWorkspace = "Default Workspace";

    [ObservableProperty]
    private string? _currentWorkspaceId;

    [ObservableProperty]
    private ObservableCollection<workspace_item> _workspaces = new();

    [ObservableProperty]
    private workspace_item? _selectedWorkspace;

    [ObservableProperty]
    private bool _isWorkspaceSelectorOpen;

    [ObservableProperty]
    private bool _hasNotifications;

    [ObservableProperty]
    private int _notificationCount;

    [ObservableProperty]
    private bool _isNotificationsPanelOpen;

    [ObservableProperty]
    private bool _isSettingsPanelOpen;

    [ObservableProperty]
    private string _newWorkspaceName = string.Empty;

    [ObservableProperty]
    private bool _isCreatingWorkspace;

    public event EventHandler? import_requested;
    public event EventHandler? export_requested;
    public event EventHandler? settings_requested;
    public event EventHandler<workspace_model>? workspace_changed;

    public void SetWorkspaceStore(i_workspace_store workspaceStore)
    {
        _workspaceStore = workspaceStore;
    }

    [RelayCommand]
    private async Task LoadWorkspacesAsync()
    {
        if (_workspaceStore == null)
            return;

        // Ensure default workspace exists
        await _workspaceStore.ensure_default_async();

        var workspaces = await _workspaceStore.get_all_async();
        Workspaces.Clear();

        foreach (var ws in workspaces)
        {
            var item = workspace_item.FromModel(ws);
            Workspaces.Add(item);

            if (ws.is_active)
            {
                CurrentWorkspace = ws.name;
                CurrentWorkspaceId = ws.id;
                SelectedWorkspace = item;
            }
        }
    }

    [RelayCommand]
    private void ToggleWorkspaceSelector()
    {
        IsWorkspaceSelectorOpen = !IsWorkspaceSelectorOpen;
        IsCreatingWorkspace = false;
        NewWorkspaceName = string.Empty;
    }

    [RelayCommand]
    private async Task SelectWorkspaceAsync(workspace_item? workspace)
    {
        if (workspace == null || _workspaceStore == null)
            return;

        await _workspaceStore.set_active_async(workspace.Id);
        CurrentWorkspace = workspace.Name;
        CurrentWorkspaceId = workspace.Id;
        SelectedWorkspace = workspace;
        IsWorkspaceSelectorOpen = false;

        // Update is_active on all workspace items
        foreach (var ws in Workspaces)
        {
            ws.IsActive = ws.Id == workspace.Id;
        }

        workspace_changed?.Invoke(this, workspace.ToModel());
    }

    [RelayCommand]
    private void ShowCreateWorkspace()
    {
        IsCreatingWorkspace = true;
        NewWorkspaceName = string.Empty;
    }

    [RelayCommand]
    private void CancelCreateWorkspace()
    {
        IsCreatingWorkspace = false;
        NewWorkspaceName = string.Empty;
    }

    [RelayCommand]
    private async Task CreateWorkspaceAsync()
    {
        if (string.IsNullOrWhiteSpace(NewWorkspaceName) || _workspaceStore == null)
            return;

        var newWorkspace = new workspace_model
        {
            name = NewWorkspaceName,
            description = string.Empty
        };

        var created = await _workspaceStore.create_async(newWorkspace);
        var item = workspace_item.FromModel(created);
        Workspaces.Add(item);

        // Automatically switch to the new workspace
        await SelectWorkspaceAsync(item);

        IsCreatingWorkspace = false;
        NewWorkspaceName = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteWorkspaceAsync(workspace_item? workspace)
    {
        if (workspace == null || _workspaceStore == null)
            return;

        // Don't allow deleting the last workspace
        if (Workspaces.Count <= 1)
            return;

        await _workspaceStore.delete_async(workspace.Id);
        Workspaces.Remove(workspace);

        // If we deleted the active workspace, switch to another one
        if (workspace.IsActive && Workspaces.Count > 0)
        {
            await SelectWorkspaceAsync(Workspaces[0]);
        }
    }

    [RelayCommand]
    private void Import()
    {
        import_requested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Export()
    {
        export_requested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Settings()
    {
        // Close all popups and open the full settings dialog
        IsSettingsPanelOpen = false;
        IsNotificationsPanelOpen = false;
        IsWorkspaceSelectorOpen = false;
        settings_requested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ToggleNotifications()
    {
        IsNotificationsPanelOpen = !IsNotificationsPanelOpen;
        IsSettingsPanelOpen = false;
        IsWorkspaceSelectorOpen = false;
    }

    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsPanelOpen = false;
    }

    [RelayCommand]
    private void CloseNotifications()
    {
        IsNotificationsPanelOpen = false;
    }

    public void AddNotification(string message)
    {
        NotificationCount++;
        HasNotifications = NotificationCount > 0;
    }

    public void ClearNotifications()
    {
        NotificationCount = 0;
        HasNotifications = false;
    }
}

public partial class workspace_item : ObservableObject
{
    public string Id { get; set; } = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _color = "#3B82F6";

    [ObservableProperty]
    private bool _isActive;

    public static workspace_item FromModel(workspace_model model)
    {
        return new workspace_item
        {
            Id = model.id,
            Name = model.name,
            Description = model.description,
            Color = model.color,
            IsActive = model.is_active
        };
    }

    public workspace_model ToModel()
    {
        return new workspace_model
        {
            id = Id,
            name = Name,
            description = Description,
            color = Color,
            is_active = IsActive
        };
    }
}
