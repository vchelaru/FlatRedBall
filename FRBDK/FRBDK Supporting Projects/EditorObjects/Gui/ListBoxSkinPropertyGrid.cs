using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall;

namespace EditorObjects.Gui
{
    public class ListBoxSkinPropertyGrid : PropertyGrid<ListBoxSkin>
    {
        #region Event Methods

        private void SetPixelPerfectClick(Window callingWindow)
        {
            SelectedObject.TextScale = .5f * SelectedObject.Font.LineHeightInPixels / 
                SpriteManager.Camera.PixelsPerUnitAt(0);
            SelectedObject.TextSpacing = SelectedObject.TextScale;
            //NewLineDistance = Scale * 1.5f;
        }

        #endregion

        #region Methods

        public ListBoxSkinPropertyGrid(Cursor cursor)
            : base(cursor)
        {
            #region Red

            IncludeMember("Red", "Font");
            ((UpDown)GetUIElementForMember("Red")).MinValue = 0;
            ((UpDown)GetUIElementForMember("Red")).MaxValue =
                FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;
            #endregion

            #region Green

            IncludeMember("Green", "Font");
            ((UpDown)GetUIElementForMember("Green")).MinValue = 0;
            ((UpDown)GetUIElementForMember("Green")).MaxValue =
                FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;

            #endregion

            #region Blue

            IncludeMember("Blue", "Font");
            ((UpDown)GetUIElementForMember("Blue")).MinValue = 0;
            ((UpDown)GetUIElementForMember("Blue")).MaxValue =
                FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;

            #endregion

            #region Disabled Red

            IncludeMember("DisabledRed", "Font");
            ((UpDown)GetUIElementForMember("DisabledRed")).MinValue = 0;
            ((UpDown)GetUIElementForMember("DisabledRed")).MaxValue =
                FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;

            #endregion

            #region Disabled Green

            IncludeMember("DisabledGreen", "Font");
            ((UpDown)GetUIElementForMember("DisabledGreen")).MinValue = 0;
            ((UpDown)GetUIElementForMember("DisabledGreen")).MaxValue =
                FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;

            #endregion

            IncludeMember("DisabledBlue", "Font");
            ((UpDown)GetUIElementForMember("DisabledBlue")).MinValue = 0;
            ((UpDown)GetUIElementForMember("DisabledBlue")).MaxValue =
                FlatRedBall.Graphics.GraphicalEnumerations.MaxColorComponentValue;

            IncludeMember("SeparatorSkin", "Separators");
            PropertyGrid<SeparatorSkin> separatorSkinPropertyGrid = new PropertyGrid<SeparatorSkin>(mCursor);
            separatorSkinPropertyGrid.HasMoveBar = false;
            GuiSkinPropertyGrid.ExcludeMoveBarMembersFrom(separatorSkinPropertyGrid);
            ReplaceMemberUIElement("SeparatorSkin", separatorSkinPropertyGrid);
            SetMemberDisplayName("SeparatorSkin", "");

            IncludeMember("HighlightBarSkin", "Highlight");
            PropertyGrid<HighlightBarSkin> highlightSkinPropertyGrid = new PropertyGrid<HighlightBarSkin>(mCursor);
            highlightSkinPropertyGrid.HasMoveBar = false;
            GuiSkinPropertyGrid.ExcludeMoveBarMembersFrom(highlightSkinPropertyGrid);
            ReplaceMemberUIElement("HighlightBarSkin", highlightSkinPropertyGrid);
            SetMemberDisplayName("HighlightBarSkin", "");


            IncludeMember("Font", "Font");
            IncludeMember("TextSpacing", "Font");
            IncludeMember("TextScale", "Font");
            IncludeMember("DistanceBetweenLines", "Font");
            IncludeMember("FirstItemDistanceFromTop", "Font");

            GuiSkinPropertyGrid.ExcludeMoveBarMembersFrom(this);

            Button SetPixelPerfectButton = new Button(mCursor);
            SetPixelPerfectButton.ScaleX = 5;
            SetPixelPerfectButton.ScaleY = 2;
            SetPixelPerfectButton.Text = "Set Pixel\nPerfect";
            SetPixelPerfectButton.Click += SetPixelPerfectClick;
            AddWindow(SetPixelPerfectButton, "Font");
        }

        #endregion
    }
}
