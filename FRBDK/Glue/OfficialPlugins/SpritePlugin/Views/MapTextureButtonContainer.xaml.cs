using FlatRedBall;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.SpritePlugin.ViewModels;
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
                var viewModel = new TextureCoordinateSelectionViewModel();

                var left = ObjectFinder.GetValueRecursively(currentNos, currentElement,
                    nameof(Sprite.LeftTexturePixel)) as float? ?? 0;

                var top = ObjectFinder.GetValueRecursively(currentNos, currentElement,
                    nameof(Sprite.TopTexturePixel)) as float? ?? 0;

                float defaultWidth = 256;
                float defaultHeight = 256;
                if(window.Texture != null)
                {
                    defaultWidth = window.Texture.Width;
                    defaultHeight = window.Texture.Height;
                }

                var right = ObjectFinder.GetValueRecursively(currentNos, currentElement,
                    nameof(Sprite.RightTexturePixel)) as float? ?? defaultWidth;

                if(right == 0)
                {
                    right = defaultWidth;
                }

                var bottom = ObjectFinder.GetValueRecursively(currentNos, currentElement,
                    nameof(Sprite.BottomTexturePixel)) as float? ?? defaultHeight;

                if(bottom == 0)
                {
                    bottom = defaultHeight;
                }

                viewModel.LeftTexturePixel = (int)left;
                viewModel.TopTexturePixel = (int)top;
                viewModel.SelectedWidthPixels = (int)(right - left);
                viewModel.SelectedHeightPixels = (int)(bottom - top);

                window.DataContext = viewModel;
                var result = window.ShowDialog();

                if(result == true)
                {
                    bool didAnyChange = false;
                    if(viewModel.LeftTexturePixel != (int)left)
                    {
                        GlueCommands.Self.GluxCommands.SetVariableOn(currentNos, 
                            nameof(Sprite.LeftTexturePixel), 
                            (float)viewModel.LeftTexturePixel, 
                            performSaveAndGenerateCode:false, updateUi:true);
                        didAnyChange = true;
                    }
                    if(viewModel.TopTexturePixel != (int)top)
                    {
                        GlueCommands.Self.GluxCommands.SetVariableOn(currentNos, 
                            nameof(Sprite.TopTexturePixel), 
                            (float)viewModel.TopTexturePixel, 
                            performSaveAndGenerateCode: false, updateUi: true);
                        didAnyChange = true;
                    }
                    if (viewModel.SelectedWidthPixels != (int)(right - left))
                    {
                        GlueCommands.Self.GluxCommands.SetVariableOn(currentNos, 
                            nameof(Sprite.RightTexturePixel), 
                            (float)(viewModel.LeftTexturePixel + viewModel.SelectedWidthPixels),
                            performSaveAndGenerateCode: false, updateUi: true);
                        didAnyChange = true;
                    }
                    if (viewModel.SelectedHeightPixels != (int)(bottom - top))
                    {
                        GlueCommands.Self.GluxCommands.SetVariableOn(currentNos,
                            nameof(Sprite.BottomTexturePixel),
                            (float)(viewModel.TopTexturePixel + viewModel.SelectedHeightPixels), 
                            performSaveAndGenerateCode: false, updateUi: true);
                        didAnyChange = true;
                    }

                    if(didAnyChange)
                    {
                        GlueCommands.Self.GluxCommands.SaveGlux();
                        GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(currentElement);
                    }
                }
            }
        }
    }
}
