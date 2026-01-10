namespace Core.Interfaces;

/// <summary>
/// Interface for vault encryption operations.
/// </summary>
public interface i_vault_encryption_service
{
    /// <summary>
    /// Sets the vault key for encryption/decryption operations.
    /// </summary>
    void set_key(string vault_key);

    /// <summary>
    /// Clears the vault key from memory (lock vault).
    /// </summary>
    void clear_key();

    /// <summary>
    /// Returns true if a key is currently set.
    /// </summary>
    bool is_unlocked { get; }

    /// <summary>
    /// Encrypts a plaintext value.
    /// </summary>
    string encrypt(string plaintext);

    /// <summary>
    /// Decrypts an encrypted value. Returns null if decryption fails.
    /// </summary>
    string? decrypt(string encrypted_base64);
}
