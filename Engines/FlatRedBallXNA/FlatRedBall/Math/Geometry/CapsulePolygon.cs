using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FlatRedBall.Math.Geometry
{
    public class CapsulePolygon : Polygon, IScalable
    {
        bool mSupressPointsRecalculation;
        Point[] mPoints;

        float mWidth;
        public float Width
        {
            get { return mWidth; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(nameof(Width) + " must be bigger than zero.", nameof(Width));
                }
                else
                {
                    bool changed = mWidth != value;
                    mWidth = value;

                    if (changed)
                    {
                        RecalculatePoints();
                    }
                }
            }
        }

        float mHeight;
        public float Height
        {
            get { return mHeight; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException(nameof(Height) + " must be bigger than zero.", nameof(Height));
                }
                else
                {
                    bool changed = mHeight != value;
                    mHeight = value;

                    if (changed)
                    {
                        RecalculatePoints();
                    }
                }
            }
        }

        int mSemicircleNumberOfSegments;

        /// <summary>
        /// Number of segments used to define each of the round edges. The more segments the more accurate the semicircle will be.
        /// </summary>
        public int SemicircleNumberOfSegments
        {
            get { return mSemicircleNumberOfSegments; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentException("The number of segments per semicircle must be bigger than one.", nameof(SemicircleNumberOfSegments));
                }
                else
                {
                    bool changed = mSemicircleNumberOfSegments != value;
                    mSemicircleNumberOfSegments = value;

                    if (changed)
                    {
                        RecalculatePoints();
                    }
                }
            }
        }

        // We don't want the user to modify the points since it could mess the shape and it would no longer resemble a capsule.
        public new IList<Point> Points { get => new ReadOnlyCollection<Point>(base.Points); }

        int SemicircleNumberOfPoints => mSemicircleNumberOfSegments - 1;
        int NumberOfShapePoints => SemicircleNumberOfPoints * 2 + 5;

        float IScalable.ScaleX
        {
            get => Width / 2;
            set => Width = value * 2;
        }
        float IScalable.ScaleY
        {
            get => Height / 2;
            set => Height = value * 2;
        }

        float IScalable.ScaleXVelocity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        float IScalable.ScaleYVelocity { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        float IReadOnlyScalable.ScaleX => Width / 2.0f;
        float IReadOnlyScalable.ScaleY => Height / 2.0f;

        public CapsulePolygon() : this(32, 16, 8) { }

        public CapsulePolygon(float width, float height, int semicircleNumberOfSegments)
        {
            mSupressPointsRecalculation = true;

            Width = width;
            Height = height;
            SemicircleNumberOfSegments = semicircleNumberOfSegments;

            mSupressPointsRecalculation = false;

            RecalculatePoints();
        }

        void RecalculatePoints()
        {
            if (mSupressPointsRecalculation)
            {
                return;
            }

            // Miguel: I'm creating the capsule in a linear fashion. One straight line
            // followed by a semicircle, another straight line and another semicircle.

            int semicircleNumberOfPoints = SemicircleNumberOfPoints;
            mPoints = new Point[NumberOfShapePoints];

            float halfWidth = mWidth / 2;
            float halfHeight = mHeight / 2;
            float semicircleCenter;
            float radiansPerSemicircleStep = MathHelper.Pi / mSemicircleNumberOfSegments;

            Vector2 s1, s2, r1, r2, r3, r4, s1Direction, s2Direction;

            bool horizontal = mWidth > mHeight;

            if (horizontal)
            {
                semicircleCenter = halfWidth - halfHeight;
                s1 = new Vector2(semicircleCenter, 0);
                s2 = new Vector2(-semicircleCenter, 0);
                r1 = new Vector2(-semicircleCenter, halfHeight);
                r2 = new Vector2(semicircleCenter, halfHeight);
                r3 = new Vector2(semicircleCenter, -halfHeight);
                r4 = new Vector2(-semicircleCenter, -halfHeight);
                s1Direction = new Vector2(0, -halfHeight);
                s2Direction = new Vector2(0, halfHeight);
            }
            else // Vertical
            {
                semicircleCenter = halfHeight - halfWidth;
                s1 = new Vector2(0, semicircleCenter);
                s2 = new Vector2(0, -semicircleCenter);
                r1 = new Vector2(-halfWidth, -semicircleCenter);
                r2 = new Vector2(-halfWidth, semicircleCenter);
                r3 = new Vector2(halfWidth, semicircleCenter);
                r4 = new Vector2(halfWidth, -semicircleCenter);
                s1Direction = new Vector2(-halfWidth, 0);
                s2Direction = new Vector2(halfWidth, 0);
            }

            // Add first straight points
            mPoints[0] = new Point(ref r1);
            mPoints[1] = new Point(ref r2);
            int lastPointInserted = 1;

            // Add first semicircle points
            for (int i = 0; i < semicircleNumberOfPoints; i++)
            {
                float rotationAmount = (i + 1) * radiansPerSemicircleStep * (horizontal ? 1f : -1f);
                var rotation = Matrix.CreateRotationZ(rotationAmount);
                var rotatedVector = Vector2.Transform(s1Direction, rotation);
                var finalPosition = s1 + rotatedVector;
                finalPosition.Y *= !horizontal ? 1f : -1f;
                mPoints[lastPointInserted + i + 1] = new Point(ref finalPosition);
            }

            // Add second straight points
            lastPointInserted = 1 + semicircleNumberOfPoints;
            mPoints[lastPointInserted + 1] = new Point(ref r3);
            mPoints[lastPointInserted + 2] = new Point(ref r4);
            lastPointInserted += 2;

            // Add second semicircle points
            for (int i = 0; i < semicircleNumberOfPoints; i++)
            {
                float rotationAmount = (i + 1) * radiansPerSemicircleStep * (horizontal ? 1f : -1f);
                var rotation = Matrix.CreateRotationZ(rotationAmount);
                var rotatedVector = Vector2.Transform(s2Direction, rotation);
                var finalPosition = s2 + rotatedVector;
                finalPosition.Y *= !horizontal ? 1f : -1f;
                mPoints[lastPointInserted + i + 1] = new Point(ref finalPosition);
            }

            lastPointInserted += semicircleNumberOfPoints;

            // Add closing point
            mPoints[lastPointInserted + 1] = new Point(ref r1);

            base.Points = mPoints;
        }

        public override void UpdateDependencies(double currentTime)
        {
            base.UpdateDependencies(currentTime);
        }
    }
}
