using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.TileCollisions;
using FlatRedBall.Utilities;
using GlueControl.Editing;
using GlueControl.Dtos;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Screens;
using StateInterpolationPlugin;
using FlatRedBall.TileGraphics;
using FlatRedBall.IO;


namespace GlueControl.Editing
{
    #region Enums

    public enum EditingMode
    {
        None,
        Adding,
        Removing
    }

    #endregion

    public class TileShapeCollectionMarker : ISelectionMarker
    {
        #region Fields/Properties

        public float ExtraPaddingInPixels { get => 0; set { } }
        public bool Visible
        {
            get => throw new NotImplementedException(); set => throw new NotImplementedException();
        }
        public double FadingSeed
        {
            get; set;
        }
        public Color BrightColor
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public string Name { get; set; }
        public bool CanMoveItem { get => false; set { } }

        Models.NamedObjectSave namedObjectSave;

        AxisAlignedRectangle currentTileHighlight;
        AxisAlignedRectangle boundsRectangle;

        public Vector3 LastUpdateMovement => throw new NotImplementedException();

        List<AxisAlignedRectangle> RectanglesAddedOrRemoved = new List<AxisAlignedRectangle>();

        bool originalVisibility;

        TileShapeCollection owner;

        FlatRedBall.TileGraphics.LayeredTileMap map;

        public INameable Owner
        {
            get => owner;
            set => SetTileShapeCollectionInternal(value);
        }

        EditingMode EditingMode { get; set; }

        #endregion

        #region Events/Delegates

        public event Action<INameable, string, object> PropertyChanged;

        #endregion

        public TileShapeCollectionMarker(INameable owner, Models.NamedObjectSave namedObjectSave)
        {
            if (namedObjectSave == null)
            {
                throw new ArgumentNullException(nameof(namedObjectSave));
            }

            this.namedObjectSave = namedObjectSave;
            this.Owner = owner;
            currentTileHighlight = new AxisAlignedRectangle();
            currentTileHighlight.Name = "TileShapeCollectionMarker current tile highlight";
            currentTileHighlight.Visible = true;
            currentTileHighlight.Width = 16;
            currentTileHighlight.Height = 16;
            currentTileHighlight.Color = Microsoft.Xna.Framework.Color.Orange;

            TryFindMap();

            boundsRectangle = new AxisAlignedRectangle();
            if (map != null)
            {
                var extraBorder = 3;

                boundsRectangle.Visible = true;
                boundsRectangle.Width = map.Width + extraBorder * 2;
                boundsRectangle.Height = map.Height + extraBorder * 2;
                boundsRectangle.Left = map.X - extraBorder;
                boundsRectangle.Top = map.Y + extraBorder;
                boundsRectangle.Color = Color.LightBlue;
                boundsRectangle.Name = "TileShapeCollectionMarker bounds rectangle";
            }
            else
            {
                boundsRectangle.Visible = false;
                currentTileHighlight.Visible = false;

            }
        }

        private void TryFindMap()
        {
            var collisionCreationOptionsValue = namedObjectSave.Properties.FirstOrDefault(item => item.Name == "CollisionCreationOptions")?.Value;

            int? collisionCreationOptions = collisionCreationOptionsValue as int?;
            if (collisionCreationOptionsValue is long asLong)
            {
                collisionCreationOptions = (int)asLong;
            }
            var sourceTmxObject = namedObjectSave.Properties.FirstOrDefault(item => item.Name == "SourceTmxName")?.Value as string;

            if (collisionCreationOptions == 4 && !string.IsNullOrEmpty(sourceTmxObject))
            {
                var foundObject = ScreenManager.CurrentScreen.GetInstanceRecursive("this." + sourceTmxObject + ".throwaway");
                map = foundObject as FlatRedBall.TileGraphics.LayeredTileMap;
            }
        }

