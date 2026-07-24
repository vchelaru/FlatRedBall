using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.DataTypes;
using Newtonsoft.Json.Linq;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace GumPlugin.CodeGeneration;
public class ContainerCodeGenerator
{
    private readonly GlueState _glueState;

    bool HasIsRenderTarget =>
        _glueState.CurrentGlueProject?.FileVersion >= (int)GluxVersions.GumVisualHasRenderTarget ||
        _glueState.CurrentMainProject?.IsFrbSourceLinked() == true;

    public ContainerCodeGenerator(GlueState glueState)
    {
        _glueState = glueState;
    }

    public void GenerateAdditionalMethods(StandardElementSave standardElementSave, ICodeBlock classBodyBlock)
    {
        if (standardElementSave.Name != "Container" || !HasIsRenderTarget)
        {
            return;
        }

        GenerateAlphaProperty(classBodyBlock);
        GenerateIsRenderTargetProperty(classBodyBlock);
        GenerateBlendProperty(classBodyBlock);
    }

    private void GenerateBlendProperty(ICodeBlock classBodyBlock)
    {
        var property = classBodyBlock.Property("public global::Gum.RenderingLibrary.Blend", "Blend")
            .Line("get { if (mContainedObjectAsIpso?.BlendState != null) return global::Gum.RenderingLibrary.BlendExtensions.ToBlend(mContainedObjectAsIpso.BlendState); else return global::Gum.RenderingLibrary.Blend.Normal; }")
            .Line("set { if (mContainedObjectAsIpso is global::RenderingLibrary.Graphics.InvisibleRenderable invisibleRenderable) invisibleRenderable.BlendState = global::Gum.RenderingLibrary.BlendExtensions.ToBlendState(value); }");

        //get { if (mContainedObjectAsIpso?.BlendState != null) return global::Gum.RenderingLibrary.BlendExtensions.ToBlend(mContainedObjectAsIpso.BlendState); else return global::Gum.RenderingLibrary.Blend.Normal; }
        //set { if (mContainedObjectAsIpso is global::RenderingLibrary.Graphics.InvisibleRenderable invisibleRenderable) invisibleRenderable.BlendState = global::Gum.RenderingLibrary.BlendExtensions.ToBlendState(value); }
    }

    private void GenerateAlphaProperty(ICodeBlock classBodyBlock)
    {
        var property = classBodyBlock.Property("public new int", "Alpha")
            .Line("get => mContainedObjectAsIpso?.Alpha ?? 255;")
            .Line("set { if (mContainedObjectAsIpso is global::RenderingLibrary.Graphics.InvisibleRenderable invisibleRenderable) invisibleRenderable.Alpha = value; }");

    }
    private void GenerateIsRenderTargetProperty(ICodeBlock classBodyBlock)
    {
        var property = classBodyBlock.Property("public new bool", "IsRenderTarget")
            .Line("get => mContainedObjectAsIpso?.IsRenderTarget ?? false;")
            .Line("set { if (mContainedObjectAsIpso is global::RenderingLibrary.Graphics.InvisibleRenderable invisibleRenderable) invisibleRenderable.IsRenderTarget = value; }");
    }


    public void AddVariableNamesToSkipForProperties(List<string> variableNamesToSkipForProperties)
    {
        if (!HasIsRenderTarget)
        {
            variableNamesToSkipForProperties.Add("IsRenderTarget");
            // If we globally exclude alpha, then we remove the built-in Alpha logic for Sprites which has
            // been around for a long time. Don't do that, we need to selectively exclude alpha
            //variableNamesToSkipForProperties.Add("Alpha");

        }
    }

    public void AddVariableNamesToSkipForStates(List<string> variableNamesToSkipForStates)
    {
        if (!HasIsRenderTarget)
        {
            variableNamesToSkipForStates.Add("IsRenderTarget");
            // See above for why we don't exclude alpha here:
            //variableNamesToSkipForStates.Add("Alpha");
        }
    }

    internal void AddTypeSpecificVariableNamesToSkipForStates(Dictionary<string, List<string>> typeSpecificVariableNamesToSkipForStates)
    {
        // Container has no backing runtime type at all (StandardsCodeGenerator.mStandardElementToQualifiedTypes["Container"]
        // is null - CreateContainedObjectMembers generates against a bare IRenderableIpso cast instead).
        // "SourceShaderFile" (Gum v3) has no equivalent surface on Container - permanent, version-
        // independent skip, unlike Alpha/Blend/IsRenderTarget below which are real InvisibleRenderable-backed
        // properties gated on HasIsRenderTarget.
        var variablesToSkip = new List<string> { "SourceShaderFile" };

        if(!HasIsRenderTarget)
        {
            variablesToSkip.Add("Alpha");
            variablesToSkip.Add("Blend");
            variablesToSkip.Add("IsRenderTarget");
        }

        typeSpecificVariableNamesToSkipForStates["Container"] = variablesToSkip;
    }

    internal void AddTypeSpecificVariableNamesToSkipForProperties(Dictionary<string, List<string>> typedVariableNamesToSkipForProperties)
    {
        // See AddTypeSpecificVariableNamesToSkipForStates above - Container has no backing runtime type,
        // so "SourceShaderFile" can't back a generated property at any version. Left unskipped it's
        // generated against ((RenderingLibrary.Graphics.IRenderableIpso)this.RenderableComponent), which
        // has no such member (CS1061).
        typedVariableNamesToSkipForProperties.Add("Container", new List<string> { "SourceShaderFile" });
    }
}

