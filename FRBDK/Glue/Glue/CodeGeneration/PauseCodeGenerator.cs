using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.CodeGeneration
{
    internal class PauseCodeGenerator : ElementComponentCodeGenerator
    {


        public override CodeBuilder.ICodeBlock GenerateAdditionalMethods(CodeBuilder.ICodeBlock codeBlock, SaveClasses.GlueElement element)
        {
            if(element is EntitySave)
            {
                bool hasBase = element.InheritsFromElement();
                if (hasBase == false)
                {
                    codeBlock.Line("protected bool mIsPaused;");

                    var function = codeBlock.Function("public override void", "Pause", "FlatRedBall.Instructions.InstructionList instructions");

                    function.Line("base.Pause(instructions);");
                    function.Line("mIsPaused = true;");

                    foreach (var nos in element.AllNamedObjects)
                    {
                        GeneratePauseForNos(nos, function);
                    }
                }
                codeBlock = GenerateSetToIgnorePausing(codeBlock, element, hasBase);
            }

            return codeBlock;
        }

        private void GeneratePauseForNos(NamedObjectSave nos, ICodeBlock codeBlock)
        {
            // eventually I may want to move this to AssetTypeInfo....
            if (nos.InstanceType == "SoundEffectInstance")
            {
                var instanceName = nos.InstanceName;
                var ifBlock = codeBlock.If(instanceName + ".State == Microsoft.Xna.Framework.Audio.SoundState.Playing");
                {
                    ifBlock.Line(instanceName + ".Pause();");
                    ifBlock.Line("instructions.Add(new FlatRedBall.Instructions.DelegateInstruction(() => " + instanceName + ".Resume()));");
                }
                ifBlock.End();
            }
        }

        private static CodeBuilder.ICodeBlock GenerateSetToIgnorePausing(CodeBuilder.ICodeBlock codeBlock, SaveClasses.GlueElement element, bool hasBase)
        {

            string virtualOrOverride = "virtual";
            if (hasBase)
            {
                virtualOrOverride = "override";
            }

            codeBlock = codeBlock.Function("public " + virtualOrOverride + " void", "SetToIgnorePausing", "");

            if (hasBase)
            {
                codeBlock.Line("base.SetToIgnorePausing();");
            }
            else
            {
                codeBlock.Line("FlatRedBall.Instructions.InstructionManager.IgnorePausingFor(this);");

            }

            foreach (NamedObjectSave nos in element.AllNamedObjects)
            {
                if (nos.IsFullyDefined && !nos.IsDisabled && !nos.IsContainer)
                {
                    NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(codeBlock, nos);

                    bool shouldWrapInNullCheck = nos.SetByDerived || nos.SetByContainer || nos.Instantiate == false;

                    if (shouldWrapInNullCheck)
                    {
                        codeBlock = codeBlock.If(nos.InstanceName + " != null");
                    }


                    var ati = nos.GetAssetTypeInfo();

                    var canIgnorePausing = ati != null && ati.CanIgnorePausing &&
                        (ati.IsPositionedObject || nos.IsList || ati == AvailableAssetTypes.CommonAtis.ShapeCollection);

                    if (canIgnorePausing)
                    {
                        codeBlock.Line("FlatRedBall.Instructions.InstructionManager.IgnorePausingFor(" + nos.InstanceName + ");");
                    }
                    else if (nos.SourceType == SourceType.Entity)
                    {
                        codeBlock.Line(nos.InstanceName + ".SetToIgnorePausing();");
                    }

                    if (shouldWrapInNullCheck)
                    {
                        codeBlock = codeBlock.End();
                    }

                    NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(codeBlock, nos);
                }
            }

            codeBlock = codeBlock.End();
            return codeBlock;
        }

        public static CodeBuilder.ICodeBlock AddToPauseIgnoreIfNecessary(CodeBuilder.ICodeBlock codeBlock, GlueElement element, NamedObjectSave nos)
        {
            if (nos.IgnoresPausing)
            {
                if (nos.SourceType == SourceType.Entity && !string.IsNullOrEmpty(nos.SourceClassType))
                {
                    // it's an Entity
                    codeBlock.Line(nos.InstanceName + ".SetToIgnorePausing();");
                }
                else if (nos.GetAssetTypeInfo() != null && nos.GetAssetTypeInfo().CanIgnorePausing)
                {
                    codeBlock.Line("FlatRedBall.Instructions.InstructionManager.IgnorePausingFor(" + nos.InstanceName + ");");
                }
            }

            return codeBlock;
        }

    }
}
