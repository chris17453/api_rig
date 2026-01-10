using FluentAssertions;
using Core.Interfaces;
using Core.Models;
using Data.Services;
using Xunit;

namespace Data.Tests;

public class request_orchestrator_tests
{
    private readonly mock_request_executor _mock_executor = new();
    private readonly mock_script_runner _mock_script_runner = new();
    private readonly mock_environment_store _mock_env_store = new();
    private readonly variable_resolver _variable_resolver = new();

    private request_orchestrator create_orchestrator()
    {
        return new request_orchestrator(
            _mock_executor,
            _mock_script_runner,
            _mock_env_store,
            _variable_resolver);
    }

    [Fact]
    public async Task execute_request_async_executes_request_without_scripts()
    {
        var orchestrator = create_orchestrator();
        var request = new http_request_model
        {
            name = "Test",
            method = http_method.get,
            url = "https://api.example.com/users"
        };

        _mock_executor.response_to_return = new http_response_model
        {
            status_code = 200,
            status_description = "OK"
        };

        var result = await orchestrator.execute_request_async(request, CancellationToken.None);

        result.response.Should().NotBeNull();
        result.response!.status_code.Should().Be(200);
        result.pre_script_result.Should().BeNull();
        result.post_script_result.Should().BeNull();
    }

    [Fact]
    public async Task execute_request_async_runs_pre_request_script_when_present()
    {
        var orchestrator = create_orchestrator();
        var request = new http_request_model
        {
            name = "Test",
            method = http_method.get,
            url = "https://api.example.com/users",
            pre_request_script = "pm.environment.set('token', 'abc123');"
        };

        _mock_executor.response_to_return = new http_response_model
        {
            status_code = 200,
            status_description = "OK"
        };

        _mock_script_runner.pre_result_to_return = new script_execution_result_model
        {
            success = true,
            environment_updates = new Dictionary<string, string> { { "token", "abc123" } }
        };

        var result = await orchestrator.execute_request_async(request, CancellationToken.None);

        result.pre_script_result.Should().NotBeNull();
        result.pre_script_result!.success.Should().BeTrue();
        _mock_script_runner.pre_script_called.Should().BeTrue();
    }

    [Fact]
    public async Task execute_request_async_runs_post_response_script_when_present()
    {
        var orchestrator = create_orchestrator();
        var request = new http_request_model
        {
            name = "Test",
            method = http_method.get,
            url = "https://api.example.com/users",
            post_response_script = "pm.test('status is 200', function() { pm.expect(pm.response.code).to.equal(200); });"
        };

        _mock_executor.response_to_return = new http_response_model
        {
            status_code = 200,
            status_description = "OK"
        };

        _mock_script_runner.post_result_to_return = new script_execution_result_model
        {
            success = true,
            test_results = new List<test_result_model>
            {
                new() { name = "status is 200", passed = true }
            }
        };

        var result = await orchestrator.execute_request_async(request, CancellationToken.None);

        result.post_script_result.Should().NotBeNull();
        result.post_script_result!.success.Should().BeTrue();
        result.all_test_results.Should().HaveCount(1);
        _mock_script_runner.post_script_called.Should().BeTrue();
    }

    [Fact]
    public async Task execute_request_async_applies_environment_updates_from_pre_script()
    {
        var orchestrator = create_orchestrator();
        _mock_env_store.active_environment = new environment_model
        {
            name = "Test",
            variables = new Dictionary<string, string> { { "base_url", "https://api.example.com" } }
        };

        var request = new http_request_model
        {
            name = "Test",
            method = http_method.get,
            url = "{{base_url}}/users",
            pre_request_script = "pm.environment.set('token', 'new_token');"
        };

        _mock_executor.response_to_return = new http_response_model
        {
            status_code = 200,
            status_description = "OK"
        };

        _mock_script_runner.pre_result_to_return = new script_execution_result_model
        {
            success = true,
            environment_updates = new Dictionary<string, string> { { "token", "new_token" } }
        };

        await orchestrator.execute_request_async(request, CancellationToken.None);

        _mock_env_store.set_variable_calls.Should().ContainKey("token");
        _mock_env_store.set_variable_calls["token"].Should().Be("new_token");
    }

    [Fact]
    public async Task execute_request_async_resolves_variables_in_url()
    {
        var orchestrator = create_orchestrator();
        _mock_env_store.active_environment = new environment_model
        {
            name = "Test",
            variables = new Dictionary<string, string> { { "base_url", "https://api.example.com" } }
        };

        var request = new http_request_model
        {
            name = "Test",
            method = http_method.get,
            url = "{{base_url}}/users"
        };

        _mock_executor.response_to_return = new http_response_model
        {
            status_code = 200,
            status_description = "OK"
        };

        await orchestrator.execute_request_async(request, CancellationToken.None);

        _mock_executor.last_request_received!.url.Should().Be("https://api.example.com/users");
    }

