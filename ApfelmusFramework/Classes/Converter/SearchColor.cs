using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ApfelmusFramework.Classes.Converter
{
    public class SearchColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch ((int)value)
            {
                case 1:
                    return Application.Current?.TryFindResource("StatusOkBrush") as Brush;
                case 2:
                    return Application.Current?.TryFindResource("StatusErrorBrush") as Brush;
                default:
                    return null;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
