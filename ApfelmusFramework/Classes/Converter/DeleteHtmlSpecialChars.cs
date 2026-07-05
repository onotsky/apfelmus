//-----------------------------------------------------------------------
// <copyright file="DeleteHtmlSpecialChars.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
using System;
using System.Text.RegularExpressions;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    public class DeleteHtmlSpecialchars : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value != null)
                return Regex.Replace(value.ToString(), "<.*?>", " ").Trim();
            else
                return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
