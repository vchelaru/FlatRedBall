using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using FlatRedBall.Instructions;
using Microsoft.Xna.Framework;


using FlatRedBall.Graphics;

namespace FlatRedBall.Math.Geometry
{
    #region Enums
    public enum ShapeDrawingOrder
    {
        UnderEverything,
        OverEverything
    }

    public enum RepositionDirections
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
        All = 15,

    }

    #endregion

    #region XML Docs
    /// <summary>
    /// Responsible for creating, destroying, and managing shapes (Circle, AxisAlignedRectangle,
    /// Polygon, Line).
    /// </summary>
    #endregion
    public static class ShapeManager
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The number of vertices used when drawing a Circle.
        /// </summary>
        #endregion
        public const int NumberOfVerticesForCircles = 20;

        #region XML Docs
        /// <summary>
        /// The number of vertices used when drawing Capsule2Ds.
        /// </summary>
        #endregion
        public const int NumberOfVerticesForCapsule2Ds = 23;

        #region XML Docs
        /// <summary>
        /// The number of vertices used when drawing an AxisAlignedCube.
        /// </summary>
        #endregion
        public const int NumberOfVerticesForCubes = 18;

        #region XML Docs
        /// <summary>
        /// The number of vertices used when drawing a Sphere.
        /// </summary>
        #endregion
        public const int NumberOfVerticesForSpheres = 60;

        #region XML Docs
        /// <summary>
        /// List of all managed objects.  This list contains all types of shapes
        /// (Circles, Polygons, etc).  This list is only used for the TimedActivity;
        /// not for drawing.  Therefore, shapes can exist both in this list as well as
        /// in the type-specific lists (mCircles, mPolygons, etc).
        /// </summary>
        #endregion
        static internal PositionedObjectList<PositionedObject> mAutomaticallyUpdated;

        // internal so the Renderer can access the lists for drawing
        static internal PositionedObjectList<AxisAlignedRectangle> mRectangles;
        static internal PositionedObjectList<Circle> mCircles;
        static internal PositionedObjectList<Polygon> mPolygons;
        static internal PositionedObjectList<Line> mLines;
        static internal PositionedObjectList<Sphere> mSpheres;
        static internal PositionedObjectList<AxisAlignedCube> mCubes;
        static internal PositionedObjectList<Capsule2D> mCapsule2Ds;

        static ReadOnlyCollection<PositionedObject> mAutomaticallyUpdatedReadOnlyCollection;
        // Readonly instances of objects which expose the visible shapes
        static ReadOnlyCollection<AxisAlignedRectangle> mRectanglesReadOnlyCollection;
        static ReadOnlyCollection<Circle> mCirclesReadOnlyCollection;
        static ReadOnlyCollection<Polygon> mPolygonsReadOnlyCollection;
        static ReadOnlyCollection<Line> mLinesReadOnlyCollection;
        static ReadOnlyCollection<Sphere> mSpheresReadOnlyCollection;
        static ReadOnlyCollection<AxisAlignedCube> mCubesReadOnlyCollection;
        static ReadOnlyCollection<Capsule2D> mCapsule2DsReadOnlyCollection;

        internal static Vector3[] UnscaledCubePoints;

        //static internal ListBuffer<PositionedObject> mAutomaticallyUpdatedBuffer;

        // internal so the Renderer can access the lists for drawing
        //static internal ListBuffer<AxisAlignedRectangle> mRectangleBuffer;
        //static internal ListBuffer<Circle> mCircleBuffer;
        //static internal ListBuffer<Polygon> mPolygonBuffer;
        //static internal ListBuffer<Line> mLineBuffer;
        //static internal ListBuffer<Sphere> mSphereBuffer;
        //static internal ListBuffer<AxisAlignedCube> mCubeBuffer;
        //static internal ListBuffer<Capsule2D> mCapsule2DBuffer;


        static bool mUseZTestingWhenDrawing = true;
        static ShapeDrawingOrder mShapeDrawingOrder = ShapeDrawingOrder.OverEverything;

#if SILVERLIGHT
        static Microsoft.Xna.Framework.Graphics.SpriteBatch sSpriteBatch;
        static Canvas mCanvas;
#endif

        #endregion

        #region Properties

        public static ShapeDrawingOrder ShapeDrawingOrder
        {
            get { return mShapeDrawingOrder; }
            set { mShapeDrawingOrder = value; }
        }

        #region XML Docs
        /// <summary>
        /// Controls whether the ZBuffer is tested against when drawing shapes.
        /// Set to false to have Shapes drawn on top.
        /// </summary>
        #endregion
        public static bool UseZTestingWhenDrawing
        {
            get { return mUseZTestingWhenDrawing; }
            set { mUseZTestingWhenDrawing = value; }
        }

        #region XML Docs
        /// <summary>
        /// A read-only list of visible Circles contained in the ShapeManager.
        /// </summary>
        #endregion
        static public ReadOnlyCollection<Circle> VisibleCircles
        {
            get { return mCirclesReadOnlyCollection; }
        }

        #region XML Docs
        /// <summary>
        /// A read-only list of visible AxisAlignedRectangles contained in the ShapeManager.
        /// </summary>
        #endregion
        static public ReadOnlyCollection<AxisAlignedRectangle> VisibleRectangles
        {
            get { return mRectanglesReadOnlyCollection; }
        }

        #region XML Docs
        /// <summary>
        /// A read-only list of visible Polygons contained in the ShapeManager.
        /// </summary>
        #endregion
        static public ReadOnlyCollection<Polygon> VisiblePolygons
        {
            get { return mPolygonsReadOnlyCollection; }
        }

        #region XML Docs
        /// <summary>
        /// A read-only list of visible Lines contained in the ShapeManager.
        /// </summary>
        #endregion
        static public ReadOnlyCollection<Line> VisibleLines
        {
            get { return mLinesReadOnlyCollection; }
        }

        #region XML Docs
        /// <summary>
        /// A read-only list of visible Spheres contained in the ShapeManager.
        /// </summary>
        #endregion
        static public ReadOnlyCollection<Sphere> VisibleSpheres
        {
            get { return mSpheresReadOnlyCollection; }
        }

        #region XML Docs
        /// <summary>
        /// A read-only list of visible AxisAlignedCubes contained in the ShapeManager.
        /// </summary>
        #endregion
        static public ReadOnlyCollection<AxisAlignedCube> VisibleAxisAlignedCubes
        {
            get { return mCubesReadOnlyCollection; }
        }

        #region XML Docs
        /// <summary>
        /// A read-only list of visible Capsules contained in the ShapeManager.
        /// </summary>
        #endregion
        static public ReadOnlyCollection<Capsule2D> VisibleCapsule2Ds
        {
            get { return mCapsule2DsReadOnlyCollection; }
        }

        #region XML Docs
        /// <summary>
        /// A read-only list of shapes updated by the ShapeManager.
        /// </summary>
        #endregion
        static public ReadOnlyCollection<PositionedObject> AutomaticallyUpdatedShapes
        {
            get { return mAutomaticallyUpdatedReadOnlyCollection; }
        }

        public static bool SuppressAddingOnVisibilityTrue
        {
            get;
            set;
        }

        #endregion

        #region Methods

        #region Constructor/Initialize

        // made public for unit tests
        public static void Initialize()
        {

            mAutomaticallyUpdated = new PositionedObjectList<PositionedObject>();
            mAutomaticallyUpdated.Name = "ShapeManager Automatically Updated Shapes";

            mRectangles = new PositionedObjectList<AxisAlignedRectangle>();
            mRectangles.Name = "Visible AxisAlignedRectangles";

            mCircles = new PositionedObjectList<Circle>();
            mCircles.Name = "Visible Circles";

            mPolygons = new PositionedObjectList<Polygon>();
            mPolygons.Name = "Visible Polygons";

            mLines = new PositionedObjectList<Line>();
            mLines.Name = "Visible Lines";

            mSpheres = new PositionedObjectList<Sphere>();
            mSpheres.Name = "Visible Spheres";

            mCubes = new PositionedObjectList<AxisAlignedCube>();
            mCubes.Name = "Visible Cubes";

            mCapsule2Ds = new PositionedObjectList<Capsule2D>();
            mCapsule2Ds.Name = "Visible Capsule2Ds";

            mAutomaticallyUpdatedReadOnlyCollection = new ReadOnlyCollection<PositionedObject>(mAutomaticallyUpdated);
            mRectanglesReadOnlyCollection = new ReadOnlyCollection<AxisAlignedRectangle>(mRectangles);
            mCirclesReadOnlyCollection = new ReadOnlyCollection<Circle>(mCircles);
            mPolygonsReadOnlyCollection = new ReadOnlyCollection<Polygon>(mPolygons);
            mLinesReadOnlyCollection = new ReadOnlyCollection<Line>(mLines);
            mSpheresReadOnlyCollection = new ReadOnlyCollection<Sphere>(mSpheres);
            mCubesReadOnlyCollection = new ReadOnlyCollection<AxisAlignedCube>(mCubes);
            mCapsule2DsReadOnlyCollection = new ReadOnlyCollection<Capsule2D>(mCapsule2Ds);

#if SILVERLIGHT
            sSpriteBatch = Renderer.GraphicsBatch;// new SpriteBatch(FlatRedBallServices.Game.GraphicsDevice);
#endif



            #region Create the UnscaledCubePoints

            UnscaledCubePoints = new Vector3[16];

            // Top
            UnscaledCubePoints[0] = new Vector3(-1, 1, 1);
            UnscaledCubePoints[1] = new Vector3(1, 1, 1);
            UnscaledCubePoints[2] = new Vector3(1, 1, -1);
            UnscaledCubePoints[3] = new Vector3(-1, 1, -1);
            UnscaledCubePoints[4] = new Vector3(-1, 1, 1);

            // Bottom
            UnscaledCubePoints[5] = new Vector3(-1, -1, 1);
            UnscaledCubePoints[6] = new Vector3(1, -1, 1);
            UnscaledCubePoints[7] = new Vector3(1, -1, -1);
            UnscaledCubePoints[8] = new Vector3(-1, -1, -1);
            UnscaledCubePoints[9] = new Vector3(-1, -1, 1);

            // Column
            UnscaledCubePoints[10] = new Vector3(1, 1, 1);
            UnscaledCubePoints[11] = new Vector3(1, -1, 1);

            // Column
            UnscaledCubePoints[12] = new Vector3(1, 1, -1);
            UnscaledCubePoints[13] = new Vector3(1, -1, -1);

            // Column
            UnscaledCubePoints[14] = new Vector3(-1, 1, -1);
            UnscaledCubePoints[15] = new Vector3(-1, -1, -1);

            #endregion


        }


        #endregion

        #region Public Methods

        #region Add Methods

