using System;
using System.Windows.Data;

namespace ApfelmusFramework.Classes.Converter
{
    public class TypeToImage : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return string.Empty;

            switch ((int)value)
            {
                case 1:
                    return @"/ApfelmusFramework;component/Images/computer.png";
                case 2:
                    return @"/ApfelmusFramework;component/Images/Harddisk.png";
                case 3:
                    return @"/ApfelmusFramework;component/Images/floppy.png";
                case 4:
                    return @"/ApfelmusFramework;component/Images/folder.png";
                case 5:
                    return @"/ApfelmusFramework;component/Images/Desktop.png";
                default:
                    return string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
