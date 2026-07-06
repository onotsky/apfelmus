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
    /// Waehlt anhand des Firewall-Status (true = hinter Firewall/nicht erreichbar) das passende
    /// Firewall-Symbol (on/off) und liefert dessen Pack-URI-Pfad. Nur Hin-Richtung.
    /// </summary>
    public class FirewallConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
                return "/ApfelmusFramework;component/Images/security_firewall_off.png";
            else
                return "/ApfelmusFramework;component/Images/security_firewall_on.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
