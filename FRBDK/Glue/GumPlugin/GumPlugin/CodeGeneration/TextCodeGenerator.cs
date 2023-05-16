using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using GumPlugin.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPluginCore.CodeGeneration
{
    internal class TextCodeGenerator : Singleton<TextCodeGenerator>
    {
        public void GenerateAdditionalMethods(StandardElementSave standardElementSave, ICodeBlock classBodyBlock)
        {
            if (standardElementSave.Name == "Text")
            {
                if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.GumDefaults2)
                {
                    var overrideTextRenderingPositionModeProperty = classBodyBlock.Property("public RenderingLibrary.Graphics.TextRenderingPositionMode?", "OverrideTextRenderingPositionMode");
                    overrideTextRenderingPositionModeProperty.Line("get => mContainedText.OverrideTextRenderingPositionMode;");
                    overrideTextRenderingPositionModeProperty.Line("set => mContainedText.OverrideTextRenderingPositionMode = value;");
                }
            }
        }

        public void GenerateVariableProperties(StandardElementSave standardElementSave, ICodeBlock currentBlock, string containedGraphicalObjectName)
        {
            if (standardElementSave.Name == "Text")
            {
                // generate text-specific properties here:
                StandardsCodeGenerator.Self.GenerateVariable(currentBlock, containedGraphicalObjectName,
                    new VariableSave { Name = "BitmapFont", Type = "RenderingLibrary.Graphics.BitmapFont" },
                    standardElementSave);

                StandardsCodeGenerator.Self.GenerateVariable(currentBlock, containedGraphicalObjectName,
                    new VariableSave { Name = "WrappedText", Type = "System.Collections.Generic.List<string>" },
                    standardElementSave,
                    generateSetter: false);

            }
        }
    }
}
