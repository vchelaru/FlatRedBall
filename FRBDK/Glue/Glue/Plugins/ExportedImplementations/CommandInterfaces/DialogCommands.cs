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

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class DialogCommands : IDialogCommands
    {
        #region NamedObjectSave

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
                    GlueState.Self.CurrentNamedObjectSave = newNamedObject;
                }
                else
                {
                    GlueGui.ShowMessageBox(whyItIsntValid);
                }
            }

            return newNamedObject;
        }

        private static AddObjectViewModel CreateAndShowAddNamedObjectWindow(AddObjectViewModel addObjectViewModel = null)
        {

            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter the new object's name";
            tiw.Text = "New Object";
            // If windows is zoomed, the text may not wrap properly, so increase it:
            tiw.Width = 450;

            var currentObject = GlueState.Self.CurrentNamedObjectSave;

            bool isTypePredetermined = currentObject != null && currentObject.IsList;

            if (addObjectViewModel == null)
            {
                addObjectViewModel = new AddObjectViewModel();
            }

            var toAdd = AvailableClassTypeConverter.GetAvailableTypes(false, SourceType.FlatRedBallType)
                .ToList();

            toAdd.Sort();

            foreach(var item in toAdd)
            {
                addObjectViewModel.FlatRedBallAndCustomTypes.Add(item);
            }

            NewObjectTypeSelectionControl typeSelectControl = null;
            if (!isTypePredetermined)
            {
                tiw.Width = 400;

                typeSelectControl = new NewObjectTypeSelectionControl(addObjectViewModel);
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
                            var classType = typeSelectControl.SourceClassType;
                            if(classType?.Contains(".") == true)
                            {
                                // un-qualify if it's something like "FlatRedBall.Sprite"
                                var lastIndex = classType.LastIndexOf(".");
                                classType = classType.Substring(lastIndex + 1);
                            }
                            string textToAssign = classType + "Instance";
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

                if (addObjectViewModel != null)
                {
                    typeSelectControl.SourceType = addObjectViewModel.SourceType;
                    typeSelectControl.SourceFile = addObjectViewModel.SourceFile;
                }


                tiw.AddControl(typeSelectControl, AboveOrBelow.Above);
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

        #endregion

        #region ReferencedFileSave

        public ReferencedFileSave ShowAddNewFileDialog()
        {
            ReferencedFileSave rfs = null;

            var nfw = CreateNewFileWindow();

            var result = nfw.ShowDialog();

            if (result == true)
            {
                string name = nfw.ResultName;
                AssetTypeInfo resultAssetTypeInfo = nfw.ResultAssetTypeInfo;
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

        private static CustomizableNewFileWindow CreateNewFileWindow()
        {
            var nfw = new CustomizableNewFileWindow();

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

                            PluginManager.ReactToNewEntityCreated(entity, window);
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
                addObjectViewModel.SourceClassType = "Sprite";
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            if (window.TextChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "TextInstance";
                addObjectViewModel.SourceClassType = "Text";
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            if (window.CircleChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "CircleInstance";
                addObjectViewModel.SourceClassType = "Circle";
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;
                gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                GlueState.Self.CurrentElement = newElement;
            }

            if (window.AxisAlignedRectangleChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "AxisAlignedRectangleInstance";
                addObjectViewModel.SourceClassType = "AxisAlignedRectangle";
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
                addObjectViewModel.SourceClassType = "Polygon";
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
            // AddScreen, add screen, addnewscreen, add new screen
            if (ProjectManager.GlueProjectSave == null)
            {
                System.Windows.Forms.MessageBox.Show("You need to create or load a project first.");
            }
            else
            {
                if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
                {
                    var tiw = new CustomizableTextInputWindow();

                    tiw.Message = "Enter a name for the new Screen";

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

                    tiw.Result = name;

                    tiw.HighlghtText();

                    var result = tiw.ShowDialog();
                    if (result == true)
                    {
                        string whyItIsntValid;

                        if (!NameVerifier.IsScreenNameValid(tiw.Result, null, out whyItIsntValid))
                        {
                            MessageBox.Show(whyItIsntValid);
                        }
                        else
                        {
                            var screen =
                                GlueCommands.Self.GluxCommands.ScreenCommands.AddScreen(tiw.Result);

                            GlueState.Self.CurrentElement = screen;
                            var treeNode = EditorLogic.CurrentScreenTreeNode;
                            if(treeNode != null)
                            {
                                treeNode.Expand();
                            }
                        }

                    }
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
