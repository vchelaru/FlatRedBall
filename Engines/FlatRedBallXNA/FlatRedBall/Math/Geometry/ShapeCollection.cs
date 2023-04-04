using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics;
using Microsoft.Xna.Framework;
using FlatRedBall.Input;

using Vector3 = Microsoft.Xna.Framework.Vector3;
using FlatRedBall.Utilities;
using System.Drawing;

namespace FlatRedBall.Math.Geometry
{
    public class ShapeCollection : ICollidable, IEquatable<ShapeCollection> 
        ,IMouseOver, INameable
    {
        #region Fields

        private delegate bool CollisionDelegate();
        private delegate void SetLastMovePositionDelegate(Vector2 lastMovePosition);

        internal PositionedObjectList<AxisAlignedRectangle> mAxisAlignedRectangles = new PositionedObjectList<AxisAlignedRectangle>();
		internal PositionedObjectList<Circle> mCircles = new PositionedObjectList<Circle>();
		internal PositionedObjectList<Polygon> mPolygons = new PositionedObjectList<Polygon>();
		internal PositionedObjectList<Line> mLines = new PositionedObjectList<Line>();
		internal PositionedObjectList<Sphere> mSpheres = new PositionedObjectList<Sphere>();
		internal PositionedObjectList<AxisAlignedCube> mAxisAlignedCubes = new PositionedObjectList<AxisAlignedCube>();
		internal PositionedObjectList<Capsule2D> mCapsule2Ds = new PositionedObjectList<Capsule2D>();
        // if you add here, add below too!

		internal protected float mMaxAxisAlignedRectanglesRadiusX;
		internal protected float mMaxAxisAlignedRectanglesRadiusY;
		internal protected float mMaxCirclesRadius;
		internal protected float mMaxPolygonsRadius;
		internal protected float mMaxLinesRadius;
		internal protected float mMaxSpheresRadius;
		internal protected float mMaxAxisAlignedCubesRadius;
		internal protected float mMaxCapsule2DsRadius;

		internal bool mSuppressLastCollisionClear;

		internal List<AxisAlignedRectangle> mLastCollisionAxisAlignedRectangles = new List<AxisAlignedRectangle>();
		internal List<Circle> mLastCollisionCircles = new List<Circle>();
		internal List<Polygon> mLastCollisionPolygons = new List<Polygon>();
		internal List<Line> mLastCollisionLines = new List<Line>();
		internal List<Sphere> mLastCollisionSpheres = new List<Sphere>();
		internal List<AxisAlignedCube> mLastCollisionAxisAlignedCubes = new List<AxisAlignedCube>();
		internal List<Capsule2D> mLastCollisionCapsule2Ds = new List<Capsule2D>();

        #endregion

        #region Properties

        ShapeCollection ICollidable.Collision { get { return this; } }

        public PositionedObjectList<AxisAlignedRectangle> AxisAlignedRectangles
        {
            get { return mAxisAlignedRectangles; }
        }

        public PositionedObjectList<AxisAlignedCube> AxisAlignedCubes
        {
            get { return mAxisAlignedCubes; }
        }

        public PositionedObjectList<Capsule2D> Capsule2Ds
        {
            get { return mCapsule2Ds; }
        }

        public PositionedObjectList<Circle> Circles
        {
            get { return mCircles; }
        }

        public PositionedObjectList<Polygon> Polygons
        {
            get { return mPolygons; }
        }

        public List<AxisAlignedRectangle> LastCollisionAxisAlignedRectangles
        {
            get
            {
                return mLastCollisionAxisAlignedRectangles;
            }
        }

        public List<Circle> LastCollisionCircles
        {
            get
            {
                return mLastCollisionCircles;

            }
        }

        public List<Polygon> LastCollisionPolygons
        {
            get
            {
                return mLastCollisionPolygons;

            }
        }

        public List<Line> LastCollisionLines
        {
            get
            {
                return mLastCollisionLines;

            }
        }

        public List<Sphere> LastCollisionSpheres
        {
            get
            {
                return mLastCollisionSpheres;

            }
        }

        public List<AxisAlignedCube> LastCollisionAxisAlignedCubes
        {
            get
            {
                return mLastCollisionAxisAlignedCubes;

            }
        }

        public List<Capsule2D> LastCollisionCapsule2Ds
        {
            get
            {
                return mLastCollisionCapsule2Ds;

            }
        }

        public PositionedObjectList<Line> Lines
        {
            get { return mLines; }
        }
        
        public PositionedObjectList<Sphere> Spheres
        {
            get { return mSpheres; }
        }

        public bool IsEmpty
        {
            get
            {
                return mAxisAlignedCubes.Count == 0 &&
                    mAxisAlignedRectangles.Count == 0 &&
                    mCapsule2Ds.Count == 0 &&
                    mCircles.Count == 0 &&
                    mPolygons.Count == 0 &&
                    mLines.Count == 0 &&
                    mSpheres.Count == 0;
            }
        }

        public string Name
        {
            get;
            set;
        }

        public bool Visible
        {
            set
            {
                foreach (AxisAlignedRectangle aar in mAxisAlignedRectangles)
                {
                    aar.Visible = value;
                }

                foreach (AxisAlignedCube aac in mAxisAlignedCubes)
                {
                    aac.Visible = value;
                }

                foreach (Circle circle in mCircles)
                {
                    circle.Visible = value;
                }

                foreach (Polygon polygon in mPolygons)
                {
                    polygon.Visible = value;
                }

                foreach (Line line in mLines)
                {
                    line.Visible = value;
                }

                foreach (Sphere sphere in mSpheres)
                {
                    sphere.Visible = value;
                }

                foreach (Capsule2D capsule2D in mCapsule2Ds)
                {
                    capsule2D.Visible = value;
                }
            }
        }

        public float MaxAxisAlignedRectanglesScale
        {
            get { return mMaxAxisAlignedRectanglesRadiusX; }
            set 
            { 
                mMaxAxisAlignedRectanglesRadiusX = value;
                mMaxAxisAlignedRectanglesRadiusY = value; 
            }
        }

        public float MaxAxisAlignedRectanglesRadiusX
        {
            get => mMaxAxisAlignedRectanglesRadiusX;
            set => mMaxAxisAlignedRectanglesRadiusX = value;
        }

        public float MaxAxisAlignedRectanglesRadiusY
        {
            get => mMaxAxisAlignedRectanglesRadiusY;
            set => mMaxAxisAlignedRectanglesRadiusY = value;
        }

        public float MaxPolygonRadius
        {
            get => mMaxPolygonsRadius;
            set => mMaxPolygonsRadius = value;
        }

        public HashSet<string> ItemsCollidedAgainst { get; private set; } = new HashSet<string>();
        public HashSet<string> LastFrameItemsCollidedAgainst { get; private set; } = new HashSet<string>();

        public HashSet<object> ObjectsCollidedAgainst { get; private set; } = new HashSet<object>();
        public HashSet<object> LastFrameObjectsCollidedAgainst { get; private set; } = new HashSet<object>();

        #endregion

        #region Methods

        #region Public Methods

        public void AddToThis(ShapeCollection shapeCollection)
        {
            mAxisAlignedRectangles.AddRange(shapeCollection.AxisAlignedRectangles);
            mCircles.AddRange(shapeCollection.Circles);
            mPolygons.AddRange(shapeCollection.Polygons);
            mLines.AddRange(shapeCollection.Lines);
            mSpheres.AddRange(shapeCollection.Spheres);
            mAxisAlignedCubes.AddRange(shapeCollection.mAxisAlignedCubes);
            mCapsule2Ds.AddRange(shapeCollection.mCapsule2Ds);
        }

        public void Add(Circle circle)
        {
            mCircles.Add(circle);
        }
        public bool Contains(Circle circle) => mCircles.Contains(circle);

        public void Add(AxisAlignedRectangle rectangle)
        {
            mAxisAlignedRectangles.Add(rectangle);
        }

        public bool Contains(AxisAlignedRectangle rectangle) => mAxisAlignedRectangles.Contains(rectangle);

        public void Add(Polygon polygon)
        {
            mPolygons.Add(polygon);
        }

        public bool Contains(Polygon polygon) => mPolygons.Contains(polygon);

        public void Add(AxisAlignedCube cube) => mAxisAlignedCubes.Add(cube);
        public bool Contains(AxisAlignedCube cube) => mAxisAlignedCubes.Contains(cube);

        public void Add(Sphere sphere) => mSpheres.Add(sphere);
        public bool Contains(Sphere sphere) => mSpheres.Contains(sphere);


        public void AddToManagers()
        {

            AddToManagers(null);
        }

        public void AddToManagers(Layer layer, bool makeAutomaticallyUpdated = true)
        {
            for (int i = 0; i < mAxisAlignedRectangles.Count; i++)
            {
                AxisAlignedRectangle rectangle = mAxisAlignedRectangles[i];
                ShapeManager.AddToLayer(rectangle, layer, makeAutomaticallyUpdated);
            }

            for (int i = 0; i < mAxisAlignedCubes.Count; i++)
            {
                AxisAlignedCube cube = mAxisAlignedCubes[i];
                ShapeManager.AddToLayer(cube, layer, makeAutomaticallyUpdated);
            }

            for (int i = 0; i < mCapsule2Ds.Count; i++)
            {
                Capsule2D capsule = mCapsule2Ds[i];
                ShapeManager.AddToLayer(capsule, layer, makeAutomaticallyUpdated);
            }

            for (int i = 0; i < mCircles.Count; i++)
            {
                Circle circle = mCircles[i];
                ShapeManager.AddToLayer(circle, layer, makeAutomaticallyUpdated);
            }

            for (int i = 0; i < mSpheres.Count; i++)
            {
                Sphere sphere = mSpheres[i];
                ShapeManager.AddToLayer(sphere, layer, makeAutomaticallyUpdated);
            }

            for (int i = 0; i < mPolygons.Count; i++)
            {
                Polygon polygon = mPolygons[i];
                ShapeManager.AddToLayer(polygon, layer, makeAutomaticallyUpdated);
            }

            for (int i = 0; i < mLines.Count; i++)
            {
                Line line = mLines[i];
                ShapeManager.AddToLayer(line, layer, makeAutomaticallyUpdated);
            }
        }

        public void AttachAllDetachedTo(PositionedObject newParent, bool changeRelative)
        {
            mAxisAlignedRectangles.AttachAllDetachedTo(newParent, changeRelative);
            mCircles.AttachAllDetachedTo(newParent, changeRelative);
            mPolygons.AttachAllDetachedTo(newParent, changeRelative);
            mLines.AttachAllDetachedTo(newParent, changeRelative);
            mSpheres.AttachAllDetachedTo(newParent, changeRelative);
            mAxisAlignedCubes.AttachAllDetachedTo(newParent, changeRelative);
            mCapsule2Ds.AttachAllDetachedTo(newParent, changeRelative);
        }


		public void AttachTo(PositionedObject newParent, bool changeRelative)
		{
			mAxisAlignedRectangles.AttachTo(newParent, changeRelative);
			mCircles.AttachTo(newParent, changeRelative);
			mPolygons.AttachTo(newParent, changeRelative);
			mLines.AttachTo(newParent, changeRelative);
			mSpheres.AttachTo(newParent, changeRelative);
			mAxisAlignedCubes.AttachTo(newParent, changeRelative);
			mCapsule2Ds.AttachTo(newParent, changeRelative);
		}

        public void CalculateAllMaxRadii()
        {

            mMaxAxisAlignedRectanglesRadiusX = 0;
            mMaxAxisAlignedRectanglesRadiusY = 0;
            mMaxCirclesRadius = 0;
            mMaxPolygonsRadius = 0;
            mMaxLinesRadius = 0;
            mMaxSpheresRadius = 0;
            mMaxAxisAlignedCubesRadius = 0;
            mMaxCapsule2DsRadius = 0;

            for(int i = 0; i < mAxisAlignedRectangles.Count; i++)
            {
                mMaxAxisAlignedRectanglesRadiusX = System.Math.Max(mMaxAxisAlignedRectanglesRadiusX,
                    mAxisAlignedRectangles[i].BoundingRadius);
                mMaxAxisAlignedRectanglesRadiusY = System.Math.Max(mMaxAxisAlignedRectanglesRadiusY,
                    mAxisAlignedRectangles[i].BoundingRadius);
            }

            for(int i = 0; i < mCircles.Count; i++)
            {
                mMaxCirclesRadius = System.Math.Max(mMaxCirclesRadius,
                    mCircles[i].Radius);
            }

            for(int i = 0; i < mPolygons.Count; i++)
            {
                mMaxPolygonsRadius = System.Math.Max(mMaxPolygonsRadius,
                    mPolygons[i].BoundingRadius);
            }

            for(int i = 0; i < mLines.Count; i++)
            {
                // mega inefficient!!!!!

                float boundingRadius = (float)System.Math.Max(
                    mLines[i].RelativePoint1.Length(),
                    mLines[i].RelativePoint2.Length());

                mMaxLinesRadius = System.Math.Max(mMaxLinesRadius,
                    boundingRadius);
            }

            for(int i = 0; i < mSpheres.Count; i++)
            {
                mMaxSpheresRadius = System.Math.Max(mMaxSpheresRadius,
                    mSpheres[i].Radius);
            }

            for(int i = 0; i < mAxisAlignedCubes.Count; i++)
            {
                mMaxAxisAlignedCubesRadius = System.Math.Max(mMaxAxisAlignedCubesRadius,
                    mAxisAlignedCubes[i].BoundingRadius);
            }

            for (int i = 0; i < mCapsule2Ds.Count; i++)
            {
                mMaxCapsule2DsRadius = System.Math.Max(mMaxCapsule2DsRadius,
                    mCapsule2Ds[i].BoundingRadius);
            }

        }

        public void Clear()
        {
            mAxisAlignedRectangles.Clear();

            mAxisAlignedCubes.Clear();

            mCircles.Clear();

            mPolygons.Clear();

            mLines.Clear();

            mSpheres.Clear();

            mCapsule2Ds.Clear();
        }

		public ShapeCollection Clone()
		{
			ShapeCollection shapeCollection = new ShapeCollection();

			for (int i = 0; i < mAxisAlignedRectangles.Count; i++)
			{
				shapeCollection.mAxisAlignedRectangles.Add(mAxisAlignedRectangles[i].Clone());
			}

			for (int i = 0; i < mCircles.Count; i++)
			{
				shapeCollection.mCircles.Add(mCircles[i].Clone());
			}

			for (int i = 0; i < mPolygons.Count; i++)
			{
				shapeCollection.mPolygons.Add(mPolygons[i].Clone());
			}

			for (int i = 0; i < mLines.Count; i++)
			{
				shapeCollection.mLines.Add(mLines[i].Clone());
			}

			for (int i = 0; i < mSpheres.Count; i++)
			{
				shapeCollection.mSpheres.Add(mSpheres[i].Clone());
			}

			for (int i = 0; i < mAxisAlignedCubes.Count; i++)
			{
				shapeCollection.mAxisAlignedCubes.Add(mAxisAlignedCubes[i].Clone());
			}

			for (int i = 0; i < mCapsule2Ds.Count; i++)
			{
				shapeCollection.mCapsule2Ds.Add(mCapsule2Ds[i].Clone());
			}

			return shapeCollection;
		}

        public void CopyAbsoluteToRelative()
        {
            CopyAbsoluteToRelative(true);
        }


        public void CopyAbsoluteToRelative(bool includeItemsWithParent)
        {
            mAxisAlignedRectangles.CopyAbsoluteToRelative(includeItemsWithParent);
            mCircles.CopyAbsoluteToRelative(includeItemsWithParent);
            mPolygons.CopyAbsoluteToRelative(includeItemsWithParent);
            mLines.CopyAbsoluteToRelative(includeItemsWithParent);
            mSpheres.CopyAbsoluteToRelative(includeItemsWithParent);
            mAxisAlignedCubes.CopyAbsoluteToRelative(includeItemsWithParent);
            mCapsule2Ds.CopyAbsoluteToRelative(includeItemsWithParent);
        }

        public PositionedObject FindByName(string nameToSearchFor)
        {
            AxisAlignedRectangle aar = mAxisAlignedRectangles.FindByName(nameToSearchFor);
            if (aar != null)
            {
                return aar;
            }

            Circle circle = mCircles.FindByName(nameToSearchFor);
            if (circle != null)
            {
                return circle;
            }

            Polygon polygon = mPolygons.FindByName(nameToSearchFor);
            if (polygon != null)
            {
                return polygon;
            }

            Line line = mLines.FindByName(nameToSearchFor);
            if(line != null)
            {
                return line;
            }

            Sphere sphere = mSpheres.FindByName(nameToSearchFor);
            if(sphere != null)
            {
                return sphere;
            }

            AxisAlignedCube aac = mAxisAlignedCubes.FindByName(nameToSearchFor);
            if(aac != null)
            {
                return aac;
            }

            Capsule2D capsule = mCapsule2Ds.FindByName(nameToSearchFor);
            if (capsule != null)
            {
                return capsule;
            }

            // If we got here then there's no object in the ShapeCollection by this name.
            return null;
        }

        /// <summary>
        /// Removes all contained shapes from the ShapeManager and clears this ShapeCollection.
        /// </summary>
        public void RemoveFromManagers()
        {
            RemoveFromManagers(true);
        }

        /// <summary>
        /// Removes all contained shapes from the ShapeManager and optionally clears this ShapeCollection.
        /// </summary>
        /// <remarks>
        /// Removal of shapes removes shapes from every-frame management and visibility.
        /// </remarks>
        /// <param name="clearThis">Whether to clear this ShapeCollection.</param>
        public void RemoveFromManagers(bool clearThis)
        {
            if (!clearThis)
            {
                MakeOneWay();
            }

            #region Remove the AxisAlignedRectangles
            for (int i = mAxisAlignedRectangles.Count - 1; i > -1; i--)
            {
                AxisAlignedRectangle shape = mAxisAlignedRectangles[i];

                PositionedObject oldParent = shape.Parent;

                ShapeManager.Remove(shape);

				if (!clearThis && oldParent != null)
                {
                    shape.AttachTo(oldParent, false);
                }
            }
            #endregion

            #region Remove the AxisAlignedCubes
            for (int i = mAxisAlignedCubes.Count - 1; i > -1; i--)
            {
                AxisAlignedCube shape = mAxisAlignedCubes[i];

                PositionedObject oldParent = shape.Parent;

                ShapeManager.Remove(shape);

                if (!clearThis && oldParent != null)
                {
                    shape.AttachTo(oldParent, false);
                }
            }
            #endregion

            #region Remove the Capsules

            for (int i = mCapsule2Ds.Count - 1; i > -1; i--)
            {
                Capsule2D shape = mCapsule2Ds[i];

                PositionedObject oldParent = shape.Parent;

                ShapeManager.Remove(shape);

                if (!clearThis && oldParent != null)
                {
                    shape.AttachTo(oldParent, false);
                }
            }

            #endregion

            #region Remove the Spheres
            for (int i = mSpheres.Count - 1; i > -1; i--)
            {
                Sphere shape = mSpheres[i];

                PositionedObject oldParent = shape.Parent;

                ShapeManager.Remove(shape);

                if (!clearThis && oldParent != null)
                {
                    shape.AttachTo(oldParent, false);
                }
            }
            #endregion

            #region Remove the Circles
            for (int i = mCircles.Count - 1; i > -1; i--)
            {
                Circle shape = mCircles[i];

                PositionedObject oldParent = shape.Parent;

                ShapeManager.Remove(shape);

                if (!clearThis && oldParent != null)
                {
                    shape.AttachTo(oldParent, false);
                }
            }
            #endregion

            #region Remove the Polygons
            for (int i = mPolygons.Count - 1; i > -1; i--)
            {
                Polygon shape = mPolygons[i];

                PositionedObject oldParent = shape.Parent;

                ShapeManager.Remove(shape);

                if (!clearThis && oldParent != null)
                {
                    shape.AttachTo(oldParent, false);
                }
            }
            #endregion

            #region Remove the Lines

            for (int i = mLines.Count - 1; i > -1; i--)
            {
                Line shape = mLines[i];

                PositionedObject oldParent = shape.Parent;

                ShapeManager.Remove(shape);

                if (!clearThis && oldParent != null)
                {
                    shape.AttachTo(oldParent, false);
                }
            }

            #endregion

            if (!clearThis)
            {
                MakeTwoWay();
            }
            else
            {
                // just in case its oneway already
                Clear();
            }
        }

        /// <summary>
        /// Makes all contained lists (such as for AxisAlignedRectangles and Circles) two-way.
        /// </summary>
        public void MakeTwoWay()
        {
            mAxisAlignedCubes.MakeTwoWay();
            mAxisAlignedRectangles.MakeTwoWay();
            mCapsule2Ds.MakeTwoWay();
            mCircles.MakeTwoWay();
            mSpheres.MakeTwoWay();
            mPolygons.MakeTwoWay();
            mLines.MakeTwoWay();
        }

        /// <summary>
        /// Makes all contained lists (such as for AxisAlignedRectangles and Circles) one-way.
        /// </summary>
        public void MakeOneWay()
        {
            mAxisAlignedCubes.MakeOneWay();
            mAxisAlignedRectangles.MakeOneWay();
            mCapsule2Ds.MakeOneWay();
            mCircles.MakeOneWay();
            mSpheres.MakeOneWay();
            mPolygons.MakeOneWay();
            mLines.MakeOneWay();
        }

        /// <summary>
        /// Changes the absolute Position value of all contained objects by the argument shiftVector.
        /// </summary>
        /// <param name="shiftVector">The amount to shift by.</param>
        public void Shift(Vector3 shiftVector)
        {
            int i;

            for (i = 0; i < mAxisAlignedRectangles.Count; i++)
            {
                mAxisAlignedRectangles[i].Position += shiftVector;
            }

            for (i = 0; i < mCircles.Count; i++)
            {
                mCircles[i].Position += shiftVector;
            }

            for (i = 0; i < mPolygons.Count; i++)
            {
                mPolygons[i].Position += shiftVector;
            }

            for (i = 0; i < mLines.Count; i++)
            {
                mLines[i].Position += shiftVector;
            }

            for (i = 0; i < mSpheres.Count; i++)
            {
                mSpheres[i].Position += shiftVector;
            }

            for (i = 0; i < mAxisAlignedCubes.Count; i++)
            {
                mAxisAlignedCubes[i].Position += shiftVector;
            }

            for (i = 0; i < mCapsule2Ds.Count; i++)
            {
                mCapsule2Ds[i].Position += shiftVector;
            }

        }

        public void ShiftRelative(float x, float y, float z)
        {
            Vector3 shiftVector = new Vector3(x, y, z);


            mAxisAlignedRectangles.ShiftRelative(shiftVector);
            
            
            mCircles.ShiftRelative(shiftVector);
            
            
            mPolygons.ShiftRelative(shiftVector);
            
            
            mLines.ShiftRelative(shiftVector);
            
            
            mSpheres.ShiftRelative(shiftVector);
            
            
            mAxisAlignedCubes.ShiftRelative(shiftVector);
            
            
            mCapsule2Ds.ShiftRelative(shiftVector);

        }

        public void SortAscending(Axis axisToSortOn)
        {
            switch (axisToSortOn)
            {
                case Axis.X:
                    mAxisAlignedRectangles.SortXInsertionAscending();
                    mCircles.SortXInsertionAscending();
                    mPolygons.SortXInsertionAscending();
                    mLines.SortXInsertionAscending();
                    mSpheres.SortXInsertionAscending();
                    mAxisAlignedCubes.SortXInsertionAscending();
                    mCapsule2Ds.SortXInsertionAscending();

                    break;
                case Axis.Y:
                    mAxisAlignedRectangles.SortYInsertionAscending();
                    mCircles.SortYInsertionAscending();
                    mPolygons.SortYInsertionAscending();
                    mLines.SortYInsertionAscending();
                    mSpheres.SortYInsertionAscending();
                    mAxisAlignedCubes.SortYInsertionAscending();
                    mCapsule2Ds.SortYInsertionAscending();
                    break;
                default:


                    throw new NotImplementedException();
                    // break;

            }
        }

        public override string ToString()
        {
            return "ShapeCollection: " + Name;
        }

        public void UpdateDependencies(double currentTime)
        {
            for (int i = 0; i < mAxisAlignedRectangles.Count; i++)
            {
                mAxisAlignedRectangles[i].UpdateDependencies(currentTime);
            }
            for (int i = 0; i < mCircles.Count; i++)
            {
                mCircles[i].UpdateDependencies(currentTime);
            }
            for (int i = 0; i < mPolygons.Count; i++)
            {
                mPolygons[i].UpdateDependencies(currentTime);
            }
            for (int i = 0; i < mLines.Count; i++)
            {
                mLines[i].UpdateDependencies(currentTime);
            }
            for (int i = 0; i < mSpheres.Count; i++)
            {
                mSpheres[i].UpdateDependencies(currentTime);
            }
            for (int i = 0; i < mAxisAlignedCubes.Count; i++)
            {
                mAxisAlignedCubes[i].UpdateDependencies(currentTime);
            }
            for (int i = 0; i < mCapsule2Ds.Count; i++)
            {
                mCapsule2Ds[i].UpdateDependencies(currentTime);
            }
        }

        public void FlipHorizontally()
        {
            #region Flip the AxisAlignedRectangles
            for (int i = mAxisAlignedRectangles.Count - 1; i > -1; i--)
            {
                AxisAlignedRectangle shape = mAxisAlignedRectangles[i];
                shape.RelativePosition.X = -shape.RelativePosition.X;
            }
            #endregion

            #region Flip the AxisAlignedCubes
            for (int i = mAxisAlignedCubes.Count - 1; i > -1; i--)
            {
                AxisAlignedCube shape = mAxisAlignedCubes[i];
                shape.RelativePosition.X = -shape.RelativePosition.X;
            }
            #endregion

            #region Flip the Capsules

            for (int i = mCapsule2Ds.Count - 1; i > -1; i--)
            {
                Capsule2D shape = mCapsule2Ds[i];
                shape.RelativePosition.X = -shape.RelativePosition.X;
                shape.RotationZ = -shape.RotationZ;
                shape.RelativeRotationZ = -shape.RelativeRotationZ;
            }

            #endregion

            #region Flip the Spheres
            for (int i = mSpheres.Count - 1; i > -1; i--)
            {
                Sphere shape = mSpheres[i];
                shape.RelativePosition.X = -shape.RelativePosition.X;
            }
            #endregion

            #region Flip the Circles
            for (int i = mCircles.Count - 1; i > -1; i--)
            {
                Circle shape = mCircles[i];
                shape.RelativePosition.X = -shape.RelativePosition.X;
            }
            #endregion

            #region Flip the Polygons
            for (int i = mPolygons.Count - 1; i > -1; i--)
            {
                Polygon shape = mPolygons[i];
                shape.FlipRelativePointsHorizontally();
                shape.RelativePosition.X = -shape.RelativePosition.X;
                shape.RotationZ = -shape.RotationZ;
                shape.RelativeRotationZ = -shape.RelativeRotationZ;
            }
            #endregion

            #region Flip the Lines

            for (int i = mLines.Count - 1; i > -1; i--)
            {
                Line shape = mLines[i];
                shape.FlipRelativePointsHorizontally();
                shape.RelativePosition.X = -shape.RelativePosition.X;
                shape.RotationZ = -shape.RotationZ;
                shape.RelativeRotationZ = -shape.RelativeRotationZ;
            }

            #endregion
        }

        /// <summary>
        /// Checks if the designated 2D point is in the 2D shapes of the shape collection
        /// </summary>
        public bool IsPointInside(float x, float y)
        {
            int count;

            count = AxisAlignedRectangles.Count;
            for (int i = 0; i < count; i++)
            {
                if (AxisAlignedRectangles[i].IsPointInside(x, y))
                    return true;
            }

            count = Circles.Count;
            for (int i = 0; i < count; i++)
            {
                if (Circles[i].IsPointInside(x, y))
                    return true;
            }

            count = Polygons.Count;
            for (int i = 0; i < count; i++)
            {
                if (Polygons[i].IsPointInside(x, y))
                    return true;
            }

            return false;
        }

        #endregion

        #region Internal Methods

		internal void ClearLastCollisionLists()
		{
			if (!mSuppressLastCollisionClear)
			{
				mLastCollisionAxisAlignedRectangles.Clear();
				mLastCollisionCircles.Clear();
				mLastCollisionPolygons.Clear();
				mLastCollisionLines.Clear();
				mLastCollisionSpheres.Clear();
				mLastCollisionAxisAlignedCubes.Clear();
				mLastCollisionCapsule2Ds.Clear();
			}
		}

        internal void ResetLastUpdateTimes()
        {
            foreach (var shape in mAxisAlignedRectangles) { shape.LastDependencyUpdate = -1; }
            foreach (var shape in mCircles) { shape.LastDependencyUpdate = -1; }
            foreach (var shape in mPolygons) { shape.LastDependencyUpdate = -1; }
            foreach (var shape in mLines) { shape.LastDependencyUpdate = -1; }
            foreach (var shape in mSpheres) { shape.LastDependencyUpdate = -1; }
            foreach (var shape in mAxisAlignedCubes) { shape.LastDependencyUpdate = -1; }
            foreach (var shape in mCapsule2Ds) { shape.LastDependencyUpdate = -1; }
        }

        #endregion

        #region Private Methods


        private bool CollideWithoutSnag2D(CollisionDelegate collisionMethod, SetLastMovePositionDelegate lastMovePosition, PositionedObject collidingShape)
        {
            Vector2 originalPosition;
            Vector2 originalVelocity;
            Vector2 originalParentPosition;

            UpdateDependencies(TimeManager.CurrentTime);
            collidingShape.UpdateDependencies(TimeManager.CurrentTime);

            originalPosition.X = collidingShape.X;
            originalPosition.Y = collidingShape.Y;

            originalVelocity.X = collidingShape.TopParent.XVelocity;
            originalVelocity.Y = collidingShape.TopParent.YVelocity;

            originalParentPosition.X = collidingShape.TopParent.X;
            originalParentPosition.Y = collidingShape.TopParent.Y;

            if (collisionMethod())
            {
                if (originalPosition.X != collidingShape.X && originalPosition.Y != collidingShape.Y)
                {
                    Vector2 finalPosition;
                    Vector2 finalVelocity;

                    Vector2 newPosition;
                    Vector2 newVelocity;
                    Vector2 newTopParentPosition;

                    List<AxisAlignedCube> finalLastCollisionAxisAlignedCubes = new List<AxisAlignedCube>();
                    List<AxisAlignedRectangle> finalLastCollisionAxisAlignedRectangles = new List<AxisAlignedRectangle>();
                    List<Capsule2D> finalLastCollisionCapsule2Ds = new List<Capsule2D>();
                    List<Circle> finalLastCollisionCircles = new List<Circle>();
                    List<Line> finalLastCollisionLines = new List<Line>();
                    List<Polygon> finalLastCollisionPolygons = new List<Polygon>();
                    List<Sphere> finalLastCollisionSpheres = new List<Sphere>();

                    newPosition.X = collidingShape.X;
                    newPosition.Y = collidingShape.Y;

                    newVelocity.X = collidingShape.TopParent.XVelocity;
                    newVelocity.Y = collidingShape.TopParent.YVelocity;

                    newTopParentPosition.X = collidingShape.TopParent.X;
                    newTopParentPosition.Y = collidingShape.TopParent.Y;

                    
                    ///////////////
                    /////Fix X/////
                    ///////////////

                    //Move back to original X position
                    collidingShape.X = originalPosition.X;
                    collidingShape.TopParent.XVelocity = originalVelocity.X;
                    collidingShape.TopParent.X = originalParentPosition.X;

                    collidingShape.ForceUpdateDependencies();

                    //Collide
                    collisionMethod();

                    //Save new position
                    finalPosition.X = collidingShape.X;
                    finalVelocity.X = collidingShape.TopParent.XVelocity;

                    //Move back to new position
                    collidingShape.X = newPosition.X;
                    collidingShape.TopParent.XVelocity = newVelocity.X;
                    collidingShape.TopParent.X = newTopParentPosition.X;

                    collidingShape.Y = newPosition.Y;
                    collidingShape.TopParent.YVelocity = newVelocity.Y;
                    collidingShape.TopParent.Y = newTopParentPosition.Y;

                    collidingShape.ForceUpdateDependencies();

                    //Save LastMove Shapes
                    finalLastCollisionAxisAlignedCubes.AddRange(mLastCollisionAxisAlignedCubes);
                    finalLastCollisionAxisAlignedRectangles.AddRange(mLastCollisionAxisAlignedRectangles);
                    finalLastCollisionCapsule2Ds.AddRange(mLastCollisionCapsule2Ds);
                    finalLastCollisionCircles.AddRange(mLastCollisionCircles);
                    finalLastCollisionLines.AddRange(mLastCollisionLines);
                    finalLastCollisionPolygons.AddRange(mLastCollisionPolygons);
                    finalLastCollisionSpheres.AddRange(mLastCollisionSpheres);

                    ///////////////
                    /////Fix Y/////
                    ///////////////

                    //Move back to original Y position
                    collidingShape.Y = originalPosition.Y;
                    collidingShape.TopParent.YVelocity = originalVelocity.Y;
                    collidingShape.TopParent.Y = originalParentPosition.Y;

                    collidingShape.ForceUpdateDependencies();

                    //Collide
                    collisionMethod();

                    //Save new position
                    finalPosition.Y = collidingShape.Y;
                    finalVelocity.Y = collidingShape.TopParent.YVelocity;


                    //Update to final position
                    collidingShape.X = finalPosition.X;
                    collidingShape.TopParent.XVelocity = finalVelocity.X;
                    collidingShape.TopParent.X = originalParentPosition.X + (finalPosition.X - originalPosition.X);


                    collidingShape.Y = finalPosition.Y;
                    collidingShape.TopParent.YVelocity = finalVelocity.Y;
                    collidingShape.TopParent.Y = originalParentPosition.Y + (finalPosition.Y - originalPosition.Y);

                    lastMovePosition(new Vector2(finalPosition.X - originalPosition.X, finalPosition.Y - originalPosition.Y));

                    collidingShape.ForceUpdateDependencies();

                    //Add missing LastMove Shapes

                    // todo - change this from a foreach because foreach generates memory
                    foreach (AxisAlignedCube cube in finalLastCollisionAxisAlignedCubes)
                    {
                        if (!LastCollisionAxisAlignedCubes.Contains(cube))
                            LastCollisionAxisAlignedCubes.Add(cube);
                    }

                    foreach (AxisAlignedRectangle rect in finalLastCollisionAxisAlignedRectangles)
                    {
                        if (!finalLastCollisionAxisAlignedRectangles.Contains(rect))
                            finalLastCollisionAxisAlignedRectangles.Add(rect);
                    }

                    foreach (Capsule2D cap2D in finalLastCollisionCapsule2Ds)
                    {
                        if (!finalLastCollisionCapsule2Ds.Contains(cap2D))
                            finalLastCollisionCapsule2Ds.Add(cap2D);
                    }

                    foreach (Circle circle in finalLastCollisionCircles)
                    {
                        if (!finalLastCollisionCircles.Contains(circle))
                            finalLastCollisionCircles.Add(circle);
                    }

                    foreach (Line line in finalLastCollisionLines)
                    {
                        if (!finalLastCollisionLines.Contains(line))
                            finalLastCollisionLines.Add(line);
                    }

                    foreach (Polygon polygon in finalLastCollisionPolygons)
                    {
                        if (!finalLastCollisionPolygons.Contains(polygon))
                            finalLastCollisionPolygons.Add(polygon);
                    }
                    
                    foreach (Sphere sphere in finalLastCollisionSpheres)
                    {
                        if (!finalLastCollisionSpheres.Contains(sphere))
                            finalLastCollisionSpheres.Add(sphere);
                    }
                }

                return true;
            }

            return false;
        }


        #endregion

        #endregion

        #region IEquatable<ShapeCollection> Members

        public bool Equals(ShapeCollection other)
        {
            return this == other;
        }

        #endregion

		#region Generated collision calling code

        public bool CollideAgainst(AxisAlignedRectangle axisAlignedRectangle)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, axisAlignedRectangle, false, Axis.X);
        }

        public bool CollideAgainst(AxisAlignedRectangle axisAlignedRectangle, bool considerPartitioning, Axis axisToUse)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, axisAlignedRectangle, considerPartitioning, axisToUse);

        }


        public bool CollideAgainst(Circle circle)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, circle, false, Axis.X);

        }

        public bool CollideAgainst(Circle circle, bool considerPartitioning, Axis axisToUse)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, circle, considerPartitioning, axisToUse);

        }


        public bool CollideAgainst(Polygon polygon)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, polygon, false, Axis.X);

        }

        public bool CollideAgainst(Polygon polygon, bool considerPartitioning, Axis axisToUse)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, polygon, considerPartitioning, axisToUse);

        }


        public bool CollideAgainst(Line line)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, line, false, Axis.X);

        }

        public bool CollideAgainst(Line line, bool considerPartitioning, Axis axisToUse)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, line, considerPartitioning, axisToUse);

        }


        public bool CollideAgainst(Capsule2D capsule2D)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, capsule2D, false, Axis.X);

        }

        public bool CollideAgainst(Capsule2D capsule2D, bool considerPartitioning, Axis axisToUse)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, capsule2D, considerPartitioning, axisToUse);

        }


        public bool CollideAgainst(Sphere sphere)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, sphere, false, Axis.X);

        }

        public bool CollideAgainst(Sphere sphere, bool considerPartitioning, Axis axisToUse)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, sphere, considerPartitioning, axisToUse);

        }


        public bool CollideAgainst(AxisAlignedCube axisAlignedCube)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, axisAlignedCube, false, Axis.X);

        }

        public bool CollideAgainst(AxisAlignedCube axisAlignedCube, bool considerPartitioning, Axis axisToUse)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, axisAlignedCube, considerPartitioning, axisToUse);

        }



        public bool CollideAgainstMove(AxisAlignedRectangle axisAlignedRectangle, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, axisAlignedRectangle, false, Axis.X, otherMass, thisMass);

        }

        public bool CollideAgainstMove(AxisAlignedRectangle axisAlignedRectangle, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, axisAlignedRectangle, considerPartitioning, axisToUse, otherMass, thisMass);

        }



        public bool CollideAgainstMove(Circle circle, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, circle, false, Axis.X, otherMass, thisMass);

        }

        public bool CollideAgainstMove(Circle circle, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, circle, considerPartitioning, axisToUse, otherMass, thisMass);

        }



        public bool CollideAgainstMove(Polygon polygon, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, polygon, false, Axis.X, otherMass, thisMass);

        }

        public bool CollideAgainstMove(Polygon polygon, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, polygon, considerPartitioning, axisToUse, otherMass, thisMass);

        }



        public bool CollideAgainstMove(Line line, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, line, false, Axis.X, otherMass, thisMass);

        }

        public bool CollideAgainstMove(Line line, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, line, considerPartitioning, axisToUse, otherMass, thisMass);

        }



        public bool CollideAgainstMove(Capsule2D capsule2D, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, capsule2D, false, Axis.X, otherMass, thisMass);

        }

        public bool CollideAgainstMove(Capsule2D capsule2D, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, capsule2D, considerPartitioning, axisToUse, otherMass, thisMass);

        }



        public bool CollideAgainstMove(Sphere sphere, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, sphere, false, Axis.X, otherMass, thisMass);

        }

        public bool CollideAgainstMove(Sphere sphere, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, sphere, considerPartitioning, axisToUse, otherMass, thisMass);

        }



        public bool CollideAgainstMove(AxisAlignedCube axisAlignedCube, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, axisAlignedCube, false, Axis.X, otherMass, thisMass);

        }

        public bool CollideAgainstMove(AxisAlignedCube axisAlignedCube, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, axisAlignedCube, considerPartitioning, axisToUse, otherMass, thisMass);

        }


        public bool CollideAgainstBounce(AxisAlignedRectangle axisAlignedRectangle, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, axisAlignedRectangle, false, Axis.X, otherMass, thisMass, elasticity);

        }

        public bool CollideAgainstBounce(AxisAlignedRectangle axisAlignedRectangle, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, axisAlignedRectangle, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

        }


        public bool CollideAgainstBounce(Circle circle, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, circle, false, Axis.X, otherMass, thisMass, elasticity);

        }

        public bool CollideAgainstBounce(Circle circle, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, circle, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

        }


        public bool CollideAgainstBounce(Polygon polygon, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, polygon, false, Axis.X, otherMass, thisMass, elasticity);

        }

        public bool CollideAgainstBounce(Polygon polygon, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, polygon, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

        }


        public bool CollideAgainstBounce(Line line, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, line, false, Axis.X, otherMass, thisMass, elasticity);

        }

        public bool CollideAgainstBounce(Line line, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, line, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

        }


        public bool CollideAgainstBounce(Capsule2D capsule2D, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, capsule2D, false, Axis.X, otherMass, thisMass, elasticity);

        }

        public bool CollideAgainstBounce(Capsule2D capsule2D, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, capsule2D, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

        }


        public bool CollideAgainstBounce(Sphere sphere, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, sphere, false, Axis.X, otherMass, thisMass, elasticity);

        }

        public bool CollideAgainstBounce(Sphere sphere, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, sphere, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

        }


        public bool CollideAgainstBounce(AxisAlignedCube axisAlignedCube, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, axisAlignedCube, false, Axis.X, otherMass, thisMass, elasticity);

        }

        public bool CollideAgainstBounce(AxisAlignedCube axisAlignedCube, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, axisAlignedCube, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);

        }


        public bool CollideAgainst(ShapeCollection shapeCollection)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, shapeCollection, false, Axis.X);

        }

        public bool CollideAgainst(ShapeCollection shapeCollection, bool considerPartitioning, Axis axisToUse)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThis(this, shapeCollection, considerPartitioning, axisToUse);

        }



        public bool CollideAgainstMove(ShapeCollection shapeCollection, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, shapeCollection, false, Axis.X, otherMass, thisMass);

        }

        public bool CollideAgainstMove(ShapeCollection shapeCollection, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, shapeCollection, considerPartitioning, axisToUse, otherMass, thisMass);

        }


        public bool CollideAgainstBounce(ShapeCollection shapeCollection, float thisMass, float otherMass, float elasticity)
        {
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, shapeCollection, false, Axis.X, otherMass, thisMass, elasticity);

        }

        public bool CollideAgainstBounce(ShapeCollection shapeCollection, bool considerPartitioning, Axis axisToUse, float thisMass, float otherMass, float elasticity)
        {
            this.ClearLastCollisionLists();
            return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, shapeCollection, considerPartitioning, axisToUse, otherMass, thisMass, elasticity);
        }



        public bool CollideAgainstMoveWithoutSnag(AxisAlignedRectangle axisAlignedRectangle)
        {
            return CollideWithoutSnag2D(delegate() {return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, axisAlignedRectangle, false, Axis.X, 0, 1);}
                                                    , delegate(Vector2 lastMovePosition) 
                                                    {
                                                        axisAlignedRectangle.mLastMoveCollisionReposition.X = lastMovePosition.X;
                                                        axisAlignedRectangle.mLastMoveCollisionReposition.Y = lastMovePosition.Y;
                                                    }
                                                    , axisAlignedRectangle);

        }

        public bool CollideAgainstMoveWithoutSnag(AxisAlignedRectangle axisAlignedRectangle, bool considerPartitioning, Axis axisToUse)
        {
            return CollideWithoutSnag2D(delegate() {return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, axisAlignedRectangle, considerPartitioning, axisToUse, 0, 1);}
                                                    , delegate(Vector2 lastMovePosition)
                                                    {
                                                        axisAlignedRectangle.mLastMoveCollisionReposition.X = lastMovePosition.X;
                                                        axisAlignedRectangle.mLastMoveCollisionReposition.Y = lastMovePosition.Y;
                                                    }
                                                    , axisAlignedRectangle);

        }



        public bool CollideAgainstMoveWithoutSnag(Circle circle)
        {
            return CollideWithoutSnag2D(delegate() { return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, circle, false, Axis.X, 0, 1); }
                                                    , delegate(Vector2 lastMovePosition)
                                                    {
                                                        circle.LastMoveCollisionReposition.X = lastMovePosition.X;
                                                        circle.LastMoveCollisionReposition.Y = lastMovePosition.Y;
                                                    }
                                                    , circle);

        }

        public bool CollideAgainstMoveWithoutSnag(Circle circle, bool considerPartitioning, Axis axisToUse)
        {
            return CollideWithoutSnag2D(delegate() { return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, circle, considerPartitioning, axisToUse, 0, 1); }
                                                    , delegate(Vector2 lastMovePosition)
                                                    {
                                                        circle.LastMoveCollisionReposition.X = lastMovePosition.X;
                                                        circle.LastMoveCollisionReposition.Y = lastMovePosition.Y;
                                                    }
                                                    , circle);

        }



        public bool CollideAgainstMoveWithoutSnag(Polygon polygon)
        {
            return CollideWithoutSnag2D(delegate() { return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, polygon, false, Axis.X, 0, 1); }
                                        , delegate(Vector2 lastMovePosition)
                                        {
                                            polygon.mLastMoveCollisionReposition.X = lastMovePosition.X;
                                            polygon.mLastMoveCollisionReposition.Y = lastMovePosition.Y;
                                        }
                                        , polygon);

        }

        public bool CollideAgainstMoveWithoutSnag(Polygon polygon, bool considerPartitioning, Axis axisToUse)
        {
            return CollideWithoutSnag2D(delegate() { return ShapeCollectionCollision.CollideShapeAgainstThisMove(this, polygon, considerPartitioning, axisToUse, 0, 1); }
                                        , delegate(Vector2 lastMovePosition)
                                        {
                                            polygon.mLastMoveCollisionReposition.X = lastMovePosition.X;
                                            polygon.mLastMoveCollisionReposition.Y = lastMovePosition.Y;
                                        }
                                        , polygon);

        }



        public bool CollideAgainstBounceWithoutSnag(AxisAlignedRectangle axisAlignedRectangle, float elasticity)
        {
            return CollideWithoutSnag2D(delegate() {return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, axisAlignedRectangle, false, Axis.X, 0, 1, elasticity);}
                                        , delegate(Vector2 lastMovePosition)
                                        {
                                            axisAlignedRectangle.mLastMoveCollisionReposition.X = lastMovePosition.X;
                                            axisAlignedRectangle.mLastMoveCollisionReposition.Y = lastMovePosition.Y;
                                        }
                                        , axisAlignedRectangle);

        }

        public bool CollideAgainstBounceWithoutSnag(AxisAlignedRectangle axisAlignedRectangle, bool considerPartitioning, Axis axisToUse, float elasticity)
        {
            return CollideWithoutSnag2D(delegate() {return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, axisAlignedRectangle, considerPartitioning, axisToUse, 0, 1, elasticity);}
                                                    , delegate(Vector2 lastMovePosition)
                                                    {
                                                        axisAlignedRectangle.mLastMoveCollisionReposition.X = lastMovePosition.X;
                                                        axisAlignedRectangle.mLastMoveCollisionReposition.Y = lastMovePosition.Y;
                                                    }
                                                    , axisAlignedRectangle);

        }


        public bool CollideAgainstBounceWithoutSnag(Circle circle, float elasticity)
        {
            return CollideWithoutSnag2D(delegate() { return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, circle, false, Axis.X, 0, 1, elasticity); }
                                                    , delegate(Vector2 lastMovePosition)
                                                    {
                                                        circle.LastMoveCollisionReposition.X = lastMovePosition.X;
                                                        circle.LastMoveCollisionReposition.Y = lastMovePosition.Y;
                                                    }
                                                    , circle);

        }

        public bool CollideAgainstBounceWithoutSnag(Circle circle, bool considerPartitioning, Axis axisToUse, float elasticity)
        {
            return CollideWithoutSnag2D(delegate() { return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, circle, considerPartitioning, axisToUse, 0, 1, elasticity); }
                                                    , delegate(Vector2 lastMovePosition)
                                                    {
                                                        circle.LastMoveCollisionReposition.X = lastMovePosition.X;
                                                        circle.LastMoveCollisionReposition.Y = lastMovePosition.Y;
                                                    }
                                                    , circle);

        }


        public bool CollideAgainstBounceWithoutSnag(Polygon polygon, float elasticity)
        {
            return CollideWithoutSnag2D(delegate() { return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, polygon, false, Axis.X, 0, 1, elasticity); }
                                        , delegate(Vector2 lastMovePosition)
                                        {
                                            polygon.mLastMoveCollisionReposition.X = lastMovePosition.X;
                                            polygon.mLastMoveCollisionReposition.Y = lastMovePosition.Y;
                                        }
                                        , polygon);

        }

        /// <summary>
        /// Performs a "snagless" collision between this and the argument Polygon.
        /// </summary>
        /// <param name="polygon">The Polygon to perform collision against.</param>
        /// <param name="considerPartitioning">Whether to consider spacial partitioning.</param>
        /// <param name="axisToUse">If partitioning is used, the axis that the list is already sorted along.</param>
        /// <param name="elasticity">The elasticity to use in the collision.</param>
        /// <returns>Whether collision has occurred.</returns>
        public bool CollideAgainstBounceWithoutSnag(Polygon polygon, bool considerPartitioning, Axis axisToUse, float elasticity)
        {
            return CollideWithoutSnag2D(delegate() { return ShapeCollectionCollision.CollideShapeAgainstThisBounce(this, polygon, considerPartitioning, axisToUse, 0, 1, elasticity); }
                                        , delegate(Vector2 lastMovePosition)
                                        {
                                            polygon.mLastMoveCollisionReposition.X = lastMovePosition.X;
                                            polygon.mLastMoveCollisionReposition.Y = lastMovePosition.Y;
                                        }
                                        , polygon);

        }

        public bool CollideAgainstMoveSoft(ShapeCollection shapeCollection, float thisMass, float otherMass, float separationVelocity)
        {
            mSuppressLastCollisionClear = true;
            bool returnValue = false;

            // Currently we only support AARect vs AARect, Circle vs Circle,
            // Polygon vs AARect and Polygon vs Polygon.

            // AARect vs AARect
            for (int i = 0; i < shapeCollection.AxisAlignedRectangles.Count; i++)
            {
                var shape = shapeCollection.AxisAlignedRectangles[i];

                for(int j = 0; j < AxisAlignedRectangles.Count; j++)
                {
                    returnValue |= shape.CollideAgainstMoveSoft(AxisAlignedRectangles[j], otherMass, thisMass, separationVelocity);
                }
            }

            // Circle vs Circle
            for (int i = 0; i < shapeCollection.Circles.Count; i++)
            {
                var shape = shapeCollection.Circles[i];

                for(int j = 0; j < Circles.Count; j++)
                {
                    returnValue |= shape.CollideAgainstMoveSoft(Circles[j], otherMass, thisMass, separationVelocity);
                }
            }

            // Other Polygon vs this AARect
            for (int i = 0; i < shapeCollection.Polygons.Count; i++)
            {
                var shape = shapeCollection.Polygons[i];

                for (int j = 0; j < AxisAlignedRectangles.Count; j++)
                {
                    returnValue |= shape.CollideAgainstMoveSoft(AxisAlignedRectangles[j], otherMass, thisMass, separationVelocity);
                }
            }

            // This Polygon vs other AARect
            for (int i = 0; i < Polygons.Count; i++)
            {
                var shape = Polygons[i];

                for (int j = 0; j < shapeCollection.AxisAlignedRectangles.Count; j++)
                {
                    returnValue |= shape.CollideAgainstMoveSoft(shapeCollection.AxisAlignedRectangles[j], thisMass, otherMass, separationVelocity);
                }
            }

            // Polygon vs Polygon
            for (int i = 0; i < shapeCollection.Polygons.Count; i++)
            {
                var shape = shapeCollection.Polygons[i];

                for (int j = 0; j < Polygons.Count; j++)
                {
                    returnValue |= shape.CollideAgainstMoveSoft(Polygons[j], otherMass, thisMass, separationVelocity);
                }
            }

            mSuppressLastCollisionClear = false;
            return returnValue;
        }

        public bool CollideAgainstMoveSoft(Polygon polygon, float thisMass, float otherMass, float separationVelocity)
        {
            mSuppressLastCollisionClear = true;
            bool returnValue = false;

            for (int i = 0; i < AxisAlignedRectangles.Count; i++)
            {
                returnValue |= polygon.CollideAgainstMoveSoft(AxisAlignedRectangles[i], otherMass, thisMass, separationVelocity);
            }

            for (int i = 0; i < Polygons.Count; i++)
            {
                returnValue |= polygon.CollideAgainstMoveSoft(Polygons[i], otherMass, thisMass, separationVelocity);
            }

            mSuppressLastCollisionClear = false;
            return returnValue;
        }

        public bool CollideAgainstMovePositionSoft(ShapeCollection shapeCollection, float thisMass, float otherMass, float separationVelocity)
        {
            mSuppressLastCollisionClear = true;
            bool returnValue = false;

            // Currently we only support AARect vs AARect,
            // Polygon vs AARect and Polygon vs Polygon.

            // AARect vs AARect
            for (int i = 0; i < shapeCollection.AxisAlignedRectangles.Count; i++)
            {
                var shape = shapeCollection.AxisAlignedRectangles[i];

                for (int j = 0; j < AxisAlignedRectangles.Count; j++)
                {
                    returnValue |= shape.CollideAgainstMovePositionSoft(AxisAlignedRectangles[j], otherMass, thisMass, separationVelocity);
                }
            }

            // Other Polygon vs this AARect
            for (int i = 0; i < shapeCollection.Polygons.Count; i++)
            {
                var shape = shapeCollection.Polygons[i];

                for (int j = 0; j < AxisAlignedRectangles.Count; j++)
                {
                    returnValue |= shape.CollideAgainstMovePositionSoft(AxisAlignedRectangles[j], otherMass, thisMass, separationVelocity);
                }
            }

            // This Polygon vs other AARect
            for (int i = 0; i < Polygons.Count; i++)
            {
                var shape = Polygons[i];

                for (int j = 0; j < shapeCollection.AxisAlignedRectangles.Count; j++)
                {
                    returnValue |= shape.CollideAgainstMovePositionSoft(shapeCollection.AxisAlignedRectangles[j], thisMass, otherMass, separationVelocity);
                }
            }

            // Polygon vs Polygon
            for (int i = 0; i < shapeCollection.Polygons.Count; i++)
            {
                var shape = shapeCollection.Polygons[i];

                for (int j = 0; j < Polygons.Count; j++)
                {
                    returnValue |= shape.CollideAgainstMovePositionSoft(Polygons[j], otherMass, thisMass, separationVelocity);
                }
            }

            mSuppressLastCollisionClear = false;
            return returnValue;
        }

        public bool CollideAgainstMovePositionSoft(Polygon polygon, float thisMass, float otherMass, float separationVelocity)
        {
            mSuppressLastCollisionClear = true;
            bool returnValue = false;

            for (int i = 0; i < AxisAlignedRectangles.Count; i++)
            {
                returnValue |= polygon.CollideAgainstMovePositionSoft(AxisAlignedRectangles[i], otherMass, thisMass, separationVelocity);
            }

            for (int i = 0; i < Polygons.Count; i++)
            {
                returnValue |= polygon.CollideAgainstMovePositionSoft(Polygons[i], otherMass, thisMass, separationVelocity);
            }

            mSuppressLastCollisionClear = false;
            return returnValue;
        }

        public bool CollideAgainstMovePositionSoft(AxisAlignedRectangle rectangle, float thisMass, float otherMass, float separationVelocity)
        {
            mSuppressLastCollisionClear = true;
            bool returnValue = false;

            for (int i = 0; i < AxisAlignedRectangles.Count; i++)
            {
                returnValue |= AxisAlignedRectangles[i].CollideAgainstMovePositionSoft(rectangle, thisMass, otherMass, separationVelocity);
            }

            for (int i = 0; i < Polygons.Count; i++)
            {
                returnValue |= Polygons[i].CollideAgainstMovePositionSoft(rectangle, thisMass, otherMass, separationVelocity);
            }

            mSuppressLastCollisionClear = false;
            return returnValue;
        }

        #endregion

        public bool CollideAgainstClosest(Line line, Axis? sortAxis, float? gridSize)
        {
            this.ClearLastCollisionLists();
            line.LastCollisionPoint = new Point(double.NaN, double.NaN);

            Segment a = line.AsSegment();

            List<Segment> currentShapeSegments = new List<Segment>();
            List<Point> currentShapeIntersectionPoints = new List<Point>();

            if(InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space))
            {
                System.Diagnostics.Debugger.Break();
            }

            object collidedObject = null;

            var leftmost = (float)System.Math.Min(line.AbsolutePoint1.X, line.AbsolutePoint2.X);
            var rightmost = (float)System.Math.Max(line.AbsolutePoint1.X, line.AbsolutePoint2.X);

            var topMost = (float)System.Math.Max(line.AbsolutePoint1.Y, line.AbsolutePoint2.Y);
            var bottomMost = (float)System.Math.Min(line.AbsolutePoint1.Y, line.AbsolutePoint2.Y);

            float clampedPositionX = line.Position.X;
            float clampedPositionY = line.Position.Y;

            bool isPositionOnEnd = false;
            if (clampedPositionX <= leftmost)
            {
                clampedPositionX = leftmost;
                isPositionOnEnd = true;
            }
            else if (clampedPositionX >= rightmost)
            {
                clampedPositionX = rightmost;
                isPositionOnEnd = true;
            }

            if(clampedPositionY <= bottomMost)
            {
                clampedPositionY = bottomMost;
                isPositionOnEnd = true;
            }
            else if(clampedPositionY >= topMost)
            {
                clampedPositionY = topMost;
                isPositionOnEnd = true;
            }

            if(!isPositionOnEnd)
            {
                throw new ArgumentException("The argument Line must have its Position be on either one or the other endpoint, so that this function knows how to decide " +
                    "the closest point.");
            }

            Point? intersectionPoint = null;

            if (sortAxis == Axis.X)
            {

                var firstIndex = 0;
                var lastIndex = 0;

                #region Rectangles

                var rectangles = this.AxisAlignedRectangles;
                if(gridSize != null)
                {
                    firstIndex= rectangles.GetFirstAfter(leftmost - gridSize.Value, sortAxis.Value, 0, rectangles.Count);
                    lastIndex = rectangles.GetFirstAfter(rightmost + gridSize.Value, sortAxis.Value, firstIndex, rectangles.Count);
                }

                if (clampedPositionX < rightmost)
                {
                    // start at the beginning of the list, go up
                    for (int i = firstIndex; i < lastIndex; i++)
                    {
                        var rectangle = rectangles[i];
                        if(intersectionPoint?.X < rectangle.Left)
                        {
                            break;
                        }

                        FillSegments(currentShapeSegments, rectangle);
                        CollideAgainstSegments(line, ref a, currentShapeSegments, ref collidedObject, ref intersectionPoint, rectangle);
                    }
                }
                else
                {
                    // start at the end of the list, go down
                    for (int i = lastIndex - 1; i >= firstIndex; i--)
                    {
                        var rectangle = rectangles[i];
                        if (intersectionPoint?.X > rectangle.Right)
                        {
                            break;
                        }

                        FillSegments(currentShapeSegments, rectangle);
                        CollideAgainstSegments(line, ref a, currentShapeSegments, ref collidedObject, ref intersectionPoint, rectangle);
                    }
                }

                #endregion

                #region Polygons

                var polygons = this.Polygons;
                if(gridSize != null)
                {
                    firstIndex = polygons.GetFirstAfter(leftmost - gridSize.Value, sortAxis.Value, 0, polygons.Count);
                    lastIndex = polygons.GetFirstAfter(rightmost + gridSize.Value, sortAxis.Value, firstIndex, polygons.Count);
                }

                if (clampedPositionX < rightmost)
                {
                    // start at the beginning of the list, go up
                    for (int i = firstIndex; i < lastIndex; i++)
                    {
                        var polygon = polygons[i];
                        if(intersectionPoint?.X < polygon.Position.X - polygon.BoundingRadius)
                        {
                            break;
                        }

                        FillSegments(currentShapeSegments, polygon);
                        CollideAgainstSegments(line, ref a, currentShapeSegments, ref collidedObject, ref intersectionPoint, polygon);

                    }
                }
                else
                {
                    // start at the end of the list, go down
                    for (int i = lastIndex - 1; i >= firstIndex; i--)
                    {
                        var polygon = polygons[i];
                        if(intersectionPoint?.X > polygon.Position.X + polygon.BoundingRadius)
                        {
                            break;
                        }

                        FillSegments(currentShapeSegments, polygon);
                        CollideAgainstSegments(line, ref a, currentShapeSegments, ref collidedObject, ref intersectionPoint, polygon);
                    }
                }
                #endregion
            }

            else if (sortAxis == Axis.Y)
            {

                var firstIndex = 0;
                var lastIndex = 0;

                #region Rectangles

                var rectangles = this.AxisAlignedRectangles;
                if (gridSize != null)
                {
                    firstIndex = rectangles.GetFirstAfter(bottomMost - gridSize.Value, sortAxis.Value, 0, rectangles.Count);
                    lastIndex = rectangles.GetFirstAfter(topMost + gridSize.Value, sortAxis.Value, firstIndex, rectangles.Count);
                }

                if(clampedPositionY <= bottomMost)
                {
                    // start at the beginning of the list, go up
                    for (int i = firstIndex; i < lastIndex; i++)
                    {
                        var rectangle = rectangles[i];
                        if (intersectionPoint?.Y < rectangle.Bottom)
                        {
                            break;
                        }

                        FillSegments(currentShapeSegments, rectangle);
                        CollideAgainstSegments(line, ref a, currentShapeSegments, ref collidedObject, ref intersectionPoint, rectangle);
                    }
                }
                else
                {
                    // start at the end of the list, go down
                    for (int i = lastIndex - 1; i >= firstIndex; i--)
                    {
                        var rectangle = rectangles[i];
                        if (intersectionPoint?.Y > rectangle.Top)
                        {
                            break;
                        }

                        FillSegments(currentShapeSegments, rectangle);
                        CollideAgainstSegments(line, ref a, currentShapeSegments, ref collidedObject, ref intersectionPoint, rectangle);
                    }
                }

                #endregion

                #region Polygons

                var polygons = this.Polygons;
                if(gridSize != null)
                {
                    firstIndex = polygons.GetFirstAfter(bottomMost - gridSize.Value, sortAxis.Value, 0, polygons.Count);
                    lastIndex = polygons.GetFirstAfter(topMost + gridSize.Value, sortAxis.Value, firstIndex, polygons.Count);
                }

                if(clampedPositionY < topMost)
                {
                    for(int i = firstIndex; i < lastIndex; i++)
                    {
                        var polygon = polygons[i];
                        if (intersectionPoint?.Y < polygon.Position.Y - polygon.BoundingRadius)
                        {
                            break;
                        }

                        FillSegments(currentShapeSegments, polygon);
                        CollideAgainstSegments(line, ref a, currentShapeSegments, ref collidedObject, ref intersectionPoint, polygon);
                    }
                }
                else
                {
                    // start at the end of the list, go down
                    for (int i = lastIndex - 1; i >= firstIndex; i--)
                    {
                        var polygon = polygons[i];
                        if (intersectionPoint?.Y > polygon.Position.Y + polygon.BoundingRadius)
                        {
                            break;
                        }

                        FillSegments(currentShapeSegments, polygon);
                        CollideAgainstSegments(line, ref a, currentShapeSegments, ref collidedObject, ref intersectionPoint, polygon);
                    }
                }

                #endregion
            }
            else
            {
                for(int i = 0; i < AxisAlignedRectangles.Count; i++)
                {
                    var rectangle = AxisAlignedRectangles[i];

                    FillSegments(currentShapeSegments, rectangle);
                    CollideAgainstSegments(line, ref a, currentShapeSegments, ref collidedObject, ref intersectionPoint, rectangle);
                }
                for(int i = 0; i < Polygons.Count; i++)
                {
                    var polygon = Polygons[i];

                    FillSegments(currentShapeSegments, polygon);
                    CollideAgainstSegments(line, ref a, currentShapeSegments, ref collidedObject, ref intersectionPoint, polygon);
                }
            }

            if (collidedObject is AxisAlignedRectangle collidedRectangle)
            {
                mLastCollisionAxisAlignedRectangles.Add(collidedRectangle);
            }
            else if (collidedObject is Polygon collidedPolygon)
            {
                mLastCollisionPolygons.Add(collidedPolygon);
            }

            line.LastCollisionPoint = intersectionPoint ?? new Point(double.NaN, double.NaN);

            return collidedObject != null;
        }

        private static void CollideAgainstSegments(Line line, ref Segment a, List<Segment> currentShapeSegments, ref object collidedShape, ref Point? intersectionPoint, object currentShape)
        {
            for (int segmentIndex = 0; segmentIndex < currentShapeSegments.Count; segmentIndex++)
            {
                var segment = currentShapeSegments[segmentIndex];
                if (a.Intersects(segment, out Point tempPoint))
                {
                    if (intersectionPoint == null)
                    {
                        intersectionPoint = tempPoint;
                        collidedShape = currentShape;
                    }
                    else
                    {
                        // which is closer?
                        var distanceToOldIntersectionSquared = (line.Position - intersectionPoint.Value).LengthSquared();
                        var distanceToNewIntersectionSquared = (line.Position - tempPoint).LengthSquared();

                        if (distanceToNewIntersectionSquared < distanceToOldIntersectionSquared)
                        {
                            intersectionPoint = tempPoint;
                            collidedShape = currentShape;
                        }
                    }
                }
            }
        }

        private static void FillSegments(List<Segment> currentShapeSegments, AxisAlignedRectangle rectangle)
        {
            currentShapeSegments.Clear();
            Point tl = new Point(
                                            rectangle.Position.X - rectangle.ScaleX,
                                            rectangle.Position.Y + rectangle.ScaleY);
            Point tr = new Point(
                rectangle.Position.X + rectangle.ScaleX,
                rectangle.Position.Y + rectangle.ScaleY);
            Point bl = new Point(
                rectangle.Position.X - rectangle.ScaleX,
                rectangle.Position.Y - rectangle.ScaleY);
            Point br = new Point(
                rectangle.Position.X + rectangle.ScaleX,
                rectangle.Position.Y - rectangle.ScaleY);

            currentShapeSegments.Add(new Segment(tl, bl));
            currentShapeSegments.Add(new Segment(bl, br));
            currentShapeSegments.Add(new Segment(tl, tr));
            currentShapeSegments.Add(new Segment(tr, br));
        }

        private static void FillSegments(List<Segment> currentShapeSegments, Polygon polygon)
        {
            currentShapeSegments.Clear();

            for (int i = 0; i < polygon.Vertices.Length - 1; i++)
            {
                var segment = new Segment(polygon.Vertices[i].Position, polygon.Vertices[i + 1].Position);
                currentShapeSegments.Add(segment);
            }
        }

        public bool IsMouseOver(Gui.Cursor cursor)
        {
            return IsMouseOver(cursor, null);
        }

        public bool IsMouseOver(Gui.Cursor cursor, Layer layer)
        {
            return cursor.IsOn3D(this, layer);
        }
    }
}
