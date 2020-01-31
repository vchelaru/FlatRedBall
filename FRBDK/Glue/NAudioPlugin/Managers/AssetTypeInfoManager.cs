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
                        QualifiedType = "NAudio.Song"
                    };
                    nAudioSongAti.DestroyMethod = null; // handled by codegen

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
                        QualifiedType = "NAudio.SoundEffect"
                    };

                    nAudioSoundEffectAti.DestroyMethod = null; // handled by codegen
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

            var relativeFileName = file.Name;

            return $"{instanceName} = LoadSongFromFile({relativeFileName});";
        }

        private static string GetLoadSoundEffectCode(IElement screenOrEntity, NamedObjectSave namedObject, 
            ReferencedFileSave file, string contentManager)
        {
            var instanceName = file.GetInstanceName();

            var relativeFileName = file.Name;

            return $"{instanceName} = LoadSoundEffectFromFile({relativeFileName});";

        }
    }
}
