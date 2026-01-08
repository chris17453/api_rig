using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using System.Collections.ObjectModel;

namespace PostmanClone.App.ViewModels;

public partial class environment_selector_view_model : ObservableObject
{
    private readonly i_environment_store _environment_store;

    [ObservableProperty]
    private ObservableCollection<environment_item_view_model> _environments = new();

    [ObservableProperty]
    private environment_item_view_model? _selectedEnvironment;

    [ObservableProperty]
    private bool _isLoading;

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
            }
            else
            {
                SelectedEnvironment = Environments.FirstOrDefault();
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
        }
    }

    private async Task set_active_environment_async(string? id)
    {
        await _environment_store.set_active_async(id, CancellationToken.None);
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
