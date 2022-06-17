using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;
using WpfDataUi;
using WpfDataUi.Controls;

namespace OfficialPlugins.Common.Controls
{
    class ColorHexTextBox : TextBoxDisplay
    {
        Border border;
        public ColorHexTextBox() : base()
        {
            border = new Border();
            border.CornerRadius = new System.Windows.CornerRadius(4);
            border.Background = Brushes.Red;
            border.BorderBrush = Brushes.Black;
            border.BorderThickness = new System.Windows.Thickness(1);
            border.Width = 15;
            border.Height = 15;
            border.Margin = new System.Windows.Thickness(3, 0, 3, 0);
            border.VerticalAlignment = System.Windows.VerticalAlignment.Center;
            AddUiAfterTextBox(border);
            // <Border BorderThickness="1" Grid.Row="0" Grid.ColumnSpan="2"
            // CornerRadius = "50,50,0,0" BorderBrush = "Black" Background = "#FF5A9AE0" >
            // </ Border >
        }

        public override ApplyValueResult TrySetValueOnUi(object valueOnInstance)
        {
            var result = base.TrySetValueOnUi(valueOnInstance);

            // todo parse color
            var newValueAsString = valueOnInstance as string;
            if (!string.IsNullOrEmpty(newValueAsString))
            {
                if (!newValueAsString.StartsWith("#"))
                {
                    newValueAsString = "#" + newValueAsString;
                }
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(newValueAsString);
                    var backgroundBrush = new SolidColorBrush(color);
                    border.Background = backgroundBrush;

                }
                catch
                {

                }
            }
            return result;
        }
    }
}
