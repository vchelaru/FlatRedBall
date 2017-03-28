namespace FlatRedBall.Glue.Controls
{
	partial class AddVariableWindow
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
            this.mCancelButton = new System.Windows.Forms.Button();
            this.mOkWindow = new System.Windows.Forms.Button();
            this.ExistingVariablePanel = new System.Windows.Forms.Panel();
            this.AvailableVariablesComboBox = new System.Windows.Forms.ComboBox();
            this.label9 = new System.Windows.Forms.Label();
            this.TunnelVariablePanel = new System.Windows.Forms.Panel();
            this.label10 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.TypeConverterComboBox = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.OverridingVariableTypeComboBox = new System.Windows.Forms.ComboBox();
            this.AlternativeNameTextBox = new System.Windows.Forms.TextBox();
            this.TunnelingVariableComboBox = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.TunnelingObjectComboBox = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.radCreateNewVariable = new System.Windows.Forms.RadioButton();
            this.radTunnelVariable = new System.Windows.Forms.RadioButton();
            this.radExistingVariable = new System.Windows.Forms.RadioButton();
            this.NewVariablePanel = new System.Windows.Forms.Panel();
            this.NewTypeListBox = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.NewVariableNameTextBox = new System.Windows.Forms.TextBox();
            this.ExistingVariablePanel.SuspendLayout();
            this.TunnelVariablePanel.SuspendLayout();
            this.NewVariablePanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mCancelButton
            // 
            this.mCancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.mCancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.mCancelButton.Location = new System.Drawing.Point(302, 260);
            this.mCancelButton.Name = "mCancelButton";
            this.mCancelButton.Size = new System.Drawing.Size(70, 23);
            this.mCancelButton.TabIndex = 4;
            this.mCancelButton.Text = "Cancel";
            this.mCancelButton.UseVisualStyleBackColor = true;
            // 
            // mOkWindow
            // 
            this.mOkWindow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.mOkWindow.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.mOkWindow.Location = new System.Drawing.Point(226, 260);
            this.mOkWindow.Name = "mOkWindow";
            this.mOkWindow.Size = new System.Drawing.Size(70, 23);
            this.mOkWindow.TabIndex = 3;
            this.mOkWindow.Text = "OK";
            this.mOkWindow.UseVisualStyleBackColor = true;
            // 
            // ExistingVariablePanel
            // 
            this.ExistingVariablePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ExistingVariablePanel.Controls.Add(this.AvailableVariablesComboBox);
            this.ExistingVariablePanel.Controls.Add(this.label9);
            this.ExistingVariablePanel.Location = new System.Drawing.Point(3, 89);
            this.ExistingVariablePanel.Name = "ExistingVariablePanel";
            this.ExistingVariablePanel.Size = new System.Drawing.Size(377, 60);
            this.ExistingVariablePanel.TabIndex = 1;
            // 
            // AvailableVariablesComboBox
            // 
            this.AvailableVariablesComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AvailableVariablesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.AvailableVariablesComboBox.FormattingEnabled = true;
            this.AvailableVariablesComboBox.Location = new System.Drawing.Point(6, 16);
            this.AvailableVariablesComboBox.Name = "AvailableVariablesComboBox";
            this.AvailableVariablesComboBox.Size = new System.Drawing.Size(361, 21);
            this.AvailableVariablesComboBox.TabIndex = 0;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(3, 0);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(133, 13);
            this.label9.TabIndex = 0;
            this.label9.Text = "Select an existing variable:";
            // 
            // TunnelVariablePanel
            // 
            this.TunnelVariablePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TunnelVariablePanel.Controls.Add(this.label10);
            this.TunnelVariablePanel.Controls.Add(this.label8);
            this.TunnelVariablePanel.Controls.Add(this.label7);
            this.TunnelVariablePanel.Controls.Add(this.TypeConverterComboBox);
            this.TunnelVariablePanel.Controls.Add(this.label6);
            this.TunnelVariablePanel.Controls.Add(this.OverridingVariableTypeComboBox);
            this.TunnelVariablePanel.Controls.Add(this.AlternativeNameTextBox);
            this.TunnelVariablePanel.Controls.Add(this.TunnelingVariableComboBox);
            this.TunnelVariablePanel.Controls.Add(this.label5);
            this.TunnelVariablePanel.Controls.Add(this.TunnelingObjectComboBox);
            this.TunnelVariablePanel.Controls.Add(this.label4);
            this.TunnelVariablePanel.Location = new System.Drawing.Point(3, 89);
            this.TunnelVariablePanel.Name = "TunnelVariablePanel";
            this.TunnelVariablePanel.Size = new System.Drawing.Size(372, 162);
            this.TunnelVariablePanel.TabIndex = 2;
            this.TunnelVariablePanel.Visible = false;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(3, 63);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(95, 13);
            this.label10.TabIndex = 12;
            this.label10.Text = "Advanced Options";
            // 
            // label8
            // 
            this.label8.Location = new System.Drawing.Point(3, 137);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(91, 13);
            this.label8.TabIndex = 9;
            this.label8.Text = "Type Converter:";
            // 
            // label7
            // 
            this.label7.Location = new System.Drawing.Point(3, 110);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(91, 19);
            this.label7.TabIndex = 7;
            this.label7.Text = "Converted Type:";
            // 
            // TypeConverterComboBox
            // 
            this.TypeConverterComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TypeConverterComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TypeConverterComboBox.FormattingEnabled = true;
            this.TypeConverterComboBox.Location = new System.Drawing.Point(106, 134);
            this.TypeConverterComboBox.Name = "TypeConverterComboBox";
            this.TypeConverterComboBox.Size = new System.Drawing.Size(261, 21);
            this.TypeConverterComboBox.TabIndex = 4;
            // 
            // label6
            // 
            this.label6.Location = new System.Drawing.Point(3, 84);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(91, 13);
            this.label6.TabIndex = 4;
            this.label6.Text = "Alternative Name:";
            // 
            // OverridingVariableTypeComboBox
            // 
            this.OverridingVariableTypeComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.OverridingVariableTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.OverridingVariableTypeComboBox.FormattingEnabled = true;
            this.OverridingVariableTypeComboBox.Location = new System.Drawing.Point(106, 107);
            this.OverridingVariableTypeComboBox.Name = "OverridingVariableTypeComboBox";
            this.OverridingVariableTypeComboBox.Size = new System.Drawing.Size(261, 21);
            this.OverridingVariableTypeComboBox.TabIndex = 3;
            // 
            // AlternativeNameTextBox
            // 
            this.AlternativeNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.AlternativeNameTextBox.Location = new System.Drawing.Point(106, 81);
            this.AlternativeNameTextBox.Name = "AlternativeNameTextBox";
            this.AlternativeNameTextBox.Size = new System.Drawing.Size(261, 20);
            this.AlternativeNameTextBox.TabIndex = 2;
            // 
            // TunnelingVariableComboBox
            // 
            this.TunnelingVariableComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TunnelingVariableComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TunnelingVariableComboBox.FormattingEnabled = true;
            this.TunnelingVariableComboBox.Location = new System.Drawing.Point(53, 27);
            this.TunnelingVariableComboBox.Name = "TunnelingVariableComboBox";
            this.TunnelingVariableComboBox.Size = new System.Drawing.Size(314, 21);
            this.TunnelingVariableComboBox.TabIndex = 1;
            this.TunnelingVariableComboBox.SelectedIndexChanged += new System.EventHandler(this.TunnelingVariableComboBox_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 27);
            this.label5.Name = "label5";
            this.label5.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.label5.Size = new System.Drawing.Size(48, 18);
            this.label5.TabIndex = 2;
            this.label5.Text = "Variable:";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // TunnelingObjectComboBox
            // 
            this.TunnelingObjectComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TunnelingObjectComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.TunnelingObjectComboBox.FormattingEnabled = true;
            this.TunnelingObjectComboBox.Location = new System.Drawing.Point(53, 0);
            this.TunnelingObjectComboBox.Name = "TunnelingObjectComboBox";
            this.TunnelingObjectComboBox.Size = new System.Drawing.Size(314, 21);
            this.TunnelingObjectComboBox.TabIndex = 0;
            this.TunnelingObjectComboBox.SelectedIndexChanged += new System.EventHandler(this.TunnelingObjectComboBox_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Name = "label4";
            this.label4.Padding = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.label4.Size = new System.Drawing.Size(41, 18);
            this.label4.TabIndex = 1;
            this.label4.Text = "Object:";
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.Color.Black;
            this.panel1.Location = new System.Drawing.Point(3, 81);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(377, 2);
            this.panel1.TabIndex = 28;
            // 
            // radCreateNewVariable
            // 
            this.radCreateNewVariable.AutoSize = true;
            this.radCreateNewVariable.Location = new System.Drawing.Point(12, 58);
            this.radCreateNewVariable.Name = "radCreateNewVariable";
            this.radCreateNewVariable.Size = new System.Drawing.Size(128, 17);
            this.radCreateNewVariable.TabIndex = 7;
            this.radCreateNewVariable.Text = "Create a new variable";
            this.radCreateNewVariable.UseVisualStyleBackColor = true;
            this.radCreateNewVariable.CheckedChanged += new System.EventHandler(this.radCreateNewVariable_CheckedChanged);
            // 
            // radTunnelVariable
            // 
            this.radTunnelVariable.AutoSize = true;
            this.radTunnelVariable.Location = new System.Drawing.Point(12, 35);
            this.radTunnelVariable.Name = "radTunnelVariable";
            this.radTunnelVariable.Size = new System.Drawing.Size(209, 17);
            this.radTunnelVariable.TabIndex = 6;
            this.radTunnelVariable.Text = "Tunnel a variable in a contained object";
            this.radTunnelVariable.UseVisualStyleBackColor = true;
            this.radTunnelVariable.CheckedChanged += new System.EventHandler(this.radCreateNewVariable_CheckedChanged);
            // 
            // radExistingVariable
            // 
            this.radExistingVariable.AutoSize = true;
            this.radExistingVariable.Checked = true;
            this.radExistingVariable.Location = new System.Drawing.Point(12, 12);
            this.radExistingVariable.Name = "radExistingVariable";
            this.radExistingVariable.Size = new System.Drawing.Size(153, 17);
            this.radExistingVariable.TabIndex = 5;
            this.radExistingVariable.TabStop = true;
            this.radExistingVariable.Text = "Expose an existing variable";
            this.radExistingVariable.UseVisualStyleBackColor = true;
            this.radExistingVariable.CheckedChanged += new System.EventHandler(this.radCreateNewVariable_CheckedChanged);
            // 
            // NewVariablePanel
            // 
            this.NewVariablePanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NewVariablePanel.Controls.Add(this.NewTypeListBox);
            this.NewVariablePanel.Controls.Add(this.label2);
            this.NewVariablePanel.Controls.Add(this.label1);
            this.NewVariablePanel.Controls.Add(this.NewVariableNameTextBox);
            this.NewVariablePanel.Location = new System.Drawing.Point(3, 89);
            this.NewVariablePanel.Name = "NewVariablePanel";
            this.NewVariablePanel.Size = new System.Drawing.Size(372, 162);
            this.NewVariablePanel.TabIndex = 0;
            this.NewVariablePanel.Visible = false;
            // 
            // NewTypeListBox
            // 
            this.NewTypeListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NewTypeListBox.FormattingEnabled = true;
            this.NewTypeListBox.Location = new System.Drawing.Point(42, 0);
            this.NewTypeListBox.Name = "NewTypeListBox";
            this.NewTypeListBox.Size = new System.Drawing.Size(325, 134);
            this.NewTypeListBox.TabIndex = 20;
            this.NewTypeListBox.Click += new System.EventHandler(this.NewTypeListBox_Click);
            this.NewTypeListBox.SelectedIndexChanged += new System.EventHandler(this.NewTypeListBox_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(2, 141);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 19;
            this.label2.Text = "Name:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(2, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(34, 13);
            this.label1.TabIndex = 18;
            this.label1.Text = "Type:";
            // 
            // NewVariableNameTextBox
            // 
            this.NewVariableNameTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.NewVariableNameTextBox.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.Suggest;
            this.NewVariableNameTextBox.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.CustomSource;
            this.NewVariableNameTextBox.Location = new System.Drawing.Point(42, 138);
            this.NewVariableNameTextBox.Name = "NewVariableNameTextBox";
            this.NewVariableNameTextBox.Size = new System.Drawing.Size(325, 20);
            this.NewVariableNameTextBox.TabIndex = 1;
            // 
            // AddVariableWindow
            // 
            this.AcceptButton = this.mOkWindow;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.mCancelButton;
            this.ClientSize = new System.Drawing.Size(384, 295);
            this.Controls.Add(this.mCancelButton);
            this.Controls.Add(this.mOkWindow);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.radCreateNewVariable);
            this.Controls.Add(this.radTunnelVariable);
            this.Controls.Add(this.radExistingVariable);
            this.Controls.Add(this.ExistingVariablePanel);
            this.Controls.Add(this.NewVariablePanel);
            this.Controls.Add(this.TunnelVariablePanel);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(400, 334);
            this.Name = "AddVariableWindow";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "New Variable";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AddVariableWindow_FormClosing);
            this.Load += new System.EventHandler(this.AddVariableWindow_Load);
            this.ExistingVariablePanel.ResumeLayout(false);
            this.ExistingVariablePanel.PerformLayout();
            this.TunnelVariablePanel.ResumeLayout(false);
            this.TunnelVariablePanel.PerformLayout();
            this.NewVariablePanel.ResumeLayout(false);
            this.NewVariablePanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

        private System.Windows.Forms.Button mCancelButton;
        private System.Windows.Forms.Button mOkWindow;
        private System.Windows.Forms.Panel ExistingVariablePanel;
        private System.Windows.Forms.ComboBox AvailableVariablesComboBox;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton radCreateNewVariable;
        private System.Windows.Forms.RadioButton radTunnelVariable;
        private System.Windows.Forms.RadioButton radExistingVariable;
        private System.Windows.Forms.Panel NewVariablePanel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox NewVariableNameTextBox;
        private System.Windows.Forms.Panel TunnelVariablePanel;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox TypeConverterComboBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.ComboBox OverridingVariableTypeComboBox;
        private System.Windows.Forms.TextBox AlternativeNameTextBox;
        private System.Windows.Forms.ComboBox TunnelingVariableComboBox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox TunnelingObjectComboBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox NewTypeListBox;


    }
}