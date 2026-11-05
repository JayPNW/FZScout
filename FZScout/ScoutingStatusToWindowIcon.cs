using System.Globalization;
using System.Windows.Data;

namespace FZScout;

class ScoutingStatusToWindowIcon : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => WindowIcon.For((ScoutingStatus)value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
