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
        GlueView2RemotingSelectionInterfaceManager gview2SelectionInterface;
        static GlueViewPlugin mSelf;

        public static GlueViewPlugin Self
        {
            get { return mSelf; }
        }

        public void SendScript(string script)
        {
            _selectionInterface.SendScript(script);
        }

        public void RefreshVariables()
        {
            _selectionInterface.RefreshVariables(true);
        }

        #region IPlugin Members
        

        public override void StartUp()
        {
            mSelf = this;
            _selectionInterface = new GlueViewRemotingSelectionInterfaceManager();
            gview2SelectionInterface = new GlueView2RemotingSelectionInterfaceManager();

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
