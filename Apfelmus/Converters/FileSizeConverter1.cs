//-----------------------------------------------------------------------
// <copyright file="FileSizeConverter1.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    /// <summary>
    /// Formatiert eine Byte-Anzahl als menschenlesbare Groesse (bytes/KB/MB/GB/TB/PB/EB),
    /// indem sie so oft durch 1024 geteilt wird, bis sie unter die naechste Einheitsgrenze
    /// faellt. Bei 0 wird ein Leerstring geliefert (Tabellenspalten bleiben leer). Negative
    /// Werte werden analog behandelt (z.B. Restgroessen). Nur Hin-Richtung.
    /// </summary>
    public class FileSizeConverter1 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (System.Convert.ToInt64(value) == 0)
                return string.Empty;

            double s = System.Convert.ToDouble(value);
            string[] format = new string[]
                  {
                      "{0} bytes", "{0} KB",  
                      "{0} MB", "{0} GB", "{0} TB", "{0} PB", "{0} EB"
                  };

            int i = 0;

            if ((s < 0.0))
            {
                while (i < format.Length && s < -1024)
                {
                    s = (long)(100 * s / 1024) / 100.0;
                    i++;
                }
            }
            else
            {
                while (i < format.Length && s > 1024)
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
