using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace PMS.Core.Extensions.Converters;


public class BoolToBackgroundConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isAvailable)
        {
          
            return isAvailable
                ? new SolidColorBrush(Color.Parse("#E8F5E9"))
                : new SolidColorBrush(Color.Parse("#F5F5F5"));
        }

        return new SolidColorBrush(Colors.Transparent);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}