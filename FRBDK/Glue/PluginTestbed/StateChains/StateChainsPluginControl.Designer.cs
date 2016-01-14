namespace PluginTestbed.StateChains
{
    partial class StateChainsPluginControl
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
            this.lbStateChains = new System.Windows.Forms.ListBox();
            this.btnAddStateChain = new System.Windows.Forms.Button();
            this.btnDeleteStateChain = new System.Windows.Forms.Button();
            this.gbStateChain = new System.Windows.Forms.GroupBox();
            this.tableLayoutPanel4 = new System.Windows.Forms.TableLayoutPanel();
            this.lbStates = new System.Windows.Forms.ListBox();
            this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
            this.btnMoveDownStateChainState = new System.Windows.Forms.Button();
            this.btnMoveUpStateChainState = new System.Windows.Forms.Button();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.btnAddStateChainState = new System.Windows.Forms.Button();
            this.btnDeleteStateChainState = new System.Windows.Forms.Button();
            this.gbSelectedState = new System.Windows.Forms.GroupBox();
            this.cbState = new System.Windows.Forms.ComboBox();
            this.tbTime = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tbName = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.epMain = new System.Windows.Forms.ErrorProvider(this.components);
            this.gbStateChain.SuspendLayout();
            this.tableLayoutPanel4.SuspendLayout();
            this.tableLayoutPanel3.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.gbSelectedState.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.epMain)).BeginInit();
            this.SuspendLayout();
            // 
            // lbStateChains
            // 
            this.lbStateChains.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.lbStateChains.Enabled = false;
            this.lbStateChains.FormattingEnabled = true;
            this.lbStateChains.Location = new System.Drawing.Point(3, 3);
            this.lbStateChains.Name = "lbStateChains";
            this.lbStateChains.Size = new System.Drawing.Size(206, 134);
            this.lbStateChains.TabIndex = 0;
            this.lbStateChains.SelectedValueChanged += new System.EventHandler(this.LbStateChainsSelectedValueChanged);
            // 
            // btnAddStateChain
            // 
            this.btnAddStateChain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAddStateChain.Enabled = false;
            this.btnAddStateChain.Location = new System.Drawing.Point(0, 0);
            this.btnAddStateChain.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.btnAddStateChain.Name = "btnAddStateChain";
            this.btnAddStateChain.Size = new System.Drawing.Size(100, 23);
            this.btnAddStateChain.TabIndex = 1;
            this.btnAddStateChain.Text = "&Add";
            this.btnAddStateChain.UseVisualStyleBackColor = true;
            this.btnAddStateChain.Click += new System.EventHandler(this.BtnAddStateChainClick);
            // 
            // btnDeleteStateChain
            // 
            this.btnDeleteStateChain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDeleteStateChain.Enabled = false;
            this.btnDeleteStateChain.Location = new System.Drawing.Point(106, 0);
            this.btnDeleteStateChain.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnDeleteStateChain.Name = "btnDeleteStateChain";
            this.btnDeleteStateChain.Size = new System.Drawing.Size(100, 23);
            this.btnDeleteStateChain.TabIndex = 2;
            this.btnDeleteStateChain.Text = "&Delete";
            this.btnDeleteStateChain.UseVisualStyleBackColor = true;
            this.btnDeleteStateChain.Click += new System.EventHandler(this.BtnDeleteStateChainClick);
            // 
            // gbStateChain
            // 
            this.gbStateChain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.gbStateChain.Controls.Add(this.tableLayoutPanel4);
            this.gbStateChain.Controls.Add(this.tableLayoutPanel2);
            this.gbStateChain.Controls.Add(this.gbSelectedState);
            this.gbStateChain.Controls.Add(this.label1);
            this.gbStateChain.Controls.Add(this.tbName);
            this.gbStateChain.Location = new System.Drawing.Point(3, 172);
            this.gbStateChain.Name = "gbStateChain";
            this.gbStateChain.Size = new System.Drawing.Size(206, 225);
            this.gbStateChain.TabIndex = 3;
            this.gbStateChain.TabStop = false;
            this.gbStateChain.Text = "Selected StateChain";
            // 
            // tableLayoutPanel4
            // 
            this.tableLayoutPanel4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel4.ColumnCount = 2;
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel4.Controls.Add(this.lbStates, 0, 0);
            this.tableLayoutPanel4.Controls.Add(this.tableLayoutPanel3, 1, 0);
            this.tableLayoutPanel4.Location = new System.Drawing.Point(6, 136);
            this.tableLayoutPanel4.Name = "tableLayoutPanel4";
            this.tableLayoutPanel4.RowCount = 1;
            this.tableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel4.Size = new System.Drawing.Size(194, 58);
            this.tableLayoutPanel4.TabIndex = 7;
            // 
            // lbStates
            // 
            this.lbStates.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbStates.Enabled = false;
            this.lbStates.FormattingEnabled = true;
            this.lbStates.Location = new System.Drawing.Point(0, 0);
            this.lbStates.Margin = new System.Windows.Forms.Padding(0);
            this.lbStates.Name = "lbStates";
            this.lbStates.Size = new System.Drawing.Size(171, 58);
            this.lbStates.TabIndex = 3;
            this.lbStates.SelectedValueChanged += new System.EventHandler(this.LbStatesSelectedValueChanged);
            // 
            // tableLayoutPanel3
            // 
            this.tableLayoutPanel3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel3.ColumnCount = 1;
            this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanel3.Controls.Add(this.btnMoveDownStateChainState, 0, 1);
            this.tableLayoutPanel3.Controls.Add(this.btnMoveUpStateChainState, 0, 0);
            this.tableLayoutPanel3.Location = new System.Drawing.Point(171, 0);
            this.tableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel3.Name = "tableLayoutPanel3";
            this.tableLayoutPanel3.RowCount = 2;
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel3.Size = new System.Drawing.Size(23, 58);
            this.tableLayoutPanel3.TabIndex = 6;
            // 
            // btnMoveDownStateChainState
            // 
            this.btnMoveDownStateChainState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveDownStateChainState.Enabled = false;
            this.btnMoveDownStateChainState.Location = new System.Drawing.Point(0, 29);
            this.btnMoveDownStateChainState.Margin = new System.Windows.Forms.Padding(0);
            this.btnMoveDownStateChainState.Name = "btnMoveDownStateChainState";
            this.btnMoveDownStateChainState.Size = new System.Drawing.Size(23, 29);
            this.btnMoveDownStateChainState.TabIndex = 1;
            this.btnMoveDownStateChainState.Text = "v";
            this.btnMoveDownStateChainState.UseVisualStyleBackColor = true;
            this.btnMoveDownStateChainState.Click += new System.EventHandler(this.BtnMoveDownStateChainStateClick);
            // 
            // btnMoveUpStateChainState
            // 
            this.btnMoveUpStateChainState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMoveUpStateChainState.Enabled = false;
            this.btnMoveUpStateChainState.Location = new System.Drawing.Point(0, 0);
            this.btnMoveUpStateChainState.Margin = new System.Windows.Forms.Padding(0);
            this.btnMoveUpStateChainState.Name = "btnMoveUpStateChainState";
            this.btnMoveUpStateChainState.Size = new System.Drawing.Size(23, 29);
            this.btnMoveUpStateChainState.TabIndex = 0;
            this.btnMoveUpStateChainState.Text = "^";
            this.btnMoveUpStateChainState.UseVisualStyleBackColor = true;
            this.btnMoveUpStateChainState.Click += new System.EventHandler(this.BtnMoveUpStateChainStateClick);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel2.ColumnCount = 2;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel2.Controls.Add(this.btnAddStateChainState, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnDeleteStateChainState, 1, 0);
            this.tableLayoutPanel2.Location = new System.Drawing.Point(6, 197);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel2.Size = new System.Drawing.Size(194, 23);
            this.tableLayoutPanel2.TabIndex = 5;
            // 
            // btnAddStateChainState
            // 
            this.btnAddStateChainState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAddStateChainState.Enabled = false;
            this.btnAddStateChainState.Location = new System.Drawing.Point(0, 0);
            this.btnAddStateChainState.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.btnAddStateChainState.Name = "btnAddStateChainState";
            this.btnAddStateChainState.Size = new System.Drawing.Size(94, 23);
            this.btnAddStateChainState.TabIndex = 1;
            this.btnAddStateChainState.Text = "&Add";
            this.btnAddStateChainState.UseVisualStyleBackColor = true;
            this.btnAddStateChainState.Click += new System.EventHandler(this.BtnAddStateChainStateClick);
            // 
            // btnDeleteStateChainState
            // 
            this.btnDeleteStateChainState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDeleteStateChainState.Enabled = false;
            this.btnDeleteStateChainState.Location = new System.Drawing.Point(100, 0);
            this.btnDeleteStateChainState.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnDeleteStateChainState.Name = "btnDeleteStateChainState";
            this.btnDeleteStateChainState.Size = new System.Drawing.Size(94, 23);
            this.btnDeleteStateChainState.TabIndex = 2;
            this.btnDeleteStateChainState.Text = "&Delete";
            this.btnDeleteStateChainState.UseVisualStyleBackColor = true;
            this.btnDeleteStateChainState.Click += new System.EventHandler(this.BtnDeleteStateChainStateClick);
            // 
            // gbSelectedState
            // 
            this.gbSelectedState.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.gbSelectedState.Controls.Add(this.cbState);
            this.gbSelectedState.Controls.Add(this.tbTime);
            this.gbSelectedState.Controls.Add(this.label3);
            this.gbSelectedState.Controls.Add(this.label2);
            this.gbSelectedState.Location = new System.Drawing.Point(6, 45);
            this.gbSelectedState.Name = "gbSelectedState";
            this.gbSelectedState.Size = new System.Drawing.Size(189, 85);
            this.gbSelectedState.TabIndex = 2;
            this.gbSelectedState.TabStop = false;
            this.gbSelectedState.Text = "Selected State";
            // 
            // cbState
            // 
            this.cbState.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.cbState.Enabled = false;
            this.cbState.FormattingEnabled = true;
            this.cbState.Location = new System.Drawing.Point(47, 21);
            this.cbState.Name = "cbState";
            this.cbState.Size = new System.Drawing.Size(139, 21);
            this.cbState.TabIndex = 4;
            this.cbState.Validating += new System.ComponentModel.CancelEventHandler(this.CbStateValidating);
            this.cbState.Validated += new System.EventHandler(this.CbStateValidated);
            // 
            // tbTime
            // 
            this.tbTime.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbTime.Enabled = false;
            this.tbTime.Location = new System.Drawing.Point(47, 55);
            this.tbTime.Name = "tbTime";
            this.tbTime.Size = new System.Drawing.Size(139, 20);
            this.tbTime.TabIndex = 3;
            this.tbTime.Validating += new System.ComponentModel.CancelEventHandler(this.TbTimeValidating);
            this.tbTime.Validated += new System.EventHandler(this.TbTimeValidated);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(8, 58);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 1;
            this.label3.Text = "Time:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(8, 24);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 0;
            this.label2.Text = "State:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(11, 22);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(38, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Name:";
            // 
            // tbName
            // 
            this.tbName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tbName.Enabled = false;
            this.tbName.Location = new System.Drawing.Point(53, 19);
            this.tbName.Name = "tbName";
            this.tbName.Size = new System.Drawing.Size(128, 20);
            this.tbName.TabIndex = 0;
            this.tbName.Validating += new System.ComponentModel.CancelEventHandler(this.TbNameValidating);
            this.tbName.Validated += new System.EventHandler(this.TbNameValidated);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.btnAddStateChain, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.btnDeleteStateChain, 1, 0);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(3, 143);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 1;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(206, 23);
            this.tableLayoutPanel1.TabIndex = 4;
            // 
            // epMain
            // 
            this.epMain.ContainerControl = this;
            // 
            // StateChainsPluginControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.gbStateChain);
            this.Controls.Add(this.lbStateChains);
            this.MinimumSize = new System.Drawing.Size(212, 400);
            this.Name = "StateChainsPluginControl";
            this.Size = new System.Drawing.Size(212, 400);
            this.gbStateChain.ResumeLayout(false);
            this.gbStateChain.PerformLayout();
            this.tableLayoutPanel4.ResumeLayout(false);
            this.tableLayoutPanel3.ResumeLayout(false);
            this.tableLayoutPanel2.ResumeLayout(false);
            this.gbSelectedState.ResumeLayout(false);
            this.gbSelectedState.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.epMain)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox lbStateChains;
        private System.Windows.Forms.Button btnAddStateChain;
        private System.Windows.Forms.Button btnDeleteStateChain;
        private System.Windows.Forms.GroupBox gbStateChain;
        private System.Windows.Forms.GroupBox gbSelectedState;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbName;
        private System.Windows.Forms.ComboBox cbState;
        private System.Windows.Forms.TextBox tbTime;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox lbStates;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.Button btnAddStateChainState;
        private System.Windows.Forms.Button btnDeleteStateChainState;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ErrorProvider epMain;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel4;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
        private System.Windows.Forms.Button btnMoveDownStateChainState;
        private System.Windows.Forms.Button btnMoveUpStateChainState;
    }
}
