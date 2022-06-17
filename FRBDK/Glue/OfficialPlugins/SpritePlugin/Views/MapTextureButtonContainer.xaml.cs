using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
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
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace OfficialPlugins.SpritePlugin.Views
{
    /// <summary>
    /// Interaction logic for MapTextureButtonContainer.xaml
    /// </summary>
    public partial class MapTextureButtonContainer : UserControl, IDataUi
    {
        public MapTextureButtonContainer()
        {
            InitializeComponent();
        }

        public InstanceMember InstanceMember { get; set; }
        public bool SuppressSettingProperty { get; set; }

        public void Refresh(bool forceRefreshEvenIfFocused = false)
        {

        }

        public ApplyValueResult TryGetValueOnUi(out object result)
        {
            result = null;
            return ApplyValueResult.Success;
        }

        public ApplyValueResult TrySetValueOnUi(object value)
        {
            return ApplyValueResult.Success;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            var currentNos = GlueState.Self.CurrentNamedObjectSave;
            var currentElement = GlueState.Self.CurrentElement;

            ReferencedFileSave textureRfs = null;

            if(currentNos != null && currentElement != null)
            {
                var textureValue = ObjectFinder.GetValueRecursively(currentNos, currentElement, "Texture") as string;

                if(textureValue != null)
                {
                    textureRfs = currentElement.GetReferencedFileSaveRecursively(textureValue);
                }

            }

            if(textureRfs != null)
            { 
                var fullFile = GlueCommands.Self.FileCommands.GetFilePath(textureRfs);

                var window = new TextureCoordinateSelectionWindow();
                window.TextureFilePath = fullFile;
                var result = window.ShowDialog();

            }
        }
    }
}
