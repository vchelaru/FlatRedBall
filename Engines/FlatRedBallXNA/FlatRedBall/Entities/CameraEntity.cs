using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Entities
{
    public enum CameraBehaviorType
    {
        PositionLocking,
    }


    // Influenced by https://www.gamasutra.com/blogs/ItayKeren/20150511/243083/Scroll_Back_The_Theory_and_Practice_of_Cameras_in_SideScrollers.php
    public class CameraEntity : PositionedObject
    {
        public CameraBehaviorType BehaviorType { get; set; }

        /// <summary>
        /// The target PositionedObjects to fillow. In a single-player game this can be one entity. In a multi-player game, this can 
        /// be all players. The camera will average their position and follow the average.
        /// </summary>
        public PositionedObjectList<PositionedObject> Targets { get; private set; } = new PositionedObjectList<PositionedObject>();

        /// <summary>
        /// The level map. If null, the camera will move without bounds. If set, the camera will not view positions outside of the map.
        /// </summary>
        public IPositionedSizedObject Map { get; set; }

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

        public bool IsActive { get; set; } = true;

        public void Activity()
        {
            ///////////////////Early Out/////////////////////
            if(!IsActive)
            {
                return;
            }
            //////////////////End Early Out//////////////////




            #region Get the average position of all the target instances

            Vector2 averagePosition = Vector2.Zero;

            for (int i = 0; i < Targets.Count; i++)
            {
                var targetAtI = Targets[i];

                averagePosition.X = targetAtI.X;
                averagePosition.Y = targetAtI.Y;
            }

            if (Targets.Count > 0)
            {
                averagePosition.X /= Targets.Count;
                averagePosition.Y /= Targets.Count;
            }

            #endregion

            #region Convert the average positions to a target given the BehaviorType

            Vector2 target = Vector2.Zero;
            switch (BehaviorType)
            {
                case CameraBehaviorType.PositionLocking:
                    target.X = averagePosition.X;
                    target.Y = averagePosition.Y;
                    break;
            }

            #endregion

            #region Limit the target position based on the map

            if (Map != null)
            {
                target.X = System.Math.Max(target.X, Map.X + Camera.Main.OrthogonalWidth / 2);
                target.X = System.Math.Min(target.X, Map.X + Map.Width - Camera.Main.OrthogonalWidth / 2);

                target.Y = System.Math.Max(target.Y, Map.Y - Map.Height + Camera.Main.OrthogonalHeight / 2);
                target.Y = System.Math.Min(target.Y, Map.Y - Camera.Main.OrthogonalHeight / 2);
            }

            #endregion

            #region Set this position or velocity depending on whether we lerp position

            var objectToMove = this.Parent ?? this;

            if (LerpSmooth == false)
            {
                objectToMove.Position.X = target.X;
                objectToMove.Position.Y = target.Y;
            }
            else
            {
                objectToMove.Velocity.X = (target.X - objectToMove.Position.X) * LerpCoefficient;
                objectToMove.Velocity.Y = (target.Y - objectToMove.Position.Y) * LerpCoefficient;
            }

            #endregion

            if (SnapToPixel)
            {
                var zoom = Camera.Main.DestinationRectangle.Height / Camera.Main.OrthogonalHeight;

                var invertZoom = 1 / zoom;

                Camera.Main.X = MathFunctions.RoundFloat(objectToMove.X, invertZoom) + SnapToPixelOffset * invertZoom;
                Camera.Main.Y = MathFunctions.RoundFloat(objectToMove.Y, invertZoom) + SnapToPixelOffset * invertZoom;

            }
            else
            {
                Camera.Main.X = objectToMove.X;
                Camera.Main.Y = objectToMove.Y;
            }
        }
    }
}
