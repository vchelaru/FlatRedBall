using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.EmbeddedPlugins.CameraPlugin;

namespace FlatRedBall.Glue.Controls
{
    public partial class CameraSettingsWindow : Form
    {
        bool isInitialized = false;
        public CameraSettingsWindow()
        {
            InitializeComponent();

            if (ProjectManager.GlueProjectSave != null)
            {
                cbSetResolution.Checked = ProjectManager.GlueProjectSave.SetResolution;
                ApplyToNonPCCheckBox.Enabled = ProjectManager.GlueProjectSave.SetResolution;
                ApplyToNonPCCheckBox.Checked = ProjectManager.GlueProjectSave.ApplyToFixedResolutionPlatforms;

                cbSetOrthResolution.Checked = ProjectManager.GlueProjectSave.SetOrthogonalResolution;

                RunFullscreenCheckBox.Checked = ProjectManager.GlueProjectSave.RunFullscreen;

                // These should always be enabled because they're used when making
                // the .scnx files
                //this.textBox1.Enabled = checkBox1.Checked;
                //this.textBox2.Enabled = checkBox1.Checked;

                this.tbResWidth.Text = ProjectManager.GlueProjectSave.ResolutionWidth.ToString();
                this.tbResHeight.Text = ProjectManager.GlueProjectSave.ResolutionHeight.ToString();

                this.tbOrthWidth.Text = ProjectManager.GlueProjectSave.OrthogonalWidth.ToString();
                this.tbOrthHeight.Text = ProjectManager.GlueProjectSave.OrthogonalHeight.ToString();

                // call this LAST because this triggers an event
                cbIs2D.Text = ProjectManager.GlueProjectSave.In2D.ToString();
                isInitialized = true;

                RefreshPresetTreeView();
            }
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void cbIs2D_SelectedIndexChanged(object sender, EventArgs e)
        {
            gbOrth.Visible = (cbIs2D.Text == "True");

			UpdateGluxCameraSettings();
        }

        private void cbIs2D_CheckedChanged(object sender, EventArgs e)
        {
            // We want to keep these enabled no matter what because
            // they will be used to set the .scnx settings
            //this.textBox1.Enabled = checkBox1.Checked;
            //this.textBox2.Enabled = checkBox1.Checked;

			UpdateGluxCameraSettings();

            ApplyToNonPCCheckBox.Enabled = cbSetResolution.Checked;
        }

		private void UpdateGluxCameraSettings()
		{
            if (!isInitialized)
            {
                return;
            }

			if (cbIs2D.Text == "True")
			{
				ProjectManager.GlueProjectSave.In2D = true;
			}
			else
			{
				ProjectManager.GlueProjectSave.In2D = false;
			}

			ProjectManager.GlueProjectSave.SetResolution = cbSetResolution.Checked;

			int desiredWidth = 0;
			int desiredHeight = 0;

			try
			{
				desiredWidth = int.Parse(tbResWidth.Text);
			}
			catch
			{
				tbResWidth.Text = ProjectManager.GlueProjectSave.ResolutionWidth.ToString();
				desiredWidth = ProjectManager.GlueProjectSave.ResolutionWidth;
				System.Windows.Forms.MessageBox.Show("Invalid width");
			}

			try
			{
				desiredHeight = int.Parse(tbResHeight.Text);
			}
			catch
			{
				tbResHeight.Text = ProjectManager.GlueProjectSave.ResolutionHeight.ToString();
				desiredHeight = ProjectManager.GlueProjectSave.ResolutionHeight;
				System.Windows.Forms.MessageBox.Show("Invalid height");
			}

			ProjectManager.GlueProjectSave.ResolutionWidth = desiredWidth;
			ProjectManager.GlueProjectSave.ResolutionHeight = desiredHeight;

            if (ProjectManager.GlueProjectSave.In2D)
            {
                ProjectManager.GlueProjectSave.SetOrthogonalResolution = cbSetOrthResolution.Checked;

                int desiredOrthWidth = 0;
                int desiredOrthHeight = 0;

                try
                {
                    desiredOrthWidth = int.Parse(tbOrthWidth.Text);
                }
                catch
                {
                    tbOrthWidth.Text = ProjectManager.GlueProjectSave.OrthogonalWidth.ToString();
                    desiredOrthWidth = ProjectManager.GlueProjectSave.OrthogonalWidth;
                    System.Windows.Forms.MessageBox.Show("Invalid Orthogonal width");
                }

                try
                {
                    desiredOrthHeight = int.Parse(tbOrthHeight.Text);
                }
                catch
                {
                    tbOrthHeight.Text = ProjectManager.GlueProjectSave.OrthogonalHeight.ToString();
                    desiredOrthHeight = ProjectManager.GlueProjectSave.OrthogonalHeight;
                    System.Windows.Forms.MessageBox.Show("Invalid Orthogonal height");
                }

                ProjectManager.GlueProjectSave.OrthogonalWidth = desiredOrthWidth;
                ProjectManager.GlueProjectSave.OrthogonalHeight = desiredOrthHeight;
            }

            ProjectManager.GlueProjectSave.RunFullscreen = RunFullscreenCheckBox.Checked;

            ProjectManager.GlueProjectSave.ApplyToFixedResolutionPlatforms = ApplyToNonPCCheckBox.Checked;

			CameraSetupCodeGenerator.UpdateOrAddCameraSetup();

            //bool whetherToCall = ProjectManager.GlueProjectSave.SetResolution || ProjectManager.GlueProjectSave.In2D;
            // I think we should alway call.
            // Even in 3D the user should be able
            // to set up the Camera in Glue.
            bool whetherToCall = true;

            CameraSetupCodeGenerator.GenerateCallInGame1(ProjectManager.GameClassFileName, whetherToCall);

            GluxCommands.Self.SaveProjectAndElements();
            GlueCommands.Self.ProjectCommands.SaveProjects();
        }

        //private void TextChanged(object sender, EventArgs e)
        //{

        //}

        private void cbSetOrthResolution_CheckedChanged(object sender, EventArgs e)
        {
            tbOrthWidth.Enabled = cbSetOrthResolution.Checked;
            tbOrthHeight.Enabled = cbSetOrthResolution.Checked;

            UpdateGluxCameraSettings();
        }

        private void AddResolutionButton_Click(object sender, EventArgs e)
        {
            TextInputWindow tiw = new TextInputWindow();
            tiw.DisplayText = "Enter name for the new resolution preset";
            if (tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string whyIsntValid = WhyIsntResolutionPresetNameValid(tiw.Result);

                if (!string.IsNullOrEmpty(whyIsntValid))
                {
                    MessageBox.Show(whyIsntValid);
                }
                else
                {
                    GlueProjectSave glueProject = ObjectFinder.Self.GlueProject;

                    int width = 0;
                    if (int.TryParse(this.tbResWidth.Text, out width) == false)
                    {
                        width = 0;
                    }

                    int height = 0;

                    if (int.TryParse(this.tbResHeight.Text, out height) == false)
                    {
                        height = 0;
                    }

                    ResolutionValues resolutionValues = new ResolutionValues();
                    resolutionValues.Width = width;
                    resolutionValues.Height = height;
                    resolutionValues.Name = tiw.Result;

                    glueProject.ResolutionPresets.Add(resolutionValues);

                    GluxCommands.Self.SaveProjectAndElements();
                    RefreshPresetTreeView();
                }
            }
        }

        string WhyIsntResolutionPresetNameValid(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
            {
                return "The preset name can't be empty";
            }
            else if (ObjectFinder.Self.GlueProject.ResolutionPresets.FirstOrDefault(preset => preset.Name == presetName) != null)
            {
                return "There is already a preset named " + presetName;
            }
            else
            {
                return null;
            }
        }

        void RefreshPresetTreeView()
        {
            this.PresetsTreeView.Nodes.Clear();
            foreach (var preset in ObjectFinder.Self.GlueProject.ResolutionPresets)
            {
                TreeNode treeNode = new TreeNode(preset.Name);
                treeNode.Tag = preset;
                this.PresetsTreeView.Nodes.Add(treeNode);
            }

        }

        private void PresetsTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            ResolutionValues selected = null;
            if (this.PresetsTreeView.SelectedNode != null)
            {
                selected = PresetsTreeView.SelectedNode.Tag as ResolutionValues;
            }
            if (selected != null)
            {
                this.tbResWidth.Text = selected.Width.ToString() ;
                this.tbResHeight.Text = selected.Height.ToString();

                UpdateGluxCameraSettings();

            }
        }


