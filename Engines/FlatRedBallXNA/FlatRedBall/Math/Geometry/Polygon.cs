using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Gui;
using FlatRedBall.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VertexPositionColor = Microsoft.Xna.Framework.Graphics.VertexPositionColor;




using System.Collections.ObjectModel;
using FlatRedBall.Graphics;

namespace FlatRedBall.Math.Geometry
{
    public class Polygon : PositionedObject, IEquatable<Polygon>, IVisible, IMouseOver
    {
        #region Fields

        /// <summary>
        /// Whether the Polygon object can have its Points assigned to null.
        /// Normally this is false, which means assigning Points to null will 
        /// result in an exception. This is set to true in edit mode to enable
        /// the user to change polygon points without the game crashing.
        /// </summary>
        public static bool TolerateEmptyPolygons = false;

        // Victor Chelaru May 17 2008
        // At the time of this writing there are no velocity or rate
        // fields in the Polygon class that don't exist in the PositionedObject class.
        // Therefore, the Polygon Pause instruction will simply use the PositionedObject's.
        // If any rate values are added to this class, the Pause method should be updated.  Likely
        // a new Unpause instruction will need to be created.

        static internal int NumberOfTimesCollideAgainstPolygonCalled = 0;
        static internal int NumberOfTimesRadiusTestPassed = 0;

        /// <summary>
        /// The points relative to the center of the Polygon.  These
        /// define the shape and size of the Polygon.
        /// </summary>
        private Point[] mPoints;

        #region XML Docs
        /// <summary>
        /// The midpoint Points used for collision.  These are updated in CollideAgainstMovePreview.
        /// </summary>
        #endregion
        private Point3D[] mCenterPoints;

        private ReadOnlyCollection<Point> pointCollection;
        Color mColor;


        /// <summary>
        /// The vertices that get fed into the VertexBuffer
        /// when the polygon is rendered.  The position is calculated
        /// by positioning the mPoints relative to the polygon using its
        /// position and rotation properties.  This is done in the
        /// FillVertexArray method (similar to the Text object)
        /// </summary>
        /// <remarks>
        /// These vertices contain the absolute vertices of the polygon, so they
        /// can be used in collision requiring these positions (rather than calculating
        /// them using mPoints and position/rotation values.
        /// </remarks>
        internal VertexPositionColor[] mVertices;


        bool mVisible;

        // this is generated any time the Points array is set.
        // It can be used to quickly tell if two polygons
        // are not touching.
        float mBoundingRadius;

        internal Vector3 mLastMoveCollisionReposition;

        // Internal so that other objects like Line can set this when performing collision
        internal Point mLastCollisionPoint;

        internal Layer mLayerBelongingTo;

        // Whether this is concave, calculated whenever the poits are set
        bool isConcaveCache;
        bool isClockwiseCache;

        #endregion

        #region Properties
        /// <summary>
        /// Sets the directions that the polygon can perform collision - this is only considered if the polygon is convex and clockwise.
        /// </summary>
        public RepositionDirections RepositionDirections { get; set; } = RepositionDirections.All;

        public float BoundingRadius => mBoundingRadius;
        

