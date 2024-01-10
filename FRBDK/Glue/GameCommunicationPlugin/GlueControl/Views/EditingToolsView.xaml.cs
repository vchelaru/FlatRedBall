using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GameCommunicationPlugin.GlueControl.CommandSending;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using TMXGlueLib;

namespace GameCommunicationPlugin.GlueControl.Views
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

            List<mapTilesetTile> tilesWithType = GetTilesWithType(tilesetImage, tileset);

            foreach (var tile in tilesWithType)
            {
                var button = CreateButtonForTilesetTile(tile, tilesetImage);
                button.Visibility = Visibility.Collapsed;
            }

        }

        private static List<mapTilesetTile> GetTilesWithType(BitmapImage tilesetImage, Tileset tileset)
        {
            List<mapTilesetTile> tilesWithType = new List<mapTilesetTile>();

            if (tilesetImage != null)
            {
                foreach (var tile in tileset.Tiles)
                {
                    if (!string.IsNullOrEmpty(tile.Type))
                    {
                        tilesWithType.Add(tile);
                    }
                }
            }

            return tilesWithType;
        }

        private ToggleButton CreateButtonForTilesetTile(mapTilesetTile tile, BitmapImage standardTilesetImage)
        {
            CroppedBitmap croppedBitmap = GlueState.Self.TiledCache.GetBitmapForStandardTilesetId(tile.id, tile.Type);

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

                var currentElement = GlueState.Self.CurrentElement;

                NamedObjectSave nosForButton = null;

                if(currentElement == null)
                {
                    currentElement = await CommandSender.Self.GetCurrentInGameScreen();
                }
                if (currentElement != null)
                {
                    nosForButton = GetNosForButton(button, currentElement);
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

        private static NamedObjectSave GetNosForButton(ToggleButton button, GlueElement currentElement)
        {
            return currentElement.NamedObjects.FirstOrDefault(item =>
                {
                    if (IsTileShapeCollection(item))
                    {
                        var properties = item.Properties;
                        var collisionCreationOptions = properties.GetValue("CollisionCreationOptions");
                        var type = properties.GetValue<string>("CollisionTileTypeName");
                        var tmxCollisionName = properties.GetValue<string>("TmxCollisionName");

                        if (collisionCreationOptions?.ToString() == "FromType" || 
                            // FromType = 4
                            collisionCreationOptions?.ToString() == "4" ||
                            // FromMapCollision = 6
                            collisionCreationOptions?.ToString() == "6" 
                            )
                        {
                            var tilesetTile = button.Tag as mapTilesetTile;

                            if(!string.IsNullOrEmpty(type))
                            {
                                return tilesetTile.Type == type;
                            }
                            else
                            {
                                return tilesetTile.Type == tmxCollisionName;
                            }
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
            var currentElement = GlueState.Self.CurrentElement;
            if (existingCheckedButton != null)
            {
                existingCheckedButton.IsChecked = false;
                if(currentElement != null)
                {
                    tileShapeCollectionNos = GetNosForButton(existingCheckedButton, currentElement);
                }
            }

            if(tileShapeCollectionNos == null && IsTileShapeCollection( GlueState.Self.CurrentNamedObjectSave ))
            {
                tileShapeCollectionNos = GlueState.Self.CurrentNamedObjectSave;
            }
            
            if(tileShapeCollectionNos != null)
            {
                GlueState.Self.CurrentElement = currentElement;
            }


        }

        public void UpdateToItemSelected()
        {
            var currentElement = GlueState.Self.CurrentElement;
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
                if(currentElement != null)
                {
                    matchingNos = GetNosForButton(button, currentElement);
                }

                button.Visibility = (matchingNos != null).ToVisibility();
                button.IsChecked = matchingNos != null && matchingNos == currentNos;
            }
        }

        // This is a pain in the butt to code properly so I'm going to save it for another day
        //private void Button_DragLeave(object sender, DragEventArgs e)
        //{
        //    var viewModel = (sender as Button).DataContext as ToolbarEntityAndStateViewModel;
        //    viewModel.HandleDragLeave();
        //}

        //private void Button_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if (e.LeftButton == MouseButtonState.Pressed)
        //    {
        //        var viewModel = (sender as Button).DataContext as ToolbarEntityAndStateViewModel;
        //        viewModel.HandleDragLeave();

        //    }
        //}
    }
}
