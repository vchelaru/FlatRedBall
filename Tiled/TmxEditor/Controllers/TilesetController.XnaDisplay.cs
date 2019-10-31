using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.SpecializedXnaControls;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using InputLibrary;
using TMXGlueLib;
using RenderingLibrary.Content;
using RenderingLibrary.Math.Geometry;
using TmxEditor.GraphicalDisplay.Tilesets;
using XnaAndWinforms;
using System.Windows.Forms;
using TmxEditor.Managers;
using TmxEditor.CommandsAndState;
using Microsoft.Xna.Framework.Graphics;

namespace TmxEditor.Controllers
{
    public partial class TilesetController
    {

        #region Fields

        GraphicsDeviceControl mControl;

        List<TilePropertyHighlight> mTilesWithPropertiesMarkers = new List<TilePropertyHighlight>();

        LineRectangle mOutlineRectangle;
        LineRectangle mHighlightRectangle;
        Sprite mSprite;

        #endregion


        #region Properties

        public Texture2D CurrentTexture
        {
            get
            {
                if(mSprite == null || mSprite.Texture == null)
                {
                    return null;
                }
                else
                {
                    return mSprite.Texture;
                }
            }
        }


        #endregion

        public void HandleXnaInitialize(SystemManagers managers)
        {
            mManagers = managers;

            mSprite = new Sprite(null);
            mSprite.Visible = false;
            mSprite.Name = "TilesetController.XnaDisplay MainSprite";
            mManagers.SpriteManager.Add(mSprite);

            mOutlineRectangle = new RenderingLibrary.Math.Geometry.LineRectangle(mManagers);
            mOutlineRectangle.Visible = false;
            mManagers.ShapeManager.Add(mOutlineRectangle);
            mOutlineRectangle.Color = new Microsoft.Xna.Framework.Color(
                1.0f, 1.0f, 1.0f, .5f);

            mHighlightRectangle = new RenderingLibrary.Math.Geometry.LineRectangle(mManagers);
            mHighlightRectangle.Visible = false;
            mManagers.ShapeManager.Add(mHighlightRectangle);
            mHighlightRectangle.Color = new Microsoft.Xna.Framework.Color(
                1.0f, 1.0f, 1.0f, 1.0f);
            

            HandleWindowResize();
            mCursor = new InputLibrary.Cursor();
            mCursor.Initialize(mControl);
            mCameraPanningLogic = new CameraPanningLogic(mControl, managers, mCursor, mKeyboard);
            mCameraPanningLogic.Panning += delegate
            {
                ApplicationEvents.Self.CallAfterWireframePanning();
            };

            mManagers.Renderer.Camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;
            mManagers.Renderer.SamplerState = Microsoft.Xna.Framework.Graphics.SamplerState.PointClamp;

            mControl.KeyDown += HandleXnaControlKeyDown;
            mControl.KeyPress += HandleXnaControlKeyPress;
        }

        private void HandleXnaControlKeyPress(object sender, KeyPressEventArgs e)
        {

            #region Copy ( (char)3 )

            if (e.KeyChar == (char)3)
            {

                if (CurrentTilesetTile != null)
                {
                    e.Handled = true;
                    CopyPasteManager.Self.StoreCopyOf(CurrentTilesetTile);
                }
            }

            #endregion

            #region Paste ( (char)22 )

            else if (e.KeyChar == (char)22)
            {
                HandlePasteTilesetTileProperties(e);
            }
            #endregion

        }

        private void HandlePasteTilesetTileProperties(KeyPressEventArgs e)
        {
            e.Handled = true;
            // Paste CTRL+V stuff

            if (CurrentTilesetTile != null)
            {
                if (CopyPasteManager.Self.CopiedTilesetTile != null)
                {
                    bool wasAnythingChanged = false;

                    foreach (var propertyToCopy in CopyPasteManager.Self.CopiedTilesetTile.properties)
                    {
                        if (propertyToCopy.StrippedNameLower != "name")
                        {
                            // Is there already a property with this name?
                            property toPasteOn = CurrentTilesetTile.properties.FirstOrDefault(item => item.StrippedNameLower == propertyToCopy.StrippedNameLower);

                            if (toPasteOn == null)
                            {
                                toPasteOn = new property();
                                CurrentTilesetTile.properties.Add(toPasteOn);
                            }

                            toPasteOn.name = propertyToCopy.name;
                            toPasteOn.value = propertyToCopy.value;

                            CurrentTilesetTile.ForceRebuildPropertyDictionary();

                            wasAnythingChanged = true;

                        }
                    }

                    if (wasAnythingChanged)
                    {
                        if (GetExistingProperty("Name", CurrentTilesetTile) == null)
                        {
                            AddRandomNameTo(CurrentTilesetTile);
                        }

                    }

                    if (wasAnythingChanged)
                    {
                        UpdateXnaDisplayToTileset();

                        RefreshUiToSelectedTile();

                        if (AnyTileMapChange != null)
                        {
                            AnyTileMapChange(this, null);
                        }

                    }
                }
            }
        }

