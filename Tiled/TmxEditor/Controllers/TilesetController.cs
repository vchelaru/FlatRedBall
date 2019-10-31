using System;
using System.Linq;
using RenderingLibrary;
using System.Windows.Forms;
using TMXGlueLib;
using XnaAndWinforms;
using TmxEditor.PropertyGridDisplayers;
using TmxEditor.UI;
using FlatRedBall.Utilities;
using TmxEditor.Events;

namespace TmxEditor.Controllers
{
    public partial class TilesetController : ToolComponent<TilesetController>
    {

        public const string HasCollisionVariableName = "HasCollision";
        public const string EntityToCreatePropertyName = "EntityToCreate";

        #region Fields

        mapTilesetTile mCurrentTilesetTile;

        SystemManagers mManagers;

        CameraPanningLogic mCameraPanningLogic;

        InputLibrary.Cursor mCursor;
        InputLibrary.Keyboard mKeyboard;
        CheckBox mHasCollisionsCheckBox;
        ComboBox mEntitiesComboBox;
        TextBox mNameTextBox;

        Tileset mTempTileset;

        Label mInfoLabel;
        #endregion

        #region Properties

        Camera Camera
        {
            get
            {
                return mManagers.Renderer.Camera;
            }
        }

        public mapTilesetTile CurrentTilesetTile
        {
            get
            {
                return mCurrentTilesetTile;
            }
            set
            {
                mCurrentTilesetTile = value;


                RefreshUiToSelectedTile();
            }
        }

        public TilesetTileDisplayer Displayer
        {
            get
            {
                return mDisplayer;
            }
        }


        #endregion

        #region Events

        public event EventHandler AnyTileMapChange;
        public event EventHandler EntityAssociationsChanged;

        #endregion

        public void Initialize(GraphicsDeviceControl control, ListBox tilesetsListBox, 
            Label infoLabel, PropertyGrid propertyGrid, CheckBox hasCollisionsCheckBox, TextBox nameTextBox, ComboBox entitiesComboBox)
        {
            InitializePropertyGrid(propertyGrid);

            mHasCollisionsCheckBox = hasCollisionsCheckBox;
            mEntitiesComboBox = entitiesComboBox;
            

            mNameTextBox = nameTextBox;
            mNameTextBox.KeyDown += HandleNameTextBoxKeyDown;
            mNameTextBox.LostFocus += HandleLostFocus;

            mPropertyGrid = propertyGrid;
            mControl = control;
            ToolComponentManager.Self.Register(this);

            InitializeListBox(tilesetsListBox);

            ReactToLoadedFile = HandleLoadedFile;
            ReactToXnaInitialize = HandleXnaInitialize;
            
            ReactToWindowResize = HandleWindowResize;

            ReactToLoadedAndMergedProperties = HandleLoadedAndMergedProperties;

            control.XnaUpdate += new Action(HandleXnaUpdate);
            
            mInfoLabel = infoLabel;

            CurrentTilesetTile = null;
        }

        void HandleLostFocus(object sender, EventArgs e)
        {
            HandleNameChange();
        }

