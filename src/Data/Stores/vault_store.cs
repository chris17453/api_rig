using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Core.Interfaces;
using Core.Models;
using Data.Context;
using Data.Entities;
using Data.Services;

namespace Data.Stores;

public class vault_store : i_vault_store
{
    private readonly postman_clone_db_context _context;
    private readonly vault_encryption_service _encryption;

    private const string VAULT_VERIFICATION_KEY = "vault_verification_token";

    public vault_store(postman_clone_db_context context, vault_encryption_service encryption)
    {
        _context = context;
        _encryption = encryption;
    }

    /// <summary>
    /// Checks if the vault has been set up (has a verification token).
    /// </summary>
    public async Task<bool> is_vault_initialized_async(CancellationToken cancellation_token = default)
    {
        var setting = await _context.app_settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.key == VAULT_VERIFICATION_KEY, cancellation_token);
        return setting != null;
    }

    /// <summary>
    /// Generates a new vault key without saving it.
    /// </summary>
    public string generate_new_vault_key()
    {
        return vault_encryption_service.generate_vault_key();
    }

    /// <summary>
    /// Initializes the vault with the provided key. Saves the verification token.
    /// </summary>
    public async Task initialize_vault_async(string vault_key, CancellationToken cancellation_token = default)
    {
        var verification_token = vault_encryption_service.create_verification_token(vault_key);

        var existing = await _context.app_settings
            .FirstOrDefaultAsync(s => s.key == VAULT_VERIFICATION_KEY, cancellation_token);

        if (existing != null)
        {
            existing.value = verification_token;
            existing.updated_at = DateTime.UtcNow;
        }
        else
        {
            _context.app_settings.Add(new app_setting_entity
            {
                key = VAULT_VERIFICATION_KEY,
                value = verification_token,
                updated_at = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellation_token);
    }

    /// <summary>
    /// Attempts to unlock the vault with the provided key.
    /// </summary>
    public async Task<bool> try_unlock_async(string vault_key, CancellationToken cancellation_token = default)
    {
        var setting = await _context.app_settings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.key == VAULT_VERIFICATION_KEY, cancellation_token);

        if (setting == null)
            return false;

        if (!vault_encryption_service.verify_vault_key(vault_key, setting.value))
            return false;

        _encryption.set_key(vault_key);
        return true;
    }

    /// <summary>
    /// Locks the vault, clearing the encryption key from memory.
    /// </summary>
    public void lock_vault()
    {
        _encryption.clear_key();
    }

    /// <summary>
    /// Returns true if the vault is currently unlocked.
    /// </summary>
    public bool is_unlocked => _encryption.is_unlocked;

    public async Task<IReadOnlyList<vault_secret_model>> get_all_async(CancellationToken cancellation_token = default)
    {
        var entities = await _context.vault_secrets
            .AsNoTracking()
            .OrderBy(s => s.name)
            .ToListAsync(cancellation_token);

        return entities.Select(map_to_model).ToList();
    }

    public async Task<vault_secret_model?> get_by_id_async(string id, CancellationToken cancellation_token = default)
    {
        var entity = await _context.vault_secrets
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.id == id, cancellation_token);

        return entity is null ? null : map_to_model(entity);
    }

    public async Task<vault_secret_model?> get_by_name_async(string name, CancellationToken cancellation_token = default)
    {
        var entity = await _context.vault_secrets
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.name == name, cancellation_token);

        return entity is null ? null : map_to_model(entity);
    }

    public async Task<string?> get_secret_value_async(string name, CancellationToken cancellation_token = default)
    {
        if (!_encryption.is_unlocked)
            return null;

        var entity = await _context.vault_secrets
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.name == name, cancellation_token);

        if (entity is null || string.IsNullOrEmpty(entity.encrypted_value))
            return null;

        // Mark as used (fire and forget - don't block on this)
        _ = mark_used_async(entity.id, CancellationToken.None);

        // Decrypt the value
        return _encryption.decrypt(entity.encrypted_value);
    }

    public async Task<IReadOnlyList<vault_secret_model>> search_async(string query, CancellationToken cancellation_token = default)
    {
        var lowerQuery = query.ToLowerInvariant();
        var entities = await _context.vault_secrets
            .AsNoTracking()
            .Where(s => s.name.ToLower().Contains(lowerQuery) ||
                       s.description.ToLower().Contains(lowerQuery) ||
                       s.tags_json.ToLower().Contains(lowerQuery))
            .OrderBy(s => s.name)
            .ToListAsync(cancellation_token);

        return entities.Select(map_to_model).ToList();
    }

    public async Task<vault_secret_model> create_async(vault_secret_model secret, CancellationToken cancellation_token = default)
    {
        if (!_encryption.is_unlocked)
            throw new InvalidOperationException("Vault must be unlocked to create secrets.");

        var entity = map_to_entity(secret);
        entity.created_at = DateTime.UtcNow;

        // Encrypt the value before storing
        if (!string.IsNullOrEmpty(entity.encrypted_value))
        {
            entity.encrypted_value = _encryption.encrypt(entity.encrypted_value);
        }

        _context.vault_secrets.Add(entity);
        await _context.SaveChangesAsync(cancellation_token);

        return map_to_model(entity);
    }

    public async Task<vault_secret_model> update_async(vault_secret_model secret, CancellationToken cancellation_token = default)
    {
        if (!_encryption.is_unlocked)
            throw new InvalidOperationException("Vault must be unlocked to update secrets.");

        var entity = await _context.vault_secrets
            .FirstOrDefaultAsync(s => s.id == secret.id, cancellation_token);

        if (entity is null)
        {
            throw new InvalidOperationException($"Secret with ID {secret.id} not found.");
        }

        entity.name = secret.name;
        entity.description = secret.description;
        entity.secret_type = secret.secret_type;

        // Encrypt the new value
        entity.encrypted_value = !string.IsNullOrEmpty(secret.encrypted_value)
            ? _encryption.encrypt(secret.encrypted_value)
            : secret.encrypted_value;

        entity.metadata_json = secret.metadata_json;
        entity.tags_json = JsonSerializer.Serialize(secret.tags);
        entity.expires_at = secret.expires_at;
        entity.updated_at = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellation_token);

        return map_to_model(entity);
    }

    public async Task delete_async(string id, CancellationToken cancellation_token = default)
    {
        var entity = await _context.vault_secrets
            .FirstOrDefaultAsync(s => s.id == id, cancellation_token);

        if (entity is not null)
        {
            _context.vault_secrets.Remove(entity);
            await _context.SaveChangesAsync(cancellation_token);
        }
    }

    public async Task mark_used_async(string id, CancellationToken cancellation_token = default)
    {
        var entity = await _context.vault_secrets
            .FirstOrDefaultAsync(s => s.id == id, cancellation_token);

        if (entity is not null)
        {
            entity.last_used_at = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellation_token);
        }
    }

    public async Task<IReadOnlyList<vault_secret_model>> get_by_type_async(vault_secret_type secret_type, CancellationToken cancellation_token = default)
    {
        var entities = await _context.vault_secrets
            .AsNoTracking()
            .Where(s => s.secret_type == secret_type)
            .OrderBy(s => s.name)
            .ToListAsync(cancellation_token);

        return entities.Select(map_to_model).ToList();
    }

    private static vault_secret_entity map_to_entity(vault_secret_model model)
    {
        return new vault_secret_entity
        {
            id = model.id,
            name = model.name,
            description = model.description,
            secret_type = model.secret_type,
            encrypted_value = model.encrypted_value,
            metadata_json = model.metadata_json,
            tags_json = JsonSerializer.Serialize(model.tags),
            created_at = model.created_at,
            updated_at = model.updated_at,
            last_used_at = model.last_used_at,
            expires_at = model.expires_at
        };
    }

    private static vault_secret_model map_to_model(vault_secret_entity entity)
    {
        return new vault_secret_model
        {
            id = entity.id,
            name = entity.name,
            description = entity.description,
            secret_type = entity.secret_type,
            encrypted_value = entity.encrypted_value,
            metadata_json = entity.metadata_json,
            tags = string.IsNullOrEmpty(entity.tags_json)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(entity.tags_json) ?? new List<string>(),
            created_at = entity.created_at,
            updated_at = entity.updated_at,
            last_used_at = entity.last_used_at,
            expires_at = entity.expires_at
        };
    }
}
