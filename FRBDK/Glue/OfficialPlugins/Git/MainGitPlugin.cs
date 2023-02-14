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

        //FilePath UpdateAllAndRunLocation =>
        //    new FilePath(GlueState.Self.CurrentGlueProjectDirectory).GetDirectoryContainingThis() + "UpdateAllAndRunFrb.bat";
        public override Version Version => new Version(1, 0);

        public override void StartUp()
        {
            this.ReactToLoadedGlux += HandleGluxLoaded;
        }


        void HandleGluxLoaded()
        {
            this.AddMenuItemTo("Add/Update .gitignore", (not, used) => AddGitIgnore(), "Update");

            // This is handled in the FrbdkUpdaterPlugin
            //this.RemoveAllMenuItems();

            //if (UpdateAllAndRunLocation.Exists())
            //{
            //    this.AddMenuItemTo("Shut Down and Update from Source", HandleShutdownAndUpdate, "Update");
            //    this.AddMenuItemTo("Refresh UpdateAllAndRun.bat", (not, used) => AddUpdateAllAndRun(), "Update");

            //}
            //else
            //{
            //    this.AddMenuItemTo("Add UpdateAllAndRun.bat", (not, used) => AddUpdateAllAndRun(), "Update");
            //}

        }

        public void AddGitIgnore()
        {
            if(GlueState.Self.CurrentSlnFileName == null)
            {
                return;
            }
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

            yield return $"{gameName}/Content/obj/";

            yield return $"!{gameName}/x64";
            yield return $"!{gameName}/x86";
            yield return $"!{gameName}/Libraries";

        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            RemoveAllMenuItems();
            return true;
        }

        //public void HandleShutdownAndUpdate(object sender, EventArgs e) 
        //{
        //    //GlueCommands.Self.FileCommands.Open(UpdateAllAndRunLocation.GetDirectoryContainingThis());
        //    //GlueCommands.Self.CloseGlue();    
        //}
    }
}
