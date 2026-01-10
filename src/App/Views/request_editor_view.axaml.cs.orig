using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using App.ViewModels;
using System;
using System.ComponentModel;

namespace App.Views;

public partial class request_editor_view : UserControl
{
    private bool _isUpdatingFromVm;

    public request_editor_view()
    {
        InitializeComponent();

        var bodyEditor = this.FindControl<TextEditor>("BodyEditor");
        if (bodyEditor != null)
        {
            bodyEditor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("JavaScript"); // JSON usually uses JS highlighting in AvaloniaEdit if no specific JSON is found
            bodyEditor.TextChanged += (s, e) =>
            {
                if (DataContext is request_editor_view_model vm && !_isUpdatingFromVm)
                {
                    vm.RequestBody = bodyEditor.Text;
                }
            };
        }

        DataContextChanged += (s, e) =>
        {
            if (DataContext is request_editor_view_model vm)
            {
                vm.PropertyChanged += OnVmPropertyChanged;
                UpdateEditorText(vm.RequestBody);
            }
        };
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(request_editor_view_model.RequestBody))
        {
            if (DataContext is request_editor_view_model vm)
            {
                UpdateEditorText(vm.RequestBody);
            }
        }
    }

    private void UpdateEditorText(string text)
    {
        var bodyEditor = this.FindControl<TextEditor>("BodyEditor");
        if (bodyEditor != null && bodyEditor.Text != text)
        {
            _isUpdatingFromVm = true;
            bodyEditor.Text = text;
            _isUpdatingFromVm = false;
        }
    }
}
