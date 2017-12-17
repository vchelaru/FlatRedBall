using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Glue.StandardTypes;
using FlatRedBall.Glue.VSHelpers.Projects;
using Glue;
using FlatRedBall.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Controls;
using System.IO;
using System.Diagnostics;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.VSHelpers;
using FlatRedBall.Glue.Elements;
using EditorObjects.SaveClasses;
using FlatRedBall.Utilities;
using FlatRedBall.Content;
using FlatRedBall.Content.Scene;
using System.Collections;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Glue.IO.Zip;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using FlatRedBall.Glue.ContentPipeline;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Content.SpriteFrame;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.SaveClasses.Helpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Factories;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.ViewModels;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Glue.FormHelpers
{
    public enum MenuShowingAction
    {
        RegularRightClick,
        RightButtonDrag
    }

    public static class RightClickHelper
    {
        #region Fields

        static ToolStripMenuItem mMoveToTop;
        static ToolStripMenuItem mMoveToBottom;

        static ToolStripMenuItem mMoveUp;
        static ToolStripMenuItem mMoveDown;
        static ToolStripMenuItem mMakeRequiredAtStartup;
        static ToolStripMenuItem mRebuildFile;

        static ToolStripMenuItem mViewSourceInExplorer;
        static ToolStripMenuItem mRecreateCompanionFiles;

        static ToolStripMenuItem mViewCodeFolderInExplorer;
        static ToolStripMenuItem mViewContentFilesInExplorer;

        static ToolStripMenuItem mFindAllReferences;

        static ToolStripMenuItem mDeleteFolder;
        static ToolStripMenuItem mRenameFolder;

        static ToolStripMenuItem mDuplicate;

        static ToolStripMenuItem mAddState;
        static ToolStripMenuItem mAddStateCategory;

        static ToolStripMenuItem mAddResetVariablesForPooling;

        static ToolStripMenuItem mFillValuesFromDefault;

        static ToolStripMenuItem mUseContentPipeline;

        static ToolStripMenuItem mRemoveFromProjectQuick;
        static ToolStripMenuItem mCreateNewFileForMissingFile;

        static ToolStripMenuItem mViewFileLoadOrder;

        static ToolStripMenuItem mCreateZipPackage;
        static ToolStripMenuItem mExportElement;
        static ToolStripMenuItem mImportElement;

        static ToolStripMenuItem mAddEventMenuItem;

        static ToolStripMenuItem mRefreshTreeNodesMenuItem;

        static ToolStripMenuItem mAddEntityInstance;
        static ToolStripMenuItem mAddEntityList;

        static ToolStripMenuItem mCopyToBuildFolder;

        static ToolStripMenuItem reGenerateCodeToolStripMenuItem;


        #endregion



        ///////////////////////////////////////////////////////////
        public static void PopulateRightClickItems(TreeNode targetNode, MenuShowingAction menuShowingAction = MenuShowingAction.RegularRightClick)
        {

            MainGlueWindow.Self.ElementTreeView.SelectedNode = targetNode;
            MainGlueWindow form = MainGlueWindow.Self;

            ContextMenuStrip menu = MainGlueWindow.Self.mElementContextMenu;

            menu.Items.Clear();
            var sourceNode = ElementViewWindow.TreeNodeDraggedOff;


            #region IsScreenNode

            if (targetNode.IsScreenNode())
            {
                if (menuShowingAction == MenuShowingAction.RightButtonDrag)
                {
                    if(sourceNode.IsEntityNode())
                    {
                        menu.Items.Add(mAddEntityInstance);
                        menu.Items.Add(mAddEntityList);
                    }   
                }
                else
                {
                    menu.Items.Add(form.setAsStartUpScreenToolStripMenuItem);
                    menu.Items.Add(mMakeRequiredAtStartup);
                    mExportElement.Text = "Export Screen";
                    menu.Items.Add(mExportElement);

                    AddRemoveFromProjectItems(form, menu);

                    if (EditorLogic.CurrentScreenSave.IsRequiredAtStartup)
                    {
                        mMakeRequiredAtStartup.Text = "Remove StartUp Requirement";
                    }
                    else
                    {
                        mMakeRequiredAtStartup.Text = "Make Required at StartUp";
                    }


                    menu.Items.Add("-");
                    menu.Items.Add(mFindAllReferences);
                    menu.Items.Add(mRefreshTreeNodesMenuItem);
                }
            }

            #endregion

            #region IsEntityNode

            else if (targetNode.IsEntityNode())
            {
                if (menuShowingAction == MenuShowingAction.RightButtonDrag && sourceNode.IsEntityNode())
                {
                    menu.Items.Add(mAddEntityInstance);
                    menu.Items.Add(mAddEntityList);
                }
                else
                {
                    AddRemoveFromProjectItems(form, menu);

                    menu.Items.Add("-");
                    mExportElement.Text = "Export Entity";
                    menu.Items.Add(mExportElement);
                    menu.Items.Add(mFindAllReferences);

                    EntitySave entitySave = ((EntityTreeNode)targetNode).EntitySave;

                    if (entitySave.PooledByFactory)
                    {
                        menu.Items.Add(mAddResetVariablesForPooling);
                    }
                    menu.Items.Add(mRefreshTreeNodesMenuItem);
                }
            }

            #endregion

            #region IsFileContainerNode OR IsFolderInFilesContainerNode

            else if (targetNode.IsFilesContainerNode() || targetNode.IsFolderInFilesContainerNode())
            {
                menu.Items.Add(form.addFileToolStripMenuItem);
                menu.Items.Add(form.addFolderToolStripMenuItem);
                menu.Items.Add("-");
                menu.Items.Add(form.viewInExplorerToolStripMenuItem);
                if (targetNode.IsFolderInFilesContainerNode())
                {
                    menu.Items.Add(mDeleteFolder);
                }
            }

            #endregion

            #region IsRootObjectNode

            else if (targetNode.IsRootObjectNode())
            {
                bool isSameObject = false;

                if (targetNode.GetContainingElementTreeNode() != null && ElementViewWindow.TreeNodeDraggedOff != null)
                {
                    isSameObject = targetNode.GetContainingElementTreeNode().Tag ==
                    ElementViewWindow.TreeNodeDraggedOff.Tag as ElementCommands;
                }

                if (menuShowingAction == MenuShowingAction.RightButtonDrag && !isSameObject && sourceNode.IsEntityNode())
                {
                    menu.Items.Add(mAddEntityInstance);
                    menu.Items.Add(mAddEntityList);
                }
                else
                {
                    menu.Items.Add(form.addObjectToolStripMenuItem);
                }
            }

            #endregion

            #region IsGlobalContentContainerNode
            else if (targetNode.IsGlobalContentContainerNode())
            {
                menu.Items.Add(form.addFileToolStripMenuItem);
                menu.Items.Add(form.addFolderToolStripMenuItem);
                menu.Items.Add(reGenerateCodeToolStripMenuItem);

                menu.Items.Add(form.viewInExplorerToolStripMenuItem);

                menu.Items.Add(mViewFileLoadOrder);
            }
            #endregion

            #region IsRootEntityNode
            else if (targetNode.IsRootEntityNode())
            {
                menu.Items.Add(form.addEntityToolStripMenuItem);

                mImportElement.Text = "Import Entity";
                menu.Items.Add(mImportElement);

                menu.Items.Add(form.addFolderToolStripMenuItem);
            }
            #endregion

            #region IsRootScreenNode
            else if (targetNode.IsRootScreenNode())
            {
                menu.Items.Add(form.addScreenToolStripMenuItem);

                mImportElement.Text = "Import Screen";
                menu.Items.Add(mImportElement);

            }
            #endregion
            
            #region IsRootCustomVariables

            else if (targetNode.IsRootCustomVariablesNode())
            {
                menu.Items.Add(form.addVariableToolStripMenuItem);
            }

            #endregion

            #region IsRootEventNode
            else if (targetNode.IsRootEventsNode())
            {
                menu.Items.Add(mAddEventMenuItem);
            }
            #endregion

            #region IsNamedObjectNode

            else if (targetNode.IsNamedObjectNode())
            {
                AddRemoveFromProjectItems(form, menu);

                menu.Items.Add(form.editResetVariablesToolStripMenuItem);
                menu.Items.Add(mFindAllReferences);

                menu.Items.Add("-");

                menu.Items.Add(mDuplicate);

                menu.Items.Add("-");

                menu.Items.Add(mMoveToTop);
                menu.Items.Add(mMoveUp);
                menu.Items.Add(mMoveDown);
                menu.Items.Add(mMoveToBottom);

                menu.Items.Add("-");

                NamedObjectSave currentNamedObject = EditorLogic.CurrentNamedObject;

                if (currentNamedObject.SourceType == SourceType.FlatRedBallType &&
                    currentNamedObject.SourceClassType == "PositionedObjectList<T>" &&
                    !string.IsNullOrEmpty(currentNamedObject.SourceClassGenericType) &&
                    !currentNamedObject.SetByDerived)
                {
                    menu.Items.Add(form.addObjectToolStripMenuItem);
                }

            }

            #endregion

            #region IsReferencedFileNode
            else if (targetNode.IsReferencedFile())
            {
                menu.Items.Add(form.viewInExplorerToolStripMenuItem);
                menu.Items.Add(mFindAllReferences);
                menu.Items.Add("Copy path to clipboard", null, HandleCopyToClipboardClick);
                menu.Items.Add("-");

                menu.Items.Add(mCreateZipPackage);
                menu.Items.Add(mRecreateCompanionFiles);

                menu.Items.Add("-");

                AddRemoveFromProjectItems(form, menu);

                menu.Items.Add(mUseContentPipeline);
                //menu.Items.Add(form.openWithDEFAULTToolStripMenuItem);

                ReferencedFileSave rfs = (ReferencedFileSave)targetNode.Tag;

                if (FileManager.GetExtension(rfs.Name) == "csv" || rfs.TreatAsCsv)
                {
                    menu.Items.Add("-");
                    menu.Items.Add(form.setCreatedClassToolStripMenuItem);
                    menu.Items.Add(reGenerateCodeToolStripMenuItem);
                }


                if (!string.IsNullOrEmpty(rfs.SourceFile) || rfs.SourceFileCache.Count != 0)
                {
                    menu.Items.Add("-");
                    menu.Items.Add(mViewSourceInExplorer);
                    menu.Items.Add(mRebuildFile);
                }

                menu.Items.Add(mCopyToBuildFolder);

                if (!File.Exists(ProjectManager.MakeAbsolute(rfs.Name, true)))
                {
                    menu.Items.Add(mCreateNewFileForMissingFile);
                }
            }

            #endregion

            #region IsCustomVariable
            else if (targetNode.IsCustomVariable())
            {
                AddRemoveFromProjectItems(form, menu);

                menu.Items.Add("-");


                menu.Items.Add(mFindAllReferences);

                menu.Items.Add("-");
                    
                menu.Items.Add(mMoveToTop);
                menu.Items.Add(mMoveUp);
                menu.Items.Add(mMoveDown);
                menu.Items.Add(mMoveToBottom);
            }

            #endregion

            #region IsCodeNode
            else if (targetNode.IsCodeNode())
            {

                menu.Items.Add(form.viewInExplorerToolStripMenuItem);
                menu.Items.Add(reGenerateCodeToolStripMenuItem);
            }

            #endregion

            #region IsRootCodeNode

            else if (targetNode.IsRootCodeNode())
            {
                menu.Items.Add(reGenerateCodeToolStripMenuItem);
            }


            #endregion

            #region IsDirectoryNode
            else if (targetNode.IsDirectoryNode())
            {
                //menu.Items.Add(form.viewInExplorerToolStripMenuItem);
                menu.Items.Add(mViewContentFilesInExplorer);
                menu.Items.Add(mViewCodeFolderInExplorer);
                menu.Items.Add("-");


                menu.Items.Add(form.addFolderToolStripMenuItem);

                bool isEntityContainingFolder = targetNode.Root().IsRootEntityNode();

                if (isEntityContainingFolder)
                {
                    menu.Items.Add(form.addEntityToolStripMenuItem);

                    mImportElement.Text = "Import Entity";
                    menu.Items.Add(mImportElement);
                }
                else
                {
                    // If not in the Entities tree structure, assume global content
                    menu.Items.Add(form.addFileToolStripMenuItem);
                }

                menu.Items.Add("-");

                menu.Items.Add(mDeleteFolder);
                if(isEntityContainingFolder)
                {
                    menu.Items.Add(mRenameFolder);
                }
            }

            #endregion

            #region IsStateListNode

            else if (targetNode.IsStateListNode())
            {
                menu.Items.Add(mAddState);
                menu.Items.Add(mAddStateCategory);
            }

            #endregion

            #region IsStateCategoryNode
            else if (targetNode.IsStateCategoryNode())
            {
                menu.Items.Add(mAddState);
                AddRemoveFromProjectItems(form, menu);

            }
            #endregion

            #region IsStateNode

            else if (targetNode.IsStateNode())
            {
                AddRemoveFromProjectItems(form, menu);

                menu.Items.Add("-");
                menu.Items.Add(mDuplicate);
                menu.Items.Add("-");
                menu.Items.Add(mFillValuesFromDefault);
            }

            #endregion

            #region IsEventTreeNode

            else if (targetNode.IsEventResponseTreeNode())
            {
                AddRemoveFromProjectItems(form, menu);

            }

            #endregion
            PluginManager.ReactToTreeViewRightClick(targetNode, menu);
        }


        



        public static void Initialize()
        {
            mMoveToTop = new ToolStripMenuItem("^^ Move To Top");
            mMoveToTop.ShortcutKeyDisplayString = "Alt+Shift+Up";
            mMoveToTop.Click += new System.EventHandler(MoveToTopClick);

            mMoveUp = new ToolStripMenuItem("^ Move Up");
            mMoveUp.ShortcutKeyDisplayString = "Alt+Up";
            mMoveUp.Click += new System.EventHandler(MoveUpClick);

            mMoveDown = new ToolStripMenuItem("v Move Down");
            mMoveDown.ShortcutKeyDisplayString = "Alt+Down";
            mMoveDown.Click += new System.EventHandler(MoveDownClick);

            mMoveToBottom = new ToolStripMenuItem("vv Move To Bottom");
            mMoveToBottom.ShortcutKeyDisplayString = "Alt+Shift+Down";
            mMoveToBottom.Click += new System.EventHandler(MoveToBottomClick);

            mMakeRequiredAtStartup = new ToolStripMenuItem("Make Required at StartUp");
            mMakeRequiredAtStartup.Click += new EventHandler(MakeRequiredAtStartupClick);

            mRebuildFile = new ToolStripMenuItem("Rebuild File");
            mRebuildFile.Click += new EventHandler(RebuildFileClick);

            mViewSourceInExplorer = new ToolStripMenuItem("View source file in explorer");
            mViewSourceInExplorer.Click += new EventHandler(ViewSourceInExplorerClick);

            mRecreateCompanionFiles = new ToolStripMenuItem("Re-create companion files");
            mRecreateCompanionFiles.Click += new EventHandler(RecreateCompanionFilesClick);

            mFindAllReferences = new ToolStripMenuItem("Find all references to this");
            mFindAllReferences.Click += new EventHandler(FindAllReferencesClick);

            mViewCodeFolderInExplorer = new ToolStripMenuItem("View code folder");
            mViewCodeFolderInExplorer.Click += new EventHandler(ViewCodeFolderInExplorerClick);

            mViewContentFilesInExplorer = new ToolStripMenuItem("View content folder");
            mViewContentFilesInExplorer.Click += new EventHandler(ViewContentFolderInExplorer);

            mDeleteFolder = new ToolStripMenuItem("Delete Folder");
            mDeleteFolder.Click += new EventHandler(DeleteFolderClick);

            mRenameFolder = new ToolStripMenuItem("Rename Folder");
            mRenameFolder.Click += HandleRenameFolderClick;

            mDuplicate = new ToolStripMenuItem("Duplicate");
            mDuplicate.Click += new EventHandler(DuplicateClick);

            mAddState = new ToolStripMenuItem("Add State");
            mAddState.Click += new EventHandler(AddStateClick);

            mAddStateCategory = new ToolStripMenuItem("Add State Category");
            mAddStateCategory.Click += new EventHandler(AddStateCategoryClick);

            mAddResetVariablesForPooling = new ToolStripMenuItem("Add Reset Variables For Pooling");
            mAddResetVariablesForPooling.Click += new EventHandler(mAddResetVariablesForPooling_Click);

            mFillValuesFromDefault = new ToolStripMenuItem("Fill Values From Variables");
            mFillValuesFromDefault.Click += new EventHandler(mFillValuesFromVariables_Click);

            mRemoveFromProjectQuick = new ToolStripMenuItem("Remove from project quick (ONLY IF YOU KNOW WHAT YOU'RE DOING!)");
            mRemoveFromProjectQuick.Click += new EventHandler(RemoveFromProjectQuick);

            mUseContentPipeline = new ToolStripMenuItem("Toggle Use Content Pipeline");
            mUseContentPipeline.Click += new EventHandler(mUseContentPipeline_Click);

            mCreateNewFileForMissingFile = new ToolStripMenuItem("Create new file for missing file");
            mCreateNewFileForMissingFile.Click += new EventHandler(CreateNewFileForMissingFileClick);

            mViewFileLoadOrder = new ToolStripMenuItem("View File Order");
            mViewFileLoadOrder.Click += new EventHandler(ViewFileOrderClick);

            mCreateZipPackage = new ToolStripMenuItem("Create Zip Package");
            mCreateZipPackage.Click += new EventHandler(CreateZipPackageClick);

            mExportElement = new ToolStripMenuItem("Export Screen");
            mExportElement.Click += new EventHandler(ExportElementClick);

            mImportElement = new ToolStripMenuItem("Import Screen");
            mImportElement.Click += new EventHandler(ImportElementClick);

            mAddEventMenuItem = new ToolStripMenuItem("Add Event");
            mAddEventMenuItem.Click += new EventHandler(AddEventClicked);

            mRefreshTreeNodesMenuItem = new ToolStripMenuItem("Refresh UI");
            mRefreshTreeNodesMenuItem.Click += new EventHandler(OnRefreshTreeNodesClick);

            mAddEntityInstance = new ToolStripMenuItem("Add Entity Instance");
            mAddEntityInstance.Click += new EventHandler(OnAddEntityInstanceClick);

            mAddEntityList = new ToolStripMenuItem("Add Entity List");
            mAddEntityList.Click += new EventHandler(OnAddEntityListClick);

            mCopyToBuildFolder = new ToolStripMenuItem("Copy to build folder");
            mCopyToBuildFolder.Click += HandleCopyToBuildFolder;

            reGenerateCodeToolStripMenuItem = new ToolStripMenuItem("Re-Generate Code");
            reGenerateCodeToolStripMenuItem.Click += HandleReGenerateCodeClick; ;

        }

        private static void HandleReGenerateCodeClick(object sender, EventArgs e)
        {
            ReGenerateCodeForSelectedElement();
        }

        private static void HandleCopyToBuildFolder(object sender, EventArgs e)
        {

            if (GlueState.Self.CurrentReferencedFileSave != null)
            {
                GlueCommands.Self.ProjectCommands.CopyToBuildFolder(GlueState.Self.CurrentReferencedFileSave);
            }
        }

        static void HandleCopyToClipboardClick(object sender, EventArgs e)
        {
            if (GlueState.Self.CurrentReferencedFileSave != null)
            {
                string absolute = ProjectManager.MakeAbsolute(GlueState.Self.CurrentReferencedFileSave.Name, true);
                absolute = FileManager.GetDirectory(absolute);
                Clipboard.SetText(absolute);
            }
        }

        static void OnAddEntityListClick(object sender, EventArgs e)
        {
            ElementViewWindow.CreateNewNamedObjectInElement(
                GlueState.Self.CurrentElement,
                (EntitySave)ElementViewWindow.TreeNodeDraggedOff.Tag,
                true);

            ProjectManager.SaveProjects();
            GlueCommands.Self.GluxCommands.SaveGlux();

        }

        static void OnAddEntityInstanceClick(object sender, EventArgs e)
        {
            ElementViewWindow.DragDropTreeNode(
                MainGlueWindow.Self.ElementTreeView,
                MainGlueWindow.Self.ElementTreeView.SelectedNode);


            ProjectManager.SaveProjects();
            GlueCommands.Self.GluxCommands.SaveGlux();
        }

        static void OnRefreshTreeNodesClick(object sender, EventArgs e)
        {
            GlueCommands.Self.RefreshCommands.RefreshUiForSelectedElement();
        }

        public static void ShowAddEventWindow(NamedObjectSave objectToTunnelInto)
        {
            AddEventWindow addEventWindow = new AddEventWindow();
            addEventWindow.DesiredEventType = CustomEventType.Tunneled;

            addEventWindow.TunnelingObject = objectToTunnelInto.InstanceName;;

            if (addEventWindow.ShowDialog(MainGlueWindow.Self) == DialogResult.OK)
            {
                HandleAddEventOk(addEventWindow);
            }

        }

        static void AddEventClicked(object sender, EventArgs e)
        {
            // add event, new event, add new event
            AddEventWindow addEventWindow = new AddEventWindow();

            if (addEventWindow.ShowDialog(MainGlueWindow.Self) == DialogResult.OK)
            {
                HandleAddEventOk(addEventWindow);
            }
        }

        private static void HandleAddEventOk(AddEventWindow addEventWindow)
        {
            string resultName = addEventWindow.ResultName;
            IElement currentElement = EditorLogic.CurrentElement;

            #region Show message boxes if there is an error with the variable
            bool isInvalid = IsVariableInvalid(null, resultName, currentElement);

            #endregion

            if (!isInvalid)
            {
                EventResponseSave eventResponseSave = new EventResponseSave();
                eventResponseSave.EventName = resultName;

                eventResponseSave.SourceObject = addEventWindow.TunnelingObject;
                eventResponseSave.SourceObjectEvent = addEventWindow.TunnelingEvent;

                eventResponseSave.SourceVariable = addEventWindow.SourceVariable;
                eventResponseSave.BeforeOrAfter = addEventWindow.BeforeOrAfter;

                eventResponseSave.DelegateType = addEventWindow.ResultDelegateType;

                AddEventToElementAndSave(currentElement, eventResponseSave);
            }
        }

        public static void AddEventToElementAndSave(IElement currentElement, EventResponseSave eventResponseSave)
        {
            currentElement.Events.Add(eventResponseSave);

            string fullGeneratedFileName = ProjectManager.ProjectBase.Directory + EventManager.GetGeneratedEventFileNameForElement(currentElement);

            if (!File.Exists(fullGeneratedFileName))
            {
                CodeWriter.AddEventGeneratedCodeFileForElement(currentElement);
            }

            ElementViewWindow.GenerateSelectedElementCode();

            EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();

            GluxCommands.Self.SaveGlux();

            EditorLogic.CurrentEventResponseSave = eventResponseSave;
        }

        static void ViewFileOrderClick(object sender, EventArgs e)
        {
            // view file order, viewfileorder, view files, viewfiles, viewfilelist, view file list
            ReferencedFileFlatListWindow rfflw = new ReferencedFileFlatListWindow();
            rfflw.Show(MainGlueWindow.Self);
            if(GlueState.Self.CurrentGlueProject != null)
            {
                rfflw.PopulateFrom(ProjectManager.GlueProjectSave.GlobalFiles);
            }
        }


        private static void AddRemoveFromProjectItems(MainGlueWindow form, ContextMenuStrip menu)
        {
            menu.Items.Add(form.removeFromProjectToolStripMenuItem);

            if (GlueState.Self.CurrentReferencedFileSave != null ||
                GlueState.Self.CurrentNamedObjectSave != null ||
                GlueState.Self.CurrentEventResponseSave != null ||
                GlueState.Self.CurrentCustomVariable != null ||
                GlueState.Self.CurrentStateSave != null ||
                GlueState.Self.CurrentStateSaveCategory != null)
            {
                if (GlueState.Self.CurrentScreenSave != null)
                {
                    form.removeFromProjectToolStripMenuItem.Text = "Remove from Screen";
                }
                else if (GlueState.Self.CurrentEntitySave != null)
                {
                    form.removeFromProjectToolStripMenuItem.Text = "Remove from Entity";
                }
                else
                {
                    form.removeFromProjectToolStripMenuItem.Text = "Remove from Global Content";
                }
            }
            else
            {
                form.removeFromProjectToolStripMenuItem.Text = "Remove item";
            }
            if ((Control.ModifierKeys & Keys.Shift) != 0)
            {
                menu.Items.Add(mRemoveFromProjectQuick);
            }
        }

        static void mFillValuesFromVariables_Click(object sender, EventArgs e)
        {
            StateSave stateSave = EditorLogic.CurrentStateSave;
            IElement element = EditorLogic.CurrentElement;

            DialogResult result = MessageBox.Show(
                "Are you sure you want to fill all values in the " +
                stateSave.Name +
                " State from the default variable values?  All previous values will be lost", "Fill values from default?",
                MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                for (int i = 0; i < element.CustomVariables.Count; i++)
                {
                    CustomVariable cv = element.CustomVariables[i];

                    stateSave.SetValue(cv.Name, cv.DefaultValue);
                }

                MainGlueWindow.Self.PropertyGrid.Refresh();

                ElementViewWindow.GenerateSelectedElementCode();

                GluxCommands.Self.SaveGlux();
            }
        }

        static void mUseContentPipeline_Click(object sender, EventArgs e)
        {
            ReferencedFileSave rfs = EditorLogic.CurrentReferencedFile;

        }
        
        static void AddStateClick(object sender, EventArgs e)
        {
            // search: addstate, add new state, addnewstate, add state
            TextInputWindow tiw = new TextInputWindow();
            tiw.DisplayText = "Enter a name for the new state";
            tiw.Text = "New State";


            DialogResult result = tiw.ShowDialog(MainGlueWindow.Self);

            if (result == DialogResult.OK)
            {
                string whyItIsntValid;
                if (!NameVerifier.IsStateNameValid(tiw.Result, EditorLogic.CurrentElement, EditorLogic.CurrentStateSaveCategory, EditorLogic.CurrentStateSave, out whyItIsntValid))
                {
                    GlueGui.ShowMessageBox(whyItIsntValid);
                }
                else
                {

                    StateSave newState = new StateSave();
                    newState.Name = tiw.Result;

                    if (EditorLogic.CurrentStateSaveCategory != null)
                    {
                        EditorLogic.CurrentStateSaveCategory.States.Add(newState);
                    }
                    else
                    {
                        IElement element = EditorLogic.CurrentElement;

                        element.States.Add(newState);
                    }

                    EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();
                    ElementViewWindow.GenerateSelectedElementCode();

                    GlueCommands.Self.TreeNodeCommands.SelectTreeNode(newState);

                    GluxCommands.Self.SaveGlux();
                    ProjectManager.SaveProjects();
                }
            }
        }

        static void AddStateCategoryClick(object sender, EventArgs e)
        {
            // add category, addcategory, add state category
            TextInputWindow tiw = new TextInputWindow();
            tiw.DisplayText = "Enter a name for the new category";
            tiw.Text = "New Category";

            DialogResult result = tiw.ShowDialog(MainGlueWindow.Self);

            if (result == DialogResult.OK)
            {
                string whyItIsntValid;

                if(!NameVerifier.IsStateCategoryNameValid(tiw.Result, out whyItIsntValid))
                {
                    GlueGui.ShowMessageBox(whyItIsntValid);
                }
                else
                {
                    IElement element = EditorLogic.CurrentElement;

                    StateSaveCategory newCategory = new StateSaveCategory();
                    newCategory.Name = tiw.Result;
                    newCategory.SharesVariablesWithOtherCategories = false;

                    element.StateCategoryList.Add(newCategory);

                    EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();
                    ElementViewWindow.GenerateSelectedElementCode();

                    GluxCommands.Self.SaveGlux();
                    ProjectManager.SaveProjects();
                }
            }
        }

        static void DuplicateClick(object sender, EventArgs e)
        {
            if (EditorLogic.CurrentNamedObject != null)
            {
                DuplicateCurrentNamedObject();
            }
            else if (EditorLogic.CurrentStateSave != null)
            {
                DuplicateCurrentStateSave();
            }
        }

        private static void DuplicateCurrentNamedObject()
        {
            // Duplicate duplicate named object, copy named object, copy object
            NamedObjectSave namedObjectToDuplicate = EditorLogic.CurrentNamedObject;

            NamedObjectSave newNamedObject = namedObjectToDuplicate.Clone();

            #region Update the instance name

            newNamedObject.InstanceName = StringFunctions.IncrementNumberAtEnd(newNamedObject.InstanceName);
            if (newNamedObject.InstanceName.EndsWith("1") && StringFunctions.GetNumberAtEnd(newNamedObject.InstanceName) == 1)
            {
                newNamedObject.InstanceName = StringFunctions.IncrementNumberAtEnd(newNamedObject.InstanceName);
            }

            #endregion

            TreeNode treeNodeForNamedObject = GlueState.Self.Find.NamedObjectTreeNode(EditorLogic.CurrentNamedObject);
            TreeNode parentTreeNode = treeNodeForNamedObject.Parent;

            #region Get the container

            IElement container = null;

            if (parentTreeNode.IsRootNamedObjectNode() && parentTreeNode.Parent.IsEntityNode())
            {
                container = ((EntityTreeNode)parentTreeNode.Parent).EntitySave;
            }
            else if (parentTreeNode.IsRootNamedObjectNode() && parentTreeNode.Parent.IsScreenNode())
            {
                container = ((ScreenTreeNode)parentTreeNode.Parent).ScreenSave;
            }
            else if (parentTreeNode.IsNamedObjectNode())
            {
                // handled below
            }
            #endregion


            if (container != null)
            {
                int indexToInsertAt = 1 + container.NamedObjects.IndexOf(EditorLogic.CurrentNamedObject);

                while (container.GetNamedObjectRecursively(newNamedObject.InstanceName) != null)
                {
                    newNamedObject.InstanceName = StringFunctions.IncrementNumberAtEnd(newNamedObject.InstanceName);
                }

                container.NamedObjects.Insert(indexToInsertAt, newNamedObject);
            }
            else
            {
                NamedObjectSave list = parentTreeNode.Tag as NamedObjectSave;

                if (list != null && list.IsList)
                {
                    int indexToInsertAt = 1 + list.ContainedObjects.IndexOf(EditorLogic.CurrentNamedObject);

                    container = EditorLogic.CurrentElement;

                    while (container.GetNamedObjectRecursively(newNamedObject.InstanceName) != null)
                    {
                        newNamedObject.InstanceName = StringFunctions.IncrementNumberAtEnd(newNamedObject.InstanceName);
                    }

                    list.ContainedObjects.Insert(indexToInsertAt, newNamedObject);

                }

            }


            ElementViewWindow.UpdateCurrentObjectReferencedTreeNodes();

            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

            ProjectManager.SaveProjects();
            GluxCommands.Self.SaveGlux();
        }

        private static void DuplicateCurrentStateSave()
        {
            StateSave stateSave = EditorLogic.CurrentStateSave;

            StateSave newStateSave = stateSave.Clone();

            // Update the new statesave name
            newStateSave.Name = StringFunctions.IncrementNumberAtEnd(newStateSave.Name);

            if (newStateSave.Name.EndsWith("1") && StringFunctions.GetNumberAtEnd(newStateSave.Name) == 1)
            {
                newStateSave.Name = StringFunctions.IncrementNumberAtEnd(newStateSave.Name);
            }

            IElement container = ObjectFinder.Self.GetElementContaining(stateSave);

            // Gotta insert this thing either in the states or category
            if (container.States.Contains(stateSave))
            {
                int indexToInsertAt = container.States.IndexOf(stateSave) + 1;

                container.States.Insert(indexToInsertAt, newStateSave);
            }
            else
            {
                foreach (StateSaveCategory ssc in container.StateCategoryList)
                {
                    if (ssc.States.Contains(stateSave))
                    {
                        int indexToInsertAt = ssc.States.IndexOf(stateSave) + 1;
                        ssc.States.Insert(indexToInsertAt, newStateSave);
                        break;
                    }
                }
            }

            ElementViewWindow.UpdateCurrentObjectReferencedTreeNodes();
            CodeWriter.GenerateCode(EditorLogic.CurrentElement);
            ProjectManager.SaveProjects();
            GluxCommands.Self.SaveGlux();

        }

        static void FindAllReferencesClick(object sender, EventArgs e)
        {


            // find all references, findallreferences, find references
            ElementReferenceListWindow erlw = new ElementReferenceListWindow();
            erlw.Show();
            if (EditorLogic.CurrentReferencedFile != null)
            {
                erlw.PopulateWithReferencesTo(EditorLogic.CurrentReferencedFile);
            }
            else if (EditorLogic.CurrentNamedObject != null)
            {
                erlw.PopulateWithReferencesTo(EditorLogic.CurrentNamedObject, EditorLogic.CurrentElement);
            }
            else if (EditorLogic.CurrentCustomVariable != null)
            {
                erlw.PopulateWithReferencesTo(EditorLogic.CurrentCustomVariable, EditorLogic.CurrentElement);
            }
            else
            {
                erlw.PopulateWithReferencesToElement(EditorLogic.CurrentElement);
            }
        }

        static void RecreateCompanionFilesClick(object sender, EventArgs e)
        {
            ReferencedFileSave rfs = EditorLogic.CurrentReferencedFile;

            if (EditorLogic.CurrentReferencedFile != null)
            {
                AssetTypeInfoExtensionMethodsGlue.CreateCompanionSettingsFile(
                    ProjectManager.MakeAbsolute(rfs.Name, true), false);

            }
        }

        static void mAddResetVariablesForPooling_Click(object sender, EventArgs e)
        {
            FactoryManager.Self.AddResetVariablesForPooling_Click();
        }

        //private static void PopulateOpenWithMenuItem()
        //{
        //    string extension = FileManager.GetExtension(
        //        EditorLogic.CurrentReferencedFile.Name);

        //    switch (extension)
        //    {
        //        case "scnx":

        //            openWithDEFAULTToolStripMenuItem.DropDownItems.Add(
        //                new ToolStripMenuItem("SpriteEditor", null, OpenWithSpriteEditor));
        //            openWithDEFAULTToolStripMenuItem.DropDownItems.Add(
        //                new ToolStripMenuItem("TileEditor", null, OpenWithTileEditor));

        //            break;

        //    }
        //}

        internal static void RemoveFromProjectToolStripMenuItem()
        {
            bool saveAndRegenerate = true;

            RemoveFromProjectOptionalSaveAndRegenerate(saveAndRegenerate, true, true);
        }

        private static void RemoveFromProjectQuick(object sender, EventArgs e)
        {
            RemoveFromProjectOptionalSaveAndRegenerate(false, true, true);
        }

        private static void RemoveFromProjectOptionalSaveAndRegenerate(bool saveAndRegenerate, bool askAreYouSure, bool askToDelete)
        {
            // delete object, remove object, DeleteObject, RemoveObject, remove from project, 
            // remove from screen, remove from entity, remove file
            ///////////////////////////////EARLY OUT///////////////////////////////////////
            // This can now be called by pushing Delete, so we should check if deleting is valid
            if (EditorLogic.CurrentTreeNode == null || EditorLogic.CurrentTreeNode.Parent == null ||
                EditorLogic.CurrentTreeNode.Text.EndsWith(".cs") || EditorLogic.CurrentTreeNode.Tag == null)
            {
                return;
            }
            //////////////////////////////END EARLY OUT/////////////////////////////////////

            lock (FileWatchManager.LockObject)
            {
                // Search terms: removefromproject, remove from project, remove file, remove referencedfilesave
                List<string> filesToRemove = new List<string>();

                if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
                {
                    #region Find out if the user really wants to remove this - don't ask if askAreYouSure is false
                    DialogResult reallyRemoveResult = DialogResult.Yes;

                    if (askAreYouSure)
                    {
                        string message = "Are you sure you want to remove this:\n\n" + EditorLogic.CurrentTreeNode.Tag.ToString();

                        reallyRemoveResult =
                            MessageBox.Show(message, "Remove?", MessageBoxButtons.YesNo);
                    }
                    #endregion

                    if (reallyRemoveResult == DialogResult.Yes)
                    {
                        #region If is NamedObjectSave
                        // test deep first
                        if (EditorLogic.CurrentNamedObject != null)
                        {
                            ProjectManager.RemoveNamedObject(EditorLogic.CurrentNamedObject, true, true, filesToRemove);
                            //ProjectManager.RemoveNamedObject(EditorLogic.CurrentNamedObject);
                        }
                        #endregion

                        #region Else if is StateSave
                        else if (EditorLogic.CurrentStateSave != null)
                        {
                            var name = EditorLogic.CurrentStateSave.Name;

                            EditorLogic.CurrentElement.RemoveState(EditorLogic.CurrentStateSave);

                            AskToRemoveCustomVariablesWithoutState(EditorLogic.CurrentElement);

                            EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();

                            PluginManager.ReactToStateRemoved(EditorLogic.CurrentElement, name);

                            


                            GluxCommands.Self.SaveGlux();
                        }

                        #endregion

                        #region Else if is StateSaveCategory

                        else if (EditorLogic.CurrentStateSaveCategory != null)
                        {
                            EditorLogic.CurrentElement.StateCategoryList.Remove(EditorLogic.CurrentStateSaveCategory);

                            EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();

                            GluxCommands.Self.SaveGlux();
                        }

                        #endregion

                        #region Else if is ReferencedFileSave

                        else if (EditorLogic.CurrentReferencedFile != null)
                        {
                            // the GluxCommand handles saving and regenerate internally, no need to do it twice
                            saveAndRegenerate = false;
                            var toRemove = EditorLogic.CurrentReferencedFile;

                            if(GlueState.Self.Find.IfReferencedFileSaveIsReferenced(toRemove))
                            {
                                IElement element = GlueState.Self.CurrentElement;

                                // this could happen at the same time as file flushing, which can cause locks.  Therefore we need to add this as a task:
                                TaskManager.Self.AddSync(() =>
                                {
                                    GluxCommands.Self.RemoveReferencedFile(toRemove, filesToRemove, regenerateCode:true);
                                    PluginManager.ReactToFileRemoved(element, toRemove);

                                },
                                "Remove file " + toRemove.ToString());

                            }
                            
                        }
                        #endregion

                        #region Else if is CustomVariable

                        else if (EditorLogic.CurrentCustomVariable != null)
                        {
                            ProjectManager.RemoveCustomVariable(EditorLogic.CurrentCustomVariable, filesToRemove);
                            //ProjectManager.RemoveCustomVariable(EditorLogic.CurrentCustomVariable);
                        }

                        #endregion

                        #region Else if is EventSave
                        else if (EditorLogic.CurrentEventResponseSave != null)
                        {
                            var element = EditorLogic.CurrentElement;
                            var eventResponse = EditorLogic.CurrentEventResponseSave;
                            EditorLogic.CurrentElement.Events.Remove(eventResponse);
                            PluginManager.ReactToEventResponseRemoved(element, eventResponse);
                            GlueCommands.Self.RefreshCommands.RefreshUiForSelectedElement();
                        }
                        #endregion

                        #region Else if is ScreenSave

                        // Then test higher if deep didn't get removed
                        else if (EditorLogic.CurrentScreenSave != null)
                        {
                            var screenToRemove = EditorLogic.CurrentScreenSave;
                            TaskManager.Self.AddSync(() =>
                            {
                                RemoveScreen(screenToRemove, filesToRemove);
                            },
                            "Remove screen");
                        }

                        #endregion

                        #region Else if is EntitySave

                        else if (EditorLogic.CurrentEntitySave != null)
                        {
                            RemoveEntity(EditorLogic.CurrentEntitySave, filesToRemove);
                            //ProjectManager.RemoveEntity(EditorLogic.CurrentEntitySave);
                        }

                        #endregion


                        #region Files were deleted and the user wants to be asked to delete

                        if (filesToRemove.Count != 0 && askToDelete)
                        {

                            for (int i = 0; i < filesToRemove.Count; i++)
                            {
                                if (FileManager.IsRelative(filesToRemove[i]))
                                {
                                    filesToRemove[i] = ProjectManager.MakeAbsolute(filesToRemove[i]);
                                }
                                filesToRemove[i] = filesToRemove[i].Replace("\\", "/");
                            }

                            StringFunctions.RemoveDuplicates(filesToRemove, true);

                            ListBoxWindow lbw = new ListBoxWindow();
                            
                            string messageString = "What would you like to do with the following files:\n";
                            lbw.Message = messageString;

                            foreach (string s in filesToRemove)
                            {
                                
                                lbw.AddItem(s);
                            }
                            lbw.ClearButtons();
                            lbw.AddButton("Nothing - leave them as part of the game project", DialogResult.No);
                            lbw.AddButton("Remove them from the project but keep the files", DialogResult.OK);
                            lbw.AddButton("Remove and delete the files", DialogResult.Yes);


                            DialogResult result = lbw.ShowDialog();

                            if (result == DialogResult.OK || result == DialogResult.Yes)
                            {
                                foreach (string file in filesToRemove)
                                {
                                    string fileName = ProjectManager.MakeAbsolute(file);
                                    // This file may have been removed
                                    // in windows explorer, and now removed
                                    // from Glue.  Check to prevent a crash.

                                    ProjectManager.RemoveItemFromAllProjects(fileName, false);
                                }
                            }

                            if (result == DialogResult.Yes)
                            {
                                foreach (string file in filesToRemove)
                                {
                                    string fileName = ProjectManager.MakeAbsolute(file);
                                    // This file may have been removed
                                    // in windows explorer, and now removed
                                    // from Glue.  Check to prevent a crash.
                                    if (File.Exists(fileName))
                                    {
                                        FileHelper.DeleteFile(fileName);
                                    }
                                }
                            }
                        }

                        #endregion

                        // Nodes aren't directly removed in the code above. Instead, 
                        // a "refresh nodes" method is called, which may remove unneeded
                        // nodes, but event raising is suppressed. Therefore, we have to explicitly 
                        // do it here:
                        PluginManager.ReactToItemSelect(GlueState.Self.CurrentTreeNode);


                        if (saveAndRegenerate)
                        {

                            Action regenerateAction = null;

                            if (EditorLogic.CurrentScreenTreeNode != null)
                            {
                                var screen = EditorLogic.CurrentScreenSave;
                                regenerateAction = () =>
                                    FlatRedBall.Glue.CodeGeneration.CodeGeneratorIElement.GenerateElementAndDerivedCode(screen);
                            }
                            else if (EditorLogic.CurrentEntityTreeNode != null)
                            {
                                var entity = EditorLogic.CurrentEntitySave;
                                regenerateAction = () =>
                                    FlatRedBall.Glue.CodeGeneration.CodeGeneratorIElement.GenerateElementAndDerivedCode(entity);
                            }
                            else if (EditorLogic.CurrentReferencedFile != null)
                            {
                                regenerateAction = GlobalContentCodeGenerator.UpdateLoadGlobalContentCode;

                                // Vic asks - do we have to do anything else here?  I don't think so...
                            }



                            TaskManager.Self.AddSync(() =>
                                {
                                    if (regenerateAction != null)
                                    {
                                        regenerateAction();
                                    }
                                    ProjectManager.SaveProjects();
                                    GluxCommands.Self.SaveGlux();
                                },
                                "Save and regenerate after removal");
                            
                        }
                    }
                }
            }
        }

        private static void RemoveEntity(EntitySave entityToRemove, List<string> filesThatCouldBeRemoved)
        {


            List<NamedObjectSave> namedObjectsToRemove = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(entityToRemove.Name);



            DialogResult result = DialogResult.Yes;

            string message = null;

            if (namedObjectsToRemove.Count != 0)
            {
                message = "The Entity " + entityToRemove.ToString() + " is referenced by the following objects:";

                for (int i = 0; i < namedObjectsToRemove.Count; i++)
                {
                    message += "\n" + namedObjectsToRemove[i].ToString();

                }


            }

            List<EntitySave> inheritingEntities = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(entityToRemove);

            if (inheritingEntities.Count != 0)
            {
                message = "The Entity " + entityToRemove.ToString() + " is the base for the following Entities:";
                for (int i = 0; i < inheritingEntities.Count; i++)
                {
                    message += "\n" + inheritingEntities[i].ToString();

                }
            }

            if (message != null)
            {
                message += "\n\nDo you really want to remove this Entity?";

                result = MessageBox.Show(message, "Are you sure?", MessageBoxButtons.YesNo);
            }

            if (result == DialogResult.Yes)
            {
                for (int i = entityToRemove.NamedObjects.Count - 1; i > -1; i--)
                {
                    NamedObjectSave nos = entityToRemove.NamedObjects[i];



                    ProjectManager.RemoveNamedObject(nos, false, false, null);
                }


                if (entityToRemove.CreatedByOtherEntities == true)
                {
                    FactoryCodeGenerator.RemoveFactory(entityToRemove);
                }

                // We used to rely on RemoveUnreferencedFiles to do the removal of all RFS's 
                // However, RemoveUnreferencedFiles looks for the file's container to remove it,
                // and by this point the entityToRemove has already been removed from the project.
                // So we'll manually remove the RFS's first before removing the entire entity
                for (int i = entityToRemove.ReferencedFiles.Count - 1; i > -1; i--)
                {
                    GluxCommands.Self.RemoveReferencedFile(entityToRemove.ReferencedFiles[i], filesThatCouldBeRemoved);
                }

                    
                ProjectManager.GlueProjectSave.Entities.Remove(entityToRemove);


                RemoveUnreferencedFiles(entityToRemove, filesThatCouldBeRemoved);

                for (int i = 0; i < namedObjectsToRemove.Count; i++)
                {
                    MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                    NamedObjectSave nos = namedObjectsToRemove[i];
                    mbmb.MessageText = "What would you like to do with the object\n\n" + nos.ToString();

                    mbmb.AddButton("Remove this Object", DialogResult.OK);
                    mbmb.AddButton("Keep this Object (the reference will be invalid)", DialogResult.Cancel);

                    DialogResult namedObjectRemovalResult = mbmb.ShowDialog();

                    if (namedObjectRemovalResult == DialogResult.OK)
                    {

                        ProjectManager.RemoveNamedObject(nos, false, true, filesThatCouldBeRemoved);
                    }
                }
                for (int i = 0; i < inheritingEntities.Count; i++)
                {
                    EntitySave inheritingEntity = inheritingEntities[i];

                    DialogResult resetInheritance = MessageBox.Show("Reset the inheritance for " + inheritingEntity.Name + "?",
                        "Reset Inheritance?", MessageBoxButtons.YesNo);

                    if (resetInheritance == DialogResult.Yes)
                    {
                        inheritingEntity.BaseEntity = "";
                        CodeWriter.GenerateCode(inheritingEntity);
                    }
                }

                ElementViewWindow.RemoveEntity(entityToRemove);

                ProjectManager.RemoveCodeFilesForElement(filesThatCouldBeRemoved, entityToRemove);

                ProjectManager.SaveProjects();
                GluxCommands.Self.SaveGlux();
            }
        }


        public static void RemoveScreen(ScreenSave screenToRemove, List<string> filesThatCouldBeRemoved)
        {
            List<ScreenSave> inheritingScreens = ObjectFinder.Self.GetAllScreensThatInheritFrom(screenToRemove);
            string message = null;
            if (inheritingScreens.Count != 0)
            {
                message = "The Screen " + screenToRemove.ToString() + " is the base for the following Screens:";
                for (int i = 0; i < inheritingScreens.Count; i++)
                {
                    message += "\n" + inheritingScreens[i].ToString();
                }
            }
            DialogResult result = DialogResult.Yes;
            if (message != null)
            {
                message += "\n\nDo you really want to remove this Screen?";
                result = MessageBox.Show(message, "Are you sure?", MessageBoxButtons.YesNo);
            }

            if (result == DialogResult.Yes)
            {


                // Remove objects before removing files.  Otherwise Glue will complain if any objects reference the files.
                #region Remove the NamedObjectSaves

                for (int i = screenToRemove.NamedObjects.Count - 1; i > -1; i--)
                {
                    NamedObjectSave nos = screenToRemove.NamedObjects[i];

                    ProjectManager.RemoveNamedObject(nos, false, false, null);
                }

                #endregion


                // remove all the files this references first before removing the Screen itself.
                // For more information see the RemoveEntity function
                for (int i = screenToRemove.ReferencedFiles.Count - 1; i > -1; i--)
                {
                    GluxCommands.Self.RemoveReferencedFile(screenToRemove.ReferencedFiles[i], filesThatCouldBeRemoved);
                }

                ProjectManager.GlueProjectSave.Screens.Remove(screenToRemove);
                // If we're going to remove the Screen, we should remove all referenced objects that it references
                // as well as any ReferencedFiles
                
                RemoveUnreferencedFiles(screenToRemove, filesThatCouldBeRemoved);

                // test this!
                if (screenToRemove.Name == ProjectManager.GlueProjectSave.StartUpScreen)
                {
                    ProjectManager.StartUpScreen = "";
                }

                for (int i = 0; i < inheritingScreens.Count; i++)
                {
                    ScreenSave inheritingScreen = inheritingScreens[i];

                    DialogResult resetInheritance = MessageBox.Show("Reset the inheritance for " + inheritingScreen.Name + "?",
                        "Reset Inheritance?", MessageBoxButtons.YesNo);

                    if (resetInheritance == DialogResult.Yes)
                    {
                        inheritingScreen.BaseScreen = "";

                        CodeWriter.GenerateCode(inheritingScreen);
                    }
                }

                TaskManager.Self.OnUiThread(() =>
                    {
                        ElementViewWindow.RemoveScreen(screenToRemove);
                    });
                IElement element = screenToRemove;

                ProjectManager.RemoveCodeFilesForElement(filesThatCouldBeRemoved, element);


                ProjectManager.SaveProjects();
                GluxCommands.Self.SaveGlux();
            }
        }

        private static void RemoveUnreferencedFiles(IElement element, List<string> filesThatCouldBeRemoved)
        {
            List<string> allReferencedFiles = GlueCommands.Self.FileCommands.GetAllReferencedFileNames();

            for (int i = element.ReferencedFiles.Count - 1; i > -1; i--)
            {
                ReferencedFileSave rfs = element.ReferencedFiles[i];

                bool shouldRemove = true;
                foreach (string file in allReferencedFiles)
                {
                    if (file.ToLowerInvariant() == rfs.Name.ToLowerInvariant())
                    {
                        shouldRemove = false;
                        break;
                    }
                }

                if (shouldRemove)
                {
                    GluxCommands.Self.RemoveReferencedFile(rfs, filesThatCouldBeRemoved);
                }
            }
        }

        private static void AskToRemoveCustomVariablesWithoutState(IElement element)
        {
            for(int i = 0; i < element.CustomVariables.Count; i++)
            {
                CustomVariable variable = element.CustomVariables[i];

                if (CustomVariableHelper.IsStateMissingFor(variable, element))
                {
                    MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                    mbmb.MessageText = "The variable\n" + variable + "\nno longer has any states associated with it.  What would you like to do?";

                    mbmb.AddButton("Remove the variable.", DialogResult.OK);
                    mbmb.AddButton("Nothing (project may not run until this is fixed)", DialogResult.Cancel);

                    if (mbmb.ShowDialog() == DialogResult.OK)
                    {
                        element.CustomVariables.RemoveAt(i);
                        i--;
                    }
                }
            }
        }

        internal static void AddEntityToolStripClick()
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
                    var result = window.ShowDialog();

                    if(result == true)
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

                            if (EditorLogic.CurrentTreeNode.IsDirectoryNode())
                            {
                                directory = EditorLogic.CurrentTreeNode.GetRelativePath();
                                directory = directory.Replace('/', '\\');
                            }
                            CreateEntityAndObjects(window, entityName, directory);

                        }
                    }
                }
            }
        }

        private static void CreateEntityAndObjects(AddEntityWindow window, string entityName, string directory)
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

            bool needsRefreshAndSave = false;

            if (window.PolygonChecked)
            {
                AddObjectViewModel addObjectViewModel = new AddObjectViewModel();
                addObjectViewModel.ObjectName = "PolygonInstance";
                addObjectViewModel.SourceClassType = "Polygon";
                addObjectViewModel.SourceType = SourceType.FlatRedBallType;

                var nos = gluxCommands.AddNewNamedObjectToSelectedElement(addObjectViewModel);
                var instructions = new CustomVariableInNamedObject();
                instructions.Member = "Points";
                var points = new List<Vector2>();
                points.Add(new Vector2(-16, 16));
                points.Add(new Vector2( 16, 16));
                points.Add(new Vector2( 16,-16));
                points.Add(new Vector2(-16,-16));
                points.Add(new Vector2(-16, 16));
                instructions.Value = points;

                nos.InstructionSaves.Add(instructions);

                needsRefreshAndSave = true;
                
                GlueState.Self.CurrentElement = newElement;
            }

            if(window.IVisibleChecked)
            {
                newElement.ImplementsIVisible = true;
                needsRefreshAndSave = true;
            }

            if(window.IClickableChecked)
            {
                newElement.ImplementsIClickable = true;
                needsRefreshAndSave = true;
            }

            if(window.IWindowChecked)
            {
                newElement.ImplementsIWindow = true;
                needsRefreshAndSave = true;
            }

            if(window.ICollidableChecked)
            {
                newElement.ImplementsICollidable = true;
                needsRefreshAndSave = true;
            }

            if(needsRefreshAndSave)
            {
                MainGlueWindow.Self.PropertyGrid.Refresh();
                ElementViewWindow.GenerateSelectedElementCode();
                GluxCommands.Self.SaveGlux();
            }
        }

        internal static void IgnoreDirectoryClick()
        {
            string directoryToIgnore = EditorLogic.CurrentTreeNode.GetRelativePath();

            directoryToIgnore = FileManager.Standardize(directoryToIgnore);

            if (!ProjectManager.GlueProjectSave.IgnoredDirectories.Contains(directoryToIgnore))
            {
                ProjectManager.GlueProjectSave.IgnoredDirectories.Add(directoryToIgnore);
            }




        }

        internal static void AddFolderClick()
        {
            // addfolder, add folder, add new folder, addnewfolder
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new folder name";
            tiw.Text = "New Folder";
            DialogResult result = tiw.ShowDialog(MainGlueWindow.Self);

            if (result == DialogResult.OK)
            {
                string folderName = tiw.Result;

                TreeNode treeNodeToAddTo = EditorLogic.CurrentTreeNode;
                GlueCommands.Self.ProjectCommands.AddDirectory(folderName, treeNodeToAddTo);

                treeNodeToAddTo.Nodes.SortByTextConsideringDirectories();
            }
        }

        internal static void ViewInExplorerClick()
        {

            if(GlueState.Self.CurrentGlueProject == null)
            {
                MessageBox.Show("You must first load or create a Glue project");
            }
            else
            {


                // view in explorer
                string locationToShow = "";

                if (EditorLogic.CurrentReferencedFile != null)
                {
                    ReferencedFileSave rfs = EditorLogic.CurrentReferencedFile;

                    locationToShow = ProjectManager.MakeAbsolute(rfs.Name);

                }
                else if (EditorLogic.CurrentTreeNode.IsDirectoryNode() || EditorLogic.CurrentTreeNode == ElementViewWindow.GlobalContentFileNode)
                {
                    locationToShow = ProjectManager.MakeAbsolute(EditorLogic.CurrentTreeNode.GetRelativePath(), true);
                }
                else if (EditorLogic.CurrentTreeNode.IsFilesContainerNode() || EditorLogic.CurrentTreeNode.IsFolderInFilesContainerNode())
                {
                    string relativePath = EditorLogic.CurrentTreeNode.GetRelativePath();

                    // Victor Chelaru April 11, 2013
                    // RelativePath already includes "Screens/"
                    // So I'm not sure why I was prepending that
                    // here.
                    //if (EditorLogic.CurrentScreenSave != null)
                    //{
                    //    relativePath = "Screens/" + relativePath;
                    //}

                    locationToShow = ProjectManager.MakeAbsolute(relativePath, true);

                    // If the user hasn't put any files in this element, then this directory may not exist.  Therefore,
                    // let's create it.
                    if (!Directory.Exists(locationToShow))
                    {
                        Directory.CreateDirectory(locationToShow);
                    }
                }
                else if (EditorLogic.CurrentTreeNode.Text.EndsWith(".cs"))
                {
                    locationToShow = ProjectManager.MakeAbsolute(EditorLogic.CurrentTreeNode.GetRelativePath(), false);

                }

                string extension = FileManager.GetExtension(locationToShow);
                bool isFile = !string.IsNullOrEmpty(extension);
                if (isFile)
                {
                    if (!File.Exists(locationToShow))
                    {
                        locationToShow = FileManager.GetDirectory(locationToShow);
                    }
                }
                else
                {
                    // the location may not exis if it's something like global content, so let's try the parent
                    if (!Directory.Exists(locationToShow))
                    {
                        locationToShow = FileManager.GetDirectory(locationToShow);
                    }
                }

                //fileToShow = @"d:/Projects";
                // The file might begin with something like c:\.  Make sure it shows "c:\" and not "c:/"
                locationToShow = locationToShow.Replace("/", "\\");

                // Make sure the quites are
                // added after everything else.
                locationToShow = "\"" + locationToShow + "\"";

                Process.Start("explorer.exe", "/select," + locationToShow);
            }



        }

        static void ViewContentFolderInExplorer(object sender, EventArgs e)
        {

            if (EditorLogic.CurrentTreeNode.IsDirectoryNode())
            {
                string locationToShow = ProjectManager.MakeAbsolute(EditorLogic.CurrentTreeNode.GetRelativePath(), true);

                if(System.IO.Directory.Exists(locationToShow))
                {
                    locationToShow = locationToShow.Replace("/", "\\");
                    Process.Start("explorer.exe", "/select," + locationToShow);
                }
                else
                {
                    if(GlueState.Self.CurrentElement != null)
                    {
                        string screenOrEntity = "screen";
                        if(GlueState.Self.CurrentEntitySave != null)
                        {
                            screenOrEntity = "entity";
                        }
                        MessageBox.Show($"This {screenOrEntity} does not have a content folder. It will be created when a file is added to this {screenOrEntity}.");
                    }
                    else
                    {
                        MessageBox.Show($"Glue has not created this content folder yet since it doesn't contain any files.");

                    }
                }
            }
        }

        static void DeleteFolderClick(object sender, EventArgs e)
        {
            // delete folder, deletefolder

            bool forceContent = false;

            if (EditorLogic.CurrentTreeNode.IsChildOfGlobalContent() ||
                EditorLogic.CurrentTreeNode.IsFolderInFilesContainerNode())
            {
                forceContent = true;
            }

            string absolutePath = ProjectManager.MakeAbsolute(EditorLogic.CurrentTreeNode.GetRelativePath(), forceContent);

            string[] files = null;
            string[] directories = null;
            if (Directory.Exists(absolutePath))
            {

                files = Directory.GetFiles(absolutePath);
                directories = Directory.GetDirectories(absolutePath);
            }


            DialogResult shouldDelete = DialogResult.Yes;

            if ((files != null && files.Length != 0) || (directories != null && directories.Length != 0))
            {
                shouldDelete = MessageBox.Show("The directory\n\n" + absolutePath + "\n\nis not empty." +
                    "Are you sure you want to delete it and everything inside of it?", "Are you sure?", MessageBoxButtons.YesNo);
            }

            if (shouldDelete == DialogResult.Yes)
            {
                if (EditorLogic.CurrentTreeNode.IsChildOfRootEntityNode() && EditorLogic.CurrentTreeNode.IsFolderForEntities())
                {
                    // We have to remove all contained Entities
                    // from the project.
                    List<EntitySave> allEntitySaves = new List<EntitySave>();
                    GetAllEntitySavesIn(EditorLogic.CurrentTreeNode, allEntitySaves);

                    foreach (EntitySave entitySave in allEntitySaves)
                    {
                        MainGlueWindow.Self.ElementTreeView.SelectedNode = GlueState.Self.Find.ElementTreeNode(entitySave);
                        RemoveFromProjectOptionalSaveAndRegenerate(entitySave == allEntitySaves[allEntitySaves.Count - 1], false, false);

                    }
                }
                else if (EditorLogic.CurrentTreeNode.IsFolderInFilesContainerNode())
                {
                    List<ReferencedFileSave> allReferencedFileSaves = new List<ReferencedFileSave>();
                    GetAllReferencedFileSavesIn(EditorLogic.CurrentTreeNode, allReferencedFileSaves);

                    foreach (ReferencedFileSave rfs in allReferencedFileSaves)
                    {
                        MainGlueWindow.Self.ElementTreeView.SelectedNode = GlueState.Self.Find.ReferencedFileSaveTreeNode(rfs);
                        // I guess we won't ask to delete here, but maybe eventually we want to?
                        RemoveFromProjectOptionalSaveAndRegenerate(rfs == allReferencedFileSaves[allReferencedFileSaves.Count - 1], false, false);
                    }
                }

                EditorLogic.CurrentTreeNode.Parent.Nodes.Remove(EditorLogic.CurrentTreeNode);
                System.IO.Directory.Delete(absolutePath, true);
                // Do we need to save the project?  For some reason removing mulitple RFS's isn't updating the .csproj
            }
        }

        static void HandleRenameFolderClick(object sender, EventArgs e)
        {
            var treeNode = GlueState.Self.CurrentTreeNode;

            var inputWindow = new TextInputWindow();
            inputWindow.Message = "Enter new folder name";
            inputWindow.Result = treeNode.Text;

            var dialogResult = inputWindow.ShowDialog();

            bool shouldPerformMove = false;

            string directoryRenaming = null;
            string newDirectoryNameRelative = null;
            string newDirectoryNameAbsolute = null;

            if (dialogResult == DialogResult.OK)
            {
                // entities use backslash:
                directoryRenaming = treeNode.GetRelativePath().Replace("/", "\\");
                newDirectoryNameRelative = FileManager.GetDirectory(directoryRenaming, RelativeType.Relative) + inputWindow.Result + "\\";
                newDirectoryNameAbsolute = GlueState.Self.CurrentGlueProjectDirectory + newDirectoryNameRelative;

                string whyIsInvalid = null;
                NameVerifier.IsDirectoryNameValid(inputWindow.Result, out whyIsInvalid);

                if (string.IsNullOrEmpty(whyIsInvalid) && Directory.Exists(newDirectoryNameAbsolute))
                {
                    whyIsInvalid = $"The directory {inputWindow.Result} already exists.";
                }

                if (!string.IsNullOrEmpty(whyIsInvalid))
                {
                    MessageBox.Show(whyIsInvalid);
                    shouldPerformMove = false;
                }
                else
                {
                    shouldPerformMove = true;
                }
            }

            if(shouldPerformMove && !Directory.Exists(newDirectoryNameAbsolute))
            {
                try
                {
                    Directory.CreateDirectory(newDirectoryNameAbsolute);
                }
                catch(Exception ex)
                {
                    PluginManager.ReceiveError(ex.ToString());
                    shouldPerformMove = false;
                }
            }

            if(shouldPerformMove)
            {
                var allContainedEntities = GlueState.Self.CurrentGlueProject.Entities
                    .Where(entity => entity.Name.StartsWith(directoryRenaming)).ToList();

                newDirectoryNameRelative = newDirectoryNameRelative.Replace('/', '\\');

                bool didAllSucceed = true;

                foreach(var entity in allContainedEntities)
                {
                    bool succeeded = GlueCommands.Self.GluxCommands.MoveEntityToDirectory(entity, newDirectoryNameRelative);

                    if(!succeeded)
                    {
                        didAllSucceed = false;
                        break;
                    }
                }

                if(didAllSucceed)
                {
                    treeNode.Text = inputWindow.Result;

                    ProjectLoader.Self.MakeGeneratedItemsNested();
                    GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
                    
                    GluxCommands.Self.SaveGlux();
                    ProjectManager.SaveProjects();

                }
            }
        }

        static void GetAllEntitySavesIn(TreeNode treeNode, List<EntitySave> allEntitySaves)
        {
            foreach (TreeNode subNode in treeNode.Nodes)
            {
                if (subNode.IsDirectoryNode())
                {
                    GetAllEntitySavesIn(subNode, allEntitySaves);
                }
                else if (subNode is EntityTreeNode)
                {
                    EntityTreeNode asEntityTreeNode = subNode as EntityTreeNode;
                    allEntitySaves.Add(asEntityTreeNode.EntitySave);
                }
            }
        }

        static void GetAllReferencedFileSavesIn(TreeNode treeNode, List<ReferencedFileSave> allReferencedFileSaves)
        {
            foreach (TreeNode subNode in treeNode.Nodes)
            {
                if (subNode.IsDirectoryNode())
                {
                    GetAllReferencedFileSavesIn(subNode, allReferencedFileSaves);
                }
                else if (subNode.IsReferencedFile())
                {
                    ReferencedFileSave rfs = subNode.Tag as ReferencedFileSave;
                    allReferencedFileSaves.Add(rfs);
                }
            }
        }

        static void ViewCodeFolderInExplorerClick(object sender, EventArgs e)
        {
            if (EditorLogic.CurrentTreeNode.IsDirectoryNode())
            {
                string locationToShow = FileManager.RelativeDirectory + EditorLogic.CurrentTreeNode.GetRelativePath();

                locationToShow = locationToShow.Replace("/", "\\");
                Process.Start("explorer.exe", "/select," + locationToShow);
            }
        }


        internal static void ReGenerateCodeForSelectedElement()
        {
            // re-generate regenerate re generate regenerate code re generate code re-generate code
            if (EditorLogic.CurrentTreeNode.IsGlobalContentContainerNode())
            {
                GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();
            }

            #region This is a content file

            else if (EditorLogic.CurrentTreeNode.IsReferencedFile())
            {
                ReferencedFileSave rfs = EditorLogic.CurrentReferencedFile;

                var isCsv = 
                    FileManager.GetExtension(rfs.Name) == "csv" || (FileManager.GetExtension(rfs.Name) == "txt" && rfs.TreatAsCsv);

                var shouldGenerateCsvDataClass =
                    isCsv && !rfs.IsDatabaseForLocalizing;

                if (shouldGenerateCsvDataClass)
                {
                    CsvCodeGenerator.GenerateAndSaveDataClass(rfs, rfs.CsvDelimiter);
                    GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();
                    ProjectManager.SaveProjects();
                    GluxCommands.Self.SaveGlux();
                }

            }

            #endregion

            #region Else, it's a code file

            else
            {
                // We used to allow regeneration of non-generated files
                // But people accidentally click this, and it means you have
                // to be careful when you right-click.  That sucks.  Now, Glue 
                // cannot regenerate the non-generated code file.


                bool isScreenOrEntity = EditorLogic.CurrentScreenSave != null ||
                    EditorLogic.CurrentEntitySave != null;

                if (isScreenOrEntity)
                {
                    ElementViewWindow.GenerateSelectedElementCode();
                }

                foreach (ProjectBase project in ProjectManager.SyncedProjects)
                {
                    project.ClearPendingTranslations();

                    project.AddCodeBuildItem(EditorLogic.CurrentTreeNode.Text);

                    project.PerformPendingTranslations();
                }

            }

            #endregion
        }

        public static void AddExistingFileClick()
        {
            bool userCancelled = false;
            // add externally built file, add external file, add built file
            if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();

                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (string fileName in openFileDialog.FileNames)
                    {
                        AddSingleFile(fileName, ref userCancelled);
                    }
                }
            }
        }

        public static ReferencedFileSave AddSingleFile(string fileName, ref bool userCancelled, string options = null)
        {

            var element = GlueState.Self.CurrentElement;
            string directoryOfTreeNode = EditorLogic.CurrentTreeNode.GetRelativePath();
            return AddSingleFile(fileName, ref userCancelled, element, directoryOfTreeNode, options);
        }


        public static ReferencedFileSave AddSingleFile(string fileName, ref bool userCancelled, IElement element, string directoryOfTreeNode, string options = null)
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

            if(userPickedNone)
            {
                isBuiltFile = false;
            }

            if (isBuiltFile && buildToolAssociation == null && !userCancelled && !userPickedNone)
            {
                TaskManager.Self.OnUiThread(() =>
                    {
                        System.Windows.Forms.MessageBox.Show("Couldn't find a tool for the file extension " + sourceExtension);
                    });
            }

            else if (!userCancelled)
            {

                toReturn = GlueCommands.Self.GluxCommands.AddSingleFileTo(fileName, rfsName, extraCommandLineArguments, buildToolAssociation,
                    isBuiltFile, options, element, directoryOfTreeNode);
            }



            return toReturn;

        }


        internal static void AddVariableClick(CustomVariableType variableType = CustomVariableType.Exposed, string tunnelingObject = "")
        {
            // Search terms:  add new variable, addnewvariable, add variable

            AddVariableWindow addVariableWindow = new AddVariableWindow(EditorLogic.CurrentElement);
            addVariableWindow.DesiredVariableType = variableType;

            addVariableWindow.TunnelingObject = tunnelingObject;

            if (addVariableWindow.ShowDialog(MainGlueWindow.Self) == DialogResult.OK)
            {
                HandleAddVariableOk(addVariableWindow);
            }
        }

        private static void HandleAddVariableOk(AddVariableWindow addVariableWindow)
        {
            string resultName = addVariableWindow.ResultName;
            IElement currentElement = EditorLogic.CurrentElement;

            bool didFailureOccur = IsVariableInvalid(addVariableWindow, resultName, currentElement);

            if (!didFailureOccur)
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

                    CustomVariable newVariable = CreateAndAddNewVariable(resultName, type, sourceObject, sourceObjectProperty, overridingType, typeConverter);

                    if (isDefinedByBase)
                    {
                        newVariable.DefinedByBase = isDefinedByBase;
                        // Refresh the UI - it's refreshed above in CreateAndAddNewVariable,
                        // but we're changing the DefinedByBase property which changes the color
                        // of the variable so refresh it again
                        EditorLogic.CurrentElementTreeNode.UpdateReferencedTreeNodes();
                    }
                    ElementViewWindow.ShowAllElementVariablesInPropertyGrid();


                    if(GlueState.Self.CurrentElement != null)
                    {
                        PluginManager.ReactToItemSelect(GlueState.Self.CurrentTreeNode);
                    }
                }
            }
        }

        private static bool IsVariableInvalid(AddVariableWindow addVariableWindow, string resultName, IElement currentElement)
        {
            bool didFailureOccur = false;

            string whyItIsntValid = "";

            didFailureOccur = NameVerifier.IsCustomVariableNameValid(resultName, null, currentElement, ref whyItIsntValid) == false;

            if (didFailureOccur)
            {
                System.Windows.Forms.MessageBox.Show(whyItIsntValid);

            }
            else if (addVariableWindow != null && NameVerifier.DoesTunneledVariableAlreadyExist(addVariableWindow.TunnelingObject, addVariableWindow.TunnelingVariable, currentElement))
            {
                didFailureOccur = true;
                MessageBox.Show("There is already a variable that is modifying " + addVariableWindow.TunnelingVariable + " on " + addVariableWindow.TunnelingObject);
            }
            else if (addVariableWindow != null && IsUserTryingToCreateNewWithExposableName(addVariableWindow.ResultName, addVariableWindow.DesiredVariableType == CustomVariableType.Exposed))
            {
                didFailureOccur = true;
                MessageBox.Show("The variable\n\n" + resultName + "\n\nis an expoable variable.  Please use a different variable name or select the variable through the Expose tab");
            }

            else if (ExposedVariableManager.IsReservedPositionedPositionedObjectMember(resultName) && currentElement is EntitySave)
            {
                didFailureOccur = true;
                MessageBox.Show("The variable\n\n" + resultName + "\n\nis reserved by FlatRedBall.");
            }
            return didFailureOccur;
        }

        private static bool IsUserTryingToCreateNewWithExposableName(string resultName, bool isExposeTabSelected)
        {
            List<string> exposables = ExposedVariableManager.GetExposableMembersFor(EditorLogic.CurrentElement, false).Select(item=>item.Member).ToList();
            if (exposables.Contains(resultName))
            {
                return isExposeTabSelected == false;
            }
            else
            {
                return false;
            }
        }

        public static CustomVariable CreateAndAddNewVariable(string resultName, string type, string sourceObject, string sourceObjectProperty, string overridingType, string typeConverter)
        {
            IElement currentElement = EditorLogic.CurrentElement;
            CustomVariable newVariable = new CustomVariable();
            newVariable.Type = type;
            newVariable.Name = resultName;
            newVariable.SourceObject = sourceObject;
            newVariable.SourceObjectProperty = sourceObjectProperty;



            if (!string.IsNullOrEmpty(overridingType))
            {
                newVariable.OverridingPropertyType = overridingType;
                newVariable.TypeConverter = typeConverter;
            }

            currentElement.CustomVariables.Add(newVariable);

            CustomVariableHelper.SetDefaultValueFor(newVariable, EditorLogic.CurrentElement);

            if (EditorLogic.CurrentEntityTreeNode != null)
            {


                EditorLogic.CurrentEntityTreeNode.UpdateReferencedTreeNodes();

            }
            else if (EditorLogic.CurrentScreenTreeNode != null)
            {
                EditorLogic.CurrentScreenTreeNode.UpdateReferencedTreeNodes();
            }

            MainGlueWindow.Self.PropertyGrid.Refresh();


            ElementViewWindow.GenerateSelectedElementCode();

            UpdateInstanceCustomVariables(currentElement);

            PluginManager.ReactToVariableAdded(newVariable);


            GluxCommands.Self.SaveGlux();

            return newVariable;
        }

        public static void UpdateInstanceCustomVariables(IElement currentElement)
        {
            List<NamedObjectSave> namedObjectsToUpdate = null;

            if (currentElement is EntitySave)
            {
                namedObjectsToUpdate = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(currentElement.Name);
            }

            if (namedObjectsToUpdate != null)
            {
                foreach (NamedObjectSave nos in namedObjectsToUpdate)
                {
                    nos.UpdateCustomProperties();
                }
            }
        }

        internal static void ShowAddNewFileWindow()
        {
            // Search terms:  add file, addfile, add new file, addnewfile
            if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
            {
                ReferencedFileSave rfs = GlueCommands.Self.DialogCommands.ShowAddNewFileDialog();

                // if rfs is null, that means the user hit cancel
                if (GlueState.Self.CurrentEntitySave != null && rfs != null)
                {
                    bool created = AskToCreateEntireFileObject(rfs);

                    if (created)
                    {
                        GluxCommands.Self.SaveGlux();
                    }
                }
            }
        }

        private static bool AskToCreateEntireFileObject(ReferencedFileSave rfs)
        {
            string extension = FileManager.GetExtension(rfs.Name);
            bool shouldAskToMakeEntireFile = extension == "scnx" || extension == "shcx";
            bool created = false;

            if (shouldAskToMakeEntireFile)
            {
                string message = "Would you like to make an Object for this new file (recommended)?";

                DialogResult result = MessageBox.Show(message, "Make entire object?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromExtension(extension);

                    string runtimeTypeName = ati.RuntimeTypeName;

                    NamedObjectSave nos = NamedObjectSaveExtensionMethodsGlue.AddNewNamedObjectToSelectedElement("Entire" + runtimeTypeName, MembershipInfo.ContainedInThis, false);
                    IElement element = GlueState.Self.CurrentElement;

                    while (element.AllNamedObjects.Any(item => item != nos && item.InstanceName == nos.InstanceName))
                    {
                        nos.InstanceName  = StringFunctions.IncrementNumberAtEnd(nos.InstanceName);
                    }

                    nos.SourceFile = rfs.Name;

                    PluginManager.ReactToNewObject(nos);

                    nos.SourceName = "Entire File (" + ati.RuntimeTypeName + ")";
                    created = true;

                    // The NOS may have had its named changed:
                    GlueCommands.Self.RefreshCommands.RefreshUiForSelectedElement();
                }
            }
            return created;
        }

        private static void MoveToTopClick(object sender, EventArgs e)
        {
            MoveToTop();

        }

        public static bool MoveToTop()
        {
            object objectToRemove;
            IList listToRemoveFrom;
            GetObjectAndListForMoving(out objectToRemove, out listToRemoveFrom);
            if (listToRemoveFrom != null)
            {
                int index = listToRemoveFrom.IndexOf(objectToRemove);
                if (index > 0)
                {
                    listToRemoveFrom.Remove(objectToRemove);
                    listToRemoveFrom.Insert(0, objectToRemove);
                    PostMoveActivity(EditorLogic.CurrentTreeNode);
                }
                return true;
            }
            return false;
        }

        private static void MoveUpClick(object sender, EventArgs e)
        {
            MoveSelectedObjectUp();
        }

        private static void MoveDownClick(object sender, EventArgs e)
        {
            MoveSelectedObjectDown();
        }

        public static bool MoveSelectedObjectUp()
        {
            int direction = -1;
            return MoveObjectInDirection(direction);
        }

        public static bool MoveSelectedObjectDown()
        {
            int direction = 1;
            return MoveObjectInDirection(direction);
        }

        private static bool MoveObjectInDirection(int direction)
        {
            object objectToRemove;
            IList listToRemoveFrom;
            GetObjectAndListForMoving(out objectToRemove, out listToRemoveFrom);
            if (listToRemoveFrom != null)
            {
                int index = listToRemoveFrom.IndexOf(objectToRemove);
                int newIndex = index + direction;
                if (newIndex >= 0 && newIndex < listToRemoveFrom.Count)
                {
                    listToRemoveFrom.Remove(objectToRemove);

                    listToRemoveFrom.Insert(newIndex, objectToRemove);

                    PostMoveActivity(EditorLogic.CurrentTreeNode);

                    return true;
                }
            }

            return false;
        }


        private static void MoveToBottomClick(object sender, EventArgs e)
        {
            MoveToBottom();
        }

        public static bool MoveToBottom()
        {
            object objectToRemove;
            IList listToRemoveFrom;
            GetObjectAndListForMoving(out objectToRemove, out listToRemoveFrom);
            if (listToRemoveFrom != null)
            {

                int index = listToRemoveFrom.IndexOf(objectToRemove);

                if (index < listToRemoveFrom.Count - 1)
                {
                    listToRemoveFrom.Remove(objectToRemove);
                    listToRemoveFrom.Insert(listToRemoveFrom.Count, objectToRemove);
                    PostMoveActivity(EditorLogic.CurrentTreeNode);
                }
                return true;
            }
            return false;
        }

        private static void GetObjectAndListForMoving(out object objectToRemove, out IList listToRemoveFrom)
        {
            objectToRemove = null;
            listToRemoveFrom = null;

            if (EditorLogic.CurrentCustomVariable != null)
            {
                objectToRemove = EditorLogic.CurrentCustomVariable;
                listToRemoveFrom = EditorLogic.CurrentElement.CustomVariables;
            }

            else if (EditorLogic.CurrentNamedObject != null)
            {
                objectToRemove = EditorLogic.CurrentNamedObject;

                NamedObjectSave container = NamedObjectContainerHelper.GetNamedObjectThatIsContainerFor(
                    EditorLogic.CurrentElement, EditorLogic.CurrentNamedObject);

                if (container != null)
                {
                    listToRemoveFrom = container.ContainedObjects;
                }
                else
                {
                    listToRemoveFrom = EditorLogic.CurrentElement.NamedObjects;
                }
            }
        }


        private static void PostMoveActivity(TreeNode namedObjectTreeNode)
        {
            // do this before refreshing the tree nodes
            var tag = namedObjectTreeNode.Tag;

            EditorLogic.CurrentElement.RefreshStatesToCustomVariables();

            UpdateCurrentElementTreeNode();

            IElement element = EditorLogic.CurrentElement;
            List<IElement> elementsToRegen = new List<IElement>();

            foreach (NamedObjectSave nos in ObjectFinder.Self.GetAllNamedObjectsThatUseElement(element))
            {
                nos.UpdateCustomProperties();
                IElement candidateToAdd = nos.GetContainer();

                if (!elementsToRegen.Contains(candidateToAdd))
                {

                    elementsToRegen.Add(candidateToAdd);
                }

            }

            foreach (IElement elementToRegen in elementsToRegen)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(elementToRegen);
            }

            // I think the variables are complete remade. I could make it preserve them, but it's easier to do this:
            if(tag is CustomVariable)
            {
                GlueState.Self.CurrentCustomVariable = tag as CustomVariable;
            }
            else
            {
                ElementViewWindow.SelectedNode = namedObjectTreeNode;
            }

            GluxCommands.Self.SaveGlux();
        }

        public static void SetExternallyBuiltFileIfHigherThanCurrent(string directoryOfFile, bool performSave)
        {
            if(directoryOfFile == null)
            {
                throw new ArgumentNullException(nameof(directoryOfFile));
            }
            string currentExternalDirectory = null;

            if (!string.IsNullOrEmpty(ProjectManager.GlueProjectSave.ExternallyBuiltFileDirectory))
            {
                currentExternalDirectory = ProjectManager.MakeAbsolute(ProjectManager.GlueProjectSave.ExternallyBuiltFileDirectory, true);
            }

            if (string.IsNullOrEmpty(currentExternalDirectory) ||
                !FileManager.IsRelativeTo(directoryOfFile, currentExternalDirectory))
            {

                //FileWatchManager.SetExternallyBuiltContentDirectory(directoryOfFile);
                //      
                string newExternalDirectoryRelativeToContent = ProjectManager.MakeRelativeContent(directoryOfFile);

                ProjectManager.GlueProjectSave.ExternallyBuiltFileDirectory = newExternalDirectoryRelativeToContent;

                if (performSave)
                {
                    GluxCommands.Self.SaveGlux();
                }
            }
        }

        private static void RebuildFileClick(object sender, EventArgs e)
        {
            // search terms: rebuild file
            ReferencedFileSave rfs = EditorLogic.CurrentReferencedFile;

            if (rfs != null)
            {
                string error;
                // This used to be false,
                // but I'm not sure we want
                // to skip building on missing
                // file.  We are forcing a build
                // so we should always build
                const bool buildOnMissingFile = true;

                rfs.RefreshSourceFileCache(buildOnMissingFile, out error);
                if (!string.IsNullOrEmpty(error))
                {
                    ErrorReporter.ReportError(rfs.Name, error, false);
                }
                else
                {
                    error = rfs.PerformExternalBuild(runAsync: true);

                    if (!string.IsNullOrEmpty(error))
                    {
                        ErrorReporter.ReportError(FileManager.MakeAbsolute(rfs.Name), error, true);
                    }

                    UpdateReactor.UpdateFile(
                        ProjectManager.MakeAbsolute(rfs.Name));


                    UnreferencedFilesManager.Self.RefreshUnreferencedFiles(false);
                }
            }
        }

        private static void ViewSourceInExplorerClick(object sender, EventArgs e)
        {
            ReferencedFileSave rfs = EditorLogic.CurrentReferencedFile;

            if (rfs != null)
            {
                if (string.IsNullOrEmpty(rfs.SourceFile))
                {
                    MessageBox.Show("This object has a null source file.", "Error opening folder");
                }
                else
                {

                    string file = FileManager.Standardize(ProjectManager.MakeAbsolute(rfs.SourceFile, true)).Replace("/", "\\");

                    Process.Start("explorer.exe", "/select," + file
                        );
                }
            }
        }

        static void MakeRequiredAtStartupClick(object sender, EventArgs e)
        {
            ScreenSave screenSave = EditorLogic.CurrentScreenSave;


            ScreenTreeNode treeNode = null;

            if (screenSave != null)
            {
                bool isAlreadyRequired = screenSave.IsRequiredAtStartup;

                if (isAlreadyRequired)
                {
                    // It's required which means no other Screen is required.  That was easy
                    screenSave.IsRequiredAtStartup = false;
                    treeNode =
                        GlueState.Self.Find.ScreenTreeNode(screenSave);

                    treeNode.BackColor = ElementViewWindow.RegularBackgroundColor;

                    CodeWriter.GenerateCode(screenSave);
                }
                else
                {
                    // We gotta un-require any other Screen that is required since right now we only
                    // support one required Screen
                    foreach (ScreenSave screenInProject in ProjectManager.GlueProjectSave.Screens)
                    {
                        if (screenInProject.IsRequiredAtStartup)
                        {
                            screenInProject.IsRequiredAtStartup = false;

                            treeNode =
                                GlueState.Self.Find.ScreenTreeNode(screenInProject);

                            treeNode.BackColor = ElementViewWindow.RegularBackgroundColor;
                            CodeWriter.GenerateCode(screenInProject);

                            break;
                        }
                    }

                    screenSave.IsRequiredAtStartup = true;

                    treeNode =
                        GlueState.Self.Find.ScreenTreeNode(screenSave);

                    treeNode.BackColor = ElementViewWindow.RequiredScreenColor;
                    CodeWriter.GenerateCode(screenSave);

                }

                CodeWriter.RefreshStartupScreenCode();

                GluxCommands.Self.SaveGlux();
            }
        }

        static void CreateNewFileForMissingFileClick(object sender, EventArgs e)
        {
            TreeNode treeNode = EditorLogic.CurrentTreeNode;

            string extension = FileManager.GetExtension(treeNode.Text);

            AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromExtension(extension);

            string resultNameInFolder = FileManager.RemoveExtension(FileManager.RemovePath(treeNode.Text));
            string directory = FileManager.GetDirectory(ProjectManager.MakeAbsolute(treeNode.Text, true));

            PluginManager.CreateNewFile(
                ati, false, directory, resultNameInFolder);

            GlueCommands.Self.RefreshCommands.RefreshUiForSelectedElement();
        }

        private static void UpdateCurrentElementTreeNode()
        {
            BaseElementTreeNode containerTreeNode = EditorLogic.CurrentElementTreeNode;

            containerTreeNode.UpdateReferencedTreeNodes();

            CodeWriter.GenerateCode(containerTreeNode.SaveObjectAsElement);
        }


        internal static void ErrorCheckClick()
        {
            ErrorManager.HandleCheckErrors();

        }

        internal static void AddScreenToolStripClick()
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
                    TextInputWindow tiw = new TextInputWindow();

                    tiw.DisplayText = "Enter a name for the new Screen";
                    tiw.Text = "New Screen";

                    if (tiw.ShowDialog(MainGlueWindow.Self) == DialogResult.OK)
                    {
                        string whyItIsntValid;

                        if (!NameVerifier.IsScreenNameValid(tiw.Result, null, out whyItIsntValid))
                        {
                            MessageBox.Show(whyItIsntValid);
                        }
                        else
                        {
                            var screen = ProjectManager.AddScreen(tiw.Result);

                            GlueState.Self.CurrentElement = screen;
                        }
                    }
                }
            }
        }


        public static void CreateZipPackageClick(object sender, EventArgs e)
        {
            // Create zip, create package, create zip package, create package zip
            ReferencedFileSave rfs = EditorLogic.CurrentReferencedFile;

            string fileName = Zipper.CreateZip(rfs);

            if (string.IsNullOrEmpty(fileName))
            {
                MessageBox.Show("Could not create packaged file - likely because " +
                    "the file " + rfs.Name + " contains files that are not relative.");
            }
            else
            {
                // .Start doesn't seem to work properly
                // if the path has forward slashes.  Replacing
                // with backslashes seems to have fixed the issue.
                Process.Start("explorer.exe", "/select," + fileName.Replace("/", "\\"));
            }
        }

        static void ExportElementClick(object sender, EventArgs e)
        {
            // export screen, export entity, export element
            ElementExporter.ExportElement(EditorLogic.CurrentElement);
        }

        static void ImportElementClick(object sender, EventArgs e)
        {
            ElementImporter.ImportElement();
        }


    }
}
