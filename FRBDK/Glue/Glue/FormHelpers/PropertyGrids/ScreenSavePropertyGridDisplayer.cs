using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    public class ScreenSavePropertyGridDisplayer : ElementPropertyGridDisplayer
    {

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {

                UpdateIncludedAndExcluded(value as ScreenSave);
                base.Instance = value;
                SetAfterMemberChangedEvents();
            }
        }

        private void UpdateIncludedAndExcluded(ScreenSave instance)
        {
            UpdateIncludedAndExcludedBase(instance);

            // instance may not be set yet, so we have to use the argument value
            IncludeMember(nameof(ScreenSave.BaseScreen), typeof(ScreenSave), new AvailableScreenTypeConverter((ScreenSave)instance));
            IncludeMember(nameof(ScreenSave.NextScreen), typeof(ScreenSave), new AvailableScreenTypeConverter((ScreenSave)instance));

            IncludeMember(nameof(ScreenSave.Name), typeof(string), SetClassName, GetClassName, null, this.CategoryAttribute("\tScreen"));

        }

    }
}
