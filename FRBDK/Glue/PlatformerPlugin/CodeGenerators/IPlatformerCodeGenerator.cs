using FlatRedBall.Glue.Plugins.CodeGenerators;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.PlatformerPlugin.Generators
{
    public class IPlatformerCodeGenerator : FullFileCodeGenerator
    {
        public override string RelativeFile => "Platformer/IPlatformer.Generated.cs";

        static IPlatformerCodeGenerator mSelf;
        public static IPlatformerCodeGenerator Self => mSelf ??= new IPlatformerCodeGenerator();

        protected override string GenerateFileContents()
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
        float MaxAbsoluteXVelocity {{ get; }}
        float MaxAbsoluteYVelocity {{ get; }}
        global::FlatRedBall.Input.I1DInput HorizontalInput {{get;}}
    }}
}}";
            return toReturn;
        }
    }
}
