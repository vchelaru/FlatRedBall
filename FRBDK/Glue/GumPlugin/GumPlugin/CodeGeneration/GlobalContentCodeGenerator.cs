using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.Interfaces;
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
        public override void GenerateInitializeStart(ICodeBlock codeBlock)
        {
            if (AppState.Self.GumProjectSave != null)
            {
                var rfs = GumProjectManager.Self.GetRfsForGumProject();
                var showMouse = false;
                if (rfs != null)
                {
                    showMouse = rfs.Properties.GetValue<bool>(nameof(GumViewModel.ShowMouse));
                }

                if (showMouse)
                {
                    codeBlock.Line("// Added by GumPlugin becasue of the Show Mouse checkbox on the .gumx:");
                    codeBlock.Line("FlatRedBall.FlatRedBallServices.Game.IsMouseVisible = true;");
                }

                // If we have a gum project, we could inspect whether any files in Global Content are screens, but I see
                // no harm in setting this:
                codeBlock.Line("// Set the content manager for Gum");
                codeBlock.Line("var contentManagerWrapper = new FlatRedBall.Gum.ContentManagerWrapper();");
                codeBlock.Line("contentManagerWrapper.ContentManagerName = FlatRedBall.FlatRedBallServices.GlobalContentManager;");
                codeBlock.Line("RenderingLibrary.Content.LoaderManager.Self.ContentLoader = contentManagerWrapper;");
            }
        }

    }
}
