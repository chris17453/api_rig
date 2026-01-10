using Core.Models;

namespace Core.Interfaces;

public interface i_variable_resolver
{
    /// <summary>
    /// Resolves variables in a string using environment variables only.
    /// </summary>
    string resolve(
        string input,
        IReadOnlyDictionary<string, string> variables,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is);

    /// <summary>
    /// Resolves variables in a string using full context (environment, vault, etc).
    /// Supports {{variable}}, {{vault:name}}, {{$global.name}}, {{$collection.name}} syntax.
    /// </summary>
    Task<string> resolve_async(
        string input,
        variable_resolution_context context,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is,
        CancellationToken cancellation_token = default);

    http_request_model resolve_request(
        http_request_model request,
        IReadOnlyDictionary<string, string> variables,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is);

    /// <summary>
    /// Resolves all variables in a request using full context.
    /// </summary>
    Task<http_request_model> resolve_request_async(
        http_request_model request,
        variable_resolution_context context,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is,
        CancellationToken cancellation_token = default);

    IReadOnlyList<key_value_pair_model> resolve_key_value_pairs(
        IReadOnlyList<key_value_pair_model> pairs,
        IReadOnlyDictionary<string, string> variables,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is);
}
