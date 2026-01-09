using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PostmanClone.App.ViewModels;

namespace PostmanClone.App.Views;

public partial class sidebar_view : UserControl
{
    public sidebar_view()
    {
        InitializeComponent();
    }

    private void OnNameDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is TextBlock textBlock && textBlock.DataContext is collection_tree_item_view_model item)
        {
            item.IsEditing = true;
            
            // Focus the TextBox after a short delay to ensure it's visible
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (textBlock.Parent is StackPanel stackPanel)
                {
                    var textBox = stackPanel.Children.OfType<TextBox>().FirstOrDefault();
                    if (textBox != null)
                    {
                        textBox.Focus();
                        textBox.SelectAll();
                    }
                }
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        }
    }

    private void OnNameEditLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && 
            textBox.DataContext is collection_tree_item_view_model item &&
            DataContext is sidebar_view_model viewModel)
        {
            viewModel.RenameItemCommand.Execute(item);
        }
    }

    private void OnNameEditKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && 
            sender is TextBox textBox && 
            textBox.DataContext is collection_tree_item_view_model item &&
            DataContext is sidebar_view_model viewModel)
        {
            viewModel.RenameItemCommand.Execute(item);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && 
                 sender is TextBox textBox2 && 
                 textBox2.DataContext is collection_tree_item_view_model item2)
        {
            // Cancel editing without saving
            item2.IsEditing = false;
            e.Handled = true;
        }
    }
}

