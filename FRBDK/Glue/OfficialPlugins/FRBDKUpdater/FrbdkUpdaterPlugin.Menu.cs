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
        private const string FrbAndGameCode = "Update FRB and game code...";

        public const string PluginsMenuItem = "Update";

        public override void StartUp()
        {
            this.AddMenuItemTo(FrbdkSyncMenuItem, () => MenuItemClick(), "Update");

            var menuItem = this.AddMenuItemTo(FrbAndGameCode, (Action)null, "Update");

            menuItem.DropDownItems.Add(new ToolStripMenuItem("FRB and Gum", null, (_, _) => UpdateFrbFromCode(false)));
            menuItem.DropDownItems.Add(new ToolStripMenuItem("FRB, Gum, and Game", null, (_, _) => UpdateFrbFromCode(true)));
        }

        public override bool ShutDown(PluginShutDownReason shutDownReason)
        {
            return true;
        }

        private async void UpdateFrbFromCode(bool updateGame)
        {
            if(GlueState.Self.CurrentGlueProject == null)
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox("You must open a project first before updating");
                return;
            }

            await TaskManager.Self.WaitForAllTasksFinished();

            string gitCommand = String.Empty;
            if(updateGame)
            {
                gitCommand +=
                    $@"echo ""Pulling game {GlueState.Self.CurrentMainProject.Name}..."" & " +
                    @"git fetch & " +
                    @"git pull & ";
            }
            gitCommand +=
                @"cd.. & " +
                @"cd Gum & " +
                $@"echo ""Pulling Gum..."" & " +
                @"git fetch & " +
                @"git pull & " +

                @"cd.. & " +
                @"cd FlatRedBall & " +

                $@"echo ""Pulling FlatRedBall..."" & " +
                @"git fetch & " +
                @"git pull & " +

                $@"echo ""Close this window to build and re-launch FRB Editor"" & " 

;

            var processStartInfo = new ProcessStartInfo("cmd.exe");
            processStartInfo.WorkingDirectory = new FilePath(GlueState.Self.CurrentGlueProjectDirectory).GetDirectoryContainingThis().FullPath;
            processStartInfo.Arguments = "/K " + gitCommand;

            var process = Process.Start(processStartInfo);

            await process.WaitForExitAsync();

            var command =
@"timeout /T 3 /NOBREAK & " +
@"cd.. & " + 
@"cd FlatRedBall & " + 
@"cd FRBDK\Glue & " +
@"dotnet build ""Glue with All.sln"" & " +
@"cd Glue\bin\Debug\ & " +
@"start GlueFormsCore.exe";

            processStartInfo = new ProcessStartInfo("cmd.exe");
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
