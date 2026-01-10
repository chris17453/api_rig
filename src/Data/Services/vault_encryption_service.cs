using System.Security.Cryptography;
using System.Text;
using Core.Interfaces;

namespace Data.Services;

/// <summary>
/// Provides AES-256-GCM encryption/decryption for vault secrets.
/// </summary>
public class vault_encryption_service : i_vault_encryption_service
{
    private const int KEY_SIZE_BYTES = 32; // 256 bits
    private const int NONCE_SIZE_BYTES = 12; // 96 bits for GCM
    private const int TAG_SIZE_BYTES = 16; // 128 bits for GCM auth tag
    private const int SALT_SIZE_BYTES = 16;
    private const int PBKDF2_ITERATIONS = 100_000;

    private byte[]? _derived_key;

    /// <summary>
    /// Generates a new vault key (Base64 encoded random bytes).
    /// </summary>
    public static string generate_vault_key()
    {
        var key_bytes = RandomNumberGenerator.GetBytes(KEY_SIZE_BYTES);
        return Convert.ToBase64String(key_bytes);
    }

    /// <summary>
    /// Creates a verification token that can be used to verify the vault key is correct.
    /// Store this in the database to check if user enters correct key.
    /// </summary>
    public static string create_verification_token(string vault_key)
    {
        var salt = RandomNumberGenerator.GetBytes(SALT_SIZE_BYTES);
        var key_bytes = Convert.FromBase64String(vault_key);

        using var hmac = new HMACSHA256(key_bytes);
        var hash = hmac.ComputeHash(salt);

        // Format: base64(salt):base64(hash)
        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    /// <summary>
    /// Verifies that the provided vault key matches the verification token.
    /// </summary>
    public static bool verify_vault_key(string vault_key, string verification_token)
    {
        try
        {
            var parts = verification_token.Split(':');
            if (parts.Length != 2) return false;

            var salt = Convert.FromBase64String(parts[0]);
            var stored_hash = Convert.FromBase64String(parts[1]);
            var key_bytes = Convert.FromBase64String(vault_key);

            using var hmac = new HMACSHA256(key_bytes);
            var computed_hash = hmac.ComputeHash(salt);

            return CryptographicOperations.FixedTimeEquals(stored_hash, computed_hash);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sets the vault key for encryption/decryption operations.
    /// Must be called before encrypt/decrypt.
    /// </summary>
    public void set_key(string vault_key)
    {
        _derived_key = Convert.FromBase64String(vault_key);
    }

    /// <summary>
    /// Clears the vault key from memory (lock vault).
    /// </summary>
    public void clear_key()
    {
        if (_derived_key != null)
        {
            CryptographicOperations.ZeroMemory(_derived_key);
            _derived_key = null;
        }
    }

    /// <summary>
    /// Returns true if a key is currently set.
    /// </summary>
    public bool is_unlocked => _derived_key != null;

    /// <summary>
    /// Encrypts a plaintext value using AES-256-GCM.
    /// Returns Base64 encoded string: nonce + ciphertext + tag
    /// </summary>
    public string encrypt(string plaintext)
    {
        if (_derived_key == null)
            throw new InvalidOperationException("Vault is locked. Call set_key first.");

        var plaintext_bytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NONCE_SIZE_BYTES);
        var ciphertext = new byte[plaintext_bytes.Length];
        var tag = new byte[TAG_SIZE_BYTES];

        using var aes = new AesGcm(_derived_key, TAG_SIZE_BYTES);
        aes.Encrypt(nonce, plaintext_bytes, ciphertext, tag);

        // Combine: nonce + ciphertext + tag
        var result = new byte[NONCE_SIZE_BYTES + ciphertext.Length + TAG_SIZE_BYTES];
        Buffer.BlockCopy(nonce, 0, result, 0, NONCE_SIZE_BYTES);
        Buffer.BlockCopy(ciphertext, 0, result, NONCE_SIZE_BYTES, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, NONCE_SIZE_BYTES + ciphertext.Length, TAG_SIZE_BYTES);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Decrypts a Base64 encoded ciphertext (nonce + ciphertext + tag).
    /// Returns null if decryption fails (wrong key or tampered data).
    /// </summary>
    public string? decrypt(string encrypted_base64)
    {
        if (_derived_key == null)
            throw new InvalidOperationException("Vault is locked. Call set_key first.");

        try
        {
            var encrypted_bytes = Convert.FromBase64String(encrypted_base64);

            if (encrypted_bytes.Length < NONCE_SIZE_BYTES + TAG_SIZE_BYTES)
                return null;

            var nonce = new byte[NONCE_SIZE_BYTES];
            var ciphertext_length = encrypted_bytes.Length - NONCE_SIZE_BYTES - TAG_SIZE_BYTES;
            var ciphertext = new byte[ciphertext_length];
            var tag = new byte[TAG_SIZE_BYTES];

            Buffer.BlockCopy(encrypted_bytes, 0, nonce, 0, NONCE_SIZE_BYTES);
            Buffer.BlockCopy(encrypted_bytes, NONCE_SIZE_BYTES, ciphertext, 0, ciphertext_length);
            Buffer.BlockCopy(encrypted_bytes, NONCE_SIZE_BYTES + ciphertext_length, tag, 0, TAG_SIZE_BYTES);

            var plaintext_bytes = new byte[ciphertext_length];

            using var aes = new AesGcm(_derived_key, TAG_SIZE_BYTES);
            aes.Decrypt(nonce, ciphertext, tag, plaintext_bytes);

            return Encoding.UTF8.GetString(plaintext_bytes);
        }
        catch (CryptographicException)
        {
            // Wrong key or tampered data
            return null;
        }
        catch
        {
            return null;
        }
    }
}