    [Fact]
    public async Task execute_request_async_aggregates_test_results_from_both_scripts()
    {
        var orchestrator = create_orchestrator();
        var request = new http_request_model
        {
            name = "Test",
            method = http_method.get,
            url = "https://api.example.com/users",
            pre_request_script = "pm.test('pre test', function() {});",
            post_response_script = "pm.test('post test', function() {});"
        };

        _mock_executor.response_to_return = new http_response_model
        {
            status_code = 200,
            status_description = "OK"
        };

        _mock_script_runner.pre_result_to_return = new script_execution_result_model
        {
            success = true,
            test_results = new List<test_result_model>
            {
                new() { name = "pre test", passed = true }
            }
        };

        _mock_script_runner.post_result_to_return = new script_execution_result_model
        {
            success = true,
            test_results = new List<test_result_model>
            {
                new() { name = "post test", passed = true }
            }
        };

        var result = await orchestrator.execute_request_async(request, CancellationToken.None);

        result.all_test_results.Should().HaveCount(2);
        result.all_test_results.Select(t => t.name).Should().Contain("pre test");
        result.all_test_results.Select(t => t.name).Should().Contain("post test");
    }

    [Fact]
    public async Task execute_request_async_reports_script_errors()
    {
        var orchestrator = create_orchestrator();
        var request = new http_request_model
        {
            name = "Test",
            method = http_method.get,
            url = "https://api.example.com/users",
            post_response_script = "invalid script syntax {"
        };

        _mock_executor.response_to_return = new http_response_model
        {
            status_code = 200,
            status_description = "OK"
        };

        _mock_script_runner.post_result_to_return = new script_execution_result_model
        {
            success = false,
            errors = new List<string> { "Syntax error" }
        };

        var result = await orchestrator.execute_request_async(request, CancellationToken.None);

        result.has_script_errors.Should().BeTrue();
        result.all_errors.Should().Contain("Syntax error");
    }
}

internal class mock_request_executor : i_request_executor
{
    public http_response_model response_to_return { get; set; } = new()
    {
        status_code = 200,
        status_description = "OK"
    };

    public http_request_model? last_request_received { get; private set; }

    public Task<http_response_model> execute_async(http_request_model request, CancellationToken cancellation_token)
    {
        last_request_received = request;
        return Task.FromResult(response_to_return);
    }
}

internal class mock_script_runner : i_script_runner
{
    public script_execution_result_model pre_result_to_return { get; set; } = new() { success = true };
    public script_execution_result_model post_result_to_return { get; set; } = new() { success = true };
    public bool pre_script_called { get; private set; }
    public bool post_script_called { get; private set; }

    public Task<script_execution_result_model> run_pre_request_async(
        string script,
        script_context_model context,
        CancellationToken cancellation_token)
    {
        pre_script_called = true;
        return Task.FromResult(pre_result_to_return);
    }

    public Task<script_execution_result_model> run_post_response_async(
        string script,
        script_context_model context,
        CancellationToken cancellation_token)
    {
        post_script_called = true;
        return Task.FromResult(post_result_to_return);
    }
}

internal class mock_environment_store : i_environment_store
{
    public environment_model? active_environment { get; set; }
    public Dictionary<string, string> set_variable_calls { get; } = new();

    public Task<IReadOnlyList<environment_model>> list_all_async(CancellationToken cancellation_token)
    {
        var list = active_environment is not null
            ? new List<environment_model> { active_environment }
            : new List<environment_model>();
        return Task.FromResult<IReadOnlyList<environment_model>>(list);
    }

    public Task<environment_model?> get_by_id_async(string id, CancellationToken cancellation_token)
    {
        return Task.FromResult(active_environment?.id == id ? active_environment : null);
    }

    public Task<environment_model?> get_active_async(CancellationToken cancellation_token)
    {
        return Task.FromResult(active_environment);
    }

    public Task set_active_async(string? id, CancellationToken cancellation_token)
    {
        return Task.CompletedTask;
    }

    public Task save_async(environment_model environment, CancellationToken cancellation_token)
    {
        active_environment = environment;
        return Task.CompletedTask;
    }

    public Task delete_async(string id, CancellationToken cancellation_token)
    {
        if (active_environment?.id == id)
            active_environment = null;
        return Task.CompletedTask;
    }

    public Task<string?> get_variable_async(string key, CancellationToken cancellation_token)
    {
        if (active_environment?.variables.TryGetValue(key, out var value) == true)
            return Task.FromResult<string?>(value);
        return Task.FromResult<string?>(null);
    }

    public Task set_variable_async(string key, string value, CancellationToken cancellation_token)
    {
        set_variable_calls[key] = value;
        if (active_environment is not null)
        {
            var vars = new Dictionary<string, string>(active_environment.variables) { [key] = value };
            active_environment = active_environment with { variables = vars };
        }
        return Task.CompletedTask;
    }
}
