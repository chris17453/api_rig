using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PostmanClone.Core.Models;

namespace PostmanClone.App.ViewModels;

public partial class main_view_model : ObservableObject
{
    [ObservableProperty]
    private request_editor_view_model _requestEditor;

    [ObservableProperty]
    private response_viewer_view_model _responseViewer;

    [ObservableProperty]
    private sidebar_view_model _sidebar;

    [ObservableProperty]
    private environment_selector_view_model _environmentSelector;

    [ObservableProperty]
    private string _title = "PostmanClone";

    [ObservableProperty]
    private bool _isSidebarVisible = true;

    public main_view_model(
        request_editor_view_model request_editor,
        response_viewer_view_model response_viewer,
        sidebar_view_model sidebar,
        environment_selector_view_model environment_selector)
    {
        _requestEditor = request_editor;
        _responseViewer = response_viewer;
        _sidebar = sidebar;
        _environmentSelector = environment_selector;

        // Wire up events
        _requestEditor.response_received += on_response_received;
        _sidebar.request_selected += on_request_selected;
    }

    [RelayCommand]
    public async Task initialize_async(CancellationToken cancellation_token)
    {
        await Sidebar.load_data_async(cancellation_token);
        await EnvironmentSelector.load_environments_async(cancellation_token);
    }

    [RelayCommand]
    private void toggle_sidebar()
    {
        IsSidebarVisible = !IsSidebarVisible;
    }

    private void on_response_received(object? sender, http_response_model response)
    {
        ResponseViewer.load_response(response);
        _ = Sidebar.refresh_history_async(CancellationToken.None);
    }

    private void on_request_selected(object? sender, http_request_model request)
    {
        RequestEditor.load_request(request);
        ResponseViewer.clear();
    }
}
