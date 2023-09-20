using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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

        static AssetTypeInfo GetNAudioMp3SongAti()
        {
            return CreateSongAti("mp3");
        }

        public static string NAudioQualifiedType = "FlatRedBall.NAudio.NAudio_Song";

        private static AssetTypeInfo CreateSongAti(string extension)
        {
            var ati = new AssetTypeInfo();

            // check if the GlueProjectSave file version has ISong
            if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.ISongInFrb)
            {
                var toClone = AvailableAssetTypes.Self.AllAssetTypes
                    .FirstOrDefault(item => item.QualifiedRuntimeTypeName.QualifiedType == "Microsoft.Xna.Framework.Media.Song" && item.Extension == "mp3");

                ati = FileManager.CloneObject(toClone);
            }

            ati.AddToManagersFunc = HandleSongAddToManagers;

            ati.MustBeAddedToContentPipeline = false;
            ati.ContentImporter = null;
            ati.ContentProcessor = null;
            ati.Extension = extension;
            ati.QualifiedRuntimeTypeName = new PlatformSpecificType()
            {
                QualifiedType = NAudioQualifiedType
            };

            ati.FriendlyName = $"NAudio Song (.{extension})";

            // Actually not handled by codegen, we match how songs work now:
            //ati.DestroyMethod = null; // handled by codegen

            ati.CustomLoadFunc = GetLoadSongCode;
            return ati;
        }

        private static string HandleSongAddToManagers(IElement element, NamedObjectSave namedObjectSave, ReferencedFileSave rfs, string layer)
        {
            var supportsEditMode = GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.SupportsEditMode;
            string addToManagersMethod = $"FlatRedBall.Audio.AudioManager.StopAndDisposeCurrentSongIfNameDiffers(\"{rfs.GetInstanceName()}\"); ";
            if (supportsEditMode)
            {
                addToManagersMethod +=

                    "if(FlatRedBall.Screens.ScreenManager.IsInEditMode == false) ";
            }

            var forceGlobal = !rfs.DestroyOnUnload;

            string isGlobal =
                forceGlobal ? "true" : "ContentManagerName == \"Global\"";

            addToManagersMethod +=
                    $"FlatRedBall.Audio.AudioManager.PlaySong({rfs.GetInstanceName()}, false, {isGlobal});";

            return addToManagersMethod;
        }

        internal static void ResetAssetTypes()
        {
            RemoveAllNAudioAtis();

            AvailableAssetTypes.Self.AddAssetType(GetNAudioMp3SongAti());
        }

        private static void RemoveAllNAudioAtis()
        {
            var listToRemove = AvailableAssetTypes.Self.AllAssetTypes
                .Where(item => item.QualifiedRuntimeTypeName.QualifiedType == NAudioQualifiedType);

            foreach (var item in listToRemove)
            {
                AvailableAssetTypes.Self.RemoveAssetType(item);
            }
        }

        internal static void RemoveAssetTypes()
        {
            RemoveAllNAudioAtis();
        }

        private static string GetLoadSongCode(IElement screenOrEntity, NamedObjectSave namedObject, 
            ReferencedFileSave file, string contentManager)
        {
            var instanceName = file.GetInstanceName();

            var relativeFileName = file.Name.ToLowerInvariant();

            var path = $"Content/{relativeFileName}";

            var contentManagerName = "contentManagerName";

            if(file.DestroyOnUnload == false)
            {
                contentManagerName = "FlatRedBall.FlatRedBallServices.GlobalContentManager";
            }

            var toReturn = 
            @$"if(FlatRedBall.FlatRedBallServices.IsLoaded<{NAudioQualifiedType}> (""{path}"", {contentManagerName}))
            {{
                {instanceName} = FlatRedBall.FlatRedBallServices.Load<{NAudioQualifiedType}>(""{path}"", {contentManagerName});
            }}
            else
            {{
                {instanceName} =  new {NAudioQualifiedType}(""{path}"");

                FlatRedBall.FlatRedBallServices.AddDisposable(""{path}"", {instanceName}, {contentManagerName});
            }}";


            return toReturn;

            //return $"{instanceName} = new {NAudioQualifiedType}(\"{path}\");";
        }
    }
}
