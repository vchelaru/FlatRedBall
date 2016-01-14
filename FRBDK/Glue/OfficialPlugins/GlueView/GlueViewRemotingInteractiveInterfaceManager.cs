using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using InteractiveInterface;

namespace OfficialPlugins.GlueView
{
    public class GlueViewRemotingInteractiveInterfaceManager : RemotingHelper.RemotingManagerTwoWay<RegisterInterface, IInteractiveInterface>
    {
        public GlueViewRemotingInteractiveInterfaceManager(IGlueCommands glueCommands, IGlueState glueState)
            : base(8686, new InteractiveInterface(glueCommands, glueState))
        {
        }

        public bool IgnoreNextRefresh
        {
            get { return ((InteractiveInterface) InInterface).IgnoreNextRefresh; }
            set { ((InteractiveInterface) InInterface).IgnoreNextRefresh = value; }
        }
    }
}
