using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.CodeGeneration
{
    public class GueDerivingClassCodeGenerator : Singleton<GueDerivingClassCodeGenerator>
    {
        #region Fields

        Dictionary<string, string> mTypeToQualifiedTypes = new Dictionary<string, string>();

        public Dictionary<string, string> TypeToQualifiedTypes
        {
            get
            {
                return mTypeToQualifiedTypes;
            }
        }

        #endregion

        public static string GueRuntimeNamespace
        {
            get
            {
                return FlatRedBall.Glue.ProjectManager.ProjectNamespace + ".GumRuntimes";
            }
        }


        public GueDerivingClassCodeGenerator()
        {
            mTypeToQualifiedTypes.Add("HorizontalAlignment", "RenderingLibrary.Graphics.HorizontalAlignment");
            mTypeToQualifiedTypes.Add("VerticalAlignment", "RenderingLibrary.Graphics.VerticalAlignment");
            mTypeToQualifiedTypes.Add("Blend", "Gum.RenderingLibrary.Blend");
            mTypeToQualifiedTypes.Add("PositionUnitType", "Gum.Managers.PositionUnitType");
            mTypeToQualifiedTypes.Add("TextureAddress", "Gum.Managers.TextureAddress");
            mTypeToQualifiedTypes.Add("DimensionUnitType", "Gum.DataTypes.DimensionUnitType");

            AddGetterReplacements();

            AddSetterReplacements();
        }
        
        private void AddGetterReplacements()
        {

        }


        private void AddSetterReplacements()
        {

        }
        
        public string GenerateCodeFor(ElementSave elementSave)
        {
            CodeBlockBase topBlock =
                new CodeBlockBase(null);
            // It's much easier to add the LINQ usings here instead of using the qualified method names in code gen below
            topBlock.Line("using System.Linq;");

            ICodeBlock currentBlock = topBlock.Namespace(GueRuntimeNamespace);

            bool generated = false;

            if (elementSave is StandardElementSave)
            {
                generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(elementSave as StandardElementSave, currentBlock);
            }
            else
            {
                GenerateScreenAndComponentCodeFor(elementSave as ElementSave, currentBlock);
                // for now always true
                generated = true;
            }

            if (generated)
            {
                return topBlock.ToString();
            }
            else
            {
                return null;
            }
        }

        public static string GetQualifiedRuntimeTypeFor(InstanceSave instance)
        {
            var element = ObjectFinder.Self.GetElementSave(instance);

            return GetQualifiedRuntimeTypeFor(element);
        }

        public static string GetQualifiedRuntimeTypeFor(ElementSave elementSave)
        {
            return GueRuntimeNamespace + "." + GetUnqualifiedRuntimeTypeFor(elementSave);
        }

        public static string GetUnqualifiedRuntimeTypeFor(ElementSave elementSave)
        {
            return FlatRedBall.IO.FileManager.RemovePath( elementSave.Name) + "Runtime";
        }



        private void GenerateScreenAndComponentCodeFor(ElementSave elementSave, ICodeBlock codeBlock)
        {
            string runtimeClassName = GetUnqualifiedRuntimeTypeFor(elementSave);
            
            ICodeBlock currentBlock = GenerateClassHeader(codeBlock, elementSave);

            StateCodeGenerator.Self.GenerateEverythingFor(elementSave, currentBlock);

            GenerateProperties(elementSave, currentBlock);


            GenerateEvents(elementSave, currentBlock);

            GenerateConstructor(elementSave, currentBlock, runtimeClassName);

            GenerateAssignReferencesMethod(elementSave, currentBlock);

            GenerateAddToManagersMethod(elementSave, currentBlock);

            GenerateCallCustomInitialize(elementSave, currentBlock);

            GeneratePartialMethods(elementSave, currentBlock);

            GenerateRaiseExposedEvents(elementSave, currentBlock);
        }

        private void GenerateCallCustomInitialize(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock = currentBlock.Function("protected override void", "CallCustomInitialize", "");
            {
                currentBlock.Line("base.CallCustomInitialize();");
                currentBlock.Line("CustomInitialize();");

            }
        }

        private ICodeBlock GenerateClassHeader(ICodeBlock codeBlock, ElementSave elementSave)
        {
            string runtimeClassName = GetUnqualifiedRuntimeTypeFor(elementSave); 

            string inheritance = "Gum.Wireframe.GraphicalUiElement";

            if(elementSave.BaseType != "Component" && !string.IsNullOrEmpty(elementSave.BaseType))
            {
                inheritance = GueRuntimeNamespace + "." + elementSave.BaseType + "Runtime";
            }

            // If it's not public then exposing an instance in a public class makes the project not compile
            //ICodeBlock currentBlock = codeBlock.Class("partial", runtimeClassName, " : " + inheritance);
            ICodeBlock currentBlock = codeBlock.Class("public partial", runtimeClassName, " : " + inheritance);



            return currentBlock;
        }

        private void GeneratePartialMethods(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("partial void CustomInitialize();");
        }

        private void GenerateEvents(ElementSave elementSave, ICodeBlock currentBlock)
        {
            foreach (var eventSave in elementSave.Events.Where(item => !IsHandledByIWindow(item) &&
                (string.IsNullOrEmpty(item.GetSourceObject()) || !string.IsNullOrEmpty(item.ExposedAsName))))
            {

                string name = eventSave.GetRootName();

                if (!string.IsNullOrEmpty(eventSave.ExposedAsName))
                {
                    name = eventSave.ExposedAsName;
                }

                currentBlock.Line("public event FlatRedBall.Gui.WindowEvent " + name + ";");
            }
        }

        private void GenerateProperties(ElementSave elementSave, ICodeBlock currentBlock)
        {
            GenerateInstanceProperties(elementSave, currentBlock);

            GenerateVariableProperties(elementSave, currentBlock);
        }

        private void GenerateVariableProperties(ElementSave elementSave, ICodeBlock currentBlock)
        {
            // Get all exposed variables and make properties otu of them.
            if (elementSave.DefaultState != null)
            {
                var allVariablesToProcess =
                    elementSave.DefaultState.Variables.Where(item => !string.IsNullOrEmpty(item.ExposedAsName));

                foreach (var variable in allVariablesToProcess)
                {

                    string variableType = variable.Type;



                    ModifyVariableTypeForProperty(ref variableType, variable, elementSave);

                    string propertyName = variable.ExposedAsName.Replace(" ", "_");

                    ICodeBlock property = currentBlock.Property("public " + variableType, propertyName);

                    string whatToGetOrSet = variable.Name;

                    if(whatToGetOrSet.Contains("IconTypeState"))
                    {
                        int m = 3;
                    }
                    if(variableType.Contains("DimensionUnit"))
                    {
                        int m = 3;
                    }
                    // If this is an exposed property on a standard element, then we just need to kill all spaces and replace
                    // them with nothing
                    var instance = elementSave.GetInstance(variable.SourceObject);

                    if (instance != null)
                    {
                        var baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance.BaseType);

                        if (baseElement != null && baseElement is StandardElementSave)
                        {
                            whatToGetOrSet = whatToGetOrSet.Replace(" ", "");
                        }

                        var rootName = variable.GetRootName();
                        if(rootName.EndsWith("State"))
                        {
                            var withoutState = rootName.Substring(0, rootName.Length - "State".Length);
                            if(rootName == "State")
                            {
                                whatToGetOrSet = variable.SourceObject + "." + "CurrentVariableState";
                            }
                            else if(baseElement != null && baseElement.Categories.Any(item=>item.Name == withoutState))
                            {
                                whatToGetOrSet = variable.SourceObject + ".Current" + withoutState + "State";
                            }

                        }
                    }


                    property.Get()
                        .Line("return " + whatToGetOrSet + ";");
                    property.Set()
                        .Line(whatToGetOrSet + " = value;");
                }

            }
        }

        private void ModifyVariableTypeForProperty(ref string variableType, VariableSave variableSave, ElementSave elementSave)
        {

            if (mTypeToQualifiedTypes.ContainsKey(variableType))
            {
                variableType = mTypeToQualifiedTypes[variableType];
            }

            if(string.IsNullOrEmpty(variableSave.SourceObject))
            {
                if(variableType == "State")
                {
                    // Not sure why this was returning CurrentVariableState, as that is the property name,
                    // not the property type, and here we want the property type.  
                    //variableType = elementSave.Name + "Runtime.CurrentVariableState";
                    variableType = elementSave.Name + "Runtime.VariableState";
                }
                else if( variableSave.Type.EndsWith("State"))
                {
                    var typeWithoutState = variableSave .Type.Substring(0, variableSave.Type.Length - "State".Length);
                    var foundCategory = elementSave.Categories.FirstOrDefault(item => item.Name == typeWithoutState);
                    if (foundCategory != null)
                    {

                        variableType = elementSave.Name + "Runtime." + foundCategory.Name ;
                    }
                }
            }
            else if(variableSave.IsFile)
            {
                variableType = "Microsoft.Xna.Framework.Graphics.Texture2D";
            }
            else
            {
                var instance = elementSave.Instances.FirstOrDefault(item => item.Name == variableSave.SourceObject);

                if(instance != null)
                {

                    var element = ObjectFinder.Self.GetElementSave(instance);

                    if(element != null)
                    {

                        var rootName = variableSave.GetRootName();
                        var variableInInstanceElement = element.DefaultState.Variables.FirstOrDefault(item => item.Name == rootName || item.ExposedAsName == rootName);

                        if(variableInInstanceElement != null)
                        {
                            ModifyVariableTypeForProperty(ref variableType, variableInInstanceElement, element);
                        }
                    }
                    
                }
            }
        }

        private void GenerateInstanceProperties(ElementSave elementSave, ICodeBlock currentBlock)
        {
            foreach (var instance in elementSave.Instances)
            {
                string type = GetQualifiedRuntimeTypeFor(instance);

                if (GetIfInstanceReferencesValidComponent(instance))
                {
                    ICodeBlock property = currentBlock.AutoProperty("private " + type, instance.MemberNameInCode());
                }
                else
                {
                    currentBlock.Line("// could not generate member for " + instance.Name + " because it doesn't reference a valid component");
                }
            }
        }

        public bool GetIfInstanceReferencesValidComponent(InstanceSave instance)
        {
            return Gum.Managers.ObjectFinder.Self.GetElementSave(instance.BaseType) != null;
        }

        /// <summary>
        /// Adjusts a variable value to be code which can execute properly. For example, converts a file string to a load call.
        /// </summary>
        /// <param name="variableSave">The variable</param>
        /// <param name="container">The container of the variable</param>
        /// <param name="variableValue">The variable value to modify</param>
        /// <param name="isEntireAssignment">Whether the modified value is an entire assignment - meaning no assignment is necessary. This is used for file loading.</param>
        public void AdjustVariableValueIfNecessary(Gum.DataTypes.Variables.VariableSave variableSave, ElementSave container, ref string variableValue, out bool isEntireAssignment)
        {
            isEntireAssignment = false;

            ElementSave categoryContainer;
            StateSaveCategory stateSaveCategory;

            string variableType = variableSave.Type;


            if(variableSave.IsState(container, out categoryContainer, out stateSaveCategory))
            {
                string categoryName = "Category";

                if(stateSaveCategory != null)
                {
                    categoryName = stateSaveCategory.Name;
                }
                else
                {
                    categoryName = "VariableState";

                }


                variableValue = GueRuntimeNamespace + "." + 
                    FlatRedBall.IO.FileManager.RemovePath(categoryContainer.Name) + "Runtime" + 
                    "." + categoryName + "." + variableSave.Value;
            }

            else if (variableSave.IsEnumeration())
            {
                AdjustEnumerationVariableValue(variableSave, container, ref variableValue, ref variableType);
            }
            else if(variableSave.IsFile)
            {
                isEntireAssignment = true;

                string fileName = "\"" + variableValue.Replace("\\", "\\\\") + "\"";

                variableValue = "SetProperty(\"" + variableSave.Name + "\", " + fileName + ");";

                ////RenderingLibrary.Content.LoaderManager.Self.Load("fileName", managers);
                //variableValue = "RenderingLibrary.Content.LoaderManager.Self.Load(\"" + variableValue.Replace("\\", "\\\\") + "\", RenderingLibrary.SystemManagers.Default)";
            }

            else if (variableSave.Type == "string")
            {
                variableValue = variableValue.Replace("\\", "\\\\");
                variableValue = variableValue.Replace("\"", "\\\"");

                variableValue = "\"" + variableValue + "\"";
             
            }
            else if (variableSave.Type == "float" && variableValue.Contains('.'))
            {
                variableValue = variableValue + "f";
            }
            else if (variableSave.Type == "bool")
            {
                variableValue = variableValue.ToLower();
            }


        }

        private void AdjustEnumerationVariableValue(Gum.DataTypes.Variables.VariableSave variableSave, ElementSave element, ref string variableValue, ref string variableType)
        {
            if (variableSave.Type == "Gum.Managers.PositionUnitType" || variableSave.Type == "PositionUnitType")
            {
                string rootName = variableSave.GetRootName();

                // convert from PositionUnitType to GeneralUnitType

                GeneralUnitType convertedValue =
                    UnitConverter.ConvertToGeneralUnit((PositionUnitType)variableSave.Value);

                variableValue = convertedValue.ToString();

                variableType = "Gum.Converters.GeneralUnitType";
            }

            string prefix = variableType;



            if (mTypeToQualifiedTypes.ContainsKey(prefix))
            {
                prefix = mTypeToQualifiedTypes[prefix];
            }
            else
            {
                ModifyVariableTypeForProperty(ref prefix, variableSave, element);
            }

            variableValue = prefix + "." + variableValue;
        }

        private bool IsHandledByIWindow(EventSave item)
        {
            // Specifically check "Name" because we want to create events for events on instances
            if (item.Name == "Click" ||
                item.Name == "RollOn" ||
                item.Name == "RollOff" ||
                item.Name == "RollOver"
                )
            {
                return true;
            }

            return false;
        }

        private void GenerateAssignReferencesMethod(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock = currentBlock.Function("public override void", "AssignReferences", "");
            {
                currentBlock.Line("base.AssignReferences();");

                foreach(var instance in elementSave.Instances)
                {
                    var foundBase = Gum.Managers.ObjectFinder.Self.GetElementSave(instance.BaseType);

                    if (foundBase != null)
                    {
                        currentBlock.Line(instance.MemberNameInCode() +
                            " = this.GetGraphicalUiElementByName(\"" + instance.MemberNameInCode() + "\") as " + 
                            GueDerivingClassCodeGenerator.GetQualifiedRuntimeTypeFor(instance) + ";");

                        ElementSave baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance.BaseType);

                        if (baseElement != null && (baseElement is StandardElementSave) == false)
                        {
                            currentBlock.Line(instance.MemberNameInCode() + ".AssignReferences();");
                        }

                        foreach (var eventSave in elementSave.Events.Where(item =>
                            item.GetSourceObject() == instance.Name && !string.IsNullOrEmpty(item.ExposedAsName)))
                        {
                            currentBlock.Line(eventSave.Name + " += Raise" + eventSave.ExposedAsName + ";");
                        }
                    }
                }

            }
        }

        private void GenerateAddToManagersMethod(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock = currentBlock.Function("public override void", "AddToManagers", "RenderingLibrary.SystemManagers managers, RenderingLibrary.Graphics.Layer layer");
            {
                currentBlock.Line("base.AddToManagers(managers, layer);");
            }
        }


        private void GenerateRaiseExposedEvents(ElementSave elementSave, ICodeBlock currentBlock)
        {
            foreach (var eventSave in elementSave.Events.Where(item => !string.IsNullOrEmpty(item.ExposedAsName)))
            {

                var function = currentBlock.Function("void", "Raise" + eventSave.ExposedAsName, "FlatRedBall.Gui.IWindow callingWindow");
                {
                    var ifBlock = function.If(eventSave.ExposedAsName + " != null");
                    {
                        ifBlock.Line(eventSave.ExposedAsName + "(this);");

                    }
                }

            }
        }

        public void GenerateConstructor(ElementSave elementSave, ICodeBlock currentBlock, string runtimeClassName)
        {
            string baseCall = null;

            bool hasBase = !string.IsNullOrEmpty(elementSave.BaseType);

            if(hasBase)
            {
                baseCall = "base(fullInstantiation, false)";
            }
            var constructor = currentBlock.Constructor("public", runtimeClassName, "bool fullInstantiation = true, bool callAssignReferences = true",  baseCall );

            var ifStatement = constructor.If("fullInstantiation");

            string componentScreenOrStandard = null;

            if(elementSave is ComponentSave)
            {
                componentScreenOrStandard = "Components";
            }
            else if(elementSave is ScreenSave)
            {
                componentScreenOrStandard = "Screens";  
            }
            else if(elementSave is StandardElementSave)
            {
                componentScreenOrStandard = "StandardElements";
            }

            ifStatement.Line("Gum.DataTypes.ElementSave elementSave = Gum.Managers.ObjectFinder.Self.GumProjectSave." + 
                componentScreenOrStandard + ".First(item => item.Name == \"" + elementSave.Name.Replace("\\", "\\\\") + "\");");
            ifStatement.Line("this.ElementSave = elementSave;");
            ifStatement.Line("string oldDirectory = FlatRedBall.IO.FileManager.RelativeDirectory;");

            ifStatement.Line("FlatRedBall.IO.FileManager.RelativeDirectory = FlatRedBall.IO.FileManager.GetDirectory(Gum.Managers.ObjectFinder.Self.GumProjectSave.FullFileName);");



            ifStatement.Line("GumRuntime.ElementSaveExtensions.SetGraphicalUiElement(elementSave, this, RenderingLibrary.SystemManagers.Default);");

            ifStatement.Line("FlatRedBall.IO.FileManager.RelativeDirectory = oldDirectory;");

            var innerIf = ifStatement.If("callAssignReferences");

            innerIf.Line("this.AssignReferences();");
        }

        public bool ShouldGenerateRuntimeFor(ElementSave elementSave)
        {
            if (elementSave is StandardElementSave)
            {
                if (elementSave.Name == "Component"
                
                    )
                {
                    return false;
                }
            }

            return true;
        }
    }
}
