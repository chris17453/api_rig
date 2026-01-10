using Core.Models;

namespace Scripting.Api;

public class pm_api
{
    private readonly pm_test_collector _test_collector;
    private readonly pm_environment_api _environment_api;
    private readonly pm_request_api? _request_api;
    private readonly pm_response_api? _response_api;

    public pm_api(
        script_context_model context,
        pm_test_collector test_collector)
    {
        _test_collector = test_collector;
        _environment_api = new pm_environment_api(context.environment.variables);
        _request_api = new pm_request_api(context.request);
        _response_api = context.response is not null
            ? new pm_response_api(context.response)
            : null;
    }

    public pm_environment_api environment => _environment_api;
    public pm_request_api request => _request_api!;
    public pm_response_api? response => _response_api;

    public void test(string name, Action test_fn)
    {
        _test_collector.test(name, test_fn);
    }

    public pm_expect expect(object? value)
    {
        return new pm_expect(value);
    }

    public IReadOnlyDictionary<string, string> get_environment_updates()
    {
        return _environment_api.updates;
    }
}
