using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using GlueView.Plugin;
using GlueView.Facades;

namespace GlueViewOfficialPlugins.ObjectCreator
{
    [Export(typeof(GlueViewPlugin))]
    class ObjectCreatorPlugin : GlueViewPlugin
    {

        AddObjectToolbox mControl;

        public override string FriendlyName
        {
            get { return "Object Creator Plugin"; }
        }

        public override Version Version
        {
            get { return new Version(); }
        }

        public override void StartUp()
        {
            mControl = new AddObjectToolbox();
            GlueViewCommands.Self.CollapsibleFormCommands.AddCollapsableForm(
                "Object Creation", 100, mControl, this);

            this.ElementLoaded += new EventHandler(ObjectCreatorPlugin_ElementLoaded);
        }

        void ObjectCreatorPlugin_ElementLoaded(object sender, EventArgs e)
        {
            mControl.PopulateWithEntityTypes();
        }

        public override bool ShutDown(FlatRedBall.Glue.Plugins.Interfaces.PluginShutDownReason shutDownReason)
        {
            return true;
        }



    }
}
