using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces;
using Core.Models;

namespace App.ViewModels;

public partial class environment_editor_view_model : ObservableObject
{
    private readonly i_environment_store? _environmentStore;

    [ObservableProperty]
    private string _environmentId = string.Empty;

    [ObservableProperty]
    private string _environmentName = string.Empty;

    [ObservableProperty]
    private ObservableCollection<env_variable_row_vm> _variables = new();

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    public event EventHandler? environment_saved;

    public environment_editor_view_model()
    {
    }

    public environment_editor_view_model(i_environment_store environmentStore)
    {
        _environmentStore = environmentStore;
    }

    public void LoadFromEnvironment(environment_model environment)
    {
        EnvironmentId = environment.id;
        EnvironmentName = environment.name;

        Variables.Clear();
        if (environment.variables != null)
        {
            foreach (var kvp in environment.variables)
            {
                Variables.Add(new env_variable_row_vm
                {
                    Key = kvp.Key,
                    Value = kvp.Value,
                    IsEnabled = true
                });
            }
        }

        // Add empty row for new entries
        Variables.Add(new env_variable_row_vm());
        HasUnsavedChanges = false;
    }

    public environment_model ToEnvironmentModel()
    {
        var varsDict = Variables
            .Where(v => !string.IsNullOrWhiteSpace(v.Key) && v.IsEnabled)
            .ToDictionary(v => v.Key, v => v.Value);

        return new environment_model
        {
            id = EnvironmentId,
            name = EnvironmentName,
            variables = varsDict
        };
    }

    [RelayCommand]
    private void AddVariable()
    {
        Variables.Add(new env_variable_row_vm());
        HasUnsavedChanges = true;
    }

    [RelayCommand]
    private void RemoveVariable(env_variable_row_vm variable)
    {
        if (variable != null)
        {
            Variables.Remove(variable);
            HasUnsavedChanges = true;
        }
    }

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (_environmentStore == null) return;

        if (string.IsNullOrWhiteSpace(EnvironmentName))
        {
            ErrorMessage = "Environment name is required";
            return;
        }

        // Check for duplicate names
        var allEnvironments = await _environmentStore.list_all_async(cancellationToken);
        var duplicate = allEnvironments.FirstOrDefault(e =>
            e.name.Equals(EnvironmentName, StringComparison.OrdinalIgnoreCase) &&
            e.id != EnvironmentId);

        if (duplicate != null)
        {
            ErrorMessage = $"An environment named '{EnvironmentName}' already exists";
            return;
        }

        ErrorMessage = string.Empty;
        var environment = ToEnvironmentModel();
        await _environmentStore.save_async(environment, cancellationToken);
        HasUnsavedChanges = false;
        environment_saved?.Invoke(this, EventArgs.Empty);
    }
}

public partial class env_variable_row_vm : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;
}
