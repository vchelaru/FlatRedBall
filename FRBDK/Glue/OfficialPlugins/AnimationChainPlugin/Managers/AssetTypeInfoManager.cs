using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.AnimationChainPlugin.Managers
{
    internal class AssetTypeInfoManager : Singleton<AssetTypeInfoManager>
    {
        /// <summary>
        /// Returns an AssetTypeInfo for the Aseprite 
        /// file extension if the project is using .NET 6 or greater.
        /// </summary>
        /// <returns>The AssetTypeInfo if created, otherwise null.</returns>
        internal AssetTypeInfo TryGetAsepriteAti()
        {
            var project = GlueState.Self.CurrentMainProject;
            var netVersion = project.DotNetVersion;
            if (netVersion.Major >= 6)
            {
                var achxAti = AvailableAssetTypes.Self.GetAssetTypeFromExtension("achx");

                var clone = FileManager.CloneObject(achxAti);

                clone.FriendlyName = "Aseprite Animation Chain";
                clone.Extension = "aseprite";
                return clone;
            }
            else
            {
                return null;
            }


        }
    }
}
