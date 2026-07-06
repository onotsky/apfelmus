using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    /// <summary>
    /// Wandelt einen Zeitstempel des Cores (Millisekunden seit der UNIX-Epoche) in eine
    /// lokale, kurze Datums- und Zeitangabe fuer die Anzeige um. Nur Hin-Richtung.
    /// </summary>
    public class SecondsToDate : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double d = System.Convert.ToDouble(value);
            //  gerechnet wird ab der UNIX Epoche
            System.DateTime dateTime = new System.DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            // den Timestamp addieren           
            dateTime = dateTime.AddMilliseconds(d);
            dateTime = dateTime.ToLocalTime();
            string Date = dateTime.ToShortDateString() + ", " + dateTime.ToShortTimeString();
            //MessageBox.Show(Date);
            return Date;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
