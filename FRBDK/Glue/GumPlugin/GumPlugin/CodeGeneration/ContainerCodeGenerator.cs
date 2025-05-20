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
        if(!HasIsRenderTarget)
        {
            variableNamesToSkipForProperties.Add("IsRenderTarget");
            variableNamesToSkipForProperties.Add("Alpha");

        }
    }

    public void AddVariableNamesToSkipForStates(List<string> variableNamesToSkipForStates)
    {
        if (!HasIsRenderTarget)
        {
            variableNamesToSkipForStates.Add("IsRenderTarget");
            variableNamesToSkipForStates.Add("Alpha");
        }
    }
}
