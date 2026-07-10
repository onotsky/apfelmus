using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ApfelmusFramework.Classes.Converter
{
    /// <summary>
    /// Hebt den aktuell verbundenen Server in der Serverliste hervor: true = verbunden liefert
    /// einen gruenen Brush, sonst keinen. Nur Hin-Richtung.
    /// </summary>
    public class ServerColor : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return Application.Current?.TryFindResource("StatusOkBrush") as Brush;
            else
                return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
