using FluentAssertions;
using Newtonsoft.Json.Linq;
using PostmanClone.Core.Models;
using PostmanClone.Data.Exporters;
using Xunit;

namespace PostmanClone.Data.Tests;

public class collection_exporter_tests
{
    private readonly collection_exporter _exporter;

    public collection_exporter_tests()
    {
        _exporter = new collection_exporter();
    }

    [Fact]
    public void export_returns_valid_postman_v21_json()
    {
        // Arrange
        var collection = create_basic_collection("Test Collection");

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        parsed["info"].Should().NotBeNull();
        parsed["info"]!["name"]!.Value<string>().Should().Be("Test Collection");
        parsed["info"]!["schema"]!.Value<string>().Should().Contain("v2.1.0");
    }

    [Fact]
    public void export_includes_collection_info()
    {
        // Arrange
        var collection = new postman_collection_model
        {
            id = "test-id-123",
            name = "My API",
            description = "API Description",
            version = "1.0.0",
            items = [],
            variables = [],
            created_at = DateTime.UtcNow
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        parsed["info"]!["_postman_id"]!.Value<string>().Should().Be("test-id-123");
        parsed["info"]!["name"]!.Value<string>().Should().Be("My API");
        parsed["info"]!["description"]!.Value<string>().Should().Be("API Description");
    }

    [Fact]
    public void export_includes_collection_variables()
    {
        // Arrange
        var collection = create_basic_collection("Vars Test") with
        {
            variables = [
                new key_value_pair_model { key = "baseUrl", value = "https://api.example.com", enabled = true },
                new key_value_pair_model { key = "apiKey", value = "secret123", enabled = false }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var variables = parsed["variable"] as JArray;
        variables.Should().NotBeNull();
        variables!.Should().HaveCount(2);
        variables[0]!["key"]!.Value<string>().Should().Be("baseUrl");
        variables[0]!["value"]!.Value<string>().Should().Be("https://api.example.com");
        variables[1]!["disabled"]!.Value<bool>().Should().BeTrue();
    }

    [Fact]
    public void export_includes_request_items()
    {
        // Arrange
        var collection = create_basic_collection("Request Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "req-1",
                    name = "Get Users",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Get Users",
                        method = http_method.get,
                        url = "https://api.example.com/users"
                    }
                }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var items = parsed["item"] as JArray;
        items.Should().NotBeNull();
        items!.Should().HaveCount(1);
        items[0]!["name"]!.Value<string>().Should().Be("Get Users");
        items[0]!["request"]!["method"]!.Value<string>().Should().Be("GET");
    }

    [Fact]
    public void export_includes_request_headers()
    {
        // Arrange
        var collection = create_basic_collection("Headers Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "req-1",
                    name = "Request With Headers",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Request With Headers",
                        method = http_method.post,
                        url = "https://api.example.com/data",
                        headers = [
                            new key_value_pair_model { key = "Content-Type", value = "application/json", enabled = true },
                            new key_value_pair_model { key = "Authorization", value = "Bearer token", enabled = true }
                        ]
                    }
                }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var headers = parsed["item"]![0]!["request"]!["header"] as JArray;
        headers.Should().NotBeNull();
        headers!.Should().HaveCount(2);
        headers[0]!["key"]!.Value<string>().Should().Be("Content-Type");
    }

    [Fact]
    public void export_includes_raw_body()
    {
        // Arrange
        var collection = create_basic_collection("Body Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "req-1",
                    name = "Post Request",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Post Request",
                        method = http_method.post,
                        url = "https://api.example.com/users",
                        body = new request_body_model
                        {
                            body_type = request_body_type.raw,
                            raw_content = "{\"name\":\"John\"}"
                        }
                    }
                }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var body = parsed["item"]![0]!["request"]!["body"];
        body.Should().NotBeNull();
        body!["mode"]!.Value<string>().Should().Be("raw");
        body["raw"]!.Value<string>().Should().Be("{\"name\":\"John\"}");
    }

