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
        static TextureCoordinateSelectionViewModel LastViewModel;
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

            ReferencedFileSave textureRfs = GetTextureReferencedFileSave(currentNos, currentElement);

            if (textureRfs != null)
            {
                TextureCoordinateSelectionWindow window;
                TextureCoordinateSelectionViewModel viewModel;
                float left, top, right, bottom;

                window = new TextureCoordinateSelectionWindow();
                var fullFile = GlueCommands.Self.FileCommands.GetFilePath(textureRfs);
                window.TextureFilePath = fullFile;

                viewModel = GetNewViewModel(currentNos, currentElement, window, out left, out top, out right, out bottom);

                if(LastViewModel != null)
                {
                    viewModel.Snapping = LastViewModel.Snapping;
                    viewModel.WindowX = LastViewModel.WindowX;
                    viewModel.WindowY = LastViewModel.WindowY;
                    viewModel.WindowWidth = LastViewModel.WindowWidth;
                    viewModel.WindowHeight = LastViewModel.WindowHeight;
                }
                else
                {
                    viewModel.WindowWidth = 400;
                    viewModel.WindowHeight = 400;
                }

                window.DataContext = viewModel;
                var result = window.ShowDialog();

                if (result == true)
                {
                    ApplyViewModel(currentNos, currentElement, viewModel, left, top, right, bottom);
                }

                LastViewModel = viewModel;
            }
        }

        private static TextureCoordinateSelectionViewModel GetNewViewModel(NamedObjectSave currentNos, GlueElement currentElement, 
            TextureCoordinateSelectionWindow window, out float left, out float top, out float right, out float bottom)
        {
            var viewModel = new TextureCoordinateSelectionViewModel();
            left = ObjectFinder.Self.GetValueRecursively(currentNos, currentElement,
                nameof(Sprite.LeftTexturePixel)) as float? ?? 0;
            top = ObjectFinder.Self.GetValueRecursively(currentNos, currentElement,
                nameof(Sprite.TopTexturePixel)) as float? ?? 0;
            float defaultWidth = 256;
            float defaultHeight = 256;
            if (window.Texture != null)
            {
                defaultWidth = window.Texture.Width;
                defaultHeight = window.Texture.Height;
            }

            right = ObjectFinder.Self.GetValueRecursively(currentNos, currentElement,
                nameof(Sprite.RightTexturePixel)) as float? ?? defaultWidth;
            if (right == 0)
            {
                right = defaultWidth;
            }

            bottom = ObjectFinder.Self.GetValueRecursively(currentNos, currentElement,
                nameof(Sprite.BottomTexturePixel)) as float? ?? defaultHeight;
            if (bottom == 0)
            {
                bottom = defaultHeight;
            }

            viewModel.LeftTexturePixel = (int)left;
            viewModel.TopTexturePixel = (int)top;
            viewModel.SelectedWidthPixels = (int)(right - left);
            viewModel.SelectedHeightPixels = (int)(bottom - top);
            return viewModel;
        }

        private static ReferencedFileSave GetTextureReferencedFileSave(NamedObjectSave currentNos, GlueElement currentElement)
        {
            ReferencedFileSave textureRfs = null;

            if (currentNos != null && currentElement != null)
            {
                var textureValue = ObjectFinder.Self.GetValueRecursively(currentNos, currentElement, "Texture") as string;

                if (textureValue != null)
                {
                    textureRfs = currentElement.GetReferencedFileSaveRecursively(textureValue);
                }

            }

            return textureRfs;
        }

        private static void ApplyViewModel(NamedObjectSave currentNos, GlueElement currentElement, TextureCoordinateSelectionViewModel viewModel, 
            float oldLeft, float oldTop, float oldRight, float oldBottom)
        {
            bool didAnyChange = false;
            if (viewModel.LeftTexturePixelInt != (int)oldLeft)
            {
                GlueCommands.Self.GluxCommands.SetVariableOn(currentNos,
                    nameof(Sprite.LeftTexturePixel),
                    (float)viewModel.LeftTexturePixelInt,
                    performSaveAndGenerateCode: false, updateUi: true);
                didAnyChange = true;
            }
            if (viewModel.TopTexturePixelInt != (int)oldTop)
            {
                GlueCommands.Self.GluxCommands.SetVariableOn(currentNos,
                    nameof(Sprite.TopTexturePixel),
                    (float)viewModel.TopTexturePixelInt,
                    performSaveAndGenerateCode: false, updateUi: true);
                didAnyChange = true;
            }
            if (viewModel.SelectedWidthPixelsInt != (int)(oldRight - oldLeft))
            {
                GlueCommands.Self.GluxCommands.SetVariableOn(currentNos,
                    nameof(Sprite.RightTexturePixel),
                    (float)(viewModel.LeftTexturePixelInt + viewModel.SelectedWidthPixelsInt),
                    performSaveAndGenerateCode: false, updateUi: true);
                didAnyChange = true;
            }
            if (viewModel.SelectedHeightPixelsInt != (int)(oldBottom - oldTop))
            {
                GlueCommands.Self.GluxCommands.SetVariableOn(currentNos,
                    nameof(Sprite.BottomTexturePixel),
                    (float)(viewModel.TopTexturePixelInt + viewModel.SelectedHeightPixelsInt),
                    performSaveAndGenerateCode: false, updateUi: true);
                didAnyChange = true;
            }

            if (didAnyChange)
            {
                GlueCommands.Self.GluxCommands.SaveGlux();
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(currentElement);
            }
        }
    }
}
