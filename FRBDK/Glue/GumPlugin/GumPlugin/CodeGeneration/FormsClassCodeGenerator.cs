using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Gum.DataTypes;
using Gum.Managers;
using GumPlugin.CodeGeneration;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static FlatRedBall.Glue.SaveClasses.GlueProjectSave;

namespace GumPluginCore.CodeGeneration
{
    public class FormsClassCodeGenerator : Singleton<FormsClassCodeGenerator>
    {
        public static string FormsRuntimeNamespace =>
            FlatRedBall.Glue.ProjectManager.ProjectNamespace + ".FormsControls";

        public string GenerateCodeFor(ElementSave elementSave)
        {
            if (elementSave == null)
            {
                throw new ArgumentNullException(nameof(elementSave));
            }

            var shouldGenerate = GetIfShouldGenerate(elementSave);

            if (shouldGenerate)
            {
                var topBlock = new CodeBlockBase();
                var fullNamespace = GetFullRuntimeNamespaceFor(elementSave);
                var currentBlock = topBlock.Namespace(fullNamespace);
                GenerateScreenAndComponentCodeFor(elementSave, currentBlock);
                return topBlock.ToString();
            }
            else
            {
                return null;
            }
        }

        public bool GetIfShouldGenerate(ElementSave elementSave)
        {
            bool shouldGenerate = false;

            var isScreen = elementSave is ScreenSave;
            var isComponent = elementSave is ComponentSave;
            if (isScreen)
            {
                shouldGenerate = true;
            }
            else if (isComponent)
            {

                // don't do anything with standards
                var component = elementSave as ComponentSave;

                var behaviors = component?.Behaviors;

                string controlType = null;

                if (behaviors != null)
                {
                    controlType = GueDerivingClassCodeGenerator.GetFormsControlTypeFrom(behaviors);
                }

                shouldGenerate = controlType == null;

                // todo - see if there are any Forms controls here? Or always generate? Not sure...
                // Update 11/27/2020 - Justin's game has lots of components that aren't forms and this
                // is adding a lot of garbage to Justin's project.
                // Update 1/8/2021
                // Vic says - we should discuss this because this is also an important feature
                if (shouldGenerate)
                {
                    var allInstances = elementSave.Instances;

                    shouldGenerate = allInstances.Any(item =>
                    {
                        var instanceElement = ObjectFinder.Self.GetElementSave(item);

                        if (instanceElement is ComponentSave component)
                        {
                            return GueDerivingClassCodeGenerator.GetFormsControlTypeFrom(component.Behaviors) != null ||
                                GetIfShouldGenerate(instanceElement);
                        }
                        return false;
                    });
                }
            }


            return shouldGenerate;
        }

        private void GenerateScreenAndComponentCodeFor(ElementSave elementSave, ICodeBlock codeBlock)
        {
            ICodeBlock currentBlock = GenerateClassHeader(codeBlock, elementSave);

            GenerateProperties(elementSave, currentBlock);

            string runtimeClassName = GetUnqualifiedRuntimeTypeFor(elementSave);

            GenerateConstructors(elementSave, currentBlock, runtimeClassName);

            GenerateReactToVisualChanged(elementSave, currentBlock);

            currentBlock.Line("partial void CustomInitialize();");
        }

        private void GenerateConstructors(ElementSave elementSave, ICodeBlock currentBlock, string runtimeClassName)
        {
            string baseCall = null;


            if (elementSave is ComponentSave)
            {
                baseCall = "base()";
            }

            var constructor = currentBlock.Constructor("public", runtimeClassName, "", baseCall);

            // is it okay if we do this? The visual hasn't been set yet...
            constructor.Line("CustomInitialize();");


            if (elementSave is ComponentSave)
            {
                baseCall = "base(visual)";
            }
            
            constructor = currentBlock.Constructor("public", runtimeClassName, "Gum.Wireframe.GraphicalUiElement visual", baseCall);

            if(elementSave is ScreenSave)
            {
                constructor.Line("Visual = visual;");
                constructor.Line("ReactToVisualChanged();");
            }

            constructor.Line("CustomInitialize();");


        }

