using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces;
using Core.Models;
using System.Collections.ObjectModel;

namespace App.ViewModels;

public partial class environment_selector_view_model : ObservableObject
{
    private readonly i_environment_store _environment_store;

    [ObservableProperty]
    private ObservableCollection<environment_item_view_model> _environments = new();

    [ObservableProperty]
    private environment_item_view_model? _selectedEnvironment;

    [ObservableProperty]
    private ObservableCollection<environment_variable_view_model> _variables = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isVariablesExpanded;

    [ObservableProperty]
    private bool _hasVariables;

    [ObservableProperty]
    private bool _isEditingName;

    [ObservableProperty]
    private string _editingEnvironmentName = string.Empty;

    [ObservableProperty]
    private string _newEnvironmentName = string.Empty;

    [ObservableProperty]
    private bool _isCreatingEnvironment;

    public environment_selector_view_model(i_environment_store environment_store)
    {
        _environment_store = environment_store;
    }

    [RelayCommand]
    public async Task load_environments_async(CancellationToken cancellation_token)
    {
        IsLoading = true;

        try
        {
            var envs = await _environment_store.list_all_async(cancellation_token);
            var active = await _environment_store.get_active_async(cancellation_token);

            Environments.Clear();
            
            // Add "No Environment" option
            Environments.Add(new environment_item_view_model
            {
                Id = null,
                Name = "No Environment"
            });

            foreach (var env in envs)
            {
                Environments.Add(new environment_item_view_model
                {
                    Id = env.id,
                    Name = env.name,
                    VariableCount = env.variables.Count
                });
            }

            // Set selected
            if (active != null)
            {
                SelectedEnvironment = Environments.FirstOrDefault(e => e.Id == active.id);
                await LoadVariablesAsync(active.id);
            }
            else
            {
                SelectedEnvironment = Environments.FirstOrDefault();
                Variables.Clear();
                HasVariables = false;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedEnvironmentChanged(environment_item_view_model? value)
    {
        if (value != null)
        {
            _ = set_active_environment_async(value.Id);
            _ = LoadVariablesAsync(value.Id);
        }
    }

    private async Task set_active_environment_async(string? id)
    {
        await _environment_store.set_active_async(id, CancellationToken.None);
    }

    private async Task LoadVariablesAsync(string? environmentId)
    {
        Variables.Clear();
        
        if (string.IsNullOrEmpty(environmentId))
        {
            HasVariables = false;
            return;
        }

        var env = await _environment_store.get_by_id_async(environmentId, CancellationToken.None);
        if (env?.variables != null)
        {
            foreach (var kvp in env.variables)
            {
                Variables.Add(new environment_variable_view_model
                {
                    Key = kvp.Key,
                    Value = kvp.Value
                });
            }
        }
        
        // Add empty row for new variable
        Variables.Add(new environment_variable_view_model { Key = "", Value = "" });
        HasVariables = Variables.Count > 1;
    }

    [RelayCommand]
    private void ToggleVariables()
    {
        IsVariablesExpanded = !IsVariablesExpanded;
    }

    [RelayCommand]
    private async Task SaveVariables()
    {
        if (SelectedEnvironment?.Id == null) return;

        var env = await _environment_store.get_by_id_async(SelectedEnvironment.Id, CancellationToken.None);
        if (env == null) return;

        var newVariables = new Dictionary<string, string>();
        foreach (var v in Variables.Where(v => !string.IsNullOrWhiteSpace(v.Key)))
        {
            newVariables[v.Key] = v.Value;
        }

        var updatedEnv = env with { variables = newVariables, updated_at = DateTime.UtcNow };
        await _environment_store.save_async(updatedEnv, CancellationToken.None);

        // Update variable count in dropdown
        var item = Environments.FirstOrDefault(e => e.Id == SelectedEnvironment.Id);
        if (item != null)
        {
            item.VariableCount = newVariables.Count;
        }

        // Reload to ensure empty row exists
        await LoadVariablesAsync(SelectedEnvironment.Id);
    }

    [RelayCommand]
    private async Task AddVariable()
    {
        // Add empty row if the last one has content
        var last = Variables.LastOrDefault();
        if (last != null && !string.IsNullOrWhiteSpace(last.Key))
        {
            Variables.Add(new environment_variable_view_model { Key = "", Value = "" });
        }
    }

    [RelayCommand]
    private void RemoveVariable(environment_variable_view_model variable)
    {
        if (Variables.Count > 1)
        {
            Variables.Remove(variable);
        }
    }

    [RelayCommand]
    private void StartCreateEnvironment()
    {
        NewEnvironmentName = string.Empty;
        IsCreatingEnvironment = true;
    }

    [RelayCommand]
    private void CancelCreateEnvironment()
    {
        IsCreatingEnvironment = false;
        NewEnvironmentName = string.Empty;
    }

    [RelayCommand]
    private async Task CreateEnvironment()
    {
        if (string.IsNullOrWhiteSpace(NewEnvironmentName)) return;

        var newEnv = new environment_model
        {
            id = Guid.NewGuid().ToString(),
            name = NewEnvironmentName.Trim(),
            variables = new Dictionary<string, string>(),
            created_at = DateTime.UtcNow,
            updated_at = DateTime.UtcNow
        };

        await _environment_store.save_async(newEnv, CancellationToken.None);
        
        IsCreatingEnvironment = false;
        NewEnvironmentName = string.Empty;

        // Reload and select the new environment
        await load_environments_async(CancellationToken.None);
        SelectedEnvironment = Environments.FirstOrDefault(e => e.Id == newEnv.id);
    }

    [RelayCommand]
    private void StartRenameEnvironment()
    {
        if (SelectedEnvironment?.Id == null) return;
        EditingEnvironmentName = SelectedEnvironment.Name;
        IsEditingName = true;
    }

    [RelayCommand]
    private void CancelRenameEnvironment()
    {
        IsEditingName = false;
        EditingEnvironmentName = string.Empty;
    }

    [RelayCommand]
    private async Task RenameEnvironment()
    {
        if (SelectedEnvironment?.Id == null || string.IsNullOrWhiteSpace(EditingEnvironmentName)) return;

        var env = await _environment_store.get_by_id_async(SelectedEnvironment.Id, CancellationToken.None);
        if (env == null) return;

        var updatedEnv = env with { name = EditingEnvironmentName.Trim(), updated_at = DateTime.UtcNow };
        await _environment_store.save_async(updatedEnv, CancellationToken.None);

        // Update the display name
        SelectedEnvironment.Name = EditingEnvironmentName.Trim();
        
        IsEditingName = false;
        EditingEnvironmentName = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteEnvironment()
    {
        if (SelectedEnvironment?.Id == null) return;

        await _environment_store.delete_async(SelectedEnvironment.Id, CancellationToken.None);
        
        // Reload and select "No Environment"
        await load_environments_async(CancellationToken.None);
        SelectedEnvironment = Environments.FirstOrDefault();
    }
}

public partial class environment_item_view_model : ObservableObject
{
    [ObservableProperty]
    private string? _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _variableCount;
}

public partial class environment_variable_view_model : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;
}
