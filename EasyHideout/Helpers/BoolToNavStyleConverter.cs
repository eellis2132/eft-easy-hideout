using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EasyHideout.Helpers;

public class BoolToNavStyleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isActive = value is bool b && b;
        var app = Application.Current;
        return isActive
            ? app.FindResource("NavButtonActive")
            : app.FindResource("NavButton");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
