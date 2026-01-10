using Core.Models;

namespace Core.Interfaces;

/// <summary>
/// Interface for managing workspaces.
/// </summary>
public interface i_workspace_store
{
    /// <summary>
    /// Gets all workspaces.
    /// </summary>
    Task<IReadOnlyList<workspace_model>> get_all_async(CancellationToken cancellation_token = default);

    /// <summary>
    /// Gets a workspace by its ID.
    /// </summary>
    Task<workspace_model?> get_by_id_async(string id, CancellationToken cancellation_token = default);

    /// <summary>
    /// Gets the currently active workspace.
    /// </summary>
    Task<workspace_model?> get_active_async(CancellationToken cancellation_token = default);

    /// <summary>
    /// Sets the active workspace.
    /// </summary>
    Task set_active_async(string id, CancellationToken cancellation_token = default);

    /// <summary>
    /// Creates a new workspace.
    /// </summary>
    Task<workspace_model> create_async(workspace_model workspace, CancellationToken cancellation_token = default);

    /// <summary>
    /// Updates an existing workspace.
    /// </summary>
    Task<workspace_model> update_async(workspace_model workspace, CancellationToken cancellation_token = default);

    /// <summary>
    /// Deletes a workspace.
    /// </summary>
    Task delete_async(string id, CancellationToken cancellation_token = default);

    /// <summary>
    /// Ensures a default workspace exists.
    /// </summary>
    Task<workspace_model> ensure_default_async(CancellationToken cancellation_token = default);
}
