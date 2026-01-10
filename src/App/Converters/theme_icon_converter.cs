using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace App.Converters;

public class theme_icon_converter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDarkTheme)
        {
            // If dark theme is active, show sun icon (to switch to light)
            // If light theme is active, show moon icon (to switch to dark)
            return isDarkTheme ? "☀" : "🌙";
        }
        return "☀";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
