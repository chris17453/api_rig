using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces;
using Core.Models;

namespace App.ViewModels;

public partial class vault_editor_view_model : ObservableObject
{
    private readonly i_vault_store _vaultStore;

    public vault_editor_view_model(i_vault_store vaultStore)
    {
        _vaultStore = vaultStore;
    }

    [ObservableProperty]
    private string _secretId = string.Empty;

    [ObservableProperty]
    private string _secretName = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private vault_secret_type _secretType = vault_secret_type.api_key;

    [ObservableProperty]
    private string _secretValue = string.Empty;

    [ObservableProperty]
    private bool _isValueVisible;

    [ObservableProperty]
    private DateTime? _expiresAt;

    [ObservableProperty]
    private ObservableCollection<string> _tags = new();

    [ObservableProperty]
    private string _newTag = string.Empty;

    [ObservableProperty]
    private bool _isNewSecret = true;

    [ObservableProperty]
    private bool _hasChanges;

    public IReadOnlyList<vault_secret_type> SecretTypes { get; } = Enum.GetValues<vault_secret_type>();

    public event EventHandler<vault_secret_model>? secret_saved;
    public event EventHandler? cancel_requested;

    public void LoadFromSecret(vault_secret_model secret)
    {
        SecretId = secret.id;
        SecretName = secret.name;
        Description = secret.description;
        SecretType = secret.secret_type;
        SecretValue = secret.encrypted_value;
        ExpiresAt = secret.expires_at;
        Tags = new ObservableCollection<string>(secret.tags);
        IsNewSecret = false;
        HasChanges = false;
    }

    public void CreateNew()
    {
        SecretId = Guid.NewGuid().ToString();
        SecretName = string.Empty;
        Description = string.Empty;
        SecretType = vault_secret_type.api_key;
        SecretValue = string.Empty;
        ExpiresAt = null;
        Tags.Clear();
        IsNewSecret = true;
        HasChanges = false;
    }

    public vault_secret_model ToSecretModel()
    {
        return new vault_secret_model
        {
            id = SecretId,
            name = SecretName,
            description = Description,
            secret_type = SecretType,
            encrypted_value = SecretValue,
            expires_at = ExpiresAt,
            tags = Tags.ToList()
        };
    }

    [RelayCommand]
    private void ToggleValueVisibility()
    {
        IsValueVisible = !IsValueVisible;
    }

    [RelayCommand]
    private void AddTag()
    {
        if (!string.IsNullOrWhiteSpace(NewTag) && !Tags.Contains(NewTag))
        {
            Tags.Add(NewTag);
            NewTag = string.Empty;
            HasChanges = true;
        }
    }

    [RelayCommand]
    private void RemoveTag(string tag)
    {
        if (Tags.Remove(tag))
        {
            HasChanges = true;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(SecretName))
            return;

        var secret = ToSecretModel();

        if (IsNewSecret)
        {
            await _vaultStore.create_async(secret);
        }
        else
        {
            await _vaultStore.update_async(secret);
        }

        IsNewSecret = false;
        HasChanges = false;
        secret_saved?.Invoke(this, secret);
    }

    [RelayCommand]
    private void Cancel()
    {
        cancel_requested?.Invoke(this, EventArgs.Empty);
    }

    partial void OnSecretNameChanged(string value) => HasChanges = true;
    partial void OnDescriptionChanged(string value) => HasChanges = true;
    partial void OnSecretTypeChanged(vault_secret_type value) => HasChanges = true;
    partial void OnSecretValueChanged(string value) => HasChanges = true;
    partial void OnExpiresAtChanged(DateTime? value) => HasChanges = true;
}
