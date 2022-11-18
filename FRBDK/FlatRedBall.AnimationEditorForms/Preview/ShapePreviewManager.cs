using FlatRedBall.AnimationEditorForms.CommandsAndState;
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

    class ShapePreviewManager
    {
        Cursor Cursor;
        Keyboard Keyboard;
        FrbShapes ShapesOver;
        FrbShapes ShapesGrabbed;

        SystemManagers SystemManagers;

        LineCircle HighlightCircle;
        LineRectangle HighlightRectangle;

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

            if(Cursor.PrimaryPush)
            {
                DoPushLogic();
            }

            DoHighlightLogic();

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

            // todo - make this Camera based
            var padding = 2 / SystemManagers.Renderer.Camera.Zoom;


            HighlightRectangle.Visible = ShapesOver.Rectangle != null;
            HighlightCircle.Visible = ShapesOver.Circle != null;

            if(ShapesOver.Rectangle != null)
            {
                HighlightRectangle.X = ShapesOver.Rectangle.X;
                HighlightRectangle.Y = -ShapesOver.Rectangle.Y;
                HighlightRectangle.Width = (ShapesOver.Rectangle.ScaleX*2) + padding;
                HighlightRectangle.Height = (ShapesOver.Rectangle.ScaleY*2) + padding;
            }
            if(ShapesOver.Circle != null)
            {
                HighlightCircle.X = ShapesOver.Circle.X;
                HighlightCircle.Y = -ShapesOver.Circle.Y;
                HighlightCircle.Radius = ShapesOver.Circle.Radius + padding;
            }
        }
        private void FillShapesOver()
        {
            ShapesOver.Clear();

            var shapeCollection = SelectedState.Self.SelectedFrame?.ShapeCollectionSave;

            /////////////////Early Out////////////////
            if (shapeCollection == null)
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
            var cursorPosition = new Vector2(Cursor.GetWorldX(SystemManagers), -Cursor.GetWorldY(SystemManagers));

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
    }
}
