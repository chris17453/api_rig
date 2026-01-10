using Avalonia.Controls;
using Avalonia.Interactivity;
using App.ViewModels;

namespace App.Views;

public partial class main_window : Window
{
    public main_window()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        DataContextChanged += OnDataContextChanged;
        Closing += OnWindowClosing;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is main_view_model mainVm)
        {
            // Wire up events from the new layout components
            mainVm.import_requested += async (_, _) => await ShowImportDialog();
            mainVm.export_requested += async (_, _) => await ShowExportDialog();
            mainVm.about_requested += async (_, _) => await ShowAboutDialog();
            mainVm.settings_requested += async (_, _) => await ShowSettingsDialog();

            // Wire up tab close confirmation
            mainVm.Tabs.close_confirmation_requested += OnCloseConfirmationRequested;
        }
    }

    private async void OnCloseConfirmationRequested(object? sender, CloseConfirmationEventArgs e)
    {
        var result = await confirm_dialog.ShowDiscardConfirmation(this, e.Tab.Title);
        e.Confirmation.TrySetResult(result == ConfirmResult.Discard);
    }

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is not main_view_model mainVm) return;

        // Check if there are unsaved changes
        if (mainVm.Tabs.has_any_unsaved_changes())
        {
            e.Cancel = true; // Cancel closing for now

            var unsavedCount = mainVm.Tabs.get_unsaved_tabs().Count();
            var result = await confirm_dialog.ShowCloseAppConfirmation(this, unsavedCount);

            if (result == ConfirmResult.Discard)
            {
                // User confirmed, close without saving
                mainVm.Tabs.close_confirmation_requested -= OnCloseConfirmationRequested;
                Close();
            }
            // If Cancel, do nothing (window stays open)
        }
    }

    private async void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (DataContext is main_view_model mainVm)
        {
            await mainVm.initialize_async(CancellationToken.None);
        }
    }

    private async Task ShowImportDialog()
    {
        var mainVm = DataContext as main_view_model;
        if (mainVm == null) return;

        var importVm = mainVm.CreateImportExportViewModel();
        var dialog = new import_dialog { DataContext = importVm };

        importVm.collection_imported += async (s, collection) =>
        {
            dialog.Close();
            await mainVm.Sidebar.load_data_async(CancellationToken.None);
        };

        await dialog.ShowDialog(this);
    }

    private async Task ShowExportDialog()
    {
        var mainVm = DataContext as main_view_model;
        if (mainVm == null) return;

        var selectedCollection = mainVm.Sidebar.SelectedCollection;
        if (selectedCollection == null)
        {
            // Show message to select a collection first
            return;
        }

        var exportVm = mainVm.CreateImportExportViewModel();
        exportVm.SelectedCollectionForExport = selectedCollection;
        var dialog = new export_dialog { DataContext = exportVm };

        exportVm.export_completed += (s, args) =>
        {
            dialog.Close();
        };

        await dialog.ShowDialog(this);
    }

    private async Task ShowAboutDialog()
    {
        var aboutVm = new about_view_model();
        var dialog = new about_dialog { DataContext = aboutVm };
        await dialog.ShowDialog(this);
    }

    private async Task ShowSettingsDialog()
    {
        var settingsVm = new settings_dialog_view_model();
        var dialog = new settings_dialog { DataContext = settingsVm };
        var result = await dialog.ShowDialog<bool?>(this);

        if (result == true)
        {
            // Settings were saved - apply theme changes if needed
            // TODO: Apply settings to the application
        }
    }
}
