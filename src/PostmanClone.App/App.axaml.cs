using Avalonia;
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
        }

        base.OnFrameworkInitializationCompleted();
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

        // ViewModels
        services.AddTransient<main_view_model>();
        services.AddTransient<request_editor_view_model>();
        services.AddTransient<response_viewer_view_model>();
        services.AddTransient<sidebar_view_model>();
        services.AddTransient<environment_selector_view_model>();
        services.AddTransient<script_editor_view_model>();
        services.AddTransient<test_results_view_model>();
    }
}
