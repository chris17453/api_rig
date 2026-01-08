namespace PostmanClone.Scripting.Api;

public class pm_environment_api
{
    private readonly IReadOnlyDictionary<string, string> _initial_variables;
    private readonly Dictionary<string, string> _updates = new();

    public pm_environment_api(IReadOnlyDictionary<string, string> initial_variables)
    {
        _initial_variables = initial_variables ?? new Dictionary<string, string>();
    }

    public IReadOnlyDictionary<string, string> updates => _updates;

    public string? get(string key)
    {
        if (_updates.TryGetValue(key, out var updated_value))
        {
            return updated_value;
        }
        if (_initial_variables.TryGetValue(key, out var initial_value))
        {
            return initial_value;
        }
        return null;
    }

    public void set(string key, string value)
    {
        _updates[key] = value;
    }

    public void unset(string key)
    {
        _updates[key] = string.Empty;
    }

    public bool has(string key)
    {
        return _updates.ContainsKey(key) || _initial_variables.ContainsKey(key);
    }

    public object to_object()
    {
        var result = new Dictionary<string, string>(_initial_variables);
        foreach (var kvp in _updates)
        {
            result[kvp.Key] = kvp.Value;
        }
        return result;
    }
}
