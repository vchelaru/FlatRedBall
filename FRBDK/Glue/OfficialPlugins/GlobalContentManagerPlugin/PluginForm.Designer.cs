namespace PluginTestbed.GlobalContentManagerPlugins
{
    partial class PluginForm
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
            this.ElementDataGrid = new System.Windows.Forms.DataGridView();
            this.CloseButton = new System.Windows.Forms.Button();
            this.Element = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.UsesGlobalContent = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.IncludeFilesInGlobal = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.ElementDataGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // ElementDataGrid
            // 
            this.ElementDataGrid.AllowUserToAddRows = false;
            this.ElementDataGrid.AllowUserToDeleteRows = false;
            this.ElementDataGrid.AllowUserToResizeRows = false;
            this.ElementDataGrid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ElementDataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.ElementDataGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Element,
            this.UsesGlobalContent,
            this.IncludeFilesInGlobal});
            this.ElementDataGrid.Location = new System.Drawing.Point(0, 1);
            this.ElementDataGrid.Name = "ElementDataGrid";
            this.ElementDataGrid.Size = new System.Drawing.Size(625, 473);
            this.ElementDataGrid.TabIndex = 1;
            // 
            // CloseButton
            // 
            this.CloseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.CloseButton.CausesValidation = false;
            this.CloseButton.Location = new System.Drawing.Point(443, 480);
            this.CloseButton.Name = "CloseButton";
            this.CloseButton.Size = new System.Drawing.Size(182, 23);
            this.CloseButton.TabIndex = 2;
            this.CloseButton.Text = "Close";
            this.CloseButton.UseVisualStyleBackColor = true;
            this.CloseButton.Click += new System.EventHandler(this.CloseButton_Click);
            // 
            // Element
            // 
            this.Element.HeaderText = "Element";
            this.Element.Name = "Element";
            this.Element.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.Element.Width = 300;
            // 
            // UsesGlobalContent
            // 
            this.UsesGlobalContent.HeaderText = "Uses Global Content";
            this.UsesGlobalContent.Name = "UsesGlobalContent";
            this.UsesGlobalContent.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // IncludeFilesInGlobal
            // 
            this.IncludeFilesInGlobal.HeaderText = "Files in GlobalContent";
            this.IncludeFilesInGlobal.Name = "IncludeFilesInGlobal";
            this.IncludeFilesInGlobal.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            // 
            // PluginForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(625, 503);
            this.Controls.Add(this.CloseButton);
            this.Controls.Add(this.ElementDataGrid);
            this.Name = "PluginForm";
            this.Text = "PluginForm";
            ((System.ComponentModel.ISupportInitialize)(this.ElementDataGrid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DataGridView ElementDataGrid;
        private System.Windows.Forms.Button CloseButton;
        private System.Windows.Forms.DataGridViewTextBoxColumn Element;
        private System.Windows.Forms.DataGridViewCheckBoxColumn UsesGlobalContent;
        private System.Windows.Forms.DataGridViewCheckBoxColumn IncludeFilesInGlobal;
    }
}