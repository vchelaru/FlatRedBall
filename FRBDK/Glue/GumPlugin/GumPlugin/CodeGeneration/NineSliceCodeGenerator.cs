using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
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

    // Gum.Wireframe.INineSliceRuntime's full member surface, in declaration order. The interface is
    // hand-written in the sibling Gum repo under #if FRB (Gum/Wireframe/CustomSetPropertyOnRenderable.cs)
    // and exists so that shared file can push variable values into Glue's generated runtime rather than
    // into the NineSlice renderable directly - so it grows as more of TrySetPropertyOnNineSlice migrates
    // to the runtime. Glue cannot read it: it has no reference to GumCore at all (see
    // mStandardElementToQualifiedTypes, which holds runtime type names as plain strings), so this list is
    // the only way to state the contract here. GumGeneratedCodeCompilesTests is what keeps it honest -
    // it compiles the generated runtime against the real built GumCore, so a member added to the
    // interface fails there instead of in a user's game. Issue #1979.
    internal static readonly IReadOnlyList<string> InterfaceMemberNames = new[]
    {
        "Blend",
        "CustomFrameTextureCoordinateWidth",
        "Red",
        "Green",
        "Blue",
        "BorderScale",
        "IsTilingMiddleSections",
    };

    public void AddAdditionalInheritance(StandardElementSave standardElementSave, List<string> inheritanceList)
    {
        if (standardElementSave.Name != "NineSlice" || !HasFrbRuntimeInterfaces)
        {
            return;
        }

        inheritanceList.Add("global::Gum.Wireframe.INineSliceRuntime");
    }

    // Deliberately gated on the same bool as AddAdditionalInheritance above, not on a gate of its own:
    // declaring the interface and implementing it must be one decision, or they drift apart again. There
    // is no new version risk in generating these - Gum.Wireframe.INineSliceRuntime and
    // RenderingLibrary.Graphics.NineSlice compile into the same assembly (GumCore, built from Gum source
    // with FRB defined), so any project whose Gum runtime has the interface has the members it needs.
    internal void AddTypeSpecificVariablesToAddForProperties(
        Dictionary<string, List<VariableSave>> typedVariablesToAddForProperties)
    {
        if (!HasFrbRuntimeInterfaces)
        {
            return;
        }

        // Canonical VariableSaves rather than hand-authored ones, so each member's type, category and
        // name-alias handling stay whatever Gum says they are - hand-writing them would be a second
        // schema to keep in sync. Anything the .gutx already defines is dropped downstream, in
        // StandardsCodeGenerator.GenerateTypeSpecificVariablesToAdd.
        var canonicalDefaultState = Gum.Managers.StandardElementsManager.Self
            .TryGetDefaultStateFor("NineSlice", throwExceptionOnMissing: false);

        if (canonicalDefaultState == null)
        {
            return;
        }

        var variablesToAdd = new List<VariableSave>();

        foreach (var memberName in InterfaceMemberNames)
        {
            var canonicalVariable = canonicalDefaultState.Variables
                .FirstOrDefault(variable => variable.GetRootName() == memberName);

            // Cloned because StandardElementsManager's default states are process-wide and shared with
            // every other consumer of the schema.
            if (canonicalVariable != null)
            {
                variablesToAdd.Add(canonicalVariable.Clone());
            }
        }

        typedVariablesToAddForProperties["NineSlice"] = variablesToAdd;
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

    // Mirrors AddTypeSpecificVariableNamesToSkipForProperties above, but for the state pipeline
    // (StateCodeGenerator). "Animate" must be skipped here PER-TYPE, keyed on "NineSlice" specifically -
    // not as a global variable-name skip - because Sprite has its own, much older "Animate" variable
    // (SpriteCodeGenerator/Sprite.gutx) that is unrelated to NineSlice's and must never be gated by
    // GumNineSliceHasAnimate. A global skip previously suppressed Sprite's Animate state assignment too,
    // for any Sprite instance nested inside a Component, on any project below that version.
    internal void AddTypeSpecificVariableNamesToSkipForStates(Dictionary<string, List<string>> typeSpecificVariableNamesToSkipForStates)
    {
        var variablesToSkip = new List<string>();

        if (!HasNineSliceAnimate)
        {
            variablesToSkip.Add("Animate");
        }

        typeSpecificVariableNamesToSkipForStates["NineSlice"] = variablesToSkip;
    }
}
