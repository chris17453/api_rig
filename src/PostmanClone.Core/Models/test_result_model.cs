namespace PostmanClone.Core.Models;

public record test_result_model
{
    public required string name { get; init; }
    public required bool passed { get; init; }
    public string? error_message { get; init; }
    public long execution_time_ms { get; init; }
}
