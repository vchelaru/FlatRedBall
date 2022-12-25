using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace NAudioPlugin.Managers
{
    public class AssetTypeInfoManager
    {
        static AssetTypeInfo nAudioSongAti;
        public static AssetTypeInfo NAudioSongAti
        {
            get
            {
                if (nAudioSongAti == null)
                {
                    nAudioSongAti = new AssetTypeInfo();

                    nAudioSongAti.MustBeAddedToContentPipeline = false;
                    nAudioSongAti.Extension = "ogg";
                    nAudioSongAti.QualifiedRuntimeTypeName = new PlatformSpecificType()
                    {
                        QualifiedType = "FlatRedBall.NAudio.NAudio_Song"
                    };

                    nAudioSongAti.FriendlyName = "NAudio Song (.ogg)";

                    nAudioSongAti.DestroyMethod = null; // handled by codegen

                    nAudioSongAti.CustomLoadFunc = GetLoadSongCode;
                }

                return nAudioSongAti;
            }
        }

        internal static void AddAssetTypes()
        {
            AvailableAssetTypes.Self.AddAssetType(NAudioSongAti);
        }

        internal static void RemoveAssetTypes()
        {
            AvailableAssetTypes.Self.RemoveAssetType(NAudioSongAti);
        }

        private static string GetLoadSongCode(IElement screenOrEntity, NamedObjectSave namedObject, 
            ReferencedFileSave file, string contentManager)
        {
            var instanceName = file.GetInstanceName();

            var relativeFileName = file.Name.ToLower();

            return $"{instanceName} =  new {nAudioSongAti.QualifiedRuntimeTypeName.QualifiedType}(\"Content/{relativeFileName}\");";
        }
    }
}
