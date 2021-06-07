using EditModeProject.Performance;
using FlatRedBall;
using FlatRedBall.Forms.Controls;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace {ProjectNamespace}.GlueControl.Forms
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

        public void PopulateWithAvailableEntities()
        {
#if WINDOWS_8 || UWP
                var assembly = typeof(TileEntityInstantiator).GetTypeInfo().Assembly;
                var typesInThisAssembly = assembly.DefinedTypes.Select(item=>item.AsType()).ToArray();

#else
            var assembly = Assembly.GetExecutingAssembly();
            var typesInThisAssembly = assembly.GetTypes();
#endif


#if WINDOWS_8 || UWP
            var filteredTypes =
                typesInThisAssembly.Where(t => t.GetInterfaces().Contains(typeof(IEntityFactory))
                            && t.GetConstructors().Any(c=>c.GetParameters().Count() == 0));
#else
            var filteredTypes =
                typesInThisAssembly.Where(t => t.GetInterfaces().Contains(typeof(Performance.IEntityFactory))
                            && t.GetConstructor(Type.EmptyTypes) != null);
#endif

            var factories = filteredTypes
                .Select(
                    t =>
                    {
#if WINDOWS_8 || UWP
                        var propertyInfo = t.GetProperty("Self");
#else
                        var propertyInfo = t.GetProperty("Self");
#endif
                        var value = propertyInfo.GetValue(null, null);
                        return value as IEntityFactory;
                    }).ToList();

            foreach (var factory in factories)
            {
                var factoryName = factory.GetType().Name;
                var name = factoryName.Substring(0, factoryName.Length - "Factory".Length);
                var button = AddButton();
                button.Click += (not, used) => InstanceLogic.Self.CreateEntity(
                    name, Camera.Main.X, Camera.Main.Y);
                button.Text = name;
            }
        }
    }
}
