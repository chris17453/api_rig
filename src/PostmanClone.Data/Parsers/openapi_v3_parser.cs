using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PostmanClone.Core.Models;

namespace PostmanClone.Data.Parsers;

public class openapi_v3_parser
{
    public bool can_parse(string json)
    {
        try
        {
            var obj = JObject.Parse(json);
            var openapi_version = obj["openapi"]?.Value<string>();
            return openapi_version?.StartsWith("3.") == true;
        }
        catch
        {
            return false;
        }
    }

    public postman_collection_model parse(string json)
    {
        JObject obj;
        try
        {
            obj = JObject.Parse(json);
        }
        catch (JsonReaderException ex)
        {
            throw new FormatException("Invalid JSON format", ex);
        }

        var info = obj["info"];
        var servers = obj["servers"] as JArray;
        var paths = obj["paths"] as JObject;
        var components = obj["components"];

        var base_url = extract_base_url(servers);
        var items = parse_paths(paths, base_url);
        var security_schemes = parse_security_schemes(components?["securitySchemes"] as JObject);
        var global_security = parse_global_security(obj["security"] as JArray, security_schemes);

        return new postman_collection_model
        {
            id = Guid.NewGuid().ToString(),
            name = info?["title"]?.Value<string>() ?? "Unnamed API",
            description = info?["description"]?.Value<string>(),
            version = info?["version"]?.Value<string>(),
            items = items,
            variables = create_base_url_variable(base_url),
            auth = global_security,
            created_at = DateTime.UtcNow
        };
    }

    private static string extract_base_url(JArray? servers)
    {
        if (servers is null || servers.Count == 0)
        {
            return "{{base_url}}";
        }

        var first_server = servers[0];
        var url = first_server["url"]?.Value<string>() ?? "{{base_url}}";
        
        // Remove trailing slash
        return url.TrimEnd('/');
    }

    private static IReadOnlyList<key_value_pair_model> create_base_url_variable(string base_url)
    {
        if (base_url == "{{base_url}}")
        {
            return [];
        }

        return
        [
            new key_value_pair_model
            {
                key = "base_url",
                value = base_url,
                enabled = true
            }
        ];
    }

    private static IReadOnlyList<collection_item_model> parse_paths(JObject? paths, string base_url)
    {
        if (paths is null)
        {
            return [];
        }

        var items = new List<collection_item_model>();
        var folders = new Dictionary<string, List<collection_item_model>>();

        foreach (var path_entry in paths)
        {
            var path = path_entry.Key;
            var operations = path_entry.Value as JObject;

            if (operations is null) continue;

            foreach (var operation_entry in operations)
            {
                var method = operation_entry.Key.ToLowerInvariant();
                if (!is_http_method(method)) continue;

                var operation = operation_entry.Value as JObject;
                if (operation is null) continue;

                var request_item = parse_operation(path, method, operation, base_url);
                
                // Group by tags (first tag becomes folder)
                var tags = operation["tags"] as JArray;
                if (tags is not null && tags.Count > 0)
                {
                    var folder_name = tags[0].Value<string>() ?? "Other";
                    if (!folders.ContainsKey(folder_name))
                    {
                        folders[folder_name] = new List<collection_item_model>();
                    }
                    folders[folder_name].Add(request_item);
                }
                else
                {
                    items.Add(request_item);
                }
            }
        }

        // Create folder structure
        foreach (var folder_entry in folders)
        {
            items.Add(new collection_item_model
            {
                id = Guid.NewGuid().ToString(),
                name = folder_entry.Key,
                is_folder = true,
                children = folder_entry.Value
            });
        }

        return items;
    }

