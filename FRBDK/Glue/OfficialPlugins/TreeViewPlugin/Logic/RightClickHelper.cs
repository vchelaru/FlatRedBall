using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using FlatRedBall.Glue.VSHelpers.Projects;
using Glue;
using FlatRedBall.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Controls;
using System.IO;
using System.Diagnostics;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Utilities;
using System.Collections;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.IO.Zip;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.SaveClasses.Helpers;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Factories;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.ViewModels;
using GlueFormsCore.FormHelpers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.Utilities;
using System.Windows.Media.Imaging;
using L = Localization;
using FlatRedBall.Glue.Plugins.Interfaces;
using OfficialPlugins.TreeViewPlugin.Logic;

namespace FlatRedBall.Glue.FormHelpers;

#region Enums

public enum MenuShowingAction
{
    RegularRightClick,
    RightButtonDrag
}

#endregion


public static class RightClickHelper
{
    #region Fields/Properties

    static GeneralToolStripMenuItem addFileToolStripMenuItem;
    static GeneralToolStripMenuItem newFileToolStripMenuItem;
    static GeneralToolStripMenuItem existingFileToolStripMenuItem;

    
    static GeneralToolStripMenuItem openWithDEFAULTToolStripMenuItem;

    static GeneralToolStripMenuItem setAsStartUpScreenToolStripMenuItem;

    static GeneralToolStripMenuItem addObjectToolStripMenuItem;
    
    static GeneralToolStripMenuItem removeFromProjectToolStripMenuItem;

    static GeneralToolStripMenuItem editResetVariablesToolStripMenuItem;

    static GeneralToolStripMenuItem setCreatedClassToolStripMenuItem;

    static GeneralToolStripMenuItem mMoveToTop;
    static GeneralToolStripMenuItem mMoveToBottom;

    static GeneralToolStripMenuItem mMoveUp;
    static GeneralToolStripMenuItem mMoveDown;
    static GeneralToolStripMenuItem mMakeRequiredAtStartup;

    static GeneralToolStripMenuItem mViewSourceInExplorer;

    static GeneralToolStripMenuItem mFindAllReferences;

    static GeneralToolStripMenuItem mDuplicate;

    static GeneralToolStripMenuItem mAddState;
    static GeneralToolStripMenuItem mAddStateCategory;

    static GeneralToolStripMenuItem mAddResetVariablesForPooling;

    static GeneralToolStripMenuItem mFillValuesFromDefault;

    static GeneralToolStripMenuItem mRemoveFromProjectQuick;
    static GeneralToolStripMenuItem mCreateNewFileForMissingFile;

    static GeneralToolStripMenuItem mCreateZipPackage;
    static GeneralToolStripMenuItem mExportElement;

    static GeneralToolStripMenuItem mAddEventMenuItem;

    static GeneralToolStripMenuItem mRefreshTreeNodesMenuItem;

    static GeneralToolStripMenuItem mCopyToBuildFolder;

    static GeneralToolStripMenuItem addLayeritem;


    static List<GeneralToolStripMenuItem> ListToAddTo = null;
    #endregion


    #region Images

    static System.Windows.Controls.Image BookmarkImage;
    static System.Windows.Controls.Image CollisionRelationshipImage;
    static System.Windows.Controls.Image DerivedEntity;
    static System.Windows.Controls.Image EntityImage;
    static System.Windows.Controls.Image FolderImage;

    static System.Windows.Controls.Image ScreenImage;
    static System.Windows.Controls.Image StartupScreenImage;

