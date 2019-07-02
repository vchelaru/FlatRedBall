using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.CodeGenerators
{
    public class InterfacesFileGenerator : Singleton<InterfacesFileGenerator>
    {
        public void GenerateAndSave()
        {
            TaskManager.Self.Add(() =>
            {
                var contents = GenerateFileContents();

                var relativeFile = "TopDown/Interfaces.cs";

                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(relativeFile);

                var fullPath = GlueState.Self.CurrentGlueProjectDirectory + relativeFile;

                GlueCommands.Self.TryMultipleTimes(() =>
               System.IO.File.WriteAllText(fullPath, contents));

            }, "Adding top-down interface files");
        }

        private string GenerateFileContents()
        {
            var toReturn = $@"


namespace {GlueState.Self.ProjectNamespace}.TopDown
{{
    public interface ITopDownEntity
    {{
        DataTypes.TopDownValues CurrentMovement {{ get; }}
    }}
}}
";
            return toReturn;
        }
    }
}
