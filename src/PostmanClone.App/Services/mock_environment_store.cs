using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;

namespace PostmanClone.App.Services;

public class mock_environment_store : i_environment_store
{
    private readonly List<environment_model> _environments;
    private string? _active_environment_id;

    public mock_environment_store()
    {
        _environments = new List<environment_model>
        {
            new environment_model
            {
                id = "env-1",
                name = "Development",
                variables = new Dictionary<string, string>
                {
                    { "base_url", "http://localhost:3000" },
                    { "api_key", "dev-api-key-12345" },
                    { "timeout", "5000" }
                }
            },
            new environment_model
            {
                id = "env-2",
                name = "Staging",
                variables = new Dictionary<string, string>
                {
                    { "base_url", "https://staging.api.example.com" },
                    { "api_key", "staging-api-key-67890" },
                    { "timeout", "10000" }
                }
            },
            new environment_model
            {
                id = "env-3",
                name = "Production",
                variables = new Dictionary<string, string>
                {
                    { "base_url", "https://api.example.com" },
                    { "api_key", "prod-api-key-secret" },
                    { "timeout", "30000" }
                }
            }
        };
        _active_environment_id = "env-1";
    }

    public Task<IReadOnlyList<environment_model>> list_all_async(CancellationToken cancellation_token)
    {
        return Task.FromResult<IReadOnlyList<environment_model>>(_environments);
    }

    public Task<environment_model?> get_by_id_async(string id, CancellationToken cancellation_token)
    {
        var env = _environments.FirstOrDefault(e => e.id == id);
        return Task.FromResult(env);
    }

    public Task<environment_model?> get_active_async(CancellationToken cancellation_token)
    {
        if (_active_environment_id == null)
            return Task.FromResult<environment_model?>(null);

        var active = _environments.FirstOrDefault(e => e.id == _active_environment_id);
        return Task.FromResult(active);
    }

    public Task set_active_async(string? id, CancellationToken cancellation_token)
    {
        _active_environment_id = id;
        return Task.CompletedTask;
    }

    public Task save_async(environment_model environment, CancellationToken cancellation_token)
    {
        var existing = _environments.FindIndex(e => e.id == environment.id);
        if (existing >= 0)
            _environments[existing] = environment;
        else
            _environments.Add(environment);
        return Task.CompletedTask;
    }

    public Task delete_async(string id, CancellationToken cancellation_token)
    {
        _environments.RemoveAll(e => e.id == id);
        if (_active_environment_id == id)
            _active_environment_id = null;
        return Task.CompletedTask;
    }

    public Task<string?> get_variable_async(string key, CancellationToken cancellation_token)
    {
        if (_active_environment_id == null)
            return Task.FromResult<string?>(null);

        var active = _environments.FirstOrDefault(e => e.id == _active_environment_id);
        if (active?.variables.TryGetValue(key, out var value) == true)
            return Task.FromResult<string?>(value);

        return Task.FromResult<string?>(null);
    }

    public Task set_variable_async(string key, string value, CancellationToken cancellation_token)
    {
        if (_active_environment_id == null)
            return Task.CompletedTask;

        var active = _environments.FirstOrDefault(e => e.id == _active_environment_id);
        if (active != null)
        {
            var vars = new Dictionary<string, string>(active.variables) { [key] = value };
            var updated = active with { variables = vars, updated_at = DateTime.UtcNow };
            var index = _environments.FindIndex(e => e.id == _active_environment_id);
            if (index >= 0)
                _environments[index] = updated;
        }
        return Task.CompletedTask;
    }
}
