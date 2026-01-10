using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Core.Models;
using Data.Context;
using Data.Repositories;
using Xunit;

namespace Data.Tests;

public class collection_repository_tests : IDisposable
{
    private readonly postman_clone_db_context _context;
    private readonly collection_repository _repository;
    private readonly string _test_files_path;

    public collection_repository_tests()
    {
        var options = new DbContextOptionsBuilder<postman_clone_db_context>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new postman_clone_db_context(options);
        _context.Database.EnsureCreated();
        _repository = new collection_repository(_context);
        _test_files_path = Path.Combine(Path.GetTempPath(), "Tests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_test_files_path);
    }

    [Fact]
    public async Task save_async_adds_new_collection_to_database()
    {
        // Arrange
        var collection = create_test_collection("Test Collection");

        // Act
        await _repository.save_async(collection, CancellationToken.None);

        // Assert
        _context.collections.Should().ContainSingle();
    }

    [Fact]
    public async Task save_async_updates_existing_collection()
    {
        // Arrange
        var collection = create_test_collection("Original Name");
        await _repository.save_async(collection, CancellationToken.None);

        var updated_collection = collection with { name = "Updated Name" };

        // Act
        await _repository.save_async(updated_collection, CancellationToken.None);

        // Assert
        _context.collections.Should().ContainSingle();
        var saved = await _context.collections.FirstAsync();
        saved.name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task save_async_preserves_all_properties()
    {
        // Arrange
        var collection = new postman_collection_model
        {
            id = "test-id",
            name = "Full Collection",
            description = "Test description",
            version = "1.0.0",
            items = [
                new collection_item_model
                {
                    id = "item-1",
                    name = "Get Users",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Get Users",
                        method = http_method.get,
                        url = "https://api.example.com/users"
                    }
                }
            ],
            variables = [
                new key_value_pair_model { key = "baseUrl", value = "https://api.example.com" }
            ],
            created_at = DateTime.UtcNow
        };

        // Act
        await _repository.save_async(collection, CancellationToken.None);
        var retrieved = await _repository.get_by_id_async("test-id", CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.name.Should().Be("Full Collection");
        retrieved.description.Should().Be("Test description");
        retrieved.version.Should().Be("1.0.0");
        retrieved.items.Should().HaveCount(1);
        retrieved.items[0].name.Should().Be("Get Users");
        retrieved.variables.Should().HaveCount(1);
        retrieved.variables[0].key.Should().Be("baseUrl");
    }

    [Fact]
    public async Task list_all_async_returns_all_collections()
    {
        // Arrange
        await _repository.save_async(create_test_collection("Collection 1"), CancellationToken.None);
        await _repository.save_async(create_test_collection("Collection 2"), CancellationToken.None);
        await _repository.save_async(create_test_collection("Collection 3"), CancellationToken.None);

        // Act
        var collections = await _repository.list_all_async(CancellationToken.None);

        // Assert
        collections.Should().HaveCount(3);
    }

    [Fact]
    public async Task list_all_async_returns_empty_when_no_collections()
    {
        // Act
        var collections = await _repository.list_all_async(CancellationToken.None);

        // Assert
        collections.Should().BeEmpty();
    }

    [Fact]
    public async Task get_by_id_async_returns_collection_when_exists()
    {
        // Arrange
        var collection = create_test_collection("Test Collection") with { id = "find-me" };
        await _repository.save_async(collection, CancellationToken.None);

        // Act
        var retrieved = await _repository.get_by_id_async("find-me", CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.name.Should().Be("Test Collection");
    }

    [Fact]
    public async Task get_by_id_async_returns_null_when_not_exists()
    {
        // Act
        var retrieved = await _repository.get_by_id_async("non-existent", CancellationToken.None);

        // Assert
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task get_by_id_async_includes_nested_items()
    {
        // Arrange
        var collection = new postman_collection_model
        {
            id = "nested-test",
            name = "Nested Collection",
            items = [
                new collection_item_model
                {
                    id = "folder-1",
                    name = "Users Folder",
                    is_folder = true,
                    children = [
                        new collection_item_model
                        {
                            id = "request-1",
                            name = "Get User",
                            is_folder = false,
                            request = new http_request_model
                            {
                                name = "Get User",
                                method = http_method.get,
                                url = "https://api.example.com/users/1"
                            }
                        }
                    ]
                }
            ]
        };

        await _repository.save_async(collection, CancellationToken.None);

        // Act
        var retrieved = await _repository.get_by_id_async("nested-test", CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.items.Should().HaveCount(1);
        retrieved.items[0].is_folder.Should().BeTrue();
        retrieved.items[0].children.Should().HaveCount(1);
        retrieved.items[0].children![0].name.Should().Be("Get User");
    }

    [Fact]
    public async Task delete_async_removes_collection()
    {
        // Arrange
        var collection = create_test_collection("Delete Me") with { id = "delete-me" };
        await _repository.save_async(collection, CancellationToken.None);

        // Act
        await _repository.delete_async("delete-me", CancellationToken.None);

        // Assert
        var retrieved = await _repository.get_by_id_async("delete-me", CancellationToken.None);
        retrieved.Should().BeNull();
    }

    [Fact]
    public async Task delete_async_does_not_throw_when_not_exists()
    {
        // Act
        var act = async () => await _repository.delete_async("non-existent", CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task export_to_json_async_returns_valid_json()
    {
        // Arrange
        var collection = create_test_collection("Export Test") with { id = "export-id" };
        await _repository.save_async(collection, CancellationToken.None);

        // Act
        var json = await _repository.export_to_json_async("export-id", CancellationToken.None);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("Export Test");
        json.Should().Contain("export-id");
    }

    [Fact]
    public async Task export_to_json_async_throws_when_not_found()
    {
        // Act
        var act = async () => await _repository.export_to_json_async("non-existent", CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task export_to_file_async_creates_file()
    {
        // Arrange
        var collection = create_test_collection("File Export") with { id = "file-export-id" };
        await _repository.save_async(collection, CancellationToken.None);
        var file_path = Path.Combine(_test_files_path, "export.json");

        // Act
        await _repository.export_to_file_async("file-export-id", file_path, CancellationToken.None);

        // Assert
        File.Exists(file_path).Should().BeTrue();
        var content = await File.ReadAllTextAsync(file_path);
        content.Should().Contain("File Export");
    }

    [Fact]
    public async Task import_from_json_async_creates_collection()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "imported-id",
                "name": "Imported Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": []
        }
        """;

        // Act
        var collection = await _repository.import_from_json_async(json, CancellationToken.None);

        // Assert
        collection.Should().NotBeNull();
        collection.name.Should().Be("Imported Collection");
    }

    [Fact]
    public async Task import_from_file_async_creates_collection()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "file-import-id",
                "name": "File Imported Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": []
        }
        """;
        var file_path = Path.Combine(_test_files_path, "import.json");
        await File.WriteAllTextAsync(file_path, json);

        // Act
        var collection = await _repository.import_from_file_async(file_path, CancellationToken.None);

        // Assert
        collection.Should().NotBeNull();
        collection.name.Should().Be("File Imported Collection");
    }

    [Fact]
    public async Task import_from_file_async_throws_when_file_not_found()
    {
        // Act
        var act = async () => await _repository.import_from_file_async(
            Path.Combine(_test_files_path, "non-existent.json"), 
            CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task save_async_preserves_request_with_headers_and_body()
    {
        // Arrange
        var collection = new postman_collection_model
        {
            id = "headers-body-test",
            name = "Headers Body Test",
            items = [
                new collection_item_model
                {
                    id = "post-request",
                    name = "Create User",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Create User",
                        method = http_method.post,
                        url = "https://api.example.com/users",
                        headers = [
                            new key_value_pair_model { key = "Content-Type", value = "application/json" },
                            new key_value_pair_model { key = "Authorization", value = "Bearer token123" }
                        ],
                        body = new request_body_model
                        {
                            body_type = request_body_type.raw,
                            raw_content = "{\"name\": \"John\"}"
                        }
                    }
                }
            ]
        };

        // Act
        await _repository.save_async(collection, CancellationToken.None);
        var retrieved = await _repository.get_by_id_async("headers-body-test", CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        var request = retrieved!.items[0].request;
        request.Should().NotBeNull();
        request!.headers.Should().HaveCount(2);
        request.body.Should().NotBeNull();
        request.body!.raw_content.Should().Be("{\"name\": \"John\"}");
    }

    private static postman_collection_model create_test_collection(string name)
    {
        return new postman_collection_model
        {
            id = Guid.NewGuid().ToString(),
            name = name,
            created_at = DateTime.UtcNow
        };
    }

    public void Dispose()
    {
        _context.Dispose();
        if (Directory.Exists(_test_files_path))
        {
            Directory.Delete(_test_files_path, true);
        }
    }
}