#if DEBUG
        static void ThrowExceptionIfNotPrimaryThread()
        {
            if (!FlatRedBallServices.IsThreadPrimary())
            {
                throw new InvalidOperationException("Objects can only be added, removed, made visible, or made invisible on the primary thread");
            }
        }
#endif

        #region XML Docs
        /// <summary>
        /// Creates and returns a new visible, managed AxisAlignedRectangle.
        /// </summary>
        /// <remarks>
        /// The new AxisAlignedRectangle will be visible, white, and have a ScaleX and ScaleY of 1.
        /// </remarks>
        /// <returns>The new AxisAlignedRectangle.</returns>
        #endregion
        static public AxisAlignedRectangle AddAxisAlignedRectangle()
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            AxisAlignedRectangle aar = new AxisAlignedRectangle();
            if (SpriteManager.Camera.Orthogonal == true)
            {
                aar.ScaleX = SpriteManager.Camera.OrthogonalWidth / 8;
                aar.ScaleY = aar.ScaleX;
            }
            return AddAxisAlignedRectangle(aar);
        }

        #region XML Docs
        /// <summary>
        /// Adds an already-created AxisAlignedRectangle to the ShapeManager.
        /// The newly-added AxisAlignedRectangle will be made visible by this method.
        /// </summary>
        /// <param name="axisAlignedRectangle">The AxisAlignedRectangle to add.</param>
        /// <returns>The same AxisAlignedRectangle as was passed to the method.</returns>
        #endregion
        static public AxisAlignedRectangle AddAxisAlignedRectangle(AxisAlignedRectangle axisAlignedRectangle)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            mAutomaticallyUpdated.Add(axisAlignedRectangle);

            axisAlignedRectangle.Visible = true;



            // If a Shape that is visible is cloned, then the new Shape
            // will also have its Visible property be true; however, it will
            // not belong to the drawn Shape List.  Setting visible to true or false
            // adds/removes a shape from the list list ONLY if the boolean changes
            // in the property.  If a cloned shape already has visible true, then the above
            // statement will not do anything.  This statement verifies that the shape is
            // drawn.
            if (axisAlignedRectangle.ListsBelongingTo.Contains(mRectangles) == false)
            {
                mRectangles.Add(axisAlignedRectangle);
            }

            return axisAlignedRectangle;
        }

        #region XML Docs
        /// <summary>
        /// Adds all AxisAlignedRectangles contained in the argument axisAlignedRectangleList to the ShapeManager.
        /// </summary>
        /// <param name="axisAlignedRectangleList">The list containing the AxisAlignedRectangles.</param>
        #endregion
        static public void AddAxisAlignedRectangleList(PositionedObjectList<AxisAlignedRectangle> axisAlignedRectangleList)
        {
            foreach (AxisAlignedRectangle aaRect in axisAlignedRectangleList)
            {
                AddAxisAlignedRectangle(aaRect);
            }
        }

        #region XML Docs
        /// <summary>
        /// Adds and returns a new visible, managed Capsule2D.
        /// </summary>
        /// <remarks>
        /// The new Capsule2D will be visible, white, and have an EndpointRadius of 1.
        /// </remarks>
        /// <returns>The new Capsule2D.</returns>
        #endregion
        static public Capsule2D AddCapsule2D()
        {
            return AddCapsule2D(new Capsule2D());
        }

        #region XML Docs
        /// <summary>
        /// Adds an already-created Capsule2D to the ShapeManager.
        /// The newly-added Capsule2D will be made visible by this method.
        /// </summary>
        /// <param name="capsule2D">The Capsule2D to add.</param>
        /// <returns>The instance that was just added.</returns>
        #endregion
        static public Capsule2D AddCapsule2D(Capsule2D capsule2D)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            mAutomaticallyUpdated.Add(capsule2D);
            capsule2D.Visible = true;

            // If a Shape that is visible is cloned, then the new Shape
            // will also have its Visible property be true; however, it will
            // not belong to the drawn Shape List.  Setting visible to true or false
            // adds/removes a shape from the list list ONLY if the boolean changes
            // in the property.  If a cloned shape already has visible true, then the above
            // statement will not do anything.  This statement verifies that the shape is
            // drawn.
            if (capsule2D.ListsBelongingTo.Contains(mCapsule2Ds) == false &&
                mCapsule2Ds.Contains(capsule2D) == false)
            {
                mCapsule2Ds.Add(capsule2D);
            }

            return capsule2D;
        }

        #region XML Docs
        /// <summary>
        /// Adds and returns a new visible, managed Circle.
        /// </summary>
        /// <remarks>
        /// The new Circle will be visible, white, and have a Radius of 1.
        /// </remarks>
        /// <returns>The new Circle.</returns>
        #endregion
        static public Circle AddCircle()
        {
            return AddCircle(new Circle());

        }

        #region XML Docs
        /// <summary>
        /// Adds an already-created Circle to the ShapeManager.
        /// The newly-added Circle will be made visible by this method.
        /// </summary>
        /// <param name="circle">The Circle to add.</param>
        /// <returns>The instance that was just added.</returns>
        #endregion
        static public Circle AddCircle(Circle circle)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            mAutomaticallyUpdated.Add(circle);
            circle.Visible = true;

            // If a Shape that is visible is cloned, then the new Shape
            // will also have its Visible property be true; however, it will
            // not belong to the drawn Shape List.  Setting visible to true or false
            // adds/removes a shape from the list list ONLY if the boolean changes
            // in the property.  If a cloned shape already has visible true, then the above
            // statement will not do anything.  This statement verifies that the shape is
            // drawn.
            if (circle.ListsBelongingTo.Contains(mCircles) == false &&
                mCircles.Contains(circle) == false)
            {
                mCircles.Add(circle);
            }

            return circle;
        }

        #region XML Docs
        /// <summary>
        /// Adds all Circles contained in the argument circleList to the ShapeManager.
        /// </summary>
        /// <param name="circleList">The list containing the Circles.</param>
        #endregion
        static public void AddCircleList(PositionedObjectList<Circle> circleList)
        {
            foreach (Circle circle in circleList)
            {
                AddCircle(circle);
            }
        }

        #region XML Docs
        /// <summary>
        /// Adds and returns a new visible, managed Sphere.
        /// </summary>
        /// <remarks>
        /// The new Sphere will be visible, white, and have a Radius of 1.
        /// </remarks>
        /// <returns>The new Sphere.</returns>
        #endregion
        static public Sphere AddSphere()
        {
            return AddSphere(new Sphere());

        }

        #region XML Docs
        /// <summary>
        /// Adds an already-created Sphere to the ShapeManager.
        /// The newly-added Sphere will be made visible by this method.
        /// </summary>
        /// <param name="sphere">The Sphere to add.</param>
        /// <returns>The instance that was just added.</returns>
        #endregion
        static public Sphere AddSphere(Sphere sphere)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            mAutomaticallyUpdated.Add(sphere);
            sphere.Visible = true;

            // If a Shape that is visible is cloned, then the new Shape
            // will also have its Visible property be true; however, it will
            // not belong to the drawn Shape List.  Setting visible to true or false
            // adds/removes a shape from the list list ONLY if the boolean changes
            // in the property.  If a cloned shape already has visible true, then the above
            // statement will not do anything.  This statement verifies that the shape is
            // drawn.
            if (sphere.ListsBelongingTo.Contains(mSpheres) == false)
            {
                mSpheres.Add(sphere);
            }

            return sphere;
        }

        #region XML Docs
        /// <summary>
        /// Adds a new visible, managed AxisAlignedCube.
        /// </summary>
        /// <returns>The new AxisAlignedCube</returns>
        #endregion
        static public AxisAlignedCube AddAxisAlignedCube()
        {
            return AddAxisAlignedCube(new AxisAlignedCube());
        }

        #region XML Docs
        /// <summary>
        /// Adds an already-created AxisAlignedCube to the ShapeManager.
        /// The newly-added AxisAlignedCube will be made visible by this method.
        /// </summary>
        /// <param name="axisAlignedCube">The AxisAlignedCube to add.</param>
        /// <returns>The instance that was just added.</returns>
        #endregion
        public static AxisAlignedCube AddAxisAlignedCube(AxisAlignedCube axisAlignedCube)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            mAutomaticallyUpdated.Add(axisAlignedCube);
            axisAlignedCube.Visible = true;

            // If a Shape that is visible is cloned, then the new Shape
            // will also have its Visible property be true; however, it will
            // not belong to the drawn Shape List.  Setting visible to true or false
            // adds/removes a shape from the list list ONLY if the boolean changes
            // in the property.  If a cloned shape already has visible true, then the above
            // statement will not do anything.  This statement verifies that the shape is
            // drawn.
            if (axisAlignedCube.ListsBelongingTo.Contains(mCubes) == false)
            {
                mCubes.Add(axisAlignedCube);
            }

            return axisAlignedCube;

        }

        #region XML Docs
        /// <summary>
        /// Adds a new 0-point Polygon to the ShapeManager.
        /// The newly-added Polygon must have its Points property
        /// set to be visible and functional.
        /// </summary>
        /// <returns>The new Polygon.</returns>
        #endregion
        static public Polygon AddPolygon()
        {
            return AddPolygon(new Polygon());
        }

        #region XML Docs
        /// <summary>
        /// Adds an already-created Polygon to the ShapeManager.
        /// The newly-added Polygon will be made visible by this method 
        /// if it has any points.
        /// </summary>
        /// <param name="polygon">The Polygon to add.</param>
        /// <returns>The instance that was just added.</returns>
        #endregion
        static public Polygon AddPolygon(Polygon polygon)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            if (mAutomaticallyUpdated.Contains(polygon) == false)
            {
                mAutomaticallyUpdated.Add(polygon);
            }

            polygon.Visible = true;
            // If a Shape that is visible is cloned, then the new Shape
            // will also have its Visible property be true; however, it will
            // not belong to the drawn Shape List.  Setting visible to true or false
            // adds/removes a shape from the list list ONLY if the boolean changes
            // in the property.  If a cloned shape already has visible true, then the above
            // statement will not do anything.  This statement verifies that the shape is
            // drawn.
            if (polygon.ListsBelongingTo.Contains(mPolygons) == false)
            {
                mPolygons.Add(polygon);
            }

            return polygon;
        }

        #region XML Docs
        /// <summary>
        /// Adds all Polygons contained in the argument polygonList to the ShapeManager.
        /// </summary>
        /// <param name="polygonList">The list containing the Polygons.</param>
        #endregion
        static public void AddPolygonList<T>(IList<T> polygonList) where T : Polygon
        {
            foreach (Polygon polygon in polygonList)
            {
                AddPolygon(polygon);
            }
        }


        #region XML Docs
        /// <summary>
        /// Adds and returns a new visible, managed Line.
        /// </summary>
        /// <remarks>
        /// The new Line will be visible, white, horizontal,
        /// and have a length of 2 units.
        /// </remarks>
        /// <returns>The new Line.</returns>
        #endregion
        static public Line AddLine()
        {
            return AddLine(new Line());
        }

        #region XML Docs
        /// <summary>
        /// Adds an already-created Line to the ShapeManager.
        /// The newly-added Line will be made visible by this method.
        /// </summary>
        /// <param name="line">The Line to add.</param>
        /// <returns>The instance that was just added.</returns>
        #endregion
        static public Line AddLine(Line line)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();

            if (mAutomaticallyUpdated.Contains(line))
            {
                throw new InvalidOperationException("This line is already part of the ShapeManager.  This exception is being thrown to help you avoid double-adds.");
            }

