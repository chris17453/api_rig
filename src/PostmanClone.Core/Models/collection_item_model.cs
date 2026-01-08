namespace PostmanClone.Core.Models;

public record collection_item_model
{
    public string id { get; init; } = Guid.NewGuid().ToString();
    public required string name { get; init; }
    public string? description { get; init; }
    public bool is_folder { get; init; }
    public string? folder_path { get; init; }
    public http_request_model? request { get; init; }
    public IReadOnlyList<collection_item_model>? children { get; init; }
}
