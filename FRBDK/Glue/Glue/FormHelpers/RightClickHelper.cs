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
using GlueFormsCore.FormHelpers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FlatRedBall.Glue.FormHelpers
{
    #region Enums

    public enum MenuShowingAction
    {
        RegularRightClick,
        RightButtonDrag
    }

    #endregion

    #region ITreeNode Interface

    public interface ITreeNode
    {
        object Tag { get; set; }

        ITreeNode Parent { get; }

        string Text { get; set; }

        IEnumerable<ITreeNode> Children { get; }

        #region "Is" methods

        public bool IsDirectoryNode()
        {
            if (Parent == null)
            {
                return false;
            }

            if (Tag != null)
            {
                return false;
            }

            if (Parent.IsRootEntityNode() || Parent.IsGlobalContentContainerNode())
                return true;


            if (Parent.IsFilesContainerNode() || Parent.IsDirectoryNode())
            {
                return true;
            }

            else
                return false;
        }

        public bool IsRootEntityNode() => Text == "Entities" && Parent == null;
        public bool IsRootScreenNode() => Text == "Screens" && Parent == null;


        public bool IsEntityNode()
        {
            return Tag is EntitySave;
        }

        public bool IsScreenNode() => Tag is ScreenSave;

        public bool IsGlobalContentContainerNode()
        {
            return Text == "Global Content Files" && Parent == null;
        }

        public bool IsFilesContainerNode()
        {
            var parentTreeNode = Parent;
            return Text == "Files" && parentTreeNode != null &&
                (parentTreeNode.IsEntityNode() || parentTreeNode.IsScreenNode());
        }

        public bool IsFolderInFilesContainerNode()
        {
            var parentTreeNode = Parent;

            return Tag == null && parentTreeNode != null &&
                (parentTreeNode.IsFilesContainerNode() || parentTreeNode.IsFolderInFilesContainerNode());

        }

        public bool IsElementNode() => Tag is GlueElement;
        public bool IsReferencedFile() => Tag is ReferencedFileSave;

        public bool IsRootObjectNode()
        {
            return Text == "Objects" && Tag == null;
        }

        public ITreeNode GetContainingElementTreeNode()
        {
            if (IsElementNode())
            {
                return this;
            }
            else if (Parent == null)
            {
                return null;
            }
            else
            {
                return Parent.GetContainingElementTreeNode();
            }
        }
        public bool IsRootLayerNode()
        {
            return Text == "Layers" &&
                Parent != null &&
                Tag == null &&
                Parent.IsRootNamedObjectNode();
        }

        public bool IsRootNamedObjectNode()
        {
            return Text == "Objects" &&
                Parent?.Tag is GlueElement;
        }

        public bool IsRootCustomVariablesNode()
        {
            return Parent?.Tag is GlueElement &&
                Text == "Variables";

        }

        public bool IsRootEventsNode()
        {
            return Parent != null &&
                (Parent.IsEntityNode() || Parent.IsScreenNode()) &&
                Text == "Events";
        }

        public bool IsNamedObjectNode()
        {
            return Tag is NamedObjectSave;
        }

        public bool IsCustomVariable()
        {
            return Tag is CustomVariable;
        }

        public bool IsCodeNode()
        {
            return Tag == null && Text.EndsWith(".cs");
        }

        public bool IsRootCodeNode()
        {

            return Text == "Code" &&
                Parent?.Tag is GlueElement;
        }

        public bool IsRootStateNode()
        {
            var parentTreeNode = Parent;
            return Text == "States" && parentTreeNode != null &&
                (parentTreeNode.IsEntityNode() || parentTreeNode.IsScreenNode());
        }

        public bool IsStateCategoryNode()
        {
            return Tag is StateSaveCategory;
        }

        public bool IsStateNode()
        {
            return Tag is StateSave;
        }

        public bool IsEventResponseTreeNode()
        {
            return Tag is EventResponseSave;

        }

        public bool IsFolderForGlobalContentFiles()
        {
            if (Parent == null)
            {
                return false;
            }

            var parent = Parent;

            while (parent != null)
            {
                if (parent.IsGlobalContentContainerNode())
                {
                    return true;
                }
                else
                {
                    parent = parent.Parent;
                }
            }

            return false;
        }

        public bool IsChildOfGlobalContent()
        {
            if (Parent == null)
            {
                return false;
            }

            if (Parent.IsGlobalContentContainerNode())
            {
                return true;
            }
            else
            {
                return Parent.IsChildOfGlobalContent();
            }
        }

        public bool IsChildOfRootEntityNode()
        {
            if (Parent == null)
            {
                return false;
            }
            else if (Parent.IsRootEntityNode())
            {
                return true;
            }
            else
            {
                return Parent.IsChildOfRootEntityNode();
            }
        }


        public bool IsFolderForEntities()
        {
            //TODO:  this fails when deleting a folder inside files.  We gotta fix that.  Try deleting the Palette folders in CreepBase in Baron

            var parent = Parent;

            if (parent == null)
            {
                return false;
            }

            if (parent.IsFilesContainerNode())
            {
                return false;
            }

            return Tag == null &&
                IsChildOfRootEntityNode();
        }

        #endregion

        void Remove(ITreeNode child);
        void Add(ITreeNode child);

        public ITreeNode Root => Parent?.Root ?? this;

        public string GetRelativePath()
        {

            #region Directory tree node
            if (((ITreeNode)this).IsDirectoryNode())
            {
                if (((ITreeNode)Parent).IsRootEntityNode())
                {
                    return "Entities/" + Text + "/";

                }
                if (((ITreeNode)Parent).IsRootScreenNode())
                {
                    return "Screens/" + Text + "/";

                }
                else if (((ITreeNode)Parent).IsGlobalContentContainerNode())
                {

                    string contentDirectory = ProjectManager.MakeAbsolute("GlobalContent/", true);

                    string returnValue = contentDirectory + Text;
                    if (((ITreeNode)this).IsDirectoryNode())
                    {
                        returnValue += "/";
                    }
                    // But we want to make this relative to the project, so let's do that
                    returnValue = ProjectManager.MakeRelativeContent(returnValue);

                    return returnValue;
                }
                else
                {
                    // It's a tree node, so make it have a "/" at the end
                    return Parent.GetRelativePath() + Text + "/";
                }
            }
            #endregion

            #region Global content container

            else if (((ITreeNode)this).IsGlobalContentContainerNode())
            {
                var returnValue = ProjectManager.ProjectBase.GetAbsoluteContentFolder() + "GlobalContent/";

                // But we want to make this relative to the project, so let's do that
                returnValue = ProjectManager.MakeRelativeContent(returnValue);



                return returnValue;
            }
            #endregion

            else if (((ITreeNode)this).IsFilesContainerNode())
            {
                // don't append "Files" here, because adding "Files" causes problems when searching for files

                //string valueToReturn = Parent.GetRelativePath() + this.Text + "/";
                string valueToReturn = Parent.GetRelativePath();



                return valueToReturn;
            }
            else if (((ITreeNode)this).IsFolderInFilesContainerNode())
            {
                return Parent.GetRelativePath() + Text + "/";
            }
            else if (((ITreeNode)this).IsReferencedFile())
            {
                string toReturn = Parent.GetRelativePath() + Text;
                toReturn = toReturn.Replace("/", "\\");
                return toReturn;
            }
            else
            {
                if (Parent == null)
                {
                    string valueToReturn = this.Text + "/";
                    return valueToReturn;
                }
                else
                {
                    string valueToReturn = Parent.GetRelativePath() + this.Text + "/";
                    return valueToReturn;

                }
            }
        }

        ITreeNode FindByName(string name);

        void RemoveGlobalContentTreeNodesIfDoesntExist(ITreeNode treeNode);

        ITreeNode FindByTagRecursive(object tag);

        void SortByTextConsideringDirectories();
    }

    #endregion

    public static class RightClickHelper
    {
        #region Fields

        static GeneralToolStripMenuItem addScreenToolStripMenuItem;

        static GeneralToolStripMenuItem addFileToolStripMenuItem;
        static GeneralToolStripMenuItem newFileToolStripMenuItem;
        static GeneralToolStripMenuItem existingFileToolStripMenuItem;


        static GeneralToolStripMenuItem openWithDEFAULTToolStripMenuItem;


        static GeneralToolStripMenuItem setAsStartUpScreenToolStripMenuItem;

        static GeneralToolStripMenuItem addObjectToolStripMenuItem;
        static GeneralToolStripMenuItem addEntityToolStripMenuItem;
        static GeneralToolStripMenuItem removeFromProjectToolStripMenuItem;

        static GeneralToolStripMenuItem addVariableToolStripMenuItem;

        static GeneralToolStripMenuItem editResetVariablesToolStripMenuItem;


        static GeneralToolStripMenuItem ignoreDirectoryToolStripMenuItem;

        static GeneralToolStripMenuItem setCreatedClassToolStripMenuItem;


        static GeneralToolStripMenuItem mMoveToTop;
        static GeneralToolStripMenuItem mMoveToBottom;

        static GeneralToolStripMenuItem mMoveUp;
        static GeneralToolStripMenuItem mMoveDown;
        static GeneralToolStripMenuItem mMakeRequiredAtStartup;
        static GeneralToolStripMenuItem mRebuildFile;

        static GeneralToolStripMenuItem mViewSourceInExplorer;

        static GeneralToolStripMenuItem mFindAllReferences;




        static GeneralToolStripMenuItem mDuplicate;

        static GeneralToolStripMenuItem mAddState;
        static GeneralToolStripMenuItem mAddStateCategory;

        static GeneralToolStripMenuItem mAddResetVariablesForPooling;

        static GeneralToolStripMenuItem mFillValuesFromDefault;

        static GeneralToolStripMenuItem mUseContentPipeline;

        static GeneralToolStripMenuItem mRemoveFromProjectQuick;
        static GeneralToolStripMenuItem mCreateNewFileForMissingFile;

        static GeneralToolStripMenuItem mViewFileLoadOrder;

        static GeneralToolStripMenuItem mCreateZipPackage;
        static GeneralToolStripMenuItem mExportElement;

        static GeneralToolStripMenuItem mAddEventMenuItem;

        static GeneralToolStripMenuItem mRefreshTreeNodesMenuItem;

        static GeneralToolStripMenuItem mCopyToBuildFolder;



        static GeneralToolStripMenuItem addLayeritem;
        #endregion

        //public static void PopulateRightClickItems(TreeNode targetNode, MenuShowingAction menuShowingAction = MenuShowingAction.RegularRightClick)
        //{
        //    menu.Items.Clear();

        //    var wrapper = TreeNodeWrapper.CreateOrNull(ElementViewWindow.TreeNodeDraggedOff);
        //    PopulateRightClickMenuItemsShared(new TreeNodeWrapper(targetNode), menuShowingAction, wrapper);

        //    PluginManager.ReactToTreeViewRightClick(targetNode, menu);
        //}

        static List<GeneralToolStripMenuItem> ListToAddTo = null;
        public static List<GeneralToolStripMenuItem> GetRightClickItems(ITreeNode targetNode, MenuShowingAction menuShowingAction, ITreeNode treeNodeMoving = null)
        {
            List<GeneralToolStripMenuItem> listToFill = new List<GeneralToolStripMenuItem>();

            ListToAddTo = listToFill;

            PopulateRightClickMenuItemsShared(targetNode, menuShowingAction, treeNodeMoving);

            PluginManager.ReactToTreeViewRightClick(targetNode, listToFill);

            ListToAddTo = null;

            return listToFill;
        }

        private static void PopulateRightClickMenuItemsShared(ITreeNode targetNode, MenuShowingAction menuShowingAction, ITreeNode sourceNode)
        {

            #region IsScreenNode

            if (targetNode.IsScreenNode())
            {
                if (menuShowingAction == MenuShowingAction.RightButtonDrag)
                {
                    if (sourceNode.IsEntityNode())
                    {
                        Add("Add Entity Instance", () => OnAddEntityInstanceClick(targetNode, sourceNode));
                        Add("Add Entity List", () => OnAddEntityListClick(targetNode, sourceNode));
                    }
                }
                else
                {
                    Add("Set as StartUp Screen", SetStartupScreen);
                    AddEvent(GlueState.Self.CurrentScreenSave.IsRequiredAtStartup
                        ? "Remove StartUp Requirement"
                        : "Make Required at StartUp", ToggleRequiredAtStartupClick);

                    AddEvent("Export Screen", ExportElementClick);

                    if(targetNode.Tag is ScreenSave screenSave && screenSave.Name == "Screens\\GameScreen")
                    {
                        AddEvent("Create Level Screen", (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog());
                    }

                    AddRemoveFromProjectItems();

                    AddSeparator();

                    AddEvent("Find all references to this", FindAllReferencesClick);
                    AddItem(mRefreshTreeNodesMenuItem);

                    if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
                    {
                        Add("Force Save Screen JSON", () => ForceSaveElementJson(targetNode.Tag as GlueElement));
                    }
                }
            }

            #endregion

            #region IsEntityNode

            else if (targetNode.IsEntityNode())
            {
                if (menuShowingAction == MenuShowingAction.RightButtonDrag && sourceNode.IsEntityNode())
                {
                    var mAddEntityInstance = new GeneralToolStripMenuItem("Add Entity Instance");
                    mAddEntityInstance.Click += (not, used) => OnAddEntityInstanceClick(targetNode, sourceNode);

                    var mAddEntityList = new GeneralToolStripMenuItem("Add Entity List");
                    mAddEntityList.Click += (not, used) => OnAddEntityListClick(targetNode, sourceNode);

                    AddItem(mAddEntityInstance);
                    AddItem(mAddEntityList);
                }
                else
                {
                    AddRemoveFromProjectItems();

                    AddSeparator();
                    mExportElement.Text = "Export Entity";
                    AddItem(mExportElement);
                    AddItem(mFindAllReferences);

                    EntitySave entitySave = targetNode.Tag as EntitySave;

                    if (entitySave.PooledByFactory)
                    {
                        AddItem(mAddResetVariablesForPooling);
                    }
                    AddItem(mRefreshTreeNodesMenuItem);
                }
            }

            #endregion

            #region IsFileContainerNode OR IsFolderInFilesContainerNode

            else if (targetNode.IsFilesContainerNode() || targetNode.IsFolderInFilesContainerNode())
            {
                AddItem(addFileToolStripMenuItem);
                Add("Add Folder", () => RightClickHelper.AddFolderClick(targetNode));
                AddSeparator();
                Add("View in explorer", () => RightClickHelper.ViewInExplorerClick(targetNode));
                if (targetNode.IsFolderInFilesContainerNode())
                {
                    Add("Delete Folder", () => DeleteFolderClick(targetNode));
                }
            }

            #endregion

            #region IsRootObjectNode

            else if (targetNode.IsRootObjectNode())
            {
                bool isSameObject = false;

                var elementForTreeNode = targetNode.GetContainingElementTreeNode()?.Tag;

                if (elementForTreeNode != null && sourceNode != null)
                {
                    isSameObject = elementForTreeNode == sourceNode?.Tag;
                }

                if (menuShowingAction == MenuShowingAction.RightButtonDrag && !isSameObject && sourceNode.IsEntityNode())
                {
                    var mAddEntityInstance = new GeneralToolStripMenuItem("Add Entity Instance");
                    mAddEntityInstance.Click += (not, used) => OnAddEntityInstanceClick(targetNode, sourceNode);

                    var mAddEntityList = new GeneralToolStripMenuItem("Add Entity List");
                    mAddEntityList.Click += (not, used) => OnAddEntityListClick(targetNode, sourceNode);

                    AddItem(mAddEntityInstance);
                    AddItem(mAddEntityList);
                }
                else
                {
                    AddItem(addObjectToolStripMenuItem);
                }
            }

            #endregion

            #region IsRootLayerNode

            else if (targetNode.IsRootLayerNode())
            {
                AddItem(addLayeritem);
            }


            #endregion

            #region IsGlobalContentContainerNode
            else if (targetNode.IsGlobalContentContainerNode())
            {
                AddItem(addFileToolStripMenuItem);
                Add("Add Folder", () => RightClickHelper.AddFolderClick(targetNode));
                Add("Re-Generate Code", () => HandleReGenerateCodeClick(targetNode));

                Add("View in explorer", () => RightClickHelper.ViewInExplorerClick(targetNode));

                AddItem(mViewFileLoadOrder);
            }
            #endregion

            #region IsRootEntityNode
            else if (targetNode.IsRootEntityNode())
            {
                AddItem(addEntityToolStripMenuItem);

                Add("Import Entity", () => ImportElementClick(targetNode));

                Add("Add Folder", () => RightClickHelper.AddFolderClick(targetNode));
            }
            #endregion

            #region IsRootScreenNode
            else if (targetNode.IsRootScreenNode())
            {
                AddItem(addScreenToolStripMenuItem);

                Add("Import Screen", () => ImportElementClick(targetNode));

            }
            #endregion

            #region IsRootCustomVariables

            else if (targetNode.IsRootCustomVariablesNode())
            {
                AddItem(addVariableToolStripMenuItem);
            }

            #endregion

            #region IsRootEventNode
            else if (targetNode.IsRootEventsNode())
            {
                AddItem(mAddEventMenuItem);
            }
            #endregion

            #region IsNamedObjectNode

            else if (targetNode.IsNamedObjectNode())
            {
                AddRemoveFromProjectItems();

                AddItem(editResetVariablesToolStripMenuItem);
                AddItem(mFindAllReferences);

                AddSeparator();

                AddItem(mDuplicate);

                AddSeparator();

                AddItem(mMoveToTop);
                AddItem(mMoveUp);
                AddItem(mMoveDown);
                AddItem(mMoveToBottom);

                AddSeparator();

                // In case something has changed which can happen mid wizard
                //var currentNamedObject = GlueState.Self.CurrentNamedObjectSave;
                var currentNamedObject = targetNode.Tag as NamedObjectSave;
                //GlueState.Self.CurrentNamedObjectSave;

                if (currentNamedObject.IsList &&
                    !string.IsNullOrEmpty(currentNamedObject.SourceClassGenericType) &&
                    !currentNamedObject.SetByDerived)
                {
                    var shouldAdd = true;
                    var genericEntityType = ObjectFinder.Self.GetEntitySave(currentNamedObject.SourceClassGenericType);
                    var isAbstractEntity = genericEntityType?.AllNamedObjects.Any(item => item.SetByDerived) == true;
                    if (isAbstractEntity)
                    {
                        shouldAdd = false;
                    }
                    if (shouldAdd)
                    {
                        AddItem(addObjectToolStripMenuItem);
                    }
                }
                else if (currentNamedObject?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.ShapeCollection)
                {
                    AddItem(addObjectToolStripMenuItem);
                }

            }

            #endregion

            #region IsReferencedFileNode
            else if (targetNode.IsReferencedFile())
            {
                Add("View in explorer", () => RightClickHelper.ViewInExplorerClick(targetNode));
                AddItem(mFindAllReferences);
                AddEvent("Copy path to clipboard", HandleCopyToClipboardClick);
                AddSeparator();

                AddItem(mCreateZipPackage);

                AddSeparator();

                AddRemoveFromProjectItems();

                AddItem(mUseContentPipeline);
                //AddItem(form.openWithDEFAULTToolStripMenuItem);

                ReferencedFileSave rfs = (ReferencedFileSave)targetNode.Tag;

                if (FileManager.GetExtension(rfs.Name) == "csv" || rfs.TreatAsCsv)
                {
                    AddSeparator();
                    AddItem(setCreatedClassToolStripMenuItem);
                    Add("Re-Generate Code", () => HandleReGenerateCodeClick(targetNode));
                }


                if (!string.IsNullOrEmpty(rfs.SourceFile) || rfs.SourceFileCache?.Count > 0)
                {
                    AddSeparator();
                    AddItem(mViewSourceInExplorer);
                    AddItem(mRebuildFile);
                }

                AddItem(mCopyToBuildFolder);

                if (!File.Exists(ProjectManager.MakeAbsolute(rfs.Name, true)))
                {
                    AddItem(mCreateNewFileForMissingFile);
                }
            }

            #endregion

            #region IsCustomVariable
            else if (targetNode.IsCustomVariable())
            {
                AddRemoveFromProjectItems();

                AddSeparator();


                AddItem(mFindAllReferences);

                AddSeparator();

                AddItem(mMoveToTop);
                AddItem(mMoveUp);
                AddItem(mMoveDown);
                AddItem(mMoveToBottom);
            }

            #endregion

            #region IsCodeNode
            else if (targetNode.IsCodeNode())
            {

                Add("View in explorer", () => RightClickHelper.ViewInExplorerClick(targetNode));
                Add("Re-Generate Code", () => HandleReGenerateCodeClick(targetNode));
            }

            #endregion

            #region IsRootCodeNode

            else if (targetNode.IsRootCodeNode())
            {
                Add("Re-Generate Code", () => HandleReGenerateCodeClick(targetNode));
            }


            #endregion

            #region IsDirectoryNode
            else if (targetNode.IsDirectoryNode())
            {
                //AddItem(form.viewInExplorerToolStripMenuItem);
                Add("View content folder", () => ViewContentFolderInExplorer(targetNode));
                Add("View code folder", () => ViewCodeFolderInExplorerClick(targetNode));
                AddSeparator();


                Add("Add Folder", () => RightClickHelper.AddFolderClick(targetNode));

                bool isEntityContainingFolder = targetNode.Root.IsRootEntityNode();

                if (isEntityContainingFolder)
                {
                    AddItem(addEntityToolStripMenuItem);

                    Add("Import Entity", () => ImportElementClick(targetNode));
                }
                else
                {
                    // If not in the Entities tree structure, assume global content
                    AddItem(addFileToolStripMenuItem);
                }

                AddSeparator();

                Add("Delete Folder", () => DeleteFolderClick(targetNode));
                if (isEntityContainingFolder)
                {
                    Add("Rename Folder", () => HandleRenameFolderClick(targetNode));
                }
            }

            #endregion

            #region IsStateListNode

            else if (targetNode.IsRootStateNode())
            {
                // We no longer support uncategorized states. They are a mess!
                //AddItem(mAddState);
                AddItem(mAddStateCategory);
            }

            #endregion

            #region IsStateCategoryNode
            else if (targetNode.IsStateCategoryNode())
            {
                AddItem(mAddState);
                AddRemoveFromProjectItems();

            }
            #endregion

            #region IsStateNode

            else if (targetNode.IsStateNode())
            {
                AddRemoveFromProjectItems();

                AddSeparator();
                AddItem(mDuplicate);
                AddSeparator();
                AddItem(mFillValuesFromDefault);
            }

            #endregion

            #region IsEventTreeNode

            else if (targetNode.IsEventResponseTreeNode())
            {
                AddRemoveFromProjectItems();

            }

            #endregion
        }


        static void Add(string text, Action action, string shortcutDisplay = null)
        {
            if (ListToAddTo != null)
            {
                var item = new GeneralToolStripMenuItem
                {
                    Text = text,
                    Click = (not, used) => action(),
                    ShortcutKeyDisplayString = shortcutDisplay
                };

                ListToAddTo.Add(item);
            }
            else
            {
                throw new NotImplementedException("Need a ListToAddTo assigned");
            }
        }

        static void AddEvent(string text, EventHandler eventHandler, string shortcutDisplay = null)
        {
            if (ListToAddTo != null)
            {
                var item = new GeneralToolStripMenuItem
                {
                    Text = text,
                    Click = eventHandler,
                    ShortcutKeyDisplayString = shortcutDisplay
                };
                ListToAddTo.Add(item);
            }
            else
            {
                throw new NotImplementedException("Need a ListToAddTo assigned");
            }
        }

        static void AddItem(GeneralToolStripMenuItem generalItem)
        {
            if (ListToAddTo != null)
            {
                ListToAddTo.Add(generalItem);
            }
            else
            {
                throw new NotImplementedException("Need a ListToAddTo assigned");
            }
        }

        static void AddSeparator()
        {
            if (ListToAddTo != null)
            {
                ListToAddTo.Add(new GeneralToolStripMenuItem
                {
                    Text = "-"
                });
            }
            else
            {
                throw new NotImplementedException("Need a ListToAddTo assigned");
            }
        }


        public static ReferencedFileSave AddSingleFile(string fullFileName, ref bool cancelled, IElement elementToAddTo = null)
        {
            return AddExistingFileManager.Self.AddSingleFile(fullFileName, ref cancelled, elementToAddTo: elementToAddTo);
        }

        public static void Initialize()
        {
            addScreenToolStripMenuItem = new GeneralToolStripMenuItem("Add Screen");
            addScreenToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog();

            setAsStartUpScreenToolStripMenuItem = new GeneralToolStripMenuItem("Set as StartUp Screen");
            setAsStartUpScreenToolStripMenuItem.Click += (not, used) =>
            {
                SetStartupScreen();
            };

            addEntityToolStripMenuItem = new GeneralToolStripMenuItem("Add Entity");
            addEntityToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewEntityDialog();



            addObjectToolStripMenuItem = new GeneralToolStripMenuItem();
            addObjectToolStripMenuItem.Text = "Add Object";
            addObjectToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewObjectDialog();

            existingFileToolStripMenuItem = new GeneralToolStripMenuItem();
            existingFileToolStripMenuItem.Text = "Existing File";
            existingFileToolStripMenuItem.Click += (not, used) => AddExistingFileManager.Self.AddExistingFileClick();

            setCreatedClassToolStripMenuItem = new GeneralToolStripMenuItem();
            setCreatedClassToolStripMenuItem.Text = "Set Created Class";
            setCreatedClassToolStripMenuItem.Click += (not, used) =>
            {
                CustomClassWindow ccw = new CustomClassWindow();

                ccw.SelectFile(GlueState.Self.CurrentReferencedFileSave);

                ccw.ShowDialog(MainGlueWindow.Self);

                GlueCommands.Self.ProjectCommands.SaveProjects();
                GluxCommands.Self.SaveGlux();
            };


            openWithDEFAULTToolStripMenuItem = new GeneralToolStripMenuItem();
            openWithDEFAULTToolStripMenuItem.Text = "Open with...";

            newFileToolStripMenuItem = new GeneralToolStripMenuItem();
            newFileToolStripMenuItem.Text = "New File";
            newFileToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewFileDialog();

            addFileToolStripMenuItem = new GeneralToolStripMenuItem();
            addFileToolStripMenuItem.DropDownItems.AddRange(new GeneralToolStripMenuItem[] {
            newFileToolStripMenuItem,
            existingFileToolStripMenuItem});

            addFileToolStripMenuItem.Text = "Add File";
            // this didn't do anything before I migrated it here. What does it do?
            //addFileToolStripMenuItem.Click += new System.EventHandler(this.addFileToolStripMenuItem_Click);


            removeFromProjectToolStripMenuItem = new GeneralToolStripMenuItem();
            removeFromProjectToolStripMenuItem.Text = "Remove from project";
            removeFromProjectToolStripMenuItem.Click += (not, used) => RightClickHelper.RemoveFromProjectToolStripMenuItem();

            mMoveToTop = new GeneralToolStripMenuItem("^^ Move To Top");
            mMoveToTop.ShortcutKeyDisplayString = "Alt+Shift+Up";
            mMoveToTop.Click += new System.EventHandler(MoveToTopClick);

            addVariableToolStripMenuItem = new GeneralToolStripMenuItem();
            addVariableToolStripMenuItem.Text = "Add Variable";
            addVariableToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog();

            editResetVariablesToolStripMenuItem = new GeneralToolStripMenuItem();
            editResetVariablesToolStripMenuItem.Text = "Edit Reset Variables";
            editResetVariablesToolStripMenuItem.Click += (not, used) =>
            {

                var nos = GlueState.Self.CurrentNamedObjectSave;

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

                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                }
            };


            mMoveUp = new GeneralToolStripMenuItem("^ Move Up");
            mMoveUp.ShortcutKeyDisplayString = "Alt+Up";
            mMoveUp.Click += new System.EventHandler(MoveUpClick);

            mMoveDown = new GeneralToolStripMenuItem("v Move Down");
            mMoveDown.ShortcutKeyDisplayString = "Alt+Down";
            mMoveDown.Click += new System.EventHandler(MoveDownClick);

            mMoveToBottom = new GeneralToolStripMenuItem("vv Move To Bottom");
            mMoveToBottom.ShortcutKeyDisplayString = "Alt+Shift+Down";
            mMoveToBottom.Click += new System.EventHandler(MoveToBottomClick);

            mMakeRequiredAtStartup = new GeneralToolStripMenuItem("Make Required at StartUp");
            mMakeRequiredAtStartup.Click += new EventHandler(ToggleRequiredAtStartupClick);

            mRebuildFile = new GeneralToolStripMenuItem("Rebuild File");
            mRebuildFile.Click += RebuildFileClick;

            mViewSourceInExplorer = new GeneralToolStripMenuItem("View source file in explorer");
            mViewSourceInExplorer.Click += new EventHandler(ViewSourceInExplorerClick);

            mFindAllReferences = new GeneralToolStripMenuItem("Find all references to this");
            mFindAllReferences.Click += new EventHandler(FindAllReferencesClick);







            mDuplicate = new GeneralToolStripMenuItem("Duplicate");
            mDuplicate.Click += new EventHandler(DuplicateClick);

            mAddState = new GeneralToolStripMenuItem("Add State");
            mAddState.Click += new EventHandler(AddStateClick);

            mAddStateCategory = new GeneralToolStripMenuItem("Add State Category");
            mAddStateCategory.Click += new EventHandler(AddStateCategoryClick);

            mAddResetVariablesForPooling = new GeneralToolStripMenuItem("Add Reset Variables For Pooling");
            mAddResetVariablesForPooling.Click += new EventHandler(mAddResetVariablesForPooling_Click);

            mFillValuesFromDefault = new GeneralToolStripMenuItem("Fill Values From Variables");
            mFillValuesFromDefault.Click += new EventHandler(mFillValuesFromVariables_Click);

            mRemoveFromProjectQuick = new GeneralToolStripMenuItem("Remove from project quick (ONLY IF YOU KNOW WHAT YOU'RE DOING!)");
            mRemoveFromProjectQuick.Click += new EventHandler(RemoveFromProjectQuick);

            mUseContentPipeline = new GeneralToolStripMenuItem("Toggle Use Content Pipeline");
            mUseContentPipeline.Click += new EventHandler(mUseContentPipeline_Click);

            mCreateNewFileForMissingFile = new GeneralToolStripMenuItem("Create new file for missing file");
            mCreateNewFileForMissingFile.Click += new EventHandler(CreateNewFileForMissingFileClick);

            mViewFileLoadOrder = new GeneralToolStripMenuItem("View File Order");
            mViewFileLoadOrder.Click += new EventHandler(ViewFileOrderClick);

            mCreateZipPackage = new GeneralToolStripMenuItem("Create Zip Package");
            mCreateZipPackage.Click += new EventHandler(CreateZipPackageClick);

            mExportElement = new GeneralToolStripMenuItem("Export Screen");
            mExportElement.Click += new EventHandler(ExportElementClick);


            mAddEventMenuItem = new GeneralToolStripMenuItem("Add Event");
            mAddEventMenuItem.Click += new EventHandler(AddEventClicked);

            mRefreshTreeNodesMenuItem = new GeneralToolStripMenuItem("Refresh UI");
            mRefreshTreeNodesMenuItem.Click += new EventHandler(OnRefreshTreeNodesClick);

            mCopyToBuildFolder = new GeneralToolStripMenuItem("Copy to build folder");
            mCopyToBuildFolder.Click += HandleCopyToBuildFolder;



            addLayeritem = new GeneralToolStripMenuItem("Add Layer");
            addLayeritem.Click += HandleAddLayerClick;
        }

        private static void SetStartupScreen()
        {
            var currentScreen = GlueState.Self.CurrentScreenSave;
            if (currentScreen != null)
            {
                GlueCommands.Self.GluxCommands.StartUpScreenName =
                    currentScreen.Name;
            }
        }

        private static void HandleReGenerateCodeClick(ITreeNode treeNode)
        {

            // re-generate regenerate re generate regenerate code re generate code re-generate code
            if (GlueState.Self.CurrentReferencedFileSave != null)
            {
                ReferencedFileSave rfs = GlueState.Self.CurrentReferencedFileSave;

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
            else if (GlueState.Self.CurrentElement != null)
            {
                // We used to allow regeneration of non-generated files
                // But people accidentally click this, and it means you have
                // to be careful when you right-click.  That sucks.  Now, Glue 
                // cannot regenerate the non-generated code file.


                var currentElement = GlueState.Self.CurrentElement;

                if (currentElement != null)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
                }


                foreach (VisualStudioProject project in ProjectManager.SyncedProjects)
                {
                    project.ClearPendingTranslations();

                    ((VisualStudioProject)project.CodeProject).AddCodeBuildItem(treeNode.Text);

                    project.PerformPendingTranslations();
                }
            }
            else // global content container?
            {
                GlobalContentCodeGenerator.UpdateLoadGlobalContentCode();
            }
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

        static void OnAddEntityListClick(ITreeNode nodeDroppedOn, ITreeNode nodeMoving)
        {
            DragDropManager.Self.CreateNewNamedObjectInElement(
                nodeDroppedOn.GetContainingElementTreeNode().Tag as GlueElement,
                nodeMoving.Tag as EntitySave,
                true);

            GlueCommands.Self.ProjectCommands.SaveProjects();
            GlueCommands.Self.GluxCommands.SaveGlux();

        }

        static void OnAddEntityInstanceClick(ITreeNode nodeDroppedOn, ITreeNode nodeMoving)
        {
            DragDropManager.DragDropTreeNode(
                 nodeDroppedOn,
                 nodeMoving);


            GlueCommands.Self.ProjectCommands.SaveProjects();
            GlueCommands.Self.GluxCommands.SaveGlux();
        }

        static void OnRefreshTreeNodesClick(object sender, EventArgs e)
        {
            GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
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
            IElement currentElement = GlueState.Self.CurrentElement;

            string failureMessage;
            bool isInvalid = NameVerifier.IsEventNameValid(resultName,
                currentElement, out failureMessage);

            if (isInvalid)
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

            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

            GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();

            GluxCommands.Self.SaveGlux();

            GlueState.Self.CurrentEventResponseSave = eventResponseSave;
        }

        static void ViewFileOrderClick(object sender, EventArgs e)
        {
            // view file order, viewfileorder, view files, viewfiles, viewfilelist, view file list
            ReferencedFileFlatListWindow rfflw = new ReferencedFileFlatListWindow();
            rfflw.Show(MainGlueWindow.Self);
            if (GlueState.Self.CurrentGlueProject != null)
            {
                rfflw.PopulateFrom(ProjectManager.GlueProjectSave.GlobalFiles);
            }
        }


        private static void AddRemoveFromProjectItems()
        {
            AddItem(removeFromProjectToolStripMenuItem);

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
                AddItem(mRemoveFromProjectQuick);
            }
        }

        static void mFillValuesFromVariables_Click(object sender, EventArgs e)
        {
            StateSave stateSave = GlueState.Self.CurrentStateSave;
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

                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

                GluxCommands.Self.SaveGlux();
            }
        }

        static void mUseContentPipeline_Click(object sender, EventArgs e)
        {
            ReferencedFileSave rfs = GlueState.Self.CurrentReferencedFileSave;

        }

        static void AddStateClick(object sender, EventArgs e)
        {
            // search: addstate, add new state, addnewstate, add state
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter a name for the new state";
            tiw.Text = "New State";


            DialogResult result = tiw.ShowDialog(MainGlueWindow.Self);

            if (result == DialogResult.OK)
            {
                var currentElement = GlueState.Self.CurrentElement;

                string whyItIsntValid;
                if (!NameVerifier.IsStateNameValid(tiw.Result, currentElement, GlueState.Self.CurrentStateSaveCategory, GlueState.Self.CurrentStateSave, out whyItIsntValid))
                {
                    GlueGui.ShowMessageBox(whyItIsntValid);
                }
                else
                {

                    StateSave newState = new StateSave();
                    newState.Name = tiw.Result;

                    var category = GlueState.Self.CurrentStateSaveCategory;

                    if (category != null)
                    {
                        category.States.Add(newState);
                    }
                    else
                    {
                        var element = currentElement;

                        element.States.Add(newState);
                    }

                    GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();

                    PluginManager.ReactToStateCreated(newState, category);

                    GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

                    GlueState.Self.CurrentStateSave = newState;

                    GluxCommands.Self.SaveGlux();
                    GlueCommands.Self.ProjectCommands.SaveProjects();
                }
            }
        }

        static void AddStateCategoryClick(object sender, EventArgs e)
        {
            GlueCommands.Self.DialogCommands.ShowAddNewCategoryDialog();
        }

        static void DuplicateClick(object sender, EventArgs e)
        {
            if (GlueState.Self.CurrentNamedObjectSave != null)
            {
                DuplicateCurrentNamedObject();
            }
            else if (GlueState.Self.CurrentStateSave != null)
            {
                DuplicateCurrentStateSave();
            }
        }

        private static void DuplicateCurrentNamedObject()
        {
            // Duplicate duplicate named object, copy named object, copy object
            NamedObjectSave namedObjectToDuplicate = GlueState.Self.CurrentNamedObjectSave;
            var element = ObjectFinder.Self.GetElementContaining(namedObjectToDuplicate);

            NamedObjectSave newNamedObject = namedObjectToDuplicate.Clone();

            #region Update the instance name

            newNamedObject.InstanceName = StringFunctions.IncrementNumberAtEnd(newNamedObject.InstanceName);
            if (newNamedObject.InstanceName.EndsWith("1") && StringFunctions.GetNumberAtEnd(newNamedObject.InstanceName) == 1)
            {
                newNamedObject.InstanceName = StringFunctions.IncrementNumberAtEnd(newNamedObject.InstanceName);
            }

            #endregion

            NamedObjectSave parentNos = element
                .NamedObjects
                .FirstOrDefault(item => item.ContainedObjects.Contains(namedObjectToDuplicate));

            if (parentNos != null)
            {
                bool IsShapeCollection(NamedObjectSave nos)
                {
                    return nos.SourceType == SourceType.FlatRedBallType &&
                        (nos.SourceClassType == "ShapeCollection" || nos.SourceClassType == "FlatRedBall.Math.Geometry.ShapeCollection");
                }

                if (parentNos != null && (parentNos.IsList || IsShapeCollection(parentNos)))
                {
                    int indexToInsertAt = 1 + parentNos.ContainedObjects.IndexOf(namedObjectToDuplicate);


                    while (element.GetNamedObjectRecursively(newNamedObject.InstanceName) != null)
                    {
                        newNamedObject.InstanceName = StringFunctions.IncrementNumberAtEnd(newNamedObject.InstanceName);
                    }

                    parentNos.ContainedObjects.Insert(indexToInsertAt, newNamedObject);
                }
            }
            else
            {
                int indexToInsertAt = 1 + element.NamedObjects.IndexOf(namedObjectToDuplicate);

                while (element.GetNamedObjectRecursively(newNamedObject.InstanceName) != null)
                {
                    newNamedObject.InstanceName = StringFunctions.IncrementNumberAtEnd(newNamedObject.InstanceName);
                }

                element.NamedObjects.Insert(indexToInsertAt, newNamedObject);
            }


            if (newNamedObject.SetByDerived)
            {
                GlueFormsCore.SetVariable.NamedObjectSaves.SetByDerivedSetLogic.ReactToChangedSetByDerived(
                    newNamedObject, element);
            }


            GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();

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

            if (GlueState.Self.CurrentElement != null)
            {
                GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
            }
            else if (GlueState.Self.CurrentReferencedFileSave != null)
            {
                GlueCommands.Self.RefreshCommands.RefreshGlobalContent();
            }
            CodeWriter.GenerateCode(GlueState.Self.CurrentElement);
            GlueCommands.Self.ProjectCommands.SaveProjects();
            GluxCommands.Self.SaveGlux();

        }

        static void FindAllReferencesClick(object sender, EventArgs e)
        {


            // find all references, findallreferences, find references
            ElementReferenceListWindow erlw = new ElementReferenceListWindow();
            erlw.Show();
            if (GlueState.Self.CurrentReferencedFileSave != null)
            {
                erlw.PopulateWithReferencesTo(GlueState.Self.CurrentReferencedFileSave);
            }
            else if (GlueState.Self.CurrentNamedObjectSave != null)
            {
                erlw.PopulateWithReferencesTo(GlueState.Self.CurrentNamedObjectSave, GlueState.Self.CurrentElement);
            }
            else if (GlueState.Self.CurrentCustomVariable != null)
            {
                erlw.PopulateWithReferencesTo(GlueState.Self.CurrentCustomVariable, GlueState.Self.CurrentElement);
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

        internal static async Task RemoveFromProjectToolStripMenuItem()
        {
            bool saveAndRegenerate = true;

            await RemoveFromProjectOptionalSaveAndRegenerate(saveAndRegenerate, true, true);
        }

        private static void RemoveFromProjectQuick(object sender, EventArgs e)
        {
            RemoveFromProjectOptionalSaveAndRegenerate(false, true, true);
        }

        private static async Task RemoveFromProjectOptionalSaveAndRegenerate(bool saveAndRegenerate = true, bool askAreYouSure = true, bool askToDeleteFiles = true)
        {
            // delete object, remove object, DeleteObject, RemoveObject, remove from project, 
            // remove from screen, remove from entity, remove file
            ///////////////////////////////EARLY OUT///////////////////////////////////////
            // This can now be called by pushing Delete, so we should check if deleting is valid
            var glueState = GlueState.Self;
            var currentObject =
                (object)glueState.CurrentNamedObjectSave ??
                glueState.CurrentStateSave ??
                glueState.CurrentStateSaveCategory ??
                glueState.CurrentReferencedFileSave ??
                glueState.CurrentCustomVariable ??
                glueState.CurrentEventResponseSave ??
                (object)glueState.CurrentEntitySave ??
                glueState.CurrentScreenSave;

            if (currentObject == null)
            {
                return;
            }
            //////////////////////////////END EARLY OUT/////////////////////////////////////

            var currentElementBeforeRemoval = GlueState.Self.CurrentElement;

            if (currentObject is NamedObjectSave asNos)
            {
                GlueCommands.Self.DialogCommands.AskToRemoveObject(asNos, saveAndRegenerate);
            }
            else
            {
                // Search terms: removefromproject, remove from project, remove file, remove referencedfilesave
                List<string> filesToRemove = new List<string>();

                if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
                {
                    #region Find out if the user really wants to remove this - don't ask if askAreYouSure is false
                    DialogResult reallyRemoveResult = DialogResult.Yes;

                    if (askAreYouSure)
                    {
                        string message = "Are you sure you want to remove this:\n\n" + currentObject.ToString();

                        reallyRemoveResult =
                            MessageBox.Show(message, "Remove?", MessageBoxButtons.YesNo);
                    }
                    #endregion

                    if (reallyRemoveResult == DialogResult.Yes)
                    {
                        GlueElement deletedElement = null;
                        #region If is NamedObjectSave
                        // handled above in AskToRemoveObject
                        #endregion

                        #region Else if is StateSave
                        if (GlueState.Self.CurrentStateSave != null)
                        {
                            var name = GlueState.Self.CurrentStateSave.Name;

                            GlueState.Self.CurrentElement.RemoveState(GlueState.Self.CurrentStateSave);

                            AskToRemoveCustomVariablesWithoutState(GlueState.Self.CurrentElement);

                            GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();

                            PluginManager.ReactToStateRemoved(GlueState.Self.CurrentElement, name);

                            GluxCommands.Self.SaveGlux();
                        }

                        #endregion

                        #region Else if is StateSaveCategory

                        else if (GlueState.Self.CurrentStateSaveCategory != null)
                        {
                            GlueCommands.Self.GluxCommands.RemoveStateSaveCategory(GlueState.Self.CurrentStateSaveCategory);
                        }

                        #endregion

                        #region Else if is ReferencedFileSave

                        else if (GlueState.Self.CurrentReferencedFileSave != null)
                        {
                            // the GluxCommand handles saving and regenerate internally, no need to do it twice
                            saveAndRegenerate = false;
                            var toRemove = GlueState.Self.CurrentReferencedFileSave;

                            if (GlueState.Self.Find.IfReferencedFileSaveIsReferenced(toRemove))
                            {
                                IElement element = GlueState.Self.CurrentElement;

                                // this could happen at the same time as file flushing, which can cause locks.  Therefore we need to add this as a task:
                                TaskManager.Self.AddOrRunIfTasked(() =>
                                {
                                    GluxCommands.Self.RemoveReferencedFile(toRemove, filesToRemove, regenerateCode: true);
                                },
                                "Remove file " + toRemove.ToString());

                            }

                        }
                        #endregion

                        #region Else if is CustomVariable

                        else if (GlueState.Self.CurrentCustomVariable != null)
                        {
                            GlueCommands.Self.GluxCommands.RemoveCustomVariable(
                                GlueState.Self.CurrentCustomVariable, filesToRemove);
                            //ProjectManager.RemoveCustomVariable(EditorLogic.CurrentCustomVariable);
                        }

                        #endregion

                        #region Else if is EventSave
                        else if (GlueState.Self.CurrentEventResponseSave != null)
                        {
                            var element = GlueState.Self.CurrentElement;
                            var eventResponse = GlueState.Self.CurrentEventResponseSave;
                            GlueState.Self.CurrentElement.Events.Remove(eventResponse);
                            PluginManager.ReactToEventResponseRemoved(element, eventResponse);
                            GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
                        }
                        #endregion

                        #region Else if is ScreenSave

                        // Then test higher if deep didn't get removed
                        else if (GlueState.Self.CurrentScreenSave != null)
                        {
                            var screenToRemove = GlueState.Self.CurrentScreenSave;
                            await TaskManager.Self.AddAsync(() =>
                            {
                                RemoveScreen(screenToRemove, filesToRemove);
                                deletedElement = screenToRemove;

                            }, "Removing Screen");

                        }

                        #endregion

                        #region Else if is EntitySave

                        else if (GlueState.Self.CurrentEntitySave != null)
                        {
                            var entityToRemove = GlueState.Self.CurrentEntitySave;
                            await TaskManager.Self.AddAsync(() =>
                            {
                                RemoveEntity(GlueState.Self.CurrentEntitySave, filesToRemove);
                                //ProjectManager.RemoveEntity(EditorLogic.CurrentEntitySave);
                                deletedElement = entityToRemove;
                            }, "Removing Entity");

                        }

                        #endregion


                        #region Files were deleted and the user wants to be asked to delete

                        if (filesToRemove.Count != 0 && askToDeleteFiles)
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

                            if(lbw.ClickedOption is DialogResult result)
                            {
                                if (result == DialogResult.OK || result == DialogResult.Yes)
                                {
                                    await TaskManager.Self.AddAsync(() =>
                                    {
                                        foreach (var file in filesToRemove)
                                        {
                                            FilePath filePath = ProjectManager.MakeAbsolute(file);
                                            // This file may have been removed
                                            // in windows explorer, and now removed
                                            // from Glue.  Check to prevent a crash.

                                            GlueCommands.Self.ProjectCommands.RemoveFromProjects(filePath, false);

                                            if (result == DialogResult.Yes && filePath.Exists())
                                            {
                                                FileHelper.DeleteFile(filePath.FullPath);
                                            }
                                        }
                                        GluxCommands.Self.ProjectCommands.SaveProjects();

                                    }, "Removing files");
                                }

                            }
                        }

                        #endregion

                        if(deletedElement == null && GlueState.Self.CurrentElement == null)
                        {
                            GlueState.Self.CurrentElement = currentElementBeforeRemoval;
                        }

                        await TaskManager.Self.AddAsync(() =>
                        {
                            // Nodes aren't directly removed in the code above. Instead, 
                            // a "refresh nodes" method is called, which may remove unneeded
                            // nodes, but event raising is suppressed. Therefore, we have to explicitly 
                            // do it here:
                            if (deletedElement != null)
                            {
                                GlueCommands.Self.RefreshCommands.RefreshTreeNodes();
                                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(deletedElement);

                            }
                            else if (glueState.CurrentElement != null)
                            {
                                GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(glueState.CurrentElement);
                            }
                            else
                            {
                                GlueCommands.Self.RefreshCommands.RefreshGlobalContent();
                            }
                        }, "Refreshing tree nodes");


                        if (saveAndRegenerate)
                        {
                            if (GlueState.Self.CurrentElement != null)
                            {
                                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(GlueState.Self.CurrentElement);
                            }
                            else if (GlueState.Self.CurrentReferencedFileSave != null)
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

                GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(message,
                    () => GlueCommands.Self.GluxCommands.RemoveEntity(entityToRemove, filesThatCouldBeRemoved));
            }
            else
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
            for (int i = 0; i < element.CustomVariables.Count; i++)
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

        internal static void AddFolderClick(ITreeNode targetNode)
        {
            // addfolder, add folder, add new folder, addnewfolder
            TextInputWindow tiw = new TextInputWindow();
            tiw.Message = "Enter new folder name";
            tiw.Text = "New Folder";
            DialogResult result = tiw.ShowDialog(MainGlueWindow.Self);

            if (result == DialogResult.OK)
            {
                string folderName = tiw.Result;
                GlueCommands.Self.ProjectCommands.AddDirectory(folderName, targetNode);

                var newNode = targetNode.Children.FirstOrDefault(item => item.Text == folderName);

                GlueState.Self.CurrentTreeNode = newNode;

                targetNode.SortByTextConsideringDirectories();


            }
        }

        internal static void ViewInExplorerClick(ITreeNode targetNode)
        {

            if (GlueState.Self.CurrentGlueProject == null)
            {
                MessageBox.Show("You must first load or create a Glue project");
            }
            else
            {


                // view in explorer
                string locationToShow = "";

                if (GlueState.Self.CurrentReferencedFileSave != null)
                {
                    ReferencedFileSave rfs = GlueState.Self.CurrentReferencedFileSave;

                    locationToShow = ProjectManager.MakeAbsolute(rfs.Name);

                }
                else if (targetNode.IsDirectoryNode() || targetNode.IsGlobalContentContainerNode())
                {
                    locationToShow = ProjectManager.MakeAbsolute(targetNode.GetRelativePath(), true);
                }
                else if (targetNode.IsFilesContainerNode() || targetNode.IsFolderInFilesContainerNode())
                {
                    string relativePath = targetNode.GetRelativePath();

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
                else if (targetNode.Text.EndsWith(".cs"))
                {
                    locationToShow = ProjectManager.MakeAbsolute(targetNode.GetRelativePath(), false);

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

                if (isFile)
                {
                    Process.Start("explorer.exe", "/select," + locationToShow);
                }
                else
                {
                    Process.Start("explorer.exe", "/root," + locationToShow);
                }
            }
        }

        static void ViewContentFolderInExplorer(ITreeNode targetNode)
        {

            if (targetNode.IsDirectoryNode())
            {
                string locationToShow = ProjectManager.MakeAbsolute(targetNode.GetRelativePath(), true);

                if (System.IO.Directory.Exists(locationToShow))
                {
                    locationToShow = locationToShow.Replace("/", "\\");
                    Process.Start("explorer.exe", "/select," + locationToShow);
                }
                else
                {
                    if (GlueState.Self.CurrentElement != null)
                    {
                        string screenOrEntity = "screen";
                        if (GlueState.Self.CurrentEntitySave != null)
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

        public static void DeleteFolderClick(ITreeNode targetNode)
        {
            // delete folder, deletefolder

            bool forceContent = false;

            if (targetNode.IsChildOfGlobalContent() ||
                targetNode.IsFolderInFilesContainerNode())
            {
                forceContent = true;
            }

            string absolutePath = ProjectManager.MakeAbsolute(targetNode.GetRelativePath(), forceContent);

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
                if (targetNode.IsChildOfRootEntityNode() && targetNode.IsFolderForEntities())
                {
                    // We have to remove all contained Entities
                    // from the project.
                    List<EntitySave> allEntitySaves = new List<EntitySave>();
                    GetAllEntitySavesIn(targetNode, allEntitySaves);

                    foreach (EntitySave entitySave in allEntitySaves)
                    {
                        GlueState.Self.CurrentEntitySave = entitySave;
                        RemoveFromProjectOptionalSaveAndRegenerate(entitySave == allEntitySaves[allEntitySaves.Count - 1], false, false);

                    }
                }
                else if (targetNode.IsFolderInFilesContainerNode())
                {
                    List<ReferencedFileSave> allReferencedFileSaves = new List<ReferencedFileSave>();
                    GetAllReferencedFileSavesIn(targetNode, allReferencedFileSaves);

                    foreach (ReferencedFileSave rfs in allReferencedFileSaves)
                    {
                        GlueState.Self.CurrentReferencedFileSave = rfs;
                        // I guess we won't ask to delete here, but maybe eventually we want to?
                        RemoveFromProjectOptionalSaveAndRegenerate(rfs == allReferencedFileSaves[allReferencedFileSaves.Count - 1], false, false);
                    }
                }

                System.IO.Directory.Delete(absolutePath, true);
                GlueCommands.Self.RefreshCommands.RefreshTreeNodes();
                GlueCommands.Self.RefreshCommands.RefreshDirectoryTreeNodes();
            }
        }

        static void HandleRenameFolderClick(ITreeNode treeNode)
        {
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

            if (shouldPerformMove && !Directory.Exists(newDirectoryNameAbsolute))
            {
                try
                {
                    Directory.CreateDirectory(newDirectoryNameAbsolute);
                }
                catch (Exception ex)
                {
                    PluginManager.ReceiveError(ex.ToString());
                    shouldPerformMove = false;
                }
            }

            if (shouldPerformMove)
            {
                var allContainedEntities = GlueState.Self.CurrentGlueProject.Entities
                    .Where(entity => entity.Name.StartsWith(directoryRenaming)).ToList();

                newDirectoryNameRelative = newDirectoryNameRelative.Replace('/', '\\');

                bool didAllSucceed = true;

                foreach (var entity in allContainedEntities)
                {
                    bool succeeded = GlueCommands.Self.GluxCommands.MoveEntityToDirectory(entity, newDirectoryNameRelative);

                    if (!succeeded)
                    {
                        didAllSucceed = false;
                        break;
                    }
                }

                if (didAllSucceed)
                {
                    treeNode.Text = inputWindow.Result;

                    GlueCommands.Self.ProjectCommands.MakeGeneratedCodeItemsNested();
                    GlueCommands.Self.GenerateCodeCommands.GenerateAllCode();

                    GluxCommands.Self.SaveGlux();
                    GlueCommands.Self.ProjectCommands.SaveProjects();

                }
            }
        }

        static void GetAllEntitySavesIn(ITreeNode treeNode, List<EntitySave> allEntitySaves)
        {
            foreach (var subNode in treeNode.Children)
            {
                if (subNode.IsDirectoryNode())
                {
                    GetAllEntitySavesIn(subNode, allEntitySaves);
                }
                else if (subNode.Tag is EntitySave asEntitySave)
                {
                    allEntitySaves.Add(asEntitySave);
                }
            }
        }

        static void GetAllReferencedFileSavesIn(ITreeNode treeNode, List<ReferencedFileSave> allReferencedFileSaves)
        {
            foreach (var subNode in treeNode.Children)
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

        static void ViewCodeFolderInExplorerClick(ITreeNode targetNode)
        {
            if (targetNode.IsDirectoryNode())
            {
                string locationToShow = FileManager.RelativeDirectory + targetNode.GetRelativePath();

                locationToShow = locationToShow.Replace("/", "\\");
                Process.Start("explorer.exe", "/select," + locationToShow);
            }
        }


        private static void MoveToTopClick(object sender, EventArgs e)
        {
            MoveToTop();

        }

        public static void MoveToTop()
        {
            TaskManager.Self.Add(() =>
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
                        PostMoveActivity();
                    }
                }
            }, "Moving to top", TaskExecutionPreference.Asap);
        }

        private static void MoveUpClick(object sender, EventArgs e)
        {
            MoveSelectedObjectUp();
        }

        private static void MoveDownClick(object sender, EventArgs e)
        {
            MoveSelectedObjectDown();
        }

        public static void MoveSelectedObjectUp()
        {
            int direction = -1;
            MoveObjectInDirection(direction);
        }

        public static void MoveSelectedObjectDown()
        {
            int direction = 1;
            MoveObjectInDirection(direction);
        }

        private static void MoveObjectInDirection(int direction)
        {
            TaskManager.Self.Add(() =>
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
                    if (newIndexInListForIndexing >= 0 && newIndexInListForIndexing < listForIndexing.Count)
                    {
                        objectToMoveBeforeOrAfter = listForIndexing[newIndexInListForIndexing];
                    }

                    //int newIndex = index + direction;
                    int newIndex = listToRemoveFrom.IndexOf(objectToMoveBeforeOrAfter);

                    if (newIndex >= 0 && newIndex < listToRemoveFrom.Count)
                    {
                        listToRemoveFrom.Remove(objectToRemove);

                        listToRemoveFrom.Insert(newIndex, objectToRemove);

                        PostMoveActivity();
                    }
                }

            }, "Moving object up or down", TaskExecutionPreference.Asap);
        }


        private static void MoveToBottomClick(object sender, EventArgs e)
        {
            MoveToBottom();
        }

        public static void MoveToBottom()
        {
            TaskManager.Self.Add(() =>
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
                        PostMoveActivity();
                    }
                }
            }, "Moving to bottom", TaskExecutionPreference.Asap);
        }

        private static void GetObjectAndListForMoving(out object objectToMove,
            out IList listToRemoveFrom, out IList listForIndexing)
        {
            objectToMove = null;
            listToRemoveFrom = null;
            listForIndexing = null;
            if (GlueState.Self.CurrentCustomVariable != null)
            {
                objectToMove = GlueState.Self.CurrentCustomVariable;
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
                else if (currentNamedObject.IsLayer)
                {
                    listToRemoveFrom = GlueState.Self.CurrentElement.NamedObjects;
                    listForIndexing = GlueState.Self.CurrentElement.NamedObjects.Where(item => item.IsLayer).ToList();
                }
                else if (currentNamedObject.IsCollisionRelationship())
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


        private static async void PostMoveActivity()
        {
            // do this before refreshing the tree nodes
            var currentCustomVariable = GlueState.Self.CurrentCustomVariable;
            var currentNamedObjectSave = GlueState.Self.CurrentNamedObjectSave;

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

            await System.Threading.Tasks.Task.Delay(10);
            // I think the variables are complete remade. I could make it preserve them, but it's easier to do this:
            if (currentCustomVariable != null)
            {
                //GlueState.Self.CurrentCustomVariable = null;
                GlueState.Self.CurrentCustomVariable = currentCustomVariable;
            }
            else if (currentNamedObjectSave != null)
            {
                //GlueState.Self.CurrentNamedObjectSave = null;
                GlueState.Self.CurrentNamedObjectSave = currentNamedObjectSave;
            }

            GluxCommands.Self.SaveGlux();
        }

        public static void SetExternallyBuiltFileIfHigherThanCurrent(string directoryOfFile, bool performSave)
        {
            if (directoryOfFile == null)
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

        private static async void RebuildFileClick(object sender, EventArgs e)
        {
            // search terms: rebuild file
            ReferencedFileSave rfs = GlueState.Self.CurrentReferencedFileSave;

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

                    await UpdateReactor.UpdateFile(absoluteFileName);

                    PluginManager.ReactToChangedBuiltFile(absoluteFileName);

                    UnreferencedFilesManager.Self.RefreshUnreferencedFiles(false);
                }

                PluginManager.ReactToFileBuildCommand(rfs);
            }
        }

        private static void ViewSourceInExplorerClick(object sender, EventArgs e)
        {
            ReferencedFileSave rfs = GlueState.Self.CurrentReferencedFileSave;

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

        static void ToggleRequiredAtStartupClick(object sender, EventArgs e)
        {
            ScreenSave screenSave = GlueState.Self.CurrentScreenSave;

            List<ScreenSave> screensToRefresh = new List<ScreenSave>();

            if (screenSave != null)
            {
                bool isAlreadyRequired = screenSave.IsRequiredAtStartup;

                if (isAlreadyRequired)
                {
                    screenSave.IsRequiredAtStartup = false;
                    screensToRefresh.Add(screenSave);
                }
                else
                {
                    // We gotta un-require any other Screen that is required since right now we only
                    // support one required Screen
                    foreach (ScreenSave screenInProject in ProjectManager.GlueProjectSave.Screens)
                    {
                        if (screenInProject.IsRequiredAtStartup)
                        {
                            screensToRefresh.Add(screenInProject);
                            screenInProject.IsRequiredAtStartup = false;
                            break;
                        }
                    }
                    screenSave.IsRequiredAtStartup = true;
                }

                foreach (var screen in screensToRefresh)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(screen);
                    GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(screen);
                }


                GlueCommands.Self.GenerateCodeCommands.GenerateStartupScreenCode();

                GluxCommands.Self.SaveGlux();
            }
        }

        static void CreateNewFileForMissingFileClick(object sender, EventArgs e)
        {
            var rfs = GlueState.Self.CurrentReferencedFileSave;
            string extension = FileManager.GetExtension(rfs.Name);

            AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromExtension(extension);

            string resultNameInFolder = FileManager.RemoveExtension(FileManager.RemovePath(rfs.Name));
            string directory = FileManager.GetDirectory(ProjectManager.MakeAbsolute(rfs.Name, true));

            PluginManager.CreateNewFile(
                ati, false, directory, resultNameInFolder);

            GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
        }

        private static void UpdateCurrentElementTreeNode()
        {
            var element = GlueState.Self.CurrentElement;
            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element);
            // Vic says - this seems wasteful and dishonest. I don't think this call
            // should generate code. Need to search the usage of this and see if
            // anywhere depends on it. If so, add explicit calls to generate code, then eventually
            // remove this.
            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
        }


        internal static void ErrorCheckClick()
        {
            ErrorManager.HandleCheckErrors();

        }


        public static void CreateZipPackageClick(object sender, EventArgs e)
        {
            // Create zip, create package, create zip package, create package zip
            ReferencedFileSave rfs = GlueState.Self.CurrentReferencedFileSave;

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

        static void ImportElementClick(ITreeNode targetTreeNode)
        {
            ElementImporter.ShowImportElementUi(targetTreeNode);
        }


        private static void ForceSaveElementJson(GlueElement glueElement)
        {
            var glueDirectory = GlueState.Self.CurrentGlueProjectDirectory;
            var fileName = glueElement.Name + ".";
            if(glueElement is ScreenSave)
            {
                fileName += GlueProjectSave.ScreenExtension;
            }
            else
            {
                fileName += GlueProjectSave.EntityExtension;
            }

            var destination = glueDirectory + fileName;

            var serialized = JsonConvert.SerializeObject(glueElement, Formatting.Indented);

            FileWatchManager.IgnoreNextChangeOnFile(destination);

            FileManager.SaveText(serialized, destination);
        }
    }
}
