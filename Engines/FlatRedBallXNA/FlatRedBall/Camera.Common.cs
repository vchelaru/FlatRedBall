using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics;
using FlatRedBall.Math;
#if FRB_MDX
using Microsoft.DirectX;
using System.Drawing;
#else
using Microsoft.Xna.Framework;

#endif

namespace FlatRedBall
{
    public partial class Camera
    {
        #region Enums
        public enum CoordinateRelativity
        {
            RelativeToWorld,
            RelativeToCamera
        }

        public enum SplitScreenViewport
        {
            FullScreen,
            TopHalf,
            BottomHalf,
            LeftHalf,
            RightHalf,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight,

            LeftThird,
            MiddleThird,
            RightThird
        }

        #endregion

        #region Fields


        float mYEdge;
        float mXEdge;

        float mTopDestination;
        float mBottomDestination;
        float mLeftDestination;
        float mRightDestination;

        internal Rectangle mDestinationRectangle;

        float mTopDestinationVelocity;
        float mBottomDestinationVelocity;
        float mLeftDestinationVelocity;
        float mRightDestinationVelocity;


        float mFieldOfView;
        float mAspectRatio;

        bool mDrawsWorld = true;
        bool mDrawsCameraLayer = true;
        bool mDrawsShapes = true;
        #endregion

        #region Properties

