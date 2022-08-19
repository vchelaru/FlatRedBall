using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Editing.Visuals
{
    public class Arrow
    {
        #region Fields/Properties

        Line MainLine;

        List<Line> FirstArrow = new List<Line>();
        List<Line> SecondArrow = new List<Line>();

        public bool Visible
        {
            get => MainLine.Visible;
            set
            {
                MainLine.Visible = value;
                foreach (var line in FirstArrow)
                {
                    line.Visible = value;
                }
                foreach (var line in SecondArrow)
                {
                    line.Visible = value;
                }
            }
        }

        public Color Color
        {
            get => MainLine.Color;
            set
            {
                MainLine.Color = value;
                foreach (var line in FirstArrow)
                {
                    line.Color = value;
                }
                foreach (var line in SecondArrow)
                {
                    line.Color = value;
                }
            }
        }

        #endregion

        public Arrow(FlatRedBall.Graphics.Layer layer, bool firstArrow = false, bool secondArrow = true)
        {
            MainLine = new Line();

            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == false)
            {
                ShapeManager.AddToLayer(MainLine, layer);
                if (firstArrow)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var arrowLine = new Line();
                        ShapeManager.AddToLayer(arrowLine, layer);
                        FirstArrow.Add(arrowLine);

                    }

                }
                if (secondArrow)
                {
                    for (int i = 0; i < 3; i++)
                    {
                        var arrowLine = new Line();
                        ShapeManager.AddToLayer(arrowLine, layer);
                        SecondArrow.Add(arrowLine);

                    }
                }
            }
        }


        public void SetFromAbsoluteEndpoints(Vector3 first, Vector3 second)
        {
            var mainFirst = first;
            var mainSecond = second;

            var angle = (second - first).Angle() ?? 0;


            var pixelsPerUnit = FlatRedBall.Camera.Main.PixelsPerUnitAt((first.Z + second.Z) / 2.0f);

            float ArrowSizeX = 5 / pixelsPerUnit;
            float ArrowSizeY = ArrowSizeX;

            if (FirstArrow.Count > 0)
            {
                mainFirst += (Vector3.Right * ArrowSizeX).RotatedBy(angle);
                var firstLineOffset = new Vector3(ArrowSizeX, 5, 0).RotatedBy(angle);
                FirstArrow[0].SetFromAbsoluteEndpoints(first, first + firstLineOffset);
                var secondLineOffset = new Vector3(ArrowSizeX, -5, 0).RotatedBy(angle);
                FirstArrow[1].SetFromAbsoluteEndpoints(first, first + secondLineOffset);

                FirstArrow[2].SetFromAbsoluteEndpoints(
                    FirstArrow[0].AbsolutePoint2,
                    FirstArrow[1].AbsolutePoint2);
            }
            if (SecondArrow.Count > 0)
            {
                mainSecond += (Vector3.Left * ArrowSizeX).RotatedBy(angle);

                var firstLineOffset = new Vector3(-ArrowSizeX, ArrowSizeY, 0).RotatedBy(angle);
                SecondArrow[0].SetFromAbsoluteEndpoints(second, second + firstLineOffset);

                var secondLineOffset = new Vector3(-ArrowSizeX, -ArrowSizeY, 0).RotatedBy(angle);
                SecondArrow[1].SetFromAbsoluteEndpoints(second, second + secondLineOffset);

                SecondArrow[2].SetFromAbsoluteEndpoints(
                    SecondArrow[0].AbsolutePoint2,
                    SecondArrow[1].AbsolutePoint2);
            }

            MainLine.SetFromAbsoluteEndpoints(mainFirst, mainSecond);
        }


        public void Destroy()
        {
            ShapeManager.Remove(MainLine);

            foreach (var line in FirstArrow)
            {
                ShapeManager.Remove(line);
            }
            foreach (var line in SecondArrow)
            {
                ShapeManager.Remove(line);
            }
        }
    }
}
