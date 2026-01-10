using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces;
using Core.Models;

namespace App.ViewModels;

public partial class vault_panel_view_model : ObservableObject
{
    private readonly i_vault_store _vaultStore;

    [ObservableProperty]
    private ObservableCollection<vault_secret_item> _secrets = new();

    [ObservableProperty]
    private vault_secret_item? _selectedSecret;

    [ObservableProperty]
    private bool _isUnlocked;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    public event EventHandler<vault_secret_item>? secret_selected;
    public event EventHandler? create_secret_requested;
    public event EventHandler? unlock_vault_requested;
    public event EventHandler? lock_vault_requested;

    /// <summary>
    /// Raised when vault setup is needed (first time use). Handler should show setup dialog.
    /// </summary>
    public event EventHandler<TaskCompletionSource<string?>>? vault_setup_requested;

    /// <summary>
    /// Raised when vault unlock is needed. Handler should show unlock dialog.
    /// </summary>
    public event EventHandler<TaskCompletionSource<string?>>? vault_unlock_requested;

    public vault_panel_view_model(i_vault_store vaultStore)
    {
        _vaultStore = vaultStore;
    }

    /// <summary>
    /// Gets the vault store for dialog operations.
    /// </summary>
    public i_vault_store VaultStore => _vaultStore;

    /// <summary>
    /// Checks if the vault is initialized (has been set up with a key).
    /// </summary>
    public async Task<bool> CheckVaultInitializedAsync()
    {
        return await _vaultStore.is_vault_initialized_async();
    }

    private bool _suppressSelectionEvent;

    partial void OnSelectedSecretChanged(vault_secret_item? value)
    {
        if (value != null && !_suppressSelectionEvent)
        {
            secret_selected?.Invoke(this, value);
        }
    }

    /// <summary>
    /// Selects a secret by ID without triggering the open tab event.
    /// Used when switching tabs to sync the left nav selection.
    /// </summary>
    public void SelectById(string? secretId)
    {
        if (string.IsNullOrEmpty(secretId))
        {
            _suppressSelectionEvent = true;
            SelectedSecret = null;
            _suppressSelectionEvent = false;
            return;
        }

        var secret = Secrets.FirstOrDefault(s => s.Id == secretId);
        if (secret != null)
        {
            _suppressSelectionEvent = true;
            SelectedSecret = secret;
            _suppressSelectionEvent = false;
        }
    }

