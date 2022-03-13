using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.Compiler.CommandSending;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TMXGlueLib;

namespace OfficialPlugins.Compiler.Views
{
    /// <summary>
    /// Interaction logic for EditingToolsView.xaml
    /// </summary>
    public partial class EditingToolsView : UserControl
    {
        #region Properties

        Dictionary<int, CroppedBitmap> CroppedBitmaps = new Dictionary<int, CroppedBitmap>();

        List<ToggleButton> tileToggleButtons = new List<ToggleButton>();

        #endregion

        public EditingToolsView()
        {
            InitializeComponent();
        }

        public void HandleGluxUnloaded()
        {
            TileButtonsStack.Children.Clear();
        }

        public void HandleGluxLoaded()
        {
            //var filePath = asdf;
            var tilesetImage = GlueState.Self.TiledCache.StandardTilesetImage;
            var tileset = GlueState.Self.TiledCache.StandardTileset;
            if (tilesetImage != null)
            {
                foreach(var tile in tileset.Tiles)
                {
                    if(!string.IsNullOrEmpty(tile.Type))
                    {
                        var button = CreateButtonForTilesetTile(tile, tilesetImage);
                        button.Visibility = Visibility.Collapsed;
                    }
                }
            }

        }

        private ToggleButton CreateButtonForTilesetTile(mapTilesetTile tile, BitmapImage standardTilesetImage)
        {
            CroppedBitmap croppedBitmap = GlueState.Self.TiledCache.GetBitmapForStandardTilesetId(tile.id);

            var button = new ToggleButton();
            button.Tag = tile;
            var innerImage = new Image();
            innerImage.Width = 16;
            innerImage.Height = 16;
            innerImage.Margin = new Thickness(2);
            innerImage.Source = croppedBitmap;

            button.Content = innerImage;
            button.Click += HandleToggleButtonClicked;
            button.ToolTip = tile.Type;
            button.Width = 24;
            tileToggleButtons.Add(button);
            TileButtonsStack.Children.Add(button);

            return button;
        }

        private async void HandleToggleButtonClicked(object sender, RoutedEventArgs e)
        {
            var button = sender as ToggleButton;
            if(button.IsChecked == true)
            {
                SelectToggleButton.IsChecked = false;
                // uncheck everything else
                foreach (var otherButton in tileToggleButtons)
                {
                    if(otherButton != button)
                    {
                        otherButton.IsChecked = false;
                    }
                }

                var currentScreen = GlueState.Self.CurrentScreenSave;

                NamedObjectSave nosForButton = null;

                if(currentScreen == null)
                {
                    currentScreen = await CommandSender.GetCurrentInGameScreen();
                }
                if (currentScreen != null)
                {
                    nosForButton = GetNosForButton(button, currentScreen);
                }

                if (nosForButton != null)
                {
                    GlueState.Self.CurrentNamedObjectSave = nosForButton;
                }
            }
            else
            {
                SelectToggleButton.IsChecked = true;

                var currentScreen = GlueState.Self.CurrentScreenSave;

                if(currentScreen != null)
                {
                    GlueState.Self.CurrentScreenSave = currentScreen;
                }
            }
        }

        private static NamedObjectSave GetNosForButton(ToggleButton button, ScreenSave currentScreen)
        {
            return currentScreen.NamedObjects.FirstOrDefault(item =>
                {
                    if (IsTileShapeCollection(item))
                    {
                        var properties = item.Properties;
                        var collisionCreationOptions = properties.GetValue("CollisionCreationOptions");
                        var type = properties.GetValue<string>("CollisionTileTypeName");
                        if (collisionCreationOptions?.ToString() == "FromType" || collisionCreationOptions?.ToString() == "4")
                        {
                            var tilesetTile = button.Tag as mapTilesetTile;
                            return tilesetTile.Type == type;

                        }
                    }
                    return false;
                });
        }

        static bool IsTileShapeCollection(NamedObjectSave nos) => nos?.GetAssetTypeInfo()?.FriendlyName == "TileShapeCollection";

        private void SelectObjectsToggleClicked(object sender, RoutedEventArgs e)
        {
            var existingCheckedButton = tileToggleButtons.FirstOrDefault(item => item.IsChecked == true);
            
            NamedObjectSave tileShapeCollectionNos = null;
            var currentScreen = GlueState.Self.CurrentScreenSave;
            if (existingCheckedButton != null)
            {
                existingCheckedButton.IsChecked = false;
                if(currentScreen != null)
                {
                    tileShapeCollectionNos = GetNosForButton(existingCheckedButton, currentScreen);
                }
            }

            if(tileShapeCollectionNos == null && IsTileShapeCollection( GlueState.Self.CurrentNamedObjectSave ))
            {
                tileShapeCollectionNos = GlueState.Self.CurrentNamedObjectSave;
            }
            
            if(tileShapeCollectionNos != null)
            {
                GlueState.Self.CurrentScreenSave = currentScreen;
            }


        }

        public void UpdateToItemSelected()
        {
            var currentScreen = GlueState.Self.CurrentScreenSave;
            var currentNos = GlueState.Self.CurrentNamedObjectSave;


            if (currentNos == null || IsTileShapeCollection(currentNos) == false)
            {
                foreach (var button in tileToggleButtons)
                {
                    button.IsChecked = false;
                }
                SelectToggleButton.IsChecked = true;
            }
            else
            {
                SelectToggleButton.IsChecked = false;
            }


            foreach (var button in tileToggleButtons)
            {
                NamedObjectSave matchingNos = null;
                if(currentScreen != null)
                {
                    matchingNos = GetNosForButton(button, currentScreen);
                }

                button.Visibility = (matchingNos != null).ToVisibility();
                button.IsChecked = matchingNos != null && matchingNos == currentNos;
            }
        }
    }
}
