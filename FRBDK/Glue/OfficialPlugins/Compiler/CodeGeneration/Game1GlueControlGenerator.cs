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
        public int PortNumber { get; set; }
        public override void GenerateClassScope(ICodeBlock codeBlock)
        {
            if(IsGlueControlManagerGenerationEnabled)
            {
                codeBlock.Line("GlueControl.GlueControlManager glueControlManager;");
            }
        }

        public override void GenerateInitialize(ICodeBlock codeBlock)
        {
            if (IsGlueControlManagerGenerationEnabled)
            {
                codeBlock.Line($"glueControlManager = new GlueControl.GlueControlManager({PortNumber});");
                codeBlock.Line("glueControlManager.Start();");
                codeBlock.Line("this.Exiting += (not, used) => glueControlManager.Kill();");

                // Vic says - We run all Glue commands before running custom initialize. The reason is - custom initialize
                // may make modifications to objects that are created by glue commands (such as assigning acceleration to objects
                // in a list), but it is unlikely that scripts will make modifications to objects created in CustomInitialize because
                // objects created in CustomInitialize cannot be modified by level editor.
                codeBlock.Line("FlatRedBall.Screens.ScreenManager.BeforeScreenCustomInitialize += (newScreen) => glueControlManager.ReRunAllGlueToGameCommands();");
            }
        }
    }
}
