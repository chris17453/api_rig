using FluentAssertions;
using PostmanClone.Core.Models;
using PostmanClone.Data.Parsers;
using Xunit;

namespace PostmanClone.Data.Tests;

public class swagger_v2_parser_tests
{
    private readonly swagger_v2_parser _parser = new();

    [Fact]
    public void can_parse_returns_true_for_swagger_v2()
    {
        // Arrange
        var json = """
        {
            "swagger": "2.0",
            "info": {
                "title": "Test API",
                "version": "1.0.0"
            },
            "paths": {}
        }
        """;

        // Act
        var result = _parser.can_parse(json);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void can_parse_returns_false_for_openapi_v3()
    {
        // Arrange
        var json = """
        {
            "openapi": "3.0.0",
            "info": {
                "title": "Test API",
                "version": "1.0.0"
            },
            "paths": {}
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
    public void parse_extracts_basic_info()
    {
        // Arrange
        var json = """
        {
            "swagger": "2.0",
            "info": {
                "title": "Pet Store API",
                "description": "A sample Pet Store API",
                "version": "1.0.0"
            },
            "paths": {}
        }
        """;

        // Act
        var result = _parser.parse(json);

        // Assert
        result.name.Should().Be("Pet Store API");
        result.description.Should().Be("A sample Pet Store API");
        result.version.Should().Be("1.0.0");
        result.items.Should().BeEmpty();
    }

    [Fact]
    public void parse_constructs_base_url_from_host_and_schemes()
    {
        // Arrange
        var json = """
        {
            "swagger": "2.0",
            "info": {
                "title": "Test API",
                "version": "1.0.0"
            },
            "host": "api.example.com",
            "basePath": "/v1",
            "schemes": ["https"],
            "paths": {}
        }
        """;

        // Act
        var result = _parser.parse(json);

        // Assert
        result.variables.Should().ContainSingle();
        result.variables[0].key.Should().Be("base_url");
        result.variables[0].value.Should().Be("https://api.example.com/v1");
    }

    [Fact]
    public void parse_creates_request_from_simple_get_operation()
    {
        // Arrange
        var json = """
        {
            "swagger": "2.0",
            "info": {
                "title": "Test API",
                "version": "1.0.0"
            },
            "host": "api.example.com",
            "schemes": ["https"],
            "paths": {
                "/users": {
                    "get": {
                        "summary": "Get all users",
                        "operationId": "getUsers",
                        "responses": {
                            "200": {
                                "description": "Success"
                            }
                        }
                    }
                }
            }
        }
        """;

        // Act
        var result = _parser.parse(json);

        // Assert
        result.items.Should().ContainSingle();
        var request_item = result.items[0];
        request_item.name.Should().Be("getUsers");
        request_item.is_folder.Should().BeFalse();
        request_item.request.Should().NotBeNull();
        request_item.request!.method.Should().Be(http_method.get);
        request_item.request.url.Should().Be("https://api.example.com/users");
    }

    [Fact]
    public void parse_groups_requests_by_tags()
    {
        // Arrange
        var json = """
        {
            "swagger": "2.0",
            "info": {
                "title": "Test API",
                "version": "1.0.0"
            },
            "host": "api.example.com",
            "schemes": ["https"],
            "paths": {
                "/users": {
                    "get": {
                        "tags": ["Users"],
                        "summary": "Get all users",
                        "operationId": "getUsers"
                    }
                },
                "/users/{id}": {
                    "get": {
                        "tags": ["Users"],
                        "summary": "Get user by ID",
                        "operationId": "getUserById"
                    }
                },
                "/products": {
                    "get": {
                        "tags": ["Products"],
                        "summary": "Get all products",
                        "operationId": "getProducts"
                    }
                }
            }
        }
        """;

        // Act
        var result = _parser.parse(json);

        // Assert
        result.items.Should().HaveCount(2);
        
        var users_folder = result.items.FirstOrDefault(i => i.name == "Users");
        users_folder.Should().NotBeNull();
        users_folder!.is_folder.Should().BeTrue();
        users_folder.children.Should().HaveCount(2);
        
        var products_folder = result.items.FirstOrDefault(i => i.name == "Products");
        products_folder.Should().NotBeNull();
        products_folder!.is_folder.Should().BeTrue();
        products_folder.children.Should().ContainSingle();
    }

    [Fact]
    public void parse_handles_path_parameters()
    {
        // Arrange
        var json = """
        {
            "swagger": "2.0",
            "info": {
                "title": "Test API",
                "version": "1.0.0"
            },
            "host": "api.example.com",
            "schemes": ["https"],
            "paths": {
                "/users/{userId}": {
                    "get": {
                        "operationId": "getUser",
                        "parameters": [
                            {
                                "name": "userId",
                                "in": "path",
                                "required": true,
                                "type": "integer"
                            }
                        ]
                    }
                }
            }
        }
        """;

        // Act
        var result = _parser.parse(json);

        // Assert
        result.items.Should().ContainSingle();
        var request = result.items[0].request;
        request.Should().NotBeNull();
        request!.url.Should().Be("https://api.example.com/users/{{userId}}");
    }

    [Fact]
    public void parse_handles_query_parameters()
    {
        // Arrange
        var json = """
        {
            "swagger": "2.0",
            "info": {
                "title": "Test API",
                "version": "1.0.0"
            },
            "host": "api.example.com",
            "schemes": ["https"],
            "paths": {
                "/users": {
                    "get": {
                        "operationId": "getUsers",
                        "parameters": [
                            {
                                "name": "page",
                                "in": "query",
                                "required": false,
                                "type": "integer",
                                "default": 1
                            },
                            {
                                "name": "limit",
                                "in": "query",
                                "required": true,
                                "type": "integer"
                            }
                        ]
                    }
                }
            }
        }
        """;

        // Act
        var result = _parser.parse(json);

        // Assert
        result.items.Should().ContainSingle();
        var request = result.items[0].request;
        request.Should().NotBeNull();
        request!.query_params.Should().HaveCount(2);
        
        var page_param = request.query_params.FirstOrDefault(p => p.key == "page");
        page_param.Should().NotBeNull();
        page_param!.value.Should().Be("1");
        page_param.enabled.Should().BeFalse();
        
        var limit_param = request.query_params.FirstOrDefault(p => p.key == "limit");
        limit_param.Should().NotBeNull();
        limit_param!.value.Should().Be("{{limit}}");
        limit_param.enabled.Should().BeTrue();
    }

    [Fact]
    public void parse_handles_body_parameter_with_json()
    {
        // Arrange
        var json = """
        {
            "swagger": "2.0",
            "info": {
                "title": "Test API",
                "version": "1.0.0"
            },
            "host": "api.example.com",
            "schemes": ["https"],
            "paths": {
                "/users": {
                    "post": {
                        "operationId": "createUser",
                        "parameters": [
                            {
                                "name": "body",
                                "in": "body",
                                "required": true,
                                "schema": {
                                    "type": "object",
                                    "properties": {
                                        "name": {
                                            "type": "string"
                                        },
                                        "email": {
                                            "type": "string"
                                        }
                                    }
                                }
                            }
                        ]
                    }
                }
            }
        }
        """;

        // Act
        var result = _parser.parse(json);

        // Assert
        result.items.Should().ContainSingle();
        var request = result.items[0].request;
        request.Should().NotBeNull();
        request!.method.Should().Be(http_method.post);
        request.body.Should().NotBeNull();
        request.body!.body_type.Should().Be(request_body_type.json);
        request.body.raw_content.Should().NotBeNull();
        request.body.raw_content.Should().Contain("name");
        request.body.raw_content.Should().Contain("email");
        
        var content_type_header = request.headers.FirstOrDefault(h => h.key == "Content-Type");
        content_type_header.Should().NotBeNull();
        content_type_header!.value.Should().Be("application/json");
    }

    [Fact]
    public void parse_handles_form_data_parameters()
    {
        // Arrange
        var json = """
        {
            "swagger": "2.0",
            "info": {
                "title": "Test API",
                "version": "1.0.0"
            },
            "host": "api.example.com",
            "schemes": ["https"],
            "paths": {
                "/upload": {
                    "post": {
                        "operationId": "uploadFile",
                        "parameters": [
                            {
                                "name": "file",
                                "in": "formData",
                                "required": true,
                                "type": "file"
                            },
                            {
                                "name": "description",
                                "in": "formData",
                                "required": false,
                                "type": "string"
                            }
                        ]
                    }
                }
            }
        }
        """;

        // Act
        var result = _parser.parse(json);

        // Assert
        result.items.Should().ContainSingle();
        var request = result.items[0].request;
        request.Should().NotBeNull();
        request!.body.Should().NotBeNull();
        request.body!.body_type.Should().Be(request_body_type.form_data);
        request.body.form_data.Should().NotBeNull();
        request.body.form_data.Should().ContainKey("file");
        request.body.form_data.Should().ContainKey("description");
    }

    [Fact]
    public void parse_handles_basic_authentication()
    {
        // Arrange
        var json = """
        {
            "swagger": "2.0",
            "info": {
                "title": "Test API",
                "version": "1.0.0"
            },
            "host": "api.example.com",
            "schemes": ["https"],
            "securityDefinitions": {
                "basicAuth": {
                    "type": "basic"
                }
            },
            "security": [
                {
                    "basicAuth": []
                }
            ],
            "paths": {}
        }
        """;

        // Act
        var result = _parser.parse(json);

        // Assert
        result.auth.Should().NotBeNull();
        result.auth!.type.Should().Be(auth_type.basic);
        result.auth.basic.Should().NotBeNull();
        result.auth.basic!.username.Should().Be("{{username}}");
        result.auth.basic.password.Should().Be("{{password}}");
    }

    [Fact]
    public void parse_handles_api_key_authentication()
    {
        // Arrange
        var json = """
        {
            "swagger": "2.0",
            "info": {
                "title": "Test API",
                "version": "1.0.0"
            },
            "host": "api.example.com",
            "schemes": ["https"],
            "securityDefinitions": {
                "apiKey": {
                    "type": "apiKey",
                    "name": "X-API-Key",
                    "in": "header"
                }
            },
            "security": [
                {
                    "apiKey": []
                }
            ],
            "paths": {}
        }
        """;

        // Act
        var result = _parser.parse(json);

        // Assert
        result.auth.Should().NotBeNull();
        result.auth!.type.Should().Be(auth_type.api_key);
        result.auth.api_key.Should().NotBeNull();
        result.auth.api_key!.key.Should().Be("X-API-Key");
        result.auth.api_key.value.Should().Be("{{api_key}}");
        result.auth.api_key.location.Should().Be(api_key_location.header);
    }

    [Fact]
    public void parse_throws_format_exception_for_invalid_json()
    {
        // Arrange
        var json = "{ invalid json }";

        // Act & Assert
        var action = () => _parser.parse(json);
        action.Should().Throw<FormatException>()
            .WithMessage("Invalid JSON format*");
    }
}
