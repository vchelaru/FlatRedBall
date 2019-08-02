using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumPlugin.DataGeneration;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes.Behaviors;
using FlatRedBall.Glue.SaveClasses;

namespace GumPlugin.CodeGeneration
{
    public class GueDerivingClassCodeGenerator : Singleton<GueDerivingClassCodeGenerator>
    {
        #region Fields / Properties

        Dictionary<string, string> mTypeToQualifiedTypes = new Dictionary<string, string>();

        public Dictionary<string, string> TypeToQualifiedTypes
        {
            get
            {
                return mTypeToQualifiedTypes;
            }
        }

        public static string GueRuntimeNamespace
        {
            get
            {
                return FlatRedBall.Glue.ProjectManager.ProjectNamespace + ".GumRuntimes";
            }
        }

        #endregion


        public GueDerivingClassCodeGenerator()
        {
            mTypeToQualifiedTypes.Add("HorizontalAlignment", "RenderingLibrary.Graphics.HorizontalAlignment");
            mTypeToQualifiedTypes.Add("VerticalAlignment", "RenderingLibrary.Graphics.VerticalAlignment");
            mTypeToQualifiedTypes.Add("Blend", "Gum.RenderingLibrary.Blend");
            mTypeToQualifiedTypes.Add("PositionUnitType", "Gum.Managers.PositionUnitType");
            mTypeToQualifiedTypes.Add("TextureAddress", "Gum.Managers.TextureAddress");
            mTypeToQualifiedTypes.Add("DimensionUnitType", "Gum.DataTypes.DimensionUnitType");
            mTypeToQualifiedTypes.Add("ChildrenLayout", "Gum.Managers.ChildrenLayout");

            AddGetterReplacements();

            AddSetterReplacements();
        }
        
