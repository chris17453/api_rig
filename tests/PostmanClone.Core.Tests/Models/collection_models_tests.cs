using FluentAssertions;
using PostmanClone.Core.Models;

namespace PostmanClone.Core.Tests.Models;

public class Collection_Models_Tests
{
    [Fact]
    public void collection_allows_minimum_viable_tree_when_items_nested()
    {
        var nested_request = new http_request_model
        {
            name = "Get User",
            method = http_method.get,
            url = "https://api.example.com/users/1"
        };

        var folder_item = new collection_item_model
        {
            name = "Users",
            is_folder = true,
            children = new List<collection_item_model>
            {
                new()
                {
                    name = "Get User",
                    is_folder = false,
                    request = nested_request
                }
            }
        };

        var collection = new postman_collection_model
        {
            name = "My API",
            items = new List<collection_item_model> { folder_item }
        };

        collection.items.Should().HaveCount(1);
        collection.items[0].is_folder.Should().BeTrue();
        collection.items[0].children.Should().NotBeNull();
        collection.items[0].children!.Count.Should().Be(1);
        var first_child = collection.items[0].children![0];
        first_child.request.Should().NotBeNull();
        first_child.request!.name.Should().Be("Get User");
    }

    [Fact]
    public void collection_generates_unique_id_when_created()
    {
        var collection1 = new postman_collection_model { name = "Collection 1" };
        var collection2 = new postman_collection_model { name = "Collection 2" };

        collection1.id.Should().NotBe(collection2.id);
    }

    [Fact]
    public void collection_item_supports_folder_path_when_specified()
    {
        var item = new collection_item_model
        {
            name = "Create User",
            folder_path = "/Users/Admin",
            request = new http_request_model
            {
                name = "Create User",
                method = http_method.post,
                url = "https://api.example.com/users"
            }
        };

        item.folder_path.Should().Be("/Users/Admin");
    }
}
