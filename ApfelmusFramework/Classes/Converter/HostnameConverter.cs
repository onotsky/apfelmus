using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    public class HostnameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.ToString() == "127.0.0.1")
                return "localhost";
            else
                return value.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value.ToString() == "localhost")
                return "127.0.0.1";
            else
                return value.ToString();
        }
    }
}
