using System.Collections.Generic;
using System.IO;
using FlatRedBall.IO;

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
                @"Copy all FRBDK .exe files, EditorObjects.dll, and the engine to ReleaseFiles\FRBDK For Zip\ (Auto)", results)
        {

            _excludedDirs = new List<string>();
            _excludeFiles = new List<string>();

            _excludedDirs.Add(@".svn\");
            _excludeFiles.Add(@"Thumbs.db");
        }

        public override void ExecuteStep()
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

            CopyDirectory(DirectoryHelper.GumBuildDirectory, "Copied Gum", "Gum");



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
