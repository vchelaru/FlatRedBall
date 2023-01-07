using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.CodeGeneration
{
    public partial class StateCodeGenerator
    {
        #region Enums

        public enum InterpolationCharacteristic
        {
            CanInterpolate,
            NeedsVelocityVariable,
            CantInterpolate
        }

        #endregion

        #region Fields

        const string FirstValue = "FirstValue";
        const string SecondValue = "SecondValue";

        #endregion

        #region Methods

        private void GenerateInterpolateBetween(ElementSave elementSave, ICodeBlock currentBlock,
            string enumType, IEnumerable<StateSave> states, StateSaveCategory category)
        {
            // We used to only generate these if there was were any states in this category, but
            // since Gum generated code supports dynamic states, there could be states in a category
            // even if they're not defined in Gum.
            //if (states.Count() > 0)
            {
                currentBlock = currentBlock.Function("public void",
                    "InterpolateBetween", enumType + " firstState, " + enumType + " secondState, float interpolationValue");

                GenerateDebugCheckForInterpolationValueNaN(currentBlock);

                Dictionary<VariableSave, InterpolationCharacteristic> interpolationCharacteristics =
                   new Dictionary<VariableSave, InterpolationCharacteristic>();

                CreateStartingValueVariables(elementSave, states, currentBlock, interpolationCharacteristics);

                currentBlock = currentBlock.Switch("firstState");
                currentBlock = SetInterpolateBetweenValuesForStates(elementSave, enumType, states, currentBlock,
                    interpolationCharacteristics, FirstValue);
                currentBlock = currentBlock.End();

                currentBlock = currentBlock.Switch("secondState");
                currentBlock = SetInterpolateBetweenValuesForStates(elementSave, enumType, states, currentBlock,
                    interpolationCharacteristics, SecondValue);
                currentBlock = currentBlock.End();

                var suspendLayout = GlueState.Self.CurrentGlueProject.FileVersion >= (int)FlatRedBall.Glue.SaveClasses.GlueProjectSave.GluxVersions.GumHasMIsLayoutSuspendedPublic;
                if (suspendLayout)
                {
                    currentBlock.Line("var wasSuppressed = mIsLayoutSuspended;");
                    currentBlock.Line("var shouldSuspend = wasSuppressed == false;");

                    currentBlock.Line("var suspendRecursively = true;");
                    // Although suspending/Resuming is much faster than a full layout, even that can
                    // cost performance, especially if suspending a very complex object, like a list box 
                    // where each item is expensive.
                    // Animations may only change X and Y. If so, then we can do a special case which is
                    // far more efficient.
                    if(category != null)
                    {
                        HashSet<string> allVariables = new HashSet<string>();
                        HashSet<string> allVariableOwners = new HashSet<string>();
                        foreach(var state in states)
                        {
                            var variablesInState = state.Variables.Select(item => item.GetRootName());
                            allVariables.AddRange(variablesInState);

                            var owners = state.Variables.Select(item =>
                            {
                                var sourceObject = item.SourceObject;
                                if(string.IsNullOrEmpty(sourceObject))
                                {
                                    sourceObject = "this";
                                }
                                return sourceObject;
                            });
                            allVariableOwners.AddRange(owners);
                        }



                        var areAllSafe = allVariables.All(item => 
                            item == "X" || 
                            item == "Y" || 
                            item == "Red" || 
                            item == "Green" || 
                            item == "Blue" ||
                            item == "Alpha" ||
                            item == "Visible" 

                            );


                        if(areAllSafe)
                        {
                            currentBlock.Line("// all values assigned in this state do not require recursive updates:");
                            currentBlock.Line("suspendRecursively = false;");

                            currentBlock.Line("var isSafeToInterpolateWithoutSuppression = true;");
                            foreach(var owner in allVariableOwners)
                            {
                                currentBlock.Line($"isSafeToInterpolateWithoutSuppression &= " +
                                    $"{owner}.Parent as Gum.Wireframe.GraphicalUiElement == null && " +
                                    $"{owner}.XUnits == Gum.Converters.GeneralUnitType.PixelsFromSmall && " +
                                    $"{owner}.XOrigin == RenderingLibrary.Graphics.HorizontalAlignment.Left;");
                            }

                            currentBlock.Line("if(isSafeToInterpolateWithoutSuppression) shouldSuspend = false;");
                        }
                    }


                    currentBlock.If("shouldSuspend")
                        .Line("SuspendLayout(suspendRecursively);");
                }

                currentBlock = AssignValuesUsingStartingValues(elementSave, currentBlock, interpolationCharacteristics);

                currentBlock = currentBlock.If("interpolationValue < 1");

                string fieldToAssign;
                if (enumType == "VariableState")
                {
                    fieldToAssign = "mCurrentVariableState";
                }
                else
                {
                    fieldToAssign = "mCurrent" + enumType + "State";
                }

                currentBlock.Line(fieldToAssign + " = firstState;");
                currentBlock = currentBlock.End().Else();
                currentBlock.Line(fieldToAssign + " = secondState;");
                currentBlock = currentBlock.End();

                if(suspendLayout)
                {
                    currentBlock.If("shouldSuspend")
                        .Line("ResumeLayout(suspendRecursively);");
                }
            }
        }

        private void GenerateStateInterpolateBetween(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("#region State Interpolation");

            string enumType = "VariableState";
            IEnumerable<StateSave> states = elementSave.States;
            GenerateInterpolateBetween(elementSave, currentBlock, enumType, states, null);

            foreach (var category in elementSave.Categories)
            {
                enumType = category.Name;
                states = category.States;
                GenerateInterpolateBetween(elementSave, currentBlock, enumType, states, category);

            }

            currentBlock.Line("#endregion");

        }


        private void GenerateStateInterpolateTo(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("#region State Interpolate To");

            string enumType = "VariableState";
            IEnumerable<StateSave> states = elementSave.States;
            GenerateInterpolateTo(elementSave, currentBlock, states, enumType);
            GenerateInterpolateToFromCurrent(currentBlock, enumType, isRelative: false);
            GenerateInterpolateToFromCurrent(currentBlock, enumType, isRelative: true);

            foreach (var category in elementSave.Categories)
            {
                enumType = category.Name;
                states = category.States;
                GenerateInterpolateTo(elementSave, currentBlock, states, enumType);
                GenerateInterpolateToFromCurrent(currentBlock, enumType, isRelative: false);
                GenerateInterpolateToFromCurrent(currentBlock, enumType, isRelative: true);
            }


            currentBlock.Line("#endregion");
        }

        private void GenerateInterpolateToFromCurrent(ICodeBlock currentBlock, string enumType, bool isRelative)
        {
            string functionName = "InterpolateTo";

            string whereToLook = "States";

            if(enumType != "VariableState")
            {
                whereToLook = $"Categories.First(item => item.Name == \"{enumType}\").States";
            }


            string toStateRightSide = $"this.ElementSave.{whereToLook}.First(item => item.Name == toState.ToString());";

            if(isRelative)
            {
                functionName = "InterpolateToRelative";

                toStateRightSide = "AddToCurrentValuesWithState(toState);";
            }

            currentBlock = currentBlock.Function("public FlatRedBall.Glue.StateInterpolation.Tweener", functionName,
                enumType + " toState, double secondsToTake, FlatRedBall.Glue.StateInterpolation.InterpolationType interpolationType, FlatRedBall.Glue.StateInterpolation.Easing easing, object owner = null ");



            currentBlock.Line("Gum.DataTypes.Variables.StateSave current = GetCurrentValuesOnState(toState);");

            currentBlock.Line("Gum.DataTypes.Variables.StateSave toAsStateSave = " + toStateRightSide);

            currentBlock.Line("FlatRedBall.Glue.StateInterpolation.Tweener tweener = new FlatRedBall.Glue.StateInterpolation.Tweener(from: 0, to: 1, duration: (float)secondsToTake, type: interpolationType, easing: easing);");
            currentBlock.If("owner == null")
                .Line("tweener.Owner = this;")
            .End()
            .Else()
                .Line("tweener.Owner = owner;");

            currentBlock.Line("tweener.PositionChanged = newPosition => this.InterpolateBetween(current, toAsStateSave, newPosition);");

            string variableName;
            if(enumType == "VariableState")
            {
                variableName = "CurrentVariableState";
            }
            else
            {
                variableName = $"Current{enumType}State";
            }

            currentBlock.Line($"tweener.Ended += ()=> this.{variableName} = toState;");
            currentBlock.Line("tweener.Start();");

            currentBlock.Line("StateInterpolationPlugin.TweenerManager.Self.Add(tweener);");
            currentBlock.Line("return tweener;");
        }

        private void GenerateDebugCheckForInterpolationValueNaN(ICodeBlock codeBlock)
        {
            codeBlock.Line("#if DEBUG");

            codeBlock.If("float.IsNaN(interpolationValue)")
                .Line("throw new System.Exception(\"interpolationValue cannot be NaN\");")
                .End();

            codeBlock.Line("#endif");
        }

        private void CreateStartingValueVariables(ElementSave element, IEnumerable<StateSave> states, ICodeBlock curBlock, Dictionary<VariableSave, InterpolationCharacteristic> interpolationCharacteristics)
        {
            foreach (StateSave state in states)
            {
                var variables = state.Variables.Where(item => GetIfShouldGenerateStateVariable(item, element)).ToList();

                foreach (VariableSave variableSave in variables)
                {
                    string member = variableSave.MemberNameInCode(element, VariableNamesToReplaceForStates);

                    if (!ContainsKey(interpolationCharacteristics, member, element))
                    {

                        InterpolationCharacteristic interpolationCharacteristic =
                                                        GetInterpolationCharacteristic(variableSave, element);

                        interpolationCharacteristics.Add(variableSave, interpolationCharacteristic);

                        if (interpolationCharacteristic != InterpolationCharacteristic.CantInterpolate)
                        {
                            string stringSuffix = variableSave.MemberNameInCode(element, VariableNamesToReplaceForStates).Replace(".", "");
                            curBlock.Line("bool set" + stringSuffix + FirstValue + " = false;");
                            curBlock.Line("bool set" + stringSuffix + SecondValue + " = false;");

                            string defaultStartingValue = "";

                            string type = variableSave.Type;

                            try
                            {
                                ElementSave categoryContainer;
                                StateSaveCategory stateSaveCategory;
                                if (variableSave.IsState(element, out categoryContainer, out stateSaveCategory, recursive:false))
                                {
                                    var qualifiedCategoryContainerRuntimeName = 
                                        GueDerivingClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(categoryContainer);
                                    type = qualifiedCategoryContainerRuntimeName + ".VariableState";
                                    string defaultValue = "Default";
                                    if (stateSaveCategory != null)
                                    {
                                        type = qualifiedCategoryContainerRuntimeName + "." + stateSaveCategory.Name;
                                        defaultValue = stateSaveCategory.States.First().MemberNameInCode();
                                    }

                                    defaultStartingValue = FlatRedBall.IO.FileManager.RemovePath(type) + "." + defaultValue;
                                }
                                else
                                {
                                    defaultStartingValue = FlatRedBall.Glue.Parsing.TypeManager.GetDefaultForType(variableSave.Type);
                                }
                            }
                            catch
                            {
                                throw new Exception("Could not get a default value for " + variableSave.GetRootName() + " of type " + variableSave.Type);
                            }



                            curBlock.Line(type + " " + member.Replace(".", "") + FirstValue + "= " + defaultStartingValue + ";");
                            curBlock.Line(type + " " + member.Replace(".", "") + SecondValue + "= " + defaultStartingValue + ";");
                        }
                    }
                }
            }
        }

        private ICodeBlock AssignValuesUsingStartingValues(ElementSave element, ICodeBlock curBlock, Dictionary<VariableSave, InterpolationCharacteristic> mInterpolationCharacteristics)
        {
            foreach (KeyValuePair<VariableSave, InterpolationCharacteristic> kvp in mInterpolationCharacteristics)
            {
                var variable = kvp.Key;

                if (kvp.Value != InterpolationCharacteristic.CantInterpolate)
                {
                    string stringSuffix = variable.MemberNameInCode(element, VariableNamesToReplaceForStates).Replace(".", "");

                    curBlock = curBlock.If("set" + stringSuffix + FirstValue + " && set" + stringSuffix + SecondValue);

                    AddAssignmentForInterpolationForVariable(curBlock, variable, element);

                    curBlock = curBlock.End();
                }
            }
            return curBlock;
        }

        private void AddAssignmentForInterpolationForVariable(ICodeBlock curBlock, VariableSave variable, ElementSave container)
        {
            string memberNameInCode = variable.MemberNameInCode(container, VariableNamesToReplaceForStates);
            ElementSave categoryContainer;
            StateSaveCategory stateSaveCategory;

            if (variable.IsState(container, out categoryContainer, out stateSaveCategory, recursive:false) && !string.IsNullOrEmpty(variable.SourceObject))
            {
                string line = string.Format(
                    "{0}.InterpolateBetween({1}FirstValue, {1}SecondValue, interpolationValue);",
                    SaveObjectExtensionMethods.InstanceNameInCode( variable.SourceObject), memberNameInCode.Replace(".", ""));
                curBlock.Line(line);
            }
            else
            {

                switch (variable.Type)
                {
                    case "int":
                        curBlock.Line(string.Format("{0} = FlatRedBall.Math.MathFunctions.RoundToInt({1}FirstValue* (1 - interpolationValue) + {1}SecondValue * interpolationValue);",
                            memberNameInCode, memberNameInCode.Replace(".", "")));

                        break;
                    case "float":
                    case "double":
                    case "decimal":
                        curBlock.Line(string.Format("{0} = {1}FirstValue * (1 - interpolationValue) + {1}SecondValue * interpolationValue;",
                            memberNameInCode, memberNameInCode.Replace(".", "")));
                        break;
                }
            }
        }

        private ICodeBlock SetInterpolateBetweenValuesForStates(ElementSave element, string enumType, IEnumerable<StateSave> states, ICodeBlock curBlock, Dictionary<VariableSave, InterpolationCharacteristic> mInterpolationCharacteristics, string firstOrSecondValue)
        {
            foreach (StateSave state in states)
            {
                curBlock = curBlock.Case(enumType + "." + state.MemberNameInCode());

                foreach (VariableSave variable in state.Variables.Where(item => GetIfShouldGenerateStateVariable(item, element)))
                {
                    var nameInCode = variable.MemberNameInCode(element, VariableNamesToReplaceForStates);

                    if (GetValue(mInterpolationCharacteristics, nameInCode, element) != InterpolationCharacteristic.CantInterpolate)
                    {
                        string stringSuffix = variable.MemberNameInCode(element, VariableNamesToReplaceForStates).Replace(".", "");
                        if (variable.Value == null)
                        {
                            //curBlock.Line("set" + stringSuffix + " = false;");
                        }
                        else
                        {
                            string variableValue = variable.Value.ToString();
                            bool isEntireAssignment;
                            
                            GueDerivingClassCodeGenerator.Self.AdjustVariableValueIfNecessary(variable, element, ref variableValue, out isEntireAssignment);

                            if (isEntireAssignment)
                            {
                                throw new NotImplementedException();
                            }
                            else
                            {
                                curBlock.Line("set" + stringSuffix + firstOrSecondValue + " = true;");

                                curBlock.Line(variable.MemberNameInCode(element, VariableNamesToReplaceForStates).Replace(".", "") + firstOrSecondValue + " = " + variableValue + ";");
                            }
                        }
                    }
                    else if (variable.Value != null) // This value can't be interpolated, but if the user has set a value of 0 or 1, then it should be set
                    {
                        ICodeBlock ifBlock;
                        //  value will come from the first state unless the interpolationValue is 1.
                        // This makes the code behave the same as InterpolateTo which uses instructions.
                        if (firstOrSecondValue == FirstValue)
                        {
                            ifBlock = curBlock.If("interpolationValue < 1");
                        }
                        else
                        {
                            ifBlock = curBlock.If("interpolationValue >= 1");
                        }

                        string variableValue = variable.Value.ToString();
                        bool isEntireAssignment;

                        GueDerivingClassCodeGenerator.Self.AdjustVariableValueIfNecessary(variable, element, ref variableValue, out isEntireAssignment);

                        if(isEntireAssignment)
                        {
                            ifBlock.Line(variableValue);
                        }
                        else
                        {
                            ifBlock.Line("this." + nameInCode + " = " + variableValue + ";");
                        }
                    }
                }
                curBlock = curBlock.End();
            }
            return curBlock;
        }

        private InterpolationCharacteristic GetValue(Dictionary<VariableSave, InterpolationCharacteristic> dictionary, string value, ElementSave container)
        {
            foreach (var kvp in dictionary)
            {
                if (kvp.Key.MemberNameInCode(container, VariableNamesToReplaceForStates) == value)
                {
                    return kvp.Value;
                }
            }

            // This could be a variable tunneling in to 
            // a disabled NOS.  Let's just say we can't interpolate
            // it:
            //throw new ArgumentException();
            return InterpolationCharacteristic.CantInterpolate;

        }

        private InterpolationCharacteristic GetInterpolationCharacteristic(VariableSave variableSave, ElementSave container)
        {
            string variableType = null;
            if (variableSave != null)
            {
                variableType = variableSave.Type;
            }


            if (variableSave != null && variableSave.IsState(container))
            {

                ElementSave categoryContainer;
                StateSaveCategory stateSaveCategory;

                if (variableSave.IsState(container, out categoryContainer, out stateSaveCategory, recursive: false))
                {
                    return InterpolationCharacteristic.CanInterpolate;
                }
                else
                {
                    // it's an exposed variable which cant be interpolated currently
                    return InterpolationCharacteristic.CantInterpolate;
                }
            }

            if (variableSave == null ||
                variableType == null ||
                variableType == "string" ||
                variableType == "bool" ||
                variableType == "Color" ||
                variableSave.IsFile
                )
            {
                return InterpolationCharacteristic.CantInterpolate;
            }

            if (variableType == "float" || variableType == "int" || variableType == "double" || variableType == "byte" || variableType == "decimal")
            {
                return InterpolationCharacteristic.CanInterpolate;
            }
            return InterpolationCharacteristic.CantInterpolate;

        }

        private bool ContainsKey(Dictionary<VariableSave, InterpolationCharacteristic> dictionary, string member, ElementSave container)
        {
            foreach (KeyValuePair<VariableSave, InterpolationCharacteristic> kvp in dictionary)
            {
                if (kvp.Key.MemberNameInCode(container, VariableNamesToReplaceForStates) == member)
                {
                    return true;
                }
            }
            return false;
        }

        private void GenerateInterpolateTo(ElementSave elementSave, ICodeBlock codeBlock, IEnumerable<StateSave> states, string enumName)
        {
            string qualifiedEnum = GueDerivingClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(elementSave) + "." +
                FlatRedBall.IO.FileManager.RemovePath( elementSave.Name) + "Runtime." + enumName;

            // Make this thing return the Tweener so the uer can customize it
            string parameters = qualifiedEnum + " fromState," + 
                qualifiedEnum + " toState, double secondsToTake, " + 
                "FlatRedBall.Glue.StateInterpolation.InterpolationType interpolationType, FlatRedBall.Glue.StateInterpolation.Easing easing, object owner = null";

            codeBlock = codeBlock.Function("public FlatRedBall.Glue.StateInterpolation.Tweener", "InterpolateTo", parameters);
            {
                codeBlock.Line("FlatRedBall.Glue.StateInterpolation.Tweener tweener = new FlatRedBall.Glue.StateInterpolation.Tweener(from:0, to:1, duration:(float)secondsToTake, type:interpolationType, easing:easing );");
                codeBlock.If("owner == null")
                    .Line("tweener.Owner = this;")
                .End()
                .Else()
                    .Line("tweener.Owner = owner;");

                codeBlock.Line("tweener.PositionChanged = newPosition => this.InterpolateBetween(fromState, toState, newPosition);");
                codeBlock.Line("tweener.Start();");
                codeBlock.Line("StateInterpolationPlugin.TweenerManager.Self.Add(tweener);");
                codeBlock.Line("return tweener;");

            }
        }

        #endregion
    }
}
