namespace PluginTestbed.SourceSetup
{
    partial class SourceSetupPluginForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tbEnginePath = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.btnEnginePath = new System.Windows.Forms.Button();
            this.cbUseSource = new System.Windows.Forms.CheckBox();
            this.btnSetupSource = new System.Windows.Forms.Button();
            this.fbdSourceSetup = new System.Windows.Forms.FolderBrowserDialog();
            this.lblLog = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tbEnginePath
            // 
            this.tbEnginePath.Location = new System.Drawing.Point(87, 9);
            this.tbEnginePath.Name = "tbEnginePath";
            this.tbEnginePath.Size = new System.Drawing.Size(255, 20);
            this.tbEnginePath.TabIndex = 0;
            this.tbEnginePath.TextChanged += new System.EventHandler(this.tbEnginePath_TextChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Engine Path:";
            // 
            // btnEnginePath
            // 
            this.btnEnginePath.Location = new System.Drawing.Point(348, 9);
            this.btnEnginePath.Name = "btnEnginePath";
            this.btnEnginePath.Size = new System.Drawing.Size(24, 19);
            this.btnEnginePath.TabIndex = 2;
            this.btnEnginePath.Text = "...";
            this.btnEnginePath.UseVisualStyleBackColor = true;
            this.btnEnginePath.Click += new System.EventHandler(this.btnEnginePath_Click);
            // 
            // cbUseSource
            // 
            this.cbUseSource.AutoSize = true;
            this.cbUseSource.Location = new System.Drawing.Point(290, 39);
            this.cbUseSource.Name = "cbUseSource";
            this.cbUseSource.Size = new System.Drawing.Size(82, 17);
            this.cbUseSource.TabIndex = 3;
            this.cbUseSource.Text = "Use Source";
            this.cbUseSource.UseVisualStyleBackColor = true;
            this.cbUseSource.CheckedChanged += new System.EventHandler(this.cbUseSource_CheckedChanged);
            // 
            // btnSetupSource
            // 
            this.btnSetupSource.Location = new System.Drawing.Point(12, 35);
            this.btnSetupSource.Name = "btnSetupSource";
            this.btnSetupSource.Size = new System.Drawing.Size(153, 23);
            this.btnSetupSource.TabIndex = 4;
            this.btnSetupSource.Text = "Setup Source for Projects";
            this.btnSetupSource.UseVisualStyleBackColor = true;
            this.btnSetupSource.Click += new System.EventHandler(this.btnSetupSource_Click);
            // 
            // fbdSourceSetup
            // 
            this.fbdSourceSetup.RootFolder = System.Environment.SpecialFolder.MyComputer;
            // 
            // lblLog
            // 
            this.lblLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lblLog.Location = new System.Drawing.Point(12, 64);
            this.lblLog.Name = "lblLog";
            this.lblLog.Size = new System.Drawing.Size(366, 14);
            this.lblLog.TabIndex = 5;
            // 
            // SourceSetupPluginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(390, 87);
            this.Controls.Add(this.lblLog);
            this.Controls.Add(this.btnSetupSource);
            this.Controls.Add(this.cbUseSource);
            this.Controls.Add(this.btnEnginePath);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbEnginePath);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SourceSetupPluginForm";
            this.ShowInTaskbar = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Source Setup";
            this.Load += new System.EventHandler(this.SourceSetupPluginForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox tbEnginePath;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnEnginePath;
        private System.Windows.Forms.CheckBox cbUseSource;
        private System.Windows.Forms.Button btnSetupSource;
        private System.Windows.Forms.FolderBrowserDialog fbdSourceSetup;
        private System.Windows.Forms.Label lblLog;
    }
}