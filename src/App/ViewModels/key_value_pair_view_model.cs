using CommunityToolkit.Mvvm.ComponentModel;

namespace App.ViewModels;

public partial class key_value_pair_view_model : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;
}
