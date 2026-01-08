using Avalonia.Controls;
using Avalonia.Interactivity;
using PostmanClone.App.ViewModels;

namespace PostmanClone.App.Views;

public partial class main_window : Window
{
    public main_window()
    {
        InitializeComponent();
    }

    private async void ImportButton_Click(object? sender, RoutedEventArgs e)
    {
        var mainVm = DataContext as main_view_model;
        if (mainVm == null) return;

        var importVm = mainVm.CreateImportExportViewModel();
        var dialog = new import_dialog { DataContext = importVm };
        
        importVm.collection_imported += async (s, collection) =>
        {
            dialog.Close();
            await mainVm.Sidebar.load_data_async(CancellationToken.None);
        };

        await dialog.ShowDialog(this);
    }

    private async void ExportButton_Click(object? sender, RoutedEventArgs e)
    {
        var mainVm = DataContext as main_view_model;
        if (mainVm == null) return;

        var selectedCollection = mainVm.Sidebar.SelectedCollection;
        if (selectedCollection == null)
        {
            // Show message to select a collection first
            return;
        }

        var exportVm = mainVm.CreateImportExportViewModel();
        exportVm.SelectedCollectionForExport = selectedCollection;
        var dialog = new export_dialog { DataContext = exportVm };
        
        exportVm.export_completed += (s, args) =>
        {
            dialog.Close();
        };

        await dialog.ShowDialog(this);
    }

    private async void AboutButton_Click(object? sender, RoutedEventArgs e)
    {
        var aboutVm = new about_view_model();
        var dialog = new about_dialog { DataContext = aboutVm };
        await dialog.ShowDialog(this);
    }
}
