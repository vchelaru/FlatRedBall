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
using GumPlugin.ViewModels;
using GumPluginCore.CodeGeneration;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

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

        public static string GueRuntimeNamespace =>
            FlatRedBall.Glue.ProjectManager.ProjectNamespace + ".GumRuntimes";

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
            mTypeToQualifiedTypes.Add("GradientType", "global::RenderingLibrary.Graphics.GradientType");
            mTypeToQualifiedTypes.Add("TextOverflowHorizontalMode", "global::RenderingLibrary.Graphics.TextOverflowHorizontalMode");
            mTypeToQualifiedTypes.Add("TextOverflowVerticalMode", "global::RenderingLibrary.Graphics.TextOverflowVerticalMode");

            AddGetterReplacements();

            AddSetterReplacements();
        }

        #region Element/Screen Top Level Generation

        public string GenerateCodeFor(ElementSave elementSave)
        {
            if(elementSave == null)
            {
                throw new ArgumentNullException(nameof(elementSave));
            }

            var topBlock = new CodeBlockBase();
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

            ICodeBlock classBlock = GenerateClassHeader(codeBlock, elementSave);

            StateCodeGenerator.Self.GenerateEverythingFor(elementSave, classBlock);

            BehaviorCodeGenerator.Self.GenerateBehaviorImplementingProperties(classBlock, elementSave);

            GenerateFields(elementSave, classBlock);
            
            GenerateProperties(elementSave, classBlock);

            EventCodeGenerator.Self.GenerateEvents(elementSave, classBlock);

            string runtimeClassName = GetUnqualifiedRuntimeTypeFor(elementSave);
            GenerateConstructor(elementSave, classBlock, runtimeClassName);

            GenerateAssignDefaultState(elementSave, classBlock);

            GenerateCreateChildrenRecursively(elementSave, classBlock);

            GenerateAssignInternalReferencesMethod(elementSave, classBlock);

            GenerateAddToManagersMethod(elementSave, classBlock);

            GenerateCallCustomInitialize(elementSave, classBlock);

            GeneratePartialMethods(elementSave, classBlock);

            GenerateRaiseExposedEvents(elementSave, classBlock);

            GenerateFormsCode(elementSave, classBlock);
        }


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

        #endregion

        #region Class Header/Name
        private ICodeBlock GenerateClassHeader(ICodeBlock codeBlock, ElementSave elementSave)
        {
            string runtimeClassName = GetUnqualifiedRuntimeTypeFor(elementSave); 

            string inheritance = "Gum.Wireframe.GraphicalUiElement";

            if(elementSave.BaseType != "Component" && !string.IsNullOrEmpty(elementSave.BaseType))
            {
                inheritance = GueRuntimeNamespace + "." + elementSave.BaseType.Replace("/", ".") + "Runtime";

                if(elementSave.BaseType == "Container")
                {
                    // does this have a contained type?
                    var rfv = new RecursiveVariableFinder(elementSave.DefaultState);

                    var containedType = rfv.GetValue<string>("Contained Type");

                    if(!string.IsNullOrEmpty(containedType))
                    {
                        var genericType =
                            GueRuntimeNamespace + "." + containedType.Replace("/", ".") + "Runtime";
                        inheritance += $"<{genericType}>";
                    }
                }
            }

            foreach(var behaviorReference in elementSave.Behaviors)
            {
                var behavior = ObjectFinder.Self.GetBehavior(behaviorReference);

                if(behavior != null)
                {
                    // This could be a bad reference, so tolerate it
                    var fullName = CodeGeneratorManager.Self.BehaviorCodeGenerator
                        .GetFullyQualifiedBehaviorName(behavior);

                    inheritance += $", {fullName}";
                }
            }

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
            string elementName = elementSave.Name;
            var isStandardElement = elementSave is StandardElementSave;

            return GetFullRuntimeNamespaceFor(isStandardElement, elementName);
        }

        public string GetFullRuntimeNamespaceFor(bool isStandardElement, string elementName)
        {
            string subNamespace = null;
            if (isStandardElement == false && (elementName.Contains('/')))
            {
                subNamespace = elementName.Substring(0, elementName.LastIndexOf('/')).Replace('/', '.');
            }
            else // if(elementSave is StandardElementSave)
            {
                // can't be in a subfolder
                subNamespace = null;
            }

            if (!string.IsNullOrEmpty(subNamespace))
            {
                subNamespace = '.' + subNamespace;
                subNamespace = subNamespace.Replace(" ", "_");
            }


            var fullNamespace = GueRuntimeNamespace + subNamespace;

            return fullNamespace;
        }

        #endregion

        #region Generate Fields

        private void GenerateFields(ElementSave elementSave, ICodeBlock currentBlock)
        {
            string throwaway;
            if(GetIfShouldGenerateFormsCode(elementSave, out throwaway) || FormsClassCodeGenerator.Self.GetIfShouldGenerate(elementSave))
            {
                currentBlock.Line("private bool tryCreateFormsObject;");
            }
        }

        #endregion

        #region Generate Properties

        public string GetQualifiedRuntimeTypeFor(InstanceSave instance, ElementSave container)
        {
            var element = ObjectFinder.Self.GetElementSave(instance);
            if(element == null)
            {
                return "UnknownType";
            }
            else
            {
                var qualifiedRuntimeType = GetQualifiedRuntimeTypeFor(element);

                var isContainer = element is StandardElementSave && element.Name == "Container";

                if(isContainer)
                {
                    var variable = instance.Name + ".Contained Type";
                    var genericType = (string)container.DefaultState.GetValueRecursive(variable);

                    if(!string.IsNullOrEmpty(genericType))
                    {
                        var qualifiedGenericType = GueDerivingClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(false, genericType) +
                            "." + 
                            FlatRedBall.IO.FileManager.RemovePath(genericType) + "Runtime";

                        qualifiedRuntimeType += $"<{qualifiedGenericType}>";
                    }
                }

                return qualifiedRuntimeType;
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
            var rfs = GumProjectManager.Self.GetRfsForGumProject();

            var makePublic = rfs?.Properties.GetValue<bool>(nameof(GumViewModel.MakeGumInstancesPublic)) == true;

            string publicOrPrivate;
            if(elementSave is Gum.DataTypes.ScreenSave || makePublic)
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
                string type = GetQualifiedRuntimeTypeFor(instance, elementSave);

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
            var instance = elementSave.GetInstance(variable.SourceObject);

            /////////////////////Early Out/////////////////////////////
            var isMissingInstance = instance == null && !string.IsNullOrEmpty(variable.SourceObject);
            if(isMissingInstance)
            {
                return;
            }
            //////////////////End Early Out///////////////////////////

            string variableType = variable.Type;

            ModifyVariableTypeForProperty(ref variableType, variable, elementSave);

            string propertyName = variable.ExposedAsName.Replace(" ", "_");

            ICodeBlock property = currentBlock.Property("public " + variableType, propertyName);

            string whatToGetOrSet = variable.Name;

            // If this is an exposed property on a standard element, then we just need to kill all spaces and replace
            // them with nothing

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

            // Create a no-arg constructor for VisualTemplates
            currentBlock.Constructor("public", runtimeClassName, "", "this(true, true)");

            bool hasBase = !string.IsNullOrEmpty(elementSave.BaseType);

            if(hasBase)
            {
                baseCall = "base(false, tryCreateFormsObject)";
            }
            var constructor = currentBlock.Constructor("public", runtimeClassName, "bool fullInstantiation = true, bool tryCreateFormsObject = true",  baseCall );

            // This may not have a value, so if not, don't set it:
            var state = elementSave.DefaultState;

            string throwaway;
            if (GetIfShouldGenerateFormsCode(elementSave, out throwaway) || FormsClassCodeGenerator.Self.GetIfShouldGenerate(elementSave))
            {
                constructor.Line("this.tryCreateFormsObject = tryCreateFormsObject;");
            }

            // State can be null if the backing file for this element
            // doesn't exist. We can tolerate it because the error window
            // in Glue will report it as an error
            if(state?.GetVariableSave("HasEvents") != null)
            {
                bool hasEvents = state.GetValueOrDefault<bool>("HasEvents");
                constructor.Line($"this.HasEvents = {hasEvents.ToString().ToLowerInvariant()};");
            }

            if (state?.GetVariableSave("ExposeChildrenEvents") != null)
            {
                bool exposeChildrenEvents = state.GetValueOrDefault<bool>("ExposeChildrenEvents");
                constructor.Line($"this.ExposeChildrenEvents = {exposeChildrenEvents.ToString().ToLowerInvariant()};");
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
                // If we don't do this:
                // This could be slow...
                // This could cause UI to update when it shouldn't (such as a Forms object responding to size changes before it is completely built)
                var suspendLayout = GlueState.Self.CurrentGlueProject.FileVersion >= (int)FlatRedBall.Glue.SaveClasses.GlueProjectSave.GluxVersions.GumHasMIsLayoutSuspendedPublic;

                if(suspendLayout)
                {
                    currentBlock.Line("var wasSuppressed = this.IsLayoutSuspended;");
                    currentBlock.Line("if(!wasSuppressed) this.SuspendLayout();");
                }
                bool shouldCallBase = elementSave is StandardElementSave == false;
                if(shouldCallBase)
                {
                    currentBlock.Line("base.SetInitialState();");
                }
                currentBlock.Line("this.CurrentVariableState = VariableState.Default;");
                if (suspendLayout)
                {
                    currentBlock.Line("if(!wasSuppressed) this.ResumeLayout();");
                }

                currentBlock.Line("CallCustomInitialize();");
            }
        }

        private void GenerateCreateChildrenRecursively(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock = currentBlock.Function("public override void", "CreateChildrenRecursively", "Gum.DataTypes.ElementSave elementSave, RenderingLibrary.SystemManagers systemManagers");
            {

                currentBlock.Line("base.CreateChildrenRecursively(elementSave, systemManagers);");
                currentBlock.Line("this.AssignInternalReferences();");
            }

            //public virtual void CreateChildrenRecursively(ElementSave elementSave, SystemManagers systemManagers)

        }

        private void GenerateAssignInternalReferencesMethod(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock = currentBlock.Function("private void", "AssignInternalReferences", "");
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
                            GetQualifiedRuntimeTypeFor(instance, elementSave) + ";");
                        
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

                if (GlueState.Self.CurrentGlueProject?.FileVersion >= (int)GluxVersions.GraphicalUiElementINotifyPropertyChanged)
                {
                    var qualifiedElementName = elementSave.Name;
                    if (elementSave is ComponentSave)
                    {
                        qualifiedElementName = "Components/" + elementSave.Name;
                    }
                    else if (elementSave is Gum.DataTypes.ScreenSave)
                    {
                        qualifiedElementName = "Screens/" + elementSave.Name;
                    }
                    else if (elementSave is StandardElementSave)
                    {
                        qualifiedElementName = "StandardElements/" + elementSave.Name;
                    }

                    // for now we'll just support variable references in default state
                    foreach(var variableList in elementSave.DefaultState.VariableLists)
                    {
                        if(variableList.GetRootName() == "VariableReferences" && variableList.ValueAsIList?.Count > 0)
                        {
                            string receiverOfReference = variableList.SourceObject ?? "this";
                            foreach(string value in variableList.ValueAsIList)
                            {
                            
                                if(value?.Contains(".") == true)
                                {
                                    var valueLeftOfEquals = value.Substring(0, value.IndexOf("=")).Trim();
                                    var valueRightOfEquals = value.Substring(value.IndexOf("=") + 1).Trim(); ;

                                    // for now we only care about internal references...
                                    if(valueRightOfEquals.Contains("/") && valueRightOfEquals.StartsWith(qualifiedElementName))
                                    {
                                        // it's qualified, so let's see if it starts with the name of this element
                                        valueRightOfEquals = valueRightOfEquals.Substring(qualifiedElementName.Length + 1);
                                    }

                                    // make sure it is no longer qualified
                                    if(!valueRightOfEquals.Contains("/"))
                                    {
                                        var instanceOwningReferencedVariable = valueRightOfEquals.Substring(0, valueRightOfEquals.IndexOf("."));
                                        var valueWithoutVariable = valueRightOfEquals.Substring(valueRightOfEquals.IndexOf(".") + 1);

                                        // for now a new event += per reference. Later we can combine these.
                                        var eventBlock = currentBlock.Line($"{instanceOwningReferencedVariable}.PropertyChanged += (sender, args) =>");
                                        eventBlock = eventBlock.Block();
                                        eventBlock.If("args.PropertyName == nameof(" + valueWithoutVariable + ")")
                                            .Line($"{receiverOfReference}.{valueLeftOfEquals} = {instanceOwningReferencedVariable}.{valueWithoutVariable};");
                                        currentBlock.Line(";");
                                    }
                                }
                            }
                        }
                    }
                }
                    

                string controlType;
                var shouldGenerate = GetIfShouldGenerateFormsCode(elementSave, out controlType);

                if(!shouldGenerate &&
                    GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.HasFormsObject)
                {
                    shouldGenerate = FormsClassCodeGenerator.Self.GetIfShouldGenerate(elementSave);

                    if(shouldGenerate)
                    {
                        controlType = FormsClassCodeGenerator.Self.GetQualifiedRuntimeTypeFor(elementSave);
                    }
                }
                if(shouldGenerate)
                {
                    currentBlock
                        .If("tryCreateFormsObject")
                        .Line($"FormsControlAsObject = new {controlType}(this);");

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
            string controlType = null;
            var shouldGenerateStandardForms = GetIfShouldGenerateFormsCode(element, out controlType);

            // This is for standard forms types like Button, TextBox, etc
            if(shouldGenerateStandardForms)
            {
                // This should be protected so derived classes can specify this
                currentBlock.Line($"public {controlType} FormsControl {{get => ({controlType}) FormsControlAsObject;}}");
            }
            // This is for custom classes that exist in Gum but don't have standard Gum representations, like game-specific pause menus, popups, etc
            else if(FormsClassCodeGenerator.Self.GetIfShouldGenerate(element))
            {
                controlType = FormsClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(element) + "." +
                    FormsClassCodeGenerator.Self.GetUnqualifiedRuntimeTypeFor(element);

                currentBlock.Line($"public {controlType} FormsControl {{get => ({controlType}) FormsControlAsObject;}}");
            }
        }

        private static bool GetIfShouldGenerateFormsCode(ElementSave element, out string controlType)
        {
            controlType = null;
            if(GlueState.Self.CurrentGlueProject.FileVersion < (int)GlueProjectSave.GluxVersions.HasFormsObject)
            {
                return false;
            }
            bool shouldGenerateFormsCode = false;

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

        public static string GetFormsControlTypeFrom(List<ElementBehaviorReference> behaviors)
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
                if(controlName.Contains("."))
                {
                    return controlName;
                }
                else
                {
                    return $"FlatRedBall.Forms.Controls.{controlName}";
                }
            }
            else
            {
                return null;
            }
        }

        #endregion



        private void GeneratePartialMethods(ElementSave elementSave, ICodeBlock currentBlock)
        {
            currentBlock.Line("partial void CustomInitialize();");
        }

        internal string GetAddToManagersFunc(IElement glueElement, NamedObjectSave namedObjectSave, ReferencedFileSave referencedFileSave, string layerName)
        {
            var stringBuilder = new StringBuilder();

            var namedObjectName = namedObjectSave?.FieldName ?? referencedFileSave.GetInstanceName();
            string layerCode = "null";
            if(!string.IsNullOrEmpty(layerName))
            {
                layerCode = $"System.Linq.Enumerable.FirstOrDefault(FlatRedBall.Gum.GumIdb.AllGumLayersOnFrbLayer({layerName}))";
            }
            if(glueElement is EntitySave)
            {
                stringBuilder.AppendLine( "{");
                stringBuilder.AppendLine( $"{namedObjectName}.AddToManagers(RenderingLibrary.SystemManagers.Default, {layerCode});");

                var shouldGenerateWrapper = namedObjectSave?.AttachToContainer == true;

                if(shouldGenerateWrapper)
                {
                    stringBuilder.AppendLine( $"var wrapperForAttachment = new GumCoreShared.FlatRedBall.Embedded.PositionedObjectGueWrapper(this, {namedObjectName});");
                    stringBuilder.AppendLine($"wrapperForAttachment.Name = \"{namedObjectName}\";");
                    stringBuilder.AppendLine( "FlatRedBall.SpriteManager.AddPositionedObject(wrapperForAttachment);");
                    stringBuilder.AppendLine("gumAttachmentWrappers.Add(wrapperForAttachment);");

                    GumCollidableCodeGenerator.GenerateAddCollision(stringBuilder, glueElement, namedObjectSave);
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

                    var instanceElement = ObjectFinder.Self.GetElementSave(instance);

                    if(instanceElement != null)
                    {

                        var rootName = variableSave.GetRootName();
                        var variableInInstanceElement = instanceElement.DefaultState.Variables.FirstOrDefault(item => item.Name == rootName || item.ExposedAsName == rootName);

                        if(variableInInstanceElement != null)
                        {
                            ModifyVariableTypeForProperty(ref variableType, variableInInstanceElement, instanceElement);
                        }
                        else if (variableSave.Type.EndsWith("State"))
                        {
                            var typeWithoutState = variableSave.Type.Substring(0, variableSave.Type.Length - "State".Length);
                            var foundCategory = instanceElement.Categories.FirstOrDefault(item => item.Name == typeWithoutState);
                            if (foundCategory != null)
                            {
                                // categorized state enums are nullable
                                variableType = $"{instanceElement.Name.Replace('/', '.').Replace('\\', '.')}Runtime.{foundCategory.Name}?";
                            }
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

                var stateSaveValueInCode = (variableSave.Value as string)?.Replace(" ", "_")?.Replace("-", "_");
                if (stateSaveValueInCode?.Length > 0 && char.IsDigit(stateSaveValueInCode[0]))
                {
                    stateSaveValueInCode = "_" + stateSaveValueInCode;
                }
                variableValue = GetQualifiedRuntimeTypeFor(categoryContainer) + 
                    "." + categoryName + "." + stateSaveValueInCode;
            }

            else if (variableSave.IsEnumeration())
            {
                AdjustEnumerationVariableValue(variableSave, container, ref variableValue, ref variableType);
            }
            else if(variableSave.GetRootName() == "Parent")
            {
                if(container is ComponentSave)
                {
                    // This does a strict comparison, but GetGraphicalUiElementByName supports the dot index, so use that
                    //variableValue = $"this.ContainedElements.FirstOrDefault(item =>item.Name == \"{variableValue}\") ?? this";
                    variableValue = $"this.GetGraphicalUiElementByName(\"{variableValue}\") ?? this";
                    
                }
                else
                {
                    variableValue = $"this.GetGraphicalUiElementByName(\"{variableValue}\")";
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
            else if(variableSave.Type == "decimal")
            {
                var value = Convert.ToDecimal(variableValue, System.Globalization.CultureInfo.CurrentCulture);
                variableValue = value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "m";
            }
            else if (variableSave.Type == "bool")
            {
                variableValue = variableValue.ToLowerInvariant();
            }


        }

        private void AdjustEnumerationVariableValue(Gum.DataTypes.Variables.VariableSave variableSave, ElementSave element, ref string variableValue, ref string variableType)
        {
            if (variableSave.Type == "Gum.Managers.PositionUnitType" || variableSave.Type == "PositionUnitType")
            {
                string rootName = variableSave.GetRootName();

                // convert from PositionUnitType to GeneralUnitType
                // Update December 28, 2022
                // The SkiaGums use different
                // unit types for gradients, so
                // let's special case this:
                var shouldConvert = true;

                var typeOfVariableOwner = element.Name;
                if(!string.IsNullOrEmpty(variableSave.SourceObject))
                {
                    var instance = element.GetInstance(variableSave.SourceObject);
                    typeOfVariableOwner = instance.BaseType;
                }

                if(typeOfVariableOwner == "Arc")
                {
                    if(rootName == "GradientX1Units" ||
                       rootName == "GradientX2Units" ||
                       rootName == "GradientY1Units" ||
                       rootName == "GradientY2Units")
                    {
                        shouldConvert = false;
                    }
                }

                if (typeOfVariableOwner == "ColoredCircle")
                {
                    if (rootName == "GradientX1Units" ||
                       rootName == "GradientX2Units" ||
                       rootName == "GradientY1Units" ||
                       rootName == "GradientY2Units")
                    {
                        shouldConvert = false;
                    }
                }

                if (typeOfVariableOwner == "RoundedRectangle")
                {
                    if (rootName == "GradientX1Units" ||
                        rootName == "GradientX2Units" ||
                        rootName == "GradientY1Units" ||
                        rootName == "GradientY2Units")
                    {
                        shouldConvert = false;
                    }
                }

                if (shouldConvert)
                {
                    GeneralUnitType convertedValue =
                        UnitConverter.ConvertToGeneralUnit((PositionUnitType)variableSave.Value);

                    variableValue = convertedValue.ToString();

                    variableType = "Gum.Converters.GeneralUnitType";
                }
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
