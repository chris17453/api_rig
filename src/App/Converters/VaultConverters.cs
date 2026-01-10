using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace App.Converters;

/// <summary>
/// Converts boolean lock status to lock icon
/// </summary>
public class BoolToLockIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUnlocked)
        {
            return isUnlocked ? "🔓" : "🔒";
        }
        return "🔒";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Converts boolean lock status to lock text
/// </summary>
public class BoolToLockTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isUnlocked)
        {
            return isUnlocked ? "Vault Unlocked" : "Vault Locked";
        }
        return "Vault Locked";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
