using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.PlatformerPlugin.Generators
{
    public class EnumFileGenerator : Singleton<EnumFileGenerator>
    {
        string RelativeFileLocation => "Platformer/Enums.Generated.cs";

        public void GenerateAndSave()
        {

            TaskManager.Self.Add(() =>
            {
                var contents = GenerateFileContents();

                var relativeDirectory = RelativeFileLocation;

                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(relativeDirectory);

                var glueProjectDirectory = GlueState.Self.CurrentGlueProjectDirectory;

                if(!string.IsNullOrEmpty(glueProjectDirectory))
                {
                    var fullFile = GlueState.Self.CurrentGlueProjectDirectory + relativeDirectory;

                    try
                    {
                        GlueCommands.Self.TryMultipleTimes(() =>
                            System.IO.File.WriteAllText(fullFile, contents));
                    }
                    catch(Exception e)
                    {
                        GlueCommands.Self.PrintError(e.ToString());
                    }

                    FilePath oldFile = GlueState.Self.CurrentGlueProjectDirectory +
                        "Platformer/Enums.cs";
                    GlueCommands.Self.ProjectCommands.RemoveFromProjects(
                        oldFile, saveAfterRemoving: true);
                }

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

    public static class HorizontalDirectionExtensions
    {{
        public static HorizontalDirection GetInverse(this HorizontalDirection direction)
        {{
            return direction == HorizontalDirection.Left ?
                HorizontalDirection.Right :
                HorizontalDirection.Left;
        }}
    }}
}}

";
            return toReturn;
        }
    }
}
