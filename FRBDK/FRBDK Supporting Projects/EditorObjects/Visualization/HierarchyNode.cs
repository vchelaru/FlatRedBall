using System;
using System.Collections.Generic;

using System.Text;
using FlatRedBall;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math;
#if FRB_XNA
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
#else
using Vector3 = Microsoft.DirectX.Vector3;
#endif
using FlatRedBall.Gui;
using FlatRedBall.Graphics;
using FlatRedBall.Utilities;

namespace EditorObjects.Visualization
{
    public enum VisibleRepresentationType
    {
        Circle,
        Sprite
    }

    public class HierarchyNode : PositionedObject
    {
        #region Fields

        Sprite mSpriteVisibleRepresentation;

        Circle mCircleVisibleRepresentation;
        Line mParentLine;
        Circle mParentAttachmentPoint;

        Layer mLayer;

        IAttachable mIAttachableRepresenting;

        Text mText;

        float mCachedWidth;
        double mLastTimeWidthCalculated = -1;

        #endregion

        #region Properties

        public Circle Collision
        {
            get 
            {
                if (mCircleVisibleRepresentation != null)
                {
                    return mCircleVisibleRepresentation;
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        public bool IsMouseOver(float worldX, float worldY)
        {

            if (mCircleVisibleRepresentation != null)
            {
                return mCircleVisibleRepresentation.IsPointInside(worldX, worldY);

                //return FlatRedBall.Input.InputManager.Mouse.IsOn3D(mCircleVisibleRepresentation, false, camera);

                //return GuiManager.Cursor.IsOn(mCircleVisibleRepresentation);
            }
            else
            {
                return worldX > mSpriteVisibleRepresentation.X - mSpriteVisibleRepresentation.ScaleX &&
                    worldX < mSpriteVisibleRepresentation.X + mSpriteVisibleRepresentation.ScaleX &&
                    worldY > mSpriteVisibleRepresentation.Y - mSpriteVisibleRepresentation.ScaleY &&
                    worldY < mSpriteVisibleRepresentation.Y + mSpriteVisibleRepresentation.ScaleY;

                //return GuiManager.Cursor.IsOn(mSpriteVisibleRepresentation);
            }
        }

        public string Label
        {
            get { return mText.DisplayText; }
            set 
            {
                string text = value;
                if (!string.IsNullOrEmpty(value))
                {
                    text = StringFunctions.InsertSpacesInCamelCaseString(value);
                }


                mText.DisplayText = text;

                float radius = 1;

                if (mCircleVisibleRepresentation != null)
                {
                    radius = mCircleVisibleRepresentation.Radius;
                }
                else
                {
                    radius = mSpriteVisibleRepresentation.ScaleX;
                }

                mText.InsertNewLines(radius * 2);
                mText.SetPixelPerfectScale(mLayer);
            }
        }

        public int NodeHierarchyDepth
        {
            get { return mIAttachableRepresenting.HierarchyDepth; }
        }

        public IAttachable ObjectRepresenting
        {
            get { return mIAttachableRepresenting; }
            set
            {
                mIAttachableRepresenting = value;
            }
        }

        public float Width
        {
            get
            {
                if (mLastTimeWidthCalculated == TimeManager.CurrentTime)
                {
                    return mCachedWidth;
                }
                else
                {
                    mLastTimeWidthCalculated = TimeManager.CurrentTime;
                    if (mChildren.Count == 0)
                    {
                        mCachedWidth = 3;
                    }
                    else
                    {
                        float totalWidth = 0;

                        foreach (HierarchyNode child in mChildren)
                        {
                            totalWidth += child.Width;
                        }

                        mCachedWidth = totalWidth;
                    }

                    return mCachedWidth;
                }
            }
        }

        public float TextRed
        {
            get { return mText.Red; }
            set { mText.Red = value; }
        }

        public float TextGreen
        {
            get { return mText.Green; }
            set { mText.Green = value; }
        }

        public float TextBlue
        {
            get { return mText.Blue; }
            set { mText.Blue = value; }
        }

        #endregion

        #region Methods

        public HierarchyNode(VisibleRepresentationType visibleRepresentationType)
        {
            SpriteManager.AddPositionedObject(this);

            if (visibleRepresentationType == VisibleRepresentationType.Circle)
            {
                mCircleVisibleRepresentation = ShapeManager.AddCircle();
            }
            else
            {
                mSpriteVisibleRepresentation = SpriteManager.AddSprite((Texture2D)null);

#if FRB_MDX
                mSpriteVisibleRepresentation.ColorOperation = Microsoft.DirectX.Direct3D.TextureOperation.SelectArg2;

#else
                mSpriteVisibleRepresentation.ColorOperation = ColorOperation.Color;
#endif
                mSpriteVisibleRepresentation.Red = 1;
                mSpriteVisibleRepresentation.Green = 1;
                mSpriteVisibleRepresentation.Blue = 1;
                
            }

            mParentLine = ShapeManager.AddLine();
            mParentLine.Visible = false;

            mParentAttachmentPoint = ShapeManager.AddCircle();
            mParentAttachmentPoint.Visible = false;
            mParentAttachmentPoint.Radius = .2f;

            mText = TextManager.AddText("");
            mText.Blue = 1;
            mText.Green = 1;
            mText.Red = 0;

            mText.HorizontalAlignment = HorizontalAlignment.Center;

        }

        public void AddToLayer(Layer layer)
        {
            mLayer = layer;
            if (layer != null)
            {
                if (layer.CameraBelongingTo == null)
                {
                    mText.CameraToAdjustPixelPerfectTo = SpriteManager.Camera;
                }
                else
                {
                    mText.CameraToAdjustPixelPerfectTo = layer.CameraBelongingTo;
                }
            }

            if (mCircleVisibleRepresentation != null)
            {
                ShapeManager.AddToLayer(mCircleVisibleRepresentation, layer);
            }
            else
            {
                SpriteManager.AddToLayer(mSpriteVisibleRepresentation, layer);

            }
            ShapeManager.AddToLayer(mParentLine, layer);
            ShapeManager.AddToLayer(mParentAttachmentPoint, layer);
            TextManager.AddToLayer(mText, layer);
            mText.SetPixelPerfectScale(mLayer);
        }

        public void Destroy()
        {
            SpriteManager.RemovePositionedObject(this);

            if (mCircleVisibleRepresentation != null)
            {

                ShapeManager.Remove(mCircleVisibleRepresentation);
            }
            else
            {
                SpriteManager.RemoveSprite(mSpriteVisibleRepresentation);
            }
            ShapeManager.Remove(mParentLine);
            TextManager.RemoveText(mText);
        }

        public void SetRelativeX()
        {
            if (Parent == null)
            {
                return;
            }

            float parentWidth = ((HierarchyNode)Parent).Width;

            int thisIndexAsChild = Parent.Children.IndexOf(this);

            float runningRelativeX = 0;

            for (int i = 0; i < thisIndexAsChild; i++)
            {
                runningRelativeX += ((HierarchyNode)Parent.Children[i]).Width;
            }

            this.RelativeX = -parentWidth / 2 + runningRelativeX + this.Width / 2.0f;


        }

        public bool UpdateElementVisibility(HierarchyNode parentNode)
        {
            bool didChange = false;

            if (mCircleVisibleRepresentation != null)
            {
                mCircleVisibleRepresentation.Position = this.Position;
            }
            else
            {
                mSpriteVisibleRepresentation.Position = this.Position;
            }
            mText.Position = this.Position;

            #region Update the attachment visibility

            bool shouldConnectionBeVisible = parentNode != null;

            if (shouldConnectionBeVisible != mParentLine.Visible)
            {

                mParentLine.Visible = shouldConnectionBeVisible;
                mParentAttachmentPoint.Visible = mParentLine.Visible;
                didChange = true;
            }

            #endregion

            #region If visible, update the position of the line itself

            if (mParentLine.Visible)
            {
                Vector3 vectorToParent = this.Position - parentNode.Position;

                if (vectorToParent.X == 0 && vectorToParent.Y == 0 && vectorToParent.Z == 0)
                {
                    return didChange;
                }


                float radius = 0;

                if (mCircleVisibleRepresentation != null)
                {
                    radius = mCircleVisibleRepresentation.Radius;
                }
                else
                {
                    radius = mSpriteVisibleRepresentation.ScaleX;
                }

                vectorToParent.Normalize();

                Vector3 endPoint1 = this.Position - vectorToParent * radius;

                Vector3 endPoint2 = parentNode.Position + vectorToParent * radius;

                if (mParentAttachmentPoint.Position != endPoint2)
                {
                    didChange = true;
                }
                mParentAttachmentPoint.Position = endPoint2;

                mParentLine.SetFromAbsoluteEndpoints(endPoint1, endPoint2);
            }

            #endregion

            return didChange;
        }


        #endregion
    }
}