        private void GenerateReactToVisualChanged(ElementSave elementSave, ICodeBlock currentBlock)
        {
            string methodPre = elementSave is ScreenSave ? "private void" : "protected override void";

            var method = currentBlock.Function(methodPre, "ReactToVisualChanged");
            if(elementSave is ScreenSave || elementSave is ComponentSave)
            {
                foreach (var instance in elementSave.Instances)
                {
                    string type = GetQualifiedRuntimeTypeFor(instance, out bool isStandard);

                    if (!string.IsNullOrEmpty(type))
                    {
                        string line;

                        if(isStandard)
                        {
                            line = 
                                $"{instance.MemberNameInCode()} = ({type})Visual.GetGraphicalUiElementByName(\"{instance.Name}\").FormsControlAsObject;";
                        }
                        else
                        {
                            line =
                                $"{instance.MemberNameInCode()} = new {type}(Visual.GetGraphicalUiElementByName(\"{instance.Name}\"));";
                        }
                        method.Line(line);
                    }
                }
            }

            if(elementSave is ComponentSave)
            {
                method.Line("base.ReactToVisualChanged();");
            }
        }

        private void GenerateProperties(ElementSave elementSave, ICodeBlock currentBlock)
        {
            var rfs = GumProjectManager.Self.GetRfsForGumProject();

            var makePublic =
                true;
                //rfs?.Properties.GetValue<bool>(nameof(GumViewModel.MakeGumInstancesPublic)) == true;

            string publicOrPrivate;
            if (elementSave is Gum.DataTypes.ScreenSave || makePublic)
            {
                // make these public for screens because the only time this will be accessed is in the Glue screen that owns it
                publicOrPrivate = "public";
            }
            else
            {
                publicOrPrivate = "private";
            }

            if(elementSave is ScreenSave)
            {
                currentBlock.Line("private Gum.Wireframe.GraphicalUiElement Visual;");

                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GluxVersions.HasFormsObject)
                {
                    currentBlock.Property("public object", "BindingContext")
                        .Get().Line("return Visual.BindingContext;").End()
                        .Set().Line("Visual.BindingContext = value;");
                }
                    
            }

            foreach (var instance in elementSave.Instances)
            {
                string type = GetQualifiedRuntimeTypeFor(instance, out bool isStandardType);

                if(!string.IsNullOrEmpty(type))
                {
                    ICodeBlock property = currentBlock.AutoProperty($"{publicOrPrivate} " + type, instance.MemberNameInCode());
                }
            }
        }

        private string GetQualifiedRuntimeTypeFor(InstanceSave instance, out bool isStandardForms)
        {
            isStandardForms = false;
            var instanceType = instance.BaseType;
            var component = ObjectFinder.Self.GetComponent(instanceType);

            var behaviors = component?.Behaviors;

            string controlType = null;

            if(behaviors != null)
            {
                controlType = GueDerivingClassCodeGenerator.GetFormsControlTypeFrom(behaviors);
            }

            if(controlType != null)
            {
                isStandardForms = true;
                return controlType;
            }

            // else it may still need to be generated as a reference to a generated form type
            if(component != null && GetIfShouldGenerate(component))
            {
                return GetFullRuntimeNamespaceFor(component) + "." + GetUnqualifiedRuntimeTypeFor(component);
            }

            return null;
        }

        private ICodeBlock GenerateClassHeader(ICodeBlock codeBlock, ElementSave elementSave)
        {
            string runtimeClassName = GetUnqualifiedRuntimeTypeFor(elementSave);

            string inheritance = elementSave is ComponentSave ? "FlatRedBall.Forms.Controls.UserControl" : null;

            ICodeBlock currentBlock = codeBlock.Class("public partial", runtimeClassName, !string.IsNullOrEmpty(inheritance) ? " : " + inheritance : null);
            return currentBlock;
        }

        public string GetUnqualifiedRuntimeTypeFor(ElementSave elementSave)
        {
            return FlatRedBall.IO.FileManager.RemovePath(elementSave.Name) + "Forms";
        }

        public string GetFullRuntimeNamespaceFor(ElementSave elementSave)
        {
            string elementName = elementSave.Name;

            var subfolder = elementSave is ScreenSave ? "Screens" : "Components";

            return GetFullRuntimeNamespaceFor(elementName, subfolder);
        }

        public string GetFullRuntimeNamespaceFor(string elementName, string screensOrComponents)
        {
            string subNamespace;
            if ((elementName.Contains('/')))
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


            var fullNamespace = FormsRuntimeNamespace + "." + screensOrComponents + subNamespace;

            return fullNamespace;
        }

        public string GetQualifiedRuntimeTypeFor(ElementSave elementSave) => GetFullRuntimeNamespaceFor(elementSave) + "." + GetUnqualifiedRuntimeTypeFor(elementSave);
    }
}
