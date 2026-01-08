using PostmanClone.Core.Models;

namespace PostmanClone.Core.Interfaces;

public interface i_environment_store
{
    Task<IReadOnlyList<environment_model>> list_all_async(CancellationToken cancellation_token);
    Task<environment_model?> get_by_id_async(string id, CancellationToken cancellation_token);
    Task<environment_model?> get_active_async(CancellationToken cancellation_token);
    Task set_active_async(string? id, CancellationToken cancellation_token);
    Task save_async(environment_model environment, CancellationToken cancellation_token);
    Task delete_async(string id, CancellationToken cancellation_token);
    Task<string?> get_variable_async(string key, CancellationToken cancellation_token);
    Task set_variable_async(string key, string value, CancellationToken cancellation_token);
}
