{CompilerDirectives}

using {ProjectNamespace}.Performance;
using FlatRedBall;
using FlatRedBall.Forms.Controls;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GlueControl.Forms
{
    class ObjectCreationWindow : UserControl
    {
        ScrollViewer scrollViewer;
        public ObjectCreationWindow() : base()
        {
            Initialize();
        }
        public ObjectCreationWindow(GraphicalUiElement visual) : base(visual)
        {
            Initialize();
        }

        private void Initialize()
        {
            var visual = this.Visual;
            visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
            visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
            Visual.X = 0;

            visual.YOrigin = RenderingLibrary.Graphics.VerticalAlignment.Center;
            visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromMiddle;
            visual.Y = 0;

            visual.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            visual.Width = 180;

            visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            visual.Height = 0;

            scrollViewer = new ScrollViewer();
            scrollViewer.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            scrollViewer.Visual.Width = -4;
            scrollViewer.Visual.HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            scrollViewer.Visual.Height = -4;



            this.AddChild(scrollViewer);
        }

        Button AddButton()
        {
            var button = new Button();
            button.Visual.WidthUnits = Gum.DataTypes.DimensionUnitType.RelativeToContainer;
            button.Visual.Width = -2;
            scrollViewer.InnerPanel.Children.Add(button.Visual);
            return button;
        }

    }
}
