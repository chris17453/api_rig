using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using PostmanClone.Data.Context;
using PostmanClone.Data.Entities;

namespace PostmanClone.Data.Repositories;

public class collection_repository : i_collection_repository
{
    private readonly postman_clone_db_context _context;
    private static readonly JsonSerializerSettings _json_settings = new()
    {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
    };

    public collection_repository(postman_clone_db_context context)
    {
        _context = context;
    }

    public async Task<postman_collection_model> import_from_file_async(string file_path, CancellationToken cancellation_token)
    {
        if (!File.Exists(file_path))
        {
            throw new FileNotFoundException($"Collection file not found: {file_path}", file_path);
        }

        var json_content = await File.ReadAllTextAsync(file_path, cancellation_token);
        return await import_from_json_async(json_content, cancellation_token);
    }

    public async Task<postman_collection_model> import_from_json_async(string json_content, CancellationToken cancellation_token)
    {
        var json_obj = JObject.Parse(json_content);
        var collection = parse_postman_collection(json_obj);
        
        await save_async(collection, cancellation_token);
        return collection;
    }

    public async Task<IReadOnlyList<postman_collection_model>> list_all_async(CancellationToken cancellation_token)
    {
        var entities = await _context.collections
            .AsNoTracking()
            .ToListAsync(cancellation_token);

        var result = new List<postman_collection_model>();
        foreach (var entity in entities)
        {
            var items = await load_collection_items_async(entity.id, cancellation_token);
            result.Add(map_to_model(entity, items));
        }

        return result;
    }

    public async Task<postman_collection_model?> get_by_id_async(string id, CancellationToken cancellation_token)
    {
        var entity = await _context.collections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.id == id, cancellation_token);

        if (entity is null)
        {
            return null;
        }

