using FlatRedBall.Glue.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.SaveClasses;

namespace OfficialPlugins.RuntimeFileWatcherPlugin
{
    public class FileSystemWatcherCodeGenerator : GlobalContentCodeGeneratorBase
    {
        public override void GenerateInitializeEnd(ICodeBlock codeBlock)
        {
            codeBlock.Line("#if DEBUG && WINDOWS");
            codeBlock.Line("InitializeFileWatch();");
            codeBlock.Line("#endif");

        }
        public override void GenerateAdditionalMethods(ICodeBlock codeBlock)
        {
            codeBlock.Line("#if DEBUG && WINDOWS");

            codeBlock.Line("static System.IO.FileSystemWatcher watcher;");
            GenerateInitialize(codeBlock);

            GenerateHandleFileChanged(codeBlock);
            codeBlock.Line("#endif");

        }

        private void GenerateHandleFileChanged(ICodeBlock codeBlock)
        {
            var method = codeBlock.Function("private static void", "HandleFileChanged", "object sender, System.IO.FileSystemEventArgs e");
            {
                var tryBlock = method.Try();

                tryBlock.Line("System.Threading.Thread.Sleep(500);");

                tryBlock.Line("var fullFileName = e.FullPath;");
                tryBlock.Line("var relativeFileName = FlatRedBall.IO.FileManager.MakeRelative(FlatRedBall.IO.FileManager.Standardize(fullFileName));");

                foreach(var rfs in GlueState.Self.CurrentGlueProject.GlobalFiles)
                {
                    var ati = rfs.GetAssetTypeInfo();

                    bool shouldGenerate = rfs.LoadedAtRuntime && rfs.IsDatabaseForLocalizing == false && 
                        ati?.QualifiedRuntimeTypeName.QualifiedType != null;

                    if(shouldGenerate)
                    {
                        var fileName =
                            ReferencedFileSaveCodeGenerator.GetFileToLoadForRfs(rfs, ati);

                        // this will strip extension for content pipeline so let's re-add XNB. Assume this is the type for textures, but
                        // eventually may need to expand this for other types like wav/mpe
                        if(ati?.MustBeAddedToContentPipeline == true || rfs.UseContentPipeline)
                        {
                            // assume XNB for now:
                            fileName = fileName + ".xnb";
                        }

                        var instanceName = rfs.GetInstanceName();

                        var ifStatement = tryBlock.If($"relativeFileName == \"{fileName}\"");
                        {
                            ifStatement.Line($"Reload({instanceName});");
                        }
                    }
                }
                var catchBlock = tryBlock.End().Line("catch{}");
            }
        }

        private static void GenerateInitialize(ICodeBlock codeBlock)
        {
            var initializeMethod = codeBlock.Function("private static void", "InitializeFileWatch", "");
            {
                initializeMethod.Line("string globalContent = FlatRedBall.IO.FileManager.RelativeDirectory + \"content/globalcontent/\";");
                var ifBlock = initializeMethod.If("System.IO.Directory.Exists(globalContent)");
                {
                    ifBlock.Line("watcher = new System.IO.FileSystemWatcher();");
                    ifBlock.Line("watcher.Path = globalContent;");
                    ifBlock.Line("watcher.NotifyFilter = System.IO.NotifyFilters.LastWrite;");
                    ifBlock.Line("watcher.Filter = \"*.*\";");
                    ifBlock.Line("watcher.Changed += HandleFileChanged;");
                    ifBlock.Line("watcher.EnableRaisingEvents = true;");
                }
            }
        }

        
    }
}
