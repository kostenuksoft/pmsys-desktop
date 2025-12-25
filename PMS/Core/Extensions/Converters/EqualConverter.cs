using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PMS.Core.Extensions.Converters
{
    public class EqualConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            try
            {
                var valDecimal = System.Convert.ToDecimal(value);
                var paramDecimal = System.Convert.ToDecimal(parameter);

                return valDecimal == paramDecimal;
            }
            catch (Exception)
            {
                return string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("EqualConverter does not support ConvertBack");
        }
    }

    public class NotEqualConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return true;

            try
            {
                var valDecimal = System.Convert.ToDecimal(value);
                var paramDecimal = System.Convert.ToDecimal(parameter);

                return valDecimal != paramDecimal;
            }
            catch (Exception)
            {
                return !string.Equals(value.ToString(), parameter.ToString(), StringComparison.OrdinalIgnoreCase);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException("NotEqualConverter does not support ConvertBack");
        }
    }
}