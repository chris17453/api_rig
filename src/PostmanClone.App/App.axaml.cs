using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PostmanClone.App.ViewModels;
using PostmanClone.App.Views;
using PostmanClone.Core.Interfaces;
using PostmanClone.Data.Context;
using PostmanClone.Data.Repositories;
using PostmanClone.Data.Services;
using PostmanClone.Data.Stores;
using PostmanClone.Http.Services;
using PostmanClone.Scripting;
using System;
using System.Threading.Tasks;

namespace PostmanClone.App;

public partial class App : Application
{
    public static IServiceProvider? services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var service_collection = new ServiceCollection();
        configure_services(service_collection);
        services = service_collection.BuildServiceProvider();

        // Initialize DB
        var db = services.GetRequiredService<postman_clone_db_context>();
        db.Database.EnsureCreated();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new main_window
            {
                DataContext = services.GetRequiredService<main_view_model>()
            };

            // Check if user has registered and show registration dialog if not
            desktop.MainWindow.Opened += async (s, e) => await check_and_show_registration_dialog_async(desktop.MainWindow);
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task check_and_show_registration_dialog_async(Window main_window)
    {
        try
        {
            using var scope = services!.CreateScope();
            var registration_store = scope.ServiceProvider.GetRequiredService<i_app_registration_store>();
            
            var is_registered = await registration_store.is_registered_async();
            
            if (!is_registered)
            {
                var registration_vm = scope.ServiceProvider.GetRequiredService<registration_view_model>();
                var registration_dialog = new registration_dialog
                {
                    DataContext = registration_vm
                };
                
                await registration_dialog.ShowDialog(main_window);
            }
        }
        catch (Exception ex)
        {
            // Log error but don't prevent app from starting
            System.Diagnostics.Debug.WriteLine($"Error checking registration: {ex.Message}");
        }
    }

    private void configure_services(IServiceCollection services)
    {
        // Core Infrastructure
        services.AddDbContext<postman_clone_db_context>(options =>
            options.UseSqlite("Data Source=postman_clone.db"));
        
        services.AddHttpClient();

        // Services
        services.AddSingleton<i_request_executor, http_request_executor>();
        services.AddSingleton<i_script_runner>(sp => new script_runner(timeout_ms: 5000));
        services.AddSingleton<i_variable_resolver, variable_resolver>();
        services.AddScoped<request_orchestrator>();

        // Repositories & Stores
        services.AddScoped<i_environment_store, environment_store>();
        services.AddScoped<i_history_repository, history_repository>();
        services.AddScoped<i_collection_repository, collection_repository>();
        services.AddScoped<i_app_registration_store, app_registration_store>();

        // ViewModels
        services.AddTransient<main_view_model>();
        services.AddTransient<request_editor_view_model>();
        services.AddTransient<response_viewer_view_model>();
        services.AddTransient<sidebar_view_model>();
        services.AddTransient<environment_selector_view_model>();
        services.AddTransient<script_editor_view_model>();
        services.AddTransient<test_results_view_model>();
        services.AddTransient<registration_view_model>();
    }
}