    [Fact]
    public void export_includes_form_data_body()
    {
        // Arrange
        var collection = create_basic_collection("Form Data Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "req-1",
                    name = "Form Request",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Form Request",
                        method = http_method.post,
                        url = "https://api.example.com/upload",
                        body = new request_body_model
                        {
                            body_type = request_body_type.form_data,
                            form_data = new Dictionary<string, string>
                            {
                                ["name"] = "John",
                                ["file"] = "test.txt"
                            }
                        }
                    }
                }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var body = parsed["item"]![0]!["request"]!["body"];
        body!["mode"]!.Value<string>().Should().Be("formdata");
        var formdata = body["formdata"] as JArray;
        formdata.Should().HaveCount(2);
    }

    [Fact]
    public void export_includes_urlencoded_body()
    {
        // Arrange
        var collection = create_basic_collection("Urlencoded Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "req-1",
                    name = "Urlencoded Request",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Urlencoded Request",
                        method = http_method.post,
                        url = "https://api.example.com/login",
                        body = new request_body_model
                        {
                            body_type = request_body_type.x_www_form_urlencoded,
                            form_urlencoded = new Dictionary<string, string>
                            {
                                ["username"] = "john",
                                ["password"] = "secret"
                            }
                        }
                    }
                }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var body = parsed["item"]![0]!["request"]!["body"];
        body!["mode"]!.Value<string>().Should().Be("urlencoded");
        var urlencoded = body["urlencoded"] as JArray;
        urlencoded.Should().HaveCount(2);
    }

    [Fact]
    public void export_includes_nested_folders()
    {
        // Arrange
        var collection = create_basic_collection("Folders Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "folder-1",
                    name = "Users",
                    is_folder = true,
                    children = [
                        new collection_item_model
                        {
                            id = "req-1",
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

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var folder = parsed["item"]![0];
        folder!["name"]!.Value<string>().Should().Be("Users");
        var nested_items = folder["item"] as JArray;
        nested_items.Should().NotBeNull();
        nested_items!.Should().HaveCount(1);
        nested_items[0]!["name"]!.Value<string>().Should().Be("Get User");
    }

    [Fact]
    public void export_includes_query_parameters()
    {
        // Arrange
        var collection = create_basic_collection("Query Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "req-1",
                    name = "Search",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Search",
                        method = http_method.get,
                        url = "https://api.example.com/search",
                        query_params = [
                            new key_value_pair_model { key = "q", value = "test", enabled = true },
                            new key_value_pair_model { key = "limit", value = "10", enabled = true }
                        ]
                    }
                }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var url = parsed["item"]![0]!["request"]!["url"];
        var query = url!["query"] as JArray;
        query.Should().NotBeNull();
        query!.Should().HaveCount(2);
        query[0]!["key"]!.Value<string>().Should().Be("q");
        query[0]!["value"]!.Value<string>().Should().Be("test");
    }

    [Fact]
    public void export_includes_basic_auth()
    {
        // Arrange
        var collection = create_basic_collection("Auth Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "req-1",
                    name = "Authenticated Request",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Authenticated Request",
                        method = http_method.get,
                        url = "https://api.example.com/protected",
                        auth = new auth_config_model
                        {
                            type = auth_type.basic,
                            basic = new basic_auth_model
                            {
                                username = "user",
                                password = "pass"
                            }
                        }
                    }
                }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var auth = parsed["item"]![0]!["request"]!["auth"];
        auth.Should().NotBeNull();
        auth!["type"]!.Value<string>().Should().Be("basic");
        var basic_auth = auth["basic"] as JArray;
        basic_auth.Should().NotBeNull();
        basic_auth!.Any(x => x["key"]!.Value<string>() == "username").Should().BeTrue();
    }

    [Fact]
    public void export_includes_bearer_auth()
    {
        // Arrange
        var collection = create_basic_collection("Bearer Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "req-1",
                    name = "Bearer Request",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Bearer Request",
                        method = http_method.get,
                        url = "https://api.example.com/api",
                        auth = new auth_config_model
                        {
                            type = auth_type.bearer,
                            bearer = new bearer_auth_model
                            {
                                token = "my-jwt-token"
                            }
                        }
                    }
                }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var auth = parsed["item"]![0]!["request"]!["auth"];
        auth!["type"]!.Value<string>().Should().Be("bearer");
        var bearer_arr = auth["bearer"] as JArray;
        bearer_arr.Should().NotBeNull();
        bearer_arr!.Any(x => x["value"]!.Value<string>() == "my-jwt-token").Should().BeTrue();
    }

    [Fact]
    public void export_includes_api_key_auth()
    {
        // Arrange
        var collection = create_basic_collection("API Key Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "req-1",
                    name = "API Key Request",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "API Key Request",
                        method = http_method.get,
                        url = "https://api.example.com/data",
                        auth = new auth_config_model
                        {
                            type = auth_type.api_key,
                            api_key = new api_key_auth_model
                            {
                                key = "X-API-Key",
                                value = "secret-key",
                                location = api_key_location.header
                            }
                        }
                    }
                }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var auth = parsed["item"]![0]!["request"]!["auth"];
        auth!["type"]!.Value<string>().Should().Be("apikey");
        var apikey_arr = auth["apikey"] as JArray;
        apikey_arr.Should().NotBeNull();
    }

    [Fact]
    public void export_includes_pre_request_script()
    {
        // Arrange
        var collection = create_basic_collection("Script Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "req-1",
                    name = "Scripted Request",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Scripted Request",
                        method = http_method.get,
                        url = "https://api.example.com/test",
                        pre_request_script = "console.log('pre-request');"
                    }
                }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var events = parsed["item"]![0]!["event"] as JArray;
        events.Should().NotBeNull();
        var prerequest_event = events!.FirstOrDefault(e => e["listen"]!.Value<string>() == "prerequest");
        prerequest_event.Should().NotBeNull();
        var exec = prerequest_event!["script"]!["exec"] as JArray;
        exec.Should().NotBeNull();
        exec!.Select(x => x.Value<string>()).Should().Contain("console.log('pre-request');");
    }

    [Fact]
    public void export_includes_test_script()
    {
        // Arrange
        var collection = create_basic_collection("Test Script Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "req-1",
                    name = "Test Request",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Test Request",
                        method = http_method.get,
                        url = "https://api.example.com/test",
                        post_response_script = "pm.test('status is 200', function() { pm.response.to.have.status(200); });"
                    }
                }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var events = parsed["item"]![0]!["event"] as JArray;
        events.Should().NotBeNull();
        var test_event = events!.FirstOrDefault(e => e["listen"]!.Value<string>() == "test");
        test_event.Should().NotBeNull();
    }

    [Fact]
    public void export_includes_collection_level_auth()
    {
        // Arrange
        var collection = create_basic_collection("Collection Auth Test") with
        {
            auth = new auth_config_model
            {
                type = auth_type.bearer,
                bearer = new bearer_auth_model { token = "collection-token" }
            }
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var auth = parsed["auth"];
        auth.Should().NotBeNull();
        auth!["type"]!.Value<string>().Should().Be("bearer");
    }

    [Fact]
    public void export_handles_all_http_methods()
    {
        // Arrange
        var methods = new[] { http_method.get, http_method.post, http_method.put, http_method.delete, http_method.patch };
        var items = methods.Select((m, i) => new collection_item_model
        {
            id = $"req-{i}",
            name = $"{m} Request",
            is_folder = false,
            request = new http_request_model
            {
                name = $"{m} Request",
                method = m,
                url = "https://api.example.com/test"
            }
        }).ToList();

        var collection = create_basic_collection("Methods Test") with { items = items };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var exported_items = parsed["item"] as JArray;
        exported_items.Should().HaveCount(5);
        exported_items![0]!["request"]!["method"]!.Value<string>().Should().Be("GET");
        exported_items[1]!["request"]!["method"]!.Value<string>().Should().Be("POST");
        exported_items[2]!["request"]!["method"]!.Value<string>().Should().Be("PUT");
        exported_items[3]!["request"]!["method"]!.Value<string>().Should().Be("DELETE");
        exported_items[4]!["request"]!["method"]!.Value<string>().Should().Be("PATCH");
    }

    [Fact]
    public void export_url_includes_host_and_path()
    {
        // Arrange
        var collection = create_basic_collection("URL Test") with
        {
            items = [
                new collection_item_model
                {
                    id = "req-1",
                    name = "Detailed URL",
                    is_folder = false,
                    request = new http_request_model
                    {
                        name = "Detailed URL",
                        method = http_method.get,
                        url = "https://api.example.com/v1/users/123"
                    }
                }
            ]
        };

        // Act
        var json = _exporter.export(collection);

        // Assert
        var parsed = JObject.Parse(json);
        var url = parsed["item"]![0]!["request"]!["url"];
        url!["raw"]!.Value<string>().Should().Be("https://api.example.com/v1/users/123");
        url["protocol"]!.Value<string>().Should().Be("https");
        var host = url["host"] as JArray;
        host.Should().NotBeNull();
        var path = url["path"] as JArray;
        path.Should().NotBeNull();
    }

    [Fact]
    public void export_to_file_creates_valid_json_file()
    {
        // Arrange
        var collection = create_basic_collection("File Export Test");
        var file_path = Path.Combine(Path.GetTempPath(), $"export_test_{Guid.NewGuid()}.json");

        try
        {
            // Act
            _exporter.export_to_file(collection, file_path);

            // Assert
            File.Exists(file_path).Should().BeTrue();
            var content = File.ReadAllText(file_path);
            var parsed = JObject.Parse(content);
            parsed["info"]!["name"]!.Value<string>().Should().Be("File Export Test");
        }
        finally
        {
            if (File.Exists(file_path))
            {
                File.Delete(file_path);
            }
        }
    }

    private static postman_collection_model create_basic_collection(string name)
    {
        return new postman_collection_model
        {
            id = Guid.NewGuid().ToString(),
            name = name,
            items = [],
            variables = [],
            created_at = DateTime.UtcNow
        };
    }
}
