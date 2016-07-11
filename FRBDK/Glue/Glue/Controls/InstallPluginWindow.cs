using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Forms;
using FlatRedBall.IO;
using Ionic.Zip;

namespace FlatRedBall.Glue.Controls
{
    public enum InstallationType
    {
        ForUser,
        ForCurrentProject
    }


    public partial class InstallPluginWindow : Form
    {
        public InstallPluginWindow()
        {
            InitializeComponent();
        }

        private void btnPath_Click(object sender, EventArgs e)
        {
            if(ofdPlugin.ShowDialog() == DialogResult.OK)
            {
                tbPath.Text = ofdPlugin.FileName;
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {

            string installationTypeAsString = cbInstallType.Text;
            string localPlugFile = tbPath.Text;

            InstallationType type = InstallationType.ForUser;

            switch (installationTypeAsString)
            {
                case "For User":
                    type = InstallationType.ForUser;
                    break;
                case "For Current Project":
                    type = InstallationType.ForCurrentProject;
                    break;
            }

            InstallPlugin(type, localPlugFile);
            Close();

        }

        public static bool InstallPlugin(InstallationType installationType, string localPlugFile)
        {
            bool succeeded = true;


            string installPath = null;
            //Validate install path
            switch (installationType)
            {
                case InstallationType.ForUser:
                    // We're now going to install to a temporary location and copy those files
                    // to their final location on a restart.

                    //installPath = FileManager.UserApplicationData + @"\FRBDK\Plugins\";
                    installPath = FileManager.UserApplicationDataForThisApplication + "InstalledPlugins\\";

                    break;
                case InstallationType.ForCurrentProject:
                    if (ProjectManager.GlueProjectFileName == null)
                    {
                        MessageBox.Show(@"Can not select For Current Project because no project is currently open.");
                        succeeded = false;
                    }

                    if (succeeded)
                    {
                        Directory.CreateDirectory(FileManager.GetDirectory(ProjectManager.GlueProjectFileName) + "Plugins");

                        installPath = FileManager.GetDirectory(ProjectManager.GlueProjectFileName) + "Plugins";
                    }
                    break;
                default:
                    MessageBox.Show(@"Unknown install type.  Please select a valid install type.");
                    succeeded = false;
                    break;
            }

            if (succeeded)
            {
                //Validate plugin file
                if (!File.Exists(localPlugFile))
                {
                    MessageBox.Show(@"Please select a valid *.plug file to install.");
                    succeeded = false;
                }
            }

            if (succeeded)
            {
                //Do install
                using (var zip = new ZipFile(localPlugFile))
                {
                    var rootDirectory = GetRootDirectory(zip.EntryFileNames);

                    //Only allow one folder in zip
                    if (String.IsNullOrEmpty(rootDirectory))
                    {
                        MessageBox.Show(@"Unexpected *.plug format (No root directory found in plugin archive)");
                        succeeded = false;
                    }

                    if (succeeded)
                    {

                        //Delete existing folder
                        if (Directory.Exists(installPath + @"\" + rootDirectory))
                        {
                            Plugins.PluginManager.ReceiveOutput("Plugin file already exists: " + installPath + @"\" + rootDirectory);
                            DialogResult result = MessageBox.Show(@"Existing plugin already exists!  Do you want to replace it?", @"Confirm delete", MessageBoxButtons.YesNo);

                            if (result == DialogResult.Yes)
                            {
                                try
                                {
                                    FileManager.DeleteDirectory(installPath + rootDirectory);
                                }
                                catch (Exception exc)
                                {
                                    MessageBox.Show("Error trying to delete " + installPath + @"\" + rootDirectory + "\n\n" + exc.ToString());
                                    succeeded = false;
                                }
                            }
                            else
                            {
                                succeeded = false;
                            }
                        }

                        if (succeeded)
                        {
                            //Extract into install path
                            zip.ExtractAll(installPath);

                            Plugins.PluginManager.ReceiveOutput("Installed to " + installPath);

                            MessageBox.Show(@"Successfully installed.  Restart Glue to use the new plugin.");
                        }
                        else
                        {
                            MessageBox.Show("Failed to install plugin.");

                        }
                    }
                }
            }

            return succeeded;
        }

        private static string GetRootDirectory(IEnumerable<string> entryFileNames)
        {
            string currentRootDirectory = null;

            foreach (var entryFileName in entryFileNames)
            {
                if(currentRootDirectory == null && !String.IsNullOrEmpty(GetBaseFolder(entryFileName)))
                {
                    currentRootDirectory = GetBaseFolder(entryFileName);
                }

                // August 18th, 2012
                // Commented the if statements out as this was always causing the method to always 
                //   return null if the first entryFileName was the parent directory, thus never getting
                //   to evaluate the actual code files in the plugin.
                //   -- KallDrexx

                //else if(String.IsNullOrEmpty(Directory.GetDirectoryRoot(entryFileName)))
                //{
                //    return null;
                //}
                //else if (GetBaseFolder(entryFileName) != currentRootDirectory)
                //{
                //    return null;
                //}
            }

            return currentRootDirectory;
        }

        private static string GetBaseFolder(string fileName)
        {
            var dirInfo = FileManager.GetDirectory(fileName, RelativeType.Relative);

            while (!String.IsNullOrEmpty(FileManager.GetDirectory(dirInfo, RelativeType.Relative)))
            {
                dirInfo = FileManager.GetDirectory(dirInfo, RelativeType.Relative);
            }

            return dirInfo;
        }
    }
}
