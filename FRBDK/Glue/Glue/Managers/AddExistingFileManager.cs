using EditorObjects.SaveClasses;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.Glue.Managers
{
    public class AddExistingFileManager : Singleton<AddExistingFileManager>
    {

        public void AddExistingFileClick()
        {
            var viewModel = new AddExistingFileViewModel();
            FillWithFiles(viewModel);


            var window = new AddExistingFileWindow();
            window.DataContext = viewModel;

            var result = window.ShowDialog();

            var element = GlueState.Self.CurrentElement;
            string directoryOfTreeNode = EditorLogic.CurrentTreeNode.GetRelativePath();
            if(result == true)
            {
                bool userCancelled = false;

                foreach(var file in viewModel.Files)
                {

                    TaskManager.Self.AddSync(() =>
                    {
                        AddSingleFile(file, ref userCancelled, element, directoryOfTreeNode);

                    }, $"Adding file {file}");
                }
            }
        }

        private void FillWithFiles(AddExistingFileViewModel viewModel)
        {
            var contentFolder = GlueState.Self.ContentDirectory;

            var files = FileManager.GetAllFilesInDirectory(contentFolder, null, int.MaxValue);

            foreach(var file in files)
            {

                // make this thing relative:
                var relativeFile = FileManager.MakeRelative(file, contentFolder);

                // Feb 22, 2019
                // Initially I thought
                // I'd only show files that
                // aren't already added to the
                // current element, but files can
                // be added multiple times to an element
                // using different converters. Even though
                // it's rare, we want to still support it so
                // don't filter out files that are already added.

                viewModel.UnfilteredFileList.Add(relativeFile);
            }
            viewModel.ContentFolder = contentFolder;
            viewModel.RefreshFilteredList();
        }

        public ReferencedFileSave AddSingleFile(string fileName, ref bool userCancelled, string options = null)
        {

            var element = GlueState.Self.CurrentElement;
            string directoryOfTreeNode = EditorLogic.CurrentTreeNode.GetRelativePath();
            return AddSingleFile(fileName, ref userCancelled, element, directoryOfTreeNode, options);
        }

        public ReferencedFileSave AddSingleFile(string fileName, ref bool userCancelled, IElement element, string directoryOfTreeNode, string options = null)
        {
            ReferencedFileSave toReturn = null;

            #region Find the BuildToolAssociation for the selected file

            string rfsName = FileManager.RemoveExtension(FileManager.RemovePath(fileName));
            string extraCommandLineArguments = null;

            BuildToolAssociation buildToolAssociation = null;
            bool isBuiltFile = BuildToolAssociationManager.Self.GetIfIsBuiltFile(fileName);
            bool userPickedNone = false;

            if (isBuiltFile)
            {
                buildToolAssociation = BuildToolAssociationManager.Self.GetBuildToolAssocationAndNameFor(fileName, out userCancelled, out userPickedNone, out rfsName, out extraCommandLineArguments);
            }

            #endregion

            string sourceExtension = FileManager.GetExtension(fileName);

            if (userPickedNone)
            {
                isBuiltFile = false;
            }

            if (isBuiltFile && buildToolAssociation == null && !userPickedNone)
            {
                GlueCommands.Self.PrintOutput("Couldn't find a tool for the file extension " + sourceExtension);
            }

            else if (!userCancelled)
            {

                toReturn = GlueCommands.Self.GluxCommands.AddSingleFileTo(fileName, rfsName, extraCommandLineArguments, buildToolAssociation,
                    isBuiltFile, options, element, directoryOfTreeNode);
            }



            return toReturn;

        }

    }
}