        var items = await load_collection_items_async(id, cancellation_token);
        return map_to_model(entity, items);
    }

    public async Task save_async(postman_collection_model collection, CancellationToken cancellation_token)
    {
        var existing = await _context.collections
            .FirstOrDefaultAsync(c => c.id == collection.id, cancellation_token);

        if (existing is not null)
        {
            // Update existing collection
            existing.name = collection.name;
            existing.description = collection.description;
            existing.version = collection.version;
            existing.updated_at = DateTime.UtcNow;
            existing.auth_json = collection.auth is null 
                ? null 
                : JsonConvert.SerializeObject(collection.auth, _json_settings);
            existing.variables_json = collection.variables.Count == 0 
                ? null 
                : JsonConvert.SerializeObject(collection.variables, _json_settings);

            // Delete existing items and re-add
            var existing_items = await _context.collection_items
                .Where(i => i.collection_id == collection.id)
                .ToListAsync(cancellation_token);
            _context.collection_items.RemoveRange(existing_items);
        }
        else
        {
            // Add new collection
            var entity = map_to_entity(collection);
            _context.collections.Add(entity);
        }

        // Add all items
        var item_entities = flatten_items(collection.items, collection.id, null);
        _context.collection_items.AddRange(item_entities);

        await _context.SaveChangesAsync(cancellation_token);
    }

    public async Task delete_async(string id, CancellationToken cancellation_token)
    {
        var entity = await _context.collections
            .FirstOrDefaultAsync(c => c.id == id, cancellation_token);

        if (entity is not null)
        {
            _context.collections.Remove(entity);
            await _context.SaveChangesAsync(cancellation_token);
        }
    }

    public async Task<string> export_to_json_async(string id, CancellationToken cancellation_token)
    {
        var collection = await get_by_id_async(id, cancellation_token);
        
        if (collection is null)
        {
            throw new InvalidOperationException($"Collection not found: {id}");
        }

        return convert_to_postman_format(collection);
    }

    public async Task export_to_file_async(string id, string file_path, CancellationToken cancellation_token)
    {
        var json = await export_to_json_async(id, cancellation_token);
        
        var directory = Path.GetDirectoryName(file_path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        
        await File.WriteAllTextAsync(file_path, json, cancellation_token);
    }

    private async Task<List<collection_item_entity>> load_collection_items_async(string collection_id, CancellationToken cancellation_token)
    {
        return await _context.collection_items
            .Where(i => i.collection_id == collection_id)
            .AsNoTracking()
            .OrderBy(i => i.sort_order)
            .ToListAsync(cancellation_token);
    }

    private static postman_collection_model parse_postman_collection(JObject json)
    {
        var info = json["info"];
        var items = json["item"] as JArray ?? [];

        return new postman_collection_model
        {
            id = info?["_postman_id"]?.Value<string>() ?? Guid.NewGuid().ToString(),
            name = info?["name"]?.Value<string>() ?? "Unnamed Collection",
            description = info?["description"]?.Value<string>(),
            version = info?["version"]?.Value<string>(),
            items = parse_items(items),
            variables = parse_variables(json["variable"] as JArray),
            auth = parse_auth(json["auth"]),
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
        var is_folder = item["item"] is not null && request_obj is null;

        return new collection_item_model
        {
            id = item["id"]?.Value<string>() ?? Guid.NewGuid().ToString(),
            name = item["name"]?.Value<string>() ?? "Unnamed",
            description = item["description"]?.Value<string>(),
            is_folder = is_folder,
            request = is_folder ? null : parse_request(request_obj, item["name"]?.Value<string>() ?? "Unnamed"),
            children = is_folder ? parse_items(item["item"] as JArray) : null
        };
    }

    private static http_request_model? parse_request(JToken? request, string name)
    {
        if (request is null)
        {
            return null;
        }

        var method_str = request["method"]?.Value<string>() ?? "GET";
        var url = request["url"];
        var url_str = url is JValue ? url.Value<string>() : url?["raw"]?.Value<string>() ?? "";

        return new http_request_model
        {
            name = name,
            method = Enum.TryParse<http_method>(method_str, true, out var method) ? method : http_method.get,
            url = url_str ?? "",
            headers = parse_key_value_pairs(request["header"] as JArray),
            body = parse_body(request["body"]),
            auth = parse_auth(request["auth"])
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

        // Simplified auth parsing - can be extended as needed
        return new auth_config_model
        {
            type = Enum.TryParse<auth_type>(type_str, true, out var type) ? type : auth_type.none
        };
    }

    private static collection_entity map_to_entity(postman_collection_model model)
    {
        return new collection_entity
        {
            id = model.id,
            name = model.name,
            description = model.description,
            version = model.version,
            created_at = model.created_at,
            updated_at = model.updated_at,
            auth_json = model.auth is null 
                ? null 
                : JsonConvert.SerializeObject(model.auth, _json_settings),
            variables_json = model.variables.Count == 0 
                ? null 
                : JsonConvert.SerializeObject(model.variables, _json_settings)
        };
    }

    private static List<collection_item_entity> flatten_items(
        IReadOnlyList<collection_item_model> items, 
        string collection_id, 
        string? parent_id,
        string folder_path = "")
    {
        var result = new List<collection_item_entity>();
        var sort_order = 0;

        foreach (var item in items)
        {
            var current_path = string.IsNullOrEmpty(folder_path) 
                ? item.name 
                : $"{folder_path}/{item.name}";

            var entity = new collection_item_entity
            {
                id = item.id,
                name = item.name,
                description = item.description,
                is_folder = item.is_folder,
                folder_path = item.is_folder ? current_path : folder_path,
                sort_order = sort_order++,
                collection_id = collection_id,
                parent_item_id = parent_id
            };

            if (!item.is_folder && item.request is not null)
            {
                entity.request_method = item.request.method;
                entity.request_url = item.request.url;
                entity.pre_request_script = item.request.pre_request_script;
                entity.post_response_script = item.request.post_response_script;
                entity.timeout_ms = item.request.timeout_ms;
                entity.request_headers_json = item.request.headers.Count == 0 
                    ? null 
                    : JsonConvert.SerializeObject(item.request.headers, _json_settings);
                entity.request_query_params_json = item.request.query_params.Count == 0 
                    ? null 
                    : JsonConvert.SerializeObject(item.request.query_params, _json_settings);
                entity.request_body_json = item.request.body is null 
                    ? null 
                    : JsonConvert.SerializeObject(item.request.body, _json_settings);
                entity.request_auth_json = item.request.auth is null 
                    ? null 
                    : JsonConvert.SerializeObject(item.request.auth, _json_settings);
            }

            result.Add(entity);

            if (item.is_folder && item.children is not null)
            {
                result.AddRange(flatten_items(item.children, collection_id, item.id, current_path));
            }
        }

        return result;
    }

    private static postman_collection_model map_to_model(collection_entity entity, List<collection_item_entity> item_entities)
    {
        var root_items = item_entities.Where(i => i.parent_item_id is null).ToList();
        var items = build_item_tree(root_items, item_entities);

        return new postman_collection_model
        {
            id = entity.id,
            name = entity.name,
            description = entity.description,
            version = entity.version,
            items = items,
            variables = string.IsNullOrEmpty(entity.variables_json) 
                ? [] 
                : JsonConvert.DeserializeObject<List<key_value_pair_model>>(entity.variables_json) ?? [],
            auth = string.IsNullOrEmpty(entity.auth_json) 
                ? null 
                : JsonConvert.DeserializeObject<auth_config_model>(entity.auth_json),
            created_at = entity.created_at,
            updated_at = entity.updated_at
        };
    }

    private static IReadOnlyList<collection_item_model> build_item_tree(
        List<collection_item_entity> items, 
        List<collection_item_entity> all_items)
    {
        return items
            .OrderBy(i => i.sort_order)
            .Select(item =>
            {
                var children = all_items.Where(i => i.parent_item_id == item.id).ToList();

                return new collection_item_model
                {
                    id = item.id,
                    name = item.name,
                    description = item.description,
                    is_folder = item.is_folder,
                    folder_path = item.folder_path,
                    request = item.is_folder ? null : map_item_to_request(item),
                    children = item.is_folder ? build_item_tree(children, all_items) : null
                };
            })
            .ToList();
    }

    private static http_request_model? map_item_to_request(collection_item_entity entity)
    {
        if (entity.request_method is null)
        {
            return null;
        }

        return new http_request_model
        {
            name = entity.name,
            method = entity.request_method.Value,
            url = entity.request_url ?? "",
            headers = string.IsNullOrEmpty(entity.request_headers_json) 
                ? [] 
                : JsonConvert.DeserializeObject<List<key_value_pair_model>>(entity.request_headers_json) ?? [],
            query_params = string.IsNullOrEmpty(entity.request_query_params_json) 
                ? [] 
                : JsonConvert.DeserializeObject<List<key_value_pair_model>>(entity.request_query_params_json) ?? [],
            body = string.IsNullOrEmpty(entity.request_body_json) 
                ? null 
                : JsonConvert.DeserializeObject<request_body_model>(entity.request_body_json),
            auth = string.IsNullOrEmpty(entity.request_auth_json) 
                ? null 
                : JsonConvert.DeserializeObject<auth_config_model>(entity.request_auth_json),
            pre_request_script = entity.pre_request_script,
            post_response_script = entity.post_response_script,
            timeout_ms = entity.timeout_ms
        };
    }

    private static string convert_to_postman_format(postman_collection_model collection)
    {
        var postman_obj = new JObject
        {
            ["info"] = new JObject
            {
                ["_postman_id"] = collection.id,
                ["name"] = collection.name,
                ["description"] = collection.description,
                ["schema"] = "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
            },
            ["item"] = convert_items_to_postman_format(collection.items)
        };

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

    private static JObject convert_request_to_postman_format(http_request_model request)
    {
        var request_obj = new JObject
        {
            ["method"] = request.method.ToString().ToUpperInvariant(),
            ["url"] = new JObject
            {
                ["raw"] = request.url
            }
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

        if (request.body is not null)
        {
            request_obj["body"] = convert_body_to_postman_format(request.body);
        }

        return request_obj;
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
}
