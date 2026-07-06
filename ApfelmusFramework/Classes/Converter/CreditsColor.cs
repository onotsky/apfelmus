using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    /// <summary>
    /// Faerbt einen Credit-Wert in der Benutzerliste: rot bei fehlendem Guthaben (&lt; 1),
    /// sonst gruen. Rueckgabe ist ein Brush-Name als String (nur Hin-Richtung).
    /// </summary>
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
