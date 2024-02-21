using System;
using System.Collections.Generic;
using System.Windows.Forms;
using L = Localization;

namespace FlatRedBall.Glue.Controls
{
    public enum InstallationType
    {
        ForUser,
        ForCurrentProject
    }


    public partial class InstallPluginWindow
    {
        public InstallPluginWindow()
        {
            InitializeComponent();

            InstallTypeComboBox.SelectionChanged += (sender, args) =>
            {
                var data = ((KeyValuePair<InstallationType, string>)args.AddedItems[0]!);
                InstallationType = data.Key;
            };
            InstallTypeComboBox.DisplayMemberPath = "Value";
            InstallTypeComboBox.SelectedValuePath = "Key";
            InstallTypeComboBox.Items.Add(new KeyValuePair<InstallationType, string>(InstallationType.ForUser, L.Texts.UserFor));
            InstallTypeComboBox.Items.Add(new KeyValuePair<InstallationType, string>(InstallationType.ForCurrentProject, L.Texts.ProjectForCurrent));
            InstallTypeComboBox.SelectedIndex = 0;
        } 

        private InstallationType InstallationType { get; set; }

        private void BtnPath_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog()
            {
                Filter = $@"{L.Texts.PluginFiles}|*.plug" 
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                pathTextBox.Text = openFileDialog.FileName;
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrWhiteSpace(pathTextBox.Text))
                return;

            Plugins.PluginManager.InstallPlugin(InstallationType, pathTextBox.Text);
            Close();

        }
    }
}