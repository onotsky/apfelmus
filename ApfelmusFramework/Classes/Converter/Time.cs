using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    public class Time : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((int)value == 0)
            {
                return string.Empty;
            }
            else
            {
                DateTime dTime = DateTime.MinValue.AddSeconds(System.Convert.ToDouble(value));
                if (dTime.Day > DateTime.MinValue.Day)
                    return string.Format("{0:dd}", dTime - DateTime.MinValue) + ":" + dTime.ToLongTimeString();
                else
                    return "00:" + dTime.ToLongTimeString();
            }


        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
