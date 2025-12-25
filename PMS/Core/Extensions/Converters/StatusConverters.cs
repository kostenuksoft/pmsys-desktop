using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using PMS.Core.Enums.General;

namespace PMS.Core.Extensions.Converters
{
    public class StatusToColorConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is RequestStatus status)
            {
                return status switch
                {
                    RequestStatus.Pending => new SolidColorBrush(Color.FromRgb(255, 185, 0)), 
                    RequestStatus.Approved => new SolidColorBrush(Color.FromRgb(16, 124, 16)), 
                    RequestStatus.Rejected => new SolidColorBrush(Color.FromRgb(196, 43, 28)), 
                    _ => new SolidColorBrush(Color.FromRgb(106, 106, 106))
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToPendingConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is RequestStatus status && status == RequestStatus.Pending;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToApprovedConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is RequestStatus status && status == RequestStatus.Approved;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToRejectedConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return value is RequestStatus status && status == RequestStatus.Rejected;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}