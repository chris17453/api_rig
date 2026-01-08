using FluentAssertions;
using PostmanClone.Core.Models;

namespace PostmanClone.Core.Tests.Models;

public class environment_model_tests
{
    [Fact]
    public void environment_stores_variables_when_provided()
    {
        var variables = new Dictionary<string, string>
        {
            { "base_url", "https://api.example.com" },
            { "api_key", "secret-key" }
        };

        var environment = new environment_model
        {
            name = "Production",
            variables = variables
        };

        environment.variables.Should().HaveCount(2);
        environment.variables["base_url"].Should().Be("https://api.example.com");
        environment.variables["api_key"].Should().Be("secret-key");
    }

    [Fact]
    public void environment_generates_unique_id_when_created()
    {
        var env1 = new environment_model { name = "Dev" };
        var env2 = new environment_model { name = "Prod" };

        env1.id.Should().NotBe(env2.id);
    }

    [Fact]
    public void environment_defaults_to_empty_variables_when_not_specified()
    {
        var environment = new environment_model { name = "Empty" };

        environment.variables.Should().BeEmpty();
    }
}