        public void Update(ResizeSide sideGrabbed)
        {
            ////////////////early out//////////////////////////////
            if (map == null)
            {
                return;
            }
            //////////////end early out////////////////////////////
            
            #region Initial Variable Assignment

            var cursor = GuiManager.Cursor;

            var tileDimensions = owner.GridSize;

            float tileDimensionHalf = tileDimensions / 2.0f;

            currentTileHighlight.X = MathFunctions.RoundFloat(cursor.WorldX - tileDimensionHalf, tileDimensions) + tileDimensionHalf;
            currentTileHighlight.Y = MathFunctions.RoundFloat(cursor.WorldY - tileDimensionHalf, tileDimensions) + tileDimensionHalf;

            #endregion

            #region Primary Push

            if (cursor.PrimaryPush && EditingMode == EditingMode.None)
            {
                EditingMode = EditingMode.Adding;
            }

            #endregion

            #region Primary Down

            if (cursor.PrimaryDown)
            {
                if (EditingMode == EditingMode.Adding)
                {
                    // try to paint
                    var existingRectangle = owner.GetRectangleAtPosition(currentTileHighlight.X, currentTileHighlight.Y);

                    if (existingRectangle == null && boundsRectangle.IsPointInside(currentTileHighlight.X, currentTileHighlight.Y))
                    {
                        PaintTileAtHighlight();
                    }
                }
            }

            #endregion

            #region Primary Click

            if (cursor.PrimaryClick)
            {
                if (EditingMode == EditingMode.Adding)
                {
                    CommitPaintedTiles();
                    RectanglesAddedOrRemoved.Clear();
                    EditingMode = EditingMode.None;
                }
            }

            #endregion

            #region Secondary Push

            if (cursor.SecondaryPush && EditingMode == EditingMode.None)
            {
                EditingMode = EditingMode.Removing;
            }

            #endregion

            #region Secondary Down

            if (cursor.SecondaryDown)
            {
                if (EditingMode == EditingMode.Removing)
                {
                    // try to erase
                    var existingRectangle = owner.GetRectangleAtPosition(currentTileHighlight.X, currentTileHighlight.Y);

                    if (existingRectangle != null && RectanglesAddedOrRemoved.Contains(existingRectangle) == false)
                    {
                        existingRectangle.Visible = true;
                        existingRectangle.Color = Color.Red;
                        existingRectangle.Width = tileDimensions - 2;
                        existingRectangle.Height = tileDimensions - 2;

                        RectanglesAddedOrRemoved.Add(existingRectangle);
                    }
                }
            }

            #endregion

            #region Secondary Click

            if (cursor.SecondaryClick)
            {
                if (EditingMode == EditingMode.Removing)
                {
                    CommitRemovedTiles();
                    RectanglesAddedOrRemoved.Clear();
                    EditingMode = EditingMode.None;
                }
            }

            #endregion
        }

        private void CommitRemovedTiles()
        {
            SendRemovedTileModifyDto();

            foreach (var tile in RectanglesAddedOrRemoved)
            {
                owner.RemoveCollisionAtWorld(tile.X, tile.Y);
            }

            var gameplayLayer = map?.MapLayers.FirstOrDefault(item => item.Name == "GameplayLayer");

            if (gameplayLayer != null)
            {
                bool removeTilesOnShapeCollectionCreation = GetIfShouldRemoveTilesOnShapeCollisionCreation();

                if (!removeTilesOnShapeCollectionCreation)
                {
                    // the tiles aren't removed on TMX creation, so they are still visible. Remove them here
                    RemoveTilesOnGameplayLayer(gameplayLayer);
                }
            }
        }

        private bool RemoveTilesOnGameplayLayer(MapDrawableBatch gameplayLayer)
        {
            var quadsToRemove = new List<int>();
            foreach (var removedRectangle in RectanglesAddedOrRemoved)
            {
                var index = gameplayLayer.GetQuadIndex(removedRectangle.X, removedRectangle.Y);

                if (index != null)
                {
                    quadsToRemove.Add(index.Value);
                }
            }

            var shouldRemove = quadsToRemove.Count > 0;

            if (shouldRemove)
            {
                gameplayLayer.RemoveQuads(quadsToRemove);
            }

            return shouldRemove;
        }

        private void SendRemovedTileModifyDto()
        {
            var dto = new ModifyCollisionDto();
            dto.TileShapeCollection = owner.Name;

            dto.RemovedPositions = new List<Vector2>();

            foreach (var tile in RectanglesAddedOrRemoved)
            {
                dto.RemovedPositions.Add(tile.Position.ToVector2());
            }
            GlueControlManager.Self.SendToGlue(dto);
        }

        private void CommitPaintedTiles()
        {


            var tileDimensions = owner.GridSize;
            foreach (var tile in RectanglesAddedOrRemoved)
            {
                tile.Width = tileDimensions;
                tile.Height = tileDimensions;

                tile.Color = Color.White; // is this always the color?
            }

            var gameplayLayer = map?.MapLayers.FirstOrDefault(item => item.Name == "GameplayLayer");

            var doesGameplayLayerHaveMissingTexture = gameplayLayer != null && gameplayLayer.Texture == null;

            bool wereAnyTilesRemoved = false;

            if (gameplayLayer != null)
            {
                bool removeTilesOnShapeCollectionCreation = GetIfShouldRemoveTilesOnShapeCollisionCreation();

                if (!removeTilesOnShapeCollectionCreation)
                {
                    // Let's remove first, which will get rid of any tiles that were painted over:
                    wereAnyTilesRemoved = RemoveTilesOnGameplayLayer(gameplayLayer);

                    PaintTileOnGameplayLayer(gameplayLayer);
                }
            }

            SendPaintedTileModifyDto(requestRestart: wereAnyTilesRemoved || doesGameplayLayerHaveMissingTexture);
        }