        public bool Visible
        {
            get => mVisible;
            set
            {
                // This is here for efficiency; however,
                // shape visibility itself is somewhat inefficient.
                // Having this here breaks Glue gencode which sets visibility
                // once when shape visibility response is off, then once again
                // with it on. 
                //if (value != mVisible)
                {
                    mVisible = value;
                    ShapeManager.NotifyOfVisibilityChange(this);
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// The Position-relative points of the Polygon. A point at 0,0 will be positioned at the center of the Polygon.
        /// </summary>
        /// <remarks>
        /// This value can be assigned to completely replace the points in a polygon.
        /// Changing this list updates the Vertices internally and immediately
        /// makes the polygon available for rendering. Note that a closed polygon will have its first point repeated. For example
        /// a closed rectangle will have 5 points (the normal 4 points, plus the last point repeating the first).
        /// </remarks>
        /// <example>
        /// Point[] points = new Point[5];
        /// points[0].X = 100;
        /// points[0].Y = 100;
        /// 
        /// points[1].X = 100;
        /// points[1].Y = -100;
        /// 
        /// ...
        /// 
        /// polygonInstance.Points = points;
        /// </example>
        #endregion
        public IList<Point> Points
        {
            get { return pointCollection; }
            set
            {
                if (value == null)
                {
                    if(TolerateEmptyPolygons == false)
                    {
                        throw new System.IndexOutOfRangeException("Cannot set the Points to null.");
                    }
                    isConcaveCache = false;
                    isClockwiseCache = false;
                }
                else
                {
                    if (value.Count == 0 && TolerateEmptyPolygons == false)
                    {
                        throw new System.IndexOutOfRangeException("Cannot set the Points property to an IList of 0 Points");
                    }

                    if (mPoints == null || mPoints.Length != value.Count)
                    {
                        mPoints = new Point[value.Count];
                        int valueCountMinusOne = 
                            // In edit mode we tolerate 0-sizes so we need a Math.Max call to prevent negative values
                            System.Math.Max(0, value.Count - 1);

                        mCenterPoints = new Point3D[valueCountMinusOne];
                    }
                    value.CopyTo(mPoints, 0);

                    mVertices = new VertexPositionColor[mPoints.Length];
                    CalculateBoundingRadius();

                    pointCollection = new ReadOnlyCollection<Point>(mPoints);
                    OnPointsChanged(EventArgs.Empty);

                    this.FillVertexArray();
                    isConcaveCache = this.IsConcave();
                    isClockwiseCache = this.IsClockwise();
                }
            }
        }

        internal VertexPositionColor[] Vertices
        {
            get
            {
                return mVertices;
            }
        }

        #region XML Docs
        /// <summary>
        /// Reports the vector along which this polygon was moved along during the last
        /// CollideAgainstMove method.
        /// </summary>
        /// <remarks>
        /// This value is reset every time CollideAgainstMove is called whether there is a
        /// successful collision or not.  If there is no collision, this value is set to 
        /// Vector3.Zero.  If reactions to collisions such as physics are being implemented
        /// using this value, then the behavior should be tested and applied after every
        /// call to CollideAgainstMove.
        /// <para>
        /// This value is set on both the instance calling the CollideAgainstMove method
        /// as well as the argument.
        /// </para>
        /// </remarks>
        #endregion
        public Vector3 LastMoveCollisionReposition
        {
            get { return mLastMoveCollisionReposition; }
        }

        #region XML Docs
        /// <summary>
        /// The absolute position where the last collision was detected in a CollieAgainst method.
        /// </summary>
        #endregion
        public Point LastCollisionPoint
        {
            get { return mLastCollisionPoint; }
        }

        public Color Color
        {
            set
            {
                mColor = value;

                if (Vertices != null)
                {
                    var premultiplied = new Color();
                    premultiplied.A = mColor.A;
                    premultiplied.R = (byte)(mColor.R * mColor.A / 255);
                    premultiplied.G = (byte)(mColor.G * mColor.A / 255);
                    premultiplied.B = (byte)(mColor.B * mColor.A / 255);

                    for (int i = 0; i < Vertices.Length; i++)
                        Vertices[i].Color = premultiplied;
                }
            }

            get
            {
                return mColor;
            }
        }



        #endregion

        #region Events

        #region XML Docs
        /// <summary>
        /// Event raised when the Points property reference is reset or when the
        /// SetPoint method is called.
        /// </summary>
        #endregion
        public event EventHandler PointsChanged;

        #endregion

        #region Constructor

        public Polygon()
        {
            Color = Color.White;
        }

        #endregion

        #region Methods


        #region Static Public Methods

        static public Polygon CreateRectangle(float scaleX, float scaleY)
        {
            return CreateRectangle<Polygon>(scaleX, scaleY);
        }

        static public Polygon CreateRectangle(IReadOnlyScalable iScalable)
        {
            return CreateRectangle(iScalable.ScaleX, iScalable.ScaleY);
        }

        static public T CreateRectangle<T>(float scaleX, float scaleY) where T : Polygon, new()
        {
            T newPolygon = new T();
            Point[] points = new Point[5];

            // clockwise
            points[0].X = -scaleX;
            points[0].Y = scaleY;

            points[1].X = scaleX;
            points[1].Y = scaleY;

            points[2].X = scaleX;
            points[2].Y = -scaleY;

            points[3].X = -scaleX;
            points[3].Y = -scaleY;

            points[4] = points[0];

            newPolygon.Points = points;

            return newPolygon;
        }

        /// <summary>
        /// Returns an equilateral shape of numberOfSides sides.
        /// </summary>
        /// <remarks>
        /// The newly-created Polygon is invisible and is not part of the ShapeManager.
        /// </remarks>
        /// <param name="numberOfSides">The number of sides of the Polygon.  Must be at least 3.</param>
        /// <param name="angleOfFirstPoint">The angle relative to the polygon of the first point.</param>
        /// <returns>The newly-created Polygon.</returns>
        static public Polygon CreateEquilateral(int numberOfSides, float radius, float angleOfFirstPoint)
        {
            Polygon polygon = new Polygon();
            int numberOfSidesPlusOne = numberOfSides + 1;

            Point[] points = new Point[numberOfSidesPlusOne];

            float angleOfCurrentPoint;

            for (int i = 0; i < numberOfSides + 1; i++)
            {
                angleOfCurrentPoint = angleOfFirstPoint + (float)( -i * System.Math.PI * 2) / (float)(numberOfSides);

                points[i].X = (float)(System.Math.Cos(angleOfCurrentPoint) * radius);
                points[i].Y = (float)(System.Math.Sin(angleOfCurrentPoint) * radius);
            }

            polygon.Points = points;

            return polygon;
        }

        static public Polygon FromSegments(Segment[] segments)
        {
            Polygon polygon = new Polygon();

            int segmentLengthPlusOne = segments.Length + 1;
            Point[] points = new Point[segmentLengthPlusOne];

            points[0] = segments[0].Point1;

            for (int i = 0; i < segments.Length; i++)
            {
                int afterI = i + 1;

                points[afterI] = segments[i].Point2;
            }
            polygon.Points = points;

            //polygon.OptimizeRadius();

            return polygon;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Returns the absolute (world) position of the point at the argument pointIndex. This considers the Polygon's position and rotation.
        /// </summary>
        /// <remarks>
        /// This method internally uses the vertices of the polygon to return the position. These vertices are updated every frame if the polygon is
        /// part of the ShapeManager. If a Polygon is not part of the ShapeManager, or if a change (such as position) has been performed on the polygon
        /// and the AbsolutePointPosition is needed immediately, then ForceUpdateDependencies can be called to update the verts internally.
        /// </remarks>
        /// <param name="pointIndex">The 0-based index of the points.</param>
        /// <returns>The absolute world position of the vert at the argument pointIndex.</returns>
        public Vector3 AbsolutePointPosition(int pointIndex)
        {
            return mVertices[pointIndex].Position;
        }

        /// <summary>
        /// Returns whether two indexes are adjacent.  This considers wrapping and duplicate
        /// points for closed polygons.
        /// </summary>
        /// <param name="firstIndex">The first index.</param>
        /// <param name="secondIndex">The second index</param>
        /// <returns>Whether the two points are adjacent.</returns>
        public bool ArePointsAdjacent(int firstIndex, int secondIndex)
        {
            // If firstIndex or secondIndex are the last point, make them
            // 0 to make the rest of this method simpler
            if(firstIndex == mPoints.Length -1)
            {
                firstIndex = 0;
            }
            if(secondIndex == mPoints.Length - 1)
            {
                secondIndex = 0;
            }
            

            int pointBeforeFirst = firstIndex - 1;
            if (pointBeforeFirst < 0)
            {
                pointBeforeFirst = mPoints.Length - 2;
            }

            int pointAfterFirst = (firstIndex + 1) % (mPoints.Length - 1);

            return secondIndex == pointBeforeFirst || secondIndex == pointAfterFirst;

        }


        public Polygon Clone()
        {
			Polygon clonedPolygon = this.Clone<Polygon>();

			return clonedPolygon;
        }


        public new T Clone<T>() where T : Polygon, new()
        {
            T newPolygon = base.Clone<T>();
            newPolygon.mVisible = false;
            
#if !SILVERLIGHT
            newPolygon.mLayerBelongingTo = null;
#endif
            Point[] newPoints = null;

            if (mPoints != null)
            {
                newPoints = new Point[mPoints.Length];
                mPoints.CopyTo(newPoints, 0);
            }
            // Set the mPoints to null so that setting newPoints
            // resets it.
            newPolygon.mPoints = null;

            if (newPoints != null)
            {
                newPolygon.Points = newPoints;
            }


            return newPolygon;
        }


        #region CollideAgainst Methods

        public bool CollideAgainst(AxisAlignedRectangle rectangle)
        {
            UpdateDependencies(TimeManager.CurrentTime);
            rectangle.UpdateDependencies(TimeManager.CurrentTime);

            if ((rectangle.BoundingRadius + mBoundingRadius) * (rectangle.BoundingRadius + mBoundingRadius) >

                (rectangle.Position.X - Position.X) * (rectangle.Position.X - Position.X) +
                (rectangle.Position.Y - Position.Y) * (rectangle.Position.Y - Position.Y))
            {
                int k;

                #region See if any of the four corners on the rectangle are inside the Polygon

                bool collisionOccurred = false;
                double longestDistanceSquared = -1;

                if (IsPointInside(rectangle.Left, rectangle.Top))
                {
                    longestDistanceSquared = VectorFrom(rectangle.Left, rectangle.Top).LengthSquared();

                    mLastCollisionPoint.X = rectangle.Left;
                    mLastCollisionPoint.Y = rectangle.Top;
                    collisionOccurred = true;
                }
                UpdateCollisionPointIfPointInsideIsFurtherInside(rectangle.Right, rectangle.Top, 
                    ref longestDistanceSquared, ref collisionOccurred);
                UpdateCollisionPointIfPointInsideIsFurtherInside(rectangle.Right, rectangle.Bottom,
                    ref longestDistanceSquared, ref collisionOccurred);
                UpdateCollisionPointIfPointInsideIsFurtherInside(rectangle.Left, rectangle.Bottom,
                    ref longestDistanceSquared, ref collisionOccurred);


                UpdateCollisionPointIfPointInsideIsFurtherInside(rectangle.X, rectangle.Top,
                    ref longestDistanceSquared, ref collisionOccurred);
                UpdateCollisionPointIfPointInsideIsFurtherInside(rectangle.X, rectangle.Bottom,
                    ref longestDistanceSquared, ref collisionOccurred);
                UpdateCollisionPointIfPointInsideIsFurtherInside(rectangle.Left, rectangle.Y,
                    ref longestDistanceSquared, ref collisionOccurred);
                UpdateCollisionPointIfPointInsideIsFurtherInside(rectangle.Right, rectangle.Y,
                    ref longestDistanceSquared, ref collisionOccurred);



                #endregion

                // Now see if any points on the polygon 

                // First check the points inside the polygon.  This will get the deepest points first
                // as opposed to segment checks.  This is also faster.

                for (int i = 0; i < mVertices.Length; i++)
                {

                    if (rectangle.IsPointInside(ref mVertices[i].Position))
                    {


                        float depthSquared = (float)rectangle.VectorFrom(
                            mVertices[i].Position.X,
                            mVertices[i].Position.Y, 
                            this.Z).Length();

                        depthSquared = depthSquared * depthSquared;

                        if (depthSquared > longestDistanceSquared)
                        {
                            mLastCollisionPoint.X = mVertices[i].Position.X;
                            mLastCollisionPoint.Y = mVertices[i].Position.Y;
                            longestDistanceSquared = depthSquared;
                            collisionOccurred = true;
                        }
                    }
                }


                if (collisionOccurred)
                {
                    return true;
                }

                for (int i = 0; i < mVertices.Length; i++)
                {
                    k = i + 1 < mVertices.Length ? i + 1 : 0;
                    s1.SetPoints(ref mVertices[i], ref mVertices[k]);

                    #region Do segment vs. segment collision
                    // rect top
                    s2.SetPoints(rectangle.X - rectangle.ScaleX, rectangle.Y + rectangle.ScaleY,
                        rectangle.X + rectangle.ScaleX, rectangle.Y + rectangle.ScaleY);
                    s1.IntersectionPoint(ref s2, out mLastCollisionPoint);
                    if (!double.IsNaN(mLastCollisionPoint.X))
                        return true;

                    // rect right
                    s2.SetPoints(rectangle.X + rectangle.ScaleX, rectangle.Y + rectangle.ScaleY,
                        rectangle.X + rectangle.ScaleX, rectangle.Y - rectangle.ScaleY);
                    s1.IntersectionPoint(ref s2, out mLastCollisionPoint);
                    if (!double.IsNaN(mLastCollisionPoint.X))
                        return true;

                    // rect bottom
                    s2.SetPoints(rectangle.X + rectangle.ScaleX, rectangle.Y - rectangle.ScaleY,
                        rectangle.X - rectangle.ScaleX, rectangle.Y - rectangle.ScaleY);
                    s1.IntersectionPoint(ref s2, out mLastCollisionPoint);
                    if (!double.IsNaN(mLastCollisionPoint.X))
                        return true;

                    // rect left
                    s2.SetPoints(rectangle.X - rectangle.ScaleX, rectangle.Y - rectangle.ScaleY,
                        rectangle.X - rectangle.ScaleX, rectangle.Y + rectangle.ScaleY);
                    s1.IntersectionPoint(ref s2, out mLastCollisionPoint);
                    if (!double.IsNaN(mLastCollisionPoint.X))
                        return true;  

                    #endregion
                }
            }

            return false;
        }

        static double newLongestDistanceSquared;
        private void UpdateCollisionPointIfPointInsideIsFurtherInside(double x, double y, ref double longestDistanceSquared, ref bool collisionOccurred)
        {
            if (IsPointInside(x, y))
            {
                newLongestDistanceSquared = VectorFrom(x, y).LengthSquared();
                if (newLongestDistanceSquared > longestDistanceSquared)
                {
                    longestDistanceSquared = newLongestDistanceSquared;
                    mLastCollisionPoint.X = x;
                    mLastCollisionPoint.Y = y;
                }
                collisionOccurred = true;
            }
        }
        static Segment s1;
        static Segment s2;
        public bool CollideAgainst(Circle circle)
        {
            // This method will test the following things:
            //  * Is the circle's center inside the polygon?
            //  * Are any of the polygon's points inside the circle?
            //  * Is the circle within Radius distance from any of the polygon's edges?
            // Of course none of this is done if the radius check fails

            UpdateDependencies(TimeManager.CurrentTime);
            circle.UpdateDependencies(TimeManager.CurrentTime);

            if ((circle.Radius + mBoundingRadius) * (circle.Radius + mBoundingRadius) >

                (circle.Position.X - Position.X) * (circle.Position.X - Position.X) +
                (circle.Position.Y - Position.Y) * (circle.Position.Y - Position.Y))
            {
                // First see if the circle is inside the polygon.
                if (IsPointInside(ref circle.Position))
                {
                    mLastCollisionPoint.X = circle.Position.X;
                    mLastCollisionPoint.Y = circle.Position.Y;
                    return true;
                }
                int i;
                // Next see if any of the Polygon's points are inside the circle
                for (i = 0; i < mVertices.Length; i++)
                {
                    if (circle.IsPointInside(ref mVertices[i].Position))
                    {
                        mLastCollisionPoint.X = mVertices[i].Position.X;
                        mLastCollisionPoint.Y = mVertices[i].Position.Y;
                        return true;
                    }
                }
                int k;
                // Next check if the circle is within Radius units of any segment.
                for (i = 0; i < mVertices.Length; i++)
                {
                    k = i + 1 < mVertices.Length ? i + 1 : 0;
                    s1.SetPoints(ref mVertices[i], ref mVertices[k]);

                    if (s1.DistanceToSquared(ref circle.Position, out s2) < circle.Radius * circle.Radius)
                    {
                        mLastCollisionPoint.X = s2.Point2.X;
                        mLastCollisionPoint.Y = s2.Point2.Y;

                        return true;
                    }
                }
            }
            return false;
        }


        public bool CollideAgainst(Polygon polygon)
        {
            NumberOfTimesCollideAgainstPolygonCalled++;

            // Need to update dependencies before performing the radius checks
            // so that positions are accurate.
            UpdateDependencies(TimeManager.CurrentTime);
            polygon.UpdateDependencies(TimeManager.CurrentTime);

            // Perform the initial check to see if it's obvious that
            // the polygons are too far away to collide.
            if ((polygon.mBoundingRadius + mBoundingRadius) * (polygon.mBoundingRadius + mBoundingRadius) >

                (polygon.Position.X - Position.X) * (polygon.Position.X - Position.X) +
                (polygon.Position.Y - Position.Y) * (polygon.Position.Y - Position.Y)   )
            {
                NumberOfTimesRadiusTestPassed++;


//                Segment s1 = new Segment();
  //              Segment s2 = new Segment();

                int j;
                int k;
                int l;

                if (IsPointInside(ref polygon.Position))
                {
                    mLastCollisionPoint.X = polygon.Position.X;
                    mLastCollisionPoint.Y = polygon.Position.Y;

                    polygon.mLastCollisionPoint = mLastCollisionPoint;
                    return true;
                }

                if (polygon.IsPointInside(ref Position))
                {
                    mLastCollisionPoint.X = polygon.Position.X;
                    mLastCollisionPoint.Y = polygon.Position.Y;

                    polygon.mLastCollisionPoint = mLastCollisionPoint;
                    return true;
                }


                // Test all points belonging to the argument
                // polygon and if they are inside this, return
                // true.  Points in other will be tested in next
                // for loop.
                for (j = 0; j < polygon.mVertices.Length; j++)
                {
                    if (IsPointInside(ref polygon.mVertices[j].Position))
                    {
                        mLastCollisionPoint.X = polygon.mVertices[j].Position.X;
                        mLastCollisionPoint.Y = polygon.mVertices[j].Position.Y;

                        polygon.mLastCollisionPoint = mLastCollisionPoint;
                        return true;
                    }
                }

                for (int i = 0; i < mVertices.Length; i++)
                {
                    if (polygon.IsPointInside(ref mVertices[i].Position))
                    {
                        mLastCollisionPoint.X = mVertices[i].Position.X;
                        mLastCollisionPoint.Y = mVertices[i].Position.Y;

                        polygon.mLastCollisionPoint = mLastCollisionPoint;
                        return true;
                    }

                    
                    k = i + 1 < mVertices.Length ? i + 1 : 0;
                    s1.SetPoints(ref mVertices[i], ref mVertices[k]);

                    //check each line against the other poly's lines
                    for (j = 0; j < polygon.mVertices.Length; j++)
                    {
                        l = j + 1 < polygon.mVertices.Length ? j + 1 : 0;
                        s2.SetPoints(ref polygon.mVertices[j], ref polygon.mVertices[l]);

                        s1.IntersectionPoint(ref s2, out mLastCollisionPoint);
                        if (!double.IsNaN(mLastCollisionPoint.X))
                        {
                            polygon.mLastCollisionPoint = mLastCollisionPoint;
                            return true;
                        }
                    }
                     
                }
                
            }
        
            return false;
        }


        public bool CollideAgainst(Line line)
        {
            return line.CollideAgainst(this);
        }


        public bool CollideAgainst(Segment segment)
        {
            return segment.CollideAgainst(this);
        }


		public bool CollideAgainst(Capsule2D capsule)
		{
			throw new NotImplementedException("This method hasn't been implemented yet.  Please complain on the FlatRedBall forums.");
		}


        public bool CollideAgainstMove(Polygon polygon, float thisMass, float otherMass)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            bool valueToReturn = 
                CollideAgainstMovePreview(polygon, thisMass, otherMass, 
                ref this.mLastMoveCollisionReposition, ref polygon.mLastMoveCollisionReposition);

            if (valueToReturn)
            {
                TopParent.Position += mLastMoveCollisionReposition;
                polygon.TopParent.Position += polygon.mLastMoveCollisionReposition;

                ForceUpdateDependencies();
                polygon.ForceUpdateDependencies();
            }

            return valueToReturn;
        }

        // I could make this static, but that would make multithreaded tests fail.  Although this may be bad if the same
        // poly is used in multiple threads.  Not sure what to do about that - maybe lock on it?  Would that hurt perf?
        VertexPositionColor[] mVerticesForRectCollision = new VertexPositionColor[5];

        public bool CollideAgainstMove(AxisAlignedRectangle rectangle, float thisMass, float otherMass)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if(CollideAgainst(rectangle))
            {
                mVerticesForRectCollision[0].Position.X = rectangle.Left;
                mVerticesForRectCollision[0].Position.Y = rectangle.Top;

                mVerticesForRectCollision[1].Position.X = rectangle.Right;
                mVerticesForRectCollision[1].Position.Y = rectangle.Top;

                mVerticesForRectCollision[2].Position.X = rectangle.Right;
                mVerticesForRectCollision[2].Position.Y = rectangle.Bottom;

                mVerticesForRectCollision[3].Position.X = rectangle.Left;
                mVerticesForRectCollision[3].Position.Y = rectangle.Bottom;

                mVerticesForRectCollision[4] = mVerticesForRectCollision[0];

                Vector3 thisMoveCollisionReposition = new Vector3();
                Vector3 otherMoveCollisionReposition = new Vector3();

                CollideAgainstMovePreview(thisMass, otherMass, ref thisMoveCollisionReposition, ref otherMoveCollisionReposition, mVerticesForRectCollision,
                    !this.isConcaveCache && this.isClockwiseCache, true);

                mLastMoveCollisionReposition = thisMoveCollisionReposition;
                rectangle.mLastMoveCollisionReposition.X = otherMoveCollisionReposition.X;
                rectangle.mLastMoveCollisionReposition.Y = otherMoveCollisionReposition.Y;

                TopParent.Position += mLastMoveCollisionReposition;
                rectangle.TopParent.Position += otherMoveCollisionReposition;
                ForceUpdateDependencies();
                rectangle.ForceUpdateDependencies();

                return true;
            }
            return false;
        }


        public bool CollideAgainstMove(Circle circle, float thisMass, float otherMass)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif
            if (this.CollideAgainst(circle))
            {
                // We need the point before to potentially calculate the normal
                int pointBefore;

                Point3D fromCircleToThis = VectorFrom(circle.Position.X, circle.Position.Y, out pointBefore);

                // The fromCircleToThis will be less than circle.Radius units in length.
                // However much less it is is how far the objects should be moved.

                double length = fromCircleToThis.Length();

                double amountToMoveOnX;
                double amountToMoveOnY;

                // If length is equal to 0, 
                // that means that the circle 
                // falls directly on the polygon's 
                // edge.  When this occurrs, the direction 
                // to move is unknown.  So we need to find the 
                // normal of the surface to know the direction to
                // move.  To get the normal we'll first look at the
                // point before on the polygon, and figure out the normal
                // from that
                if (length == 0)
                {
                    Vector3 direction = this.Vertices[pointBefore + 1].Position - this.Vertices[pointBefore].Position;

                    // now rotate it 90 degrees:
                    direction = new Vector3(-direction.Y, direction.X, 0);

                    // this is a little scary - we need to move the circle along the normal a little bit to see if the normal
                    // is pointing inside or out.  If it's pointing inside, then IsPointInside should return true, so we will flip it;
                    // Update:  Oops, we're actually getting the vector to move the circle, so if the point is *not* inside, then we want
                    // to flip the direction
                    //if (IsPointInside(circle.Position.X + direction.X * .001, circle.Position.Y + direction.Y * .001))
                    if (!IsPointInside(circle.Position.X + direction.X * .001, circle.Position.Y + direction.Y * .001))
                    {
                        direction.X = -direction.X;
                        direction.Y = -direction.Y;
                    }

                    direction.Normalize();
                    
                    
                    fromCircleToThis.X = direction.X;
                    fromCircleToThis.Y = direction.Y;

                    double distanceToMove = circle.Radius;

                    amountToMoveOnX = distanceToMove * fromCircleToThis.X;
                    amountToMoveOnY = distanceToMove * fromCircleToThis.Y;
                }
                else
                {

                    double distanceToMove = circle.Radius - length;

                    if (IsPointInside(ref circle.Position))
                    {
                        // If the circle falls inside of the shape, then it should be moved
                        // outside.  That means moving to the edge of the polygon then also moving out
                        // the distance of the radius.
                        distanceToMove = -(circle.Radius + length);
                    }

                    amountToMoveOnX = distanceToMove * fromCircleToThis.X / length;
                    amountToMoveOnY = distanceToMove * fromCircleToThis.Y / length;
                }
                float totalMass = thisMass + otherMass;

                circle.LastMoveCollisionReposition.X = - (float)(amountToMoveOnX * thisMass / totalMass);
                circle.LastMoveCollisionReposition.Y = - (float)(amountToMoveOnY * thisMass / totalMass);

                circle.TopParent.Position.X += circle.LastMoveCollisionReposition.X;
                circle.TopParent.Position.Y += circle.LastMoveCollisionReposition.Y;

                mLastMoveCollisionReposition.X = (float)((otherMass / totalMass) * amountToMoveOnX);
                mLastMoveCollisionReposition.Y = (float)((otherMass / totalMass) * amountToMoveOnY);

                this.TopParent.Position.X += mLastMoveCollisionReposition.X;
                this.TopParent.Position.Y += mLastMoveCollisionReposition.Y;

                ForceUpdateDependencies();
                circle.ForceUpdateDependencies();

                return true;
            }
            return false;
        }
           

		public bool CollideAgainstMove(Capsule2D capsule2D, float thisMass, float otherMass)
		{
            throw new NotImplementedException("This method is not implemented. Capsules are intended only for CollideAgainst - use Polygons for CollideAgainstMove and CollideAgainstBounce");
		}


		public bool CollideAgainstMove(Line line, float thisMass, float otherMass)
		{
			throw new NotImplementedException("This method is not implemented.  Complain on the FlatRedBall forums.");
		}


        public bool CollideAgainstMove(ShapeCollection shapeCollection, float thisMass, float otherMass)
        {
            return shapeCollection.CollideAgainstMove(this, otherMass, thisMass);

        }


        public bool CollideAgainstBounce(Polygon polygon, float thisMass, float otherMass, float elasticity)
        {
#if DEBUG
            if (thisMass == 0 && otherMass == 0)
            {
                throw new ArgumentException("Both masses cannot be 0.  For equal masses pick a non-zero value");
            }
#endif

            if(CollideAgainstMove(polygon, thisMass, otherMass))
            {
                PositionedObject thisTopParent = this.TopParent;
                PositionedObject otherTopParent = polygon.TopParent;

                Vector2 collisionNormal = new Vector2(
                    mLastMoveCollisionReposition.X,
                    mLastMoveCollisionReposition.Y);

                if (otherMass == 0)
                {
                    collisionNormal.X = -polygon.mLastMoveCollisionReposition.X;
                    collisionNormal.Y = -polygon.mLastMoveCollisionReposition.Y;
                }

                ShapeManager.ApplyBounce(thisTopParent, otherTopParent, thisMass, otherMass, elasticity, ref collisionNormal);

                return true;
            }

            return false;
        }


        public bool CollideAgainstBounce(AxisAlignedRectangle rectangle, float thisMass, float otherMass, float elasticity)
        {
            return rectangle.CollideAgainstBounce(this, otherMass, thisMass, elasticity);
        }


        public bool CollideAgainstBounce(Circle circle, float thisMass, float otherMass, float elasticity)
        {
            return circle.CollideAgainstBounce(this, otherMass, thisMass, elasticity);
        }


        public bool CollideAgainstBounce(ShapeCollection shapeCollection, float thisMass, float otherMass, float elasticity)
        {
            bool didCollide = false;
            for (int i = 0; i < shapeCollection.AxisAlignedRectangles.Count; i++)
            {
                didCollide |= this.CollideAgainstBounce(shapeCollection.AxisAlignedRectangles[i], thisMass, otherMass, elasticity);
            }

#if DEBUG
            // Vic says:  DO THIS!!!!
            if (shapeCollection.Capsule2Ds.Count != 0)
            {
                throw new NotImplementedException();
            }
#endif

            for (int i = 0; i < shapeCollection.Capsule2Ds.Count; i++)
            {
                //this.CollideAgainstBounce(shapeCollection.Capsule2Ds[i], thisMass, otherMass);
            }

            for (int i = 0; i < shapeCollection.Circles.Count; i++)
            {
                didCollide |= this.CollideAgainstBounce(shapeCollection.Circles[i], thisMass, otherMass, elasticity);
            }

#if DEBUG
            if (shapeCollection.Lines.Count != 0)
            {
                throw new NotImplementedException();
            }
#endif
            for (int i = 0; i < shapeCollection.Lines.Count; i++)
            {
                //didCollide |= this.CollideAgainstBounce(shapeCollection.Lines[i], thisMass, otherMass, elasticity);
            }

            for (int i = 0; i < shapeCollection.Polygons.Count; i++)
            {
                didCollide |= this.CollideAgainstBounce(shapeCollection.Polygons[i], thisMass, otherMass, elasticity);
            }

            return didCollide;
        }


		public bool CollideAgainstBounce(Line line, float thisMass, float otherMass, float elasticity)
		{
			throw new NotImplementedException();
		}

		public bool CollideAgainstBounce(Capsule2D capsule2D, float thisMass, float otherMass, float elasticity)
		{
			throw new NotImplementedException("This method is not implemented. Capsules are intended only for CollideAgainst - use Polygons for CollideAgainstMove and CollideAgainstBounce");
		}


        public bool CollideAgainstMovePreview(Polygon polygon, float thisMass, float otherMass,
            ref Vector3 thisMoveCollisionReposition, ref Vector3 argumentPolygonMoveCollisionReposition)
        {

            bool toReturn = false;

            if (this.CollideAgainst(polygon))
            {

                VertexPositionColor[] otherVertices = polygon.mVertices;

                CollideAgainstMovePreview(thisMass, otherMass, ref thisMoveCollisionReposition, ref argumentPolygonMoveCollisionReposition, otherVertices, !this.isConcaveCache, !polygon.isConcaveCache);

                toReturn = true;
				return toReturn;
            }
            return toReturn;
        }

        private void CollideAgainstMovePreview(float thisMass, float otherMass, ref Vector3 thisMoveCollisionReposition, ref Vector3 otherMoveCollisionReposition, 
            VertexPositionColor[] otherVertices,
            bool canUseAxisSeparating, bool canOtherUseAxisSeparating)
        {
            if(canUseAxisSeparating && canOtherUseAxisSeparating)
            {
                PerformAxisSeparatingTheoremCollision(this.mVertices, otherVertices, thisMass, otherMass, out thisMoveCollisionReposition, out otherMoveCollisionReposition);
            }
            else
            {
                thisMoveCollisionReposition = new Vector3(0, 0, 0);
                otherMoveCollisionReposition = new Vector3(0, 0, 0);

                Vector3 positionBefore = Position;

                // declare some variables
                double longestDistanceSquared = double.NegativeInfinity;
                Point3D tempVector;
                Point3D vectorTo = new Point3D(0, 0, 0);

                #region Point inside Polygon tests



                int throwaway;

                for (int i = 0; i < mPoints.Length; i++)
                {
                    // The other polygon should have already been updated, so no 
                    // need to do the calculation for the points again:
                    //if (polygon.IsPointInside(ref mPoints[i], ref Position, ref mRotationMatrix)) 
                    //if (polygon.IsPointInside(ref mVertices[i].Position))
                    if (Polygon.IsPointInside(mVertices[i].Position.X, mVertices[i].Position.Y, otherVertices))
                    {
                        tempVector = Polygon.VectorFrom(mVertices[i].Position.X, mVertices[i].Position.Y, otherVertices, out throwaway);
                        //tempVector = polygon.VectorFrom(mVertices[i].Position.X, mVertices[i].Position.Y);
                        if (tempVector.LengthSquared() > longestDistanceSquared)
                        {
                            vectorTo = tempVector;
                            longestDistanceSquared = tempVector.LengthSquared();
                        }
                    }
                }


                for (int i = 0; i < otherVertices.Length; i++)
                {
                    if (IsPointInside(ref otherVertices[i].Position))
                    {
                        tempVector = VectorFrom(otherVertices[i].Position.X, otherVertices[i].Position.Y);
                        if (tempVector.LengthSquared() > longestDistanceSquared)
                        {
                            vectorTo = tempVector;
                            longestDistanceSquared = tempVector.LengthSquared();

                            // We want to reverse the vector here because this instance is going to be moved.
                            vectorTo.X = -vectorTo.X;
                            vectorTo.Y = -vectorTo.Y;
                        }
                    }
                }

                #endregion

                #region Center-point inside polygon tests

                // It's possible that the two polygons overlap but their points are not inside of eachother
                // Although a side intersection will usually solve the problem, it won't solve it every time
                // as in the case of two equal squares on the same axis overlapping.
                // To make the collisions more accurate, test the midpoints.

                for (int i = 0; i < mCenterPoints.Length; i++)
                {
                    int afterI = i + 1;
                    mCenterPoints[i].X = (mVertices[i].Position.X + mVertices[afterI].Position.X) / 2.0f;
                    mCenterPoints[i].Y = (mVertices[i].Position.Y + mVertices[afterI].Position.Y) / 2.0f;
                    mCenterPoints[i].Z = (mVertices[i].Position.Z + mVertices[afterI].Position.Z) / 2.0f;
                }


                for (int i = 0; i < mCenterPoints.Length; i++)
                {
                    //if (polygon.IsPointInside(mCenterPoints[i]))
                    if (Polygon.IsPointInside((float)mCenterPoints[i].X, (float)mCenterPoints[i].Y, otherVertices))
                    {
                        tempVector = Polygon.VectorFrom(mCenterPoints[i].X, mCenterPoints[i].Y, otherVertices, out sThrowAwayInt);
                        //tempVector = polygon.VectorFrom(mCenterPoints[i]);
                        if (tempVector.LengthSquared() > longestDistanceSquared)
                        {
                            vectorTo = tempVector;
                            longestDistanceSquared = tempVector.LengthSquared();

                        }
                    }
                }

                #endregion

                #region Test side vs. side

                bool hasIntersections = false;

                // it's possible that there could still be a collision.  In this case, the points might not fall
                // inside of eachother, but the edges will still overlap
                //if (double.IsNegativeInfinity(longestDistanceSquared))
                {

                    List<List<int>> sidesTouching = new List<List<int>>();

                    // Find all of the sides that are intersecting and store it in a List<List<int>>
                    // the first index represents the the side on this, the second represents the
                    // segments on the argument polygon that the first index is intersecting with.
                    for (int i = 0; i < mVertices.Length - 1; i++)
                    {
                        List<int> currentIntersections = new List<int>();
                        sidesTouching.Add(currentIntersections);
                        int afterI = i + 1;

                        Segment thisSegment = new Segment(ref mVertices[i], ref mVertices[afterI]);


                        for (int j = 0; j < otherVertices.Length - 1; j++)
                        {
                            int afterJ = j + 1;
                            Segment otherSegment = new Segment(ref otherVertices[j], ref otherVertices[afterJ]);
                            if (thisSegment.Intersects(otherSegment))
                            {
                                currentIntersections.Add(j);
                                hasIntersections = true;
                            }
                        }
                    }

                    float longestDistance = (float)System.Math.Sqrt(longestDistanceSquared);
                    if (double.IsNegativeInfinity(longestDistanceSquared))
                        longestDistance = float.NegativeInfinity;


                    // todo - we do this vs. other, now need to do other vs. this and get the shortest of the two, I think

                    float longestDistanceStep4This = float.NegativeInfinity;
                    Vector3 vector3Step4This = Vector3.Zero;

                    // Step 1: This segment that intersects with other segment can be moved one of two ways. Get each distance
                    // Step 2: Get the shortest distance of the two values obtained in step 1 
                    // Step 3: Perform step 1 and step 2 on the other segment against this segment (opposite order) and get the shortest distance of the two
                    // Step 4: See if the result of step 3 is the shortest distance obtained so far
                    for (int i = 0; i < sidesTouching.Count; i++)
                    {
                        var thisSegment = new Segment(ref mVertices[i], ref mVertices[i + 1]);
                        for (int j = 0; j < sidesTouching[i].Count; j++)
                        {
                            var otherSegment = new Segment(ref otherVertices[sidesTouching[i][j]], ref otherVertices[sidesTouching[i][j] + 1]);

                            // step 1 for this:
                            var thisDistance1 = otherSegment.DistanceTo(thisSegment.Point1);
                            var thisDistance2 = otherSegment.DistanceTo(thisSegment.Point2);


                            // step 2 for this/other:
                            var shortestDistanceThis = System.Math.Min(thisDistance1, thisDistance2);

                            float step3ShortestDistance;
                            Point step3Endpoint;
                            Segment step3Segment;
                            // step 3:

                            step3Segment = otherSegment;
                            if (shortestDistanceThis == thisDistance1)
                            {
                                step3Endpoint = thisSegment.Point1;
                                step3ShortestDistance = thisDistance1;
                            }
                            else // thisDistance2
                            {
                                step3Endpoint = thisSegment.Point2;
                                step3ShortestDistance = thisDistance2;
                            }


                            if (step3ShortestDistance > longestDistanceStep4This)
                            {
                                longestDistanceStep4This = step3ShortestDistance;
                                var step3ClosestPoint = step3Segment.ClosestPointTo(step3Endpoint);
                                vector3Step4This = new Vector3((float)(step3ClosestPoint.X - step3Endpoint.X), (float)(step3ClosestPoint.Y - step3Endpoint.Y), 0);

                            }
                        }
                    }

                    float longestDistanceStep4Other = float.NegativeInfinity;
                    Vector3 vector3Step4Other = Vector3.Zero;


                    // now do the 2nd polygon vs. the first
                    for (int i = 0; i < sidesTouching.Count; i++)
                    {
                        var thisSegment = new Segment(ref mVertices[i], ref mVertices[i + 1]);
                        for (int j = 0; j < sidesTouching[i].Count; j++)
                        {
                            var otherSegment = new Segment(ref otherVertices[sidesTouching[i][j]], ref otherVertices[sidesTouching[i][j] + 1]);

                            // step 1 for this:
                            var otherDistance1 = thisSegment.DistanceTo(otherSegment.Point1);
                            var otherDistance2 = thisSegment.DistanceTo(otherSegment.Point2);


                            // step 2 for this/other:
                            var shortestDistanceOther = System.Math.Min(otherDistance1, otherDistance2);

                            float step3ShortestDistance;
                            Point step3Endpoint;
                            Segment step3Segment;
                            // step 3:

                            step3Segment = thisSegment;
                            if (shortestDistanceOther == otherDistance1)
                            {
                                step3Endpoint = otherSegment.Point1;
                                step3ShortestDistance = otherDistance1;
                            }
                            else // thisDistance2
                            {
                                step3Endpoint = otherSegment.Point2;
                                step3ShortestDistance = otherDistance2;
                            }


                            if (step3ShortestDistance > longestDistanceStep4Other)
                            {
                                longestDistanceStep4Other = step3ShortestDistance;
                                var step3ClosestPoint = step3Segment.ClosestPointTo(step3Endpoint);
                                vector3Step4Other = new Vector3((float)(step3ClosestPoint.X - step3Endpoint.X), (float)(step3ClosestPoint.Y - step3Endpoint.Y), 0);

                            }
                        }
                    }

                    float distanceToConsider = float.NegativeInfinity;
                    if(float.IsPositiveInfinity(longestDistanceStep4This) && float.IsPositiveInfinity(longestDistanceStep4Other))
                    {
                        // do nothing
                    }
                    else if (!float.IsPositiveInfinity(longestDistanceStep4This) && float.IsPositiveInfinity(longestDistanceStep4Other))
                    {
                        distanceToConsider = longestDistanceStep4This;
                    }
                    else if (float.IsPositiveInfinity(longestDistanceStep4This) && !float.IsPositiveInfinity(longestDistanceStep4Other))
                    {
                        distanceToConsider = longestDistanceStep4Other;
                    }
                    else if(longestDistanceStep4This < longestDistanceStep4Other)
                    {
                        distanceToConsider = longestDistanceStep4This;
                    }
                    else
                    {
                        distanceToConsider = longestDistanceStep4Other;
                    }

                    if(!float.IsNegativeInfinity(distanceToConsider) && distanceToConsider > longestDistance)
                    {
                        if(distanceToConsider == longestDistanceStep4This)
                        {
                            vectorTo.X = vector3Step4This.X;
                            vectorTo.Y = vector3Step4This.Y;
                        }
                        else
                        {
                            vectorTo.X = vector3Step4Other.X;
                            vectorTo.Y = vector3Step4Other.Y;
                        }
                    }
                


                }


                #endregion

                #region Shift polygons

                float thisRatio = thisMass / (thisMass + otherMass);


                thisMoveCollisionReposition.X = (float)vectorTo.X * (1 - thisRatio);
                thisMoveCollisionReposition.Y = (float)vectorTo.Y * (1 - thisRatio);


                otherMoveCollisionReposition.X = -(float)vectorTo.X * thisRatio;
                otherMoveCollisionReposition.Y = -(float)vectorTo.Y * thisRatio;

                #endregion
            }
        }

        private void PerformAxisSeparatingTheoremCollision(VertexPositionColor[] vertices, VertexPositionColor[] otherVertices, float thisMass, float otherMass, 
            out Vector3 thisMoveCollisionReposition, out Vector3 otherMoveCollisionReposition)
        {
            Vector3 firstVectorResult, secondVectorResult;
            int firstDeepestIndex;
            GetSeparatingVectors(vertices, otherVertices, thisMass, otherMass,
                // for now the 2nd will use "all", but we could change this later...

                this.RepositionDirections, RepositionDirections.All,
                out firstVectorResult, out secondVectorResult, out firstDeepestIndex);

            // This was a test for fixing slopes reporting flat in certain situations
            // See comments in the platformer plugin
            //bool testInvert = false;
            //if(testInvert)
            {
                // invert them, test that so that we check the edges of the 2nd object
                Vector3 firstVectorResultInvertOrder, secondVectorResultInvertOrder;
                int secondDeepestIndex;
                GetSeparatingVectors(otherVertices, vertices, otherMass, thisMass,
                    RepositionDirections.All, this.RepositionDirections, 
                    out secondVectorResultInvertOrder, out firstVectorResultInvertOrder, out secondDeepestIndex);

                if (firstVectorResult.Length() + secondVectorResult.Length() < firstVectorResultInvertOrder.Length() + secondVectorResultInvertOrder.Length())
                {
                    thisMoveCollisionReposition = firstVectorResult;
                    otherMoveCollisionReposition = secondVectorResult;
                }
                else
                {
                    thisMoveCollisionReposition = firstVectorResultInvertOrder;
                    otherMoveCollisionReposition = secondVectorResultInvertOrder;
                }
            }
            //else
            //{
            //    thisMoveCollisionReposition = firstVectorResult;
            //    otherMoveCollisionReposition = secondVectorResult;
            //}

        }

        private static void GetSeparatingVectors(VertexPositionColor[] firstVertices, VertexPositionColor[] secondVertices, 
            float firstMass, float secondMass, 
            RepositionDirections firstRepositionDirections, RepositionDirections secondRepositionDirections,
            out Vector3 firstVectorResult, out Vector3 secondVectorResult, 
            out int secondVectorIndex)
        {
            firstVectorResult = new Vector3();
            secondVectorResult = new Vector3();
            int firstLengthMinusOne = firstVertices.Length - 1;
            int secondLengthMinusOne = secondVertices.Length - 1;

            Vector2 smallestOverlapVector = new Vector2();
            float? smallestOverlapLength = null;

            Vector2 vectorForPoint = new Vector2();

            secondVectorIndex = -1;

            for (int i = 0; i < firstLengthMinusOne; i++)
            {

                float firstVectorX = firstVertices[i + 1].Position.X - firstVertices[i].Position.X;
                float firstVectorY = firstVertices[i + 1].Position.Y - firstVertices[i].Position.Y;

                // normal is -y, x
                var normalizedSurface = Vector2.Normalize(new Vector2(-firstVectorY, firstVectorX));

                float minSecond = float.PositiveInfinity;

                int smallestIndexForThisEdge = -1;
                for (int j = 0; j < secondLengthMinusOne; j++)
                {
                    vectorForPoint.X = secondVertices[j].Position.X - firstVertices[i].Position.X;
                    vectorForPoint.Y = secondVertices[j].Position.Y - firstVertices[i].Position.Y;

                    var result = Vector2.Dot(vectorForPoint, normalizedSurface);

                    if (result < minSecond)
                    {
                        minSecond = result;
                        smallestIndexForThisEdge = j;
                    }
                }

                var overlaps = minSecond < 0;

                if (overlaps)
                {
                    var valueToUse = -minSecond;

                    bool shouldConsider = (normalizedSurface.X < 0 && (firstRepositionDirections & RepositionDirections.Left) == RepositionDirections.Left) ||
                        (normalizedSurface.X > 0 && (firstRepositionDirections & RepositionDirections.Right) == RepositionDirections.Right) ||
                        (normalizedSurface.Y < 0 && (firstRepositionDirections & RepositionDirections.Down) == RepositionDirections.Down) ||
                        (normalizedSurface.Y > 0 && (firstRepositionDirections & RepositionDirections.Up) == RepositionDirections.Up);


                    if (shouldConsider && (smallestOverlapLength == null || System.Math.Abs(valueToUse) < System.Math.Abs(smallestOverlapLength.Value)))
                    {
                        smallestOverlapLength = valueToUse;
                        smallestOverlapVector = normalizedSurface;
                        secondVectorIndex = smallestIndexForThisEdge;
                    }
                }
                else
                {
                    smallestOverlapLength = null;

                    // no possible collision
                    break;
                }

            }

            if (smallestOverlapLength != null)
            {
                var totalMass = firstMass + secondMass;
                var thisRatioToMove = 1 - (firstMass / totalMass);
                var otherRatiotoMove = (firstMass / totalMass);

                firstVectorResult.X = -thisRatioToMove * smallestOverlapVector.X * smallestOverlapLength.Value;
                firstVectorResult.Y = -thisRatioToMove * smallestOverlapVector.Y * smallestOverlapLength.Value;


                secondVectorResult.X = otherRatiotoMove * smallestOverlapVector.X * smallestOverlapLength.Value;
                secondVectorResult.Y = otherRatiotoMove * smallestOverlapVector.Y * smallestOverlapLength.Value;

            }
        }


        #endregion

        /// <summary>
        /// Modifies the internal points list to flip horizontally. All X values will be multiplied by -1.
        /// </summary>
        public void FlipRelativePointsHorizontally()
        {
            for (int i = 0; i < mPoints.Length; i++)
            {
                mPoints[i].X = -mPoints[i].X;
            }
        }

        /// <summary>
        /// Modifies the internal points list to flip vertically. All Y values will be multiplied by -1;
        /// </summary>
        public void FlipRelativePointsVertically()
        {
            for (int i = 0; i < mPoints.Length; i++)
            {
                mPoints[i].Y = -mPoints[i].Y;
            }
        }


        public override void ForceUpdateDependencies()
        {
            base.ForceUpdateDependencies();

            FillVertexArray();
        }

        public override void ForceUpdateDependenciesDeep()
        {
            base.ForceUpdateDependenciesDeep();

            FillVertexArray();
        }


        #region GetPointsInside
        /// <summary>
        /// Returns a Vector3 array storing all of the points belonging to this instance that are inside the argument
        /// Polygon.
        /// </summary>
        /// <param name="polygon"></param>
        /// <returns></returns>
        public Point3D[] GetPointsInside(Polygon polygon)
        {
            List<Point3D> pointsInside = new List<Point3D>();
            for (int i = 0; i < mVertices.Length; i++)
            {
                VertexPositionColor vertex = mVertices[i];
                if (polygon.IsPointInside(ref vertex.Position))
                {
                    pointsInside.Add(new Point3D(vertex.Position));
                }
            }

            Point3D[] pointsToReturn = pointsInside.ToArray();
            return pointsToReturn;
        }


        public Point3D[] GetPointsInside(Vector3[] vertices)
        {
            List<Point3D> pointsInside = new List<Point3D>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                if (IsPointInside(ref vertices[i]))
                {
                    pointsInside.Add( new Point3D(vertices[i]));
                }
            }

            Point3D[] pointsToReturn = pointsInside.ToArray();
            return pointsToReturn;            
        }


