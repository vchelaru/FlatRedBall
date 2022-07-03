using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Compiler.CodeGeneration.GlueCalls
{
    internal class Method
    {
        public string Name { get; set; }
        public Parameter[] Parameters { get; set; }
        public bool AddEchoToGame { get; internal set; }
        public string ReturnType { get; internal set; }
    }
}
