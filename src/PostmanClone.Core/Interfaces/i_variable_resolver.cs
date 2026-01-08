using PostmanClone.Core.Models;

namespace PostmanClone.Core.Interfaces;

public interface i_variable_resolver
{
    string resolve(
        string input,
        IReadOnlyDictionary<string, string> variables,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is);

    http_request_model resolve_request(
        http_request_model request,
        IReadOnlyDictionary<string, string> variables,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is);

    IReadOnlyList<key_value_pair_model> resolve_key_value_pairs(
        IReadOnlyList<key_value_pair_model> pairs,
        IReadOnlyDictionary<string, string> variables,
        variable_resolution_policy policy = variable_resolution_policy.leave_as_is);
}
