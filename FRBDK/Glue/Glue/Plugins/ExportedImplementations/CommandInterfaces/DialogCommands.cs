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

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class DialogCommands : IDialogCommands
    {
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

            bool isTypePredetermined = currentObject != null && currentObject.IsList;

            var isNewWindow = false;
            if (addObjectViewModel == null)
            {
                addObjectViewModel = new AddObjectViewModel();
                isNewWindow = true;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
            }

            var toAdd = AvailableAssetTypes.Self.AllAssetTypes
                .Where(item => item.CanBeObject)
                .OrderBy(item => item.FriendlyName);

            addObjectViewModel.FlatRedBallAndCustomTypes.AddRange(toAdd);

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

                var genericType = parentList.SourceClassGenericType;

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
                string name = viewModel.FileName;
                AssetTypeInfo resultAssetTypeInfo =
                    //nfw.ResultAssetTypeInfo;
                    viewModel.SelectedAssetTypeInfo;

                string errorMessage;
                string directory = null;
                var element = GlueState.Self.CurrentElement;

                if (EditorLogic.CurrentTreeNode.IsDirectoryNode())
                {
                    directory = EditorLogic.CurrentTreeNode.GetRelativePath().Replace("/", "\\");
                }

                var option = nfw.GetOptionFor(resultAssetTypeInfo);

                rfs = GlueProjectSaveExtensionMethods.AddReferencedFileSave(
                    element, directory, name, resultAssetTypeInfo,
                    option, out errorMessage);

                if (!string.IsNullOrEmpty(errorMessage))
                {
                    MessageBox.Show(errorMessage);
                }
                else if (rfs != null)
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

                    GluxCommands.Self.SaveGluxTask();
                }

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

                    var result = window.ShowDialog();

                    if (result == true)
                    {
                        string entityName = window.EnteredText;

                        string whyIsntValid;

                        if (!NameVerifier.IsEntityNameValid(entityName, null, out whyIsntValid))
                        {
                            MessageBox.Show(whyIsntValid);
                        }
                        else
                        {
                            string directory = "";

                            if (EditorLogic.CurrentTreeNode?.IsDirectoryNode() == true)
                            {
                                directory = EditorLogic.CurrentTreeNode.GetRelativePath();
                                directory = directory.Replace('/', '\\');
                            }
                            var entity = CreateEntityAndObjects(window, entityName, directory);

                            PluginManager.ReactToNewEntityCreatedWithUi(entity, window);
                        }
                    }
                }
            }
        }

        private static EntitySave CreateEntityAndObjects(AddEntityWindow window, string entityName, string directory)
        {
            var gluxCommands = GlueCommands.Self.GluxCommands;

            var newElement = gluxCommands.EntityCommands.AddEntity(
                directory + entityName, is2D: true);

            GlueState.Self.CurrentElement = newElement;

            if (window.SpriteChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "SpriteInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Sprite;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            if (window.TextChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "TextInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Text;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            if (window.CircleChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "CircleInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Circle;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            if (window.AxisAlignedRectangleChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "AxisAlignedRectangleInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.AxisAlignedRectangle;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            // There are a few important things to note about this function:
            // 1. Whenever gluxCommands.AddNewNamedObjectToSelectedElement is called, Glue performs a full
            //    refresh and save. The reason for this is that gluxCommands.AddNewNamedObjectToSelectedElement
            //    is the standard way to add a new named object to an element, and it may be called by other parts
            //    of the code (and plugins) that expect the add to be a complete set of logic (add, refresh, save, etc).
            //    This is less efficient than adding all of them and saving only once, but that would require a second add
            //    method, which would add complexity. For now, we deal with the slower calls because it's not really noticeable.
            // 2. Some actions, like adding Points to a polygon, are done after the polygon is created and added, and that requires
            //    an additional save. Therefore, we do one last save/refresh at the end of this method in certain situations.
            //    Again, this is less efficient than if we performed just a single call, but a single call would be more complicated.
            //    because we'd have to suppress all the other calls.
            bool needsRefreshAndSave = false;

            if (window.PolygonChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "PolygonInstance";
                addObjectViewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Polygon;
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;

                var nos = gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                CustomVariableInNamedObject instructions = null;
                instructions = nos.GetCustomVariable("Points");
                if (instructions == null)
                {
                    instructions = new CustomVariableInNamedObject();
                    instructions.Member = "Points";
                    nos.InstructionSaves.Add(instructions);
                }
                var points = new List<Vector2>();
                points.Add(new Vector2(-16, 16));
                points.Add(new Vector2(16, 16));
                points.Add(new Vector2(16, -16));
                points.Add(new Vector2(-16, -16));
                points.Add(new Vector2(-16, 16));
                instructions.Value = points;


                needsRefreshAndSave = true;

                GlueState.Self.CurrentElement = newElement;
            }

            if (window.IVisibleChecked)
            {
                newElement.ImplementsIVisible = true;
                needsRefreshAndSave = true;
            }

            if (window.IClickableChecked)
            {
                newElement.ImplementsIClickable = true;
                needsRefreshAndSave = true;
            }

            if (window.IWindowChecked)
            {
                newElement.ImplementsIWindow = true;
                needsRefreshAndSave = true;
            }

            if (window.ICollidableChecked)
            {
                newElement.ImplementsICollidable = true;
                needsRefreshAndSave = true;
            }

            if (needsRefreshAndSave)
            {
                MainGlueWindow.Self.PropertyGrid.Refresh();
                ElementViewWindow.GenerateSelectedElementCode();
                GluxCommands.Self.SaveGluxTask();
            }

            return newElement;
        }

        #endregion

        #region Variable

        public void ShowAddNewVariableDialog(CustomVariableType variableType = CustomVariableType.Exposed, 
            string tunnelingObject = "",
            string tunneledVariableName = "")
        {
            // Search terms:  add new variable, addnewvariable, add variable

            AddVariableWindow addVariableWindow = new AddVariableWindow(EditorLogic.CurrentElement);
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
