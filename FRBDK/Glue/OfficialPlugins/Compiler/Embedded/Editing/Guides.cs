using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace {ProjectNamespace}.GlueControl.Editing
{
    class Guides
    {
        Line HorizontalLine;
        Line VerticalLine;

        const int spacing = 32;
        List<Line> verticalLines = new List<Line>();
        List<Line> horizontalLines = new List<Line>();

        Color centerLineColor = new Color(new Microsoft.Xna.Framework.Vector4
            (.5f, .5f, .5f, .5f));

        Color smallGridLineColor = new Color(new Microsoft.Xna.Framework.Vector4
            (.3f, .3f, .3f, .3f));
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
                    foreach(var line in horizontalLines)
                    {
                        line.Visible = value;
                    }
                }
            }
        }

        public Guides()
        {

            HorizontalLine = new Line();
            HorizontalLine.SetFromAbsoluteEndpoints(
                new Microsoft.Xna.Framework.Vector3(-10_0000, 0, 0),
                new Microsoft.Xna.Framework.Vector3(10_000, 0, 0));
            HorizontalLine.Color = centerLineColor;

            VerticalLine = new Line();
            VerticalLine.SetFromAbsoluteEndpoints(
                new Microsoft.Xna.Framework.Vector3(0, -10_0000, 0),
                new Microsoft.Xna.Framework.Vector3(0, 10_000, 0));
            VerticalLine.Color = centerLineColor;

            ScreenManager.PersistentLines.Add(HorizontalLine);
            ScreenManager.PersistentLines.Add(VerticalLine);

        }

        public void UpdateGridLines()
        {
            var camera = Camera.Main;
            var leftmost = MathFunctions.RoundFloat( camera.AbsoluteLeftXEdge, spacing);
            var rightMost = MathFunctions.RoundFloat(camera.AbsoluteRightXEdge, spacing);

            var numberOfLines = MathFunctions.RoundToInt((rightMost - leftmost) / spacing);

            while(horizontalLines.Count < numberOfLines)
            {
                var line = new Line();
                ScreenManager.PersistentLines.Add(line);

                line.Color = smallGridLineColor;
                horizontalLines.Add(line);
            }
            while(horizontalLines.Count > numberOfLines)
            {
                var lineToRemove = horizontalLines.Last();
                lineToRemove.Visible = false;
                horizontalLines.Remove(lineToRemove);
                ScreenManager.PersistentLines.Remove(lineToRemove);

            }

            var currentX = leftmost;

            foreach(var line in horizontalLines)
            {
                line.SetFromAbsoluteEndpoints(
                    new Vector3(currentX, 1_000_000, 0),
                    new Vector3(currentX, -1_000_000, 0)
                    );
                currentX += spacing;
            }
        }
    }
}
