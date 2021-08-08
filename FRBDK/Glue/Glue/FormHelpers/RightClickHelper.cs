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
//using Microsoft.Xna.Framework.Content.Pipeline.Processors;
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
using EditorObjects.IoC;
using FlatRedBall.Glue.SetVariable;
using GlueFormsCore.Plugins.EmbeddedPlugins.ExplorerTabPlugin;

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

        static ToolStripMenuItem addScreenToolStripMenuItem;

        static ToolStripMenuItem addFileToolStripMenuItem;
        static ToolStripMenuItem newFileToolStripMenuItem;
        static ToolStripMenuItem existingFileToolStripMenuItem;
        static ToolStripMenuItem viewInExplorerToolStripMenuItem;

        static ToolStripMenuItem openWithDEFAULTToolStripMenuItem;


        static ToolStripMenuItem setAsStartUpScreenToolStripMenuItem;

        static ToolStripMenuItem addObjectToolStripMenuItem;
        static ToolStripMenuItem addEntityToolStripMenuItem;
        static ToolStripMenuItem removeFromProjectToolStripMenuItem;

        static ToolStripMenuItem addVariableToolStripMenuItem;

        static ToolStripMenuItem editResetVariablesToolStripMenuItem;

        static ToolStripMenuItem addFolderToolStripMenuItem;

        static ToolStripMenuItem ignoreDirectoryToolStripMenuItem;

        static ToolStripMenuItem setCreatedClassToolStripMenuItem;


        static ToolStripMenuItem mMoveToTop;
        static ToolStripMenuItem mMoveToBottom;

        static ToolStripMenuItem mMoveUp;
        static ToolStripMenuItem mMoveDown;
        static ToolStripMenuItem mMakeRequiredAtStartup;
        static ToolStripMenuItem mRebuildFile;

        static ToolStripMenuItem mViewSourceInExplorer;

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
        static ToolStripMenuItem createDerivedScreen;

        static ToolStripMenuItem mAddEventMenuItem;

        static ToolStripMenuItem mRefreshTreeNodesMenuItem;

        static ToolStripMenuItem mAddEntityInstance;
        static ToolStripMenuItem mAddEntityList;

        static ToolStripMenuItem mCopyToBuildFolder;

        static ToolStripMenuItem reGenerateCodeToolStripMenuItem;

        static ToolStripMenuItem addLayeritem;
        #endregion



        ///////////////////////////////////////////////////////////
        public static void PopulateRightClickItems(TreeNode targetNode, MenuShowingAction menuShowingAction = MenuShowingAction.RegularRightClick)
        {

            MainExplorerPlugin.Self.ElementTreeView.SelectedNode = targetNode;
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
                    menu.Items.Add(setAsStartUpScreenToolStripMenuItem);
                    menu.Items.Add(mMakeRequiredAtStartup);
                    mExportElement.Text = "Export Screen";
                    menu.Items.Add(mExportElement);
                    menu.Items.Add(createDerivedScreen);

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
                menu.Items.Add(addFileToolStripMenuItem);
                menu.Items.Add(addFolderToolStripMenuItem);
                menu.Items.Add("-");
                menu.Items.Add(viewInExplorerToolStripMenuItem);
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
                    menu.Items.Add(addObjectToolStripMenuItem);
                }
            }

            #endregion

            #region IsRootLayerNode

            else if(targetNode.IsRootLayerNode())
            {
                menu.Items.Add(addLayeritem);
            }


            #endregion

            #region IsGlobalContentContainerNode
            else if (targetNode.IsGlobalContentContainerNode())
            {
                menu.Items.Add(addFileToolStripMenuItem);
                menu.Items.Add(addFolderToolStripMenuItem);
                menu.Items.Add(reGenerateCodeToolStripMenuItem);

                menu.Items.Add(viewInExplorerToolStripMenuItem);

                menu.Items.Add(mViewFileLoadOrder);
            }
            #endregion

            #region IsRootEntityNode
            else if (targetNode.IsRootEntityNode())
            {
                menu.Items.Add(addEntityToolStripMenuItem);

                mImportElement.Text = "Import Entity";
                menu.Items.Add(mImportElement);

                menu.Items.Add(addFolderToolStripMenuItem);
            }
            #endregion

            #region IsRootScreenNode
            else if (targetNode.IsRootScreenNode())
            {
                menu.Items.Add(addScreenToolStripMenuItem);

                mImportElement.Text = "Import Screen";
                menu.Items.Add(mImportElement);

            }
            #endregion
            
            #region IsRootCustomVariables

            else if (targetNode.IsRootCustomVariablesNode())
            {
                menu.Items.Add(addVariableToolStripMenuItem);
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

                menu.Items.Add(editResetVariablesToolStripMenuItem);
                menu.Items.Add(mFindAllReferences);

                menu.Items.Add("-");

                menu.Items.Add(mDuplicate);

                menu.Items.Add("-");

                menu.Items.Add(mMoveToTop);
                menu.Items.Add(mMoveUp);
                menu.Items.Add(mMoveDown);
                menu.Items.Add(mMoveToBottom);

                menu.Items.Add("-");

                var currentNamedObject = GlueState.Self.CurrentNamedObjectSave;

                if (currentNamedObject.SourceType == SourceType.FlatRedBallType &&
                    currentNamedObject?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.PositionedObjectList &&
                    !string.IsNullOrEmpty(currentNamedObject.SourceClassGenericType) &&
                    !currentNamedObject.SetByDerived)
                {
                    menu.Items.Add(addObjectToolStripMenuItem);
                }
                else if(currentNamedObject?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.ShapeCollection)
                {
                    menu.Items.Add(addObjectToolStripMenuItem);
                }

            }

            #endregion

            #region IsReferencedFileNode
            else if (targetNode.IsReferencedFile())
            {
                menu.Items.Add(viewInExplorerToolStripMenuItem);
                menu.Items.Add(mFindAllReferences);
                menu.Items.Add("Copy path to clipboard", null, HandleCopyToClipboardClick);
                menu.Items.Add("-");

                menu.Items.Add(mCreateZipPackage);

                menu.Items.Add("-");

                AddRemoveFromProjectItems(form, menu);

                menu.Items.Add(mUseContentPipeline);
                //menu.Items.Add(form.openWithDEFAULTToolStripMenuItem);

                ReferencedFileSave rfs = (ReferencedFileSave)targetNode.Tag;

                if (FileManager.GetExtension(rfs.Name) == "csv" || rfs.TreatAsCsv)
                {
                    menu.Items.Add("-");
                    menu.Items.Add(setCreatedClassToolStripMenuItem);
                    menu.Items.Add(reGenerateCodeToolStripMenuItem);
                }


                if (!string.IsNullOrEmpty(rfs.SourceFile) || rfs.SourceFileCache?.Count > 0)
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

                menu.Items.Add(viewInExplorerToolStripMenuItem);
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


                menu.Items.Add(addFolderToolStripMenuItem);

                bool isEntityContainingFolder = targetNode.Root().IsRootEntityNode();

                if (isEntityContainingFolder)
                {
                    menu.Items.Add(addEntityToolStripMenuItem);

                    mImportElement.Text = "Import Entity";
                    menu.Items.Add(mImportElement);
                }
                else
                {
                    // If not in the Entities tree structure, assume global content
                    menu.Items.Add(addFileToolStripMenuItem);
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

        public static ReferencedFileSave AddSingleFile(string fullFileName, ref bool cancelled, IElement elementToAddTo = null)
        {
            return AddExistingFileManager.Self.AddSingleFile(fullFileName, ref cancelled, elementToAddTo:elementToAddTo);
        }

        public static void Initialize()
        {
            addScreenToolStripMenuItem = new ToolStripMenuItem();
            addScreenToolStripMenuItem.Name = "addScreenToolStripMenuItem";
            addScreenToolStripMenuItem.Text = "Add Screen";
            addScreenToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog();

            setAsStartUpScreenToolStripMenuItem = new ToolStripMenuItem();
            setAsStartUpScreenToolStripMenuItem.Name = "setAsStartUpScreenToolStripMenuItem";
            setAsStartUpScreenToolStripMenuItem.Text = "Set as StartUp Screen";
            setAsStartUpScreenToolStripMenuItem.Click += (not, used) =>
            {
                var selectedNode = GlueState.Self.CurrentTreeNode;
                if (selectedNode != null)
                {
                    GlueCommands.Self.GluxCommands.StartUpScreenName =
                        GlueState.Self.CurrentScreenSave?.Name;
                //    ElementViewWindow.StartUpScreenTreeNode = selectedNode;
                }
            };

            addEntityToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            addEntityToolStripMenuItem.Name = "addEntityToolStripMenuItem";
            addEntityToolStripMenuItem.Text = "Add Entity";
            addEntityToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewEntityDialog();

            addFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            addFolderToolStripMenuItem.Name = "addFolderToolStripMenuItem";
            addFolderToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            addFolderToolStripMenuItem.Text = "Add Folder";
            addFolderToolStripMenuItem.Click += (not, used) => RightClickHelper.AddFolderClick(); 
            
            addObjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            addObjectToolStripMenuItem.Name = "addObjectToolStripMenuItem";
            addObjectToolStripMenuItem.Text = "Add Object";
            addObjectToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewObjectDialog();

            ignoreDirectoryToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ignoreDirectoryToolStripMenuItem.Name = "ignoreDirectoryToolStripMenuItem";
            ignoreDirectoryToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            ignoreDirectoryToolStripMenuItem.Text = "Ignore Directory";
            ignoreDirectoryToolStripMenuItem.Click += (not, used) => RightClickHelper.IgnoreDirectoryClick();

            existingFileToolStripMenuItem = new ToolStripMenuItem();
            existingFileToolStripMenuItem.Name = "existingFileToolStripMenuItem";
            existingFileToolStripMenuItem.Text = "Existing File";
            existingFileToolStripMenuItem.Click += (not, used) => AddExistingFileManager.Self.AddExistingFileClick();

            setCreatedClassToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            setCreatedClassToolStripMenuItem.Name = "setCreatedClassToolStripMenuItem";
            setCreatedClassToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            setCreatedClassToolStripMenuItem.Text = "Set Created Class";
            setCreatedClassToolStripMenuItem.Click += (not, used) =>
            {
                CustomClassWindow ccw = new CustomClassWindow();

                ccw.SelectFile(GlueState.Self.CurrentReferencedFileSave);

                ccw.ShowDialog(MainGlueWindow.Self);

                GlueCommands.Self.ProjectCommands.SaveProjects();
                GluxCommands.Self.SaveGlux();
            };
            

            openWithDEFAULTToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            openWithDEFAULTToolStripMenuItem.Name = "openWithDEFAULTToolStripMenuItem";
            openWithDEFAULTToolStripMenuItem.Text = "Open with...";

            newFileToolStripMenuItem = new ToolStripMenuItem();
            newFileToolStripMenuItem.Name = "newFileToolStripMenuItem";
            newFileToolStripMenuItem.Size = new System.Drawing.Size(135, 22);
            newFileToolStripMenuItem.Text = "New File";
            newFileToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewFileDialog(); 

            addFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            addFileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            newFileToolStripMenuItem,
            existingFileToolStripMenuItem});

            addFileToolStripMenuItem.Name = "addFileToolStripMenuItem";
            addFileToolStripMenuItem.Text = "Add File";
            // this didn't do anything before I migrated it here. What does it do?
            //addFileToolStripMenuItem.Click += new System.EventHandler(this.addFileToolStripMenuItem_Click);

            viewInExplorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            viewInExplorerToolStripMenuItem.Name = "viewInExplorerToolStripMenuItem";
            viewInExplorerToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            viewInExplorerToolStripMenuItem.Text = "View in explorer";
            viewInExplorerToolStripMenuItem.Click += (not, used) => RightClickHelper.ViewInExplorerClick();

            removeFromProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            removeFromProjectToolStripMenuItem.Name = "removeFromProjectToolStripMenuItem";
            removeFromProjectToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            removeFromProjectToolStripMenuItem.Text = "Remove from project";
            removeFromProjectToolStripMenuItem.Click += (not, used) => RightClickHelper.RemoveFromProjectToolStripMenuItem(); 

            mMoveToTop = new ToolStripMenuItem("^^ Move To Top");
            mMoveToTop.ShortcutKeyDisplayString = "Alt+Shift+Up";
            mMoveToTop.Click += new System.EventHandler(MoveToTopClick);

            addVariableToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            addVariableToolStripMenuItem.Name = "addVariableToolStripMenuItem";
            addVariableToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            addVariableToolStripMenuItem.Text = "Add Variable";
            addVariableToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog();

            editResetVariablesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            editResetVariablesToolStripMenuItem.Name = "editResetVariablesToolStripMenuItem";
            editResetVariablesToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
            editResetVariablesToolStripMenuItem.Text = "Edit Reset Variables";
            editResetVariablesToolStripMenuItem.Click += (not, used) =>
            {

                NamedObjectSave nos = EditorLogic.CurrentNamedObject;

                VariablesToResetWindow vtrw = new VariablesToResetWindow(nos.VariablesToReset);
                DialogResult result = vtrw.ShowDialog(MainGlueWindow.Self);

                if (result == DialogResult.OK)
                {

                    string[] results = vtrw.Results;
                    nos.VariablesToReset.Clear();

                    nos.VariablesToReset.AddRange(results);

                    for (int i = nos.VariablesToReset.Count - 1; i > -1; i--)
                    {
                        nos.VariablesToReset[i] = nos.VariablesToReset[i].Replace("\n", "").Replace("\r", "");

                        if (string.IsNullOrEmpty(nos.VariablesToReset[i]))
                        {
                            nos.VariablesToReset.RemoveAt(i);
                        }
                    }
                    StringFunctions.RemoveDuplicates(nos.VariablesToReset);
                    GluxCommands.Self.SaveGlux();

                    ElementViewWindow.GenerateSelectedElementCode();


                }
            };
            

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
            mRebuildFile.Click += RebuildFileClick;

            mViewSourceInExplorer = new ToolStripMenuItem("View source file in explorer");
            mViewSourceInExplorer.Click += new EventHandler(ViewSourceInExplorerClick);

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

            createDerivedScreen = new ToolStripMenuItem("Create Derived (Level) Screen");
            createDerivedScreen.Click += HandleCreateDerivedScreenClicked;

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
            reGenerateCodeToolStripMenuItem.Click += HandleReGenerateCodeClick;

            addLayeritem = new ToolStripMenuItem("Add Layer");
            addLayeritem.Click += HandleAddLayerClick;
        }

        private static void HandleCreateDerivedScreenClicked(object sender, EventArgs e)
        {
            var baseScreen = GlueState.Self.CurrentScreenSave;

            GlueCommands.Self.DialogCommands.ShowCreateDerivedScreenDialog(baseScreen);
        }



        private static void HandleReGenerateCodeClick(object sender, EventArgs e)
        {
            ReGenerateCodeForSelectedElement();
        }

        private static void HandleAddLayerClick(object sender, EventArgs e)
        {
            var viewModel = new AddObjectViewModel();
            viewModel.SourceType = SourceType.FlatRedBallType;
            viewModel.SelectedAti = AvailableAssetTypes.CommonAtis.Layer;
            viewModel.IsTypePredetermined = true;

            GlueCommands.Self.DialogCommands.ShowAddNewObjectDialog(viewModel);
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
            DragDropManager.Self.CreateNewNamedObjectInElement(
                GlueState.Self.CurrentElement,
                (EntitySave)ElementViewWindow.TreeNodeDraggedOff.Tag,
                true);

            GlueCommands.Self.ProjectCommands.SaveProjects();
            GlueCommands.Self.GluxCommands.SaveGlux();

        }

        static void OnAddEntityInstanceClick(object sender, EventArgs e)
        {
            ElementViewWindow.DragDropTreeNode(
                MainExplorerPlugin.Self.ElementTreeView,
                MainExplorerPlugin.Self.ElementTreeView.SelectedNode);


            GlueCommands.Self.ProjectCommands.SaveProjects();
            GlueCommands.Self.GluxCommands.SaveGlux();
        }

        static void OnRefreshTreeNodesClick(object sender, EventArgs e)
        {
            GlueCommands.Self.RefreshCommands.RefreshUiForSelectedElement();
        }

        [Obsolete("Use GlueCommands.DialogCommands.ShowAddNewEventDialog")]
        public static void ShowAddEventWindow(NamedObjectSave objectToTunnelInto)
        {
            GlueCommands.Self.DialogCommands.ShowAddNewEventDialog(objectToTunnelInto);

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

        public static void HandleAddEventOk(AddEventWindow addEventWindow)
        {
            string resultName = addEventWindow.ResultName;
            IElement currentElement = EditorLogic.CurrentElement;

            string failureMessage;
            bool isInvalid = DialogCommands.IsVariableInvalid(null, resultName, currentElement, out failureMessage);

            if(isInvalid)
            {
                MessageBox.Show(failureMessage);
            }
            else if (!isInvalid)
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

            EditorLogic.CurrentElementTreeNode.RefreshTreeNodes();

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
            menu.Items.Add(removeFromProjectToolStripMenuItem);

            if (GlueState.Self.CurrentReferencedFileSave != null ||
                GlueState.Self.CurrentNamedObjectSave != null ||
                GlueState.Self.CurrentEventResponseSave != null ||
                GlueState.Self.CurrentCustomVariable != null ||
                GlueState.Self.CurrentStateSave != null ||
                GlueState.Self.CurrentStateSaveCategory != null)
            {
                if (GlueState.Self.CurrentScreenSave != null)
                {
                    removeFromProjectToolStripMenuItem.Text = "Remove from Screen";
                }
                else if (GlueState.Self.CurrentEntitySave != null)
                {
                    removeFromProjectToolStripMenuItem.Text = "Remove from Entity";
                }
                else
                {
                    removeFromProjectToolStripMenuItem.Text = "Remove from Global Content";
                }
            }
            else
            {
                removeFromProjectToolStripMenuItem.Text = "Remove item";
            }
            if ((Control.ModifierKeys & Keys.Shift) != 0)
            {
                menu.Items.Add(mRemoveFromProjectQuick);
            }
        }

        static void mFillValuesFromVariables_Click(object sender, EventArgs e)
        {
            StateSave stateSave = EditorLogic.CurrentStateSave;
            IElement element = GlueState.Self.CurrentElement;

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
            tiw.Message  = "Enter a name for the new state";
            tiw.Text = "New State";


            DialogResult result = tiw.ShowDialog(MainGlueWindow.Self);

            if (result == DialogResult.OK)
            {
                var currentElement = GlueState.Self.CurrentElement;

                string whyItIsntValid;
                if (!NameVerifier.IsStateNameValid(tiw.Result, currentElement, EditorLogic.CurrentStateSaveCategory, EditorLogic.CurrentStateSave, out whyItIsntValid))
                {
                    GlueGui.ShowMessageBox(whyItIsntValid);
                }
                else
                {

                    StateSave newState = new StateSave();
                    newState.Name = tiw.Result;

                    var category = EditorLogic.CurrentStateSaveCategory;

                    if (category != null)
                    {
                        category.States.Add(newState);
                    }
                    else
                    {
                        var element = currentElement;

                        element.States.Add(newState);
                    }

                    EditorLogic.CurrentElementTreeNode.RefreshTreeNodes();

                    PluginManager.ReactToStateCreated(newState, category);

                    ElementViewWindow.GenerateSelectedElementCode();

                    GlueCommands.Self.TreeNodeCommands.SelectTreeNode(newState);

                    GluxCommands.Self.SaveGlux();
                    GlueCommands.Self.ProjectCommands.SaveProjects();
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

                    EditorLogic.CurrentElementTreeNode.RefreshTreeNodes();
                    ElementViewWindow.GenerateSelectedElementCode();

                    GluxCommands.Self.SaveGlux();
                    GlueCommands.Self.ProjectCommands.SaveProjects();
                }
            }
        }

        static void DuplicateClick(object sender, EventArgs e)
        {
            if (GlueState.Self.CurrentNamedObjectSave != null)
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
            NamedObjectSave namedObjectToDuplicate = GlueState.Self.CurrentNamedObjectSave;

            NamedObjectSave newNamedObject = namedObjectToDuplicate.Clone();

            #region Update the instance name

            newNamedObject.InstanceName = StringFunctions.IncrementNumberAtEnd(newNamedObject.InstanceName);
            if (newNamedObject.InstanceName.EndsWith("1") && StringFunctions.GetNumberAtEnd(newNamedObject.InstanceName) == 1)
            {
                newNamedObject.InstanceName = StringFunctions.IncrementNumberAtEnd(newNamedObject.InstanceName);
            }

            #endregion

            TreeNode treeNodeForNamedObject = GlueState.Self.Find.NamedObjectTreeNode(namedObjectToDuplicate);
            TreeNode parentTreeNode = treeNodeForNamedObject.Parent;

            #region Get the container

            GlueElement container = null;

            if (parentTreeNode.IsRootNamedObjectNode() && parentTreeNode.Parent.IsEntityNode())
            {
                container = ((EntityTreeNode)parentTreeNode.Parent).SaveObject;
            }
            else if (parentTreeNode.IsRootNamedObjectNode() && parentTreeNode.Parent.IsScreenNode())
            {
                container = ((ScreenTreeNode)parentTreeNode.Parent).SaveObject;
            }
            else if (parentTreeNode.IsNamedObjectNode())
            {
                // handled below
            }
            #endregion


            if (container != null)
            {
                int indexToInsertAt = 1 + container.NamedObjects.IndexOf(namedObjectToDuplicate);

                while (container.GetNamedObjectRecursively(newNamedObject.InstanceName) != null)
                {
                    newNamedObject.InstanceName = StringFunctions.IncrementNumberAtEnd(newNamedObject.InstanceName);
                }

                container.NamedObjects.Insert(indexToInsertAt, newNamedObject);
            }
            else
            {
                NamedObjectSave list = parentTreeNode.Tag as NamedObjectSave;

                bool IsShapeCollection(NamedObjectSave nos)
                {
                    return nos.SourceType == SourceType.FlatRedBallType &&
                        (nos.SourceClassType == "ShapeCollection" || nos.SourceClassType == "FlatRedBall.Math.Geometry.ShapeCollection");
                }

                if (list != null && (list.IsList || IsShapeCollection(list)))
                {
                    int indexToInsertAt = 1 + list.ContainedObjects.IndexOf(namedObjectToDuplicate);

                    container = GlueState.Self.CurrentElement;

                    while (container.GetNamedObjectRecursively(newNamedObject.InstanceName) != null)
                    {
                        newNamedObject.InstanceName = StringFunctions.IncrementNumberAtEnd(newNamedObject.InstanceName);
                    }

                    list.ContainedObjects.Insert(indexToInsertAt, newNamedObject);

                }

            }

            if(newNamedObject.SetByDerived)
            {
                Container.Get<NamedObjectSetVariableLogic>().ReactToChangedSetByDerived(newNamedObject, container);
            }



            ElementViewWindow.UpdateCurrentObjectReferencedTreeNodes();

            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

            // run after generated code so plugins like level editor work off latest code
            PluginManager.ReactToNewObject(newNamedObject);

            GlueCommands.Self.ProjectCommands.SaveProjects();
            GluxCommands.Self.SaveGlux();
        }

        private static void DuplicateCurrentStateSave()
        {
            var stateSave = GlueState.Self.CurrentStateSave;

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
            CodeWriter.GenerateCode(GlueState.Self.CurrentElement);
            GlueCommands.Self.ProjectCommands.SaveProjects();
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
                erlw.PopulateWithReferencesTo(EditorLogic.CurrentNamedObject, GlueState.Self.CurrentElement);
            }
            else if (EditorLogic.CurrentCustomVariable != null)
            {
                erlw.PopulateWithReferencesTo(EditorLogic.CurrentCustomVariable, GlueState.Self.CurrentElement);
            }
            else
            {
                erlw.PopulateWithReferencesToElement(GlueState.Self.CurrentElement);
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
                        if (GlueState.Self.CurrentNamedObjectSave != null)
                        {
                            GlueCommands.Self.GluxCommands
                                .RemoveNamedObject(GlueState.Self.CurrentNamedObjectSave, true, true, filesToRemove);
                            //ProjectManager.RemoveNamedObject(EditorLogic.CurrentNamedObject);
                        }
                        #endregion

                        #region Else if is StateSave
                        else if (GlueState.Self.CurrentStateSave != null)
                        {
                            var name = GlueState.Self.CurrentStateSave.Name;

                            GlueState.Self.CurrentElement.RemoveState(GlueState.Self.CurrentStateSave);

                            AskToRemoveCustomVariablesWithoutState(GlueState.Self.CurrentElement);

                            EditorLogic.CurrentElementTreeNode.RefreshTreeNodes();

                            PluginManager.ReactToStateRemoved(GlueState.Self.CurrentElement, name);

                            GluxCommands.Self.SaveGlux();
                        }

                        #endregion

                        #region Else if is StateSaveCategory

                        else if (EditorLogic.CurrentStateSaveCategory != null)
                        {
                            GlueState.Self.CurrentElement.StateCategoryList.Remove(EditorLogic.CurrentStateSaveCategory);

                            EditorLogic.CurrentElementTreeNode.RefreshTreeNodes();

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
                                TaskManager.Self.AddOrRunIfTasked(() =>
                                {
                                    GluxCommands.Self.RemoveReferencedFile(toRemove, filesToRemove, regenerateCode:true);
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
                            RemoveScreen(screenToRemove, filesToRemove);
                        }

                        #endregion

                        #region Else if is EntitySave

                        else if (GlueState.Self.CurrentEntitySave != null)
                        {
                            RemoveEntity(GlueState.Self.CurrentEntitySave, filesToRemove);
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

                            var lbw = new ListBoxWindowWpf();
                            
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

                            var dialogShowResult = lbw.ShowDialog();
                            DialogResult result = (DialogResult)lbw.ClickedOption;

                            if (result == DialogResult.OK || result == DialogResult.Yes)
                            {
                                foreach (string file in filesToRemove)
                                {
                                    FilePath fileName = ProjectManager.MakeAbsolute(file);
                                    // This file may have been removed
                                    // in windows explorer, and now removed
                                    // from Glue.  Check to prevent a crash.

                                    GlueCommands.Self.ProjectCommands.RemoveFromProjects(fileName, false);
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
                            if (EditorLogic.CurrentScreenTreeNode != null)
                            {
                                var screen = EditorLogic.CurrentScreenSave;
                                GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeTask(screen);
                            }
                            else if (EditorLogic.CurrentEntityTreeNode != null)
                            {
                                var entity = EditorLogic.CurrentEntitySave;
                                GlueCommands.Self.GenerateCodeCommands.GenerateElementCodeTask(entity);
                            }
                            else if (EditorLogic.CurrentReferencedFile != null)
                            {
                                GlueCommands.Self.GenerateCodeCommands.GenerateGlobalContentCodeTask();

                                // Vic asks - do we have to do anything else here?  I don't think so...
                            }


                            GluxCommands.Self.ProjectCommands.SaveProjects();
                            GluxCommands.Self.SaveGlux();
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
                GlueCommands.Self.GluxCommands.RemoveEntity(entityToRemove, filesThatCouldBeRemoved);
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


                GlueCommands.Self.GluxCommands.RemoveScreen(screenToRemove, filesThatCouldBeRemoved);
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

                if(isFile)
                {
                    Process.Start("explorer.exe", "/select," + locationToShow);
                }
                else
                {
                    Process.Start("explorer.exe", "/root," + locationToShow);
                }
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
                        MainExplorerPlugin.Self.ElementTreeView.SelectedNode = GlueState.Self.Find.ElementTreeNode(entitySave);
                        RemoveFromProjectOptionalSaveAndRegenerate(entitySave == allEntitySaves[allEntitySaves.Count - 1], false, false);

                    }
                }
                else if (EditorLogic.CurrentTreeNode.IsFolderInFilesContainerNode())
                {
                    List<ReferencedFileSave> allReferencedFileSaves = new List<ReferencedFileSave>();
                    GetAllReferencedFileSavesIn(EditorLogic.CurrentTreeNode, allReferencedFileSaves);

                    foreach (ReferencedFileSave rfs in allReferencedFileSaves)
                    {
                        MainExplorerPlugin.Self.ElementTreeView.SelectedNode = GlueState.Self.Find.ReferencedFileSaveTreeNode(rfs);
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

                    GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();
                    GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();
                    
                    GluxCommands.Self.SaveGlux(); 
                    GlueCommands.Self.ProjectCommands.SaveProjects();

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
                    GlueCommands.Self.ProjectCommands.SaveProjects();
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


                var currentElement = EditorLogic.CurrentElement;

                if (currentElement != null)
                {
                    ElementViewWindow.GenerateSelectedElementCode();
                }


                foreach (VisualStudioProject project in ProjectManager.SyncedProjects)
                {
                    project.ClearPendingTranslations();

                    ((VisualStudioProject)project.CodeProject).AddCodeBuildItem(EditorLogic.CurrentTreeNode.Text);

                    project.PerformPendingTranslations();
                }

            }

            #endregion
        }

        public static CustomVariable CreateAndAddNewVariable(CustomVariable newVariable, bool save = true)
        {
            IElement currentElement = GlueState.Self.CurrentElement;


            currentElement.CustomVariables.Add(newVariable);

            CustomVariableHelper.SetDefaultValueFor(newVariable, currentElement);

            if (EditorLogic.CurrentEntityTreeNode != null)
            {
                EditorLogic.CurrentEntityTreeNode.RefreshTreeNodes();

            }
            else if (EditorLogic.CurrentScreenTreeNode != null)
            {
                EditorLogic.CurrentScreenTreeNode.RefreshTreeNodes();
            }

            MainGlueWindow.Self.PropertyGrid.Refresh();


            ElementViewWindow.GenerateSelectedElementCode();

            UpdateInstanceCustomVariables(currentElement);

            PluginManager.ReactToVariableAdded(newVariable);

            if(save)
            {
                GluxCommands.Self.SaveGlux();
            }

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
        
        private static void MoveToTopClick(object sender, EventArgs e)
        {
            MoveToTop();

        }

        public static bool MoveToTop()
        {
            object objectToRemove;
            IList listToRemoveFrom;
            IList listForIndexing;
            GetObjectAndListForMoving(out objectToRemove, out listToRemoveFrom, out listForIndexing);
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
            IList listForIndexing;
            GetObjectAndListForMoving(out objectToRemove, out listToRemoveFrom, out listForIndexing);
            if (listToRemoveFrom != null)
            {
                int index = listToRemoveFrom.IndexOf(objectToRemove);
                
                var oldIndexInListForIndexing = listForIndexing.IndexOf(objectToRemove);
                var newIndexInListForIndexing = oldIndexInListForIndexing + direction;

                object objectToMoveBeforeOrAfter = objectToRemove;
                if(newIndexInListForIndexing >= 0 && newIndexInListForIndexing < listForIndexing.Count)
                {
                    objectToMoveBeforeOrAfter = listForIndexing[newIndexInListForIndexing];
                }

                //int newIndex = index + direction;
                int newIndex = listToRemoveFrom .IndexOf(objectToMoveBeforeOrAfter);

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
            IList throwaway;
            GetObjectAndListForMoving(out objectToRemove, out listToRemoveFrom, out throwaway);
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

        private static void GetObjectAndListForMoving(out object objectToMove, 
            out IList listToRemoveFrom, out IList listForIndexing)
        {
            objectToMove = null;
            listToRemoveFrom = null;
            listForIndexing = null;
            if (EditorLogic.CurrentCustomVariable != null)
            {
                objectToMove = EditorLogic.CurrentCustomVariable;
                listToRemoveFrom = GlueState.Self.CurrentElement.CustomVariables;
                listForIndexing = listToRemoveFrom;
            }

            else if (GlueState.Self.CurrentNamedObjectSave != null)
            {
                var currentNamedObject = GlueState.Self.CurrentNamedObjectSave;

                objectToMove = currentNamedObject;

                NamedObjectSave container = NamedObjectContainerHelper.GetNamedObjectThatIsContainerFor(
                    GlueState.Self.CurrentElement, GlueState.Self.CurrentNamedObjectSave);

                if (container != null)
                {
                    listToRemoveFrom = container.ContainedObjects;
                    listForIndexing = listToRemoveFrom;
                }
                else if(currentNamedObject.IsLayer)
                {
                    listToRemoveFrom = GlueState.Self.CurrentElement.NamedObjects;
                    listForIndexing = GlueState.Self.CurrentElement.NamedObjects.Where(item => item.IsLayer).ToList();
                }
                else if(currentNamedObject.IsCollisionRelationship())
                {
                    listToRemoveFrom = GlueState.Self.CurrentElement.NamedObjects;
                    listForIndexing = GlueState.Self.CurrentElement.NamedObjects.Where(item => item.IsCollisionRelationship()).ToList();
                }
                else
                {
                    listToRemoveFrom = GlueState.Self.CurrentElement.NamedObjects;
                    listForIndexing = GlueState.Self.CurrentElement.NamedObjects
                        .Where(item => item.IsLayer == false && item.IsCollisionRelationship() == false)
                        .ToList();
                }
            }
        }


        private static void PostMoveActivity(TreeNode namedObjectTreeNode)
        {
            // do this before refreshing the tree nodes
            var tag = namedObjectTreeNode.Tag;

            GlueState.Self.CurrentElement.RefreshStatesToCustomVariables();

            UpdateCurrentElementTreeNode();

            var element = GlueState.Self.CurrentElement;
            var elementsToRegen = new List<IElement>();

            foreach (NamedObjectSave nos in ObjectFinder.Self.GetAllNamedObjectsThatUseElement(element))
            {
                nos.UpdateCustomProperties();
                var candidateToAdd = nos.GetContainer();

                if (!elementsToRegen.Contains(candidateToAdd))
                {

                    elementsToRegen.Add(candidateToAdd);
                }

            }

            foreach (var elementToRegen in elementsToRegen)
            {
                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(elementToRegen as GlueElement);
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

                    var absoluteFileName =
                        ProjectManager.MakeAbsolute(rfs.Name);

                    UpdateReactor.UpdateFile(absoluteFileName);

                    PluginManager.ReactToChangedBuiltFile(absoluteFileName);

                    UnreferencedFilesManager.Self.RefreshUnreferencedFiles(false);
                }

                PluginManager.ReactToFileBuildCommand(rfs);
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

                GlueCommands.Self.GenerateCodeCommands.GenerateStartupScreenCode();

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

            containerTreeNode.RefreshTreeNodes();

            CodeWriter.GenerateCode(containerTreeNode.SaveObject);
        }


        internal static void ErrorCheckClick()
        {
            ErrorManager.HandleCheckErrors();

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
            ElementExporter.ExportElement(GlueState.Self.CurrentElement, GlueState.Self.CurrentGlueProject);
        }

        static void ImportElementClick(object sender, EventArgs e)
        {
            ElementImporter.ShowImportElementUi();
        }


    }
}
