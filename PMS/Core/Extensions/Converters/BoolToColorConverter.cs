using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace PMS.Core.Extensions.Converters;

public class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isActive)
        {
            return isActive ? Color.Parse("#4CAF50") : Color.Parse("#F44336");
        }

        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
