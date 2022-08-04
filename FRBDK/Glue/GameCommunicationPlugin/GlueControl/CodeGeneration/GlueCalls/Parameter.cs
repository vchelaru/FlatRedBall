using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls
{
    internal class Parameter
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public bool IsParameterUsedByGlue { get; set; }
        public string[] Dependencies { get; set; }
        public string DefaultValue { get; internal set; }
    }
}
