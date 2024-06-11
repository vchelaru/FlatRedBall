using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.SaveClasses;
using GumPlugin.CodeGeneration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPluginCore.CodeGeneration
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

                var elementName = element.GetStrippedName();

                if (hasForms)
                {
                    var formsObjectType = FormsClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(elementName, "Screens") +
                        "." + rfs.GetInstanceName() + "Forms";

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
                var elementName = element.GetStrippedName();

                
                var rfs = GumPluginCodeGenerator.GetGumScreenRfs(element);

                if (hasForms && rfs?.RuntimeType != "FlatRedBall.Gum.GumIdb" && rfs?.RuntimeType != "Gum.Wireframe.GraphicalUiElement")
                {
                    var rfsName = rfs.GetInstanceName();
                    var formsObjectType = FormsClassCodeGenerator.Self.GetFullRuntimeNamespaceFor(elementName, "Screens") +
                        "." + rfsName + "Forms";
                    var formsInstantiationLine =
                        $"Forms = {rfsName}.FormsControl ?? new {formsObjectType}({rfsName});";
                    codeBlock.Line(formsInstantiationLine);
                }

            }

            return codeBlock;
        }

    }
}