        private bool GetIfShouldRemoveTilesOnShapeCollisionCreation()
        {
            var removeTilesOnShapeCollectionCreation = false;
            var removeProperty = namedObjectSave.Properties.FirstOrDefault(item => item.Name == "RemoveTilesAfterCreatingCollision");
            if (removeProperty != null && removeProperty.Value is bool asBool)
            {
                removeTilesOnShapeCollectionCreation = asBool;
            }

            return removeTilesOnShapeCollectionCreation;
        }

        private void PaintTileOnGameplayLayer(MapDrawableBatch gameplayLayer)
        {
            var tileDimensions = owner.GridSize;
            var collisionType = namedObjectSave.Properties.FirstOrDefault(item => item.Name == "CollisionTileTypeName")?.Value as string;

            int textureLeftPixel = 0;
            int textureTopPixel = 0;
            int tileWidth = 16;
            int tileHeight = 16;

            var gameplayLayerHasTexture = gameplayLayer.Texture != null;

            foreach (var tileProperty in map.TileProperties)
            {
                var hasValue = tileProperty.Value.Any(item => item.Name == "Type" && (string)item.Value == collisionType);

                if (hasValue)
                {
                    // We don't know the file path of the tileset, so we'll just assume there's no duplicate texture names. There could be, and if so then we'll worry about
                    // that then, because that would be a ton of work.
                    string textureNameStripped = null;
                    if (gameplayLayer.Texture != null)
                    {
                        textureNameStripped = FileManager.RemovePath(FileManager.RemoveExtension(gameplayLayer.Texture.Name)).ToLowerInvariant();
                    }
                    // this is the name, but how do we get tile 
                    var tileset = map.Tilesets.FirstOrDefault(item =>
                    {
                        if (item.Images.Length > 0)
                        {
                            var imageName = item.Images[0].sourceFileName;

                            if (!string.IsNullOrEmpty(imageName))
                            {
                                return FileManager.RemovePath(FileManager.RemoveExtension(imageName)).ToLowerInvariant() == textureNameStripped;
                            }
                        }
                        return false;
                    });

                    int? textureTileId = null;

                    if (tileset != null)
                    {
                        foreach (var kvp in tileset.TileDictionary)
                        {
                            var type = kvp.Value.PropertyDictionary.FirstOrDefault(item => item.Key == "Type");

                            if (type.Key == "Type" && type.Value == collisionType)
                            {
                                textureTileId = kvp.Value.id;
                                break;
                            }
                        }
                    }

                    if (textureTileId != null)
                    {
                        tileWidth = tileset.Tilewidth;
                        tileHeight = tileset.Tileheight;

                        var yIndex = textureTileId.Value / tileHeight;
                        var xIndex = textureTileId.Value % tileWidth;

                        textureLeftPixel = xIndex * tileWidth;
                        textureTopPixel = yIndex * tileHeight;
                        break;
                    }

                }
            }

            // todo - need to somehow figure out which texture to use...
            if (gameplayLayerHasTexture)
            {
                var layer = new FlatRedBall.TileGraphics.MapDrawableBatch(RectanglesAddedOrRemoved.Count, gameplayLayer.Texture);

                foreach (var tile in RectanglesAddedOrRemoved)
                {
                    Vector3 bottomLeft = new Vector3(
                        tile.X - tileDimensions / 2.0f,
                        tile.Y - tileDimensions / 2.0f,
                        0);
                    layer.AddTile(bottomLeft, new Vector2(tileDimensions, tileDimensions), textureLeftPixel, textureTopPixel, textureLeftPixel + tileWidth, textureTopPixel + tileHeight);
                }

                layer.SortQuadsOnAxis(gameplayLayer.SortAxis);

                gameplayLayer.MergeOntoThis(new List<MapDrawableBatch>() { layer });
            }
        }

        private void SendPaintedTileModifyDto(bool requestRestart = false)
        {
            var tileDimensions = owner.GridSize;
            var dto = new ModifyCollisionDto();
            dto.TileShapeCollection = owner.Name;

            dto.AddedPositions = new List<Vector2>();

            foreach (var tile in RectanglesAddedOrRemoved)
            {
                dto.AddedPositions.Add(tile.Position.ToVector2());
            }

            dto.RequestRestart = requestRestart;

            GlueControlManager.Self.SendToGlue(dto);
        }

