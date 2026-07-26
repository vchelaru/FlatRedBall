using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using GumPlugin.CodeGeneration;
using NAudio.SoundFont;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace GumPlugin.CodeGeneration;

public class SpriteCodeGenerator
{
    private readonly GlueState _glueState;

    public SpriteCodeGenerator(GlueState glueState)
    {
        _glueState = glueState;
    }

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

    internal void AddTypeSpecificVariableNamesToSkipForProperties(Dictionary<string, List<string>> typedVariableNamesToSkipForProperties)
    {
        // RenderTargetTextureSource is entirely handled by GenerateIRenderTargetTextureReferencerProperties
        // below (correctly typed as IRenderableIpso?, gated on GluxVersions.GumHasIRenderTargetTextureReferencer).
        // The generic property-generation loop must never also generate it from the raw schema (Type
        // "string") - left unskipped, it either mismatches the real backing type (CS0029, below the gate,
        // where it's never generated at all) or duplicates SpriteCodeGenerator's own emission (CS0102, at/
        // above the gate). Permanent, version-independent skip - SpriteCodeGenerator is always the sole
        // source when this property exists.
        typedVariableNamesToSkipForProperties.Add("Sprite", new List<string> { "RenderTargetTextureSource" });
    }

    public void AddAdditionalInheritance(StandardElementSave standardElementSave, List<string> inheritanceList)
    {
        if (standardElementSave.Name != "Sprite")
        {
            return;
        }
        var hasIRenderTargetTextureReferencer = _glueState.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumHasIRenderTargetTextureReferencer;

        if (hasIRenderTargetTextureReferencer)
        {
            inheritanceList.Add("global::RenderingLibrary.Graphics.IRenderTargetTextureReferencer");
        }

        var hasFrbRuntimeInterfaces = _glueState.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumHasFrbRuntimeInterfaces ||
            _glueState.CurrentMainProject.IsFrbSourceLinked();

        if (hasFrbRuntimeInterfaces)
        {
            inheritanceList.Add("global::Gum.Wireframe.ISpriteRuntime");
        }
    }

    public void GenerateAdditionalMethods(StandardElementSave standardElementSave, ICodeBlock classBodyBlock)
    {
        if (standardElementSave.Name != "Sprite")
        {
            return;
        }

        GenerateSetTextureCoordinatesFrom(classBodyBlock);
        GenerateSourceFileNameProperty(classBodyBlock);
        GenerateSourceRectangle(classBodyBlock);
        GenerateCurrentChainNameProperty(classBodyBlock);
        GenerateAnimationChainsProperty(classBodyBlock);
        if (_glueState.CurrentGlueProject.FileVersion >= (int)GluxVersions.TimeManagerHasDelaySeconds)
        {
            GeneratePlayAnimationChainsAsync(classBodyBlock);

            GenerateTimeIntoAnimation(classBodyBlock);
        }
        GenerateIRenderTargetTextureReferencerProperties(classBodyBlock);

        StandardsCodeGenerator.Self.GenerateVariable(classBodyBlock, "ContainedSprite",
            new VariableSave { Name = "Texture", Type = "Microsoft.Xna.Framework.Graphics.Texture2D" },
            standardElementSave);


    }

    private void GenerateSourceRectangle(ICodeBlock classBodyBlock)
    {
        //public Microsoft.Xna.Framework.Rectangle? SourceRectangle
        //{
            //get => ContainedSprite.SourceRectangle?.ToXNA();
            //set => ContainedSprite.SourceRectangle = value?.ToSystemDrawing();
        //}

        var property = classBodyBlock.Property("public Microsoft.Xna.Framework.Rectangle?", "SourceRectangle")
            .Line("get => ContainedSprite.SourceRectangle != null ? global::RenderingLibrary.Graphics.XNAExtensions.ToXNA(ContainedSprite.SourceRectangle.Value) : null;")
            .Line("set => ContainedSprite.SourceRectangle = value != null ? global::RenderingLibrary.Graphics.XNAExtensions.ToSystemDrawing(value.Value) : null;");

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
        var hasCommon = _glueState.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumCommonCodeReferencing ||
            _glueState.CurrentMainProject.IsFrbSourceLinked();
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
        var hasCommon = _glueState.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumCommonCodeReferencing ||
            _glueState.CurrentMainProject.IsFrbSourceLinked();
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
        if (hasCommon)
        {
            setter.If("ContainedSprite.UpdateToCurrentAnimationFrame()")
                .Line("UpdateTextureValuesFrom(ContainedSprite);");
        }

    }

    private static void GeneratePlayAnimationChainsAsync(ICodeBlock classBodyBlock)
    {
        var version = GlueState.Self.CurrentGlueProject.FileVersion;
        var hasCommon = GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumCommonCodeReferencing ||
            GlueState.Self.CurrentMainProject.IsFrbSourceLinked();
        var hasProtectedAnimationProperties = version >= (int)GlueProjectSave.GluxVersions.GraphicalUiElementProtectedAnimationProperties;

        classBodyBlock.Line("[System.Obsolete(\"Use PlayAnimationChainsAsync instead\")]");
        var playAnimationsAsyncMethodBlock = classBodyBlock.Function("public async System.Threading.Tasks.Task", "PlayAnimationsAsync", "params string[] animations");
        playAnimationsAsyncMethodBlock.Line("await PlayAnimationChainsAsync(animations);");


        var playAnimationChainssAsyncMethodBlock = classBodyBlock.Function("public async System.Threading.Tasks.Task", "PlayAnimationChainsAsync", "params string[] animations");



        var foreachBlock = playAnimationChainssAsyncMethodBlock.ForEach("var animation in animations");
        {
            if (hasCommon)
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

    private void GenerateIRenderTargetTextureReferencerProperties(ICodeBlock classBodyBlock)
    {
        var hasIRenderTargetTextureReferencer = _glueState.CurrentGlueProject.FileVersion >= (int)GluxVersions.GumHasIRenderTargetTextureReferencer;

        if(!hasIRenderTargetTextureReferencer)
        {
            return;
        }

        var property = classBodyBlock.Property(
            "public global::RenderingLibrary.Graphics.IRenderableIpso?",
            "RenderTargetTextureSource");

        property.Get()
            .Line("return ContainedSprite.RenderTargetTextureSource;");

        property.Set()
            .If("ContainedSprite.RenderTargetTextureSource != value")
            .Line("ContainedSprite.RenderTargetTextureSource = value;")
            .Line("UpdateLayout();");

        property = classBodyBlock.Property(
            "global::RenderingLibrary.Graphics.IRenderableIpso?",
            "global::RenderingLibrary.Graphics.IRenderTargetTextureReferencer.RenderTargetTextureSource");

        property.Get()
            .Line("return ContainedSprite.RenderTargetTextureSource;");
    }
}
