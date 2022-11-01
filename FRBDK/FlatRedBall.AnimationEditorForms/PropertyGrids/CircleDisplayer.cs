using FlatRedBall.Glue.GuiDisplay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.AnimationEditorForms.PropertyGrids
{
    internal class CircleDisplayer : PropertyGridDisplayer
    {
        public override System.Windows.Forms.PropertyGrid PropertyGrid
        {
            get => base.PropertyGrid;
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
            get => base.Instance;
            set
            {
                base.Instance = value;
                UpdateShownProperties();
            }
        }

        private void UpdateShownProperties() {/*eventually include/exclude?*/}
    }
}
