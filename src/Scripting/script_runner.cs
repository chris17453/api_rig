using System.Diagnostics;
using Jint;
using Jint.Runtime;
using Core.Interfaces;
using Core.Models;
using Scripting.Api;
using Scripting.Engine;

namespace Scripting;

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

            var console = new ConsoleInterop(test_collector);
            engine.SetValue("console", console);

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

    private class ConsoleInterop
    {
        private readonly pm_test_collector _collector;

        public ConsoleInterop(pm_test_collector collector)
        {
            _collector = collector;
        }

        public void log(params object[] args) => _collector.log(args);
        public void info(params object[] args) => _collector.log(args);
        public void warn(params object[] args) => _collector.log(args);
        public void error(params object[] args) => _collector.log(args);
    }
}
