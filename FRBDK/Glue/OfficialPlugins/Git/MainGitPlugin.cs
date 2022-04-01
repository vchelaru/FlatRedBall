using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;

namespace OfficialPlugins.Git
{
    [Export(typeof(PluginBase))]
    public class MainGitPlugin : PluginBase
    {
        public override string FriendlyName => "Git Plugin";

        public override Version Version => new Version(1, 0);

        public override void StartUp()
        {
            this.AddMenuItemTo("Add/Update .gitignore", (not, used) => AddGitIgnore(), "Update");
            this.AddMenuItemTo("Add UpdateAllAndRun.bat", (not, used) => AddUpdateAllAndRun(), "Update");
        }

        public void AddGitIgnore()
        {
            // Vic asks - does gitignore sit in the root? I think so...
            var gitIgnoreFolder = GlueState.Self.CurrentSlnFileName.GetDirectoryContainingThis();

            string gitIgnoreFile = gitIgnoreFolder.FullPath + ".gitignore";

            List<string> contents = new List<string>() ;

            if(System.IO.File.Exists(gitIgnoreFile))
            {
                contents = System.IO.File.ReadAllLines(gitIgnoreFile)
                    .ToList()
                    ;
            }

            var necessaryLines = GetNecessaryLines();

            bool didAnythingChange = false;

            foreach(var necessaryLine in necessaryLines)
            {
                var alreadyContainsLine = contents
                    .Any(item => item?.ToLowerInvariant() == necessaryLine?.ToLowerInvariant());

                if (!alreadyContainsLine)
                {
                    contents.Add(necessaryLine);
                    didAnythingChange = true;
                }
            }
            if(didAnythingChange)
            {
                System.IO.File.WriteAllLines(gitIgnoreFile, contents.ToArray());
                PluginManager.ReceiveOutput($"Updated {gitIgnoreFile}");
            }
        }

        public void AddUpdateAllAndRun()
        {
            if(GlueState.Self.CurrentGlueProject != null)
            {
                var contents =
@"git fetch
git pull
cd..
cd Gum
git fetch
git pull
cd..
cd FlatRedBall
git fetch
git pull
cd FRBDK\Glue
dotnet build ""Glue with All.sln""
cd Glue\bin\x86\Debug\netcoreapp3.0\
start GlueFormsCore.exe";
                var locationToSave = new FilePath(GlueState.Self.CurrentGlueProjectDirectory).GetDirectoryContainingThis();

                var destinationFileName = locationToSave.FullPath + "UpdateAllAndRunFrb.bat";

                try
                {
                    System.IO.File.WriteAllText(destinationFileName, contents);
                    GlueCommands.Self.PrintOutput($"Added batch file to:\n{destinationFileName}");
                }
                catch (Exception ex)
                {
                    GlueCommands.Self.PrintOutput(ex.ToString());
                }
            }
        }

        private IEnumerable<string> GetNecessaryLines()
        {
            // accoding to this:
            // https://stackoverflow.com/questions/8783093/gitignore-syntax-bin-vs-bin-vs-bin-vs-bin#:~:text=Since%20the%20earlier%20ignores%20the%20directory%20as%20a,%28ahem%29%20ignoresit%20if%20you%20ignored%20the%20parent%20directory%21
            // we will not append * at the end of ignores like "bin/"

            string gameName = 
                FileManager.RemovePath(FileManager.RemoveExtension(GlueState.Self.CurrentCodeProjectFileName.FullPath));

            yield return "*.user";

            yield return "*.aeproperties";

            yield return ".vs/";

            yield return "*.tmp";

            yield return "*.Generated.cs";
            yield return "*.Generated.Event.cs";
            yield return "*.generatedpreview.png";

            yield return "TiledObjects.Generated.xml";

            yield return "*.cachefile";

            yield return "*.csvSettings";

            yield return "*.sln.metaproj";

            yield return "packages/";

            yield return $"{gameName}/{gameName}/bin/";
            yield return $"{gameName}/bin/";

            yield return $"{gameName}/{gameName}/obj/";
            yield return $"{gameName}/obj/";

            yield return $"{gameName}/{gameName}Content/obj/";

            yield return $"!{gameName}/x64";
            yield return $"!{gameName}/x86";
            yield return $"!{gameName}/Libraries";

        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            RemoveAllMenuItems();
            return true;
        }
    }
}