        public Point3D[] GetPointsInside(Point3D[] vertices)
        {
            List<Point3D> pointsInside = new List<Point3D>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                Point3D vertex = vertices[i];

                if (IsPointInside(vertex))
                    pointsInside.Add(vertex);
            }

            Point3D[] pointsToReturn = pointsInside.ToArray();
            return pointsToReturn;
        }
        #endregion

        public double GetArea()
        {
            if (IsConcave())
            {
                throw new NotImplementedException("Cannot get random points inside a concave Polygon.");
            }

            if (mPoints.Length < 4)
            {
                throw new InvalidOperationException("Polygons must have at least three points (plus one extra to close the shape) to have area for random points.");
            }

            Point firstPoint = mPoints[0];
            Point secondPoint = mPoints[1];

            Point thirdPoint;

            double totalArea = 0;

            int i = 0;
            for (i = 2; i < mPoints.Length - 1; i++)
            {
                thirdPoint = mPoints[i];

                // The following formula was retrieved from http://softsurfer.com/Archive/algorithm_0101/algorithm_0101.htm
                // The formula is area = .5 * (   (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0)  )

                double triangleArea =
                    System.Math.Abs(.5 * ((secondPoint.X - firstPoint.X) * (thirdPoint.Y - firstPoint.Y) - (thirdPoint.X - firstPoint.X) * (secondPoint.Y - firstPoint.Y)));
                totalArea += triangleArea;

                secondPoint = thirdPoint;
            }

            return totalArea;

        }

