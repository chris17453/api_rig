using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;

namespace PostmanClone.App.ViewModels;

/// <summary>
/// ViewModel for the pre-request and post-response script editors.
/// </summary>
public partial class script_editor_view_model : ObservableObject
{
    private readonly i_script_runner _script_runner;

    [ObservableProperty]
    private string _preRequestScript = string.Empty;

    [ObservableProperty]
    private string _postResponseScript = string.Empty;

    [ObservableProperty]
    private string _consoleOutput = string.Empty;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private string _selectedTab = "Pre-request";

    public script_editor_view_model(i_script_runner script_runner)
    {
        _script_runner = script_runner;
        
        // Default sample scripts
        _preRequestScript = @"// Pre-request script runs before the request is sent.
// Use pm.environment.set() to set variables
// Use pm.variables.get() to read variables

console.log(""Running pre-request script..."");

// Example: Set a timestamp header
// pm.environment.set(""timestamp"", Date.now());
";

        _postResponseScript = @"// Post-response script runs after receiving the response.
// Use pm.response to access the response data
// Use pm.test() and pm.expect() for assertions

console.log(""Running post-response script..."");

// Example: Validate status code
pm.test(""Status code is 200"", function () {
    pm.expect(pm.response.status).to.equal(200);
});

// Example: Check response time
pm.test(""Response time is acceptable"", function () {
    pm.expect(pm.response.responseTime).to.be.below(500);
});
";
    }

    [RelayCommand]
    private void ClearConsole()
    {
        ConsoleOutput = string.Empty;
    }

    [RelayCommand]
    private async Task RunScript()
    {
        IsRunning = true;
        try
        {
            var script = SelectedTab == "Pre-request" ? PreRequestScript : PostResponseScript;
            
            ConsoleOutput += $"[{DateTime.Now:HH:mm:ss}] Executing {SelectedTab} script...\n";
            
            // Create a mock context for testing scripts
            var context = new script_context_model
            {
                phase = SelectedTab == "Pre-request" ? script_phase.pre_request : script_phase.post_response,
                request = new http_request_model { name = "Test Request", url = "https://example.com", method = http_method.get },
                response = SelectedTab == "Post-response" ? new http_response_model 
                { 
                    status_code = 200, 
                    status_description = "OK",
                    body_string = "{\"test\": \"data\"}" 
                } : null,
                environment = new environment_model { name = "test", variables = new Dictionary<string, string>() }
            };

            script_execution_result_model result;
            if (SelectedTab == "Pre-request")
            {
                result = await _script_runner.run_pre_request_async(script, context, CancellationToken.None);
            }
            else
            {
                result = await _script_runner.run_post_response_async(script, context, CancellationToken.None);
            }

            // Show logs from script execution
            foreach (var log in result.logs)
            {
                ConsoleOutput += $"[{DateTime.Now:HH:mm:ss}] {log}\n";
            }

            // Show test results
            foreach (var test in result.test_results)
            {
                var status = test.passed ? "PASS" : "FAIL";
                ConsoleOutput += $"[{DateTime.Now:HH:mm:ss}] [{status}] {test.name}\n";
                if (!test.passed && !string.IsNullOrEmpty(test.error_message))
                {
                    ConsoleOutput += $"[{DateTime.Now:HH:mm:ss}]   Error: {test.error_message}\n";
                }
            }

            // Show errors
            foreach (var error in result.errors)
            {
                ConsoleOutput += $"[{DateTime.Now:HH:mm:ss}] [ERROR] {error}\n";
            }

            ConsoleOutput += $"[{DateTime.Now:HH:mm:ss}] Script completed {(result.success ? "successfully" : "with errors")}.\n";
        }
        catch (Exception ex)
        {
            ConsoleOutput += $"[{DateTime.Now:HH:mm:ss}] Error: {ex.Message}\n";
        }
        finally
        {
            IsRunning = false;
        }
    }

    public void AppendConsoleOutput(string message)
    {
        ConsoleOutput += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
    }

    public void LoadScriptsFromRequest(http_request_model request)
    {
        PreRequestScript = request.pre_request_script ?? string.Empty;
        PostResponseScript = request.post_response_script ?? string.Empty;
    }

    /// <summary>
    /// Creates a new request model with updated scripts (since models are immutable).
    /// </summary>
    public http_request_model CreateRequestWithScripts(http_request_model original)
    {
        return new http_request_model
        {
            id = original.id,
            name = original.name,
            method = original.method,
            url = original.url,
            headers = original.headers,
            query_params = original.query_params,
            body = original.body,
            auth = original.auth,
            pre_request_script = PreRequestScript,
            post_response_script = PostResponseScript,
            timeout_ms = original.timeout_ms
        };
    }
}
