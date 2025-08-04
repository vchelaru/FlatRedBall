using EditorObjects.SaveClasses;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
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
        // todo - this probably needs to move to some commands object.
        public async Task<ReferencedFileSave> AddSingleFile(FilePath fileName, object options = null, GlueElement elementToAddTo = null, string directoryOfTreeNode = null,
    AssetTypeInfo forcedAti = null)
        {
            bool userCancelled = false;
            elementToAddTo = elementToAddTo ?? GlueState.Self.CurrentElement;

            ReferencedFileSave toReturn = null;

            #region Find the BuildToolAssociation for the selected file

            string rfsName = fileName.NoPathNoExtension;
            string extraCommandLineArguments = null;

            BuildToolAssociation buildToolAssociation = null;
            bool isBuiltFile = BuildToolAssociationManager.Self.GetIfIsBuiltFile(fileName.FullPath);
            bool userPickedNone = false;

            if (isBuiltFile)
            {
                buildToolAssociation = BuildToolAssociationManager.Self.GetBuildToolAssocationAndNameFor(fileName.FullPath, out userCancelled, out userPickedNone, out rfsName, out extraCommandLineArguments);
            }

            #endregion

            string sourceExtension = fileName.Extension;

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

                //toReturn = GlueCommands.Self.GluxCommands.AddSingleFileTo(fileName.FullPath, rfsName, extraCommandLineArguments, buildToolAssociation,
                //    isBuiltFile, options, elementToAddTo, directoryOfTreeNode, forcedAssetTypeInfo:forcedAti);

                var response =
                    await GlueCommands.Self.GluxCommands.CreateReferencedFileSaveForExistingFileAsync(elementToAddTo, fileName, forcedAti, buildToolAssociation);

                if(response.Succeeded == false)
                {
                    GlueCommands.Self.PrintError(response.Message);
                    toReturn = null;
                }
                else
                {
                    toReturn = response.Data;

                }

            }



            return toReturn;

        }

    }
}
