using FlatRedBall.AnimationEditorForms.CommandsAndState;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using InputLibrary;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.AnimationEditorForms.Preview
{
    #region FrbShapes Class

    class FrbShapes
    {
        public AxisAlignedRectangleSave Rectangle;
        public CircleSave Circle;


        public void Clear()
        {
            Rectangle = null;
            Circle = null;
        }
    }

    #endregion

    class ShapePreviewManager
    {
        #region Fields/Properties

        Cursor Cursor;
        Keyboard Keyboard;
        FrbShapes ShapesOver;
        FrbShapes ShapesGrabbed;

        SystemManagers SystemManagers;

        LineCircle HighlightCircle;
        LineRectangle HighlightRectangle;

        List<RenderingLibrary.Math.Geometry.LineRectangle> SelectedRectangleMarkers = new List<LineRectangle>();
        List<RenderingLibrary.Math.Geometry.LineCircle> SelectedCircleMarkers = new List<LineCircle>();

        List<RenderingLibrary.Math.Geometry.LineRectangle> frameRectangles = new List<LineRectangle>();
        List<RenderingLibrary.Math.Geometry.LineCircle> frameCircles = new List<LineCircle>();
        float HighlightPadding => 3 / SystemManagers.Renderer.Camera.Zoom;
        float SelectionPadding => 1 / SystemManagers.Renderer.Camera.Zoom;

        #endregion

        public ShapePreviewManager(Cursor cursor, Keyboard keyboard, SystemManagers systemManagers)
        {
            Cursor = cursor;
            Keyboard = keyboard;
            ShapesOver = new FrbShapes();
            ShapesGrabbed = new FrbShapes();

            SystemManagers = systemManagers;

            HighlightCircle = new LineCircle(systemManagers);
            SystemManagers.ShapeManager.Add(HighlightCircle);
            HighlightCircle.Visible = false;

            HighlightRectangle = new LineRectangle(systemManagers);
            SystemManagers.ShapeManager.Add(HighlightRectangle);
            HighlightRectangle.Visible = false;
        }

        public bool Update()
        {
            var shouldUpdate = false;

            FillShapesOver();

            if(Cursor.PrimaryPush && Cursor.IsInWindow)
            {
                DoPushLogic();
            }

            DoHighlightLogic();

            UpdateSelectionVisibilityToSelectedShapes();

            if (Cursor.PrimaryDown)
            {
                DoDownLogic();

                if (ShapesGrabbed.Circle != null || ShapesGrabbed.Rectangle != null)
                {
                    shouldUpdate = true;
                }
            }

            if(Cursor.PrimaryClick)
            {
                DoClickLogic();

                // we'll just spam save, can be smarter later:
                CommandsAndState.AppCommands.Self.SaveCurrentAnimationChainList();
            }

            return shouldUpdate;
        }

        private void UpdateSelectionVisibilityToSelectedShapes()
        {
            var numberOfCirclesNeeded = SelectedState.Self.SelectedCircles.Count;

            while(SelectedCircleMarkers.Count < numberOfCirclesNeeded)
            {
                var circle = new LineCircle(SystemManagers);
                SystemManagers.ShapeManager.Add(circle);
                SelectedCircleMarkers.Add(circle);
            }
            while(SelectedCircleMarkers.Count > numberOfCirclesNeeded)
            {
                var toRemove = SelectedCircleMarkers.Last();
                SelectedCircleMarkers.RemoveAt(SelectedCircleMarkers.Count - 1);
                SystemManagers.ShapeManager.Remove(toRemove);
            }

            for(int i = 0; i < SelectedCircleMarkers.Count; i++)
            {
                var marker = SelectedCircleMarkers[i];
                UpdateVisualCircleToCircleSave(marker, SelectedState.Self.SelectedCircles[i]);
                marker.Radius += SelectionPadding;
            }

            var numberOfRectanglesNeeded = SelectedState.Self.SelectedRectangles.Count;
            while(SelectedRectangleMarkers.Count < numberOfRectanglesNeeded)
            {
                var rectangle = new LineRectangle(SystemManagers);
                SystemManagers.ShapeManager.Add(rectangle);
                SelectedRectangleMarkers.Add(rectangle);
            }
            while(SelectedRectangleMarkers.Count > numberOfRectanglesNeeded)
            {
                var toRemove = SelectedRectangleMarkers.Last();
                SelectedRectangleMarkers.RemoveAt(SelectedRectangleMarkers.Count - 1);
                SystemManagers.ShapeManager.Remove(toRemove);
            }

            for(int i = 0; i < SelectedRectangleMarkers.Count; i++)
            {
                var marker = SelectedRectangleMarkers[i];
                UpdateVisualRectToAarectSave(marker, SelectedState.Self.SelectedRectangles[i], SelectionPadding);
            }
        }



        private void DoHighlightLogic()
        {
            /////////////Early Out////////////////
            if (Cursor.PrimaryDown)
            {
                HighlightRectangle.Visible = false;
                HighlightCircle.Visible = false;
                return;
            }
            ///////////End Early Out////////////////

            var highlightPadding = HighlightPadding;

            HighlightRectangle.Visible = ShapesOver.Rectangle != null;
            HighlightCircle.Visible = ShapesOver.Circle != null;

            if(ShapesOver.Rectangle != null)
            {
                UpdateVisualRectToAarectSave(HighlightRectangle, ShapesOver.Rectangle, highlightPadding);
            }
            if(ShapesOver.Circle != null)
            {
                UpdateVisualCircleToCircleSave(HighlightCircle, ShapesOver.Circle);
                HighlightCircle.Radius += highlightPadding;
            }
        }

        private void FillShapesOver()
        {
            ShapesOver.Clear();

            var shapeCollection = SelectedState.Self.SelectedFrame?.ShapeCollectionSave;

            /////////////////Early Out////////////////
            if (shapeCollection == null || Cursor.IsInWindow == false)
            {
                return;
            }
            ///////////////End Early Out////////////////

            if(Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Space))
            {
                int m = 3;
            }
            
            foreach(var circle in shapeCollection.CircleSaves)
            {
                if(IsOver(circle))
                {
                    ShapesOver.Circle = circle;
                    break;
                }
            }
            foreach(var rectangle in shapeCollection.AxisAlignedRectangleSaves)
            {
                if(IsOver(rectangle))
                {
                    ShapesOver.Rectangle = rectangle;
                    break;
                }
            }
        }

        bool IsOver(CircleSave circle)
        {
            var cursorPosition = new Vector2(Cursor.GetWorldX(SystemManagers), Cursor.GetWorldY(SystemManagers));
            var circlePosition = new Vector2(circle.X, -circle.Y);

            var distance = (cursorPosition - circlePosition).Length();

            return distance < circle.Radius;
        }

        bool IsOver(AxisAlignedRectangleSave rectangle)
        {
            // Not sure why GetWorldY is positive while going up but...not going to worry about that for now
            var cursorPosition = new Vector2(Cursor.GetWorldX(SystemManagers), Cursor.GetWorldY(SystemManagers));

            return
                cursorPosition.X <= rectangle.X + rectangle.ScaleX &&
                cursorPosition.X >= rectangle.X - rectangle.ScaleX &&
                cursorPosition.Y <= -rectangle.Y + rectangle.ScaleY &&
                cursorPosition.Y >= -rectangle.Y - rectangle.ScaleY;
        }

        private void DoPushLogic()
        {
            ShapesGrabbed.Circle = ShapesOver.Circle;
            ShapesGrabbed.Rectangle = ShapesOver.Rectangle;

            if (ShapesGrabbed.Circle != null)
            {
                SelectedState.Self.SelectedCircle = ShapesGrabbed.Circle;
            }
            
            if (ShapesGrabbed.Rectangle != null)
            {
                SelectedState.Self.SelectedRectangle = ShapesGrabbed.Rectangle;
            }
        }

        private void DoDownLogic()
        {
            var xChange = Cursor.XChange / SystemManagers.Renderer.Camera.Zoom;
            var yChange = -Cursor.YChange / SystemManagers.Renderer.Camera.Zoom;
            if (ShapesGrabbed.Circle != null)
            {
                ShapesGrabbed.Circle.X += xChange;
                ShapesGrabbed.Circle.Y += yChange;
            }

            if(ShapesGrabbed.Rectangle != null)
            {
                ShapesGrabbed.Rectangle.X += xChange;
                ShapesGrabbed.Rectangle.Y += yChange;
            }
        }

        private void DoClickLogic()
        {

        }

        public void UpdateShapesToFrame(AnimationFrameSave frame)
        {
            if (frame?.ShapeCollectionSave != null)
            {
                foreach (var frameAarectSave in frame.ShapeCollectionSave.AxisAlignedRectangleSaves)
                {
                    LineRectangle rectangle = null;

                    rectangle = frameRectangles.FirstOrDefault(possibleRectangle => possibleRectangle.Tag == frameAarectSave);

                    if (rectangle == null)
                    {
                        rectangle = new RenderingLibrary.Math.Geometry.LineRectangle(SystemManagers);
                        rectangle.IsDotted = false;
                        rectangle.Tag = frameAarectSave;
                        SystemManagers.ShapeManager.Add(rectangle);
                        frameRectangles.Add(rectangle);
                    }

                    UpdateVisualRectToAarectSave(rectangle, frameAarectSave, 0);
                }

                foreach (var frameCircleSave in frame.ShapeCollectionSave.CircleSaves)
                {
                    LineCircle circle = null;

                    circle = frameCircles.FirstOrDefault(possibleCircle => possibleCircle.Tag == frameCircleSave);

                    if (circle == null)
                    {
                        circle = new LineCircle(SystemManagers);
                        circle.Tag = frameCircleSave;
                        SystemManagers.ShapeManager.Add(circle);
                        frameCircles.Add(circle);
                    }

                    circle.Radius = frameCircleSave.Radius;
                    circle.X = frameCircleSave.X;// - frameCircleSave.Radius/2.0f;
                    circle.Y = -frameCircleSave.Y;// - frameCircleSave.Radius / 2.0f;
                }

                for (int i = frameRectangles.Count - 1; i > -1; i--)
                {
                    var frameRectangle = frameRectangles[i];

                    var tag = frameRectangle.Tag;

                    var isReferencedByCurrentFrame = false;

                    if (tag is AxisAlignedRectangleSave tagAsRectangle)
                    {
                        isReferencedByCurrentFrame = frame.ShapeCollectionSave.AxisAlignedRectangleSaves
                            .Contains(tagAsRectangle);
                    }

                    if (!isReferencedByCurrentFrame)
                    {
                        SystemManagers.ShapeManager.Remove(frameRectangle);
                        frameRectangles.RemoveAt(i);
                    }
                }

                for (int i = frameCircles.Count - 1; i > -1; i--)
                {
                    var frameCircle = frameCircles[i];

                    var tag = frameCircle.Tag;

                    var isReferencedByCurrentFrame = false;

                    if (tag is CircleSave tagAsCircle)
                    {
                        isReferencedByCurrentFrame = frame.ShapeCollectionSave.CircleSaves
                            .Contains(tagAsCircle);
                    }

                    if (!isReferencedByCurrentFrame)
                    {
                        SystemManagers.ShapeManager.Remove(frameCircle);
                        frameCircles.RemoveAt(i);
                    }
                }
            }
            else
            {
                for (int i = 0; i < frameRectangles.Count; i++)
                {
                    var rectangle = frameRectangles[i];
                    SystemManagers.ShapeManager.Remove(rectangle);
                }

                for (int i = 0; i < frameCircles.Count; i++)
                {
                    var circle = frameCircles[i];
                    SystemManagers.ShapeManager.Remove(circle);
                }

                frameRectangles.Clear();
                frameCircles.Clear();
            }
        }

        private static void UpdateVisualRectToAarectSave(LineRectangle rectangle, AxisAlignedRectangleSave frameAarectSave, float padding)
        {
            rectangle.Width = frameAarectSave.ScaleX * 2 + padding*2;
            rectangle.Height = frameAarectSave.ScaleY * 2 + padding * 2;
            rectangle.X = frameAarectSave.X - frameAarectSave.ScaleX - padding;
            rectangle.Y = -frameAarectSave.Y - frameAarectSave.ScaleY - padding;
        }

        private static void UpdateVisualCircleToCircleSave(LineCircle circle, CircleSave frameCircleSave)
        {
            circle.X = frameCircleSave.X;
            circle.Y = -frameCircleSave.Y;
            circle.Radius = frameCircleSave.Radius;
        }
    }
}
