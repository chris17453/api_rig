using Core.Interfaces;
using Core.Models;

namespace Scripting.Api;

/// <summary>
/// Provides vault access to scripts via the 'vault' global object.
/// Usage in scripts:
///   vault.get('secret_name')  - returns the secret value or null
///   vault.isUnlocked()        - returns true if vault is accessible
///   vault.getSecret('name')   - returns full secret object or null
/// </summary>
public class vault_api
{
    private readonly i_vault_store? _vaultStore;
    private readonly bool _isUnlocked;

    public vault_api(i_vault_store? vaultStore, bool isUnlocked)
    {
        _vaultStore = vaultStore;
        _isUnlocked = isUnlocked;
    }

    /// <summary>
    /// Gets the value of a secret by name.
    /// Returns null if vault is locked or secret not found.
    /// </summary>
    public string? get(string name)
    {
        if (!_isUnlocked || _vaultStore == null || string.IsNullOrEmpty(name))
            return null;

        try
        {
            // Synchronous call - Jint doesn't support async
            return _vaultStore.get_secret_value_async(name).GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a full secret object by name.
    /// Returns null if vault is locked or secret not found.
    /// </summary>
    public object? getSecret(string name)
    {
        if (!_isUnlocked || _vaultStore == null || string.IsNullOrEmpty(name))
            return null;

        try
        {
            var secret = _vaultStore.get_by_name_async(name).GetAwaiter().GetResult();
            if (secret == null)
                return null;

            // Return a JS-friendly object
            return new
            {
                name = secret.name,
                value = secret.encrypted_value, // The "decrypted" value
                type = secret.secret_type.ToString(),
                description = secret.description,
                expiresAt = secret.expires_at?.ToString("o"),
                isExpired = secret.expires_at.HasValue && secret.expires_at.Value < DateTime.UtcNow
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if the vault is unlocked and accessible.
    /// </summary>
    public bool isUnlocked()
    {
        return _isUnlocked && _vaultStore != null;
    }

    /// <summary>
    /// Lists all secret names (not values) for autocomplete/discovery.
    /// Returns empty array if vault is locked.
    /// </summary>
    public string[] list()
    {
        if (!_isUnlocked || _vaultStore == null)
            return Array.Empty<string>();

        try
        {
            var secrets = _vaultStore.get_all_async().GetAwaiter().GetResult();
            return secrets.Select(s => s.name).ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }
}
