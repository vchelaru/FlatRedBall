using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using NUnit.Framework;
using FlatRedBall;
using FlatRedBall.Graphics;

namespace NonGraphicalTests.Gui
{

    public class IWindowImplementation : IWindow, IVisible
    {
        public event WindowEvent Click;
        public event WindowEvent ClickNoSlide;
        public event WindowEvent SlideOnClick;
        public event WindowEvent Push;
        public event WindowEvent DragOver;
        public event WindowEvent EnabledChange;

        public event WindowEvent RollOn;

        public event WindowEvent RollOff;

        public event WindowEvent RollOver;
        public System.Collections.ObjectModel.ReadOnlyCollection<IWindow> Children
        {
            get { throw new NotImplementedException(); }
        }



        public bool Enabled
        {
            get
            {
                return true;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool MovesWhenGrabbed
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool GuiManagerDrawn
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool IgnoredByCursor
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public System.Collections.ObjectModel.ReadOnlyCollection<IWindow> FloatingChildren
        {
            get { throw new NotImplementedException(); }
        }

        public FlatRedBall.ManagedSpriteGroups.SpriteFrame SpriteFrame
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float WorldUnitX
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float WorldUnitY
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float WorldUnitRelativeX
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float WorldUnitRelativeY
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float ScaleX
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float ScaleY
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float X
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float Y
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public float Z
        {
            get { throw new NotImplementedException(); }
        }

        public bool Visible
        {
            get
            {
                return true;
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public IWindow Parent
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }



        public void Activity(FlatRedBall.Camera camera)
        {

        }

        public void CallRemovedAsPushedWindow() { }
        public void RemovedAsPushedWindow() { }

        public void CallRollOff()
        {
            throw new NotImplementedException();
        }

        public void CallRollOn()
        {
            if (RollOn != null)
            {
                RollOn(this);
            }
        }

        public void CallRollOver()
        {
            if(RollOver != null)
            {
                RollOver(this);
            }

        }

        public void CallClick()
        {
            throw new NotImplementedException();
        }

        public void CloseWindow()
        {
            throw new NotImplementedException();
        }

        public bool GetParentVisibility()
        {
            throw new NotImplementedException();
        }

        public bool IsPointOnWindow(float x, float y)
        {
            throw new NotImplementedException();
        }

        public void OnDragging()
        {
            throw new NotImplementedException();
        }

        public void OnResize()
        {
            throw new NotImplementedException();
        }

        public void OnResizeEnd()
        {
            throw new NotImplementedException();
        }

        public void OnLosingFocus()
        {
            throw new NotImplementedException();
        }

        public bool OverlapsWindow(IWindow otherWindow)
        {
            throw new NotImplementedException();
        }

        public void SetScaleTL(float newScaleX, float newScaleY)
        {
            throw new NotImplementedException();
        }

        public void SetScaleTL(float newScaleX, float newScaleY, bool keepTopLeftStatic)
        {
            throw new NotImplementedException();
        }

        public void TestCollision(Cursor cursor)
        {
            if (cursor.ScreenX == 0)
            {
                cursor.WindowOver = this;
            }
        }

        public void UpdateDependencies()
        {
            

        }

        public string Name
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public bool HasCursorOver(Cursor cursor)
        {
            return cursor.ScreenX == 0;
        }

        public FlatRedBall.Graphics.Layer Layer
        {
            get { throw new NotImplementedException(); }
        }


        IVisible IVisible.Parent
        {
            get { throw new NotImplementedException(); }
        }

        public bool AbsoluteVisible
        {
            get { throw new NotImplementedException(); }
        }

        public bool IgnoresParentVisibility
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }
    }

    [TestFixture]
    public class IWindowTests
    {
        [TestFixtureSetUp]
        public void Initialize()
        {
            SpriteManager.Camera.UsePixelCoordinates();
        }

        [Test]
        public void TestEvents()
        {
            IWindowImplementation iwi = new IWindowImplementation();
            GuiManager.AddWindow(iwi);
            GuiManager.Cursor.UsingMouse = false;

            Microsoft.Xna.Framework.GameTime gameTime = new Microsoft.Xna.Framework.GameTime(
                new TimeSpan(0, 0, 0, 0, 1), new TimeSpan(0, 0, 0, 0, 1));

            FlatRedBallServices.Update(gameTime);

            if (GuiManager.Cursor.WindowOver != iwi)
            {
                throw new Exception("The cursor should be over the window");
            }
        }



    }
}
