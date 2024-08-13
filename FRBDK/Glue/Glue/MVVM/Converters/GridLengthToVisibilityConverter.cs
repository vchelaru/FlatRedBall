using System.Globalization;
using System.Windows.Data;
using System.Windows;
using System;

namespace FlatRedBall.Glue.MVVM.Converters;

public class GridLengthToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is GridLength gridLength)
        {
            return gridLength.Value == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("GridLengthToVisibilityConverter does not support ConvertBack.");
    }
}