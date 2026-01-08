using CommunityToolkit.Mvvm.ComponentModel;

namespace PostmanClone.App.ViewModels;

/// <summary>
/// ViewModel for a single test result.
/// </summary>
public partial class test_result_view_model : ObservableObject
{
    [ObservableProperty]
    private string _testName = string.Empty;

    [ObservableProperty]
    private bool _passed;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private TimeSpan _duration = TimeSpan.Zero;

    public string StatusIcon => Passed ? "✓" : "✗";
    public string StatusColor => Passed ? "#4CAF50" : "#F44336";
}
