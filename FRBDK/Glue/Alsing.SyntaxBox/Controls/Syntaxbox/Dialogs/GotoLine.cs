// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;

namespace Alsing.Windows.Forms.SyntaxBox
{
    /// <summary>
    /// Summary description for GotoLine.
    /// </summary>
    public class GotoLineForm : Form
    {
        private readonly EditViewControl mOwner;
        private Button btnCancel;
        private Button btnOK;

        /// <summary>
        /// Required designer variable.
        /// </summary>
        private Container components;

        private Label lblLines;
        private TextBox txtRow;

        /// <summary>
        /// Default constructor for the GotoLineForm.
        /// </summary>
        public GotoLineForm()
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
        }

        /// <summary>
        /// Creates a GotoLineForm that will be assigned to a specific Owner control.
        /// </summary>
        /// <param name="Owner">The SyntaxBox that will use the GotoLineForm</param>
        /// <param name="RowCount">The number of lines in the owner control</param>
        public GotoLineForm(EditViewControl Owner, int RowCount)
        {
            //
            // Required for Windows Form Designer support
            //
            InitializeComponent();

            //
            // TODO: Add any constructor code after InitializeComponent call
            //
            lblLines.Text = "Line number (1-" + RowCount.ToString(CultureInfo.InvariantCulture) + "):";
            mOwner = Owner;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                int row = int.Parse(txtRow.Text) - 1;
                mOwner.GotoLine(row);
            }
            catch {}
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void GotoLine_Closing(object sender,
                                      CancelEventArgs e)
        {
            //e.Cancel =true;
            //this.Hide ();
        }

        private void GotoLine_Activated(object sender, EventArgs e)
        {
            txtRow.Focus();
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.txtRow = new System.Windows.Forms.TextBox();
            this.lblLines = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(160, 48);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(72, 24);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(80, 48);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(72, 24);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // txtRow
            // 
            this.txtRow.Location = new System.Drawing.Point(8, 24);
            this.txtRow.Name = "txtRow";
            this.txtRow.Size = new System.Drawing.Size(224, 20);
            this.txtRow.TabIndex = 2;
            this.txtRow.Text = "";
            // 
            // lblLines
            // 
            this.lblLines.Location = new System.Drawing.Point(8, 8);
            this.lblLines.Name = "lblLines";
            this.lblLines.Size = new System.Drawing.Size(128, 16);
            this.lblLines.TabIndex = 3;
            this.lblLines.Text = "-";
            // 
            // GotoLineForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(242, 82);
            this.Controls.AddRange(new System.Windows.Forms.Control[]
                                   {
                                       this.lblLines, this.txtRow, this.btnOK, this.btnCancel
                                   }
                );
            this.FormBorderStyle =
                System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "GotoLineForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Go To Line";
            this.Closing += new System.ComponentModel.CancelEventHandler
                (this.GotoLine_Closing);
            this.Activated += new System.EventHandler(this.GotoLine_Activated);
            this.ResumeLayout(false);
        }

        #endregion
    }
}