        public Vector3 GetRandomPositionInThis()
        {
            if (IsConcave())
            {
                throw new NotImplementedException("Cannot get random points inside a concave Polygon.");
            }

            if (mPoints.Length < 4)
            {
                throw new InvalidOperationException("Polygons must have at least three points (plus one extra to close the shape) to have area for random points.");
            }

            // The approach that we'll take here is first get all of the areas of each of the triangles, then add them up to get the sum.
            // Next, use a random number between 0 and the sum, then see which triangle that falls in.  This will give larger triangles a greater
            // chance of being used.  Finally, we get a random point inside that triangle.

            Point firstPoint = mPoints[0];
            Point secondPoint = mPoints[1];

            Point thirdPoint;

            List<double> areas = new List<double>();
            double totalArea = 0;

            int i = 0;
            for (i = 2; i < mPoints.Length - 1; i++)
            {
                thirdPoint = mPoints[i];

                // The following formula was retrieved from http://softsurfer.com/Archive/algorithm_0101/algorithm_0101.htm
                // The formula is area = .5 * (   (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0)  )

                double triangleArea =
                    System.Math.Abs(.5 * ((secondPoint.X - firstPoint.X) * (thirdPoint.Y - firstPoint.Y) - (thirdPoint.X - firstPoint.X) * (secondPoint.Y - firstPoint.Y)));
                areas.Add(triangleArea);
                totalArea += triangleArea;

                secondPoint = thirdPoint;

            }

            double randomValue = FlatRedBallServices.Random.NextDouble() * totalArea;

            double sumSoFar = 0;

            for (i = 0; i < areas.Count; i++)
            {
                sumSoFar += areas[i];

                if (sumSoFar >= randomValue)
                {
                    break;
                }
            }

            // Looks like we're using triangle at index [i];  That means we're using point[0], [i + 1], and [i + 2]
            int afterI = i + 1;
            int afterAfterI = i + 2;

            Point relativePoint = MathFunctions.GetPointInTriangle(
                mPoints[0],
                mPoints[afterI],
                mPoints[afterAfterI]);

            MathFunctions.RotatePointAroundPoint(Point.Zero, ref relativePoint, this.mRotationZ);

            relativePoint += this.Position;

            return relativePoint.ToVector3();


        }


