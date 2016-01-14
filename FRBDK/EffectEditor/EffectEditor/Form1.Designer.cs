namespace EffectEditor
{
    partial class EditorForm
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
            this.tabControl = new System.Windows.Forms.TabControl();
            this.viewTab = new System.Windows.Forms.TabPage();
            this.blueCustomColorBar = new System.Windows.Forms.TrackBar();
            this.greenCustomColorBar = new System.Windows.Forms.TrackBar();
            this.BLabel = new System.Windows.Forms.Label();
            this.bgColorLabel = new System.Windows.Forms.Label();
            this.redCustomColorBar = new System.Windows.Forms.TrackBar();
            this.GLabel = new System.Windows.Forms.Label();
            this.RLabel = new System.Windows.Forms.Label();
            this.bgColorComboBox = new System.Windows.Forms.ComboBox();
            this.modelPage = new System.Windows.Forms.TabPage();
            this.modelSelectionBox = new System.Windows.Forms.ComboBox();
            this.modelPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.currentModelLabel = new System.Windows.Forms.Label();
            this.effectTab = new System.Windows.Forms.TabPage();
            this.effectPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.modelViewPanel = new EffectEditor.Controls.ModelViewControl();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.mainTabControl = new System.Windows.Forms.TabControl();
            this.modelViewTab = new System.Windows.Forms.TabPage();
            this.effectEditTab = new System.Windows.Forms.TabPage();
            this.effectEditorContainer = new System.Windows.Forms.SplitContainer();
            this.effectEditorTabPages = new System.Windows.Forms.TabControl();
            this.componentsTab = new System.Windows.Forms.TabPage();
            this.removeEffectComponentButton = new System.Windows.Forms.Button();
            this.addEffectComponentButton = new System.Windows.Forms.Button();
            this.effectComponentsList = new System.Windows.Forms.ListBox();
            this.compileEffectButton = new System.Windows.Forms.Button();
            this.parametersTab = new System.Windows.Forms.TabPage();
            this.addStandardParameterButton = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.standardParametersList = new System.Windows.Forms.ListBox();
            this.effectParametersList = new EffectEditor.Controls.ComponentParameterList();
            this.vertexShadersTab = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.removeVertexShaderButton = new System.Windows.Forms.Button();
            this.addVertexShaderButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.vertexShadersList = new System.Windows.Forms.ListBox();
            this.effectEditBox = new System.Windows.Forms.RichTextBox();
            this.vertexShaderEditor = new EffectEditor.Controls.ComponentEditor();
            this.pixelShadersTab = new System.Windows.Forms.TabPage();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.removePixelShaderButton = new System.Windows.Forms.Button();
            this.addPixelShaderButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.pixelShadersList = new System.Windows.Forms.ListBox();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.pixelShaderEditor = new EffectEditor.Controls.ComponentEditor();
            this.techniquesTab = new System.Windows.Forms.TabPage();
            this.splitContainer4 = new System.Windows.Forms.SplitContainer();
            this.removeTechniqueButton = new System.Windows.Forms.Button();
            this.addTechniqueButton = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.effectTechniquesList = new System.Windows.Forms.ListBox();
            this.effectTechniqueEditor = new System.Windows.Forms.Panel();
            this.techniqueNameBox = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.effectTechniquePassesEditor = new System.Windows.Forms.SplitContainer();
            this.removePassButton = new System.Windows.Forms.Button();
            this.effectTechniquePassesList = new System.Windows.Forms.ListBox();
            this.addPassButton = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.passPixelShaderProfileBox = new System.Windows.Forms.ComboBox();
            this.label11 = new System.Windows.Forms.Label();
            this.passPixelShaderBox = new System.Windows.Forms.ComboBox();
            this.passVertexShaderProfileBox = new System.Windows.Forms.ComboBox();
            this.label10 = new System.Windows.Forms.Label();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.passVertexShaderBox = new System.Windows.Forms.ComboBox();
            this.passNameBox = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.effectCompileOutput = new System.Windows.Forms.RichTextBox();
            this.componentTab = new System.Windows.Forms.TabPage();
            this.componentEditor = new EffectEditor.Controls.ComponentEditor();
            this.hlslInfoBox = new System.Windows.Forms.RichTextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.effectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadEffectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveEffectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.componentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newComponentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.loadComponentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveComponentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openModelFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.panel1 = new System.Windows.Forms.Panel();
            this.saveComponentDialog = new System.Windows.Forms.SaveFileDialog();
            this.openComponentDialog = new System.Windows.Forms.OpenFileDialog();
            this.saveEffectDialog = new System.Windows.Forms.SaveFileDialog();
            this.openEffectDialog = new System.Windows.Forms.OpenFileDialog();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl.SuspendLayout();
            this.viewTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.blueCustomColorBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.greenCustomColorBar)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.redCustomColorBar)).BeginInit();
            this.modelPage.SuspendLayout();
            this.effectTab.SuspendLayout();
            this.mainTabControl.SuspendLayout();
            this.modelViewTab.SuspendLayout();
            this.effectEditTab.SuspendLayout();
            this.effectEditorContainer.Panel1.SuspendLayout();
            this.effectEditorContainer.Panel2.SuspendLayout();
            this.effectEditorContainer.SuspendLayout();
            this.effectEditorTabPages.SuspendLayout();
            this.componentsTab.SuspendLayout();
            this.parametersTab.SuspendLayout();
            this.vertexShadersTab.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.pixelShadersTab.SuspendLayout();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.techniquesTab.SuspendLayout();
            this.splitContainer4.Panel1.SuspendLayout();
            this.splitContainer4.Panel2.SuspendLayout();
            this.splitContainer4.SuspendLayout();
            this.effectTechniqueEditor.SuspendLayout();
            this.effectTechniquePassesEditor.Panel1.SuspendLayout();
            this.effectTechniquePassesEditor.Panel2.SuspendLayout();
            this.effectTechniquePassesEditor.SuspendLayout();
            this.componentTab.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tabControl);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.modelViewPanel);
            this.splitContainer1.Size = new System.Drawing.Size(778, 488);
            this.splitContainer1.SplitterDistance = 221;
            this.splitContainer1.TabIndex = 1;
            // 
            // tabControl
            // 
            this.tabControl.Controls.Add(this.viewTab);
            this.tabControl.Controls.Add(this.modelPage);
            this.tabControl.Controls.Add(this.effectTab);
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(0, 0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(221, 488);
            this.tabControl.TabIndex = 4;
            // 
            // viewTab
            // 
            this.viewTab.Controls.Add(this.blueCustomColorBar);
            this.viewTab.Controls.Add(this.greenCustomColorBar);
            this.viewTab.Controls.Add(this.BLabel);
            this.viewTab.Controls.Add(this.bgColorLabel);
            this.viewTab.Controls.Add(this.redCustomColorBar);
            this.viewTab.Controls.Add(this.GLabel);
            this.viewTab.Controls.Add(this.RLabel);
            this.viewTab.Controls.Add(this.bgColorComboBox);
            this.viewTab.Location = new System.Drawing.Point(4, 22);
            this.viewTab.Name = "viewTab";
            this.viewTab.Padding = new System.Windows.Forms.Padding(3);
            this.viewTab.Size = new System.Drawing.Size(213, 462);
            this.viewTab.TabIndex = 0;
            this.viewTab.Text = "View";
            this.viewTab.UseVisualStyleBackColor = true;
            // 
            // blueCustomColorBar
            // 
            this.blueCustomColorBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.blueCustomColorBar.BackColor = System.Drawing.SystemColors.Window;
            this.blueCustomColorBar.Enabled = false;
            this.blueCustomColorBar.Location = new System.Drawing.Point(22, 98);
            this.blueCustomColorBar.Maximum = 255;
            this.blueCustomColorBar.Name = "blueCustomColorBar";
            this.blueCustomColorBar.Size = new System.Drawing.Size(185, 45);
            this.blueCustomColorBar.TabIndex = 6;
            this.blueCustomColorBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.blueCustomColorBar.Scroll += new System.EventHandler(this.CustomColorBar_Scroll);
            // 
            // greenCustomColorBar
            // 
            this.greenCustomColorBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.greenCustomColorBar.BackColor = System.Drawing.SystemColors.Window;
            this.greenCustomColorBar.Enabled = false;
            this.greenCustomColorBar.Location = new System.Drawing.Point(22, 72);
            this.greenCustomColorBar.Maximum = 255;
            this.greenCustomColorBar.Name = "greenCustomColorBar";
            this.greenCustomColorBar.Size = new System.Drawing.Size(185, 45);
            this.greenCustomColorBar.TabIndex = 5;
            this.greenCustomColorBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.greenCustomColorBar.Scroll += new System.EventHandler(this.CustomColorBar_Scroll);
            // 
            // BLabel
            // 
            this.BLabel.AutoSize = true;
            this.BLabel.Enabled = false;
            this.BLabel.Location = new System.Drawing.Point(4, 102);
            this.BLabel.Name = "BLabel";
            this.BLabel.Size = new System.Drawing.Size(14, 13);
            this.BLabel.TabIndex = 9;
            this.BLabel.Text = "B";
            // 
            // bgColorLabel
            // 
            this.bgColorLabel.AutoSize = true;
            this.bgColorLabel.Location = new System.Drawing.Point(4, 3);
            this.bgColorLabel.Name = "bgColorLabel";
            this.bgColorLabel.Size = new System.Drawing.Size(92, 13);
            this.bgColorLabel.TabIndex = 2;
            this.bgColorLabel.Text = "Background Color";
            // 
            // redCustomColorBar
            // 
            this.redCustomColorBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.redCustomColorBar.BackColor = System.Drawing.SystemColors.Window;
            this.redCustomColorBar.Enabled = false;
            this.redCustomColorBar.Location = new System.Drawing.Point(22, 46);
            this.redCustomColorBar.Maximum = 255;
            this.redCustomColorBar.Name = "redCustomColorBar";
            this.redCustomColorBar.Size = new System.Drawing.Size(185, 45);
            this.redCustomColorBar.TabIndex = 4;
            this.redCustomColorBar.TickStyle = System.Windows.Forms.TickStyle.None;
            this.redCustomColorBar.Scroll += new System.EventHandler(this.CustomColorBar_Scroll);
            // 
            // GLabel
            // 
            this.GLabel.AutoSize = true;
            this.GLabel.Enabled = false;
            this.GLabel.Location = new System.Drawing.Point(4, 76);
            this.GLabel.Name = "GLabel";
            this.GLabel.Size = new System.Drawing.Size(15, 13);
            this.GLabel.TabIndex = 8;
            this.GLabel.Text = "G";
            // 
            // RLabel
            // 
            this.RLabel.AutoSize = true;
            this.RLabel.Enabled = false;
            this.RLabel.Location = new System.Drawing.Point(4, 50);
            this.RLabel.Name = "RLabel";
            this.RLabel.Size = new System.Drawing.Size(15, 13);
            this.RLabel.TabIndex = 7;
            this.RLabel.Text = "R";
            // 
            // bgColorComboBox
            // 
            this.bgColorComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.bgColorComboBox.FormattingEnabled = true;
            this.bgColorComboBox.Items.AddRange(new object[] {
            "Black",
            "White",
            "Red",
            "Orange",
            "Yellow",
            "Green",
            "Blue",
            "Purple",
            "Custom..."});
            this.bgColorComboBox.Location = new System.Drawing.Point(4, 19);
            this.bgColorComboBox.Name = "bgColorComboBox";
            this.bgColorComboBox.Size = new System.Drawing.Size(203, 21);
            this.bgColorComboBox.TabIndex = 3;
            this.bgColorComboBox.Text = "Black";
            this.bgColorComboBox.SelectedIndexChanged += new System.EventHandler(this.bgColorComboBox_SelectedIndexChanged);
            // 
            // modelPage
            // 
            this.modelPage.Controls.Add(this.modelSelectionBox);
            this.modelPage.Controls.Add(this.modelPropertyGrid);
            this.modelPage.Controls.Add(this.currentModelLabel);
            this.modelPage.Location = new System.Drawing.Point(4, 22);
            this.modelPage.Name = "modelPage";
            this.modelPage.Padding = new System.Windows.Forms.Padding(3);
            this.modelPage.Size = new System.Drawing.Size(213, 462);
            this.modelPage.TabIndex = 1;
            this.modelPage.Text = "Model";
            this.modelPage.UseVisualStyleBackColor = true;
            // 
            // modelSelectionBox
            // 
            this.modelSelectionBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.modelSelectionBox.FormattingEnabled = true;
            this.modelSelectionBox.Location = new System.Drawing.Point(6, 19);
            this.modelSelectionBox.Name = "modelSelectionBox";
            this.modelSelectionBox.Size = new System.Drawing.Size(201, 21);
            this.modelSelectionBox.TabIndex = 0;
            this.modelSelectionBox.SelectedIndexChanged += new System.EventHandler(this.modelSelectionBox_SelectedIndexChanged);
            // 
            // modelPropertyGrid
            // 
            this.modelPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.modelPropertyGrid.BackColor = System.Drawing.SystemColors.Window;
            this.modelPropertyGrid.CommandsBackColor = System.Drawing.SystemColors.ControlLightLight;
            this.modelPropertyGrid.Location = new System.Drawing.Point(6, 46);
            this.modelPropertyGrid.Name = "modelPropertyGrid";
            this.modelPropertyGrid.Size = new System.Drawing.Size(201, 209);
            this.modelPropertyGrid.TabIndex = 3;
            // 
            // currentModelLabel
            // 
            this.currentModelLabel.AutoSize = true;
            this.currentModelLabel.Location = new System.Drawing.Point(3, 3);
            this.currentModelLabel.Name = "currentModelLabel";
            this.currentModelLabel.Size = new System.Drawing.Size(73, 13);
            this.currentModelLabel.TabIndex = 1;
            this.currentModelLabel.Text = "Current Model";
            // 
            // effectTab
            // 
            this.effectTab.Controls.Add(this.effectPropertyGrid);
            this.effectTab.Location = new System.Drawing.Point(4, 22);
            this.effectTab.Name = "effectTab";
            this.effectTab.Padding = new System.Windows.Forms.Padding(3);
            this.effectTab.Size = new System.Drawing.Size(213, 462);
            this.effectTab.TabIndex = 2;
            this.effectTab.Text = "Effect";
            this.effectTab.UseVisualStyleBackColor = true;
            // 
            // effectPropertyGrid
            // 
            this.effectPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.effectPropertyGrid.Location = new System.Drawing.Point(9, 48);
            this.effectPropertyGrid.Name = "effectPropertyGrid";
            this.effectPropertyGrid.Size = new System.Drawing.Size(198, 408);
            this.effectPropertyGrid.TabIndex = 1;
            // 
            // modelViewPanel
            // 
            this.modelViewPanel.BackgroundColor = new Microsoft.Xna.Framework.Graphics.Color(((byte)(0)), ((byte)(0)), ((byte)(0)), ((byte)(255)));
            this.modelViewPanel.CurrentModel = null;
            this.modelViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.modelViewPanel.Location = new System.Drawing.Point(0, 0);
            this.modelViewPanel.Name = "modelViewPanel";
            this.modelViewPanel.Size = new System.Drawing.Size(553, 488);
            this.modelViewPanel.StatusStrip = this.statusLabel;
            this.modelViewPanel.TabIndex = 0;
            this.modelViewPanel.Text = "modelViewControl";
            this.modelViewPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.modelViewPanel_MouseDown);
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(38, 17);
            this.statusLabel.Text = "Ready";
            // 
            // mainTabControl
            // 
            this.mainTabControl.Controls.Add(this.modelViewTab);
            this.mainTabControl.Controls.Add(this.effectEditTab);
            this.mainTabControl.Controls.Add(this.componentTab);
            this.mainTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTabControl.Location = new System.Drawing.Point(0, 0);
            this.mainTabControl.Name = "mainTabControl";
            this.mainTabControl.SelectedIndex = 0;
            this.mainTabControl.Size = new System.Drawing.Size(792, 520);
            this.mainTabControl.TabIndex = 1;
            this.mainTabControl.SelectedIndexChanged += new System.EventHandler(this.mainTabControl_SelectedIndexChanged);
            // 
            // modelViewTab
            // 
            this.modelViewTab.Controls.Add(this.splitContainer1);
            this.modelViewTab.Location = new System.Drawing.Point(4, 22);
            this.modelViewTab.Name = "modelViewTab";
            this.modelViewTab.Padding = new System.Windows.Forms.Padding(3);
            this.modelViewTab.Size = new System.Drawing.Size(784, 494);
            this.modelViewTab.TabIndex = 0;
            this.modelViewTab.Text = "Viewport";
            this.modelViewTab.UseVisualStyleBackColor = true;
            // 
            // effectEditTab
            // 
            this.effectEditTab.Controls.Add(this.effectEditorContainer);
            this.effectEditTab.Location = new System.Drawing.Point(4, 22);
            this.effectEditTab.Name = "effectEditTab";
            this.effectEditTab.Padding = new System.Windows.Forms.Padding(3);
            this.effectEditTab.Size = new System.Drawing.Size(784, 494);
            this.effectEditTab.TabIndex = 1;
            this.effectEditTab.Text = "Effect Editor";
            this.effectEditTab.UseVisualStyleBackColor = true;
            // 
            // effectEditorContainer
            // 
            this.effectEditorContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.effectEditorContainer.Location = new System.Drawing.Point(3, 3);
            this.effectEditorContainer.Name = "effectEditorContainer";
            this.effectEditorContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // effectEditorContainer.Panel1
            // 
            this.effectEditorContainer.Panel1.Controls.Add(this.effectEditorTabPages);
            // 
            // effectEditorContainer.Panel2
            // 
            this.effectEditorContainer.Panel2.Controls.Add(this.effectCompileOutput);
            this.effectEditorContainer.Size = new System.Drawing.Size(778, 488);
            this.effectEditorContainer.SplitterDistance = 388;
            this.effectEditorContainer.TabIndex = 1;
            // 
            // effectEditorTabPages
            // 
            this.effectEditorTabPages.Controls.Add(this.componentsTab);
            this.effectEditorTabPages.Controls.Add(this.parametersTab);
            this.effectEditorTabPages.Controls.Add(this.vertexShadersTab);
            this.effectEditorTabPages.Controls.Add(this.pixelShadersTab);
            this.effectEditorTabPages.Controls.Add(this.techniquesTab);
            this.effectEditorTabPages.Dock = System.Windows.Forms.DockStyle.Fill;
            this.effectEditorTabPages.Location = new System.Drawing.Point(0, 0);
            this.effectEditorTabPages.Name = "effectEditorTabPages";
            this.effectEditorTabPages.SelectedIndex = 0;
            this.effectEditorTabPages.Size = new System.Drawing.Size(778, 388);
            this.effectEditorTabPages.TabIndex = 5;
            // 
            // componentsTab
            // 
            this.componentsTab.Controls.Add(this.removeEffectComponentButton);
            this.componentsTab.Controls.Add(this.addEffectComponentButton);
            this.componentsTab.Controls.Add(this.effectComponentsList);
            this.componentsTab.Controls.Add(this.compileEffectButton);
            this.componentsTab.Location = new System.Drawing.Point(4, 22);
            this.componentsTab.Name = "componentsTab";
            this.componentsTab.Padding = new System.Windows.Forms.Padding(3);
            this.componentsTab.Size = new System.Drawing.Size(770, 362);
            this.componentsTab.TabIndex = 0;
            this.componentsTab.Text = "Components";
            this.componentsTab.UseVisualStyleBackColor = true;
            // 
            // removeEffectComponentButton
            // 
            this.removeEffectComponentButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.removeEffectComponentButton.Enabled = false;
            this.removeEffectComponentButton.Location = new System.Drawing.Point(379, 289);
            this.removeEffectComponentButton.Name = "removeEffectComponentButton";
            this.removeEffectComponentButton.Size = new System.Drawing.Size(75, 23);
            this.removeEffectComponentButton.TabIndex = 7;
            this.removeEffectComponentButton.Text = "Remove";
            this.removeEffectComponentButton.UseVisualStyleBackColor = true;
            this.removeEffectComponentButton.Click += new System.EventHandler(this.removeEffectComponentButton_Click);
            // 
            // addEffectComponentButton
            // 
            this.addEffectComponentButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.addEffectComponentButton.Location = new System.Drawing.Point(298, 289);
            this.addEffectComponentButton.Name = "addEffectComponentButton";
            this.addEffectComponentButton.Size = new System.Drawing.Size(75, 23);
            this.addEffectComponentButton.TabIndex = 6;
            this.addEffectComponentButton.Text = "Add";
            this.addEffectComponentButton.UseVisualStyleBackColor = true;
            this.addEffectComponentButton.Click += new System.EventHandler(this.addEffectComponentButton_Click);
            // 
            // effectComponentsList
            // 
            this.effectComponentsList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.effectComponentsList.FormattingEnabled = true;
            this.effectComponentsList.HorizontalScrollbar = true;
            this.effectComponentsList.Location = new System.Drawing.Point(6, 6);
            this.effectComponentsList.Name = "effectComponentsList";
            this.effectComponentsList.Size = new System.Drawing.Size(758, 277);
            this.effectComponentsList.TabIndex = 5;
            this.effectComponentsList.SelectedIndexChanged += new System.EventHandler(this.effectComponentsList_SelectedIndexChanged);
            // 
            // compileEffectButton
            // 
            this.compileEffectButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.compileEffectButton.Location = new System.Drawing.Point(84, 334);
            this.compileEffectButton.Name = "compileEffectButton";
            this.compileEffectButton.Size = new System.Drawing.Size(592, 22);
            this.compileEffectButton.TabIndex = 4;
            this.compileEffectButton.Text = "Compile and Apply";
            this.compileEffectButton.UseVisualStyleBackColor = true;
            this.compileEffectButton.Click += new System.EventHandler(this.compileEffectButton_Click_1);
            // 
            // parametersTab
            // 
            this.parametersTab.Controls.Add(this.addStandardParameterButton);
            this.parametersTab.Controls.Add(this.label3);
            this.parametersTab.Controls.Add(this.standardParametersList);
            this.parametersTab.Controls.Add(this.effectParametersList);
            this.parametersTab.Location = new System.Drawing.Point(4, 22);
            this.parametersTab.Name = "parametersTab";
            this.parametersTab.Padding = new System.Windows.Forms.Padding(3);
            this.parametersTab.Size = new System.Drawing.Size(770, 362);
            this.parametersTab.TabIndex = 3;
            this.parametersTab.Text = "Parameters";
            this.parametersTab.UseVisualStyleBackColor = true;
            // 
            // addStandardParameterButton
            // 
            this.addStandardParameterButton.Location = new System.Drawing.Point(260, 167);
            this.addStandardParameterButton.Name = "addStandardParameterButton";
            this.addStandardParameterButton.Size = new System.Drawing.Size(75, 23);
            this.addStandardParameterButton.TabIndex = 3;
            this.addStandardParameterButton.Text = "<<<";
            this.addStandardParameterButton.UseVisualStyleBackColor = true;
            this.addStandardParameterButton.Click += new System.EventHandler(this.addStandardParameterButton_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(338, 6);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(109, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "Standard Parameters:";
            // 
            // standardParametersList
            // 
            this.standardParametersList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.standardParametersList.FormattingEnabled = true;
            this.standardParametersList.Location = new System.Drawing.Point(341, 22);
            this.standardParametersList.Name = "standardParametersList";
            this.standardParametersList.Size = new System.Drawing.Size(236, 329);
            this.standardParametersList.TabIndex = 1;
            this.standardParametersList.SelectedIndexChanged += new System.EventHandler(this.standardParametersList_SelectedIndexChanged);
            // 
            // effectParametersList
            // 
            this.effectParametersList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)));
            this.effectParametersList.Location = new System.Drawing.Point(3, 6);
            this.effectParametersList.Name = "effectParametersList";
            this.effectParametersList.Parameters = null;
            this.effectParametersList.SemanticEnabled = false;
            this.effectParametersList.Semantics = null;
            this.effectParametersList.Size = new System.Drawing.Size(248, 350);
            this.effectParametersList.TabIndex = 0;
            // 
            // vertexShadersTab
            // 
            this.vertexShadersTab.Controls.Add(this.splitContainer2);
            this.vertexShadersTab.Location = new System.Drawing.Point(4, 22);
            this.vertexShadersTab.Name = "vertexShadersTab";
            this.vertexShadersTab.Padding = new System.Windows.Forms.Padding(3);
            this.vertexShadersTab.Size = new System.Drawing.Size(770, 362);
            this.vertexShadersTab.TabIndex = 1;
            this.vertexShadersTab.Text = "Vertex Shaders";
            this.vertexShadersTab.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(3, 3);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.removeVertexShaderButton);
            this.splitContainer2.Panel1.Controls.Add(this.addVertexShaderButton);
            this.splitContainer2.Panel1.Controls.Add(this.label1);
            this.splitContainer2.Panel1.Controls.Add(this.vertexShadersList);
            this.splitContainer2.Panel1.Controls.Add(this.effectEditBox);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.vertexShaderEditor);
            this.splitContainer2.Size = new System.Drawing.Size(764, 356);
            this.splitContainer2.SplitterDistance = 168;
            this.splitContainer2.TabIndex = 4;
            // 
            // removeVertexShaderButton
            // 
            this.removeVertexShaderButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.removeVertexShaderButton.Location = new System.Drawing.Point(94, 327);
            this.removeVertexShaderButton.Name = "removeVertexShaderButton";
            this.removeVertexShaderButton.Size = new System.Drawing.Size(71, 23);
            this.removeVertexShaderButton.TabIndex = 11;
            this.removeVertexShaderButton.Text = "Remove";
            this.removeVertexShaderButton.UseVisualStyleBackColor = true;
            this.removeVertexShaderButton.Click += new System.EventHandler(this.removeVertexShaderButton_Click);
            // 
            // addVertexShaderButton
            // 
            this.addVertexShaderButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.addVertexShaderButton.Location = new System.Drawing.Point(5, 327);
            this.addVertexShaderButton.Name = "addVertexShaderButton";
            this.addVertexShaderButton.Size = new System.Drawing.Size(83, 23);
            this.addVertexShaderButton.TabIndex = 10;
            this.addVertexShaderButton.Text = "Add";
            this.addVertexShaderButton.UseVisualStyleBackColor = true;
            this.addVertexShaderButton.Click += new System.EventHandler(this.addVertexShaderButton_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 13);
            this.label1.TabIndex = 6;
            this.label1.Text = "Vertex Shaders";
            // 
            // vertexShadersList
            // 
            this.vertexShadersList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.vertexShadersList.FormattingEnabled = true;
            this.vertexShadersList.Location = new System.Drawing.Point(5, 18);
            this.vertexShadersList.Name = "vertexShadersList";
            this.vertexShadersList.Size = new System.Drawing.Size(161, 342);
            this.vertexShadersList.TabIndex = 4;
            this.vertexShadersList.SelectedIndexChanged += new System.EventHandler(this.vertexShadersList_SelectedIndexChanged);
            // 
            // effectEditBox
            // 
            this.effectEditBox.AcceptsTab = true;
            this.effectEditBox.DetectUrls = false;
            this.effectEditBox.Font = new System.Drawing.Font("Courier New", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.effectEditBox.Location = new System.Drawing.Point(5, 362);
            this.effectEditBox.Name = "effectEditBox";
            this.effectEditBox.Size = new System.Drawing.Size(189, 23);
            this.effectEditBox.TabIndex = 2;
            this.effectEditBox.Text = "";
            this.effectEditBox.WordWrap = false;
            // 
            // vertexShaderEditor
            // 
            this.vertexShaderEditor.AllowComponentUsage = false;
            this.vertexShaderEditor.AllowShaderSelection = false;
            this.vertexShaderEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.vertexShaderEditor.Enabled = false;
            this.vertexShaderEditor.IsPixelShader = false;
            this.vertexShaderEditor.IsVertexShader = true;
            this.vertexShaderEditor.Location = new System.Drawing.Point(0, 0);
            this.vertexShaderEditor.Name = "vertexShaderEditor";
            this.vertexShaderEditor.SemanticEnabled = true;
            this.vertexShaderEditor.Size = new System.Drawing.Size(592, 356);
            this.vertexShaderEditor.TabIndex = 0;
            this.vertexShaderEditor.ComponentChanged += new System.EventHandler(this.vertexShaderEditor_ComponentChanged);
            // 
            // pixelShadersTab
            // 
            this.pixelShadersTab.Controls.Add(this.splitContainer3);
            this.pixelShadersTab.Location = new System.Drawing.Point(4, 22);
            this.pixelShadersTab.Name = "pixelShadersTab";
            this.pixelShadersTab.Padding = new System.Windows.Forms.Padding(3);
            this.pixelShadersTab.Size = new System.Drawing.Size(770, 362);
            this.pixelShadersTab.TabIndex = 2;
            this.pixelShadersTab.Text = "Pixel Shaders";
            this.pixelShadersTab.UseVisualStyleBackColor = true;
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(3, 3);
            this.splitContainer3.Name = "splitContainer3";
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.removePixelShaderButton);
            this.splitContainer3.Panel1.Controls.Add(this.addPixelShaderButton);
            this.splitContainer3.Panel1.Controls.Add(this.label2);
            this.splitContainer3.Panel1.Controls.Add(this.pixelShadersList);
            this.splitContainer3.Panel1.Controls.Add(this.richTextBox1);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.pixelShaderEditor);
            this.splitContainer3.Size = new System.Drawing.Size(764, 356);
            this.splitContainer3.SplitterDistance = 168;
            this.splitContainer3.TabIndex = 5;
            // 
            // removePixelShaderButton
            // 
            this.removePixelShaderButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.removePixelShaderButton.Location = new System.Drawing.Point(93, 327);
            this.removePixelShaderButton.Name = "removePixelShaderButton";
            this.removePixelShaderButton.Size = new System.Drawing.Size(72, 23);
            this.removePixelShaderButton.TabIndex = 11;
            this.removePixelShaderButton.Text = "Remove";
            this.removePixelShaderButton.UseVisualStyleBackColor = true;
            this.removePixelShaderButton.Click += new System.EventHandler(this.removePixelShaderButton_Click);
            // 
            // addPixelShaderButton
            // 
            this.addPixelShaderButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.addPixelShaderButton.Location = new System.Drawing.Point(5, 327);
            this.addPixelShaderButton.Name = "addPixelShaderButton";
            this.addPixelShaderButton.Size = new System.Drawing.Size(82, 23);
            this.addPixelShaderButton.TabIndex = 10;
            this.addPixelShaderButton.Text = "Add";
            this.addPixelShaderButton.UseVisualStyleBackColor = true;
            this.addPixelShaderButton.Click += new System.EventHandler(this.addPixelShaderButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(71, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Pixel Shaders";
            // 
            // pixelShadersList
            // 
            this.pixelShadersList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.pixelShadersList.FormattingEnabled = true;
            this.pixelShadersList.Location = new System.Drawing.Point(5, 18);
            this.pixelShadersList.Name = "pixelShadersList";
            this.pixelShadersList.Size = new System.Drawing.Size(161, 342);
            this.pixelShadersList.TabIndex = 4;
            this.pixelShadersList.SelectedIndexChanged += new System.EventHandler(this.pixelShadersList_SelectedIndexChanged);
            // 
            // richTextBox1
            // 
            this.richTextBox1.AcceptsTab = true;
            this.richTextBox1.DetectUrls = false;
            this.richTextBox1.Font = new System.Drawing.Font("Courier New", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.richTextBox1.Location = new System.Drawing.Point(5, 362);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(189, 23);
            this.richTextBox1.TabIndex = 2;
            this.richTextBox1.Text = "";
            this.richTextBox1.WordWrap = false;
            // 
            // pixelShaderEditor
            // 
            this.pixelShaderEditor.AllowComponentUsage = false;
            this.pixelShaderEditor.AllowShaderSelection = false;
            this.pixelShaderEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pixelShaderEditor.Enabled = false;
            this.pixelShaderEditor.IsPixelShader = false;
            this.pixelShaderEditor.IsVertexShader = false;
            this.pixelShaderEditor.Location = new System.Drawing.Point(0, 0);
            this.pixelShaderEditor.Name = "pixelShaderEditor";
            this.pixelShaderEditor.SemanticEnabled = true;
            this.pixelShaderEditor.Size = new System.Drawing.Size(592, 356);
            this.pixelShaderEditor.TabIndex = 0;
            this.pixelShaderEditor.ComponentChanged += new System.EventHandler(this.pixelShaderEditor_ComponentChanged);
            // 
            // techniquesTab
            // 
            this.techniquesTab.Controls.Add(this.splitContainer4);
            this.techniquesTab.Location = new System.Drawing.Point(4, 22);
            this.techniquesTab.Name = "techniquesTab";
            this.techniquesTab.Padding = new System.Windows.Forms.Padding(3);
            this.techniquesTab.Size = new System.Drawing.Size(770, 362);
            this.techniquesTab.TabIndex = 4;
            this.techniquesTab.Text = "Techniques";
            this.techniquesTab.UseVisualStyleBackColor = true;
            // 
            // splitContainer4
            // 
            this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer4.Location = new System.Drawing.Point(3, 3);
            this.splitContainer4.Name = "splitContainer4";
            // 
            // splitContainer4.Panel1
            // 
            this.splitContainer4.Panel1.Controls.Add(this.removeTechniqueButton);
            this.splitContainer4.Panel1.Controls.Add(this.addTechniqueButton);
            this.splitContainer4.Panel1.Controls.Add(this.label4);
            this.splitContainer4.Panel1.Controls.Add(this.effectTechniquesList);
            // 
            // splitContainer4.Panel2
            // 
            this.splitContainer4.Panel2.Controls.Add(this.effectTechniqueEditor);
            this.splitContainer4.Size = new System.Drawing.Size(764, 356);
            this.splitContainer4.SplitterDistance = 248;
            this.splitContainer4.TabIndex = 0;
            // 
            // removeTechniqueButton
            // 
            this.removeTechniqueButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.removeTechniqueButton.Location = new System.Drawing.Point(170, 330);
            this.removeTechniqueButton.Name = "removeTechniqueButton";
            this.removeTechniqueButton.Size = new System.Drawing.Size(75, 23);
            this.removeTechniqueButton.TabIndex = 3;
            this.removeTechniqueButton.Text = "Remove";
            this.removeTechniqueButton.UseVisualStyleBackColor = true;
            this.removeTechniqueButton.Click += new System.EventHandler(this.removeTechniqueButton_Click);
            // 
            // addTechniqueButton
            // 
            this.addTechniqueButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.addTechniqueButton.Location = new System.Drawing.Point(3, 330);
            this.addTechniqueButton.Name = "addTechniqueButton";
            this.addTechniqueButton.Size = new System.Drawing.Size(75, 23);
            this.addTechniqueButton.TabIndex = 2;
            this.addTechniqueButton.Text = "Add";
            this.addTechniqueButton.UseVisualStyleBackColor = true;
            this.addTechniqueButton.Click += new System.EventHandler(this.addTechniqueButton_Click);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(0, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(66, 13);
            this.label4.TabIndex = 1;
            this.label4.Text = "Techniques:";
            // 
            // effectTechniquesList
            // 
            this.effectTechniquesList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.effectTechniquesList.FormattingEnabled = true;
            this.effectTechniquesList.Location = new System.Drawing.Point(3, 16);
            this.effectTechniquesList.Name = "effectTechniquesList";
            this.effectTechniquesList.Size = new System.Drawing.Size(242, 303);
            this.effectTechniquesList.TabIndex = 0;
            this.effectTechniquesList.SelectedIndexChanged += new System.EventHandler(this.effectTechniquesList_SelectedIndexChanged);
            // 
            // effectTechniqueEditor
            // 
            this.effectTechniqueEditor.Controls.Add(this.techniqueNameBox);
            this.effectTechniqueEditor.Controls.Add(this.label6);
            this.effectTechniqueEditor.Controls.Add(this.effectTechniquePassesEditor);
            this.effectTechniqueEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.effectTechniqueEditor.Location = new System.Drawing.Point(0, 0);
            this.effectTechniqueEditor.Name = "effectTechniqueEditor";
            this.effectTechniqueEditor.Size = new System.Drawing.Size(512, 356);
            this.effectTechniqueEditor.TabIndex = 1;
            // 
            // techniqueNameBox
            // 
            this.techniqueNameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.techniqueNameBox.Location = new System.Drawing.Point(101, 3);
            this.techniqueNameBox.Name = "techniqueNameBox";
            this.techniqueNameBox.Size = new System.Drawing.Size(408, 20);
            this.techniqueNameBox.TabIndex = 5;
            this.techniqueNameBox.TextChanged += new System.EventHandler(this.techniqueNameBox_TextChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 6);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(92, 13);
            this.label6.TabIndex = 4;
            this.label6.Text = "Technique Name:";
            // 
            // effectTechniquePassesEditor
            // 
            this.effectTechniquePassesEditor.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.effectTechniquePassesEditor.Location = new System.Drawing.Point(2, 29);
            this.effectTechniquePassesEditor.Name = "effectTechniquePassesEditor";
            // 
            // effectTechniquePassesEditor.Panel1
            // 
            this.effectTechniquePassesEditor.Panel1.Controls.Add(this.removePassButton);
            this.effectTechniquePassesEditor.Panel1.Controls.Add(this.effectTechniquePassesList);
            this.effectTechniquePassesEditor.Panel1.Controls.Add(this.addPassButton);
            this.effectTechniquePassesEditor.Panel1.Controls.Add(this.label5);
            // 
            // effectTechniquePassesEditor.Panel2
            // 
            this.effectTechniquePassesEditor.Panel2.Controls.Add(this.passPixelShaderProfileBox);
            this.effectTechniquePassesEditor.Panel2.Controls.Add(this.label11);
            this.effectTechniquePassesEditor.Panel2.Controls.Add(this.passPixelShaderBox);
            this.effectTechniquePassesEditor.Panel2.Controls.Add(this.passVertexShaderProfileBox);
            this.effectTechniquePassesEditor.Panel2.Controls.Add(this.label10);
            this.effectTechniquePassesEditor.Panel2.Controls.Add(this.label9);
            this.effectTechniquePassesEditor.Panel2.Controls.Add(this.label8);
            this.effectTechniquePassesEditor.Panel2.Controls.Add(this.passVertexShaderBox);
            this.effectTechniquePassesEditor.Panel2.Controls.Add(this.passNameBox);
            this.effectTechniquePassesEditor.Panel2.Controls.Add(this.label7);
            this.effectTechniquePassesEditor.Panel2.Enabled = false;
            this.effectTechniquePassesEditor.Size = new System.Drawing.Size(507, 330);
            this.effectTechniquePassesEditor.SplitterDistance = 217;
            this.effectTechniquePassesEditor.TabIndex = 0;
            // 
            // removePassButton
            // 
            this.removePassButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.removePassButton.Location = new System.Drawing.Point(139, 304);
            this.removePassButton.Name = "removePassButton";
            this.removePassButton.Size = new System.Drawing.Size(75, 23);
            this.removePassButton.TabIndex = 7;
            this.removePassButton.Text = "Remove";
            this.removePassButton.UseVisualStyleBackColor = true;
            this.removePassButton.Click += new System.EventHandler(this.removePassButton_Click);
            // 
            // effectTechniquePassesList
            // 
            this.effectTechniquePassesList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.effectTechniquePassesList.FormattingEnabled = true;
            this.effectTechniquePassesList.Location = new System.Drawing.Point(3, 16);
            this.effectTechniquePassesList.Name = "effectTechniquePassesList";
            this.effectTechniquePassesList.Size = new System.Drawing.Size(211, 277);
            this.effectTechniquePassesList.TabIndex = 4;
            this.effectTechniquePassesList.SelectedIndexChanged += new System.EventHandler(this.effectTechniquePassesList_SelectedIndexChanged);
            // 
            // addPassButton
            // 
            this.addPassButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.addPassButton.Location = new System.Drawing.Point(3, 304);
            this.addPassButton.Name = "addPassButton";
            this.addPassButton.Size = new System.Drawing.Size(75, 23);
            this.addPassButton.TabIndex = 6;
            this.addPassButton.Text = "Add";
            this.addPassButton.UseVisualStyleBackColor = true;
            this.addPassButton.Click += new System.EventHandler(this.addPassButton_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(0, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(44, 13);
            this.label5.TabIndex = 5;
            this.label5.Text = "Passes:";
            // 
            // passPixelShaderProfileBox
            // 
            this.passPixelShaderProfileBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.passPixelShaderProfileBox.FormattingEnabled = true;
            this.passPixelShaderProfileBox.Location = new System.Drawing.Point(230, 119);
            this.passPixelShaderProfileBox.Name = "passPixelShaderProfileBox";
            this.passPixelShaderProfileBox.Size = new System.Drawing.Size(53, 21);
            this.passPixelShaderProfileBox.TabIndex = 15;
            this.passPixelShaderProfileBox.SelectedIndexChanged += new System.EventHandler(this.pass_Changed);
            // 
            // label11
            // 
            this.label11.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(185, 126);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(39, 13);
            this.label11.TabIndex = 14;
            this.label11.Text = "Profile:";
            // 
            // passPixelShaderBox
            // 
            this.passPixelShaderBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.passPixelShaderBox.FormattingEnabled = true;
            this.passPixelShaderBox.Location = new System.Drawing.Point(6, 119);
            this.passPixelShaderBox.Name = "passPixelShaderBox";
            this.passPixelShaderBox.Size = new System.Drawing.Size(173, 21);
            this.passPixelShaderBox.TabIndex = 13;
            this.passPixelShaderBox.SelectedIndexChanged += new System.EventHandler(this.pass_Changed);
            // 
            // passVertexShaderProfileBox
            // 
            this.passVertexShaderProfileBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.passVertexShaderProfileBox.FormattingEnabled = true;
            this.passVertexShaderProfileBox.Location = new System.Drawing.Point(230, 59);
            this.passVertexShaderProfileBox.Name = "passVertexShaderProfileBox";
            this.passVertexShaderProfileBox.Size = new System.Drawing.Size(53, 21);
            this.passVertexShaderProfileBox.TabIndex = 12;
            this.passVertexShaderProfileBox.SelectedIndexChanged += new System.EventHandler(this.pass_Changed);
            // 
            // label10
            // 
            this.label10.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(185, 66);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(39, 13);
            this.label10.TabIndex = 11;
            this.label10.Text = "Profile:";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(3, 103);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(69, 13);
            this.label9.TabIndex = 10;
            this.label9.Text = "Pixel Shader:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 43);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(77, 13);
            this.label8.TabIndex = 9;
            this.label8.Text = "Vertex Shader:";
            // 
            // passVertexShaderBox
            // 
            this.passVertexShaderBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.passVertexShaderBox.FormattingEnabled = true;
            this.passVertexShaderBox.Location = new System.Drawing.Point(6, 59);
            this.passVertexShaderBox.Name = "passVertexShaderBox";
            this.passVertexShaderBox.Size = new System.Drawing.Size(173, 21);
            this.passVertexShaderBox.TabIndex = 8;
            this.passVertexShaderBox.SelectedIndexChanged += new System.EventHandler(this.pass_Changed);
            // 
            // passNameBox
            // 
            this.passNameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.passNameBox.Location = new System.Drawing.Point(73, 3);
            this.passNameBox.Name = "passNameBox";
            this.passNameBox.Size = new System.Drawing.Size(210, 20);
            this.passNameBox.TabIndex = 7;
            this.passNameBox.TextChanged += new System.EventHandler(this.pass_Changed);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 6);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(64, 13);
            this.label7.TabIndex = 6;
            this.label7.Text = "Pass Name:";
            // 
            // effectCompileOutput
            // 
            this.effectCompileOutput.Dock = System.Windows.Forms.DockStyle.Fill;
            this.effectCompileOutput.Location = new System.Drawing.Point(0, 0);
            this.effectCompileOutput.Name = "effectCompileOutput";
            this.effectCompileOutput.ReadOnly = true;
            this.effectCompileOutput.Size = new System.Drawing.Size(778, 96);
            this.effectCompileOutput.TabIndex = 0;
            this.effectCompileOutput.Text = "";
            // 
            // componentTab
            // 
            this.componentTab.Controls.Add(this.componentEditor);
            this.componentTab.Controls.Add(this.hlslInfoBox);
            this.componentTab.Location = new System.Drawing.Point(4, 22);
            this.componentTab.Name = "componentTab";
            this.componentTab.Padding = new System.Windows.Forms.Padding(3);
            this.componentTab.Size = new System.Drawing.Size(784, 494);
            this.componentTab.TabIndex = 2;
            this.componentTab.Text = "Component Editor";
            this.componentTab.UseVisualStyleBackColor = true;
            // 
            // componentEditor
            // 
            this.componentEditor.AllowComponentUsage = false;
            this.componentEditor.AllowShaderSelection = false;
            this.componentEditor.Dock = System.Windows.Forms.DockStyle.Fill;
            this.componentEditor.IsPixelShader = false;
            this.componentEditor.IsVertexShader = false;
            this.componentEditor.Location = new System.Drawing.Point(3, 3);
            this.componentEditor.Name = "componentEditor";
            this.componentEditor.SemanticEnabled = false;
            this.componentEditor.Size = new System.Drawing.Size(778, 488);
            this.componentEditor.TabIndex = 5;
            // 
            // hlslInfoBox
            // 
            this.hlslInfoBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.hlslInfoBox.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.hlslInfoBox.Location = new System.Drawing.Point(510, -12268);
            this.hlslInfoBox.Name = "hlslInfoBox";
            this.hlslInfoBox.Size = new System.Drawing.Size(0, 75);
            this.hlslInfoBox.TabIndex = 0;
            this.hlslInfoBox.Text = "";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.effectToolStripMenuItem,
            this.componentToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(792, 24);
            this.menuStrip1.TabIndex = 2;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.exitToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.exitToolStripMenuItem.Text = "E&xit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // effectToolStripMenuItem
            // 
            this.effectToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadEffectToolStripMenuItem,
            this.saveEffectToolStripMenuItem});
            this.effectToolStripMenuItem.Name = "effectToolStripMenuItem";
            this.effectToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.effectToolStripMenuItem.Text = "&Effect";
            // 
            // loadEffectToolStripMenuItem
            // 
            this.loadEffectToolStripMenuItem.Name = "loadEffectToolStripMenuItem";
            this.loadEffectToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            this.loadEffectToolStripMenuItem.Text = "&Load";
            this.loadEffectToolStripMenuItem.Click += new System.EventHandler(this.loadEffectToolStripMenuItem_Click);
            // 
            // saveEffectToolStripMenuItem
            // 
            this.saveEffectToolStripMenuItem.Name = "saveEffectToolStripMenuItem";
            this.saveEffectToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            this.saveEffectToolStripMenuItem.Text = "&Save";
            this.saveEffectToolStripMenuItem.Click += new System.EventHandler(this.saveEffectToolStripMenuItem_Click);
            // 
            // componentToolStripMenuItem
            // 
            this.componentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.newComponentToolStripMenuItem,
            this.toolStripSeparator1,
            this.loadComponentToolStripMenuItem,
            this.saveComponentToolStripMenuItem});
            this.componentToolStripMenuItem.Name = "componentToolStripMenuItem";
            this.componentToolStripMenuItem.Size = new System.Drawing.Size(74, 20);
            this.componentToolStripMenuItem.Text = "&Component";
            this.componentToolStripMenuItem.Visible = false;
            // 
            // newComponentToolStripMenuItem
            // 
            this.newComponentToolStripMenuItem.Name = "newComponentToolStripMenuItem";
            this.newComponentToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            this.newComponentToolStripMenuItem.Text = "&New";
            this.newComponentToolStripMenuItem.Click += new System.EventHandler(this.newComponentToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(106, 6);
            // 
            // loadComponentToolStripMenuItem
            // 
            this.loadComponentToolStripMenuItem.Name = "loadComponentToolStripMenuItem";
            this.loadComponentToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            this.loadComponentToolStripMenuItem.Text = "&Load";
            this.loadComponentToolStripMenuItem.Click += new System.EventHandler(this.loadComponentToolStripMenuItem_Click);
            // 
            // saveComponentToolStripMenuItem
            // 
            this.saveComponentToolStripMenuItem.Name = "saveComponentToolStripMenuItem";
            this.saveComponentToolStripMenuItem.Size = new System.Drawing.Size(109, 22);
            this.saveComponentToolStripMenuItem.Text = "&Save";
            this.saveComponentToolStripMenuItem.Click += new System.EventHandler(this.saveComponentToolStripMenuItem_Click);
            // 
            // openModelFileDialog
            // 
            this.openModelFileDialog.DefaultExt = "x";
            this.openModelFileDialog.FileName = "openFileDialog1";
            this.openModelFileDialog.Filter = "X model|*.x|Compiled Model|*.xnb";
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 544);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(792, 22);
            this.statusStrip.TabIndex = 3;
            this.statusStrip.Text = "statusStrip1";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.mainTabControl);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 24);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(792, 520);
            this.panel1.TabIndex = 4;
            // 
            // saveComponentDialog
            // 
            this.saveComponentDialog.DefaultExt = "cmpx";
            this.saveComponentDialog.Filter = "FRB Effect Component (cmpx)|*.cmpx";
            // 
            // openComponentDialog
            // 
            this.openComponentDialog.DefaultExt = "cmpx";
            this.openComponentDialog.Filter = "FRB Effect Component (cmpx)|*.cmpx";
            // 
            // saveEffectDialog
            // 
            this.saveEffectDialog.DefaultExt = "efx";
            this.saveEffectDialog.Filter = "FRB Effect (efx)|*.efx";
            // 
            // openEffectDialog
            // 
            this.openEffectDialog.DefaultExt = "efx";
            this.openEffectDialog.FileName = "openFileDialog1";
            this.openEffectDialog.Filter = "FRB Effect (efx)|*.efx";
            // 
            // EditorForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(792, 566);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "EditorForm";
            this.Text = "Effect Editor";
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.tabControl.ResumeLayout(false);
            this.viewTab.ResumeLayout(false);
            this.viewTab.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.blueCustomColorBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.greenCustomColorBar)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.redCustomColorBar)).EndInit();
            this.modelPage.ResumeLayout(false);
            this.modelPage.PerformLayout();
            this.effectTab.ResumeLayout(false);
            this.mainTabControl.ResumeLayout(false);
            this.modelViewTab.ResumeLayout(false);
            this.effectEditTab.ResumeLayout(false);
            this.effectEditorContainer.Panel1.ResumeLayout(false);
            this.effectEditorContainer.Panel2.ResumeLayout(false);
            this.effectEditorContainer.ResumeLayout(false);
            this.effectEditorTabPages.ResumeLayout(false);
            this.componentsTab.ResumeLayout(false);
            this.parametersTab.ResumeLayout(false);
            this.parametersTab.PerformLayout();
            this.vertexShadersTab.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.pixelShadersTab.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel1.PerformLayout();
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.ResumeLayout(false);
            this.techniquesTab.ResumeLayout(false);
            this.splitContainer4.Panel1.ResumeLayout(false);
            this.splitContainer4.Panel1.PerformLayout();
            this.splitContainer4.Panel2.ResumeLayout(false);
            this.splitContainer4.ResumeLayout(false);
            this.effectTechniqueEditor.ResumeLayout(false);
            this.effectTechniqueEditor.PerformLayout();
            this.effectTechniquePassesEditor.Panel1.ResumeLayout(false);
            this.effectTechniquePassesEditor.Panel1.PerformLayout();
            this.effectTechniquePassesEditor.Panel2.ResumeLayout(false);
            this.effectTechniquePassesEditor.Panel2.PerformLayout();
            this.effectTechniquePassesEditor.ResumeLayout(false);
            this.componentTab.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.Label currentModelLabel;
        private System.Windows.Forms.ComboBox modelSelectionBox;
        private EffectEditor.Controls.ModelViewControl modelViewPanel;
        private System.Windows.Forms.Label bgColorLabel;
        private System.Windows.Forms.ComboBox bgColorComboBox;
        private System.Windows.Forms.TrackBar blueCustomColorBar;
        private System.Windows.Forms.TrackBar greenCustomColorBar;
        private System.Windows.Forms.TrackBar redCustomColorBar;
        private System.Windows.Forms.Label BLabel;
        private System.Windows.Forms.Label GLabel;
        private System.Windows.Forms.Label RLabel;
        private System.Windows.Forms.OpenFileDialog openModelFileDialog;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
        private System.Windows.Forms.PropertyGrid modelPropertyGrid;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage viewTab;
        private System.Windows.Forms.TabPage modelPage;
        private System.Windows.Forms.TabControl mainTabControl;
        private System.Windows.Forms.TabPage modelViewTab;
        private System.Windows.Forms.TabPage effectEditTab;
        private System.Windows.Forms.TabPage effectTab;
        private System.Windows.Forms.SplitContainer effectEditorContainer;
        private System.Windows.Forms.RichTextBox effectEditBox;
        private System.Windows.Forms.RichTextBox effectCompileOutput;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PropertyGrid effectPropertyGrid;
        private System.Windows.Forms.TabPage componentTab;
        private System.Windows.Forms.RichTextBox hlslInfoBox;
        private System.Windows.Forms.SaveFileDialog saveComponentDialog;
        private System.Windows.Forms.ToolStripMenuItem componentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveComponentToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadComponentToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openComponentDialog;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private EffectEditor.Controls.ComponentEditor componentEditor;
        private System.Windows.Forms.ToolStripMenuItem newComponentToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ListBox vertexShadersList;
        private System.Windows.Forms.Button addVertexShaderButton;
        private System.Windows.Forms.TabControl effectEditorTabPages;
        private System.Windows.Forms.TabPage componentsTab;
        private System.Windows.Forms.Button compileEffectButton;
        private System.Windows.Forms.TabPage vertexShadersTab;
        private System.Windows.Forms.Button removeVertexShaderButton;
        private EffectEditor.Controls.ComponentEditor vertexShaderEditor;
        private System.Windows.Forms.TabPage pixelShadersTab;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.Button removePixelShaderButton;
        private System.Windows.Forms.Button addPixelShaderButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox pixelShadersList;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private EffectEditor.Controls.ComponentEditor pixelShaderEditor;
        private System.Windows.Forms.TabPage parametersTab;
        private System.Windows.Forms.TabPage techniquesTab;
        private EffectEditor.Controls.ComponentParameterList effectParametersList;
        private System.Windows.Forms.Button addStandardParameterButton;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox standardParametersList;
        private System.Windows.Forms.Button removeEffectComponentButton;
        private System.Windows.Forms.Button addEffectComponentButton;
        private System.Windows.Forms.ListBox effectComponentsList;
        private System.Windows.Forms.SplitContainer splitContainer4;
        private System.Windows.Forms.Button removeTechniqueButton;
        private System.Windows.Forms.Button addTechniqueButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox effectTechniquesList;
        private System.Windows.Forms.SplitContainer effectTechniquePassesEditor;
        private System.Windows.Forms.Button removePassButton;
        private System.Windows.Forms.ListBox effectTechniquePassesList;
        private System.Windows.Forms.Button addPassButton;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Panel effectTechniqueEditor;
        private System.Windows.Forms.TextBox techniqueNameBox;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox passNameBox;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ComboBox passPixelShaderProfileBox;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.ComboBox passPixelShaderBox;
        private System.Windows.Forms.ComboBox passVertexShaderProfileBox;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.ComboBox passVertexShaderBox;
        private System.Windows.Forms.ToolStripMenuItem effectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadEffectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveEffectToolStripMenuItem;
        private System.Windows.Forms.SaveFileDialog saveEffectDialog;
        private System.Windows.Forms.OpenFileDialog openEffectDialog;
    }
}

