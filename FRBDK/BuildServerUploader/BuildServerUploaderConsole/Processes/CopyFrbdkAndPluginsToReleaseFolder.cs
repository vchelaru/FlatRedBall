using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using FlatRedBall.IO;
using Ionic.Zip;

namespace BuildServerUploaderConsole.Processes
{
    public class CopyFrbdkAndPluginsToReleaseFolder : ProcessStep
    {
        #region Fields

        private List<string> _excludedDirs;
        private List<string> _excludeFiles;
        private string _destDirectory;

        List<string> extraTools = new List<string>
        {
            @"PrebuiltTools\MGCB"
        };

        string GlueRegularBuildDestinationFolder =
            @"Glue\Glue\bin\Debug\";

        // This is the output from: dotnet publish GlueFormsCore.csproj -r win-x86 -c DEBUG
        string GluePublishDestinationFolder
        {
            get
            {
                return DirectoryHelper.FrbdkDirectory + @"Glue\Glue\bin\DEBUG\win-x86\publish\";
            }
        }


        // I'd like to have all the tools sit in their own directories, but
        // this is a big change so I'm going to do it incrementally by moving
        // them 
        List<string> mXna4_0ToolsInOwnDirectories = new List<string>
        {
            @"AnimationEditor\PreviewProject\bin\Debug",
            // This is removed because FRB XNA is dying.
            //@"SplineEditor\SplineEditor\bin\x86\Debug",
        };

        #endregion

        public CopyFrbdkAndPluginsToReleaseFolder(IResults results)
            : base(
                @"Copy all FRBDK files to ReleaseFiles\FRBDK For Zip\ (Auto)", results)
        {

            _excludedDirs = new List<string>();
            _excludeFiles = new List<string>();

            _excludedDirs.Add(@".svn\");
            _excludeFiles.Add(@"Thumbs.db");
        }

        public override async Task ExecuteStepAsync()
        {
            //Create Directory
            var frbdkForZipDirectory = DirectoryHelper.FrbdkForZipReleaseDirectory;

            DirectoryHelper.DeleteDirectory(frbdkForZipDirectory);

            if (!Directory.Exists(frbdkForZipDirectory))
                Directory.CreateDirectory(frbdkForZipDirectory);

            _destDirectory = frbdkForZipDirectory;

            foreach(var extraTool in extraTools)
            {
                CopyDirectory(DirectoryHelper.FrbdkDirectory + extraTool, "Copied" + extraTool, subdirectoryName:extraTool);
            }

            // Gum can't be built on github actions because it fails with dotnetbuild - something to do with 
            // it being net 4.7.1 or maybe XNA? So instead...
            //if(Directory.Exists(DirectoryHelper.GumBuildDirectory))
            //{
            //    CopyDirectory(DirectoryHelper.GumBuildDirectory, "Copied Gum", "Gum");
            //}
            // ... we'll download the file:
            await DownloadGum();



            //XNA 4 TOOLS
            string xna4ToolsDirectory = FileManager.Standardize(frbdkForZipDirectory + @"\Xna 4 Tools\");

            if (!Directory.Exists(xna4ToolsDirectory))
            {
                Directory.CreateDirectory(xna4ToolsDirectory);
            }

            _destDirectory = xna4ToolsDirectory;

            foreach (var xna4_0tool in mXna4_0ToolsInOwnDirectories)
            {
                string subdirectory = xna4_0tool.Substring(0, xna4_0tool.IndexOf("\\")) + "\\";

                CopyDirectory(DirectoryHelper.FrbdkDirectory + xna4_0tool, "Copied " + xna4_0tool, subdirectory);
            }

            if(!System.IO.Directory.Exists(DirectoryHelper.GluePublishDestinationFolder))
            {
                throw new System.Exception($"The {nameof(DirectoryHelper.GluePublishDestinationFolder)} {DirectoryHelper.GluePublishDestinationFolder} doesn't exist but it should");
            }
            CopyDirectory(DirectoryHelper.GluePublishDestinationFolder, "Copied " + DirectoryHelper.GluePublishDestinationFolder);
            CopyDirectory(DirectoryHelper.FrbdkDirectory + GlueRegularBuildDestinationFolder + @"Plugins\", "Copied plugins to Glue", @"\Plugins\");

            // save the run FlatRedBall batch file:
            System.IO.File.WriteAllText(path:frbdkForZipDirectory + "Run FlatRedBall.bat", contents: @"START """" ""%~dp0Xna 4 Tools\GlueFormsCore.exe""");

        }

        async Task DownloadGum()
        {
            string url = "http://files.flatredball.com/content/Tools/Gum/Gum.zip"; // Replace with your actual URL
            string targetDirectory = Path.Combine(_destDirectory, "Gum");
            string zipFilePath = Path.Combine(targetDirectory, "Gum.zip");
            string unzipDirectory = targetDirectory;

            Results.WriteMessage($"Downloading Gum from {url}");

            if(!System.IO.Directory.Exists(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            using (var client = new HttpClient())
            {
                // Download the file
                using (var response = await client.GetAsync(url))
                {
                    using (Stream stream = await response.Content.ReadAsStreamAsync())
                    {
                        using (FileStream fileStream = new FileStream(zipFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                    }
                }

                Results.WriteMessage($"Unzipping Gum from {zipFilePath}");

                // Unzip the file
                System.IO.Compression.ZipFile.ExtractToDirectory(zipFilePath, unzipDirectory);

                // delete the zip - we don't need it anymore and it bloats the ultimate file:
                System.IO.File.Delete(zipFilePath);

                Results.WriteMessage($"Gum unzipped to {unzipDirectory}");
            }
        }

        private void CopyDirectory(string sourceDirectory, string successfulMessage, string subdirectoryName = null)
        {
            string destination = _destDirectory;

            if (!string.IsNullOrEmpty(subdirectoryName))
            {
                destination += subdirectoryName;
            }


            FileManager.CopyDirectory(sourceDirectory,
                destination, false, _excludeFiles, _excludedDirs);

            Results.WriteMessage(successfulMessage + @" Copied from " + sourceDirectory + " to " + destination);
        }
    }
}
