using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls
{
    internal class GenerationOptions
    {
        public GenerationOptions()
        {
            Defines = new string[0];
            Usings = new string[0];
            Methods = new Method[0];
            Properties = new Property[0];
        }

        public string Name { get; set; }
        public string BaseClass { get; set; }
        public string Namespace { get; set; }
        public string[] Defines { get; internal set; }
        public string[] Usings { get; internal set; }
        public Method[] Methods { get; internal set; }
        public Property[] Properties { get; internal set; }
        public bool AddStaticSelfReference { get; internal set; }
    }
}
