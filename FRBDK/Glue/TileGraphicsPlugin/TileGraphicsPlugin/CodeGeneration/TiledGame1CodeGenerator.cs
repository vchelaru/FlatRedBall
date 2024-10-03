using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TiledPlugin.CodeGeneration
{
    public class TiledGame1CodeGenerator : Game1CodeGenerator
    {
        public override void GenerateInitialize(ICodeBlock codeBlock)
        {
            codeBlock.Line("// This value is used for parallax. If the game doesn't change its resolution, this this code should solve parallax with zooming cameras.");
            codeBlock.Line($"global::FlatRedBall.TileGraphics.MapDrawableBatch.NativeCameraWidth = global::{GlueState.Self.ProjectNamespace}.CameraSetup.Data.ResolutionWidth;");
            codeBlock.Line($"global::FlatRedBall.TileGraphics.MapDrawableBatch.NativeCameraHeight = global::{GlueState.Self.ProjectNamespace}.CameraSetup.Data.ResolutionHeight;");
        }
    }
}
