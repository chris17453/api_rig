using Core.Interfaces;

namespace Core.Models;

/// <summary>
/// Context for variable resolution containing all sources of variables.
/// </summary>
public class variable_resolution_context
{
    /// <summary>
    /// Environment variables (from the selected environment).
    /// </summary>
    public IReadOnlyDictionary<string, string> environment_variables { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Vault store for resolving {{vault:name}} references.
    /// Can be null if vault is locked or not available.
    /// </summary>
    public i_vault_store? vault_store { get; init; }

    /// <summary>
    /// Whether the vault is unlocked and accessible.
    /// </summary>
    public bool is_vault_unlocked { get; init; }

    /// <summary>
    /// Collection variables (from the request's parent collection).
    /// </summary>
    public IReadOnlyDictionary<string, string> collection_variables { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Global variables that apply to all requests.
    /// </summary>
    public IReadOnlyDictionary<string, string> global_variables { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Creates an empty context (for testing or when no variables are available).
    /// </summary>
    public static variable_resolution_context Empty => new();

    /// <summary>
    /// Creates a simple context with just environment variables.
    /// </summary>
    public static variable_resolution_context FromEnvironment(IReadOnlyDictionary<string, string> variables)
    {
        return new variable_resolution_context
        {
            environment_variables = variables
        };
    }
}
