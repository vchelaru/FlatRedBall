using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math;

namespace EditorObjects.Gui
{
    public class LineGridPropertyGrid : PropertyGrid<LineGrid>
    {
        #region Fields
        #endregion

        #region Properties

        #endregion

        #region Event Methods

        #endregion

        #region Methods

        public LineGridPropertyGrid(LineGrid lineGrid)
            : base(GuiManager.Cursor)
        {
            HasCloseButton = true;

            GuiManager.AddWindow(this);

            lineGrid.Visible = false;
            base.SelectedObject = lineGrid;

            IncludeMember("GridColor", "Color");
            IncludeMember("CenterLineColor", "Color");

            ExcludeMember("Layer");

            // Vic says - I started working on this but it took more time than expected.
            // Finish this another time
            //SaveUseWindow saveUseWindow = new SaveUseWindow(mCursor);
            //this.AddWindow(saveUseWindow, "Uncategorized");
            //saveUseWindow.SaveOKClick += new GuiMessage(SaveCurrentLayout);


        }

        #endregion

    }
}
