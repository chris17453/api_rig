using FluentAssertions;
using Scripting.Api;

namespace Scripting.Tests;

public class pm_environment_api_tests
{
    [Fact]
    public void get_returns_initial_value_when_key_exists()
    {
        var initial = new Dictionary<string, string> { { "base_url", "https://api.example.com" } };
        var env = new pm_environment_api(initial);

        var result = env.get("base_url");

        result.Should().Be("https://api.example.com");
    }

    [Fact]
    public void get_returns_null_when_key_does_not_exist()
    {
        var env = new pm_environment_api(new Dictionary<string, string>());

        var result = env.get("missing_key");

        result.Should().BeNull();
    }

    [Fact]
    public void set_stores_value_in_updates()
    {
        var env = new pm_environment_api(new Dictionary<string, string>());

        env.set("token", "abc123");

        env.updates.Should().ContainKey("token");
        env.updates["token"].Should().Be("abc123");
    }

    [Fact]
    public void get_returns_updated_value_when_key_was_set()
    {
        var initial = new Dictionary<string, string> { { "token", "old" } };
        var env = new pm_environment_api(initial);

        env.set("token", "new");
        var result = env.get("token");

        result.Should().Be("new");
    }

    [Fact]
    public void has_returns_true_when_key_exists_in_initial()
    {
        var initial = new Dictionary<string, string> { { "key", "value" } };
        var env = new pm_environment_api(initial);

        env.has("key").Should().BeTrue();
    }

    [Fact]
    public void has_returns_true_when_key_exists_in_updates()
    {
        var env = new pm_environment_api(new Dictionary<string, string>());

        env.set("new_key", "value");

        env.has("new_key").Should().BeTrue();
    }

    [Fact]
    public void has_returns_false_when_key_does_not_exist()
    {
        var env = new pm_environment_api(new Dictionary<string, string>());

        env.has("missing").Should().BeFalse();
    }

    [Fact]
    public void to_object_merges_initial_and_updates()
    {
        var initial = new Dictionary<string, string> { { "a", "1" }, { "b", "2" } };
        var env = new pm_environment_api(initial);

        env.set("b", "updated");
        env.set("c", "3");

        var result = env.to_object() as Dictionary<string, string>;

        result.Should().NotBeNull();
        result!["a"].Should().Be("1");
        result["b"].Should().Be("updated");
        result["c"].Should().Be("3");
    }
}
