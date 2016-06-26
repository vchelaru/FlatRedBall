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
            codeBlock.Line("#if DEBUG");
            codeBlock.Line("InitializeFileWatch();");
            codeBlock.Line("#endif");

        }
        public override void GenerateAdditionalMethods(ICodeBlock codeBlock)
        {
            codeBlock.Line("#if DEBUG");

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
                    var fileName = ProjectBase.AccessContentDirectory + rfs.Name.ToLower().Replace("\\", "/");
                    var instanceName = rfs.GetInstanceName();

                    var ifStatement = tryBlock.If($"relativeFileName == \"{fileName}\"");
                    {
                        ifStatement.Line($"Reload({instanceName});");
                    }

                }
                var catchBlock = tryBlock.End().Line("catch{}");
            }
        }

        private static void GenerateInitialize(ICodeBlock codeBlock)
        {
            var initializeMethod = codeBlock.Function("private static void", "InitializeFileWatch", "");
            {
                initializeMethod.Line("watcher = new System.IO.FileSystemWatcher();");
                initializeMethod.Line("watcher.Path = FlatRedBall.IO.FileManager.RelativeDirectory + \"content/globalcontent/\";");
                initializeMethod.Line("watcher.NotifyFilter = System.IO.NotifyFilters.LastWrite;");
                initializeMethod.Line("watcher.Filter = \"*.*\";");
                initializeMethod.Line("watcher.Changed += HandleFileChanged;");
                initializeMethod.Line("watcher.EnableRaisingEvents = true;");
            }
        }

        
    }
}
