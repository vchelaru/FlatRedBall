using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TMXGlueLib;

namespace FlatRedBall.Glue.Controls
{
    /// <summary>
    /// Interaction logic for TilesetTileSelector.xaml
    /// </summary>
    public partial class TilesetTileSelector : UserControl
    {
        public PropertyListContainerViewModel ViewModel => DataContext as PropertyListContainerViewModel;

        #region ImageSource Property
        public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
            nameof(Source),
            typeof(ImageSource),
            typeof(TilesetTileSelector),
            new PropertyMetadata(null, SourcePropertyChanged));

        private static void SourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = d as TilesetTileSelector;
            if(d != null)
            {
                selector.Image.Source = selector.Source;
            }
        }

        public ImageSource Source
        {
            get => (ImageSource)GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        #endregion

        #region Entire Tileset Image Property

        public static readonly DependencyProperty EntireTilesetSourceProperty = DependencyProperty.Register(
            nameof(EntireTilesetSource),
            typeof(ImageSource),
            typeof(TilesetTileSelector),
            new PropertyMetadata(null, null));

        public ImageSource EntireTilesetSource
        {
            get => (ImageSource)GetValue(EntireTilesetSourceProperty);
            set => SetValue(SourceProperty, value);
        }

        #endregion

        #region Text Property

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(TilesetTileSelector),
            new PropertyMetadata(null, TextPropertyChanged));

        private static void TextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var selector = d as TilesetTileSelector;
            if(d != null)
            {
                selector.Label.Content = selector.Text;
            }
        }

        public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
        #endregion

        public Visibility TextVisibility
        {
            get => Label.Visibility;
            set => Label.Visibility = value;
        }

        #region Events

        public event Action<TilesetTileSelectorFullViewModel> NewTileSelected;

        #endregion

        public TilesetTileSelector()
        {
            InitializeComponent();
        }

        private void StackPanel_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var window = new Window();
            window.SizeToContent = SizeToContent.WidthAndHeight;

            var vm = new TilesetTileSelectorFullViewModel();
            var namedObject = ViewModel?.GlueObject as NamedObjectSave;
            vm.TileShapeCollectionName = namedObject?.InstanceName;
            if(namedObject != null)
            {
                var collisionCreationOptions = 
                    namedObject.Properties.GetValue("CollisionCreationOptions")?.ToString();
                var collisionType =
                    namedObject.Properties.GetValue("CollisionTileTypeName")?.ToString(); 

                if(collisionCreationOptions == "4" && !string.IsNullOrEmpty(collisionType))
                {
                    vm.ExistingId = GlueState.Self.TiledCache.GetTileIdFromType(collisionType);
                    if(vm.ExistingId != null)
                    {
                        vm.TileId = vm.ExistingId.Value;
                    }
                }
            }

            var content = new TilesetTileSelectorFull();
            content.OkClicked += () =>
            {
                window.DialogResult = true;
                HandleSetTileId(vm);
            };
            content.CancelClicked += () => window.DialogResult = false;
            content.PopupImage.Source = EntireTilesetSource;

            content.DataContext = vm;

            window.Content = content;
            window.Loaded += (not, used) => GlueCommands.Self.DialogCommands.MoveToCursor(window);
            window.ShowDialog();
        }

        private void HandleSetTileId(TilesetTileSelectorFullViewModel vm)
        {
            // We need to:
            // 1. Change the tileset
            var needsToSave = false;
            if(vm.IsNullingOutExisting)
            {
                var tileset = GlueState.Self.TiledCache.StandardTileset;
                var existingId = vm.ExistingId.Value;
                var tile = tileset.TileDictionary[(uint)existingId];

                tile.Type = null;
                needsToSave = true;
            }
            if(vm.WillSetNewTileType)
            {
                var tileset = GlueState.Self.TiledCache.StandardTileset;
                var newId = (uint)vm.TileId;

                mapTilesetTile newTile = null;
                if(tileset.TileDictionary.TryGetValue(newId, out var value))
                {
                    newTile = value;
                }
                if(newTile == null)
                {
                    newTile = new mapTilesetTile();
                    newTile.id = (int)newId;
                    tileset.Tiles.Add(newTile);
                    tileset.Tiles.Sort((a, b) => a.id.CompareTo(b.id));

                    tileset.TileDictionary.Add(newId, newTile);
                }
                newTile.Type = vm.TileShapeCollectionName;

                needsToSave = true;
            }

            // 2. Save the tileset
            if(needsToSave)
            {
                GlueState.Self.TiledCache.SaveStandardTileset();
            }

            // 3. Set the property on the underlying view model - this is done through an event
            //     - this should regen and save etc
            NewTileSelected?.Invoke(vm);
        }
    }
}
