using System.Globalization;
using System.Windows.Data;
using System;

namespace FlatRedBall.Glue.Themes.Converters;

public class NotNullConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null; // Returns true if value is not null
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}