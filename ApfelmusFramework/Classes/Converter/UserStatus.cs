using System;
using System.Windows.Data;
using System.Windows;
using ApfelmusFramework.Classes.Serializer;
using ApfelmusFramework.Classes.Logic;

namespace ApfelmusFramework.Classes.Converter
{
    /// <summary>
    /// Uebersetzt den Verbindungs-/Userstatuscode eines Nutzers in einen lokalisierten Text
    /// (Sprachschluessel "userstatusN") anhand der in der Config gewaehlten Sprache. Nur Hin-Richtung.
    /// </summary>
    public class UserStatus : IValueConverter
    {
        ResourceDictionary dict;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (string.IsNullOrEmpty(ConfigSerializer.DeserializeFromFile().LanguageFile))
                dict = LanguageDictionary.GetLanguageDictionary();
            else
                dict = LanguageDictionary.GetLanguageDictionary(ConfigSerializer.DeserializeFromFile().LanguageFile);

            switch ((int)value)
            {
                case 1:
                    return dict["userstatus1"].ToString();
                case 2:
                    return dict["userstatus2"].ToString();
                case 3:
                    return dict["userstatus3"].ToString();
                case 4:
                    return dict["userstatus4"].ToString();
                case 5:
                    return dict["userstatus5"].ToString();
                case 6:
                    return dict["userstatus6"].ToString();
                case 7:
                    return dict["userstatus7"].ToString();
                case 8:
                    return dict["userstatus8"].ToString();
                case 9:
                    return dict["userstatus9"].ToString();
                case 11:
                    return dict["userstatus11"].ToString();
                case 12:
                    return dict["userstatus12"].ToString();
                case 13:
                    return dict["userstatus13"].ToString();
                case 14:
                    return dict["userstatus14"].ToString();
                case 15:
                    return dict["userstatus15"].ToString();
                case 16:
                    return dict["userstatus16"].ToString();
                default:
                    return dict["userstatus17"].ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
