using FlatRedBall.Glue.GuiDisplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.AnimationEditorForms.PropertyGrids
{
    class AxisAlignedRectangleDisplayer : PropertyGridDisplayer
    {
        public override System.Windows.Forms.PropertyGrid PropertyGrid
        {
            get
            {
                return base.PropertyGrid;
            }
            set
            {
                if (value != null)
                {
                    value.PropertySort = System.Windows.Forms.PropertySort.Categorized;
                }
                base.PropertyGrid = value;
            }
        }

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                base.Instance = value;
                UpdateShownProperties();
            }
        }

        private void UpdateShownProperties()
        {
            // eventually include/exclude?
        }
    }
}