    static bool HasCreatedImages = false;
    private static void CreateImages()
    {
        if (!HasCreatedImages)
        {

            BookmarkImage = MakeImage("/Content/Icons/StarFilled.png");
            CollisionRelationshipImage = MakeImage("/Content/Icons/icon_collisions.png");
            DerivedEntity = MakeImage("/Content/Icons/icon_entity_derived.png");
            EntityImage = MakeImage("/Content/Icons/icon_entity.png");
            FolderImage = MakeImage("/Content/Icons/icon_folder.png");
            ScreenImage = MakeImage("/Content/Icons/icon_screen.png");
            StartupScreenImage = MakeImage("/Content/Icons/icon_screen_startup.png");

            HasCreatedImages = true;
        }
        System.Windows.Controls.Image MakeImage(string sourceName)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(sourceName, UriKind.Relative);
            bitmapImage.EndInit();

            var toReturn = new System.Windows.Controls.Image()
            {
                Source = bitmapImage
            };


            return toReturn;
        }

    }


    #endregion

    private static void PopulateRightClickMenuItemsShared(ITreeNode targetNode, MenuShowingAction menuShowingAction, ITreeNode draggedNode)
    {

        #region IsScreenNode

        if (targetNode.IsScreenNode())
        {
            var screen = targetNode.Tag as ScreenSave;
            if (menuShowingAction == MenuShowingAction.RightButtonDrag)
            {
                if (draggedNode?.IsEntityNode() == true)
                {
                    Add(L.Texts.EntityAddInstance, () => OnAddEntityInstanceClick(targetNode, draggedNode));
                    Add(L.Texts.EntityListAdd, () => OnAddEntityListClick(targetNode, draggedNode));
                }
            }
            else
            {
                Add("Set as StartUp Screen (first screen when running)", SetStartupScreen, image: StartupScreenImage);
                AddEvent(screen.IsRequiredAtStartup
                    ? L.Texts.ScreenRemoveRequirement
                    : L.Texts.MakeRequiredAtStartup, ToggleRequiredAtStartupClick);

                AddEvent(L.Texts.ScreenExport, ExportElementClick);

                if(targetNode.Tag is ScreenSave { Name: "Screens\\GameScreen" })
                {
                    AddEvent("Create Level Screen", (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog());
                }

                AddRemoveFromProjectItems();

                AddSeparator();
                Add("Rename", () =>
                {
                    if (SelectionLogic.Current.CurrentNode?.IsEditable == true)
                    {
                        SelectionLogic.Current.CurrentNode.IsEditing = true;
                    }
                });
                AddEvent("Find all references to this", FindAllReferencesClick);
                AddItem(mRefreshTreeNodesMenuItem);

                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
                {
                    Add(L.Texts.ScreenSaveForceJson, () => ForceSaveElementJson(targetNode.Tag as GlueElement));
                    Add(L.Texts.ViewInExplorer, () => ViewElementInExplorer(targetNode.Tag as GlueElement), image: FolderImage);
                }
                Add(L.Texts.FileOpenCs, () => OpenCsFile(targetNode.Tag as GlueElement));
            }
        }

        #endregion

        #region IsEntityNode

        else if (targetNode.IsEntityNode())
        {
            if (menuShowingAction == MenuShowingAction.RightButtonDrag && draggedNode?.IsEntityNode() == true)
            {

                var mAddEntityList = new GeneralToolStripMenuItem(L.Texts.EntityListAdd);
                mAddEntityList.Click += (not, used) => OnAddEntityListClick(targetNode, draggedNode);

                Add(L.Texts.EntityAddInstance, () => OnAddEntityInstanceClick(targetNode, draggedNode));
                AddItem(mAddEntityList);
            }
            else
            {
                EntitySave entitySave = targetNode.Tag as EntitySave;

                Add("Add Derived Entity", () => ShowAddDerivedEntityDialog(entitySave), image: DerivedEntity);

                AddSeparator();

                AddRemoveFromProjectItems();

                AddSeparator();
                Add("Rename", () =>
                {
                    if (SelectionLogic.Current.CurrentNode?.IsEditable == true)
                    {
                        SelectionLogic.Current.CurrentNode.IsEditing = true;
                    }
                });
                mExportElement.Text = "Export Entity";
                AddItem(mExportElement);
                AddItem(mFindAllReferences);


                if (entitySave.PooledByFactory)
                {
                    AddItem(mAddResetVariablesForPooling);
                }
                AddItem(mRefreshTreeNodesMenuItem);

                if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
                {
                    Add(L.Texts.ViewInExplorer, () => ViewElementInExplorer(targetNode.Tag as GlueElement), image: FolderImage);
                }

                Add(L.Texts.FileOpenCs, () => OpenCsFile(targetNode.Tag as GlueElement));
            }
        }

        #endregion

        #region IsFileContainerNode OR IsFolderInFilesContainerNode

        else if (targetNode.IsFilesContainerNode() || targetNode.IsFolderInFilesContainerNode())
        {
            AddItem(addFileToolStripMenuItem);
            Add(L.Texts.FolderAdd, () => RightClickHelper.AddFolderClick(targetNode), image: FolderImage);
            AddSeparator();
            Add(L.Texts.ViewInExplorer, () => RightClickHelper.ViewInExplorerClick(targetNode), image: FolderImage);
            Add(L.Texts.CopyPathClipboard, () => HandleCopyToClipboardClick(targetNode));

            AddSeparator();
            if (targetNode.IsFolderInFilesContainerNode())
            {
                Add("Delete Folder", () => GlueCommands.Self.GluxCommands.DeleteFolderClick(targetNode));
            }
        }

        #endregion

        #region IsRootObjectNode

        else if (targetNode.IsRootNamedObjectNode())
        {
            bool isSameObject = false;

            var elementForTreeNode = targetNode.GetContainingElementTreeNode()?.Tag;

            if (elementForTreeNode != null && draggedNode != null)
            {
                isSameObject = elementForTreeNode == draggedNode?.Tag;
            }

            if (menuShowingAction == MenuShowingAction.RightButtonDrag && !isSameObject && draggedNode.IsEntityNode())
            {
                var mAddEntityList = new GeneralToolStripMenuItem(L.Texts.EntityListAdd);
                mAddEntityList.Click += (not, used) => OnAddEntityListClick(targetNode, draggedNode);

                Add(L.Texts.EntityAddInstance, () => OnAddEntityInstanceClick(targetNode, draggedNode));
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

        #region IsRootCollisionRelationships node

        else if(targetNode.IsRootCollisionRelationshipsNode())
        {
            Add(L.Texts.RightClick_Add_Collision_Relationship, 
                () => AddNewCollisionRelationshipTo(GlueState.Self.CurrentElement),
                image:CollisionRelationshipImage);
        }

        #endregion

        #region IsGlobalContentContainerNode
        else if (targetNode.IsGlobalContentContainerNode())
        {
            AddItem(addFileToolStripMenuItem);
            Add(L.Texts.FolderAdd, () => RightClickHelper.AddFolderClick(targetNode), image: FolderImage);
            Add("Re-Generate Code", () => HandleReGenerateCodeClick(targetNode));

            Add(L.Texts.ViewInExplorer, () => RightClickHelper.ViewInExplorerClick(targetNode), image: FolderImage);
        }
        #endregion

        #region IsRootEntityNode
        else if (targetNode.IsRootEntityNode())
        {
            Add("Add Entity", () => GlueCommands.Self.DialogCommands.ShowAddNewEntityDialog(), image: EntityImage);

            Add(L.Texts.FolderAdd, () => RightClickHelper.AddFolderClick(targetNode), image: FolderImage);

            Add(L.Texts.EntityImport, () => ImportElementClick(targetNode));
        }
        #endregion

        #region IsRootScreenNode
        else if (targetNode.IsRootScreenNode())
        {
            Add(L.Texts.ScreenAdd, () => GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog(), image:ScreenImage);

            Add(L.Texts.FolderAdd, () => RightClickHelper.AddFolderClick(targetNode), image: FolderImage);

            Add(L.Texts.ScreenImport, () => ImportElementClick(targetNode));

        }
        #endregion

        #region IsRootCustomVariables

        else if (targetNode.IsRootCustomVariablesNode())
        {
            var targetElement = targetNode.GetContainingElementTreeNode()?.Tag as GlueElement;

            if(targetElement == null)
            {
                // for Vic to figure out what's up... This should never be null because the target node 
                System.Diagnostics.Debugger.Break();
            }

            Add(L.Texts.VariableAdd, () => 
                GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog(CustomVariableType.New, container: targetElement));

            
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
            AddSeparator();

            AddItem(mFindAllReferences);

            Add(L.Texts.GoToDefinition, () => GlueCommands.Self.DialogCommands.GoToDefinitionOfSelection());

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
                bool IsAbstract(GlueElement element) => element?.AllNamedObjects.Any(item => item.SetByDerived) == true;
                var isAbstractEntity = IsAbstract(genericEntityType);
                if (isAbstractEntity)
                {
                    // It's okay if it's abstract, so long as there are derived entities that are not abstract:

                    var derived = ObjectFinder.Self.GetAllDerivedElementsRecursive(genericEntityType);

                    var hasNonAbstract = derived.Any(item => !IsAbstract(item));

                    shouldAdd = hasNonAbstract;
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
            Add(L.Texts.ViewInExplorer, () => RightClickHelper.ViewInExplorerClick(targetNode), image: FolderImage);
            Add(L.Texts.Open, () => HandleOpen(targetNode));
            AddItem(mFindAllReferences);

            var rfs = targetNode.Tag as ReferencedFileSave;

            var topLevelCopyNameItem = Add("Copy Name...", () => { });
            {
                var oldList = ListToAddTo;
                ListToAddTo = topLevelCopyNameItem.DropDownItems;

                Add(L.Texts.CopyPathClipboard, () => HandleCopyToClipboardClick(targetNode));
                var name = rfs.GetInstanceName();
                Add($"Copy Code Instance Name ({name})", () =>
                {
                    Clipboard.SetText(name);


                });

                var strippedName = FileManager.RemovePath(FileManager.RemoveExtension(rfs.Name));
                Add($"Copy Stripped Name ({strippedName})", () =>
                {
                    Clipboard.SetText(strippedName);
                });

                ListToAddTo = oldList;
            }
            AddSeparator();

            AddItem(mCreateZipPackage);

            AddSeparator();

            AddRemoveFromProjectItems();

            if (FileManager.GetExtension(rfs.Name) == "csv" || rfs.TreatAsCsv)
            {
                AddSeparator();
                AddItem(setCreatedClassToolStripMenuItem);
                Add("Re-Generate Code", () => HandleReGenerateCodeClick(targetNode));
            }


            AddItem(mCopyToBuildFolder);

            var filePath = GlueCommands.Self.GetAbsoluteFilePath(rfs);

            if (!filePath.Exists())
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
            AddItem(mDuplicate);
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

            Add(L.Texts.ViewInExplorer, () => RightClickHelper.ViewInExplorerClick(targetNode), image: FolderImage);
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
            Add(L.Texts.ViewContentFolder, () => ViewContentFolderInExplorer(targetNode));

            if(!targetNode.IsChildOfGlobalContent())
            {
                Add("View code folder", () => ViewCodeFolderInExplorerClick(targetNode));
            }
            Add(L.Texts.CopyPathClipboard, () => HandleCopyToClipboardClick(targetNode));

            AddSeparator();


            Add(L.Texts.FolderAdd, () => RightClickHelper.AddFolderClick(targetNode), image: FolderImage);

            bool isEntityContainingFolder = targetNode.Root.IsRootEntityNode();
            bool isScreenContainingFolder = targetNode.Root.IsRootScreenNode();

            if (isEntityContainingFolder)
            {
                Add("Add Entity", () => GlueCommands.Self.DialogCommands.ShowAddNewEntityDialog(), image: EntityImage);

                Add(L.Texts.EntityImport, () => ImportElementClick(targetNode));
            }
            else if(isScreenContainingFolder)
            {
                Add("Add Screen", () => GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog(), image: ScreenImage);
            }
            else
            {
                // If not in the Entities tree structure, assume global content
                AddItem(addFileToolStripMenuItem);
            }

            AddSeparator();

            Add(L.Texts.FolderDelete, () => GluxCommands.Self.DeleteFolderClick(targetNode));
            if (isEntityContainingFolder || isScreenContainingFolder)
            {
                Add(L.Texts.FolderRename, () => HandleRenameFolderClick(targetNode));
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

        #region All Nodes

        if(menuShowingAction == MenuShowingAction.RegularRightClick)
        {
            AddSeparator();
            Add("Bookmark", () => PluginManager.CallPluginMethod("Tree View Plugin", "AddBookmark", targetNode), image: BookmarkImage);
        }

        #endregion
    }

    private static async void AddNewCollisionRelationshipTo(GlueElement currentElement)
    {
        var viewModel = new AddObjectViewModel();

        viewModel.ForcedElementToAddTo = currentElement;
        viewModel.SourceType = SourceType.FlatRedBallType;
        viewModel.SourceClassType = "CollisionRelationship";

        viewModel.ObjectName = "CollisionRelationshipInstance";
        while(currentElement.GetNamedObjectRecursively(viewModel.ObjectName) != null)
        {
            viewModel.ObjectName = StringFunctions.IncrementNumberAtEnd(viewModel.ObjectName);
        }
        viewModel.SelectedAti = AvailableAssetTypes.Self.AllAssetTypes
            .FirstOrDefault(item => item.QualifiedRuntimeTypeName.QualifiedType == "FlatRedBall.Math.Collision.CollisionRelationship");
        var newNamedObject = await GlueCommands.Self.GluxCommands.AddNewNamedObjectToSelectedElementAsync(viewModel);
        GlueState.Self.CurrentNamedObjectSave = newNamedObject;
    }


    private static void HandleOpen(ITreeNode targetNode)
    {
        if(targetNode.Tag is ReferencedFileSave rfs)
        {
            GlueCommands.Self.FileCommands.OpenReferencedFileInDefaultProgram(rfs);
        }
    }

    private static void OpenCsFile(GlueElement glueElement)
    {
        var customCodeFile = GlueCommands.Self.FileCommands.GetCustomCodeFilePath(glueElement);
        if(customCodeFile?.Exists() == true)
        {
            GlueCommands.Self.FileCommands.Open(customCodeFile);
        }
    }

    public static List<GeneralToolStripMenuItem> GetRightClickItems(ITreeNode targetNode, MenuShowingAction menuShowingAction, ITreeNode treeNodeMoving = null)
    {
        List<GeneralToolStripMenuItem> listToFill = new List<GeneralToolStripMenuItem>();

        ListToAddTo = listToFill;

        PopulateRightClickMenuItemsShared(targetNode, menuShowingAction, treeNodeMoving);

        PluginManager.ReactToTreeViewRightClick(targetNode, listToFill);

        ListToAddTo = null;

        return listToFill;
    }


    #region Utility Methods

    static GeneralToolStripMenuItem Add(string text, Action action, string shortcutDisplay = null, System.Windows.Controls.Image image = null)
    {
        if (ListToAddTo != null)
        {
            var item = new GeneralToolStripMenuItem
            {
                Text = text,
                Click = (not, used) => action(),
                ShortcutKeyDisplayString = shortcutDisplay
            };

            item.Image = image;

            ListToAddTo.Add(item);

            return item;
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

    #endregion

    public static void Initialize()
    {
        CreateImages();

        setAsStartUpScreenToolStripMenuItem = new GeneralToolStripMenuItem("Set as StartUp Screen (first screen when running)");
        setAsStartUpScreenToolStripMenuItem.Click += (not, used) =>
        {
            SetStartupScreen();
        };

        addObjectToolStripMenuItem = new GeneralToolStripMenuItem();
        addObjectToolStripMenuItem.Text = L.Texts.ObjectAdd;
        addObjectToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewObjectDialog();

        existingFileToolStripMenuItem = new GeneralToolStripMenuItem();
        existingFileToolStripMenuItem.Text = "Existing File(s)";
        existingFileToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddExistingFileDialog();

        setCreatedClassToolStripMenuItem = new GeneralToolStripMenuItem();
        setCreatedClassToolStripMenuItem.Text = L.Texts.CreatedClass;
        setCreatedClassToolStripMenuItem.Click += (not, used) =>
        {
            CustomClassWindow ccw = new CustomClassWindow();

            ccw.SelectFile(GlueState.Self.CurrentReferencedFileSave);

            ccw.ShowDialog(MainGlueWindow.Self);

            GlueCommands.Self.ProjectCommands.SaveProjects();
            GluxCommands.Self.SaveProjectAndElements();
        };

        openWithDEFAULTToolStripMenuItem = new GeneralToolStripMenuItem();
        openWithDEFAULTToolStripMenuItem.Text = L.Texts.OpenWith;

        newFileToolStripMenuItem = new GeneralToolStripMenuItem();
        newFileToolStripMenuItem.Text = L.Texts.FileNew;
        newFileToolStripMenuItem.Click += async (not, used) => await GlueCommands.Self.DialogCommands.ShowAddNewFileDialogAsync();

        addFileToolStripMenuItem = new GeneralToolStripMenuItem();
        addFileToolStripMenuItem.DropDownItems.AddRange(new GeneralToolStripMenuItem[] {
            newFileToolStripMenuItem,
            existingFileToolStripMenuItem});

        addFileToolStripMenuItem.Text = L.Texts.FileAdd;

        removeFromProjectToolStripMenuItem = new GeneralToolStripMenuItem();
        removeFromProjectToolStripMenuItem.Text = "Remove from project";
        removeFromProjectToolStripMenuItem.Click += (not, used) => RightClickHelper.RemoveFromProjectToolStripMenuItem();

        mMoveToTop = new GeneralToolStripMenuItem($"^^ {L.Texts.MoveToTop}");
        mMoveToTop.ShortcutKeyDisplayString = "Alt+Shift+Up";
        mMoveToTop.Click += MoveToTopClick;

        editResetVariablesToolStripMenuItem = new GeneralToolStripMenuItem();
        editResetVariablesToolStripMenuItem.Text = L.Texts.VariableResetEdit;
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
                GluxCommands.Self.SaveProjectAndElements();

                GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
            }
        };


        mMoveUp = new GeneralToolStripMenuItem($"^ {L.Texts.MoveUp}")
        {
            ShortcutKeyDisplayString = "Alt+Up"
        };
        mMoveUp.Click += MoveUpClick;

        mMoveDown = new GeneralToolStripMenuItem($"v {L.Texts.MoveDown}")
        {
            ShortcutKeyDisplayString = "Alt+Down"
        };
        mMoveDown.Click += MoveDownClick;

        mMoveToBottom = new GeneralToolStripMenuItem($"vv {L.Texts.MoveBottom}")
        {
            ShortcutKeyDisplayString = "Alt+Shift+Down"
        };
        mMoveToBottom.Click += MoveToBottomClick;

        mMakeRequiredAtStartup = new GeneralToolStripMenuItem(L.Texts.MakeRequiredAtStartup);
        mMakeRequiredAtStartup.Click += ToggleRequiredAtStartupClick;

        mViewSourceInExplorer = new GeneralToolStripMenuItem(L.Texts.ViewSourceExplorer);
        mViewSourceInExplorer.Click += ViewSourceInExplorerClick;

        mFindAllReferences = new GeneralToolStripMenuItem("Find all references to this");
        mFindAllReferences.Click += FindAllReferencesClick;

        mDuplicate = new GeneralToolStripMenuItem(L.Texts.Duplicate);
        mDuplicate.Click += DuplicateClick;

        mAddState = new GeneralToolStripMenuItem(L.Texts.StateAdd);
        mAddState.Click += AddStateClick;

        mAddStateCategory = new GeneralToolStripMenuItem("Add State Category");
        mAddStateCategory.Click += AddStateCategoryClick;

        mAddResetVariablesForPooling = new GeneralToolStripMenuItem(L.Texts.ResetVariablesPoolingAdd);
        mAddResetVariablesForPooling.Click += mAddResetVariablesForPooling_Click;

        mFillValuesFromDefault = new GeneralToolStripMenuItem(L.Texts.VariableFillValues);
        mFillValuesFromDefault.Click += mFillValuesFromVariables_Click;

        mRemoveFromProjectQuick = new GeneralToolStripMenuItem(L.Texts.RemoveFromProjectQuick);
        mRemoveFromProjectQuick.Click += RemoveFromProjectQuick;

        mCreateNewFileForMissingFile = new GeneralToolStripMenuItem(L.Texts.FileCreateForMissing);
        mCreateNewFileForMissingFile.Click += CreateNewFileForMissingFileClick;

        mCreateZipPackage = new GeneralToolStripMenuItem("Create Zip Package");
        mCreateZipPackage.Click += CreateZipPackageClick;

        mExportElement = new GeneralToolStripMenuItem(L.Texts.ScreenExport);
        mExportElement.Click += ExportElementClick;

        mAddEventMenuItem = new GeneralToolStripMenuItem(L.Texts.EventAdd);
        mAddEventMenuItem.Click += AddEventClicked;

        mRefreshTreeNodesMenuItem = new GeneralToolStripMenuItem(L.Texts.RefreshUi);
        mRefreshTreeNodesMenuItem.Click += OnRefreshTreeNodesClick;

        mCopyToBuildFolder = new GeneralToolStripMenuItem(L.Texts.CopyBuildFolder);
        mCopyToBuildFolder.Click += HandleCopyToBuildFolder;



        addLayeritem = new GeneralToolStripMenuItem(L.Texts.LayerAdd);
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
                GluxCommands.Self.SaveProjectAndElements();
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

        viewModel.ForcedElementToAddTo = GlueState.Self.CurrentElement;
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

    static void HandleCopyToClipboardClick(ITreeNode node)
    {
        if (node.Tag is ReferencedFileSave rfs)
        {
            var filePath = GlueCommands.Self.GetAbsoluteFilePath(rfs);
            var absolute = filePath.FullPath;
            Clipboard.SetText(absolute);
        }
        else if(node.IsFolderInFilesContainerNode() || node.IsFolderForGlobalContentFiles() || node.IsFilesContainerNode())
        {
            var filePath = node.GetRelativeFilePath();
            var absolute = GlueCommands.Self.GetAbsoluteFilePath(filePath, forceAsContent:true).FullPath;
            Clipboard.SetText(absolute);
        }
        else if(node.IsDirectoryNode())
        {
            var filePath = node.GetRelativeFilePath();
            var absolute = GlueCommands.Self.GetAbsoluteFilePath(filePath, forceAsContent: false).FullPath;
            Clipboard.SetText(absolute);
        }
    }

    static async void OnAddEntityListClick(ITreeNode nodeDroppedOn, ITreeNode nodeMoving)
    {
        await DragDropManager.Self.CreateNewNamedObjectInElement(
            nodeDroppedOn.GetContainingElementTreeNode().Tag as GlueElement,
            nodeMoving.Tag as EntitySave,
            true);

        GlueCommands.Self.ProjectCommands.SaveProjects();
        GlueCommands.Self.GluxCommands.SaveProjectAndElements();

    }

    static async void OnAddEntityInstanceClick(ITreeNode nodeDroppedOn, ITreeNode nodeMoving)
    {
        await DragDropManager.DragDropTreeNode(
            nodeDroppedOn,
            nodeMoving);


        GlueCommands.Self.ProjectCommands.SaveProjects();
        GlueCommands.Self.GluxCommands.SaveProjectAndElements();
    }

    static void OnRefreshTreeNodesClick(object sender, EventArgs e) =>
        GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();

    static void AddEventClicked(object sender, EventArgs e) =>
        GlueCommands.Self.DialogCommands.ShowAddNewEventDialog(GlueState.Self.CurrentElement);


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

            if(GlueState.Self.CurrentReferencedFileSave?.IsCreatedByWildcard == true)
            {
                removeFromProjectToolStripMenuItem.Text = $"Delete [{GlueState.Self.CurrentReferencedFileSave.Name}]";
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

        var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(
            String.Format(L.Texts.QuestionFillValuesDefault, stateSave.Name),
            L.Texts.FillValuesDefault);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            for (int i = 0; i < element.CustomVariables.Count; i++)
            {
                CustomVariable cv = element.CustomVariables[i];

                stateSave.SetValue(cv.Name, cv.DefaultValue);
            }

            MainGlueWindow.Self.PropertyGrid.Refresh();

            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();

            GluxCommands.Self.SaveProjectAndElements();
        }
    }

    static void AddStateClick(object sender, EventArgs e)
    {
        GlueCommands.Self.DialogCommands.ShowAddNewStateDialog();
    }

    static void AddStateCategoryClick(object sender, EventArgs e)
    {
        GlueCommands.Self.DialogCommands.ShowAddNewCategoryDialog();
    }

    static async void DuplicateClick(object sender, EventArgs e)
    {
        if(GlueState.Self.CurrentCustomVariable != null)
        {
            await GlueCommands.Self.GluxCommands.DuplicateAsync(GlueState.Self.CurrentCustomVariable);
        }
        else if (GlueState.Self.CurrentNamedObjectSave != null)
        {
            await GlueCommands.Self.GluxCommands.CopyNamedObjectIntoElement(GlueState.Self.CurrentNamedObjectSave, GlueState.Self.CurrentElement);
        }
        else if (GlueState.Self.CurrentStateSave != null)
        {
            DuplicateCurrentStateSave();
        }
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
        GluxCommands.Self.SaveProjectAndElements();

    }

    private static void FindAllReferencesClick(object sender, EventArgs e)
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

    internal static async Task RemoveFromProjectToolStripMenuItem()
    {
        bool saveAndRegenerate = true;

        await GlueCommands.Self.GluxCommands.RemoveFromProjectOptionalSaveAndRegenerate(saveAndRegenerate, true, true);
    }

    private static void RemoveFromProjectQuick(object sender, EventArgs e)
    {
        GlueCommands.Self.GluxCommands.RemoveFromProjectOptionalSaveAndRegenerate(false, true, true);
    }







    internal static void AddFolderClick(ITreeNode targetNode)
    {
        // addfolder, add folder, add new folder, addnewfolder
        CustomizableTextInputWindow tiw = new()
        {
            Message = L.Texts.NewFolderEnter,
        };

        if (tiw.ShowDialog() is true)
        {
            string folderName = tiw.Result;
            GlueCommands.Self.ProjectCommands.AddDirectory(folderName, targetNode);

            var newNode = targetNode.Children.FirstOrDefault(item => item.Text == folderName);

            GlueState.Self.CurrentTreeNode = newNode;

            targetNode.SortByTextConsideringDirectories();
        }
    }

    internal static void ViewElementInExplorer(GlueElement element)
    {
        var extension = element is ScreenSave
            ? GlueProjectSave.ScreenExtension
            : GlueProjectSave.EntityExtension;
        var filePath = GlueState.Self.CurrentGlueProjectDirectory + element.Name + "." + extension;
        GlueCommands.Self.FileCommands.ViewInExplorer(filePath);
    }

    internal static void ViewInExplorerClick(ITreeNode targetNode)
    {

        if (GlueState.Self.CurrentGlueProject == null)
        {
            GlueCommands.Self.DialogCommands.ShowMessageBox(L.Texts.ProjectGlueLoadOrCreateFirst);
        }
        else
        {
            // view in explorer
            string locationToShow = "";

            if (GlueState.Self.CurrentReferencedFileSave != null)
            {
                var rfs = GlueState.Self.CurrentReferencedFileSave;
                locationToShow = GlueCommands.Self.GetAbsoluteFileName(rfs);

            }
            else if (targetNode.IsDirectoryNode() || targetNode.IsGlobalContentContainerNode())
            {
                locationToShow = GlueCommands.Self.GetAbsoluteFileName(targetNode.GetRelativeFilePath(), true);
                // global content may not have yet been created. If not, just show the level above:
                if(targetNode.IsGlobalContentContainerNode() && !File.Exists(locationToShow))
                {
                    // actually, we should just create the directory. Maybe the user wants to put a file there?
                    System.IO.Directory.CreateDirectory(locationToShow);
                }
            }
            else if (targetNode.IsFilesContainerNode() || targetNode.IsFolderInFilesContainerNode())
            {
                string relativePath = targetNode.GetRelativeFilePath();

                // Victor Chelaru April 11, 2013
                // RelativePath already includes "Screens/"
                // So I'm not sure why I was prepending that
                // here.
                //if (EditorLogic.CurrentScreenSave != null)
                //{
                //    relativePath = "Screens/" + relativePath;
                //}

                locationToShow = GlueCommands.Self.GetAbsoluteFileName(relativePath, true);

                // If the user hasn't put any files in this element, then this directory may not exist.  Therefore,
                // let's create it.
                if (!Directory.Exists(locationToShow))
                {
                    Directory.CreateDirectory(locationToShow);
                }
            }
            else if (targetNode.Text.EndsWith(".cs"))
            {
                var relativePath = targetNode.GetRelativeFilePath();

                locationToShow = GlueCommands.Self.GetAbsoluteFileName(relativePath, false);
            }

            string extension = FileManager.GetExtension(locationToShow);
            GlueCommands.Self.FileCommands.ViewInExplorer(locationToShow);
        }
    }

    static void ViewContentFolderInExplorer(ITreeNode targetNode)
    {

        if (targetNode.IsDirectoryNode())
        {
            string locationToShow = GlueCommands.Self.GetAbsoluteFileName(targetNode.GetRelativeFilePath(), true);

            if (System.IO.Directory.Exists(locationToShow))
            {
                locationToShow = locationToShow.Replace("/", "\\");
                Process.Start("explorer.exe", "/select," + locationToShow);
            }
            else
            {
                if (GlueState.Self.CurrentElement != null)
                {
                    var screenOrEntity = (GlueState.Self.CurrentEntitySave != null) ? "Entities" : "Screens";
                    GlueCommands.Self.DialogCommands.ShowMessageBox(String.Format(L.Texts.FolderGlueMadeWhenFileAdded, screenOrEntity, screenOrEntity));
                }
                else
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox(L.Texts.FolderGlueNotMadeLackFiles);
                }
            }
        }
    }


    static void HandleRenameFolderClick(ITreeNode treeNode)
    {
        CustomizableTextInputWindow inputWindow = new()
        {
            Message = L.Texts.NewFolderEnter,
            Result = treeNode.Text
        };

        if (inputWindow.ShowDialog() is true)
        {
            GlueCommands.Self.GluxCommands.RenameFolder(treeNode, inputWindow.Result);
        }
    }




    static void ViewCodeFolderInExplorerClick(ITreeNode targetNode)
    {
        if (targetNode.IsDirectoryNode())
        {
            string locationToShow = FileManager.RelativeDirectory + targetNode.GetRelativeFilePath();

            locationToShow = locationToShow.Replace("/", "\\");
            Process.Start("explorer.exe", "/select," + locationToShow);
        }
    }


    private static void MoveToTopClick(object sender, EventArgs e)
    {
        GlueCommands.Self.GluxCommands.MoveToTop();

    }


    private static async void MoveUpClick(object sender, EventArgs e)
    {
        await GlueCommands.Self.GluxCommands.MoveSelectedObjectUp();
    }

    private static async void MoveDownClick(object sender, EventArgs e)
    {
        await GlueCommands.Self.GluxCommands.MoveSelectedObjectDown();
    }




    private static void MoveToBottomClick(object sender, EventArgs e)
    {
        GlueCommands.Self.GluxCommands.MoveToBottom();
    }







    private static void ViewSourceInExplorerClick(object sender, EventArgs e)
    {
        ReferencedFileSave rfs = GlueState.Self.CurrentReferencedFileSave;

        if (rfs != null)
        {
            if (string.IsNullOrEmpty(rfs.SourceFile))
            {
                GlueCommands.Self.DialogCommands.ShowMessageBox(L.Texts.ObjectNullSource, L.Texts.ErrorOpeningFolder);
            }
            else
            {

                string file = FileManager.Standardize(GlueCommands.Self.GetAbsoluteFileName(rfs.SourceFile, true)).Replace("/", "\\");

                Process.Start("explorer.exe", "/select," + file
                );
            }
        }
    }

    static void ToggleRequiredAtStartupClick(object sender, EventArgs e)
    {
        var screenSave = GlueState.Self.CurrentScreenSave;
        var screensToRefresh = new List<ScreenSave>();

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

            GluxCommands.Self.SaveProjectAndElements();
        }
    }

    static void CreateNewFileForMissingFileClick(object sender, EventArgs e)
    {
        var rfs = GlueState.Self.CurrentReferencedFileSave;
        string extension = FileManager.GetExtension(rfs.Name);

        AssetTypeInfo ati = AvailableAssetTypes.Self.GetAssetTypeFromExtension(extension);

        string resultNameInFolder = FileManager.RemoveExtension(FileManager.RemovePath(rfs.Name));
        string directory = FileManager.GetDirectory(GlueCommands.Self.GetAbsoluteFileName(rfs));

        PluginManager.CreateNewFile(
            ati, false, directory, resultNameInFolder);

        GlueCommands.Self.RefreshCommands.RefreshCurrentElementTreeNode();
    }


    public static void CreateZipPackageClick(object sender, EventArgs e)
    {
        // Create zip, create package, create zip package, create package zip
        ReferencedFileSave rfs = GlueState.Self.CurrentReferencedFileSave;

        string fileName = Zipper.CreateZip(rfs);

        if (string.IsNullOrEmpty(fileName))
        {
            GlueCommands.Self.DialogCommands.ShowMessageBox(String.Format(L.Texts.ErrorCouldNotPackageFileRelative, rfs.Name));
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

    private static void ShowAddDerivedEntityDialog(EntitySave entitySave)
    {
        var vm = GlueCommands.Self.DialogCommands.CreateAddNewEntityViewModel();
        vm.SelectedBaseEntity = entitySave.Name;
        GlueCommands.Self.DialogCommands.ShowAddNewEntityDialog(vm);
    }
}