        #region XML Docs
        /// <summary>
        /// Returns the relative position of the point at the argument index.
        /// </summary>
        /// <param name="index">The index of the point to get.</param>
        /// <returns>The point at the argument index.</returns>
        #endregion
        public Point GetPoint(int index)
        {
            return mPoints[index];
        }


        public bool Intersects(Segment otherSegment, out Point intersectionPoint)
        {
            intersectionPoint = Point.Zero;

            // Check if one of the polygon's edges intersects the line segment
            for (int i = 0; i < mVertices.Length - 1; i++)
            {
                int indexAfter = i + 1;

                Segment segmentForSide = new Segment(
                      new Point(mVertices[i].Position.X,
                                mVertices[i].Position.Y),
                      new Point(mVertices[indexAfter].Position.X,
                                mVertices[indexAfter].Position.Y));


                if (otherSegment.Intersects(segmentForSide, out intersectionPoint))
                {
                    return true;
                }
            }

            return false;

        }

        #region XML Docs
        /// <summary>
        /// Inserts a new point at the given index.  The point will be inserted at object space.
        /// </summary>
        /// <remarks>
        /// This method recreates the internal point list so it is expensive to call repeatedly.
        /// </remarks>
        /// <param name="index">Index where the point should be inserted.</param>
        /// <param name="newPoint">The (object space) point to insert.</param>
        #endregion
        public void Insert(int index, Point newPoint)
        {
            int pointsCountPlusOne = Points.Count + 1;
            Point[] newPoints = new Point[pointsCountPlusOne];

            for (int i = 0; i < index; i++)
            {
                newPoints[i] = Points[i];
            }

            newPoints[index] = newPoint;

            for (int i = index + 1; i < newPoints.Length; i++)
            {
                int previousIndex = i - 1;

                newPoints[i] = Points[previousIndex];
            }

            Points = newPoints;
        }


