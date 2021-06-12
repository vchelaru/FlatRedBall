using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using System;
using System.Collections.Generic;
using System.Text;

namespace {ProjectNamespace}.GlueControl.Editing
{
    class Guides
    {
        Line HorizontalLine;
        Line VerticalLine;

        bool visible;
        public bool Visible
        {
            get => visible;
            set
            {
                if (value != visible)
                {
                    visible = value;
                    HorizontalLine.Visible = value;
                    VerticalLine.Visible = value;
                }
            }
        }

        public Guides()
        {
            HorizontalLine = new Line();
            HorizontalLine.SetFromAbsoluteEndpoints(
                new Microsoft.Xna.Framework.Vector3(-10_0000, 0, 0),
                new Microsoft.Xna.Framework.Vector3(10_000, 0, 0));
            VerticalLine = new Line();
            VerticalLine.SetFromAbsoluteEndpoints(
                new Microsoft.Xna.Framework.Vector3(0, -10_0000, 0),
                new Microsoft.Xna.Framework.Vector3(0, 10_000, 0));
            ScreenManager.PersistentLines.Add(HorizontalLine);
            ScreenManager.PersistentLines.Add(VerticalLine);
        }
    }
}
