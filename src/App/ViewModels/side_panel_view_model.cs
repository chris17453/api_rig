using CommunityToolkit.Mvvm.ComponentModel;

namespace App.ViewModels;

public partial class side_panel_view_model : ObservableObject
{
    [ObservableProperty]
    private SidePanelType _currentPanelType = SidePanelType.Collections;

    [ObservableProperty]
    private sidebar_view_model? _collectionsPanel;

    [ObservableProperty]
    private environments_panel_view_model? _environmentsPanel;

    [ObservableProperty]
    private history_panel_view_model? _historyPanel;

    [ObservableProperty]
    private vault_panel_view_model? _vaultPanel;

    public bool IsCollectionsVisible => CurrentPanelType == SidePanelType.Collections;
    public bool IsEnvironmentsVisible => CurrentPanelType == SidePanelType.Environments;
    public bool IsHistoryVisible => CurrentPanelType == SidePanelType.History;
    public bool IsVaultVisible => CurrentPanelType == SidePanelType.Vault;

    public string PanelTitle => CurrentPanelType switch
    {
        SidePanelType.Collections => "Collections",
        SidePanelType.Environments => "Environments",
        SidePanelType.History => "History",
        SidePanelType.Vault => "Vault",
        _ => "Collections"
    };

    partial void OnCurrentPanelTypeChanged(SidePanelType value)
    {
        OnPropertyChanged(nameof(IsCollectionsVisible));
        OnPropertyChanged(nameof(IsEnvironmentsVisible));
        OnPropertyChanged(nameof(IsHistoryVisible));
        OnPropertyChanged(nameof(IsVaultVisible));
        OnPropertyChanged(nameof(PanelTitle));

        // Load history when switching to History panel
        if (value == SidePanelType.History && HistoryPanel != null)
        {
            _ = HistoryPanel.LoadHistoryAsync();
        }
    }

    public void SwitchPanel(SidePanelType panelType)
    {
        CurrentPanelType = panelType;
    }
}