        public bool IsConcave()
        {
            // Subtract 2.  One because we don't want to run the last point since
            // it's the same as the first, and again because we are going to compare against
            // another inner loop

            // July 30, 202 - this is really really slow for large polygons, so 
            // this function has been replaced with a faster one:
            //for (int firstPoint = 0; firstPoint < mPoints.Length - 2; firstPoint++)
            //{
            //    for (int secondPoint = 1; secondPoint < mPoints.Length - 1; secondPoint++)
            //    {
            //        if (firstPoint != secondPoint && !ArePointsAdjacent(firstPoint, secondPoint))
            //        {
            //            // If a segment is drawn between two non-adjacent points, then the midpoint
            //            // of that segment should always fall inside the Polygon. If it doesn't, then
            //            // that means that the polygon is concave, so we return true

            //            Point midpoint = (mPoints[firstPoint] + mPoints[secondPoint]) / 2.0f;

            //            FlatRedBall.Math.MathFunctions.TransformPoint(ref midpoint, ref mRotationMatrix);

            //            midpoint += this.Position;

            //            if (!IsPointInside(midpoint.X, midpoint.Y))
            //            {
            //                return true;
            //            }
            //        }
            //    }
            //}
            //return false;


            var pointCount = Points.Count;
            if (pointCount < 4)
            {
                return false;
            }

            var firstAngle = PointExtensionMethods.Angle(mPoints[1] - mPoints[0]) ?? 0;
            var secondAngle = PointExtensionMethods.Angle(mPoints[2] - mPoints[1]) ?? 0;

            var angle = MathFunctions.AngleToAngle(firstAngle, secondAngle);

            for (int i = 1; i < pointCount - 2; i++)
            {
                firstAngle = PointExtensionMethods.Angle(mPoints[i + 1] - mPoints[i]) ?? 0;
                secondAngle = PointExtensionMethods.Angle(mPoints[i + 2] - mPoints[i + 1]) ?? 0;

                var newAngle = MathFunctions.AngleToAngle(firstAngle, secondAngle);

                if(angle == 0)
                {
                    continue;
                }

                if (System.Math.Sign(angle) != System.Math.Sign(newAngle))
                {
                    return true;
                }
            }

            return false;

        }

        // From:
        // https://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order
        public bool IsClockwise()
        {
            double sum = 0;
            for(int i = 0; i < Points.Count-1; i++)
            {
                var point = Points[i];
                var pointAfter = Points[i + 1];

                sum += (pointAfter.X - point.X) * (pointAfter.Y + point.Y);
            }

            return sum > 0;
        }

        #region IsPointInside

