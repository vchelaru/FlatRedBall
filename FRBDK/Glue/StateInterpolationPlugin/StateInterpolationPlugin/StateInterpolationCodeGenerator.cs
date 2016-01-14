using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;

namespace OfficialPlugins.StateInterpolation
{
    public class StateInterpolationCodeGenerator : ElementComponentCodeGenerator
    {
        string TweenerNameFor(string enumName)
        {
            return "m" + enumName + "Tweener";
        }
        
        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, FlatRedBall.Glue.SaveClasses.IElement element)
        {
            if (element.SupportsAdvancedInterpolation())
            {
                foreach (var enumName in element.GetStateEnumNames())
                {
                    codeBlock.Line("FlatRedBall.Glue.StateInterpolation.Tweener " + TweenerNameFor(enumName) + ";");
                    codeBlock.Line(enumName + " mFrom" + enumName + "Tween;");
                    codeBlock.Line(enumName + " mTo" + enumName + "Tween;");
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            if (element.SupportsAdvancedInterpolation())
            {
                foreach (var enumName in element.GetStateEnumNames())
                {
                    codeBlock.Line(TweenerNameFor(enumName) + " = new FlatRedBall.Glue.StateInterpolation.Tweener();");
                    codeBlock.Line(TweenerNameFor(enumName) + ".PositionChanged = delegate(float value) { this.InterpolateBetween(this.mFrom" + enumName + "Tween, this.mTo" + enumName + "Tween, value); };");

                    string currentVariableName = "CurrentState";
                    if (enumName != "VariableState")
                    {
                        currentVariableName = "Current" + enumName + "State";
                    }



                    codeBlock.Line(TweenerNameFor(enumName) + ".Ended += delegate() { this." + currentVariableName + " = this.mTo" + enumName + "Tween; };");
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateActivity(ICodeBlock codeBlock, FlatRedBall.Glue.SaveClasses.IElement element)
        {
            if (element.SupportsAdvancedInterpolation())
            {
                foreach (var enumName in element.GetStateEnumNames())
                {
                    codeBlock.Line(TweenerNameFor(enumName) + ".Update(FlatRedBall.TimeManager.SecondDifference);");

                }
            }
            return codeBlock;
        }

        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, FlatRedBall.Glue.SaveClasses.IElement element)
        {
            if (element.SupportsAdvancedInterpolation())
            {

                
                foreach (var enumName in element.GetStateEnumNames())
                {
                    codeBlock = GenerateInterpolateToStateAdvanced(codeBlock, enumName);

                }
            }
            return codeBlock;
        }

        private ICodeBlock GenerateInterpolateToStateAdvanced(ICodeBlock codeBlock, string enumName)
        {
            codeBlock = codeBlock.Function("public void", "InterpolateToState",
                enumName + " fromState, " + enumName + " toState, double secondsToTake, FlatRedBall.Glue.StateInterpolation.InterpolationType interpolationType, FlatRedBall.Glue.StateInterpolation.Easing easing");

            string variableName;
            if (enumName == "VariableState")
            {
                variableName = "CurrentState";
            }
            else
            {
                variableName = "Current" + enumName + "State";
            }

            codeBlock = codeBlock.If("secondsToTake <= 0");
            codeBlock.Line(variableName + " = toState;");
            codeBlock = codeBlock.End().Else();

            // Immediately set the state to the from state:
            codeBlock.Line(variableName + " = fromState;");
            
            codeBlock.Line("mFrom" + enumName + "Tween = fromState;");
            codeBlock.Line("mTo" + enumName + "Tween = toState;");

            codeBlock.Line(
                TweenerNameFor(enumName) + ".Start(0, 1, (float)secondsToTake, FlatRedBall.Glue.StateInterpolation.Tweener.GetInterpolationFunction(interpolationType, easing));");
            codeBlock = codeBlock.End();// else
            codeBlock = codeBlock.End();
            return codeBlock;
        }
    }

    #region StateInterpolationElement ExtensionMethods
    static class StateInterpolationElementExtensionMethods
    {
        public static bool SupportsAdvancedInterpolation(this IElement element)
        {

            if (element is ScreenSave)
            {
                return ((ScreenSave)element).SupportsAdvancedInterpolation();
            }
            else
            {
                return ((EntitySave)element).SupportsAdvancedInterpolation();
            }
        }

        public static List<string> GetStateEnumNames(this IElement element)
        {
            List<string> toReturn = new List<string>();

            const string defaultEnum = "VariableState";

            if(element.States.Count != 0)
            {
                toReturn.Add(defaultEnum);
            }
            foreach (StateSaveCategory category in element.StateCategoryList)
            {
                if(category.SharesVariablesWithOtherCategories == false)
                {
                    toReturn.Add(category.Name);    
                }
                else if (!toReturn.Contains(defaultEnum))
                {
                    toReturn.Add(defaultEnum);
                }
            }

            return toReturn;
        }

        public static bool SupportsAdvancedInterpolation(this ScreenSave screenSave)
        {
            return screenSave.Properties.ContainsValue(StateInterpolationPlugin.VariableName) &&
                screenSave.Properties.GetValue<bool>(StateInterpolationPlugin.VariableName);

        }

        public static bool SupportsAdvancedInterpolation(this EntitySave entitySave)
        {
            return entitySave.Properties.ContainsValue(StateInterpolationPlugin.VariableName) &&
                entitySave.Properties.GetValue<bool>(StateInterpolationPlugin.VariableName);

        }


    }
#endregion

}