#endif
            mAutomaticallyUpdated.Add(line);
            line.Visible = true;

            // If a Shape that is visible is cloned, then the new Shape
            // will also have its Visible property be true; however, it will
            // not belong to the drawn Shape List.  Setting visible to true or false
            // adds/removes a shape from the list list ONLY if the boolean changes
            // in the property.  If a cloned shape already has visible true, then the above
            // statement will not do anything.  This statement verifies that the shape is
            // drawn.
            if (line.ListsBelongingTo.Contains(mLines) == false)
            {
                mLines.Add(line);
            }


            return line;
        }
        #endregion

        #region AddToLayer

        /// <summary>
        /// Adds the argument rectangle to the layer and optionally makes the rectangle automatically managed.
        /// </summary>
        /// <remarks>If the rectnagle's Visible property is set to false, the rectangle will not be added to the layer, but it will
        /// set an internal value so that it will show up on the layer when its Visibility is set to true.</remarks>
        /// <param name="rectangle">The rectangle to add.</param>
        /// <param name="layer">The layer to add to.</param>
        /// <param name="makeAutomaticallyUpdated">Whether the rectangle should also be automatically managed by the ShapeManager.</param>
        public static void AddToLayer(AxisAlignedRectangle rectangle, Layer layer, bool makeAutomaticallyUpdated = true)
        {
            if (makeAutomaticallyUpdated && !rectangle.ListsBelongingTo.Contains(mAutomaticallyUpdated))
            {
                mAutomaticallyUpdated.Add(rectangle);
            }
            


            if (layer != null)
            {
                bool shouldMakeVisible = rectangle.mLayerBelongingTo == null && rectangle.ListsBelongingTo.Contains(mRectangles) == false
                    && rectangle.Visible;


                if (rectangle.Visible)
                {
                    if (rectangle.mLayerBelongingTo != null)
                    {
                        rectangle.mLayerBelongingTo.mRectangles.Remove(rectangle);
                    }
                    else
                    {
                        mRectangles.Remove(rectangle);
                    }

                    layer.mRectangles.Add(rectangle);
                }

                rectangle.mLayerBelongingTo = layer;

                if (shouldMakeVisible)
                {
                    rectangle.Visible = true;
                }
            }
            else
            {
                // See AddToLayer(Line, Layer) for info on this
                rectangle.Visible = true;

                if (rectangle.ListsBelongingTo.Contains(mRectangles) == false)
                {
                    mRectangles.Add(rectangle);
                }
            }
        }

        public static void AddToLayer(AxisAlignedCube cube, Layer layer, bool makeAutomaticallyUpdated = true)
        {
            if (makeAutomaticallyUpdated && !cube.ListsBelongingTo.Contains(mAutomaticallyUpdated))
            {
                mAutomaticallyUpdated.Add(cube);
            }

            if (layer != null)
            {
                if (!cube.ListsBelongingTo.Contains(mAutomaticallyUpdated))
                {
                    mAutomaticallyUpdated.Add(cube);

                }

                if (cube.Visible)
                {
                    if (cube.mLayerBelongingTo != null)
                    {
                        cube.mLayerBelongingTo.mCubes.Remove(cube);
                    }
                    else
                    {
                        mCubes.Remove(cube);
                    }

                    layer.mCubes.Add(cube);
                }

                cube.mLayerBelongingTo = layer;
            }
            else
            {
                cube.Visible = true;

                if (cube.ListsBelongingTo.Contains(mCubes) == false)
                {
                    mCubes.Add(cube);
                }
            }
        }

        public static void AddToLayer(Circle circle, Layer layer, bool makeAutomaticallyUpdated = true)
        {
            if (makeAutomaticallyUpdated && !circle.ListsBelongingTo.Contains(mAutomaticallyUpdated))
            {
                mAutomaticallyUpdated.Add(circle);
            }

            if (layer != null)
            {
                bool shouldMakeVisible = circle.mLayerBelongingTo == null && circle.ListsBelongingTo.Contains(mCircles) == false
                    && circle.Visible;

                if (!circle.ListsBelongingTo.Contains(mAutomaticallyUpdated))
                {
                    mAutomaticallyUpdated.Add(circle);
                }

                if (circle.Visible)
                {
                    if (circle.mLayerBelongingTo != null)
                    {
                        circle.mLayerBelongingTo.mCircles.Remove(circle);
                    }
                    else
                    {
                        mCircles.Remove(circle);
                    }

                    layer.mCircles.Add(circle);
                }

                // else if not visible, don't add it to a layer

                circle.mLayerBelongingTo = layer;
                if (shouldMakeVisible)
                {
                    circle.Visible = true;
                }
            }
            else
            {
                circle.Visible = true;

                if (circle.ListsBelongingTo.Contains(mCircles) == false)
                {
                    mCircles.Add(circle);
                }
            }
        }

        public static void AddToLayer(Sphere sphere, Layer layer, bool makeAutomaticallyUpdated = true)
        {
            if (makeAutomaticallyUpdated && !sphere.ListsBelongingTo.Contains(mAutomaticallyUpdated))
            {
                mAutomaticallyUpdated.Add(sphere);
            }

            if (layer != null)
            {
                bool shouldMakeVisible = sphere.mLayerBelongingTo == null && sphere.ListsBelongingTo.Contains(mSpheres) == false
                    && sphere.Visible;

                if (!sphere.ListsBelongingTo.Contains(mAutomaticallyUpdated))
                {
                    mAutomaticallyUpdated.Add(sphere);
                }

                if (sphere.Visible)
                {
                    if (sphere.mLayerBelongingTo != null)
                    {
                        sphere.mLayerBelongingTo.mSpheres.Remove(sphere);
                    }
                    else
                    {
                        mSpheres.Remove(sphere);
                    }

                    layer.mSpheres.Add(sphere);
                }

                sphere.mLayerBelongingTo = layer;
                if (shouldMakeVisible)
                {
                    sphere.Visible = true;
                }

            }
            else
            {
                sphere.Visible = true;

                if (sphere.ListsBelongingTo.Contains(mSpheres) == false)
                {
                    mSpheres.Add(sphere);
                }
            }
        }

        public static void AddToLayer(Line line, Layer layer, bool makeAutomaticallyUpdated = true)
        {
            if (makeAutomaticallyUpdated && !line.ListsBelongingTo.Contains(mAutomaticallyUpdated))
            {
                mAutomaticallyUpdated.Add(line);
            }

            if (layer != null)
            {

                bool shouldMakeVisible = line.mLayerBelongingTo == null && line.ListsBelongingTo.Contains(mLines) == false
                    && line.Visible;

                if (line.Visible)
                {
                    if (line.mLayerBelongingTo != null)
                    {
                        line.mLayerBelongingTo.mLines.Remove(line);
                    }
                    else
                    {
                        mLines.Remove(line);
                    }

                    layer.mLines.Add(line);
                }

                line.mLayerBelongingTo = layer;
                if (shouldMakeVisible)
                {
                    line.Visible = true;
                }
            }
            else
            {
                // 12/21/2010
                // COPIED CODE!!
                // This following 
                // section contains
                // copied code from the
                // AddLine method. We do
                // this because AddLine will
                // throw an exception if the added
                // Line is already part of the engine.
                // However, it's common practice to call
                // AddToLayer and expect it to not crash whether
                // the object is part of the engine or not.  So we
                // need to take the functional parts of AddLine and 
                // just wrap them in if checks.

                // If a Shape that is visible is cloned, then the new Shape
                // will also have its Visible property be true; however, it will
                // not belong to the drawn Shape List.  Setting visible to true or false
                // adds/removes a shape from the list list ONLY if the boolean changes
                // in the property.  If a cloned shape already has visible true, then the above
                // statement will not do anything.  This statement verifies that the shape is
                // drawn.
                line.Visible = true;


                if (line.ListsBelongingTo.Contains(mLines) == false)
                {
                    mLines.Add(line);
                }

            }
        }

        public static void AddToLayer(Polygon polygon, Layer layer, bool makeAutomaticallyUpdated = true)
        {
            if (makeAutomaticallyUpdated && !polygon.ListsBelongingTo.Contains(mAutomaticallyUpdated))
            {
                mAutomaticallyUpdated.Add(polygon);
            }

            if (layer != null)
            {
                bool shouldMakeVisible = polygon.mLayerBelongingTo == null && polygon.ListsBelongingTo.Contains(mRectangles) == false
                    && polygon.Visible;

                if (polygon.Visible)
                {
                    if (polygon.mLayerBelongingTo != null)
                    {
                        polygon.mLayerBelongingTo.mPolygons.Remove(polygon);
                    }
                    else
                    {
                        mPolygons.Remove(polygon);
                    }

                    layer.mPolygons.Add(polygon);
                }

                // else if not visible, don't add it to a layer

                polygon.mLayerBelongingTo = layer;

                if (shouldMakeVisible)
                {
                    polygon.Visible = true;
                }
            }
            else
            {
                polygon.Visible = true;

                if (polygon.ListsBelongingTo.Contains(mPolygons) == false)
                {
                    mPolygons.Add(polygon);
                }
            }
        }

        public static void AddToLayer(Capsule2D capsule, Layer layer, bool makeAutomaticallyUpdated = true)
        {
            if (makeAutomaticallyUpdated && !capsule.ListsBelongingTo.Contains(mAutomaticallyUpdated))
            {
                mAutomaticallyUpdated.Add(capsule);
            }

            if (layer != null)
            {
                bool shouldMakeVisible = capsule.mLayerBelongingTo == null && capsule.ListsBelongingTo.Contains(mCircles) == false
                    && capsule.Visible;


                if (capsule.Visible)
                {
                    if (capsule.mLayerBelongingTo != null)
                    {
                        capsule.mLayerBelongingTo.mCapsule2Ds.Remove(capsule);
                    }
                    else
                    {
                        mCapsule2Ds.Remove(capsule);
                    }

                    layer.mCapsule2Ds.Add(capsule);
                }

                // else if not visible, don't add it to a layer

                capsule.mLayerBelongingTo = layer;
                if (shouldMakeVisible)
                {
                    capsule.Visible = true;
                }
            }
            else
            {
                capsule.Visible = true;

                if (capsule.ListsBelongingTo.Contains(mCapsule2Ds) == false)
                {
                    mCapsule2Ds.Add(capsule);
                }
            }
        }

        public static void AddToLayer(ShapeCollection shapeCollection, Layer layer, bool makeAutomaticallyUpdated = true)
        {
            for (int i = 0; i < shapeCollection.AxisAlignedCubes.Count; i++)
            {
                AddToLayer(shapeCollection.AxisAlignedCubes[i], layer, makeAutomaticallyUpdated);
            }

            for (int i = 0; i < shapeCollection.AxisAlignedRectangles.Count; i++)
            {
                AddToLayer(shapeCollection.AxisAlignedRectangles[i], layer, makeAutomaticallyUpdated);
            }

            for (int i = 0; i < shapeCollection.Capsule2Ds.Count; i++)
            {
                AddToLayer(shapeCollection.Capsule2Ds[i], layer, makeAutomaticallyUpdated);
            }

            for (int i = 0; i < shapeCollection.Circles.Count; i++)
            {
                AddToLayer(shapeCollection.Circles[i], layer, makeAutomaticallyUpdated);
            }

            for (int i = 0; i < shapeCollection.Lines.Count; i++)
            {
                AddToLayer(shapeCollection.Lines[i], layer, makeAutomaticallyUpdated);
            }

            for (int i = 0; i < shapeCollection.Polygons.Count; i++)
            {
                AddToLayer(shapeCollection.Polygons[i], layer, makeAutomaticallyUpdated);
            }

            for (int i = 0; i < shapeCollection.Spheres.Count; i++)
            {
                AddToLayer(shapeCollection.Spheres[i], layer, makeAutomaticallyUpdated);
            }
        }

        #endregion

        public static void ApplyBounce(PositionedObject object1, PositionedObject object2, float object1Mass, float object2Mass, float elasticity, ref Vector2 collisionReposition)
        {
            Vector2 normalized = collisionReposition;
            //NM - 15/04/2012
            //Occasionally collisionReposition comes in as a zero length vector,
            //we need to make sure it has magnitude before normalising, otherwise we get a NaN vector.
            if (normalized.Length() > 0.0f)
            {
                normalized.Normalize();
            }

            if(elasticity > 0)
            {
                if (object1.Drag == 0)
                {
                    #region Adjust for object1's Y Acceleration

                    // Quadratic formula time!
                    // For a refresher, check:
                    // http://www.purplemath.com/modules/quadform.htm
                    // In short, where:
                    // ax^2 + bx + c = 0
                    // then:
                    // x = (-b (+ or -) sqrt(b^2 -4ac) ) / 2a
                    // In our case:
                    // a = acceleration/2
                    // b = velocity
                    // c = reposition
                    // x = amount of time spent in the shape (estimated based off of the reposition, not actual time because we don't know the actual time)
                    // 
                    if (object1.Acceleration.Y != 0)
                    {
                        // C is inverted because we bring
                        // it from the right side of the equation
                        // over to the left to make the equation equal
                        // 0.  But we also want to invert the velocity/acceleration
                        // values because we're trying to "rewind time" to see where
                        // the initial pass into the shape occurred.  So we'll keep everything
                        // positive and the math should work out
                        double aValue = -object1.Acceleration.Y / 2.0;
                        double bValue = object1.Velocity.Y;
                        double cValue = collisionReposition.Y;
                        double discriminantSquareRoot = System.Math.Sqrt(bValue * bValue - 4.0 * aValue * cValue);
                        double twoTimesA = 2.0 * aValue;

                        double solutionToUse = GetSolution(bValue, discriminantSquareRoot, twoTimesA);

                        // Now we can adjust the velocity according to the acceleration value
                        object1.Velocity.Y -= (float)(object1.Acceleration.Y * solutionToUse * System.Math.Abs(normalized.Y));
                    }

                    #endregion

                    #region Adjust for object1's XAcceleration

                    if (object1.Acceleration.X != 0)
                    {
                        double aValue = -object1.Acceleration.X / 2.0;
                        double bValue = object1.Velocity.X;
                        double cValue = collisionReposition.X;
                        double discriminantSquareRoot = System.Math.Sqrt(bValue * bValue - 4.0 * aValue * cValue);
                        double twoTimesA = 2.0 * aValue;

                        double solutionToUse = GetSolution(bValue, discriminantSquareRoot, twoTimesA);

                        // Now we can adjust the velocity according to the acceleration value
                        object1.Velocity.X -= (float)(object1.Acceleration.X * solutionToUse * System.Math.Abs(normalized.X));
                    }

                    #endregion
                }

                if (object2.Drag == 0)
                {
                    #region Adjust for object2's YAcceleration

                    // See comments in object1's YAcceleration section
                    if (object2.Acceleration.Y != 0)
                    {
                        double aValue = -object2.Acceleration.Y / 2.0;
                        double bValue = object2.Velocity.Y;
                        double cValue = -collisionReposition.Y;
                        double discriminantSquareRoot = System.Math.Sqrt(bValue * bValue - 4.0 * aValue * cValue);
                        double twoTimesA = 2.0 * aValue;

                        double solutionToUse = GetSolution(bValue, discriminantSquareRoot, twoTimesA);

                        // Now we can adjust the velocity according to the acceleration value
                        object2.Velocity.Y -= (float)(object2.Acceleration.Y * solutionToUse * System.Math.Abs(normalized.Y));
                    }

                    #endregion

                    #region Adjust for object2's XAcceleration

                    // See comments in object1's YAcceleration section
                    if (object2.Acceleration.X != 0)
                    {
                        double aValue = -object2.Acceleration.X / 2.0;
                        double bValue = object2.Velocity.X;
                        double cValue = -collisionReposition.X;
                        double discriminantSquareRoot = System.Math.Sqrt(bValue * bValue - 4.0 * aValue * cValue);
                        double twoTimesA = 2.0 * aValue;
                        double solutionToUse = GetSolution(bValue, discriminantSquareRoot, twoTimesA);

                        // Now we can adjust the velocity according to the acceleration value
                        object2.Velocity.X -= (float)(object2.Acceleration.X * solutionToUse * System.Math.Abs(normalized.X));
                    }

                    #endregion
                }
            }

            Vector2 vectorAsVelocity = new Vector2(
                object1.Velocity.X - object2.Velocity.X,
                object1.Velocity.Y - object2.Velocity.Y);

            collisionReposition = normalized;

            float projected = Vector2.Dot(vectorAsVelocity, collisionReposition);

            if (projected < 0)
            {
                Vector2 velocityComponentPerpendicularToTangent =
                    collisionReposition * projected;

                object2.Velocity.X += (1 + elasticity) * object1Mass / (object1Mass + object2Mass) * velocityComponentPerpendicularToTangent.X;
                object2.Velocity.Y += (1 + elasticity) * object1Mass / (object1Mass + object2Mass) * velocityComponentPerpendicularToTangent.Y;

                object1.Velocity.X -= (1 + elasticity) * object2Mass / (object1Mass + object2Mass) * velocityComponentPerpendicularToTangent.X;
                object1.Velocity.Y -= (1 + elasticity) * object2Mass / (object1Mass + object2Mass) * velocityComponentPerpendicularToTangent.Y;

            }
        }

        private static double GetSolution(double bValue, double discriminantSquareRoot, double twoTimesA)
        {
            double solution1 = (float)((-bValue - discriminantSquareRoot) / (twoTimesA));


            double solution2 = (float)((-bValue + discriminantSquareRoot) / (twoTimesA));

            double solutionToUse = 0;

            if (solution1 > 0 && solution2 < 0)
            {
                solutionToUse = solution1;
            }
            else if (solution2 > 0 && solution1 < 0)
            {
                solutionToUse = solution2;
            }
            else if (solution1 > 0 && solution2 > 0)
            {
                if (solution1 < solution2)
                {
                    solutionToUse = solution1;
                }
                else
                {
                    solutionToUse = solution2;
                }
            }

            // There was a bug where the solution was something like 4
            // on a game that ran 60 fps.  The solution should always be between
            // 0 and second differene.  This happened when the rectangle was falling.
            // Not sure if I want to investigate this deeper, but making sure the value
            // is no bigger than SecondDifference throws away bad values and fixes that issue.
            if (solutionToUse > TimeManager.SecondDifference)
            {
                solutionToUse = 0;
            }

            return solutionToUse;
        }

        #region BringToFront Methods

        #region XML Docs
        /// <summary>
        /// Brings the passed in Shape to the front so it's drawn on top.
        /// </summary>
        #endregion
        static public void BringToFront(AxisAlignedRectangle shape)
        {
            PositionedObjectList<AxisAlignedRectangle> list;

            if (shape.mLayerBelongingTo != null)
            {
                list = shape.mLayerBelongingTo.mRectangles;
            }
            else
            {
                list = mRectangles;
            }

            int shapeIndex = list.IndexOf(shape);
            if (shapeIndex != -1)
            {
                list.RemoveAtOneWay(shapeIndex);
                list.AddOneWay(shape);
            }
        }

        #region XML Docs
        /// <summary>
        /// Brings the passed in Shape to the front so it's drawn on top.
        /// </summary>
        #endregion
        static public void BringToFront(Capsule2D shape)
        {
            PositionedObjectList<Capsule2D> list;

            if (shape.mLayerBelongingTo != null)
            {
                list = shape.mLayerBelongingTo.mCapsule2Ds;
            }
            else
            {
                list = mCapsule2Ds;
            }

            int shapeIndex = list.IndexOf(shape);
            if (shapeIndex != -1)
            {
                list.RemoveAtOneWay(shapeIndex);
                list.AddOneWay(shape);
            }
        }

        #region XML Docs
        /// <summary>
        /// Brings the passed in Shape to the front so it's drawn on top.
        /// </summary>
        #endregion
        static public void BringToFront(Circle shape)
        {
            PositionedObjectList<Circle> list;

            if (shape.mLayerBelongingTo != null)
            {
                list = shape.mLayerBelongingTo.mCircles;
            }
            else
            {
                list = mCircles;
            }

            int shapeIndex = list.IndexOf(shape);
            if (shapeIndex != -1)
            {
                list.RemoveAtOneWay(shapeIndex);
                list.AddOneWay(shape);
            }
        }

        #region XML Docs
        /// <summary>
        /// Brings the passed in Shape to the front so it's drawn on top.
        /// </summary>
        #endregion
        static public void BringToFront(Sphere shape)
        {
            PositionedObjectList<Sphere> list;

            if (shape.mLayerBelongingTo != null)
            {
                list = shape.mLayerBelongingTo.mSpheres;
            }
            else
            {
                list = mSpheres;
            }

            int shapeIndex = list.IndexOf(shape);
            if (shapeIndex != -1)
            {
                list.RemoveAtOneWay(shapeIndex);
                list.AddOneWay(shape);
            }
        }

        #region XML Docs
        /// <summary>
        /// Brings the passed in Shape to the front so it's drawn on top.
        /// </summary>
        #endregion
        static public void BringToFront(AxisAlignedCube shape)
        {
            PositionedObjectList<AxisAlignedCube> list;

            if (shape.mLayerBelongingTo != null)
            {
                list = shape.mLayerBelongingTo.mCubes;
            }
            else
            {
                list = mCubes;
            }

            int shapeIndex = list.IndexOf(shape);
            if (shapeIndex != -1)
            {
                list.RemoveAtOneWay(shapeIndex);
                list.AddOneWay(shape);
            }
        }

        #region XML Docs
        /// <summary>
        /// Brings the passed in Shape to the front so it's drawn on top.
        /// </summary>
        #endregion
        static public void BringToFront(Polygon shape)
        {
            PositionedObjectList<Polygon> list;
#if !SILVERLIGHT
            if (shape.mLayerBelongingTo != null)
            {
                list = shape.mLayerBelongingTo.mPolygons;
            }
            else
            {
                list = mPolygons;
            }
#else
            list = mPolygons;
#endif
            int shapeIndex = list.IndexOf(shape);
            if (shapeIndex != -1)
            {
                list.RemoveAtOneWay(shapeIndex);
                list.AddOneWay(shape);
            }
        }

        #region XML Docs
        /// <summary>
        /// Brings the passed in Shape to the front so it's drawn on top.
        /// </summary>
        #endregion
        static public void BringToFront(Line shape)
        {
            PositionedObjectList<Line> list;

            if (shape.mLayerBelongingTo != null)
            {
                list = shape.mLayerBelongingTo.mLines;
            }
            else
            {
                list = mLines;
            }

            int shapeIndex = list.IndexOf(shape);
            if (shapeIndex != -1)
            {
                list.RemoveAtOneWay(shapeIndex);
                list.AddOneWay(shape);
            }
        }

        #endregion

        [Obsolete("This method will eventually be removed. Why have this and not all other shape types?")]
        public static void MakeAllLinesInvisible()
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            for (int i = 0; i < mLines.Count; i++)
            {
                Line line = mLines[i];

                line.mVisible = false;

                if (line.mLayerBelongingTo != null)
                {
                    line.mLayerBelongingTo.mLines.Remove(line);
                    line.mLayerBelongingTo = null;
                }
            }
            mLines.Clear();
        }

        #region Remove Methods

        #region XML Docs
        /// <summary>
        /// Removes the argument AxisAlignedRectangle from the ShapeManager and any 2-way PositionedObjectLists it belongs to.
        /// </summary>
        /// <param name="axisAlignedRectangleToRemove">The AxisAlignedRectangle to remove.  Cannot be null.</param>
        #endregion
        static public void Remove(AxisAlignedRectangle axisAlignedRectangleToRemove)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            axisAlignedRectangleToRemove.RemoveSelfFromListsBelongingTo();
        }


        /// <summary>
        /// Removes the argument AxisAlignedCube from the ShapeManager and any 2-way PositionedObjectLists it belongs to.
        /// </summary>
        /// <param name="axisAlignedCubeToRemove">The AxisAlignedCube to remove.  Cannot be null.</param>
        static public void Remove(AxisAlignedCube axisAlignedCubeToRemove)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            axisAlignedCubeToRemove.RemoveSelfFromListsBelongingTo();
        }

        #region XML Docs
        /// <summary>
        /// Removes the argument Capsule2D from the ShapeManager and any 2-way
        /// PositionedObjectLists it belongs to.
        /// </summary>
        /// <param name="capsule2DToRemove">The Capsule2D to remove.  Should not be null.</param>
        #endregion
        static public void Remove(Capsule2D capsule2DToRemove)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            capsule2DToRemove.RemoveSelfFromListsBelongingTo();
        }

        #region XML Docs
        /// <summary>
        /// Removes the argument Circle from the ShapeManager and any 2-way PositionedObjectLists it belongs to.
        /// </summary>
        /// <param name="circleToRemove">The Circle to remove.  Should not be null.</param>
        #endregion
        static public void Remove(Circle circleToRemove)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            circleToRemove.RemoveSelfFromListsBelongingTo();
        }

        #region XML Docs
        /// <summary>
        /// Removes the argument Sphere from the ShapeManager and any 2-way PositionedObjectLists it belongs to.
        /// </summary>
        /// <param name="sphereToRemove">The Sphere to remove.  Should not be null.</param>
        #endregion
        static public void Remove(Sphere sphereToRemove)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            sphereToRemove.RemoveSelfFromListsBelongingTo();
        }

        #region XML Docs
        /// <summary>
        /// Removes the argument Polygon from the ShapeManager and any 2-way PositionedObjectLists it belongs to.
        /// </summary>
        /// <param name="polygonToRemove">The Polygon to remove.  Should not be null.</param>
        #endregion
        static public void Remove(Polygon polygonToRemove)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            polygonToRemove.RemoveSelfFromListsBelongingTo();
        }

        /// <summary>
        /// Removes the argument Line from the ShapeManager and any 2-way PositionedObjectLists it belongs to.
        /// </summary>
        /// <param name="lineToRemove">The Line to remove.  Should not be null.</param>
        static public void Remove(Line lineToRemove)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            lineToRemove.RemoveSelfFromListsBelongingTo();
        }

        public static void RemoveOneWay(Circle circleToRemove)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            mAutomaticallyUpdated.Remove(circleToRemove);
            circleToRemove.Visible = false;
        }

        /// <summary>
        /// Removes the argument Polygon from the ShapeManager.
        /// </summary>
        /// <param name="polygonToRemove">The Polygon to remove.  Should not be null.</param>
        static public void RemoveOneWay(Polygon polygonToRemove)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            mAutomaticallyUpdated.Remove(polygonToRemove);
            polygonToRemove.Visible = false;
        }

        /// <summary>
        /// Removes the argument AxisAlignedRectangle from the ShapeManager.
        /// </summary>
        /// <param name="axisAlignedRectangleToRemove">The AxisAlignedRectangle to remove.  Cannot be null.</param>
        static public void RemoveOneWay(AxisAlignedRectangle axisAlignedRectangleToRemove)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            mAutomaticallyUpdated.Remove(axisAlignedRectangleToRemove);
            axisAlignedRectangleToRemove.Visible = false;
        }

        public static void RemoveOneWay(Line lineToRemove)
        {
#if DEBUG
            ThrowExceptionIfNotPrimaryThread();
#endif
            mAutomaticallyUpdated.Remove(lineToRemove);
            lineToRemove.Visible = false;
        }


        #region XML Docs
        /// <summary>
        /// Removes all Polygons held in the argument listToRemove from the Shapemanager and any 2-way PositionedObjectLists they belong to.
        /// </summary>
        /// <typeparam name="T">The type of object which must be a Polygon.</typeparam>
        /// <param name="listToRemove">The list of objects to remove.</param>
        #endregion
        static public void Remove<T>(IList<T> listToRemove) where T : Polygon
        {
            for (int i = listToRemove.Count - 1; i > -1; i--)
            {
                Remove(listToRemove[i]);
            }
        }

        #endregion

        #region XML Docs
        /// <summary>
        /// Returns information about the ShapeManager.
        /// </summary>
        /// <returns>A string containing information about the ShapeManager.</returns>
        #endregion
        static public new string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("Number of Polygons: ").Append(mPolygons.Count);

            return stringBuilder.ToString();
        }

        #endregion

        #region Internal Methods

        static internal void FillPolygonVertexArrays()
        {
            FillPolygonVertexArrays(mPolygons);
        }

        static internal void FillPolygonVertexArrays(IList<Polygon> polygons)
        {
            for (int i = 0; i < polygons.Count; i++)
            {
                Polygon polygon = polygons[i];

                if (polygon.ListsBelongingTo.Contains(mAutomaticallyUpdated) == false)
                {
                    polygon.FillVertexArray();
                }
            }
        }

        static internal int GetTotalPolygonVertexCount(IList<Polygon> polygons)
        {
            int count = 0;

            for (int i = 0; i < polygons.Count; i++)
            {
                var polygon = polygons[i];


                if (polygon.Points != null && polygon.Points.Count > 1)
                {
                    count += polygon.Points.Count;
                }

            }

            return count;

        }

        static internal void NotifyOfVisibilityChange(Sphere sphere)
        {
            if (!SuppressAddingOnVisibilityTrue && sphere.Visible && sphere.ListsBelongingTo.Contains(mSpheres) == false)
            {
#if DEBUG
                ThrowExceptionIfNotPrimaryThread();
#endif
                mSpheres.Add(sphere);
            }
            else if (sphere.Visible == false && sphere.ListsBelongingTo.Contains(mSpheres))
            {
#if DEBUG
                ThrowExceptionIfNotPrimaryThread();
#endif
                mSpheres.Remove(sphere);
            }
        }

        static internal void NotifyOfVisibilityChange(AxisAlignedCube axisAlignedCube)
        {

            if (axisAlignedCube.mLayerBelongingTo == null)
            {
                if (!SuppressAddingOnVisibilityTrue && axisAlignedCube.Visible && axisAlignedCube.ListsBelongingTo.Contains(mCubes) == false)
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    mCubes.Add(axisAlignedCube);
                }
                else if (axisAlignedCube.Visible == false && axisAlignedCube.ListsBelongingTo.Contains(mCubes))
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    mCubes.Remove(axisAlignedCube);
                }
            }
            else
            {
                if (!SuppressAddingOnVisibilityTrue && axisAlignedCube.Visible && axisAlignedCube.ListsBelongingTo.Contains(axisAlignedCube.mLayerBelongingTo.mCubes) == false)
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    axisAlignedCube.mLayerBelongingTo.mCubes.Add(axisAlignedCube);
                }
                else if (axisAlignedCube.Visible == false && axisAlignedCube.ListsBelongingTo.Contains(axisAlignedCube.mLayerBelongingTo.mCubes))
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    axisAlignedCube.mLayerBelongingTo.mCubes.Remove(axisAlignedCube);
                }
            }
        }

        static internal void NotifyOfVisibilityChange(Capsule2D capsule2D)
        {

            if (!SuppressAddingOnVisibilityTrue && capsule2D.Visible && capsule2D.ListsBelongingTo.Contains(mCapsule2Ds) == false)
            {
#if DEBUG
                ThrowExceptionIfNotPrimaryThread();
#endif
                mCapsule2Ds.Add(capsule2D);
            }
            else if (capsule2D.Visible == false && capsule2D.ListsBelongingTo.Contains(mCapsule2Ds))
            {
#if DEBUG
                ThrowExceptionIfNotPrimaryThread();
#endif
                mCapsule2Ds.Remove(capsule2D);
            }
        }

        static internal void NotifyOfVisibilityChange(Circle circle)
        {

            if (circle.mLayerBelongingTo == null)
            {
                if (!SuppressAddingOnVisibilityTrue && circle.Visible && circle.ListsBelongingTo.Contains(mCircles) == false)
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    mCircles.Add(circle);
                }
                else if (circle.Visible == false && circle.ListsBelongingTo.Contains(mCircles))
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    mCircles.Remove(circle);
                }
            }
            else
            {
                if (!SuppressAddingOnVisibilityTrue && circle.Visible && circle.ListsBelongingTo.Contains(circle.mLayerBelongingTo.mCircles) == false)
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    circle.mLayerBelongingTo.mCircles.Add(circle);
                }
                else if (circle.Visible == false && circle.ListsBelongingTo.Contains(circle.mLayerBelongingTo.mCircles))
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    circle.mLayerBelongingTo.mCircles.Remove(circle);
                }
            }
        }

        static internal void NotifyOfVisibilityChange(AxisAlignedRectangle rectangle)
        {

            if (rectangle.mLayerBelongingTo == null)
            {
                if (!SuppressAddingOnVisibilityTrue && rectangle.Visible && rectangle.ListsBelongingTo.Contains(mRectangles) == false)
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    mRectangles.Add(rectangle);
                }
                else if (rectangle.Visible == false && rectangle.ListsBelongingTo.Contains(mRectangles))
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    mRectangles.Remove(rectangle);
                }
            }
            else
            {
                if (!SuppressAddingOnVisibilityTrue && rectangle.Visible && rectangle.ListsBelongingTo.Contains(rectangle.mLayerBelongingTo.mRectangles) == false)
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    rectangle.mLayerBelongingTo.mRectangles.Add(rectangle);
                }
                else if (rectangle.Visible == false && rectangle.ListsBelongingTo.Contains(rectangle.mLayerBelongingTo.mRectangles))
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    rectangle.mLayerBelongingTo.mRectangles.Remove(rectangle);
                }
            }
        }

        static internal void NotifyOfVisibilityChange(Polygon polygon)
        {

#if !SILVERLIGHT
            if (polygon.mLayerBelongingTo != null)
            {
                if (!SuppressAddingOnVisibilityTrue && polygon.Visible && polygon.ListsBelongingTo.Contains(polygon.mLayerBelongingTo.mPolygons) == false)
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    polygon.mLayerBelongingTo.mPolygons.Add(polygon);
                }
                else if (polygon.Visible == false && polygon.ListsBelongingTo.Contains(polygon.mLayerBelongingTo.mPolygons))
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    polygon.mLayerBelongingTo.mPolygons.Remove(polygon);
                }
            }
            else