        /// <summary>
        /// Returns whether the argument vector is in this polygon.
        /// </summary>
        /// <param name="vector">The position of the point</param>
        /// <returns>Whether the point is inside.</returns>
        public bool IsPointInside(ref Vector3 vector)
        {
            bool b = false;

            for (int i = 0, j = mVertices.Length - 1; i < mVertices.Length; j = i++)
            {
                if ((((mVertices[i].Position.Y <= vector.Y) && (vector.Y < mVertices[j].Position.Y)) || ((mVertices[j].Position.Y <= vector.Y) && (vector.Y < mVertices[i].Position.Y))) &&
                    (vector.X < (mVertices[j].Position.X - mVertices[i].Position.X) * (vector.Y - mVertices[i].Position.Y) / (mVertices[j].Position.Y - mVertices[i].Position.Y) + mVertices[i].Position.X)) b = !b;
            }

            return b;
        }

        public static bool IsPointInside(float x, float y, VertexPositionColor[] vertices)
        {
            bool b = false;

            for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
            {
                if ((((vertices[i].Position.Y <= y) && (y < vertices[j].Position.Y)) ||
                    ((vertices[j].Position.Y <= y) && (y < vertices[i].Position.Y))) &&
                    (x < (vertices[j].Position.X - vertices[i].Position.X) * (y - vertices[i].Position.Y) /
                    (vertices[j].Position.Y - vertices[i].Position.Y) + vertices[i].Position.X))
                {
                    b = !b;
                }
            }

            return b;
        }
        

        public bool IsPointInside(Point3D vector)
        {
            bool b = false;

            for (int i = 0, j = mVertices.Length - 1; i < mVertices.Length; j = i++)
            {
                if ((((mVertices[i].Position.Y <= vector.Y) && (vector.Y < mVertices[j].Position.Y)) || ((mVertices[j].Position.Y <= vector.Y) && (vector.Y < mVertices[i].Position.Y))) &&
                    (vector.X < (mVertices[j].Position.X - mVertices[i].Position.X) * (vector.Y - mVertices[i].Position.Y) / (mVertices[j].Position.Y - mVertices[i].Position.Y) + mVertices[i].Position.X)) b = !b;
            }

            return b;
        }


        public bool IsPointInside(double x, double y)
        {
            bool b = false;

            for (int i = 0, j = mVertices.Length - 1; i < mVertices.Length; j = i++)
            {
                if ((((mVertices[i].Position.Y <= y) && (y < mVertices[j].Position.Y)) || ((mVertices[j].Position.Y <= y) && (y < mVertices[i].Position.Y))) &&
                    (x < (mVertices[j].Position.X - mVertices[i].Position.X) * (y - mVertices[i].Position.Y) / (mVertices[j].Position.Y - mVertices[i].Position.Y) + mVertices[i].Position.X)) b = !b;
            }

            return b;
        }

        static Point sIsPointInsidePoint;
        public bool IsPointInside(ref Point point, ref Vector3 pointOffset, ref Matrix rotationMatrix)
        {
            bool b = false;

            sIsPointInsidePoint = point;
            MathFunctions.TransformPoint(ref sIsPointInsidePoint, ref rotationMatrix);
            sIsPointInsidePoint.X += pointOffset.X;
            sIsPointInsidePoint.Y += pointOffset.Y;

            for (int i = 0, j = mVertices.Length - 1; i < mVertices.Length; j = i++)
            {
                if ((((mVertices[i].Position.Y <= sIsPointInsidePoint.Y) && (sIsPointInsidePoint.Y < mVertices[j].Position.Y)) || ((mVertices[j].Position.Y <= sIsPointInsidePoint.Y) && (sIsPointInsidePoint.Y < mVertices[i].Position.Y))) &&
                    (sIsPointInsidePoint.X < (mVertices[j].Position.X - mVertices[i].Position.X) * (sIsPointInsidePoint.Y - mVertices[i].Position.Y) / (mVertices[j].Position.Y - mVertices[i].Position.Y) + mVertices[i].Position.X)) b = !b;
            }

            return b;
        }


        #endregion


        public void InvertPointOrder()
        {
            Point temporaryPoint = new Point();
            for (int i = 0; i < mPoints.Length/2; i++)
            {
                temporaryPoint = mPoints[i];

                int invertValue = mPoints.Length - 1 - i;

                mPoints[i] = mPoints[invertValue];

                mPoints[invertValue] = temporaryPoint;
            }
        }


        public void OptimizeRadius()
        {
            if (this.Parent != null)
                throw new NotImplementedException("Cannot optimize radius on attached Polygons yet");

            if (Points.Count == 0)
                return;
            else if (Points.Count == 1)
            {
                Point[] newPoints = new Point[1];
                newPoints[0] = new Point(0, 0);

                X = (float)Points[0].X;
                Y = (float)Points[0].Y;
                Points = newPoints;

                CalculateBoundingRadius();

            }
            else
            {
                // for now find the average point.
                Point sumOfAllPoints = new Point(0, 0);

                int numberOfPoints = this.Points.Count;

                // Don't average the last point if it's the same as
                // the first point. We don't wan to skew the values in
                // toward the first point.
                int countMinusOne = Points.Count - 1;

                if (Points[0] == Points[countMinusOne])
                {
                    numberOfPoints = Points.Count - 1;
                }


                for (int i = 0; i < numberOfPoints; i++)
                {
                    sumOfAllPoints.X += Points[i].X + Position.X;
                    sumOfAllPoints.Y += Points[i].Y + Position.Y;
                }

                float newX = (float)sumOfAllPoints.X / (float)numberOfPoints;
                float newY = (float)sumOfAllPoints.Y / (float)numberOfPoints;

                float xDifference = newX - X;
                float yDifference = newY - Y;

                for (int i = 0; i < mPoints.Length; i++)
                {
                    mPoints[i].X -= xDifference;
                    mPoints[i].Y -= yDifference;
                }

                X = newX;
                Y = newY;

                CalculateBoundingRadius();
            }
        }


        public void ProjectParentVelocityOnLastMoveCollisionTangent()
        {
            ProjectParentVelocityOnLastMoveCollisionTangent(0);
        }
        

