using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using GlueControl.Editing.Visuals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Editing
{
    public static class EditorVisuals
    {
        #region Fields/Properties

        static int nextLine = 0;
        static List<Line> Lines = new List<Line>();

        static int nextText = 0;
        static List<Text> Texts = new List<Text>();

        static int nextArrow = 0;
        static List<Arrow> Arrows = new List<Arrow>();

        static double lastFrameReset;

        #endregion

        public static void Text(string text, Vector3 position)
        {
            // This screen is cleaning up, so don't make anymore objects:
            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                return;
            }
            TryResetEveryFrameValues();

            if (nextText == Texts.Count)
            {
                Texts.Add(TextManager.AddText(String.Empty));
            }

            var textInstance = Texts[nextText];
            textInstance.Name = $"EditorVisuals Text {nextText}";
            textInstance.Visible = true;
            textInstance.DisplayText = text;
            textInstance.Position = position;
            textInstance.HorizontalAlignment = HorizontalAlignment.Center;
            textInstance.VerticalAlignment = VerticalAlignment.Center;

            nextText++;
        }

        internal static void Line(Vector3 point1, Vector3 point2)
        {
            // This screen is cleaning up, so don't make anymore objects:
            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                return;
            }
            TryResetEveryFrameValues();

            if (nextLine == Lines.Count)
            {
                Lines.Add(ShapeManager.AddLine());
            }

            var lineInstance = Lines[nextLine];
            lineInstance.Name = $"EditorVisuals Line {nextLine}";
            lineInstance.Visible = true;
            lineInstance.SetFromAbsoluteEndpoints(point1, point2);
            nextLine++;
        }

        internal static void Arrow(Vector3 point1, Vector3 point2)
        {
            // This screen is cleaning up, so don't make anymore objects:
            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                return;
            }
            TryResetEveryFrameValues();

            if (nextArrow == Arrows.Count)
            {
                Arrows.Add(new Visuals.Arrow());
            }


            var arrowInstance = Arrows[nextArrow];
            //arrowInstance.Name = $"EditorVisuals Line {nextLine}";
            arrowInstance.Visible = true;
            arrowInstance.SetFromAbsoluteEndpoints(point1, point2);
            nextArrow++;
        }

        private static void TryResetEveryFrameValues()
        {
            if (lastFrameReset != TimeManager.CurrentTime)
            {
                lastFrameReset = TimeManager.CurrentTime;

                foreach (var text in Texts)
                {
                    text.Visible = false;
                }
                foreach (var line in Lines)
                {
                    line.Visible = false;
                }
                foreach (var arrow in Arrows)
                {
                    arrow.Visible = false;
                }
                nextText = 0;
                nextArrow = 0;
                nextLine = 0;
            }
        }


        public static void DestroyContainedObjects()
        {
            foreach (var line in Lines)
            {
                ShapeManager.Remove(line);
            }
            Lines.Clear();

            foreach (Text text in Texts)
            {
                TextManager.RemoveText(text);
            }
            Texts.Clear();

            foreach (var arrow in Arrows)
            {
                arrow.Destroy();
            }
            Arrows.Clear();

        }
    }
}
