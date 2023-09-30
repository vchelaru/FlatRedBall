using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;


namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.LoadRecentFilesPlugin.Converters;

public class WidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        double actualWidth = (double)value;
        double padding = 40; // Replace this with the actual padding value
        return actualWidth - padding;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
