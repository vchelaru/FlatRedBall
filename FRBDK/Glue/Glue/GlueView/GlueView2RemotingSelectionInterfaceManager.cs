using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueView2.EmbeddedPlugins.SelectionFromGlue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlatRedBall.GlueView2
{
    public interface IGlueView2Selection
    {
        void LoadGluxFile(string glueProjectFileName);
        void ShowElement(string name);

    }
}


namespace FlatRedBall.Glue.GlueView
{
    class GlueView2RemotingSelectionInterfaceManager 
    {
        GlueView2.IGlueView2Selection remoteObject;

        public void DoTest()
        {
            AttemptConnection();
        }

        internal void AttemptConnection()
        {
            if(remoteObject != null)
            {
                return;
            }

            TcpChannel tcpChannel = new TcpChannel();
            ChannelServices.RegisterChannel(tcpChannel, false);

            Type requiredType = typeof(GlueView2.IGlueView2Selection);

            remoteObject = (GlueView2.IGlueView2Selection)Activator.GetObject(requiredType,
                "tcp://localhost:9998/MovieTicketBooking");
        }

        internal void SetGlueProjectFile(string glueProjectFileName)
        {
            Task.Run(() =>
            {
                try
                {
                    remoteObject.LoadGluxFile(glueProjectFileName);
                }
                catch(Exception e)
                {

                }
            });
        }

        internal void UpdateSelectedNode()
        {
            var elementName = GlueState.Self.CurrentElement?.Name;

            Task.Run( () =>
            {
                try
                {
                    remoteObject.ShowElement(elementName);
                }
                catch(Exception e)
                {

                }
            });
        }
    }
}
