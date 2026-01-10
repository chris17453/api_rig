using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Core.Models;
using System;
using System.Globalization;

namespace App.Converters;

/// <summary>
/// Converts HTTP method to its corresponding color brush
/// </summary>
public class HttpMethodToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is http_method method)
        {
            var brushKey = method switch
            {
                http_method.get => "HttpGetBrush",
                http_method.post => "HttpPostBrush",
                http_method.put => "HttpPutBrush",
                http_method.patch => "HttpPatchBrush",
                http_method.delete => "HttpDeleteBrush",
                http_method.head => "HttpHeadBrush",
                http_method.options => "HttpOptionsBrush",
                _ => "HttpGetBrush"
            };

            if (Application.Current?.Resources.TryGetResource(brushKey, Application.Current.ActualThemeVariant, out var brush) == true && brush is IBrush b)
            {
                return b;
            }
        }

        return new SolidColorBrush(Color.Parse("#3FB950")); // Default to GET color
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts HTTP method to uppercase string
/// </summary>
public class HttpMethodToUpperConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is http_method method)
        {
            return method.ToString().ToUpperInvariant();
        }
        return value?.ToString()?.ToUpperInvariant() ?? "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
