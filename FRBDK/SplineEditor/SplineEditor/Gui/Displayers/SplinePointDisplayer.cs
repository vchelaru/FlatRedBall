using FlatRedBall.Glue.GuiDisplay;
using SplineEditor.Commands;
using SplineEditor.States;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SplineEditor.Gui.Displayers
{
    public class SplinePointDisplayer : PropertyGridDisplayer
    {
        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                base.Instance = value;

                UpdateDisplayedProperties();
            }
        }

        private void UpdateDisplayedProperties()
        {

            GetPropertyGridMember("Time").AfterMemberChange += AfterTimeSet;
        }

        private void AfterTimeSet(object sender, MemberChangeArgs args)
        {
            if (AppState.Self.CurrentSpline != null)
            {
                AppState.Self.CurrentSpline.Sort();
            }

            // Refresh the entire spline because time and ordering may have changed
            AppCommands.Self.Gui.RefreshTreeView(AppState.Self.CurrentSpline);
        }
    }
}
