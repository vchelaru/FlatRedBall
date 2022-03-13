using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.PreviewGenerator.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace OfficialPlugins.PreviewGenerator.Views
{
    /// <summary>
    /// Interaction logic for MainPreviewGeneratorControl.xaml
    /// </summary>
    public partial class MainPreviewGeneratorControl : UserControl
    {
        NamedObjectSave NamedObjectSave { get; set; }
        GlueElement GlueElement { get; set; }
        StateSave StateSave { get; set; }

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
