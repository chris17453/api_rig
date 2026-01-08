using Microsoft.EntityFrameworkCore;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using PostmanClone.Data.Context;
using PostmanClone.Data.Entities;

namespace PostmanClone.Data.Stores;

public class environment_store : i_environment_store
{
    private readonly postman_clone_db_context _context;

    public environment_store(postman_clone_db_context context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<environment_model>> list_all_async(CancellationToken cancellation_token)
    {
        var entities = await _context.environments
            .Include(e => e.variables)
            .AsNoTracking()
            .ToListAsync(cancellation_token);

        return entities.Select(map_to_model).ToList();
    }

    public async Task<environment_model?> get_by_id_async(string id, CancellationToken cancellation_token)
    {
        var entity = await _context.environments
            .Include(e => e.variables)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.id == id, cancellation_token);

        return entity is null ? null : map_to_model(entity);
    }

    public async Task<environment_model?> get_active_async(CancellationToken cancellation_token)
    {
        var entity = await _context.environments
            .Include(e => e.variables)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.is_active, cancellation_token);

        return entity is null ? null : map_to_model(entity);
    }

    public async Task set_active_async(string? id, CancellationToken cancellation_token)
    {
        // Deactivate all environments
        var active_environments = await _context.environments
            .Where(e => e.is_active)
            .ToListAsync(cancellation_token);

        foreach (var env in active_environments)
        {
            env.is_active = false;
        }

        // Activate the specified environment
        if (id is not null)
        {
            var environment = await _context.environments
                .FirstOrDefaultAsync(e => e.id == id, cancellation_token);

            if (environment is not null)
            {
                environment.is_active = true;
            }
        }

        await _context.SaveChangesAsync(cancellation_token);
    }

    public async Task save_async(environment_model environment, CancellationToken cancellation_token)
    {
        var existing = await _context.environments
            .Include(e => e.variables)
            .FirstOrDefaultAsync(e => e.id == environment.id, cancellation_token);

        if (existing is not null)
        {
            // Update existing environment
            existing.name = environment.name;
            existing.updated_at = DateTime.UtcNow;

            // Remove existing variables and add new ones
            _context.environment_variables.RemoveRange(existing.variables);
            existing.variables = environment.variables
                .Select(kv => new environment_variable_entity
                {
                    key = kv.Key,
                    value = kv.Value,
                    environment_id = environment.id
                })
                .ToList();
        }
        else
        {
            // Add new environment
            var entity = map_to_entity(environment);
            _context.environments.Add(entity);
        }

        await _context.SaveChangesAsync(cancellation_token);
    }

    public async Task delete_async(string id, CancellationToken cancellation_token)
    {
        var entity = await _context.environments
            .FirstOrDefaultAsync(e => e.id == id, cancellation_token);

        if (entity is not null)
        {
            _context.environments.Remove(entity);
            await _context.SaveChangesAsync(cancellation_token);
        }
    }

    public async Task<string?> get_variable_async(string key, CancellationToken cancellation_token)
    {
        var active_environment = await _context.environments
            .Include(e => e.variables)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.is_active, cancellation_token);

        if (active_environment is null)
        {
            return null;
        }

        var variable = active_environment.variables.FirstOrDefault(v => v.key == key);
        return variable?.value;
    }

    public async Task set_variable_async(string key, string value, CancellationToken cancellation_token)
    {
        var active_environment = await _context.environments
            .Include(e => e.variables)
            .FirstOrDefaultAsync(e => e.is_active, cancellation_token);

        if (active_environment is null)
        {
            throw new InvalidOperationException("No active environment to set variable on.");
        }

        var existing_variable = active_environment.variables.FirstOrDefault(v => v.key == key);

        if (existing_variable is not null)
        {
            existing_variable.value = value;
        }
        else
        {
            active_environment.variables.Add(new environment_variable_entity
            {
                key = key,
                value = value,
                environment_id = active_environment.id
            });
        }

        active_environment.updated_at = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellation_token);
    }

    private static environment_entity map_to_entity(environment_model model)
    {
        return new environment_entity
        {
            id = model.id,
            name = model.name,
            is_active = false,
            created_at = model.created_at,
            updated_at = model.updated_at,
            variables = model.variables
                .Select(kv => new environment_variable_entity
                {
                    key = kv.Key,
                    value = kv.Value,
                    environment_id = model.id
                })
                .ToList()
        };
    }

    private static environment_model map_to_model(environment_entity entity)
    {
        return new environment_model
        {
            id = entity.id,
            name = entity.name,
            variables = entity.variables.ToDictionary(v => v.key, v => v.value),
            created_at = entity.created_at,
            updated_at = entity.updated_at
        };
    }
}
