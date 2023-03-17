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
using FlatRedBall.Math.Paths;

namespace GlueControl.Editing
{
    /// <summary>
    /// Object used to create immediate-mode graphics (fire and forget). This can be used to display debug information
    /// when the game is running or markers and other indicators in edit mode. All calls must be made every frame for the objects
    /// to appear.
    /// </summary>
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

        static int nextCircle = 0;
        static List<Circle> Circles = new List<Circle>();

        static int nextPolygon = 0;
        static List<Polygon> Polygons = new List<Polygon>();

        static double lastFrameReset;

        public static Layer DefaultLayer { get; set; }

        #endregion

        static EditorVisuals()
        {
            FlatRedBallServices.AddManager(new EditorVisuals());
            DefaultLayer = SpriteManager.TopLayer;
        }

        #region Draw specific shapes
        public static Text Text(string text, Vector3 position, Color? color = null)
        {
            if (position.Z == Camera.Main.Z)
            {
                position.Z = 0;
            }
            Color textColor = color ?? Color.White;

            // This screen is cleaning up, so don't make anymore objects:
            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                return new FlatRedBall.Graphics.Text();
            }
            TryResetEveryFrameValues();

            while (nextText >= Texts.Count)
            {
                Texts.Add(TextManager.AddText(String.Empty, DefaultLayer));
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
            return textInstance;
        }

        public static Line Line(Vector3 point1, Vector3 point2, Color? color = null)
        {
            if (point1.Z == Camera.Main.Z)
            {
                point1.Z = 0;
            }
            if (point2.Z == Camera.Main.Z)
            {
                point2.Z = 0;
            }
            Color lineColor = color ?? Color.White;

            // This screen is cleaning up, so don't make anymore objects:
            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                var tempLine = new Line();
                tempLine.Name = "Temp line returned when screen is transitioning";
                return tempLine;
            }
            TryResetEveryFrameValues();

            while (nextLine >= Lines.Count)
            {
                var line = new Line();
                ShapeManager.AddToLayer(line, DefaultLayer);
                Lines.Add(line);
            }

            var lineInstance = Lines[nextLine];
            lineInstance.Name = $"EditorVisuals Line {nextLine}";
            lineInstance.Visible = true;
            lineInstance.SetFromAbsoluteEndpoints(point1, point2);
            lineInstance.Color = lineColor;
            nextLine++;

