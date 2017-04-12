namespace FlatRedBall.Glue.Controls
{
    partial class CameraSettingsWindow
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
            this.label1 = new System.Windows.Forms.Label();
            this.cbIs2D = new System.Windows.Forms.ComboBox();
            this.cbSetResolution = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.RunFullscreenCheckBox = new System.Windows.Forms.CheckBox();
            this.ApplyToNonPCCheckBox = new System.Windows.Forms.CheckBox();
            this.AddResolutionButton = new System.Windows.Forms.Button();
            this.PresetsTreeView = new System.Windows.Forms.TreeView();
            this.tbResHeight = new System.Windows.Forms.TextBox();
            this.tbResWidth = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.gbOrth = new System.Windows.Forms.GroupBox();
            this.tbOrthHeight = new System.Windows.Forms.TextBox();
            this.tbOrthWidth = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.cbSetOrthResolution = new System.Windows.Forms.CheckBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.button1 = new System.Windows.Forms.Button();
            this.groupBox1.SuspendLayout();
            this.gbOrth.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Padding = new System.Windows.Forms.Padding(0, 7, 0, 0);
            this.label1.Size = new System.Drawing.Size(32, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Is 2D";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // cbIs2D
            // 
            this.cbIs2D.FormattingEnabled = true;
            this.cbIs2D.Items.AddRange(new object[] {
            "True",
            "False"});
            this.cbIs2D.Location = new System.Drawing.Point(41, 3);
            this.cbIs2D.Name = "cbIs2D";
            this.cbIs2D.Size = new System.Drawing.Size(67, 21);
            this.cbIs2D.TabIndex = 1;
            this.cbIs2D.Text = "False";
            this.cbIs2D.SelectedIndexChanged += new System.EventHandler(this.cbIs2D_SelectedIndexChanged);
            // 
            // cbSetResolution
            // 
            this.cbSetResolution.AutoSize = true;
            this.cbSetResolution.Location = new System.Drawing.Point(6, 19);
            this.cbSetResolution.Name = "cbSetResolution";
            this.cbSetResolution.Size = new System.Drawing.Size(95, 17);
            this.cbSetResolution.TabIndex = 2;
            this.cbSetResolution.Text = "Set Resolution";
            this.cbSetResolution.UseVisualStyleBackColor = true;
            this.cbSetResolution.CheckedChanged += new System.EventHandler(this.cbIs2D_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.RunFullscreenCheckBox);
            this.groupBox1.Controls.Add(this.ApplyToNonPCCheckBox);
            this.groupBox1.Controls.Add(this.AddResolutionButton);
            this.groupBox1.Controls.Add(this.PresetsTreeView);
            this.groupBox1.Controls.Add(this.tbResHeight);
            this.groupBox1.Controls.Add(this.tbResWidth);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.cbSetResolution);
            this.groupBox1.Location = new System.Drawing.Point(3, 36);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(240, 130);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Resolution";
            // 
            // RunFullscreenCheckBox
            // 
            this.RunFullscreenCheckBox.AutoSize = true;
            this.RunFullscreenCheckBox.Location = new System.Drawing.Point(117, 12);
            this.RunFullscreenCheckBox.Name = "RunFullscreenCheckBox";
            this.RunFullscreenCheckBox.Size = new System.Drawing.Size(97, 17);
            this.RunFullscreenCheckBox.TabIndex = 11;
            this.RunFullscreenCheckBox.Text = "Run Fullscreen";
            this.RunFullscreenCheckBox.UseVisualStyleBackColor = true;
            this.RunFullscreenCheckBox.CheckedChanged += new System.EventHandler(this.RunFullscreen_CheckedChanged);
            // 
            // ApplyToNonPCCheckBox
            // 
            this.ApplyToNonPCCheckBox.AutoSize = true;
            this.ApplyToNonPCCheckBox.Location = new System.Drawing.Point(6, 35);
            this.ApplyToNonPCCheckBox.Name = "ApplyToNonPCCheckBox";
            this.ApplyToNonPCCheckBox.Size = new System.Drawing.Size(147, 17);
            this.ApplyToNonPCCheckBox.TabIndex = 10;
            this.ApplyToNonPCCheckBox.Text = "Apply to non-PC platforms";
            this.ApplyToNonPCCheckBox.UseVisualStyleBackColor = true;
            this.ApplyToNonPCCheckBox.CheckedChanged += new System.EventHandler(this.ApplyToNonPCCheckBox_CheckedChanged);
            // 
            // AddResolutionButton
            // 
            this.AddResolutionButton.Location = new System.Drawing.Point(6, 103);
            this.AddResolutionButton.Name = "AddResolutionButton";
            this.AddResolutionButton.Size = new System.Drawing.Size(102, 23);
            this.AddResolutionButton.TabIndex = 7;
            this.AddResolutionButton.Text = "Add";
            this.AddResolutionButton.UseVisualStyleBackColor = true;
            this.AddResolutionButton.Click += new System.EventHandler(this.AddResolutionButton_Click);
            // 
            // PresetsTreeView
            // 
            this.PresetsTreeView.Location = new System.Drawing.Point(117, 58);
            this.PresetsTreeView.Name = "PresetsTreeView";
            this.PresetsTreeView.ShowLines = false;
            this.PresetsTreeView.ShowPlusMinus = false;
            this.PresetsTreeView.ShowRootLines = false;
            this.PresetsTreeView.Size = new System.Drawing.Size(120, 67);
            this.PresetsTreeView.TabIndex = 9;
            this.PresetsTreeView.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.PresetsTreeView_AfterSelect);
            this.PresetsTreeView.KeyDown += new System.Windows.Forms.KeyEventHandler(this.PresetsTreeView_KeyDown);
            // 
            // tbResHeight
            // 
            this.tbResHeight.Location = new System.Drawing.Point(47, 79);
            this.tbResHeight.Name = "tbResHeight";
            this.tbResHeight.Size = new System.Drawing.Size(61, 20);
            this.tbResHeight.TabIndex = 6;
            this.tbResHeight.Text = "600";
            this.tbResHeight.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxKeyDown);
            this.tbResHeight.Leave += new System.EventHandler(this.TextBoxLeave);
            // 
            // tbResWidth
            // 
            this.tbResWidth.Location = new System.Drawing.Point(47, 58);
            this.tbResWidth.Name = "tbResWidth";
            this.tbResWidth.Size = new System.Drawing.Size(61, 20);
            this.tbResWidth.TabIndex = 5;
            this.tbResWidth.Text = "800";
            this.tbResWidth.TextChanged += new System.EventHandler(this.tbResWidth_TextChanged);
            this.tbResWidth.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxKeyDown);
            this.tbResWidth.Leave += new System.EventHandler(this.TextBoxLeave);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 82);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(38, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Height";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 61);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Width";
            // 
            // gbOrth
            // 
            this.gbOrth.Controls.Add(this.tbOrthHeight);
            this.gbOrth.Controls.Add(this.tbOrthWidth);
            this.gbOrth.Controls.Add(this.label4);
            this.gbOrth.Controls.Add(this.label5);
            this.gbOrth.Controls.Add(this.cbSetOrthResolution);
            this.gbOrth.Location = new System.Drawing.Point(3, 172);
            this.gbOrth.Name = "gbOrth";
            this.gbOrth.Size = new System.Drawing.Size(130, 91);
            this.gbOrth.TabIndex = 7;
            this.gbOrth.TabStop = false;
            this.gbOrth.Text = "Orthogonal Values";
            this.gbOrth.Visible = false;
            // 
            // tbOrthHeight
            // 
            this.tbOrthHeight.Location = new System.Drawing.Point(47, 57);
            this.tbOrthHeight.Name = "tbOrthHeight";
            this.tbOrthHeight.Size = new System.Drawing.Size(61, 20);
            this.tbOrthHeight.TabIndex = 6;
            this.tbOrthHeight.Text = "600";
            this.tbOrthHeight.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxKeyDown);
            this.tbOrthHeight.Leave += new System.EventHandler(this.TextBoxLeave);
            // 
            // tbOrthWidth
            // 
            this.tbOrthWidth.Location = new System.Drawing.Point(47, 36);
            this.tbOrthWidth.Name = "tbOrthWidth";
            this.tbOrthWidth.Size = new System.Drawing.Size(61, 20);
            this.tbOrthWidth.TabIndex = 5;
            this.tbOrthWidth.Text = "800";
            this.tbOrthWidth.KeyDown += new System.Windows.Forms.KeyEventHandler(this.TextBoxKeyDown);
            this.tbOrthWidth.Leave += new System.EventHandler(this.TextBoxLeave);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 60);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Height";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(6, 39);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(35, 13);
            this.label5.TabIndex = 3;
            this.label5.Text = "Width";
            // 
            // cbSetOrthResolution
            // 
            this.cbSetOrthResolution.AutoSize = true;
            this.cbSetOrthResolution.Location = new System.Drawing.Point(6, 19);
            this.cbSetOrthResolution.Name = "cbSetOrthResolution";
            this.cbSetOrthResolution.Size = new System.Drawing.Size(92, 17);
            this.cbSetOrthResolution.TabIndex = 2;
            this.cbSetOrthResolution.Text = "Set Values to:";
            this.cbSetOrthResolution.UseVisualStyleBackColor = true;
            this.cbSetOrthResolution.CheckedChanged += new System.EventHandler(this.cbSetOrthResolution_CheckedChanged);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.Controls.Add(this.flowLayoutPanel2);
            this.flowLayoutPanel1.Controls.Add(this.groupBox1);
            this.flowLayoutPanel1.Controls.Add(this.gbOrth);
            this.flowLayoutPanel1.Controls.Add(this.button1);
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(246, 295);
            this.flowLayoutPanel1.TabIndex = 8;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel2.Controls.Add(this.label1);
            this.flowLayoutPanel2.Controls.Add(this.cbIs2D);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(3, 3);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(111, 27);
            this.flowLayoutPanel2.TabIndex = 9;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(3, 269);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(179, 23);
            this.button1.TabIndex = 10;
            this.button1.Text = " Update to new display settings";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.HandleUpdateToNewDisplaySettingsClicked);
            // 
            // CameraSettingsWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size(277, 303);
            this.Controls.Add(this.flowLayoutPanel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "CameraSettingsWindow";
            this.ShowIcon = false;
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Camera Settings";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CameraSettingsWindow_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.gbOrth.ResumeLayout(false);
            this.gbOrth.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbIs2D;
        private System.Windows.Forms.CheckBox cbSetResolution;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox tbResHeight;
        private System.Windows.Forms.TextBox tbResWidth;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox gbOrth;
        private System.Windows.Forms.TextBox tbOrthHeight;
        private System.Windows.Forms.TextBox tbOrthWidth;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.CheckBox cbSetOrthResolution;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Button AddResolutionButton;
        private System.Windows.Forms.TreeView PresetsTreeView;
        private System.Windows.Forms.CheckBox ApplyToNonPCCheckBox;
        private System.Windows.Forms.CheckBox RunFullscreenCheckBox;
        private System.Windows.Forms.Button button1;
    }
}