    [RelayCommand]
    private async Task LoadSecretsAsync()
    {
        if (_vaultStore == null || !IsUnlocked)
            return;

        try
        {
            IsLoading = true;
            Secrets.Clear();

            var secrets = await _vaultStore.get_all_async();
            foreach (var secret in secrets)
            {
                Secrets.Add(vault_secret_item.FromModel(secret));
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task SearchSecretsAsync()
    {
        if (_vaultStore == null || !IsUnlocked || string.IsNullOrWhiteSpace(SearchQuery))
        {
            await LoadSecretsAsync();
            return;
        }

        try
        {
            IsLoading = true;
            Secrets.Clear();

            var secrets = await _vaultStore.search_async(SearchQuery);
            foreach (var secret in secrets)
            {
                Secrets.Add(vault_secret_item.FromModel(secret));
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void SelectSecret(vault_secret_item? secret)
    {
        SelectedSecret = secret;
    }

    [RelayCommand]
    private void CreateSecret()
    {
        create_secret_requested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task UnlockVaultAsync()
    {
        try
        {
            IsLoading = true;

            // Check if vault needs initialization (first time setup)
            var isInitialized = await _vaultStore.is_vault_initialized_async();

            if (!isInitialized)
            {
                // First time - need to set up vault
                var setupTcs = new TaskCompletionSource<string?>();
                vault_setup_requested?.Invoke(this, setupTcs);

                var vaultKey = await setupTcs.Task;
                if (string.IsNullOrEmpty(vaultKey))
                {
                    // User cancelled setup
                    return;
                }

                // Save the verification token for this key
                await _vaultStore.initialize_vault_async(vaultKey);

                // Unlock with the key
                var setupSuccess = await _vaultStore.try_unlock_async(vaultKey);
                if (!setupSuccess)
                {
                    return;
                }
            }
            else
            {
                // Vault exists - prompt for key
                var unlockTcs = new TaskCompletionSource<string?>();
                vault_unlock_requested?.Invoke(this, unlockTcs);

                var vaultKey = await unlockTcs.Task;
                if (string.IsNullOrEmpty(vaultKey))
                {
                    // User cancelled
                    return;
                }

                var unlockSuccess = await _vaultStore.try_unlock_async(vaultKey);
                if (!unlockSuccess)
                {
                    // Key verification failed - dialog should have shown error
                    return;
                }
            }

            IsUnlocked = true;
            await LoadSecretsAsync();
            unlock_vault_requested?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void LockVault()
    {
        _vaultStore.lock_vault();
        IsUnlocked = false;
        Secrets.Clear();
        SelectedSecret = null;
        lock_vault_requested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private async Task DeleteSecretAsync(vault_secret_item? secret)
    {
        if (secret == null || _vaultStore == null)
            return;

        await _vaultStore.delete_async(secret.Id);
        Secrets.Remove(secret);

        if (SelectedSecret == secret)
        {
            SelectedSecret = null;
        }
    }

    public async Task AddSecretAsync(vault_secret_model secret)
    {
        if (_vaultStore == null)
            return;

        var created = await _vaultStore.create_async(secret);
        Secrets.Add(vault_secret_item.FromModel(created));
    }

    public async Task UpdateSecretAsync(vault_secret_model secret)
    {
        if (_vaultStore == null)
            return;

        var updated = await _vaultStore.update_async(secret);
        var existing = Secrets.FirstOrDefault(s => s.Id == secret.id);
        if (existing != null)
        {
            var index = Secrets.IndexOf(existing);
            Secrets[index] = vault_secret_item.FromModel(updated);
        }
    }
}

public partial class vault_secret_item : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private vault_secret_type _secretType = vault_secret_type.api_key;

    [ObservableProperty]
    private DateTime _createdAt = DateTime.UtcNow;

    [ObservableProperty]
    private DateTime? _lastUsedAt;

    [ObservableProperty]
    private DateTime? _expiresAt;

    [ObservableProperty]
    private List<string> _tags = new();

    public string SecretTypeDisplay => SecretType switch
    {
        vault_secret_type.api_key => "API Key",
        vault_secret_type.bearer_token => "Bearer Token",
        vault_secret_type.basic_auth => "Basic Auth",
        vault_secret_type.oauth2 => "OAuth 2.0",
        vault_secret_type.certificate => "Certificate",
        vault_secret_type.custom => "Custom",
        _ => "Unknown"
    };

    public string SecretTypeIcon => SecretType switch
    {
        vault_secret_type.api_key => "🔑",
        vault_secret_type.bearer_token => "🎫",
        vault_secret_type.basic_auth => "👤",
        vault_secret_type.oauth2 => "🔐",
        vault_secret_type.certificate => "📜",
        vault_secret_type.custom => "⚙",
        _ => "?"
    };

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    public static vault_secret_item FromModel(vault_secret_model model)
    {
        return new vault_secret_item
        {
            Id = model.id,
            Name = model.name,
            Description = model.description,
            SecretType = model.secret_type,
            CreatedAt = model.created_at,
            LastUsedAt = model.last_used_at,
            ExpiresAt = model.expires_at,
            Tags = model.tags
        };
    }

    public vault_secret_model ToModel()
    {
        return new vault_secret_model
        {
            id = Id,
            name = Name,
            description = Description,
            secret_type = SecretType,
            created_at = CreatedAt,
            last_used_at = LastUsedAt,
            expires_at = ExpiresAt,
            tags = Tags
        };
    }
}
