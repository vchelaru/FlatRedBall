using FlatRedBall.Glue.VSHelpers.Projects;
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

        // Adding "compress:True" will append "/compress:True", so no prefixed slashes are needed
        public List<string> AdditionalParameters { get; set; } = new List<string>();

        // /build:C:\Users\vchel\Documents\FlatRedBallProjects\GlTest8\GlTest8\Content\FR_BattleSong_Loop.mp3
        /// <summary>
        /// The raw (prebuilt) file to build to XNB.
        /// </summary>
        public FilePath BuildFileName { get; set; }

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


        public static ContentItem CreateWmaBuild()
        {
            var toReturn = new ContentItem();

            toReturn.Importer = "WmaImporter";
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

        public static ContentItem CreateFbxBuild()
        {
            var toReturn = new ContentItem();
            toReturn.Importer = "FbxImporter";
            toReturn.Processor = "ModelProcessor";
            toReturn.ProcessorParameters.Add("ColorKeyEnabled=True");
            toReturn.ProcessorParameters.Add("DefaultEffect=BasicEffect");
            return toReturn;
            /*
             *                     "/outputDir:bin /intermediateDir:obj /platform:Windows /config: /profile:Reach /compress:False " +
                    "/importer:FbxImporter /processor:ModelProcessor /processorParam:ColorKeyColor=0,0,0,0 " +
                    "/processorParam:ColorKeyEnabled=True /processorParam:DefaultEffect=BasicEffect " +
                    "/processorParam:GenerateMipmaps=True /processorParam:GenerateTangentFrames=False " +
                    "/processorParam:PremultiplyTextureAlpha=True /processorParam:PremultiplyVertexColors=True " +
                    "/processorParam:ResizeTexturesToPowerOfTwo=False /processorParam:RotationX=0 " +
                    "/processorParam:RotationY=0 /processorParam:RotationZ=0 /processorParam:Scale=1 " +
                    "/processorParam:SwapWindingOrder=False " +
                    "/processorParam:TextureFormat=Compressed /build:VertexColorTree.fbx";

             * 
             * 
             * 
             * 
             */
        }

        public static ContentItem CreateTextureBuild(ProjectBase project)
        {
            var toReturn = new ContentItem();

            toReturn.Importer = "TextureImporter";
            toReturn.Processor = "TextureProcessor";
            toReturn.ProcessorParameters.Add("ColorKeyEnabled=False");
            toReturn.ProcessorParameters.Add("GenerateMipmaps=False");
            toReturn.ProcessorParameters.Add("PremultiplyAlpha=True");
            toReturn.ProcessorParameters.Add("TextureFormat=Color");

            if(project is KniWebProject)
            {
                toReturn.AdditionalParameters.Add("compress:True");
                toReturn.AdditionalParameters.Add("compression:Brotli");
            }

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

        public string GenerateCommandLine(VisualStudioProject project, bool rebuild = false) =>
            GenerateCommandLine(new[] { this }, rebuild);

        /// <summary>
        /// One MGCB command line that builds every item. The output, intermediate and platform options apply to
        /// the whole run, so the items must share them (see <see cref="GetBatchKey"/>); the importer, processor
        /// and its parameters apply to the /build that follows them, and MGCB clears the processor parameters
        /// whenever /processor is set.
        /// </summary>
        public static string GenerateCommandLine(IReadOnlyList<ContentItem> items, bool rebuild = false)
        {
            var stringBuilder = new StringBuilder();
            var first = items[0];

            stringBuilder.Append($"/outputDir:\"{first.OutputDirectory}\" /intermediateDir:\"{first.IntermediateDirectory}\" /platform:{first.Platform} ");

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (i > 0)
                {
                    stringBuilder.Append(' ');
                }
                stringBuilder.Append($"/importer:{item.Importer} /processor:{item.Processor} ");
                foreach (var parameter in item.ProcessorParameters)
                {
                    stringBuilder.Append($"/processorParam:{parameter} ");
                }

                foreach (var parameter in item.AdditionalParameters)
                {
                    stringBuilder.Append($"/{parameter} ");
                }

                // Keeps the case as-is: Glue no longer lower-cases paths (April 21, 2025), which matters for
                // the case-sensitive file systems of iOS and Android.
                stringBuilder.Append($"/build:\"{item.BuildFileName.FullPath}\"");
            }

            if(rebuild == false)
            {
                stringBuilder.Append(" /incremental");
            }
            else
            {
                stringBuilder.Append(" /rebuild ");
            }
            return stringBuilder.ToString();
        }


        /// <summary>
        /// Items with the same key can be built by one MGCB run: they share the options that apply to the whole run
        /// (/outputDir, /intermediateDir, /platform, and global switches such as /compress) and the working directory.
        /// </summary>
        public string GetBatchKey() => string.Join("|",
            BuildFileName.GetDirectoryContainingThis().FullPath, OutputDirectory, IntermediateDirectory, Platform,
            string.Join(" ", AdditionalParameters));

        public bool GetIfNeedsBuild(string destinationDirectory)
        {
            var lastSourceWrite = System.IO.File.GetLastWriteTime(BuildFileName.FullPath);
            foreach(var extension in GetBuiltExtensions())
            {
                string rawFileName = BuildFileName.NoPathNoExtension;
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
                case "SoundEffectProcessor": // wav
                case "ModelProcessor":
                    yield return "xnb";
                    break;
                case "TextureProcessor":
                    yield return "xnb";
                    break;
                case "SongProcessor":
                    // does this depend on platform?
                    // Yes it does:
                    yield return "xnb";

                    if (Platform == "Android")
                    {
                        yield return "m4a";
                    }
                    else if (Platform == "WindowsStoreApp")
                    {
                        yield return "wma";
                    }
                    else if (Platform == "BlazorGL")
                    {
                        var builtExtension = BuildFileName.Extension;
                        if (builtExtension == "mp3")
                        {
                            yield return "mp3";
                        }
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
            return $"{BuildFileName.FullPath} {Importer} {Processor}";
        }

        #endregion

    }
}
