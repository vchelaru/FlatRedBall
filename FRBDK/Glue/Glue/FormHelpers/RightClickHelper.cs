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

namespace FlatRedBall.Glue.FormHelpers;

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

    public bool IsRootEntityNode() => Text.Equals("Entities", StringComparison.OrdinalIgnoreCase) && Parent == null;
    public bool IsRootScreenNode() => Text.Equals("Screens", StringComparison.OrdinalIgnoreCase) && Parent == null;


    public bool IsEntityNode()
    {
        return Tag is EntitySave;
    }

    public bool IsScreenNode() => Tag is ScreenSave;

    public bool IsGlobalContentContainerNode()
    {
        return Text.Equals("Global Content Files", StringComparison.OrdinalIgnoreCase) && Parent == null;
    }

    public bool IsFilesContainerNode()
    {
        var parentTreeNode = Parent;
        return Text.Equals("Files", StringComparison.OrdinalIgnoreCase) && parentTreeNode != null && (parentTreeNode.IsEntityNode() || parentTreeNode.IsScreenNode());
    }

    public bool IsFolderInFilesContainerNode()
    {
        var parentTreeNode = Parent;

        return Tag == null && parentTreeNode != null &&
               (parentTreeNode.IsFilesContainerNode() || parentTreeNode.IsFolderInFilesContainerNode());

    }

    public bool IsElementNode() => Tag is GlueElement;
    public bool IsReferencedFile() => Tag is ReferencedFileSave;

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
        return Text.Equals("Layers", StringComparison.OrdinalIgnoreCase) &&
               Parent != null &&
               Tag == null &&
               Parent.IsRootNamedObjectNode();
    }

    public bool IsRootNamedObjectNode()
    {
        return Text.Equals("Objects", StringComparison.OrdinalIgnoreCase) &&
               Parent?.Tag is GlueElement;
    }

    public bool IsRootCustomVariablesNode()
    {
        return Parent?.Tag is GlueElement &&
               Text.Equals("Variables", StringComparison.OrdinalIgnoreCase);

    }

    public bool IsRootEventsNode()
    {
        return Parent != null &&
               (Parent.IsEntityNode() || Parent.IsScreenNode()) &&
               Text.Equals("Events", StringComparison.OrdinalIgnoreCase);
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
        return Text.Equals("Code", StringComparison.OrdinalIgnoreCase) &&
               Parent?.Tag is GlueElement;
    }

    public bool IsRootStateNode()
    {
        var parentTreeNode = Parent;
        return Text.Equals("States", StringComparison.OrdinalIgnoreCase) && parentTreeNode != null &&
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

    /// <summary>
    /// Returns whether this is a folder inside GloalContent.
    /// </summary>
    /// <returns>Whether this is a folder inside GlobalContent.</returns>
    public bool IsFolderForGlobalContentFiles()
    {
        if (Parent == null || Tag != null)
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
        //TODO:  this fails when deleting a folder inside files.  We got to fix that.  Try deleting the Palette folders in CreepBase in Baron

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

    /// <summary>
    /// The root tree node - the node which has no parent. Can be 'this', the parent of 'this',
    /// or many levels up from 'this'.
    /// </summary>
    public ITreeNode Root => Parent?.Root ?? this;

    /// <summary>
    /// Returns the file path of the selected node relative to the project root. Note that this is NOT the relative path considering
    /// the tree nodes.
    /// </summary>
    /// <returns></returns>
    public string GetRelativeFilePath()
    {
        var asTreeNode = this;
        #region Directory tree node
        if (this.IsDirectoryNode())
        {
            if (Parent.IsRootEntityNode())
            {
                return $"Entities/" + Text + "/";

            }
            if (Parent.IsRootScreenNode())
            {
                return $"Screens/" + Text + "/";
            }
            
            if (Parent.IsGlobalContentContainerNode())
            {

                string contentDirectory = GlueCommands.Self.GetAbsoluteFileName("GlobalContent/", true);

                string returnValue = contentDirectory + Text;
                if (this.IsDirectoryNode())
                {
                    returnValue += "/";
                }
                // But we want to make this relative to the project, so let's do that
                returnValue = ProjectManager.MakeRelativeContent(returnValue);

                return returnValue;
            }

            // It's a tree node, so make it have a "/" at the end
            return Parent.GetRelativeFilePath() + Text + "/";
        }
        #endregion

        #region Global content container

        if (this.IsGlobalContentContainerNode())
        {
            var returnValue = ProjectManager.ProjectBase.GetAbsoluteContentFolder() + "GlobalContent/";

            // But we want to make this relative to the project, so let's do that
            returnValue = ProjectManager.MakeRelativeContent(returnValue);



            return returnValue;
        }
        #endregion

        if (asTreeNode.IsFilesContainerNode())
        {
            // don't append "Files" here, because adding "Files" causes problems when searching for files
            string valueToReturn = Parent.GetRelativeFilePath();
            return Parent.GetRelativeFilePath();
        }

        if (asTreeNode.IsFolderInFilesContainerNode())
        {
            return Parent.GetRelativeFilePath() + Text + "/";
        }

        if (asTreeNode.IsReferencedFile())
        {
            string toReturn = Parent.GetRelativeFilePath() + Text;
            toReturn = toReturn.Replace("/", "\\");
            return toReturn;
        }

        if(asTreeNode.IsCodeNode())
        {
            var toReturn = Parent.GetRelativeFilePath();
            // take off "code"...
            toReturn = FileManager.GetDirectory(toReturn, RelativeType.Relative);
            // ... and the name of the element
            toReturn = FileManager.GetDirectory(toReturn, RelativeType.Relative);
            toReturn = toReturn + Text;
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
                var extension = FileManager.GetExtension(this.Text);
                string valueToReturn = Parent.GetRelativeFilePath() + this.Text;
                if(string.IsNullOrEmpty(extension))
                {
                    valueToReturn += "/";
                }
                return valueToReturn;

            }
        }
    }

    public string GetRelativeTreeNodePath()
    {
        if(Parent != null)
        {
            return Parent.GetRelativeTreeNodePath() + "/" + this.Text;
        }
        else
        {
            return this.Text;
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
    #region Fields/Properties

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


    static List<GeneralToolStripMenuItem> ListToAddTo = null;
    #endregion


    #region Images

    static System.Windows.Controls.Image BookmarkImage;
    static System.Windows.Controls.Image DerivedEntity;

    #endregion

    private static void PopulateRightClickMenuItemsShared(ITreeNode targetNode, MenuShowingAction menuShowingAction, ITreeNode draggedNode)
    {
        CreateImages();

        #region IsScreenNode

        if (targetNode.IsScreenNode())
        {
            var screen = targetNode.Tag as ScreenSave;
            if (menuShowingAction == MenuShowingAction.RightButtonDrag)
            {
                if (draggedNode.IsEntityNode())
                {
                    Add(L.Texts.EntityAddInstance, () => OnAddEntityInstanceClick(targetNode, draggedNode));
                    Add(L.Texts.EntityListAdd, () => OnAddEntityListClick(targetNode, draggedNode));
                }
            }
            else
            {
                Add(L.Texts.SetAsStartupScreen, SetStartupScreen);
                AddEvent(screen.IsRequiredAtStartup
                    ? L.Texts.ScreenRemoveRequirement
                    : L.Texts.MakeRequiredAtStartup, ToggleRequiredAtStartupClick);

                AddEvent(L.Texts.ScreenExport, ExportElementClick);

                if(targetNode.Tag is ScreenSave { Name: "Screens\\GameScreen" })
                {
                    AddEvent(L.Texts.ScreenLevelCreate, (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog());
                }

                AddRemoveFromProjectItems();

                AddSeparator();

                AddEvent(L.Texts.FindAllReferences, FindAllReferencesClick);
                AddItem(mRefreshTreeNodesMenuItem);

                if(GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
                {
                    Add(L.Texts.ScreenSaveForceJson, () => ForceSaveElementJson(targetNode.Tag as GlueElement));
                    Add(L.Texts.ViewInExplorer, () => ViewElementInExplorer(targetNode.Tag as GlueElement));
                }
                Add(L.Texts.FileOpenCs, () => OpenCsFile(targetNode.Tag as GlueElement));
            }
        }

        #endregion

        #region IsEntityNode

        else if (targetNode.IsEntityNode())
        {
            if (menuShowingAction == MenuShowingAction.RightButtonDrag && draggedNode.IsEntityNode())
            {
                var mAddEntityInstance = new GeneralToolStripMenuItem(L.Texts.EntityAddInstance);
                mAddEntityInstance.Click += (not, used) => OnAddEntityInstanceClick(targetNode, draggedNode);

                var mAddEntityList = new GeneralToolStripMenuItem(L.Texts.EntityListAdd);
                mAddEntityList.Click += (not, used) => OnAddEntityListClick(targetNode, draggedNode);

                AddItem(mAddEntityInstance);
                AddItem(mAddEntityList);
            }
            else
            {
                EntitySave entitySave = targetNode.Tag as EntitySave;

                Add(L.Texts.EntityAddDerived, () => ShowAddDerivedEntityDialog(entitySave), image: DerivedEntity);

                AddSeparator();

                AddRemoveFromProjectItems();

                AddSeparator();
                mExportElement.Text = L.Texts.EntityExport;
                AddItem(mExportElement);
                AddItem(mFindAllReferences);


                if (entitySave.PooledByFactory)
                {
                    AddItem(mAddResetVariablesForPooling);
                }
                AddItem(mRefreshTreeNodesMenuItem);

                if (GlueState.Self.CurrentGlueProject.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
                {
                    Add(L.Texts.ViewInExplorer, () => ViewElementInExplorer(targetNode.Tag as GlueElement));
                }

                Add(L.Texts.FileOpenCs, () => OpenCsFile(targetNode.Tag as GlueElement));
            }
        }

        #endregion

        #region IsFileContainerNode OR IsFolderInFilesContainerNode

        else if (targetNode.IsFilesContainerNode() || targetNode.IsFolderInFilesContainerNode())
        {
            AddItem(addFileToolStripMenuItem);
            Add(L.Texts.FolderAdd, () => RightClickHelper.AddFolderClick(targetNode));
            AddSeparator();
            Add(L.Texts.ViewInExplorer, () => RightClickHelper.ViewInExplorerClick(targetNode));
            AddEvent(L.Texts.CopyPathClipboard, (_, _) => HandleCopyToClipboardClick(targetNode));
            AddSeparator();
            if (targetNode.IsFolderInFilesContainerNode())
            {
                Add("Delete Folder", () => DeleteFolderClick(targetNode));
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
                var mAddEntityInstance = new GeneralToolStripMenuItem(L.Texts.EntityAddInstance);
                mAddEntityInstance.Click += (not, used) => OnAddEntityInstanceClick(targetNode, draggedNode);

                var mAddEntityList = new GeneralToolStripMenuItem(L.Texts.EntityListAdd);
                mAddEntityList.Click += (not, used) => OnAddEntityListClick(targetNode, draggedNode);

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
            Add(L.Texts.FolderAdd, () => RightClickHelper.AddFolderClick(targetNode));
            Add(L.Texts.CodeRegenerate, () => HandleReGenerateCodeClick(targetNode));

            Add(L.Texts.ViewInExplorer, () => RightClickHelper.ViewInExplorerClick(targetNode));

            AddItem(mViewFileLoadOrder);
        }
        #endregion

        #region IsRootEntityNode
        else if (targetNode.IsRootEntityNode())
        {
            AddItem(addEntityToolStripMenuItem);

            Add(L.Texts.EntityImport, () => ImportElementClick(targetNode));

            Add(L.Texts.FolderAdd, () => RightClickHelper.AddFolderClick(targetNode));
        }
        #endregion

        #region IsRootScreenNode
        else if (targetNode.IsRootScreenNode())
        {
            AddItem(addScreenToolStripMenuItem);

            Add(L.Texts.ScreenImport, () => ImportElementClick(targetNode));

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
            Add(L.Texts.ViewInExplorer, () => RightClickHelper.ViewInExplorerClick(targetNode));
            Add(L.Texts.Open, () => HandleOpen(targetNode));
            AddItem(mFindAllReferences);
            AddEvent(L.Texts.CopyPathClipboard, (_,_) => HandleCopyToClipboardClick(targetNode));
            AddSeparator();

            AddItem(mCreateZipPackage);
            var rfs = (ReferencedFileSave)targetNode.Tag;

            AddSeparator();

            if(rfs.IsCreatedByWildcard == false)
            {
                AddRemoveFromProjectItems();
            }

            if (rfs.IsCreatedByWildcard == false)
            {
                AddItem(mUseContentPipeline);
            }
            //AddItem(form.openWithDEFAULTToolStripMenuItem);


            if (FileManager.GetExtension(rfs.Name) == "csv" || rfs.TreatAsCsv)
            {
                AddSeparator();
                AddItem(setCreatedClassToolStripMenuItem);
                Add(L.Texts.CodeRegenerate, () => HandleReGenerateCodeClick(targetNode));
            }


            if (!string.IsNullOrEmpty(rfs.SourceFile) || rfs.SourceFileCache?.Count > 0)
            {
                AddSeparator();
                AddItem(mViewSourceInExplorer);
                AddItem(mRebuildFile);
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

            Add(L.Texts.ViewInExplorer, () => RightClickHelper.ViewInExplorerClick(targetNode));
            Add(L.Texts.CodeRegenerate, () => HandleReGenerateCodeClick(targetNode));
        }

        #endregion

        #region IsRootCodeNode

        else if (targetNode.IsRootCodeNode())
        {
            Add(L.Texts.CodeRegenerate, () => HandleReGenerateCodeClick(targetNode));
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
            AddEvent(L.Texts.CopyPathClipboard, (_, _) => HandleCopyToClipboardClick(targetNode));

            AddSeparator();


            Add(L.Texts.FolderAdd, () => RightClickHelper.AddFolderClick(targetNode));

            bool isEntityContainingFolder = targetNode.Root.IsRootEntityNode();

            if (isEntityContainingFolder)
            {
                AddItem(addEntityToolStripMenuItem);

                Add(L.Texts.EntityImport, () => ImportElementClick(targetNode));
            }
            else
            {
                // If not in the Entities tree structure, assume global content
                AddItem(addFileToolStripMenuItem);
            }

            AddSeparator();

            Add(L.Texts.FolderDelete, () => DeleteFolderClick(targetNode));
            if (isEntityContainingFolder)
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
            Add(L.Texts.Bookmark, () => PluginManager.CallPluginMethod("Tree View Plugin", "AddBookmark", targetNode), image: BookmarkImage);
        }

        #endregion
    }


    static bool HasCreatedImages = false;
    private static void CreateImages()
    {
        if(!HasCreatedImages)
        {
                
            BookmarkImage = MakeImage("/Content/Icons/StarFilled.png");
            DerivedEntity = MakeImage("/Content/Icons/icon_entity_derived.png");
                

            HasCreatedImages = true;
        }
        System.Windows.Controls.Image MakeImage(string sourceName)
        {
            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(sourceName, UriKind.Relative);
            bitmapImage.EndInit();

            return new System.Windows.Controls.Image()
            {
                Source = bitmapImage
            };
        }

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

    static void Add(string text, Action action, string shortcutDisplay = null, System.Windows.Controls.Image image = null)
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
        addScreenToolStripMenuItem = new GeneralToolStripMenuItem(L.Texts.ScreenAdd);
        addScreenToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog();

        setAsStartUpScreenToolStripMenuItem = new GeneralToolStripMenuItem(L.Texts.SetAsStartupScreen);
        setAsStartUpScreenToolStripMenuItem.Click += (not, used) =>
        {
            SetStartupScreen();
        };

        addEntityToolStripMenuItem = new GeneralToolStripMenuItem(L.Texts.EntityAdd);
        addEntityToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewEntityDialog();
        
        addObjectToolStripMenuItem = new GeneralToolStripMenuItem();
        addObjectToolStripMenuItem.Text = L.Texts.ObjectAdd;
        addObjectToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewObjectDialog();

        existingFileToolStripMenuItem = new GeneralToolStripMenuItem();
        existingFileToolStripMenuItem.Text = L.Texts.FileExisting;
        existingFileToolStripMenuItem.Click += (not, used) => AddExistingFileManager.Self.AddExistingFileClick();

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
        removeFromProjectToolStripMenuItem.Text = L.Texts.RemoveFromProject;
        removeFromProjectToolStripMenuItem.Click += (not, used) => RightClickHelper.RemoveFromProjectToolStripMenuItem();

        mMoveToTop = new GeneralToolStripMenuItem($"^^ {L.Texts.MoveToTop}");
        mMoveToTop.ShortcutKeyDisplayString = "Alt+Shift+Up";
        mMoveToTop.Click += MoveToTopClick;

        addVariableToolStripMenuItem = new GeneralToolStripMenuItem();
        addVariableToolStripMenuItem.Text = L.Texts.VariableAdd;
        addVariableToolStripMenuItem.Click += (not, used) => GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog(CustomVariableType.New);

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

        mRebuildFile = new GeneralToolStripMenuItem(L.Texts.FileRebuild);
        mRebuildFile.Click += RebuildFileClick;

        mViewSourceInExplorer = new GeneralToolStripMenuItem(L.Texts.ViewSourceExplorer);
        mViewSourceInExplorer.Click += ViewSourceInExplorerClick;

        mFindAllReferences = new GeneralToolStripMenuItem(L.Texts.FindAllReferences);
        mFindAllReferences.Click += FindAllReferencesClick;

        mDuplicate = new GeneralToolStripMenuItem(L.Texts.Duplicate);
        mDuplicate.Click += DuplicateClick;

        mAddState = new GeneralToolStripMenuItem(L.Texts.StateAdd);
        mAddState.Click += AddStateClick;

        mAddStateCategory = new GeneralToolStripMenuItem(L.Texts.CategoryAddState);
        mAddStateCategory.Click += AddStateCategoryClick;

        mAddResetVariablesForPooling = new GeneralToolStripMenuItem(L.Texts.ResetVariablesPoolingAdd);
        mAddResetVariablesForPooling.Click += mAddResetVariablesForPooling_Click;

        mFillValuesFromDefault = new GeneralToolStripMenuItem(L.Texts.VariableFillValues);
        mFillValuesFromDefault.Click += mFillValuesFromVariables_Click;

        mRemoveFromProjectQuick = new GeneralToolStripMenuItem(L.Texts.RemoveFromProjectQuick);
        mRemoveFromProjectQuick.Click += RemoveFromProjectQuick;

        mUseContentPipeline = new GeneralToolStripMenuItem(L.Texts.PipelineContentToggle);
        mUseContentPipeline.Click += mUseContentPipeline_Click;

        mCreateNewFileForMissingFile = new GeneralToolStripMenuItem(L.Texts.FileCreateForMissing);
        mCreateNewFileForMissingFile.Click += CreateNewFileForMissingFileClick;

        mViewFileLoadOrder = new GeneralToolStripMenuItem(L.Texts.ViewFileOrder);
        mViewFileLoadOrder.Click += ViewFileOrderClick;

        mCreateZipPackage = new GeneralToolStripMenuItem(L.Texts.ZipPackageCreate);
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

    public static void HandleAddEventOk(AddEventWindow addEventWindow)
    {
        var viewModel = new GlueFormsCore.ViewModels.AddEventViewModel();
        viewModel.EventName = addEventWindow.ResultName;
        viewModel.TunnelingObject = addEventWindow.TunnelingObject;
        viewModel.TunnelingEvent = addEventWindow.TunnelingEvent;

        viewModel.SourceVariable = addEventWindow.SourceVariable;
        viewModel.BeforeOrAfter = addEventWindow.BeforeOrAfter;

        viewModel.DelegateType = addEventWindow.ResultDelegateType;

        GlueCommands.Self.GluxCommands.ElementCommands.AddEventToElement(viewModel, GlueState.Self.CurrentElement);

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
                removeFromProjectToolStripMenuItem.Text = L.Texts.RemoveFromScreen;
            }
            else if (GlueState.Self.CurrentEntitySave != null)
            {
                removeFromProjectToolStripMenuItem.Text = L.Texts.RemoveFromEntity;
            }
            else
            {
                removeFromProjectToolStripMenuItem.Text = L.Texts.RemoveFromGlobalContent;
            }
        }
        else
        {
            removeFromProjectToolStripMenuItem.Text = L.Texts.ItemRemove;
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

            GluxCommands.Self.SaveGlux();
        }
    }

    static void mUseContentPipeline_Click(object sender, EventArgs e)
    {
        ReferencedFileSave rfs = GlueState.Self.CurrentReferencedFileSave;

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
        GluxCommands.Self.SaveGlux();

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
            // screens and entities will be current when the objects earlier in this statement are also current,
            // so only make them the currentObject if the types above are not current.
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
            GlueCommands.Self.DialogCommands.AskToRemoveObjectList(glueState.CurrentNamedObjectSaves.ToList(), saveAndRegenerate);
        }
        else
        {
            // Search terms: removefromproject, remove from project, remove file, remove referencedfilesave
            List<string> filesToRemove = new List<string>();

            if (ProjectManager.StatusCheck() == ProjectManager.CheckResult.Passed)
            {
                GlueElement deletedElement = null;
                ReferencedFileSave deletedRfs = null;
                #region Find out if the user really wants to remove this - don't ask if askAreYouSure is false
                var reallyRemoveResult = System.Windows.MessageBoxResult.Yes;

                // Some objects may use a custom delete dialog. Those types should be checked here first:
                if(currentObject is ScreenSave screenToRemove)
                {

                    await TaskManager.Self.AddAsync(() =>
                    {
                        if (RemoveScreen(screenToRemove, filesToRemove))
                        {
                            deletedElement = screenToRemove;
                        }

                    }, L.Texts.ScreenRemove);

                    askAreYouSure = false;
                }

                    

                if (askAreYouSure)
                {
                    var message = String.Format(L.Texts.DeleteQuestionX, currentObject);

                    reallyRemoveResult = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(message, "Remove?");
                }
                #endregion

                if (reallyRemoveResult == System.Windows.MessageBoxResult.Yes)
                {
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

                    else if (currentObject is ReferencedFileSave rfs)
                    {
                        // the GluxCommand handles saving and regenerate internally, no need to do it twice
                        saveAndRegenerate = false;
                        var toRemove = rfs;
                        deletedRfs = rfs;
                        if (GlueState.Self.Find.IfReferencedFileSaveIsReferenced(toRemove))
                        {
                            await GlueCommands.Self.GluxCommands.RemoveReferencedFileAsync(toRemove, filesToRemove, regenerateAndSave: true);
                        }
                    }
                    #endregion

                    #region Else if is CustomVariable

                    else if (GlueState.Self.CurrentCustomVariable != null)
                    {
                        GlueCommands.Self.GluxCommands.RemoveCustomVariable(GlueState.Self.CurrentCustomVariable, filesToRemove);
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

                    #region Else if is EntitySave

                    else if (GlueState.Self.CurrentEntitySave != null)
                    {
                        var entityToRemove = GlueState.Self.CurrentEntitySave;
                        await TaskManager.Self.AddAsync(async () =>
                        {
                            await RemoveEntity(GlueState.Self.CurrentEntitySave, filesToRemove);
                            //ProjectManager.RemoveEntity(EditorLogic.CurrentEntitySave);
                            deletedElement = entityToRemove;
                        }, L.Texts.EntityRemove);

                    }

                    #endregion

                    #region Files were deleted and the user wants to be asked to delete

                    if (filesToRemove.Count != 0 && askToDeleteFiles)
                    {

                        for (int i = 0; i < filesToRemove.Count; i++)
                        {
                            if (FileManager.IsRelative(filesToRemove[i]))
                            {
                                filesToRemove[i] = GlueCommands.Self.GetAbsoluteFileName(filesToRemove[i], false);
                            }
                            filesToRemove[i] = filesToRemove[i].Replace("\\", "/");
                        }

                        StringFunctions.RemoveDuplicates(filesToRemove, true);

                        var lbw = new ListBoxWindowWpf();

                        string messageString = L.Texts.WhatToDoWithFiles + "\n";
                        lbw.Message = messageString;

                        foreach (string s in filesToRemove)
                        {

                            lbw.AddItem(s);
                        }
                        lbw.ClearButtons();
                        lbw.AddButton(L.Texts.FilesLeaveAsPartOfProject, DialogResult.No);
                        lbw.AddButton(L.Texts.FilesRemoveFromProjectButKeep, DialogResult.OK);
                        lbw.AddButton(L.Texts.FilesRemoveAndDelete, DialogResult.Yes);

                        var dialogShowResult = lbw.ShowDialog();

                        if(lbw.ClickedOption is DialogResult result)
                        {
                            if (result == DialogResult.OK || result == DialogResult.Yes)
                            {
                                await TaskManager.Self.AddAsync(() =>
                                {
                                    foreach (var file in filesToRemove)
                                    {
                                        FilePath filePath = GlueCommands.Self.GetAbsoluteFileName(file, false);
                                        // This file may have been removed
                                        // in windows explorer, and now removed
                                        // from Glue.  Check to prevent a crash.

                                        GlueCommands.Self.ProjectCommands.RemoveFromProjects(filePath, false);

                                        if (result == DialogResult.Yes && filePath.Exists())
                                        {
                                            FileHelper.MoveToRecycleBin(filePath.FullPath);
                                        }
                                    }
                                    GluxCommands.Self.ProjectCommands.SaveProjects();

                                }, L.Texts.FilesRemove);
                            }

                        }
                    }

                    #endregion

                    if(deletedElement == null && GlueState.Self.CurrentElement == null && currentElementBeforeRemoval != null)
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
                    }, L.Texts.RefreshingTreeNodes);


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

    private static async Task RemoveEntity(EntitySave entityToRemove, List<string> filesThatCouldBeRemoved)
    {
        var namedObjectsToRemove = ObjectFinder.Self.GetAllNamedObjectsThatUseEntity(entityToRemove.Name);

        string message = null;

        if (namedObjectsToRemove.Count != 0)
        {
            message = String.Format(L.Texts.EntityReferencedByFollowingObjects, entityToRemove);

            for (var i = 0; i < namedObjectsToRemove.Count; i++)
            {
                message += "\n" + namedObjectsToRemove[i];
            }
        }

        var inheritingEntities = ObjectFinder.Self.GetAllEntitiesThatInheritFrom(entityToRemove);

        if (inheritingEntities.Count != 0)
        {
            message = String.Format(L.Texts.EntityIsBaseForEntities, entityToRemove);
            for (int i = 0; i < inheritingEntities.Count; i++)
            {
                message += "\n" + inheritingEntities[i];

            }
        }

        if (message != null)
        {
            message += "\n\n" + L.Texts.DeleteQuestion;

            var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(message);
            if(result == System.Windows.MessageBoxResult.Yes) {
                await GlueCommands.Self.GluxCommands.RemoveEntityAsync(entityToRemove, filesThatCouldBeRemoved);
            }
        }
        else
        {
            await GlueCommands.Self.GluxCommands.RemoveEntityAsync(entityToRemove, filesThatCouldBeRemoved);
        }
    }


    private static bool RemoveScreen(ScreenSave screenToRemove, List<string> filesThatCouldBeRemoved)
    {
        var message = String.Format(L.Texts.DeleteQuestionX, screenToRemove);

        List<ScreenSave> inheritingScreens = ObjectFinder.Self.GetAllScreensThatInheritFrom(screenToRemove);
        if (inheritingScreens.Count != 0)
        {
            message += String.Format("\n\n" + L.Texts.WarningScreenXBaseForScreens, screenToRemove.GetStrippedName());
            for (var i = 0; i < inheritingScreens.Count; i++)
            {
                message += "\n" + inheritingScreens[i];
            }
        }

        var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(message, "Are you sure?");

        var wasRemoved = false;
        if (result == System.Windows.MessageBoxResult.Yes)
        {
            wasRemoved = true;

            GlueCommands.Self.GluxCommands.RemoveScreen(screenToRemove, filesThatCouldBeRemoved);
        }
        return wasRemoved;
    }

    private static void AskToRemoveCustomVariablesWithoutState(IElement element)
    {
        for (var i = 0; i < element.CustomVariables.Count; i++)
        {
            var variable = element.CustomVariables[i];

            if (CustomVariableHelper.IsStateMissingFor(variable, element))
            {
                var mbmb = new MultiButtonMessageBox();
                mbmb.MessageText = String.Format(L.Texts.VariableHasNoStates, variable);

                mbmb.AddButton(L.Texts.VariableRemove, DialogResult.OK);
                mbmb.AddButton(L.Texts.VariableRemoveNothing, DialogResult.Cancel);

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
        var tiw = new TextInputWindow();
        tiw.Message = L.Texts.NewFolderEnter;
        tiw.Text = L.Texts.NewFolder;

        var result = tiw.ShowDialog(MainGlueWindow.Self);

        if (result == DialogResult.OK)
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

    /// <summary>
    /// Deletes the folder represented by this tree node. Note that this should only be called on tree nodes which
    /// are folders.
    /// </summary>
    /// <param name="targetNode">The tree node to delete.</param>
    public static void DeleteFolderClick(ITreeNode targetNode)
    {
        // delete folder, deletefolder

        var forceContent = targetNode.IsChildOfGlobalContent() || targetNode.IsFolderInFilesContainerNode();

        var absolutePath = GlueCommands.Self.GetAbsoluteFileName(targetNode.GetRelativeFilePath(), forceContent);

        string[] files = null;
        string[] directories = null;
        if (Directory.Exists(absolutePath))
        {

            files = Directory.GetFiles(absolutePath);
            directories = Directory.GetDirectories(absolutePath);
        }

        var shouldDelete = System.Windows.MessageBoxResult.Yes;

        if ((files != null && files.Length != 0) || (directories != null && directories.Length != 0))
        {
            shouldDelete = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(String.Format(L.Texts.FolderDeleteNotEmpty, absolutePath), L.Texts.Sure);
        }

        if (shouldDelete == System.Windows.MessageBoxResult.Yes)
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
                    RemoveFromProjectOptionalSaveAndRegenerate(entitySave == allEntitySaves[^1], false, false);

                }
            }
            else if (targetNode.IsFolderInFilesContainerNode() || targetNode.IsChildOfGlobalContent())
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
        inputWindow.Message = L.Texts.NewFolderEnter;
        inputWindow.Result = treeNode.Text;

        var dialogResult = inputWindow.ShowDialog();

        if (dialogResult == DialogResult.OK)
        {
            GlueCommands.Self.GluxCommands.RenameFolder(treeNode, inputWindow.Result);
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
            string locationToShow = FileManager.RelativeDirectory + targetNode.GetRelativeFilePath();

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
            object objectToMove;
            IList listToRemoveFrom;
            IList listForIndexing;
            GetObjectAndListForMoving(out objectToMove, out listToRemoveFrom, out listForIndexing);
            if (listToRemoveFrom != null)
            {
                int index = listToRemoveFrom.IndexOf(objectToMove);
                if (index > 0)
                {
                    listToRemoveFrom.Remove(objectToMove);
                    listToRemoveFrom.Insert(0, objectToMove);
                    var variable = objectToMove as CustomVariable;
                    PostMoveActivity(variable);
                }
            }
        }, L.Texts.MovingToTop, TaskExecutionPreference.Asap);
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

    private static async Task MoveObjectInDirection(int direction)
    {
        await TaskManager.Self.AddAsync(() =>
            {
                object objectToMove;
                IList listToRemoveFrom;
                IList listForIndexing;
                GetObjectAndListForMoving(out objectToMove, out listToRemoveFrom, out listForIndexing);
                if (listToRemoveFrom != null)
                {
                    int index = listToRemoveFrom.IndexOf(objectToMove);

                    var oldIndexInListForIndexing = listForIndexing.IndexOf(objectToMove);
                    var newIndexInListForIndexing = oldIndexInListForIndexing + direction;
                    if(newIndexInListForIndexing != -1 && newIndexInListForIndexing < listForIndexing.Count)
                    {
                        object objectToMoveBeforeOrAfter = objectToMove;
                        if (newIndexInListForIndexing >= 0 && newIndexInListForIndexing < listForIndexing.Count)
                        {
                            objectToMoveBeforeOrAfter = listForIndexing[newIndexInListForIndexing];
                        }

                        //int newIndex = index + direction;
                        int newIndex = listToRemoveFrom.IndexOf(objectToMoveBeforeOrAfter);

                        if (newIndex >= 0 && newIndex < listToRemoveFrom.Count)
                        {
                            listToRemoveFrom.Remove(objectToMove);

                            listToRemoveFrom.Insert(newIndex, objectToMove);
                            var variable = objectToMove as CustomVariable;
                            PostMoveActivity(variable);
                        }
                    }
                }

            },
            String.Format(L.Texts.MovingXUpOrDown, GlueState.Self.CurrentCustomVariable?.ToString() ?? GlueState.Self.CurrentNamedObjectSave?.ToString()), 
            TaskExecutionPreference.Asap);
    }


    private static void MoveToBottomClick(object sender, EventArgs e)
    {
        MoveToBottom();
    }

    public static void MoveToBottom()
    {
        TaskManager.Self.Add(() =>
        {
            object objectToMove;
            IList listToRemoveFrom;
            IList throwaway;
            GetObjectAndListForMoving(out objectToMove, out listToRemoveFrom, out throwaway);
            if (listToRemoveFrom != null)
            {

                int index = listToRemoveFrom.IndexOf(objectToMove);

                if (index < listToRemoveFrom.Count - 1)
                {
                    listToRemoveFrom.Remove(objectToMove);
                    listToRemoveFrom.Insert(listToRemoveFrom.Count, objectToMove);
                    var variable = objectToMove as CustomVariable;
                    PostMoveActivity(variable);
                }
            }
        }, L.Texts.MovingToBottom, TaskExecutionPreference.Asap);
    }

    private static void GetObjectAndListForMoving(out object objectToMove,
        out IList listToRemoveFrom, 
        // The list to use when adjusting index, which is needed if the object being shifted is in a list that is filtered. For example,
        // Layers appear in a sub-list of all layers.
        out IList listForIndexing)
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
        else if(GlueState.Self.CurrentStateSave != null)
        {
            var category = GlueState.Self.CurrentStateSaveCategory;

            objectToMove = GlueState.Self.CurrentStateSave;
            listToRemoveFrom = category.States ?? GlueState.Self.CurrentElement.States;
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


    private static void PostMoveActivity(CustomVariable variableMoved)
    {
        // do this before refreshing the tree nodes
        var currentCustomVariable = GlueState.Self.CurrentCustomVariable;
        var currentNamedObjectSave = GlueState.Self.CurrentNamedObjectSave;
        var currentStateSave = GlueState.Self.CurrentStateSave;

        var element = GlueState.Self.CurrentElement;


        // I think the variables are complete remade. I could make it preserve them, but it's easier to do this:
        if (currentCustomVariable != null)
        {
            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element, TreeNodeRefreshType.CustomVariables);
            //GlueState.Self.CurrentCustomVariable = null;
            GlueState.Self.CurrentCustomVariable = currentCustomVariable;
        }
        else if (currentNamedObjectSave != null)
        {
            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element, TreeNodeRefreshType.NamedObjects);
            //GlueState.Self.CurrentNamedObjectSave = null;
            GlueState.Self.CurrentNamedObjectSave = currentNamedObjectSave;
        }
        else if (currentStateSave != null)
        {
            GlueCommands.Self.RefreshCommands.RefreshTreeNodeFor(element, TreeNodeRefreshType.All); // todo - this could be more efficient...

            GlueState.Self.CurrentStateSave = currentStateSave;
        }

        GlueState.Self.CurrentElement.SortStatesToCustomVariables();
        var elementsToRegen = new HashSet<GlueElement>();
        if(element != null)
        {
            elementsToRegen.Add(element);
        }

        foreach (NamedObjectSave nos in ObjectFinder.Self.GetAllNamedObjectsThatUseElement(element))
        {
            nos.UpdateCustomProperties();

            // We only want to re-generate this if the object actually has this variable assigned.
            // If it doesn't, then the re-order won't matter

            var shouldRegenerateEntity = true;

            if (variableMoved != null)
            {
                var doesNosAssignVariable = nos.InstructionSaves.Any(item => item.Member == variableMoved.Name);

                if(!doesNosAssignVariable)
                {
                    shouldRegenerateEntity = false;
                }
            }


            if (shouldRegenerateEntity)
            {
                var container = nos.GetContainer();

                var baseElements = ObjectFinder.Self.GetAllBaseElementsRecursively(container);
                if(!elementsToRegen.ContainsAny(baseElements))
                {
                    elementsToRegen.Add(container);
                }
            }
        }

        foreach (var elementToRegen in elementsToRegen)
        {
            // February 18, 2022
            // performance note: This generates all elements that inherit from the argument, so this
            // could re-generate lots of code over and over. This could be improved if needed, but I'm 
            // writing this as I've already made tons of performance improvements to the post move activity
            // method. Therefore, I'll leave this for a 2nd pass if we really need it.
            GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(elementToRegen);
        }

        foreach(var elementToSave in elementsToRegen)
        {
            GlueCommands.Self.GluxCommands.SaveElementAsync(elementToSave, TaskExecutionPreference.AddOrMoveToEnd);
        }
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
            currentExternalDirectory = GlueCommands.Self.GetAbsoluteFileName(ProjectManager.GlueProjectSave.ExternallyBuiltFileDirectory, true);
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
                    GlueCommands.Self.GetAbsoluteFileName(rfs);

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

            GluxCommands.Self.SaveGlux();
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