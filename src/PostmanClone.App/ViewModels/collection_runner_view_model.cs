using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using PostmanClone.Data.Services;
using System.Collections.ObjectModel;

namespace PostmanClone.App.ViewModels;

/// <summary>
/// ViewModel for running all requests in a collection sequentially.
/// </summary>
public partial class collection_runner_view_model : ObservableObject
{
    private readonly request_orchestrator _orchestrator;
    private readonly i_collection_repository _collection_repository;
    private CancellationTokenSource? _cancellation_source;

    [ObservableProperty]
    private ObservableCollection<postman_collection_model> _collections = new();

    [ObservableProperty]
    private postman_collection_model? _selectedCollection;

    [ObservableProperty]
    private ObservableCollection<collection_run_item_view_model> _runResults = new();

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _totalRequests;

    [ObservableProperty]
    private int _completedRequests;

    [ObservableProperty]
    private int _passedRequests;

    [ObservableProperty]
    private int _failedRequests;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private int _delayBetweenRequests = 0;

    [ObservableProperty]
    private int _iterations = 1;

    public collection_runner_view_model(
        request_orchestrator orchestrator,
        i_collection_repository collection_repository)
    {
        _orchestrator = orchestrator;
        _collection_repository = collection_repository;
    }

    public event EventHandler? run_completed;

    [RelayCommand]
    public async Task LoadCollections(CancellationToken cancellation_token)
    {
        var collections = await _collection_repository.list_all_async(cancellation_token);
        Collections.Clear();
        foreach (var col in collections)
        {
            Collections.Add(col);
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartRun))]
    public async Task StartRun()
    {
        if (SelectedCollection == null) return;

        IsRunning = true;
        RunResults.Clear();
        CompletedRequests = 0;
        PassedRequests = 0;
        FailedRequests = 0;
        Progress = 0;
        StatusMessage = "Starting collection run...";

        _cancellation_source = new CancellationTokenSource();
        var token = _cancellation_source.Token;

        try
        {
            var requests = GetAllRequests(SelectedCollection.items);
            TotalRequests = requests.Count * Iterations;

            for (int iteration = 0; iteration < Iterations && !token.IsCancellationRequested; iteration++)
            {
                if (Iterations > 1)
                {
                    StatusMessage = $"Iteration {iteration + 1} of {Iterations}";
                }

                foreach (var request in requests)
                {
                    if (token.IsCancellationRequested) break;

                    var runItem = new collection_run_item_view_model
                    {
                        RequestName = request.name,
                        Method = request.method,
                        Url = request.url,
                        Status = "Running..."
                    };
                    RunResults.Add(runItem);

                    try
                    {
                        var result = await _orchestrator.execute_request_async(request, token);
                        
                        runItem.StatusCode = result.response?.status_code;
                        runItem.ElapsedMs = result.response?.elapsed_ms ?? 0;
                        runItem.TestsPassed = result.post_script_result?.test_results?.Count(t => t.passed) ?? 0;
                        runItem.TestsFailed = result.post_script_result?.test_results?.Count(t => !t.passed) ?? 0;
                        
                        bool hasFailedTests = runItem.TestsFailed > 0;
                        bool hasError = result.response?.status_code >= 400 || !string.IsNullOrEmpty(result.response?.error_message);
                        
                        if (hasError || hasFailedTests)
                        {
                            runItem.Status = "Failed";
                            runItem.ErrorMessage = result.response?.error_message;
                            FailedRequests++;
                        }
                        else
                        {
                            runItem.Status = "Passed";
                            PassedRequests++;
                        }
                    }
                    catch (Exception ex)
                    {
                        runItem.Status = "Error";
                        runItem.ErrorMessage = ex.Message;
                        FailedRequests++;
                    }

                    CompletedRequests++;
                    Progress = (double)CompletedRequests / TotalRequests * 100;

                    if (DelayBetweenRequests > 0 && !token.IsCancellationRequested)
                    {
                        await Task.Delay(DelayBetweenRequests, token);
                    }
                }
            }

            StatusMessage = token.IsCancellationRequested 
                ? "Run cancelled" 
                : $"Run completed: {PassedRequests} passed, {FailedRequests} failed";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Run cancelled";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Run failed: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
            _cancellation_source?.Dispose();
            _cancellation_source = null;
            run_completed?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    public void StopRun()
    {
        _cancellation_source?.Cancel();
        StatusMessage = "Stopping...";
    }

    private bool CanStartRun() => !IsRunning && SelectedCollection != null;

    partial void OnIsRunningChanged(bool value) => StartRunCommand.NotifyCanExecuteChanged();
    partial void OnSelectedCollectionChanged(postman_collection_model? value) => StartRunCommand.NotifyCanExecuteChanged();

    private List<http_request_model> GetAllRequests(IEnumerable<collection_item_model> items)
    {
        var requests = new List<http_request_model>();
        
        foreach (var item in items)
        {
            if (!item.is_folder && item.request != null)
            {
                requests.Add(item.request);
            }
            
            if (item.children?.Any() == true)
            {
                requests.AddRange(GetAllRequests(item.children));
            }
        }
        
        return requests;
    }
}

/// <summary>
/// Represents a single request execution result in the collection runner.
/// </summary>
public partial class collection_run_item_view_model : ObservableObject
{
    [ObservableProperty]
    private string _requestName = string.Empty;

    [ObservableProperty]
    private http_method _method;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    [ObservableProperty]
    private int? _statusCode;

    [ObservableProperty]
    private long _elapsedMs;

    [ObservableProperty]
    private int _testsPassed;

    [ObservableProperty]
    private int _testsFailed;

    [ObservableProperty]
    private string? _errorMessage;
}
