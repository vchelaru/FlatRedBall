using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.CodeGenerators;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.PlatformerPlugin.Generators
{
    public class EnumFileGenerator : FullFileCodeGenerator
    {
        public override string RelativeFile => "Platformer/Enums.Generated.cs";

        static EnumFileGenerator mSelf;
        public static EnumFileGenerator Self => mSelf ??= new EnumFileGenerator();

        protected override void AfterSave()
        {
            // What this generator used to be called, before it was suffixed .Generated.
            FilePath oldFile = GlueState.Self.CurrentGlueProjectDirectory + "Platformer/Enums.cs";
            GlueCommands.Self.ProjectCommands.RemoveFromProjects(oldFile, saveAfterRemoving: true);
        }

        protected override string GenerateFileContents()
        {
            var toReturn =
$@"


namespace {GlueState.Self.ProjectNamespace}.Entities
{{
    public enum MovementType
    {{
        Ground,
        Air,
        AfterDoubleJump,
        Climbing
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


        public static float XSign(this HorizontalDirection direction) => direction == HorizontalDirection.Left
            ? -1
            : 1;
    }}
}}

";
            return toReturn;
        }
    }
}
