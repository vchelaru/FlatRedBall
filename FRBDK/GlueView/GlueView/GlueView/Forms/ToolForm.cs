using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GlueView.Forms;
using FlatRedBall.Glue.Controls;
using GlueView.Plugin;
using FlatRedBall.IO;
using System.Diagnostics;
using GlueView.Facades;

namespace GlueView
{
    public partial class ToolForm : Form
    {
        public event EventHandler ItemCollapsedOrExpanded;

        public ToolForm()
        {
            InitializeComponent();

            this.collapsibleContainerStrip1.ItemCollapsedOrExpanded += (not, used) => ItemCollapsedOrExpanded?.Invoke(this, null);

            CollapsibleFormHelper.Self.Initialize(this.collapsibleContainerStrip1);
        }

        private void managePluginsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //PluginsWindow pluginsWindow = new PluginsWindow();
            //pluginsWindow.Show();
        }

        private void viewPluginCompileErrorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string toSave = "";

            foreach(string error in PluginManager.GlobalInstance.CompileOutput)
            {
                toSave += error + "\r\n";
            }

            string fileName = FileManager.UserApplicationDataForThisApplication + "PluginCompileErrors.txt";
            FileManager.SaveText(toSave, fileName);

            Process.Start(fileName);
        
        }

        private void ToolForm_Load(object sender, EventArgs e)
        {
        }

        public void Minimize()
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var assembly = this.GetType().Assembly;
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            MessageBox.Show($"Glue Preview {version}");
        }
    }
}
