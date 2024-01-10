using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.LayerPlugin
{
    [Export(typeof(PluginBase))]
    public class MainLayerPlugin : PluginBase
    {
        public override void StartUp()
        {
            var ati = AvailableAssetTypes.CommonAtis.Layer;

            var newVariableDefinition = new VariableDefinition();
            newVariableDefinition.Name = nameof(FlatRedBall.Graphics.Layer.RenderTarget);
            newVariableDefinition.Type = "string";
            // category?
            newVariableDefinition.CustomGenerationFunc = GenerateRenderTargetCode;
            newVariableDefinition.CustomGetForcedOptionFunc = GetAvailableRenderTargets;

            ati.VariableDefinitions.Add(newVariableDefinition);
        }

        private string GenerateRenderTargetCode(IElement element, NamedObjectSave nos, ReferencedFileSave save2, string arg4)
        {
            var value = 
                nos.GetCustomVariable(nameof(FlatRedBall.Graphics.Layer.RenderTarget))?.Value as string;

            if(!string.IsNullOrEmpty(value))
            {
                return $"{nos.InstanceName}.RenderTarget = {value};";
            }
            else
            {
                return null;
            }
        }

        private List<string> GetAvailableRenderTargets(IElement element, NamedObjectSave save1, ReferencedFileSave save2)
        {
            var availableRenderTargets = (element as GlueElement).GetAllNamedObjectsRecurisvely()
                .Where(item => item.GetAssetTypeInfo()?.QualifiedRuntimeTypeName.QualifiedType == "Microsoft.Xna.Framework.Graphics.RenderTarget2D")
                .Select(item => item.InstanceName)
                .ToList();

            return availableRenderTargets;
        }
    }
}
