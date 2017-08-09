using FlatRedBall.Glue.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Managers;

namespace OfficialPlugins.ContentPipelinePlugin
{
    public class AliasCodeGenerator : GlobalContentCodeGeneratorBase
    {
        ContentPipelineController controller;

        public void Initialize(ContentPipelineController controller)
        {
            this.controller = controller;
        }

        public override void GenerateInitializeStart(ICodeBlock codeBlock)
        {
            if(controller.Settings.UseContentPipelineOnAllPngs)
            {
                codeBlock.Line(GlueState.Self.ProjectNamespace + ".FileAliasLogic.SetFileAliases();");
            }
        }

        public void GenerateFileAliasLogicCode(bool isUsingContentPipeline)
        {
            TaskManager.Self.AddSync(() =>
            {
                string codeFileContents = GetFileAliasLogicFileContents(isUsingContentPipeline);
     
                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile("FileAliases.Generated.cs");
     
                var absolutePath = GlueState.Self.CurrentGlueProjectDirectory + "FileAliases.Generated.cs";


                GlueCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(absolutePath, codeFileContents), 5);
                
     
            }, "Generating FileAliases for content pipeline.");
        }

        private static string GetFileAliasLogicFileContents(bool isUsingContentPipeline)
        {
            var namespaceBlock = new CodeBlockNamespace(null, GlueState.Self.ProjectNamespace);

            var classBlock = namespaceBlock.Class("public", "FileAliasLogic");

            var codeBlock = classBlock.Function("public static void", "SetFileAliases", "");

            var files = ContentPipelineController.GetReferencedPngs();

            var contentFolder = GlueState.Self.ContentDirectory;

            if(isUsingContentPipeline)
            {
                foreach (var file in files)
                {
                    var relativeFile = "content/" + FileManager.MakeRelative(file, contentFolder).ToLowerInvariant();
                    string withExtension = relativeFile;
                    string noExtension = FileManager.RemoveExtension(relativeFile);
                    var line =
                        $"FlatRedBall.Content.ContentManager.FileAliases.Add(FlatRedBall.IO.FileManager.Standardize(\"{withExtension}\"), " +
                        $"FlatRedBall.IO.FileManager.Standardize(\"{noExtension}\"));";
                    codeBlock.Line(line);
                }
            }

            var codeFileContents = namespaceBlock.ToString();
            return codeFileContents;
        }
    }
}
