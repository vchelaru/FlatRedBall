using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;

namespace EditorObjects.Gui
{
    public class PolygonPropertyGrid : PropertyGrid<Polygon>
    {
        #region Fields

        PositionedObjectList<Polygon> mEditorPolygonList;

        PolygonPointListDisplayWindow mPointsListDisplayWindow;

        static bool IsFirstTimeCalled = true;

        #endregion

        #region Properties

        public PositionedObjectList<Polygon> EditorPolygonList
        {
            set 
            {
                if (mEditorPolygonList == null)
                {
                    mEditorPolygonList = value;

                    SetMemberChangeEvent("Name", MakeSelectedObjectNameUnique);
                }
                else
                {
                    throw new ArgumentException("Can't set the polygon list again - it's already been set once.");
                }
            }
        }

        public int CornerIndexHighlighted
        {
            get
            {
                if (ObjectDisplaying == null)
                {
                    return -1;
                }

                CollapseItem item = mPointsListDisplayWindow.GetFirstHighlightedItem();

                if (item == null)
                {
                    return -1;
                }
                else
                {
                    int index = mPointsListDisplayWindow.ListBox.Items.IndexOf(item);

                    if (index == ObjectDisplaying.Points.Count - 1)
                    {
                        index = 0;
                    }

                    return index;
                }

            }
        }

        public override Polygon SelectedObject
        {
            get
            {
                return base.SelectedObject;
            }
            set
            {
                base.SelectedObject = value;

                if (mPointsListDisplayWindow != null)
                {
                    mPointsListDisplayWindow.SelectedPolygon = value;
                }
                Visible = (SelectedObject != null);
            }
        }



        #endregion

        #region Events

        public event GuiMessage NewPointHighlight;

        #endregion

        #region Event Methods

        private void MakeSelectedObjectNameUnique(Window callingWindow)
        {
            // This gets called if the PropertyGrid is
            // referencing a valid list and
            // the selected object has its name changed.  When
            // this happens make sure the selected Object's name
            // is unique.
            FlatRedBall.Utilities.StringFunctions.MakeNameUnique<Polygon>(SelectedObject, mEditorPolygonList);
        }

        private void MergeParallelSegmentsClick(Window callingWindow)
        {
            MergeParallelSegments(ObjectDisplaying);
        }

        public static void MergeParallelSegments(Polygon polygon)
        {
            // EARLY OUT

            if (polygon.Points.Count < 2)
            {
                return;
            }

            // END EARLY OUT
            float minimumVariance = .01f;

            List<int> pointsToRemove = new List<int>();

            for (int i = polygon.Points.Count - 1; i > -1 + 2; i--)
            {


                Segment thisSegment = new Segment(polygon.Points[i], polygon.Points[i - 1]);
                Segment segmentBefore = new Segment(polygon.Points[i - 1], polygon.Points[i - 2]);

                if (thisSegment.GetLength() < minimumVariance || segmentBefore.GetLength() < minimumVariance)
                {
                    int m = 3;
                    m++;
                }

                double thisSegmentAngle = thisSegment.Angle;
                double segmentBeforeAngle = segmentBefore.Angle;

                if (Math.Abs(MathFunctions.AngleToAngle(thisSegmentAngle, segmentBeforeAngle)) < minimumVariance)
                {
                    if ((polygon.Points[i] - polygon.Points[i - 2]).LengthSquared() < 2)
                    {
                        int m = 3;
                    }                    
                    
                    pointsToRemove.Add(i - 1);
                }
            }

            bool removeAt0 = false;

            Segment endSegment = new Segment(polygon.Points[polygon.Points.Count - 1], polygon.Points[polygon.Points.Count - 2]);
            Segment startSegment = new Segment(polygon.Points[1], polygon.Points[0] );

            if (endSegment.GetLength() < minimumVariance || startSegment.GetLength() < minimumVariance)
            {
                int m = 3;
                m++;
            }

            double endAngle = endSegment.Angle;
            double startAngle = startSegment.Angle;

            if (Math.Abs(MathFunctions.AngleToAngle(endAngle, startAngle)) < minimumVariance)
            {
                removeAt0 = true;
            }

            List<Point> points = new List<Point>(polygon.Points);

            for (int i = 0; i < pointsToRemove.Count; i++)
            {
                points.RemoveAt(pointsToRemove[i]);
            }

            if (removeAt0)
            {
                points.RemoveAt(0);
                points[points.Count-1] = points[0];

            }

            polygon.Points = points;

        }

        private void NewPointHighlighted(Window callingWindow)
        {
            if (NewPointHighlight != null)
            {
                NewPointHighlight(this);
            }
        }

        private void OptimizeRadiusClick(Window callingWindow)
        {
            if (SelectedObject != null)
            {
                SelectedObject.OptimizeRadius();
            }
        }

        #endregion

        #region Methods

        public PolygonPropertyGrid(Cursor cursor)
            : base(cursor)
        {
            ExcludeAllMembers();

            #region Basic

            IncludeMember("X", "Basic");
            IncludeMember("Y", "Basic");

            IncludeMember("RotationZ", "Basic");

            IncludeMember("Color", "Basic");

            IncludeMember("Visible", "Basic");

            IncludeMember("Name", "Basic");

            #endregion

            #region Points

            IncludeMember("Points", "Points");
            SetMemberDisplayName("Points", "");
            mPointsListDisplayWindow = new PolygonPointListDisplayWindow(cursor);
            mPointsListDisplayWindow.ShowPropertyGridOnStrongSelect = true;
            mPointsListDisplayWindow.ScaleX = 20;
            mPointsListDisplayWindow.ScaleY = 10;
            mPointsListDisplayWindow.DrawBorders = false;
            ReplaceMemberUIElement("Points", mPointsListDisplayWindow);
            mPointsListDisplayWindow.SelectedPolygon = ObjectDisplaying;

            mPointsListDisplayWindow.ListBox.Highlight += NewPointHighlighted;

            #endregion

            #region Relative

            IncludeMember("RelativeX", "Relative");
            IncludeMember("RelativeY", "Relative");

            IncludeMember("RelativeRotationZ", "Relative");

            #endregion



            #region Optimize

            IncludeMember("BoundingRadius", "Optimize");

            const float buttonScale = 6;

            Button optimizeRadius = new Button(mCursor);
            optimizeRadius.ScaleX = buttonScale;
            optimizeRadius.ScaleY = 2;
            optimizeRadius.Text = "Optimize\nRadius";
            AddWindow(optimizeRadius, "Optimize");
            optimizeRadius.Click += OptimizeRadiusClick;

            Button mergeParallelSegments = new Button(mCursor);
            mergeParallelSegments.ScaleX = buttonScale;
            mergeParallelSegments.ScaleY = 2;
            mergeParallelSegments.Text = "Merge Parallel\nSegments";
            AddWindow(mergeParallelSegments, "Optimize");
            mergeParallelSegments.Click += MergeParallelSegmentsClick;

            #endregion

            RemoveCategory("Uncategorized");

            SelectCategory("Basic");

            if (IsFirstTimeCalled == true)
            {
                LastXPosition = 30;
                IsFirstTimeCalled = false;
            }

			X = LastXPosition;
			Y = LastYPosition;
        }

        #endregion

    }
}
