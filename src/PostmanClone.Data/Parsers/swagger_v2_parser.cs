using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PostmanClone.Core.Models;

namespace PostmanClone.Data.Parsers;

public class swagger_v2_parser
{
    public bool can_parse(string json)
    {
        try
        {
            var obj = JObject.Parse(json);
            var swagger_version = obj["swagger"]?.Value<string>();
            return swagger_version == "2.0";
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
        var host = obj["host"]?.Value<string>();
        var base_path = obj["basePath"]?.Value<string>() ?? "";
        var schemes = obj["schemes"] as JArray;
        var paths = obj["paths"] as JObject;
        var security_definitions = obj["securityDefinitions"] as JObject;

        var base_url = construct_base_url(schemes, host, base_path);
        var items = parse_paths(paths, base_url);
        var security_schemes = parse_security_definitions(security_definitions);
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

    private static string construct_base_url(JArray? schemes, string? host, string base_path)
    {
        if (string.IsNullOrEmpty(host))
        {
            return "{{base_url}}";
        }

        var scheme = "https";
        if (schemes is not null && schemes.Count > 0)
        {
            scheme = schemes[0].Value<string>() ?? "https";
        }

        var url = $"{scheme}://{host}{base_path}";
        
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

        // Parse request body (Swagger 2.0 uses parameters with in: body)
        var request_body = parse_request_body_from_parameters(parameters, headers);

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
            
            // Skip body parameters as they're handled separately
            if (param_in == "body") continue;

            var required = param["required"]?.Value<bool>() ?? false;
            var description = param["description"]?.Value<string>();
            var default_value = param["default"]?.Value<string>();
            var example = param["x-example"]?.Value<string>();

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
                case "formData":
                    // Form data is handled in parse_request_body_from_parameters
                    break;
            }
        }
    }

    private static request_body_model? parse_request_body_from_parameters(JArray? parameters, List<key_value_pair_model> headers)
    {
        if (parameters is null) return null;

        // Check for body parameter
        JToken? body_param = null;
        var form_params = new List<JToken>();

        foreach (var param in parameters)
        {
            var param_in = param["in"]?.Value<string>();
            if (param_in == "body")
            {
                body_param = param;
            }
            else if (param_in == "formData")
            {
                form_params.Add(param);
            }
        }

        // Handle body parameter (JSON)
        if (body_param is not null)
        {
            var schema = body_param["schema"] as JObject;
            var example = body_param["x-example"];

            string body_text;
            if (example is not null)
            {
                body_text = example.ToString(Formatting.Indented);
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

        // Handle form data parameters
        if (form_params.Count > 0)
        {
            var form_data = new List<key_value_pair_model>();
            var is_multipart = false;

            foreach (var param in form_params)
            {
                var name = param["name"]?.Value<string>();
                if (name is null) continue;

                var type = param["type"]?.Value<string>();
                var example = param["x-example"]?.Value<string>() ?? "";

                // Check if any param is a file
                if (type == "file")
                {
                    is_multipart = true;
                }

                form_data.Add(new key_value_pair_model
                {
                    key = name,
                    value = example,
                    enabled = true
                });
            }

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

        return null;
    }

    private static string generate_example_from_schema(JObject? schema)
    {
        if (schema is null)
        {
            return "{}";
        }

        var type = schema["type"]?.Value<string>();
        var example = schema["example"];

        if (example is not null)
        {
            return example.ToString(Formatting.Indented);
        }

        if (type == "object")
        {
            var properties = schema["properties"] as JObject;
            if (properties is null)
            {
                return "{}";
            }

            var example_obj = new JObject();
            foreach (var prop in properties)
            {
                var prop_name = prop.Key;
                var prop_schema = prop.Value as JObject;
                var prop_example = prop_schema?["example"];

                if (prop_example is not null)
                {
                    example_obj[prop_name] = prop_example;
                }
                else
                {
                    var prop_type = prop_schema?["type"]?.Value<string>();
                    example_obj[prop_name] = get_default_value_for_type(prop_type);
                }
            }

            return example_obj.ToString(Formatting.Indented);
        }

        if (type == "array")
        {
            return "[]";
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

    private static Dictionary<string, JObject> parse_security_definitions(JObject? security_definitions)
    {
        var schemes = new Dictionary<string, JObject>();
        
        if (security_definitions is null) return schemes;

        foreach (var scheme in security_definitions)
        {
            schemes[scheme.Key] = scheme.Value as JObject ?? new JObject();
        }

        return schemes;
    }

    private static auth_config_model? parse_global_security(JArray? security, Dictionary<string, JObject> security_definitions)
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
            if (!security_definitions.ContainsKey(scheme_name)) continue;

            var scheme = security_definitions[scheme_name];
            return create_auth_config(scheme);
        }

        return null;
    }

    private static auth_config_model? create_auth_config(JObject scheme)
    {
        var type = scheme["type"]?.Value<string>();

        return type switch
        {
            "basic" => new auth_config_model
            {
                type = auth_type.basic,
                basic = new basic_auth_model
                {
                    username = "{{username}}",
                    password = "{{password}}"
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
                    token_url = scheme["tokenUrl"]?.Value<string>() ?? "{{token_url}}",
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
