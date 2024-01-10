using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Instructions;
using FlatRedBall.Glue.SaveClasses.Helpers;
using FlatRedBall.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.CodeGeneration
{
    public partial class StateCodeGenerator
    {
        const string FirstValue = "FirstValue";
        const string SecondValue = "SecondValue";

        private static void GenerateInterpolationAdditionalMethods(ICodeBlock codeBlock, GlueElement element, List<StateSave> sharedVariableStates)
        {
            StateCodeGenerator.GenerateInterpolateToStateMethod(element, codeBlock, "VariableState", sharedVariableStates);
            StateCodeGenerator.GenerateInterpolateBetweenMethod(element, codeBlock, "VariableState", sharedVariableStates);

            foreach (StateSaveCategory category in element.StateCategoryList)
            {
                StateCodeGenerator.GenerateInterpolateToStateMethod(element, codeBlock, category.Name, category.States);
                StateCodeGenerator.GenerateInterpolateBetweenMethod(element, codeBlock, category.Name, category.States);
            }
        }
        
        private static ICodeBlock GenerateInterpolateForIndividualState(IElement element, ICodeBlock codeBlock, ICodeBlock otherBlock, StateSave stateSave, string enumType, bool isElse)
        {

            //.Switch("stateToInterpolateTo");

            //otherBlock = otherBlock
            //    .Function("public void", "StopStateInterpolation", enumType + " stateToStop");
            //.Switch("stateToStop");

            if (isElse)
            {
                codeBlock = codeBlock.ElseIf($"stateToInterpolateTo == {enumType}.{stateSave.Name}");
                otherBlock = otherBlock.ElseIf($"stateToStop == {enumType}.{stateSave.Name}");
            }
            else
            {
                codeBlock = codeBlock.If($"stateToInterpolateTo == {enumType}.{stateSave.Name}");
                otherBlock = otherBlock.If($"stateToStop == {enumType}.{stateSave.Name}");
            }

            var currentInstructionSaves = stateSave.InstructionSaves.ToList();

            //Adding instructions for default values
            var category = element.GetStateCategory(enumType);
            
            foreach(var variable in element.CustomVariables)
            {
                if (category?.ExcludedVariables.Contains(variable.Name) == true)
                    continue;

                if (currentInstructionSaves.Any(item => item.Member == variable.Name))
                    continue;

                currentInstructionSaves.Add(new InstructionSave
                {
                    Member = variable.Name,
                    Value = element.GetCustomVariable(variable.Name).DefaultValue
                });
            }
            
            foreach (InstructionSave instruction in currentInstructionSaves)
            {

                CustomVariable customVariable = null;
                customVariable = element.GetCustomVariable(instruction.Member);

                string valueAsString = CodeParser.ConvertValueToCodeString(instruction.Value);

                if (customVariable != null && !string.IsNullOrEmpty(valueAsString))
                {
                    NamedObjectSave sourceNamedObjectSave = element.GetNamedObjectRecursively(customVariable.SourceObject);

                    if (sourceNamedObjectSave == null || sourceNamedObjectSave.IsDisabled == false)
                    {
                        if (sourceNamedObjectSave != null)
                        {
                            NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(codeBlock, sourceNamedObjectSave);
                            NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(otherBlock, sourceNamedObjectSave);
                        }
                        string timeCastString = "";

                        if (instruction.Value is float)
                        {
                            timeCastString = "(float)";
                        }

                        if (string.IsNullOrEmpty(customVariable.SourceObject))
                        {
                            GenerateInterpolateForIndividualStateNoSource(ref codeBlock, element, ref otherBlock, instruction, customVariable, valueAsString, timeCastString);
                        }
                        else
                        {
                            GenerateInterpolateForIndividualStateWithSource(ref codeBlock, element, ref otherBlock, customVariable, valueAsString, sourceNamedObjectSave, timeCastString);
                        }
                        if (sourceNamedObjectSave != null)
                        {
                            NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(codeBlock, sourceNamedObjectSave);
                            NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(otherBlock, sourceNamedObjectSave);
                        }
                    }
                }
            }

            return codeBlock;
        }

        private static void GenerateInterpolateForIndividualStateWithSource(ref ICodeBlock codeBlock, IElement element, ref ICodeBlock otherBlock, CustomVariable customVariable, string valueAsString, NamedObjectSave sourceNamedObjectSave, string timeCastString)
        {
            if (customVariable.GetIsVariableState())
            {
                GenerateInterpolateForIndividualStateWithSourceStateVariable(codeBlock, customVariable, element, valueAsString.Replace("\"", ""));
            }
            else
            {
                string velocityMember =
                    FlatRedBall.Instructions.InstructionManager.GetVelocityForState(customVariable.SourceObjectProperty);



                bool generatedVelocity = false;

                if (velocityMember == null &&
                    customVariable.HasAccompanyingVelocityProperty)
                {
                    velocityMember = customVariable.Name + "Velocity";
                    generatedVelocity = true;
                }
                bool velocityComesFromTunnel = false;

                if (velocityMember == null &&
                    // Only want to go 1 deep.  The reason is if the tunneled variable has a
                    // velocity value, we can use that.  However, if it's tunneled multiple times
                    // into a variable that ultimately has a velocity variable we may not be able to
                    // get to it, so we shouldn't just assume we can add "Velocity" to the variable name.
                    customVariable.HasAccompanyingVelocityConsideringTunneling(element, 1))
                {
                    velocityMember = customVariable.SourceObjectProperty + "Velocity";
                    generatedVelocity = true;
                    velocityComesFromTunnel = true;
                }

                if (!string.IsNullOrEmpty(velocityMember))
                {
                    string sourceDot = customVariable.SourceObject + ".";

                    IEnumerable<string> exposableVariables = 
                        FlatRedBall.Glue.Reflection.ExposedVariableManager.GetExposableMembersFor(sourceNamedObjectSave).Select(item=>item.Member);

                    // We will generate this if the variable is contained in exposable variables,
                    // if the user explicitly said to generate a velocity variable, or if
                    // this is a FRB type. The reason we check if it's a FRB Type is because FRB
                    // types have velocity variables which may not be exposable. We don't want Glue
                    // to mess with temporary values like Velocity, so they are removed from the exposab
                    // variable list:
                    bool shouldGenerate = exposableVariables.Contains(velocityMember) ||
                        generatedVelocity ||
                        sourceNamedObjectSave.SourceType == SourceType.FlatRedBallType;


                    if (shouldGenerate)
                    {
                        string relativeVelocity = InstructionManager.GetRelativeForAbsolute(velocityMember);

                        string leftHandPlusEquals = null;

                        if (!string.IsNullOrEmpty(relativeVelocity))
                        {
                            codeBlock = codeBlock
                                .If(customVariable.SourceObject + ".Parent != null");

                            otherBlock = otherBlock
                                .If(customVariable.SourceObject + ".Parent != null");

                            string sourceObjectPropertyRelative = InstructionManager.GetRelativeForAbsolute(customVariable.SourceObjectProperty);

                            leftHandPlusEquals = sourceDot + relativeVelocity + " = ";

                            codeBlock.Line(leftHandPlusEquals + "(" + valueAsString + " - " + sourceDot +
                                           sourceObjectPropertyRelative + ") / " + timeCastString +
                                           "secondsToTake;");

                            otherBlock.Line(leftHandPlusEquals + " 0;");

                            codeBlock = codeBlock
                                .End()
                                .Else();

                            otherBlock = otherBlock
                                .End()
                                .Else();
                        }

                        // If we're using a custom velocity value, we don't want to 
                        // use the sourceDot.  We just want to use the velocity value
                        if (generatedVelocity && !velocityComesFromTunnel)
                        {
                            leftHandPlusEquals = velocityMember + " = ";
                        }
                        else
                        {
                            leftHandPlusEquals = sourceDot + velocityMember + " = ";
                        }

                        // Sept 9, 2022
                        // If the variable is referencing a source variable, the variable may be a "converted" type which overrides
                        // the base type.
                        // In that case, the tunneled variable could interpoalte, but the base couldn't, so we should use the
                        // exposed:
                        //codeBlock.Line(leftHandPlusEquals + "(" + valueAsString + " - " + sourceDot +
                        //               customVariable.SourceObjectProperty + ") / " + timeCastString +
                        //               "secondsToTake;");
                        codeBlock.Line($"{leftHandPlusEquals} ({valueAsString} - {customVariable.Name}) / {timeCastString}secondsToTake;");

                        otherBlock.Line(leftHandPlusEquals + " 0;");

                        if (!string.IsNullOrEmpty(relativeVelocity))
                        {
                            codeBlock = codeBlock.End();
                            otherBlock = otherBlock.End();
                        }
                    }
                }
            }
        }

        private static void GenerateInterpolateForIndividualStateWithSourceStateVariable(ICodeBlock codeBlock, CustomVariable variable, IElement container, string value)
        {
            if (string.IsNullOrEmpty(value) == false)
            {
                var nos = container.GetNamedObjectRecursively(variable.SourceObject);



                var sourceElement = ObjectFinder.Self.GetIElement(nos.SourceClassType);
                // InterpolationEntityInstance.InterpolateToState(InterpolationEntity.VariableState.Small, secondsToTake);
                //
                string type = "VariableState";
                if (variable != null && !String.Equals(variable.Type, "string", StringComparison.OrdinalIgnoreCase))
                {
                    type = variable.Type;
                }


                codeBlock.Line(variable.SourceObject + ".InterpolateToState(" + 
                    sourceElement.GetQualifiedName(ProjectManager.ProjectNamespace) + "." + type + "." + value + ", secondsToTake);");
            }
        }

        private static void GenerateInterpolateForIndividualStateNoSource(ref ICodeBlock codeBlock, IElement element, ref ICodeBlock otherBlock, InstructionSave instruction, CustomVariable customVariable, string valueAsString, string timeCastString)
        {
            string velocityMember =
                FlatRedBall.Instructions.InstructionManager.GetVelocityForState(instruction.Member);

            // If the velocityMember exists, we need to make sure it's actually exposable
            if (!ExposedVariableManager.GetExposableMembersFor(element, false).Any(item=>item.Member==velocityMember))
            {
                velocityMember = null;
            }

            if (velocityMember == null && customVariable.HasAccompanyingVelocityProperty)
            {
                velocityMember = customVariable.Name + "Velocity";
            }

            if (!string.IsNullOrEmpty(velocityMember))
            {
                string relativeVelocity = InstructionManager.GetRelativeForAbsolute(velocityMember);

                string leftHandPlusEquals = null;

                if (!string.IsNullOrEmpty(relativeVelocity))
                {
                    codeBlock = codeBlock
                        .If("this.Parent != null");

                    otherBlock = otherBlock
                        .If("this.Parent != null");

                    string instructionMemberRelative = InstructionManager.GetRelativeForAbsolute(instruction.Member);

                    leftHandPlusEquals = relativeVelocity + " = ";

                    codeBlock.Line(leftHandPlusEquals + "(" + valueAsString + " - " +
                                   instructionMemberRelative + ") / " + timeCastString + "secondsToTake;");

                    otherBlock.Line(leftHandPlusEquals + " 0;");

                    codeBlock = codeBlock
                        .End()
                        .Else();

                    otherBlock = otherBlock
                        .End()
                        .Else();
                }
                leftHandPlusEquals = velocityMember + " = ";

                codeBlock.Line(leftHandPlusEquals + "(" + valueAsString + " - " + instruction.Member +
                               ") / " + timeCastString + "secondsToTake;");

                otherBlock.Line(leftHandPlusEquals + " 0;");

                if (!string.IsNullOrEmpty(relativeVelocity))
                {
                    codeBlock = codeBlock.End();
                    otherBlock = otherBlock.End();
                }
            }
        }

        static bool ContainsKey(Dictionary<InstructionSave, InterpolationCharacteristic> dictionary, string member)
        {
            foreach (KeyValuePair<InstructionSave, InterpolationCharacteristic> kvp in dictionary)
            {
                if (kvp.Key.Member == member)
                {
                    return true;
                }
            }
            return false;
        }


        private static ICodeBlock GenerateInterpolateBetweenMethod(GlueElement element, ICodeBlock codeBlock, string enumType, List<StateSave> states)
        {
            var curBlock = codeBlock;


            // Right now we
            // create the InterpolateBetween
            // method if there are any states
            // in the argument List.  We may want
            // to only create the InterpolateBetween
            // method only if there are variables that
            // can be interpolated between - do we want
            // to do this?  Not creating the method will
            // clean up the Entity/Screen's interface, but
            // the programmer should not have to remove calls
            // to this method if the designer decides to modify
            // variables or states.  Also, always creating the method
            // means that Entities/Screens can be stubbed out and coded
            // against before the states are filled.
            // Update September 18, 2012 by Victor Chelaru
            // We used to only generate this method if there was more than
            // one state in the entity, but instead we want to generate it if
            // there are any for two reasons - so that code doesn't break and so
            // plugins (like the advanced state interpolator) can simply check for
            // the presence of state categories and not worry about the complexity of
            // looking for more than one state.
            //if (states.Count > 1)
            if (states.Count > 0)
            {

                var elementNameWithoutPath = FileManager.RemovePath(element.Name);

                curBlock = curBlock
                    .Function("public void", "InterpolateBetween", enumType + " firstState, " + enumType + " secondState, float interpolationValue");

                GenerateDebugCheckForInterpolationValueNaN(curBlock);
                // Create the bools
                StateSave firstState = states[0];

                Dictionary<InstructionSave, InterpolationCharacteristic> interpolationCharacteristics =
                    new Dictionary<InstructionSave, InterpolationCharacteristic>();

                // states only include instructions for things they set.  Otherwise they'll be null

                CreateStartingValueVariables(element, states, curBlock, interpolationCharacteristics);

                curBlock = SetInterpolateBetweenValuesForStates(element, enumType, states, curBlock, interpolationCharacteristics, FirstValue, "firstState");

                curBlock = SetInterpolateBetweenValuesForStates(element, enumType, states, curBlock, interpolationCharacteristics, SecondValue, "secondState");


                curBlock = AssignValuesUsingStartingValues(element, curBlock, interpolationCharacteristics);


                curBlock = curBlock.If("interpolationValue < 1");
//                mCurrentCircleVisibilityState = (int)firstState;
                // 
                string fieldToAssign;
                if (enumType == "VariableState")
                {
                    fieldToAssign = "mCurrentState";
                }
                else
                {
                    fieldToAssign = "mCurrent" + enumType + "State";
                }

                curBlock.Line(fieldToAssign + " = firstState;");
                curBlock = curBlock.End().Else();
                curBlock.Line(fieldToAssign + " = secondState;");
                curBlock = curBlock.End();
            }

            return curBlock;
        }

        private static ICodeBlock AssignValuesUsingStartingValues(GlueElement element, ICodeBlock curBlock, Dictionary<InstructionSave, InterpolationCharacteristic> mInterpolationCharacteristics)
        {
            foreach (KeyValuePair<InstructionSave, InterpolationCharacteristic> kvp in mInterpolationCharacteristics)
            {
                if (kvp.Value != InterpolationCharacteristic.CantInterpolate)
                {
                    curBlock = curBlock.If("set" + kvp.Key.Member);

                    CustomVariable variable = element.GetCustomVariable(kvp.Key.Member);
                    var nos = element.GetNamedObjectRecursively(variable.SourceObject);

                    if(nos != null)
                    {
                        NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(curBlock, nos);
                    }

                    string relativeValue = InstructionManager.GetRelativeForAbsolute(kvp.Key.Member);

                    string variableToAssign = kvp.Key.Member;


                    string leftSideOfEqualsWithRelative = GetLeftSideOfEquals(element, variable, kvp.Key.Member, true);

                    if (!string.IsNullOrEmpty(leftSideOfEqualsWithRelative) && leftSideOfEqualsWithRelative != kvp.Key.Member)
                    {
                        string beforeDotParent = variable.SourceObject;

                        if (string.IsNullOrEmpty(variable.SourceObject))
                        {
                            beforeDotParent = "this";
                        }

                        curBlock = curBlock.If(beforeDotParent + ".Parent != null");



                        AddAssignmentForInterpolationForVariable(curBlock, variable, variableToAssign, leftSideOfEqualsWithRelative);
                        curBlock = curBlock.End().Else();
                    }

                    AddAssignmentForInterpolationForVariable(curBlock, variable, variableToAssign, variableToAssign);

                    if (!string.IsNullOrEmpty(relativeValue))
                    {
                        curBlock = curBlock.End(); // end the else
                    }

                    curBlock = curBlock.End();
                    if (nos != null)
                    {
                        NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(curBlock, nos);
                    }
                }
            }
            return curBlock;
        }

        private static void CreateStartingValueVariables(GlueElement element, List<StateSave> states, ICodeBlock curBlock, Dictionary<InstructionSave, InterpolationCharacteristic> interpolationCharacteristics)
        {
            foreach (StateSave state in states)
            {
                foreach (InstructionSave instructionSave in state.InstructionSaves)
                {
                    string member = instructionSave.Member;

                    if (!ContainsKey(interpolationCharacteristics, member))
                    {
                        CustomVariable customVariable = element.GetCustomVariable(member);
                        customVariable = ObjectFinder.Self.GetBaseCustomVariable(customVariable);

                        NamedObjectSave nos = null;

                        if (customVariable != null)
                        {
                            nos = element.GetNamedObjectRecursively(customVariable.SourceObject);
                        }

                        if (nos == null || nos.IsDisabled == false)
                        {
                            InterpolationCharacteristic interpolationCharacteristic =
                                                            CustomVariableHelper.GetInterpolationCharacteristic(customVariable, element);

                            interpolationCharacteristics.Add(instructionSave, interpolationCharacteristic);

                            if (interpolationCharacteristic != InterpolationCharacteristic.CantInterpolate)
                            {
                                curBlock.Line("bool set" + instructionSave.Member + " = true;");

                                string defaultStartingValue = "";

                                try
                                {
                                    if (customVariable.GetIsVariableState(element))
                                    {

                                        IElement stateContainingEntity = null;

                                        if (nos != null)
                                        {
                                            stateContainingEntity = ObjectFinder.Self.GetElement(nos.SourceClassType);
                                        }
                                        else if(customVariable.Type?.StartsWith("Entities.") == true)
                                        {
                                            var lastPeriod = customVariable.Type.LastIndexOf('.');
                                            var stripped = customVariable.Type.Substring(0, lastPeriod);
                                            var elementName = stripped.Replace(".", @"\");

                                            stateContainingEntity = GlueState.Self.CurrentGlueProject.GetElement(elementName);

                                        }
                                        else if (string.IsNullOrEmpty(customVariable.SourceObject))
                                        {
                                            stateContainingEntity = element;
                                        }


                                        if (stateContainingEntity != null)
                                        {
                                            string stateType = "VariableState";
                                            if (customVariable != null && !String.Equals(customVariable.Type, "string", StringComparison.OrdinalIgnoreCase))
                                            {
                                                stateType = customVariable.Type;
                                                if(stateType?.Contains('.') == true)
                                                {
                                                    var lastPeriod = stateType.LastIndexOf('.');
                                                    stateType = stateType.Substring(lastPeriod + 1);
                                                }
                                            }

                                            defaultStartingValue = StateCodeGenerator.FullyQualifiedDefaultStateValue(stateContainingEntity, stateType);
                                        }
                                    }
                                    else
                                    {

                                        defaultStartingValue = TypeManager.GetDefaultForType(instructionSave.Type);
                                    }
                                }
                                catch
                                {
                                    // why barf here? We could just mark it as not allowing interpolation
                                    interpolationCharacteristic = InterpolationCharacteristic.CanInterpolate;
                                    //throw new Exception("Could not get a default value for " + instructionSave.Member + " of type " + instructionSave.Type);
                                }

                                if(interpolationCharacteristic != InterpolationCharacteristic.CantInterpolate)
                                {
                                    string type = CustomVariableCodeGenerator.GetMemberTypeFor(customVariable, element);

                                    curBlock.Line(type + " " + member + FirstValue + "= " + defaultStartingValue + ";");
                                    curBlock.Line(type + " " + member + SecondValue + "= " + defaultStartingValue + ";");
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void GenerateDebugCheckForInterpolationValueNaN(ICodeBlock codeBlock)
        {
            codeBlock.Line("#if DEBUG");

            codeBlock.If("float.IsNaN(interpolationValue)")
                .Line("throw new System.Exception(\"interpolationValue cannot be NaN\");")
                .End();

            codeBlock.Line("#endif");
        }

        private static void AddAssignmentForInterpolationForVariable(ICodeBlock curBlock, CustomVariable variable, string prepend, string variableToAssign)
        {
            if (variable.GetIsVariableState() && ! string.IsNullOrEmpty(variable.SourceObject ))
            {
                string line =
                    string.Format("{0}.InterpolateBetween({2}FirstValue, {2}SecondValue, interpolationValue);", variable.SourceObject, variable.SourceObjectProperty, prepend);
                curBlock.Line(line);
            }
            else
            {

                switch (variable.Type)
                {
                    case "int":
                    case "long":
                        curBlock.Line(string.Format("{0} = FlatRedBall.Math.MathFunctions.RoundToInt({1}FirstValue* (1 - interpolationValue) + {1}SecondValue * interpolationValue);", variableToAssign, prepend));

                        break;
                    case "byte":
                        curBlock.Line(string.Format("{0} = (byte)FlatRedBall.Math.MathFunctions.RoundToInt({1}FirstValue* (1 - interpolationValue) + {1}SecondValue * interpolationValue);", variableToAssign, prepend));
                        break;
                    case "float":
                    case "double":
                        curBlock.Line(string.Format("{0} = {1}FirstValue * (1 - interpolationValue) + {1}SecondValue * interpolationValue;", variableToAssign, prepend));
                        break;
                    case "decimal":
                        curBlock.Line(string.Format("{0} =  {1}FirstValue * (1 - (decimal)interpolationValue) + {1}SecondValue * (decimal)interpolationValue;", variableToAssign, prepend));
                        break;
                }
            }
        }


        static InterpolationCharacteristic GetValue(Dictionary<InstructionSave, InterpolationCharacteristic> dictionary, string value)
        {
            foreach (var kvp in dictionary)
            {
                if (kvp.Key.Member == value)
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

        private static ICodeBlock SetInterpolateBetweenValuesForStates(GlueElement element, string enumType, List<StateSave> states, 
            ICodeBlock curBlock, Dictionary<InstructionSave, InterpolationCharacteristic> mInterpolationCharacteristics, string firstOrSecondValue, string localStateName)
        {
            bool isElse = false;
            foreach (StateSave state in states)
            {
                if(isElse)
                {
                    curBlock = curBlock.ElseIf($"{localStateName} == {enumType}.{state.Name}");

                }
                else
                {
                    curBlock = curBlock.If($"{localStateName} == {enumType}.{state.Name}");
                }
                isElse = true;
                foreach (InstructionSave instructionSave in state.InstructionSaves)
                {

                    
                    var customVariable = element.GetCustomVariable(instructionSave.Member);
                    customVariable = ObjectFinder.Self.GetBaseCustomVariable(customVariable);
                    NamedObjectSave sourceNamedObjectSave = null;
                    if (customVariable != null)
                    {
                        sourceNamedObjectSave = element.GetNamedObjectRecursively(customVariable.SourceObject);
                    }

                    if(sourceNamedObjectSave != null)
                    {
                        NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(curBlock, sourceNamedObjectSave);
                    }

                    if (GetValue(mInterpolationCharacteristics, instructionSave.Member) != InterpolationCharacteristic.CantInterpolate)
                    {
                        if (instructionSave.Value == null)
                        {
                            curBlock.Line("set" + instructionSave.Member + " = false;");
                        }
                        else
                        {
                            string valueToWrite = GetRightSideAssignmentValueAsString(element, instructionSave);

                            curBlock.Line(instructionSave.Member + firstOrSecondValue + " = " + valueToWrite + ";");
                        }
                    }
                    else // This value can't be interpolated, but if the user has set a value of 0 or 1, then it should be set
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
                        string valueToWrite = GetRightSideAssignmentValueAsString(element, instructionSave);
                        ifBlock.Line("this." + instructionSave.Member + " = " + valueToWrite + ";");
                    }
                    if (sourceNamedObjectSave != null)
                    {
                        NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(curBlock, sourceNamedObjectSave);
                    }
                }
                curBlock = curBlock.End();
            }
            return curBlock;
        }

        private static ICodeBlock GenerateInterpolateToStateMethod(IElement element, ICodeBlock codeBlock, string enumType, List<StateSave> states)
        {
            if (states.Count != 0)
            {
                string variableNameModifier = enumType;
                if (enumType == "VariableState")
                {
                    variableNameModifier = "";
                }


                var curBlock = codeBlock;
                var otherBlock = codeBlock;
                var elementNameWithoutPath = FileManager.RemovePath(element.Name);

                curBlock = curBlock
                    .Function("public FlatRedBall.Instructions.Instruction", "InterpolateToState", enumType + " stateToInterpolateTo, double secondsToTake");
                        //.Switch("stateToInterpolateTo");

                otherBlock = otherBlock
                    .Function("public void", "StopStateInterpolation", enumType + " stateToStop");
                //.Switch("stateToStop");

                bool isElse = false;
                foreach (StateSave stateSave in states)
                {
                    GenerateInterpolateForIndividualState(element, curBlock, otherBlock, stateSave, enumType, isElse);
                    isElse = true;
                }
               
                string instructionListAdd = "this.Instructions.Add";

                if (element is ScreenSave)
                {
                    instructionListAdd = "FlatRedBall.Instructions.InstructionManager.Add";
                }

                //curBlock.Line("var instruction = new MethodInstruction<" + elementNameWithoutPath +
                //              ">(this, \"StopStateInterpolation\", new object[]{stateToInterpolateTo}, TimeManager.CurrentTime + secondsToTake);");


                curBlock.Line("var instruction = new FlatRedBall.Instructions.DelegateInstruction<" + enumType + ">(StopStateInterpolation, stateToInterpolateTo);");
                curBlock.Line("instruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + secondsToTake;");





                curBlock.Line(instructionListAdd + "(instruction);");
                curBlock.Line("return instruction;");

                otherBlock.Line("Current" + variableNameModifier + "State = stateToStop;");
            }

            return codeBlock;
        }

    }
}
