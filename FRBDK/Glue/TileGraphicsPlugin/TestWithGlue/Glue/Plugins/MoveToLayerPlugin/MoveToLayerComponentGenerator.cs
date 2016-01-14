using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.SaveClasses;

namespace PluginTestbed.MoveToLayerPlugin
{
    class MoveToLayerComponentGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, FlatRedBall.Glue.SaveClasses.IElement element)
        {
            //////////////////////////EARLY OUT//////////////////////////////////////
            if (element is ScreenSave)
            {
                return codeBlock;
            }
            ///////////////////////END EARLY OUT/////////////////////////////////////

            codeBlock = codeBlock.Function("public void", "MoveToLayer", "Layer layerToMoveTo");

            foreach (NamedObjectSave nos in element.NamedObjects)
            {
                if (!nos.IsDisabled)
                {
                    if (nos.GetAssetTypeInfo() != null && !string.IsNullOrEmpty(nos.GetAssetTypeInfo().RemoveFromLayerMethod))
                    {
                        NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(codeBlock, nos);
                        bool shouldSkip = GetShouldSkip(nos);

                        if (!shouldSkip)
                        {
                            codeBlock.If("LayerProvidedByContainer != null")
                                .Line(nos.GetAssetTypeInfo().RemoveFromLayerMethod.Replace("this", nos.InstanceName).Replace("mLayer", "LayerProvidedByContainer") + ";")
                            .End();

                            codeBlock.Line(nos.GetAssetTypeInfo().LayeredAddToManagersMethod[0].Replace("this", nos.InstanceName).Replace("mLayer", "layerToMoveTo") + ";");
                        }
                        NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(codeBlock, nos);
                    }
                    else if (nos.SourceType == SourceType.Entity && !string.IsNullOrEmpty(nos.SourceClassType))
                    {
                        NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(codeBlock, nos);
                        codeBlock.Line(nos.InstanceName + ".MoveToLayer(layerToMoveTo);");
                        NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(codeBlock, nos);
                    }
                }
            }

            codeBlock.Line("LayerProvidedByContainer = layerToMoveTo;");

            codeBlock = codeBlock.End();

            return codeBlock;
        }

        private bool GetShouldSkip(NamedObjectSave nos)
        {
            if (nos.SourceType == SourceType.File)
            {
                if (nos.SourceFile == null)
                {
                    return true;
                }
                if (!nos.SourceName.StartsWith("Entire File ("))
                {
                    // this could be handled by another object
                    foreach (string[] stringPair in NamedObjectSaveCodeGenerator.ReusableEntireFileRfses)
                    {
                        if (stringPair[0] == nos.SourceFile)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }


    }
}
