using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using GumPlugin.CodeGeneration;
using NAudio.SoundFont;
using SkiaSharp.Skottie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace GumPluginCore.CodeGeneration
{
    internal class SpriteCodeGenerator : Singleton<SpriteCodeGenerator>
    {
        public void GenerateAdditionalMethods(StandardElementSave standardElementSave, ICodeBlock classBodyBlock)
        {
            if (standardElementSave.Name == "Sprite")
            {

                GenerateSetTextureCoordinatesFrom(classBodyBlock);
                GenerateSourceFileNameProperty(classBodyBlock);
                GenerateCurrentChainNameProperty(classBodyBlock);
                GeneratePlayAnimationsAsync(classBodyBlock);
            }
        }

        private void GenerateCurrentChainNameProperty(ICodeBlock classBodyBlock)
        {
            var hasCommon = GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumCommonCodeReferencing ||
                GlueState.Self.CurrentMainProject.IsFrbSourceLinked();
            if (hasCommon)
            {
                var sourceFileNameProperty = classBodyBlock.Property("public string", "CurrentChainName");
                sourceFileNameProperty.Line("get => ContainedSprite.CurrentChainName;");
                sourceFileNameProperty.Line("set => ContainedSprite.CurrentChainName = value;");
            }
        }

        private static void GenerateSourceFileNameProperty(ICodeBlock classBodyBlock)
        {
            var sourceFileNameProperty = classBodyBlock.Property("public string", "SourceFileName");
            sourceFileNameProperty.Line("set => base.SetProperty(\"SourceFile\", value);");
        }

        private static void GeneratePlayAnimationsAsync(ICodeBlock classBodyBlock)
        {
            var version = GlueState.Self.CurrentGlueProject.FileVersion;
            var hasCommon = GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumCommonCodeReferencing ||
                GlueState.Self.CurrentMainProject.IsFrbSourceLinked();
            var hasProtectedAnimationProperties = version >= (int)GlueProjectSave.GluxVersions.GraphicalUiElementProtectedAnimationProperties;

            var playAnimationsAsyncMethodBlock = classBodyBlock.Function("public async System.Threading.Tasks.Task", "PlayAnimationsAsync", "params string[] animations");
            var foreachBlock = playAnimationsAsyncMethodBlock.ForEach("var animation in animations");
            {
                if(hasCommon)
                {
                    foreachBlock.Line("var sprite = this.RenderableComponent as RenderingLibrary.Graphics.Sprite;");
                    foreachBlock.Line("sprite.CurrentChainName = animation;");
                    foreachBlock.Line("sprite.TimeIntoAnimation = 0;");
                    foreachBlock.Line("sprite.CurrentFrameIndex = 0;");
                    foreachBlock.Line("sprite.UpdateToCurrentAnimationFrame();");
                    foreachBlock.Line("// subtract second difference to prevent it from looping once if it happens to fall mid-frame");
                    foreachBlock.Line("// Due to frame order, we need to delay one frame less, and multiply by 1.1 to fix possible accuracy issues");
                    foreachBlock.Line("await FlatRedBall.TimeManager.DelaySeconds(sprite.CurrentChain.TotalLength - FlatRedBall.TimeManager.SecondDifference * 1.1f);");
                }
                else
                {
                    foreachBlock.Line("CurrentChainName = animation;");
                    if (hasProtectedAnimationProperties)
                    {
                        foreachBlock.Line("mTimeIntoAnimation = 0;");
                        foreachBlock.Line("mCurrentFrameIndex = 0;");
                    }

                    foreachBlock.Line("UpdateToCurrentAnimationFrame();");
                    foreachBlock.Line("// subtract second difference to prevent it from looping once if it happens to fall mid-frame");
                    foreachBlock.Line("// Due to frame order, we need to delay one frame less, and multiply by 1.1 to fix possible accuracy issues");
                    foreachBlock.Line("await FlatRedBall.TimeManager.DelaySeconds(CurrentChain.TotalLength - FlatRedBall.TimeManager.SecondDifference*1.1f);");
                }


            }
        }

        private static void GenerateSetTextureCoordinatesFrom(ICodeBlock classBodyBlock)
        {
            var textureCoordinatesMethodBlock = classBodyBlock.Function("public void", "SetTextureCoordinatesFrom", "FlatRedBall.Graphics.Animation.AnimationFrame frbAnimationFrame");

            textureCoordinatesMethodBlock.Line("this.Texture = frbAnimationFrame.Texture;");
            textureCoordinatesMethodBlock.Line("this.TextureAddress = Gum.Managers.TextureAddress.Custom;");
            textureCoordinatesMethodBlock.Line("this.TextureLeft = FlatRedBall.Math.MathFunctions.RoundToInt(frbAnimationFrame.LeftCoordinate * frbAnimationFrame.Texture.Width);");
            textureCoordinatesMethodBlock.Line("this.TextureWidth = FlatRedBall.Math.MathFunctions.RoundToInt((frbAnimationFrame.RightCoordinate - frbAnimationFrame.LeftCoordinate) * frbAnimationFrame.Texture.Width);");
            textureCoordinatesMethodBlock.Line("this.TextureTop = FlatRedBall.Math.MathFunctions.RoundToInt(frbAnimationFrame.TopCoordinate * frbAnimationFrame.Texture.Height);");
            textureCoordinatesMethodBlock.Line("this.TextureHeight = FlatRedBall.Math.MathFunctions.RoundToInt((frbAnimationFrame.BottomCoordinate - frbAnimationFrame.TopCoordinate) * frbAnimationFrame.Texture.Height);");
        }
    }
}
