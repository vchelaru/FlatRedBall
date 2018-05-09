using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.RuntimeDebuggingPlugin.Logic
{
    public class EmbeddedResourceLogic
    {
        public static void SaveAllRfses()
        {
            var embeddedResourcePath =
                "FlatRedBall.RuntimeDebuggingPlugin.Embedded.Content.SpriteSheet.png";
            var targetPath = GlueState.Self.Find.GlobalContentFilesPath +
                "RuntimeDebugging\\SpriteSheet.png";

            FileManager.SaveEmbeddedResource(typeof(EmbeddedResourceLogic).Assembly,
                embeddedResourcePath, targetPath);


            const string expectedComment =
                "This file was automatically added by the Runtime Debugging Plugin. Do not remove it or the plugin will not display icons";

            TaskManager.Self.AddSync(() =>
            {
                var referencedFile = GlueCommands.Self.FileCommands.GetReferencedFile(targetPath);
                if (referencedFile == null)
                {
                    referencedFile = GlueCommands.Self.GluxCommands.AddReferencedFileToGlobalContent(
                        "GlobalContent/RuntimeDebugging/SpriteSheet.png", true);
                    referencedFile.Summary = expectedComment;
                    GlueCommands.Self.GluxCommands.SaveGlux();
                }

                // todo - add the code file here
            }, "Updating files for plugin");

            
        }



    }
}
