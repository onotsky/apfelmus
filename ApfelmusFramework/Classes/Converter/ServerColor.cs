using System;
using System.Windows.Data;
using System.Windows.Media;

namespace ApfelmusFramework.Classes.Converter
{
    public class ServerColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return Brushes.LightGreen;
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
