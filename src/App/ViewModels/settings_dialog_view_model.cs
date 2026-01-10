using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Media;
using Core.Interfaces;
using Core.Models;

namespace App.ViewModels;

public partial class settings_dialog_view_model : ObservableObject
{
    private readonly i_settings_service? _settingsService;

    [ObservableProperty]
    private string _selectedSection = "General";

    // General Settings
    [ObservableProperty]
    private string _databasePath = "api_rig.db";

    [ObservableProperty]
    private string _workspacePath = string.Empty;

    [ObservableProperty]
    private bool _autoSaveEnabled = true;

    [ObservableProperty]
    private int _autoSaveIntervalSeconds = 30;

    [ObservableProperty]
    private int _historyMaxEntries = 100;

    [ObservableProperty]
    private bool _confirmOnClose = true;

    [ObservableProperty]
    private bool _showConsoleOnStartup;

    // Theme Settings
    [ObservableProperty]
    private ObservableCollection<theme_preset> _availableThemes = new();

    [ObservableProperty]
    private theme_preset? _selectedTheme;

    [ObservableProperty]
    private bool _isDarkMode = true;

    // Current theme colors (editable)
    [ObservableProperty]
    private string _accentColor = "#3B82F6";

    [ObservableProperty]
    private string _accentHoverColor = "#2563EB";

    [ObservableProperty]
    private string _successColor = "#22C55E";

    [ObservableProperty]
    private string _warningColor = "#F59E0B";

    [ObservableProperty]
    private string _errorColor = "#EF4444";

    [ObservableProperty]
    private string _appBackgroundColor = "#0F172A";

    [ObservableProperty]
    private string _panelBackgroundColor = "#1E293B";

    [ObservableProperty]
    private string _cardBackgroundColor = "#334155";

    [ObservableProperty]
    private string _inputBackgroundColor = "#1E293B";

    [ObservableProperty]
    private string _textPrimaryColor = "#F8FAFC";

    [ObservableProperty]
    private string _textSecondaryColor = "#94A3B8";

    [ObservableProperty]
    private string _borderColor = "#475569";

    // Request Settings
    [ObservableProperty]
    private int _requestTimeoutSeconds = 30;

    [ObservableProperty]
    private bool _followRedirects = true;

    [ObservableProperty]
    private int _maxRedirects = 10;

    [ObservableProperty]
    private bool _validateSslCertificates = true;

    // Script Settings
    [ObservableProperty]
    private int _scriptTimeoutMs = 5000;

    [ObservableProperty]
    private bool _enableScriptConsoleLog = true;

    // Proxy Settings
    [ObservableProperty]
    private bool _useProxy;

    [ObservableProperty]
    private string _proxyHost = string.Empty;

    [ObservableProperty]
    private int _proxyPort = 8080;

    [ObservableProperty]
    private string _proxyUsername = string.Empty;

    [ObservableProperty]
    private string _proxyPassword = string.Empty;

    [ObservableProperty]
    private bool _proxyBypassLocal = true;

    // Editor Settings
    [ObservableProperty]
    private string _fontFamily = "Consolas";

    [ObservableProperty]
    private int _fontSize = 13;

    [ObservableProperty]
    private bool _wordWrap = true;

    [ObservableProperty]
    private bool _showLineNumbers = true;

    [ObservableProperty]
    private int _tabSize = 2;

    public IReadOnlyList<string> AvailableSections { get; } = new[]
    {
        "General",
        "Appearance",
        "Requests",
        "Scripts",
        "Proxy",
        "Editor"
    };

    public IReadOnlyList<string> AvailableFonts { get; } = new[]
    {
        "Consolas",
        "Cascadia Code",
        "Fira Code",
        "JetBrains Mono",
        "Source Code Pro",
        "Ubuntu Mono",
        "Roboto Mono"
    };

    public event EventHandler? settings_saved;
    public event EventHandler? dialog_closed;

    public settings_dialog_view_model() : this(App.Settings)
    {
    }

    public settings_dialog_view_model(i_settings_service? settingsService)
    {
        _settingsService = settingsService;
        InitializeThemes();
        LoadSettings();
    }

