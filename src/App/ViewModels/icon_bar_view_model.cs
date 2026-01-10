using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace App.ViewModels;

public enum SidePanelType
{
    Collections,
    Environments,
    History,
    Vault
}

public partial class icon_bar_view_model : ObservableObject
{
    [ObservableProperty]
    private SidePanelType _selectedPanel = SidePanelType.Collections;

    [ObservableProperty]
    private bool _isDarkTheme = true;

    public bool IsCollectionsSelected => SelectedPanel == SidePanelType.Collections;
    public bool IsEnvironmentsSelected => SelectedPanel == SidePanelType.Environments;
    public bool IsHistorySelected => SelectedPanel == SidePanelType.History;
    public bool IsVaultSelected => SelectedPanel == SidePanelType.Vault;

    public string ThemeIcon => IsDarkTheme ? "🌙" : "☀️";

    public event EventHandler<SidePanelType>? panel_changed;
    public event EventHandler? theme_toggled;
    public event EventHandler? about_requested;

    partial void OnSelectedPanelChanged(SidePanelType value)
    {
        OnPropertyChanged(nameof(IsCollectionsSelected));
        OnPropertyChanged(nameof(IsEnvironmentsSelected));
        OnPropertyChanged(nameof(IsHistorySelected));
        OnPropertyChanged(nameof(IsVaultSelected));
        panel_changed?.Invoke(this, value);
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        OnPropertyChanged(nameof(ThemeIcon));
    }

    [RelayCommand]
    private void SelectPanel(string panelName)
    {
        SelectedPanel = panelName?.ToLower() switch
        {
            "collections" => SidePanelType.Collections,
            "environments" => SidePanelType.Environments,
            "history" => SidePanelType.History,
            "vault" => SidePanelType.Vault,
            _ => SidePanelType.Collections
        };
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        theme_toggled?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void About()
    {
        about_requested?.Invoke(this, EventArgs.Empty);
    }
}
