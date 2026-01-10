using Core.Interfaces;
using Core.Models;

namespace Data.Services;

public class request_orchestrator
{
    private readonly i_request_executor _request_executor;
    private readonly i_script_runner _script_runner;
    private readonly i_environment_store _environment_store;
    private readonly i_variable_resolver _variable_resolver;

    public request_orchestrator(
        i_request_executor request_executor,
        i_script_runner script_runner,
        i_environment_store environment_store,
        i_variable_resolver variable_resolver)
    {
        _request_executor = request_executor;
        _script_runner = script_runner;
        _environment_store = environment_store;
        _variable_resolver = variable_resolver;
    }

    public async Task<request_execution_result> execute_request_async(
        http_request_model request,
        CancellationToken cancellation_token)
    {
        var result = new request_execution_result();

        var environment = await _environment_store.get_active_async(cancellation_token);
        var variables = environment?.variables ?? new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(request.pre_request_script))
        {
            var pre_context = new script_context_model
            {
                phase = script_phase.pre_request,
                request = request,
                response = null,
                environment = environment ?? new environment_model { name = "default", variables = variables }
            };

            result.pre_script_result = await _script_runner.run_pre_request_async(
                request.pre_request_script,
                pre_context,
                cancellation_token);

            if (result.pre_script_result.environment_updates is not null)
            {
                foreach (var update in result.pre_script_result.environment_updates)
                {
                    await _environment_store.set_variable_async(update.Key, update.Value, cancellation_token);
                }

                environment = await _environment_store.get_active_async(cancellation_token);
                variables = environment?.variables ?? new Dictionary<string, string>();
            }
        }

        var resolved_request = _variable_resolver.resolve_request(
            request,
            variables,
            variable_resolution_policy.leave_as_is);

        result.response = await _request_executor.execute_async(resolved_request, cancellation_token);

        if (!string.IsNullOrWhiteSpace(request.post_response_script))
        {
            var post_context = new script_context_model
            {
                phase = script_phase.post_response,
                request = resolved_request,
                response = result.response,
                environment = environment ?? new environment_model { name = "default", variables = variables }
            };

            result.post_script_result = await _script_runner.run_post_response_async(
                request.post_response_script,
                post_context,
                cancellation_token);

            if (result.post_script_result.environment_updates is not null)
            {
                foreach (var update in result.post_script_result.environment_updates)
                {
                    await _environment_store.set_variable_async(update.Key, update.Value, cancellation_token);
                }
            }
        }

        return result;
    }
}

public class request_execution_result
{
    public http_response_model? response { get; set; }
    public script_execution_result_model? pre_script_result { get; set; }
    public script_execution_result_model? post_script_result { get; set; }

    public bool has_script_errors =>
        (pre_script_result?.success == false) ||
        (post_script_result?.success == false);

    public IReadOnlyList<test_result_model> all_test_results
    {
        get
        {
            var results = new List<test_result_model>();
            if (pre_script_result?.test_results is not null)
                results.AddRange(pre_script_result.test_results);
            if (post_script_result?.test_results is not null)
                results.AddRange(post_script_result.test_results);
            return results;
        }
    }

    public IReadOnlyList<string> all_logs
    {
        get
        {
            var logs = new List<string>();
            if (pre_script_result?.logs is not null)
                logs.AddRange(pre_script_result.logs);
            if (post_script_result?.logs is not null)
                logs.AddRange(post_script_result.logs);
            return logs;
        }
    }

    public IReadOnlyList<string> all_errors
    {
        get
        {
            var errors = new List<string>();
            if (pre_script_result?.errors is not null)
                errors.AddRange(pre_script_result.errors);
            if (post_script_result?.errors is not null)
                errors.AddRange(post_script_result.errors);
            return errors;
        }
    }
}
