using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Entities
{
    #region Enums

    public enum CameraBehaviorType
    {
        PositionLocking,
    }
    #endregion

    // Influenced by https://www.gamedeveloper.com/design/scroll-back-the-theory-and-practice-of-cameras-in-side-scrollers
    public class CameraControllingEntity : PositionedObject
    {
        #region Fields/Properties

        bool hasActivityBeenCalled = false;
        private float defaultOrthoWidth;
        private float defaultOrthoHeight;
        private float minZoomPercent;
        private bool isAutoZoomEnabled;
        private float furthestZoom;


        /// <summary>
        /// Defines the behavior of the camera when determining its target position. 
        /// Currently it always performs PositionLocking, but additional types of bhehavior may be added in the future.
        /// </summary>
        public CameraBehaviorType BehaviorType { get; set; }

        // August 24, 2021
        // Vic asks - Targets used to be an IList instead of IList<PositionedObject>. Why? Was this intentional?
        // Changing it to IList of PositionedObject.
        // Update - the reason is because we assign PositionedObjectList<PositionedObjectType> which does not implement
        // IList<PositionedObject>
        /// <summary>
        /// The target PositionedObjects to follow. In a single-player game this can be one entity. In a multi-player game, this can 
        /// be all players. The camera will average their position and follow the average.
        /// </summary>
        public System.Collections.IList Targets { get; set; } = new List<PositionedObject>();

        /// <summary>
        /// Sets a single target for following. If Targets has been previously set, this changes
        /// the Targets to a new list.
        /// </summary>
        /// <remarks>
        /// If the target is an Entity, then the entity is destroyed, it will be removed as the CameraControllingEntity target.
        /// </remarks>
        public PositionedObject Target
        {
            set
            {
                Targets = new PositionedObjectList<PositionedObject>();
                Targets.Add(value);
            }
        }

        /// <summary>
        /// The level map. If null, the camera will move without bounds. If set, the camera will not view positions outside of the map.
        /// </summary>
        public IPositionedSizedObject Map { get; set; }

        /// <summary>
        /// Extra padding which can be used to add a buffer between the edge of the actual map and the
        /// desired visible edge. A positive value adds padding, effective shrinking the available area
        /// that the camera can view. A negative value allows the camera to move outside of the map.
        /// </summary>
        public float ExtraMapPadding { get; set; }

        /// <summary>
        /// Whether to smoothly approach the target location. If false, the camera follows the entity without any smoothing.
        /// </summary>
        public bool LerpSmooth { get; set; } = true;

        /// <summary>
        /// The amount of smoothing. The larger the number, the longer the camera takes to smooth. This value is ignored if LerpSmooth is false.
        /// </summary>
        /// <remarks>
        /// Mathematically this is the velocity value per pixel offset from the target. For example, if this value is 5, and the target is 20 pixels away,
        /// then the velocity of the camera will be 20*5 = 100.
        /// </remarks>
        public float LerpCoefficient { get; set; } = 5;

        /// <summary>
        /// Whether to snap the camera position to the screen pixel. This value can be used to prevent half-pixels from being drawn.
        /// </summary>
        public bool SnapToPixel { get; set; } = true;

        /// <summary>
        /// The offset to apply when snapping to pixel. This can be used to improve rendering depending on the pixel that is sampled by graphics cards.
        /// </summary>
        /// <remarks>
        /// This value has only been tested on a limited number of devices/games. It's not clear if this should be a fixed value, or if it should depend
        /// </remarks>
        public float SnapToPixelOffset { get; set; } = .25f;

        /// <summary>
        /// Whether to perform logic in the Activity call. This exists to allow control over whether Activity
        /// should apply if Activity is called in generated code. This can be set to false to manually override
        /// the camera following behavior.
        /// </summary>
        public bool IsActive { get; set; } = true;

        public float ScrollingWindowWidth { get; set; }
        public float ScrollingWindowHeight { get; set; }

        bool visible;
        AxisAlignedRectangle windowVisualization;
        public bool Visible
        {
            get
            {
                return visible;
            }
            set
            {
                if(value != visible)
                {
                    visible = value;

                    if(visible)
                    {
                        windowVisualization = new AxisAlignedRectangle();
                        windowVisualization.Name = "CameraControllingEntity WindowVisualization Rectangle";
                        windowVisualization.AttachTo(this);
                        ShapeManager.AddAxisAlignedRectangle(windowVisualization);
                    }
                    else
                    {
                        if(windowVisualization != null)
                        {
                            ShapeManager.Remove(windowVisualization);
                            windowVisualization = null;
                        }
                    }
                }
            }
        }

        #endregion

        public void EnableAutoZooming(float defaultOrthoWidth, float defaultOrthoHeight, float minZoomPercent, float furthestZoom = 0)
        {
            this.defaultOrthoWidth = defaultOrthoWidth;
            this.defaultOrthoHeight = defaultOrthoHeight;
            this.minZoomPercent = minZoomPercent;
            this.isAutoZoomEnabled = true;
            this.furthestZoom = furthestZoom;
        }

        public void Activity()
        {
            ///////////////////Early Out/////////////////////
            if (!IsActive || Targets == null)
            {
                return;
            }
            //////////////////End Early Out//////////////////

            if(windowVisualization != null)
            {
                windowVisualization.Width = ScrollingWindowWidth;
                windowVisualization.Height = ScrollingWindowHeight;
            }


            var target = GetTarget();



            ApplyTarget(target, hasActivityBeenCalled ? LerpSmooth : false);

            if(isAutoZoomEnabled)
            {
                ApplyZoom();
            }

            hasActivityBeenCalled = true;
        }

        private void ApplyZoom()
        {
            var separationVector = GetTargetSeparation();
            ApplySeparationForZoom(separationVector);
        }

        /// <summary>
        /// Returns the desired position of the camera (X and Y) given the position of all Targets and the bounds of the camera.
        /// </summary>
        /// <returns>The target X and Y of the camera.</returns>
        public Vector2 GetTarget()
        {
            #region Get the average position of all the target instances

            Vector2 averagePosition = this.Position.ToVector2();

            if(Targets.Count > 0)
            {
                averagePosition = Vector2.Zero;
                foreach (PositionedObject targetAtI in Targets)
                {
                    averagePosition.X += targetAtI.X;
                    averagePosition.Y += targetAtI.Y;
                }

                averagePosition.X /= Targets.Count;
                averagePosition.Y /= Targets.Count;
            }

            #endregion

            Vector2 target = Vector2.Zero;
            //#region Convert the average positions to a target given the BehaviorType

            //switch (BehaviorType)
            //{
            //    case CameraBehaviorType.PositionLocking:
            //        target.X = averagePosition.X;
            //        target.Y = averagePosition.Y;
            //        break;
            //}

            //#endregion



            // Compare the target with the window
            var windowWidthHalf = ScrollingWindowWidth / 2.0f;
            var windowHeightHalf = ScrollingWindowHeight / 2.0f;

            var effectiveThis = this.Parent ?? this;

            var windowLeft = effectiveThis.X - windowWidthHalf;
            var windowRight = effectiveThis.X + windowWidthHalf;

            var windowBottom = effectiveThis.Y - windowHeightHalf;
            var windowTop = effectiveThis.Y + windowHeightHalf;

            if(averagePosition.X < windowLeft)
            {
                target.X = averagePosition.X + windowWidthHalf;
            }
            else if(averagePosition.X > windowRight)
            {
                target.X = averagePosition.X - windowWidthHalf;
            }
            else
            {
                target.X = effectiveThis.X;
            }

            if(averagePosition.Y < windowBottom)
            {
                target.Y = averagePosition.Y + windowHeightHalf;
            }
            else if(averagePosition.Y > windowTop)
            {
                target.Y = averagePosition.Y - windowHeightHalf;
            }
            else
            {
                target.Y = effectiveThis.Y;
            }

            #region Limit the target position based on the map

            if (Map != null)
            {
                // window sizes allow the target to be closer to the edge. For example, if the
                // window were the size of the actual screen, then the target could go all the way
                // to the edge and still be in the window
                var effectivePaddingX = ExtraMapPadding;
                var effectivePaddingY = ExtraMapPadding;


                var mapLeft = Map.Left + effectivePaddingX;
                var mapRight = Map.Left + Map.Width - effectivePaddingX;

                var mapBottom = Map.Top - Map.Height + effectivePaddingY;
                var mapTop = Map.Top - effectivePaddingY;

                if (Camera.Main.OrthogonalWidth > Map.Width)
                {
                    target.X = mapLeft + Map.Width / 2;
                }
                else
                {
                    target.X = System.Math.Max(target.X, mapLeft + Camera.Main.OrthogonalWidth / 2);
                    target.X = System.Math.Min(target.X, mapRight - Camera.Main.OrthogonalWidth / 2);
                }

                if (Camera.Main.OrthogonalHeight > Map.Height)
                {
                    target.Y = mapBottom + Map.Height / 2;
                }
                else
                {
                    target.Y = System.Math.Max(target.Y, mapBottom + Camera.Main.OrthogonalHeight / 2);
                    target.Y = System.Math.Min(target.Y, mapTop - Camera.Main.OrthogonalHeight / 2);
                }
            }

            #endregion

            return target;
        }

        private Vector2 GetTargetSeparation()
        {
            //////// Early Out///////////////
            if(Targets.Count == 0)
            {
                return Vector2.Zero;
            }
            //////End Early Out//////////////

            var firstTargetPosition = (Targets[0] as PositionedObject).Position;

            Vector2 min = firstTargetPosition.ToVector2();
            Vector2 max = min;

            for(int i = 1; i < Targets.Count; i++)
            {
                var atI = Targets[i] as PositionedObject;

                if (atI.X < min.X) min.X = atI.X;
                if (atI.X > max.X) max.X = atI.X;

                if (atI.Y < min.Y) min.Y = atI.Y;
                if (atI.Y > max.Y) max.Y = atI.Y;
            }

            return max - min;
        }

        public void ForceToTarget()
        {
            var target = GetTarget();
            ApplyTarget(target, lerpSmooth: false);
        }

        public void ApplyTarget(Vector2 target, bool lerpSmooth = true)
        {
            var effectiveThis = this.Parent ?? this;

            if (lerpSmooth == false)
            {
                effectiveThis.Position.X = target.X;
                effectiveThis.Position.Y = target.Y;
            }
            else
            {
                float xDifference = 0;
                float yDifference = 0;

                xDifference = target.X - effectiveThis.Position.X;
                yDifference = target.Y - effectiveThis.Position.Y;

                effectiveThis.Velocity.X = xDifference * LerpCoefficient;
                effectiveThis.Velocity.Y = yDifference * LerpCoefficient;
            }


            if (SnapToPixel)
            {
                var zoom = Camera.Main.DestinationRectangle.Height / Camera.Main.OrthogonalHeight;

                var invertZoom = 1 / zoom;

                Camera.Main.X = MathFunctions.RoundFloat(effectiveThis.X, invertZoom) + SnapToPixelOffset * invertZoom;
                Camera.Main.Y = MathFunctions.RoundFloat(effectiveThis.Y, invertZoom) + SnapToPixelOffset * invertZoom;

            }
            else
            {
                Camera.Main.X = effectiveThis.X;
                Camera.Main.Y = effectiveThis.Y;
            }
        }

        public void ApplySeparationForZoom(Vector2 separationVector)
        {
            // for now we'll assume a padding:
            float noZoomRatio = .8f;
            float noZoomDistance = System.Math.Min(defaultOrthoWidth, defaultOrthoHeight) * noZoomRatio;

            var currentSeparationDistance = separationVector.Length();

            if(currentSeparationDistance > noZoomDistance)
            {
                var newZoom = System.Math.Max(furthestZoom, currentSeparationDistance / noZoomDistance);
                Camera.Main.OrthogonalHeight = defaultOrthoHeight * newZoom;
            }
            else
            {
                Camera.Main.OrthogonalHeight = defaultOrthoHeight;
            }
            Camera.Main.FixAspectRatioYConstant();
        }
    }
}
