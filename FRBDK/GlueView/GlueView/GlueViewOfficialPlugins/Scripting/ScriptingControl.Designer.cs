namespace GlueViewOfficialPlugins.Scripting
{
    partial class ScriptingControl
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
            this.OutputTextBox = new System.Windows.Forms.RichTextBox();
            this.ViewDebugInfo = new System.Windows.Forms.Button();
            this.ReplTextBox = new System.Windows.Forms.TextBox();
            this.BottomPanel = new System.Windows.Forms.Panel();
            this.EnabledCheckBox = new System.Windows.Forms.CheckBox();
            this.TopPanel = new System.Windows.Forms.Panel();
            this.EditInWindowButton = new System.Windows.Forms.Button();
            this.BottomPanel.SuspendLayout();
            this.TopPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // OutputTextBox
            // 
            this.OutputTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.OutputTextBox.Location = new System.Drawing.Point(0, 26);
            this.OutputTextBox.Name = "OutputTextBox";
            this.OutputTextBox.Size = new System.Drawing.Size(357, 265);
            this.OutputTextBox.TabIndex = 0;
            this.OutputTextBox.Text = "";
            // 
            // ViewDebugInfo
            // 
            this.ViewDebugInfo.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ViewDebugInfo.Location = new System.Drawing.Point(74, 0);
            this.ViewDebugInfo.Name = "ViewDebugInfo";
            this.ViewDebugInfo.Size = new System.Drawing.Size(280, 23);
            this.ViewDebugInfo.TabIndex = 1;
            this.ViewDebugInfo.Text = "View Debug Info";
            this.ViewDebugInfo.UseVisualStyleBackColor = true;
            this.ViewDebugInfo.Click += new System.EventHandler(this.button1_Click);
            // 
            // ReplTextBox
            // 
            this.ReplTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ReplTextBox.Location = new System.Drawing.Point(0, 3);
            this.ReplTextBox.Name = "ReplTextBox";
            this.ReplTextBox.Size = new System.Drawing.Size(299, 20);
            this.ReplTextBox.TabIndex = 2;
            this.ReplTextBox.KeyDown += new System.Windows.Forms.KeyEventHandler(this.ReplTextBox_KeyDown);
            this.ReplTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.ReplTextBox_KeyPress);
            // 
            // BottomPanel
            // 
            this.BottomPanel.Controls.Add(this.EnabledCheckBox);
            this.BottomPanel.Controls.Add(this.ViewDebugInfo);
            this.BottomPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.BottomPanel.Location = new System.Drawing.Point(0, 291);
            this.BottomPanel.Name = "BottomPanel";
            this.BottomPanel.Size = new System.Drawing.Size(357, 24);
            this.BottomPanel.TabIndex = 3;
            // 
            // EnabledCheckBox
            // 
            this.EnabledCheckBox.AutoSize = true;
            this.EnabledCheckBox.Checked = true;
            this.EnabledCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.EnabledCheckBox.Location = new System.Drawing.Point(3, 6);
            this.EnabledCheckBox.Name = "EnabledCheckBox";
            this.EnabledCheckBox.Size = new System.Drawing.Size(65, 17);
            this.EnabledCheckBox.TabIndex = 2;
            this.EnabledCheckBox.Text = "Enabled";
            this.EnabledCheckBox.UseVisualStyleBackColor = true;
            this.EnabledCheckBox.CheckedChanged += new System.EventHandler(this.EnabledCheckBox_CheckedChanged);
            // 
            // TopPanel
            // 
            this.TopPanel.Controls.Add(this.EditInWindowButton);
            this.TopPanel.Controls.Add(this.ReplTextBox);
            this.TopPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.TopPanel.Location = new System.Drawing.Point(0, 0);
            this.TopPanel.Name = "TopPanel";
            this.TopPanel.Size = new System.Drawing.Size(357, 26);
            this.TopPanel.TabIndex = 4;
            // 
            // EditInWindowButton
            // 
            this.EditInWindowButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.EditInWindowButton.Location = new System.Drawing.Point(305, 1);
            this.EditInWindowButton.Name = "EditInWindowButton";
            this.EditInWindowButton.Size = new System.Drawing.Size(52, 23);
            this.EditInWindowButton.TabIndex = 3;
            this.EditInWindowButton.Text = "Edit";
            this.EditInWindowButton.UseVisualStyleBackColor = true;
            this.EditInWindowButton.Click += new System.EventHandler(this.EditInWindowButton_Click);
            // 
            // ScriptingControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.OutputTextBox);
            this.Controls.Add(this.TopPanel);
            this.Controls.Add(this.BottomPanel);
            this.Name = "ScriptingControl";
            this.Size = new System.Drawing.Size(357, 315);
            this.BottomPanel.ResumeLayout(false);
            this.BottomPanel.PerformLayout();
            this.TopPanel.ResumeLayout(false);
            this.TopPanel.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox OutputTextBox;
        private System.Windows.Forms.Button ViewDebugInfo;
        private System.Windows.Forms.TextBox ReplTextBox;
        private System.Windows.Forms.Panel BottomPanel;
        private System.Windows.Forms.CheckBox EnabledCheckBox;
        private System.Windows.Forms.Panel TopPanel;
        private System.Windows.Forms.Button EditInWindowButton;
    }
}
