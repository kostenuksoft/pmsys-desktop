using System;
using System.Globalization;
using Avalonia.Data.Converters;
using PMS.Core.Enums.General;

namespace PMS.Core.Extensions.Converters;


public class RoleConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is UserRole role)
        {
            return role switch
            {
                UserRole.Administrator => "Адміністратор",
                UserRole.Operator => "Оператор",
                UserRole.Authorized => "Авторизований",
                UserRole.Guest => "Гість",
                _ => role.ToString()
            };
        }

        return value?.ToString() ?? string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            return text switch
            {
                "Адміністратор" => UserRole.Administrator,
                "Оператор" => UserRole.Operator,
                "Авторизований" => UserRole.Authorized,
                "Гість" => UserRole.Guest,
                _ => null
            };
        }

        return null;
    }
}
