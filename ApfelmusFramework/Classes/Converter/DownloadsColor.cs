using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ApfelmusFramework.Classes.Converter
{
    public class DownloadsColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch ((int)value)
            {
                case 1:
                    return Application.Current?.TryFindResource("StatusErrorBrush") as Brush;
                case 14:
                    return Application.Current?.TryFindResource("StatusOkBrush") as Brush;
                case 17:
                    return Application.Current?.TryFindResource("StatusWarnBrush") as Brush;
                case 18:
                    return Application.Current?.TryFindResource("StatusInfoBrush") as Brush;
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
