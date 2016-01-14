using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;

namespace PolygonEditor.Gui
{
    public class PolygonPropertyGrid : EditorObjects.Gui.PolygonPropertyGrid
    {
        private void UpdateSelectedPoint(Window callingWindow)
        {
            EditorData.EditingLogic.SelectPolygonCorner(CornerIndexHighlighted);
        }


        public PolygonPropertyGrid(Cursor cursor)
            : base(cursor)
        {
            this.NewPointHighlight += UpdateSelectedPoint;
        }
    }
}
