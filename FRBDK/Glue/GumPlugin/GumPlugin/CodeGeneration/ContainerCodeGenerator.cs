using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.DataTypes;
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

        if(!HasIsRenderTarget)
        {
            typeSpecificVariableNamesToSkipForStates["Container"] = new List<string>
            {
                "Alpha",
                "Blend",
                "IsRenderTarget"
            };
        }
    }

    internal void AddTypeSpecificVariableNamesToSkipForProperties(Dictionary<string, List<string>> typedVariableNamesToSkipForProperties)
    {

    }
}

