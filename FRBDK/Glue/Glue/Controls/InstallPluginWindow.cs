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

            Plugins.PluginManager.InstallPlugin(type, localPlugFile);
            Close();

        }

    }
}
