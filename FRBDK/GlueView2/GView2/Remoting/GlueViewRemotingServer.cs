using FlatRedBall.Instructions;
using GlueView2.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.GlueView2
{
    public class GlueViewRemotingServer : MarshalByRefObject, IGlueView2Selection
    {
        public static void StartHosting()
        {

            TcpChannel tcpChannel = new TcpChannel(9998);
            ChannelServices.RegisterChannel(tcpChannel, false);

            Type commonInterfaceType = typeof(FlatRedBall.GlueView2.GlueViewRemotingServer);

            RemotingConfiguration.RegisterWellKnownServiceType(commonInterfaceType,
                "MovieTicketBooking", WellKnownObjectMode.SingleCall);
        }


        public void LoadGluxFile(string glueProjectFileName)
        {
            GlueViewCommands.Self.LoadProject(glueProjectFileName);
        }

        public void ShowElement(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                InstructionManager.AddSafe(
                    () => GlueViewCommands.Self.ShowScreen(name));
            }
            else
            {
                GlueViewCommands.Self.ClearShownElements();
            }
        }
    }
}
