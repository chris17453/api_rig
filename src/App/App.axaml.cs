using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using App.Services;
using App.ViewModels;
using App.Views;
using Core.Interfaces;
using Core.Models;
using Data.Context;
using Data.Repositories;
using Data.Services;
using Data.Stores;
using Http.Services;
using Scripting;
using System;
using System.Net;
using System.Net.Http;

namespace App;

public partial class App : Application
{
    public static IServiceProvider? services { get; private set; }
    public static i_settings_service? Settings { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        // Load settings first (synchronously for startup)
        var settingsService = new settings_service();
        await settingsService.LoadAsync();
        Settings = settingsService;

        var service_collection = new ServiceCollection();
        configure_services(service_collection, settingsService);
        services = service_collection.BuildServiceProvider();

        // Initialize DB
        var db = services.GetRequiredService<postman_clone_db_context>();
        db.Database.EnsureCreated();

        // Set theme based on settings
        RequestedThemeVariant = settingsService.Settings.is_dark_mode
            ? ThemeVariant.Dark
            : ThemeVariant.Light;

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new main_window
            {
                DataContext = services.GetRequiredService<main_view_model>()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void configure_services(IServiceCollection services, i_settings_service settingsService)
    {
        var settings = settingsService.Settings;

        // Settings Service (singleton, already created)
        services.AddSingleton(settingsService);

        // Core Infrastructure - use database path from settings
        var dbPath = string.IsNullOrEmpty(settings.database_path)
            ? "api_rig.db"
            : settings.database_path;
        services.AddDbContext<postman_clone_db_context>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Configure HttpClient with settings
        services.AddHttpClient("configured", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(settings.request_timeout_seconds);
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = settings.follow_redirects,
                MaxAutomaticRedirections = settings.max_redirects
            };

            // SSL validation
            if (!settings.validate_ssl_certificates)
            {
                handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
            }

            // Proxy configuration
            if (settings.use_proxy && !string.IsNullOrEmpty(settings.proxy_host))
            {
                var proxy = new WebProxy(settings.proxy_host, settings.proxy_port);

                if (!string.IsNullOrEmpty(settings.proxy_username))
                {
                    proxy.Credentials = new NetworkCredential(
                        settings.proxy_username,
                        settings.proxy_password);
                }

                proxy.BypassProxyOnLocal = settings.proxy_bypass_local;
                handler.Proxy = proxy;
                handler.UseProxy = true;
            }

            return handler;
        });

        // Services - use script timeout from settings
        services.AddSingleton<i_request_executor>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var client = factory.CreateClient("configured");
            return new http_request_executor(client);
        });
        services.AddSingleton<i_script_runner>(sp =>
            new script_runner(timeout_ms: settingsService.Settings.script_timeout_ms));
        services.AddSingleton<i_variable_resolver, variable_resolver>();
        services.AddScoped<request_orchestrator>();

        // Repositories & Stores
        services.AddScoped<i_environment_store, environment_store>();
        services.AddScoped<i_history_repository, history_repository>();
        services.AddScoped<i_collection_repository, collection_repository>();
        services.AddScoped<i_vault_store, vault_store>();
        services.AddScoped<i_workspace_store, workspace_store>();

        // ViewModels
        services.AddTransient<request_editor_view_model>();
        services.AddTransient<response_viewer_view_model>();
        services.AddTransient<sidebar_view_model>();
        services.AddTransient<environment_selector_view_model>();
        services.AddTransient<script_editor_view_model>();
        services.AddTransient<test_results_view_model>();
        services.AddTransient<tabs_view_model>();
        services.AddTransient<top_bar_view_model>();
        services.AddTransient<icon_bar_view_model>();
        services.AddTransient<side_panel_view_model>();
        services.AddTransient<bottom_bar_view_model>();
        services.AddTransient<environments_panel_view_model>();
        services.AddTransient<history_panel_view_model>();
        services.AddTransient<vault_panel_view_model>();
        services.AddTransient<environment_editor_view_model>(sp =>
            new environment_editor_view_model(sp.GetRequiredService<i_environment_store>()));
        services.AddSingleton<console_panel_view_model>();
        services.AddTransient<vault_editor_view_model>(sp =>
            new vault_editor_view_model(sp.GetRequiredService<i_vault_store>()));
        services.AddTransient(sp => new MainViewDependencies(
            sp.GetRequiredService<request_editor_view_model>(),
            sp.GetRequiredService<response_viewer_view_model>(),
            sp.GetRequiredService<sidebar_view_model>(),
            sp.GetRequiredService<environment_selector_view_model>(),
            sp.GetRequiredService<script_editor_view_model>(),
            sp.GetRequiredService<test_results_view_model>(),
            sp.GetRequiredService<tabs_view_model>(),
            sp.GetRequiredService<top_bar_view_model>(),
            sp.GetRequiredService<icon_bar_view_model>(),
            sp.GetRequiredService<side_panel_view_model>(),
            sp.GetRequiredService<bottom_bar_view_model>(),
            sp.GetRequiredService<environment_editor_view_model>(),
            sp.GetRequiredService<console_panel_view_model>(),
            sp.GetRequiredService<vault_editor_view_model>(),
            sp.GetRequiredService<environments_panel_view_model>(),
            sp.GetRequiredService<history_panel_view_model>(),
            sp.GetRequiredService<vault_panel_view_model>()));
        services.AddTransient<main_view_model>();
    }
}
