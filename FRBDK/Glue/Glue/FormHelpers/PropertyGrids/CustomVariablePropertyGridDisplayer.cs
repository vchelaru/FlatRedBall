using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    /// <summary>
    /// Shows a selected variable in the WinForms Properties grid. Which properties exist and how they change comes from
    /// <see cref="CustomVariablePropertyModel"/>; this only renders them.
    /// </summary>
    public class CustomVariablePropertyGridDisplayer : PropertyGridDisplayer
    {
        readonly CustomVariablePropertyModel model;

        public CustomVariablePropertyGridDisplayer(CustomVariablePropertyModel model)
        {
            this.model = model;
        }

        /// <summary>The element that defines the variable. Set it before <see cref="Instance"/>.</summary>
        public GlueElement CurrentElement
        {
            get;
            set;
        }

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                var variable = (CustomVariable)value;
                EditorPropertyGridMapping.Apply(this, variable, model.GetProperties(variable, CurrentElement),
                    // The default value is edited through the variable's own type converter (enums, files, states...).
                    property => property.Name == nameof(CustomVariable.DefaultValue) ? variable.GetTypeConverter(CurrentElement) : null);

                base.Instance = value;
            }
        }
    }
}
