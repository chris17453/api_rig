using System.Text.RegularExpressions;
using Core.Interfaces;
using Core.Models;

namespace Data.Services;

public partial class variable_resolver : i_variable_resolver
{
    // Matches {{variable}}, {{vault:name}}, {{$global.name}}, {{$collection.name}}
    private static readonly Regex variable_pattern = MyRegex();
    private static readonly Regex vault_pattern = VaultRegex();

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

    public async Task<string> resolve_async(
        string input,
        variable_resolution_context context,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is,
        CancellationToken cancellation_token = default)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        // First pass: resolve vault references {{vault:name}}
        if (context.is_vault_unlocked && context.vault_store != null)
        {
            var vaultMatches = vault_pattern.Matches(input);
            foreach (Match match in vaultMatches)
            {
                var secretName = match.Groups[1].Value;
                var secretValue = await context.vault_store.get_secret_value_async(secretName, cancellation_token);

                if (secretValue != null)
                {
                    input = input.Replace(match.Value, secretValue);
                }
                else if (policy == variable_resolution_policy.replace_with_empty)
                {
                    input = input.Replace(match.Value, string.Empty);
                }
                else if (policy == variable_resolution_policy.throw_error)
                {
                    throw new InvalidOperationException($"Vault secret '{secretName}' not found");
                }
                // leave_as_is: keep the {{vault:name}} text
            }
        }

        // Second pass: resolve regular variables with priority order
        return variable_pattern.Replace(input, match =>
        {
            var variableExpr = match.Groups[1].Value;

            // Check for prefixed variables
            if (variableExpr.StartsWith("$global."))
            {
                var name = variableExpr[8..]; // Remove "$global."
                if (context.global_variables.TryGetValue(name, out var globalValue))
                    return globalValue;
            }
            else if (variableExpr.StartsWith("$collection."))
            {
                var name = variableExpr[12..]; // Remove "$collection."
                if (context.collection_variables.TryGetValue(name, out var collValue))
                    return collValue;
            }
            else if (variableExpr.StartsWith("vault:"))
            {
                // Already handled in first pass, but handle any remaining
                return match.Value;
            }
            else
            {
                // Regular variable - check environment first, then collection, then global
                if (context.environment_variables.TryGetValue(variableExpr, out var envValue))
                    return envValue;
                if (context.collection_variables.TryGetValue(variableExpr, out var collValue))
                    return collValue;
                if (context.global_variables.TryGetValue(variableExpr, out var globalValue))
                    return globalValue;
            }

            return policy switch
            {
                variable_resolution_policy.leave_as_is => match.Value,
                variable_resolution_policy.replace_with_empty => string.Empty,
                variable_resolution_policy.throw_error => throw new InvalidOperationException($"Variable '{variableExpr}' not found"),
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

    public async Task<http_request_model> resolve_request_async(
        http_request_model request,
        variable_resolution_context context,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is,
        CancellationToken cancellation_token = default)
    {
        return new http_request_model
        {
            id = request.id,
            name = request.name,
            method = request.method,
            url = await resolve_async(request.url, context, policy, cancellation_token),
            headers = await resolve_key_value_pairs_async(request.headers, context, policy, cancellation_token),
            query_params = await resolve_key_value_pairs_async(request.query_params, context, policy, cancellation_token),
            body = request.body is not null
                ? await resolve_body_async(request.body, context, policy, cancellation_token)
                : null,
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

    private async Task<List<key_value_pair_model>> resolve_key_value_pairs_async(
        IReadOnlyList<key_value_pair_model> pairs,
        variable_resolution_context context,
        variable_resolution_policy policy,
        CancellationToken cancellation_token)
    {
        var result = new List<key_value_pair_model>();
        foreach (var p in pairs)
        {
            result.Add(new key_value_pair_model
            {
                key = await resolve_async(p.key, context, policy, cancellation_token),
                value = await resolve_async(p.value, context, policy, cancellation_token),
                enabled = p.enabled
            });
        }
        return result;
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

    private async Task<request_body_model> resolve_body_async(
        request_body_model body,
        variable_resolution_context context,
        variable_resolution_policy policy,
        CancellationToken cancellation_token)
    {
        Dictionary<string, string>? formData = null;
        Dictionary<string, string>? formUrlEncoded = null;

        if (body.form_data != null)
        {
            formData = new Dictionary<string, string>();
            foreach (var kvp in body.form_data)
            {
                var key = await resolve_async(kvp.Key, context, policy, cancellation_token);
                var value = await resolve_async(kvp.Value, context, policy, cancellation_token);
                formData[key] = value;
            }
        }

        if (body.form_urlencoded != null)
        {
            formUrlEncoded = new Dictionary<string, string>();
            foreach (var kvp in body.form_urlencoded)
            {
                var key = await resolve_async(kvp.Key, context, policy, cancellation_token);
                var value = await resolve_async(kvp.Value, context, policy, cancellation_token);
                formUrlEncoded[key] = value;
            }
        }

        return new request_body_model
        {
            body_type = body.body_type,
            raw_content = body.raw_content is not null
                ? await resolve_async(body.raw_content, context, policy, cancellation_token)
                : null,
            form_data = formData,
            form_urlencoded = formUrlEncoded
        };
    }

    [GeneratedRegex(@"\{\{([^}]+)\}\}")]
    private static partial Regex MyRegex();

    [GeneratedRegex(@"\{\{vault:([^}]+)\}\}")]
    private static partial Regex VaultRegex();
}
