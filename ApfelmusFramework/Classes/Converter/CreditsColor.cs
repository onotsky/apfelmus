using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    public class CreditsColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((long)value < 1)
                return "Red";
            else
                return "Green";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
