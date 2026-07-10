using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    /// <summary>
    /// Setzt einen nicht-leeren Wert zur Anzeige in Klammern ("(Wert)"), sonst Leerstring -
    /// z.B. fuer optionale Zusatzangaben hinter einem Namen. Nur Hin-Richtung.
    /// </summary>
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
