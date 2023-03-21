using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.IO;

namespace OfficialPlugins.FrbdkUpdater
{

    [Export(typeof(PluginBase))]
    public class FrbdkUpdaterPlugin : PluginBase
    {
        public override string FriendlyName => "Updater Plugin";
        public override Version Version => new System.Version(2,0);


        private const string FrbdkSyncMenuItem = "Update FRB editor binaries";
        private const string FrbFromCode = "Update FRB and game code in Git, build and relaunch FRB";

        public const string PluginsMenuItem = "Update";

        public override void StartUp()
        {
            this.AddMenuItemTo(FrbdkSyncMenuItem, () => MenuItemClick(), "Update");

            this.AddMenuItemTo(FrbFromCode, () => UpdateFrbFromCode(), "Update");
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        private async void UpdateFrbFromCode()
        {
            if(GlueState.Self.CurrentGlueProject == null)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox("You must open a project first before updating");
                return;
            }

            await TaskManager.Self.WaitForAllTasksFinished();

            var command =
@"timeout /T 4 /NOBREAK & " + 
@"git fetch & " + 
@"git pull & " + 
@"cd.. & " + 
@"cd Gum & " + 
@"git fetch & " + 
@"git pull & " + 
@"cd.. & " + 
@"cd FlatRedBall & " + 
@"git fetch & " + 
@"git pull & " + 
@"cd FRBDK\Glue & " + 
@"dotnet build ""Glue with All.sln"" & " + 
@"cd Glue\bin\Debug\ & " +
@"start GlueFormsCore.exe & " +
@"exit";

            var processStartInfo = new ProcessStartInfo("cmd.exe");
            processStartInfo.WorkingDirectory = new FilePath(GlueState.Self.CurrentGlueProjectDirectory).GetDirectoryContainingThis().FullPath;
            processStartInfo.Arguments = "/K " + command;

            Process.Start(processStartInfo);

            GlueCommands.Self.CloseGlue();
        }

        void MenuItemClick()
        {
            var _form = new FrbdkUpdaterPluginForm(this);

            GlueCommands.Self.DialogCommands.SetFormOwner(_form);

            _form.Show();
        }


    }
}
