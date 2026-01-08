using PostmanClone.Core.Models;

namespace PostmanClone.Scripting.Api;

public class pm_test_collector
{
    private readonly List<test_result_model> _results = new();
    private readonly List<string> _logs = new();

    public IReadOnlyList<test_result_model> results => _results;
    public IReadOnlyList<string> logs => _logs;

    public void test(string name, Action test_fn)
    {
        var start = DateTime.UtcNow;
        try
        {
            test_fn();
            _results.Add(new test_result_model
            {
                name = name,
                passed = true,
                execution_time_ms = (long)(DateTime.UtcNow - start).TotalMilliseconds
            });
        }
        catch (Exception ex)
        {
            _results.Add(new test_result_model
            {
                name = name,
                passed = false,
                error_message = ex.Message,
                execution_time_ms = (long)(DateTime.UtcNow - start).TotalMilliseconds
            });
        }
    }

    public void log(params object[] args)
    {
        var message = string.Join(" ", args.Select(a => a?.ToString() ?? "null"));
        _logs.Add(message);
    }
}
