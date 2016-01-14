using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueView.Plugin;
using System.ComponentModel.Composition;
using StaffDotNet.CollapsiblePanel;
using GlueView.Facades;
using FlatRedBall.Glue.StateInterpolation;

namespace GlueViewOfficialPlugins.States
{

    [Export(typeof(GlueViewPlugin))]
    public class StateViewPlugin : GlueViewPlugin
    {
        static StateViewPlugin mSelf;

        StateSaveControl mStateSaveControl;

        public static StateViewPlugin Self
        {
            get
            {
                return mSelf;
            }
        }

        public override string FriendlyName
        {
            get { return "State Control"; }
        }

        public override Version Version
        {
            get { return new Version(); }
        }

        public override void StartUp()
        {
            mSelf = this;
            this.ElementLoaded += new EventHandler(OnElementLoad);
            // here we create the form
            mStateSaveControl = new StateSaveControl();

            GlueViewCommands.Self.CollapsibleFormCommands.AddCollapsableForm("States", 300, mStateSaveControl, this);

        }

        void OnElementLoad(object sender, EventArgs e)
        {
            mStateSaveControl.CurrentElement = GlueViewState.Self.CurrentElement;
        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            // remove the form
            return true;
        }

        public void ApplyInterpolateToState(object firstState, object secondState, float time, InterpolationType interpolationType, Easing easing)
        {
            mStateSaveControl.ApplyInterpolateToState(firstState, secondState, time, interpolationType, easing);

        }
    }
}
