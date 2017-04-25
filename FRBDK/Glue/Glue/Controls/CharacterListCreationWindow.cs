using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.Controls
{
    public partial class CharacterListCreationWindow : Form
    {
        string DirectoryContentIsRelativeTo
        {
            get
            {
                string directoryToMakeRelativeTo = FileManager.RelativeDirectory;
                if (ProjectManager.ContentProject != null)
                {
                    return ProjectManager.ContentProject.GetAbsoluteContentFolder();
                }

                return directoryToMakeRelativeTo;
            }
        }


        public CharacterListCreationWindow()
        {
            InitializeComponent();

            FillWithAvailableNamedObjects();
        }

        private void FillWithAvailableNamedObjects()
        {

            List<ReferencedFileSave> referencedFiles = ObjectFinder.Self.GetAllReferencedFiles();

            List<string> validExtensions = new List<string>();
            validExtensions.Add("txt");
            validExtensions.Add("csv");

            string directoryToMakeRelativeTo = DirectoryContentIsRelativeTo;

            foreach (ReferencedFileSave rfs in referencedFiles)
            {
                string extension = FileManager.GetExtension(rfs.Name);

                if (validExtensions.Contains(extension))
                {
                    AllFilesList.Nodes.Add(rfs.Name);
                }

            }
        }

        private void AllFilesList_DoubleClick(object sender, EventArgs e)
        {
            TreeNode selectedNode = AllFilesList.SelectedNode;

            if (selectedNode != null && !SelectedFileList.Nodes.ContainsText(selectedNode.Text))
            {
                SelectedFileList.Nodes.Add(selectedNode.Text);
            }
        }

        private void SelectedFileList_DoubleClick(object sender, EventArgs e)
        {
            TreeNode selectedNode = SelectedFileList.SelectedNode;

            SelectedFileList.Nodes.Remove(selectedNode);
        }

        private void GetCharacterListButton_Click(object sender, EventArgs e)
        {

            string directoryToMakeRelativeTo = DirectoryContentIsRelativeTo;

            int supportedCharacterSetSize = 120000;// increase this if we ever have characters outside of the set
            bool[] usedCharacters = new bool[supportedCharacterSetSize];

            foreach (TreeNode treeNode in SelectedFileList.Nodes)
            {
                string fileName = ProjectManager.MakeAbsolute(treeNode.Text, true);

                string contents = FileManager.FromFileText(fileName);

                AddCharacterContentsToList(contents, usedCharacters);
            }


            string resultingText = GetResultingTextFromUsedCharacters(usedCharacters); ;

            if (!string.IsNullOrEmpty(resultingText))
            {
                MessageBox.Show("Added the filter string to your clipboard. CTRL+V it anywhere to see it.");
                Clipboard.SetText(resultingText);
            }
        }

        void AddCharacterContentsToList(string contents, bool[] usedCharacters)
        {
            for (int i = 0; i < contents.Length; i++)
            {
                usedCharacters[contents[i]] = true;

            }
        }


        public string GetResultingTextFromUsedCharacters(bool[] usedCharacters)
        {
            StringBuilder resultingString = new StringBuilder();

            int openingIndex = -1;
            string emptyOrComma = "";

            for (int i = 0; i < usedCharacters.Length; i++)
            {
                if (usedCharacters[i])
                {
                    if (openingIndex == -1)
                    {
                        openingIndex = i;
                    }
                    // else do nothing
                }
                else
                {
                    if (openingIndex != -1)
                    {
                        if (i == openingIndex + 1)
                        {
                            resultingString.Append(emptyOrComma + openingIndex.ToString());
                            emptyOrComma = ",";
                        }
                        else
                        {
                            resultingString.Append(emptyOrComma + openingIndex + "-" + (i - 1));
                            emptyOrComma = ",";
                        }
                        openingIndex = -1;
                    }
                }
            }


            return resultingString.ToString();

        }
    }
}