    private void InitializeThemes()
    {
        AvailableThemes.Add(new theme_preset
        {
            Name = "Dark (Default)",
            IsDark = true,
            AccentColor = "#3B82F6",
            AppBackground = "#0F172A",
            PanelBackground = "#1E293B",
            CardBackground = "#334155",
            TextPrimary = "#F8FAFC",
            TextSecondary = "#94A3B8"
        });

        AvailableThemes.Add(new theme_preset
        {
            Name = "Light",
            IsDark = false,
            AccentColor = "#3B82F6",
            AppBackground = "#F8FAFC",
            PanelBackground = "#FFFFFF",
            CardBackground = "#F1F5F9",
            TextPrimary = "#1E293B",
            TextSecondary = "#64748B"
        });

        AvailableThemes.Add(new theme_preset
        {
            Name = "Midnight Blue",
            IsDark = true,
            AccentColor = "#6366F1",
            AppBackground = "#0F0F23",
            PanelBackground = "#1A1A2E",
            CardBackground = "#25253D",
            TextPrimary = "#E2E8F0",
            TextSecondary = "#A0AEC0"
        });

        AvailableThemes.Add(new theme_preset
        {
            Name = "Forest Green",
            IsDark = true,
            AccentColor = "#10B981",
            AppBackground = "#0D1F17",
            PanelBackground = "#132A1F",
            CardBackground = "#1A3D2B",
            TextPrimary = "#E2E8F0",
            TextSecondary = "#A0AEC0"
        });

        AvailableThemes.Add(new theme_preset
        {
            Name = "Sunset Orange",
            IsDark = true,
            AccentColor = "#F97316",
            AppBackground = "#1C1410",
            PanelBackground = "#2D1F1A",
            CardBackground = "#3D2A22",
            TextPrimary = "#FEF3C7",
            TextSecondary = "#D6BCAB"
        });

        SelectedTheme = AvailableThemes.First();
    }

    private void LoadSettings()
    {
        if (_settingsService == null) return;

        var s = _settingsService.Settings;

        // General
        DatabasePath = s.database_path;
        WorkspacePath = s.workspace_path;
        AutoSaveEnabled = s.auto_save_enabled;
        AutoSaveIntervalSeconds = s.auto_save_interval_seconds;
        HistoryMaxEntries = s.history_max_entries;
        ConfirmOnClose = s.confirm_on_close;
        ShowConsoleOnStartup = s.show_console_on_startup;

        // Appearance
        IsDarkMode = s.is_dark_mode;
        AccentColor = s.theme_colors.accent;
        AccentHoverColor = s.theme_colors.accent_hover;
        SuccessColor = s.theme_colors.success;
        WarningColor = s.theme_colors.warning;
        ErrorColor = s.theme_colors.error;
        AppBackgroundColor = s.theme_colors.app_background;
        PanelBackgroundColor = s.theme_colors.panel_background;
        CardBackgroundColor = s.theme_colors.card_background;
        InputBackgroundColor = s.theme_colors.input_background;
        TextPrimaryColor = s.theme_colors.text_primary;
        TextSecondaryColor = s.theme_colors.text_secondary;
        BorderColor = s.theme_colors.border;

        // Requests
        RequestTimeoutSeconds = s.request_timeout_seconds;
        FollowRedirects = s.follow_redirects;
        MaxRedirects = s.max_redirects;
        ValidateSslCertificates = s.validate_ssl_certificates;

        // Scripts
        ScriptTimeoutMs = s.script_timeout_ms;
        EnableScriptConsoleLog = s.enable_script_console_log;

        // Proxy
        UseProxy = s.use_proxy;
        ProxyHost = s.proxy_host;
        ProxyPort = s.proxy_port;
        ProxyUsername = s.proxy_username;
        ProxyPassword = s.proxy_password;
        ProxyBypassLocal = s.proxy_bypass_local;

        // Editor
        FontFamily = s.font_family;
        FontSize = s.font_size;
        WordWrap = s.word_wrap;
        ShowLineNumbers = s.show_line_numbers;
        TabSize = s.tab_size;

        // Select theme
        SelectedTheme = AvailableThemes.FirstOrDefault(t => t.Name == s.selected_theme)
                       ?? AvailableThemes.First();

        // Load custom themes
        foreach (var ct in s.custom_themes)
        {
            AvailableThemes.Add(new theme_preset
            {
                Name = ct.name,
                IsDark = ct.is_dark,
                IsCustom = true,
                AccentColor = ct.colors.accent,
                AppBackground = ct.colors.app_background,
                PanelBackground = ct.colors.panel_background,
                CardBackground = ct.colors.card_background,
                TextPrimary = ct.colors.text_primary,
                TextSecondary = ct.colors.text_secondary
            });
        }
    }

    partial void OnSelectedThemeChanged(theme_preset? value)
    {
        if (value != null)
        {
            ApplyThemePreset(value);
        }
    }

