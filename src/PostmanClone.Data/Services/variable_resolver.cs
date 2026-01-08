using System.Text.RegularExpressions;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;

namespace PostmanClone.Data.Services;

public partial class variable_resolver : i_variable_resolver
{
    private static readonly Regex variable_pattern = MyRegex();

    public string resolve(
        string input,
        IReadOnlyDictionary<string, string> variables,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return variable_pattern.Replace(input, match =>
        {
            var variable_name = match.Groups[1].Value;

            if (variables.TryGetValue(variable_name, out var value))
            {
                return value;
            }

            return policy switch
            {
                variable_resolution_policy.leave_as_is => match.Value,
                variable_resolution_policy.replace_with_empty => string.Empty,
                variable_resolution_policy.throw_error => throw new InvalidOperationException($"Variable '{variable_name}' not found"),
                _ => match.Value
            };
        });
    }

    public http_request_model resolve_request(
        http_request_model request,
        IReadOnlyDictionary<string, string> variables,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is)
    {
        return new http_request_model
        {
            id = request.id,
            name = request.name,
            method = request.method,
            url = resolve(request.url, variables, policy),
            headers = resolve_key_value_pairs(request.headers, variables, policy).ToList(),
            query_params = resolve_key_value_pairs(request.query_params, variables, policy).ToList(),
            body = request.body is not null ? resolve_body(request.body, variables, policy) : null,
            auth = request.auth,
            timeout_ms = request.timeout_ms,
            pre_request_script = request.pre_request_script,
            post_response_script = request.post_response_script
        };
    }

    public IReadOnlyList<key_value_pair_model> resolve_key_value_pairs(
        IReadOnlyList<key_value_pair_model> pairs,
        IReadOnlyDictionary<string, string> variables,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is)
    {
        return pairs.Select(p => new key_value_pair_model
        {
            key = resolve(p.key, variables, policy),
            value = resolve(p.value, variables, policy),
            enabled = p.enabled
        }).ToList();
    }

    private request_body_model resolve_body(
        request_body_model body,
        IReadOnlyDictionary<string, string> variables,
        variable_resolution_policy policy)
    {
        return new request_body_model
        {
            body_type = body.body_type,
            raw_content = body.raw_content is not null ? resolve(body.raw_content, variables, policy) : null,
            form_data = body.form_data?.ToDictionary(
                kvp => resolve(kvp.Key, variables, policy),
                kvp => resolve(kvp.Value, variables, policy)),
            form_urlencoded = body.form_urlencoded?.ToDictionary(
                kvp => resolve(kvp.Key, variables, policy),
                kvp => resolve(kvp.Value, variables, policy))
        };
    }

    [GeneratedRegex(@"\{\{([^}]+)\}\}")]
    private static partial Regex MyRegex();
}