        private void HandleXnaControlKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (e.KeyCode == System.Windows.Forms.Keys.Delete && CurrentTilesetTile != null)
            {
                // Delete everything here after asking the user if it's okay:
                string message;

                var dialogResult = MessageBox.Show("Is it okay to delete all properties on this tile?", "Delete properties?",
                    MessageBoxButtons.YesNo);

                if (dialogResult == DialogResult.Yes)
                {
                    AppState.Self.CurrentTileset.Tiles.Remove(CurrentTilesetTile);
                    CurrentTilesetTile = null;

                    UpdateXnaDisplayToTileset();

                    UpdatePropertiesUI();

                    if (AnyTileMapChange != null)
                    {
                        AnyTileMapChange(this, null);
                    }
                    
                }
            }




        }

        void HandleXnaUpdate()
        {

            mapTilesetTile tileSetOver = CursorActivity();


            UpdateInfoLabelToTileset(tileSetOver);
        }

        private mapTilesetTile CursorActivity()
        {
            if (mCursor == null)
            {
                throw new Exception("mCursor is null");
            }

            mCursor.Activity(TimeManager.Self.CurrentTime);
            //mKeyboard.Activity();

            mapTilesetTile tileSetOver;
            GetTilesetTileOver(out tileSetOver);

            // let's make whatever has focus lose it:
            if (mCursor.PrimaryClick)
            {
                mControl.Focus();

                if (AppState.Self.CurrentTileset != null)
                {
                    if (tileSetOver != null)
                    {
                        CurrentTilesetTile = tileSetOver;
                    }
                    else
                    {
                        CurrentTilesetTile = TryGetOrMakeNewTilesetTileAtCursor(mCursor);
                    }
                }
            }
            return tileSetOver;
        }

        private void UpdateInfoLabelToTileset(mapTilesetTile tileSetOver)
        {
            string whatToShow = null;
            if (tileSetOver != null)
            {

                foreach (var property in tileSetOver.properties)
                {

                    whatToShow += "(" + property.name + ", " + property.value + ") ";

                }

                whatToShow += "(ID, " + tileSetOver.id + ")";
            }
            else if(CurrentTileset != null)
            {
                var width = CurrentTileset.Tilewidth;

                int x = (int)(mCursor.GetWorldX(mManagers) / width);
                int y = (int)(mCursor.GetWorldY(mManagers) / width);

                whatToShow = "ID, " + (x + y* CurrentTileset.GetNumberOfTilesWide());
            }
            mInfoLabel.Text = whatToShow;
        }

        private mapTilesetTile TryGetOrMakeNewTilesetTileAtCursor( InputLibrary.Cursor cursor)
        {
            var tileset = AppState.Self.CurrentTileset;

            float worldX = cursor.GetWorldX(mManagers);
            float worldY = cursor.GetWorldY(mManagers);

            int id = tileset.CoordinateToLocalId(
                    (int)worldX,
                    (int)worldY);

            mapTilesetTile newTile = tileset.Tiles.FirstOrDefault(item=>item.id == id);

            if (newTile == null)
            {
                if (worldX > -1 && worldY > -1 &&
                    worldX < AppState.Self.CurrentTileset.Images[0].width &&
                    worldY < AppState.Self.CurrentTileset.Images[0].height)
                {
                    newTile = new mapTilesetTile();
                    newTile.id = id;

                    newTile.properties = new List<property>();
                }
            }

            return newTile;
        }

        void HandleWindowResize()
        {
            // No need to handle this now that the camera is positioned top left
            //Camera.X = (int)(mManagers.Renderer.GraphicsDevice.Viewport.Width / 2.0f);
            //Camera.Y = (int)(mManagers.Renderer.GraphicsDevice.Viewport.Height / 2.0f);
        }

