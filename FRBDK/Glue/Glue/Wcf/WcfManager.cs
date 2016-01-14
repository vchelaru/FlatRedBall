using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Managers;
using Glue.Wcf;
using GlueWcfServices;

namespace FlatRedBall.Glue.Wcf
{
    internal class WcfManager : Singleton<WcfManager>
    {
        ServiceHost mWcfServiceHost;

        public void Initialize()
        {
            try
            {
                mWcfServiceHost = new ServiceHost(typeof(WcfService));

                mWcfServiceHost.AddServiceEndpoint(
                    typeof(IWcfService),
                    new NetNamedPipeBinding(),
                    "net.pipe://localhost/GlueWcfEndPoint"
                    );
                mWcfServiceHost.Open();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }
    }
}
