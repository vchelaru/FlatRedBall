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
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using GlueFormsCore.ViewModels;
using GlueFormsCore.Controls;
using FlatRedBall.Glue.VSHelpers;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class DialogCommands : IDialogCommands
    {
        #region Project

        public async void ShowLoadProjectDialog()
        {
            if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
            {
                OpenFileDialog openFileDialog1 = new OpenFileDialog();

                openFileDialog1.InitialDirectory = "c:\\";
                openFileDialog1.Filter = "Project/Solution files (*.vcproj;*.csproj;*.sln)|*.vcproj;*.csproj;*.sln;";
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.RestoreDirectory = true;

                if (openFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    string projectFileName = openFileDialog1.FileName;

                    if (FileManager.GetExtension(projectFileName) == "sln")
                    {
                        var solution = VSSolution.FromFile(projectFileName);

                        string solutionName = projectFileName;

                        projectFileName = solution.ReferencedProjects.FirstOrDefault(item =>
                        {
                            var isRegularProject = FileManager.GetExtension(item) == "csproj" || FileManager.GetExtension(item) == "vsproj";

                            bool hasSameName = FileManager.RemovePath(FileManager.RemoveExtension(solutionName)).ToLowerInvariant() ==
                                FileManager.RemovePath(FileManager.RemoveExtension(item)).ToLowerInvariant();


                            return isRegularProject && hasSameName;
                        });

                        projectFileName = FileManager.GetDirectory(solutionName) + projectFileName;
                    }

                    await GlueCommands.Self.LoadProjectAsync(projectFileName);

                    // not sure why we need to do this....
                    //SaveSettings();
                }
            }
        }

        #endregion

        #region NamedObjectSave

        public NamedObjectSave ShowAddNewObjectDialog(AddObjectViewModel addObjectViewModel = null)
        {
            NamedObjectSave newNamedObject = null;

            // add named object, add object, addnamedobject, add new object, addnewobject, createobject, addobject

            var shouldAdd = CreateAndShowAddNamedObjectWindow(ref addObjectViewModel);

            if (shouldAdd == true)
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
                    GlueState.Self.CurrentNamedObjectSave = newNamedObject;
                }
                else
                {
                    GlueGui.ShowMessageBox(whyItIsntValid);
                }
            }

            return newNamedObject;
        }

        private static bool? CreateAndShowAddNamedObjectWindow(ref AddObjectViewModel addObjectViewModel)
        {
            var currentObject = GlueState.Self.CurrentNamedObjectSave;

            bool isTypePredetermined = (currentObject != null && currentObject.IsList) ||
                addObjectViewModel?.IsTypePredetermined == true;

            var isNewWindow = false;
            if (addObjectViewModel == null)
            {
                addObjectViewModel = new AddObjectViewModel();
                isNewWindow = true;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
            }

            if(isTypePredetermined && addObjectViewModel.SelectedAti != null)
            {
                addObjectViewModel.FlatRedBallAndCustomTypes.Add(addObjectViewModel.SelectedAti);
            }
            else
            {
                var toAdd = AvailableAssetTypes.Self.AllAssetTypes
                    .Where(item => item.CanBeObject)
                    .OrderBy(item => item.FriendlyName);

                addObjectViewModel.FlatRedBallAndCustomTypes.AddRange(toAdd);
            }
            addObjectViewModel.AvailableEntities =
                ObjectFinder.Self.GlueProject.Entities.ToList();

            addObjectViewModel.AvailableFiles =
                GlueState.Self.CurrentElement.ReferencedFiles.ToList();

            if(addObjectViewModel.SelectedItem != null)
            {

                var backingObject = addObjectViewModel.SelectedItem.BackingObject;

                // refresh the lists before trying to assing the object so the VM can select from the internal list, but do it
                // after grabbing the backingObject.

                addObjectViewModel.ForceRefreshToSourceType();
                // re-assign the backing object so it uses the current set of wrappers:
                if(backingObject is EntitySave backingEntitySave)
                {
                    addObjectViewModel.SelectedEntitySave = backingEntitySave;
                }
                else if(backingObject is AssetTypeInfo backingAti)
                {
                    addObjectViewModel.SelectedAti = backingAti;
                }
                else if(backingObject is ReferencedFileSave backingFile)
                {
                    addObjectViewModel.SourceFile = backingFile;
                }
            }
            if(isNewWindow)
            {
                addObjectViewModel.ForceRefreshToSourceType();
            }
            AvailableClassGenericTypeConverter converter = new AvailableClassGenericTypeConverter();

            var availableTypes = converter.GetAvailableValues(false);
            addObjectViewModel.AvailableListTypes.AddRange(availableTypes);

            // todo - need to handle predetermined types here

            addObjectViewModel.IsTypePredetermined = isTypePredetermined;
            
            if (isTypePredetermined)
            {
                var parentList = GlueState.Self.CurrentNamedObjectSave;

                var genericType = parentList?.SourceClassGenericType;

                if (!string.IsNullOrEmpty(genericType))
                {
                    addObjectViewModel.SelectedAti =
                        AvailableAssetTypes.Self.AllAssetTypes.FirstOrDefault(item =>
                            item.FriendlyName == genericType || item.QualifiedRuntimeTypeName.QualifiedType == genericType);

                    var genericEntityType =
                        ObjectFinder.Self.GetEntitySave(genericType);
                    if (genericEntityType != null)
                    {
                        addObjectViewModel.SourceType = SourceType.Entity;
                        addObjectViewModel.SelectedEntitySave = genericEntityType;
                    }
                    else
                    {
                        addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                    }
                }
            }

            var wpf = new NewObjectTypeSelectionControlWpf();
            wpf.DataContext = addObjectViewModel;
            var dialogResult = wpf.ShowDialog();
            return dialogResult;
        }

        #endregion

        #region ReferencedFileSave

        public ReferencedFileSave ShowAddNewFileDialog(AddNewFileViewModel viewModel = null)
        {
            ReferencedFileSave rfs = null;

            if (viewModel == null)
            {
                viewModel = new AddNewFileViewModel();
            }
            var nfw = CreateNewFileWindow(viewModel);

            if (viewModel.SelectedAssetTypeInfo == null)
            {
                viewModel.SelectedAssetTypeInfo = viewModel.FilteredOptions.FirstOrDefault();
            }

            var result = nfw.ShowDialog();

            if (result == true)
            {
                AssetTypeInfo resultAssetTypeInfo =
                    viewModel.SelectedAssetTypeInfo;

                var option = nfw.GetOptionFor(resultAssetTypeInfo);
                rfs = GlueCommands.Self.GluxCommands.CreateNewFileAndReferencedFileSave(viewModel, option);

            }

            return rfs;
        }

        private static CustomizableNewFileWindow CreateNewFileWindow(AddNewFileViewModel viewModel)
        {
            var nfw = new CustomizableNewFileWindow();
            nfw.DataContext = viewModel;

            PluginManager.AddNewFileOptions(nfw);

            if (GlueState.Self.CurrentElement != null)
            {
                foreach (ReferencedFileSave fileInElement in GlueState.Self.CurrentElement.ReferencedFiles)
                {
                    nfw.NamesAlreadyUsed.Add(FileManager.RemovePath(FileManager.RemoveExtension(fileInElement.Name)));
                }
            }

            // Also add CSV files
            nfw.AddOption(new AssetTypeInfo("csv", "", null, "Spreadsheet (.csv)", "", ""));
            return nfw;
        }

        #endregion

        #region EntitySave

        public void ShowAddNewEntityDialog()
        {
            // search:  addentity, add entity
            if (ProjectManager.GlueProjectSave == null)
            {
                System.Windows.Forms.MessageBox.Show("You need to create or load a project first.");
            }
            else
            {
                if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
                {
                    AddEntityWindow window = new Controls.AddEntityWindow();
                    var viewModel = new AddEntityViewModel();

                    var project = GlueState.Self.CurrentGlueProject;
                    var sortedEntities = project.Entities.ToList().OrderBy(item => item);

                    viewModel.BaseEntityOptions.Add("<NONE>");

                    foreach (var entity in project.Entities.ToList())
                    {
                        viewModel.BaseEntityOptions.Add(entity.Name);
                    }

                    viewModel.SelectedBaseEntity = "<NONE>";

                    window.DataContext = viewModel;
                    window.WindowStartupLocation = System.Windows.WindowStartupLocation.Manual;

                    double width = window.Width;
                    if(double.IsNaN(width))
                    {
                        width = 0;
                    }
                    double height = window.Height;
                    if(double.IsNaN(height))
                    {
                        height = 0;
                    }

                    window.Left = MainGlueWindow.MousePosition.X - width / 2;
                    window.Top = MainGlueWindow.MousePosition.Y - height / 2;

                    PluginManager.ModifyAddEntityWindow(window);

                    string directory = "";

                    if (EditorLogic.CurrentTreeNode?.IsDirectoryNode() == true)
                    {
                        directory = EditorLogic.CurrentTreeNode.GetRelativePath();
                        directory = directory.Replace('/', '\\');
                    }
                        
                    var result = window.ShowDialog();

                    if (result == true)
                    {
                        string entityName = viewModel.Name;

                        string whyIsntValid;

                        if (!NameVerifier.IsEntityNameValid(entityName, null, out whyIsntValid))
                        {
                            MessageBox.Show(whyIsntValid);
                        }
                        else
                        {
                            var entity = GlueCommands.Self.GluxCommands.EntityCommands.AddEntity(viewModel, directory);

                            PluginManager.ReactToNewEntityCreatedWithUi(entity, window);
                        }
                    }
                }
            }
        }

        #endregion

        #region Variable

        public void ShowAddNewVariableDialog(CustomVariableType variableType = CustomVariableType.Exposed, 
            string tunnelingObject = "",
            string tunneledVariableName = "")
        {
            // Search terms:  add new variable, addnewvariable, add variable

            AddVariableWindow addVariableWindow = new AddVariableWindow(GlueState.Self.CurrentElement);
            addVariableWindow.DesiredVariableType = variableType;

            addVariableWindow.TunnelingObject = tunnelingObject;
            addVariableWindow.TunnelingVariable = tunneledVariableName;

            if (addVariableWindow.ShowDialog(MainGlueWindow.Self) == DialogResult.OK)
            {
                HandleAddVariableOk(addVariableWindow);
            }
        }

        private static void HandleAddVariableOk(AddVariableWindow addVariableWindow)
        {
            string resultName = addVariableWindow.ResultName;
            IElement currentElement = EditorLogic.CurrentElement;
            string failureMessage;

            bool didFailureOccur = IsVariableInvalid(addVariableWindow, resultName, currentElement, out failureMessage);


            if (!didFailureOccur)
            {
                if (!string.IsNullOrEmpty(addVariableWindow.TunnelingObject) && string.IsNullOrEmpty(addVariableWindow.TunnelingVariable))
                {
                    didFailureOccur = true;
                    failureMessage = $"You must select a variable on {addVariableWindow.TunnelingObject}";
                }
            }

            if (didFailureOccur)
            {
                MessageBox.Show(failureMessage);
            }
            else
            {

                // See if there is already a variable in the base with this name
                CustomVariable existingVariableInBase = EditorLogic.CurrentElement.GetCustomVariableRecursively(resultName);

                bool canCreate = true;
                bool isDefinedByBase = false;
                if (existingVariableInBase != null)
                {
                    if (existingVariableInBase.SetByDerived)
                    {
                        isDefinedByBase = true;
                    }
                    else
                    {
                        MessageBox.Show("There is already a variable named\n\n" + resultName +
                            "\n\nin the base element, but it is not SetByDerived.\nGlue will not " +
                            "create a variable because it would result in a name conflict.");

                        canCreate = false;
                    }
                }

                if (canCreate)
                {
                    string type = addVariableWindow.ResultType;
                    string sourceObject = addVariableWindow.TunnelingObject;
                    string sourceObjectProperty = null;
                    if (!string.IsNullOrEmpty(sourceObject))
                    {
                        sourceObjectProperty = addVariableWindow.TunnelingVariable;
                    }
                    string overridingType = addVariableWindow.OverridingType;
                    string typeConverter = addVariableWindow.TypeConverter;

                    CustomVariable newVariable = new CustomVariable();
                    newVariable.Name = resultName;
                    newVariable.Type = type;
                    newVariable.SourceObject = sourceObject;
                    newVariable.SourceObjectProperty = sourceObjectProperty;

                    newVariable.IsShared = addVariableWindow.IsStatic;



                    if (!string.IsNullOrEmpty(overridingType))
                    {
                        newVariable.OverridingPropertyType = overridingType;
                        newVariable.TypeConverter = typeConverter;
                    }


                    RightClickHelper.CreateAndAddNewVariable(newVariable);

                    if (isDefinedByBase)
                    {
                        newVariable.DefinedByBase = isDefinedByBase;
                        // Refresh the UI - it's refreshed above in CreateAndAddNewVariable,
                        // but we're changing the DefinedByBase property which changes the color
                        // of the variable so refresh it again
                        EditorLogic.CurrentElementTreeNode.RefreshTreeNodes();
                    }
                    ElementViewWindow.ShowAllElementVariablesInPropertyGrid();


                    if (GlueState.Self.CurrentElement != null)
                    {
                        PluginManager.ReactToItemSelect(GlueState.Self.CurrentTreeNode);
                    }
                }
            }
        }

        public static bool IsVariableInvalid(AddVariableWindow addVariableWindow, string resultName, IElement currentElement, out string failureMessage)
        {
            bool didFailureOccur = false;

            string whyItIsntValid = "";

            didFailureOccur = NameVerifier.IsCustomVariableNameValid(resultName, null, currentElement, ref whyItIsntValid) == false;
            failureMessage = null;
            if (didFailureOccur)
            {
                failureMessage = whyItIsntValid;

            }
            else if (addVariableWindow != null && NameVerifier.DoesTunneledVariableAlreadyExist(addVariableWindow.TunnelingObject, addVariableWindow.TunnelingVariable, currentElement))
            {
                didFailureOccur = true;
                failureMessage = "There is already a variable that is modifying " + addVariableWindow.TunnelingVariable + " on " + addVariableWindow.TunnelingObject;
            }
            else if (addVariableWindow != null && IsUserTryingToCreateNewWithExposableName(addVariableWindow.ResultName, addVariableWindow.DesiredVariableType == CustomVariableType.Exposed))
            {
                didFailureOccur = true;
                failureMessage = "The variable\n\n" + resultName + "\n\nis an expoable variable.  Please use a different variable name or select the variable through the Expose tab";
            }

            else if (ExposedVariableManager.IsReservedPositionedPositionedObjectMember(resultName) && currentElement is EntitySave)
            {
                didFailureOccur = true;
                failureMessage = "The variable\n\n" + resultName + "\n\nis reserved by FlatRedBall.";
            }

            return didFailureOccur;
        }

        private static bool IsUserTryingToCreateNewWithExposableName(string resultName, bool isExposeTabSelected)
        {
            List<string> exposables = ExposedVariableManager.GetExposableMembersFor(EditorLogic.CurrentElement, false).Select(item => item.Member).ToList();
            if (exposables.Contains(resultName))
            {
                return isExposeTabSelected == false;
            }
            else
            {
                return false;
            }
        }


        #endregion

        #region Screen

        public void ShowAddNewScreenDialog()
        {
            //////////////Early Out////////////
            if (ProjectManager.GlueProjectSave == null)
            {
                System.Windows.Forms.MessageBox.Show("You need to create or load a project first.");
                return;
            }
            if (ProjectManager.StatusCheck() != ProjectManager.CheckResult.Passed)
            {
                return;
            }
            ////////////End Early Out

            // AddScreen, add screen, addnewscreen, add new screen
            var addScreenWindow = new AddScreenWindow();

            addScreenWindow.Message = "Enter a name for the new Screen";

            string name = "NewScreen";

            if(GlueState.Self.CurrentGlueProject.Screens.Count == 0)
            {
                name = "GameScreen";
            }

            var allScreenNames =
                GlueState.Self.CurrentGlueProject.Screens
                .Select(item => item.GetStrippedName())
                .ToList();

            name = StringFunctions.MakeStringUnique(name,
                allScreenNames, 2);

            addScreenWindow.Result = name;

            addScreenWindow.HighlghtText();

            PluginManager.ModifyAddScreenWindow(addScreenWindow);


            var result = addScreenWindow.ShowDialog();
            if (result == true)
            {
                string whyItIsntValid;

                if (!NameVerifier.IsScreenNameValid(addScreenWindow.Result, null, out whyItIsntValid))
                {
                    MessageBox.Show(whyItIsntValid);
                }
                else
                {
                    var screen =
                        GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(addScreenWindow.Result);

                    GlueState.Self.CurrentElement = screen;
                    var treeNode = EditorLogic.CurrentScreenTreeNode;
                    if(treeNode != null)
                    {
                        treeNode.Expand();
                    }

                    PluginManager.ReactToNewScreenCreatedWithUi(screen, addScreenWindow);

                }

            }
        }

        public void ShowCreateDerivedScreenDialog(ScreenSave baseScreen)
        {
            var popup = new TextInputWindow();
            popup.Message = "Enter new screen (level) name";
            var dialogResult = popup.ShowDialog();

            if (dialogResult == DialogResult.OK)
            {
                var newScreenName = popup.Result;

                var screen = new ScreenSave();
                screen.Name = @"Screens\" + newScreenName;
                screen.BaseScreen = baseScreen.Name;

                GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(screen);

                GlueState.Self.CurrentScreenSave = screen;
                screen.UpdateFromBaseType();
            }
        }

        #endregion

        #region Event

        public void ShowAddNewEventDialog(NamedObjectSave eventOwner)
        {
            var name = eventOwner.InstanceName;

            AddEventViewModel viewModel = new AddEventViewModel();
            viewModel.TunnelingObject = name;
            viewModel.DesiredEventType = CustomEventType.Tunneled;

            ShowAddNewEventDialog(viewModel);
        }

        public void ShowAddNewEventDialog(AddEventViewModel viewModel)
        {
            AddEventWindow addEventWindow = new AddEventWindow();
            addEventWindow.ViewModel = viewModel;

            if (addEventWindow.ShowDialog(MainGlueWindow.Self) == DialogResult.OK)
            {
                RightClickHelper.HandleAddEventOk(addEventWindow);
            }

        }

        #endregion

        public void FocusTab(string dialogTitle)
        {
            bool TryFocus(TabControl control )
            {
                foreach(TabPage tabPage in control.TabPages)
                {
                    if (tabPage.Text?.Trim() == dialogTitle)
                    {
                        control.SelectedTab = tabPage;
                        if(tabPage is PluginTab pluginTab)
                        {
                            pluginTab.LastTimeClicked = DateTime.Now;
                        }
                        return true;
                    }
                }
                return false;
            }

            var focused = TryFocus(PluginManager.TopTab);

            if (!focused) focused = TryFocus(PluginManager.BottomTab);
            if (!focused) focused = TryFocus(PluginManager.LeftTab);
            if (!focused) focused = TryFocus(PluginManager.CenterTab);
            if (!focused) focused = TryFocus(PluginManager.RightTab);
        }

        public void SetFormOwner(Form form)
        {
            if (MainGlueWindow.Self != null)
                form.Owner = MainGlueWindow.Self;
        }

        #region Show Message Box

        public void ShowMessageBox(string message)
        {
            GlueGui.ShowMessageBox(message);
        }

        public void ShowYesNoMessageBox(string message, Action yesAction, Action noAction = null)
        {
            if (GlueGui.ShowGui)
            {
                GlueCommands.Self.DoOnUiThread(() =>
               {
                    var result = MessageBox.Show(message, "", MessageBoxButtons.YesNo);

                   if(result == DialogResult.Yes)
                   {
                       yesAction?.Invoke();
                   }
                   else if(result == DialogResult.No)
                   {
                       noAction?.Invoke();
                   }
               });
                
            }
        }

        #endregion



        public List<T> ToList<T>(System.Collections.IList list)
        {
            List<T> toReturn = new List<T>();


            foreach(T item in list)
            {
                toReturn.Add(item);
            }

            return toReturn;
        }
    }
}
