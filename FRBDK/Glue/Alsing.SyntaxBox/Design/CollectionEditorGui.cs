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
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Alsing.Design
{
    /// <summary>
    /// Summary description for CollectionEditorGui.
    /// </summary>
    [ToolboxItem(false)]
    public class CollectionEditorGui : UserControl
    {
        public Button btnAdd;
        public Button btnCancel;
        public Button btnDown;
        public Button btnDropdown;
        public Button btnOK;
        public Button btnRemove;
        public Button btnUp;

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private Container components;

        private GroupBox groupBox1;

        public Label lblMembers;
        public Label lblProperties;
        public ListBox lstMembers;
        public Panel pnlMain;
        public Panel pnlMembers;
        public PropertyGrid pygProperties;

        public CollectionEditorGui()
        {
            // This call is required by the Windows.Forms Form Designer.
            InitializeComponent();

            // TODO: Add any initialization after the InitForm call
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

        public void Bind()
        {
            var e = EditValue as ICollection;
            if (e == null)
            {
                MessageBox.Show("EditValue is null");
            }
            else
            {
                
                lstMembers.Items.Clear();
                foreach (object o in e)
                {
                    lstMembers.Items.Add(o);
                }

                EnableRemove();
                SelectObject();
            }
        }

        private void lstMembers_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {
                if (e.Index == -1)
                    return;

                int c = lstMembers.Items.Count;
                SizeF s = e.Graphics.MeasureString(c.ToString(), lstMembers.Font);
                var maxwidth = (int) s.Width;
                if (maxwidth < 16 + 2)
                    maxwidth = 16 + 2;

                var r = new Rectangle(0, e.Bounds.Top, maxwidth, lstMembers.ItemHeight);
                bool SupportPaint = Editor.GetPaintValueSupported();
                int w = SupportPaint ? 20 : 0;

                var rcItem = new Rectangle(r.Right + w, r.Top, e.Bounds.Width - r.Right - w, lstMembers.ItemHeight);

                ControlPaint.DrawBorder3D(e.Graphics, r, Border3DStyle.Raised, Border3DSide.All);
                StringFormat sf = StringFormat.GenericDefault;
                sf.Alignment = StringAlignment.Far;
                r.Inflate(-1, -1);
                e.Graphics.DrawString(e.Index.ToString(), lstMembers.Font, Brushes.Black, r, sf);


                bool Selected = ((int) e.State & (int) DrawItemState.Selected) != 0;

                using (SolidBrush bg = GetBgBrush(Selected))
                using (SolidBrush fg = GetFgBrush(Selected))
                {
                    e.Graphics.FillRectangle(bg, rcItem);
                    if (Selected && e.Index != -1)
                    {
                        if (((int) e.State & (int) DrawItemState.Focus) != 0)
                        {
                            ControlPaint.DrawFocusRectangle(e.Graphics, rcItem);
                        }
                    }

                    if (e.Index >= 0)
                    {
                        object o = lstMembers.Items[e.Index];
                        string name = GetDisplayText(o);
                        e.Graphics.DrawString(name, lstMembers.Font, fg, rcItem);
                    }
                }
            }
            catch { }
        }

        private SolidBrush GetFgBrush(bool Selected)
        {
            SolidBrush fg = Selected ? new SolidBrush(SystemColors.HighlightText) : new SolidBrush(lstMembers.ForeColor);
            return fg;
        }

        private SolidBrush GetBgBrush(bool Selected)
        {
            SolidBrush bg = Selected ? new SolidBrush(SystemColors.Highlight) : new SolidBrush(lstMembers.BackColor);
            return bg;
        }

        private void lstMembers_SelectedIndexChanged(object sender, EventArgs e)
        {
            EnableRemove();
            SelectObject();
        }

        private void SelectObject()
        {
            pygProperties.SelectedObject = lstMembers.SelectedIndex >= 0 ? lstMembers.SelectedItem : null;
        }

        private void EnableRemove()
        {
            btnRemove.Enabled = lstMembers.SelectedIndices.Count > 0;
        }


        private static string GetDisplayText(object Item)
        {
            string ObjectName = null;

            if (Item == null)
            {
                return string.Empty;
            }
            PropertyDescriptor descriptor1 = TypeDescriptor.GetProperties(Item)["Name"];
            if (descriptor1 != null)
            {
                ObjectName = ((string) descriptor1.GetValue(Item));
                if (!string.IsNullOrEmpty(ObjectName))
                {
                    return ObjectName;
                }
            }

            if (string.IsNullOrEmpty(ObjectName))
            {
                ObjectName = Item.GetType().Name;
            }
            return ObjectName;
        }

        public void AddObject(object o)
        {
            Editor.AddObject(o);
        }

        public void RemoveObject(object o)
        {
            Editor.RemoveObject(o);
        }

        private void btnDown_Click(object sender, EventArgs e) {}

        private void btnUp_Click(object sender, EventArgs e) {}

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            var resources = new System.Resources.ResourceManager(typeof (CollectionEditorGui));
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.pygProperties = new System.Windows.Forms.PropertyGrid();
            this.lstMembers = new System.Windows.Forms.ListBox();
            this.btnUp = new System.Windows.Forms.Button();
            this.btnDown = new System.Windows.Forms.Button();
            this.pnlMembers = new System.Windows.Forms.Panel();
            this.btnRemove = new System.Windows.Forms.Button();
            this.lblMembers = new System.Windows.Forms.Label();
            this.btnDropdown = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.pnlMain = new System.Windows.Forms.Panel();
            this.lblProperties = new System.Windows.Forms.Label();
            this.pnlMembers.SuspendLayout();
            this.pnlMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
            this.btnCancel.Location = new System.Drawing.Point(456, 312);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancel";
            // 
            // btnOK
            // 
            this.btnOK.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(376, 312);
            this.btnOK.Name = "btnOK";
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                                     | System.Windows.Forms.AnchorStyles.Right);
            this.groupBox1.Location = new System.Drawing.Point(8, 296);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(520, 8);
            this.groupBox1.TabIndex = 2;
            this.groupBox1.TabStop = false;
            // 
            // pygProperties
            // 
            this.pygProperties.CommandsVisibleIfAvailable = true;
            this.pygProperties.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pygProperties.HelpVisible = false;
            this.pygProperties.LargeButtons = false;
            this.pygProperties.LineColor = System.Drawing.SystemColors.ScrollBar;
            this.pygProperties.Location = new System.Drawing.Point(240, 16);
            this.pygProperties.Name = "pygProperties";
            this.pygProperties.Size = new System.Drawing.Size(280, 280);
            this.pygProperties.TabIndex = 3;
            this.pygProperties.Text = "propertyGrid1";
            this.pygProperties.ToolbarVisible = false;
            this.pygProperties.ViewBackColor = System.Drawing.SystemColors.Window;
            this.pygProperties.ViewForeColor = System.Drawing.SystemColors.WindowText;
            // 
            // lstMembers
            // 
            this.lstMembers.Anchor = (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                       | System.Windows.Forms.AnchorStyles.Left)
                                      | System.Windows.Forms.AnchorStyles.Right);
            this.lstMembers.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lstMembers.IntegralHeight = false;
            this.lstMembers.ItemHeight = 16;
            this.lstMembers.Location = new System.Drawing.Point(0, 16);
            this.lstMembers.Name = "lstMembers";
            this.lstMembers.Size = new System.Drawing.Size(208, 240);
            this.lstMembers.TabIndex = 4;
            this.lstMembers.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lstMembers_DrawItem);
            this.lstMembers.SelectedIndexChanged += new System.EventHandler(this.lstMembers_SelectedIndexChanged);
            // 
            // btnUp
            // 
            this.btnUp.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
            this.btnUp.Image = ((System.Drawing.Bitmap) (resources.GetObject("btnUp.Image")));
            this.btnUp.Location = new System.Drawing.Point(212, 16);
            this.btnUp.Name = "btnUp";
            this.btnUp.Size = new System.Drawing.Size(22, 28);
            this.btnUp.TabIndex = 5;
            this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            // 
            // btnDown
            // 
            this.btnDown.Anchor = (System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right);
            this.btnDown.Image = ((System.Drawing.Bitmap) (resources.GetObject("btnDown.Image")));
            this.btnDown.Location = new System.Drawing.Point(212, 48);
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new System.Drawing.Size(22, 28);
            this.btnDown.TabIndex = 6;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            // 
            // pnlMembers
            // 
            this.pnlMembers.Controls.AddRange(new System.Windows.Forms.Control[]
                                              {
                                                  this.btnRemove,
                                                  this.lstMembers,
                                                  this.lblMembers,
                                                  this.btnDown,
                                                  this.btnUp,
                                                  this.btnDropdown,
                                                  this.btnAdd
                                              });
            this.pnlMembers.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlMembers.Name = "pnlMembers";
            this.pnlMembers.Size = new System.Drawing.Size(240, 296);
            this.pnlMembers.TabIndex = 7;
            // 
            // btnRemove
            // 
            this.btnRemove.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
            this.btnRemove.Location = new System.Drawing.Point(136, 264);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(72, 24);
            this.btnRemove.TabIndex = 11;
            this.btnRemove.Text = "Remove";
            // 
            // lblMembers
            // 
            this.lblMembers.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblMembers.Name = "lblMembers";
            this.lblMembers.Size = new System.Drawing.Size(240, 16);
            this.lblMembers.TabIndex = 10;
            this.lblMembers.Text = "Members:";
            // 
            // btnDropdown
            // 
            this.btnDropdown.Anchor = (System.Windows.Forms.AnchorStyles.Bottom |
                                       System.Windows.Forms.AnchorStyles.Right);
            this.btnDropdown.Image = ((System.Drawing.Bitmap) (resources.GetObject("btnDropdown.Image")));
            this.btnDropdown.Location = new System.Drawing.Point(95, 264);
            this.btnDropdown.Name = "btnDropdown";
            this.btnDropdown.Size = new System.Drawing.Size(24, 24);
            this.btnDropdown.TabIndex = 9;
            this.btnDropdown.Visible = false;
            // 
            // btnAdd
            // 
            this.btnAdd.Anchor = (System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
            this.btnAdd.Location = new System.Drawing.Point(8, 264);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(88, 24);
            this.btnAdd.TabIndex = 12;
            this.btnAdd.Text = "Add";
            // 
            // pnlMain
            // 
            this.pnlMain.Anchor = (((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                                    | System.Windows.Forms.AnchorStyles.Left)
                                   | System.Windows.Forms.AnchorStyles.Right);
            this.pnlMain.Controls.AddRange(new System.Windows.Forms.Control[]
                                           {
                                               this.pygProperties,
                                               this.lblProperties,
                                               this.pnlMembers
                                           });
            this.pnlMain.Location = new System.Drawing.Point(8, 0);
            this.pnlMain.Name = "pnlMain";
            this.pnlMain.Size = new System.Drawing.Size(520, 296);
            this.pnlMain.TabIndex = 8;
            // 
            // lblProperties
            // 
            this.lblProperties.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblProperties.Location = new System.Drawing.Point(240, 0);
            this.lblProperties.Name = "lblProperties";
            this.lblProperties.Size = new System.Drawing.Size(280, 16);
            this.lblProperties.TabIndex = 9;
            this.lblProperties.Text = "Properties:";
            // 
            // CollectionEditorGui
            // 
            this.Controls.AddRange(new System.Windows.Forms.Control[]
                                   {
                                       this.groupBox1,
                                       this.btnOK,
                                       this.btnCancel,
                                       this.pnlMain
                                   });
            this.Name = "CollectionEditorGui";
            this.Size = new System.Drawing.Size(536, 352);
            this.pnlMembers.ResumeLayout(false);
            this.pnlMain.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        #region PUBLIC PROPERTY EDITOR

        public ComponaCollectionEditor Editor { get; set; }

        #endregion

        #region PUBLIC PROPERTY EDITORSERVICE

        public IWindowsFormsEditorService EditorService { get; set; }

        #endregion

        #region PUBLIC PROPERTY EDITVALUE

        public object EditValue { get; set; }

        #endregion
    }
}