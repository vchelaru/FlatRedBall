using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Gum.DataTypes.Behaviors;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using GumPlugin.DataGeneration;
using Gum.DataTypes;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;

namespace GumPlugin.CodeGeneration;

public class BehaviorCodeGenerator : Singleton<BehaviorCodeGenerator>
{
    internal string GenerateInterfaceCodeFor(BehaviorSave behavior)
    {
        CodeBlockBase fileLevel = new CodeBlockBase(null);
        ICodeBlock namespaceLevel = fileLevel.Namespace(GueDerivingClassCodeGenerator.GueRuntimeNamespace);



        ICodeBlock interfaceBlock = namespaceLevel.Interface("public", GetStrippedBehavorName(behavior), "");

        StateCodeGenerator.Self.GenerateStateEnums(behavior, interfaceBlock);

        GenerateInterface(interfaceBlock, behavior);

        return fileLevel.ToString();
    }

    public string GetStrippedBehavorName(BehaviorSave behavior) => $"I{behavior.Name}";

    public string GetFullyQualifiedBehaviorName(BehaviorSave behavior, bool prefixGlobal = true)
    {
        var toReturn = GueDerivingClassCodeGenerator.GueRuntimeNamespace + "." + GetStrippedBehavorName(behavior);
        if(prefixGlobal)
        {
            toReturn = "global::" + toReturn;
        }
        return toReturn;
    }

    private void GenerateInterface(ICodeBlock codeBlock, BehaviorSave behavior)
    {
        var canGenerate = StateCodeGenerator.Self.SupportsEnumsInInterfaces;
        if(canGenerate)
        {
            foreach (var category in behavior.Categories)
            {
                string propertyName = category.Name;

                codeBlock.Line($"{propertyName} Current{propertyName}State {{set;}}");
            }
        }
    }

    public void GenerateBehaviorImplementingProperties(ICodeBlock codeBlock, ElementSave elementSave)
    {
        if(!StateCodeGenerator.Self.SupportsEnumsInInterfaces)
        {
            return;
        }
        foreach(var behaviorReference in elementSave.Behaviors)
        {
            var behavior = Gum.Managers.ObjectFinder.Self.GetBehavior(behaviorReference);

            if(behavior != null)
            {
                foreach(var behaviorCategory in behavior.Categories)
                {
                    var behaviorPropertyType =
                        $"{GetFullyQualifiedBehaviorName(behavior)}.{behaviorCategory.Name}";

                    var propertyName =
                        $"{GetFullyQualifiedBehaviorName(behavior)}.Current{behaviorCategory.Name}State";

                    var propertyBlock = codeBlock.Property(behaviorPropertyType, propertyName);
                    var setter = propertyBlock.Set();
                    if(behaviorCategory.States.Count > 0)
                    {
                        var switchBlock = setter.Switch("value");

                        var matchingElementCategory = elementSave.Categories.FirstOrDefault(item => item.Name ==  behaviorCategory.Name);

                        foreach(var state in behaviorCategory.States)
                        {
                            var caseBlock = switchBlock.Case($"{behaviorPropertyType}.{state.Name}");

                            var matchingElementState = matchingElementCategory?.States.FirstOrDefault(item => item.Name ==  state.Name);
                            if(matchingElementState == null)
                            {
                                if(matchingElementCategory == null)
                                {
                                    caseBlock.Line($"//Cannot assign this state because this element is missing the category {behaviorCategory.Name}");

                                }
                                else // has the category, but not the state....
                                {
                                    caseBlock.Line($"//Cannot assign this state because this element is missing the state {state.Name}");
                                }
                                caseBlock.Line($"//this.Current{behaviorCategory.Name}State = {behaviorCategory.Name}.{state.Name};");

                            }
                            else
                            {
                                caseBlock.Line($"this.Current{behaviorCategory.Name}State = {behaviorCategory.Name}.{state.Name};");
                            }
                        }
                    }
                }
            }
        }
    }

}
