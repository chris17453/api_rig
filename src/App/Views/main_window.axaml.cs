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

            // Wire up vault dialogs
            mainVm.SidePanel.VaultPanel.vault_setup_requested += OnVaultSetupRequested;
            mainVm.SidePanel.VaultPanel.vault_unlock_requested += OnVaultUnlockRequested;
        }
    }

    private async void OnCloseConfirmationRequested(object? sender, CloseConfirmationEventArgs e)
    {
        var result = await confirm_dialog.ShowDiscardConfirmation(this, e.Tab.Title);
        e.Confirmation.TrySetResult(result == ConfirmResult.Discard);
    }

    private async void OnVaultSetupRequested(object? sender, TaskCompletionSource<string?> tcs)
    {
        var mainVm = DataContext as main_view_model;
        if (mainVm == null)
        {
            tcs.TrySetResult(null);
            return;
        }

        // Generate the vault key (not saved yet - will be saved if user confirms)
        var vaultKey = mainVm.SidePanel.VaultPanel.VaultStore.generate_new_vault_key();

        var dialog = new vault_setup_dialog();
        dialog.SetVaultKey(vaultKey);

        var result = await dialog.ShowDialog<bool?>(this);

        if (result == true && dialog.WasConfirmed)
        {
            tcs.TrySetResult(vaultKey);
        }
        else
        {
            tcs.TrySetResult(null);
        }
    }

    private async void OnVaultUnlockRequested(object? sender, TaskCompletionSource<string?> tcs)
    {
        var mainVm = DataContext as main_view_model;
        if (mainVm == null)
        {
            tcs.TrySetResult(null);
            return;
        }

        var vaultStore = mainVm.SidePanel.VaultPanel.VaultStore;
        var dialog = new vault_unlock_dialog(key =>
        {
            // Synchronously verify the key
            var task = vaultStore.try_unlock_async(key);
            task.Wait();
            var unlocked = task.Result;
            // If successful, lock it again - we just want to verify
            if (unlocked)
            {
                vaultStore.lock_vault();
            }
            return unlocked;
        });

        var result = await dialog.ShowDialog<bool?>(this);

        if (result == true && dialog.WasConfirmed && !string.IsNullOrEmpty(dialog.EnteredKey))
        {
            tcs.TrySetResult(dialog.EnteredKey);
        }
        else
        {
            tcs.TrySetResult(null);
        }
    }

    private bool _forceClose = false;

    private async void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_forceClose) return;
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
                _forceClose = true;
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

        var exportVm = mainVm.CreateImportExportViewModel();
        await exportVm.LoadCollectionsForExportAsync();

        // Pre-select the currently selected collection if there is one
        var selectedCollection = mainVm.Sidebar.SelectedCollection;
        if (selectedCollection != null)
        {
            var item = exportVm.CollectionsForExport.FirstOrDefault(c => c.Collection.id == selectedCollection.id);
            if (item != null)
            {
                item.IsSelected = true;
            }
        }

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
