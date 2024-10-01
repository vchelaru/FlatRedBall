using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Gum.DataTypes;
using GumPlugin.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GumPlugin.CodeGeneration
{
    class GumLayerCodeGenerator : ElementComponentCodeGenerator
    {
        const string TopLayerGumName = GumLayerAssociationCodeGenerator.AboveEverythingLayerPrefix + "Gum";
        const string UnderEverythingLayerGumName = GumLayerAssociationCodeGenerator.UnderEverythingLayerPrefix + "Gum";



        bool ShouldGenerate
        {
            get
            {
                return AppState.Self.GumProjectSave != null;
            }
        }
        public override FlatRedBall.Glue.Plugins.Interfaces.CodeLocation CodeLocation
        {
            get
            {
                return FlatRedBall.Glue.Plugins.Interfaces.CodeLocation.AfterStandardGenerated;
            }
        }

        public static IEnumerable<NamedObjectSave> GetObjectsForGumLayers(GlueElement element)
        {
            return element.AllNamedObjects.Where(item => item.IsLayer &&
                NamedObjectSaveCodeGenerator.GetFieldCodeGenerationType(item) == CodeGenerationType.Full);
        }

        public override ICodeBlock GenerateFields(ICodeBlock codeBlock, GlueElement element)
        {
            List<string> gumLayerNames = GetGumLayerNames(element);

            foreach (var layerName in gumLayerNames)
            {
                codeBlock.Line($"protected global::RenderingLibrary.Graphics.Layer {layerName};");

            }

            return codeBlock;
        }

        private List<string> GetGumLayerNames(GlueElement element)
        {
            List<string> gumLayerNames = new List<string>();

            if (ShouldGenerate)
            {
                foreach (var layer in GetObjectsForGumLayers(element))
                {
                    gumLayerNames.Add(layer.InstanceName + "Gum");
                }

                // We also need to generate a gum layer for the under-all layer if there is one:
                bool anyOnUnderAllLayer = element.NamedObjects
                    .Any(item => item.LayerOn == AvailableLayersTypeConverter.UnderEverythingLayerName);

                if (anyOnUnderAllLayer)
                {
                    gumLayerNames.Add(UnderEverythingLayerGumName);
                }

                bool anyOnAboveAllLayer = element.NamedObjects
                    .Any(item => item.LayerOn == AvailableLayersTypeConverter.TopLayerName);

                if (anyOnAboveAllLayer)
                {
                    gumLayerNames.Add(TopLayerGumName);
                }
            }

            return gumLayerNames;
        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, GlueElement element)
        {
            return base.GenerateAddToManagers(codeBlock, element);
        }

        public override void GenerateAddToManagersBottomUp(ICodeBlock codeBlock, GlueElement element)
        {
            GenerateMoveObjectsToGumLayers(codeBlock, element);
            base.GenerateAddToManagersBottomUp(codeBlock, element);
        }

        private void GenerateMoveObjectsToGumLayers(ICodeBlock codeBlock, GlueElement element)
        {
            if (ShouldGenerate)
            {
                bool wasAnythingMovedToALayer = false;
                // todo:  Need to register the layer here
                foreach (var item in element.AllNamedObjects.Where(item =>
                    GumPluginCodeGenerator.IsGue(item) &&
                    !string.IsNullOrEmpty(item.LayerOn) &&
                    NamedObjectSaveCodeGenerator.GetFieldCodeGenerationType(item) == CodeGenerationType.Full))
                {
                    string frbLayerName = item.LayerOn;
                    string gumLayerName = $"{item.LayerOn}Gum";

                    if (item.LayerOn == AvailableLayersTypeConverter.UnderEverythingLayerName)
                    {
                        frbLayerName = AvailableLayersTypeConverter.UnderEverythingLayerCode;
                        gumLayerName = UnderEverythingLayerGumName;
                    }

                    if (item.LayerOn == AvailableLayersTypeConverter.TopLayerName)
                    {
                        frbLayerName = AvailableLayersTypeConverter.TopLayerName;
                        gumLayerName = TopLayerGumName;
                    }

                    codeBlock.Line($"{item.FieldName}.MoveToFrbLayer({frbLayerName}, {gumLayerName});");
                    wasAnythingMovedToALayer = true;
                }

                if (wasAnythingMovedToALayer && element is FlatRedBall.Glue.SaveClasses.ScreenSave)
                {
                    codeBlock.Line("FlatRedBall.Gui.GuiManager.SortZAndLayerBased();");
                }
            }
        }
    }
}
