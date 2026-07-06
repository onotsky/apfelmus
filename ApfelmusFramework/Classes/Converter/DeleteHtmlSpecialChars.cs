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
    /// <summary>
    /// Entfernt HTML-Tags (&lt;...&gt;) aus einem Text und liefert reinen, getrimmten Klartext -
    /// z.B. fuer vom Core gelieferte Beschreibungen/Namen mit Markup. Nur Hin-Richtung.
    /// </summary>
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
