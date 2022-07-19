using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueCommunication.Json
{
    internal abstract class JsonContainer
    {
        public JToken Json { get; private set; }

        public JsonContainer(JToken json)
        {
            Json = json;
        }
    }
}