    private static collection_item_model parse_operation(string path, string method, JObject operation, string base_url)
    {
        var operation_id = operation["operationId"]?.Value<string>();
        var summary = operation["summary"]?.Value<string>();
        var description = operation["description"]?.Value<string>();
        
        var name = operation_id ?? summary ?? $"{method.ToUpperInvariant()} {path}";
        var url = $"{base_url}{path}";

        var headers = new List<key_value_pair_model>();
        var query_params = new List<key_value_pair_model>();
        var path_params = new List<string>();

        // Parse parameters
        var parameters = operation["parameters"] as JArray;
        parse_parameters(parameters, headers, query_params, path_params);

        // Replace path parameters with variables
        foreach (var param in path_params)
        {
            url = url.Replace($"{{{param}}}", $"{{{{{param}}}}}");
        }

        // Parse request body
        var request_body = parse_request_body(operation["requestBody"] as JObject, headers);

        var http_method = map_http_method(method);

        var request = new http_request_model
        {
            id = Guid.NewGuid().ToString(),
            name = name,
            method = http_method,
            url = url,
            headers = headers,
            query_params = query_params,
            body = request_body
        };

        return new collection_item_model
        {
            id = Guid.NewGuid().ToString(),
            name = name,
            description = description ?? summary,
            is_folder = false,
            request = request
        };
    }

    private static void parse_parameters(JArray? parameters, List<key_value_pair_model> headers,
        List<key_value_pair_model> query_params, List<string> path_params)
    {
        if (parameters is null) return;

        foreach (var param in parameters)
        {
            var name = param["name"]?.Value<string>();
            if (name is null) continue;

            var param_in = param["in"]?.Value<string>();
            var required = param["required"]?.Value<bool>() ?? false;
            var description = param["description"]?.Value<string>();
            var example = param["example"]?.Value<string>();
            var schema = param["schema"] as JObject;
            var default_value = schema?["default"]?.Value<string>();

            var value = example ?? default_value ?? (required ? $"{{{{{name}}}}}" : "");

            switch (param_in)
            {
                case "query":
                    query_params.Add(new key_value_pair_model
                    {
                        key = name,
                        value = value,
                        enabled = required
                    });
                    break;
                case "header":
                    headers.Add(new key_value_pair_model
                    {
                        key = name,
                        value = value,
                        enabled = required
                    });
                    break;
                case "path":
                    path_params.Add(name);
                    break;
            }
        }
    }

    private static request_body_model? parse_request_body(JObject? request_body_obj, List<key_value_pair_model> headers)
    {
        if (request_body_obj is null) return null;

        var content = request_body_obj["content"] as JObject;
        if (content is null) return null;

        // Prefer JSON content
        var json_content = content["application/json"] as JObject;
        if (json_content is not null)
        {
            var schema = json_content["schema"] as JObject;
            var example = json_content["example"];
            var examples = json_content["examples"] as JObject;

            string body_text;
            if (example is not null)
            {
                body_text = example.ToString(Formatting.Indented);
            }
            else if (examples is not null && examples.Count > 0)
            {
                var first_example_property = examples.First;
                var first_example_value = first_example_property?.First?["value"];
                body_text = first_example_value?.ToString(Formatting.Indented) ?? generate_example_from_schema(schema);
            }
            else
            {
                body_text = generate_example_from_schema(schema);
            }

            // Add Content-Type header if not present
            if (!headers.Any(h => h.key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)))
            {
                headers.Add(new key_value_pair_model
                {
                    key = "Content-Type",
                    value = "application/json",
                    enabled = true
                });
            }

            return new request_body_model
            {
                body_type = request_body_type.json,
                raw_content = body_text
            };
        }

        // Handle form data
        var form_content = content["application/x-www-form-urlencoded"] as JObject 
                           ?? content["multipart/form-data"] as JObject;
        if (form_content is not null)
        {
            var schema = form_content["schema"] as JObject;
            var properties = schema?["properties"] as JObject;

            if (properties is not null)
            {
                var form_data = new List<key_value_pair_model>();
                foreach (var prop in properties)
                {
                    var prop_name = prop.Key;
                    var prop_schema = prop.Value as JObject;
                    var example = prop_schema?["example"]?.Value<string>() ?? "";
                    
                    form_data.Add(new key_value_pair_model
                    {
                        key = prop_name,
                        value = example,
                        enabled = true
                    });
                }

                var is_multipart = content.Property("multipart/form-data") is not null;
                var body_type = is_multipart
                    ? request_body_type.form_data
                    : request_body_type.x_www_form_urlencoded;

                var form_dict = form_data
                    .GroupBy(kvp => kvp.key)
                    .ToDictionary(g => g.Key, g => g.First().value);

                return new request_body_model
                {
                    body_type = body_type,
                    form_data = body_type == request_body_type.form_data ? form_dict : null,
                    form_urlencoded = body_type == request_body_type.x_www_form_urlencoded ? form_dict : null
                };
            }
        }

