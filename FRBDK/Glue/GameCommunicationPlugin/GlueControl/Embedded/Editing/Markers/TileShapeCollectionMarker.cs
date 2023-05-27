{CompilerDirectives}

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
        Removing,
        AddingLine
    }

    #endregion

    public class TileShapeCollectionMarker : ISelectionMarker
    {
        #region Fields/Properties

        public bool UsesRightMouseButton => true;

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
        Vector3 lastFrameTileHighlight;
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

        Vector2 PositionPushed;
        Vector2 LastLineDrawingPosition;

        #endregion

        #region Events/Delegates

        public event Action<INameable, string, object> PropertyChanged;

        #endregion

        #region Constructor/Initialization

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

            var isUsingFromTypeCollisionOptions =
                // FromType = 4,
                collisionCreationOptions == 4 ||
                // FromMapCollision = 6
                collisionCreationOptions == 6;

            if (isUsingFromTypeCollisionOptions && !string.IsNullOrEmpty(sourceTmxObject))
            {
                map = EditingManager.Self.GetObjectByName(sourceTmxObject) as FlatRedBall.TileGraphics.LayeredTileMap;
            }
        }

        #endregion

        public void Update(bool didGameBecomeActive)
        {
            ////////////////early out//////////////////////////////
            if (map == null)
            {
                return;
            }
            //////////////end early out////////////////////////////

            #region Initial Variable Assignment

            var cursor = GuiManager.Cursor;
            var keyboard = FlatRedBall.Input.InputManager.Keyboard;

            var tileDimensions = owner.GridSize;

            float tileDimensionHalf = tileDimensions / 2.0f;

            #endregion

            #region Update Highlight and X
            currentTileHighlight.X = MathFunctions.RoundFloat(cursor.WorldX - tileDimensionHalf, tileDimensions) + tileDimensionHalf;
            currentTileHighlight.Y = MathFunctions.RoundFloat(cursor.WorldY - tileDimensionHalf, tileDimensions) + tileDimensionHalf;
            currentTileHighlight.Visible = cursor.IsInWindow();

            if (EditingMode == EditingMode.Adding)
            {
                if (!boundsRectangle.IsPointInside(currentTileHighlight.X, currentTileHighlight.Y))
                {
                    var top = currentTileHighlight.Y + tileDimensionHalf;
                    var bottom = currentTileHighlight.Y - tileDimensionHalf;
                    var left = currentTileHighlight.X - tileDimensionHalf;
                    var right = currentTileHighlight.X + tileDimensionHalf;
                    EditorVisuals.Line(new Vector3(left, top, 0), new Vector3(right, bottom, 0)).Color = Color.Red;
                    EditorVisuals.Line(new Vector3(right, top, 0), new Vector3(left, bottom, 0)).Color = Color.Red;
                }
            }
            #endregion

            #region Primary Push

            if (cursor.PrimaryPush && EditingMode == EditingMode.None)
            {
                if (keyboard.IsShiftDown)
                {
                    EditingMode = EditingMode.AddingLine;
                }
                else
                {
                    EditingMode = EditingMode.Adding;
                }
                PositionPushed = cursor.WorldPosition;
            }

            #endregion

            #region Primary Down

            if (EditingMode == EditingMode.Adding)
            {
                var oldXIndex = MathFunctions.RoundToInt(
                    (lastFrameTileHighlight.X - (owner.LeftSeedX + owner.GridSize / 2)) / owner.GridSize);

                var oldYIndex = MathFunctions.RoundToInt(
                    (lastFrameTileHighlight.Y - (owner.BottomSeedY + owner.GridSize / 2)) / owner.GridSize);

                var newXIndex = MathFunctions.RoundToInt(
                    (currentTileHighlight.X - (owner.LeftSeedX + owner.GridSize / 2)) / owner.GridSize);

                var newYIndex = MathFunctions.RoundToInt(
                    (currentTileHighlight.Y - (owner.BottomSeedY + owner.GridSize / 2)) / owner.GridSize);

#if HasGetGridLine
                if ((oldXIndex != newXIndex || oldYIndex != newYIndex) &&
                    // If it's a push, we don't want to "zoom" from the previous location and draw a huge line.
                    !cursor.PrimaryPush)
                {
                    // need to paint a line
                    var listOfPoints = MathFunctions.GetGridLine(oldXIndex, oldYIndex, newXIndex, newYIndex);

                    foreach (var pointToPaint in listOfPoints)
                    {
                        var xIndex = pointToPaint.X;
                        var yIndex = pointToPaint.Y;

                        var worldX = owner.LeftSeedX + owner.GridSize / 2 + owner.GridSize * xIndex;
                        var worldY = owner.BottomSeedY + owner.GridSize / 2 + owner.GridSize * yIndex;

                        var existingRectangle = owner.GetRectangleAtPosition(worldX, worldY);

                        if (existingRectangle == null)
                        {
                            PaintTileAtWorldPosition(worldX, worldY);
                        }
                    }
                }
                else
#endif
                {
                    // paint a single spot
                    // try to paint
                    var existingRectangle = owner.GetRectangleAtPosition(currentTileHighlight.X, currentTileHighlight.Y);

                    if (existingRectangle == null && boundsRectangle.IsPointInside(currentTileHighlight.X, currentTileHighlight.Y))
                    {
                        PaintTileAtHighlight();
                    }
                }

            }
            else if (EditingMode == EditingMode.AddingLine)
            {
                UpdateTileHighlightsToLine();
            }

            #endregion

            #region Primary Click

            if (cursor.PrimaryClick)
            {
                if (EditingMode == EditingMode.Adding || EditingMode == EditingMode.AddingLine)
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
                    var oldXIndex = MathFunctions.RoundToInt(
                        (lastFrameTileHighlight.X - (owner.LeftSeedX + owner.GridSize / 2)) / owner.GridSize);

                    var oldYIndex = MathFunctions.RoundToInt(
                        (lastFrameTileHighlight.Y - (owner.BottomSeedY + owner.GridSize / 2)) / owner.GridSize);

                    var newXIndex = MathFunctions.RoundToInt(
                        (currentTileHighlight.X - (owner.LeftSeedX + owner.GridSize / 2)) / owner.GridSize);

                    var newYIndex = MathFunctions.RoundToInt(
                        (currentTileHighlight.Y - (owner.BottomSeedY + owner.GridSize / 2)) / owner.GridSize);

#if HasGetGridLine

                    if ((oldXIndex != newXIndex || oldYIndex != newYIndex) &&
                    // If it's a push, we don't want to "zoom" from the previous location and draw a huge line.
                        !cursor.SecondaryPush)
                    {
                        var listOfPoints = MathFunctions.GetGridLine(oldXIndex, oldYIndex, newXIndex, newYIndex);

                        foreach (var pointToPaint in listOfPoints)
                        {
                            var xIndex = pointToPaint.X;
                            var yIndex = pointToPaint.Y;

                            var worldX = owner.LeftSeedX + owner.GridSize / 2 + owner.GridSize * xIndex;
                            var worldY = owner.BottomSeedY + owner.GridSize / 2 + owner.GridSize * yIndex;

                            EraseTileAtWorld(worldX, worldY);
                        }
                    }
                    else
#endif
                    {
                        var worldX = currentTileHighlight.X;
                        var worldY = currentTileHighlight.Y;
                        EraseTileAtWorld(worldX, worldY);

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

            lastFrameTileHighlight = currentTileHighlight.Position;
        }

        private void EraseTileAtWorld(float worldX, float worldY)
        {
            var tileDimensions = owner.GridSize;

            // try to erase
            var existingRectangle = owner.GetRectangleAtPosition(worldX, worldY);

            if (existingRectangle != null && RectanglesAddedOrRemoved.Contains(existingRectangle) == false)
            {
                existingRectangle.Visible = true;
                existingRectangle.Color = Color.Red;
                existingRectangle.Width = tileDimensions - 2;
                existingRectangle.Height = tileDimensions - 2;

                RectanglesAddedOrRemoved.Add(existingRectangle);
            }
        }

        public bool ShouldSuppress(string variableName) => false;

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

                        int numberOfTilesWide = 512 / tileWidth;

                        if (gameplayLayer.Texture != null)
                        {
                            numberOfTilesWide = gameplayLayer.Texture.Width / tileWidth;
                        }

                        var yIndex = textureTileId.Value / numberOfTilesWide;
                        var xIndex = textureTileId.Value % numberOfTilesWide;

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
            var worldX = currentTileHighlight.X;
            var worldY = currentTileHighlight.Y;

            PaintTileAtWorldPosition(worldX, worldY);
        }

        private void PaintTileAtWorldPosition(float worldX, float worldY)
        {
            var tileDimensions = owner.GridSize;
            owner.AddCollisionAtWorld(worldX, worldY);
            var newRect = owner.GetRectangleAtPosition(worldX, worldY);
            newRect.Visible = true;
            newRect.Color = Color.Green;
            newRect.Width = tileDimensions - 2;
            newRect.Height = tileDimensions - 2;

            RectanglesAddedOrRemoved.Add(newRect);
        }

        private void UpdateTileHighlightsToLine()
        {
            float startX, startY, endX, endY;
            var currentCursorPosition = GuiManager.Cursor.WorldPosition;

            var leftSeed = owner.LeftSeedX + owner.GridSize / 2.0f;
            var bottomSeed = owner.BottomSeedY + owner.GridSize / 2.0f;

            endX = MathFunctions.RoundFloat(currentCursorPosition.X, owner.GridSize, leftSeed);
            endY = MathFunctions.RoundFloat(currentCursorPosition.Y, owner.GridSize, bottomSeed);

            //////////////////early out///////////////////////////////////
            if (endX == LastLineDrawingPosition.X && endY == LastLineDrawingPosition.Y)
            {
                return;
            }
            //////////////end early out///////////////////////////////////

            LastLineDrawingPosition.X = endX;
            LastLineDrawingPosition.Y = endY;

            startX = MathFunctions.RoundFloat(PositionPushed.X, owner.GridSize, leftSeed);
            startY = MathFunctions.RoundFloat(PositionPushed.Y, owner.GridSize, bottomSeed);


            var xDifference = Math.Abs(endX - startX);
            var yDifference = Math.Abs(endY - startY);

            var tileDimensions = owner.GridSize;

            if (xDifference >= yDifference)
            {
                // horizontal
                var sign = Math.Sign(endX - startX);

                var clampedStartX = ClampX(startX);
                var clampedEndX = ClampX(endX);

                float ClampX(float xValue)
                {
                    if (xValue < boundsRectangle.Left)
                    {
                        // ...but assign including the offset:
                        xValue = boundsRectangle.Left + owner.GridSize / 2.0f;
                    }
                    if (xValue > boundsRectangle.Right)
                    {
                        xValue = boundsRectangle.Right - owner.GridSize / 2.0f;
                    }
                    return xValue;
                }


                var numberOfTiles = 1 + Math.Abs(MathFunctions.RoundToInt((clampedEndX - clampedStartX) / owner.GridSize));

                ClearRectangles();

                for (int i = 0; i < numberOfTiles; i++)
                {
                    var worldX = clampedStartX + sign * i * owner.GridSize;
                    var worldY = startY;

                    CreateLocalRectangleForLineAt(worldX, worldY, i);
                }
                while (numberOfTiles < RectanglesAddedOrRemoved.Count)
                {
                    var rectToRemove = RectanglesAddedOrRemoved[RectanglesAddedOrRemoved.Count - 1];
                    owner.RemoveCollisionAtWorld(rectToRemove.X, rectToRemove.Y);
                    RectanglesAddedOrRemoved.RemoveAt(RectanglesAddedOrRemoved.Count - 1);
                }
            }
            else
            {
                // vertical
                var sign = Math.Sign(endY - startY);


                var clampedStartY = ClampY(startY);
                var clampedEndY = ClampY(endY);

                float ClampY(float yValue)
                {
                    if (yValue < boundsRectangle.Bottom)
                    {
                        // ...but assign including the offset:
                        yValue = boundsRectangle.Bottom + owner.GridSize / 2.0f;
                    }
                    if (yValue > boundsRectangle.Top)
                    {
                        yValue = boundsRectangle.Top - owner.GridSize / 2.0f;
                    }
                    return yValue;
                }

                var numberOfTiles = 1 + Math.Abs(MathFunctions.RoundToInt((clampedEndY - clampedStartY) / owner.GridSize));

                ClearRectangles();

                for (int i = 0; i < numberOfTiles; i++)
                {
                    var worldX = startX;
                    var worldY = clampedStartY + sign * i * owner.GridSize;

                    CreateLocalRectangleForLineAt(worldX, worldY, i);
                }
                while (numberOfTiles < RectanglesAddedOrRemoved.Count)
                {
                    var rectToRemove = RectanglesAddedOrRemoved[RectanglesAddedOrRemoved.Count - 1];
                    owner.RemoveCollisionAtWorld(rectToRemove.X, rectToRemove.Y);
                    RectanglesAddedOrRemoved.RemoveAt(RectanglesAddedOrRemoved.Count - 1);
                }
            }

            void ClearRectangles()
            {
                while (RectanglesAddedOrRemoved.Count > 0)
                {
                    var rectToRemove = RectanglesAddedOrRemoved[RectanglesAddedOrRemoved.Count - 1];
                    owner.RemoveCollisionAtWorld(rectToRemove.X, rectToRemove.Y);
                    RectanglesAddedOrRemoved.RemoveAt(RectanglesAddedOrRemoved.Count - 1);
                }
            }

            void CreateLocalRectangleForLineAt(float worldX, float worldY, int i)
            {


                owner.AddCollisionAtWorld(worldX, worldY);

                var newRect = owner.GetRectangleAtPosition(worldX, worldY);

                newRect.Visible = true;
                newRect.Color = Color.Green;
                newRect.Width = tileDimensions - 2;
                newRect.Height = tileDimensions - 2;

                RectanglesAddedOrRemoved.Add(newRect);
            }
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

        public bool HandleDelete() => false;

        #region General ISelectionMarker implementations

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

        public void MakePersistent()
        {
            ScreenManager.PersistentAxisAlignedRectangles.Add(currentTileHighlight);
            ScreenManager.PersistentAxisAlignedRectangles.Add(boundsRectangle);
        }

        public bool IsMouseOverThis()
        {
            // always say "true" because the user can paint when selecting a TileShapeCollection.
            // Let them select something else in Glue
            return true;
        }


        #endregion
    }


}
