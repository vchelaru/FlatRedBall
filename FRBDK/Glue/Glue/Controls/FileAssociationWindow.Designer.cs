namespace FlatRedBall.Glue.Controls
{
	partial class FileAssociationWindow
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
            this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
            this.OkButton = new System.Windows.Forms.Button();
            this.MakeRelativeToProjectButton = new System.Windows.Forms.Button();
            this.MakeAbsoluteButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // propertyGrid1
            // 
            this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.propertyGrid1.Location = new System.Drawing.Point(0, 0);
            this.propertyGrid1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.propertyGrid1.Name = "propertyGrid1";
            this.propertyGrid1.Size = new System.Drawing.Size(490, 600);
            this.propertyGrid1.TabIndex = 0;
            this.propertyGrid1.SelectedGridItemChanged += new System.Windows.Forms.SelectedGridItemChangedEventHandler(this.propertyGrid1_SelectedGridItemChanged);
            // 
            // OkButton
            // 
            this.OkButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.OkButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.OkButton.Location = new System.Drawing.Point(393, 701);
            this.OkButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.OkButton.Name = "OkButton";
            this.OkButton.Size = new System.Drawing.Size(88, 27);
            this.OkButton.TabIndex = 1;
            this.OkButton.Text = "Close";
            this.OkButton.UseVisualStyleBackColor = true;
            this.OkButton.Click += new System.EventHandler(this.OkButton_Click);
            // 
            // MakeRelativeToProjectButton
            // 
            this.MakeRelativeToProjectButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.MakeRelativeToProjectButton.Location = new System.Drawing.Point(18, 660);
            this.MakeRelativeToProjectButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MakeRelativeToProjectButton.Name = "MakeRelativeToProjectButton";
            this.MakeRelativeToProjectButton.Size = new System.Drawing.Size(261, 27);
            this.MakeRelativeToProjectButton.TabIndex = 2;
            this.MakeRelativeToProjectButton.Text = "Make Relative (..\\..\\program.exe)";
            this.MakeRelativeToProjectButton.UseVisualStyleBackColor = true;
            this.MakeRelativeToProjectButton.Click += new System.EventHandler(this.MakeRelativeToProjectButton_Click);
            // 
            // MakeAbsoluteButton
            // 
            this.MakeAbsoluteButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.MakeAbsoluteButton.Location = new System.Drawing.Point(18, 626);
            this.MakeAbsoluteButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MakeAbsoluteButton.Name = "MakeAbsoluteButton";
            this.MakeAbsoluteButton.Size = new System.Drawing.Size(261, 27);
            this.MakeAbsoluteButton.TabIndex = 3;
            this.MakeAbsoluteButton.Text = "Make Absolute (c:\\program.exe)";
            this.MakeAbsoluteButton.UseVisualStyleBackColor = true;
            this.MakeAbsoluteButton.Click += new System.EventHandler(this.MakeAbsoluteButton_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 608);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(228, 15);
            this.label1.TabIndex = 4;
            this.label1.Text = "On the selected file association I want to...";
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.SystemColors.ActiveBorder;
            this.panel1.Location = new System.Drawing.Point(-16, 693);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(532, 1);
            this.panel1.TabIndex = 5;
            // 
            // FileAssociationWindow
            // 
            this.AcceptButton = this.OkButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.CancelButton = this.OkButton;
            this.ClientSize = new System.Drawing.Size(491, 737);
            this.ControlBox = false;
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.MakeAbsoluteButton);
            this.Controls.Add(this.MakeRelativeToProjectButton);
            this.Controls.Add(this.OkButton);
            this.Controls.Add(this.propertyGrid1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FileAssociationWindow";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "File Associations";
            this.Shown += new System.EventHandler(this.FileAssociationWindow_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PropertyGrid propertyGrid1;
        private System.Windows.Forms.Button OkButton;
        private System.Windows.Forms.Button MakeRelativeToProjectButton;
        private System.Windows.Forms.Button MakeAbsoluteButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel1;
	}
}