using System;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;

namespace PluginTestbed.LevelEditor
{

    public partial class LevelEditorPlugin : ICurrentElement
    {
        LevelEditorRemotingSelectionInterfaceManager _selectionInterface;
        private LevelEditorRemotingInteractiveInterfaceManager _interactiveInterface;

        [Import("GlueCommands")]
        public IGlueCommands GlueCommands { get; set; }

        [Import("GlueState")]
        public IGlueState GlueState { get; set; }

        #region IPlugin Members

        public string FriendlyName
        {
            get { return "Level Editor"; }
        }

        public Version Version
        {
            get { return new Version(1, 0); }
        }

        public void StartUp()
        {
            _selectionInterface = new LevelEditorRemotingSelectionInterfaceManager();
            _interactiveInterface = new LevelEditorRemotingInteractiveInterfaceManager(GlueCommands, GlueState);
        }

        public bool ShutDown(PluginShutDownReason shutDownReason)
        {
            if (_menuStrip != null)
            {
                _menuStrip.Items.Remove(_menuItem);
            }

            return true;
        }

        #endregion
    }
}
