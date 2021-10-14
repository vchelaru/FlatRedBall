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
using FlatRedBall.Graphics.Animation;

namespace GlueControl.Editing
{
    public class EditorVisuals : FlatRedBall.Managers.IManager
    {
        #region Fields/Properties

        static int nextLine = 0;
        static List<Line> Lines = new List<Line>();

        static int nextText = 0;
        static List<Text> Texts = new List<Text>();

        static int nextArrow = 0;
        static List<Arrow> Arrows = new List<Arrow>();

        static int nextSprite = 0;
        static List<Sprite> Sprites = new List<Sprite>();

        static int nextRectangle = 0;
        static List<AxisAlignedRectangle> Rectangles = new List<AxisAlignedRectangle>();

        static double lastFrameReset;

        #endregion

        static EditorVisuals()
        {
            FlatRedBallServices.AddManager(new EditorVisuals());
        }


        public static void Text(string text, Vector3 position, Color? color = null)
        {
            Color textColor = color ?? Color.White;

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
            textInstance.SetPixelPerfectScale(Camera.Main);
            textInstance.Red = textColor.R / 255.0f;
            textInstance.Green = textColor.G / 255.0f;
            textInstance.Blue = textColor.B / 255.0f;
            nextText++;
        }

        public static void Line(Vector3 point1, Vector3 point2, Color? color = null)
        {
            Color lineColor = color ?? Color.White;

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
            lineInstance.Color = lineColor;
            nextLine++;
        }

        public static void Arrow(Vector3 point1, Vector3 point2, Color? color = null)
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
            arrowInstance.Color = color ?? Color.White;
            nextArrow++;
        }

        public static void Sprite(AnimationChain animationChain, Vector3 position, float textureScale = 1)
        {
            // This screen is cleaning up, so don't make anymore objects:
            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                return;
            }

            TryResetEveryFrameValues();

            if (nextSprite == Sprites.Count)
            {
                Sprites.Add(SpriteManager.AddSprite(animationChain));
            }

            var sprite = Sprites[nextSprite];
            sprite.Name = $"EditorVisuals Sprite {nextSprite}";
            sprite.Visible = true;
            sprite.SetAnimationChain(animationChain);
            sprite.Position = position;
            sprite.TextureScale = textureScale;

            nextSprite++;

        }

        public static void Rectangle(float width, float height, Vector3 centerPosition, Color? color = null)
        {
            Color rectColor = color ?? Color.White;

            // This screen is cleaning up, so don't make anymore objects:
            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                return;
            }

            TryResetEveryFrameValues();

            if (nextRectangle == Rectangles.Count)
            {
                Rectangles.Add(ShapeManager.AddAxisAlignedRectangle());
            }

            var rectangle = Rectangles[nextRectangle];
            rectangle.Name = $"EditorVisuals Rectangle {nextRectangle}";
            rectangle.Visible = true;
            rectangle.Width = width;
            rectangle.Height = height;
            rectangle.Position = centerPosition;
            rectangle.Color = rectColor;
            nextRectangle++;
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
                foreach (var sprite in Sprites)
                {
                    sprite.Visible = false;
                }
                foreach (var rectangle in Rectangles)
                {
                    rectangle.Visible = false;
                }
                nextText = 0;
                nextArrow = 0;
                nextLine = 0;
                nextSprite = 0;
                nextRectangle = 0;
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

            foreach (var sprite in Sprites)
            {
                SpriteManager.RemoveSprite(sprite);
            }
            Sprites.Clear();

            foreach (var rectangle in Rectangles)
            {
                ShapeManager.Remove(rectangle);
            }
            Rectangles.Clear();
        }

        public void Update()
        {
            TryResetEveryFrameValues();
        }

        public void UpdateDependencies()
        {
        }
    }
}
