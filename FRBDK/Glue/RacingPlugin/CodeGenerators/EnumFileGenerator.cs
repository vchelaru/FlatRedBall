using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RacingPlugin.CodeGenerators
{
    class EnumFileGenerator : Singleton<EnumFileGenerator>
    {
        public void GenerateAndSaveEnumFile()
        {
            TaskManager.Self.Add(() =>
            {
                var contents = GenerateFileContents();

                var relativeDirectory = "RacingEntity/Enums.cs";

                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(relativeDirectory);

                var fullFile = GlueState.Self.CurrentGlueProjectDirectory + relativeDirectory;

                GlueCommands.Self.TryMultipleTimes(() =>
                    System.IO.File.WriteAllText(fullFile, contents));

            }, "Adding racing entity enum files to the project");
        }

        private string GenerateFileContents()
        {
            var toReturn =
$@"
using Microsoft.Xna.Framework;

namespace {GlueState.Self.ProjectNamespace}.Entities
{{
    public enum RacingDirection
    {{
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3,
    }}
}}
";
            return toReturn;
        }
    }
}
