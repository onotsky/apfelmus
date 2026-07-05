using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    public class SetClip : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(!string.IsNullOrEmpty(value.ToString()))
            {
                return string.Format("({0})",value.ToString());
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
