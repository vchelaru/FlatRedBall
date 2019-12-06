using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.CodeGenerators
{
    public abstract class FullFileCodeGenerator
    {
        public abstract string RelativeFile { get; }

        public void GenerateAndSave()
        {
            TaskManager.Self.Add(() =>
            {
                var contents = GenerateFileContents();

                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(RelativeFile);

                var fullPath = GlueState.Self.CurrentGlueProjectDirectory + RelativeFile;

                GlueCommands.Self.TryMultipleTimes(() =>
                    System.IO.File.WriteAllText(fullPath, contents));

            }, $"Adding {RelativeFile}");
        }

        protected abstract string GenerateFileContents();
    }
}
