using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.PlatformerPlugin.Generators
{
    class EnumFileGenerator : Singleton<EnumFileGenerator>
    {
        public void GenerateAndSaveEnumFile()
        {

            TaskManager.Self.AddSync(() =>
            {
                var contents = GenerateFileContents();

                var relativeDirectory = "Platformer/Enums.Generated.cs";

                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(relativeDirectory);

                var fullFile = GlueState.Self.CurrentGlueProjectDirectory + relativeDirectory;

                GlueCommands.Self.TryMultipleTimes(() =>
                    System.IO.File.WriteAllText(fullFile, contents));

                FilePath oldFile = GlueState.Self.CurrentGlueProjectDirectory +
                    "Platformer/Enums.cs";
                GlueCommands.Self.ProjectCommands.RemoveFromProjects(
                    oldFile, saveAfterRemoving: true);

            }, "Adding platformer enum files to the project");

            
        }

        private string GenerateFileContents()
        {
            var toReturn =
$@"


namespace {GlueState.Self.ProjectNamespace}.Entities
{{
    public enum MovementType
    {{
        Ground,
        Air,
        AfterDoubleJump
    }}
    public enum HorizontalDirection
    {{
        Left,
        Right
    }}
}}

";
            return toReturn;
        }
    }
}
