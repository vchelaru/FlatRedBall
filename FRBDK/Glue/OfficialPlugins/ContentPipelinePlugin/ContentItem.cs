using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.MonoGameContent
{
    class ContentItem
    {
        #region Properties

        // /outputDir:C:\Users\vchel\Documents\FlatRedBallProjects\GlTest8\GlTest8\Content\bin
        public string OutputDirectory { get; set; }

        // /platform:DesktopGL
        public string Platform { get; set; }

        // /importer:Mp3Importer 
        public string Importer { get; set; }

        // /processor:SongProcessor
        public string Processor { get; set; }

        // /processorParam:Quality=Best
        public List<string> ProcessorParameters { get; set; } = new List<string>();

        // /build:C:\Users\vchel\Documents\FlatRedBallProjects\GlTest8\GlTest8\Content\FR_BattleSong_Loop.mp3
        /// <summary>
        /// The raw (prebuilt) file to build to XNB.
        /// </summary>
        public string BuildFileName { get; set; }

        // /intermediateDir:/outputDir:C:\Users\vchel\Documents\FlatRedBallProjects\GlTest8\GlTest8\Content\obj
        public string IntermediateDirectory { get; set; }

        public string OutputFileNoExtension
        {
            get;
            set;
        }

        #endregion

        #region Methods

        #region CreateXXXXBuild() methods

        // The CreateXXXXBuild() methods create a ContentItem
        // which can be used to build a content type using the
        // MonoGame Content Pipeline. These are used to run the
        // commandline tool. More types may be added here over time
        // to handle more types of content. To figure out how to add
        // one here, you can create a new monogame project and add the
        // desired file type to the project, then save the .mgcb, which
        // is a text file. That text file will include the parameters which
        // are sent to the content pipeline, and those can be used to assign
        // properties on the ContentItem.

        public static ContentItem CreateMp3Build()
        {
            var toReturn = new ContentItem();

            toReturn.Importer = "Mp3Importer";
            toReturn.Processor = "SongProcessor";
            toReturn.ProcessorParameters.Add("Quality=Best");

            return toReturn;
        }

        public static ContentItem CreateWavBuild()
        {
            var toReturn = new ContentItem();

            toReturn.Importer = "WavImporter";
            toReturn.Processor = "SoundEffectProcessor";
            toReturn.ProcessorParameters.Add("Quality=Best");

            return toReturn;
        }

        public static ContentItem CreateTextureBuild()
        {
            var toReturn = new ContentItem();

            toReturn.Importer = "TextureImporter";
            toReturn.Processor = "TextureProcessor";
            toReturn.ProcessorParameters.Add("ColorKeyEnabled=False");
            toReturn.ProcessorParameters.Add("GenerateMipmaps=False");
            toReturn.ProcessorParameters.Add("PremultiplyAlpha=True");
            toReturn.ProcessorParameters.Add("TextureFormat=Color");

            return toReturn;
        }


        internal static ContentItem CreateEffectBuild()
        {
            var toReturn = new ContentItem();

            toReturn.Importer = "EffectImporter";
            toReturn.Processor = "EffectProcessor";
            toReturn.ProcessorParameters.Add("DebugMode=Auto");

            return toReturn;
        }

        #endregion

        public string GenerateCommandLine()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"/outputDir:\"{OutputDirectory}\" /intermediateDir:\"{IntermediateDirectory}\" /platform:{Platform} /importer:{Importer} /processor:{Processor} ");
            foreach(var parameter in ProcessorParameters)
            {
                stringBuilder.Append($"/processorParam:{parameter} ");
            }

            // iOS and Android have case sensitive file systems, so we'll to-lower it here

            string buildArgument = BuildFileName;

            if(Platform == "Android" || Platform == "iOS")
            {
                buildArgument = buildArgument.ToLowerInvariant();
            }

            stringBuilder.Append($"/build:\"{buildArgument}\" /incremental");

            return stringBuilder.ToString();
        }


        public bool GetIfNeedsBuild(string destinationDirectory)
        {
            var lastSourceWrite = System.IO.File.GetLastWriteTime(BuildFileName);
            foreach(var extension in GetBuiltExtensions())
            {
                string rawFileName = FileManager.RemovePath(FileManager.RemoveExtension(BuildFileName));
                foreach(var outputExtension in GetBuiltExtensions())
                {
                    var outputFile = destinationDirectory + rawFileName + "." + outputExtension;
                    bool exists = FileManager.FileExists(outputFile);

                    if(!exists)
                    {
                        return true;
                    }
                    else
                    {
                        var lastTargetWrite = System.IO.File.GetLastWriteTime(outputFile);

                        if(lastSourceWrite > lastTargetWrite)
                        {
                            return true;
                        }
                    }

                }
            }

            return false;
        }

        internal IEnumerable<string> GetBuiltExtensions()
        {
            switch(Processor)
            {
                case "SoundEffectProcessor":
                    yield return "xnb";
                    break;
                case "TextureProcessor":
                    yield return "xnb";
                    break;
                case "SongProcessor":
                    // does this depend on platform?
                    // Yes it does:
                    yield return "xnb";

                    if(Platform == "Android")
                    {
                        yield return "m4a";
                    }
                    else if(Platform == "WindowsStoreApp")
                    {
                        yield return "wma";
                    }
                    else
                    {
                        // Not sure if other platforms use .ogg, need to test this
                        yield return "ogg";
                    }
                    break;
                case "EffectProcessor":
                    yield return "xnb";
                    break;
            }
        }

        public override string ToString()
        {
            return $"{BuildFileName} {Importer} {Processor}";
        }

        #endregion

    }
}
