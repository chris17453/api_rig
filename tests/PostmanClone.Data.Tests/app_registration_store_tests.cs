using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using PostmanClone.Core.Models;
using PostmanClone.Data.Context;
using PostmanClone.Data.Stores;
using Xunit;

namespace PostmanClone.Data.Tests;

public class app_registration_store_tests : IDisposable
{
    private readonly postman_clone_db_context _context;
    private readonly app_registration_store _store;

    public app_registration_store_tests()
    {
        var options = new DbContextOptionsBuilder<postman_clone_db_context>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new postman_clone_db_context(options);
        _store = new app_registration_store(_context);
    }

    [Fact]
    public async Task is_registered_returns_false_when_no_registration_exists()
    {
        // Act
        var result = await _store.is_registered_async();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task save_registration_stores_registration_successfully()
    {
        // Arrange
        var registration = new app_registration_model
        {
            id = Guid.NewGuid().ToString(),
            user_email = "test@example.com",
            user_name = "Test User",
            organization = "Test Org",
            opted_in = true,
            registered_at = DateTime.UtcNow
        };

        // Act
        await _store.save_registration_async(registration);
        var is_registered = await _store.is_registered_async();
        var retrieved = await _store.get_registration_async();

        // Assert
        is_registered.Should().BeTrue();
        retrieved.Should().NotBeNull();
        retrieved!.user_email.Should().Be("test@example.com");
        retrieved.user_name.Should().Be("Test User");
        retrieved.organization.Should().Be("Test Org");
        retrieved.opted_in.Should().BeTrue();
    }

    [Fact]
    public async Task get_registration_returns_null_when_no_registration_exists()
    {
        // Act
        var result = await _store.get_registration_async();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task update_registration_updates_existing_registration()
    {
        // Arrange
        var registration = new app_registration_model
        {
            id = Guid.NewGuid().ToString(),
            user_email = "test@example.com",
            user_name = "Test User",
            organization = "Test Org",
            opted_in = true,
            registered_at = DateTime.UtcNow
        };

        await _store.save_registration_async(registration);

        var updated_registration = registration with
        {
            user_email = "updated@example.com",
            user_name = "Updated User",
            organization = "Updated Org",
            opted_in = false
        };

        // Act
        await _store.update_registration_async(updated_registration);
        var retrieved = await _store.get_registration_async();

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.id.Should().Be(registration.id);
        retrieved.user_email.Should().Be("updated@example.com");
        retrieved.user_name.Should().Be("Updated User");
        retrieved.organization.Should().Be("Updated Org");
        retrieved.opted_in.Should().BeFalse();
        retrieved.last_updated_at.Should().NotBeNull();
    }

    [Fact]
    public async Task save_registration_with_opted_out_stores_minimal_data()
    {
        // Arrange
        var registration = new app_registration_model
        {
            id = Guid.NewGuid().ToString(),
            user_email = string.Empty,
            user_name = string.Empty,
            organization = string.Empty,
            opted_in = false,
            registered_at = DateTime.UtcNow
        };

        // Act
        await _store.save_registration_async(registration);
        var is_registered = await _store.is_registered_async();
        var retrieved = await _store.get_registration_async();

        // Assert
        is_registered.Should().BeTrue();
        retrieved.Should().NotBeNull();
        retrieved!.user_email.Should().BeEmpty();
        retrieved.user_name.Should().BeEmpty();
        retrieved.organization.Should().BeEmpty();
        retrieved.opted_in.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
