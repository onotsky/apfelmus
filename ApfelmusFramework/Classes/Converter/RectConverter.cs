//-----------------------------------------------------------------------
// <copyright file="RectConverter.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
using System;
using System.Windows;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{

    public class RectConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                double width = (double)values[0];
                double height = (double)values[1];
                return new Rect(0, 0, width, height);
            }
            catch
            {
                return null;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
