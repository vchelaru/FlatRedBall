using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EditorObjects.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.Managers
{
    public class BuildToolAssociationManager
    {
        #region Fields

        static BuildToolAssociationManager mSelf;

        #endregion

        #region Properties

        List<BuildToolAssociation> ProjectSpecificBuildTools => GlueState.Self.GlueSettingsSave.BuildToolAssociations;

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

        #endregion

        internal BuildToolAssociation GetBuilderToolAssociationForSourceExtension(string sourceExtension)
        {
            BuildToolAssociation buildToolAssociation = null;

            foreach (BuildToolAssociation bta in ProjectSpecificBuildTools)
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

            foreach (BuildToolAssociation bta in ProjectSpecificBuildTools)
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
            return ProjectSpecificBuildTools.FirstOrDefault(item =>
                item.SourceFileType != null && 
                item.SourceFileType.ToLowerInvariant() == sourceExtension.ToLowerInvariant() &&
                item.DestinationFileType.ToLowerInvariant() == destinationExtension.ToLowerInvariant());
        }

        internal BuildToolAssociation GetBuilderToolAssociationByName(string name)
        {
            BuildToolAssociation buildToolAssociation = null;

            foreach (BuildToolAssociation bta in ProjectSpecificBuildTools)
            {
                if (bta.ToString().ToLower() == name.ToLower())
                {
                    buildToolAssociation = bta;
                    break;
                }
            }
            return buildToolAssociation;
        }


        public BuildToolAssociation GetBuildToolAssocationAndNameFor(string fileName, out bool userCancelled, out bool userPickedNone, out string rfsName, out string extraCommandLineArguments)
        {
            userCancelled = false;
            userPickedNone = false;
            rfsName = null;

            BuildToolAssociation buildToolAssociation = null;

            string sourceExtension = FileManager.GetExtension(fileName);

            List<BuildToolAssociation> btaList = new List<BuildToolAssociation>();
            foreach (BuildToolAssociation bta in ProjectSpecificBuildTools)
            {
                if (bta.SourceFileType != null && bta.SourceFileType.ToLower() == sourceExtension.ToLower())
                {
                    btaList.Add(bta);
                }
            }



            NewFileWindow nfw = new NewFileWindow();
            nfw.ComboBoxMessage = "Which builder would you like to use for this file?";

            int commandLineArgumentsId = nfw.AddTextBox("Enter extra command line arguments:");
            
            bool showNoneOption = Elements.AvailableAssetTypes.Self.AllAssetTypes
                .Any(item => item.Extension == sourceExtension && string.IsNullOrEmpty(item.CustomBuildToolName));

            if(showNoneOption)
            {
                nfw.AddOption("<None>");
            }

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
                if (buildToolAssociation != null)
                {
                    rfsName = nfw.ResultName;
                    extraCommandLineArguments = nfw.GetValueFromId(commandLineArgumentsId);
                }
                else
                {
                    userPickedNone = nfw.SelectedItem is string && (nfw.SelectedItem as string) == "<None>";
                }
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
                return GlueState.Self.GlueSettingsSave.BuildToolAssociations.Any(item => item.SourceFileType != null && item.SourceFileType.ToLower() == sourceExtension.ToLower());
            }
        }
    }
}
