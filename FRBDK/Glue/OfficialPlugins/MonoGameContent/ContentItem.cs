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
        public string BuildFileName { get; set; }

        // /intermediateDir:/outputDir:C:\Users\vchel\Documents\FlatRedBallProjects\GlTest8\GlTest8\Content\obj
        public string IntermediateDirectory { get; set; }

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


        public string GenerateCommandLine()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append($"/outputDir:{OutputDirectory} /intermediateDir:{IntermediateDirectory} /platform:{Platform} /importer:{Importer} /processor:{Processor} ");
            foreach(var parameter in ProcessorParameters)
            {
                stringBuilder.Append($"/processorParam:{ProcessorParameters} ");
            }
            stringBuilder.Append($"/build:{BuildFileName} /incremental");

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
                    yield return "xnb";
                    yield return "ogg";
                    break;
            }
        }
        
    }
}
