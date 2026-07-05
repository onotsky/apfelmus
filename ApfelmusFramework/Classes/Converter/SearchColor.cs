using System;
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
                    return Brushes.LightGreen;
                case 2:
                    return Brushes.OrangeRed;
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