        return null;
    }

    private static string generate_example_from_schema(JObject? schema)
    {
        if (schema is null)
        {
            return "{}";
        }

        var type = schema["type"]?.Value<string>();
        
        if (type == "object")
        {
            var properties = schema["properties"] as JObject;
            if (properties is null)
            {
                return "{}";
            }

            var example = new JObject();
            foreach (var prop in properties)
            {
                var prop_name = prop.Key;
                var prop_schema = prop.Value as JObject;
                var prop_type = prop_schema?["type"]?.Value<string>();
                var prop_example = prop_schema?["example"];

                if (prop_example is not null)
                {
                    example[prop_name] = prop_example;
                }
                else
                {
                    example[prop_name] = get_default_value_for_type(prop_type);
                }
            }

            return example.ToString(Formatting.Indented);
        }

        return "{}";
    }

    private static JToken get_default_value_for_type(string? type)
    {
        return type switch
        {
            "string" => "",
            "integer" or "number" => 0,
            "boolean" => false,
            "array" => new JArray(),
            "object" => new JObject(),
            _ => ""
        };
    }

    private static Dictionary<string, JObject> parse_security_schemes(JObject? security_schemes)
    {
        var schemes = new Dictionary<string, JObject>();
        
        if (security_schemes is null) return schemes;

        foreach (var scheme in security_schemes)
        {
            schemes[scheme.Key] = scheme.Value as JObject ?? new JObject();
        }

        return schemes;
    }

    private static auth_config_model? parse_global_security(JArray? security, Dictionary<string, JObject> security_schemes)
    {
        if (security is null || security.Count == 0)
        {
            return null;
        }

        // Get first security requirement
        var first_security = security[0] as JObject;
        if (first_security is null) return null;

        foreach (var sec in first_security)
        {
            var scheme_name = sec.Key;
            if (!security_schemes.ContainsKey(scheme_name)) continue;

            var scheme = security_schemes[scheme_name];
            return create_auth_config(scheme);
        }

        return null;
    }

    private static auth_config_model? create_auth_config(JObject scheme)
    {
        var type = scheme["type"]?.Value<string>();
        var scheme_name = scheme["scheme"]?.Value<string>();

        return type switch
        {
            "http" when scheme_name == "basic" => new auth_config_model
            {
                type = auth_type.basic,
                basic = new basic_auth_model
                {
                    username = "{{username}}",
                    password = "{{password}}"
                }
            },
            "http" when scheme_name == "bearer" => new auth_config_model
            {
                type = auth_type.bearer,
                bearer = new bearer_auth_model
                {
                    token = "{{bearer_token}}"
                }
            },
            "apiKey" => new auth_config_model
            {
                type = auth_type.api_key,
                api_key = new api_key_auth_model
                {
                    key = scheme["name"]?.Value<string>() ?? "api_key",
                    value = "{{api_key}}",
                    location = scheme["in"]?.Value<string>() == "header" 
                        ? api_key_location.header 
                        : api_key_location.query
                }
            },
            "oauth2" => new auth_config_model
            {
                type = auth_type.oauth2_client_credentials,
                oauth2_client_credentials = new oauth2_client_credentials_model
                {
                    token_url = "{{token_url}}",
                    client_id = "{{client_id}}",
                    client_secret = "{{client_secret}}",
                    scope = ""
                }
            },
            _ => null
        };
    }

    private static bool is_http_method(string method)
    {
        return method is "get" or "post" or "put" or "patch" or "delete" or "head" or "options";
    }

    private static http_method map_http_method(string method)
    {
        return method.ToLowerInvariant() switch
        {
            "get" => http_method.get,
            "post" => http_method.post,
            "put" => http_method.put,
            "patch" => http_method.patch,
            "delete" => http_method.delete,
            "head" => http_method.head,
            "options" => http_method.options,
            _ => http_method.get
        };
    }
}
