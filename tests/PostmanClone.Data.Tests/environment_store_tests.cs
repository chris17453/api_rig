using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PostmanClone.Core.Models;
using PostmanClone.Data.Context;
using PostmanClone.Data.Stores;
using Xunit;

namespace PostmanClone.Data.Tests;

public class environment_store_tests : IDisposable
{
    private readonly postman_clone_db_context _context;
    private readonly environment_store _store;

    public environment_store_tests()
    {
        var options = new DbContextOptionsBuilder<postman_clone_db_context>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new postman_clone_db_context(options);
        _context.Database.EnsureCreated();
        _store = new environment_store(_context);
    }

    [Fact]
    public async Task save_async_adds_new_environment()
    {
        // Arrange
        var environment = create_test_environment("Development");

        // Act
        await _store.save_async(environment, CancellationToken.None);

        // Assert
        _context.environments.Should().ContainSingle();
    }

    [Fact]
    public async Task save_async_updates_existing_environment()
    {
        // Arrange
        var environment = create_test_environment("Dev") with { id = "update-me" };
        await _store.save_async(environment, CancellationToken.None);

        var updated = environment with { name = "Development" };

        // Act
        await _store.save_async(updated, CancellationToken.None);

        // Assert
        _context.environments.Should().ContainSingle();
        var saved = await _context.environments.FirstAsync();
        saved.name.Should().Be("Development");
    }

