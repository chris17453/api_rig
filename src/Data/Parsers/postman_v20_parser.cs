using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Core.Models;

namespace Data.Parsers;

public class postman_v20_parser
{
    private const string V20_SCHEMA = "https://schema.getpostman.com/json/collection/v2.0.0/collection.json";

    public bool can_parse(string json)
    {
        try
        {
            var obj = JObject.Parse(json);
            var schema = obj["info"]?["schema"]?.Value<string>();
            return schema == V20_SCHEMA;
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
        var items = obj["item"] as JArray ?? [];
        var variables = obj["variable"] as JArray;

        return new postman_collection_model
        {
            id = info?["_postman_id"]?.Value<string>() ?? Guid.NewGuid().ToString(),
            name = info?["name"]?.Value<string>() ?? "Unnamed Collection",
            description = info?["description"]?.Value<string>(),
            version = info?["version"]?.Value<string>(),
            items = parse_items(items),
            variables = parse_variables(variables),
            auth = parse_auth(obj["auth"]),
            created_at = DateTime.UtcNow
        };
    }

    private static IReadOnlyList<collection_item_model> parse_items(JArray? items)
    {
        if (items is null || items.Count == 0)
        {
            return [];
        }

        var result = new List<collection_item_model>();
        foreach (var item in items)
        {
            result.Add(parse_item(item));
        }
        return result;
    }

    private static collection_item_model parse_item(JToken item)
    {
        var request_obj = item["request"];
        var nested_items = item["item"] as JArray;
        var is_folder = nested_items is not null && request_obj is null;

        return new collection_item_model
        {
            id = item["id"]?.Value<string>() ?? Guid.NewGuid().ToString(),
            name = item["name"]?.Value<string>() ?? "Unnamed",
            description = item["description"]?.Value<string>(),
            is_folder = is_folder,
            request = is_folder ? null : parse_request(request_obj, item["name"]?.Value<string>() ?? "Unnamed"),
            children = is_folder ? parse_items(nested_items) : null
        };
    }

    private static http_request_model? parse_request(JToken? request, string name)
    {
        if (request is null)
        {
            return null;
        }

        var method_str = request["method"]?.Value<string>() ?? "GET";
        var (url, query_params) = parse_url(request["url"]);

        return new http_request_model
        {
            name = name,
            method = parse_http_method(method_str),
            url = url,
            headers = parse_key_value_pairs(request["header"] as JArray),
            query_params = query_params,
            body = parse_body(request["body"]),
            auth = parse_auth(request["auth"])
        };
    }

    private static (string url, IReadOnlyList<key_value_pair_model> query_params) parse_url(JToken? url_token)
    {
        if (url_token is null)
        {
            return ("", []);
        }

        // v2.0 can have URL as string or object
        if (url_token is JValue)
        {
            return (url_token.Value<string>() ?? "", []);
        }

        var raw = url_token["raw"]?.Value<string>() ?? "";
        var query_array = url_token["query"] as JArray;
        var query_params = parse_key_value_pairs(query_array);

        return (raw, query_params);
    }

    private static http_method parse_http_method(string method)
    {
        return method.ToUpperInvariant() switch
        {
            "GET" => http_method.get,
            "POST" => http_method.post,
            "PUT" => http_method.put,
            "DELETE" => http_method.delete,
            "PATCH" => http_method.patch,
            "HEAD" => http_method.head,
            "OPTIONS" => http_method.options,
            _ => http_method.get
        };
    }

    private static IReadOnlyList<key_value_pair_model> parse_key_value_pairs(JArray? array)
    {
        if (array is null || array.Count == 0)
        {
            return [];
        }

        return array
            .Select(item => new key_value_pair_model
            {
                key = item["key"]?.Value<string>() ?? "",
                value = item["value"]?.Value<string>() ?? "",
                enabled = item["disabled"]?.Value<bool>() != true
            })
            .ToList();
    }

    private static IReadOnlyList<key_value_pair_model> parse_variables(JArray? array)
    {
        if (array is null || array.Count == 0)
        {
            return [];
        }

        return array
            .Select(item => new key_value_pair_model
            {
                key = item["key"]?.Value<string>() ?? "",
                value = item["value"]?.Value<string>() ?? "",
                enabled = item["disabled"]?.Value<bool>() != true
            })
            .ToList();
    }

    private static request_body_model? parse_body(JToken? body)
    {
        if (body is null)
        {
            return null;
        }

        var mode = body["mode"]?.Value<string>();

        return mode switch
        {
            "raw" => new request_body_model
            {
                body_type = request_body_type.raw,
                raw_content = body["raw"]?.Value<string>()
            },
            "formdata" => new request_body_model
            {
                body_type = request_body_type.form_data,
                form_data = parse_form_data(body["formdata"] as JArray)
            },
            "urlencoded" => new request_body_model
            {
                body_type = request_body_type.x_www_form_urlencoded,
                form_urlencoded = parse_form_data(body["urlencoded"] as JArray)
            },
            _ => null
        };
    }

    private static IReadOnlyDictionary<string, string>? parse_form_data(JArray? array)
    {
        if (array is null || array.Count == 0)
        {
            return null;
        }

        return array
            .Where(item => item["disabled"]?.Value<bool>() != true)
            .ToDictionary(
                item => item["key"]?.Value<string>() ?? "",
                item => item["value"]?.Value<string>() ?? "");
    }

    private static auth_config_model? parse_auth(JToken? auth)
    {
        if (auth is null)
        {
            return null;
        }

        var type_str = auth["type"]?.Value<string>();
        if (string.IsNullOrEmpty(type_str) || type_str == "noauth")
        {
            return null;
        }

        var auth_type_value = type_str.ToLowerInvariant() switch
        {
            "basic" => auth_type.basic,
            "bearer" => auth_type.bearer,
            "apikey" => auth_type.api_key,
            "oauth2" => auth_type.oauth2_client_credentials,
            _ => auth_type.none
        };

        // v2.0 format has auth details in nested objects
        return new auth_config_model
        {
            type = auth_type_value,
            basic = auth_type_value == auth_type.basic ? parse_basic_auth(auth["basic"]) : null,
            bearer = auth_type_value == auth_type.bearer ? parse_bearer_auth(auth["bearer"]) : null,
            api_key = auth_type_value == auth_type.api_key ? parse_api_key_auth(auth["apikey"]) : null
        };
    }

    private static basic_auth_model? parse_basic_auth(JToken? basic)
    {
        if (basic is null)
        {
            return null;
        }

        // v2.0 can have basic auth as object with username/password properties
        return new basic_auth_model
        {
            username = basic["username"]?.Value<string>() ?? "",
            password = basic["password"]?.Value<string>() ?? ""
        };
    }

    private static bearer_auth_model? parse_bearer_auth(JToken? bearer)
    {
        if (bearer is null)
        {
            return null;
        }

        return new bearer_auth_model
        {
            token = bearer["token"]?.Value<string>() ?? ""
        };
    }

    private static api_key_auth_model? parse_api_key_auth(JToken? apikey)
    {
        if (apikey is null)
        {
            return null;
        }

        var location_str = apikey["in"]?.Value<string>() ?? "header";
        var location = location_str.ToLowerInvariant() == "query" 
            ? api_key_location.query 
            : api_key_location.header;

        return new api_key_auth_model
        {
            key = apikey["key"]?.Value<string>() ?? "",
            value = apikey["value"]?.Value<string>() ?? "",
            location = location
        };
    }
}
