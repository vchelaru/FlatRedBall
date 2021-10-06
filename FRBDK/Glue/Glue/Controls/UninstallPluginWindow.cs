using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Controls
{
    public partial class UninstallPluginWindow : Form
    {
        public static string UninstallPluginFile
        {
            get
            {
                return FileManager.UserApplicationData + "FRBDK/Plugins/Uninstall.txt";
            }
        }

        private List<UninstallPlugin> databoundList;

        public UninstallPluginWindow()
        {
            InitializeComponent();
        }

        private void UninstallPluginWindow_Load(object sender, EventArgs e)
        {
            var UserPath = FileManager.UserApplicationData + "FRBDK/Plugins";

            databoundList = new List<UninstallPlugin>();

            if (Directory.Exists(UserPath) && Directory.GetDirectories(UserPath).Count() > 0)
            {
                databoundList.AddRange(Directory.GetDirectories(UserPath).Select(plugin => new UninstallPlugin
                                                                                         {
                                                                                             Name =
                                                                                                 new DirectoryInfo(
                                                                                                 plugin).Name,
                                                                                             Type = "User",
                                                                                             FolderPath = plugin
                                                                                         }).ToList());
            }

            if (GlueState.Self.GlueProjectFileName != null)
            {
                var projectPath = FileManager.GetDirectory(GlueState.Self.GlueProjectFileName) + "Plugins";

                if (Directory.Exists(projectPath) && Directory.GetDirectories(projectPath).Count() > 0)
                {
                    databoundList.AddRange(Directory.GetDirectories(projectPath).Select(plugin => new UninstallPlugin
                                                                                                {
                                                                                                    Name =
                                                                                                        new DirectoryInfo
                                                                                                        (plugin).Name,
                                                                                                    Type = "User",
                                                                                                    FolderPath = plugin
                                                                                                }));
                }
            }

            dgvPlugins.DataSource = databoundList;
        }

        private void btnUninstall_Click(object sender, EventArgs e)
        {
            if (dgvPlugins.CurrentRow == null)
            {
                MessageBox.Show(@"Please select a plugin to install.");
                return;
            }

            var plugin = (UninstallPlugin)dgvPlugins.CurrentRow.DataBoundItem;

            if (!Uninstall(plugin.FolderPath)) return;

            databoundList.Remove(plugin);
            dgvPlugins.DataSource = null;
            dgvPlugins.DataSource = databoundList;
            MessageBox.Show(@"Plugin " + plugin.Name + @" successfully uninstalled.");
        }

        private bool Uninstall(string plugin)
        {
            try
            {
                Directory.Delete(plugin, true);
                return true;
            }
            catch (Exception ex)
            {
                using (StreamWriter w = File.AppendText(UninstallPluginFile))
                {
                    w.WriteLine(plugin);
                }

                MessageBox.Show(@"Failed to uninstall plugin.  Plugin will be uninstalled on next Glue start.");

                return false;
            }
        }
    }

    internal class UninstallPlugin
    {
        public string Name { get; set; }

        public string Type { get; set; }

        [Browsable(false)]
        public string FolderPath { get; set; }
    }
}
