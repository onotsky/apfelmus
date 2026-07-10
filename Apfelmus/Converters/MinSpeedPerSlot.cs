using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    /// <summary>
    /// Gegenstueck zu MaxSpeedPerSlot: unterer Richtwert fuer "Geschwindigkeit pro Slot" aus
    /// der Gesamtgeschwindigkeit ueber eine flachere Kennlinie (x^0,2 auf KB). Nur Hin-Richtung.
    /// </summary>
    public class MinSpeedPerSlot : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double temp = System.Convert.ToDouble(value) / 1024;
            temp = Math.Pow(temp, 0.2);
            return System.Convert.ToInt32(temp);
        } 
             

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
