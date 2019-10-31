namespace TestForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.tmxFileDialog = new System.Windows.Forms.OpenFileDialog();
            this.tmxFilename = new System.Windows.Forms.TextBox();
            this.tmxButton = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tmxDestinationFolder = new System.Windows.Forms.TextBox();
            this.tmxDestinationButton = new System.Windows.Forms.Button();
            this.tmxConvertToScnx = new System.Windows.Forms.Button();
            this.tmxConvertToShcx = new System.Windows.Forms.Button();
            this.tmxConvertToNntx = new System.Windows.Forms.Button();
            this.tmxFolderDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.tmxCSVButton = new System.Windows.Forms.Button();
            this.tmxLayerCSVButton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.tmxLayerName = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.offsetX = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.offsetY = new System.Windows.Forms.TextBox();
            this.offsetZ = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(66, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(52, 13);
            this.label1.TabIndex = 0;
            this.label1.Text = "TMX File:";
            // 
            // tmxFileDialog
            // 
            this.tmxFileDialog.DefaultExt = "tmx";
            this.tmxFileDialog.Filter = "TMX File|*.tmx";
            this.tmxFileDialog.Title = "Choose a TMX File to Open";
            this.tmxFileDialog.FileOk += new System.ComponentModel.CancelEventHandler(this.TmxFiledialogOk);
            // 
            // tmxFilename
            // 
            this.tmxFilename.Location = new System.Drawing.Point(124, 12);
            this.tmxFilename.Name = "tmxFilename";
            this.tmxFilename.Size = new System.Drawing.Size(354, 20);
            this.tmxFilename.TabIndex = 1;
            // 
            // tmxButton
            // 
            this.tmxButton.Location = new System.Drawing.Point(484, 9);
            this.tmxButton.Name = "tmxButton";
            this.tmxButton.Size = new System.Drawing.Size(24, 23);
            this.tmxButton.TabIndex = 2;
            this.tmxButton.Text = "...";
            this.tmxButton.UseVisualStyleBackColor = true;
            this.tmxButton.Click += new System.EventHandler(this.tmxButton_Click);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(23, 49);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(95, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Destination Folder:";
            // 
            // tmxDestinationFolder
            // 
            this.tmxDestinationFolder.Location = new System.Drawing.Point(124, 45);
            this.tmxDestinationFolder.Name = "tmxDestinationFolder";
            this.tmxDestinationFolder.Size = new System.Drawing.Size(354, 20);
            this.tmxDestinationFolder.TabIndex = 4;
            // 
            // tmxDestinationButton
            // 
            this.tmxDestinationButton.Location = new System.Drawing.Point(484, 43);
            this.tmxDestinationButton.Name = "tmxDestinationButton";
            this.tmxDestinationButton.Size = new System.Drawing.Size(24, 23);
            this.tmxDestinationButton.TabIndex = 5;
            this.tmxDestinationButton.Text = "...";
            this.tmxDestinationButton.UseVisualStyleBackColor = true;
            this.tmxDestinationButton.Click += new System.EventHandler(this.tmxDestinationButton_Click);
            // 
            // tmxConvertToScnx
            // 
            this.tmxConvertToScnx.Location = new System.Drawing.Point(165, 160);
            this.tmxConvertToScnx.Name = "tmxConvertToScnx";
            this.tmxConvertToScnx.Size = new System.Drawing.Size(75, 23);
            this.tmxConvertToScnx.TabIndex = 6;
            this.tmxConvertToScnx.Text = "SCNX";
            this.tmxConvertToScnx.UseVisualStyleBackColor = true;
            this.tmxConvertToScnx.Click += new System.EventHandler(this.tmxConvertToScnx_Click);
            // 
            // tmxConvertToShcx
            // 
            this.tmxConvertToShcx.Location = new System.Drawing.Point(327, 160);
            this.tmxConvertToShcx.Name = "tmxConvertToShcx";
            this.tmxConvertToShcx.Size = new System.Drawing.Size(75, 23);
            this.tmxConvertToShcx.TabIndex = 7;
            this.tmxConvertToShcx.Text = "SHCX";
            this.tmxConvertToShcx.UseVisualStyleBackColor = true;
            this.tmxConvertToShcx.Click += new System.EventHandler(this.tmxConvertToShcx_Click);
            // 
            // tmxConvertToNntx
            // 
            this.tmxConvertToNntx.Location = new System.Drawing.Point(246, 160);
            this.tmxConvertToNntx.Name = "tmxConvertToNntx";
            this.tmxConvertToNntx.Size = new System.Drawing.Size(75, 23);
            this.tmxConvertToNntx.TabIndex = 8;
            this.tmxConvertToNntx.Text = "NNTX";
            this.tmxConvertToNntx.UseVisualStyleBackColor = true;
            this.tmxConvertToNntx.Click += new System.EventHandler(this.tmxConvertToNntx_Click);
            // 
            // tmxFolderDialog
            // 
            this.tmxFolderDialog.Description = "Choose the destination folder";
            // 
            // tmxCSVButton
            // 
            this.tmxCSVButton.Location = new System.Drawing.Point(327, 189);
            this.tmxCSVButton.Name = "tmxCSVButton";
            this.tmxCSVButton.Size = new System.Drawing.Size(75, 23);
            this.tmxCSVButton.TabIndex = 9;
            this.tmxCSVButton.Text = "Tile CSV";
            this.tmxCSVButton.UseVisualStyleBackColor = true;
            this.tmxCSVButton.Click += new System.EventHandler(this.tmxCSVButton_Click);
            // 
            // tmxLayerCSVButton
            // 
            this.tmxLayerCSVButton.Location = new System.Drawing.Point(246, 189);
            this.tmxLayerCSVButton.Name = "tmxLayerCSVButton";
            this.tmxLayerCSVButton.Size = new System.Drawing.Size(75, 23);
            this.tmxLayerCSVButton.TabIndex = 10;
            this.tmxLayerCSVButton.Text = "Layer CSV";
            this.tmxLayerCSVButton.UseVisualStyleBackColor = true;
            this.tmxLayerCSVButton.Click += new System.EventHandler(this.tmxLayerCSVButton_Click);
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(165, 189);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 11;
            this.button1.Text = "Object CSV";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tmxLayerName
            // 
            this.tmxLayerName.Location = new System.Drawing.Point(124, 75);
            this.tmxLayerName.Name = "tmxLayerName";
            this.tmxLayerName.Size = new System.Drawing.Size(354, 20);
            this.tmxLayerName.TabIndex = 13;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(51, 78);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(67, 13);
            this.label3.TabIndex = 12;
            this.label3.Text = "Layer Name:";
            // 
            // offsetX
            // 
            this.offsetX.Location = new System.Drawing.Point(165, 103);
            this.offsetX.Name = "offsetX";
            this.offsetX.Size = new System.Drawing.Size(34, 20);
            this.offsetX.TabIndex = 14;
            this.offsetX.Text = "0.0";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(80, 106);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(38, 13);
            this.label4.TabIndex = 15;
            this.label4.Text = "Offset:";
            // 
            // offsetY
            // 
            this.offsetY.Location = new System.Drawing.Point(248, 103);
            this.offsetY.Name = "offsetY";
            this.offsetY.Size = new System.Drawing.Size(34, 20);
            this.offsetY.TabIndex = 16;
            this.offsetY.Text = "0.0";
            // 
            // offsetZ
            // 
            this.offsetZ.Location = new System.Drawing.Point(327, 103);
            this.offsetZ.Name = "offsetZ";
            this.offsetZ.Size = new System.Drawing.Size(34, 20);
            this.offsetZ.TabIndex = 17;
            this.offsetZ.Text = "0.0";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(147, 106);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(12, 13);
            this.label5.TabIndex = 18;
            this.label5.Text = "x";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(230, 106);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(12, 13);
            this.label6.TabIndex = 19;
            this.label6.Text = "y";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(309, 106);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(12, 13);
            this.label7.TabIndex = 20;
            this.label7.Text = "z";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(530, 226);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.offsetZ);
            this.Controls.Add(this.offsetY);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.offsetX);
            this.Controls.Add(this.tmxLayerName);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.tmxLayerCSVButton);
            this.Controls.Add(this.tmxCSVButton);
            this.Controls.Add(this.tmxConvertToNntx);
            this.Controls.Add(this.tmxConvertToShcx);
            this.Controls.Add(this.tmxConvertToScnx);
            this.Controls.Add(this.tmxDestinationButton);
            this.Controls.Add(this.tmxDestinationFolder);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.tmxButton);
            this.Controls.Add(this.tmxFilename);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.OpenFileDialog tmxFileDialog;
        private System.Windows.Forms.TextBox tmxFilename;
        private System.Windows.Forms.Button tmxButton;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tmxDestinationFolder;
        private System.Windows.Forms.Button tmxDestinationButton;
        private System.Windows.Forms.Button tmxConvertToScnx;
        private System.Windows.Forms.Button tmxConvertToShcx;
        private System.Windows.Forms.Button tmxConvertToNntx;
        private System.Windows.Forms.FolderBrowserDialog tmxFolderDialog;
        private System.Windows.Forms.Button tmxCSVButton;
        private System.Windows.Forms.Button tmxLayerCSVButton;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox tmxLayerName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox offsetX;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox offsetY;
        private System.Windows.Forms.TextBox offsetZ;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label7;
    }
}

