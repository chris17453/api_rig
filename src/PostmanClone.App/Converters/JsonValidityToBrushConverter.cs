using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace PostmanClone.App.Converters;

public class JsonValidityToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isValid)
        {
            return isValid ? new SolidColorBrush(Color.Parse("#30363D")) : new SolidColorBrush(Color.Parse("#FF7B72"));
        }
        return new SolidColorBrush(Color.Parse("#30363D"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
