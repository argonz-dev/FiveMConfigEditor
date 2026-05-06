using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace FiveMConfigEditorWPF.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool active = value is bool b && b;
            return active
                ? new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71))
                : new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
