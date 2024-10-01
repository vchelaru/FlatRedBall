using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;

namespace PluginTestbed.MoveToLayerPlugin
{
    class MoveToLayerComponentGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateAdditionalMethods(ICodeBlock codeBlock, GlueElement element)
        {
            //////////////////////////EARLY OUT//////////////////////////////////////
            if (element is ScreenSave)
            {
                return codeBlock;
            }
            ///////////////////////END EARLY OUT/////////////////////////////////////

            bool isInDerived = element.InheritsFromElement();

            if (isInDerived)
            {
                codeBlock = codeBlock.Function("public override void", "MoveToLayer", "FlatRedBall.Graphics.Layer layerToMoveTo");
                codeBlock.Line("var layerToRemoveFrom = LayerProvidedByContainer; // assign before calling base so removal is not impacted by base call");
                codeBlock.Line("base.MoveToLayer(layerToMoveTo);");
            }
            else
            {
                codeBlock = codeBlock.Function("public virtual void", "MoveToLayer", "FlatRedBall.Graphics.Layer layerToMoveTo");
                codeBlock.Line("var layerToRemoveFrom = LayerProvidedByContainer;");

            }


            if (element.InheritsFromFrbType())
            {
                AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(element.BaseElement, element);

                if (ati != null )
                {
                    if (ati.RemoveFromLayerMethod != null)
                    {
                        codeBlock.If("layerToRemoveFrom != null")
                            .Line(ati.RemoveFromLayerMethod.Replace("mLayer", "layerToRemoveFrom") + ";")
                        .End();
                    }

                    if (ati.LayeredAddToManagersMethod.Count != 0)
                    {
                        codeBlock.Line(ati.LayeredAddToManagersMethod[0].Replace("mLayer", "layerToMoveTo") + ";");
                    }

                }

            }


            foreach (NamedObjectSave nos in element.NamedObjects)
            {
                GenerateMoveToLayerForNos(codeBlock, nos);
            }

            if (isInDerived == false)
            {
                // Doesn't hurt if derived assigns this but...I guess we can keep it clean and only do it in one place
                codeBlock.Line("LayerProvidedByContainer = layerToMoveTo;");
            }

            codeBlock = codeBlock.End();

            return codeBlock;
        }

        private void GenerateMoveToLayerForNos(ICodeBlock codeBlock, NamedObjectSave nos)
        {
            if (!nos.IsDisabled && !nos.IsContainer && !nos.DefinedByBase)
            {
                bool shouldCheckForNull = nos.Instantiate == false;

                var nosAti = nos.GetAssetTypeInfo();

                if (nosAti != null && !string.IsNullOrEmpty(nos.GetAssetTypeInfo().RemoveFromLayerMethod))
                {
                    NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(codeBlock, nos);
                    bool shouldSkip = GetShouldSkip(nos);

                    if (!shouldSkip)
                    {

                        if (shouldCheckForNull)
                        {
                            codeBlock = codeBlock.If(nos.InstanceName + " != null");
                        }
                        codeBlock.If("layerToRemoveFrom != null")
                            .Line(nos.GetAssetTypeInfo().RemoveFromLayerMethod.Replace("this", nos.InstanceName).Replace("mLayer", "layerToRemoveFrom") + ";")
                        .End();

                        var codeBlockForAddCall = codeBlock;

                        // hardcoed special case
                        if (nosAti.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Sprite")
                        {
                            codeBlockForAddCall = codeBlock.If($"layerToMoveTo != null || !SpriteManager.AutomaticallyUpdatedSprites.Contains({nos.InstanceName})");
                        }
                        else if (nosAti.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Graphics.Text")
                        {
                            codeBlockForAddCall = codeBlock.If($"layerToMoveTo != null || !TextManager.AutomaticallyUpdatedTexts.Contains({nos.InstanceName})");
                        }

                        codeBlockForAddCall.Line(nosAti.LayeredAddToManagersMethod[0].Replace("this", nos.InstanceName).Replace("mLayer", "layerToMoveTo") + ";");

                        if (shouldCheckForNull)
                        {
                            codeBlock = codeBlock.End();
                        }
                    }
                    NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(codeBlock, nos);
                }
                else if (nos.SourceType == SourceType.Entity && !string.IsNullOrEmpty(nos.SourceClassType))
                {

                    if (shouldCheckForNull)
                    {
                        codeBlock = codeBlock.If(nos.InstanceName + " != null");
                    }
                    NamedObjectSaveCodeGenerator.AddIfConditionalSymbolIfNecesssary(codeBlock, nos);
                    codeBlock.Line(nos.InstanceName + ".MoveToLayer(layerToMoveTo);");
                    NamedObjectSaveCodeGenerator.AddEndIfIfNecessary(codeBlock, nos);


                    if (shouldCheckForNull)
                    {
                        codeBlock = codeBlock.End();
                    }
                }

            }
        }

        private bool GetShouldSkip(NamedObjectSave nos)
        {
            if (nos.SourceType == SourceType.File)
            {
                if (nos.SourceFile == null)
                {
                    return true;
                }
                if (!nos.SourceName.StartsWith("Entire File (") &&
                    NamedObjectSaveCodeGenerator.ReusableEntireFileRfses.ContainsKey(nos.SourceFile))
                {
                    return true;
                }
            }

            return false;
        }


    }
}
