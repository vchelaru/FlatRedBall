using System;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Threading;

namespace RemotingHelper
{
    public class RemotingManagerTwoWay<TOutgoing, TIncoming> where TOutgoing : MarshalByRefObject, IRegisterCallback<TIncoming> where TIncoming : class
    {
        protected TOutgoing OutInterface;
        protected TIncoming InInterface;
        protected bool ConnectionFailed;
        private readonly int _mPort;

        protected delegate void ErrorMessageDelegate(string error);
        protected event ErrorMessageDelegate ErrorMessage;

        private Timer _timer;

        public RemotingManagerTwoWay(int port, TIncoming incoming)
        {
            _mPort = port;
            InInterface = incoming;
            AttemptConnection(false);
            _timer = new Timer(delegate
                                   {
                                       try
                                       {
                                           if (OutInterface != null)
                                           {
                                               //Register Incoming
                                               OutInterface.RegisterCallback(InInterface);
                                           }
                                       }catch(Exception ex)
                                       {
                                           //Debug.WriteLine(ex.ToString());
                                       }
                                   },null,0,2000);
        }

        public void AttemptConnection()
        {
            AttemptConnection(true);
        }

        public void AttemptConnection(bool showWarning)
        {
            ConnectionFailed = false;

            if (OutInterface == null)
            {
                try
                {
                    //Register Outgoing
                    OutInterface =
                    (TOutgoing)Activator.GetObject(typeof(TOutgoing),
                    "http://localhost:" + _mPort + "/" + RemotingServer.GetName(typeof(TOutgoing)) + ".rem");

                    //Register Incoming channel
                    var channel = new HttpChannel(0);
                    ChannelServices.RegisterChannel(channel, false);
                }
                catch (Exception e)
                {
                    if (showWarning)
                    {
                        OnConnectionFail(e);
                    }
                }
            }
        }

        protected void OnConnectionFail(Exception e)
        {
            OnConnectionFail(e, true);
        }


        protected void OnConnectionFail(Exception e, bool showError)
        {
            if (showError)
            {
                const string error = "Could not connect. \n\n Exception:\n";
                if(ErrorMessage != null)
                    ErrorMessage(error + e);
            }
            ConnectionFailed = true;
            OutInterface = default(TOutgoing);
        }
    }
}