    [Fact]
    public async Task save_async_preserves_variables()
    {
        // Arrange
        var environment = new environment_model
        {
            id = "with-vars",
            name = "Production",
            variables = new Dictionary<string, string>
            {
                { "baseUrl", "https://api.example.com" },
                { "apiKey", "secret-key-123" }
            }
        };

        // Act
        await _store.save_async(environment, CancellationToken.None);
        var retrieved = await _store.get_by_id_async("with-vars", CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.variables.Should().HaveCount(2);
        retrieved.variables["baseUrl"].Should().Be("https://api.example.com");
        retrieved.variables["apiKey"].Should().Be("secret-key-123");
    }

    [Fact]
    public async Task list_all_async_returns_all_environments()
    {
        // Arrange
        await _store.save_async(create_test_environment("Dev"), CancellationToken.None);
        await _store.save_async(create_test_environment("Staging"), CancellationToken.None);
        await _store.save_async(create_test_environment("Prod"), CancellationToken.None);

        // Act
        var environments = await _store.list_all_async(CancellationToken.None);

        // Assert
        environments.Should().HaveCount(3);
    }

    [Fact]
    public async Task list_all_async_returns_empty_when_no_environments()
    {
        // Act
        var environments = await _store.list_all_async(CancellationToken.None);

        // Assert
        environments.Should().BeEmpty();
    }

    [Fact]
    public async Task get_by_id_async_returns_environment_when_exists()
    {
        // Arrange
        var environment = create_test_environment("Test Env") with { id = "find-me" };
        await _store.save_async(environment, CancellationToken.None);

        // Act
        var retrieved = await _store.get_by_id_async("find-me", CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.name.Should().Be("Test Env");
    }

    [Fact]
    public async Task get_by_id_async_returns_null_when_not_exists()
    {
        // Act
        var retrieved = await _store.get_by_id_async("non-existent", CancellationToken.None);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task get_active_async_returns_null_when_no_active_environment()
    {
        // Arrange
        await _store.save_async(create_test_environment("Inactive"), CancellationToken.None);

        // Act
        var active = await _store.get_active_async(CancellationToken.None);

        // Assert
        active.Should().BeNull();
    }

    [Fact]
    public async Task set_active_async_activates_environment()
    {
        // Arrange
        var environment = create_test_environment("To Activate") with { id = "activate-me" };
        await _store.save_async(environment, CancellationToken.None);

        // Act
        await _store.set_active_async("activate-me", CancellationToken.None);
        var active = await _store.get_active_async(CancellationToken.None);

        // Assert
        active.Should().NotBeNull();
        active!.id.Should().Be("activate-me");
    }

    [Fact]
    public async Task set_active_async_deactivates_previous_active()
    {
        // Arrange
        var env1 = create_test_environment("Env 1") with { id = "env-1" };
        var env2 = create_test_environment("Env 2") with { id = "env-2" };
        await _store.save_async(env1, CancellationToken.None);
        await _store.save_async(env2, CancellationToken.None);
        await _store.set_active_async("env-1", CancellationToken.None);

        // Act
        await _store.set_active_async("env-2", CancellationToken.None);

        // Assert
        var active = await _store.get_active_async(CancellationToken.None);
        active.Should().NotBeNull();
        active!.id.Should().Be("env-2");

        var env1_entity = await _context.environments.FirstAsync(e => e.id == "env-1");
        env1_entity.is_active.Should().BeFalse();
    }

    [Fact]
    public async Task set_active_async_with_null_deactivates_all()
    {
        // Arrange
        var environment = create_test_environment("Active") with { id = "active-id" };
        await _store.save_async(environment, CancellationToken.None);
        await _store.set_active_async("active-id", CancellationToken.None);

        // Act
        await _store.set_active_async(null, CancellationToken.None);

        // Assert
        var active = await _store.get_active_async(CancellationToken.None);
        active.Should().BeNull();
    }

    [Fact]
    public async Task delete_async_removes_environment()
    {
        // Arrange
        var environment = create_test_environment("Delete Me") with { id = "delete-me" };
        await _store.save_async(environment, CancellationToken.None);

        // Act
        await _store.delete_async("delete-me", CancellationToken.None);

        // Assert
        var retrieved = await _store.get_by_id_async("delete-me", CancellationToken.None);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task delete_async_does_not_throw_when_not_exists()
    {
        // Act
        var act = async () => await _store.delete_async("non-existent", CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task get_variable_async_returns_value_from_active_environment()
    {
        // Arrange
        var environment = new environment_model
        {
            id = "var-env",
            name = "Variables Env",
            variables = new Dictionary<string, string>
            {
                { "apiUrl", "https://api.example.com" }
            }
        };
        await _store.save_async(environment, CancellationToken.None);
        await _store.set_active_async("var-env", CancellationToken.None);

        // Act
        var value = await _store.get_variable_async("apiUrl", CancellationToken.None);

        // Assert
        value.Should().Be("https://api.example.com");
    }

    [Fact]
    public async Task get_variable_async_returns_null_when_key_not_found()
    {
        // Arrange
        var environment = new environment_model
        {
            id = "var-env-2",
            name = "Variables Env",
            variables = new Dictionary<string, string>
            {
                { "existingKey", "value" }
            }
        };
        await _store.save_async(environment, CancellationToken.None);
        await _store.set_active_async("var-env-2", CancellationToken.None);

        // Act
        var value = await _store.get_variable_async("nonExistentKey", CancellationToken.None);

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public async Task get_variable_async_returns_null_when_no_active_environment()
    {
        // Act
        var value = await _store.get_variable_async("anyKey", CancellationToken.None);

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public async Task set_variable_async_adds_variable_to_active_environment()
    {
        // Arrange
        var environment = create_test_environment("Set Var Env") with { id = "set-var-env" };
        await _store.save_async(environment, CancellationToken.None);
        await _store.set_active_async("set-var-env", CancellationToken.None);

        // Act
        await _store.set_variable_async("newKey", "newValue", CancellationToken.None);

        // Assert
        var value = await _store.get_variable_async("newKey", CancellationToken.None);
        value.Should().Be("newValue");
    }

    [Fact]
    public async Task set_variable_async_updates_existing_variable()
    {
        // Arrange
        var environment = new environment_model
        {
            id = "update-var-env",
            name = "Update Var Env",
            variables = new Dictionary<string, string>
            {
                { "existingKey", "oldValue" }
            }
        };
        await _store.save_async(environment, CancellationToken.None);
        await _store.set_active_async("update-var-env", CancellationToken.None);

        // Act
        await _store.set_variable_async("existingKey", "newValue", CancellationToken.None);

        // Assert
        var value = await _store.get_variable_async("existingKey", CancellationToken.None);
        value.Should().Be("newValue");
    }

    [Fact]
    public async Task set_variable_async_throws_when_no_active_environment()
    {
        // Act
        var act = async () => await _store.set_variable_async("key", "value", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task save_async_updates_variables_correctly()
    {
        // Arrange
        var environment = new environment_model
        {
            id = "multi-var-env",
            name = "Multi Var Env",
            variables = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            }
        };
        await _store.save_async(environment, CancellationToken.None);

        // Update with different variables
        var updated = environment with
        {
            variables = new Dictionary<string, string>
            {
                { "key1", "updated1" },
                { "key3", "value3" }
            }
        };

        // Act
        await _store.save_async(updated, CancellationToken.None);
        var retrieved = await _store.get_by_id_async("multi-var-env", CancellationToken.None);

        // Assert
        retrieved!.variables.Should().HaveCount(2);
        retrieved.variables["key1"].Should().Be("updated1");
        retrieved.variables["key3"].Should().Be("value3");
        retrieved.variables.Should().NotContainKey("key2");
    }

    private static environment_model create_test_environment(string name)
    {
        return new environment_model
        {
            id = Guid.NewGuid().ToString(),
            name = name,
            variables = new Dictionary<string, string>()
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
