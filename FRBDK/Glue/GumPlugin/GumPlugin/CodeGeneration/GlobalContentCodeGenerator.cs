using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using GumPlugin.Managers;
using GumPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.CodeGeneration
{
    public class GlobalContentCodeGenerator : GlobalContentCodeGeneratorBase
    {
        public override void GenerateInitializeEnd(ICodeBlock codeBlock)
        {
            if(AppState.Self.GumProjectSave != null)
            {
                var rfs = GumProjectManager.Self.GetRfsForGumProject();
                var showMouse = false;
                if(rfs != null)
                {
                    showMouse = rfs.Properties.GetValue<bool>(nameof(GumViewModel.ShowMouse));
                }

                if(showMouse)
                {
                    codeBlock.Line("// Added by GumPlugin becasue of the Show Mouse checkbox on the .gumx:");
                    codeBlock.Line("FlatRedBall.FlatRedBallServices.Game.IsMouseVisible = true;");
                }
            }
        }
    }
}
