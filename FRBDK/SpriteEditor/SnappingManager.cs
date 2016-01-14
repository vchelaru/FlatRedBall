using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

using SpriteEditor.Gui;
using Microsoft.DirectX;

namespace SpriteEditor
{
    public class SnappingManager
    {
        #region Fields
        Polygon mAfterSnappingPosition;
        #endregion

        #region Properties

        #region Public Properties

        public bool ShouldSnap
        {
            get { return mAfterSnappingPosition.Visible; }
        }

        public Vector3 SnappingPosition
        {
            get
            {
                if (ShouldSnap)
                {
                    return mAfterSnappingPosition.Position;
                }
                else
                {
                    throw new System.InvalidOperationException("ShouldSnap is false - always check ShouldSnap before getting SnappingPosition.");
                }
            }
        }

        #endregion

        #region Private Properties

        private float DistanceBasedBuffer
        {
            get 
            {
                // Change this to consider orthogonal!!!
                float unitsPerPixel = 1 / SpriteManager.Camera.PixelsPerUnitAt(0);
                const int numberOfPixes = 8;
                return numberOfPixes * unitsPerPixel; 
            }
        }

        #endregion

        #endregion

        #region Methods

        #region Constructor

        public SnappingManager()
        {
            mAfterSnappingPosition = Polygon.CreateRectangle(1, 1);
            mAfterSnappingPosition.Color = System.Drawing.Color.White;
            ShapeManager.AddPolygon(mAfterSnappingPosition);
        }

        #endregion

        #region Public Methods

        public void Update()
        {
            // Update the visibility of the object
            if (GuiData.ToolsWindow.SnapSprite.IsPressed)
            {
                if (GameData.Cursor.SpritesGrabbed.Count != 0)
                {
                    // This method will control the visibility of the mAfterSnappingPosition Polygon
                    ControlAfterSnappingPosition();
                }
                else
                {
                    mAfterSnappingPosition.Visible = false;
                }
            }
            else
            {
                mAfterSnappingPosition.Visible = false;
            }
        }

        #endregion

        #region Private Methods

        private bool AreSpritesCloseOnX(Sprite sprite, Sprite otherSprite)
        {
            return
                sprite.X + sprite.ScaleX + DistanceBasedBuffer > otherSprite.X - otherSprite.ScaleX &&
                sprite.X - sprite.ScaleX - DistanceBasedBuffer < otherSprite.X + otherSprite.ScaleX;
        }

        private bool AreSpritesCloseOnY(Sprite sprite, Sprite otherSprite)
        {
            return
                sprite.Y + sprite.ScaleY + DistanceBasedBuffer > otherSprite.Y - otherSprite.ScaleY &&
                sprite.Y - sprite.ScaleY - DistanceBasedBuffer < otherSprite.Y + otherSprite.ScaleY;

        }

