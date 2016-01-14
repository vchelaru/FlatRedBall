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

        SpriteFrame SpriteFrame
        {
            get;
            set;
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

        // Don't think we ned this because of IVisible
        //bool Visible
        //{
        //    get;
        //    set;
        //}

        IWindow Parent
        {
            get;
            set;
        }

        void Activity(Camera camera);

        void CallRollOff();

        void CallRollOn();

        void CallRollOver();

        void CallClick();

        void CloseWindow();

        bool GetParentVisibility();

        bool IsPointOnWindow(float x, float y);

        void OnDragging();

        void OnResize();

        void OnResizeEnd();

        void OnLosingFocus();

        bool OverlapsWindow(IWindow otherWindow);

        void SetScaleTL(float newScaleX, float newScaleY);

        void SetScaleTL(float newScaleX, float newScaleY, bool keepTopLeftStatic);

        void TestCollision(Cursor cursor);

        void UpdateDependencies();


    }
}
