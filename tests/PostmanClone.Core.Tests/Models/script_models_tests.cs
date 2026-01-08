using FluentAssertions;
using PostmanClone.Core.Models;

namespace PostmanClone.Core.Tests.Models;

public class script_models_tests
{
    [Fact]
    public void script_result_aggregates_test_results_when_multiple_tests_run()
    {
        var test_results = new List<test_result_model>
        {
            new() { name = "Status is 200", passed = true },
            new() { name = "Body contains user", passed = true },
            new() { name = "Response time < 500ms", passed = false, error_message = "Response took 600ms" }
        };

        var result = new script_execution_result_model
        {
            success = false,
            test_results = test_results,
            execution_time_ms = 50
        };

        result.test_results.Should().HaveCount(3);
        result.test_results.Count(t => t.passed).Should().Be(2);
        result.test_results.Count(t => !t.passed).Should().Be(1);
    }

    [Fact]
    public void script_context_contains_request_and_environment_when_pre_request()
    {
        var request = new http_request_model
        {
            name = "Test",
            method = http_method.get,
            url = "https://api.example.com"
        };

        var environment = new environment_model
        {
            name = "Test Env",
            variables = new Dictionary<string, string> { { "key", "value" } }
        };

        var context = new script_context_model
        {
            phase = script_phase.pre_request,
            request = request,
            environment = environment
        };

        context.phase.Should().Be(script_phase.pre_request);
        context.request.Should().NotBeNull();
        context.response.Should().BeNull();
        context.environment.Should().NotBeNull();
    }

    [Fact]
    public void script_context_contains_response_when_post_response()
    {
        var request = new http_request_model
        {
            name = "Test",
            method = http_method.get,
            url = "https://api.example.com"
        };

        var response = new http_response_model
        {
            status_code = 200,
            status_description = "OK"
        };

        var environment = new environment_model { name = "Test Env" };

        var context = new script_context_model
        {
            phase = script_phase.post_response,
            request = request,
            response = response,
            environment = environment
        };

        context.phase.Should().Be(script_phase.post_response);
        context.response.Should().NotBeNull();
        context.response!.status_code.Should().Be(200);
    }

    [Fact]
    public void script_result_stores_environment_updates_when_script_modifies_env()
    {
        var updates = new Dictionary<string, string>
        {
            { "token", "new-token-value" },
            { "timestamp", "2024-01-01" }
        };

        var result = new script_execution_result_model
        {
            success = true,
            environment_updates = updates
        };

        result.environment_updates.Should().HaveCount(2);
        result.environment_updates!["token"].Should().Be("new-token-value");
    }

    [Fact]
    public void test_result_stores_error_message_when_test_fails()
    {
        var test_result = new test_result_model
        {
            name = "Response contains data",
            passed = false,
            error_message = "Expected 'data' but got 'error'"
        };

        test_result.passed.Should().BeFalse();
        test_result.error_message.Should().NotBeNullOrEmpty();
    }
}
