using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;
using ToolsUtilities;

namespace GameCommunicationPlugin.GlueControl.CodeGeneration
{
    class CompilerPluginElementCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, GlueElement element)
        {
            if(MainCompilerPlugin.MainViewModel.IsGenerateGlueControlManagerInGame1Checked && element is EntitySave &&
                // If it has a base element, then the base will handle this...
                string.IsNullOrEmpty(element.BaseElement))
            {
                codeBlock.Line($"public string EditModeType {{ get; set; }} = \"{CodeWriter.GetGlueElementNamespace(element as GlueElement)}.{FileManager.RemovePath(element.Name)}\";");
            }
            return codeBlock;
        }
    }
}
