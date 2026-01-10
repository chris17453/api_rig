using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace App.ViewModels;

public partial class settings_view_model : ObservableObject
{
    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private int _requestTimeoutSeconds = 30;

    [ObservableProperty]
    private bool _followRedirects = true;

    [ObservableProperty]
    private bool _validateSslCertificates = true;

    [ObservableProperty]
    private string _proxyUrl = string.Empty;

    [ObservableProperty]
    private bool _useProxy;

    [ObservableProperty]
    private bool _saveHistory = true;

    [ObservableProperty]
    private int _maxHistoryEntries = 100;

    [ObservableProperty]
    private bool _autoSaveRequests = true;

    [ObservableProperty]
    private string _defaultUserAgent = "API Rig/1.0";

    public event EventHandler<bool>? theme_changed;
    public event EventHandler? settings_saved;

    partial void OnIsDarkThemeChanged(bool value)
    {
        theme_changed?.Invoke(this, value);
    }

    [RelayCommand]
    private void Save()
    {
        // TODO: Persist settings to storage
        settings_saved?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        IsDarkTheme = true;
        RequestTimeoutSeconds = 30;
        FollowRedirects = true;
        ValidateSslCertificates = true;
        ProxyUrl = string.Empty;
        UseProxy = false;
        SaveHistory = true;
        MaxHistoryEntries = 100;
        AutoSaveRequests = true;
        DefaultUserAgent = "API Rig/1.0";
    }
}
