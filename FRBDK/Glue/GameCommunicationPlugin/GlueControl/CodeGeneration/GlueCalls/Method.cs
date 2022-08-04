using System;
using System.Collections.Generic;
using System.Text;

namespace GameCommunicationPlugin.GlueControl.CodeGeneration.GlueCalls
{
    internal abstract class MethodBase
    {
        public string Name { get; set; }
        public Parameter[] Parameters { get; set; }
    }

    internal class Method : MethodBase
    {
        public bool AddEchoToGame { get; internal set; }
        public string ReturnType { get; internal set; }
    }

    internal class PropertyMethod : MethodBase
    {

    }
}
