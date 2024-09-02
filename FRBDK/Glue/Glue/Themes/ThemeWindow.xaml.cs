using Glue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using GlueFormsCore.Controls;
using FlatRedBall.Glue.MVVM;

namespace FlatRedBall.Glue.Themes
{
    /// <summary>
    /// Interaction logic for ThemeWindow.xaml
    /// </summary>
    public partial class ThemeWindow : Window
    {
        public ThemeWindow()
        {
            InitializeComponent();
        }

        private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class ThemeWindowViewModel : ViewModel
    {
        public ThemeWindowViewModel()
        {
            PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(CurrentMode) && CurrentMode is { } mode)
                {
                    MainPanelControl.SwitchThemes(mode);
                }
                else if (args.PropertyName == nameof(CurrentAccent) && CurrentAccent is { } accent)
                {
                    MainPanelControl.SwitchThemes(null, accent.Color);
                }
            };
        }

        public ThemeMode CurrentMode
        {
            get => Get<ThemeMode>();
            set => Set(value);
        }

        public SolidColorBrush CurrentAccent
        {
            get => Get<SolidColorBrush>();
            set => Set(value);
        }

        public string[] ThemeModes { get; } = Enum.GetNames<ThemeMode>();
        public List<SolidColorBrush> AccentOptions { get; } = new ()
        {
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F44336")), // Red
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E91E63")), // Pink
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9C27B0")), // Purple
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#673AB7")), // Deep Purple
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3F51B5")), // Indigo
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2196F3")), // Blue
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#03A9F4")), // Light Blue
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00BCD4")), // Cyan
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#009688")), // Teal
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#4CAF50")), // Green
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8BC34A")), // Light Green
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CDDC39")), // Lime
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFEB3B")), // Yellow
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFC107")), // Amber
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF9800")), // Orange
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF5722")), // Deep Orange
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#795548")), // Brown
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#9E9E9E")), // Grey
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#607D8B"))  // Blue Grey
        };
    }

    public enum ThemeMode
    {
        Light,
        Dark
    }
}
