using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
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
                // If we don't set this to true, the file will show up in the new file window.
                // As of October 9, 2024 we do not have a sample .aseprite file to use so Glue
                // attempts to create a new .achx file but save it as an .aseprite file, which causes
                // corruption.
                clone.HideFromNewFileWindow = true;
                return clone;
            }
            else
            {
                return null;
            }
        }

        internal AssetTypeInfo GetGumAnimationChainListAti()
        {
            var achxAti = AvailableAssetTypes.CommonAtis.AnimationChainList;

            var clone = FileManager.CloneObject(achxAti);

            clone.FriendlyName = "Gum Animation Chain List (.achx)";
            clone.QualifiedRuntimeTypeName = new PlatformSpecificType()
            {
                QualifiedType = "Gum.Graphics.Animation.AnimationChainList"
            };
            clone.CustomLoadFunc = (element, nos, referencedFileSave, contentManager) =>
            {
                var fileName = ReferencedFileSaveCodeGenerator.GetFileToLoadForRfs(referencedFileSave, referencedFileSave.GetAssetTypeInfo());

                // use the exe location because this internally can get confused by not using the relative directory properly. By making this use 
                // the exe location, all ambiguity is removed
                return $"{referencedFileSave.GetInstanceName()} = global::Gum.Content.AnimationChain.AnimationChainListSave.FromFile(global::ToolsUtilities.FileManager.ExeLocation + \"{fileName}\").ToAnimationChainList(\"{contentManager}\");";
            };

            return clone;
        }
    }
}
