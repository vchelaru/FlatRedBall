using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using GumPlugin.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPluginCore.CodeGeneration
{
    internal class SpriteCodeGenerator : Singleton<SpriteCodeGenerator>
    {
        public void GenerateAdditionalMethods(StandardElementSave standardElementSave, ICodeBlock classBodyBlock)
        {
            if (standardElementSave.Name == "Sprite")
            {
                var version = GlueState.Self.CurrentGlueProject.FileVersion;
                var hasProtectedAnimationProperties = version >= (int)GlueProjectSave.GluxVersions.GraphicalUiElementProtectedAnimationProperties;

                var textureCoordinatesMethodBlock = classBodyBlock.Function("public void", "SetTextureCoordinatesFrom", "FlatRedBall.Graphics.Animation.AnimationFrame frbAnimationFrame");

                textureCoordinatesMethodBlock.Line("this.Texture = frbAnimationFrame.Texture;");
                textureCoordinatesMethodBlock.Line("this.TextureAddress = Gum.Managers.TextureAddress.Custom;");
                textureCoordinatesMethodBlock.Line("this.TextureLeft = FlatRedBall.Math.MathFunctions.RoundToInt(frbAnimationFrame.LeftCoordinate * frbAnimationFrame.Texture.Width);");
                textureCoordinatesMethodBlock.Line("this.TextureWidth = FlatRedBall.Math.MathFunctions.RoundToInt((frbAnimationFrame.RightCoordinate - frbAnimationFrame.LeftCoordinate) * frbAnimationFrame.Texture.Width);");
                textureCoordinatesMethodBlock.Line("this.TextureTop = FlatRedBall.Math.MathFunctions.RoundToInt(frbAnimationFrame.TopCoordinate * frbAnimationFrame.Texture.Height);");
                textureCoordinatesMethodBlock.Line("this.TextureHeight = FlatRedBall.Math.MathFunctions.RoundToInt((frbAnimationFrame.BottomCoordinate - frbAnimationFrame.TopCoordinate) * frbAnimationFrame.Texture.Height);");

                var sourceFileNameProperty = classBodyBlock.Property("public string", "SourceFileName");
                sourceFileNameProperty.Line("set => base.SetProperty(\"SourceFile\", value);");

                var playAnimationsAsyncMethodBlock = classBodyBlock.Function("public async System.Threading.Tasks.Task", "PlayAnimationsAsync", "params string[] animations");
                var foreachBlock = playAnimationsAsyncMethodBlock.ForEach("var animation in animations");
                foreachBlock.Line("CurrentChainName = animation;");
                if (hasProtectedAnimationProperties)
                {
                    foreachBlock.Line("mTimeIntoAnimation = 0;");
                    foreachBlock.Line("mCurrentFrameIndex = 0;");
                }

                foreachBlock.Line("UpdateToCurrentAnimationFrame();");
                foreachBlock.Line("// subtract second difference to prevent it from looping once if it happens to fall mid-frame");
                foreachBlock.Line("await FlatRedBall.TimeManager.DelaySeconds(CurrentChain.TotalLength - FlatRedBall.TimeManager.SecondDifference);");


            }
        }
    }
}
