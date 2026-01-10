using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces;
using Core.Models;

namespace App.ViewModels;

public partial class environments_panel_view_model : ObservableObject
{
    private readonly i_environment_store _environmentStore;

    [ObservableProperty]
    private ObservableCollection<environment_list_item> _environments = new();

    [ObservableProperty]
    private environment_list_item? _selectedEnvironment;

    [ObservableProperty]
    private environment_list_item? _activeEnvironment;

    public event EventHandler<environment_list_item>? environment_selected;
    public event EventHandler<environment_list_item>? environment_activated;
    public event EventHandler? create_environment_requested;

    public environments_panel_view_model(i_environment_store environmentStore)
    {
        _environmentStore = environmentStore;
    }

    public async Task LoadEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        if (_environmentStore == null) return;

        var envs = await _environmentStore.list_all_async(cancellationToken);
        Environments.Clear();
        foreach (var env in envs)
        {
            Environments.Add(new environment_list_item
            {
                Id = env.id,
                Name = env.name,
                IsActive = ActiveEnvironment?.Id == env.id
            });
        }
    }

    private bool _suppressSelectionEvent;

    partial void OnSelectedEnvironmentChanged(environment_list_item? value)
    {
        if (value != null && !_suppressSelectionEvent)
        {
            environment_selected?.Invoke(this, value);
        }
    }

    [RelayCommand]
    private void SelectEnvironment(environment_list_item? env)
    {
        SelectedEnvironment = env;
    }

    /// <summary>
    /// Selects an environment by ID without triggering the open tab event.
    /// Used when switching tabs to sync the left nav selection.
    /// </summary>
    public void SelectById(string? environmentId)
    {
        if (string.IsNullOrEmpty(environmentId))
        {
            _suppressSelectionEvent = true;
            SelectedEnvironment = null;
            _suppressSelectionEvent = false;
            return;
        }

        var env = Environments.FirstOrDefault(e => e.Id == environmentId);
        if (env != null)
        {
            _suppressSelectionEvent = true;
            SelectedEnvironment = env;
            _suppressSelectionEvent = false;
        }
    }

    [RelayCommand]
    private void ActivateEnvironment(environment_list_item? env)
    {
        if (env == null) return;

        // Deactivate previous
        if (ActiveEnvironment != null)
        {
            ActiveEnvironment.IsActive = false;
        }

        // Activate new
        env.IsActive = true;
        ActiveEnvironment = env;
        environment_activated?.Invoke(this, env);
    }

    [RelayCommand]
    private void CreateEnvironment()
    {
        create_environment_requested?.Invoke(this, EventArgs.Empty);
    }
}

public partial class environment_list_item : ObservableObject
{
    public string Id { get; set; } = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isActive;
}
