using System.Diagnostics;
using Jint;
using Jint.Runtime;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using PostmanClone.Scripting.Api;
using PostmanClone.Scripting.Engine;

namespace PostmanClone.Scripting;

public class script_runner : i_script_runner
{
    private readonly int _timeout_ms;

    public script_runner(int timeout_ms = 5000)
    {
        _timeout_ms = timeout_ms;
    }

    public Task<script_execution_result_model> run_pre_request_async(
        string script,
        script_context_model context,
        CancellationToken cancellation_token)
    {
        return Task.Run(() => execute_script(script, context, cancellation_token), cancellation_token);
    }

    public Task<script_execution_result_model> run_post_response_async(
        string script,
        script_context_model context,
        CancellationToken cancellation_token)
    {
        return Task.Run(() => execute_script(script, context, cancellation_token), cancellation_token);
    }

    private script_execution_result_model execute_script(
        string script,
        script_context_model context,
        CancellationToken cancellation_token)
    {
        if (string.IsNullOrWhiteSpace(script))
        {
            return new script_execution_result_model
            {
                success = true,
                execution_time_ms = 0
            };
        }

        var stopwatch = Stopwatch.StartNew();
        var test_collector = new pm_test_collector();
        var errors = new List<string>();

        try
        {
            var engine = jint_engine_factory.create_with_cancellation(cancellation_token, _timeout_ms);
            var pm = new pm_api(context, test_collector);

            engine.SetValue("pm", pm);

            engine.SetValue("console", new
            {
                log = new Action<object[]>(args => test_collector.log(args)),
                info = new Action<object[]>(args => test_collector.log(args)),
                warn = new Action<object[]>(args => test_collector.log(args)),
                error = new Action<object[]>(args => test_collector.log(args))
            });

            engine.Execute(script);

            stopwatch.Stop();

            return new script_execution_result_model
            {
                success = errors.Count == 0,
                logs = test_collector.logs.ToList(),
                errors = errors,
                test_results = test_collector.results.ToList(),
                environment_updates = pm.get_environment_updates()
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                execution_time_ms = stopwatch.ElapsedMilliseconds
            };
        }
        catch (TimeoutException)
        {
            stopwatch.Stop();
            errors.Add("Script execution timed out");
            return new script_execution_result_model
            {
                success = false,
                logs = test_collector.logs.ToList(),
                errors = errors,
                test_results = test_collector.results.ToList(),
                execution_time_ms = stopwatch.ElapsedMilliseconds
            };
        }
        catch (ExecutionCanceledException)
        {
            stopwatch.Stop();
            errors.Add("Script execution was cancelled");
            return new script_execution_result_model
            {
                success = false,
                logs = test_collector.logs.ToList(),
                errors = errors,
                test_results = test_collector.results.ToList(),
                execution_time_ms = stopwatch.ElapsedMilliseconds
            };
        }
        catch (JavaScriptException jsEx)
        {
            stopwatch.Stop();
            errors.Add($"JavaScript error: {jsEx.Message}");
            return new script_execution_result_model
            {
                success = false,
                logs = test_collector.logs.ToList(),
                errors = errors,
                test_results = test_collector.results.ToList(),
                execution_time_ms = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            errors.Add($"Script error: {ex.Message}");
            return new script_execution_result_model
            {
                success = false,
                logs = test_collector.logs.ToList(),
                errors = errors,
                test_results = test_collector.results.ToList(),
                execution_time_ms = stopwatch.ElapsedMilliseconds
            };
        }
    }
}
