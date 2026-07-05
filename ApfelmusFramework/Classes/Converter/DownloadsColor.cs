using System;
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
                    return Brushes.Red;
                case 14:
                    return Brushes.LightGreen;
                case 17:
                    return Brushes.Orange;
                case 18:
                    return Brushes.Blue;
                default:
                    return Brushes.White;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
