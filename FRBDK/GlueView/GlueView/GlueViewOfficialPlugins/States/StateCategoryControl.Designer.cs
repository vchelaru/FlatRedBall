namespace GlueViewOfficialPlugins.States
{
    partial class StateCategoryControl
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
            this.CategoryNameLabel = new System.Windows.Forms.Label();
            this.StateComboBox = new System.Windows.Forms.ComboBox();
            this.InterpolateFromComboBox = new System.Windows.Forms.ComboBox();
            this.InterpolateToComboBox = new System.Windows.Forms.ComboBox();
            this.PercentageTrackBar = new System.Windows.Forms.TrackBar();
            this.InterpolationPanel = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.PercentLabel = new System.Windows.Forms.Label();
            this.ToPercentageTextBox = new System.Windows.Forms.TextBox();
            this.FromPercentageTextBox = new System.Windows.Forms.TextBox();
            this.AdvancedInterpolationPanel = new System.Windows.Forms.Panel();
            this.CopyStateCodeButton = new System.Windows.Forms.Button();
            this.ExampleCodeTextBox = new System.Windows.Forms.TextBox();
            this.InterpolationTimeTextBox = new System.Windows.Forms.TextBox();
            this.StartTweenButton = new System.Windows.Forms.Button();
            this.EasingTypeComboBox = new System.Windows.Forms.ComboBox();
            this.InterpolationTypeComboBox = new System.Windows.Forms.ComboBox();
            ((System.ComponentModel.ISupportInitialize)(this.PercentageTrackBar)).BeginInit();
            this.InterpolationPanel.SuspendLayout();
            this.AdvancedInterpolationPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // CategoryNameLabel
            // 
            this.CategoryNameLabel.AutoSize = true;
            this.CategoryNameLabel.Location = new System.Drawing.Point(3, 0);
            this.CategoryNameLabel.Name = "CategoryNameLabel";
            this.CategoryNameLabel.Size = new System.Drawing.Size(35, 13);
            this.CategoryNameLabel.TabIndex = 0;
            this.CategoryNameLabel.Text = "label1";
            // 
            // StateComboBox
            // 
            this.StateComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.StateComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.StateComboBox.FormattingEnabled = true;
            this.StateComboBox.Location = new System.Drawing.Point(6, 16);
            this.StateComboBox.Name = "StateComboBox";
            this.StateComboBox.Size = new System.Drawing.Size(295, 21);
            this.StateComboBox.TabIndex = 1;
            this.StateComboBox.SelectedIndexChanged += new System.EventHandler(this.StateComboBox_SelectedIndexChanged);
            // 
            // InterpolateFromComboBox
            // 
            this.InterpolateFromComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.InterpolateFromComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.InterpolateFromComboBox.FormattingEnabled = true;
            this.InterpolateFromComboBox.Location = new System.Drawing.Point(6, 3);
            this.InterpolateFromComboBox.Name = "InterpolateFromComboBox";
            this.InterpolateFromComboBox.Size = new System.Drawing.Size(206, 21);
            this.InterpolateFromComboBox.TabIndex = 2;
            this.InterpolateFromComboBox.SelectedIndexChanged += new System.EventHandler(this.StateComboBox_SelectedIndexChanged);
            // 
            // InterpolateToComboBox
            // 
            this.InterpolateToComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.InterpolateToComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.InterpolateToComboBox.FormattingEnabled = true;
            this.InterpolateToComboBox.Location = new System.Drawing.Point(6, 40);
            this.InterpolateToComboBox.Name = "InterpolateToComboBox";
            this.InterpolateToComboBox.Size = new System.Drawing.Size(206, 21);
            this.InterpolateToComboBox.TabIndex = 3;
            this.InterpolateToComboBox.SelectedIndexChanged += new System.EventHandler(this.StateComboBox_SelectedIndexChanged);
            // 
            // PercentageTrackBar
            // 
            this.PercentageTrackBar.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.PercentageTrackBar.Location = new System.Drawing.Point(264, -7);
            this.PercentageTrackBar.Maximum = 100;
            this.PercentageTrackBar.Name = "PercentageTrackBar";
            this.PercentageTrackBar.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.PercentageTrackBar.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.PercentageTrackBar.Size = new System.Drawing.Size(45, 78);
            this.PercentageTrackBar.TabIndex = 4;
            this.PercentageTrackBar.TickFrequency = 10;
            this.PercentageTrackBar.TickStyle = System.Windows.Forms.TickStyle.TopLeft;
            this.PercentageTrackBar.Scroll += new System.EventHandler(this.PercentageTrackBar_Scroll);
            this.PercentageTrackBar.ValueChanged += new System.EventHandler(this.PercentageTrackBar_ValueChanged);
            // 
            // InterpolationPanel
            // 
            this.InterpolationPanel.Controls.Add(this.label2);
            this.InterpolationPanel.Controls.Add(this.PercentLabel);
            this.InterpolationPanel.Controls.Add(this.ToPercentageTextBox);
            this.InterpolationPanel.Controls.Add(this.FromPercentageTextBox);
            this.InterpolationPanel.Controls.Add(this.InterpolateFromComboBox);
            this.InterpolationPanel.Controls.Add(this.PercentageTrackBar);
            this.InterpolationPanel.Controls.Add(this.InterpolateToComboBox);
            this.InterpolationPanel.Location = new System.Drawing.Point(0, 42);
            this.InterpolationPanel.Name = "InterpolationPanel";
            this.InterpolationPanel.Size = new System.Drawing.Size(297, 68);
            this.InterpolationPanel.TabIndex = 5;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(249, 43);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(15, 13);
            this.label2.TabIndex = 8;
            this.label2.Text = "%";
            // 
            // PercentLabel
            // 
            this.PercentLabel.AutoSize = true;
            this.PercentLabel.Location = new System.Drawing.Point(249, 7);
            this.PercentLabel.Name = "PercentLabel";
            this.PercentLabel.Size = new System.Drawing.Size(15, 13);
            this.PercentLabel.TabIndex = 7;
            this.PercentLabel.Text = "%";
            // 
            // ToPercentageTextBox
            // 
            this.ToPercentageTextBox.Location = new System.Drawing.Point(218, 40);
            this.ToPercentageTextBox.Name = "ToPercentageTextBox";
            this.ToPercentageTextBox.Size = new System.Drawing.Size(28, 20);
            this.ToPercentageTextBox.TabIndex = 6;
            this.ToPercentageTextBox.WordWrap = false;
            this.ToPercentageTextBox.TextChanged += new System.EventHandler(this.ToPercentageTextBox_TextChanged);
            // 
            // FromPercentageTextBox
            // 
            this.FromPercentageTextBox.Location = new System.Drawing.Point(218, 4);
            this.FromPercentageTextBox.Name = "FromPercentageTextBox";
            this.FromPercentageTextBox.Size = new System.Drawing.Size(28, 20);
            this.FromPercentageTextBox.TabIndex = 5;
            this.FromPercentageTextBox.TextChanged += new System.EventHandler(this.FromPercentageTextBox_TextChanged);
            // 
            // AdvancedInterpolationPanel
            // 
            this.AdvancedInterpolationPanel.Controls.Add(this.CopyStateCodeButton);
            this.AdvancedInterpolationPanel.Controls.Add(this.ExampleCodeTextBox);
            this.AdvancedInterpolationPanel.Controls.Add(this.InterpolationTimeTextBox);
            this.AdvancedInterpolationPanel.Controls.Add(this.StartTweenButton);
            this.AdvancedInterpolationPanel.Controls.Add(this.EasingTypeComboBox);
            this.AdvancedInterpolationPanel.Controls.Add(this.InterpolationTypeComboBox);
            this.AdvancedInterpolationPanel.Location = new System.Drawing.Point(0, 110);
            this.AdvancedInterpolationPanel.Name = "AdvancedInterpolationPanel";
            this.AdvancedInterpolationPanel.Size = new System.Drawing.Size(301, 52);
            this.AdvancedInterpolationPanel.TabIndex = 6;
            // 
            // CopyStateCodeButton
            // 
            this.CopyStateCodeButton.Location = new System.Drawing.Point(252, 29);
            this.CopyStateCodeButton.Margin = new System.Windows.Forms.Padding(0);
            this.CopyStateCodeButton.Name = "CopyStateCodeButton";
            this.CopyStateCodeButton.Size = new System.Drawing.Size(49, 21);
            this.CopyStateCodeButton.TabIndex = 5;
            this.CopyStateCodeButton.Text = "Copy";
            this.CopyStateCodeButton.UseVisualStyleBackColor = true;
            this.CopyStateCodeButton.Click += new System.EventHandler(this.CopyStateCodeClick_Click);
            // 
            // ExampleCodeTextBox
            // 
            this.ExampleCodeTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ExampleCodeTextBox.Location = new System.Drawing.Point(6, 29);
            this.ExampleCodeTextBox.Name = "ExampleCodeTextBox";
            this.ExampleCodeTextBox.Size = new System.Drawing.Size(243, 20);
            this.ExampleCodeTextBox.TabIndex = 4;
            // 
            // InterpolationTimeTextBox
            // 
            this.InterpolationTimeTextBox.Location = new System.Drawing.Point(164, 7);
            this.InterpolationTimeTextBox.Name = "InterpolationTimeTextBox";
            this.InterpolationTimeTextBox.Size = new System.Drawing.Size(56, 20);
            this.InterpolationTimeTextBox.TabIndex = 3;
            this.InterpolationTimeTextBox.Text = "1";
            this.InterpolationTimeTextBox.TextChanged += new System.EventHandler(this.InterpolationTimeTextBox_TextChanged);
            this.InterpolationTimeTextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.InterpolationTimeTextBox_KeyPress);
            // 
            // StartTweenButton
            // 
            this.StartTweenButton.Location = new System.Drawing.Point(226, 5);
            this.StartTweenButton.Name = "StartTweenButton";
            this.StartTweenButton.Size = new System.Drawing.Size(75, 23);
            this.StartTweenButton.TabIndex = 2;
            this.StartTweenButton.Text = "Start Tween";
            this.StartTweenButton.UseVisualStyleBackColor = true;
            this.StartTweenButton.Click += new System.EventHandler(this.StartTweenButton_Click);
            // 
            // EasingTypeComboBox
            // 
            this.EasingTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.EasingTypeComboBox.FormattingEnabled = true;
            this.EasingTypeComboBox.Location = new System.Drawing.Point(101, 6);
            this.EasingTypeComboBox.Name = "EasingTypeComboBox";
            this.EasingTypeComboBox.Size = new System.Drawing.Size(57, 21);
            this.EasingTypeComboBox.TabIndex = 1;
            this.EasingTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.EasingTypeComboBox_SelectedIndexChanged);
            // 
            // InterpolationTypeComboBox
            // 
            this.InterpolationTypeComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.InterpolationTypeComboBox.FormattingEnabled = true;
            this.InterpolationTypeComboBox.Location = new System.Drawing.Point(6, 6);
            this.InterpolationTypeComboBox.Name = "InterpolationTypeComboBox";
            this.InterpolationTypeComboBox.Size = new System.Drawing.Size(89, 21);
            this.InterpolationTypeComboBox.TabIndex = 0;
            this.InterpolationTypeComboBox.SelectedIndexChanged += new System.EventHandler(this.InterpolationTypeComboBox_SelectedIndexChanged);
            // 
            // StateCategoryControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.Color.Silver;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.AdvancedInterpolationPanel);
            this.Controls.Add(this.InterpolationPanel);
            this.Controls.Add(this.StateComboBox);
            this.Controls.Add(this.CategoryNameLabel);
            this.Name = "StateCategoryControl";
            this.Size = new System.Drawing.Size(304, 165);
            ((System.ComponentModel.ISupportInitialize)(this.PercentageTrackBar)).EndInit();
            this.InterpolationPanel.ResumeLayout(false);
            this.InterpolationPanel.PerformLayout();
            this.AdvancedInterpolationPanel.ResumeLayout(false);
            this.AdvancedInterpolationPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label CategoryNameLabel;
        private System.Windows.Forms.ComboBox StateComboBox;
        private System.Windows.Forms.ComboBox InterpolateFromComboBox;
        private System.Windows.Forms.ComboBox InterpolateToComboBox;
        private System.Windows.Forms.TrackBar PercentageTrackBar;
        private System.Windows.Forms.Panel InterpolationPanel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label PercentLabel;
        private System.Windows.Forms.TextBox ToPercentageTextBox;
        private System.Windows.Forms.TextBox FromPercentageTextBox;
        private System.Windows.Forms.Panel AdvancedInterpolationPanel;
        private System.Windows.Forms.Button StartTweenButton;
        private System.Windows.Forms.ComboBox EasingTypeComboBox;
        private System.Windows.Forms.ComboBox InterpolationTypeComboBox;
        private System.Windows.Forms.TextBox InterpolationTimeTextBox;
        private System.Windows.Forms.TextBox ExampleCodeTextBox;
        private System.Windows.Forms.Button CopyStateCodeButton;
    }
}
