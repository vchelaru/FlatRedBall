using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Text;

namespace NAudioPlugin.Managers
{
    public class AssetTypeInfoManager
    {
        // I want to support this, but doing so causes
        // conflicts between some Vorbis objects that are
        // a part of MonoGame with the same namespace. I could
        // fix this by setting properties on the project to alias
        // the class but that's something that would have to be done
        // in the game by the user, or by a new feature in Glue. That's
        // a lot of work so let's just support MP3 for now.
        //static AssetTypeInfo nAudioOggSongAti;
        //public static AssetTypeInfo NAudioOggSongAti
        //{
        //    get
        //    {
        //        if (nAudioOggSongAti == null)
        //        {
        //            nAudioOggSongAti = CreateSongAti("ogg");
        //        }
        //        return nAudioOggSongAti;
        //    }
        //}

        static AssetTypeInfo nAudioMp3SongAti;
        public static AssetTypeInfo NAudioMp3SongAti
        {
            get
            {
                if (nAudioMp3SongAti == null)
                {
                    nAudioMp3SongAti = CreateSongAti("mp3");
                }
                return nAudioMp3SongAti;
            }
        }

        private static AssetTypeInfo CreateSongAti(string extension)
        {
            var ati = new AssetTypeInfo();

            ati.MustBeAddedToContentPipeline = false;
            ati.Extension = extension;
            ati.QualifiedRuntimeTypeName = new PlatformSpecificType()
            {
                QualifiedType = "FlatRedBall.NAudio.NAudio_Song"
            };

            ati.FriendlyName = $"NAudio Song (.{extension})";

            ati.DestroyMethod = null; // handled by codegen

            ati.CustomLoadFunc = GetLoadSongCode;
            return ati;
        }

        internal static void AddAssetTypes()
        {
            //AvailableAssetTypes.Self.AddAssetType(NAudioOggSongAti);
            AvailableAssetTypes.Self.AddAssetType(NAudioMp3SongAti);
        }

        internal static void RemoveAssetTypes()
        {
            //AvailableAssetTypes.Self.RemoveAssetType(NAudioOggSongAti);
            AvailableAssetTypes.Self.RemoveAssetType(NAudioMp3SongAti);
        }

        private static string GetLoadSongCode(IElement screenOrEntity, NamedObjectSave namedObject, 
            ReferencedFileSave file, string contentManager)
        {
            var instanceName = file.GetInstanceName();

            var relativeFileName = file.Name.ToLower();

            return $"{instanceName} =  new {nAudioMp3SongAti.QualifiedRuntimeTypeName.QualifiedType}(\"Content/{relativeFileName}\");";
        }
    }
}
