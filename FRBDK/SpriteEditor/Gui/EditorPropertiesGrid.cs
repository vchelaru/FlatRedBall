using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Gui;
using EditorObjects;

namespace SpriteEditor.Gui
{
    public class EditorPropertiesGrid : PropertyGrid<EditorProperties>
    {
        #region Delegate Methods

        public void ChangePixelSize(Window callingWindow)
        {

        }

        #endregion

        #region Method

        public EditorPropertiesGrid()
            : base(GuiManager.Cursor)
        {
            #region "this" properties

            GuiManager.AddWindow(this);
            SelectedObject = GameData.EditorProperties;
            HasCloseButton = true;

            #endregion

            #region Exclude members

            ExcludeMember("ConstrainDimensions");
            ExcludeMember("CullSpriteGrids");

            #endregion

            UpDown additionalFadeUpDown = GetUIElementForMember("AdditionalFade") as UpDown;
            additionalFadeUpDown.MaxValue = 255;
            additionalFadeUpDown.MinValue = 0;
            additionalFadeUpDown.Sensitivity = 1f;

            UpDown pixelSizeUpDown = GetUIElementForMember("PixelSize") as UpDown;
            pixelSizeUpDown.MinValue = 0;
            pixelSizeUpDown.Sensitivity = .01f;

            Name = "Editor Properties";

            #region Axes

            IncludeMember("WorldAxesDisplayVisible", "Axes");
            SetMemberDisplayName("WorldAxesDisplayVisible", "Visible");

            IncludeMember("WorldAxesColor", "Axes");
            SetMemberDisplayName("WorldAxesColor", "Color");
            #endregion

            #region LineGrid  Properties

            PropertyGrid<LineGrid> lineGridPropertyGrid = new PropertyGrid<LineGrid>(mCursor);
            lineGridPropertyGrid.HasMoveBar = false;
            lineGridPropertyGrid.HasCloseButton = false;
            lineGridPropertyGrid.ExcludeMember("Layer");

            ReplaceMemberUIElement("LineGrid", lineGridPropertyGrid);

            IncludeMember("LineGrid", "LineGrid");
            SetMemberDisplayName("LineGrid", "");

            #endregion
        }

        public void Refresh()
        {
            SelectedObject = GameData.EditorProperties;
        }
        #endregion
    }
}
