using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    /// <summary>
    /// Stellt einen PowerDownload-Wert als Verhaeltnis "1:x" dar, wobei x = (Wert + 10) / 10
    /// (Anzahl paralleler Quellen pro fertiger Datei). Nur Hin-Richtung.
    /// </summary>
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
