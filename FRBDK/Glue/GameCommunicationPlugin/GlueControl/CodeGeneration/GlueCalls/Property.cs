using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls
{
    internal class Property
    {
        public string Name { get; set; }
        public string GetBody { get; set; }
        public PropertyMethod SetMethod { get; set; }
        public string ReturnType { get; internal set; }
        public string GetSimpleBody { get; internal set; }
        public bool IsAutomaticProperty { get; internal set; }
        public bool ReturnToPropertyType { get; internal set; }

        public override string ToString()
        {
            return "Name";
        }
    }
}
