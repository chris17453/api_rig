using FluentAssertions;
using PostmanClone.Core.Models;
using PostmanClone.Data.Parsers;
using Xunit;

namespace PostmanClone.Data.Tests;

public class postman_v21_parser_tests
{
    private readonly postman_v21_parser _parser = new();

    [Fact]
    public void can_parse_returns_true_for_v21_schema()
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
        result.Should().BeTrue();
    }

    [Fact]
    public void can_parse_returns_false_for_v20_schema()
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
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
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
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                {
                    "name": "Get Users",
                    "request": {
                        "method": "GET",
                        "url": {
                            "raw": "https://api.example.com/users",
                            "protocol": "https",
                            "host": ["api", "example", "com"],
                            "path": ["users"]
                        }
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
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                {
                    "name": "Auth Request",
                    "request": {
                        "method": "GET",
                        "header": [
                            {
                                "key": "Authorization",
                                "value": "Bearer token123",
                                "type": "text"
                            },
                            {
                                "key": "Content-Type",
                                "value": "application/json",
                                "type": "text"
                            }
                        ],
                        "url": {
                            "raw": "https://api.example.com/protected"
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
        request!.headers.Should().HaveCount(2);
        request.headers[0].key.Should().Be("Authorization");
        request.headers[0].value.Should().Be("Bearer token123");
    }

    [Fact]
    public void parse_extracts_basic_auth_v21_format()
    {
        // Arrange - v2.1 uses array format for auth
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                {
                    "name": "Auth Request",
                    "request": {
                        "method": "GET",
                        "url": { "raw": "https://api.example.com/protected" },
                        "auth": {
                            "type": "basic",
                            "basic": [
                                { "key": "username", "value": "myuser", "type": "string" },
                                { "key": "password", "value": "mypass", "type": "string" }
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
        request!.auth.Should().NotBeNull();
        request.auth!.type.Should().Be(auth_type.basic);
        request.auth.basic.Should().NotBeNull();
        request.auth.basic!.username.Should().Be("myuser");
        request.auth.basic.password.Should().Be("mypass");
    }

    [Fact]
    public void parse_extracts_bearer_auth_v21_format()
    {
        // Arrange - v2.1 uses array format for bearer
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                {
                    "name": "Bearer Request",
                    "request": {
                        "method": "GET",
                        "url": { "raw": "https://api.example.com/protected" },
                        "auth": {
                            "type": "bearer",
                            "bearer": [
                                { "key": "token", "value": "my-jwt-token", "type": "string" }
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
        request!.auth.Should().NotBeNull();
        request.auth!.type.Should().Be(auth_type.bearer);
        request.auth.bearer.Should().NotBeNull();
        request.auth.bearer!.token.Should().Be("my-jwt-token");
    }

    [Fact]
    public void parse_extracts_api_key_auth_v21_format()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                {
                    "name": "API Key Request",
                    "request": {
                        "method": "GET",
                        "url": { "raw": "https://api.example.com/protected" },
                        "auth": {
                            "type": "apikey",
                            "apikey": [
                                { "key": "key", "value": "X-API-Key", "type": "string" },
                                { "key": "value", "value": "secret-api-key", "type": "string" },
                                { "key": "in", "value": "header", "type": "string" }
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
        request!.auth.Should().NotBeNull();
        request.auth!.type.Should().Be(auth_type.api_key);
        request.auth.api_key.Should().NotBeNull();
        request.auth.api_key!.key.Should().Be("X-API-Key");
        request.auth.api_key.value.Should().Be("secret-api-key");
        request.auth.api_key.location.Should().Be(api_key_location.header);
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
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                {
                    "name": "Create User",
                    "request": {
                        "method": "POST",
                        "body": {
                            "mode": "raw",
                            "raw": "{\"name\": \"John\", \"email\": \"john@example.com\"}",
                            "options": {
                                "raw": {
                                    "language": "json"
                                }
                            }
                        },
                        "url": { "raw": "https://api.example.com/users" }
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
    public void parse_extracts_folder_structure()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                {
                    "name": "Users",
                    "item": [
                        {
                            "name": "Get User",
                            "request": {
                                "method": "GET",
                                "url": { "raw": "https://api.example.com/users/1" }
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
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [],
            "variable": [
                { "key": "baseUrl", "value": "https://api.example.com", "type": "string" },
                { "key": "apiKey", "value": "secret123", "type": "string" }
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
    public void parse_extracts_query_params()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                {
                    "name": "Search",
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
        request!.query_params.Should().HaveCount(2);
        request.query_params[0].key.Should().Be("q");
        request.query_params[0].value.Should().Be("test");
    }

    [Fact]
    public void parse_handles_disabled_query_params()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                {
                    "name": "Search",
                    "request": {
                        "method": "GET",
                        "url": {
                            "raw": "https://api.example.com/search",
                            "query": [
                                { "key": "active", "value": "true" },
                                { "key": "debug", "value": "true", "disabled": true }
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
        request!.query_params.Should().HaveCount(2);
        request.query_params[0].enabled.Should().BeTrue();
        request.query_params[1].enabled.Should().BeFalse();
    }

    [Fact]
    public void parse_handles_pre_request_script()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                {
                    "name": "Scripted Request",
                    "event": [
                        {
                            "listen": "prerequest",
                            "script": {
                                "exec": [
                                    "console.log('Pre-request script');",
                                    "pm.variables.set('timestamp', Date.now());"
                                ],
                                "type": "text/javascript"
                            }
                        }
                    ],
                    "request": {
                        "method": "GET",
                        "url": { "raw": "https://api.example.com/test" }
                    }
                }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        var request = collection.items[0].request;
        request!.pre_request_script.Should().NotBeNullOrEmpty();
        request.pre_request_script.Should().Contain("Pre-request script");
    }

    [Fact]
    public void parse_handles_test_script()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Test Collection",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                {
                    "name": "Tested Request",
                    "event": [
                        {
                            "listen": "test",
                            "script": {
                                "exec": [
                                    "pm.test('Status is 200', function() {",
                                    "    pm.response.to.have.status(200);",
                                    "});"
                                ],
                                "type": "text/javascript"
                            }
                        }
                    ],
                    "request": {
                        "method": "GET",
                        "url": { "raw": "https://api.example.com/test" }
                    }
                }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        var request = collection.items[0].request;
        request!.post_response_script.Should().NotBeNullOrEmpty();
        request.post_response_script.Should().Contain("Status is 200");
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
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "item": [
                { "name": "GET", "request": { "method": "GET", "url": { "raw": "http://test.com" } } },
                { "name": "POST", "request": { "method": "POST", "url": { "raw": "http://test.com" } } },
                { "name": "PUT", "request": { "method": "PUT", "url": { "raw": "http://test.com" } } },
                { "name": "DELETE", "request": { "method": "DELETE", "url": { "raw": "http://test.com" } } },
                { "name": "PATCH", "request": { "method": "PATCH", "url": { "raw": "http://test.com" } } },
                { "name": "HEAD", "request": { "method": "HEAD", "url": { "raw": "http://test.com" } } },
                { "name": "OPTIONS", "request": { "method": "OPTIONS", "url": { "raw": "http://test.com" } } }
            ]
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        collection.items.Should().HaveCount(7);
        collection.items[0].request!.method.Should().Be(http_method.get);
        collection.items[1].request!.method.Should().Be(http_method.post);
        collection.items[2].request!.method.Should().Be(http_method.put);
        collection.items[3].request!.method.Should().Be(http_method.delete);
        collection.items[4].request!.method.Should().Be(http_method.patch);
        collection.items[5].request!.method.Should().Be(http_method.head);
        collection.items[6].request!.method.Should().Be(http_method.options);
    }

    [Fact]
    public void parse_handles_collection_level_auth()
    {
        // Arrange
        var json = """
        {
            "info": {
                "_postman_id": "test-id",
                "name": "Collection with Auth",
                "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            "auth": {
                "type": "bearer",
                "bearer": [
                    { "key": "token", "value": "collection-token", "type": "string" }
                ]
            },
            "item": []
        }
        """;

        // Act
        var collection = _parser.parse(json);

        // Assert
        collection.auth.Should().NotBeNull();
        collection.auth!.type.Should().Be(auth_type.bearer);
    }
}
