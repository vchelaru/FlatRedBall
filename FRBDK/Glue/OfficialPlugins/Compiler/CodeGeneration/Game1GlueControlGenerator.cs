using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration.Game1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.Compiler.CodeGeneration
{
    public class Game1GlueControlGenerator : Game1CodeGenerator
    {
        public bool IsGlueControlManagerGenerationEnabled { get; set; }
        public override void GenerateClassScope(ICodeBlock codeBlock)
        {
            if(IsGlueControlManagerGenerationEnabled)
            {
                codeBlock.Line("GlueControlManager glueControlManager;");
            }
        }

        public override void GenerateInitialize(ICodeBlock codeBlock)
        {
            if (IsGlueControlManagerGenerationEnabled)
            {
                codeBlock.Line("glueControlManager = new GlueControlManager(8021);");
                codeBlock.Line("glueControlManager.Start();");
            }
        }
    }
}
