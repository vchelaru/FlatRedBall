using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math;
using System.Collections.ObjectModel;
using FlatRedBall.Utilities;
using FlatRedBall.Graphics;

namespace FlatRedBall.Gui
{

    public interface IWindow : INameable, IClickable, ILayered, IVisible
    {

        #region Events

        /// <summary>
        /// An event raised when the window is clicked (the user pushes and releases on the window)
        /// </summary>
        event WindowEvent Click;
        /// <summary>
        /// An event raised when the window is pushed and released without moving.
        /// </summary>
        event WindowEvent ClickNoSlide;
        /// <summary>
        /// An event raised when the cursor is pushed when not on the window, but then released when it is on the window. 
        /// This can be used as a drag+drop event.
        /// </summary>
        event WindowEvent SlideOnClick;
        /// <summary>
        /// An event raised when the cursor is pushed (not down last frame, is on this frame)
        /// </summary>
        event WindowEvent Push;
        /// <summary>
        /// Event raied when the cursor moves and this window is the pressed window. This is called whether
        /// the cursor is over the current window or not.
        /// </summary>
        event WindowEvent DragOver;

        /// <summary>
        /// An event raised when the cursor moves onto the window for the first time. 
        /// This is raised only once until the user moves the cursor off and then back on.
        /// </summary>
        event WindowEvent RollOn;
        /// <summary>
        /// An event raised when the cursor moves off of the window 
        /// </summary>
        event WindowEvent RollOff;
        /// <summary>
        /// An event raised when the cursor moves while it is over the window. This is raised every frame that
        /// the cursor moves and is over the window. This event is not raised unless the cursor is over the window.
        /// </summary>
        /// <seealso cref="DragOver"/>
        event WindowEvent RollOver;
        /// <summary>
        /// Event raised when the Enabled property changes on the window.
        /// </summary>
        event WindowEvent EnabledChange;

        /// <summary>
        /// Event raised when this Window is pushed, then is no longer the pushed window due to a cursor releasing the primary button.
        /// </summary>
        event WindowEvent RemovedAsPushedWindow;
        #endregion

        ReadOnlyCollection<IWindow> Children
        {
            get;
        }

        bool Enabled
        {
            get;
            set;
        }

        bool MovesWhenGrabbed
        {
            set;
            get;
        }

        bool GuiManagerDrawn
        {
            get;
            set;
        }

        bool IgnoredByCursor
        {
            get;
            set;
        }

        ReadOnlyCollection<IWindow> FloatingChildren
        {
            get;
        }


        float WorldUnitX
        {
            get;
            set;
        }

        float WorldUnitY
        {
            get;
            set;
        }

        float WorldUnitRelativeX
        {
            get;
            set;
        }

        float WorldUnitRelativeY
        {
            get;
            set;
        }


        float ScaleX
        {
            get;
            set;
        }

        float ScaleY
        {
            get;
            set;
        }

        float X
        {
            get;
            set;
        }

        float Y
        {
            get;
            set;
        }

        float Z
        {
            get;
        }

        new IWindow Parent
        {
            get;
            set;
        }

        void Activity(Camera camera);

        void CallRollOff();

        void CallRollOn();

        void CallRollOver();

        void CallClick();

        void CallRemovedAsPushedWindow();

        void CloseWindow();

        bool GetParentVisibility();

        bool IsPointOnWindow(float x, float y);

        void OnDragging();

        void OnResize();

        void OnResizeEnd();

        void OnLosingFocus();

        bool OverlapsWindow(IWindow otherWindow);

        void TestCollision(Cursor cursor);

        void UpdateDependencies();


    }


    public static class IWindowExtensions
    {
        public static IWindow GetParentRoot(this IWindow window)
        {
            if(window.Parent == null)
            {
                return window;
            }
            else
            {
                return window.Parent.GetParentRoot();
            }
        }

        public static bool IsInParentChain(this IWindow window, IWindow possibleParent)
        {
            if(window.Parent == possibleParent)
            {
                return true;
            }
            else if(window.Parent != null)
            {
                return window.Parent.IsInParentChain(possibleParent);
            }
            else
            {
                return false;
            }
        }
    }
}
