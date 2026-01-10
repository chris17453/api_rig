using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace App.Views;

public partial class vault_unlock_dialog : Window
{
    public string? EnteredKey { get; private set; }
    public bool WasConfirmed { get; private set; }

    private readonly Func<string, bool>? _verifyKey;

    public vault_unlock_dialog()
    {
        InitializeComponent();
    }

    public vault_unlock_dialog(Func<string, bool> verifyKey) : this()
    {
        _verifyKey = verifyKey;
    }

    public void ShowError(string message)
    {
        ErrorMessage.Text = message;
        ErrorBorder.IsVisible = true;
    }

    public void ClearError()
    {
        ErrorBorder.IsVisible = false;
    }

    private async void LoadFromFileButton_Click(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Load Vault Key",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Text files") { Patterns = new[] { "*.txt" } },
                new FilePickerFileType("All files") { Patterns = new[] { "*" } }
            }
        });

        if (files.Count > 0)
        {
            await using var stream = await files[0].OpenReadAsync();
            using var reader = new System.IO.StreamReader(stream);
            var key = await reader.ReadToEndAsync();
            VaultKeyInput.Text = key.Trim();
            ClearError();
        }
    }

    private void CancelButton_Click(object? sender, RoutedEventArgs e)
    {
        WasConfirmed = false;
        Close(false);
    }

    private void UnlockButton_Click(object? sender, RoutedEventArgs e)
    {
        ClearError();

        var key = VaultKeyInput.Text?.Trim();

        if (string.IsNullOrEmpty(key))
        {
            ShowError("Please enter your vault key.");
            return;
        }

        // Verify the key if a verification function was provided
        if (_verifyKey != null && !_verifyKey(key))
        {
            ShowError("Invalid vault key. Please check and try again.");
            return;
        }

        EnteredKey = key;
        WasConfirmed = true;
        Close(true);
    }
}
