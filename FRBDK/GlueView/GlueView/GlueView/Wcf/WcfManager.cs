using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using GlueView.Managers;
using GlueWcfServices;

namespace GlueView.Wcf
{
    public class WcfManager : Singleton<WcfManager>
    {
        static IWcfService mWcfInstance;

        public void Initialize()
        {
            mWcfInstance = ChannelFactory<IWcfService>.CreateChannel(
                new NetNamedPipeBinding(),
                new EndpointAddress(
                    "net.pipe://localhost/GlueWcfEndPoint"));
        }

        public void GlueSelect(string elementName, string objectName)
        {
            // If it's null, that means no connection has been made yet.
            if (mWcfInstance != null)
            {
                mWcfInstance.SelectNamedObject(elementName, objectName);
            }
        }

        public void GlueSelectElement(string elementName)
        {
            mWcfInstance.SelectElement(elementName);
        }

        public void PrintOutput(string output)
        {
            mWcfInstance?.PrintOutput(output);
        }

    }
}
