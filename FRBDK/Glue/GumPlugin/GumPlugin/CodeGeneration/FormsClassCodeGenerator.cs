using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using Gum.DataTypes;
using Gum.Managers;
using GumPlugin.CodeGeneration;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Text;

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

            bool generated = false;

            var isScreen = elementSave is ScreenSave;
            var isComponent = elementSave is ComponentSave;
            if (isScreen)
            {
                generated = true;
            }
            else if(isComponent)
            {
                var component = elementSave as ComponentSave;

                var behaviors = component?.Behaviors;

                string controlType = null;

                if (behaviors != null)
                {
                    controlType = GueDerivingClassCodeGenerator.GetFormsControlTypeFrom(behaviors);
                }

                // todo - see if there are any Forms controls here? Or always generate? Not sure...
                generated = controlType == null;
            }
            // don't do anything with standards

            if (generated)
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

        private void GenerateScreenAndComponentCodeFor(ElementSave elementSave, ICodeBlock codeBlock)
        {
            ICodeBlock currentBlock = GenerateClassHeader(codeBlock, elementSave);

            GenerateProperties(elementSave, currentBlock);

            string runtimeClassName = GetUnqualifiedRuntimeTypeFor(elementSave);

            GenerateConstructor(elementSave, currentBlock, runtimeClassName);

            currentBlock.Line("partial void CustomInitialize();");
        }

        private void GenerateConstructor(ElementSave elementSave, ICodeBlock currentBlock, string runtimeClassName)
        {
            string baseCall = null;
            var constructor = currentBlock.Constructor("public", runtimeClassName, "Gum.Wireframe.GraphicalUiElement visual", baseCall);

            if(elementSave is ScreenSave || elementSave is ComponentSave)
            {
                foreach (var instance in elementSave.Instances)
                {
                    string type = GetQualifiedRuntimeTypeFor(instance, elementSave);

                    if (!string.IsNullOrEmpty(type))
                    {
                        var line = 
                            $"{instance.MemberNameInCode()} = ({type})visual.GetGraphicalUiElementByName(\"{instance.Name}\").FormsControlAsObject;";
                        constructor.Line(line);
                    }
                }
            }

            constructor.Line("CustomInitialize();");
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
            foreach (var instance in elementSave.Instances)
            {
                string type = GetQualifiedRuntimeTypeFor(instance, elementSave);

                if(!string.IsNullOrEmpty(type))
                {
                    ICodeBlock property = currentBlock.AutoProperty($"{publicOrPrivate} " + type, instance.MemberNameInCode());
                }
            }
        }

        private string GetQualifiedRuntimeTypeFor(InstanceSave instance, ElementSave elementSave)
        {
            var instanceType = instance.BaseType;
            var component = ObjectFinder.Self.GetComponent(instanceType);

            var behaviors = component?.Behaviors;

            string controlType = null;

            if(behaviors != null)
            {
                controlType = GueDerivingClassCodeGenerator.GetFormsControlTypeFrom(behaviors);
            }

            return controlType;
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
    }
}
