using System;

namespace RemotingHelper
{
    public class RemotingManager<TOutgoing> 
    {
        protected TOutgoing OutInterface;
        protected bool ConnectionFailed;
        readonly int _port;

        protected delegate void ErrorMessageDelegate(string error);
        protected event ErrorMessageDelegate ErrorMessage;

        public RemotingManager(int port)
        {
            _port = port;
            AttemptConnection(false);
        }

        public void AttemptConnection()
        {
            AttemptConnection(true);
        }

        public void AttemptConnection(bool showWarning)
        {

            if (OutInterface != null)
            {
                return;
            }
            ConnectionFailed = false;

            try
            {
                var url = "http://localhost:" + _port + "/" + RemotingServer.GetName(typeof(TOutgoing)) + ".rem";
                OutInterface = (TOutgoing)Activator.GetObject(typeof(TOutgoing), url);

            }
            catch (Exception e)
            {
                if (showWarning)
                {
                    OnConnectionFail(e);
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
