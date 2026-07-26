using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.DataTypes;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace GumPlugin.CodeGeneration;
public class NineSliceCodeGenerator
{
    private readonly GlueState _glueState;

    bool HasNineSliceAnimate =>
        _glueState.CurrentGlueProject?.FileVersion >= (int)GluxVersions.GumNineSliceHasAnimate ||
        _glueState.CurrentMainProject?.IsFrbSourceLinked() == true;

    bool HasNineSliceTilingMiddleSections =>
        _glueState.CurrentGlueProject?.FileVersion >= (int)GluxVersions.NineSliceHasTilingMiddleSections ||
        _glueState.CurrentMainProject?.IsFrbSourceLinked() == true;

    bool HasFrbRuntimeInterfaces =>
        _glueState.CurrentGlueProject?.FileVersion >= (int)GluxVersions.GumHasFrbRuntimeInterfaces ||
        _glueState.CurrentMainProject?.IsFrbSourceLinked() == true;

    public NineSliceCodeGenerator(GlueState glueState)
    {
        _glueState = glueState;
    }

    public void AddAdditionalInheritance(StandardElementSave standardElementSave, List<string> inheritanceList)
    {
        if (standardElementSave.Name != "NineSlice" || !HasFrbRuntimeInterfaces)
        {
            return;
        }

        inheritanceList.Add("global::Gum.Wireframe.INineSliceRuntime");
    }

    internal void AddTypeSpecificVariableNamesToSkipForProperties(Dictionary<string, List<string>> typedVariableNamesToSkipForProperties)
    {
        var variablesToIgnore = new List<string>();
        typedVariableNamesToSkipForProperties.Add("NineSlice", variablesToIgnore);

        if(!HasNineSliceAnimate)
        {
            variablesToIgnore.Add("Animate");
        }

        if(!HasNineSliceTilingMiddleSections)
        {
            variablesToIgnore.Add("IsTilingMiddleSections");
        }
    }
}
