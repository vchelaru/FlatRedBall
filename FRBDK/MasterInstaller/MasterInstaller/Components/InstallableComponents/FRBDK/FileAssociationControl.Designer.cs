namespace MasterInstaller.Components.InstallableComponents.FRBDK
{
    partial class FileAssociationControl
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
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.installationTopBar1 = new MasterInstaller.Controls.InstallationTopBar();
            this.SuspendLayout();
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Checked = true;
            this.checkBox1.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBox1.Location = new System.Drawing.Point(29, 72);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(160, 17);
            this.checkBox1.TabIndex = 0;
            this.checkBox1.Text = "Set FRBDK File Assocations";
            this.checkBox1.UseVisualStyleBackColor = true;
            // 
            // installationTopBar1
            // 
            this.installationTopBar1.Location = new System.Drawing.Point(0, 0);
            this.installationTopBar1.Name = "installationTopBar1";
            this.installationTopBar1.Size = new System.Drawing.Size(528, 100);
            this.installationTopBar1.TabIndex = 1;
            // 
            // FileAssociationControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.checkBox1);
            this.Controls.Add(this.installationTopBar1);
            this.Name = "FileAssociationControl";
            this.Size = new System.Drawing.Size(528, 109);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox checkBox1;
        private Controls.InstallationTopBar installationTopBar1;
    }
}
