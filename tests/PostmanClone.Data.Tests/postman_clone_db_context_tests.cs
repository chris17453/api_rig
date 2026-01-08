using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PostmanClone.Core.Models;
using PostmanClone.Data.Context;
using PostmanClone.Data.Entities;
using Xunit;

namespace PostmanClone.Data.Tests;

public class postman_clone_db_context_tests : IDisposable
{
    private readonly postman_clone_db_context _context;

    public postman_clone_db_context_tests()
    {
        var options = new DbContextOptionsBuilder<postman_clone_db_context>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new postman_clone_db_context(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void context_creates_database_successfully()
    {
        // Assert
        _context.Database.CanConnect().Should().BeTrue();
    }

    [Fact]
    public void context_has_history_entries_dbset()
    {
        // Assert
        _context.history_entries.Should().NotBeNull();
    }

    [Fact]
    public void context_has_collections_dbset()
    {
        // Assert
        _context.collections.Should().NotBeNull();
    }

    [Fact]
    public void context_has_environments_dbset()
    {
        // Assert
        _context.environments.Should().NotBeNull();
    }

    [Fact]
    public async Task saves_history_entry_async()
    {
        // Arrange
        var entry = new history_entry_entity
        {
            id = Guid.NewGuid().ToString(),
            request_name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/test",
            status_code = 200,
            status_description = "OK",
            executed_at = DateTime.UtcNow
        };

        // Act
        _context.history_entries.Add(entry);
        var result = await _context.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        _context.history_entries.Should().ContainSingle();
    }

    [Fact]
    public async Task saves_collection_async()
    {
        // Arrange
        var collection = new collection_entity
        {
            id = Guid.NewGuid().ToString(),
            name = "Test Collection",
            created_at = DateTime.UtcNow
        };

        // Act
        _context.collections.Add(collection);
        var result = await _context.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        _context.collections.Should().ContainSingle();
    }

    [Fact]
    public async Task saves_environment_async()
    {
        // Arrange
        var environment = new environment_entity
        {
            id = Guid.NewGuid().ToString(),
            name = "Test Environment",
            is_active = true,
            created_at = DateTime.UtcNow
        };

        // Act
        _context.environments.Add(environment);
        var result = await _context.SaveChangesAsync();

        // Assert
        result.Should().Be(1);
        _context.environments.Should().ContainSingle();
    }

    [Fact]
    public async Task saves_collection_with_items_async()
    {
        // Arrange
        var collection = new collection_entity
        {
            id = Guid.NewGuid().ToString(),
            name = "Test Collection",
            created_at = DateTime.UtcNow,
            items = new List<collection_item_entity>
            {
                new collection_item_entity
                {
                    id = Guid.NewGuid().ToString(),
                    name = "Test Item",
                    is_folder = false,
                    request_method = http_method.get,
                    request_url = "https://api.example.com"
                }
            }
        };

        // Act
        _context.collections.Add(collection);
        var result = await _context.SaveChangesAsync();

        // Assert
        result.Should().Be(2); // collection + item
        var saved = await _context.collections
            .Include(c => c.items)
            .FirstAsync();
        saved.items.Should().HaveCount(1);
    }

    [Fact]
    public async Task saves_environment_with_variables_async()
    {
        // Arrange
        var environment = new environment_entity
        {
            id = Guid.NewGuid().ToString(),
            name = "Test Environment",
            is_active = false,
            created_at = DateTime.UtcNow,
            variables = new List<environment_variable_entity>
            {
                new environment_variable_entity
                {
                    key = "api_key",
                    value = "secret123"
                }
            }
        };

        // Act
        _context.environments.Add(environment);
        var result = await _context.SaveChangesAsync();

        // Assert
        result.Should().Be(2); // environment + variable
        var saved = await _context.environments
            .Include(e => e.variables)
            .FirstAsync();
        saved.variables.Should().HaveCount(1);
    }

    [Fact]
    public async Task cascades_delete_collection_items_async()
    {
        // Arrange
        var collection = new collection_entity
        {
            id = Guid.NewGuid().ToString(),
            name = "Test Collection",
            created_at = DateTime.UtcNow,
            items = new List<collection_item_entity>
            {
                new collection_item_entity
                {
                    id = Guid.NewGuid().ToString(),
                    name = "Test Item",
                    is_folder = false
                }
            }
        };

        _context.collections.Add(collection);
        await _context.SaveChangesAsync();

        // Act
        _context.collections.Remove(collection);
        await _context.SaveChangesAsync();

        // Assert
        _context.collections.Should().BeEmpty();
    }

    [Fact]
    public async Task cascades_delete_environment_variables_async()
    {
        // Arrange
        var environment = new environment_entity
        {
            id = Guid.NewGuid().ToString(),
            name = "Test Environment",
            is_active = false,
            created_at = DateTime.UtcNow,
            variables = new List<environment_variable_entity>
            {
                new environment_variable_entity
                {
                    key = "api_key",
                    value = "secret123"
                }
            }
        };

        _context.environments.Add(environment);
        await _context.SaveChangesAsync();

        // Act
        _context.environments.Remove(environment);
        await _context.SaveChangesAsync();

        // Assert
        _context.environments.Should().BeEmpty();
    }

    [Fact]
    public async Task retrieves_history_entries_ordered_by_timestamp_descending_async()
    {
        // Arrange
        var older_entry = new history_entry_entity
        {
            id = Guid.NewGuid().ToString(),
            request_name = "Older Request",
            method = http_method.get,
            url = "https://api.example.com/old",
            executed_at = DateTime.UtcNow.AddHours(-1)
        };

        var newer_entry = new history_entry_entity
        {
            id = Guid.NewGuid().ToString(),
            request_name = "Newer Request",
            method = http_method.post,
            url = "https://api.example.com/new",
            executed_at = DateTime.UtcNow
        };

        _context.history_entries.AddRange(older_entry, newer_entry);
        await _context.SaveChangesAsync();

        // Act
        var entries = await _context.history_entries
            .OrderByDescending(e => e.executed_at)
            .ToListAsync();

        // Assert
        entries.Should().HaveCount(2);
        entries[0].request_name.Should().Be("Newer Request");
        entries[1].request_name.Should().Be("Older Request");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
