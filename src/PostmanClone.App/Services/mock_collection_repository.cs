using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;

namespace PostmanClone.App.Services;

public class mock_collection_repository : i_collection_repository
{
    private readonly List<postman_collection_model> _collections;

    public mock_collection_repository()
    {
        _collections = new List<postman_collection_model>
        {
            new postman_collection_model
            {
                id = "col-1",
                name = "Sample API",
                description = "A sample API collection for testing",
                items = new List<collection_item_model>
                {
                    new collection_item_model
                    {
                        id = "req-1",
                        name = "Get Users",
                        is_folder = false,
                        request = new http_request_model
                        {
                            name = "Get Users",
                            method = http_method.get,
                            url = "https://jsonplaceholder.typicode.com/users"
                        }
                    },
                    new collection_item_model
                    {
                        id = "req-2",
                        name = "Get Posts",
                        is_folder = false,
                        request = new http_request_model
                        {
                            name = "Get Posts",
                            method = http_method.get,
                            url = "https://jsonplaceholder.typicode.com/posts"
                        }
                    },
                    new collection_item_model
                    {
                        id = "folder-1",
                        name = "User Operations",
                        is_folder = true,
                        children = new List<collection_item_model>
                        {
                            new collection_item_model
                            {
                                id = "req-3",
                                name = "Create User",
                                is_folder = false,
                                request = new http_request_model
                                {
                                    name = "Create User",
                                    method = http_method.post,
                                    url = "https://jsonplaceholder.typicode.com/users"
                                }
                            },
                            new collection_item_model
                            {
                                id = "req-4",
                                name = "Delete User",
                                is_folder = false,
                                request = new http_request_model
                                {
                                    name = "Delete User",
                                    method = http_method.delete,
                                    url = "https://jsonplaceholder.typicode.com/users/1"
                                }
                            }
                        }
                    }
                }
            },
            new postman_collection_model
            {
                id = "col-2",
                name = "Auth API",
                description = "Authentication endpoints",
                items = new List<collection_item_model>
                {
                    new collection_item_model
                    {
                        id = "req-5",
                        name = "Login",
                        is_folder = false,
                        request = new http_request_model
                        {
                            name = "Login",
                            method = http_method.post,
                            url = "https://api.example.com/auth/login"
                        }
                    }
                }
            }
        };
    }

    public Task<postman_collection_model?> get_by_id_async(string id, CancellationToken cancellation_token)
    {
        var collection = _collections.FirstOrDefault(c => c.id == id);
        return Task.FromResult(collection);
    }

    public Task<IReadOnlyList<postman_collection_model>> list_all_async(CancellationToken cancellation_token)
    {
        return Task.FromResult<IReadOnlyList<postman_collection_model>>(_collections);
    }

    public Task save_async(postman_collection_model collection, CancellationToken cancellation_token)
    {
        var existing = _collections.FindIndex(c => c.id == collection.id);
        if (existing >= 0)
            _collections[existing] = collection;
        else
            _collections.Add(collection);
        return Task.CompletedTask;
    }

    public Task delete_async(string id, CancellationToken cancellation_token)
    {
        _collections.RemoveAll(c => c.id == id);
        return Task.CompletedTask;
    }

    public Task<postman_collection_model> import_from_file_async(string file_path, CancellationToken cancellation_token)
    {
        // Mock: Return a new collection as if imported
        var imported = new postman_collection_model
        {
            name = Path.GetFileNameWithoutExtension(file_path),
            description = $"Imported from {file_path}"
        };
        _collections.Add(imported);
        return Task.FromResult(imported);
    }

    public Task<postman_collection_model> import_from_json_async(string json_content, CancellationToken cancellation_token)
    {
        var imported = new postman_collection_model
        {
            name = "Imported Collection",
            description = "Imported from JSON"
        };
        _collections.Add(imported);
        return Task.FromResult(imported);
    }

    public Task<string> export_to_json_async(string id, CancellationToken cancellation_token)
    {
        return Task.FromResult("{ \"info\": { \"name\": \"Exported Collection\" } }");
    }

    public Task export_to_file_async(string id, string file_path, CancellationToken cancellation_token)
    {
        return Task.CompletedTask;
    }
}
