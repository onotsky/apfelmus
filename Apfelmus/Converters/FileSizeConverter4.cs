//-----------------------------------------------------------------------
// <copyright file="FileSizeConverter2.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{

    /// <summary>
    /// Formatiert einen Geschwindigkeitswert als B/s bzw. Kb/s (nur diese zwei Einheiten);
    /// 0 ergibt Leerstring. Nur Hin-Richtung.
    /// </summary>
    public class FileSizeConverter4 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((int)value == 0)
                return string.Empty;

            double s = System.Convert.ToDouble(value);
            string[] format = new string[] { "{0} B/s", "{0} Kb/s" };

            int i = 0;
           
            if ((s < 0.0))
            {
                while (i < format.Length - 1 && s <= -1024)
                {
                    s = (long)(100 * s / 1024) / 100.0;
                    i++;
                }
            }
            else
            {
                while (i < format.Length - 1 && s >= 1024)
                {
                    s = (long)(100 * s / 1024) / 100.0;
                    i++;
                }
            }
            return string.Format(format[i], s);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
