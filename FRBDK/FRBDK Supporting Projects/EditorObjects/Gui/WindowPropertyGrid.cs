using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Graphics.Animation;

namespace EditorObjects.Gui
{
    public class WindowPropertyGrid<T> : PropertyGrid<T> where T : Window
    {
        public WindowPropertyGrid(Cursor cursor)
            : base(cursor)
        {
            ExcludeMembersInType(typeof(IAnimationChainAnimatable));

            ExcludeMember("ScaleXVelocity");
            ExcludeMember("ScaleYVelocity");

            ExcludeMember("WorldUnitX");
            ExcludeMember("WorldUnitY");
            ExcludeMember("WorldUnitRelativeX");
            ExcludeMember("WorldUnitRelativeY");

            ExcludeMember("ScreenRelativeX");
            ExcludeMember("ScreenRelativeY");

            ExcludeMember("SpriteFrame");

            ExcludeMember("BaseTexture");
            ExcludeMember("Alpha");

            ExcludeMember("MoveBarHeight");
            ExcludeMember("KeepWindowsInScreen");

            ExcludeMember("AbsoluteWorldUnitZ");
            ExcludeMember("GuiManagerDrawn");

            ExcludeMember("VisibleSettingIgnoringParent");
            ExcludeMember("IgnoredByCursor");

            ExcludeMember("FloatingChildren");
            ExcludeMember("Children");

            ExcludeMember("IsWindowOrChildrenReceivingInput");
            ExcludeMember("DrawBorders");
        }

    }
}
