using FluentAssertions;
using PostmanClone.Scripting.Api;

namespace PostmanClone.Scripting.Tests;

public class pm_test_collector_tests
{
    [Fact]
    public void test_collects_passed_result_when_assertion_succeeds()
    {
        var collector = new pm_test_collector();

        collector.test("should pass", () => { });

        collector.results.Should().HaveCount(1);
        collector.results[0].name.Should().Be("should pass");
        collector.results[0].passed.Should().BeTrue();
        collector.results[0].error_message.Should().BeNull();
    }

    [Fact]
    public void test_collects_failed_result_when_assertion_throws()
    {
        var collector = new pm_test_collector();

        collector.test("should fail", () => throw new Exception("Test failed"));

        collector.results.Should().HaveCount(1);
        collector.results[0].name.Should().Be("should fail");
        collector.results[0].passed.Should().BeFalse();
        collector.results[0].error_message.Should().Be("Test failed");
    }

    [Fact]
    public void test_collects_multiple_results_when_multiple_tests_run()
    {
        var collector = new pm_test_collector();

        collector.test("test 1", () => { });
        collector.test("test 2", () => throw new Exception("error"));
        collector.test("test 3", () => { });

        collector.results.Should().HaveCount(3);
        collector.results.Count(r => r.passed).Should().Be(2);
        collector.results.Count(r => !r.passed).Should().Be(1);
    }

    [Fact]
    public void log_collects_messages_when_called()
    {
        var collector = new pm_test_collector();

        collector.log("message 1");
        collector.log("message", "2");

        collector.logs.Should().HaveCount(2);
        collector.logs[0].Should().Be("message 1");
        collector.logs[1].Should().Be("message 2");
    }
}