        public void ProjectParentVelocityOnLastMoveCollisionTangent(float minimumVectorLengthSquared)
        {
#if FRB_MDX
            if (mLastMoveCollisionReposition.LengthSq() > minimumVectorLengthSquared &&
#else
            if (mLastMoveCollisionReposition.LengthSquared() > minimumVectorLengthSquared &&
#endif
                Vector3.Dot(TopParent.Velocity, mLastMoveCollisionReposition) < 0 )
            {
                Vector3 collisionAdjustmentNormalized = mLastMoveCollisionReposition;
                collisionAdjustmentNormalized.Normalize();
                float temporaryFloat = collisionAdjustmentNormalized.X;
                collisionAdjustmentNormalized.X = -collisionAdjustmentNormalized.Y;
                collisionAdjustmentNormalized.Y = temporaryFloat;

                float length = Vector3.Dot(TopParent.Velocity, collisionAdjustmentNormalized);
                TopParent.Velocity = Vector3.Multiply(collisionAdjustmentNormalized, length);
            }
        }

        /// <summary>
        /// Scales all points in the polygon by the argument value. A value of 1 will leave the polygon unchanged.
        /// </summary>
        /// <param name="amountToScaleBy">The value to scale by. For example, a value of 2 will double the X and Y values of all points.</param>
        public void ScaleBy(double amountToScaleBy)
        {
            for (int i = 0; i < mPoints.Length; i++)
            {
                mPoints[i].X *= amountToScaleBy;
                mPoints[i].Y *= amountToScaleBy;
            }

            CalculateBoundingRadius();

        }

        /// <summary>
        /// Scales all points in object space.
        /// </summary>
        /// <param name="scaleX">Amount to scale by on the object's X axis. For example, a value of 2 would double the relative X values of all points.</param>
        /// <param name="scaleY">Amount to scale by on the object's Y axis. For example, a value of 2 would double the relative Y values of all points.</param>
        public void ScaleBy(double scaleX, double scaleY)
        {
            for (int i = 0; i < mPoints.Length; i++)
            {
                mPoints[i].X *= scaleX;
                mPoints[i].Y *= scaleY;
            }

            CalculateBoundingRadius();

        }

        #region XML Docs
        /// <summary>
        /// Changes the position of the point at argument index, recalculates the bounding radius, and raises the
        /// OnPointsChanged event.
        /// </summary>
        /// <param name="index">The index of the point to change.</param>
        /// <param name="xRelativeToPolygonCenter">The new X position of the point in polygon object space.</param>
        /// <param name="yRelativeToPolygonCenter">The new Y position of the point in polygon object space.</param>
        #endregion
        public void SetPoint(int index, double xRelativeToPolygonCenter, double yRelativeToPolygonCenter)
        {

            mPoints[index].X = xRelativeToPolygonCenter;
            mPoints[index].Y = yRelativeToPolygonCenter;

            CalculateBoundingRadius();
            OnPointsChanged(EventArgs.Empty);
        }

        public void SetPoint(int index, Point point)
        {
            mPoints[index] = point;

            CalculateBoundingRadius();
            OnPointsChanged(EventArgs.Empty);
        }

        public void SetPoints(IList<Point> firstPoints, IList<Point> secondPoints, float interpolationValue)
        {
            if (firstPoints.Count != secondPoints.Count)
            {
                throw new ArgumentException("The firstPoints and secondPoints lists must have the same number of Points");
            }

            if (mPoints == null)
            {
                Points = firstPoints;
            }

            if (mPoints.Length != firstPoints.Count)
            {
                throw new ArgumentException("The number of points in the argument point lists does not match the number of points in the Polygon.");
            }

            for (int i = 0; i < mPoints.Length; i++)
            {
                mPoints[i] = new Point(
                    firstPoints[i].X * (1 - interpolationValue) + secondPoints[i].X * interpolationValue,
                    firstPoints[i].Y * (1 - interpolationValue) + secondPoints[i].Y * interpolationValue);
            }
        }


        public void SetPointFromAbsolutePosition(int index, double xAbsolute, double yAbsolute)
        {
            xAbsolute -= Position.X;
            yAbsolute -= Position.Y;

            Matrix invertedMatrix = Matrix.Invert(RotationMatrix);

            mPoints[index].X = xAbsolute;
            mPoints[index].Y = yAbsolute;

            FlatRedBall.Math.MathFunctions.TransformPoint(ref mPoints[index], ref invertedMatrix);
            
            CalculateBoundingRadius();
            OnPointsChanged(EventArgs.Empty);
        }


        public void SimulateCollideAgainstMove(ref Vector3 moveVector)
        {
            TopParent.Position += moveVector;
            mLastMoveCollisionReposition = moveVector;
            ForceUpdateDependencies();
        }


        public override string ToString()
        {
            return $"Name: {Name ?? "<NULL>"} Points: {mPoints.Length}";


        }


        public override void UpdateDependencies(double currentTime)
        {
            UpdateDependencies(currentTime, true);
        }


        public void UpdateDependencies(double currentTime, bool callFillVertexArray)
        {
            base.UpdateDependencies(currentTime);
            if (callFillVertexArray)
            {
                FillVertexArray();
            }
        }

        /// <summary>
        /// Returns a vector from the argument vector to the closest point on the Polygon's edges. This method considers
        /// only the perimeter, so an argument point inside the polygon will still return a non-zero-length vector.
        /// </summary>
        /// <param name="vector">The point to start from.</param>
        /// <returns>A vector representing the distance from the argument vector to this.</returns>
        public Point3D VectorFrom(Point3D vector)
        {

            double minDistance = double.PositiveInfinity;
            double tempMinDistance;
            Point3D vectorToReturn = new Point3D();

            if (mVertices.Length == 1)
            {
                return new Point3D(
                    mVertices[0].Position.X - vector.X,
                    mVertices[0].Position.Y - vector.Y,
                    mVertices[0].Position.Z - vector.Z);
            }

            Segment connectingSegment = new Segment();
            Segment segment = new Segment();

            for (int i = 0; i < mVertices.Length - 1; i++)
            {
                int afterI = i + 1;
                segment.Point1 = new Point(mVertices[i].Position.X, mVertices[i].Position.Y);
                segment.Point2 = new Point(mVertices[afterI].Position.X, mVertices[afterI].Position.Y);

                tempMinDistance = segment.DistanceTo(new Point(vector.X, vector.Y), out connectingSegment);
                if (tempMinDistance < minDistance)
                {
                    minDistance = tempMinDistance;
                    vectorToReturn = new Point3D(
                        connectingSegment.Point2.X - connectingSegment.Point1.X,
                        connectingSegment.Point2.Y - connectingSegment.Point1.Y,
                        0);
                }
            }
            return vectorToReturn;
        }

        private static int sThrowAwayInt;
        /// <summary>
        /// Returns a vector from the argument vector to the closest point on the Polygon's edges. This method considers
        /// only the perimeter, so an argument point inside the polygon will still return a non-zero-length vector.
        /// </summary>
        /// <param name="x">The absolute X to check against the polygon.</param>
        /// <param name="y">The absolute Y to check against the polygon.</param>
        /// <returns>The shortest vector from the argument x,y to the Polygon.</returns>
        public Point3D VectorFrom(double x, double y)
        {
            return VectorFrom(x, y, out sThrowAwayInt);
        }

        /// <summary>
        /// Returns a vector from the argument vector to the closest point on the Polygon's edges. This method considers
        /// only the perimeter, so an argument point inside the polygon will still return a non-zero-length vector.
        /// </summary>
        /// <param name="x">The absolute X to check against the polygon.</param>
        /// <param name="y">The absolute Y to check against the polygon.</param>
        /// <param name="pointIndexBefore">The index of the point that begins the line on which the closest point falls upon.</param>
        /// <returns>The shortest vector from the argument x, y to the Polygon.</returns>
        public Point3D VectorFrom(double x, double y, out int pointIndexBefore)
        {
            return VectorFrom(x, y, mVertices, out pointIndexBefore);
        }

        public static Point3D VectorFrom(double x, double y, VertexPositionColor[] vertices, out int pointIndexBefore)
        {

            pointIndexBefore = -1;

            double minDistance = double.PositiveInfinity;
            double tempMinDistance;
            Point3D vectorToReturn = new Point3D();

            if (vertices.Length == 1)
            {
                return new Point3D(
                    vertices[0].Position.X - x,
                    vertices[0].Position.Y - y,
                    0);
            }

            Segment connectingSegment = new Segment();
            Segment segment = new Segment();

            for (int i = 0; i < vertices.Length - 1; i++)
            {
                int afterI = i + 1;
                segment.Point1 = new Point(vertices[i].Position.X, vertices[i].Position.Y);
                segment.Point2 = new Point(vertices[afterI].Position.X, vertices[afterI].Position.Y);

                tempMinDistance = segment.DistanceTo(new Point(x, y), out connectingSegment);
                if (tempMinDistance < minDistance)
                {
                    pointIndexBefore = i;
                    minDistance = tempMinDistance;
                    vectorToReturn = new Point3D(
                        connectingSegment.Point2.X - connectingSegment.Point1.X,
                        connectingSegment.Point2.Y - connectingSegment.Point1.Y,
                        0);
                }
            }
            return vectorToReturn;
        }

        Point mVectorFromPoint;
        public Point3D VectorFrom(ref Point relativePoint, ref Vector3 absolutePosition, ref Matrix rotationMatrix)
        {
            mVectorFromPoint = relativePoint;
            MathFunctions.TransformPoint(ref mVectorFromPoint, ref rotationMatrix);
            mVectorFromPoint += absolutePosition;

            double minDistance = double.PositiveInfinity;
            double tempMinDistance;
            Point3D vectorToReturn = new Point3D();

            if (mVertices.Length == 1)
            {
                return new Point3D(
                    mVertices[0].Position.X - (mVectorFromPoint.X),
                    mVertices[0].Position.Y - (mVectorFromPoint.Y),
                    0);
            }

            Segment connectingSegment = new Segment();
            Segment segment = new Segment();

            for (int i = 0; i < mVertices.Length - 1; i++)
            {
                int afterI = i + 1;
                segment.Point1 = new Point(mVertices[i].Position.X, mVertices[i].Position.Y);
                segment.Point2 = new Point(mVertices[afterI].Position.X, mVertices[afterI].Position.Y);

                tempMinDistance = segment.DistanceTo(mVectorFromPoint, out connectingSegment);
                if (tempMinDistance < minDistance)
                {
                    minDistance = tempMinDistance;
                    vectorToReturn = new Point3D(
                        connectingSegment.Point2.X - connectingSegment.Point1.X,
                        connectingSegment.Point2.Y - connectingSegment.Point1.Y,
                        0);


                }
            }


            return vectorToReturn;
        }

        #endregion

        #region Private Methods

        void CalculateBoundingRadius()
        {
            // calculate the mBoundingRadius which is used in collisions.
            // just having this around can help speed up most collisions.
            float boundingRadiusSquared = 0;
            for (int i = 0; i < mPoints.Length; i++)
            {
                Point p = mPoints[i];
                boundingRadiusSquared =
                    System.Math.Max(boundingRadiusSquared, (float)(p.X * p.X + p.Y * p.Y));
            }

            mBoundingRadius = (float)System.Math.Sqrt(boundingRadiusSquared);
        }

        internal void FillVertexArray()
        {
#if DEBUG
            if (mVertices == null && TolerateEmptyPolygons == false)
            {
                throw new NullReferenceException("Polygon has not had its points set.");

            }

#endif

            if(mVertices != null)
            {

                var premultiplied = new Color();
                premultiplied.A = mColor.A;
                premultiplied.R = (byte)(mColor.R * mColor.A / 255);
                premultiplied.G = (byte)(mColor.G * mColor.A / 255);
                premultiplied.B = (byte)(mColor.B * mColor.A / 255);
                
                for (int i = 0; i < mPoints.Length; i++)
                {

                    mVertices[i].Position.X = (float)(Position.X +
                        mRotationMatrix.M11 * mPoints[i].X +
                        mRotationMatrix.M21 * mPoints[i].Y  );

                    mVertices[i].Position.Y = (float)(Position.Y +
                        mRotationMatrix.M12 * mPoints[i].X +
                        mRotationMatrix.M22 * mPoints[i].Y  );

                    mVertices[i].Position.Z = (float)(Position.Z +
                        mRotationMatrix.M13 * mPoints[i].X +
                        mRotationMatrix.M23 * mPoints[i].Y);


                    mVertices[i].Color= premultiplied;


                }
            }
        }

        #endregion

        #region Protected Methods

        protected virtual void OnPointsChanged(EventArgs e)
        {

            if (PointsChanged != null)
                PointsChanged(this, e);
        }

        #endregion

        #endregion

        #region IEquatable<Polygon> Members

        bool IEquatable<Polygon>.Equals(Polygon other)
        {
            return this == other;
        }

        #endregion

        #region IMouseOver Implementation
        public bool IsMouseOver(Cursor cursor)
        {
            return cursor.IsOn3D(this);
        }

        public bool IsMouseOver(Cursor cursor, Layer layer)
        {
            return cursor.IsOn3D(this, layer);
        }
        #endregion

        #region IVisible implementation
        IVisible IVisible.Parent
        {
            get
            {
                return mParent as IVisible;
            }
        }

        public bool AbsoluteVisible
        {
            get
            {
                IVisible iVisibleParent = ((IVisible)this).Parent;
                return mVisible && (iVisibleParent == null || IgnoresParentVisibility || iVisibleParent.AbsoluteVisible);
            }
        }

        public bool IgnoresParentVisibility
        {
            get;
            set;
        }
        #endregion
    }

    #region PolygonListExtensionMethods

    public static class PolygonListExtensionMethods
    {
        public static Polygon GetRandomPolygonWithWeightedArea(this IList<Polygon> polygonList)
        {
            double totalArea = 0;
            List<double> areas = new List<double>();

            if(polygonList.Count == 0)
            {
                return null;
            }
            if(polygonList.Count == 1)
            {
                return polygonList[1];
            }

            int count = polygonList.Count;
            int i = 0;
            for(i = 0; i < count; i++)
            {
                double area = polygonList[i].GetArea();

                totalArea += area;
                areas.Add(area);
            }

            double randomValue = FlatRedBallServices.Random.NextDouble() * totalArea;

                        
            double sumSoFar = 0;

            for (i = 0; i < areas.Count; i++)
            {
                sumSoFar += areas[i];

                if (sumSoFar >= randomValue)
                {
                    break;
                }
            }

            return polygonList[i];
        }


    }

    #endregion
}
