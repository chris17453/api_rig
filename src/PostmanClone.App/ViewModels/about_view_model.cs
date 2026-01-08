using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace PostmanClone.App.ViewModels;

public partial class about_view_model : ObservableObject
{
    [ObservableProperty]
    private string _applicationName = "API Rig (PostmanClone)";

    [ObservableProperty]
    private string _version = "1.0.0";

    [ObservableProperty]
    private string _description = @"A cross-platform Postman clone built with .NET 10 and Avalonia UI.

API Rig allows you to import Postman collections, execute HTTP requests, manage environments with variable substitution, run pre/post scripts with assertions, and track request history.

Features:
• Import/Export Postman collections (v2.0 and v2.1)
• Execute HTTP requests (GET, POST, PUT, PATCH, DELETE, HEAD, OPTIONS, TRACE)
• Request body types: none, raw, JSON, form-data, x-www-form-urlencoded
• Authentication: Basic, Bearer, API Key, OAuth2 Client Credentials
• Environment variables with {{variable}} syntax
• Pre-request and post-response scripts
• pm.test() and pm.expect() assertions (Chai-style)
• Request history persisted to SQLite";

    [ObservableProperty]
    private string _buildDate = "January 2026";

    [ObservableProperty]
    private string _techStack = ".NET 10 | Avalonia 11 | EF Core + SQLite | Jint | xUnit";

    public ObservableCollection<team_member_model> TeamMembers { get; } = new()
    {
        new team_member_model
        {
            Name = "Amlan",
            Role = "Data Developer",
            Contributions = "SQLite schema, DbContext, Postman v2.0/v2.1 parsers, " +
                           "collection exporter, environment store, history repository"
        },
        new team_member_model
        {
            Name = "Ankur",
            Role = "UI/UX Developer",
            Contributions = "Avalonia UI implementation, main window layout, " +
                           "request editor, response viewer, sidebar with collections/history, " +
                           "environment selector, script editor, test results display"
        },
        new team_member_model
        {
            Name = "Juan",
            Role = "HTTP & Integration Developer",
            Contributions = "HTTP request executor, authentication handlers " +
                           "(Basic, Bearer, API Key, OAuth2), response processing, " +
                           "request orchestrator, variable resolver"
        },
        new team_member_model
        {
            Name = "Edwar",
            Role = "Core & Scripting Developer",
            Contributions = "Core models, interfaces, Jint JavaScript engine integration, " +
                           "pm.test() implementation, pm.expect() assertions (20+ types), " +
                           "pm.environment API, pm.request/pm.response objects, sandbox security"
        }
    };

    [ObservableProperty]
    private string _testingStatus = "243 Tests Passing";

    [ObservableProperty]
    private string _license = "MIT License";
}

public class team_member_model
{
    public string Name { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Contributions { get; set; } = string.Empty;
}
