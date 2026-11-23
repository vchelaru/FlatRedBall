using System;
using System.Windows.Forms;
using FlatRedBall.Glue.EditorProperties;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.GuiDisplay
{
    /// <summary>
    /// Shows the selected object in the WinForms Properties grid. Which properties exist, which can change and the
    /// values offered come from <see cref="NamedObjectSavePropertyModel"/>; this renders them and keeps the grid's
    /// selected row when the object changes.
    /// </summary>
    public class NamedObjectPropertyGridDisplayer : PropertyGridDisplayer
    {
        readonly NamedObjectSavePropertyModel model;

        public NamedObjectPropertyGridDisplayer(NamedObjectSavePropertyModel model)
        {
            this.model = model;
        }

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                throw new Exception("Use UpdateToState instead");
            }
        }

        public NamedObjectSave CurrentNamedObject => mInstance as NamedObjectSave;

        public override System.Windows.Forms.PropertyGrid PropertyGrid
        {
            get
            {
                return base.PropertyGrid;
            }
            set
            {
                GridItem gridItem = null;
                if (base.PropertyGrid != null)
                {
                    gridItem = base.PropertyGrid.SelectedGridItem;
                }
                base.PropertyGrid = value;
                // Assigning the grid scrolls it to the top; select whatever was selected before.
                if (base.PropertyGrid != null && gridItem != null && base.PropertyGrid.SelectedGridItem != null)
                {
                    var parent = PropertyGrid.SelectedGridItem.Parent;
                    if (parent != null)
                    {
                        foreach (GridItem child in parent.GridItems)
                        {
                            if (child.Label == gridItem.Label)
                            {
                                base.PropertyGrid.SelectedGridItem = child;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>Shows <paramref name="glueState"/>'s selected object. Returns whether the object changed.</summary>
        public bool UpdateToState(IGlueState glueState)
        {
            var nos = glueState.CurrentNamedObjectSave;
            bool didInstanceChange = nos != CurrentNamedObject;

            mInstance = nos;
            if (nos != null && glueState.CurrentGlueProject != null)
            {
                EditorPropertyGridMapping.Apply(this, nos, model.GetProperties(nos, glueState.CurrentElement));
            }

            base.Instance = mInstance;

            return didInstanceChange;
        }
    }
}
