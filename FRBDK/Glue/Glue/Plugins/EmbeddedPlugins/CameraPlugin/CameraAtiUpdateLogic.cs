using FlatRedBall.Glue.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin
{
    public static class CameraAtiUpdateLogic
    {
        internal static void UpdateAtiTo(DisplaySettingsViewModel viewModel)
        {
            var ati = AvailableAssetTypes.Self.GetAssetTypeFromRuntimeType(
                "FlatRedBall.Camera", null);

            if(ati != null)
            {
                if(viewModel.GenerateDisplayCode)
                {
                    SetAtiValue(ati, "Orthogonal", viewModel.Is2D.ToString().ToLowerInvariant());
                    SetAtiValue(ati, "OrthogonalWidth", viewModel.ResolutionWidth.ToString().ToLowerInvariant());
                    SetAtiValue(ati, "OrthogonalHeight", viewModel.ResolutionHeight.ToString().ToLowerInvariant());
                }

            }
        }

        private static void SetAtiValue(AssetTypeInfo ati, string variableName, string value)
        {
            var orthogonal = ati.VariableDefinitions.FirstOrDefault(item => item.Name == variableName);
            if (orthogonal != null)
            {
                orthogonal.DefaultValue = value;
            }
        }
    }
}
