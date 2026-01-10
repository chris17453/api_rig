namespace Core.Models;

public record script_execution_result_model
{
    public bool success { get; init; }
    public IReadOnlyList<string> logs { get; init; } = [];
    public IReadOnlyList<string> errors { get; init; } = [];
    public IReadOnlyList<test_result_model> test_results { get; init; } = [];
    public IReadOnlyDictionary<string, string>? environment_updates { get; init; }
    public long execution_time_ms { get; init; }
}
