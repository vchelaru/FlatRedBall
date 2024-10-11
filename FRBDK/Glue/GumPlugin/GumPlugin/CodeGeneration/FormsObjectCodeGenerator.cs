using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using GumPlugin.CodeGeneration;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Automation;

namespace GumPlugin.CodeGeneration
{
    class FormsObjectCodeGenerator : ElementComponentCodeGenerator
    {

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, IElement element)
        {
            bool isGlueScreen, hasGumScreen, hasForms;
            bool needsGumIdb = GumPluginCodeGenerator.NeedsGumIdb(element, out isGlueScreen, out hasGumScreen, out hasForms);

            if (isGlueScreen && hasGumScreen)
            {
                var rfs = GumPluginCodeGenerator.GetGumScreenRfs(element);

                // Now that FRB screens can exist in folders, we shouldn't strip the name:
                //var elementName = element.GetStrippedName();

                var elementName = element.Name.Substring("Screens/".Length);


                if (hasForms)
                {
                    var formsObjectType = GetFullyQualifiedFormsObjectFor(rfs);
                    codeBlock.Line($"{formsObjectType} Forms;");
                }

                var gumScreenName = GumPluginCodeGenerator.GumScreenObjectNameFor(element);
                var shouldGenerateGum = element.AllNamedObjects.Any(item => item.InstanceName == gumScreenName) == false;
                if(shouldGenerateGum)
                {
                    codeBlock.Line($"global::{rfs.RuntimeType} {gumScreenName};");
                }

            }

            return codeBlock;
        }

        public override ICodeBlock GenerateInitialize(ICodeBlock codeBlock, IElement element)
        {
            var gumScreenRfs = element.ReferencedFiles.FirstOrDefault(item => item.Name.EndsWith(".gusx"));


            bool needsGumIdb = GumPluginCodeGenerator.NeedsGumIdb(element, out bool isGlueScreen, out bool hasGumScreen, out bool hasForms);

            if (isGlueScreen && hasGumScreen)
            {
                //var elementName = element.GetStrippedName();
                var glueElementName = element.Name.Substring("Screens/".Length);

                var rfs = GumPluginCodeGenerator.GetGumScreenRfs(element);

                if (hasForms && rfs?.RuntimeType != "FlatRedBall.Gum.GumIdb" && rfs?.RuntimeType != "Gum.Wireframe.GraphicalUiElement")
                {
                    var formsObjectType = GetFullyQualifiedFormsObjectFor(rfs);
                    if(!string.IsNullOrEmpty( formsObjectType ))
                    {
                        var rfsName = rfs.GetInstanceName();
                        var formsInstantiationLine = $"Forms = {rfsName}.FormsControl ?? new {formsObjectType}({rfsName});";
                        codeBlock.Line(formsInstantiationLine);
                    }
                }
            }

            return codeBlock;
        }

        private string GetFullyQualifiedFormsObjectFor(ReferencedFileSave rfs)
        {
            var gumElement = GumPluginCommands.Self.GetGumElement(rfs);
            if(gumElement != null)
            {
                var namespaceName = FormsClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(gumElement, prefixGlobal: true);
                var rfsName = rfs.GetInstanceName();
                // This shouldn't use the Glue element because FRB screens can exist in folders that differ from
                // their Gum screen:
                //var formsObjectType = FormsClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(glueElementName, "") +
                var formsObjectType = namespaceName + "." + rfsName + "Forms";
                return formsObjectType;
            }
            return string.Empty;
        }
    }
}
