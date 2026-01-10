using Core.Models;

namespace Core.Interfaces;

public interface i_settings_service
{
    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    app_settings_model Settings { get; }

    /// <summary>
    /// Loads settings from storage.
    /// </summary>
    Task LoadAsync();

    /// <summary>
    /// Saves current settings to storage.
    /// </summary>
    Task SaveAsync();

    /// <summary>
    /// Resets all settings to defaults.
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Event fired when settings are changed.
    /// </summary>
    event EventHandler<app_settings_model>? settings_changed;
}
