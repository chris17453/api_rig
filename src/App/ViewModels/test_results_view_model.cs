using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace App.ViewModels;

/// <summary>
/// ViewModel for the test results panel showing pm.test() and pm.expect() assertion results.
/// </summary>
public partial class test_results_view_model : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<test_result_view_model> _testResults = new();

    [ObservableProperty]
    private int _totalTests;

    [ObservableProperty]
    private int _passedTests;

    [ObservableProperty]
    private int _failedTests;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private TimeSpan _totalDuration = TimeSpan.Zero;

    public string Summary => HasResults 
        ? $"{PassedTests}/{TotalTests} tests passed ({FailedTests} failed)"
        : "No test results";

    public string SummaryColor => FailedTests == 0 ? "#4CAF50" : "#F44336";

    public test_results_view_model()
    {
        // Start with empty results - tests will be populated after request execution
    }

    public void AddTestResult(string name, bool passed, string? errorMessage = null)
    {
        var result = new test_result_view_model
        {
            TestName = name,
            Passed = passed,
            ErrorMessage = errorMessage ?? string.Empty,
            Duration = TimeSpan.FromMilliseconds(1)
        };

        TestResults.Add(result);
        UpdateSummary();
    }

    [RelayCommand]
    private void ClearResults()
    {
        TestResults.Clear();
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        TotalTests = TestResults.Count;
        PassedTests = TestResults.Count(r => r.Passed);
        FailedTests = TestResults.Count(r => !r.Passed);
        TotalDuration = TimeSpan.FromMilliseconds(TestResults.Sum(r => r.Duration.TotalMilliseconds));
        HasResults = TotalTests > 0;
        
        OnPropertyChanged(nameof(Summary));
        OnPropertyChanged(nameof(SummaryColor));
    }
}
