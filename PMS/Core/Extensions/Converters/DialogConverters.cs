using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace PMS.Core.Extensions.Converters
{
    public class EnumToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            if (parameter is string paramString && Enum.TryParse(value.GetType(), paramString, out var paramEnum))
            {
                return value.Equals(paramEnum);
            }

            return value.Equals(parameter);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue && parameter != null)
            {
                if (parameter is string paramString && Enum.TryParse(targetType, paramString, out var result))
                {
                    return result;
                }
                return parameter;
            }

            return Avalonia.Data.BindingOperations.DoNothing;
        }
    }

    public class StringConverters
    {
        public static readonly IValueConverter IsNotNullOrEmpty = new IsNotNullOrEmptyConverter();
        public static readonly IValueConverter IsNullOrEmpty = new IsNullOrEmptyConverter();

        private class IsNotNullOrEmptyConverter : IValueConverter
        {
            public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return !string.IsNullOrEmpty(value as string);
            }

            public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private class IsNullOrEmptyConverter : IValueConverter
        {
            public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return string.IsNullOrEmpty(value as string);
            }

            public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }

    public class ObjectConverters
    {
        public static readonly IValueConverter IsNotNull = new IsNotNullConverter();
        public static readonly IValueConverter IsNull = new IsNullConverter();

        private class IsNotNullConverter : IValueConverter
        {
            public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return value != null;
            }

            public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        private class IsNullConverter : IValueConverter
        {
            public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return value == null;
            }

            public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}