        private void ControlAfterSnappingPosition()
        {
            

            #region initialize variables to be used for this method
            Sprite spriteGrabbed = GameData.Cursor.SpritesGrabbed[0];
            float originalX = spriteGrabbed.X;
            float originalY = spriteGrabbed.Y;

            // Modify this later to support rotation
            float scaleXAfterRotation = spriteGrabbed.X ;
            float scaleYAfterRotation = spriteGrabbed.Y ;

            bool snappingFound = false;

            #endregion

            if (spriteGrabbed != null)
            {
                // Get all the Sprites that are children or parents 
                // of the current
                SpriteList spritesToIgnore = new SpriteList();
                spriteGrabbed.GetAllDescendantsOneWay(spritesToIgnore);
                spriteGrabbed.GetAllAncestorsOneWay(spritesToIgnore);

                float otherSpriteScaleX;
                float otherSpriteScaleY;

                mAfterSnappingPosition.X = spriteGrabbed.X;
                mAfterSnappingPosition.Y = spriteGrabbed.Y;
                mAfterSnappingPosition.Z = spriteGrabbed.Z;

                #region Loop through all of the other Sprites excluding this one to see if this one should snap anywhere
                foreach (Sprite otherSprite in GameData.Scene.Sprites)
                {
                    if (otherSprite != spriteGrabbed && spritesToIgnore.Contains(otherSprite) == false)
                    {
                        otherSpriteScaleX = otherSprite.ScaleX;
                        otherSpriteScaleY = otherSprite.ScaleY;


                        #region Check for X snapping

                        if (AreSpritesCloseOnY(spriteGrabbed, otherSprite))
                        {
                            float value = otherSprite.X - otherSprite.ScaleX - spriteGrabbed.ScaleX;

                            if (Math.Abs(value - spriteGrabbed.X) < DistanceBasedBuffer)
                            {
                                mAfterSnappingPosition.X = value;
                                snappingFound = true;
                            }

                            value = otherSprite.X - otherSprite.ScaleX + spriteGrabbed.ScaleX;

                            if (Math.Abs(value - spriteGrabbed.X) < DistanceBasedBuffer)
                            {
                                mAfterSnappingPosition.X = value;
                                snappingFound = true;
                            }

                            value = otherSprite.X + otherSprite.ScaleX - spriteGrabbed.ScaleX;

                            if (Math.Abs(value - spriteGrabbed.X) < DistanceBasedBuffer)
                            {
                                mAfterSnappingPosition.X = value;
                                snappingFound = true;
                            }

                            value = otherSprite.X + otherSprite.ScaleX + spriteGrabbed.ScaleX;

                            if (Math.Abs(value - spriteGrabbed.X) < DistanceBasedBuffer)
                            {
                                mAfterSnappingPosition.X = value;
                                snappingFound = true;
                            }
                        }

                        #endregion

                        #region Check for Y Snapping

                        if (AreSpritesCloseOnX(spriteGrabbed, otherSprite))
                        {
                            float value = otherSprite.Y - otherSprite.ScaleY - spriteGrabbed.ScaleY;

                            if (Math.Abs(value - spriteGrabbed.Y) < DistanceBasedBuffer)
                            {
                                mAfterSnappingPosition.Y = value;
                                snappingFound = true;
                            }

                            value = otherSprite.Y - otherSprite.ScaleY + spriteGrabbed.ScaleY;

                            if (Math.Abs(value - spriteGrabbed.Y) < DistanceBasedBuffer)
                            {
                                mAfterSnappingPosition.Y = value;
                                snappingFound = true;
                            }

                            value = otherSprite.Y + otherSprite.ScaleY - spriteGrabbed.ScaleY;

                            if (Math.Abs(value - spriteGrabbed.Y) < DistanceBasedBuffer)
                            {
                                mAfterSnappingPosition.Y = value;
                                snappingFound = true;
                            }

                            value = otherSprite.Y + otherSprite.ScaleY + spriteGrabbed.ScaleY;

                            if (Math.Abs(value - spriteGrabbed.Y) < DistanceBasedBuffer)
                            {
                                mAfterSnappingPosition.Y = value;
                                snappingFound = true;
                            }
                        }

                        #endregion
                    }
                }
                #endregion
            }

            if (snappingFound)
            {
                UpdateSnappingDisplayToCurrentSprite();
            }
            mAfterSnappingPosition.Visible = snappingFound;

        }


        private void UpdateSnappingDisplayToCurrentSprite()
        {
            float currentScaleX = (float)mAfterSnappingPosition.Points[0].X;
            float currentScaleY = (float)mAfterSnappingPosition.Points[0].Y;

            float amountToScaleXBy = GameData.Cursor.SpritesGrabbed[0].ScaleX / currentScaleX;
            float amountToScaleYBy = GameData.Cursor.SpritesGrabbed[0].ScaleY / currentScaleY;

            mAfterSnappingPosition.ScaleBy(amountToScaleXBy, amountToScaleYBy);

            mAfterSnappingPosition.RotationZ = GameData.Cursor.SpritesGrabbed[0].RotationZ;
        }

        #endregion

        #endregion
    }
}
