namespace Core.Models;

public record script_context_model
{
    public required script_phase phase { get; init; }
    public required http_request_model request { get; init; }
    public http_response_model? response { get; init; }
    public required environment_model environment { get; init; }
    public IReadOnlyDictionary<string, string> collection_variables { get; init; } = new Dictionary<string, string>();
}
