using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// Interface for managing vault secrets.
/// </summary>
public interface i_vault_store
{
    /// <summary>
    /// Gets all secrets from the vault.
    /// </summary>
    Task<IReadOnlyList<vault_secret_model>> get_all_async(CancellationToken cancellation_token = default);

    /// <summary>
    /// Gets a secret by its ID.
    /// </summary>
    Task<vault_secret_model?> get_by_id_async(string id, CancellationToken cancellation_token = default);

    /// <summary>
    /// Gets a secret by its name.
    /// </summary>
    Task<vault_secret_model?> get_by_name_async(string name, CancellationToken cancellation_token = default);

    /// <summary>
    /// Gets the decrypted value of a secret by name. Returns null if not found or vault is locked.
    /// </summary>
    Task<string?> get_secret_value_async(string name, CancellationToken cancellation_token = default);

    /// <summary>
    /// Searches secrets by name or tags.
    /// </summary>
    Task<IReadOnlyList<vault_secret_model>> search_async(string query, CancellationToken cancellation_token = default);

    /// <summary>
    /// Creates a new secret in the vault.
    /// </summary>
    Task<vault_secret_model> create_async(vault_secret_model secret, CancellationToken cancellation_token = default);

    /// <summary>
    /// Updates an existing secret.
    /// </summary>
    Task<vault_secret_model> update_async(vault_secret_model secret, CancellationToken cancellation_token = default);

    /// <summary>
    /// Deletes a secret from the vault.
    /// </summary>
    Task delete_async(string id, CancellationToken cancellation_token = default);

    /// <summary>
    /// Marks a secret as recently used.
    /// </summary>
    Task mark_used_async(string id, CancellationToken cancellation_token = default);

    /// <summary>
    /// Gets all secrets of a specific type.
    /// </summary>
    Task<IReadOnlyList<vault_secret_model>> get_by_type_async(vault_secret_type secret_type, CancellationToken cancellation_token = default);
}
