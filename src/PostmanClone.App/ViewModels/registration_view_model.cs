using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using System.Threading.Tasks;

namespace PostmanClone.App.ViewModels;

public partial class registration_view_model : ObservableObject
{
    private readonly i_app_registration_store _registration_store;

    [ObservableProperty]
    private string _userEmail = string.Empty;

    [ObservableProperty]
    private string _userName = string.Empty;

    [ObservableProperty]
    private string _organization = string.Empty;

    [ObservableProperty]
    private bool _optedIn = true;

    [ObservableProperty]
    private bool _isSaving = false;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public registration_view_model(i_app_registration_store registration_store)
    {
        _registration_store = registration_store;
    }

    public async Task load_existing_registration_async()
    {
        var existing = await _registration_store.get_registration_async();
        if (existing != null)
        {
            UserEmail = existing.user_email;
            UserName = existing.user_name;
            Organization = existing.organization;
            OptedIn = existing.opted_in;
        }
    }

    [RelayCommand]
    public async Task save_registration_async()
    {
        IsSaving = true;
        StatusMessage = string.Empty;

        try
        {
            var is_registered = await _registration_store.is_registered_async();
            
            var registration = new app_registration_model
            {
                id = Guid.NewGuid().ToString(),
                user_email = UserEmail,
                user_name = UserName,
                organization = Organization,
                opted_in = OptedIn,
                registered_at = DateTime.UtcNow,
                last_updated_at = is_registered ? DateTime.UtcNow : null
            };

            if (is_registered)
            {
                var existing = await _registration_store.get_registration_async();
                if (existing != null)
                {
                    registration = registration with { id = existing.id };
                    await _registration_store.update_registration_async(registration);
                }
            }
            else
            {
                await _registration_store.save_registration_async(registration);
            }

            StatusMessage = "Registration saved successfully!";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving registration: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    [RelayCommand]
    public async Task skip_registration_async()
    {
        // Save a minimal registration record indicating the user opted out
        var registration = new app_registration_model
        {
            id = Guid.NewGuid().ToString(),
            user_email = string.Empty,
            user_name = string.Empty,
            organization = string.Empty,
            opted_in = false,
            registered_at = DateTime.UtcNow
        };

        await _registration_store.save_registration_async(registration);
    }
}
