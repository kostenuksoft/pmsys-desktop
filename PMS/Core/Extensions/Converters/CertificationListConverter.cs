using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using PMS.Core.Models;

namespace PMS.Core.Extensions.Converters;

public class CertificationListConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is List<Certification> certifications && certifications.Any())
        {
            return string.Join(", ", certifications.Select(c => c.Name));
        }
        return "Немає сертифікатів";
    }


    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return new object();
    }
}