using Microsoft.EntityFrameworkCore;
using Core.Interfaces;
using Core.Models;
using Data.Context;
using Data.Entities;

namespace Data.Stores;

public class workspace_store : i_workspace_store
{
    private readonly postman_clone_db_context _context;

    public workspace_store(postman_clone_db_context context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<workspace_model>> get_all_async(CancellationToken cancellation_token = default)
    {
        var entities = await _context.workspaces
            .AsNoTracking()
            .OrderBy(w => w.name)
            .ToListAsync(cancellation_token);

        return entities.Select(map_to_model).ToList();
    }

    public async Task<workspace_model?> get_by_id_async(string id, CancellationToken cancellation_token = default)
    {
        var entity = await _context.workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.id == id, cancellation_token);

        return entity is null ? null : map_to_model(entity);
    }

    public async Task<workspace_model?> get_active_async(CancellationToken cancellation_token = default)
    {
        var entity = await _context.workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.is_active, cancellation_token);

        return entity is null ? null : map_to_model(entity);
    }

    public async Task set_active_async(string id, CancellationToken cancellation_token = default)
    {
        // Deactivate all workspaces
        var activeWorkspaces = await _context.workspaces
            .Where(w => w.is_active)
            .ToListAsync(cancellation_token);

        foreach (var ws in activeWorkspaces)
        {
            ws.is_active = false;
        }

        // Activate the specified workspace
        var workspace = await _context.workspaces
            .FirstOrDefaultAsync(w => w.id == id, cancellation_token);

        if (workspace != null)
        {
            workspace.is_active = true;
        }

        await _context.SaveChangesAsync(cancellation_token);
    }

    public async Task<workspace_model> create_async(workspace_model workspace, CancellationToken cancellation_token = default)
    {
        var entity = map_to_entity(workspace);
        entity.created_at = DateTime.UtcNow;

        _context.workspaces.Add(entity);
        await _context.SaveChangesAsync(cancellation_token);

        return map_to_model(entity);
    }

    public async Task<workspace_model> update_async(workspace_model workspace, CancellationToken cancellation_token = default)
    {
        var entity = await _context.workspaces
            .FirstOrDefaultAsync(w => w.id == workspace.id, cancellation_token);

        if (entity is null)
        {
            throw new InvalidOperationException($"Workspace with ID {workspace.id} not found.");
        }

        entity.name = workspace.name;
        entity.description = workspace.description;
        entity.icon = workspace.icon;
        entity.color = workspace.color;
        entity.updated_at = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellation_token);

        return map_to_model(entity);
    }

    public async Task delete_async(string id, CancellationToken cancellation_token = default)
    {
        var entity = await _context.workspaces
            .FirstOrDefaultAsync(w => w.id == id, cancellation_token);

        if (entity != null)
        {
            _context.workspaces.Remove(entity);
            await _context.SaveChangesAsync(cancellation_token);
        }
    }

    public async Task<workspace_model> ensure_default_async(CancellationToken cancellation_token = default)
    {
        var existingWorkspace = await _context.workspaces
            .FirstOrDefaultAsync(cancellation_token);

        if (existingWorkspace != null)
        {
            return map_to_model(existingWorkspace);
        }

        // Create default workspace
        var defaultWorkspace = new workspace_entity
        {
            id = Guid.NewGuid().ToString(),
            name = "Default Workspace",
            description = "Your default workspace",
            is_active = true,
            created_at = DateTime.UtcNow
        };

        _context.workspaces.Add(defaultWorkspace);
        await _context.SaveChangesAsync(cancellation_token);

        return map_to_model(defaultWorkspace);
    }

    private static workspace_entity map_to_entity(workspace_model model)
    {
        return new workspace_entity
        {
            id = model.id,
            name = model.name,
            description = model.description,
            icon = model.icon,
            color = model.color,
            is_active = model.is_active,
            created_at = model.created_at,
            updated_at = model.updated_at
        };
    }

    private static workspace_model map_to_model(workspace_entity entity)
    {
        return new workspace_model
        {
            id = entity.id,
            name = entity.name,
            description = entity.description,
            icon = entity.icon,
            color = entity.color,
            is_active = entity.is_active,
            created_at = entity.created_at,
            updated_at = entity.updated_at
        };
    }
}
