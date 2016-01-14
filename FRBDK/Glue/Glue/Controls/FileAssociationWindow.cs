using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.IO;

namespace FlatRedBall.Glue.Controls
{
	public partial class FileAssociationWindow : Form
	{
		public FileAssociationWindow()
		{
			InitializeComponent();

            propertyGrid1_SelectedGridItemChanged(null, null);
		}

		private void FileAssociationWindow_Shown(object sender, EventArgs e)
		{
			propertyGrid1.SelectedObject = EditorData.FileAssociationSettings;
		}

        private void OkButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        private void propertyGrid1_SelectedGridItemChanged(object sender, SelectedGridItemChangedEventArgs e)
        {
            bool enable = propertyGrid1.SelectedGridItem != null && propertyGrid1.SelectedGridItem.GridItemType == GridItemType.Property;
            
            this.MakeAbsoluteButton.Enabled = enable; 
            this.MakeRelativeToProjectButton.Enabled = enable;
        }

        private void MakeAbsoluteButton_Click(object sender, EventArgs e)
        {
            string extensionToMakeAbsolute = propertyGrid1.SelectedGridItem.Label;

            string oldApplicationName = EditorData.FileAssociationSettings.GetApplicationForExtension(extensionToMakeAbsolute);

            if (oldApplicationName != "<DEFAULT>" && FileManager.IsRelative(oldApplicationName))
            {
                string newApplicationName = FileManager.MakeAbsolute(oldApplicationName);

                EditorData.FileAssociationSettings.SetApplicationForExtension(extensionToMakeAbsolute, newApplicationName);
                EditorData.FileAssociationSettings.ReplaceApplicationInList(oldApplicationName, newApplicationName);

                propertyGrid1.Refresh();
            }
        }

        private void MakeRelativeToProjectButton_Click(object sender, EventArgs e)
        {
            string extensionToMakeAbsolute = propertyGrid1.SelectedGridItem.Label;

            string oldApplicationName = EditorData.FileAssociationSettings.GetApplicationForExtension(extensionToMakeAbsolute);

            if (oldApplicationName != "<DEFAULT>" && !FileManager.IsRelative(oldApplicationName))
            {
                string newApplicationName = FileManager.MakeRelative(oldApplicationName);


                EditorData.FileAssociationSettings.SetApplicationForExtension(extensionToMakeAbsolute, newApplicationName);
                EditorData.FileAssociationSettings.ReplaceApplicationInList(oldApplicationName, newApplicationName);
                propertyGrid1.Refresh();
            }
        }


	}
}
