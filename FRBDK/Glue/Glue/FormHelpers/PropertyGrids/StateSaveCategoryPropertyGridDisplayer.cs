using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    public class StateSaveCategoryPropertyGridDisplayer : PropertyGridDisplayer
    {
        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                UpdateIncludedAndExcluded(value as StateSaveCategory);
                base.Instance = value;
            }
        }
        private void UpdateIncludedAndExcluded(StateSaveCategory category)
        {
            ExcludeMember("States");
        }
    }
}
