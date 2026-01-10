using Core.Models;

namespace Core.Interfaces;

public interface i_theme_service
{
    /// <summary>
    /// Applies theme colors from the given settings.
    /// </summary>
    void ApplyTheme(app_settings_model settings);

    /// <summary>
    /// Applies theme colors from the current settings.
    /// </summary>
    void ApplyCurrentTheme();

    /// <summary>
    /// Sets whether dark mode is enabled.
    /// </summary>
    void SetDarkMode(bool isDark);
}
