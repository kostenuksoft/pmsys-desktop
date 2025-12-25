using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PMS.Core.Extensions.Converters;

public class BoolToStatusConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? "Активний" : "Неактивний";
        }

        return "Невідомо";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            return text == "Активний";
        }

        return false;
    }
}
