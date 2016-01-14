namespace GlueViewTestPlugins.EntityControl
{
	partial class EntityControlControls
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
			this.moveButton = new System.Windows.Forms.Button();
			this.noneButton = new System.Windows.Forms.Button();
			this.layerComboBox = new System.Windows.Forms.ComboBox();
			this.layerLabel = new System.Windows.Forms.Label();
			this.saveCheckBox = new System.Windows.Forms.CheckBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.panel2 = new System.Windows.Forms.Panel();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.propertyGrid1 = new System.Windows.Forms.PropertyGrid();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// moveButton
			// 
			this.moveButton.Location = new System.Drawing.Point(82, 3);
			this.moveButton.Name = "moveButton";
			this.moveButton.Size = new System.Drawing.Size(72, 23);
			this.moveButton.TabIndex = 4;
			this.moveButton.Text = "On";
			this.moveButton.UseVisualStyleBackColor = true;
			this.moveButton.Click += new System.EventHandler(this.moveButton_Click);
			// 
			// noneButton
			// 
			this.noneButton.Location = new System.Drawing.Point(4, 3);
			this.noneButton.Name = "noneButton";
			this.noneButton.Size = new System.Drawing.Size(72, 23);
			this.noneButton.TabIndex = 6;
			this.noneButton.Text = "Off";
			this.noneButton.UseVisualStyleBackColor = true;
			this.noneButton.Click += new System.EventHandler(this.noneButton_Click);
			// 
			// layerComboBox
			// 
			this.layerComboBox.FormattingEnabled = true;
			this.layerComboBox.Location = new System.Drawing.Point(50, 3);
			this.layerComboBox.Name = "layerComboBox";
			this.layerComboBox.Size = new System.Drawing.Size(138, 21);
			this.layerComboBox.TabIndex = 7;
			// 
			// layerLabel
			// 
			this.layerLabel.AutoSize = true;
			this.layerLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
			this.layerLabel.Location = new System.Drawing.Point(1, 4);
			this.layerLabel.Name = "layerLabel";
			this.layerLabel.Size = new System.Drawing.Size(44, 17);
			this.layerLabel.TabIndex = 8;
			this.layerLabel.Text = "Layer";
			// 
			// saveCheckBox
			// 
			this.saveCheckBox.AutoSize = true;
			this.saveCheckBox.Checked = true;
			this.saveCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
			this.saveCheckBox.Location = new System.Drawing.Point(3, 293);
			this.saveCheckBox.Name = "saveCheckBox";
			this.saveCheckBox.Size = new System.Drawing.Size(76, 17);
			this.saveCheckBox.TabIndex = 15;
			this.saveCheckBox.Text = "Auto Save";
			this.saveCheckBox.UseVisualStyleBackColor = true;
			this.saveCheckBox.CheckedChanged += new System.EventHandler(this.saveCheckBox_CheckedChanged);
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.noneButton);
			this.panel1.Controls.Add(this.moveButton);
			this.panel1.Location = new System.Drawing.Point(3, 3);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(220, 30);
			this.panel1.TabIndex = 18;
			// 
			// panel2
			// 
			this.panel2.Controls.Add(this.layerComboBox);
			this.panel2.Controls.Add(this.layerLabel);
			this.panel2.Location = new System.Drawing.Point(3, 39);
			this.panel2.Name = "panel2";
			this.panel2.Size = new System.Drawing.Size(220, 27);
			this.panel2.TabIndex = 19;
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Controls.Add(this.panel1);
			this.flowLayoutPanel1.Controls.Add(this.panel2);
			this.flowLayoutPanel1.Controls.Add(this.propertyGrid1);
			this.flowLayoutPanel1.Controls.Add(this.saveCheckBox);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(220, 328);
			this.flowLayoutPanel1.TabIndex = 20;
			// 
			// propertyGrid1
			// 
			this.propertyGrid1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.propertyGrid1.HelpVisible = false;
			this.propertyGrid1.Location = new System.Drawing.Point(3, 72);
			this.propertyGrid1.Margin = new System.Windows.Forms.Padding(3, 3, 10, 3);
			this.propertyGrid1.Name = "propertyGrid1";
			this.propertyGrid1.Size = new System.Drawing.Size(213, 215);
			this.propertyGrid1.TabIndex = 20;
			this.propertyGrid1.ToolbarVisible = false;
			// 
			// EntityControlControls
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.flowLayoutPanel1);
			this.Name = "EntityControlControls";
			this.Size = new System.Drawing.Size(220, 328);
			this.panel1.ResumeLayout(false);
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

        private System.Windows.Forms.Button moveButton;
		private System.Windows.Forms.Button noneButton;
		private System.Windows.Forms.ComboBox layerComboBox;
		private System.Windows.Forms.Label layerLabel;
		private System.Windows.Forms.CheckBox saveCheckBox;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.PropertyGrid propertyGrid1;
	}
}