    private void ApplyThemePreset(theme_preset preset)
    {
        IsDarkMode = preset.IsDark;
        AccentColor = preset.AccentColor;
        AppBackgroundColor = preset.AppBackground;
        PanelBackgroundColor = preset.PanelBackground;
        CardBackgroundColor = preset.CardBackground;
        TextPrimaryColor = preset.TextPrimary;
        TextSecondaryColor = preset.TextSecondary;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (_settingsService == null) return;

        var s = _settingsService.Settings;

        // General
        s.database_path = DatabasePath;
        s.workspace_path = WorkspacePath;
        s.auto_save_enabled = AutoSaveEnabled;
        s.auto_save_interval_seconds = AutoSaveIntervalSeconds;
        s.history_max_entries = HistoryMaxEntries;
        s.confirm_on_close = ConfirmOnClose;
        s.show_console_on_startup = ShowConsoleOnStartup;

        // Appearance
        s.selected_theme = SelectedTheme?.Name ?? "Dark (Default)";
        s.is_dark_mode = IsDarkMode;
        s.theme_colors.accent = AccentColor;
        s.theme_colors.accent_hover = AccentHoverColor;
        s.theme_colors.success = SuccessColor;
        s.theme_colors.warning = WarningColor;
        s.theme_colors.error = ErrorColor;
        s.theme_colors.app_background = AppBackgroundColor;
        s.theme_colors.panel_background = PanelBackgroundColor;
        s.theme_colors.card_background = CardBackgroundColor;
        s.theme_colors.input_background = InputBackgroundColor;
        s.theme_colors.text_primary = TextPrimaryColor;
        s.theme_colors.text_secondary = TextSecondaryColor;
        s.theme_colors.border = BorderColor;

        // Requests
        s.request_timeout_seconds = RequestTimeoutSeconds;
        s.follow_redirects = FollowRedirects;
        s.max_redirects = MaxRedirects;
        s.validate_ssl_certificates = ValidateSslCertificates;

        // Scripts
        s.script_timeout_ms = ScriptTimeoutMs;
        s.enable_script_console_log = EnableScriptConsoleLog;

        // Proxy
        s.use_proxy = UseProxy;
        s.proxy_host = ProxyHost;
        s.proxy_port = ProxyPort;
        s.proxy_username = ProxyUsername;
        s.proxy_password = ProxyPassword;
        s.proxy_bypass_local = ProxyBypassLocal;

        // Editor
        s.font_family = FontFamily;
        s.font_size = FontSize;
        s.word_wrap = WordWrap;
        s.show_line_numbers = ShowLineNumbers;
        s.tab_size = TabSize;

        // Custom themes
        s.custom_themes.Clear();
        foreach (var theme in AvailableThemes.Where(t => t.IsCustom))
        {
            s.custom_themes.Add(new custom_theme_model
            {
                name = theme.Name,
                is_dark = theme.IsDark,
                colors = new theme_colors_model
                {
                    accent = theme.AccentColor,
                    app_background = theme.AppBackground,
                    panel_background = theme.PanelBackground,
                    card_background = theme.CardBackground,
                    text_primary = theme.TextPrimary,
                    text_secondary = theme.TextSecondary
                }
            });
        }

        await _settingsService.SaveAsync();
        settings_saved?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Cancel()
    {
        dialog_closed?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void ResetToDefaults()
    {
        // Reset all settings to defaults
        DatabasePath = "api_rig.db";
        AutoSaveEnabled = true;
        AutoSaveIntervalSeconds = 30;
        HistoryMaxEntries = 100;
        ConfirmOnClose = true;
        ShowConsoleOnStartup = false;

        RequestTimeoutSeconds = 30;
        FollowRedirects = true;
        MaxRedirects = 10;
        ValidateSslCertificates = true;

        ScriptTimeoutMs = 5000;
        EnableScriptConsoleLog = true;

        UseProxy = false;
        ProxyHost = string.Empty;
        ProxyPort = 8080;

        FontFamily = "Consolas";
        FontSize = 13;
        WordWrap = true;
        ShowLineNumbers = true;
        TabSize = 2;

        SelectedTheme = AvailableThemes.First();
    }

    [RelayCommand]
    private void CreateNewTheme()
    {
        var newTheme = new theme_preset
        {
            Name = $"Custom Theme {AvailableThemes.Count + 1}",
            IsDark = IsDarkMode,
            AccentColor = AccentColor,
            AppBackground = AppBackgroundColor,
            PanelBackground = PanelBackgroundColor,
            CardBackground = CardBackgroundColor,
            TextPrimary = TextPrimaryColor,
            TextSecondary = TextSecondaryColor,
            IsCustom = true
        };

        AvailableThemes.Add(newTheme);
        SelectedTheme = newTheme;
    }

    [RelayCommand]
    private void DeleteSelectedTheme()
    {
        if (SelectedTheme?.IsCustom == true)
        {
            var index = AvailableThemes.IndexOf(SelectedTheme);
            AvailableThemes.Remove(SelectedTheme);
            SelectedTheme = AvailableThemes.FirstOrDefault();
        }
    }

    [RelayCommand]
    private void BrowseDatabasePath()
    {
        // TODO: Open file dialog
    }

    [RelayCommand]
    private void BrowseWorkspacePath()
    {
        // TODO: Open folder dialog
    }
}

public partial class theme_preset : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    public bool IsDark { get; set; }
    public bool IsCustom { get; set; }

    public string AccentColor { get; set; } = "#3B82F6";
    public string AppBackground { get; set; } = "#0F172A";
    public string PanelBackground { get; set; } = "#1E293B";
    public string CardBackground { get; set; } = "#334155";
    public string TextPrimary { get; set; } = "#F8FAFC";
    public string TextSecondary { get; set; } = "#94A3B8";

    public override string ToString() => Name;
}
