using System.Text.Json;
using Core.Interfaces;
using Core.Models;

namespace Data.Services;

public class settings_service : i_settings_service
{
    private readonly string _settingsPath;
    private app_settings_model _settings = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public app_settings_model Settings => _settings;

    public event EventHandler<app_settings_model>? settings_changed;

    public settings_service(string? settingsPath = null)
    {
        _settingsPath = settingsPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ApiRig",
            "settings.json");

        // Ensure directory exists
        var dir = Path.GetDirectoryName(_settingsPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    public async Task LoadAsync()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                var loaded = JsonSerializer.Deserialize<app_settings_model>(json, _jsonOptions);
                if (loaded != null)
                {
                    _settings = loaded;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SETTINGS] Failed to load settings: {ex.Message}");
            // Use defaults
            _settings = new app_settings_model();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(_settings, _jsonOptions);
            await File.WriteAllTextAsync(_settingsPath, json);
            settings_changed?.Invoke(this, _settings);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SETTINGS] Failed to save settings: {ex.Message}");
            throw;
        }
    }

    public void ResetToDefaults()
    {
        _settings = new app_settings_model();
        settings_changed?.Invoke(this, _settings);
    }
}
