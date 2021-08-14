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

namespace GlueControl.Editing
{
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
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public Color BrightColor
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
        public string Name { get; set; }
        public bool CanMoveItem { get => false; set { } }

        AxisAlignedRectangle currentTileHighlight;

        public Vector3 LastUpdateMovement => throw new NotImplementedException();

        List<AxisAlignedRectangle> RectanglesAddedOrRemoved = new List<AxisAlignedRectangle>();

        bool originalVisibility;

        TileShapeCollection owner;
        public INameable Owner
        {
            get => owner;
            set => SetTileShapeCollectionInternal(value);
        }

        #endregion

        public event Action<INameable, string, object> PropertyChanged;

        public TileShapeCollectionMarker(INameable owner)
        {
            this.Owner = owner;
            currentTileHighlight = new AxisAlignedRectangle();
            currentTileHighlight.Name = "TileShapeCollectionMarker current tile highlight";
            currentTileHighlight.Visible = true;
            currentTileHighlight.Width = 16;
            currentTileHighlight.Height = 16;
            currentTileHighlight.Color = Microsoft.Xna.Framework.Color.Orange;
        }

        public void Destroy()
        {
            if (owner != null)
            {
                owner.Visible = originalVisibility;
            }

            currentTileHighlight.Visible = false;
            ScreenManager.PersistentAxisAlignedRectangles.Remove(currentTileHighlight);

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
        }

        public void PlayBumpAnimation(float endingExtraPaddingBeforeZoom, bool isSynchronized)
        {
            throw new NotImplementedException();
        }

        public void Update(ResizeSide sideGrabbed)
        {
            var cursor = GuiManager.Cursor;

            float tileDimensions = 16;
            float tileDimensionHalf = 8;

            currentTileHighlight.X = MathFunctions.RoundFloat(cursor.WorldX - tileDimensionHalf, 16) + tileDimensionHalf;
            currentTileHighlight.Y = MathFunctions.RoundFloat(cursor.WorldY - tileDimensionHalf, 16) + tileDimensionHalf;

            #region Primary Down

            if (cursor.PrimaryDown)
            {
                // try to paint
                var existingRectangle = owner.GetRectangleAtPosition(currentTileHighlight.X, currentTileHighlight.Y);

                if (existingRectangle == null)
                {
                    owner.AddCollisionAtWorld(currentTileHighlight.X, currentTileHighlight.Y);
                    var newRect = owner.GetRectangleAtPosition(currentTileHighlight.X, currentTileHighlight.Y);
                    newRect.Visible = true;
                    newRect.Color = Color.Green;
                    newRect.Width = tileDimensions - 2;
                    newRect.Height = tileDimensions - 2;

                    RectanglesAddedOrRemoved.Add(newRect);
                }
            }

            #endregion


            #region Secondary Down

            if (cursor.SecondaryDown)
            {
                // try to erase
                var existingRectangle = owner.GetRectangleAtPosition(currentTileHighlight.X, currentTileHighlight.Y);

                if (existingRectangle != null)
                {
                    owner.AddCollisionAtWorld(currentTileHighlight.X, currentTileHighlight.Y);
                    var newRect = owner.GetRectangleAtPosition(currentTileHighlight.X, currentTileHighlight.Y);
                    newRect.Visible = true;
                    newRect.Color = Color.Red;
                    newRect.Width = tileDimensions - 2;
                    newRect.Height = tileDimensions - 2;

                    RectanglesAddedOrRemoved.Add(newRect);
                }
            }

            #endregion


            if (cursor.PrimaryClick)
            {
                var dto = new ModifyCollisionDto();
                dto.TileShapeCollection = owner.Name;

                dto.AddedPositions = new List<Vector2>();

                foreach (var tile in RectanglesAddedOrRemoved)
                {
                    tile.Width = tileDimensions;
                    tile.Height = tileDimensions;

                    tile.Color = Color.White; // is this always the color?
                    dto.AddedPositions.Add(tile.Position.ToVector2());
                }
                GlueControlManager.Self.SendToGlue(dto);
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