        private void PresetsTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                if (this.PresetsTreeView.SelectedNode != null)
                {
                    ResolutionValues values = PresetsTreeView.SelectedNode.Tag as ResolutionValues;

                    PresetsTreeView.Nodes.Remove(PresetsTreeView.SelectedNode);

                    ObjectFinder.Self.GlueProject.ResolutionPresets.Remove(values);
                    GluxCommands.Self.SaveProjectAndElements();
                }
            }
        }

        private void TextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter ||
                e.KeyCode == Keys.Return)
            {
                ((TextBox)sender).Enabled = false;
                ((TextBox)sender).Enabled = true;
                e.Handled = true;

                e.SuppressKeyPress = true;

            }
        }

        private void TextBoxLeave(object sender, EventArgs e)
        {
            UpdateGluxCameraSettings();
        }

        private void ApplyToNonPCCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateGluxCameraSettings();
        }

        private void tbResWidth_TextChanged(object sender, EventArgs e)
        {

        }

        private void CameraSettingsWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            UpdateGluxCameraSettings();
        }

        private void RunFullscreen_CheckedChanged(object sender, EventArgs e)
        {
            UpdateGluxCameraSettings();
        }

        private void HandleUpdateToNewDisplaySettingsClicked(object sender, EventArgs e)
        {
            var project = GlueState.Self.CurrentGlueProject;

            if(project == null)
            {
                MessageBox.Show("No project loaded");
            }
            else
            {
                CameraMainPlugin.CreateGlueProjectSettingsFor(project);

                GlueCommands.Self.GluxCommands.SaveProjectAndElements();

                this.Close();

                CameraMainPlugin.Self.ShowCameraUi();
            }
        }


    }
}
