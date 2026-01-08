using FluentAssertions;
using PostmanClone.Core.Models;
using PostmanClone.Data.Parsers;
using Xunit;

namespace PostmanClone.Data.Tests;

public class postman_v20_parser_tests
{
    private readonly postman_v20_parser _parser = new();

    [Fact]
    public void can_parse_returns_true_for_v20_schema()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": []
        }
        """;

        // Act
        var result = _parser.can_parse(json);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void can_parse_returns_false_for_v21_schema()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": []
        }
        """;

        // Act
        var result = _parser.can_parse(json);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void can_parse_returns_false_for_invalid_json()
    {
        // Arrange
        var json = "not valid json";

        // Act
        var result = _parser.can_parse(json);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void parse_extracts_collection_info()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "abc-123",
                "name": "My API Collection",
                "description": "Collection description",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": []
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        collection.id.Should().Be("abc-123");
        collection.name.Should().Be("My API Collection");
        collection.description.Should().Be("Collection description");
    }

    [Fact]
    public void parse_extracts_simple_request()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": [
                {
                    "name": "Get Users",
                    "request": {
                        "method": "GET",
                        "url": "https://api.example.com/users"
                    }
                }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        collection.items.Should().HaveCount(1);
        collection.items[0].name.Should().Be("Get Users");
        collection.items[0].is_folder.Should().BeFalse();
        collection.items[0].request.Should().NotBeNull();
        collection.items[0].request!.method.Should().Be(http_method.get);
        collection.items[0].request!.url.Should().Be("https://api.example.com/users");
    }

    [Fact]
    public void parse_extracts_request_with_headers()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": [
                {
                    "name": "Auth Request",
                    "request": {
                        "method": "GET",
                        "header": [
                            {
                                "key": "Authorization",
                                "value": "Bearer token123"
                            },
                            {
                                "key": "Content-Type",
                                "value": "application/json"
                            }
                        ],
                        "url": "https://api.example.com/protected"
                    }
                }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        var request = collection.items[0].request;
        request!.headers.Should().HaveCount(2);
        request.headers[0].key.Should().Be("Authorization");
        request.headers[0].value.Should().Be("Bearer token123");
    }

    [Fact]
    public void parse_extracts_request_with_raw_body()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": [
                {
                    "name": "Create User",
                    "request": {
                        "method": "POST",
                        "body": {
                            "mode": "raw",
                            "raw": "{\"name\": \"John\", \"email\": \"john@example.com\"}"
                        },
                        "url": "https://api.example.com/users"
                    }
                }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        var request = collection.items[0].request;
        request!.body.Should().NotBeNull();
        request.body!.body_type.Should().Be(request_body_type.raw);
        request.body.raw_content.Should().Contain("John");
    }

    [Fact]
    public void parse_extracts_request_with_formdata_body()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": [
                {
                    "name": "Upload File",
                    "request": {
                        "method": "POST",
                        "body": {
                            "mode": "formdata",
                            "formdata": [
                                { "key": "name", "value": "test.txt" },
                                { "key": "description", "value": "Test file" }
                            ]
                        },
                        "url": "https://api.example.com/upload"
                    }
                }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        var request = collection.items[0].request;
        request!.body.Should().NotBeNull();
        request.body!.body_type.Should().Be(request_body_type.form_data);
        request.body.form_data.Should().ContainKey("name");
    }

    [Fact]
    public void parse_extracts_folder_structure()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": [
                {
                    "name": "Users",
                    "item": [
                        {
                            "name": "Get User",
                            "request": {
                                "method": "GET",
                                "url": "https://api.example.com/users/1"
                            }
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        collection.items.Should().HaveCount(1);
        collection.items[0].name.Should().Be("Users");
        collection.items[0].is_folder.Should().BeTrue();
        collection.items[0].children.Should().HaveCount(1);
        collection.items[0].children![0].name.Should().Be("Get User");
    }

    [Fact]
    public void parse_extracts_collection_variables()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": [],
            "variable": [
                { "key": "baseUrl", "value": "https://api.example.com" },
                { "key": "apiKey", "value": "secret123" }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        collection.variables.Should().HaveCount(2);
        collection.variables[0].key.Should().Be("baseUrl");
        collection.variables[0].value.Should().Be("https://api.example.com");
    }

    [Fact]
    public void parse_extracts_basic_auth()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": [
                {
                    "name": "Auth Request",
                    "request": {
                        "method": "GET",
                        "url": "https://api.example.com/protected",
                        "auth": {
                            "type": "basic",
                            "basic": {
                                "username": "user",
                                "password": "pass"
                            }
                        }
                    }
                }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        var request = collection.items[0].request;
        request!.auth.Should().NotBeNull();
        request.auth!.type.Should().Be(auth_type.basic);
    }

    [Fact]
    public void parse_extracts_bearer_auth()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": [
                {
                    "name": "Bearer Request",
                    "request": {
                        "method": "GET",
                        "url": "https://api.example.com/protected",
                        "auth": {
                            "type": "bearer",
                            "bearer": {
                                "token": "my-jwt-token"
                            }
                        }
                    }
                }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        var request = collection.items[0].request;
        request!.auth.Should().NotBeNull();
        request.auth!.type.Should().Be(auth_type.bearer);
    }

    [Fact]
    public void parse_handles_disabled_headers()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": [
                {
                    "name": "Request",
                    "request": {
                        "method": "GET",
                        "header": [
                            { "key": "Active", "value": "yes" },
                            { "key": "Disabled", "value": "no", "disabled": true }
                        ],
                        "url": "https://api.example.com/test"
                    }
                }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        var headers = collection.items[0].request!.headers;
        headers.Should().HaveCount(2);
        headers[0].enabled.Should().BeTrue();
        headers[1].enabled.Should().BeFalse();
    }

    [Fact]
    public void parse_handles_query_params_in_url()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": [
                {
                    "name": "Request with Query",
                    "request": {
                        "method": "GET",
                        "url": {
                            "raw": "https://api.example.com/search?q=test&limit=10",
                            "query": [
                                { "key": "q", "value": "test" },
                                { "key": "limit", "value": "10" }
                            ]
                        }
                    }
                }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        var request = collection.items[0].request;
        request!.url.Should().Contain("api.example.com");
        request.query_params.Should().HaveCount(2);
        request.query_params[0].key.Should().Be("q");
    }

    [Fact]
    public void parse_generates_id_when_missing()
    {
        // Arrange
        var json = """
        {
            "info": {
                "name": "No ID Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": []
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        collection.id.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void parse_handles_deeply_nested_folders()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Deep Nesting",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": [
                {
                    "name": "Level 1",
                    "item": [
                        {
                            "name": "Level 2",
                            "item": [
                                {
                                    "name": "Level 3 Request",
                                    "request": {
                                        "method": "GET",
                                        "url": "https://api.example.com/deep"
                                    }
                                }
                            ]
                        }
                    ]
                }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        collection.items[0].name.Should().Be("Level 1");
        collection.items[0].children![0].name.Should().Be("Level 2");
        collection.items[0].children![0].children![0].name.Should().Be("Level 3 Request");
    }

    [Fact]
    public void parse_throws_on_invalid_json()
    {
        // Arrange
        var json = "not valid json";

        // Act
        var act = () => _parser.parse(json);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void parse_handles_all_http_methods()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "HTTP Methods",
                "schema": "https://schema.getpostman.com/json/collection/v2.0.0/collection.json"
            },
            "item": [
                { "name": "GET", "request": { "method": "GET", "url": "http://test.com" } },
                { "name": "POST", "request": { "method": "POST", "url": "http://test.com" } },
                { "name": "PUT", "request": { "method": "PUT", "url": "http://test.com" } },
                { "name": "DELETE", "request": { "method": "DELETE", "url": "http://test.com" } },
                { "name": "PATCH", "request": { "method": "PATCH", "url": "http://test.com" } }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        collection.items.Should().HaveCount(5);
        collection.items[0].request!.method.Should().Be(http_method.get);
        collection.items[1].request!.method.Should().Be(http_method.post);
        collection.items[2].request!.method.Should().Be(http_method.put);
        collection.items[3].request!.method.Should().Be(http_method.delete);
        collection.items[4].request!.method.Should().Be(http_method.patch);
    }
}
