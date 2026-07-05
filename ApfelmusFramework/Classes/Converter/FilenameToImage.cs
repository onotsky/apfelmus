using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    public class FilenameToImage : IValueConverter
    {
        private System.Windows.Media.ImageSource icon;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                if (value.ToString() == string.Empty)
                    return null;
                string fileName = value.ToString();
                char[] sep = new char[] { '.' };
                string[] extension = fileName.Split(sep, StringSplitOptions.RemoveEmptyEntries);
                string tempFile = Path.Combine(Path.GetTempPath(), "_temp" + Guid.NewGuid().ToString() + "." + extension[extension.Length - 1]);

                File.WriteAllText(tempFile, "dummy");

                using (Icon sysicon = Icon.ExtractAssociatedIcon(tempFile))
                {
                    icon = Imaging.CreateBitmapSourceFromHIcon(sysicon.Handle, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromWidthAndHeight(16, 16));
                }


                File.Delete(tempFile);
                return icon;
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
