using PostmanClone.Core.Models;

namespace PostmanClone.Core.Interfaces;

public interface i_history_repository
{
    Task append_async(history_entry_model entry, CancellationToken cancellation_token);
    Task<IReadOnlyList<history_entry_model>> get_all_async(CancellationToken cancellation_token);
    Task<IReadOnlyList<history_entry_model>> get_recent_async(int count, CancellationToken cancellation_token);
    Task<history_entry_model?> get_by_id_async(string id, CancellationToken cancellation_token);
    Task delete_async(string id, CancellationToken cancellation_token);
    Task clear_all_async(CancellationToken cancellation_token);
}
