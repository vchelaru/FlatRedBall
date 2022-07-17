using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.PlatformerPlugin.Generators
{
    public class IPlatformerCodeGenerator : Singleton<IPlatformerCodeGenerator>
    {
        string RelativeFileLocation => "Platformer/IPlatformer.Generated.cs";

        public void GenerateAndSave()
        {
            TaskManager.Self.Add(() =>
            {
                var contents = GenerateFileContents();

                var relativeDirectory = RelativeFileLocation;

                GlueCommands.Self.ProjectCommands.CreateAndAddCodeFile(relativeDirectory);

                var glueProjectDirectory = GlueState.Self.CurrentGlueProjectDirectory;

                if (!string.IsNullOrEmpty(glueProjectDirectory))
                {
                    var fullFile = GlueState.Self.CurrentGlueProjectDirectory + relativeDirectory;

                    try
                    {
                        GlueCommands.Self.TryMultipleTimes(() =>
                            System.IO.File.WriteAllText(fullFile, contents));
                    }
                    catch (Exception e)
                    {
                        GlueCommands.Self.PrintError(e.ToString());
                    }
                }

            }, "Adding IPlatformer.Generated.cs to the project");
        }

        private string GenerateFileContents()
        {
            var toReturn =
$@"
namespace {GlueState.Self.ProjectNamespace}.Entities
{{
    public interface IPlatformer : FlatRedBall.Math.IPositionable
    {{
        HorizontalDirection DirectionFacing {{ get; }}
        bool IsOnGround {{ get; }}
        string CurrentMovementName {{ get; }}
    }}
}}";
            return toReturn;
        }
    }
}
