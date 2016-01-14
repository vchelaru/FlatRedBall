using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;

namespace PolygonEditor.Gui
{
    public class EditorPropertiesGrid : PropertyGrid<EditorProperties>
    {
        #region Methods
        public EditorPropertiesGrid()
            : base(GuiManager.Cursor)
        {
            GuiManager.AddWindow(this);
            SelectedObject = EditorData.EditorProperties;
            HasCloseButton = true;

            IncludeMember("BackgroundColor", "General Options");
            IncludeMember("AxisAlignedCubeColor", "Axis Aligned Cube");
            IncludeMember("AxisAlignedRectangleColor", "Axis Aligned Rectangle");
            IncludeMember("Capsule2DColor", "Capsule2D");
            IncludeMember("CircleColor", "Circle");
            IncludeMember("SphereColor", "Sphere");

            IncludeMember("PolygonColor", "Polygon");
            IncludeMember("SelectedCornerColor", "Polygon");
            IncludeMember("NewPointPolygonColor", "Polygon");

            ExcludeMember("SnapToGrid");
            ExcludeMember("SnappingGridSize");
            ExcludeMember("PixelSize");
            ExcludeMember("SortYSecondary");
            ExcludeMember("ConstrainDimensions");
            ExcludeMember("CullSpriteGrids");
            ExcludeMember("AdditionalFade");
            ExcludeMember("FilteringOn");

            RemoveCategory("Uncategorized");

            Name = "Editor Properties";
        }

        public void Refresh()
        {
            SelectedObject = EditorData.EditorProperties;
        }
        #endregion
    }
}