        private void PaintTileAtHighlight()
        {
            var tileDimensions = owner.GridSize;
            owner.AddCollisionAtWorld(currentTileHighlight.X, currentTileHighlight.Y);
            var newRect = owner.GetRectangleAtPosition(currentTileHighlight.X, currentTileHighlight.Y);
            newRect.Visible = true;
            newRect.Color = Color.Green;
            newRect.Width = tileDimensions - 2;
            newRect.Height = tileDimensions - 2;

            RectanglesAddedOrRemoved.Add(newRect);
        }

        public void Destroy()
        {
            // If the owner was invisible, let's make it invisible again
            // If it's visible, then this didn't change that at all, so it
            // will remain visible. Setting Visible = true here may force visibility
            // on an already-destroyed TileShapeCollection, so only setting it to false
            // avoids multiple problems
            if (owner != null && originalVisibility == false)
            {
                owner.Visible = originalVisibility;
            }

            currentTileHighlight.Visible = false;
            boundsRectangle.Visible = false;

            ScreenManager.PersistentAxisAlignedRectangles.Remove(currentTileHighlight);
            ScreenManager.PersistentAxisAlignedRectangles.Remove(boundsRectangle);

        }

        public void HandleCursorPushed()
        {
            throw new NotImplementedException();
        }

        public void HandleCursorRelease()
        {
            throw new NotImplementedException();
        }

        public bool IsCursorOverThis()
        {
            // always say "true" because the user can paint when selecting a TileShapeCollection.
            // Let them select something else in Glue
            return true;
        }

        public void MakePersistent()
        {
            ScreenManager.PersistentAxisAlignedRectangles.Add(currentTileHighlight);
            ScreenManager.PersistentAxisAlignedRectangles.Add(boundsRectangle);
        }

        public void PlayBumpAnimation(float endingExtraPaddingBeforeZoom, bool isSynchronized)
        {
            var forBump = GetRectanglesForBump();

            var endingExtraPadding = endingExtraPaddingBeforeZoom;
            TweenerManager.Self.StopAllTweenersOwnedBy(currentTileHighlight);

            //IsFadingInAndOut = false;
            ExtraPaddingInPixels = 0;
            const float growTime = 0.25f;
            float extraPaddingFromBump = 10;
            var gridSize = owner.GridSize;

            // Let the highlight rectangle own the tweener:

            void SetExtraSize(float size)
            {
                for (int i = 0; i < forBump.Count; i++)
                {
                    forBump[i].Width = size + gridSize;
                    forBump[i].Height = size + gridSize;
                }
            }

            var tweener = currentTileHighlight.Tween(SetExtraSize, 0, extraPaddingFromBump, growTime,
                FlatRedBall.Glue.StateInterpolation.InterpolationType.Quadratic,
                FlatRedBall.Glue.StateInterpolation.Easing.Out);

            tweener.Ended += () =>
            {
                var shrinkTime = growTime;
                var tweener2 = currentTileHighlight.Tween(SetExtraSize,
                        extraPaddingFromBump, 0, shrinkTime,
                    FlatRedBall.Glue.StateInterpolation.InterpolationType.Quadratic,
                    FlatRedBall.Glue.StateInterpolation.Easing.InOut);

                tweener2.Ended += () =>
                {
                    //IsFadingInAndOut = true;
                    if (!isSynchronized)
                    {
                        FadingSeed = TimeManager.CurrentTime;
                    }
                    SetExtraSize(0);
                };
            };
        }

        private List<AxisAlignedRectangle> GetRectanglesForBump()
        {
            var rectangles = owner.Rectangles;
            var gridSize = owner.GridSize;

            var leftX = Camera.Main.AbsoluteLeftXEdge - gridSize;
            var rightX = Camera.Main.AbsoluteRightXEdge + gridSize;

            var firstIndex = rectangles.GetFirstAfter(leftX, owner.SortAxis, 0, rectangles.Count);
            var lastIndexExclusive = rectangles.GetFirstAfter(rightX, owner.SortAxis, firstIndex, rectangles.Count);

            if (lastIndexExclusive > firstIndex)
            {
                var forBump = new List<AxisAlignedRectangle>(lastIndexExclusive - firstIndex);

                for (int i = firstIndex; i < lastIndexExclusive; i++)
                {
                    forBump.Add(rectangles[i]);
                }
                return forBump;
            }
            else
            {
                return new List<AxisAlignedRectangle>();
            }
        }

        private void SetTileShapeCollectionInternal(INameable value)
        {
            var shapeCollection = value as TileShapeCollection;

            originalVisibility = shapeCollection.Visible;

            shapeCollection.Visible = true;

            owner = shapeCollection;
        }
    }
}
