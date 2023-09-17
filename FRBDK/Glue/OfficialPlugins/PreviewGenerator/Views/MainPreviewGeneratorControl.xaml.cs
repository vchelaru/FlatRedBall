using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.PreviewGenerator.Managers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace OfficialPlugins.PreviewGenerator.Views
{
    /// <summary>
    /// Interaction logic for MainPreviewGeneratorControl.xaml
    /// </summary>
    public partial class MainPreviewGeneratorControl : UserControl
    {
        private NamedObjectSave NamedObjectSave { get; set; }
        private GlueElement GlueElement { get; set; }
        private StateSave StateSave { get; set; }

        public MainPreviewGeneratorControl()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NamedObjectSave = GlueState.Self.CurrentNamedObjectSave;
            GlueElement = GlueState.Self.CurrentElement;
            StateSave = GlueState.Self.CurrentStateSave;

            var imageSource = PreviewGenerationLogic.GetImageSourceForSelection(NamedObjectSave, GlueElement, StateSave);
            this.Image.Source = imageSource;
        }

        private void SaveItClicked(object sender, RoutedEventArgs e)
        {
            PreviewSaver.SavePreview(Image.Source as BitmapSource, GlueElement, StateSave);
        }
    }
}
