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
        public string ProcessorParameters { get; set; }

        // /build:C:\Users\vchel\Documents\FlatRedBallProjects\GlTest8\GlTest8\Content\FR_BattleSong_Loop.mp3
        public string BuildFileName { get; set; }

        // /intermediateDir:/outputDir:C:\Users\vchel\Documents\FlatRedBallProjects\GlTest8\GlTest8\Content\obj
        public string IntermediateDirectory { get; set; }

        public static ContentItem CreateMp3Build()
        {
            var toReturn = new ContentItem();

            toReturn.Importer = "Mp3Importer";
            toReturn.Processor = "SongProcessor";
            toReturn.ProcessorParameters = "Quality=Best";

            return toReturn;
        }

        public string GenerateCommandLine()
        {
            return $"/outputDir:{OutputDirectory} /intermediateDir:{IntermediateDirectory} /platform:{Platform} /importer:{Importer} /processor:{Processor} /processorParam:{ProcessorParameters} /build:{BuildFileName}";
        }
    }
}
