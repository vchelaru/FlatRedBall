using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace GlueWcfServices
{
    // Services for sending commands from GlueView to Glue
    [ServiceContract(Namespace = "http://www.flatredball.com/Glue/wcf")]
    public interface IWcfService
    {
        //[OperationContract]
        //void SelectEntitySave(string entitySaveName);

        [OperationContract]
        void SelectNamedObject(string elementName, string namedObjectName);

        [OperationContract]
        void SelectElement(string elementName);

        [OperationContract]
        void PrintOutput(string output);
    }
}
