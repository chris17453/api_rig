using Avalonia.Controls;
using Avalonia.Interactivity;

namespace App.Views;

public enum ConfirmResult
{
    Cancel,
    Discard,
    Save
}

public partial class confirm_dialog : Window
{
    public confirm_dialog()
    {
        InitializeComponent();
    }

    public string DialogTitle
    {
        get => TitleText.Text ?? "Confirm";
        set => TitleText.Text = value;
    }

    public string Message
    {
        get => MessageText.Text ?? "";
        set => MessageText.Text = value;
    }

    public bool ShowSaveButton
    {
        get => SaveButton.IsVisible;
        set => SaveButton.IsVisible = value;
    }

    public string DiscardButtonText
    {
        get => DiscardButton.Content?.ToString() ?? "Discard";
        set => DiscardButton.Content = value;
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(ConfirmResult.Cancel);
    }

    private void OnDiscardClick(object? sender, RoutedEventArgs e)
    {
        Close(ConfirmResult.Discard);
    }

    private void OnSaveClick(object? sender, RoutedEventArgs e)
    {
        Close(ConfirmResult.Save);
    }

    /// <summary>
    /// Shows a simple discard confirmation dialog.
    /// </summary>
    public static async Task<ConfirmResult> ShowDiscardConfirmation(Window owner, string itemName)
    {
        var dialog = new confirm_dialog
        {
            DialogTitle = "Unsaved Changes",
            Message = $"'{itemName}' has unsaved changes. Do you want to discard them?",
            ShowSaveButton = false
        };
        return await dialog.ShowDialog<ConfirmResult>(owner);
    }

    /// <summary>
    /// Shows a save/discard/cancel confirmation dialog.
    /// </summary>
    public static async Task<ConfirmResult> ShowSaveConfirmation(Window owner, string itemName)
    {
        var dialog = new confirm_dialog
        {
            DialogTitle = "Unsaved Changes",
            Message = $"'{itemName}' has unsaved changes. What would you like to do?",
            ShowSaveButton = true
        };
        return await dialog.ShowDialog<ConfirmResult>(owner);
    }

    /// <summary>
    /// Shows a confirmation for closing app with multiple unsaved items.
    /// </summary>
    public static async Task<ConfirmResult> ShowCloseAppConfirmation(Window owner, int unsavedCount)
    {
        var dialog = new confirm_dialog
        {
            DialogTitle = "Unsaved Changes",
            Message = $"You have {unsavedCount} tab(s) with unsaved changes. Do you want to discard all changes and close?",
            ShowSaveButton = false,
            DiscardButtonText = "Close Anyway"
        };
        return await dialog.ShowDialog<ConfirmResult>(owner);
    }
}