#endif
            {
                if (!SuppressAddingOnVisibilityTrue && polygon.Visible && polygon.ListsBelongingTo.Contains(mPolygons) == false)
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    mPolygons.Add(polygon);
                }
                else if (polygon.Visible == false && polygon.ListsBelongingTo.Contains(mPolygons))
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    mPolygons.Remove(polygon);
                }
            }
        }

        static internal void NotifyOfVisibilityChange(Line line)
        {

            if (line.mLayerBelongingTo == null)
            {
                if (!SuppressAddingOnVisibilityTrue && line.Visible && line.ListsBelongingTo.Contains(mLines) == false)
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    mLines.Add(line);
                }
                else if (line.Visible == false && line.ListsBelongingTo.Contains(mLines))
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    mLines.Remove(line);
                }
            }
            else
            {
                if (!SuppressAddingOnVisibilityTrue && line.Visible && line.ListsBelongingTo.Contains(line.mLayerBelongingTo.mLines) == false)
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    line.mLayerBelongingTo.mLines.Add(line);
                }
                else if (line.Visible == false && line.ListsBelongingTo.Contains(line.mLayerBelongingTo.mLines))
                {
#if DEBUG
                    ThrowExceptionIfNotPrimaryThread();
#endif
                    line.mLayerBelongingTo.mLines.Remove(line);
                }
            }
        }

        static internal void Pause(InstructionList instructions)
        {
            for (int i = 0; i < mAutomaticallyUpdated.Count; i++)
            {
                PositionedObject shape = mAutomaticallyUpdated[i];

                if (!InstructionManager.PositionedObjectsIgnoringPausing.Contains(shape))
                {
                    shape.Pause(instructions);
                }
            }
        }

        public static void Update()
        {
            ShapeManager.ExecuteInstructions<PositionedObject>(mAutomaticallyUpdated, TimeManager.CurrentTime);

            Polygon.NumberOfTimesRadiusTestPassed = 0;
            Polygon.NumberOfTimesCollideAgainstPolygonCalled = 0;

            float secondDifference = TimeManager.SecondDifference;
            float secondDifferenceSquaredDividedByTwo = TimeManager.SecondDifferenceSquaredDividedByTwo;
            float lastSecondDifference = TimeManager.LastSecondDifference;

            for (int i = 0; i < mAutomaticallyUpdated.Count; i++)
            {
                PositionedObject po = mAutomaticallyUpdated[i];

                po.TimedActivity(secondDifference,
                    secondDifferenceSquaredDividedByTwo,
                    lastSecondDifference);
            }
        }

        static internal void UpdateDependencies()
        {
            UpdateDependencies(mAutomaticallyUpdated, TimeManager.CurrentTime);

        }

        #endregion

        #region Private Methods

        private static void ExecuteInstructions<T>(PositionedObjectList<T> list, double currentTime) where T : PositionedObject
        {
            for (int i = 0; i < list.Count; i++)
            {// loop through the sprites
                if (i < list.Count)
                {
                    list[i].ExecuteInstructions(currentTime);
                }
            }
        }

        //private static void Flush()
        //{
        //    mAutomaticallyUpdatedBuffer.Flush();
        //
        //    mRectangleBuffer.Flush();
        //    mCircleBuffer.Flush();
        //    mPolygonBuffer.Flush();
        //    mLineBuffer.Flush();
        //    mSphereBuffer.Flush();
        //    mCubeBuffer.Flush();
        //    mCapsule2DBuffer.Flush();
        //}

        private static void UpdateDependencies(AttachableList<PositionedObject> list, double currentTime)
        {
            //Flush();

            for (int i = 0; i < list.Count; i++)
            {// loop through the sprites
                list[i].UpdateDependencies(currentTime);
            }
        }

        #endregion

        #endregion
    }
}