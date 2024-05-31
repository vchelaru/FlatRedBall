using FlatRedBall.Glue.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using EditorObjects.IoC;

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
                var glueState = Container.Get<IGlueState>();
                codeBlock.Line(glueState.ProjectNamespace + ".FileAliasLogic.SetFileAliases();");
            }
        }

        public void GenerateFileAliasLogicCode(bool isUsingContentPipeline)
        {
            var glueCommands = Container.Get<IGlueCommands>();
            var glueState = Container.Get<IGlueState>();

            TaskManager.Self.Add(() =>
            {
                if(glueState.CurrentGlueProject != null)
                {
                    string codeFileContents = GetFileAliasLogicFileContents(isUsingContentPipeline);

                    glueCommands.ProjectCommands.CreateAndAddCodeFile("FileAliases.Generated.cs");
     
                    var absolutePath = glueState.CurrentGlueProjectDirectory + "FileAliases.Generated.cs";


                    glueCommands.TryMultipleTimes(() =>
                    {
                        glueCommands.FileCommands.SaveIfDiffers(absolutePath, codeFileContents);
                    });
                }
                
     
            }, 
            "Generating FileAliases for content pipeline.",
            TaskExecutionPreference.AddOrMoveToEnd
            );

            // This may be the first time the user has set to use content pipeline, so re-gen global content
            TaskManager.Self.Add(glueCommands.GenerateCodeCommands.GenerateGlobalContentCode,
                "Generateing global content code", 
                TaskExecutionPreference.AddOrMoveToEnd);
        }

        private static string GetFileAliasLogicFileContents(bool isUsingContentPipeline)
        {
            var glueState = Container.Get<IGlueState>();

            var namespaceBlock = new CodeBlockNamespace(null, glueState.ProjectNamespace);

            var classBlock = namespaceBlock.Class("public", "FileAliasLogic");

            var codeBlock = classBlock.Function("public static void", "SetFileAliases", "");

            var files = ContentPipelineController.GetReferencedPngs();

            var contentFolder = glueState.ContentDirectory;

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
