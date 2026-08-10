using FlatRedBall.Glue.Plugins.CodeGenerators;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RacingPlugin.CodeGenerators
{
    class EnumFileGenerator : FullFileCodeGenerator
    {
        public override string RelativeFile => "RacingEntity/Enums.cs";

        static EnumFileGenerator mSelf;
        public static EnumFileGenerator Self => mSelf ??= new EnumFileGenerator();

        protected override string GenerateFileContents()
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
