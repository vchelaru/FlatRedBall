using System;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using InteractiveInterface;

namespace PluginTestbed.LevelEditor
{
    public class LevelEditorRemotingInteractiveInterfaceManager : RemotingHelper.RemotingManagerTwoWay<RegisterInterface, IInteractiveInterface>
    {
        public LevelEditorRemotingInteractiveInterfaceManager(IGlueCommands glueCommands, IGlueState glueState)
            : base(9426, new InteractiveInterface(glueCommands, glueState))
        {
        }

        public bool IgnoreNextRefresh
        {
            get { return ((InteractiveInterface) InInterface).IgnoreNextRefresh; }
            set { ((InteractiveInterface) InInterface).IgnoreNextRefresh = value; }
        }
    }
}
