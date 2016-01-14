using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue;
using NCalc;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using GlueView.SaveClasses;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Localization;
using FlatRedBall.IO;
using FlatRedBall.Content.Particle;
using FlatRedBall.Graphics.Particle;
using System.Reflection;
using FlatRedBall.Glue.Parsing;
using GlueView.Scripting;

namespace GlueViewOfficialPlugins.Scripting
{
    public class MethodCallParser
    {
        ExpressionParser mExpressionParser;

        public const string LiteralQuoteMethodName = "___LiteralQuote";

        public StringBuilder LogStringBuilder
        {
            get;
            set;
        }


        public MethodCallParser(ExpressionParser expressionParser)
        {
            mExpressionParser = expressionParser;

        }

        private bool TryHandleMethodCall(string name, Expression[] expressions, List<object> dotOperatorStack, ref object result )
        {
            object last = dotOperatorStack.Last();

            if (last is EmitterList // add more types that we want to support
                )
            {
                try
                {
                    Type type = last.GetType();
                    Type[] argTypes = new Type[expressions.Length];
                    List<object> evaluatedExpressions = new List<object>();
                    for(int i = 0; i < expressions.Length; i++)
                    {
                        var expression = expressions[i];

                        object evaluated = expression.Evaluate();
                        evaluatedExpressions.Add(evaluated);

                        if (evaluated != null)
                        {
                            argTypes[i] = evaluated.GetType();
                        }
                        else
                        {
                            // This shouldn't happen...should it?  Hard to say
                            argTypes[i] = typeof(object);
                        }
                    }


                    MethodInfo method = type.GetMethod(name, argTypes);

                    result = method.Invoke(last, evaluatedExpressions.ToArray());
                }
                catch (Exception e)
                {
                    int m = 3;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private void EvaluateTranslate(FunctionArgs args, List<Dictionary<string, object>> localVariableStack, List<object> dotOperatorStack)
        {
            string evaluated = (string)args.Parameters[0].Evaluate();

            args.Result = LocalizationManager.Translate(evaluated);
        }

        private void EvaluateCreateNewInstance(FunctionArgs args, List<Dictionary<string, object>> localVariableStack, List<object> dotOperatorStack)
        {
            string typeAsString = args.Parameters[0].ParsedExpression.ToString();
            typeAsString = typeAsString.Substring(1, typeAsString.Length - 2);

            Type type = TypeManager.GetTypeFromString(typeAsString);
            int constructorParameterCount = args.Parameters.Length - 1;
            object[] constructorParameters;

            constructorParameters = new object[constructorParameterCount];

            for (int i = 0; i < constructorParameterCount; i++)
            {
                constructorParameters[i] = args.Parameters[i + 1].Evaluate();
            }
            
            object toReturn = Activator.CreateInstance(type, constructorParameters);

            args.Result = toReturn;
        }
        
        private void GetEvaluated(ElementRuntime elementRuntime, FunctionArgs args, List<Dictionary<string, object>> localVariableStack, List<object> dotOperatorStack, out object firstEvaluated, out object secondEvaluated)
        {
            firstEvaluated = args.Parameters[0].Evaluate();
            secondEvaluated = args.Parameters[1].Evaluate();
        }

        public static string GetDottedChainString(List<object> dotOperatorStack)
        {
            string toReturn = null;
            for (int i = dotOperatorStack.Count - 1; i > -1; i--)
            {
                if (dotOperatorStack[i] is string)
                {
                    toReturn = dotOperatorStack[i] + "." + toReturn;
                }
                else
                {
                    break;
                }
            }
            return toReturn;
        }

        private bool IsReferencedFileSave(string containerName, IElement iElement)
        {
            return iElement.GetReferencedFileSaveByInstanceNameRecursively(containerName) != null;
        }

        private static void GetContainerAndVariableNames(FunctionArgs args, out string containerName, out string variableName)
        {
            containerName = (string)args.Parameters[0].ParsedExpression.ToString();
            variableName = (string)args.Parameters[1].ParsedExpression.ToString();

            // The ParsedExpression.ToString returns the variable name with brackets
            // so we need to remove the first and last char
            if (containerName.Length > 2)
            {
                containerName = containerName.Substring(1, containerName.Length - 2);
            }
            if (variableName.Length > 2)
            {
                variableName = variableName.Substring(1, variableName.Length - 2);
            }
        }

        private bool IsVariableState(string containerName, IElement element)
        {
            return containerName == "VariableState" || 
                element.GetStateCategoryRecursively(containerName) != null;
        }

        private void HandleFlatRedBallServicesValues(FunctionArgs args, List<object> dotOperatorStack, string containerName, ref bool found, ref bool wasSet)
        {
            if (!found && containerName == "FlatRedBallServices")
            {
                dotOperatorStack.Add("FlatRedBallServices");

                wasSet = true;
                args.Result = args.Parameters[1].Evaluate();

                dotOperatorStack.RemoveAt(dotOperatorStack.Count - 1);
            }
            if (!found && containerName == "GraphicsOptions" && GetDottedChainString(dotOperatorStack) == "FlatRedBallServices.")
            {
                dotOperatorStack.Add("GraphicsOptions");

                wasSet = true;
                args.Result = args.Parameters[1].Evaluate();

                dotOperatorStack.RemoveAt(dotOperatorStack.Count - 1);
            }

        }

        public static CustomVariable GetCustomVariableFromNosOrElement(ElementRuntime elementRuntime, string variableName)
        {
            CustomVariable variable = elementRuntime.AssociatedIElement.GetCustomVariableRecursively(variableName);



            if (variable != null)
            {

                // The NOS may be overwriting the value from the element, so we need to set that if so:
                if (elementRuntime.AssociatedNamedObjectSave != null)
                {
                    NamedObjectSave nos = elementRuntime.AssociatedNamedObjectSave;

                    InstructionSave instruction = nos.GetInstructionFromMember(variable.Name);

                    if (instruction != null && instruction.Value != null)
                    {
                        variable = variable.Clone();
                        variable.DefaultValue = instruction.Value;
                    }
                }
            }
            return variable;
        }

        private bool GetFloatValue(ElementRuntime elementRuntime, FunctionArgs args, string variableName)
        {
            float valueToSet = float.NaN;
            bool wasSet = false;
            switch (variableName)
            {
                case "PositiveInfinity":
                    valueToSet = float.PositiveInfinity;
                    wasSet = true;
                    break;
                case "NegativeInfinity":
                    valueToSet = float.NegativeInfinity;
                    wasSet = true;
                    break;
                case "MaxValue":
                    valueToSet = float.MaxValue;
                    wasSet = true;
                    break;
                case "MinValue":
                    valueToSet = float.MinValue;
                    wasSet = true;
                    break;
            }

            if (wasSet)
            {
                args.Result = valueToSet;
            }

            return wasSet;
        }

        private void EvaluateFunctionGetStaticMember(ElementRuntime elementRuntime, FunctionArgs args, CodeContext codeContext)
        {

            string argument = (string)args.Parameters[0].ParsedExpression.ToString();

            string value = (string) mExpressionParser.EvaluateExpression(argument, codeContext);

            ReferencedFileSave rfs = elementRuntime.AssociatedIElement.GetReferencedFileSaveByInstanceNameRecursively(value);

            args.Result = elementRuntime.LoadReferencedFileSave(rfs, true, elementRuntime.AssociatedIElement);
        }

        private void EvaluateInterpolateBetween(FunctionArgs args, List<Dictionary<string, object>> localVariableStack, List<object> dotOperatorStack)
        {
            // finish here
            if (args.Parameters.Length == 3)
            {
                ElementRuntime elementRuntime = dotOperatorStack.Last() as ElementRuntime;


                object firstStateSaveAsObject = args.Parameters[0].Evaluate();
                object secondStateSaveAsObject = args.Parameters[1].Evaluate();
                object interpolationValueAsObject = args.Parameters[2].Evaluate();

                InterpolateBetween(elementRuntime, firstStateSaveAsObject, secondStateSaveAsObject, interpolationValueAsObject, LogStringBuilder);
                args.Result = SpecialValues.Null;
            }
            else
            {
                // This is bad code!
            }
        }

        public static void InterpolateBetween(ElementRuntime elementRuntime, object firstStateSaveAsObject, object secondStateSaveAsObject, object interpolationValueAsObject,
            StringBuilder logStringBuilder = null)
        {
            StateSave firstStateSave = firstStateSaveAsObject as StateSave;
            StateSave secondStateSave = secondStateSaveAsObject as StateSave;
            float interpolationValue = 0;

            if (interpolationValueAsObject is float)
            {
                interpolationValue = (float)interpolationValueAsObject;
            }
            if (interpolationValueAsObject is double)
            {
                interpolationValue = (float)((double)interpolationValueAsObject);
            }
            else if (interpolationValueAsObject is int)
            {
                interpolationValue = (int)interpolationValueAsObject;
            }

            if (float.IsNaN(interpolationValue))
            {
                throw new Exception("InterpolationValue is NaN");
            }

            StateSave resultingStateSave = StateSaveExtensionMethodsGlueView.CreateCombinedState(firstStateSave, secondStateSave, interpolationValue);
            string nameOfObject = "unnamed object";
            if (elementRuntime.AssociatedNamedObjectSave != null)
            {
                nameOfObject = elementRuntime.AssociatedNamedObjectSave.ToString();
            }
            else if (elementRuntime.AssociatedIElement != null)
            {
                nameOfObject = elementRuntime.AssociatedIElement.ToString();
            }
            if (logStringBuilder != null)
            {
                logStringBuilder.AppendLine("Interpolating " + nameOfObject + " between " + firstStateSave + " and " + secondStateSave + " with value " + interpolationValue);

                foreach (InstructionSave instruction in resultingStateSave.InstructionSaves)
                {
                    logStringBuilder.AppendLine("\t" + instruction);
                }
            }

            try
            {
                elementRuntime.SetState(resultingStateSave, elementRuntime.AssociatedIElement);
            }
            catch (Exception e)
            {
                throw new StateSettingException("Error in script trying to interpolate " + nameOfObject + " between " + firstStateSave + " and " + secondStateSave + " with value " + interpolationValue);
            }
        }

    }
}
