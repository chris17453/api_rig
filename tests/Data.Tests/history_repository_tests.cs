using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Core.Models;
using Data.Context;
using Data.Repositories;
using Xunit;

namespace Data.Tests;

public class history_repository_tests : IDisposable
{
    private readonly postman_clone_db_context _context;
    private readonly history_repository _repository;

    public history_repository_tests()
    {
        var options = new DbContextOptionsBuilder<postman_clone_db_context>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new postman_clone_db_context(options);
        _context.Database.EnsureCreated();
        _repository = new history_repository(_context);
    }

    [Fact]
    public async Task append_async_adds_entry_to_database()
    {
        // Arrange
        var entry = create_test_entry("Test Request");

        // Act
        await _repository.append_async(entry, CancellationToken.None);

        // Assert
        _context.history_entries.Should().ContainSingle();
    }

    [Fact]
    public async Task append_async_preserves_all_properties()
    {
        // Arrange
        var entry = new history_entry_model
        {
            id = "test-id-123",
            request_name = "Test Request",
            method = http_method.post,
            url = "https://api.example.com/users",
            status_code = 201,
            status_description = "Created",
            elapsed_ms = 150,
            response_size_bytes = 1024,
            environment_id = "env-1",
            environment_name = "Production",
            collection_id = "col-1",
            collection_name = "User API",
            executed_at = DateTime.UtcNow,
            error_message = null
        };

        // Act
        await _repository.append_async(entry, CancellationToken.None);
        var retrieved = await _repository.get_by_id_async("test-id-123", CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.request_name.Should().Be("Test Request");
        retrieved.method.Should().Be(http_method.post);
        retrieved.url.Should().Be("https://api.example.com/users");
        retrieved.status_code.Should().Be(201);
        retrieved.status_description.Should().Be("Created");
        retrieved.elapsed_ms.Should().Be(150);
        retrieved.response_size_bytes.Should().Be(1024);
        retrieved.environment_id.Should().Be("env-1");
        retrieved.environment_name.Should().Be("Production");
        retrieved.collection_id.Should().Be("col-1");
        retrieved.collection_name.Should().Be("User API");
    }

    [Fact]
    public async Task get_all_async_returns_all_entries()
    {
        // Arrange
        await _repository.append_async(create_test_entry("Request 1"), CancellationToken.None);
        await _repository.append_async(create_test_entry("Request 2"), CancellationToken.None);
        await _repository.append_async(create_test_entry("Request 3"), CancellationToken.None);

        // Act
        var entries = await _repository.get_all_async(CancellationToken.None);

        // Assert
        entries.Should().HaveCount(3);
    }

    [Fact]
    public async Task get_all_async_returns_entries_ordered_by_executed_at_descending()
    {
        // Arrange
        var older_entry = create_test_entry("Older Request") with 
        { 
            executed_at = DateTime.UtcNow.AddHours(-2) 
        };
        var middle_entry = create_test_entry("Middle Request") with 
        { 
            executed_at = DateTime.UtcNow.AddHours(-1) 
        };
        var newer_entry = create_test_entry("Newer Request") with 
        { 
            executed_at = DateTime.UtcNow 
        };

        await _repository.append_async(older_entry, CancellationToken.None);
        await _repository.append_async(newer_entry, CancellationToken.None);
        await _repository.append_async(middle_entry, CancellationToken.None);

        // Act
        var entries = await _repository.get_all_async(CancellationToken.None);

        // Assert
        entries[0].request_name.Should().Be("Newer Request");
        entries[1].request_name.Should().Be("Middle Request");
        entries[2].request_name.Should().Be("Older Request");
    }

    [Fact]
    public async Task get_all_async_returns_empty_list_when_no_entries()
    {
        // Act
        var entries = await _repository.get_all_async(CancellationToken.None);

        // Assert
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task get_recent_async_returns_specified_count()
    {
        // Arrange
        for (int i = 0; i < 10; i++)
        {
            await _repository.append_async(
                create_test_entry($"Request {i}") with { executed_at = DateTime.UtcNow.AddMinutes(-i) }, 
                CancellationToken.None);
        }

        // Act
        var entries = await _repository.get_recent_async(5, CancellationToken.None);

        // Assert
        entries.Should().HaveCount(5);
    }

    [Fact]
    public async Task get_recent_async_returns_most_recent_entries()
    {
        // Arrange
        await _repository.append_async(
            create_test_entry("Old Request") with { executed_at = DateTime.UtcNow.AddHours(-1) }, 
            CancellationToken.None);
        await _repository.append_async(
            create_test_entry("New Request") with { executed_at = DateTime.UtcNow }, 
            CancellationToken.None);

        // Act
        var entries = await _repository.get_recent_async(1, CancellationToken.None);

        // Assert
        entries.Should().ContainSingle();
        entries[0].request_name.Should().Be("New Request");
    }

    [Fact]
    public async Task get_recent_async_returns_all_when_count_exceeds_total()
    {
        // Arrange
        await _repository.append_async(create_test_entry("Request 1"), CancellationToken.None);
        await _repository.append_async(create_test_entry("Request 2"), CancellationToken.None);

        // Act
        var entries = await _repository.get_recent_async(100, CancellationToken.None);

        // Assert
        entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task get_by_id_async_returns_entry_when_exists()
    {
        // Arrange
        var entry = create_test_entry("Test Request") with { id = "specific-id" };
        await _repository.append_async(entry, CancellationToken.None);

        // Act
        var retrieved = await _repository.get_by_id_async("specific-id", CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.request_name.Should().Be("Test Request");
    }

    [Fact]
    public async Task get_by_id_async_returns_null_when_not_exists()
    {
        // Act
        var retrieved = await _repository.get_by_id_async("non-existent-id", CancellationToken.None);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task delete_async_removes_entry()
    {
        // Arrange
        var entry = create_test_entry("Test Request") with { id = "delete-me" };
        await _repository.append_async(entry, CancellationToken.None);

        // Act
        await _repository.delete_async("delete-me", CancellationToken.None);

        // Assert
        var retrieved = await _repository.get_by_id_async("delete-me", CancellationToken.None);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task delete_async_does_not_throw_when_id_not_exists()
    {
        // Act
        var act = async () => await _repository.delete_async("non-existent", CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task clear_all_async_removes_all_entries()
    {
        // Arrange
        await _repository.append_async(create_test_entry("Request 1"), CancellationToken.None);
        await _repository.append_async(create_test_entry("Request 2"), CancellationToken.None);
        await _repository.append_async(create_test_entry("Request 3"), CancellationToken.None);

        // Act
        await _repository.clear_all_async(CancellationToken.None);

        // Assert
        var entries = await _repository.get_all_async(CancellationToken.None);
        entries.Should().BeEmpty();
    }

    [Fact]
    public async Task clear_all_async_does_not_throw_when_empty()
    {
        // Act
        var act = async () => await _repository.clear_all_async(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task append_async_stores_request_snapshot()
    {
        // Arrange
        var request_snapshot = new http_request_model
        {
            name = "Test Request",
            method = http_method.post,
            url = "https://api.example.com",
            headers = [new key_value_pair_model { key = "Content-Type", value = "application/json" }]
        };

        var entry = create_test_entry("Test Request") with 
        { 
            request_snapshot = request_snapshot 
        };

        // Act
        await _repository.append_async(entry, CancellationToken.None);
        var retrieved = await _repository.get_by_id_async(entry.id, CancellationToken.None);

        // Assert
        retrieved!.request_snapshot.Should().NotBeNull();
        retrieved.request_snapshot!.url.Should().Be("https://api.example.com");
        retrieved.request_snapshot.method.Should().Be(http_method.post);
    }

    [Fact]
    public async Task append_async_stores_response_snapshot()
    {
        // Arrange
        var response_snapshot = new http_response_model
        {
            status_code = 200,
            status_description = "OK",
            body_string = "{\"success\": true}",
            elapsed_ms = 100
        };

        var entry = create_test_entry("Test Request") with 
        { 
            response_snapshot = response_snapshot 
        };

        // Act
        await _repository.append_async(entry, CancellationToken.None);
        var retrieved = await _repository.get_by_id_async(entry.id, CancellationToken.None);

        // Assert
        retrieved!.response_snapshot.Should().NotBeNull();
        retrieved.response_snapshot!.status_code.Should().Be(200);
        retrieved.response_snapshot.body_string.Should().Be("{\"success\": true}");
    }

    private static history_entry_model create_test_entry(string name)
    {
        return new history_entry_model
        {
            id = Guid.NewGuid().ToString(),
            request_name = name,
            method = http_method.get,
            url = "https://api.example.com/test",
            status_code = 200,
            status_description = "OK",
            executed_at = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