        void HandleNameTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                HandleNameChange();
                e.Handled = true;
            }
        }

        void HandleLoadedFile(string fileName)
        {
            RefreshAll();
        }

        private void RefreshAll()
        {
            mTilesetsListBox.Invoke((MethodInvoker)delegate
            {
                FillListBox();

                ClearAllHighlights();

                mSprite.Visible = false;
                mOutlineRectangle.Visible = false;
            });
        }

        void HandleLoadedAndMergedProperties(string fileName)
        {
            RefreshAll();

        }

        private void GetTopLeftWidthHeight(mapTilesetTile tile, out float left, out float top, out float width, out float height)
        {
            var currentTileset = mTilesetsListBox.SelectedItem as Tileset;

            int numberTilesWide = currentTileset.GetNumberOfTilesWide();


            // I think GID are global IDs for tiles in the map
            // but within a tile set the IDs start at 0
            //int index = (int)(tile.id - currentTileset.Firstgid);
            int index = tile.id;

            long xIndex = index % numberTilesWide;
            long yIndex = index / numberTilesWide;

            int leftAsInt;
            int topAsInt;
            currentTileset.IndexToCoordinate(xIndex, yIndex, out leftAsInt, out topAsInt);

            left = leftAsInt;
            top = topAsInt;
            width = currentTileset.Tilewidth;
            height = currentTileset.Tileheight;
        }

        internal void EntitiesComboBoxChanged(string entityToCreate)
        {
            ////////////Early Out//////////////////////
            if(CurrentTilesetTile == null)
            {
                return;
            }
            ///////////End Early Out///////////////////

            var tileset = AppState.Self.CurrentMapTilesetTile;

            var existingProperty = GetExistingProperty(EntityToCreatePropertyName, CurrentTilesetTile);

            bool changesOccurred = false;
            if (entityToCreate != "None" && existingProperty == null)
            {
                // New property added
                const bool raiseChangedEvent = false;
                existingProperty = AddProperty(CurrentTilesetTile, EntityToCreatePropertyName, "string",
                    raiseChangedEvent);
                existingProperty.value = entityToCreate;

                changesOccurred = true;

                if (GetExistingProperty("Name", CurrentTilesetTile) == null)
                {
                    AddRandomNameTo(CurrentTilesetTile);
                }
            }
            else if (entityToCreate != "None" && existingProperty != null)
            {
                // existingProperty is not null, so check if it changed
                if (existingProperty.value != entityToCreate)
                {
                    // Changed
                    changesOccurred = true;
                    existingProperty.value = entityToCreate;
                }
            }
            else if (entityToCreate == "None" && existingProperty != null)
            {
                // The property was removed
                CurrentTilesetTile.properties.Remove(existingProperty);

                UpdateXnaDisplayToTileset();
                changesOccurred = true;
            }
            

            if (changesOccurred)
            {
                RefreshUiToSelectedTile();

                if (EntityAssociationsChanged != null)
                {
                    EntityAssociationsChanged(this, null);
                }
            }

            if (changesOccurred && AnyTileMapChange != null)
            {
                var args = new TileMapChangeEventArgs();

                args.ChangeType = ChangeType.Tileset;

                AnyTileMapChange(this, args);
            }
        }

        internal void HasCollisionsCheckBoxChanged(bool hasCollisions)
        {
            // let's see if the property exists
            var tileset = AppState.Self.CurrentMapTilesetTile;

            var existingProperty = GetExistingProperty(HasCollisionVariableName, CurrentTilesetTile);

            bool changesOccurred = false;

            if (hasCollisions && existingProperty == null)
            {
                // We'll do it after setting the value
                const bool raiseChangedEvent = false;
                existingProperty = AddProperty(CurrentTilesetTile, HasCollisionVariableName, "bool", raiseChangedEvent);
                existingProperty.value = hasCollisions.ToString();
                changesOccurred = true;


                if (GetExistingProperty("Name", CurrentTilesetTile) == null)
                {
                    AddRandomNameTo(CurrentTilesetTile);
                }

            }
            else if (hasCollisions == false && existingProperty != null)
            {
                CurrentTilesetTile.properties.Remove(existingProperty);

                UpdateXnaDisplayToTileset();
                changesOccurred = true;
            }


            if (changesOccurred)
            {
                RefreshUiToSelectedTile();
            }

            if (changesOccurred && AnyTileMapChange != null)
            {
                var args = new TileMapChangeEventArgs();

                args.ChangeType = ChangeType.Tileset;

                AnyTileMapChange(this, args);
            }
        }

        private void AddRandomNameTo(mapTilesetTile tile)
        {
            string value = "Unnamed1";

            while (GetTilesetTileByName(value) != null)
            {
                value = StringFunctions.IncrementNumberAtEnd(value);
            }

            AddProperty(tile, "Name", "string", false).value = value;
        }

        private object GetTilesetTileByName(string value)
        {
            foreach (var tileset in AppState.Self.CurrentTiledMapSave.Tilesets)
            {
                foreach (var tile in tileset.Tiles)
                {
                    foreach (var property in tile.properties)
                    {
                        if (property.GetStrippedName(property.name).ToLower() == "name" &&
                            property.value == value)
                        {
                            return tile;
                        }

                    }
                }


            }

            return null;
        }


        private void HandleNameChange()
        {
            var tileset = AppState.Self.CurrentMapTilesetTile;

            if (tileset != null)
            {

                var existingProperty = GetExistingProperty("Name", CurrentTilesetTile);

                bool changesOccurred = false;

                bool hasNameEnteredName = string.IsNullOrEmpty(mNameTextBox.Text) == false;

                if (hasNameEnteredName)
                {
                    if (existingProperty == null)
                    {
                        // There's no property for Name, so let's add it...
                        const bool raiseChangedEvent = false;
                        existingProperty = AddProperty(CurrentTilesetTile, "Name", "string", raiseChangedEvent);
                    }

                    // let's see if they've modified it, for performance reasons:
                    if (existingProperty.value != mNameTextBox.Text)
                    {
                        // ...and now that we've added, let's modify it:
                        existingProperty.value = mNameTextBox.Text;
                        changesOccurred = true;
                    }
                }
                else if (hasNameEnteredName == false && existingProperty != null)
                {
                    CurrentTilesetTile.properties.Remove(existingProperty);

                    UpdateXnaDisplayToTileset();
                    changesOccurred = true;
                }


                if (changesOccurred && AnyTileMapChange != null)
                {
                    TileMapChangeEventArgs args = new TileMapChangeEventArgs();
                    args.ChangeType = ChangeType.Tileset;
                    AnyTileMapChange(this, args);
                }
            }
        }

        private void RefreshUiToSelectedTile()
        {
            mDisplayer.Instance = mCurrentTilesetTile;
            mDisplayer.PropertyGrid.Refresh();

            UpdateHighlightRectangle();

            bool hasSelectedTileset = mCurrentTilesetTile != null;

            mHasCollisionsCheckBox.Enabled = hasSelectedTileset;
            mNameTextBox.Enabled = hasSelectedTileset;
            mEntitiesComboBox.Enabled = hasSelectedTileset;

            if (hasSelectedTileset)
            {
                Func<property, bool> predicate = item => property.GetStrippedName( item.name ) == TilesetController.HasCollisionVariableName;

                mHasCollisionsCheckBox.Checked =
                    mCurrentTilesetTile.properties.Any(predicate) &&
                    mCurrentTilesetTile.properties.First(predicate).value.ToLowerInvariant() == "true";

                var entityProperty =
                    mCurrentTilesetTile.properties.FirstOrDefault(item => property.GetStrippedName(item.name) == TilesetController.EntityToCreatePropertyName);
                if (entityProperty != null)
                {
                    foreach (var item in mEntitiesComboBox.Items)
                    {
                        var value = item as string;
                        if (value == entityProperty.value)
                        {
                            mEntitiesComboBox.SelectedItem = item;
                            break;
                        }
                    }
                }
                else
                {
                    mEntitiesComboBox.SelectedItem = "None";
                }

                var nameProperty = mCurrentTilesetTile.properties.FirstOrDefault(item => property.GetStrippedName(item.name) == "Name");

                if (nameProperty != null)
                {
                    mNameTextBox.Text = nameProperty.value;
                }
                else
                {
                    mNameTextBox.Text = "";
                }
            }
            else
            {
                mHasCollisionsCheckBox.Checked = false;
                mNameTextBox.Text = "";
                mEntitiesComboBox.SelectedItem = "None";
            }

            
        }

        
    }
}
