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

                    nAudioSongAti.CustomLoadFunc = GetLoadSongCode;
                }

                return nAudioSongAti;
            }
        }

        static AssetTypeInfo nAudioSoundEffectAti;
        public static AssetTypeInfo NAudioSoundEffectAti
        {
            get
            {
                if(nAudioSoundEffectAti == null)
                {
                    nAudioSoundEffectAti = new AssetTypeInfo();
                    nAudioSoundEffectAti.MustBeAddedToContentPipeline = false;
                    nAudioSoundEffectAti.Extension = "wav";
                    nAudioSoundEffectAti.QualifiedRuntimeTypeName = new PlatformSpecificType()
                    {
                        QualifiedType = "FlatRedBall.NAudio.NAudio_Sfx"
                    };

                    nAudioSoundEffectAti.CustomLoadFunc = GetLoadSoundEffectCode;
                }
                return nAudioSoundEffectAti;
            }
        }

        internal static void AddAssetTypes()
        {
            AvailableAssetTypes.Self.AddAssetType(NAudioSongAti);
            AvailableAssetTypes.Self.AddAssetType(NAudioSoundEffectAti);
        }

        internal static void RemoveAssetTypes()
        {
            AvailableAssetTypes.Self.RemoveAssetType(NAudioSongAti);
            AvailableAssetTypes.Self.RemoveAssetType(NAudioSoundEffectAti);
        }

        private static string GetLoadSongCode(IElement screenOrEntity, NamedObjectSave namedObject, 
            ReferencedFileSave file, string contentManager)
        {
            var instanceName = file.GetInstanceName();

            var relativeFileName = file.Name.ToLower();

            return $"{instanceName} =  new {nAudioSongAti.QualifiedRuntimeTypeName.QualifiedType}(\"Content/{relativeFileName}\");";
        }

        private static string GetLoadSoundEffectCode(IElement screenOrEntity, NamedObjectSave namedObject, 
            ReferencedFileSave file, string contentManager)
        {
            var instanceName = file.GetInstanceName();

            var relativeFileName = file.Name.ToLower();

            return $"{instanceName} =  new {nAudioSoundEffectAti.QualifiedRuntimeTypeName.QualifiedType}(\"Content/{relativeFileName}\");";

        }
    }
}
