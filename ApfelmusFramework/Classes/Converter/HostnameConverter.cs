using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    /// <summary>
    /// Zeigt die Loopback-Adresse 127.0.0.1 im Host-Feld benutzerfreundlich als "localhost"
    /// an und wandelt sie bei der Eingabe wieder zurueck (bidirektionaler XAML-Converter).
    /// </summary>
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
