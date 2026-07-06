//-----------------------------------------------------------------------
// <copyright file="RectConverter.cs" company="ZSoft">
//     Copyright (c) ZSoft.
// </copyright>
// <author>daredevil</author>
//-----------------------------------------------------------------------
using System;
using System.Windows;
using System.Windows.Data;
using ApfelmusFramework.Classes.Logic;
using ApfelmusFramework.Classes.Serializer;

namespace ApfelmusFramework.Classes.Converter
{

    /// <summary>
    /// Baut den anzeigbaren Download-Statustext aus mehreren gebundenen Werten: values[0] ist der
    /// Statuscode des Cores, der auf einen lokalisierten Text (Sprachschluessel "downloadstatusN")
    /// abgebildet wird; die weiteren Werte (z.B. Quellen, Prozent) werden je nach Status in den
    /// Text eingesetzt. Sprache kommt aus der aktuellen Config. Nur Hin-Richtung.
    /// </summary>
    public class SetDownloadStatus : IMultiValueConverter
    {
        ResourceDictionary dict;

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (string.IsNullOrEmpty(ConfigSerializer.DeserializeFromFile().LanguageFile))
                    dict = LanguageDictionary.GetLanguageDictionary();
                else
                    dict = LanguageDictionary.GetLanguageDictionary(ConfigSerializer.DeserializeFromFile().LanguageFile);

                string[] valuesString = new string[values.Length];

                for (int i = 1; i < values.Length; i++)
                {
                    valuesString[i] = values[i].ToString();
                }

                switch ((int)values[0])
                {
                    case 0:
                        valuesString[0] = dict["downloadstatus0"].ToString();
                        return String.Format("{0} {1}/{2}", valuesString);
                    case 1:
                        valuesString[0] = dict["downloadstatus1"].ToString();
                        return String.Format("{0}", valuesString);
                    case 2:
                        valuesString[0] = dict["downloadstatus2"].ToString();
                        return String.Format("{0} {1}/{2} ({3})", valuesString);
                    case 12:
                        valuesString[0] = dict["downloadstatus12"].ToString();
                        return String.Format("{0}", valuesString);
                    case 13:
                        valuesString[0] = dict["downloadstatus13"].ToString();
                        return String.Format("{0}", valuesString);
                    case 14:
                        valuesString[0] = dict["downloadstatus14"].ToString();
                        return String.Format("{0}", valuesString);
                    case 15:
                        valuesString[0] = dict["downloadstatus15"].ToString();
                        return String.Format("{0}", valuesString);
                    case 16:
                        valuesString[0] = dict["downloadstatus16"].ToString();
                        return String.Format("{0}", valuesString);
                    case 17:
                        valuesString[0] = dict["downloadstatus17"].ToString();
                        return String.Format("{0}", valuesString);
                    case 18:
                        valuesString[0] = dict["downloadstatus18"].ToString();
                        return String.Format("{0}", valuesString);
                    default:
                        valuesString[0] = dict["downloadstatus19"].ToString();
                        return String.Format("{0}", valuesString);
                }
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
