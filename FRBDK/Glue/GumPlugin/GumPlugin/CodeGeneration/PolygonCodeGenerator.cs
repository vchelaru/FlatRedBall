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

public class PolygonCodeGenerator
{
    private readonly GlueState _glueState;

    bool HasFrbRuntimeInterfaces =>
        _glueState.CurrentGlueProject?.FileVersion >= (int)GluxVersions.GumHasFrbRuntimeInterfaces ||
        _glueState.CurrentMainProject?.IsFrbSourceLinked() == true;

    public PolygonCodeGenerator(GlueState glueState)
    {
        _glueState = glueState;
    }

    public void AddAdditionalInheritance(StandardElementSave standardElementSave, List<string> inheritanceList)
    {
        if (standardElementSave.Name != "Polygon" || !HasFrbRuntimeInterfaces)
        {
            return;
        }

        inheritanceList.Add("global::Gum.Wireframe.IPolygonRuntime");
    }

    public void GenerateAdditionalMethods(StandardElementSave standardElementSave, ICodeBlock classBodyBlock)
    {
        if (standardElementSave.Name != "Polygon")
        {
            return;
        }

        if(_glueState.CurrentGlueProject.FileVersion >=
            (int)GluxVersions.GumHasGueVirtualIsPointInside)
        {
            GenerateIsPointInside(classBodyBlock);
        }
    }

    private void GenerateIsPointInside(ICodeBlock classBodyBlock)
    {
        classBodyBlock.Line("public override bool IsPointInside(float x, float y) => ContainedPolygon.IsPointInside(x, y);");
    }
}
