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
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Elements;

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
            // See if any files use content pipeline
            var anyUseContentPipeline = ObjectFinder.Self.GetAllReferencedFiles().Any(item => item.UseContentPipeline);
            if(anyUseContentPipeline)
            {
                var glueState = Container.Get<IGlueState>();
                codeBlock.Line(glueState.ProjectNamespace + ".FileAliasLogic.SetFileAliases();");
            }
        }

        public void GenerateFileAliasLogicCode(bool forceUseContentPipelineOnPngs)
        {
            var glueCommands = Container.Get<IGlueCommands>();
            var glueState = Container.Get<IGlueState>();

            TaskManager.Self.Add(() =>
            {
                if(glueState.CurrentGlueProject != null)
                {
                    string codeFileContents = GetFileAliasLogicFileContents();

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

        private static string GetFileAliasLogicFileContents()
        {
            var glueState = Container.Get<IGlueState>();

            var namespaceBlock = new CodeBlockNamespace(null, glueState.ProjectNamespace);

            var classBlock = namespaceBlock.Class("public", "FileAliasLogic");

            var codeBlock = classBlock.Function("public static void", "SetFileAliases", "");

            var fileNamesRelative = ObjectFinder.Self.GetAllReferencedFiles().Where(item => item.UseContentPipeline)
                .Select(item => item.Name).ToHashSet();

            var contentFolder = glueState.ContentDirectory;

            foreach (var fileName in fileNamesRelative)
            {
                var relativeFile = "Content/" + fileName;
                string withExtension = relativeFile;
                string noExtension = FileManager.RemoveExtension(relativeFile);
                var line =
                    $"global::FlatRedBall.Content.ContentManager.FileAliases.Add(global::FlatRedBall.IO.FileManager.Standardize(\"{withExtension}\"), " +
                    $"global::FlatRedBall.IO.FileManager.Standardize(\"{noExtension}\"));";
                codeBlock.Line(line);
            }

            var codeFileContents = namespaceBlock.ToString();
            return codeFileContents;
        }
    }
}
