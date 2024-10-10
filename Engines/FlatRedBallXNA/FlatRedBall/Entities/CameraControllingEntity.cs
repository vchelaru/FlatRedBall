using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Entities
{
    #region Enums

    public enum TargetApproachStyle
    {
        /// <summary>
        /// The camera moves to the target position immediately, effectively locking on to its position.
        /// </summary>
        Immediate,
        /// <summary>
        /// The camera moves to the target position smoothly, but not at a constant speed. The camera will move faster
        /// if it is further away from the target, and slower if it is closer.
        /// </summary>
        Smooth,
        /// <summary>
        /// The camera moves to the target at a constant speed, regardless of the distance between the camera and the target.
        /// </summary>
        ConstantSpeed
    }

    #endregion

    // Influenced by https://www.gamedeveloper.com/design/scroll-back-the-theory-and-practice-of-cameras-in-side-scrollers
    public class CameraControllingEntity : PositionedObject
    {
        #region Fields/Properties

        /// <summary>
        /// The camera controlled by this instance.
        /// </summary>
        public Camera Camera { get; set; }

        bool hasActivityBeenCalled = false;
        private float defaultOrthoWidth;
        private float defaultOrthoHeight;
        private float minZoomPercent;
        private bool isAutoZoomEnabled;
        private float furthestZoom;
        private AxisAlignedRectangle MaximumViewRectangle = new AxisAlignedRectangle();


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
                if (Targets == null || 
                    // This is a little inefficient but the reason we need this is a user
                    // may use Targets initially and then switch to using a single Target.
                    // If Targets are used, the user may assign the Targets to a list that is
                    // not compatible with the assigned value. For example, Targets could be assigned
                    // to a List<Enemy>, but then the Target is set to a Player. This would result in an
                    // invalid cast operation when the Player is added to the List<Enemy>.
                    Targets is PositionedObjectList<PositionedObject> == false)
                {
                    Targets = new PositionedObjectList<PositionedObject>();
                }
                else
                {
                    Targets.Clear();
                }


                if (value != null)
                {
                    Targets.Add(value);
                }
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

        [Obsolete("This variable is incorrectly named. Lerp means linear interpolation. While this is technically doing a liner interpolation every frame," +
            "the effect is not linear in the end, so this can be confusing. Use TargetApproachStyle instead.")]
        public bool LerpSmooth
        {
            get => TargetApproachStyle == TargetApproachStyle.Smooth;
            set
            {
                if (value)
                {
                    TargetApproachStyle = TargetApproachStyle.Smooth;
                }
                else
                {
                    TargetApproachStyle = TargetApproachStyle.Immediate;
                }
            }
        }

        /// <summary>
        /// The type of approach to use when moving the camera to the target position.
        /// </summary>
        public TargetApproachStyle TargetApproachStyle { get; set; } = TargetApproachStyle.Smooth;

        /// <summary>
        /// Whether to smoothly approach the desired zoom. If false, the camera immediately adjusts zoom without any smoothing.
        /// </summary>
        public bool LerpSmoothZoom { get; set; } = true;

        /// <summary>
        /// The value used to mulitply the OrthogonalWidth and OrthogonalHeight. A larger value means the camera is more zoomed out, and can see more of the game world.
        /// A smaller value means the camera is zoomed in, so it can see less of the game world.
        /// </summary>
        public float ViewableAreaMultiplier { get; private set; } = 1;

        /// <summary>
        /// Returns the maximum possible value that ViewableAreaMultiplier can be set to. This is based on the size of the presence and size of the map.
        /// If Map is null, this returns float.PositiveInfinity.
        /// </summary>
        public float MaxViewableAreaMultiplier
        {
            get
            {
                float maxViewableMultiplier = float.PositiveInfinity;

                if (Map != null)
                {
                    var mapHeight = Map.Height;
                    var mapWidth = Map.Width;

                    var maxViewableMultiplierX = mapWidth / defaultOrthoWidth;
                    var maxViewableMultiplierY = mapHeight / defaultOrthoHeight;

                    maxViewableMultiplier = System.Math.Min(maxViewableMultiplierX, maxViewableMultiplierY);
                }
                return System.Math.Max(1, maxViewableMultiplier);
            }
        }

        /// <summary>
        /// Returns the maximum possible viewable width when the camera is zoomed out as far as possible. This is based on the size of the presence and size of the map.
        /// </summary>
        public float MaxViewableAreaWidth => defaultOrthoWidth * MaxViewableAreaMultiplier;
        /// <summary>
        /// Returns the maximum possible viewable height when the camera is zoomed out as far as possible. This is based on the size of the presence and size of the map.
        /// </summary>
        public float MaxViewableAreaHeight => defaultOrthoHeight * MaxViewableAreaMultiplier;

        public bool IsKeepingTargetsInView { get; set; } = false;

        [Obsolete("Use TargetApproachCoefficient instead, since this value is confusingly named.")]
        public float LerpCoefficient
        {
            get => TargetApproachCoefficient;
            set => TargetApproachCoefficient = value;
        }

        /// <summary>
        /// The amount of smoothing. The larger the number, faster the Camera moves. This value is ignored if TargetApproachStyle is Immediate.
        /// </summary>
        /// <remarks>
        /// If TargetApproachStyle is Smooth, this is the velocity value per pixel offset from the target. For example, if this value is 5, and the target is 20 pixels away,
        /// then the velocity of the camera will be 20*5 = 100. 
        /// If TargetApproachStyle is ConstantSpeed, this is the speed of the camera in pixels per second regardless of the distance to the target.
        /// </remarks>
        public float TargetApproachCoefficient { get; set; } = 5;

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

        public float? CustomSnapToPixelZoom { get; set; } = null;


        /// <summary>
        /// Whether to perform logic in the Activity call. This exists to allow control over whether Activity
        /// should apply if Activity is called in generated code. This can be set to false to manually override
        /// the camera following behavior.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// The width of the scrolling window. If an object is inside of the scrolling window, the CameraControllingEntity will not move the camera.
        /// </summary>
        public float ScrollingWindowWidth { get; set; }

        /// <summary>
        /// The height of the scrolling window. If an object is inside of the scrolling window, the CameraControllingEntity will not move the camera.
        /// </summary>
        public float ScrollingWindowHeight { get; set; }

        bool visible;
        AxisAlignedRectangle windowVisualization;
        /// <summary>
        /// Whether the visualization of the window is visible. This is typically only used to diagnose problems.
        /// </summary>
        public bool Visible
        {
            get
            {
                return visible;
            }
            set
            {
                if (value != visible)
                {
                    visible = value;

                    if (visible)
                    {
                        windowVisualization = new AxisAlignedRectangle();
                        windowVisualization.Name = "CameraControllingEntity WindowVisualization Rectangle";
                        windowVisualization.AttachTo(this);
                        ShapeManager.AddAxisAlignedRectangle(windowVisualization);
                    }
                    else
                    {
                        if (windowVisualization != null)
                        {
                            ShapeManager.Remove(windowVisualization);
                            windowVisualization = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The offset to use when positioning the camera relative to this instance's position. By default this is 0, but setting
        /// a non-zero results in the camera always being positioned by an offset.
        /// </summary>
        /// <example>
        /// Setting this value to have a positive Y results in the Camera's center being above this instance's Y.
        /// </example>
        public Vector3 CameraOffset;

        #endregion

        /// <summary>
        /// Instantiates a new CameraControllingEntity which follows the main Camear (Camera.Main).
        /// </summary>
        public CameraControllingEntity()
        {
            Camera = Camera.Main;
        }

        /// <summary>
        /// Enables auto zooming which will zoom the camera (adjust orthogonal values) to attempt to keep all targets in screen.
        /// </summary>
        /// <param name="defaultOrthoWidth">The default camera orthogonal width. Usually this will be CameraSetup.Data.ResolutionWidth.</param>
        /// <param name="defaultOrthoHeight">The default camera orthogonal height. Usually this will be CameraSetup.Data.ResolutionHeight.</param>
        /// <param name="furthestZoom">The furthest the camera can zoom. A value of 1 prevents any zoom. A value of 2 allows 2 times
        /// as much to be seen. A value of float.PositiveInfinity allows the camera to zoom indefinitely.</param>
        public void EnableAutoZooming(float defaultOrthoWidth, float defaultOrthoHeight, float furthestZoom = float.PositiveInfinity)
        {
            this.defaultOrthoWidth = defaultOrthoWidth;
            this.defaultOrthoHeight = defaultOrthoHeight;
            this.isAutoZoomEnabled = true;
            this.furthestZoom = furthestZoom;
        }

        /// <summary>
        /// Performs every frame activity which updates this instance's position according to its targets and interpolation/zoom values.
        /// This is typically called in generated code if the CameraControllingEntity is part of a Screen in the FRB Editor.
        /// </summary>
        /// <seealso cref="IsActive"/>
        public void Activity()
        {
            ///////////////////Early Out/////////////////////
            if (!IsActive || Targets == null)
            {
                return;
            }
            //////////////////End Early Out//////////////////

            if (windowVisualization != null)
            {
                windowVisualization.Width = ScrollingWindowWidth;
                windowVisualization.Height = ScrollingWindowHeight;
            }

            if (IsKeepingTargetsInView && hasActivityBeenCalled)
            {
                KeepTargetsInView();
            }

            // Zoom should be happening first, and then targeting:
            if (isAutoZoomEnabled)
            {
                ApplyZoom();
            }


            var target = GetTarget();

            var effectiveTargetApproachStyleX =
                TargetApproachStyle.Immediate;
            var effectiveTargetApproachStyleY =
                TargetApproachStyle.Immediate;


            if (hasActivityBeenCalled)
            {
                effectiveTargetApproachStyleX = TargetApproachStyle;
                effectiveTargetApproachStyleY = TargetApproachStyle;
            }

            // Even if we want to lerp, if we are outside of the bounds of the map and if
            // the camera can fit within the map (on either axis) then we don't lerp smooth:
            if (Map != null && (effectiveTargetApproachStyleX != TargetApproachStyle.Immediate))
            {
                var canFitX = Camera.OrthogonalWidth <= Map.Width;
                var canFitY = Camera.OrthogonalHeight <= Map.Height;
                if (canFitX)
                {
                    if (X - Camera.OrthogonalWidth / 2 < Map.Left)
                    {
                        effectiveTargetApproachStyleX = TargetApproachStyle.Immediate;
                    }
                    else if (X + Camera.OrthogonalWidth / 2 > Map.Left + Map.Width)
                    {
                        effectiveTargetApproachStyleX = TargetApproachStyle.Immediate;
                    }
                }
                if (canFitY)
                {
                    if (Y + Camera.OrthogonalHeight / 2 > Map.Top)
                    {
                        effectiveTargetApproachStyleY = TargetApproachStyle.Immediate;
                    }
                    else if (Y - Camera.OrthogonalHeight / 2 < Map.Top - Map.Height)
                    {
                        effectiveTargetApproachStyleY = TargetApproachStyle.Immediate;
                    }
                }
            }

            ApplyTarget(target, effectiveTargetApproachStyleX, effectiveTargetApproachStyleY);

            hasActivityBeenCalled = true;
        }

        private void KeepTargetsInView()
        {
            MaximumViewRectangle.Position = this.Position.AtZ(0);
            MaximumViewRectangle.Width = MaxViewableAreaWidth;
            MaximumViewRectangle.Height = MaxViewableAreaHeight;


            for (int i = 0; i < Targets.Count; i++)
            {
                var target = Targets[i] as PositionedObject;

                if(target != null)
                {
                    if(target.Y > MaximumViewRectangle.Y + MaximumViewRectangle.Height / 2)
                    {
                        target.Y = MaximumViewRectangle.Y + MaximumViewRectangle.Height / 2;
                    }
                    else if(target.Y < MaximumViewRectangle.Y - MaximumViewRectangle.Height / 2)
                    {
                        target.Y = MaximumViewRectangle.Y - MaximumViewRectangle.Height / 2;
                    }

                    if(target.X > MaximumViewRectangle.X + MaximumViewRectangle.Width / 2)
                    {
                        target.X = MaximumViewRectangle.X + MaximumViewRectangle.Width / 2;
                    }
                    else if(target.X < MaximumViewRectangle.X - MaximumViewRectangle.Width / 2)
                    {
                        target.X = MaximumViewRectangle.X - MaximumViewRectangle.Width / 2;
                    }
                }
            }
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

            Vector2 centerOfTargets = this.Position.ToVector2();

            if (Targets.Count > 0)
            {
                var first = (PositionedObject)Targets[0];

                float minX = first.X;
                float maxX = first.X;

                float minY = first.Y;
                float maxY = first.Y;

                if (Map != null)
                {
                    minX = System.Math.Max(minX, Map.Left);
                    maxX = System.Math.Min(maxX, Map.Left + Map.Width);

                    minY = System.Math.Max(minY, Map.Top - Map.Height);
                    maxY = System.Math.Min(maxY, Map.Top);
                }

                for (int i = 1; i < Targets.Count; i++)
                {
                    var positionable = ((PositionedObject)Targets[i]);
                    var position = positionable.Position;

                    if (Map != null)
                    {
                        position.X = System.Math.Max(position.X, Map.Left);
                        position.X = System.Math.Min(position.X, Map.Left + Map.Width);

                        position.Y = System.Math.Max(position.Y, Map.Top - Map.Height);
                        position.Y = System.Math.Min(position.Y, Map.Top);
                    }


                    if (position.X < minX)
                    {
                        minX = position.X;
                    }
                    if (position.X > maxX)
                    {
                        maxX = position.X;
                    }

                    if (position.Y < minY)
                    {
                        minY = position.Y;
                    }
                    if (position.Y > maxY)
                    {
                        maxY = position.Y;
                    }
                }

                centerOfTargets.X = (minX + maxX) / 2.0f;
                centerOfTargets.Y = (minY + maxY) / 2.0f;
            }

            #endregion

            Vector2 target = Vector2.Zero;

            // Compare the target with the window
            var windowWidthHalf = ScrollingWindowWidth / 2.0f;
            var windowHeightHalf = ScrollingWindowHeight / 2.0f;

            var effectiveThis = this.Parent ?? this;

            var windowLeft = effectiveThis.X - windowWidthHalf;
            var windowRight = effectiveThis.X + windowWidthHalf;

            var windowBottom = effectiveThis.Y - windowHeightHalf;
            var windowTop = effectiveThis.Y + windowHeightHalf;

            if (centerOfTargets.X < windowLeft)
            {
                target.X = centerOfTargets.X + windowWidthHalf;
            }
            else if (centerOfTargets.X > windowRight)
            {
                target.X = centerOfTargets.X - windowWidthHalf;
            }
            else
            {
                target.X = effectiveThis.X;
            }

            if (centerOfTargets.Y < windowBottom)
            {
                target.Y = centerOfTargets.Y + windowHeightHalf;
            }
            else if (centerOfTargets.Y > windowTop)
            {
                target.Y = centerOfTargets.Y - windowHeightHalf;
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

                if (Camera.OrthogonalWidth > Map.Width)
                {
                    target.X = mapLeft + Map.Width / 2;
                }
                else
                {
                    target.X = System.Math.Max(target.X, mapLeft + Camera.OrthogonalWidth / 2);
                    target.X = System.Math.Min(target.X, mapRight - Camera.OrthogonalWidth / 2);
                }

                if (Camera.OrthogonalHeight > Map.Height)
                {
                    target.Y = mapBottom + Map.Height / 2;
                }
                else
                {
                    target.Y = System.Math.Max(target.Y, mapBottom + Camera.OrthogonalHeight / 2);
                    target.Y = System.Math.Min(target.Y, mapTop - Camera.OrthogonalHeight / 2);
                }
            }

            #endregion

            return target;
        }

        private Vector2 GetTargetSeparation()
        {
            //////// Early Out///////////////
            if (Targets.Count == 0)
            {
                return Vector2.Zero;
            }
            //////End Early Out//////////////

            var firstTargetPosition = (Targets[0] as PositionedObject).Position;

            Vector2 min = firstTargetPosition.ToVector2();
            Vector2 max = min;

            for (int i = 1; i < Targets.Count; i++)
            {
                var atI = Targets[i] as PositionedObject;

                if (atI.X < min.X) min.X = atI.X;
                if (atI.X > max.X) max.X = atI.X;

                if (atI.Y < min.Y) min.Y = atI.Y;
                if (atI.Y > max.Y) max.Y = atI.Y;
            }

            if (Map != null)
            {
                min.X = System.Math.Max(min.X, Map.Left);
                min.Y = System.Math.Max(min.Y, Map.Top - Map.Height);

                max.X = System.Math.Min(max.X, Map.Left + Map.Width);
                max.Y = System.Math.Min(max.Y, Map.Top);
            }


            return max - min;
        }

        /// <summary>
        /// Immediately gets the destination position according to the current targets and sets this instance's position to that destination.
        /// This can be called to snap to the target position without any smoothing.
        /// </summary>
        public void ForceToTarget()
        {
            var target = GetTarget();
            ApplyTarget(target, lerpSmooth: false);
        }

        public void ApplyTarget(Vector2 target, bool lerpSmooth = true)
        {
            var approachStyle = lerpSmooth ? TargetApproachStyle.Smooth : TargetApproachStyle.Immediate;
            ApplyTarget(target, approachStyle, approachStyle);
        }

        public void ApplyTarget(Vector2 target, bool lerpSmoothX, bool lerpSmoothY)
        {
            var approachStyleX = lerpSmoothX ? TargetApproachStyle.Smooth : TargetApproachStyle.Immediate;
            var approachStyleY = lerpSmoothY ? TargetApproachStyle.Smooth : TargetApproachStyle.Immediate;

            ApplyTarget(target, approachStyleX, approachStyleY);
        }

        public void ApplyTarget(Vector2 target, TargetApproachStyle approachStyleX, TargetApproachStyle approachStyleY)
        {
            var effectiveThis = this.Parent ?? this;


            switch (approachStyleX)
            {
                case TargetApproachStyle.Smooth:
                    effectiveThis.Velocity.X = (target.X - effectiveThis.Position.X) * TargetApproachCoefficient;
                    break;
                case TargetApproachStyle.ConstantSpeed:
                    // todo - need to have a test here to see if we're within a range so we don't overshoot/jitter
                    effectiveThis.Velocity.X = System.Math.Sign(target.X - effectiveThis.Position.X) * TargetApproachCoefficient;
                    break;
                case TargetApproachStyle.Immediate:
                    effectiveThis.Position.X = target.X;
                    break;
            }

            switch (approachStyleY)
            {
                case TargetApproachStyle.Smooth:
                    effectiveThis.Velocity.Y = (target.Y - effectiveThis.Position.Y) * TargetApproachCoefficient;
                    break;
                case TargetApproachStyle.ConstantSpeed:
                    // todo - need to have a test here to see if we're within a range so we don't overshoot/jitter
                    effectiveThis.Velocity.Y = System.Math.Sign(target.Y - effectiveThis.Position.Y) * TargetApproachCoefficient;
                    break;
                case TargetApproachStyle.Immediate:
                    effectiveThis.Position.Y = target.Y;
                    break;
            }



            if (SnapToPixel)
            {
                var zoom = Camera.DestinationRectangle.Height / Camera.OrthogonalHeight;

                var invertZoom = 1 / zoom;

                if(CustomSnapToPixelZoom != null)
                {
                    invertZoom = 1 / CustomSnapToPixelZoom.Value;
                }

                Camera.X = MathFunctions.RoundFloat(effectiveThis.X + CameraOffset.X, invertZoom) + SnapToPixelOffset * invertZoom;
                Camera.Y = MathFunctions.RoundFloat(effectiveThis.Y + CameraOffset.Y, invertZoom) + SnapToPixelOffset * invertZoom;

            }
            else
            {
                Camera.X = effectiveThis.X + CameraOffset.X;
                Camera.Y = effectiveThis.Y + CameraOffset.Y;
            }
        }

        public void ApplySeparationForZoom(Vector2 separationVector)
        {
            // for now we'll assume a padding:
            float noZoomRatio = .8f;
            float noZoomDistance = System.Math.Min(defaultOrthoWidth, defaultOrthoHeight) * noZoomRatio;

            var currentSeparationDistance = separationVector.Length();

            float desiredZoom;

            if (currentSeparationDistance > noZoomDistance)
            {
                desiredZoom = System.Math.Min(furthestZoom, currentSeparationDistance / noZoomDistance);

                desiredZoom = System.Math.Min(desiredZoom, MaxViewableAreaMultiplier);
                desiredZoom = System.Math.Max(desiredZoom, 1);
            }
            else
            {
                desiredZoom = 1;
            }

            if (LerpSmoothZoom)
            {
                ViewableAreaMultiplier = MathHelper.Lerp(ViewableAreaMultiplier, desiredZoom, .1f);
            }
            else
            {
                ViewableAreaMultiplier = desiredZoom;
            }

            Camera.OrthogonalHeight = defaultOrthoHeight * ViewableAreaMultiplier;
            Camera.FixAspectRatioYConstant();
        }

        const float individualShakeDurationInSeconds = .05f;

        /// <summary>
        /// Shakes the screen for a certain duration. This can be used to create a screen shake effect.
        /// </summary>
        /// <param name="shakeRadius">The shake radius - a larger value creates more shaking.</param>
        /// <param name="durationInSeconds">How long to perofrm shaking in seconds.</param>
        /// <returns>A task which is completed when the shaking finishes.</returns>
        public async Task ShakeScreen(float shakeRadius, float durationInSeconds)
        {

            var random = FlatRedBallServices.Random;
            for (float timePassed = 0; timePassed < durationInSeconds; timePassed += individualShakeDurationInSeconds)
            {
                var point = random.PointInCircle(shakeRadius);

                // todo - use velocity here instead of snapping
                CameraOffset.X = point.X;
                CameraOffset.Y = point.Y;

                await TimeManager.DelaySeconds(individualShakeDurationInSeconds);
            }

            CameraOffset.X = 0;
            CameraOffset.Y = 0;
        }

        /// <summary>
        /// Shakes the camera until the taskToAwait is completed. This can be used to shake the camera until a certain event occurs.
        /// </summary>
        /// <param name="shakeRadius">The shake radius - a larger value creates more shaking.</param>
        /// <param name="taskToAwait">The task to await before shaking stops.</param>
        public async Task ShakeScreenUntil(float shakeRadius, Task taskToAwait)
        {
            var random = FlatRedBallServices.Random;
            while(!taskToAwait.IsCompleted)
            {
                var point = random.PointInCircle(shakeRadius);

                // todo - use velocity here instead of snapping
                CameraOffset.X = point.X;
                CameraOffset.Y = point.Y;

                try
                {
                    await TimeManager.DelaySeconds(individualShakeDurationInSeconds);
                }
                // do nothing, the user cancelled it, so let's just break out...
                catch (TaskCanceledException) 
                {
                    break;
                }

            }

            CameraOffset.X = 0;
            CameraOffset.Y = 0;
        }
    }
}
