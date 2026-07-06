using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    /// <summary>
    /// Errechnet aus der konfigurierten Gesamtgeschwindigkeit (Bytes/s) einen sinnvollen
    /// oberen Richtwert fuer "Geschwindigkeit pro Slot" ueber eine nichtlineare Kennlinie
    /// (Wurzelfunktion x^0,6 auf KB), damit hohe Bandbreiten nicht linear durchschlagen.
    /// Nur Hin-Richtung.
    /// </summary>
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
