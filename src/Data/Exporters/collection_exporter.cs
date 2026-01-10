using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Core.Models;

namespace Data.Exporters;

public class collection_exporter
{
    private static readonly JsonSerializerSettings _json_settings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
    };

    public string export(postman_collection_model collection)
    {
        var postman_obj = new JObject
        {
            ["info"] = create_info_object(collection),
            ["item"] = convert_items_to_postman_format(collection.items)
        };

        if (collection.auth is not null)
        {
            postman_obj["auth"] = convert_auth_to_postman_format(collection.auth);
        }

        if (collection.variables.Count > 0)
        {
            postman_obj["variable"] = JArray.FromObject(collection.variables.Select(v => new
            {
                key = v.key,
                value = v.value,
                disabled = !v.enabled
            }));
        }

        return postman_obj.ToString(Formatting.Indented);
    }

    public void export_to_file(postman_collection_model collection, string file_path)
    {
        var json = export(collection);
        
        var directory = Path.GetDirectoryName(file_path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        File.WriteAllText(file_path, json);
    }

    private static JObject create_info_object(postman_collection_model collection)
    {
        var info = new JObject
        {
            ["_postman_id"] = collection.id,
            ["name"] = collection.name,
            ["schema"] = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
        };

        if (!string.IsNullOrEmpty(collection.description))
        {
            info["description"] = collection.description;
        }

        if (!string.IsNullOrEmpty(collection.version))
        {
            info["version"] = collection.version;
        }

        return info;
    }

    private static JArray convert_items_to_postman_format(IReadOnlyList<collection_item_model> items)
    {
        var array = new JArray();

        foreach (var item in items)
        {
            var item_obj = new JObject
            {
                ["id"] = item.id,
                ["name"] = item.name
            };

            if (!string.IsNullOrEmpty(item.description))
            {
                item_obj["description"] = item.description;
            }

            // Add events for scripts
            var events = create_events_array(item);
            if (events.Count > 0)
            {
                item_obj["event"] = events;
            }

            if (item.is_folder && item.children is not null)
            {
                item_obj["item"] = convert_items_to_postman_format(item.children);
            }
            else if (item.request is not null)
            {
                item_obj["request"] = convert_request_to_postman_format(item.request);
            }

            array.Add(item_obj);
        }

        return array;
    }

    private static JArray create_events_array(collection_item_model item)
    {
        var events = new JArray();

        if (item.request is not null && !string.IsNullOrEmpty(item.request.pre_request_script))
        {
            events.Add(new JObject
            {
                ["listen"] = "prerequest",
                ["script"] = new JObject
                {
                    ["type"] = "text/javascript",
                    ["exec"] = new JArray(item.request.pre_request_script.Split('\n'))
                }
            });
        }

        if (item.request is not null && !string.IsNullOrEmpty(item.request.post_response_script))
        {
            events.Add(new JObject
            {
                ["listen"] = "test",
                ["script"] = new JObject
                {
                    ["type"] = "text/javascript",
                    ["exec"] = new JArray(item.request.post_response_script.Split('\n'))
                }
            });
        }

        return events;
    }

    private static JObject convert_request_to_postman_format(http_request_model request)
    {
        var request_obj = new JObject
        {
            ["method"] = request.method.ToString().ToUpperInvariant(),
            ["url"] = create_url_object(request)
        };

        if (request.headers.Count > 0)
        {
            request_obj["header"] = JArray.FromObject(request.headers.Select(h => new
            {
                key = h.key,
                value = h.value,
                disabled = !h.enabled
            }));
        }

        if (request.body is not null && request.body.body_type != request_body_type.none)
        {
            var body = convert_body_to_postman_format(request.body);
            if (body is not null)
            {
                request_obj["body"] = body;
            }
        }

        if (request.auth is not null)
        {
            request_obj["auth"] = convert_auth_to_postman_format(request.auth);
        }

        return request_obj;
    }

    private static JObject create_url_object(http_request_model request)
    {
        var url_obj = new JObject
        {
            ["raw"] = request.url
        };

        // Parse URL to extract components
        if (Uri.TryCreate(request.url, UriKind.Absolute, out var uri))
        {
            url_obj["protocol"] = uri.Scheme;
            url_obj["host"] = new JArray(uri.Host.Split('.'));
            
            if (uri.AbsolutePath != "/")
            {
                var path_segments = uri.AbsolutePath.Trim('/').Split('/').Where(s => !string.IsNullOrEmpty(s));
                url_obj["path"] = new JArray(path_segments);
            }
            else
            {
                url_obj["path"] = new JArray();
            }

            if (!uri.IsDefaultPort)
            {
                url_obj["port"] = uri.Port.ToString();
            }
        }

        // Add query parameters if present
        if (request.query_params.Count > 0)
        {
            url_obj["query"] = JArray.FromObject(request.query_params.Select(q => new
            {
                key = q.key,
                value = q.value,
                disabled = !q.enabled
            }));
        }

        return url_obj;
    }

    private static JObject? convert_body_to_postman_format(request_body_model body)
    {
        return body.body_type switch
        {
            request_body_type.raw => new JObject
            {
                ["mode"] = "raw",
                ["raw"] = body.raw_content
            },
            request_body_type.form_data when body.form_data is not null => new JObject
            {
                ["mode"] = "formdata",
                ["formdata"] = JArray.FromObject(body.form_data.Select(kv => new
                {
                    key = kv.Key,
                    value = kv.Value
                }))
            },
            request_body_type.x_www_form_urlencoded when body.form_urlencoded is not null => new JObject
            {
                ["mode"] = "urlencoded",
                ["urlencoded"] = JArray.FromObject(body.form_urlencoded.Select(kv => new
                {
                    key = kv.Key,
                    value = kv.Value
                }))
            },
            _ => null
        };
    }

    private static JObject convert_auth_to_postman_format(auth_config_model auth)
    {
        var auth_obj = new JObject();

        switch (auth.type)
        {
            case auth_type.basic when auth.basic is not null:
                auth_obj["type"] = "basic";
                auth_obj["basic"] = new JArray
                {
                    new JObject { ["key"] = "username", ["value"] = auth.basic.username, ["type"] = "string" },
                    new JObject { ["key"] = "password", ["value"] = auth.basic.password, ["type"] = "string" }
                };
                break;

            case auth_type.bearer when auth.bearer is not null:
                auth_obj["type"] = "bearer";
                auth_obj["bearer"] = new JArray
                {
                    new JObject { ["key"] = "token", ["value"] = auth.bearer.token, ["type"] = "string" }
                };
                break;

            case auth_type.api_key when auth.api_key is not null:
                auth_obj["type"] = "apikey";
                auth_obj["apikey"] = new JArray
                {
                    new JObject { ["key"] = "key", ["value"] = auth.api_key.key, ["type"] = "string" },
                    new JObject { ["key"] = "value", ["value"] = auth.api_key.value, ["type"] = "string" },
                    new JObject { ["key"] = "in", ["value"] = auth.api_key.location.ToString().ToLowerInvariant(), ["type"] = "string" }
                };
                break;

            case auth_type.oauth2_client_credentials when auth.oauth2_client_credentials is not null:
                auth_obj["type"] = "oauth2";
                auth_obj["oauth2"] = new JArray
                {
                    new JObject { ["key"] = "accessTokenUrl", ["value"] = auth.oauth2_client_credentials.token_url, ["type"] = "string" },
                    new JObject { ["key"] = "clientId", ["value"] = auth.oauth2_client_credentials.client_id, ["type"] = "string" },
                    new JObject { ["key"] = "clientSecret", ["value"] = auth.oauth2_client_credentials.client_secret, ["type"] = "string" },
                    new JObject { ["key"] = "scope", ["value"] = auth.oauth2_client_credentials.scope, ["type"] = "string" },
                    new JObject { ["key"] = "grant_type", ["value"] = "client_credentials", ["type"] = "string" }
                };
                break;

            case auth_type.none:
            default:
                auth_obj["type"] = "noauth";
                break;
        }

        return auth_obj;
    }
}
