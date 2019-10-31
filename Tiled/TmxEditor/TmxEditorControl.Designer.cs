namespace TmxEditor
{
    partial class TmxEditorControl
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
            this.components = new System.ComponentModel.Container();
            this.TilesetsListBox = new System.Windows.Forms.ListBox();
            this.TilesetListContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.TilesetXnaContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.LoadedTmxLabel = new System.Windows.Forms.Label();
            this.StatusLabel = new System.Windows.Forms.Label();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.TilesetsTab = new System.Windows.Forms.TabPage();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.elementHost1 = new System.Windows.Forms.Integration.ElementHost();
            this.EntitiesComboBox = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.NameTextBox = new System.Windows.Forms.TextBox();
            this.NameLabel = new System.Windows.Forms.Label();
            this.HasCollisionsCheckBox = new System.Windows.Forms.CheckBox();
            this.RemoveTilesetPropertyButton = new System.Windows.Forms.Button();
            this.AddTilesetPropertyButton = new System.Windows.Forms.Button();
            this.TilesetTilePropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.TilesetPropertiesMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.LayersTab = new System.Windows.Forms.TabPage();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.LayersListBox = new System.Windows.Forms.TreeView();
            this.LayerListContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.RemoveLayerPropertyButton = new System.Windows.Forms.Button();
            this.AddLayerPropertyButton = new System.Windows.Forms.Button();
            this.LayerPropertyGrid = new System.Windows.Forms.PropertyGrid();
            this.LayerPropertyGridContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.LevelsTab = new System.Windows.Forms.TabPage();
            this.splitContainer4 = new System.Windows.Forms.SplitContainer();
            this.NonLevelsListBox = new System.Windows.Forms.ListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.LevelsListBox = new System.Windows.Forms.ListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.LevelLabel = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.TilesetsTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.LayersTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.LevelsTab.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).BeginInit();
            this.splitContainer4.Panel1.SuspendLayout();
            this.splitContainer4.Panel2.SuspendLayout();
            this.splitContainer4.SuspendLayout();
            this.SuspendLayout();
            // 
            // TilesetsListBox
            // 
            this.TilesetsListBox.ContextMenuStrip = this.TilesetListContextMenu;
            this.TilesetsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TilesetsListBox.FormattingEnabled = true;
            this.TilesetsListBox.Location = new System.Drawing.Point(0, 0);
            this.TilesetsListBox.Name = "TilesetsListBox";
            this.TilesetsListBox.Size = new System.Drawing.Size(179, 472);
            this.TilesetsListBox.TabIndex = 6;
            this.TilesetsListBox.SelectedIndexChanged += new System.EventHandler(this.TilesetsListBox_SelectedIndexChanged);
            // 
            // TilesetListContextMenu
            // 
            this.TilesetListContextMenu.Name = "TilesetListContextMenu";
            this.TilesetListContextMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // TilesetXnaContextMenu
            // 
            this.TilesetXnaContextMenu.Name = "TilesetXnaContextMenu";
            this.TilesetXnaContextMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // LoadedTmxLabel
            // 
            this.LoadedTmxLabel.AutoSize = true;
            this.LoadedTmxLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.LoadedTmxLabel.Location = new System.Drawing.Point(0, 0);
            this.LoadedTmxLabel.Name = "LoadedTmxLabel";
            this.LoadedTmxLabel.Size = new System.Drawing.Size(86, 13);
            this.LoadedTmxLabel.TabIndex = 4;
            this.LoadedTmxLabel.Text = "No TMX Loaded";
            // 
            // StatusLabel
            // 
            this.StatusLabel.AutoSize = true;
            this.StatusLabel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.StatusLabel.Location = new System.Drawing.Point(0, 521);
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(66, 13);
            this.StatusLabel.TabIndex = 7;
            this.StatusLabel.Text = "Status Label";
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.TilesetsTab);
            this.tabControl1.Controls.Add(this.LayersTab);
            this.tabControl1.Controls.Add(this.LevelsTab);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 13);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(827, 508);
            this.tabControl1.TabIndex = 8;
            // 
            // TilesetsTab
            // 
            this.TilesetsTab.Controls.Add(this.splitContainer2);
            this.TilesetsTab.Location = new System.Drawing.Point(4, 22);
            this.TilesetsTab.Name = "TilesetsTab";
            this.TilesetsTab.Padding = new System.Windows.Forms.Padding(3);
            this.TilesetsTab.Size = new System.Drawing.Size(819, 482);
            this.TilesetsTab.TabIndex = 0;
            this.TilesetsTab.Text = "Tilesets";
            this.TilesetsTab.UseVisualStyleBackColor = true;
            // 
            // splitContainer2
            // 
            this.splitContainer2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(3, 3);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.TilesetsListBox);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer3);
            this.splitContainer2.Size = new System.Drawing.Size(813, 476);
            this.splitContainer2.SplitterDistance = 183;
            this.splitContainer2.TabIndex = 7;
            this.splitContainer2.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer2_SplitterMoved);
            // 
            // splitContainer3
            // 
            this.splitContainer3.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.elementHost1);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.EntitiesComboBox);
            this.splitContainer3.Panel2.Controls.Add(this.label1);
            this.splitContainer3.Panel2.Controls.Add(this.NameTextBox);
            this.splitContainer3.Panel2.Controls.Add(this.NameLabel);
            this.splitContainer3.Panel2.Controls.Add(this.HasCollisionsCheckBox);
            this.splitContainer3.Panel2.Controls.Add(this.RemoveTilesetPropertyButton);
            this.splitContainer3.Panel2.Controls.Add(this.AddTilesetPropertyButton);
            this.splitContainer3.Panel2.Controls.Add(this.TilesetTilePropertyGrid);
            this.splitContainer3.Size = new System.Drawing.Size(626, 476);
            this.splitContainer3.SplitterDistance = 282;
            this.splitContainer3.TabIndex = 6;
            // 
            // elementHost1
            // 
            this.elementHost1.Dock = System.Windows.Forms.DockStyle.Top;
            this.elementHost1.Location = new System.Drawing.Point(0, 0);
            this.elementHost1.Name = "elementHost1";
            this.elementHost1.Size = new System.Drawing.Size(622, 35);
            this.elementHost1.TabIndex = 6;
            this.elementHost1.Text = "elementHost1";
            this.elementHost1.Child = null;
            // 
            // EntitiesComboBox
            // 
            this.EntitiesComboBox.FormattingEnabled = true;
            this.EntitiesComboBox.Location = new System.Drawing.Point(295, 5);
            this.EntitiesComboBox.Margin = new System.Windows.Forms.Padding(2, 1, 2, 1);
            this.EntitiesComboBox.Name = "EntitiesComboBox";
            this.EntitiesComboBox.Size = new System.Drawing.Size(161, 21);
            this.EntitiesComboBox.TabIndex = 9;
            this.EntitiesComboBox.SelectedValueChanged += new System.EventHandler(this.EntitiesComboBox_SelectedValueChanged);
            this.EntitiesComboBox.Click += new System.EventHandler(this.EntitiesComboBox_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(230, 6);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(63, 13);
            this.label1.TabIndex = 8;
            this.label1.Text = "Entity Type:";
            // 
            // NameTextBox
            // 
            this.NameTextBox.AcceptsReturn = true;
            this.NameTextBox.Location = new System.Drawing.Point(47, 4);
            this.NameTextBox.Name = "NameTextBox";
            this.NameTextBox.Size = new System.Drawing.Size(172, 20);
            this.NameTextBox.TabIndex = 7;
            // 
            // NameLabel
            // 
            this.NameLabel.AutoSize = true;
            this.NameLabel.Location = new System.Drawing.Point(3, 7);
            this.NameLabel.Name = "NameLabel";
            this.NameLabel.Size = new System.Drawing.Size(38, 13);
            this.NameLabel.TabIndex = 6;
            this.NameLabel.Text = "Name:";
            // 
            // HasCollisionsCheckBox
            // 
            this.HasCollisionsCheckBox.AutoSize = true;
            this.HasCollisionsCheckBox.Location = new System.Drawing.Point(3, 27);
            this.HasCollisionsCheckBox.Name = "HasCollisionsCheckBox";
            this.HasCollisionsCheckBox.Size = new System.Drawing.Size(91, 17);
            this.HasCollisionsCheckBox.TabIndex = 5;
            this.HasCollisionsCheckBox.Text = "Has Collisions";
            this.HasCollisionsCheckBox.UseVisualStyleBackColor = true;
            this.HasCollisionsCheckBox.CheckedChanged += new System.EventHandler(this.HasCollisionsCheckBox_CheckedChanged);
            // 
            // RemoveTilesetPropertyButton
            // 
            this.RemoveTilesetPropertyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RemoveTilesetPropertyButton.Location = new System.Drawing.Point(114, 149);
            this.RemoveTilesetPropertyButton.Name = "RemoveTilesetPropertyButton";
            this.RemoveTilesetPropertyButton.Size = new System.Drawing.Size(105, 23);
            this.RemoveTilesetPropertyButton.TabIndex = 4;
            this.RemoveTilesetPropertyButton.Text = "Remove Property";
            this.RemoveTilesetPropertyButton.UseVisualStyleBackColor = true;
            this.RemoveTilesetPropertyButton.Click += new System.EventHandler(this.RemoveTilesetPropertyButton_Click);
            // 
            // AddTilesetPropertyButton
            // 
            this.AddTilesetPropertyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.AddTilesetPropertyButton.Location = new System.Drawing.Point(3, 149);
            this.AddTilesetPropertyButton.Name = "AddTilesetPropertyButton";
            this.AddTilesetPropertyButton.Size = new System.Drawing.Size(105, 23);
            this.AddTilesetPropertyButton.TabIndex = 3;
            this.AddTilesetPropertyButton.Text = "Add Property";
            this.AddTilesetPropertyButton.UseVisualStyleBackColor = true;
            this.AddTilesetPropertyButton.Click += new System.EventHandler(this.AddTilesetPropertyButton_Click);
            // 
            // TilesetTilePropertyGrid
            // 
            this.TilesetTilePropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.TilesetTilePropertyGrid.ContextMenuStrip = this.TilesetPropertiesMenuStrip;
            this.TilesetTilePropertyGrid.HelpVisible = false;
            this.TilesetTilePropertyGrid.Location = new System.Drawing.Point(0, 51);
            this.TilesetTilePropertyGrid.Name = "TilesetTilePropertyGrid";
            this.TilesetTilePropertyGrid.Size = new System.Drawing.Size(622, 93);
            this.TilesetTilePropertyGrid.TabIndex = 0;
            this.TilesetTilePropertyGrid.ToolbarVisible = false;
            // 
            // TilesetPropertiesMenuStrip
            // 
            this.TilesetPropertiesMenuStrip.Name = "TilesetPropertiesMenuStrip";
            this.TilesetPropertiesMenuStrip.Size = new System.Drawing.Size(61, 4);
            // 
            // LayersTab
            // 
            this.LayersTab.Controls.Add(this.splitContainer1);
            this.LayersTab.Location = new System.Drawing.Point(4, 22);
            this.LayersTab.Name = "LayersTab";
            this.LayersTab.Padding = new System.Windows.Forms.Padding(3);
            this.LayersTab.Size = new System.Drawing.Size(819, 482);
            this.LayersTab.TabIndex = 1;
            this.LayersTab.Text = "Layers";
            this.LayersTab.UseVisualStyleBackColor = true;
            // 
            // splitContainer1
            // 
            this.splitContainer1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.LayersListBox);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.RemoveLayerPropertyButton);
            this.splitContainer1.Panel2.Controls.Add(this.AddLayerPropertyButton);
            this.splitContainer1.Panel2.Controls.Add(this.LayerPropertyGrid);
            this.splitContainer1.Size = new System.Drawing.Size(813, 476);
            this.splitContainer1.SplitterDistance = 209;
            this.splitContainer1.TabIndex = 8;
            // 
            // LayersListBox
            // 
            this.LayersListBox.ContextMenuStrip = this.LayerListContextMenu;
            this.LayersListBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LayersListBox.HideSelection = false;
            this.LayersListBox.Location = new System.Drawing.Point(0, 0);
            this.LayersListBox.Name = "LayersListBox";
            this.LayersListBox.ShowRootLines = false;
            this.LayersListBox.Size = new System.Drawing.Size(205, 472);
            this.LayersListBox.TabIndex = 7;
            this.LayersListBox.MouseClick += new System.Windows.Forms.MouseEventHandler(this.LayersListBox_MouseClick);
            // 
            // LayerListContextMenu
            // 
            this.LayerListContextMenu.Name = "LayerListContextMenu";
            this.LayerListContextMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // RemoveLayerPropertyButton
            // 
            this.RemoveLayerPropertyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.RemoveLayerPropertyButton.Location = new System.Drawing.Point(114, 446);
            this.RemoveLayerPropertyButton.Name = "RemoveLayerPropertyButton";
            this.RemoveLayerPropertyButton.Size = new System.Drawing.Size(105, 23);
            this.RemoveLayerPropertyButton.TabIndex = 2;
            this.RemoveLayerPropertyButton.Text = "Remove Property";
            this.RemoveLayerPropertyButton.UseVisualStyleBackColor = true;
            this.RemoveLayerPropertyButton.Click += new System.EventHandler(this.RemovePropertyButton_Click);
            // 
            // AddLayerPropertyButton
            // 
            this.AddLayerPropertyButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.AddLayerPropertyButton.Location = new System.Drawing.Point(3, 446);
            this.AddLayerPropertyButton.Name = "AddLayerPropertyButton";
            this.AddLayerPropertyButton.Size = new System.Drawing.Size(105, 23);
            this.AddLayerPropertyButton.TabIndex = 1;
            this.AddLayerPropertyButton.Text = "Add Property";
            this.AddLayerPropertyButton.UseVisualStyleBackColor = true;
            this.AddLayerPropertyButton.Click += new System.EventHandler(this.AddLayerPropertyButton_Click);
            // 
            // LayerPropertyGrid
            // 
            this.LayerPropertyGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.LayerPropertyGrid.ContextMenuStrip = this.LayerPropertyGridContextMenu;
            this.LayerPropertyGrid.HelpVisible = false;
            this.LayerPropertyGrid.Location = new System.Drawing.Point(0, 0);
            this.LayerPropertyGrid.Name = "LayerPropertyGrid";
            this.LayerPropertyGrid.Size = new System.Drawing.Size(582, 444);
            this.LayerPropertyGrid.TabIndex = 0;
            this.LayerPropertyGrid.ToolbarVisible = false;
            this.LayerPropertyGrid.SelectedGridItemChanged += new System.Windows.Forms.SelectedGridItemChangedEventHandler(this.LayerPropertyGrid_SelectedGridItemChanged);
            this.LayerPropertyGrid.Click += new System.EventHandler(this.LayerPropertyGrid_Click);
            // 
            // LayerPropertyGridContextMenu
            // 
            this.LayerPropertyGridContextMenu.Name = "LayerListContextMenu";
            this.LayerPropertyGridContextMenu.Size = new System.Drawing.Size(61, 4);
            // 
            // LevelsTab
            // 
            this.LevelsTab.Controls.Add(this.splitContainer4);
            this.LevelsTab.Location = new System.Drawing.Point(4, 22);
            this.LevelsTab.Margin = new System.Windows.Forms.Padding(2);
            this.LevelsTab.Name = "LevelsTab";
            this.LevelsTab.Size = new System.Drawing.Size(819, 482);
            this.LevelsTab.TabIndex = 2;
            this.LevelsTab.Text = "Levels";
            this.LevelsTab.UseVisualStyleBackColor = true;
            // 
            // splitContainer4
            // 
            this.splitContainer4.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer4.Location = new System.Drawing.Point(0, 0);
            this.splitContainer4.Margin = new System.Windows.Forms.Padding(2);
            this.splitContainer4.Name = "splitContainer4";
            // 
            // splitContainer4.Panel1
            // 
            this.splitContainer4.Panel1.Controls.Add(this.NonLevelsListBox);
            this.splitContainer4.Panel1.Controls.Add(this.label3);
            this.splitContainer4.Panel1.Controls.Add(this.LevelsListBox);
            this.splitContainer4.Panel1.Controls.Add(this.label2);
            // 
            // splitContainer4.Panel2
            // 
            this.splitContainer4.Panel2.Controls.Add(this.comboBox1);
            this.splitContainer4.Panel2.Controls.Add(this.label4);
            this.splitContainer4.Panel2.Controls.Add(this.LevelLabel);
            this.splitContainer4.Size = new System.Drawing.Size(819, 482);
            this.splitContainer4.SplitterDistance = 184;
            this.splitContainer4.SplitterWidth = 3;
            this.splitContainer4.TabIndex = 0;
            // 
            // NonLevelsListBox
            // 
            this.NonLevelsListBox.FormattingEnabled = true;
            this.NonLevelsListBox.Location = new System.Drawing.Point(7, 226);
            this.NonLevelsListBox.Margin = new System.Windows.Forms.Padding(2);
            this.NonLevelsListBox.Name = "NonLevelsListBox";
            this.NonLevelsListBox.Size = new System.Drawing.Size(251, 147);
            this.NonLevelsListBox.TabIndex = 12;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(2, 198);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(134, 29);
            this.label3.TabIndex = 11;
            this.label3.Text = "Non Levels";
            // 
            // LevelsListBox
            // 
            this.LevelsListBox.FormattingEnabled = true;
            this.LevelsListBox.Location = new System.Drawing.Point(7, 37);
            this.LevelsListBox.Margin = new System.Windows.Forms.Padding(2);
            this.LevelsListBox.Name = "LevelsListBox";
            this.LevelsListBox.Size = new System.Drawing.Size(251, 147);
            this.LevelsListBox.TabIndex = 10;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 18F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(2, 8);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(256, 29);
            this.label2.TabIndex = 9;
            this.label2.Text = "Levels In GameScreen";
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(96, 61);
            this.comboBox1.Margin = new System.Windows.Forms.Padding(2);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(233, 21);
            this.comboBox1.TabIndex = 2;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(2, 58);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(89, 24);
            this.label4.TabIndex = 1;
            this.label4.Text = "CSV File:";
            // 
            // LevelLabel
            // 
            this.LevelLabel.AutoSize = true;
            this.LevelLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.LevelLabel.Location = new System.Drawing.Point(2, 14);
            this.LevelLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.LevelLabel.Name = "LevelLabel";
            this.LevelLabel.Size = new System.Drawing.Size(190, 24);
            this.LevelLabel.TabIndex = 0;
            this.LevelLabel.Text = "Level1.tmx Properties";
            // 
            // TmxEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.StatusLabel);
            this.Controls.Add(this.LoadedTmxLabel);
            this.Name = "TmxEditorControl";
            this.Size = new System.Drawing.Size(827, 534);
            this.tabControl1.ResumeLayout(false);
            this.TilesetsTab.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
            this.splitContainer3.ResumeLayout(false);
            this.LayersTab.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.LevelsTab.ResumeLayout(false);
            this.splitContainer4.Panel1.ResumeLayout(false);
            this.splitContainer4.Panel1.PerformLayout();
            this.splitContainer4.Panel2.ResumeLayout(false);
            this.splitContainer4.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer4)).EndInit();
            this.splitContainer4.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ListBox TilesetsListBox;
        private System.Windows.Forms.Label LoadedTmxLabel;
        private System.Windows.Forms.Label StatusLabel;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage TilesetsTab;
        private System.Windows.Forms.TabPage LayersTab;
        private System.Windows.Forms.TreeView LayersListBox;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.PropertyGrid LayerPropertyGrid;
        private System.Windows.Forms.Button RemoveLayerPropertyButton;
        private System.Windows.Forms.Button AddLayerPropertyButton;
        private System.Windows.Forms.ContextMenuStrip LayerListContextMenu;
        private System.Windows.Forms.ContextMenuStrip LayerPropertyGridContextMenu;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.SplitContainer splitContainer3;
        private System.Windows.Forms.PropertyGrid TilesetTilePropertyGrid;
        private System.Windows.Forms.Button RemoveTilesetPropertyButton;
        private System.Windows.Forms.Button AddTilesetPropertyButton;
        public System.Windows.Forms.ContextMenuStrip TilesetXnaContextMenu;
        private System.Windows.Forms.CheckBox HasCollisionsCheckBox;
        private System.Windows.Forms.TextBox NameTextBox;
        private System.Windows.Forms.Label NameLabel;
        private System.Windows.Forms.ComboBox EntitiesComboBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ContextMenuStrip TilesetPropertiesMenuStrip;
        private System.Windows.Forms.ContextMenuStrip TilesetListContextMenu;
        private System.Windows.Forms.TabPage LevelsTab;
        private System.Windows.Forms.SplitContainer splitContainer4;
        private System.Windows.Forms.ListBox NonLevelsListBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ListBox LevelsListBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label LevelLabel;
        private System.Windows.Forms.Integration.ElementHost elementHost1;
    }
}
