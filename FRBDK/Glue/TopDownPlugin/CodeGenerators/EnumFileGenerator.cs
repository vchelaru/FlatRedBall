using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TopDownPlugin.CodeGenerators
{
    class EnumFileGenerator : Singleton<EnumFileGenerator>
    {
        public void GenerateAndSaveEnumFile()
        {
            TaskManager.Self.AddSync(() =>
            {
                var contents = GenerateFileContents();

                var relativeDirectory = "TopDown/Enums.cs";

                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(relativeDirectory);

                var fullFile = GlueState.Self.CurrentGlueProjectDirectory + relativeDirectory;

                GlueCommands.Self.TryMultipleTimes(() =>
                    System.IO.File.WriteAllText(fullFile, contents));

            }, "Adding top-down enum files to the project");
        }

        private string GenerateFileContents()
        {
            var toReturn =
$@"


namespace {GlueState.Self.ProjectNamespace}.Entities
{{
    public enum TopDownDirection
    {{
        Right = 0,
        UpRight = 1,
        Up = 2,
        UpLeft = 3,
        Left = 4,
        DownLeft = 5,
        Down = 6,
        DownRight = 7
    }}

    public enum PossibleDirections
    {{
        LeftRight,
        FourWay,
        EightWay
    }}

}}

";
            return toReturn;
        }
    }
}
