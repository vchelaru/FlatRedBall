using FlatRedBall.Math.Splines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ToolTemplate.Gui;

namespace SplineEditor.Commands
{
    public class GuiCommands
    {
        public void RefreshTreeView()
        {
            GuiData.SplineListDisplay.UpdateToList();

        }

        public void RefreshTreeView(Spline spline)
        {
            GuiData.SplineListDisplay.UpdateToList(spline);
        }

        public void RefreshTreeView(SplinePoint splinePoint)
        {
            GuiData.SplineListDisplay.UpdateToList(splinePoint);
        }


        public void RefreshPropertyGrid()
        {
            GuiData.PropertyGrid.Refresh();
        }
    }
}
