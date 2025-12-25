using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PMS.Core.Enums.General;

namespace PMS.Core.Extensions.Converters;


public class RoleToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UserRole role)
        {
            return role switch
            {
                UserRole.Administrator => new SolidColorBrush(Color.Parse("#D32F2F")),
                UserRole.Operator => new SolidColorBrush(Color.Parse("#1976D2")),
                UserRole.Authorized => new SolidColorBrush(Color.Parse("#388E3C")),
                UserRole.Guest => new SolidColorBrush(Color.Parse("#757575")),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