        public static Camera Main
        {
            get
            {
                return SpriteManager.Camera;
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets the top side of the destination rectangle (where on the window
        /// the camera will display).  Measured in pixels.  Destination uses an inverted Y (positive points down).
        /// </summary>
        #endregion
        public virtual float TopDestination
        {
            get { return mTopDestination; }
            set 
            { 
                mTopDestination = value;
                mUsesSplitScreenViewport = false;
                UpdateDestinationRectangle(); 
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets the bottom side of the destination rectangle (where on the window
        /// the camera will display).  Measured in pixels.   Destination uses an inverted Y (positive points down).
        /// </summary>
        #endregion
        public virtual float BottomDestination
        {
            get { return mBottomDestination; }
            set 
            { 
                mBottomDestination = value;
                mUsesSplitScreenViewport = false;
                UpdateDestinationRectangle();
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets the left side of the destination rectangle (where on the window
        /// the camera will display).  Measured in pixels.
        /// </summary>
        #endregion
        public virtual float LeftDestination
        {
            get { return mLeftDestination; }
            set 
            { 
                mLeftDestination = value;
                mUsesSplitScreenViewport = false;
                UpdateDestinationRectangle();
            }
        }

        #region XML Docs
        /// <summary>
        /// Gets and sets the right side of the destination rectangle (where on the window
        /// the camera will display).  Measured in pixels.
        /// </summary>
        #endregion
        public virtual float RightDestination
        {
            get { return mRightDestination; }
            set 
            { 
                mRightDestination = value;
                mUsesSplitScreenViewport = false;
                UpdateDestinationRectangle();
            }
        }

        #region XML Docs
        /// <summary>
        /// Represents the top left justified area the Camera will draw over.
        /// </summary>
        /// <remarks>
        /// This represents the area in pixel coordinates that the camera will display relative
        /// to the top left of the owning Control.  If the Control is resized, the camera should modify
        /// its DestinationRectangle to match the new area.
        /// 
        /// <para>
        /// Multiple cameras with different DestinationRectangles can be used to display split screen
        /// or picture-in-picture.
        /// </para>
        /// </remarks>
        #endregion
        public virtual Rectangle DestinationRectangle
        {
            get
            {
                return mDestinationRectangle;
            }
            set
            {
                mUsesSplitScreenViewport = false;

                mDestinationRectangle = value;

                mTopDestination = mDestinationRectangle.Top;
                mBottomDestination = mDestinationRectangle.Bottom;
                mLeftDestination = mDestinationRectangle.Left;
                mRightDestination = mDestinationRectangle.Right;

                FixAspectRatioYConstant();
            }
        }

        /// <summary>
        /// Returns the absolute X value of the right edge of the visible area for this camera at Z = 0.
        /// </summary>
        public float AbsoluteRightXEdge => AbsoluteRightXEdgeAt(0);

        /// <summary>
        /// Returns the right-most visible X value at a given absolute Z value.
        /// The absoluteZ parameter is ignored if the camera has its Orthogonal = true (is 2D)
        /// </summary>
        /// <param name="absoluteZ">The absolute Z to use for determining the right-edge.</param>
        /// <returns>The furthest-right visible X value at the given absolute Z.</returns>
        public float AbsoluteRightXEdgeAt(float absoluteZ)
        {
            return Position.X + RelativeXEdgeAt(absoluteZ);
        }

        /// <summary>
        /// Returns the absolute X value of the left edge of the visible area for this camera at Z = 0.
        /// </summary>
        public float AbsoluteLeftXEdge => AbsoluteLeftXEdgeAt(0);

        /// <summary>
        /// Returns the left-most visible X value at a given absolute Z value.
        /// The absoluteZ parameter is ignored if the camera has its Orthogonal = true (is 2D)
        /// </summary>
        /// <param name="absoluteZ">The absolute Z to use for determing the left-edge.</param>
        /// <returns>The furthest-left visible X value at the given absolute Z.</returns>
        public float AbsoluteLeftXEdgeAt(float absoluteZ)
        {
            return Position.X - RelativeXEdgeAt(absoluteZ);
        }

        /// <summary>
        /// Returns the absolute Y value of the top edge of the visible area for this camera at Z = 0.
        /// </summary>
        public float AbsoluteTopYEdge => AbsoluteTopYEdgeAt(0);

        /// <summary>
        /// Returns the top-most visible Y value at a given absolute Z value.
        /// The absoluteZ parameter is ignored if the camera has its Orthogonal = true (is 2D)
        /// </summary>
        /// <param name="absoluteZ">The absolute Z to use for determining the top-edge.</param>
        /// <returns>The furthest-top visible Y value at the given absolute Z.</returns>
        public float AbsoluteTopYEdgeAt(float absoluteZ)
        {
            return Position.Y + RelativeYEdgeAt(absoluteZ);
        }

        /// <summary>
        /// Returns the absolute Y value of the bottom edge of the visible area for this camera at Z = 0.
        /// </summary>
        public float AbsoluteBottomYEdge => AbsoluteBottomYEdgeAt(0);

        /// <summary>
        /// Returns the bottom-most visible Y value at a given absolute Z value.
        /// The absoluteZ parameter is ignored if the camera has its Orthogonal = true (is 2D)
        /// </summary>
        /// <param name="absoluteZ">The absolute Z to use for determining the bottom-edge.</param>
        /// <returns>The furthest-bottom visible Y value at the given absolute Z.</returns>
        public float AbsoluteBottomYEdgeAt(float absoluteZ)
        {
            return Position.Y - RelativeYEdgeAt(absoluteZ);
        }

        public bool ClearsDepthBuffer
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// The width/height of the view of the camera
        /// </summary>
        /// <remarks>
        /// This determines the ratio of the width to height of the camera.  By default, the aspect ratio is 4/3,
        /// but this should be changed for widescreen monitors or in situations using multiple cameras.  For example, if
        /// a game is in split screen with a vertical split, then each camera will show the same height, but half the width.
        /// The aspect ratio should be 2/3.
        /// </remarks>
        #endregion
        public float AspectRatio
        {
            get { return mAspectRatio; }
            set
            {
                mAspectRatio = value;
                mXEdge = mYEdge * AspectRatio;
                // The user may expect AspectRatio to work when in 2D mode
                if (mOrthogonal)
                {
                    mOrthogonalWidth = mOrthogonalHeight * mAspectRatio;
                }
            }
        }


        /// <summary>
        /// Returns whether the camera is using an orthogonal perspective. If true, the camera is 2D.
        /// </summary>
        public bool Orthogonal
        {
            get => mOrthogonal; 
            set => mOrthogonal = value; 
        }



        /// <summary>
        /// Whether the camera draws its layers.
        /// </summary>
        public bool DrawsCameraLayer
        {
            get { return mDrawsCameraLayer; }
            set { mDrawsCameraLayer = value; }
        }

        /// <summary>
        /// Whether the Camera draws world objects (objects not on the Camera's Layer). This is true by default.
        /// This is usually set to false for cameras used in render targets which only draw layers.
        /// </summary>
        public bool DrawsWorld
        {
            get { return mDrawsWorld; }
            set { mDrawsWorld = value; }
        }

        #region XML Docs
        /// <summary>
        /// Whether the Camera draws shapes
        /// </summary>
        #endregion
        public bool DrawsShapes
        {
            get { return mDrawsShapes; }
            set { mDrawsShapes = value; }
        }

        public bool DrawsToScreen
        {
            get;
            set;
        }

        #endregion

        #region XML Docs
        /// <summary>
        /// Sets the aspectRatio to match the width/height of the area that the camera is drawing to.
        /// </summary>
        /// <remarks>
        /// This is usually used in applications with split screen or when on a widescreen display.
        /// </remarks>
        #endregion
        public void FixAspectRatioYConstant()
        {
            // We may have a 0-height
            // DestinationRectangle in
            // test scenarios.
            if (mDestinationRectangle.Height != 0)
            {
                this.AspectRatio = (float)mDestinationRectangle.Width / (float)mDestinationRectangle.Height;

                mOrthogonalWidth = mOrthogonalHeight * mAspectRatio;
            }
        }


        public void FixAspectRatioXConstant()
        {
            float oldWidth = mOrthogonalWidth;
            float newAspectRatio = mDestinationRectangle.Width / (float)mDestinationRectangle.Height;
            this.FieldOfView *= newAspectRatio / mAspectRatio;
            AspectRatio = newAspectRatio;

            mOrthogonalWidth = oldWidth;
            mOrthogonalHeight = mOrthogonalWidth / mAspectRatio;

        }

        /// <summary>
        /// Checks if an Entity is within the Camera's boundaries.
        /// </summary>
        /// <param name="entity">The entity to check</param>
        /// <param name="buffer">A buffer zone to account for entity size on boundary check. Defaults to 0</param>
        /// <returns>Returns true in case it is inside the camera's boundaries, false otherwise</returns>
        public bool IsEntityInView(PositionedObject entity, float buffer = 0)
        {
            return IsEntityInView(entity.Position, buffer);
        }

        /// <summary>
        /// Checks if an object with Vector3 position is inside the Camera's boundaries.
        /// </summary>
        /// <param name="position">The position of the entity to check</param>
        /// <param name="buffer">A buffer zone to account for entity size on boundary check. Defaults to 0</param>
        /// <returns>Returns true in case it is inside the camera's boundaries, false otherwise</returns>
        public bool IsEntityInView(Vector3 position, float buffer = 0)
        {
            var z = position.Z;

            bool isOnScreen = position.X < Camera.Main.AbsoluteRightXEdgeAt(z) + buffer &&
                                position.X > Camera.Main.AbsoluteLeftXEdgeAt(z) - buffer &&
                                position.Y < Camera.Main.AbsoluteTopYEdgeAt(z) + buffer &&
                                position.Y > Camera.Main.AbsoluteBottomYEdgeAt(z) - buffer;

            return isOnScreen;
        }

        public bool IsSpriteInView(Sprite sprite)
        {
            return IsSpriteInView(sprite, false);
        }

        static float mLongestDimension;
        static float mDistanceFromCamera;
        /// <summary>
        /// Returns whether the argument sprite is in view, considering the CameraCullMode. This will always return
        /// true if cull mode is set to None.
        /// </summary>
        /// <remarks>
        /// This method does not do a perfectly precise check of whether the Sprite is on screen or not, as such
        /// a check would require considering the Sprite's rotation. Instead, this uses approximations to avoid
        /// trigonometric functions, and will err on the side of returning true when a Sprite may actually be out
        /// of view.
        /// </remarks>
        /// <param name="sprite">The sprite to check in view</param>
        /// <param name="relativeToCamera">Whether the sprite's position is relative to the camera. This value may be true if the 
        /// sprite is on a Layer, and the Layer's RelativeToCamera value is true.</param>
        /// <returns>Whether the sprite is in view</returns>
        /// <seealso cref="FlatRedBall.Graphics.Layer.RelativeToCamera"/>
        public bool IsSpriteInView(Sprite sprite, bool relativeToCamera)
        {
            switch (CameraCullMode)
            {

                case CameraCullMode.UnrotatedDownZ:
                    {
                        mLongestDimension = (float)(System.Math.Max(sprite.ScaleX, sprite.ScaleY) * 1.42f);


                        if (mOrthogonal)
                        {
                            if (relativeToCamera)
                            {
                                if (System.Math.Abs(sprite.X) - mLongestDimension > mOrthogonalWidth)
                                {
                                    return false;
                                }
                                if (System.Math.Abs(sprite.Y) - mLongestDimension > mOrthogonalHeight)
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                if (System.Math.Abs(X - sprite.X) - mLongestDimension > mOrthogonalWidth)
                                {
                                    return false;
                                }
                                if (System.Math.Abs(Y - sprite.Y) - mLongestDimension > mOrthogonalHeight)
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            // Multiply by 1.5 to increase the range in case the Camera is rotated
                            if (relativeToCamera)
                            {
                                mDistanceFromCamera = (-sprite.Z) / 100.0f;

                                if (System.Math.Abs(sprite.X) - mLongestDimension > mXEdge * 1.5f * mDistanceFromCamera)
                                {
                                    return false;
                                }
                                if (System.Math.Abs(sprite.Y) - mLongestDimension > mYEdge * 1.5f * mDistanceFromCamera)
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                mDistanceFromCamera = (Z - sprite.Z) / 100.0f;

                                if (System.Math.Abs(X - sprite.X) - mLongestDimension > mXEdge * 1.5f * mDistanceFromCamera)
                                {
                                    return false;
                                }
                                if (System.Math.Abs(Y - sprite.Y) - mLongestDimension > mYEdge * 1.5f * mDistanceFromCamera)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    return true;
                case CameraCullMode.None:
                    return true;
            }
            return true;

        }

        public bool IsTextInView(Text text, bool relativeToCamera)
        {
            if(this.CameraCullMode == Graphics.CameraCullMode.UnrotatedDownZ)
            {
                float cameraLeft;
                float cameraRight;
                float cameraTop;
                float cameraBottom;

                if(relativeToCamera)
                {
                    cameraLeft = -this.RelativeXEdgeAt(text.Z);
                    cameraRight = this.RelativeXEdgeAt(text.Z);
                    cameraTop = this.RelativeYEdgeAt(text.Z);
                    cameraBottom = -this.RelativeYEdgeAt(text.Z);

                }
                else
                {
                    cameraLeft = this.AbsoluteLeftXEdgeAt(text.Z);
                    cameraRight = this.AbsoluteRightXEdgeAt(text.Z);
                    cameraTop = this.AbsoluteTopYEdgeAt(text.Z);
                    cameraBottom = this.AbsoluteBottomYEdgeAt(text.Z);
                }
                float textVerticalCenter = text.VerticalCenter;
                float textHorizontalCenter = text.HorizontalCenter;

                float longestCenterToEdge
                    = (float)(System.Math.Max(text.Width, text.Height) * 1.42f/2.0f);


                float textLeft = textHorizontalCenter - longestCenterToEdge;
                float textRight = textHorizontalCenter + longestCenterToEdge;
                float textTop = textVerticalCenter + longestCenterToEdge;
                float textBottom = textVerticalCenter - longestCenterToEdge;

                return textRight > cameraLeft &&
                    textLeft < cameraRight &&
                    textBottom < cameraTop &&
                    textTop > cameraBottom;                
            }


            return true;
        }

        public bool IsPointInView(double x, double y, double absoluteZ)
        {
            if (mOrthogonal)
            {
                return (x > Position.X - mOrthogonalWidth / 2.0f && x < Position.X + mOrthogonalWidth / 2.0f) &&
                    y > Position.Y - mOrthogonalHeight / 2.0f && y < Position.Y + mOrthogonalHeight / 2.0f;
            }
            else
            {
#if FRB_MDX
                double cameraDistance = (absoluteZ - Position.Z) / 100.0;
#else
                double cameraDistance = (Position.Z - absoluteZ) / 100.0;
#endif
                if (x > Position.X - mXEdge * cameraDistance && x < Position.X + mXEdge * cameraDistance &&
                    y > Position.Y - mYEdge * cameraDistance && y < Position.Y + mYEdge * cameraDistance)
                    return true;
                else
                    return false;
            }
        }

        #region XML Docs
        /// <summary>
        /// Determines if the X value is in view, assuming the camera is viewing down the Z axis.
        /// </summary>
        /// <remarks>
        /// Currently, this method assumes viewing down the Z axis.
        /// </remarks>
        /// <param name="x">The absolute X position of the point.</param>
        /// <param name="absoluteZ">The absolute Z position of the point.</param>
        /// <returns></returns>
        #endregion
        public bool IsXInView(double x, double absoluteZ)
        {
            if (mOrthogonal)
            {
                return (x > Position.X - mOrthogonalWidth / 2.0f && x < Position.X + mOrthogonalWidth / 2.0f);
            }
            else
            {
#if FRB_MDX
                double cameraDistance = (absoluteZ - Position.Z) / 100.0;
#else
                double cameraDistance = (Position.Z - absoluteZ) / 100.0;
#endif
                if (x > Position.X - mXEdge * cameraDistance && x < Position.X + mXEdge * cameraDistance)
                    return true;
                else
                    return false;
            }
        }

        /// <summary>
        /// Determines if the Y value is in view, assuming the camera is viewing down the Z axis.
        /// </summary>
        /// <remarks>
        /// Currently, this method assumes viewing down the Z axis.
        /// </remarks>
        /// <param name="y">The absolute Y position of the point.</param>
        /// <param name="absoluteZ">The absolute Z position of the point.</param>
        /// <returns></returns>
        public bool IsYInView(double y, double absoluteZ)
        {
            if (mOrthogonal)
            {
                return y > Position.Y - mOrthogonalHeight / 2.0f && y < Position.Y + mOrthogonalHeight / 2.0f;
            }
            else
            {
#if FRB_MDX
                double cameraDistance = (absoluteZ - Position.Z) / 100.0;
#else
                double cameraDistance = (Position.Z - absoluteZ) / 100.0;
#endif
                if (y > Position.Y - mYEdge * cameraDistance && y < Position.Y + mYEdge * cameraDistance)
                    return true;
                else
                    return false;
            }
        }


        /// <summary>
        /// Returns the number of pixels per unit at the given absolute Z value.  Assumes
        /// that the Camera is unrotated.
        /// </summary>
        /// <remarks>
        /// If using the PixelsPerUnitAt for a rotated camera, use the overload which
        /// takes a Vector3 argument.
        /// </remarks>
        /// <param name="absoluteZ">The absolute Z position.</param>
        /// <returns>The number of pixels per world unit (perpendicular to the camera's forward vector).</returns>
        public float PixelsPerUnitAt(float absoluteZ)
        {
            // June 7, 2011
            // This used to use
            // width values, but
            // that means aspect ratio
            // can screw with these values
            // which we don't want.  Instead
            // we should use height, as that is
            // usually what FRB games use as their
            //// fixed dimension
            //if (mOrthogonal)
            //{
            //    return mDestinationRectangle.Width / mOrthogonalWidth;
            //}
            //else
            //{
            //    return mDestinationRectangle.Width / (2 * RelativeXEdgeAt(absoluteZ));
            //}

            if (mOrthogonal)
            {
                return mDestinationRectangle.Height / mOrthogonalHeight;
            }
            else
            {
                return mDestinationRectangle.Height / (2 * RelativeYEdgeAt(absoluteZ));
            }

        }


        public float PixelsPerUnitAt(ref Vector3 absolutePosition)
        {

            return PixelsPerUnitAt(ref absolutePosition, mFieldOfView, mOrthogonal, mOrthogonalHeight);
        }


        public float PixelsPerUnitAt(ref Vector3 absolutePosition, float fieldOfView, bool orthogonal, float orthogonalHeight)
        {
            if (orthogonal)
            {
                return mDestinationRectangle.Height / orthogonalHeight;
            }
            else
            {
                float distance = Vector3.Dot(
                    (absolutePosition - Position), RotationMatrix.Forward);

                return mDestinationRectangle.Height /
                    (2 * RelativeYEdgeAt(Position.Z + (Math.MathFunctions.ForwardVector3.Z * distance), fieldOfView, mAspectRatio, orthogonal, orthogonalHeight));
            }
        }



        public float RelativeXEdgeAt(float absoluteZ)
        {
            return RelativeXEdgeAt(absoluteZ, mFieldOfView, mAspectRatio, mOrthogonal, mOrthogonalWidth);
        }


        public float RelativeXEdgeAt(float absoluteZ, float fieldOfView, float aspectRatio, bool orthogonal, float orthogonalWidth)
        {
            if (orthogonal)
            {
                return orthogonalWidth / 2.0f;
            }
            else
            {
                float yEdge = (float)(100 * System.Math.Tan(fieldOfView / 2.0));
                float xEdge = yEdge * aspectRatio;
                return xEdge * (absoluteZ - Position.Z) / 100 * FlatRedBall.Math.MathFunctions.ForwardVector3.Z;
            }
        }


        public float RelativeYEdgeAt(float absoluteZ)
        {
            return RelativeYEdgeAt(absoluteZ, mFieldOfView, AspectRatio, Orthogonal, OrthogonalHeight);
        }


        public float RelativeYEdgeAt(float absoluteZ, float fieldOfView, float aspectRatio, bool orthogonal, float orthogonalHeight)
        {
            if (orthogonal)
            {
                // fieldOfView is ignored if it's Orthogonal
                return orthogonalHeight / 2.0f;
            }
            else
            {
                float yEdge = (float)(System.Math.Tan(fieldOfView / 2.0));

                return yEdge * (absoluteZ - Position.Z) / FlatRedBall.Math.MathFunctions.ForwardVector3.Z;
            }
        }


        #region XML Docs
        /// <summary>
        /// Sets the camera to Orthogonal, sets the OrthogonalWidth and
        /// OrthogonalHeight to match the argument values, and can move the
        /// so the bottom-left corner of the screen is at the origin.
        /// </summary>
        /// <param name="moveCornerToOrigin">Whether the camera should be repositioned
        /// so the bottom left is at the origin.</param>
        /// <param name="desiredWidth">The desired unit width of the view.</param>
        /// <param name="desiredHeight">The desired unit height of the view.</param>
        #endregion
        public void UsePixelCoordinates(bool moveCornerToOrigin, int desiredWidth, int desiredHeight)
        {
            this.Orthogonal = true;
            OrthogonalWidth = desiredWidth;
            OrthogonalHeight = desiredHeight;

            if (moveCornerToOrigin)
            {
                X = OrthogonalWidth / 2.0f;
                Y = OrthogonalHeight / 2.0f;
            }
        }

        /// <summary>
        /// Adjusts the camera's Z value so that 1 unit equals 1 pixel at the argument absolute Z value.
        /// Note that objects closer to the camera will appear bigger and objects further will appear smaller.
        /// This function assumes that Orthogonal is set to false.
        /// </summary>
        /// <param name="zToMakePixelPerfect">The absolute Z value to make pixel perfect.</param>
        public void UsePixelCoordinates3D(float zToMakePixelPerfect)
        {
            double distance = GetZDistanceForPixelPerfect();
            this.Z = -MathFunctions.ForwardVector3.Z * (float)(distance) + zToMakePixelPerfect;
            this.FarClipPlane = System.Math.Max(this.FarClipPlane,
                (float)distance * 2);
        }

        public float GetZDistanceForPixelPerfect()
        {
            double sin = System.Math.Sin(FieldOfView / 2.0);
            double cos = System.Math.Cos(FieldOfView / 2.0f);


            double edgeToEdge = 2 * sin;
            float desiredHeight = this.DestinationRectangle.Height;

            double distance = cos * desiredHeight / edgeToEdge;
            return (float)distance;
        }


        public float WorldXAt(float screenX, float zPosition)
        {
            return WorldXAt(screenX, zPosition, this.Orthogonal, this.OrthogonalWidth);
        }

        public float WorldXAt(float screenX, float zPosition, Layer layer)
        {
            if (layer == null || layer.LayerCameraSettings == null)
            {
                return WorldXAt(screenX, zPosition, this.Orthogonal, this.OrthogonalWidth);
            }
            else
            {
                LayerCameraSettings lcs = layer.LayerCameraSettings;

                Camera cameraToUse = layer.CameraBelongingTo;

                if (cameraToUse == null)
                {
                    cameraToUse = this;
                }

                // If the orthogonal resolution per destination width/height
                // of the layer matches the Camera's orthogonal per destination, then
                // we can just use the camera.  This is the most common case so we'll just
                // use that.
                float destinationLeft = cameraToUse.DestinationRectangle.Left;
                float destinationRight = cameraToUse.DestinationRectangle.Right;


                float destinationWidth = destinationRight - destinationLeft;

                float horizontalPercentage = (screenX - destinationLeft) / (float)destinationWidth;

                float orthogonalWidthToUse = cameraToUse.OrthogonalWidth;

                var useLayerOrtho = lcs.Orthogonal && !cameraToUse.Orthogonal;

                if (useLayerOrtho)
                {
                    orthogonalWidthToUse = lcs.OrthogonalWidth;
                }

                // used for adjusting the ortho width/height if the Layer is zoomed
                float layerMultiplier = 1;
                float bottomDestination = lcs.BottomDestination;
                float topDestination = lcs.TopDestination;
                if (bottomDestination == -1 || topDestination == -1)
                {
                    bottomDestination = cameraToUse.BottomDestination;
                    topDestination = cameraToUse.TopDestination;
                }

                // Make sure the destinations aren't equal or else we'd divide by 0
                if (lcs.Orthogonal && bottomDestination != topDestination)
                {
                    layerMultiplier = lcs.OrthogonalHeight / (float)(bottomDestination - topDestination);
                }

                float cameraMultiplier = 1;
                // Make sure the destinations aren't equal or else we'd divide by 0
                if (cameraToUse.Orthogonal && cameraToUse.BottomDestination != cameraToUse.TopDestination)
                {
                    cameraMultiplier = cameraToUse.OrthogonalHeight / (float)(cameraToUse.BottomDestination - cameraToUse.TopDestination);
                }
                layerMultiplier /= cameraMultiplier;

                if(!useLayerOrtho)
                {
                    orthogonalWidthToUse *= layerMultiplier;
                }

                // I think we want to use the Camera's orthogonalWidth if it's orthogonal
                //return GetWorldXGivenHorizontalPercentage(zPosition, cameraToUse, lcs.Orthogonal, lcs.OrthogonalWidth, horizontalPercentage);
                return GetWorldXGivenHorizontalPercentage(zPosition, lcs.Orthogonal, orthogonalWidthToUse, horizontalPercentage);
            }
        }

        public float WorldXAt(float screenX, float zPosition, bool overridingOrthogonal, float overridingOrthogonalWidth)
        {
            float screenRelativeX = screenX;
            return WorldXAt(zPosition, overridingOrthogonal, overridingOrthogonalWidth, screenRelativeX);
        }

        public float WorldXAt(float zPosition, bool overridingOrthogonal, float overridingOrthogonalWidth, float screenX)
        {

            float horizontalPercentage = (screenX - this.DestinationRectangle.Left) / (float)this.DestinationRectangle.Width;

            return GetWorldXGivenHorizontalPercentage(zPosition, overridingOrthogonal, overridingOrthogonalWidth, horizontalPercentage);
        }

        private float GetWorldXGivenHorizontalPercentage(float zPosition, bool overridingOrthogonal, float overridingOrthogonalWidth, float horizontalPercentage)
        {
            if (!overridingOrthogonal)
            {
                float absoluteLeft = this.AbsoluteLeftXEdgeAt(zPosition);
                float width = this.RelativeXEdgeAt(zPosition) * 2;
                return absoluteLeft + width * horizontalPercentage;
            }
            else
            {
                float xDistanceFromEdge = horizontalPercentage * overridingOrthogonalWidth;
                return (this.X + -overridingOrthogonalWidth / 2.0f + xDistanceFromEdge);
            }
        }


        public float WorldYAt(float screenY, float zPosition)
        {
            return WorldYAt(screenY, zPosition, this.Orthogonal, this.OrthogonalHeight);
        }

        public float WorldYAt(float screenY, float zPosition, Layer layer)
        {
            if (layer == null || layer.LayerCameraSettings == null)
            {
                return WorldYAt(screenY, zPosition, this.Orthogonal, this.OrthogonalHeight);
            }
            else
            {
                LayerCameraSettings lcs = layer.LayerCameraSettings;

                Camera cameraToUse = layer.CameraBelongingTo;

                if (cameraToUse == null)
                {
                    cameraToUse = this;
                }


                // If the orthogonal resolution per destination width/height
                // of the layer matches the Camera's orthogonal per destination, then
                // we can just use the camera.  This is the most common case so we'll just
                // use that.
                //return WorldYAt(zPosition, cameraToUse, lcs.Orthogonal, lcs.OrthogonalHeight);
                // If we have a 2D layer ona 3D camera, then we shouldn't use the Camera's orthogonal values
                float orthogonalHeightToUse = cameraToUse.OrthogonalHeight;

                // multiplier is used if the orghogonal height of the layer doesn't match the orthogonal height of the 
                // camera. But if we're going to pass the ortho height of the layer, then the multiplier should be 1

                var usedLayerOrtho = lcs.Orthogonal && !cameraToUse.Orthogonal;

                if (usedLayerOrtho)
                {
                    orthogonalHeightToUse = lcs.OrthogonalHeight;
                }

                float layerMultiplier = 1;
                float bottomDestination = lcs.BottomDestination;
                float topDestination = lcs.TopDestination;
                if (bottomDestination == -1 || topDestination == -1)
                {
                    bottomDestination = cameraToUse.BottomDestination;
                    topDestination = cameraToUse.TopDestination;
                }

                if (lcs.Orthogonal && bottomDestination != topDestination)
                {
                    layerMultiplier = lcs.OrthogonalHeight / (float)(bottomDestination - topDestination);
                }

                float cameraMultiplier = 1;
                if (cameraToUse.Orthogonal && cameraToUse.BottomDestination != cameraToUse.TopDestination)
                {
                    cameraMultiplier = cameraToUse.OrthogonalHeight / (float)(cameraToUse.BottomDestination - cameraToUse.TopDestination);
                }
                layerMultiplier /= cameraMultiplier;

                if(!usedLayerOrtho)
                {
                    orthogonalHeightToUse *= layerMultiplier;
                }

                return WorldYAt(screenY, zPosition, lcs.Orthogonal, orthogonalHeightToUse);
            }
        }

        public float WorldYAt(float screenY, float zPosition, bool overridingOrthogonal, float overridingOrthogonalHeight)
        {
            float screenRelativeY = screenY;

            return WorldYAt(zPosition, overridingOrthogonal, overridingOrthogonalHeight, screenRelativeY);
        }

        public float WorldYAt(float zPosition, bool orthogonal, float orthogonalHeight, float screenY)
        {
            float verticalPercentage = (screenY - this.DestinationRectangle.Top) / (float)this.DestinationRectangle.Height;

            if (!orthogonal)
            {
                float absoluteTop = this.AbsoluteTopYEdgeAt(zPosition);
                float height = this.RelativeYEdgeAt(zPosition) * 2;
                return absoluteTop - height * verticalPercentage;
            }
            else
            {
                float yDistanceFromEdge = verticalPercentage * orthogonalHeight;
                return (this.Y + orthogonalHeight / 2.0f - yDistanceFromEdge);
            }
        }






    }
    
    
}
