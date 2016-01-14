using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueView.Plugin;
using System.ComponentModel.Composition;
using StaffDotNet.CollapsiblePanel;
using GlueView.Facades;

namespace GlueViewOfficialPlugins.States
{

    [Export(typeof(GlueViewPlugin))]
    public class StateViewPlugin : GlueViewPlugin
    {
        StateSaveControl mStateSaveControl;

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
    }
}
