using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace App.Views;

public partial class vault_setup_dialog : Window
{
    public string? VaultKey { get; private set; }
    public bool WasConfirmed { get; private set; }

    public vault_setup_dialog()
    {
        InitializeComponent();
        ConfirmCheckbox.IsCheckedChanged += ConfirmCheckbox_Changed;
    }

    public void SetVaultKey(string key)
    {
        VaultKey = key;
        VaultKeyTextBox.Text = key;
    }

    private void ConfirmCheckbox_Changed(object? sender, RoutedEventArgs e)
    {
        ContinueButton.IsEnabled = ConfirmCheckbox.IsChecked == true;
    }

    private async void CopyButton_Click(object? sender, RoutedEventArgs e)
    {
        if (VaultKey != null && Clipboard != null)
        {
            await Clipboard.SetTextAsync(VaultKey);
        }
    }

    private async void SaveToFileButton_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(VaultKey)) return;

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Vault Key",
            SuggestedFileName = "api-rig-vault-key.txt",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } }
            }
        });

        if (file != null)
        {
            await using var stream = await file.OpenWriteAsync();
            await using var writer = new System.IO.StreamWriter(stream);
            await writer.WriteAsync(VaultKey);

            // Auto-check the confirmation since they saved it
            ConfirmCheckbox.IsChecked = true;
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        WasConfirmed = false;
        Close(false);
    }

    private void ContinueButton_Click(object? sender, RoutedEventArgs e)
    {
        WasConfirmed = true;
        Close(true);
    }
}
