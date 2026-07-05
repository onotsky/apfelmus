using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    public class MaxSpeedPerSlot : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double temp = System.Convert.ToDouble(value) / 1024;
            temp = Math.Pow(temp, 0.6);
            return System.Convert.ToInt32(temp);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
