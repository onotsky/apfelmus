using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    public class PowerDownload : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double temp = System.Convert.ToDouble(value);
            temp = (temp + 10.0) / 10.0;

            return "1:" + temp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
