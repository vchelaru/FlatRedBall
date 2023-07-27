using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using OfficialPlugins.PathPlugin.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text;

namespace OfficialPlugins.PathPlugin
{
    [Export(typeof(PluginBase))]
    public class MainPathPlugin : PluginBase
    {
        public override string FriendlyName => "Path Plugin";

        public override void StartUp()
        {
            AddAssetTypeInfo(AssetTypeInfoManager.PathAssetTypeInfo);

            ReactToVariableAdded += HandleVariableAdded;

            ReactToSelectedSubIndexChanged += HandleSelectedSubIndex;
        }

        private void HandleSelectedSubIndex(int? nullable)
        {
            if (nullable.HasValue)
            {
                AssetTypeInfoManager.HighlightIndex(nullable.Value);
            }
        }

        private void HandleVariableAdded(CustomVariable newVariable)
        {

            if(!string.IsNullOrEmpty(newVariable.SourceObject) && newVariable.SourceObjectProperty == "Path")
            {
                var element = ObjectFinder.Self.GetElementContaining(newVariable);

                if(element != null)
                {
                    var nosOwner = element.GetNamedObjectRecursively(newVariable.SourceObject);

                    if(nosOwner?.GetAssetTypeInfo() == AssetTypeInfoManager.PathAssetTypeInfo)
                    {
                        nosOwner.HasPublicProperty = true;
                    }
                }
            }
        }
    }
}
