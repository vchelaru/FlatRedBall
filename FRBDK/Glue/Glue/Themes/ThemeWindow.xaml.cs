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
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System.Windows.Threading;

#nullable enable
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

        protected override void OnClosed(EventArgs e)
        {
            if (DataContext is ThemeWindowViewModel viewModel)
            {
                viewModel.SaveConfig();
            }
            base.OnClosed(e);
        }
    }

    public class ThemeWindowViewModel : ViewModel
    {
        public ThemeWindowViewModel()
        {


            if (GlueState.Self.GlueSettingsSave.ThemeConfig is { } savedConfig)
            {

                CurrentAccent = savedConfig.Accent is { } accent ? 
                    AccentOptions.FirstOrDefault(x => x.Color == accent) ?? new(accent) : null;
                CurrentMode = savedConfig.Mode ?? default;
            }

            PropertyChanged += (_, args) =>
            {
                ThemeConfig? config = args.PropertyName switch
                {
                    nameof(CurrentMode) => new(CurrentMode, null),
                    nameof(CurrentAccent) => new(null, CurrentAccent?.Color),
                    _ => null
                };

                if (config is not null)
                {
                    MainPanelControl.Self.SwitchThemes(config);
                }
                
            };
        }

        public ThemeMode CurrentMode
        {
            get => Get<ThemeMode>();
            set => Set(value);
        }

        public SolidColorBrush? CurrentAccent
        {
            get => Get<SolidColorBrush>();
            set => Set(value);
        }

        public Array ThemeModes { get; } = Enum.GetValues(typeof(ThemeMode));
        public List<SolidColorBrush> AccentOptions { get; } = new ()
        {
            new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3E9ECE")), // Default FRB Blue
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

        public void SaveConfig()
        {
             GlueState.Self.GlueSettingsSave.ThemeConfig = new(CurrentMode, CurrentAccent?.Color);
             GlueState.Self.GlueSettingsSave.Save();
        }
    }

    public enum ThemeMode
    {
        Light,
        Dark
    }

    public record ThemeConfig(ThemeMode? Mode = null, Color? Accent = null)
    {
        public ThemeConfig() : this(null, null) { }
    }
}
#nullable disable
