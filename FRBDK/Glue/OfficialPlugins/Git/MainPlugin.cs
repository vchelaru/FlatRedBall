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
    public class MainPlugin : PluginBase
    {
        public override string FriendlyName
        {
            get
            {
                return "Git Plugin";
            }
        }

        public override Version Version
        {
            get
            {
                return new Version(1, 0);
            }
        }

        public override void StartUp()
        {
            this.AddMenuItemTo("Add/Update .gitignore", HandleAddGitIgnore, "Update");
        }

        private void HandleAddGitIgnore(object sender, EventArgs e)
        {
            // Vic asks - does gitignore sit in the root? I think so...
            var gitIgnoreFolder = GlueState.Self.CurrentSlnFileName.GetDirectoryContainingThis();

            string gitIgnoreFile = gitIgnoreFolder.FullPath + ".gitignore";

            List<string> contents = new List<string>() ;

            if(System.IO.File.Exists(gitIgnoreFile))
            {
                contents = System.IO.File.ReadAllLines(gitIgnoreFile).ToList();
            }

            var necessaryLines = GetNecessaryLines();

            bool didAnythingChange = false;
            foreach(var necessaryLine in necessaryLines)
            {
                if(contents.Contains(necessaryLine) == false)
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
            
            string gameName = 
                FileManager.RemovePath(FileManager.RemoveExtension(GlueState.Self.CurrentCodeProjectFileName));

            yield return "*.user";

            yield return "*.aeproperties";

            yield return ".vs/";

            yield return "*.tmp";

            yield return "*.Generated.cs";
            yield return "*.Generated.Event.cs";

            yield return "*.cachefile";

            yield return "*.csvSettings";

            yield return "*.sln.metaproj";

            yield return $"{gameName}/{gameName}/bin/";
            yield return $"{gameName}/bin/";

            yield return $"{gameName}/{gameName}/obj/";
            yield return $"{gameName}/obj/";

            yield return $"{gameName}/{gameName}Content/obj/";
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            RemoveAllMenuItems();
            return true;
        }
    }
}
