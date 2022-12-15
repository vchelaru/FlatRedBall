using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace GumPluginCore.CodeGeneration
{
    internal class GumGame1CodeGenerator : Game1CodeGenerator
    {
        public override void GenerateInitialize(ICodeBlock codeBlock)
        {
            if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.HasFrameworkElementManager)
            {
                codeBlock.Line("FlatRedBall.FlatRedBallServices.AddManager(FlatRedBall.Forms.Managers.FrameworkElementManager.Self);");
            }
        }
    }
}
