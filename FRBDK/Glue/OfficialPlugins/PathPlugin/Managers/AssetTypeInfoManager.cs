using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.PathPlugin.Views;
using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.PathPlugin.Managers
{
    static class AssetTypeInfoManager
    {
        public static AssetTypeInfo PathAssetTypeInfo { get; private set; }

        static AssetTypeInfoManager()
        {
            PathAssetTypeInfo = CreatePathAssetTypeInfo();
        }

        static AssetTypeInfo CreatePathAssetTypeInfo()
        {
            var ati = new AssetTypeInfo();
            ati.FriendlyName = "Path";
            ati.QualifiedRuntimeTypeName = new PlatformSpecificType();
            ati.QualifiedRuntimeTypeName.QualifiedType = typeof(FlatRedBall.Math.Paths.Path).FullName;
            ati.CanBeObject = true;

            var segmentDefinition = new VariableDefinition();
            segmentDefinition.Type = "List<FlatRedBall.Math.Paths.PathSegment>";
            segmentDefinition.Name = "Paths";
            segmentDefinition.UsesCustomCodeGeneration = true;
            segmentDefinition.CustomGenerationFunc = GeneratePaths;
            segmentDefinition.PreferredDisplayer = typeof(PathView);
            ati.VariableDefinitions.Add(segmentDefinition);


            return ati;
        }

        private static string GeneratePaths(IElement element, NamedObjectSave nos, ReferencedFileSave rfs)
        {
            return "// vic is working on it, okay?";
        }
    }
}
