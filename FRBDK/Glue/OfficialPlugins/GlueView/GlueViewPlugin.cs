using System;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.GlueView;

namespace OfficialPlugins.GlueView
{
    [Export(typeof(PluginBase))]
    public partial class GlueViewPlugin : EmbeddedPlugin
    {
        GlueViewRemotingSelectionInterfaceManager _selectionInterface;
        
        static GlueViewPlugin mSelf;

        public static GlueViewPlugin Self
        {
            get { return mSelf; }
        }

        public void SendScript(string script)
        {
            _selectionInterface.SendScript(script);
        }

        #region IPlugin Members
        

        public override void StartUp()
        {
            mSelf = this;
            _selectionInterface = new GlueViewRemotingSelectionInterfaceManager();

            var toolbar = new GlueViewToolbar();
            toolbar.SelectionInterface = _selectionInterface;
            base.AddToToolBar(toolbar, "Standard");

        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            if(_menuStrip != null)
            {
                _menuStrip.Items.Remove(_menuItem);
            }

            return true;
        }

        #endregion
    }
}
