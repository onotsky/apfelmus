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

    public class FileSizeConverter3 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            double s = System.Convert.ToDouble(value);

            for (int i = 0; i < 1; i++)
            {
                s = (long)(100 * s / 1024) / 100.0;
            }
            return s;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            double s = System.Convert.ToDouble(value);

            return s *= 1024;
        }
    }
}
