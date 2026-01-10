using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace App.ViewModels;

public partial class bottom_bar_view_model : ObservableObject
{
    [ObservableProperty]
    private bool _isTerminalVisible;

    [ObservableProperty]
    private bool _isConsoleVisible;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    public RotateTransform TerminalToggleRotation => IsTerminalVisible
        ? new RotateTransform(90)
        : new RotateTransform(0);

    public event EventHandler<bool>? terminal_visibility_changed;
    public event EventHandler<bool>? console_visibility_changed;

    partial void OnIsTerminalVisibleChanged(bool value)
    {
        OnPropertyChanged(nameof(TerminalToggleRotation));
        terminal_visibility_changed?.Invoke(this, value);
    }

    partial void OnIsConsoleVisibleChanged(bool value)
    {
        console_visibility_changed?.Invoke(this, value);
    }

    [RelayCommand]
    private void ToggleTerminal()
    {
        IsTerminalVisible = !IsTerminalVisible;
    }

    [RelayCommand]
    private void ToggleConsole()
    {
        IsConsoleVisible = !IsConsoleVisible;
    }

    public void SetStatus(string message)
    {
        StatusMessage = message;
    }
}
