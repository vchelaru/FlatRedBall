using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.Compiler.CodeGeneration.GlueCalls
{
    internal class GenerationOptions
    {
        public string Name { get; set; }
        public string BaseClass { get; set; }
        public string Namespace { get; set; }
        public string[] Defines { get; internal set; }
        public string[] Usings { get; internal set; }
        public Method[] Methods { get; internal set; }
    }
}