        public string GenerateCodeFor(ElementSave elementSave)
        {
            if(elementSave == null)
            {
                throw new ArgumentNullException(nameof(elementSave));
            }

            CodeBlockBase topBlock =
                new CodeBlockBase(null);
            // It's much easier to add the LINQ usings here instead of using the qualified method names in code gen below
            topBlock.Line("using System.Linq;");

            var fullNamespace = GetFullRuntimeNamespaceFor(elementSave);

            ICodeBlock currentBlock = topBlock.Namespace(fullNamespace);

            bool generated = false;

            if (elementSave is StandardElementSave)
            {
                generated = StandardsCodeGenerator.Self.GenerateStandardElementSaveCodeFor(elementSave as StandardElementSave, currentBlock);
            }
            else
            {
                bool doesElementSaveHaveDefaultState = elementSave.DefaultState != null;
                // If the element has no default state that may mean that the element is referenced 
                // by the Gum project, but that the file (such as the .gusx or gucx) is missing from
                // the file system. Therefore we should not try to generate code for it.
                if(doesElementSaveHaveDefaultState)
                {
                    GenerateScreenAndComponentCodeFor(elementSave, currentBlock);
                    // for now always true
                    generated = true;
                }
                else
                {
                    generated = false;
                }
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

        private void GenerateScreenAndComponentCodeFor(ElementSave elementSave, ICodeBlock codeBlock)
        {
            if(elementSave == null)
            {
                throw new ArgumentNullException(nameof(elementSave));
            }

            ICodeBlock currentBlock = GenerateClassHeader(codeBlock, elementSave);

            StateCodeGenerator.Self.GenerateEverythingFor(elementSave, currentBlock);

            GenerateFields(elementSave, currentBlock);
            
            GenerateProperties(elementSave, currentBlock);

            EventCodeGenerator.Self.GenerateEvents(elementSave, currentBlock);

            string runtimeClassName = GetUnqualifiedRuntimeTypeFor(elementSave);
            GenerateConstructor(elementSave, currentBlock, runtimeClassName);

            GenerateAssignDefaultState(elementSave, currentBlock);

            GenerateCreateChildrenRecursively(elementSave, currentBlock);

            GenerateAssignReferencesMethod(elementSave, currentBlock);

            GenerateAddToManagersMethod(elementSave, currentBlock);

            GenerateCallCustomInitialize(elementSave, currentBlock);

            GeneratePartialMethods(elementSave, currentBlock);

            GenerateRaiseExposedEvents(elementSave, currentBlock);

            GenerateFormsCode(elementSave, currentBlock);
        }

        #region Class Name
        private ICodeBlock GenerateClassHeader(ICodeBlock codeBlock, ElementSave elementSave)
        {
            string runtimeClassName = GetUnqualifiedRuntimeTypeFor(elementSave); 

            string inheritance = "Gum.Wireframe.GraphicalUiElement";

            if(elementSave.BaseType != "Component" && !string.IsNullOrEmpty(elementSave.BaseType))
            {
                inheritance = GueRuntimeNamespace + "." + elementSave.BaseType.Replace("/", ".") + "Runtime";
            }

            var asComponentSave = elementSave as ComponentSave;

            // If it's not public then exposing an instance in a public class makes the project not compile
            //ICodeBlock currentBlock = codeBlock.Class("partial", runtimeClassName, " : " + inheritance);
            ICodeBlock currentBlock = codeBlock.Class("public partial", runtimeClassName, " : " + inheritance);



            return currentBlock;
        }

        public string GetQualifiedRuntimeTypeFor(ElementSave elementSave)
        {
            return GueDerivingClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(elementSave) + 
                "." + GetUnqualifiedRuntimeTypeFor(elementSave);
        }

        public string GetUnqualifiedRuntimeTypeFor(ElementSave elementSave)
        {
            if(elementSave == null)
            {
                throw new ArgumentNullException(nameof(elementSave));
            }
            return FlatRedBall.IO.FileManager.RemovePath( elementSave.Name) + "Runtime";
        }

        public string GetFullRuntimeNamespaceFor(ElementSave elementSave)
        {

            string subNamespace = null;

            if ((elementSave is Gum.DataTypes.ScreenSave || elementSave is Gum.DataTypes.ComponentSave) && (elementSave.Name.Contains('/')))
            {
                subNamespace = elementSave.Name.Substring(0, elementSave.Name.LastIndexOf('/')).Replace('/', '.');
            }
            else // if(elementSave is StandardElementSave)
            {
                // can't be in a subfolder
                subNamespace = null;
            }

            if (!string.IsNullOrEmpty(subNamespace))
            {
                subNamespace = '.' + subNamespace;
            }

            var fullNamespace = GueRuntimeNamespace + subNamespace;

            return fullNamespace;
        }

        #endregion

        #region Generate Fields

        private void GenerateFields(ElementSave elementSave, ICodeBlock currentBlock)
        {
            string throwaway;
            if(GetIfShouldGenerateFormsCode(elementSave, out throwaway))
            {
                currentBlock.Line("private bool tryCreateFormsObject;");
            }
        }

        #endregion

        #region Generate Properties

        public string GetQualifiedRuntimeTypeFor(InstanceSave instance)
        {
            var element = ObjectFinder.Self.GetElementSave(instance);
            if(element == null)
            {
                return "UnknownType";
            }
            else
            {
                return GetQualifiedRuntimeTypeFor(element);
            }
        }

        private void GenerateProperties(ElementSave elementSave, ICodeBlock currentBlock)
        {
            GenerateInstanceProperties(elementSave, currentBlock);

            // We only generate exposed variables here, because all other
            // variables are declared in the base class.
            GenerateExposedVariableProperties(elementSave, currentBlock);
        }

        private void GenerateInstanceProperties(ElementSave elementSave, ICodeBlock currentBlock)
        {

            string publicOrPrivate;
            if(elementSave is Gum.DataTypes.ScreenSave)
            {
                // make these public for screens because the only time this will be accessed is in the Glue screen that owns it
                publicOrPrivate = "public";
            }
            else
            {
                publicOrPrivate = "private";
            }
            foreach (var instance in elementSave.Instances)
            {
                string type = GetQualifiedRuntimeTypeFor(instance);

                if (GetIfInstanceReferencesValidComponent(instance))
                {
                    ICodeBlock property = currentBlock.AutoProperty($"{publicOrPrivate} " + type, instance.MemberNameInCode());
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

        private void GenerateExposedVariableProperties(ElementSave elementSave, ICodeBlock currentBlock)
        {
            // Get all exposed variables and make properties out of them.
            // All other properties should be handled by the base type
            if (elementSave.DefaultState != null)
            {
                var allVariablesToProcess = elementSave.DefaultState.Variables
                    .Where(item => !string.IsNullOrEmpty(item.ExposedAsName))
                    .ToList();

                foreach (var variable in allVariablesToProcess)
                {
                    GenerateExposedVariableProperty(elementSave, currentBlock, variable);
                }

            }
        }

        private void GenerateExposedVariableProperty(ElementSave elementSave, ICodeBlock currentBlock, VariableSave variable)
        {
            string variableType = variable.Type;

            ModifyVariableTypeForProperty(ref variableType, variable, elementSave);

            string propertyName = variable.ExposedAsName.Replace(" ", "_");

            ICodeBlock property = currentBlock.Property("public " + variableType, propertyName);

            string whatToGetOrSet = variable.Name;

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
                if (rootName.EndsWith("State"))
                {
                    var withoutState = rootName.Substring(0, rootName.Length - "State".Length);
                    if (rootName == "State")
                    {
                        whatToGetOrSet = variable.SourceObject + "." + "CurrentVariableState";
                    }
                    else if (baseElement != null && baseElement.Categories.Any(item => item.Name == withoutState))
                    {
                        whatToGetOrSet = variable.SourceObject + ".Current" + withoutState + "State";
                    }

                }
            }

            property.Get()
                .Line("return " + whatToGetOrSet + ";");
            var setter = property.Set();

            if (EventCodeGenerator.Self.GetIfShouldGenerateEventOnVariableSet(elementSave, variable))
            {
                string eventName = EventCodeGenerator.Self.GetEventName(variable, elementSave);

                setter.If($"{whatToGetOrSet} != value")
                    .Line(whatToGetOrSet + " = value;")
                    .Line($"{eventName}?.Invoke(this, null);");
            }
            else
            {
                setter.Line(whatToGetOrSet + " = value;");
            }
        }


        private void AddGetterReplacements()
        {

        }

        private void AddSetterReplacements()
        {

        }
        #endregion

        #region Constructor

        public void GenerateConstructor(ElementSave elementSave, ICodeBlock currentBlock, string runtimeClassName)
        {
            string baseCall = null;

            bool hasBase = !string.IsNullOrEmpty(elementSave.BaseType);

            if(hasBase)
            {
                baseCall = "base(false, tryCreateFormsObject)";
            }
            var constructor = currentBlock.Constructor("public", runtimeClassName, "bool fullInstantiation = true, bool tryCreateFormsObject = true",  baseCall );

            // This may not have a value, so if not, don't set it:
            var state = elementSave.DefaultState;

            string throwaway;
            if (GetIfShouldGenerateFormsCode(elementSave, out throwaway))
            {
                constructor.Line("this.tryCreateFormsObject = tryCreateFormsObject;");
            }

            // State can be null if the backing file for this element
            // doesn't exist. We can tolerate it because the error window
            // in Glue will report it as an error
            if(state?.GetVariableSave("HasEvents") != null)
            {
                bool hasEvents = state.GetValueOrDefault<bool>("HasEvents");
                constructor.Line($"this.HasEvents = {hasEvents.ToString().ToLower()};");
            }

            if (state?.GetVariableSave("ExposeChildrenEvents") != null)
            {
                bool exposeChildrenEvents = state.GetValueOrDefault<bool>("ExposeChildrenEvents");
                constructor.Line($"this.ExposeChildrenEvents = {exposeChildrenEvents.ToString().ToLower()};");
            }

            var ifStatement = constructor.If("fullInstantiation");

            string componentScreenOrStandard = null;

            if(elementSave is ComponentSave)
            {
                componentScreenOrStandard = "Components";
            }
            else if(elementSave is Gum.DataTypes.ScreenSave)
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
        }

        #endregion

        #region Initializing Methods

        private void GenerateAssignDefaultState(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock = currentBlock.Function("public override void", "SetInitialState", "");
            {
                bool shouldCallBase = elementSave is StandardElementSave == false;
                if(shouldCallBase)
                {
                    currentBlock.Line("base.SetInitialState();");
                }
                currentBlock.Line("this.CurrentVariableState = VariableState.Default;");

                currentBlock.Line("CallCustomInitialize();");
            }
        }

        private void GenerateCreateChildrenRecursively(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock = currentBlock.Function("public override void", "CreateChildrenRecursively", "Gum.DataTypes.ElementSave elementSave, RenderingLibrary.SystemManagers systemManagers");
            {

                currentBlock.Line("base.CreateChildrenRecursively(elementSave, systemManagers);");
                currentBlock.Line("this.AssignReferences();");
            }

            //public virtual void CreateChildrenRecursively(ElementSave elementSave, SystemManagers systemManagers)

        }

        private void GenerateAssignReferencesMethod(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock = currentBlock.Function("private void", "AssignReferences", "");
            {
                foreach (var instance in elementSave.Instances)
                {
                    var foundBase = Gum.Managers.ObjectFinder.Self.GetElementSave(instance.BaseType);

                    if (foundBase != null)
                    {
                        currentBlock.Line(instance.MemberNameInCode() +
                            // Use the actual instance name rather than MemberNameInCode here, because it may differ
                            // if there's invalid member characters like dashes
                            $" = this.GetGraphicalUiElementByName(\"{instance.Name}\") as " + 
                            GetQualifiedRuntimeTypeFor(instance) + ";");
                        
                        foreach (var eventSave in elementSave.Events.Where(item =>
                            item.GetSourceObject() == instance.Name && !string.IsNullOrEmpty(item.ExposedAsName)))
                        {
                            currentBlock.Line(eventSave.Name + " += Raise" + eventSave.ExposedAsName + ";");
                        }
                    }
                }

                List<EventSave> exposedChildrenEvents = EventCodeGenerator.Self.GetExposedChildrenEvents(elementSave);

                foreach (var exposedChildEvent in exposedChildrenEvents)
                {
                    currentBlock.Line($"{exposedChildEvent.Name} += (unused) => {exposedChildEvent.ExposedAsName}?.Invoke(this);");
                }

                string controlType;
                var shouldGenerate = GetIfShouldGenerateFormsCode(elementSave, out controlType);
                if(shouldGenerate)
                {
                    currentBlock
                        .If("tryCreateFormsObject")
                        .Line($"FormsControl = new {controlType}(this);");

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

        private void GenerateCallCustomInitialize(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock = currentBlock.Function("private void", "CallCustomInitialize", "");
            {
                currentBlock.Line("CustomInitialize();");

            }
        }

        #endregion

        #region FlatRedBall.Forms code

        private void GenerateFormsCode(ElementSave element, ICodeBlock currentBlock)
        {
            string controlType;
            var shouldGenerate = GetIfShouldGenerateFormsCode(element, out controlType);

            if(shouldGenerate)
            {
                currentBlock.Line($"public {controlType} FormsControl {{get; private set;}}");
                currentBlock.Line($"public override object FormsControlAsObject {{ get {{ return FormsControl; }} }}");

            }
        }

        private static bool GetIfShouldGenerateFormsCode(ElementSave element, out string controlType)
        {
            bool shouldGenerateFormsCode = false;
            controlType = null;

            if(element is ComponentSave)
            {
                var component = (ComponentSave)element;

                var behaviors = component.Behaviors;

                controlType = GetFormsControlTypeFrom(behaviors);
                
                if(!string.IsNullOrEmpty(controlType))
                {
                    var gumxRfs = GumProjectManager.Self.GetRfsForGumProject();


                    if (gumxRfs != null)
                    {
                        var property = gumxRfs.Properties
                            .FirstOrDefault(item => item.Name == "IncludeFormsInComponents");

                        if (property != null)
                        {
                            shouldGenerateFormsCode = (bool)property.Value;
                        }
                    }

                }
            }
            return shouldGenerateFormsCode;
        }

        #endregion

        public bool ShouldGenerateRuntimeFor(ElementSave elementSave)
        {
            if (elementSave is StandardElementSave)
            {
                if (elementSave.Name == "Component")
                {
                    return false;
                }
            }

            return true;
        }

        private static string GetFormsControlTypeFrom(List<ElementBehaviorReference> behaviors)
        {
            string controlName = null;
            foreach(var behavior in behaviors)
            {
                var control = FormsControlInfo.AllControls
                    .FirstOrDefault(item => item.BehaviorName == behavior.BehaviorName);

                // this may not have a control name if it's only a component used in other controls
                if(control?.ControlName != null)
                {
                    controlName = control.ControlName;
                    break;
                }
            }

            if(controlName != null)
            {
                return $"FlatRedBall.Forms.Controls.{controlName}";
            }
            else
            {
                return null;
            }
        }


        private void GeneratePartialMethods(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("partial void CustomInitialize();");
        }

        internal string GetAddToManagersFunc(IElement glueElement, NamedObjectSave namedObjectSave, ReferencedFileSave referencedFileSave, string layerName)
        {
            var stringBuilder = new StringBuilder();

            var namedObjectName = namedObjectSave.FieldName;
            string layerCode = "null";
            if(!string.IsNullOrEmpty(layerName))
            {
                layerCode = $"System.Linq.Enumerable.FirstOrDefault(FlatRedBall.Gum.GumIdb.AllGumLayersOnFrbLayer({layerName}))";
            }
            if(glueElement is EntitySave)
            {
                stringBuilder.AppendLine( "{");
                stringBuilder.AppendLine( $"{namedObjectName}.AddToManagers(RenderingLibrary.SystemManagers.Default, {layerCode});");

                var shouldGenerateWrapper = namedObjectSave.AttachToContainer;

                if(shouldGenerateWrapper)
                {
                    stringBuilder.AppendLine( $"var wrapperForAttachment = new GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper(this, {namedObjectName});");
                    stringBuilder.AppendLine(  "FlatRedBall.SpriteManager.AddPositionedObject(wrapperForAttachment);");
                    stringBuilder.AppendLine("gumAttachmentWrappers.Add(wrapperForAttachment);");
                }


                stringBuilder.AppendLine( "}");
            }
            else
            {
                stringBuilder.AppendLine($"{namedObjectName}.AddToManagers(RenderingLibrary.SystemManagers.Default, {layerCode});");
            }

            return stringBuilder.ToString();
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
                    //variableType = FlatRedBall.IO.FileManager.RemovePath(elementSave.Name) + "Runtime.VariableState";
                    // Actually we want to include the prefix for namespace:
                    //variableType = FlatRedBall.IO.FileManager.RemovePath(elementSave.Name) + "Runtime.VariableState";
                    variableType = elementSave.Name.Replace('/', '.').Replace('\\', '.') + "Runtime.VariableState";
                }
                else if( variableSave.Type.EndsWith("State"))
                {
                    var typeWithoutState = variableSave .Type.Substring(0, variableSave.Type.Length - "State".Length);
                    var foundCategory = elementSave.Categories.FirstOrDefault(item => item.Name == typeWithoutState);
                    if (foundCategory != null)
                    {
                        // categorized state enums are nullable
                        variableType = $"{elementSave.Name.Replace('/', '.').Replace('\\', '.')}Runtime.{foundCategory.Name}?" ;
                    }
                }
                else if(variableSave.IsState(elementSave))
                {
                    var foundCategory = elementSave.Categories.FirstOrDefault(item => item.Name == variableSave.Type);
                    if (foundCategory != null)
                    {
                        // categorized state enums are nullable
                        variableType = $"{elementSave.Name.Replace('/', '.').Replace('\\', '.')}Runtime.{foundCategory.Name}?";
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
            Gum.DataTypes.Variables.StateSaveCategory stateSaveCategory;

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


                variableValue = GetQualifiedRuntimeTypeFor(categoryContainer) + 
                    "." + categoryName + "." + variableSave.Value;
            }

            else if (variableSave.IsEnumeration())
            {
                AdjustEnumerationVariableValue(variableSave, container, ref variableValue, ref variableType);
            }
            else if(variableSave.GetRootName() == "Parent")
            {
                if(container is ComponentSave)
                {
                    variableValue = $"this.ContainedElements.FirstOrDefault(item =>item.Name == \"{variableValue}\") ?? this";

                }
                else
                {
                    variableValue = $"this.ContainedElements.FirstOrDefault(item =>item.Name == \"{variableValue}\")";
                }
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
                // do this after replacing the backslashes up above
                variableValue = variableValue.Replace("\n", "\\n");
                variableValue = "\"" + variableValue + "\"";
             
            }
            else if (variableSave.Type == "float")
            {
                //variableValue = variableValue + "f";
                // convert this using the current language:

                var value = Convert.ToSingle(variableValue, System.Globalization.CultureInfo.CurrentCulture);
                variableValue = value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";

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


    }
}
