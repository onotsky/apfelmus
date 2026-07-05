using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ApfelmusFramework.Classes.Converter
{
    public class TypeToImage : IValueConverter
    {
        private static readonly Brush IconBrush = CreateFrozenBrush();
        private static readonly Pen IconPen = CreateFrozenPen();

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
            {
                return null;
            }

            DrawingGroup drawing = new DrawingGroup();
            using (DrawingContext context = drawing.Open())
            {
                switch ((int)value)
                {
                    case 1:
                        DrawMonitor(context, withTaskbar: false);
                        break;
                    case 2:
                        DrawHarddisk(context);
                        break;
                    case 3:
                        DrawFloppy(context);
                        break;
                    case 4:
                        DrawFolder(context);
                        break;
                    case 5:
                        DrawMonitor(context, withTaskbar: true);
                        break;
                    default:
                        return null;
                }
            }

            drawing.Freeze();
            DrawingImage image = new DrawingImage(drawing);
            image.Freeze();
            return image;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private static void DrawMonitor(DrawingContext context, bool withTaskbar)
        {
            context.DrawRoundedRectangle(null, IconPen, new Rect(1, 2, 16, 11), 1, 1);
            context.DrawLine(IconPen, new Point(9, 13), new Point(9, 16));
            context.DrawLine(IconPen, new Point(5, 16), new Point(13, 16));

            if (withTaskbar)
            {
                context.DrawRectangle(IconBrush, null, new Rect(0, 17, 18, 1));
            }
        }

        private static void DrawHarddisk(DrawingContext context)
        {
            context.DrawRoundedRectangle(null, IconPen, new Rect(1, 3, 16, 12), 1, 1);
            context.DrawLine(IconPen, new Point(1, 9), new Point(17, 9));
            context.DrawEllipse(IconBrush, null, new Point(13, 13), 1.2, 1.2);
        }

        private static void DrawFloppy(DrawingContext context)
        {
            context.DrawRoundedRectangle(null, IconPen, new Rect(2, 2, 14, 14), 1, 1);
            context.DrawRectangle(IconBrush, null, new Rect(6, 2, 6, 6));
            context.DrawRectangle(null, IconPen, new Rect(5, 10, 8, 5));
        }

        private static void DrawFolder(DrawingContext context)
        {
            StreamGeometry geometry = new StreamGeometry();
            using (StreamGeometryContext geometryContext = geometry.Open())
            {
                geometryContext.BeginFigure(new Point(1, 4), true, true);
                geometryContext.LineTo(new Point(7, 4), true, false);
                geometryContext.LineTo(new Point(9, 6), true, false);
                geometryContext.LineTo(new Point(17, 6), true, false);
                geometryContext.LineTo(new Point(17, 15), true, false);
                geometryContext.LineTo(new Point(1, 15), true, false);
            }

            geometry.Freeze();
            context.DrawGeometry(IconBrush, null, geometry);
        }

        private static Brush CreateFrozenBrush()
        {
            SolidColorBrush brush = new SolidColorBrush(Color.FromRgb(0x8F, 0xA0, 0xAB));
            brush.Freeze();
            return brush;
        }

        private static Pen CreateFrozenPen()
        {
            Pen pen = new Pen(IconBrush, 1.5);
            pen.Freeze();
            return pen;
        }
    }
}
