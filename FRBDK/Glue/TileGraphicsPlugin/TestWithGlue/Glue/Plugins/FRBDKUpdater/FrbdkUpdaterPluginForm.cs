using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Forms;
using FlatRedBall.IO;

namespace OfficialPlugins.FrbdkUpdater
{
    public partial class FrbdkUpdaterPluginForm : Form
    {
        private FrbdkUpdaterSettings _settings;
        private readonly FrbdkUpdaterPlugin _plugin;

        public FrbdkUpdaterPluginForm(FrbdkUpdaterPlugin pPlugin)
        {
            InitializeComponent();
            _plugin = pPlugin;
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

            var inList = false;
            foreach (var item in cbSyncTo.Items.Cast<Item>().Where(item => cbSyncTo.Text == item.Name))
            {
                inList = true;
            }

            if(!inList)
            {
                MessageBox.Show(@"Must pick source to sync to.");
                return;
            }

            if (cbCleanFolder.Checked && MessageBox.Show(
                    @"Are you sure you want to clear the contents of this folder and put the selected FRBDK into it?",
                    @"Warning", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            _settings.SelectedDirectory = tbPath.Text;
            _settings.SelectedSource = (cbSyncTo.SelectedItem as Item).Value;
            _settings.CleanFolder = cbCleanFolder.Checked;
            _settings.ForceDownload = cbForceDownload.Checked;
            _settings.GlueRunPath = Application.ExecutablePath;
            _settings.SaveSettings();

            var path = FileManager.UserApplicationData + @"FRBDK/TempPlugins/";
            var destinationPath = path + "FRBDKUpdater/";
            var sourcePath = Application.StartupPath + @"\Plugins\FRBDKUpdater\";

            if (Directory.Exists(sourcePath))
            {
                //Create Plugins folder
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);

                //Delete old FRBDKUpdater folder
                if(Directory.Exists(destinationPath))
                    Directory.Delete(destinationPath, true);

                //Create FRBDKUpdater folder
                Directory.CreateDirectory(destinationPath);

                //Copy directories
                foreach(var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
                }

                //Copy Files
                foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(sourcePath, destinationPath));
                }
            }else
            {
                MessageBox.Show(@"Unable to find FRBDKUpdater plug-in folder");
                return;
            }

            if (File.Exists(destinationPath + @"FRBDKUpdater.exe"))
            {
                _plugin.GlueCommands.OpenCommands.OpenExternalApplication(
                    destinationPath + @"FRBDKUpdater.exe", String.Empty);
                _plugin.GlueCommands.CloseGlue();
            }
            else if (File.Exists(destinationPath + @"FRBDKUpdater\FRBDKUpdater.exe"))
            {
                _plugin.GlueCommands.OpenCommands.OpenExternalApplication(
                    destinationPath + @"FRBDKUpdater\FRBDKUpdater.exe", String.Empty);
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

        private void SyncFormLoad(object sender, EventArgs e)
        {
            BuildMenu();
            _settings = FrbdkUpdaterSettings.LoadSettings();
            tbPath.Text = _settings.SelectedDirectory;

            foreach (Item item in cbSyncTo.Items)
            {
                if(item.Value == _settings.SelectedSource)
                {
                    cbSyncTo.SelectedItem = item;
                    break;
                }
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

        private void BuildMenu()
        {
            cbSyncTo.Items.Clear();

            try
            {
                var wc = new WebClient();
                string path = Path.GetTempPath() + @"\BackupFolders.txt";
                wc.DownloadFile("http://www.flatredball.com/content/FrbXnaTemplates/BackupFolders.txt", path);

                var file = new StreamReader(path);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    var split = line.Split(Convert.ToChar(","));
                    cbSyncTo.Items.Add(new Item(split[0], split[1]));
                }
            }
            catch (Exception)
            {
            }
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            _settings.SetDefaultPath();
            tbPath.Text = _settings.SelectedDirectory;
        }
    }
}
