namespace GlueView.Forms
{
    partial class CameraControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.CameraConfigurationComboBox = new System.Windows.Forms.ComboBox();
            this.ToOriginButton = new System.Windows.Forms.Button();
            this.FlickeringCheckBox = new System.Windows.Forms.CheckBox();
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.guidesControl1 = new GlueView.EmbeddedPlugins.CameraControlsPlugin.Controls.GuidesControl();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.CameraConfigurationComboBox);
            this.flowLayoutPanel1.Controls.Add(this.ToOriginButton);
            this.flowLayoutPanel1.Controls.Add(this.FlickeringCheckBox);
            this.flowLayoutPanel1.Controls.Add(this.elementHost1);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(226, 122);
            this.flowLayoutPanel1.TabIndex = 0;
            // 
            // CameraConfigurationComboBox
            // 
            this.CameraConfigurationComboBox.FormattingEnabled = true;
            this.CameraConfigurationComboBox.Location = new System.Drawing.Point(3, 3);
            this.CameraConfigurationComboBox.Name = "CameraConfigurationComboBox";
            this.CameraConfigurationComboBox.Size = new System.Drawing.Size(220, 21);
            this.CameraConfigurationComboBox.TabIndex = 0;
            this.CameraConfigurationComboBox.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // ToOriginButton
            // 
            this.ToOriginButton.Location = new System.Drawing.Point(3, 30);
            this.ToOriginButton.Name = "ToOriginButton";
            this.ToOriginButton.Size = new System.Drawing.Size(103, 23);
            this.ToOriginButton.TabIndex = 2;
            this.ToOriginButton.Text = "Move to origin";
            this.ToOriginButton.UseVisualStyleBackColor = true;
            this.ToOriginButton.Click += new System.EventHandler(this.ToOriginButton_Click);
            // 
            // FlickeringCheckBox
            // 
            this.FlickeringCheckBox.AutoSize = true;
            this.FlickeringCheckBox.Checked = true;
            this.FlickeringCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.FlickeringCheckBox.Location = new System.Drawing.Point(112, 30);
            this.FlickeringCheckBox.Name = "FlickeringCheckBox";
            this.FlickeringCheckBox.Size = new System.Drawing.Size(111, 17);
            this.FlickeringCheckBox.TabIndex = 1;
            this.FlickeringCheckBox.Text = "Same Z Flickering";
            this.FlickeringCheckBox.UseVisualStyleBackColor = true;
            this.FlickeringCheckBox.CheckedChanged += new System.EventHandler(this.FlickeringCheckBox_CheckedChanged);
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.propertyGrid1.HelpVisible = false;
            this.propertyGrid1.Location = new System.Drawing.Point(0, 122);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(226, 96);
            this.propertyGrid1.TabIndex = 3;
            this.propertyGrid1.ToolbarVisible = false;
            // 
            // elementHost1
            // 
            this.elementHost1.Location = new System.Drawing.Point(3, 59);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(220, 54);
            this.elementHost1.TabIndex = 3;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = this.guidesControl1;
            // 
            // CameraControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.propertyGrid1);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Name = "CameraControl";
            this.Size = new System.Drawing.Size(226, 218);
            this.Load += new System.EventHandler(this.CameraControl_Load);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.ComboBox CameraConfigurationComboBox;
        private System.Windows.Forms.CheckBox FlickeringCheckBox;
        private System.Windows.Forms.Button ToOriginButton;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
        private EmbeddedPlugins.CameraControlsPlugin.Controls.GuidesControl guidesControl1;
    }
}
