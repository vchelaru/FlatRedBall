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
        public override Version Version => new (2,0);

        public override void StartUp()
        {
            this.AddMenuItemTo(Localization.Texts.FrbEditorBinaries, Localization.MenuIds.FrbEditorBinariesId, MenuItemClick, Localization.MenuIds.UpdateId);

            var menuItem = this.AddMenuItemTo(Localization.Texts.FrbUpdateAndGameCode, Localization.MenuIds.FrbUpdateAndGameCodeId, (Action)null, Localization.MenuIds.UpdateId);

            menuItem.DropDownItems.Add(new ToolStripMenuItem(Localization.Texts.FrbAndGum, null, (_, _) => UpdateFrbFromCode(false)));
            menuItem.DropDownItems.Add(new ToolStripMenuItem(Localization.Texts.FrbGumAndGame, null, (_, _) => UpdateFrbFromCode(true)));
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

            int numberOfCds = 0;
            var slnPath = new FilePath(GlueState.Self.CurrentGlueProjectDirectory).GetDirectoryContainingThis();
            var currentPath = slnPath;

            while(true)
            {
                numberOfCds++;
                currentPath = currentPath.GetDirectoryContainingThis();
                var directories = System.IO.Directory.GetDirectories(currentPath.FullPath);

                var hasGum = directories.Any(item => item.EndsWith("Gum"));
                var hasFrb = directories.Any(item => item.EndsWith("FlatRedBall"));

                if(hasGum && hasFrb)
                {
                    break;
                }
            }

            gitCommand +=
                @"echo ""Moving to the Gum folder"" & ";

            for(int i = 0; i < numberOfCds; i++)
            {
                gitCommand +=
                    @"cd.. & ";
            }
            
            gitCommand +=
                @"cd Gum & " +

                $@"echo ""Pulling Gum..."" & " +
                @"git fetch & " +
                @"git pull & " +

                @"echo ""Moving to the FRB folder"" & " +
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
                @"timeout /T 3 /NOBREAK & ";

            for(int i = 0; i < numberOfCds; i++)
            {
                command +=
                    @"cd.. & ";
            }



            command +=
@"cd FlatRedBall & " + 
@"cd FRBDK\Glue & " +
@"rd Glue\bin /S /Q & " + 
@"dotnet build --no-incremental ""Glue with All.sln"" & " +
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
