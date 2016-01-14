using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;

namespace EditorObjects.Gui
{
    public class GuiSkinPropertyGrid : PropertyGrid<GuiSkin>
    {
        #region Methods

        #region Constructor

        public GuiSkinPropertyGrid()
            : base(GuiManager.Cursor)
        {
            GuiManager.AddWindow(this);

            #region Modify the Button Properties

            IncludeMember("ButtonSkin", "Button");

            PropertyGrid<ButtonSkin> buttonSkinPropertyGrid = new PropertyGrid<ButtonSkin>(this.mCursor);
            buttonSkinPropertyGrid.HasMoveBar = false;
            ReplaceMemberUIElement("ButtonSkin", buttonSkinPropertyGrid);
            SetMemberDisplayName("ButtonSkin", "Up");
            ExcludeMoveBarMembersFrom(buttonSkinPropertyGrid);

            IncludeMember("ButtonDownSkin", "Button");

            PropertyGrid<ButtonSkin> buttonDownSkinPropertyGrid = new PropertyGrid<ButtonSkin>(this.mCursor);
            buttonDownSkinPropertyGrid.HasMoveBar = false;
            ReplaceMemberUIElement("ButtonDownSkin", buttonDownSkinPropertyGrid);
            SetMemberDisplayName("ButtonDownSkin", "Down");
            ExcludeMoveBarMembersFrom(buttonDownSkinPropertyGrid);

            #endregion

            #region Modify the TextBoxSkin Property
            IncludeMember("TextBoxSkin", "TextBox");

            PropertyGrid<TextBoxSkin> textBoxSkinPropertyGrid = new PropertyGrid<TextBoxSkin>(this.mCursor);
            textBoxSkinPropertyGrid.HasMoveBar = false;
            ReplaceMemberUIElement("TextBoxSkin", textBoxSkinPropertyGrid);
            SetMemberDisplayName("TextBoxSkin", "");
            ExcludeMoveBarMembersFrom(textBoxSkinPropertyGrid);

            #endregion

            #region Modify the WindowSkin Property
            IncludeMember("WindowSkin", "Window");

            PropertyGrid<WindowSkin> windowSkinPropertyGrid = new PropertyGrid<WindowSkin>(this.mCursor);
            windowSkinPropertyGrid.HasMoveBar = false;
            ReplaceMemberUIElement("WindowSkin", windowSkinPropertyGrid);
            SetMemberDisplayName("WindowSkin", "");

            #endregion

            #region Modify the ListBoxSkin Property

            IncludeMember("ListBoxSkin", "ListBox");
            ListBoxSkinPropertyGrid listBoxSkinPropertyGrid = new ListBoxSkinPropertyGrid(this.mCursor);
            listBoxSkinPropertyGrid.HasMoveBar = false;
            ReplaceMemberUIElement("ListBoxSkin", listBoxSkinPropertyGrid);
            SetMemberDisplayName("ListBoxSkin", "");

            #endregion

            #region Modify the ScrollBarSkin Property

            IncludeMember("ScrollBarSkin", "ScrollBar");

            PropertyGrid<ScrollBarSkin> scrollBarSkinPropertyGrid = new PropertyGrid<ScrollBarSkin>(mCursor);
            scrollBarSkinPropertyGrid.HasMoveBar = false;
            ReplaceMemberUIElement("ScrollBarSkin", scrollBarSkinPropertyGrid);
            SetMemberDisplayName("ScrollBarSkin", "");
            ExcludeMoveBarMembersFrom(scrollBarSkinPropertyGrid);

            #endregion

            RemoveCategory("Uncategorized");
        }

        #endregion


        internal static void ExcludeMoveBarMembersFrom(PropertyGrid propertyGrid)
        {
            propertyGrid.ExcludeMember("MoveBarTexture");
            propertyGrid.ExcludeMember("MoveBarSpriteBorderWidth");
            propertyGrid.ExcludeMember("MoveBarTextureBorderWidth");
            propertyGrid.ExcludeMember("MoveBarBorderSides");
        }

        #endregion

    }
}
