using Core.Models;

namespace Core.Interfaces;

public interface i_script_runner
{
    Task<script_execution_result_model> run_pre_request_async(
        string script,
        script_context_model context,
        CancellationToken cancellation_token);

    Task<script_execution_result_model> run_post_response_async(
        string script,
        script_context_model context,
        CancellationToken cancellation_token);
}
