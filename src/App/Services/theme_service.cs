using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Core.Interfaces;
using Core.Models;

namespace App.Services;

public class theme_service : i_theme_service
{
    private readonly i_settings_service _settingsService;

    public theme_service(i_settings_service settingsService)
    {
        _settingsService = settingsService;

        // Subscribe to settings changes
        _settingsService.settings_changed += OnSettingsChanged;
    }

    private void OnSettingsChanged(object? sender, app_settings_model settings)
    {
        ApplyTheme(settings);
    }

    public void ApplyCurrentTheme()
    {
        ApplyTheme(_settingsService.Settings);
    }

    public void ApplyTheme(app_settings_model settings)
    {
        var app = Application.Current;
        if (app == null) return;

        // Set dark/light mode
        SetDarkMode(settings.is_dark_mode);

        // Apply custom colors from settings to resources
        var colors = settings.theme_colors;

        UpdateColorResource(app, "AccentColor", colors.accent);
        UpdateColorResource(app, "AccentHoverColor", colors.accent_hover);
        UpdateColorResource(app, "SuccessTextColor", colors.success);
        UpdateColorResource(app, "WarningTextColor", colors.warning);
        UpdateColorResource(app, "ErrorTextColor", colors.error);
        UpdateColorResource(app, "AppBackgroundColor", colors.app_background);
        UpdateColorResource(app, "PanelBackgroundColor", colors.panel_background);
        UpdateColorResource(app, "CardBackgroundColor", colors.card_background);
        UpdateColorResource(app, "InputBackgroundColor", colors.input_background);
        UpdateColorResource(app, "TextPrimaryColor", colors.text_primary);
        UpdateColorResource(app, "TextSecondaryColor", colors.text_secondary);
        UpdateColorResource(app, "BorderColor", colors.border);

        // Also update SolidColorBrush resources directly for immediate effect
        UpdateBrushResource(app, "AccentBrush", colors.accent);
        UpdateBrushResource(app, "AccentHoverBrush", colors.accent_hover);
        UpdateBrushResource(app, "SuccessTextBrush", colors.success);
        UpdateBrushResource(app, "WarningTextBrush", colors.warning);
        UpdateBrushResource(app, "ErrorTextBrush", colors.error);
        UpdateBrushResource(app, "AppBackgroundBrush", colors.app_background);
        UpdateBrushResource(app, "PanelBackgroundBrush", colors.panel_background);
        UpdateBrushResource(app, "CardBackgroundBrush", colors.card_background);
        UpdateBrushResource(app, "InputBackgroundBrush", colors.input_background);
        UpdateBrushResource(app, "TextPrimaryBrush", colors.text_primary);
        UpdateBrushResource(app, "TextSecondaryBrush", colors.text_secondary);
        UpdateBrushResource(app, "BorderBrush", colors.border);
    }

    public void SetDarkMode(bool isDark)
    {
        var app = Application.Current;
        if (app == null) return;

        app.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private static void UpdateColorResource(Application app, string key, string hexColor)
    {
        try
        {
            if (Color.TryParse(hexColor, out var color))
            {
                app.Resources[key] = color;
            }
        }
        catch
        {
            // Ignore invalid colors
        }
    }

    private static void UpdateBrushResource(Application app, string key, string hexColor)
    {
        try
        {
            if (Color.TryParse(hexColor, out var color))
            {
                app.Resources[key] = new SolidColorBrush(color);
            }
        }
        catch
        {
            // Ignore invalid colors
        }
    }
}
