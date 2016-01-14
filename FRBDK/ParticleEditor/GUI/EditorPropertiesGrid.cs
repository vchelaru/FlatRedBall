using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Gui;

namespace ParticleEditor.GUI
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

                ExcludeMember("SnapToGrid");
                ExcludeMember("SnappingGridSize");
                ExcludeMember("PixelSize");
                ExcludeMember("SortYSecondary");
                ExcludeMember("ConstrainDimensions");
                ExcludeMember("CullSpriteGrids");
                ExcludeMember("AdditionalFade");
                ExcludeMember("FilteringOn");

                Name = "Editor Properties";
            }

            public void Refresh()
            {
                SelectedObject = EditorData.EditorProperties;
            }
        #endregion
    }
}