        void MoveToTopLeftOfDisplay()
        {
        }



        public void UpdateXnaDisplayToTileset()
        {

            var currentTileset = mTilesetsListBox.SelectedItem as Tileset;

            ClearAllHighlights();

            SetTilesetSpriteTexture();

            //int numberTilesTall = mSprite.Texture.Height / currentTileset.Tileheight;
            if (currentTileset != null)
            {
                foreach (var tile in currentTileset.Tiles.Where(item => item.properties.Count != 0))
                {

                    int count = tile.properties.Count;


                    float left;
                    float top;
                    float width;
                    float height;
                    GetTopLeftWidthHeight(tile, out left, out top, out width, out height);
                    TilePropertyHighlight tph = new TilePropertyHighlight(mManagers);
                    tph.X = left;
                    tph.Y = top;

                    tph.Width = width;
                    tph.Height = height;
                    tph.Count = count;
                    tph.AddToManagers();

                    tph.TextScale = 1/mManagers.Renderer.Camera.Zoom;

                    tph.Tag = tile;

                    mTilesWithPropertiesMarkers.Add(tph);
                }
            }
        }

        private void GetTilesetTileOver(out mapTilesetTile tileSetOver)
        {
            tileSetOver = null;

            float x = mCursor.GetWorldX(mManagers);
            float y = mCursor.GetWorldY(mManagers);

            foreach (var highlight in mTilesWithPropertiesMarkers)
            {
                if (x > highlight.X && x < highlight.X + highlight.Width &&
                    y > highlight.Y && y < highlight.Y + highlight.Height)
                {
                    tileSetOver = highlight.Tag as mapTilesetTile;
                }
            }
        }


        private void SetTilesetSpriteTexture()
        {
            if (CurrentTileset != null && CurrentTileset.Images != null && CurrentTileset.Images.Length != 0)
            {
                var image = CurrentTileset.Images[0];

                string fileName = image.Source;

                if (!string.IsNullOrEmpty(CurrentTileset.SourceDirectory) && CurrentTileset.SourceDirectory != ".")
                {

                    fileName = CurrentTileset.SourceDirectory + fileName;
                }


                string absoluteFile = fileName;
                if (FlatRedBall.IO.FileManager.IsRelative(fileName))
                {
                    absoluteFile = FlatRedBall.IO.FileManager.RemoveDotDotSlash(ProjectManager.Self.MakeAbsolute(fileName));
                }
                
                mSprite.Visible = true;

                if (System.IO.File.Exists(absoluteFile))
                {
                    try
                    {
                        mSprite.Texture = LoaderManager.Self.LoadContent<Texture2D>(absoluteFile);
                    }
                    catch(Exception e)
                    {
                        MessageBox.Show("Error loading file:\n" + e.ToString());
                    }

                    if(mSprite.Texture != null)
                    {
                        mSprite.Width = mSprite.Texture.Width;
                        mSprite.Height = mSprite.Texture.Height;

                    }
                }

                mOutlineRectangle.Visible = true;
                mOutlineRectangle.X = mSprite.X;
                mOutlineRectangle.Y = mSprite.Y;
                mOutlineRectangle.Width = mSprite.EffectiveWidth;
                mOutlineRectangle.Height = mSprite.EffectiveHeight;


            }







        }

        private void UpdateHighlightRectangle()
        {

            if (CurrentTilesetTile == null)
            {
                if (mHighlightRectangle != null)
                {
                    mHighlightRectangle.Visible = false;
                }
            }
            else
            {
                mHighlightRectangle.Visible = true;

                float left;
                float top;
                float width;
                float height;
                GetTopLeftWidthHeight(CurrentTilesetTile, out left, out top, out width, out height);

                mHighlightRectangle.X = left - 1;
                mHighlightRectangle.Y = top - 1;
                mHighlightRectangle.Width = width + 3;
                mHighlightRectangle.Height = height + 3;

                //mHighlightRectangle.X =  - 2;
                //mHighlightRectangle.Y = - 2;
                //mHighlightRectangle.Width = 1000;
                //mHighlightRectangle.Height = 1000;
                //mHighlightRectangle.Z = 4;
            }

        }


        private void ClearAllHighlights()
        {
            foreach (TilePropertyHighlight tph in mTilesWithPropertiesMarkers)
            {
                tph.RemoveFromManagers();
            }
            mTilesWithPropertiesMarkers.Clear();
        }
    }
}
