using System.Windows.Forms;
using System.Linq;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using FlatRedBall.Glue.FormHelpers;
using System;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Utilities;

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

        public NamedObjectSave ShowAddNewObjectDialog(AddObjectViewModel addObjectViewModel = null)
        {
            NamedObjectSave newNamedObject = null;

            // add named object, add object, addnamedobject, add new object, addnewobject, createobject, addobject

            addObjectViewModel = CreateAndShowAddNamedObjectWindow(addObjectViewModel);

            if (addObjectViewModel.DialogResult == DialogResult.OK)
            {
                string whyItIsntValid = null;
                bool isValid = NameVerifier.IsNamedObjectNameValid(addObjectViewModel.ObjectName, out whyItIsntValid);

                if (isValid)
                {
                    if (addObjectViewModel.SourceType == SourceType.Entity && !RecursionManager.Self.CanContainInstanceOf(GlueState.Self.CurrentElement, addObjectViewModel.SourceClassType))
                    {
                        isValid = false;
                        whyItIsntValid = "This type would result in infinite recursion";
                    }
                }

                if (isValid)
                {
                    newNamedObject = GlueCommands.Self.GluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                }
                else
                {
                    GlueGui.ShowMessageBox(whyItIsntValid);
                }
            }

            return newNamedObject;
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

        private static AddObjectViewModel CreateAndShowAddNamedObjectWindow(AddObjectViewModel addObjectViewModel = null)
        {

            TextInputWindow tiw = new TextInputWindow();
            tiw.DisplayText = "Enter the new object's name";
            tiw.Text = "New Object";

            var currentObject = GlueState.Self.CurrentNamedObjectSave;

            bool isTypePredetermined = currentObject != null && currentObject.IsList;

            NewObjectTypeSelectionControl typeSelectControl = null;
            if (!isTypePredetermined)
            {
                tiw.Width = 400;

                typeSelectControl = new NewObjectTypeSelectionControl();
                typeSelectControl.Width = tiw.Width - 22;

                typeSelectControl.AfterStrongSelect += delegate
                {
                    tiw.ClickOk();
                };

                typeSelectControl.AfterSelect += delegate (object sender, EventArgs args)
                {
                    string result = tiw.Result;

                    bool isDefault = string.IsNullOrEmpty(result);

                    // Victor Chelaru November 3, 2012
                    // I don't know if we want to only re-assign when default.
                    // The downside is that the user may have already entered a
                    // name, an then changed the type.  This would result in the
                    // user-entered name being overwritten.  However, if we don't
                    // change the name, then an old name that the user entered which
                    // is specific to the type may not get reset.  I'm leaning towards
                    // always changing the name to help prevent misnaming, and it's also
                    // less programatically complex.
                    //if (isDefault)
                    {
                        string newName;

                        if (!string.IsNullOrEmpty(typeSelectControl.SourceFile) && !string.IsNullOrEmpty(typeSelectControl.SourceName))
                        {
                            newName = HandleObjectInFileSelected(typeSelectControl);
                        }
                        else if (string.IsNullOrEmpty(typeSelectControl.SourceClassType))
                        {
                            newName = "ObjectInstance";

                        }
                        else
                        {

                            string textToAssign = typeSelectControl.SourceClassType + "Instance";
                            if (textToAssign.Contains("/") || textToAssign.Contains("\\"))
                            {
                                textToAssign = FileManager.RemovePath(textToAssign);
                            }

                            newName = textToAssign.Replace("<T>", "");
                        }

                        // We need to make sure this is a unique name.
                        newName = StringFunctions.MakeStringUnique(newName, EditorLogic.CurrentElement.AllNamedObjects);
                        tiw.Result = newName;
                    }
                };

                if(addObjectViewModel != null)
                {
                    typeSelectControl.SourceType = addObjectViewModel.SourceType;
                    typeSelectControl.SourceFile = addObjectViewModel.SourceFile;
                }


                tiw.AddControl(typeSelectControl, AboveOrBelow.Above);
            }

            if (addObjectViewModel == null)
            {
                addObjectViewModel = new AddObjectViewModel();
            }
            addObjectViewModel.DialogResult = tiw.ShowDialog();

            addObjectViewModel.SourceType = SourceType.FlatRedBallType;
            addObjectViewModel.SourceClassType = null;
            addObjectViewModel.SourceFile = null;
            addObjectViewModel.SourceNameInFile = null;
            addObjectViewModel.SourceClassGenericType = null;
            addObjectViewModel.ObjectName = tiw.Result;

            if (isTypePredetermined)
            {
                var parentList = GlueState.Self.CurrentNamedObjectSave;

                var genericType = parentList.SourceClassGenericType;

                if (!string.IsNullOrEmpty(genericType))
                {
                    addObjectViewModel.SourceClassType = genericType;

                    // the generic type will be fully qualified (like FlatRedBall.Sprite)
                    // but object types for FRB primitives are not qualified, so we need to remove
                    // any dots

                    if (addObjectViewModel.SourceClassType.Contains("."))
                    {
                        int lastDot = addObjectViewModel.SourceClassType.LastIndexOf('.');

                        addObjectViewModel.SourceClassType = addObjectViewModel.SourceClassType.Substring(lastDot + 1);
                    }

                    if (ObjectFinder.Self.GetEntitySave(genericType) != null)
                    {
                        addObjectViewModel.SourceType = SourceType.Entity;
                    }
                    else
                    {
                        addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                    }
                }
            }

            if (typeSelectControl != null)
            {
                if (!string.IsNullOrEmpty(typeSelectControl.SourceClassType) || typeSelectControl.SourceType == SourceType.File)
                {
                    addObjectViewModel.SourceType = typeSelectControl.SourceType;
                }
                addObjectViewModel.SourceFile = typeSelectControl.SourceFile;
                addObjectViewModel.SourceNameInFile = typeSelectControl.SourceName;

                addObjectViewModel.SourceClassType = typeSelectControl.SourceClassType;
                addObjectViewModel.SourceClassGenericType = typeSelectControl.SourceClassGenericType;
            }

            return addObjectViewModel;
        }

        public void ShowMessageBox(string message)
        {
            GlueGui.ShowMessageBox(message);
        }


        private static string HandleObjectInFileSelected(NewObjectTypeSelectionControl typeSelectControl)
        {
            string newName;
            var spaceParen = typeSelectControl.SourceName.IndexOf(" (");

            if (spaceParen != -1)
            {
                newName = typeSelectControl.SourceName.Substring(0, spaceParen);
            }
            else
            {
                newName = typeSelectControl.SourceName;
            }

            // If the user selected "Entire File" we want to make sure the space doesn't show up:
            newName = newName.Replace(" ", "");

            string throwaway;
            bool isInvalid = NameVerifier.IsNamedObjectNameValid(newName, out throwaway);

            if (!isInvalid)
            {
                // let's get the type:
                var split = typeSelectControl.SourceName.Split('(', ')');

                var last = split.LastOrDefault(item => !string.IsNullOrEmpty(item));

                if (last != null)
                {
                    var lastDot = last.LastIndexOf('.');

                    newName = last.Substring(lastDot + 1, last.Length - (lastDot + 1));
                }
            }

            return newName;
        }
    }
}
