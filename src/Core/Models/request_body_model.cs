namespace Core.Models;

public record request_body_model
{
    public request_body_type body_type { get; init; } = request_body_type.none;
    public string? raw_content { get; init; }
    public IReadOnlyDictionary<string, string>? form_data { get; init; }
    public IReadOnlyDictionary<string, string>? form_urlencoded { get; init; }
}
