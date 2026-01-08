using FluentAssertions;
using PostmanClone.Core.Models;

namespace PostmanClone.Scripting.Tests;

public class script_runner_tests
{
    private static script_context_model create_test_context(http_response_model? response = null)
    {
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/users"
        };

        var environment = new environment_model
        {
            name = "Test Environment",
            variables = new Dictionary<string, string>
            {
                { "base_url", "https://api.example.com" },
                { "token", "test-token" }
            }
        };

        return new script_context_model
        {
            phase = response is null ? script_phase.pre_request : script_phase.post_response,
            request = request,
            response = response,
            environment = environment
        };
    }

    private static http_response_model create_test_response(int status_code = 200, string body = "{\"success\": true}")
    {
        return new http_response_model
        {
            status_code = status_code,
            status_description = status_code == 200 ? "OK" : "Error",
            body_string = body,
            elapsed_ms = 100,
            size_bytes = body.Length
        };
    }

    [Fact]
    public async Task run_pre_request_async_returns_success_when_script_is_empty()
    {
        var runner = new script_runner();
        var context = create_test_context();

        var result = await runner.run_pre_request_async("", context, CancellationToken.None);

        result.success.Should().BeTrue();
        result.errors.Should().BeEmpty();
    }

    [Fact]
    public async Task run_pre_request_async_returns_success_when_script_is_valid()
    {
        var runner = new script_runner();
        var context = create_test_context();

        var result = await runner.run_pre_request_async("var x = 1 + 1;", context, CancellationToken.None);

        result.success.Should().BeTrue();
        result.errors.Should().BeEmpty();
    }

    [Fact]
    public async Task run_pre_request_async_returns_error_when_script_has_syntax_error()
    {
        var runner = new script_runner();
        var context = create_test_context();

        var result = await runner.run_pre_request_async("var x = ;", context, CancellationToken.None);

        result.success.Should().BeFalse();
        result.errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task run_pre_request_async_collects_test_results_when_pm_test_is_called()
    {
        var runner = new script_runner();
        var context = create_test_context();

        var script = @"
            pm.test('should pass', function() {
                pm.expect(1).to.equal(1);
            });
            pm.test('should fail', function() {
                pm.expect(1).to.equal(2);
            });
        ";

        var result = await runner.run_pre_request_async(script, context, CancellationToken.None);

        result.test_results.Should().HaveCount(2);
        result.test_results[0].passed.Should().BeTrue();
        result.test_results[1].passed.Should().BeFalse();
    }

    [Fact]
    public async Task run_pre_request_async_captures_environment_updates_when_pm_environment_set_is_called()
    {
        var runner = new script_runner();
        var context = create_test_context();

        var script = @"
            pm.environment.set('new_token', 'abc123');
            pm.environment.set('timestamp', '2024-01-01');
        ";

        var result = await runner.run_pre_request_async(script, context, CancellationToken.None);

        result.success.Should().BeTrue();
        result.environment_updates.Should().NotBeNull();
        result.environment_updates!["new_token"].Should().Be("abc123");
        result.environment_updates["timestamp"].Should().Be("2024-01-01");
    }

    [Fact]
    public async Task run_pre_request_async_can_read_environment_variables()
    {
        var runner = new script_runner();
        var context = create_test_context();

        var script = @"
            var url = pm.environment.get('base_url');
            pm.test('env var exists', function() {
                pm.expect(url).to.equal('https://api.example.com');
            });
        ";

        var result = await runner.run_pre_request_async(script, context, CancellationToken.None);

        result.test_results.Should().HaveCount(1);
        result.test_results[0].passed.Should().BeTrue();
    }

    [Fact]
    public async Task run_post_response_async_can_access_response_code()
    {
        var runner = new script_runner();
        var response = create_test_response(200);
        var context = create_test_context(response);

        var script = @"
            pm.test('status is 200', function() {
                pm.expect(pm.response.code).to.equal(200);
            });
        ";

        var result = await runner.run_post_response_async(script, context, CancellationToken.None);

        result.test_results.Should().HaveCount(1);
        result.test_results[0].passed.Should().BeTrue();
    }

    [Fact]
    public async Task run_post_response_async_can_access_response_body()
    {
        var runner = new script_runner();
        var response = create_test_response(200, "{\"message\": \"hello\"}");
        var context = create_test_context(response);

        var script = @"
            pm.test('body contains message', function() {
                var body = pm.response.text();
                pm.expect(body).to.include('hello');
            });
        ";

        var result = await runner.run_post_response_async(script, context, CancellationToken.None);

        result.test_results.Should().HaveCount(1);
        result.test_results[0].passed.Should().BeTrue();
    }

    [Fact]
    public async Task run_pre_request_async_can_access_request_url()
    {
        var runner = new script_runner();
        var context = create_test_context();

        var script = @"
            pm.test('request url is correct', function() {
                pm.expect(pm.request.url).to.include('api.example.com');
            });
        ";

        var result = await runner.run_pre_request_async(script, context, CancellationToken.None);

        result.test_results.Should().HaveCount(1);
        result.test_results[0].passed.Should().BeTrue();
    }

    [Fact]
    public async Task run_pre_request_async_can_access_request_method()
    {
        var runner = new script_runner();
        var context = create_test_context();

        var script = @"
            pm.test('request method is GET', function() {
                pm.expect(pm.request.method).to.equal('GET');
            });
        ";

        var result = await runner.run_pre_request_async(script, context, CancellationToken.None);

        result.test_results.Should().HaveCount(1);
        result.test_results[0].passed.Should().BeTrue();
    }

    [Fact]
    public async Task run_pre_request_async_respects_cancellation_token()
    {
        var runner = new script_runner(timeout_ms: 10000);
        var context = create_test_context();
        var cts = new CancellationTokenSource();

        var script = "while(true) {}";

        cts.CancelAfter(100);

        var result = await runner.run_pre_request_async(script, context, cts.Token);

        result.success.Should().BeFalse();
        result.errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task run_pre_request_async_enforces_timeout_for_infinite_loop()
    {
        var runner = new script_runner(timeout_ms: 100);
        var context = create_test_context();

        var script = "while(true) {}";

        var result = await runner.run_pre_request_async(script, context, CancellationToken.None);

        result.success.Should().BeFalse();
        result.errors.Should().NotBeEmpty();
    }
}
