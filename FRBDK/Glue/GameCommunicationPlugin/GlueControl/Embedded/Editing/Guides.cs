{CompilerDirectives}

using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Screens;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GlueControl.Editing
{
    class Guides
    {
        #region Fields/Properties

        Line HorizontalLine;
        Line VerticalLine;


        public float GridSpacing { get; set; } = 32;

        List<Line> verticalLines = new List<Line>();
        List<Line> horizontalLines = new List<Line>();

        Color centerLineColor = new Color(new Microsoft.Xna.Framework.Vector4
            (.5f, .5f, .5f, .5f));

        public Color SmallGridLineColor = new Color(new Microsoft.Xna.Framework.Vector4
            (.15f, .15f, .15f, .15f));
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
                    foreach (var line in horizontalLines)
                    {
                        line.Visible = value;
                    }
                    foreach (var line in verticalLines)
                    {
                        line.Visible = value;
                    }
                }
            }
        }

        #endregion

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

#if SupportsEditMode

            ScreenManager.PersistentLines.Add(HorizontalLine);
            ScreenManager.PersistentLines.Add(VerticalLine);
#endif
        }

        public void RefreshColors()
        {
            for (int i = 0; i < verticalLines.Count; i++)
            {
                var line = verticalLines[i];
                line.Color = SmallGridLineColor;
            }

            for (int i = 0; i < horizontalLines.Count; i++)
            {
                var line = horizontalLines[i];
                line.Color = SmallGridLineColor;
            }
        }


        public void UpdateGridLines()
        {
            ////////Early Out////////////
            if (!visible)
            {
                return;
            }
            /////End Early Out//////////////

            var camera = Camera.Main;
            var effectiveGridSpacing = GridSpacing;
            var visibleAreaWidth = camera.RelativeXEdgeAt(0) * 2;
            var destinationWidth = camera.DestinationRectangle.Width;
            var zoom = visibleAreaWidth / destinationWidth;
            var spacing = effectiveGridSpacing / zoom;
            while (effectiveGridSpacing / zoom < 8)
            {
                effectiveGridSpacing *= 2;
            }

            var leftmost = MathFunctions.RoundFloat(camera.AbsoluteLeftXEdge, effectiveGridSpacing);
            var rightMost = MathFunctions.RoundFloat(camera.AbsoluteRightXEdge, effectiveGridSpacing);


            var numberOfVerticalLines = 1 + MathFunctions.RoundToInt((rightMost - leftmost) / effectiveGridSpacing);

            numberOfVerticalLines = Math.Min(1000, numberOfVerticalLines);


#if SupportsEditMode

            while(verticalLines.Count < numberOfVerticalLines)
            {
                var line = new Line();
                ScreenManager.PersistentLines.Add(line);
                line.Visible = true;
                line.Color = SmallGridLineColor;
                verticalLines.Add(line);
            }
            while(verticalLines.Count > numberOfVerticalLines)
            {
                var lineToRemove = verticalLines.Last();
                lineToRemove.Visible = false;
                verticalLines.Remove(lineToRemove);
                ScreenManager.PersistentLines.Remove(lineToRemove);
            }

            var currentX = leftmost;

            foreach(var line in verticalLines)
            {
                line.SetFromAbsoluteEndpoints(
                    new Vector3(currentX, 1_000_000, 0),
                    new Vector3(currentX, -1_000_000, 0)
                    );
                currentX += effectiveGridSpacing;
            }

            // ---------------------------------------------------------------------------------

            var bottomMost = MathFunctions.RoundFloat(camera.AbsoluteBottomYEdge, effectiveGridSpacing);
            var topmost = MathFunctions.RoundFloat(camera.AbsoluteTopYEdge, effectiveGridSpacing);

            var numberOfHorizontalLines = 1 + MathFunctions.RoundToInt((topmost - bottomMost) / effectiveGridSpacing);

            
            // in case there's some error or the camera zooms out too far
            numberOfHorizontalLines = Math.Min(1000, numberOfHorizontalLines);

            while(horizontalLines.Count < numberOfHorizontalLines)
            {
                var line = new Line();
                ScreenManager.PersistentLines.Add(line);
                line.Visible = true;
                line.Color = SmallGridLineColor;
                horizontalLines.Add(line);
            }
            while(horizontalLines.Count > numberOfHorizontalLines)
            {
                var lineToRemove = horizontalLines.Last();
                lineToRemove.Visible = false;
                horizontalLines.Remove(lineToRemove);
                ScreenManager.PersistentLines.Remove(lineToRemove);
            }

            var currentY = bottomMost;

            foreach(var line in horizontalLines)
            {
                line.SetFromAbsoluteEndpoints(
                    new Vector3(1_000_000, currentY, 0),
                    new Vector3(-1_000_000, currentY, 0)
                    );
                currentY += effectiveGridSpacing;
            }
#endif
        }
    }
}
