using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Linq;
using FlatRedBall.IO;
using Ionic.Zip;
using System.Collections.Generic;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins;

namespace FlatRedBall.Glue.Controls
{
    public partial class CreatePluginWindow : Form
    {
        #region Fields

        List<string> mWhatIsConsideredCode = new List<string>();

        #endregion

        string PluginFolder
        {
            get
            {
                if (FromFolderRadioButton.Checked)
                {
                    return FromFolderTextBox.Text;
                }
                else
                {
                    PluginContainer container = FromInstalledPluginComboBox.SelectedItem as PluginContainer;

                    if (container != null)
                    {
                        return FileManager.GetDirectory( container.AssemblyLocation );
                    }
                }

                return null;
            }
        }

        public CreatePluginWindow()
        {
            InitializeComponent();

            mWhatIsConsideredCode.Add("cs");
            mWhatIsConsideredCode.Add("resx");

            FillInstalledPluginComboBox();

            UpdateToSelectedSource();
        }

        private void FillInstalledPluginComboBox()
        {
            List<PluginContainer> plugins = new List<PluginContainer>();
            foreach (PluginContainer pluginContainer in PluginManager.AllPluginContainers)
            {
                if (pluginContainer.Plugin is EmbeddedPlugin == false)
                {
                    plugins.Add(pluginContainer);
                }
            }

            plugins.Sort((first, second) => first.Name.CompareTo(second.Name));

            foreach (var plugin in plugins)
            {
                this.FromInstalledPluginComboBox.Items.Add(plugin);
            }
        }

        private void btnPath_Click(object sender, EventArgs e)
        {
            if(fbdPath.ShowDialog() == DialogResult.OK)
            {
                FromFolderTextBox.Text = fbdPath.SelectedPath;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {

            //Validate plugin folder
            if (!Directory.Exists(PluginFolder))
            {
                MessageBox.Show(@"Please select a valid folder to create the plugin from.");
                return;
            }

            //Prompt for save path
            if(sfdPlugin.ShowDialog() != DialogResult.OK)
            {
                return;   
            }

            if (File.Exists(sfdPlugin.FileName))
            {
                FileHelper.DeleteFile(sfdPlugin.FileName);
            }
            //Create plugin file
            using(var zip = new ZipFile())
            {
                var directory = new DirectoryInfo(PluginFolder);

                //Add files in directory
                foreach (var fileToAdd in Directory.GetFiles(PluginFolder, "*.*", SearchOption.AllDirectories))
                {
                    if (this.AllFilesRadioButton.Checked || mWhatIsConsideredCode.Contains(FileManager.GetExtension(fileToAdd).ToLower()))
                    {
                        string relativeDirectory = null;

                        relativeDirectory = FileManager.MakeRelative(FileManager.GetDirectory(fileToAdd), directory.Parent.FullName);

                        if (relativeDirectory.EndsWith("/"))
                        {
                            relativeDirectory = relativeDirectory.Substring(0, relativeDirectory.Length - 1);
                        }
                        zip.AddFile(fileToAdd, relativeDirectory);
                    }
                }

                //Add compatibility file
                var time = new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime;


                if (zip.Entries.Any(item => item.FileName == directory.Name + "/" + "Compatibility.txt"))
                {
                    zip.RemoveEntry(directory.Name + "/" + "Compatibility.txt");
                }

                try
                {

                    zip.AddFileFromString("Compatibility.txt", directory.Name, time.ToString());
                }
                catch (Exception)
                {
                    MessageBox.Show("The directory already contains a Compatibility.txt file name.  The plugin will still be created but it may not properly include compatibility information.  Consider removing this file and re-creating the plugin.");
                }
                zip.Save(sfdPlugin.FileName);
            }

            MessageBox.Show(@"Successfully created.");

            System.Diagnostics.Process.Start(FileManager.GetDirectory(sfdPlugin.FileName));

            Close();
        }

        void UpdateToSelectedSource()
        {
            bool fromFolder = false;

            if (FromFolderRadioButton.Checked)
            {
                fromFolder = true;
            }

            FromFolderButton.Visible = fromFolder;
            FromFolderLabel.Visible = fromFolder;
            FromFolderTextBox.Visible = fromFolder;

            FromInstalledPluginComboBox.Visible = !fromFolder;


        }

        private void InstalledPluginRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            UpdateToSelectedSource();
        }

        private void FromFolderRadioButton_CheckedChanged(object sender, EventArgs e)
        {
            UpdateToSelectedSource();
        }
    }
}
