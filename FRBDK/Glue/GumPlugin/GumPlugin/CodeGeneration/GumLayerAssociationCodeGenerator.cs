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
using System.Threading.Tasks;

namespace GumPlugin.CodeGeneration
{
    class GumLayerAssociationCodeGenerator : ElementComponentCodeGenerator
    {
        public const string UnderEverythingLayerPrefix = "UnderEverythingLayer";
        public const string AboveEverythingLayerPrefix = "AboveEverythingLayer";

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
                // This needs to be before so that all layers are created and associated before
                // any components try to access them
                return FlatRedBall.Glue.Plugins.Interfaces.CodeLocation.BeforeStandardGenerated;
            }
        }

        string[] GetUsedFrbLayerNames(IElement element)
        {
            var list = element.AllNamedObjects.Where(item => item.IsLayer &&
                NamedObjectSaveCodeGenerator.GetFieldCodeGenerationType(item) == CodeGenerationType.Full)
                .Select(item => item.InstanceName)
                .ToList();

            bool anyOnUnderAllLayer = element.NamedObjects
                .Any(item => item.LayerOn == AvailableLayersTypeConverter.UnderEverythingLayerName);

            if(anyOnUnderAllLayer)
            {
                list.Add(UnderEverythingLayerPrefix);
            }

            bool anyOnAboveAllLayer = element.NamedObjects
                    .Any(item => item.LayerOn == AvailableLayersTypeConverter.TopLayerName);

            if(anyOnAboveAllLayer)
            {
                list.Add(AboveEverythingLayerPrefix);
            }


            return list.ToArray();
        }


        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, GlueElement element)
        {
            if (ShouldGenerate)
            {
                string idbName = GetIdbName(element);

                if (idbName != null)
                {
                    var frbLayerNames = GetUsedFrbLayerNames(element);
                    // Creates Gum layers for every FRB layer, so that objects can be moved between layers at runtime, and so code gen
                    // can use these for objects that are placed on layers in Glue.
                    foreach (var layerPrefix in frbLayerNames)
                    {
                        var gumLayerName = layerPrefix + "Gum";
                        codeBlock.Line($"{gumLayerName} = RenderingLibrary.SystemManagers.Default.Renderer.AddLayer();");
                        codeBlock.Line($"{gumLayerName}.Name = \"{gumLayerName}\";");

                        string frbLayerName = layerPrefix;

                        if (frbLayerName == UnderEverythingLayerPrefix)
                        {
                            frbLayerName = "global::FlatRedBall.SpriteManager.UnderAllDrawnLayer";
                        }
                        else if (frbLayerName == AboveEverythingLayerPrefix)
                        {
                            frbLayerName = "global::FlatRedBall.SpriteManager.TopLayer";
                        }

                        codeBlock.Line($"{idbName}.AddGumLayerToFrbLayer({gumLayerName}, {frbLayerName});");
                    }
                }

            }
            return base.GenerateAddToManagers(codeBlock, element);
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, GlueElement element)
        {
            if (ShouldGenerate)
            {
                string idbName = GetIdbName(element);

                if (idbName != null)
                {
                    var frbLayerNames = GetUsedFrbLayerNames(element);

                    foreach (var layerPrefix in frbLayerNames)
                    {
                        var gumLayerName = layerPrefix + "Gum";

                        codeBlock.Line($"RenderingLibrary.SystemManagers.Default.Renderer.RemoveLayer({gumLayerName});");
                    }
                }
            }

            return codeBlock;
        }

        private string GetIdbName(IElement element)
        {
            var rfs = GetScreenRfsIn(element);
            var idbName = rfs?.GetInstanceName();
            var rfsAssetTpe = rfs?.GetAssetTypeInfo();
            var isIdb = rfsAssetTpe == AssetTypeInfoManager.Self.ScreenIdbAti;

            // As of June 6, 2023 we always use the self IDB:
            //if (string.IsNullOrEmpty(idbName) && element is FlatRedBall.Glue.SaveClasses.ScreenSave)
            //{
            //    idbName = "gumIdb";
            //}
            //else if (rfs != null && isIdb == false)
            {
                idbName = "FlatRedBall.Gum.GumIdb.Self";
            }

            return idbName;
        }

        private ReferencedFileSave GetScreenRfsIn(IElement element)
        {
            return element.ReferencedFiles.FirstOrDefault(item =>
                FileManager.GetExtension(item.Name) == GumProjectSave.ScreenExtension);
        }
    }
}
