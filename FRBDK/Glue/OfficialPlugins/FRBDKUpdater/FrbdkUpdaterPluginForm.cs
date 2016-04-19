using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using FlatRedBall.IO;
using FlatRedBall.Glue.Plugins;

namespace OfficialPlugins.FrbdkUpdater
{
    public partial class FrbdkUpdaterPluginForm : Form
    {
        private FrbdkUpdaterSettings _settings;
        private readonly FrbdkUpdaterPlugin _plugin;

        static string GetProgramFilesx86()
        {
            if (8 == IntPtr.Size
                || (!String.IsNullOrEmpty(Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432"))))
            {
                return Environment.GetEnvironmentVariable("ProgramFiles(x86)");
            }

            return Environment.GetEnvironmentVariable("ProgramFiles");
        }

        public FrbdkUpdaterPluginForm(FrbdkUpdaterPlugin pPlugin)
        {
            InitializeComponent();
            _plugin = pPlugin;

            if (System.IO.Directory.Exists(GetProgramFilesx86() + @"\FlatRedBall\FRBDK\"))
            {
                this.tbPath.Text = GetProgramFilesx86() + @"\FlatRedBall\FRBDK\";
            }

        }

        private void BtnSelectDirectoryClick(object sender, EventArgs e)
        {
            if (fbdFRBDK.ShowDialog() == DialogResult.OK)
            {
                tbPath.Text = fbdFRBDK.SelectedPath;
            }
        }

        private void BtnSyncClick(object sender, EventArgs e)
        {
            if (!Directory.Exists(tbPath.Text))
            {
                MessageBox.Show(@"Directory does not exist.");
                return;
            }

            if (cbCleanFolder.Checked && MessageBox.Show(
                    @"Are you sure you want to clear the contents of this folder and put the selected FRBDK into it?",
                    @"Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            
            SaveFrbdkDownloadSettings();

            var path = FileManager.UserApplicationData + @"FRBDK/TempPlugins/";
            var destinationPath = path + "FRBDKUpdater/";
            var sourcePath = Application.StartupPath + @"\Plugins\FRBDKUpdater\";

            bool succeeded = true;

            if (ExtractUpdaterCheckBox.Checked)
            {
                succeeded = ExtractFrbdkUpdater(path, destinationPath, sourcePath, succeeded);
            }

            if(!succeeded)
            {
                return;
            }

            // see if the app exists locally where it might be checked out from github:
            var myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            var builtLocation = myDocuments + @"\FlatRedBall\FRBDK\FRBDKUpdater\FRBDKUpdater\bin\Debug\FRBDKUpdater.exe";

            System.Diagnostics.Process process = null;
            string exePath = null;

            if (System.IO.File.Exists(builtLocation))
            {
                exePath = builtLocation;
            }
            if (File.Exists(destinationPath + @"FRBDKUpdater.exe"))
            {
                exePath = destinationPath + @"FRBDKUpdater.exe";
            }
            else if (File.Exists(destinationPath + @"FRBDKUpdater\FRBDKUpdater.exe"))
            {
                exePath = destinationPath + @"FRBDKUpdater\FRBDKUpdater.exe";
            }

            if(exePath != null)
            {
                string parameters = "\"" + FrbdkUpdaterSettings.DefaultSaveLocation + "\"";
                process = new System.Diagnostics.Process();
                process.StartInfo.Verb = "runas";
                process.StartInfo.FileName = exePath;
                process.StartInfo.Arguments = parameters;
                process.Start();

                _plugin.GlueCommands.CloseGlue();
            }

            else
            {
                MessageBox.Show(
                    @"Unable to find FRBDKUpdater at

" + destinationPath +
                    @"\FRBDKUpdater.exe
or
" + destinationPath +
                    @"\FRBDKUpdater\FRBDKUpdater.exe");
            }
        }

        private static bool ExtractFrbdkUpdater(string path, string destinationPath, string sourcePath, bool succeeded)
        {
            if (Directory.Exists(sourcePath))
            {
                //Create Plugins folder
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                //Delete old FRBDKUpdater folder
                if (Directory.Exists(destinationPath))
                    Directory.Delete(destinationPath, true);

                //Create FRBDKUpdater folder
                Directory.CreateDirectory(destinationPath);

                //Copy directories
                foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
                }

                //Copy Files
                foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(sourcePath, destinationPath));
                }
            }
            else
            {
                MessageBox.Show(@"Unable to find FRBDKUpdater plug-in folder");
                succeeded = false;
            }
            return succeeded;
        }

        private void SaveFrbdkDownloadSettings()
        {

            _settings.SelectedDirectory = tbPath.Text;
            _settings.SelectedSource = @"DailyBuild\";
            _settings.CleanFolder = cbCleanFolder.Checked;
            _settings.ForceDownload = cbForceDownload.Checked;
            _settings.GlueRunPath = Application.ExecutablePath;
            string whereToSave = FrbdkUpdaterSettings.DefaultSaveLocation;
            _settings.SaveSettings();
        }

        private void SyncFormLoad(object sender, EventArgs e)
        {
            _settings = FrbdkUpdaterSettings.LoadSettings();

            if (!string.IsNullOrEmpty(_settings.SelectedDirectory))
            {
                tbPath.Text = _settings.SelectedDirectory;
            }

            cbCleanFolder.Checked = _settings.CleanFolder;
            cbForceDownload.Checked = _settings.ForceDownload;
        }

        private class Item
        {
            public string Name;
            public string Value;
            public Item(string name, string value)
            {
                Name = name; Value = value;
            }
            public override string ToString()
            {
                // Generates the text shown in the combo box
                return Name;
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            _settings.SetDefaultPath();
            tbPath.Text = _settings.SelectedDirectory;
        }
    }
}
