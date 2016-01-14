using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall;
using FlatRedBall.Math.Splines;

namespace ToolTemplate.Entities
{
    public enum PreviewVelocityType
    {
        UseSplineVelocity,
        UseConstantVelocity
    }

    public class SplineCrawler : PositionedObject
    {
        #region Fields

        Sprite mVisibleRepresentation;

        double mTimeIntoSpline = 0;
        Spline mSpline;


        #endregion

        #region Properties

        public float ConstantVelocityValue
        {
            get;
            set;
        }

        public PreviewVelocityType PreviewVelocityType
        {
            get;
            set;
        }



        public bool IsDestroyed
        {
            get { return !SpriteManager.ManagedPositionedObjects.Contains(this); }
        }

        #endregion

        #region Methods

        public SplineCrawler(Spline splineToMoveAlong)
        {
            ConstantVelocityValue = 3;

            mSpline = splineToMoveAlong;

            mVisibleRepresentation = SpriteManager.AddSprite("redball.bmp");
            mVisibleRepresentation.AttachTo(this, false);
            mVisibleRepresentation.ColorOperation = FlatRedBall.Graphics.ColorOperation.Add;
            mVisibleRepresentation.Blue = .5f;

            SpriteManager.AddPositionedObject(this);

            this.Position = mSpline.GetPositionAtTime(0);

            const float scaleMultiplier = 20; // so it pulses big for a sec
            mVisibleRepresentation.ScaleX = mVisibleRepresentation.ScaleY = 
                scaleMultiplier / SpriteManager.Camera.PixelsPerUnitAt(this.Z);

        }

        float? lastX;

        public void Activity()
        {
            mTimeIntoSpline += TimeManager.SecondDifference;

            if (PreviewVelocityType == Entities.PreviewVelocityType.UseSplineVelocity)
            {
                this.Position = mSpline.GetPositionAtTime(mTimeIntoSpline);

                if (mTimeIntoSpline > mSpline.StartTime + mSpline.Duration)
                {
                    Destroy();
                }
            }
            else
            {
                this.Position = mSpline.GetPositionAtLengthAlongSpline(
                    (float)mTimeIntoSpline * ConstantVelocityValue);

                lastX = Position.X;

            }
            const float scaleMultiplier = 15; // smaller than in the constructor
            mVisibleRepresentation.ScaleX = mVisibleRepresentation.ScaleY =
                scaleMultiplier / SpriteManager.Camera.PixelsPerUnitAt(this.Z);
        }

        public void Destroy()
        {
            SpriteManager.RemoveSprite(mVisibleRepresentation);
            SpriteManager.RemovePositionedObject(this);
        }

        #endregion

    }
}
