using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;

namespace EditorObjects.Gui
{
    public class PolygonPointListDisplayWindow : ListDisplayWindow
    {
        public Polygon SelectedPolygon
        {
            get;
            set;
        }

        public PolygonPointListDisplayWindow(Cursor cursor)
            : base(cursor)
        {
        }



        protected override void SetSelectedObjectsObjectAtIndex(int index, object value)
        {
            if (SelectedPolygon == null)
            {
                throw new InvalidOperationException();
            }

            SelectedPolygon.SetPoint(index, (Point)value);

            if (index == 0)
            {
                SelectedPolygon.SetPoint(SelectedPolygon.Points.Count - 1, (Point)value);
            }

            if (index == SelectedPolygon.Points.Count - 1)
            {
                SelectedPolygon.SetPoint(0, (Point)value);
            }
        }

    }
}
