using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using PostmanClone.App.Services;
using PostmanClone.App.ViewModels;
using PostmanClone.App.Views;
using PostmanClone.Core.Interfaces;
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
        // Mock services (will be replaced with real implementations later)
        services.AddSingleton<i_request_executor, mock_request_executor>();
        services.AddSingleton<i_collection_repository, mock_collection_repository>();
        services.AddSingleton<i_environment_store, mock_environment_store>();
        services.AddSingleton<i_history_repository, mock_history_repository>();

        // ViewModels
        services.AddTransient<main_view_model>();
        services.AddTransient<request_editor_view_model>();
        services.AddTransient<response_viewer_view_model>();
        services.AddTransient<sidebar_view_model>();
        services.AddTransient<environment_selector_view_model>();
    }
}
