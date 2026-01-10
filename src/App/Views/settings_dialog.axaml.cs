using Avalonia.Controls;
using Avalonia.Interactivity;
using App.ViewModels;

namespace App.Views;

public partial class settings_dialog : Window
{
    public settings_dialog()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is settings_dialog_view_model vm)
        {
            vm.settings_saved += OnSettingsSaved;
            vm.dialog_closed += OnDialogClosed;
        }
    }

    private void OnSettingsSaved(object? sender, EventArgs e)
    {
        Close(true);
    }

    private void OnDialogClosed(object? sender, EventArgs e)
    {
        Close(false);
    }

    private void OnGeneralClick(object? sender, RoutedEventArgs e)
    {
        ShowSection("General");
    }

    private void OnAppearanceClick(object? sender, RoutedEventArgs e)
    {
        ShowSection("Appearance");
    }

    private void OnRequestsClick(object? sender, RoutedEventArgs e)
    {
        ShowSection("Requests");
    }

    private void OnScriptsClick(object? sender, RoutedEventArgs e)
    {
        ShowSection("Scripts");
    }

    private void OnProxyClick(object? sender, RoutedEventArgs e)
    {
        ShowSection("Proxy");
    }

    private void OnEditorClick(object? sender, RoutedEventArgs e)
    {
        ShowSection("Editor");
    }

    private void ShowSection(string section)
    {
        if (DataContext is settings_dialog_view_model vm)
        {
            vm.SelectedSection = section;
        }

        // Hide all sections
        GeneralSection.IsVisible = false;
        AppearanceSection.IsVisible = false;
        RequestsSection.IsVisible = false;
        ScriptsSection.IsVisible = false;
        ProxySection.IsVisible = false;
        EditorSection.IsVisible = false;

        // Show selected section
        switch (section)
        {
            case "General":
                GeneralSection.IsVisible = true;
                break;
            case "Appearance":
                AppearanceSection.IsVisible = true;
                break;
            case "Requests":
                RequestsSection.IsVisible = true;
                break;
            case "Scripts":
                ScriptsSection.IsVisible = true;
                break;
            case "Proxy":
                ProxySection.IsVisible = true;
                break;
            case "Editor":
                EditorSection.IsVisible = true;
                break;
        }
    }
}
