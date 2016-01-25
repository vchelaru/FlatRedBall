using System.Windows.Forms;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using FlatRedBall.Glue.FormHelpers;
using System;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class DialogCommands : IDialogCommands
    {
        public ReferencedFileSave ShowAddNewFileDialog()
        {
            ReferencedFileSave rfs = null;

            NewFileWindow nfw = CreateNewFileWindow();

            if (nfw.ShowDialog(MainGlueWindow.Self) == DialogResult.OK)
            {
                string name = nfw.ResultName;
                AssetTypeInfo resultAssetTypeInfo = nfw.ResultAssetTypeInfo;
                string errorMessage;
                string directory = null;
                IElement element = EditorLogic.CurrentElement;

                if (EditorLogic.CurrentTreeNode.IsDirectoryNode())
                {
                    directory = EditorLogic.CurrentTreeNode.GetRelativePath().Replace("/", "\\");
                }

                var option = nfw.GetOptionFor(resultAssetTypeInfo);

                rfs = GlueProjectSaveExtensionMethods.AddReferencedFileSave(element, directory, name, resultAssetTypeInfo, 
                    option, out errorMessage);




                if (!string.IsNullOrEmpty(errorMessage))
                {
                    MessageBox.Show(errorMessage);
                }
                else if(rfs != null)
                {
                    
                    var createdFile = ProjectManager.MakeAbsolute(rfs.GetRelativePath());

                    if (createdFile.EndsWith(".csv"))
                    {
                        string location = ProjectManager.MakeAbsolute(createdFile);

                        CsvCodeGenerator.GenerateAndSaveDataClass(rfs, AvailableDelimiters.Comma);
                    }


                    ElementViewWindow.UpdateChangedElements();

                    ElementViewWindow.SelectedNode = GlueState.Self.Find.ReferencedFileSaveTreeNode(rfs);

                    PluginManager.ReactToNewFile(rfs);

                    GluxCommands.Self.SaveGlux();
                }

            }

            return rfs;
        }

        private static NewFileWindow CreateNewFileWindow()
        {
            NewFileWindow nfw = new NewFileWindow();

            PluginManager.AddNewFileOptions(nfw);

            if (GlueState.Self.CurrentElement != null)
            {
                foreach (ReferencedFileSave fileInElement in GlueState.Self.CurrentElement.ReferencedFiles)
                {
                    nfw.NamedAlreadyUsed.Add(FileManager.RemovePath(FileManager.RemoveExtension(fileInElement.Name)));
                }
            }

            // Also add CSV files
            nfw.AddOption(new AssetTypeInfo("csv", "", null, "Spreadsheet (.csv)", "", ""));
            return nfw;
        }

        public void SetFormOwner(Form form)
        {
            if(MainGlueWindow.Self != null)
                form.Owner = MainGlueWindow.Self;
        }




    }
}
