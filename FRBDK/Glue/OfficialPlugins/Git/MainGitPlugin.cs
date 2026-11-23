using FlatRedBall.Glue.Plugins;
using System;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.ComponentModel.Composition;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace OfficialPlugins.Git
{
    [Export(typeof(PluginBase))]
    public class MainGitPlugin : PluginBase
    {
        readonly GitCommands gitCommands = new GitCommands();

        public override string FriendlyName => "Git Plugin";

        //FilePath UpdateAllAndRunLocation =>
        //    new FilePath(GlueState.Self.CurrentGlueProjectDirectory).GetDirectoryContainingThis() + "UpdateAllAndRunFrb.bat";
        public override Version Version => new Version(1, 0);

        public override void StartUp()
        {
            GlueCommands.Self.GitCommands = gitCommands;

            this.ReactToLoadedGlux += HandleGluxLoaded;
            this.ReactToUnloadedGlux += HandleUnloadedGlux;
        }

        void HandleGluxLoaded()
        {
            this.AddMenuItemTo("Add/Update .gitignore", Localization.MenuIds.AddUpdateGitignoreId, gitCommands.AddGitIgnore, Localization.MenuIds.UpdateId);
        }

        private void HandleUnloadedGlux()
        {
            this.RemoveAllMenuItems();
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            if (GlueCommands.Self.GitCommands == gitCommands)
            {
                GlueCommands.Self.GitCommands = null;
            }

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
