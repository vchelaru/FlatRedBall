using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorObjects.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Managers
{
    public class BuildToolAssociationManager
    {
        #region Fields

        static BuildToolAssociationManager mSelf;

        #endregion

        #region Properties


        public BuildToolAssociationList ProjectSpecificBuildTools
        {
            get;
            set;
        }

        public static BuildToolAssociationManager Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new BuildToolAssociationManager();
                }
                return mSelf;
            }
        }

        public string ProjectSpecificBuildToolAssociationFileName
        {
            get { return ProjectManager.ProjectSpecificSettingsFolder + "BuildToolAssociation.xml"; }
        }

        #endregion

        internal BuildToolAssociation GetBuilderToolAssociationForSourceExtension(string sourceExtension)
        {
            BuildToolAssociation buildToolAssociation = null;

            foreach (BuildToolAssociation bta in ProjectSpecificBuildTools.BuildToolList)
            {
                if (bta.SourceFileType != null && bta.SourceFileType.ToLowerInvariant() == sourceExtension.ToLowerInvariant())
                {
                    buildToolAssociation = bta;
                    break;
                }
            }
            return buildToolAssociation;
        }

        public BuildToolAssociation GetBuilderToolAssociationForDestinationExtension(string destinationExtension)
        {
            BuildToolAssociation buildToolAssociation = null;

            foreach (BuildToolAssociation bta in ProjectSpecificBuildTools.BuildToolList)
            {
                if (bta.DestinationFileType.ToLowerInvariant() == destinationExtension.ToLowerInvariant())
                {
                    buildToolAssociation = bta;
                    break;
                }
            }
            return buildToolAssociation;
        }

        public BuildToolAssociation GetBuilderToolAssociationForExtensions(string sourceExtension, string destinationExtension)
        {
            return ProjectSpecificBuildTools.BuildToolList.FirstOrDefault(item =>
                item.SourceFileType != null && 
                item.SourceFileType.ToLowerInvariant() == sourceExtension.ToLowerInvariant() &&
                item.DestinationFileType.ToLowerInvariant() == destinationExtension.ToLowerInvariant());
        }

        internal BuildToolAssociation GetBuilderToolAssociationByName(string name)
        {
            BuildToolAssociation buildToolAssociation = null;

            foreach (BuildToolAssociation bta in ProjectSpecificBuildTools.BuildToolList)
            {
                if (bta.ToString().ToLower() == name.ToLower())
                {
                    buildToolAssociation = bta;
                    break;
                }
            }
            return buildToolAssociation;
        }



        internal bool LoadOrCreateProjectSpecificBuildTools(string projectFolder)
        {
            string settingsFolder = projectFolder + "GlueSettings/";

            string fileName = settingsFolder + "BuildToolAssociation.xml";


            bool wasLoaded = false;

            if (System.IO.File.Exists(fileName))
            {
                ProjectSpecificBuildTools = BuildToolAssociationList.FromFileXml(fileName);
                wasLoaded = true;
            }
            else
            {
                ProjectSpecificBuildTools = new BuildToolAssociationList();

                foreach (BuildToolAssociation bta in ProjectManager.GlueSettingsSave.BuildToolAssociations)
                {
                    ProjectSpecificBuildTools.BuildToolList.Add(bta);
                    if (!FileManager.IsRelative(bta.BuildTool))
                    {
                        bta.BuildTool = FileManager.MakeRelative(bta.BuildTool, projectFolder);
                    }
                }

                FileManager.XmlSerialize(ProjectSpecificBuildTools, fileName);
            }

            return wasLoaded;
        }

        public void SaveProjectSpecificBuildTools()
        {
            if (ProjectSpecificBuildTools != null)
            {
                FileManager.XmlSerialize(ProjectSpecificBuildTools, ProjectSpecificBuildToolAssociationFileName);

            }

        }

        public BuildToolAssociation GetBuildToolAssocationAndNameFor(string fileName, out bool userCancelled, out string rfsName, out string extraCommandLineArguments)
        {
            userCancelled = false;
            rfsName = null;

            BuildToolAssociation buildToolAssociation = null;

            string sourceExtension = FileManager.GetExtension(fileName);

            List<BuildToolAssociation> btaList = new List<BuildToolAssociation>();
            foreach (BuildToolAssociation bta in BuildToolAssociationManager.Self.ProjectSpecificBuildTools.BuildToolList)
            {
                if (bta.SourceFileType != null && bta.SourceFileType.ToLower() == sourceExtension.ToLower())
                {
                    btaList.Add(bta);
                }
            }



            NewFileWindow nfw = new NewFileWindow();
            nfw.ComboBoxMessage = "Which builder would you like to use for this file?";

            int commandLineArgumentsId = nfw.AddTextBox("Enter extra command line arguments:");

            foreach (BuildToolAssociation bta in btaList)
            {
                nfw.AddOption(bta);
            }

            if (btaList.Count != 0)
            {
                nfw.SelectedItem = btaList[0];
            }

            nfw.ResultName = FileManager.RemoveExtension(FileManager.RemovePath(fileName));
            //DialogResult result = cbmb.ShowDialog();
            DialogResult result = nfw.ShowDialog();
            extraCommandLineArguments = "";

            if (result == DialogResult.OK)
            {
                buildToolAssociation = nfw.SelectedItem as BuildToolAssociation;
                rfsName = nfw.ResultName;
                extraCommandLineArguments = nfw.GetValueFromId(commandLineArgumentsId);
            }
            else
            {
                userCancelled = true;
            }




            return buildToolAssociation;
        }

        public bool GetIfIsBuiltFile(string fileName)
        {

            string sourceExtension = FileManager.GetExtension(fileName);

            if (string.IsNullOrEmpty(sourceExtension))
            {
                return false;
            }
            else
            {
                return ProjectSpecificBuildTools.BuildToolList.Any(item => item.SourceFileType != null && item.SourceFileType.ToLower() == sourceExtension.ToLower());
            }
        }
    }
}
