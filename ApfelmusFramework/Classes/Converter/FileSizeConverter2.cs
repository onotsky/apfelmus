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
    /// Wie FileSizeConverter1, jedoch ab MB beginnend und auf zwei Nachkommastellen gerundet
    /// (fuer groessere Werte wie Gesamt-Transfervolumen). Behandelt "." als Dezimaltrenner.
    /// Nur Hin-Richtung.
    /// </summary>
    public class FileSizeConverter2 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            if (value.ToString().Contains("."))
                value = value.ToString().Replace(".", ",");

            double s = System.Convert.ToDouble(value);
            
            string[] format = new string[]
                  {
                      "{0} MB", "{0} GB", "{0} TB", "{0} PB", "{0} EB"
                  };

            int i = 0;

            if ((s < 0.0))
            {
                while (i < format.Length && s <= -1024)
                {
                    s = (100 * s / 1024) / 100;
                    i++;
                }
            }
            else
            {
                while (i < format.Length && s >= 1024)
                {
                    s = (100 * s / 1024) / 100;
                    i++;
                }
            }
            return string.Format(format[i], Math.Round(s, 2));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
