using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using GameJsonCommunicationPlugin.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCommunicationPlugin.CodeGeneration;

internal static class ReflectionBasedStateCodeGenerator
{
    public static void AddReflectionStateAssignmentCode(ICodeBlock codeBlock, StateSaveCategory category)
    {
        var isLiveEditEnabled = GameConnectionManager.Self.DoConnections;

        ///////////////////////////////Early Out///////////////////////////////

        if (!isLiveEditEnabled)
        {
            return;
        }

        /////////////////////////////End Early Out/////////////////////////////

        codeBlock.Line("var thisType = this.GetType().FullName;");
        var ifBlock = codeBlock.If("GlueControl.InstanceLogic.Self.StatesAddedAtRuntime.ContainsKey(thisType)");
        {
            ifBlock.Line("var statesForThisElement = GlueControl.InstanceLogic.Self.StatesAddedAtRuntime[thisType];");

            ifBlock.Line($"var category = statesForThisElement.FirstOrDefault(item => item.Name == \"{category.Name}\");");

            ifBlock.Line($"var stateSave = category?.States.FirstOrDefault(item => item.Name == value.Name);");

            var stateNotNullBlock = ifBlock.If("stateSave != null");
            {
                stateNotNullBlock.Line("var throwawayResponse = new global::GlueControl.Dtos.GlueVariableSetDataResponse();");
                stateNotNullBlock.Line("var throwawayConversion = string.Empty;");

                var foreachBlock = stateNotNullBlock.ForEach("var instruction in stateSave.InstructionSaves");
                {
                    foreachBlock.Line("var stateValue = instruction.Value;");

                    foreachBlock.Line("bool convertFileNamesToObjects = true;");

                    var isStringIf = foreachBlock.If("instruction.Value is string valueAsString");
                    {
                        isStringIf.Line("stateValue = global::GlueControl.Editing.VariableAssignmentLogic.ConvertStringToType(instruction.Type, valueAsString, false, out throwawayConversion, convertFileNamesToObjects);");
                    }

                    foreachBlock.Line("GlueControl.Editing.VariableAssignmentLogic.SetVariable(");
                    foreachBlock.Line("    \"this.\" + instruction.Member,");
                    foreachBlock.Line("stateValue, null,");
                    foreachBlock.Line("thisType, throwawayResponse);");
                }
            }
        }
    }
}
