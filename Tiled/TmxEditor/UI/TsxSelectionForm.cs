using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TmxEditor.UI
{
    public partial class TsxSelectionForm : Form
    {

        #region Properties

        public string FileName 
        {
            get
            {
                if (FromThisProjectRadioButton.Checked)
                {
                    return FromThisProjectTreeView.SelectedNode.Text;
                }
                else
                {
                    return FromFileTextBox.Text;
                }
            }
        }

        #endregion


        #region Methods

        public TsxSelectionForm()
        {
            InitializeComponent();

            RefreshFromProjectListView();
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            RefreshPanelVisibility();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            RefreshPanelVisibility();
        }

        private void RefreshPanelVisibility()
        {
            this.FromThisProjectPanel.Visible = this.FromThisProjectRadioButton.Checked;

            bool fromFileVisible = FromFileRadioButton.Checked;
            this.FromFilePanel.Visible = fromFileVisible;

        }

        private void RefreshFromProjectListView()
        {
            this.FromThisProjectTreeView.Nodes.Clear();

            foreach (var option in AppState.Self.ProvidedContext.AvailableTsxFiles)
            {
                // If these aren't absolute, that's not good
                if (FileManager.IsRelative(option))
                {
                    throw new Exception("The ProvidedContext contains TSX files which aren't absolute.");
                }

                this.FromThisProjectTreeView.Nodes.Add(option);
            }
        }

        #endregion

        private void OkayButton_Click(object sender, EventArgs e)
        {
            string whyIsntValid = null;
            if (FromThisProjectRadioButton.Checked)
            {
                if(FromThisProjectTreeView.SelectedNode == null)
                {
                    whyIsntValid = "You must select a tileset";  
                }
            }

            if(!string.IsNullOrEmpty(whyIsntValid))
            {
                MessageBox.Show(whyIsntValid);    
            }
            else
            {
                DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }

        }

        private void CancelButton_Click(object sender, EventArgs e)
        {
            DialogResult = System.Windows.Forms.DialogResult.Cancel;

            this.Close();
        }

        private void FromFileButton_Click(object sender, EventArgs e)
        {
            string fileName;
            bool succeeded = TsxSelectionForm.TryGetTsxFileNameFromDisk(out fileName, null);

            if (succeeded)
            {
                this.FromFileTextBox.Text = fileName;

            }

        }


        public static bool TryGetTsxFileNameFromDisk(out string fileName, string alternativeFilter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if(!string.IsNullOrEmpty(alternativeFilter))
            {
                openFileDialog.Filter = alternativeFilter;
            }
            else
            {
                openFileDialog.Filter = "Tileset Files|*.tsx";
            }

            fileName = null;
            bool succeeded = false;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileName = openFileDialog.FileName;
                succeeded = true;
            }

            return succeeded;
        }

        private void FromThisProjectTreeView_DoubleClick(object sender, EventArgs e)
        {
            if (FromThisProjectTreeView.SelectedNode != null)
            {
                DialogResult = System.Windows.Forms.DialogResult.OK;
                this.Close();
            }
        }

    }
}
