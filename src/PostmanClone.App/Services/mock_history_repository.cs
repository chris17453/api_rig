using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;

namespace PostmanClone.App.Services;

public class mock_history_repository : i_history_repository
{
    private readonly List<history_entry_model> _history;

    public mock_history_repository()
    {
        _history = new List<history_entry_model>
        {
            new history_entry_model
            {
                id = "hist-1",
                request_name = "Get Users",
                method = http_method.get,
                url = "https://jsonplaceholder.typicode.com/users",
                status_code = 200,
                status_description = "OK",
                elapsed_ms = 245,
                response_size_bytes = 5862,
                executed_at = DateTime.UtcNow.AddMinutes(-30)
            },
            new history_entry_model
            {
                id = "hist-2",
                request_name = "Create Post",
                method = http_method.post,
                url = "https://jsonplaceholder.typicode.com/posts",
                status_code = 201,
                status_description = "Created",
                elapsed_ms = 312,
                response_size_bytes = 128,
                executed_at = DateTime.UtcNow.AddHours(-1)
            },
            new history_entry_model
            {
                id = "hist-3",
                request_name = "Auth Login",
                method = http_method.post,
                url = "https://api.example.com/auth/login",
                status_code = 401,
                status_description = "Unauthorized",
                elapsed_ms = 156,
                response_size_bytes = 64,
                error_message = "Invalid credentials",
                executed_at = DateTime.UtcNow.AddHours(-2)
            }
        };
    }

    public Task append_async(history_entry_model entry, CancellationToken cancellation_token)
    {
        _history.Insert(0, entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<history_entry_model>> get_all_async(CancellationToken cancellation_token)
    {
        return Task.FromResult<IReadOnlyList<history_entry_model>>(_history);
    }

    public Task<IReadOnlyList<history_entry_model>> get_recent_async(int count, CancellationToken cancellation_token)
    {
        var recent = _history.Take(count).ToList();
        return Task.FromResult<IReadOnlyList<history_entry_model>>(recent);
    }

    public Task<history_entry_model?> get_by_id_async(string id, CancellationToken cancellation_token)
    {
        var entry = _history.FirstOrDefault(h => h.id == id);
        return Task.FromResult(entry);
    }

    public Task delete_async(string id, CancellationToken cancellation_token)
    {
        _history.RemoveAll(h => h.id == id);
        return Task.CompletedTask;
    }

    public Task clear_all_async(CancellationToken cancellation_token)
    {
        _history.Clear();
        return Task.CompletedTask;
    }
}
