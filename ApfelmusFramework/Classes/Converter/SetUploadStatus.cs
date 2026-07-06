//-----------------------------------------------------------------------
// <copyright file="RectConverter.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
namespace ApfelmusFramework.Classes.Converter
{
    using System;
    using System.Windows.Data;
    using ApfelmusFramework.Classes.Logic;
    using ApfelmusFramework.Classes.Serializer;

    /// <summary>
    /// Uebersetzt den Upload-Statuscode in einen lokalisierten Text (1 =&gt; "uploadstatus1",
    /// sonst "uploadstatus2") anhand der in der Config gewaehlten Sprache. Nur Hin-Richtung.
    /// </summary>
    public class SetUploadStatus : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if ((int)value == 1)
                    if (string.IsNullOrEmpty(ConfigSerializer.DeserializeFromFile().LanguageFile))
                        return LanguageDictionary.GetLanguageDictionary()["uploadstatus1"];
                    else
                        return LanguageDictionary.GetLanguageDictionary(ConfigSerializer.DeserializeFromFile().LanguageFile)["uploadstatus1"];
                else
                    if (string.IsNullOrEmpty(ConfigSerializer.DeserializeFromFile().LanguageFile))
                        return LanguageDictionary.GetLanguageDictionary()["uploadstatus2"];
                    else
                        return LanguageDictionary.GetLanguageDictionary(ConfigSerializer.DeserializeFromFile().LanguageFile)["uploadstatus2"];
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
