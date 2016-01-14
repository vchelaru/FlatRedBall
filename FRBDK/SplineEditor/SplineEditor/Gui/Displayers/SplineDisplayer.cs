using FlatRedBall.Glue.GuiDisplay;
using SplineEditor.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SplineEditor.Gui.Displayers
{
    
    public class SplineDisplayer : PropertyGridDisplayer
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
            ExcludeMember("Item");
            ExcludeMember("PathColor");
            ExcludeMember("PointColor");
            // We should include visible
            //ExcludeMember("Visible");
            ExcludeMember("SplinePointVisibleRadius");
            ExcludeMember("Count");
            ExcludeMember("IsReadOnly");
            SetCategory("Name", "\tSpline");

            SetCategory("Duration", "Time");
            SetCategory("StartTime", "Time");

            GetPropertyGridMember("Name").AfterMemberChange += AfterNameSet;
            
        }

        void AfterNameSet(object sender, MemberChangeArgs args)
        {
            AppCommands.Self.Gui.RefreshTreeView();
        }
    }
}
