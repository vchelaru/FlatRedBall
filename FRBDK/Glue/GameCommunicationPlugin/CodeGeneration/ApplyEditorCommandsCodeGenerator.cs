using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameCommunicationPlugin.CodeGeneration
{
    internal class ApplyEditorCommandsCodeGenerator : ElementComponentCodeGenerator
    {
        public override CodeLocation CodeLocation => CodeLocation.AfterStandardGenerated;

        public bool IsGameCommunicationEnabled { get; set; }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, GlueElement element)
        {
            ////////////////////////Early Out////////////////////////
            if (!IsGameCommunicationEnabled) return codeBlock;
            //////////////////////End Early Out//////////////////////

            // only do this if it has no base:
            var isBase = string.IsNullOrEmpty(element.BaseElement);
            if(isBase)
            {
                if(element is EntitySave)
                {
                    codeBlock.Line($"GlueControl.InstanceLogic.Self.ApplyEditorCommandsToNewEntity(this, GlueControl.CommandReceiver.GameElementTypeToGlueElement(this.GetType().FullName));");
                }
                else // screen save
                {
                    codeBlock.Line($"GlueControl.InstanceLogic.Self.ApplyEditorCommandsToNewEntity(null, GlueControl.CommandReceiver.GameElementTypeToGlueElement(this.GetType().FullName));");
                }
            }
            return codeBlock;
        }
    }
}
