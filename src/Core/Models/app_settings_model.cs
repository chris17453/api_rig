namespace Core.Models;

public class app_settings_model
{
    // General
    public string database_path { get; set; } = "api_rig.db";
    public string workspace_path { get; set; } = string.Empty;
    public bool auto_save_enabled { get; set; } = true;
    public int auto_save_interval_seconds { get; set; } = 30;
    public int history_max_entries { get; set; } = 100;
    public bool confirm_on_close { get; set; } = true;
    public bool show_console_on_startup { get; set; } = false;

    // Appearance
    public string selected_theme { get; set; } = "Dark (Default)";
    public bool is_dark_mode { get; set; } = true;
    public theme_colors_model theme_colors { get; set; } = new();

    // Requests
    public int request_timeout_seconds { get; set; } = 30;
    public bool follow_redirects { get; set; } = true;
    public int max_redirects { get; set; } = 10;
    public bool validate_ssl_certificates { get; set; } = true;

    // Scripts
    public int script_timeout_ms { get; set; } = 5000;
    public bool enable_script_console_log { get; set; } = true;

    // Proxy
    public bool use_proxy { get; set; } = false;
    public string proxy_host { get; set; } = string.Empty;
    public int proxy_port { get; set; } = 8080;
    public string proxy_username { get; set; } = string.Empty;
    public string proxy_password { get; set; } = string.Empty;
    public bool proxy_bypass_local { get; set; } = true;

    // Editor
    public string font_family { get; set; } = "Consolas";
    public int font_size { get; set; } = 13;
    public bool word_wrap { get; set; } = true;
    public bool show_line_numbers { get; set; } = true;
    public int tab_size { get; set; } = 2;

    // Custom themes
    public List<custom_theme_model> custom_themes { get; set; } = new();
}

public class theme_colors_model
{
    public string accent { get; set; } = "#3B82F6";
    public string accent_hover { get; set; } = "#2563EB";
    public string success { get; set; } = "#22C55E";
    public string warning { get; set; } = "#F59E0B";
    public string error { get; set; } = "#EF4444";
    public string app_background { get; set; } = "#0F172A";
    public string panel_background { get; set; } = "#1E293B";
    public string card_background { get; set; } = "#334155";
    public string input_background { get; set; } = "#1E293B";
    public string text_primary { get; set; } = "#F8FAFC";
    public string text_secondary { get; set; } = "#94A3B8";
    public string border { get; set; } = "#475569";
}

public class custom_theme_model
{
    public string name { get; set; } = string.Empty;
    public bool is_dark { get; set; } = true;
    public theme_colors_model colors { get; set; } = new();
}
