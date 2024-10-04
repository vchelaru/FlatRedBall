using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using GumPlugin.CodeGeneration;
using NAudio.SoundFont;
using SkiaSharp.Skottie;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace GumPlugin.CodeGeneration
{
    internal class SpriteCodeGenerator : Singleton<SpriteCodeGenerator>
    {
        public void AddStandardGetterSetterReplacements(
            Dictionary<string, Action<ICodeBlock>> standardGetterReplacements,
            Dictionary<string, Action<ICodeBlock>> standardSetterReplacements)
        {

            standardSetterReplacements.Add("Texture", (codeBlock) =>
            {
                //codeBlock.Line("ContainedSprite.Texture = value;");
                //codeBlock.Line("UpdateLayout();");

                // This allows the object to prevent unnecessary layouts when texture changes:


                codeBlock.Line("var shouldUpdateLayout = false;");

                codeBlock.Line("int widthBefore = -1;");
                codeBlock.Line("int heightBefore = -1;");

                codeBlock.Line("var isUsingPercentageWidthOrHeight = WidthUnits == Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile || HeightUnits == Gum.DataTypes.DimensionUnitType.PercentageOfSourceFile;");
                codeBlock.Line("if (isUsingPercentageWidthOrHeight)");
                codeBlock.Line("{");
                codeBlock.Line("    if (ContainedSprite.Texture != null)");
                codeBlock.Line("    {");
                codeBlock.Line("        widthBefore = ContainedSprite.Texture.Width;");
                codeBlock.Line("        heightBefore = ContainedSprite.Texture.Height;");
                codeBlock.Line("    }");
                codeBlock.Line("}");
                codeBlock.Line("ContainedSprite.Texture = value;");

                codeBlock.Line("if (isUsingPercentageWidthOrHeight)");
                codeBlock.Line("{");
                codeBlock.Line("    int widthAfter = -1;");
                codeBlock.Line("    int heightAfter = -1;");
                codeBlock.Line("    if (ContainedSprite.Texture != null)");
                codeBlock.Line("    {");
                codeBlock.Line("        widthAfter = ContainedSprite.Texture.Width;");
                codeBlock.Line("        heightAfter = ContainedSprite.Texture.Height;");
                codeBlock.Line("    }");
                codeBlock.Line("    shouldUpdateLayout = widthBefore != widthAfter || heightBefore != heightAfter;");
                codeBlock.Line("}");

                codeBlock.Line("if (shouldUpdateLayout)");
                codeBlock.Line("{");
                codeBlock.Line("    UpdateLayout();");
                codeBlock.Line("}");
            });

        }

        public void GenerateAdditionalMethods(StandardElementSave standardElementSave, ICodeBlock classBodyBlock)
        {
            if (standardElementSave.Name == "Sprite")
            {

                GenerateSetTextureCoordinatesFrom(classBodyBlock);
                GenerateSourceFileNameProperty(classBodyBlock);
                GenerateCurrentChainNameProperty(classBodyBlock);
                GenerateAnimationChainsProperty(classBodyBlock);
                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.TimeManagerHasDelaySeconds)
                {
                    GeneratePlayAnimationsAsync(classBodyBlock);

                    GenerateTimeIntoAnimation(classBodyBlock);
                }

                StandardsCodeGenerator.Self.GenerateVariable(classBodyBlock, "ContainedSprite",
                    new VariableSave { Name = "Texture", Type = "Microsoft.Xna.Framework.Graphics.Texture2D" },
                    standardElementSave);


            }
        }


        private void GenerateTimeIntoAnimation(ICodeBlock classBodyBlock)
        {
            classBodyBlock.Line("public double TimeIntoAnimation");
            classBodyBlock.Line("{");
            classBodyBlock.Line("    get => ContainedSprite.TimeIntoAnimation;");
            classBodyBlock.Line("    set => ContainedSprite.TimeIntoAnimation = value;");
            classBodyBlock.Line("}");
        }

        private void GenerateCurrentChainNameProperty(ICodeBlock classBodyBlock)
        {
            var hasCommon = GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumCommonCodeReferencing ||
                GlueState.Self.CurrentMainProject.IsFrbSourceLinked();
            if (hasCommon)
            {
                var sourceFileNameProperty = classBodyBlock.Property("public string", "CurrentChainName");
                sourceFileNameProperty.Line("get => ContainedSprite.CurrentChainName;");

                var setter = sourceFileNameProperty.Set();
                setter.Line("ContainedSprite.CurrentChainName = value;");

                setter.If("ContainedSprite.UpdateToCurrentAnimationFrame()")
                    .Line("UpdateTextureValuesFrom(ContainedSprite);");
            }
        }
        private void GenerateAnimationChainsProperty(ICodeBlock classBodyBlock)
        {
            var hasCommon = GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumCommonCodeReferencing ||
                GlueState.Self.CurrentMainProject.IsFrbSourceLinked();
            if (hasCommon)
            {
                var sourceFileNameProperty = classBodyBlock.Property("public Gum.Graphics.Animation.AnimationChainList", "AnimationChains");
                sourceFileNameProperty.Line("get => ContainedSprite.AnimationChains;");

                var setter = sourceFileNameProperty.Set();
                setter.Line("ContainedSprite.AnimationChains = value;");

                setter.If("ContainedSprite.UpdateToCurrentAnimationFrame()")
                    .Line("UpdateTextureValuesFrom(ContainedSprite);");
            }

        }

        private static void GenerateSourceFileNameProperty(ICodeBlock classBodyBlock)
        {
            var sourceFileNameProperty = classBodyBlock.Property("public string", "SourceFileName");
            var setter = sourceFileNameProperty.Set();
            setter.Line("base.SetProperty(\"SourceFile\", value);");

            var hasCommon = GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumCommonCodeReferencing ||
                GlueState.Self.CurrentMainProject.IsFrbSourceLinked();
            if(hasCommon)
            {
                setter.If("ContainedSprite.UpdateToCurrentAnimationFrame()")
                    .Line("UpdateTextureValuesFrom(ContainedSprite);");
            }

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

                    foreachBlock.Line("sprite.Animate = true;");
                    foreachBlock.Line("sprite.CurrentChainName = animation;");
                    foreachBlock.Line("sprite.TimeIntoAnimation = 0;");
                    foreachBlock.Line("sprite.CurrentFrameIndex = 0;");
                    foreachBlock.Line("sprite.UpdateToCurrentAnimationFrame();");

                    foreachBlock.If("sprite.CurrentChain == null")
                        .Line("throw new System.InvalidOperationException($\"Could not find the animation {animation}\");");

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
