namespace EffectParameterEditor
{
    partial class Form1
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.parametersBox = new System.Windows.Forms.ComboBox();
            this.effectPropGrid = new System.Windows.Forms.PropertyGrid();
            this.label2 = new System.Windows.Forms.Label();
            this.effectBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.meshPartBox = new System.Windows.Forms.ComboBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.modelPropGrid = new System.Windows.Forms.PropertyGrid();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadModelToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadEffectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadParametersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.loadMaterialToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveMaterialToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveParxFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.openParxFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.openModelDialog = new System.Windows.Forms.OpenFileDialog();
            this.openEffectDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveMaterialDialog = new System.Windows.Forms.SaveFileDialog();
            this.openMaterialDialog = new System.Windows.Forms.OpenFileDialog();
            this.modelViewControl1 = new EffectParameterEditor.Controls.ModelViewControl();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBox3);
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.modelViewControl1);
            this.splitContainer1.Size = new System.Drawing.Size(845, 555);
            this.splitContainer1.SplitterDistance = 259;
            this.splitContainer1.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.parametersBox);
            this.groupBox3.Controls.Add(this.effectPropGrid);
            this.groupBox3.Controls.Add(this.label2);
            this.groupBox3.Controls.Add(this.effectBox);
            this.groupBox3.Controls.Add(this.label1);
            this.groupBox3.Controls.Add(this.meshPartBox);
            this.groupBox3.Location = new System.Drawing.Point(4, 195);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(251, 357);
            this.groupBox3.TabIndex = 3;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Part";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 96);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(60, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Parameters";
            // 
            // parametersBox
            // 
            this.parametersBox.FormattingEnabled = true;
            this.parametersBox.Location = new System.Drawing.Point(6, 112);
            this.parametersBox.Name = "parametersBox";
            this.parametersBox.Size = new System.Drawing.Size(239, 21);
            this.parametersBox.TabIndex = 6;
            this.parametersBox.SelectedIndexChanged += new System.EventHandler(this.parametersBox_SelectedIndexChanged);
            // 
            // effectPropGrid
            // 
            this.effectPropGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.effectPropGrid.Location = new System.Drawing.Point(3, 139);
            this.effectPropGrid.Name = "effectPropGrid";
            this.effectPropGrid.Size = new System.Drawing.Size(248, 218);
            this.effectPropGrid.TabIndex = 1;
            this.effectPropGrid.PropertyValueChanged += new System.Windows.Forms.PropertyValueChangedEventHandler(this.effectPropGrid_PropertyValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 56);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Effect";
            // 
            // effectBox
            // 
            this.effectBox.FormattingEnabled = true;
            this.effectBox.Location = new System.Drawing.Point(6, 72);
            this.effectBox.Name = "effectBox";
            this.effectBox.Size = new System.Drawing.Size(239, 21);
            this.effectBox.TabIndex = 4;
            this.effectBox.SelectedIndexChanged += new System.EventHandler(this.effectBox_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Current Part";
            // 
            // meshPartBox
            // 
            this.meshPartBox.FormattingEnabled = true;
            this.meshPartBox.Location = new System.Drawing.Point(6, 32);
            this.meshPartBox.Name = "meshPartBox";
            this.meshPartBox.Size = new System.Drawing.Size(239, 21);
            this.meshPartBox.TabIndex = 2;
            this.meshPartBox.SelectedIndexChanged += new System.EventHandler(this.meshPartBox_SelectedIndexChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.modelPropGrid);
            this.groupBox1.Location = new System.Drawing.Point(4, 4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(252, 185);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Model";
            // 
            // modelPropGrid
            // 
            this.modelPropGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modelPropGrid.Location = new System.Drawing.Point(3, 16);
            this.modelPropGrid.Name = "modelPropGrid";
            this.modelPropGrid.Size = new System.Drawing.Size(246, 166);
            this.modelPropGrid.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(845, 24);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadModelToolStripMenuItem,
            this.loadEffectToolStripMenuItem,
            this.loadParametersToolStripMenuItem,
            this.toolStripSeparator1,
            this.loadMaterialToolStripMenuItem,
            this.saveMaterialToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // loadModelToolStripMenuItem
            // 
            this.loadModelToolStripMenuItem.Name = "loadModelToolStripMenuItem";
            this.loadModelToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.loadModelToolStripMenuItem.Text = "Load &Model";
            this.loadModelToolStripMenuItem.Click += new System.EventHandler(this.loadModelToolStripMenuItem_Click);
            // 
            // loadEffectToolStripMenuItem
            // 
            this.loadEffectToolStripMenuItem.Name = "loadEffectToolStripMenuItem";
            this.loadEffectToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.loadEffectToolStripMenuItem.Text = "Load &Effect";
            this.loadEffectToolStripMenuItem.Click += new System.EventHandler(this.loadEffectToolStripMenuItem_Click);
            // 
            // loadParametersToolStripMenuItem
            // 
            this.loadParametersToolStripMenuItem.Name = "loadParametersToolStripMenuItem";
            this.loadParametersToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.loadParametersToolStripMenuItem.Text = "Load &Parameters";
            this.loadParametersToolStripMenuItem.Click += new System.EventHandler(this.loadParametersToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(163, 6);
            // 
            // loadMaterialToolStripMenuItem
            // 
            this.loadMaterialToolStripMenuItem.Name = "loadMaterialToolStripMenuItem";
            this.loadMaterialToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.loadMaterialToolStripMenuItem.Text = "&Load Material";
            this.loadMaterialToolStripMenuItem.Click += new System.EventHandler(this.loadMaterialToolStripMenuItem_Click);
            // 
            // saveMaterialToolStripMenuItem
            // 
            this.saveMaterialToolStripMenuItem.Name = "saveMaterialToolStripMenuItem";
            this.saveMaterialToolStripMenuItem.Size = new System.Drawing.Size(166, 22);
            this.saveMaterialToolStripMenuItem.Text = "&Save Material";
            this.saveMaterialToolStripMenuItem.ToolTipText = "Save the material, and changes to all parameters";
            this.saveMaterialToolStripMenuItem.Click += new System.EventHandler(this.saveMaterialToolStripMenuItem_Click);
            // 
            // saveParxFileDialog
            // 
            this.saveParxFileDialog.DefaultExt = "fxparx";
            this.saveParxFileDialog.Filter = "Parameter files (*.fxparx)|*.fxparx";
            // 
            // openParxFileDialog
            // 
            this.openParxFileDialog.DefaultExt = "fxparx";
            this.openParxFileDialog.Filter = "Parameter files (*.fxparx)|*.fxparx";
            // 
            // openModelDialog
            // 
            this.openModelDialog.DefaultExt = "x";
            this.openModelDialog.Filter = "X Model (*.x)|*.x|XNB Model (*.xnb)|*.xnb";
            // 
            // openEffectDialog
            // 
            this.openEffectDialog.DefaultExt = "fx";
            this.openEffectDialog.Filter = "HLSL Effect (*.fx)|*.fx";
            // 
            // saveMaterialDialog
            // 
            this.saveMaterialDialog.DefaultExt = "matx";
            this.saveMaterialDialog.Filter = "FRB Material (*.matx)|*.matx";
            // 
            // openMaterialDialog
            // 
            this.openMaterialDialog.DefaultExt = "matx";
            this.openMaterialDialog.Filter = "FRB Material (*.matx)|*.matx";
            // 
            // modelViewControl1
            // 
            this.modelViewControl1.BackgroundColor = new Microsoft.Xna.Framework.Graphics.Color(((byte)(0)), ((byte)(0)), ((byte)(0)), ((byte)(255)));
            this.modelViewControl1.CurrentModel = null;
            this.modelViewControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modelViewControl1.Location = new System.Drawing.Point(0, 0);
            this.modelViewControl1.Name = "modelViewControl1";
            this.modelViewControl1.Size = new System.Drawing.Size(582, 555);
            this.modelViewControl1.StatusStrip = null;
            this.modelViewControl1.TabIndex = 0;
            this.modelViewControl1.Text = "modelViewControl1";
            this.modelViewControl1.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.modelViewControl1_PreviewKeyDown);
            this.modelViewControl1.MouseDown += new System.Windows.Forms.MouseEventHandler(this.modelViewControl1_MouseDown);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(845, 579);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.menuStrip1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.PropertyGrid effectPropGrid;
        private System.Windows.Forms.PropertyGrid modelPropGrid;
        private EffectParameterEditor.Controls.ModelViewControl modelViewControl1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveParxFileDialog;
        private System.Windows.Forms.ToolStripMenuItem loadParametersToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openParxFileDialog;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem loadModelToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadEffectToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openModelDialog;
        private System.Windows.Forms.OpenFileDialog openEffectDialog;
        private System.Windows.Forms.ComboBox meshPartBox;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox effectBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox parametersBox;
        private System.Windows.Forms.ToolStripMenuItem saveMaterialToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveMaterialDialog;
        private System.Windows.Forms.ToolStripMenuItem loadMaterialToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openMaterialDialog;
    }
}

