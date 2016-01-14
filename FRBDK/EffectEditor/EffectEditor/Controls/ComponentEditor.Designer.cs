namespace EffectEditor.Controls
{
    partial class ComponentEditor
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
            this.componentEditTabs = new System.Windows.Forms.TabControl();
            this.componentDesignTab = new System.Windows.Forms.TabPage();
            this.shaderProfilePanel = new System.Windows.Forms.Panel();
            this.shaderProfileSelector = new System.Windows.Forms.ComboBox();
            this.label7 = new System.Windows.Forms.Label();
            this.componentAvailabilityPanel = new System.Windows.Forms.Panel();
            this.pixelShaderProfileSelector = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.vertexShaderProfileSelector = new System.Windows.Forms.ComboBox();
            this.vertexShaderCheckbox = new System.Windows.Forms.CheckBox();
            this.label6 = new System.Windows.Forms.Label();
            this.pixelShaderCheckbox = new System.Windows.Forms.CheckBox();
            this.label5 = new System.Windows.Forms.Label();
            this.componentNameBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.componentParameterInputs = new EffectEditor.Controls.ComponentParameterList();
            this.componentParameterOutputs = new EffectEditor.Controls.ComponentParameterList();
            this.componentCodeEditTab = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.componentFunctionStart = new System.Windows.Forms.RichTextBox();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.componentSelectionBox = new System.Windows.Forms.ComboBox();
            this.componentSelectionLabel = new System.Windows.Forms.Label();
            this.componentFunctionCode = new System.Windows.Forms.RichTextBox();
            this.componentFunctionEnd = new System.Windows.Forms.RichTextBox();
            this.componentEditTabs.SuspendLayout();
            this.componentDesignTab.SuspendLayout();
            this.shaderProfilePanel.SuspendLayout();
            this.componentAvailabilityPanel.SuspendLayout();
            this.componentCodeEditTab.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // componentEditTabs
            // 
            this.componentEditTabs.Controls.Add(this.componentDesignTab);
            this.componentEditTabs.Controls.Add(this.componentCodeEditTab);
            this.componentEditTabs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.componentEditTabs.Location = new System.Drawing.Point(0, 0);
            this.componentEditTabs.Name = "componentEditTabs";
            this.componentEditTabs.SelectedIndex = 0;
            this.componentEditTabs.Size = new System.Drawing.Size(576, 494);
            this.componentEditTabs.TabIndex = 5;
            // 
            // componentDesignTab
            // 
            this.componentDesignTab.Controls.Add(this.shaderProfilePanel);
            this.componentDesignTab.Controls.Add(this.componentAvailabilityPanel);
            this.componentDesignTab.Controls.Add(this.componentNameBox);
            this.componentDesignTab.Controls.Add(this.label3);
            this.componentDesignTab.Controls.Add(this.label2);
            this.componentDesignTab.Controls.Add(this.label1);
            this.componentDesignTab.Controls.Add(this.componentParameterInputs);
            this.componentDesignTab.Controls.Add(this.componentParameterOutputs);
            this.componentDesignTab.Location = new System.Drawing.Point(4, 22);
            this.componentDesignTab.Name = "componentDesignTab";
            this.componentDesignTab.Padding = new System.Windows.Forms.Padding(3);
            this.componentDesignTab.Size = new System.Drawing.Size(568, 468);
            this.componentDesignTab.TabIndex = 0;
            this.componentDesignTab.Text = "Design";
            this.componentDesignTab.UseVisualStyleBackColor = true;
            // 
            // shaderProfilePanel
            // 
            this.shaderProfilePanel.Controls.Add(this.shaderProfileSelector);
            this.shaderProfilePanel.Controls.Add(this.label7);
            this.shaderProfilePanel.Location = new System.Drawing.Point(0, 30);
            this.shaderProfilePanel.Name = "shaderProfilePanel";
            this.shaderProfilePanel.Size = new System.Drawing.Size(568, 23);
            this.shaderProfilePanel.TabIndex = 16;
            this.shaderProfilePanel.Visible = false;
            // 
            // shaderProfileSelector
            // 
            this.shaderProfileSelector.FormattingEnabled = true;
            this.shaderProfileSelector.Location = new System.Drawing.Point(129, 1);
            this.shaderProfileSelector.Name = "shaderProfileSelector";
            this.shaderProfileSelector.Size = new System.Drawing.Size(85, 21);
            this.shaderProfileSelector.TabIndex = 24;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(6, 4);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(117, 13);
            this.label7.TabIndex = 23;
            this.label7.Text = "Minimum Shader Profile";
            // 
            // componentAvailabilityPanel
            // 
            this.componentAvailabilityPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.componentAvailabilityPanel.Controls.Add(this.pixelShaderProfileSelector);
            this.componentAvailabilityPanel.Controls.Add(this.label4);
            this.componentAvailabilityPanel.Controls.Add(this.vertexShaderProfileSelector);
            this.componentAvailabilityPanel.Controls.Add(this.vertexShaderCheckbox);
            this.componentAvailabilityPanel.Controls.Add(this.label6);
            this.componentAvailabilityPanel.Controls.Add(this.pixelShaderCheckbox);
            this.componentAvailabilityPanel.Controls.Add(this.label5);
            this.componentAvailabilityPanel.Location = new System.Drawing.Point(0, 30);
            this.componentAvailabilityPanel.Name = "componentAvailabilityPanel";
            this.componentAvailabilityPanel.Size = new System.Drawing.Size(568, 45);
            this.componentAvailabilityPanel.TabIndex = 15;
            // 
            // pixelShaderProfileSelector
            // 
            this.pixelShaderProfileSelector.FormattingEnabled = true;
            this.pixelShaderProfileSelector.Location = new System.Drawing.Point(348, 24);
            this.pixelShaderProfileSelector.Name = "pixelShaderProfileSelector";
            this.pixelShaderProfileSelector.Size = new System.Drawing.Size(85, 21);
            this.pixelShaderProfileSelector.TabIndex = 22;
            this.pixelShaderProfileSelector.SelectedIndexChanged += new System.EventHandler(this.pixelShaderProfileSelector_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(5, 4);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(59, 13);
            this.label4.TabIndex = 17;
            this.label4.Text = "Availability:";
            // 
            // vertexShaderProfileSelector
            // 
            this.vertexShaderProfileSelector.FormattingEnabled = true;
            this.vertexShaderProfileSelector.Location = new System.Drawing.Point(348, 1);
            this.vertexShaderProfileSelector.Name = "vertexShaderProfileSelector";
            this.vertexShaderProfileSelector.Size = new System.Drawing.Size(85, 21);
            this.vertexShaderProfileSelector.TabIndex = 21;
            this.vertexShaderProfileSelector.SelectedIndexChanged += new System.EventHandler(this.vertexShaderProfileSelector_SelectedIndexChanged);
            // 
            // vertexShaderCheckbox
            // 
            this.vertexShaderCheckbox.AutoSize = true;
            this.vertexShaderCheckbox.Location = new System.Drawing.Point(95, 3);
            this.vertexShaderCheckbox.Name = "vertexShaderCheckbox";
            this.vertexShaderCheckbox.Size = new System.Drawing.Size(93, 17);
            this.vertexShaderCheckbox.TabIndex = 16;
            this.vertexShaderCheckbox.Text = "Vertex Shader";
            this.vertexShaderCheckbox.UseVisualStyleBackColor = true;
            this.vertexShaderCheckbox.CheckedChanged += new System.EventHandler(this.vertexShaderCheckbox_CheckedChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(225, 27);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(117, 13);
            this.label6.TabIndex = 20;
            this.label6.Text = "Minimum Shader Profile";
            // 
            // pixelShaderCheckbox
            // 
            this.pixelShaderCheckbox.AutoSize = true;
            this.pixelShaderCheckbox.Location = new System.Drawing.Point(95, 26);
            this.pixelShaderCheckbox.Name = "pixelShaderCheckbox";
            this.pixelShaderCheckbox.Size = new System.Drawing.Size(85, 17);
            this.pixelShaderCheckbox.TabIndex = 18;
            this.pixelShaderCheckbox.Text = "Pixel Shader";
            this.pixelShaderCheckbox.UseVisualStyleBackColor = true;
            this.pixelShaderCheckbox.CheckedChanged += new System.EventHandler(this.pixelShaderCheckbox_CheckedChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(225, 4);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(117, 13);
            this.label5.TabIndex = 19;
            this.label5.Text = "Minimum Shader Profile";
            // 
            // componentNameBox
            // 
            this.componentNameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.componentNameBox.Location = new System.Drawing.Point(47, 4);
            this.componentNameBox.Name = "componentNameBox";
            this.componentNameBox.Size = new System.Drawing.Size(516, 20);
            this.componentNameBox.TabIndex = 5;
            this.componentNameBox.TextChanged += new System.EventHandler(this.componentNameBox_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(35, 13);
            this.label3.TabIndex = 4;
            this.label3.Text = "Name";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(288, 78);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(44, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Outputs";
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 78);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Inputs";
            // 
            // componentParameterInputs
            // 
            this.componentParameterInputs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.componentParameterInputs.Location = new System.Drawing.Point(6, 94);
            this.componentParameterInputs.Name = "componentParameterInputs";
            this.componentParameterInputs.Parameters = null;
            this.componentParameterInputs.Semantics = null;
            this.componentParameterInputs.Size = new System.Drawing.Size(276, 368);
            this.componentParameterInputs.StorageClassEnabled = false;
            this.componentParameterInputs.TabIndex = 1;
            this.componentParameterInputs.ParametersChanged += new System.EventHandler(this.componentParameterInputs_ParametersChanged);
            // 
            // componentParameterOutputs
            // 
            this.componentParameterOutputs.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.componentParameterOutputs.Location = new System.Drawing.Point(287, 94);
            this.componentParameterOutputs.Name = "componentParameterOutputs";
            this.componentParameterOutputs.Parameters = null;
            this.componentParameterOutputs.Semantics = null;
            this.componentParameterOutputs.Size = new System.Drawing.Size(276, 368);
            this.componentParameterOutputs.StorageClassEnabled = false;
            this.componentParameterOutputs.TabIndex = 0;
            this.componentParameterOutputs.ParametersChanged += new System.EventHandler(this.componentParameterOutputs_ParametersChanged);
            // 
            // componentCodeEditTab
            // 
            this.componentCodeEditTab.Controls.Add(this.splitContainer1);
            this.componentCodeEditTab.Location = new System.Drawing.Point(4, 22);
            this.componentCodeEditTab.Name = "componentCodeEditTab";
            this.componentCodeEditTab.Padding = new System.Windows.Forms.Padding(3);
            this.componentCodeEditTab.Size = new System.Drawing.Size(568, 468);
            this.componentCodeEditTab.TabIndex = 1;
            this.componentCodeEditTab.Text = "Code";
            this.componentCodeEditTab.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.componentFunctionStart);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(562, 462);
            this.splitContainer1.SplitterDistance = 74;
            this.splitContainer1.TabIndex = 4;
            // 
            // componentFunctionStart
            // 
            this.componentFunctionStart.BackColor = System.Drawing.SystemColors.Control;
            this.componentFunctionStart.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.componentFunctionStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.componentFunctionStart.Font = new System.Drawing.Font("Courier New", 9F);
            this.componentFunctionStart.ForeColor = System.Drawing.SystemColors.GrayText;
            this.componentFunctionStart.Location = new System.Drawing.Point(0, 0);
            this.componentFunctionStart.Name = "componentFunctionStart";
            this.componentFunctionStart.ReadOnly = true;
            this.componentFunctionStart.Size = new System.Drawing.Size(562, 74);
            this.componentFunctionStart.TabIndex = 1;
            this.componentFunctionStart.Text = "";
            this.componentFunctionStart.WordWrap = false;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.componentSelectionBox);
            this.splitContainer2.Panel1.Controls.Add(this.componentSelectionLabel);
            this.splitContainer2.Panel1.Controls.Add(this.componentFunctionCode);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.componentFunctionEnd);
            this.splitContainer2.Size = new System.Drawing.Size(562, 384);
            this.splitContainer2.SplitterDistance = 308;
            this.splitContainer2.TabIndex = 5;
            // 
            // componentSelectionBox
            // 
            this.componentSelectionBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.componentSelectionBox.FormattingEnabled = true;
            this.componentSelectionBox.Location = new System.Drawing.Point(102, 287);
            this.componentSelectionBox.Name = "componentSelectionBox";
            this.componentSelectionBox.Size = new System.Drawing.Size(457, 21);
            this.componentSelectionBox.TabIndex = 5;
            this.componentSelectionBox.SelectedIndexChanged += new System.EventHandler(this.componentSelectionBox_SelectedIndexChanged);
            // 
            // componentSelectionLabel
            // 
            this.componentSelectionLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.componentSelectionLabel.AutoSize = true;
            this.componentSelectionLabel.Location = new System.Drawing.Point(3, 290);
            this.componentSelectionLabel.Name = "componentSelectionLabel";
            this.componentSelectionLabel.Size = new System.Drawing.Size(93, 13);
            this.componentSelectionLabel.TabIndex = 4;
            this.componentSelectionLabel.Text = "Insert Component:";
            // 
            // componentFunctionCode
            // 
            this.componentFunctionCode.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.componentFunctionCode.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.componentFunctionCode.Font = new System.Drawing.Font("Courier New", 9F);
            this.componentFunctionCode.Location = new System.Drawing.Point(3, 0);
            this.componentFunctionCode.Name = "componentFunctionCode";
            this.componentFunctionCode.Size = new System.Drawing.Size(556, 281);
            this.componentFunctionCode.TabIndex = 3;
            this.componentFunctionCode.Text = "";
            this.componentFunctionCode.WordWrap = false;
            this.componentFunctionCode.TextChanged += new System.EventHandler(this.componentFunctionCode_TextChanged);
            // 
            // componentFunctionEnd
            // 
            this.componentFunctionEnd.BackColor = System.Drawing.SystemColors.Control;
            this.componentFunctionEnd.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.componentFunctionEnd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.componentFunctionEnd.Font = new System.Drawing.Font("Courier New", 9F);
            this.componentFunctionEnd.ForeColor = System.Drawing.SystemColors.GrayText;
            this.componentFunctionEnd.Location = new System.Drawing.Point(0, 0);
            this.componentFunctionEnd.Name = "componentFunctionEnd";
            this.componentFunctionEnd.ReadOnly = true;
            this.componentFunctionEnd.Size = new System.Drawing.Size(562, 72);
            this.componentFunctionEnd.TabIndex = 2;
            this.componentFunctionEnd.Text = "";
            this.componentFunctionEnd.WordWrap = false;
            // 
            // ComponentEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.componentEditTabs);
            this.Name = "ComponentEditor";
            this.Size = new System.Drawing.Size(576, 494);
            this.componentEditTabs.ResumeLayout(false);
            this.componentDesignTab.ResumeLayout(false);
            this.componentDesignTab.PerformLayout();
            this.shaderProfilePanel.ResumeLayout(false);
            this.shaderProfilePanel.PerformLayout();
            this.componentAvailabilityPanel.ResumeLayout(false);
            this.componentAvailabilityPanel.PerformLayout();
            this.componentCodeEditTab.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl componentEditTabs;
        private System.Windows.Forms.TabPage componentDesignTab;
        private System.Windows.Forms.Panel componentAvailabilityPanel;
        private System.Windows.Forms.ComboBox pixelShaderProfileSelector;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox vertexShaderProfileSelector;
        private System.Windows.Forms.CheckBox vertexShaderCheckbox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.CheckBox pixelShaderCheckbox;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox componentNameBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private ComponentParameterList componentParameterInputs;
        private ComponentParameterList componentParameterOutputs;
        private System.Windows.Forms.TabPage componentCodeEditTab;
        private System.Windows.Forms.RichTextBox componentFunctionStart;
        private System.Windows.Forms.RichTextBox componentFunctionEnd;
        private System.Windows.Forms.RichTextBox componentFunctionCode;
        private System.Windows.Forms.Panel shaderProfilePanel;
        private System.Windows.Forms.ComboBox shaderProfileSelector;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.ComboBox componentSelectionBox;
        private System.Windows.Forms.Label componentSelectionLabel;

    }
}
