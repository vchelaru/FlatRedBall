namespace EffectEditor.Controls
{
    partial class ComponentParameterList
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
            this.parameterListBox = new System.Windows.Forms.ListBox();
            this.paramTypeBox = new System.Windows.Forms.ComboBox();
            this.paramNameBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.paramTypeSizeBox = new System.Windows.Forms.ComboBox();
            this.paramTypeSizeA = new System.Windows.Forms.NumericUpDown();
            this.paramTypeSizeB = new System.Windows.Forms.NumericUpDown();
            this.paramSizeDividerLabel = new System.Windows.Forms.Label();
            this.paramEditPanel = new System.Windows.Forms.Panel();
            this.semanticNumberBox = new System.Windows.Forms.NumericUpDown();
            this.semanticBox = new System.Windows.Forms.ComboBox();
            this.semanticLabel = new System.Windows.Forms.Label();
            this.storageClassBox = new System.Windows.Forms.ComboBox();
            this.storageClassLabel = new System.Windows.Forms.Label();
            this.newParamButton = new System.Windows.Forms.Button();
            this.deleteParamButton = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.paramTypeSizeA)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.paramTypeSizeB)).BeginInit();
            this.paramEditPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.semanticNumberBox)).BeginInit();
            this.SuspendLayout();
            // 
            // parameterListBox
            // 
            this.parameterListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.parameterListBox.FormattingEnabled = true;
            this.parameterListBox.Location = new System.Drawing.Point(0, 0);
            this.parameterListBox.Name = "parameterListBox";
            this.parameterListBox.Size = new System.Drawing.Size(244, 277);
            this.parameterListBox.TabIndex = 0;
            this.parameterListBox.SelectedIndexChanged += new System.EventHandler(this.parameterListBox_SelectedIndexChanged);
            // 
            // paramTypeBox
            // 
            this.paramTypeBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.paramTypeBox.FormattingEnabled = true;
            this.paramTypeBox.Location = new System.Drawing.Point(47, 29);
            this.paramTypeBox.Name = "paramTypeBox";
            this.paramTypeBox.Size = new System.Drawing.Size(194, 21);
            this.paramTypeBox.TabIndex = 1;
            this.paramTypeBox.SelectedIndexChanged += new System.EventHandler(this.paramTypeBox_SelectedIndexChanged);
            // 
            // paramNameBox
            // 
            this.paramNameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.paramNameBox.Location = new System.Drawing.Point(47, 3);
            this.paramNameBox.Name = "paramNameBox";
            this.paramNameBox.Size = new System.Drawing.Size(197, 20);
            this.paramNameBox.TabIndex = 2;
            this.paramNameBox.TextChanged += new System.EventHandler(this.paramEdited);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Name";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 32);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Type";
            // 
            // paramTypeSizeBox
            // 
            this.paramTypeSizeBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.paramTypeSizeBox.FormattingEnabled = true;
            this.paramTypeSizeBox.Items.AddRange(new object[] {
            "Scalar",
            "Vector",
            "Matrix",
            "Array"});
            this.paramTypeSizeBox.Location = new System.Drawing.Point(47, 56);
            this.paramTypeSizeBox.Name = "paramTypeSizeBox";
            this.paramTypeSizeBox.Size = new System.Drawing.Size(94, 21);
            this.paramTypeSizeBox.TabIndex = 5;
            this.paramTypeSizeBox.Text = "Scalar";
            this.paramTypeSizeBox.SelectedIndexChanged += new System.EventHandler(this.paramTypeSizeBox_SelectedIndexChanged);
            // 
            // paramTypeSizeA
            // 
            this.paramTypeSizeA.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.paramTypeSizeA.Location = new System.Drawing.Point(148, 56);
            this.paramTypeSizeA.Maximum = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.paramTypeSizeA.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.paramTypeSizeA.Name = "paramTypeSizeA";
            this.paramTypeSizeA.Size = new System.Drawing.Size(35, 20);
            this.paramTypeSizeA.TabIndex = 6;
            this.paramTypeSizeA.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.paramTypeSizeA.Visible = false;
            this.paramTypeSizeA.ValueChanged += new System.EventHandler(this.paramEdited);
            // 
            // paramTypeSizeB
            // 
            this.paramTypeSizeB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.paramTypeSizeB.Location = new System.Drawing.Point(206, 56);
            this.paramTypeSizeB.Maximum = new decimal(new int[] {
            4,
            0,
            0,
            0});
            this.paramTypeSizeB.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.paramTypeSizeB.Name = "paramTypeSizeB";
            this.paramTypeSizeB.Size = new System.Drawing.Size(35, 20);
            this.paramTypeSizeB.TabIndex = 7;
            this.paramTypeSizeB.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.paramTypeSizeB.Visible = false;
            this.paramTypeSizeB.ValueChanged += new System.EventHandler(this.paramEdited);
            // 
            // paramSizeDividerLabel
            // 
            this.paramSizeDividerLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.paramSizeDividerLabel.AutoSize = true;
            this.paramSizeDividerLabel.Location = new System.Drawing.Point(188, 58);
            this.paramSizeDividerLabel.Name = "paramSizeDividerLabel";
            this.paramSizeDividerLabel.Size = new System.Drawing.Size(12, 13);
            this.paramSizeDividerLabel.TabIndex = 8;
            this.paramSizeDividerLabel.Text = "x";
            this.paramSizeDividerLabel.Visible = false;
            // 
            // paramEditPanel
            // 
            this.paramEditPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.paramEditPanel.Controls.Add(this.semanticNumberBox);
            this.paramEditPanel.Controls.Add(this.semanticBox);
            this.paramEditPanel.Controls.Add(this.semanticLabel);
            this.paramEditPanel.Controls.Add(this.storageClassBox);
            this.paramEditPanel.Controls.Add(this.storageClassLabel);
            this.paramEditPanel.Controls.Add(this.paramNameBox);
            this.paramEditPanel.Controls.Add(this.paramSizeDividerLabel);
            this.paramEditPanel.Controls.Add(this.paramTypeBox);
            this.paramEditPanel.Controls.Add(this.paramTypeSizeB);
            this.paramEditPanel.Controls.Add(this.label1);
            this.paramEditPanel.Controls.Add(this.paramTypeSizeA);
            this.paramEditPanel.Controls.Add(this.label2);
            this.paramEditPanel.Controls.Add(this.paramTypeSizeBox);
            this.paramEditPanel.Location = new System.Drawing.Point(0, 312);
            this.paramEditPanel.Name = "paramEditPanel";
            this.paramEditPanel.Size = new System.Drawing.Size(244, 140);
            this.paramEditPanel.TabIndex = 9;
            // 
            // semanticNumberBox
            // 
            this.semanticNumberBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.semanticNumberBox.Location = new System.Drawing.Point(206, 110);
            this.semanticNumberBox.Name = "semanticNumberBox";
            this.semanticNumberBox.Size = new System.Drawing.Size(35, 20);
            this.semanticNumberBox.TabIndex = 13;
            this.semanticNumberBox.ValueChanged += new System.EventHandler(this.semanticNumberBox_ValueChanged);
            // 
            // semanticBox
            // 
            this.semanticBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.semanticBox.FormattingEnabled = true;
            this.semanticBox.Location = new System.Drawing.Point(79, 109);
            this.semanticBox.Name = "semanticBox";
            this.semanticBox.Size = new System.Drawing.Size(121, 21);
            this.semanticBox.TabIndex = 12;
            this.semanticBox.SelectedIndexChanged += new System.EventHandler(this.semanticBox_SelectedIndexChanged);
            // 
            // semanticLabel
            // 
            this.semanticLabel.AutoSize = true;
            this.semanticLabel.Location = new System.Drawing.Point(6, 112);
            this.semanticLabel.Name = "semanticLabel";
            this.semanticLabel.Size = new System.Drawing.Size(51, 13);
            this.semanticLabel.TabIndex = 11;
            this.semanticLabel.Text = "Semantic";
            // 
            // storageClassBox
            // 
            this.storageClassBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.storageClassBox.FormattingEnabled = true;
            this.storageClassBox.Location = new System.Drawing.Point(79, 82);
            this.storageClassBox.Name = "storageClassBox";
            this.storageClassBox.Size = new System.Drawing.Size(162, 21);
            this.storageClassBox.TabIndex = 10;
            this.storageClassBox.SelectedIndexChanged += new System.EventHandler(this.storageClassBox_SelectedIndexChanged);
            // 
            // storageClassLabel
            // 
            this.storageClassLabel.AutoSize = true;
            this.storageClassLabel.Location = new System.Drawing.Point(6, 85);
            this.storageClassLabel.Name = "storageClassLabel";
            this.storageClassLabel.Size = new System.Drawing.Size(72, 13);
            this.storageClassLabel.TabIndex = 9;
            this.storageClassLabel.Text = "Storage Class";
            // 
            // newParamButton
            // 
            this.newParamButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.newParamButton.Location = new System.Drawing.Point(3, 283);
            this.newParamButton.Name = "newParamButton";
            this.newParamButton.Size = new System.Drawing.Size(75, 23);
            this.newParamButton.TabIndex = 10;
            this.newParamButton.Text = "New";
            this.newParamButton.UseVisualStyleBackColor = true;
            this.newParamButton.Click += new System.EventHandler(this.newParamButton_Click);
            // 
            // deleteParamButton
            // 
            this.deleteParamButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.deleteParamButton.Enabled = false;
            this.deleteParamButton.Location = new System.Drawing.Point(166, 283);
            this.deleteParamButton.Name = "deleteParamButton";
            this.deleteParamButton.Size = new System.Drawing.Size(75, 23);
            this.deleteParamButton.TabIndex = 12;
            this.deleteParamButton.Text = "Delete";
            this.deleteParamButton.UseVisualStyleBackColor = true;
            this.deleteParamButton.Click += new System.EventHandler(this.deleteParamButton_Click);
            // 
            // ComponentParameterList
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.deleteParamButton);
            this.Controls.Add(this.newParamButton);
            this.Controls.Add(this.paramEditPanel);
            this.Controls.Add(this.parameterListBox);
            this.Name = "ComponentParameterList";
            this.Size = new System.Drawing.Size(244, 452);
            ((System.ComponentModel.ISupportInitialize)(this.paramTypeSizeA)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.paramTypeSizeB)).EndInit();
            this.paramEditPanel.ResumeLayout(false);
            this.paramEditPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.semanticNumberBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox parameterListBox;
        private System.Windows.Forms.ComboBox paramTypeBox;
        private System.Windows.Forms.TextBox paramNameBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox paramTypeSizeBox;
        private System.Windows.Forms.NumericUpDown paramTypeSizeA;
        private System.Windows.Forms.NumericUpDown paramTypeSizeB;
        private System.Windows.Forms.Label paramSizeDividerLabel;
        private System.Windows.Forms.Panel paramEditPanel;
        private System.Windows.Forms.Button newParamButton;
        private System.Windows.Forms.Button deleteParamButton;
        private System.Windows.Forms.NumericUpDown semanticNumberBox;
        private System.Windows.Forms.ComboBox semanticBox;
        private System.Windows.Forms.Label semanticLabel;
        private System.Windows.Forms.ComboBox storageClassBox;
        private System.Windows.Forms.Label storageClassLabel;
    }
}