            return lineInstance;
        }

        public static void LinePath(List<Vector3> points, Color? color = null)
        {
            for (int i = 0; i < points.Count - 1; i++)
            {
                Line(points[i], points[i + 1], color);
            }
        }

        public static Arrow Arrow(Vector3 point1, Vector3 point2, Color? color = null)
        {
            if (point1.Z == Camera.Main.Z)
            {
                point1.Z = 0;
            }
            if (point2.Z == Camera.Main.Z)
            {
                point2.Z = 0;
            }

            // This screen is cleaning up, so don't make anymore objects:
            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                return new Arrow(DefaultLayer);
            }
            TryResetEveryFrameValues();

            while (nextArrow >= Arrows.Count)
            {
                Arrows.Add(new Visuals.Arrow(DefaultLayer));
            }


            var arrowInstance = Arrows[nextArrow];
            //arrowInstance.Name = $"EditorVisuals Line {nextLine}";
            arrowInstance.Visible = true;
            arrowInstance.SetFromAbsoluteEndpoints(point1, point2);
            arrowInstance.Color = color ?? Color.White;
            nextArrow++;
            return arrowInstance;
        }

        public static Sprite Sprite(AnimationChain animationChain, Vector3 position, float textureScale = 1)
        {
            if (position.Z == Camera.Main.Z)
            {
                position.Z = 0;
            }

            // This screen is cleaning up, so don't make anymore objects:
            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                return new Sprite();
            }

            TryResetEveryFrameValues();

            if (nextSprite == Sprites.Count)
            {
                var newSprite = SpriteManager.AddSprite(animationChain);
                SpriteManager.AddToLayer(newSprite, DefaultLayer);
                Sprites.Add(newSprite);
            }

            var sprite = Sprites[nextSprite];
            sprite.Name = $"EditorVisuals Sprite {nextSprite}";
            sprite.Visible = true;
            sprite.SetAnimationChain(animationChain);
            sprite.Position = position;
            sprite.TextureScale = textureScale;

            nextSprite++;

            return sprite;
        }

        public static Sprite ColoredRectangle(float width, float height, Vector3 centerPosition, Color? color = null)
        {
            var sprite = Sprite(null, centerPosition, textureScale: -1);
            sprite.Width = width;
            sprite.Height = height;
            sprite.ColorOperation = ColorOperation.Color;

            var effectiveColor = color ?? new Color(255, 0, 0, 100);

            sprite.Red = effectiveColor.R / 255.0f;
            sprite.Green = effectiveColor.G / 255.0f;
            sprite.Blue = effectiveColor.B / 255.0f;
            sprite.Alpha = effectiveColor.A / 255.0f;

            return sprite;
        }

        public static AxisAlignedRectangle Rectangle(float width, float height, Vector3 centerPosition, Color? color = null)
        {
            if (centerPosition.Z == Camera.Main.Z)
            {
                centerPosition.Z = 0;
            }

            Color rectColor = color ?? Color.White;

            // This screen is cleaning up, so don't make anymore objects:
            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                return new AxisAlignedRectangle();
            }

            TryResetEveryFrameValues();

            if (nextRectangle == Rectangles.Count)
            {
                var newRectangle = new AxisAlignedRectangle();
                ShapeManager.AddToLayer(newRectangle, DefaultLayer);
                Rectangles.Add(newRectangle);
            }

            var rectangle = Rectangles[nextRectangle];
            rectangle.Name = $"EditorVisuals Rectangle {nextRectangle}";
            rectangle.Visible = true;
            rectangle.Width = width;
            rectangle.Height = height;
            rectangle.Position = centerPosition;
            rectangle.Color = rectColor;
            nextRectangle++;

            return rectangle;
        }

        public static Circle Circle(float radius, Vector3 position)
        {
            if (position.Z == Camera.Main.Z)
            {
                position.Z = 0;
            }

            Color color = Color.White;

            // This screen is cleaning up, so don't make anymore objects:
            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                return new FlatRedBall.Math.Geometry.Circle();
            }

            TryResetEveryFrameValues();

            if (nextCircle == Circles.Count)
            {
                var newCircle = new Circle();
                ShapeManager.AddToLayer(newCircle, DefaultLayer);
                Circles.Add(newCircle);
            }

            var circle = Circles[nextCircle];
            circle.Name = $"EditorVisuals Circle {nextCircle}";
            circle.Visible = true;
            circle.Radius = radius;
            circle.Position = position;
            circle.Color = color;
            nextCircle++;

            return circle;
        }

        public static Polygon Polygon(Vector3 centerPosition, Color? color = null)
        {
            if (centerPosition.Z == Camera.Main.Z)
            {
                centerPosition.Z = 0;
            }

            Color polygonColor = color ?? Color.White;

            // This screen is cleaning up, so don't make anymore objects:
            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                return new Polygon();
            }

            TryResetEveryFrameValues();

            if (nextPolygon == Polygons.Count)
            {
                var newPolygon = new Polygon();
                ShapeManager.AddToLayer(newPolygon, DefaultLayer);
                Polygons.Add(newPolygon);
            }

            var polygon = Polygons[nextPolygon];
            polygon.Name = $"EditorVisuals Polygon {nextPolygon}";
            polygon.Visible = true;
            polygon.Position = centerPosition;
            polygon.Color = polygonColor;
            nextPolygon++;

            return polygon;
        }

        public static Polygon DrawPath(Path path, bool includeOffsetArrow = false)
        {

            var pathPolygon = Polygon(Vector3.Zero);


            if (path != null && path.TotalLength > 0)
            {
                var points = GetPoints(path, flipHorizontally: false);
                pathPolygon.Points = points;
            }

            if (includeOffsetArrow && pathPolygon.Points.Count > 0 && (pathPolygon.Points[0].X != 0 || pathPolygon.Points[0].Y != 0))
            {
                Arrow(new Vector3(), pathPolygon.Points[0].ToVector3()).Color = Color.Yellow;
            }

            return pathPolygon;
        }

        #endregion

        public static void DrawRepositionDirections(AxisAlignedRectangle rectangle)
        {
            if (rectangle.RepositionDirections.HasFlag(RepositionDirections.Up))
            {
                var endpoint = rectangle.Position;
                endpoint.Y += rectangle.Height / 2;
                EditorVisuals.Arrow(rectangle.Position, endpoint);
            }

            if (rectangle.RepositionDirections.HasFlag(RepositionDirections.Down))
            {
                var endpoint = rectangle.Position;
                endpoint.Y += -rectangle.Height / 2;
                EditorVisuals.Arrow(rectangle.Position, endpoint);
            }

            if (rectangle.RepositionDirections.HasFlag(RepositionDirections.Left))
            {
                var endpoint = rectangle.Position;
                endpoint.X += -rectangle.Width / 2;
                EditorVisuals.Arrow(rectangle.Position, endpoint);
            }

            if (rectangle.RepositionDirections.HasFlag(RepositionDirections.Right))
            {
                var endpoint = rectangle.Position;
                endpoint.X += rectangle.Width / 2;
                EditorVisuals.Arrow(rectangle.Position, endpoint);
            }
        }

        public static List<FlatRedBall.Math.Geometry.Point> GetPoints(Path pathInstance, bool flipHorizontally)
        {
            var points = new List<FlatRedBall.Math.Geometry.Point>();

            const float pointFrequency = 4;
            var pathLength = pathInstance.TotalLength;

            for (float f = 0; f < pathLength; f += pointFrequency)
            {
                var pointAtLength = pathInstance.PointAtLength(f);

                var point = new FlatRedBall.Math.Geometry.Point
                {
                    X = pointAtLength.X,
                    Y = pointAtLength.Y
                };

                if (flipHorizontally)
                {
                    point.X *= -1;
                }

                points.Add(point);
            }

            //pathPolygon.Points = points;
            return points;
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
                foreach (var circle in Circles)
                {
                    circle.Visible = false;
                }
                foreach (var polygon in Polygons)
                {
                    polygon.Visible = false;
                }
                nextText = 0;
                nextArrow = 0;
                nextLine = 0;
                nextSprite = 0;
                nextRectangle = 0;
                nextCircle = 0;
                nextPolygon = 0;
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

            foreach (var circle in Circles)
            {
                ShapeManager.Remove(circle);
            }
            Circles.Clear();

            foreach (var polygon in Polygons)
            {
                ShapeManager.Remove(polygon);
            }
            Polygons.Clear();
        }

        public void Update()
        {
            TryResetEveryFrameValues();

            if (FlatRedBall.Screens.ScreenManager.CurrentScreen?.IsActivityFinished == true)
            {
                DestroyContainedObjects();
            }
        }

        public void UpdateDependencies()
        {
        }
    }
}
