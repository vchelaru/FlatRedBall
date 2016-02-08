using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.CodeGeneration
{
    public partial class StateCodeGenerator : Singleton<StateCodeGenerator>
    {
        #region Fields

        List<string> mVariableNamesToSkipForStates = new List<string>();

        Dictionary<string, string> mVariableNamesToReplaceForStates = new Dictionary<string, string>();
        
        #endregion

        #region Methods

        #region Constructor/Init

        public StateCodeGenerator()
        {
            AddVariablesToSkipForStates();
        }

        private void AddVariablesToSkipForStates()
        {
            mVariableNamesToSkipForStates.Add("CustomFontFile");
            mVariableNamesToSkipForStates.Add("UseCustomFont");
            mVariableNamesToSkipForStates.Add("Guide");
            mVariableNamesToSkipForStates.Add("Parent");

            mVariableNamesToSkipForStates.Add("Height Units");
            mVariableNamesToSkipForStates.Add("Width Units");
            mVariableNamesToSkipForStates.Add("Custom Texture Coordinates");
            mVariableNamesToSkipForStates.Add("Children Layout");

            mVariableNamesToSkipForStates.Add("Font");
            mVariableNamesToSkipForStates.Add("FontSize");
            mVariableNamesToSkipForStates.Add("OutlineThickness");
            //mVariableNamesToSkipForStates.Add("SourceFile");

            // Eventually we'll support this but first Gum needs to support 
            // setting categorized states on instances
            // September 17 2014
            // no longer needed:
            //mVariableNamesToSkipForStates.Add("State");


            mVariableNamesToReplaceForStates.Add("Texture Address", "TextureAddress");
            mVariableNamesToReplaceForStates.Add("Texture Height Scale", "TextureHeightScale");
            mVariableNamesToReplaceForStates.Add("Texture Width Scale", "TextureWidthScale");
            mVariableNamesToReplaceForStates.Add("Texture Height", "TextureHeight");
            mVariableNamesToReplaceForStates.Add("Texture Width", "TextureWidth");
            mVariableNamesToReplaceForStates.Add("Texture Left", "TextureLeft");
            mVariableNamesToReplaceForStates.Add("Texture Top", "TextureTop");
            mVariableNamesToReplaceForStates.Add("Font Scale", "FontScale");
            mVariableNamesToReplaceForStates.Add("Clips Children", "ClipsChildren");

            mVariableNamesToReplaceForStates.Add("X Origin", "XOrigin");
            mVariableNamesToReplaceForStates.Add("X Units", "XUnits");
            mVariableNamesToReplaceForStates.Add("Y Origin", "YOrigin");
            mVariableNamesToReplaceForStates.Add("Y Units", "YUnits");
            mVariableNamesToReplaceForStates.Add("Wraps Children", "WrapsChildren");
            mVariableNamesToReplaceForStates.Add("Source File", "SourceFile");


            
        }

        #endregion

        public void GenerateEverythingFor(ElementSave elementSave, ICodeBlock currentBlock)
        {
            GenerateStateEnums(elementSave, currentBlock);

            GenerateCurrentStateFields(elementSave, currentBlock);
            
            GenerateCurrentStateProperties(elementSave, currentBlock);

            GenerateStateInterpolateBetween(elementSave, currentBlock);

            GenerateStateInterpolateTo(elementSave, currentBlock);

            GenerateAnimationEnumerables(elementSave, currentBlock);

            GenerateStopAnimations(elementSave, currentBlock);

            GenerateGetCurrentValuesOnState(elementSave, currentBlock);
        }

        private void GenerateStateEnums(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("#region State Enums");
            string categoryName = "VariableState";
            var states = elementSave.States;
            GenerateEnumsForCategory(currentBlock, categoryName, states);

            // loop through categories:
            foreach (var category in elementSave.Categories)
            {
                categoryName = category.Name;
                states = category.States;
                GenerateEnumsForCategory(currentBlock, categoryName, states);

            }

            currentBlock.Line("#endregion");
        }

        private void GenerateEnumsForCategory(ICodeBlock codeBlock, string categoryName, List<Gum.DataTypes.Variables.StateSave> states)
        {
            var enumBlock = codeBlock.Enum("public", categoryName);

            foreach (var item in states)
            {
                if (item == states.Last())
                {
                    enumBlock.Line(item.MemberNameInCode());
                }
                else
                {
                    enumBlock.Line(item.MemberNameInCode() + ",");
                }
            }
        }


        private bool GetIfShouldGenerateStateVariable(Gum.DataTypes.Variables.VariableSave variable, ElementSave container)
        {
            bool toReturn = true;

            string variableName = variable.GetRootName();



            if (variable.Value == null || !variable.SetsValue)
            {
                toReturn = false;
            }
            // states can't set states on this
            if(variable.IsState(container) && string.IsNullOrEmpty(variable.SourceObject ) )
            {
                toReturn = false;
            }

            if (toReturn && mVariableNamesToSkipForStates.Contains(variableName))
            {
                toReturn = false;
            }

            bool hasSourceObject = !string.IsNullOrEmpty(variable.SourceObject);

            if (toReturn && hasSourceObject)
            {
                InstanceSave instanceSave = container.GetInstance(variable.SourceObject);

                if(instanceSave == null)
                {
                    toReturn = false;
                }
                else 
                { 
                    var baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instanceSave.BaseType);

                    if (baseElement == null)
                    {
                        toReturn = false;
                    }

                    if (toReturn)
                    {

                        // Gum (just like Glue) keeps variables that aren't needed around.  This allows users to rename things and not lose
                        // important information accidentally.  But because of that we have to make sure that the variable we're working with is
                        // valid for the type of object we're dealing with.  
                        var defaultState = baseElement.DefaultState;
                        
                        RecursiveVariableFinder rvf = new RecursiveVariableFinder(defaultState);

                        var foundVariable = rvf.GetVariable(variable.GetRootName());

                        if (foundVariable == null)
                        {
                            // This doesn't exist anywhere in the inheritance chain, so we don't want to generate it:
                            toReturn = false;
                        }
                    }
                }
            }

            if(toReturn && !hasSourceObject)
            {
                // If a variable is part of a component, it better be defined in the base type or else we won't generate it.
                // For example, consider a component that used to inherit from Text. It will have variables for fonts. If that
                // component switches to inheriting from Sprite, those variables will still exist in the XML for that component,
                // but we shouldn't generate any state variables for those variables. So we'll go to the base type and see if those
                // variables exist
                bool isComponent = container is ComponentSave;

                var rootComponent = Gum.Managers.ObjectFinder.Self.GetRootStandardElementSave(container);

                // If the Container is a Screen, then rootComponent will be null, so we don't need to do anything
                if (rootComponent == null)
                {
                    toReturn = false;
                }
                else
                {
                    IEnumerable<VariableSave> variablesToCheck;

                    if (isComponent)
                    {
                        var component = Gum.Managers.ObjectFinder.Self.GetStandardElement("Component");

                        variablesToCheck = rootComponent.DefaultState.Variables.Concat(component.DefaultState.Variables);
                    }
                    else
                    {
                        var defaultState = rootComponent.DefaultState;

                        variablesToCheck = defaultState.Variables;
                    }


                    bool wasMatchFound = variablesToCheck.Any(item => item.Name == variable.GetRootName());
                    toReturn = wasMatchFound;
                }
            }



            return toReturn;
        }

        private void GenerateCurrentStateFields(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("#region State Fields");
            string propertyName = "CurrentVariableState";
            string propertyType = "VariableState";
            currentBlock.Line(propertyType + " m" + propertyName + ";");


            foreach (var category in elementSave.Categories)
            {
                propertyName = "Current" + category.Name + "State";
                propertyType = category.Name;

                currentBlock.Line(propertyType + " m" + propertyName + ";");
            }
            currentBlock.Line("#endregion");
        }

        private void GenerateCurrentStateProperties(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("#region State Properties");

            string propertyName = "CurrentVariableState";
            string propertyType = "VariableState";
            var states = elementSave.States;



            GeneratePropertyForCurrentState(currentBlock, propertyType, propertyName, states, elementSave);

            foreach (var category in elementSave.Categories)
            {


                propertyName = "Current" + category.Name + "State";
                propertyType = category.Name;
                states = category.States;



                GeneratePropertyForCurrentState(currentBlock, propertyType, propertyName, states, elementSave);
            }

            currentBlock.Line("#endregion");

        }

        private void GeneratePropertyForCurrentState(ICodeBlock currentBlock, string propertyType, string propertyName, List<Gum.DataTypes.Variables.StateSave> states, ElementSave container)
        {


            var property = currentBlock.Property("public " + propertyType, propertyName);

            property.Get().Line("return m" + propertyName + ";");

            var setter = property.Set();
            {
                setter.Line("m" + propertyName + " = value;");

                var switchBlock = setter.Switch("m" + propertyName);

                foreach (var state in states)
                {
                    var caseBlock = switchBlock.Case(propertyType + "." + state.MemberNameInCode());
                    {
                        foreach (var variable in state.Variables)
                        {
                            // where block doesn't debug well for some reason, so I unrolled it...
                            if (GetIfShouldGenerateStateVariable(variable, container))
                            {
                                string variableValue = variable.Value.ToString();
                                bool isEntireAssignment;

                                GueDerivingClassCodeGenerator.Self.AdjustVariableValueIfNecessary(variable, container, ref variableValue, out isEntireAssignment);
                                if (isEntireAssignment)
                                {
                                    caseBlock.Line(variableValue);
                                }
                                else
                                {
                                    string memberNameInCode = variable.MemberNameInCode(container, mVariableNamesToReplaceForStates);
                                    caseBlock.Line(memberNameInCode + " = " + variableValue + ";");
                                }
                            }
                        }
                    }
                }
            }
        }

        private void GenerateGetCurrentValuesOnState(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("#region Get Current Values on State");

            string categoryName = "VariableState";
            var states = elementSave.States;
            GenerateGetCurrentValuesOnStateForCategory(currentBlock, elementSave, categoryName, states, addValues:false);
            GenerateGetCurrentValuesOnStateForCategory(currentBlock, elementSave, categoryName, states, addValues:true);


            foreach (var category in elementSave.Categories)
            {
                categoryName = category.Name;
                states = category.States;
                GenerateGetCurrentValuesOnStateForCategory(currentBlock, elementSave, categoryName, states, addValues:false);
                GenerateGetCurrentValuesOnStateForCategory(currentBlock, elementSave, categoryName, states, addValues:true);
            }

            currentBlock.Line("#endregion");
        }

        private void GenerateGetCurrentValuesOnStateForCategory(ICodeBlock currentBlock, ElementSave container, string categoryName, List<StateSave> states, bool addValues = false)
        {
            string methodName = "GetCurrentValuesOnState";

            if(addValues)
            {
                methodName = "AddToCurrentValuesWithState";
            }

            currentBlock = currentBlock.Function("private Gum.DataTypes.Variables.StateSave", methodName, categoryName + " state");

            currentBlock.Line("Gum.DataTypes.Variables.StateSave newState = new Gum.DataTypes.Variables.StateSave();");

            var switchBlock = currentBlock.Switch("state");
            {
                foreach (var state in states)
                {
                    var caseBlock = switchBlock.Case(categoryName + "." + state.MemberNameInCode());
                    {
                        foreach (var variable in state.Variables.Where(item => GetIfShouldGenerateStateVariable(item, container)))
                        {
                            string memberNameInCode = variable.MemberNameInCode(container, mVariableNamesToReplaceForStates);

                            caseBlock.Line("newState.Variables.Add(new Gum.DataTypes.Variables.VariableSave()");
                            var instantiatorBlock = caseBlock.Block();
                            {
                                instantiatorBlock.Line("SetsValue = true,");

                                instantiatorBlock.Line("Name = \"" + memberNameInCode + "\",");

                                string valueString = "Value = " + memberNameInCode + "";
                                if(addValues && IsVariableNumeric(variable))
                                {
                                    
                                    string variableValue = variable.Value.ToString();
                                    bool isEntireAssignment;
                                    GueDerivingClassCodeGenerator.Self.AdjustVariableValueIfNecessary(variable, container, ref variableValue, out isEntireAssignment);

                                    if (isEntireAssignment)
                                    {
                                        valueString = variableValue;
                                    }
                                    else
                                    {
                                        valueString += " + " + variableValue;
                                    }
                                }
                                instantiatorBlock.Line(valueString);

                            }
                            caseBlock.Line(");");
                        }
                    }
                }
            }

            currentBlock.Line("return newState;");
        }

        private bool IsVariableNumeric(VariableSave variable)
        {
            string type = variable.Type;

            return type == "float" ||
                type == "int" ||
                type == "double" ||
                type == "byte" ||
                type == "decimal" ||
                type == "long";
        }

        #endregion



    }
}
