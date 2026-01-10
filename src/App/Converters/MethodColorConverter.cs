using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace App.Converters;

/// <summary>
/// Converts HTTP method string to its corresponding color brush
/// </summary>
public class MethodColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var method = value?.ToString()?.ToUpperInvariant() ?? "GET";

        var brushKey = method switch
        {
            "GET" => "HttpGetBrush",
            "POST" => "HttpPostBrush",
            "PUT" => "HttpPutBrush",
            "PATCH" => "HttpPatchBrush",
            "DELETE" => "HttpDeleteBrush",
            "HEAD" => "HttpHeadBrush",
            "OPTIONS" => "HttpOptionsBrush",
            _ => "HttpGetBrush"
        };

        if (Application.Current?.Resources.TryGetResource(brushKey, Application.Current.ActualThemeVariant, out var brush) == true && brush is IBrush b)
        {
            return b;
        }

        return new SolidColorBrush(Color.Parse("#3FB950")); // Default to GET color
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
