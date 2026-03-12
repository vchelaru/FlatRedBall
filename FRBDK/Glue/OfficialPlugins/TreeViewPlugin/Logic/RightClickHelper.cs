using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Factories;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.IO.Zip;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.Plugins.Interfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SaveClasses.Helpers;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.Glue.Utilities;
using FlatRedBall.Glue.ViewModels;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.IO;
using FlatRedBall.Utilities;
using Glue;
using GlueFormsCore.FormHelpers;
using Newtonsoft.Json;
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
        if (System.Windows.Application.Current != null)
            CreateImages();

        var descriptors = GetItemDescriptors(
            targetNode, GlueState.Self, menuShowingAction, draggedNode,
            shiftHeld: (Control.ModifierKeys & Keys.Shift) != 0,
            fileExists: rfs => GlueCommands.Self.GetAbsoluteFilePath(rfs).Exists());

        foreach (var desc in descriptors)
        {
            if (desc.IsSeparator)
            {
                AddSeparator();
            }
            else if (desc.SubItems != null)
            {
                var parentItem = Add(desc.Text, desc.Handler ?? (() => { }));
                var oldList = ListToAddTo;
                ListToAddTo = parentItem.DropDownItems;
                foreach (var sub in desc.SubItems)
                    Add(sub.Text, sub.Handler!, shortcutDisplay: sub.ShortcutKeyDisplayString);
                ListToAddTo = oldList;
            }
            else
            {
                Add(desc.Text, desc.Handler!, shortcutDisplay: desc.ShortcutKeyDisplayString, image: desc.Image);
            }
        }
    }

    /// <summary>
    /// Returns the ordered list of items that should appear in the right-click menu for
    /// <paramref name="targetNode"/>. Does not build any UI objects — call this from unit tests
    /// to assert which items appear for a given node type.
    /// </summary>
    internal static IReadOnlyList<RightClickItemDescriptor> GetItemDescriptors(
        ITreeNode targetNode,
        IGlueState glueState,
        MenuShowingAction action,
        ITreeNode? draggedNode = null,
        bool shiftHeld = false,
        Func<ReferencedFileSave, bool>? fileExists = null)
    {
        var list = new List<RightClickItemDescriptor>();

        // Uses glueState instead of GlueState.Self so it is mockable in tests.
        IEnumerable<RightClickItemDescriptor> RemoveItems()
        {
            string removeText;
            if (glueState.CurrentReferencedFileSave != null ||
                glueState.CurrentNamedObjectSave != null ||
                glueState.CurrentEventResponseSave != null ||
                glueState.CurrentCustomVariable != null ||
                glueState.CurrentStateSave != null ||
                glueState.CurrentStateSaveCategory != null)
            {
                if (glueState.CurrentScreenSave != null)
                    removeText = "Remove from Screen";
                else if (glueState.CurrentEntitySave != null)
                    removeText = "Remove from Entity";
                else
                    removeText = "Remove from Global Content";

                if (glueState.CurrentReferencedFileSave?.IsCreatedByWildcard == true)
                    removeText = $"Delete [{glueState.CurrentReferencedFileSave.Name}]";
            }
            else
            {
                removeText = "Remove Item";
            }

            yield return new RightClickItemDescriptor
            {
                Text = removeText,
                Handler = async () => await RemoveFromProjectToolStripMenuItem()
            };

            if (shiftHeld)
                yield return new RightClickItemDescriptor
                {
                    Text = "Remove from Project Quick (ONLY IF YOU KNOW WHAT YOU'RE DOING!)",
                    Handler = () => RemoveFromProjectQuick(null, EventArgs.Empty)
                };
        }

        RightClickItemDescriptor AddFileSubMenu() => new()
        {
            Text = "Add File",
            Handler = () => { },
            SubItems = new[]
            {
                new RightClickItemDescriptor { Text = "New File", Handler = async () => await GlueCommands.Self.DialogCommands.ShowAddNewFileDialogAsync() },
                new RightClickItemDescriptor { Text = "Existing File(s)", Handler = () => GlueCommands.Self.DialogCommands.ShowAddExistingFileDialog() },
            }
        };

        RightClickItemDescriptor RenameItem() => new()
        {
            Text = "Rename",
            Handler = () =>
            {
                if (SelectionLogic.Current.CurrentNode?.IsEditable == true)
                    SelectionLogic.Current.CurrentNode.IsEditing = true;
            }
        };

        #region IsScreenNode

        if (targetNode.IsScreenNode())
        {
            var screen = targetNode.Tag as ScreenSave;
            if (action == MenuShowingAction.RightButtonDrag)
            {
                if (draggedNode?.IsEntityNode() == true)
                {
                    list.Add(new RightClickItemDescriptor { Text = "Add Entity Instance", Handler = () => OnAddEntityInstanceClick(targetNode, draggedNode) });
                    list.Add(new RightClickItemDescriptor { Text = "Add Entity List", Handler = () => OnAddEntityListClick(targetNode, draggedNode) });
                }
            }
            else
            {
                // Config
                list.Add(new RightClickItemDescriptor { Text = "Set as Startup Screen", Handler = SetStartupScreen, Image = StartupScreenImage });
                list.Add(new RightClickItemDescriptor
                {
                    Text = screen.IsRequiredAtStartup ? "Remove Startup Requirement" : "Make Required at Startup",
                    Handler = () => ToggleRequiredAtStartupClick(null, EventArgs.Empty)
                });
                list.Add(RightClickItemDescriptor.Separator);

                // Creation
                if (targetNode.Tag is ScreenSave { Name: "Screens\\GameScreen" })
                {
                    list.Add(new RightClickItemDescriptor { Text = "Create Level Screen", Handler = () => GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog() });
                    list.Add(RightClickItemDescriptor.Separator);
                }

                // Common ops
                list.Add(RenameItem());
                list.Add(new RightClickItemDescriptor { Text = "Duplicate", Handler = async () => await GlueCommands.Self.GluxCommands.CopyGlueElement(screen) });
                list.AddRange(RemoveItems());
                list.Add(new RightClickItemDescriptor { Text = "Export Screen", Handler = () => ExportElementClick(null, EventArgs.Empty) });
                list.Add(RightClickItemDescriptor.Separator);

                // Discovery
                if (glueState.CurrentGlueProject?.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
                    list.Add(new RightClickItemDescriptor { Text = "View in Explorer", Handler = () => ViewElementInExplorer(targetNode.Tag as GlueElement), Image = FolderImage });
                list.Add(new RightClickItemDescriptor { Text = "Open Custom Code", Handler = () => OpenCsFile(targetNode.Tag as GlueElement) });
                list.Add(RightClickItemDescriptor.Separator);

                // Maintenance
                list.Add(new RightClickItemDescriptor { Text = "Find All References", Handler = () => FindAllReferencesClick(null, EventArgs.Empty) });
                list.Add(new RightClickItemDescriptor { Text = "Refresh UI", Handler = () => OnRefreshTreeNodesClick(null, EventArgs.Empty) });
                if (glueState.CurrentGlueProject?.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
                    list.Add(new RightClickItemDescriptor { Text = "Force Save Screen JSON", Handler = () => ForceSaveElementJson(targetNode.Tag as GlueElement) });
            }
        }

        #endregion

        #region IsEntityNode

        else if (targetNode.IsEntityNode())
        {
            if (action == MenuShowingAction.RightButtonDrag && draggedNode?.IsEntityNode() == true)
            {
                list.Add(new RightClickItemDescriptor { Text = "Add Entity Instance", Handler = () => OnAddEntityInstanceClick(targetNode, draggedNode) });
                list.Add(new RightClickItemDescriptor { Text = "Add Entity List", Handler = () => OnAddEntityListClick(targetNode, draggedNode) });
            }
            else
            {
                EntitySave entitySave = targetNode.Tag as EntitySave;

                // Creation
                list.Add(new RightClickItemDescriptor { Text = "Add Derived Entity", Handler = () => ShowAddDerivedEntityDialog(entitySave), Image = DerivedEntity });
                list.Add(RightClickItemDescriptor.Separator);

                // Common ops
                list.Add(RenameItem());
                list.Add(new RightClickItemDescriptor { Text = "Duplicate", Handler = async () => await GlueCommands.Self.GluxCommands.CopyGlueElement(entitySave) });
                list.AddRange(RemoveItems());
                list.Add(new RightClickItemDescriptor { Text = "Export Entity", Handler = () => ExportElementClick(null, EventArgs.Empty) });
                list.Add(RightClickItemDescriptor.Separator);

                // Discovery
                if (glueState.CurrentGlueProject?.FileVersion >= (int)GlueProjectSave.GluxVersions.GlueSavedToJson)
                    list.Add(new RightClickItemDescriptor { Text = "View in Explorer", Handler = () => ViewElementInExplorer(targetNode.Tag as GlueElement), Image = FolderImage });
                list.Add(new RightClickItemDescriptor { Text = "Open Custom Code", Handler = () => OpenCsFile(targetNode.Tag as GlueElement) });
                list.Add(RightClickItemDescriptor.Separator);

                // Maintenance
                list.Add(new RightClickItemDescriptor { Text = "Find All References", Handler = () => FindAllReferencesClick(null, EventArgs.Empty) });
                if (entitySave.PooledByFactory)
                    list.Add(new RightClickItemDescriptor { Text = "Add Reset Variables for Pooling", Handler = () => mAddResetVariablesForPooling_Click(null, EventArgs.Empty) });
                list.Add(new RightClickItemDescriptor { Text = "Refresh UI", Handler = () => OnRefreshTreeNodesClick(null, EventArgs.Empty) });
            }
        }

        #endregion

        #region IsFileContainerNode OR IsFolderInFilesContainerNode

        else if (targetNode.IsFilesContainerNode() || targetNode.IsFolderInFilesContainerNode())
        {
            list.Add(AddFileSubMenu());
            list.Add(new RightClickItemDescriptor { Text = "Add Folder", Handler = () => RightClickHelper.AddFolderClick(targetNode), Image = FolderImage });
            list.Add(RightClickItemDescriptor.Separator);
            list.Add(new RightClickItemDescriptor { Text = "View in Explorer", Handler = () => RightClickHelper.ViewInExplorerClick(targetNode), Image = FolderImage });
            list.Add(new RightClickItemDescriptor { Text = "Copy Path to Clipboard", Handler = () => HandleCopyToClipboardClick(targetNode) });
            list.Add(RightClickItemDescriptor.Separator);
            if (targetNode.IsFolderInFilesContainerNode())
                list.Add(new RightClickItemDescriptor { Text = "Delete Folder", Handler = () => GlueCommands.Self.GluxCommands.DeleteFolderClick(targetNode) });
        }

        #endregion

        #region IsRootObjectNode

        else if (targetNode.IsRootNamedObjectNode())
        {
            bool isSameObject = false;
            var elementForTreeNode = targetNode.GetContainingElementTreeNode()?.Tag;
            if (elementForTreeNode != null && draggedNode != null)
                isSameObject = elementForTreeNode == draggedNode?.Tag;

            if (action == MenuShowingAction.RightButtonDrag && !isSameObject && draggedNode.IsEntityNode())
            {
                list.Add(new RightClickItemDescriptor { Text = "Add Entity Instance", Handler = () => OnAddEntityInstanceClick(targetNode, draggedNode) });
                list.Add(new RightClickItemDescriptor { Text = "Add Entity List", Handler = () => OnAddEntityListClick(targetNode, draggedNode) });
            }
            else
            {
                list.Add(new RightClickItemDescriptor { Text = "Add Object", Handler = () => GlueCommands.Self.DialogCommands.ShowAddNewObjectDialog() });
            }
        }

        #endregion

        #region IsRootLayerNode

        else if (targetNode.IsRootLayerNode())
        {
            list.Add(new RightClickItemDescriptor { Text = "Add Layer", Handler = () => HandleAddLayerClick(null, EventArgs.Empty) });
        }

        #endregion

        #region IsRootCollisionRelationships node

        else if (targetNode.IsRootCollisionRelationshipsNode())
        {
            list.Add(new RightClickItemDescriptor { Text = "Add Collision Relationship", Handler = () => AddNewCollisionRelationshipTo(glueState.CurrentElement), Image = CollisionRelationshipImage });
        }

        #endregion

        #region IsGlobalContentContainerNode

        else if (targetNode.IsGlobalContentContainerNode())
        {
            list.Add(AddFileSubMenu());
            list.Add(new RightClickItemDescriptor { Text = "Add Folder", Handler = () => RightClickHelper.AddFolderClick(targetNode), Image = FolderImage });
            list.Add(RightClickItemDescriptor.Separator);
            list.Add(new RightClickItemDescriptor { Text = "View in Explorer", Handler = () => RightClickHelper.ViewInExplorerClick(targetNode), Image = FolderImage });
            list.Add(RightClickItemDescriptor.Separator);
            list.Add(new RightClickItemDescriptor { Text = "Regenerate Code", Handler = () => HandleReGenerateCodeClick(targetNode) });
        }

        #endregion

        #region IsRootEntityNode

        else if (targetNode.IsRootEntityNode())
        {
            list.Add(new RightClickItemDescriptor { Text = "Add Entity", Handler = () => GlueCommands.Self.DialogCommands.ShowAddNewEntityDialog(), Image = EntityImage });
            list.Add(new RightClickItemDescriptor { Text = "Add Folder", Handler = () => RightClickHelper.AddFolderClick(targetNode), Image = FolderImage });
            list.Add(new RightClickItemDescriptor { Text = "Import Entity", Handler = () => ImportElementClick(targetNode) });
        }

        #endregion

        #region IsRootScreenNode

        else if (targetNode.IsRootScreenNode())
        {
            list.Add(new RightClickItemDescriptor { Text = "Add Screen", Handler = () => GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog(), Image = ScreenImage });
            list.Add(new RightClickItemDescriptor { Text = "Add Folder", Handler = () => RightClickHelper.AddFolderClick(targetNode), Image = FolderImage });
            list.Add(new RightClickItemDescriptor { Text = "Import Screen", Handler = () => ImportElementClick(targetNode) });
        }

        #endregion

        #region IsRootCustomVariables

        else if (targetNode.IsRootCustomVariablesNode())
        {
            var targetElement = targetNode.GetContainingElementTreeNode()?.Tag as GlueElement;

            if (targetElement == null)
            {
                // for Vic to figure out what's up... This should never be null because the target node
                System.Diagnostics.Debugger.Break();
            }

            list.Add(new RightClickItemDescriptor
            {
                Text = "Add Variable",
                Handler = () => GlueCommands.Self.DialogCommands.ShowAddNewVariableDialog(CustomVariableType.New, container: targetElement)
            });
        }

        #endregion

        #region IsRootEventNode

        else if (targetNode.IsRootEventsNode())
        {
            list.Add(new RightClickItemDescriptor { Text = "Add Event", Handler = () => AddEventClicked(null, EventArgs.Empty) });
        }

        #endregion

        #region IsNamedObjectNode

        else if (targetNode.IsNamedObjectNode())
        {
            // In case something has changed which can happen mid wizard
            var currentNamedObject = targetNode.Tag as NamedObjectSave;

            // Creation
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
                    var derived = ReferenceService.Self.GetAllDerivedElementsRecursive(genericEntityType);
                    var hasNonAbstract = derived.Any(item => !IsAbstract(item));
                    shouldAdd = hasNonAbstract;
                }
                if (shouldAdd)
                {
                    list.Add(new RightClickItemDescriptor { Text = "Add Object", Handler = () => GlueCommands.Self.DialogCommands.ShowAddNewObjectDialog() });
                    list.Add(RightClickItemDescriptor.Separator);
                }
            }
            else if (currentNamedObject?.GetAssetTypeInfo() == AvailableAssetTypes.CommonAtis.ShapeCollection)
            {
                list.Add(new RightClickItemDescriptor { Text = "Add Object", Handler = () => GlueCommands.Self.DialogCommands.ShowAddNewObjectDialog() });
                list.Add(RightClickItemDescriptor.Separator);
            }

            // Common ops
            list.Add(new RightClickItemDescriptor { Text = "Duplicate", Handler = () => DuplicateClick(null, EventArgs.Empty) });
            list.AddRange(RemoveItems());
            list.Add(RightClickItemDescriptor.Separator);

            // Ordering
            list.Add(new RightClickItemDescriptor { Text = "^^ Move to Top", Handler = () => MoveToTopClick(null, EventArgs.Empty), ShortcutKeyDisplayString = "Alt+Shift+Up" });
            list.Add(new RightClickItemDescriptor { Text = "^ Move Up", Handler = () => MoveUpClick(null, EventArgs.Empty), ShortcutKeyDisplayString = "Alt+Up" });
            list.Add(new RightClickItemDescriptor { Text = "v Move Down", Handler = () => MoveDownClick(null, EventArgs.Empty), ShortcutKeyDisplayString = "Alt+Down" });
            list.Add(new RightClickItemDescriptor { Text = "vv Move to Bottom", Handler = () => MoveToBottomClick(null, EventArgs.Empty), ShortcutKeyDisplayString = "Alt+Shift+Down" });
            list.Add(RightClickItemDescriptor.Separator);

            // Maintenance
            list.Add(new RightClickItemDescriptor { Text = "Find All References", Handler = () => FindAllReferencesClick(null, EventArgs.Empty) });
            list.Add(new RightClickItemDescriptor { Text = "Go to Definition", Handler = () => GlueCommands.Self.DialogCommands.GoToDefinitionOfSelection() });
            list.Add(new RightClickItemDescriptor { Text = "Edit Reset Variables", Handler = EditResetVariablesClick });
        }

        #endregion

        #region IsReferencedFileNode

        else if (targetNode.IsReferencedFile())
        {
            var rfs = targetNode.Tag as ReferencedFileSave;
            var name = rfs.GetInstanceName();
            var strippedName = FileManager.RemovePath(FileManager.RemoveExtension(rfs.Name));

            // Discovery
            list.Add(new RightClickItemDescriptor { Text = "Open", Handler = () => HandleOpen(targetNode) });
            list.Add(new RightClickItemDescriptor { Text = "View in Explorer", Handler = () => RightClickHelper.ViewInExplorerClick(targetNode), Image = FolderImage });
            list.Add(new RightClickItemDescriptor
            {
                Text = "Copy Name...",
                Handler = () => { },
                SubItems = new[]
                {
                    new RightClickItemDescriptor { Text = "Copy Path to Clipboard", Handler = () => HandleCopyToClipboardClick(targetNode) },
                    new RightClickItemDescriptor { Text = $"Copy Code Instance Name ({name})", Handler = () => Clipboard.SetText(name) },
                    new RightClickItemDescriptor { Text = $"Copy Stripped Name ({strippedName})", Handler = () => Clipboard.SetText(strippedName) },
                }
            });
            list.Add(RightClickItemDescriptor.Separator);

            // Common ops
            list.AddRange(RemoveItems());
            list.Add(RightClickItemDescriptor.Separator);

            // Maintenance / tools
            list.Add(new RightClickItemDescriptor { Text = "Find All References", Handler = () => FindAllReferencesClick(null, EventArgs.Empty) });

            if (FileManager.GetExtension(rfs.Name) == "csv" || rfs.TreatAsCsv)
            {
                list.Add(new RightClickItemDescriptor { Text = "Set Created Class", Handler = SetCreatedClassClick });
                list.Add(new RightClickItemDescriptor { Text = "Regenerate Code", Handler = () => HandleReGenerateCodeClick(targetNode) });
            }

            list.Add(new RightClickItemDescriptor { Text = "Copy to Build Folder", Handler = () => HandleCopyToBuildFolder(null, EventArgs.Empty) });
            list.Add(new RightClickItemDescriptor { Text = "Create Zip Package", Handler = () => CreateZipPackageClick(null, EventArgs.Empty) });

            bool fileMissing = fileExists != null && !fileExists(rfs);
            if (fileMissing)
                list.Add(new RightClickItemDescriptor { Text = "Create New File for Missing File", Handler = () => CreateNewFileForMissingFileClick(null, EventArgs.Empty) });
        }

        #endregion

        #region IsCustomVariable

        else if (targetNode.IsCustomVariable())
        {
            var customVar = targetNode.Tag as CustomVariable;
            var containingElement = targetNode.GetContainingElementTreeNode()?.Tag as GlueElement;

            // Common ops
            list.Add(new RightClickItemDescriptor { Text = "Duplicate", Handler = () => DuplicateClick(null, EventArgs.Empty) });
            list.AddRange(RemoveItems());
            list.Add(RightClickItemDescriptor.Separator);

            // Ordering
            list.Add(new RightClickItemDescriptor { Text = "^^ Move to Top", Handler = () => MoveToTopClick(null, EventArgs.Empty), ShortcutKeyDisplayString = "Alt+Shift+Up" });
            list.Add(new RightClickItemDescriptor { Text = "^ Move Up", Handler = () => MoveUpClick(null, EventArgs.Empty), ShortcutKeyDisplayString = "Alt+Up" });
            list.Add(new RightClickItemDescriptor { Text = "v Move Down", Handler = () => MoveDownClick(null, EventArgs.Empty), ShortcutKeyDisplayString = "Alt+Down" });
            list.Add(new RightClickItemDescriptor { Text = "vv Move to Bottom", Handler = () => MoveToBottomClick(null, EventArgs.Empty), ShortcutKeyDisplayString = "Alt+Shift+Down" });
            list.Add(RightClickItemDescriptor.Separator);

            // Maintenance
            list.Add(new RightClickItemDescriptor { Text = "Find All References", Handler = () => FindAllReferencesClick(null, EventArgs.Empty) });

            if (customVar?.SetByDerived == true && containingElement != null)
            {
                var derivedElements = ReferenceService.Self.GetAllElementsThatInheritFrom(containingElement);
                if (derivedElements.Count > 0)
                {
                    list.Add(RightClickItemDescriptor.Separator);
                    list.Add(new RightClickItemDescriptor
                    {
                        Text = "Set All Derived Values to Default",
                        Handler = () => HandleSetAllDerivedVariablesToDefault(customVar, containingElement)
                    });
                }
            }
        }

        #endregion

        #region IsCodeNode

        else if (targetNode.IsCodeNode())
        {
            list.Add(new RightClickItemDescriptor { Text = "View in Explorer", Handler = () => RightClickHelper.ViewInExplorerClick(targetNode), Image = FolderImage });
            list.Add(new RightClickItemDescriptor { Text = "Regenerate Code", Handler = () => HandleReGenerateCodeClick(targetNode) });
        }

        #endregion

        #region IsRootCodeNode

        else if (targetNode.IsRootCodeNode())
        {
            list.Add(new RightClickItemDescriptor { Text = "Regenerate Code", Handler = () => HandleReGenerateCodeClick(targetNode) });
        }

        #endregion

        #region IsDirectoryNode

        else if (targetNode.IsDirectoryNode())
        {
            bool isEntityContainingFolder = targetNode.Root.IsRootEntityNode();
            bool isScreenContainingFolder = targetNode.Root.IsRootScreenNode();

            // Creation
            list.Add(new RightClickItemDescriptor { Text = "Add Folder", Handler = () => RightClickHelper.AddFolderClick(targetNode), Image = FolderImage });

            if (isEntityContainingFolder)
            {
                list.Add(new RightClickItemDescriptor { Text = "Add Entity", Handler = () => GlueCommands.Self.DialogCommands.ShowAddNewEntityDialog(), Image = EntityImage });
                list.Add(new RightClickItemDescriptor { Text = "Import Entity", Handler = () => ImportElementClick(targetNode) });
            }
            else if (isScreenContainingFolder)
            {
                list.Add(new RightClickItemDescriptor { Text = "Add Screen", Handler = () => GlueCommands.Self.DialogCommands.ShowAddNewScreenDialog(), Image = ScreenImage });
            }
            else
            {
                // If not in the Entities tree structure, assume global content
                list.Add(AddFileSubMenu());
            }

            list.Add(RightClickItemDescriptor.Separator);

            // Discovery
            list.Add(new RightClickItemDescriptor { Text = "View Content Folder", Handler = () => ViewContentFolderInExplorer(targetNode) });

            if (!targetNode.IsChildOfGlobalContent())
                list.Add(new RightClickItemDescriptor { Text = "View Code Folder", Handler = () => ViewCodeFolderInExplorerClick(targetNode) });

            list.Add(new RightClickItemDescriptor { Text = "Copy Path to Clipboard", Handler = () => HandleCopyToClipboardClick(targetNode) });
            list.Add(RightClickItemDescriptor.Separator);

            // Destructive
            if (isEntityContainingFolder || isScreenContainingFolder)
                list.Add(new RightClickItemDescriptor { Text = "Rename Folder", Handler = () => HandleRenameFolderClick(targetNode) });
            list.Add(new RightClickItemDescriptor { Text = "Delete Folder", Handler = () => GluxCommands.Self.DeleteFolderClick(targetNode) });
        }

        #endregion

        #region IsStateListNode

        else if (targetNode.IsRootStateNode())
        {
            // We no longer support uncategorized states. They are a mess!
            list.Add(new RightClickItemDescriptor { Text = "Add State Category", Handler = () => AddStateCategoryClick(null, EventArgs.Empty) });
        }

        #endregion

        #region IsStateCategoryNode

        else if (targetNode.IsStateCategoryNode())
        {
            list.Add(new RightClickItemDescriptor { Text = "Add State", Handler = () => AddStateClick(null, EventArgs.Empty) });
            list.Add(RightClickItemDescriptor.Separator);
            list.AddRange(RemoveItems());
        }

        #endregion

        #region IsStateNode

        else if (targetNode.IsStateNode())
        {
            list.Add(new RightClickItemDescriptor { Text = "Duplicate", Handler = () => DuplicateClick(null, EventArgs.Empty) });
            list.AddRange(RemoveItems());
            list.Add(RightClickItemDescriptor.Separator);
            list.Add(new RightClickItemDescriptor { Text = "Fill Values from Variables", Handler = () => mFillValuesFromVariables_Click(null, EventArgs.Empty) });
        }

        #endregion

        #region IsEventTreeNode

        else if (targetNode.IsEventResponseTreeNode())
        {
            list.AddRange(RemoveItems());
        }

        #endregion

        #region All Nodes

        if (action == MenuShowingAction.RegularRightClick)
        {
            list.Add(RightClickItemDescriptor.Separator);
            list.Add(new RightClickItemDescriptor { Text = "Bookmark", Handler = () => PluginManager.CallPluginMethod("Tree View Plugin", "AddBookmark", targetNode), Image = BookmarkImage });
        }

        #endregion

        return list;
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


    private static void EditResetVariablesClick()
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
                    nos.VariablesToReset.RemoveAt(i);
            }
            StringFunctions.RemoveDuplicates(nos.VariablesToReset);
            GluxCommands.Self.SaveProjectAndElements();
            GlueCommands.Self.GenerateCodeCommands.GenerateCurrentElementCode();
        }
    }

    private static void SetCreatedClassClick()
    {
        CustomClassWindow ccw = new CustomClassWindow();
        ccw.SelectFile(GlueState.Self.CurrentReferencedFileSave);
        ccw.ShowDialog(MainGlueWindow.Self);
        GlueCommands.Self.ProjectCommands.SaveProjects();
        GluxCommands.Self.SaveProjectAndElements();
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

    static void mFillValuesFromVariables_Click(object sender, EventArgs e)
    {
        StateSave stateSave = GlueState.Self.CurrentStateSave;
        IElement element = GlueState.Self.CurrentElement;

        var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(
            String.Format("Are you sure you want to fill all values in the {0} State from the default variable values?  All previous values will be lost", stateSave.Name),
            "Fill values from default?");

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

    private static async void HandleSetAllDerivedVariablesToDefault(CustomVariable baseVariable, GlueElement baseElement)
    {
        var refs = ReferenceService.Self.GetCustomVariableReferences(baseVariable, baseElement);
        var changes = refs.DerivedVariableReferences
            .Where(r => r.Variable.DefaultValue != null)
            .ToList();

        if (changes.Count == 0)
        {
            GlueCommands.Self.DialogCommands.ShowMessageBox(
                $"All derived values for '{baseVariable.Name}' are already at the default value.",
                "No changes");
            return;
        }

        var baseDefault = baseVariable.DefaultValue?.ToString() ?? "(null)";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Reset '{baseVariable.Name}' to default ({baseDefault}) in:");
        sb.AppendLine();
        foreach (var (element, variable) in changes)
        {
            var current = variable.DefaultValue?.ToString() ?? "(null)";
            sb.AppendLine($"  {element.Name}: {current} \u2192 {baseDefault}");
        }

        var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(
            sb.ToString(),
            "Set all derived values to default");

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            foreach (var (element, variable) in changes)
            {
                var oldValue = variable.DefaultValue;
                variable.DefaultValue = null;

                await FlatRedBall.Glue.Services.Builder.Get<CustomVariableSaveSetPropertyLogic>().ReactToCustomVariableChangedValue(
                    "DefaultValue", variable, oldValue);

                GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element, generateDerivedElements: false);
            }
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
        ElementReferenceListWindow erlw = new ElementReferenceListWindow(ReferenceService.Self);
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
            Message = "Enter new folder name",
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
            GlueCommands.Self.DialogCommands.ShowMessageBox("You must first load or create a Glue project");
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
                    GlueCommands.Self.DialogCommands.ShowMessageBox(String.Format("This {0} does not have a content folder. It will be created when a file is added to this {1}.", screenOrEntity, screenOrEntity));
                }
                else
                {
                    GlueCommands.Self.DialogCommands.ShowMessageBox("Glue has not created this content folder yet since it doesn't contain any files.");
                }
            }
        }
    }


    static void HandleRenameFolderClick(ITreeNode treeNode)
    {
        CustomizableTextInputWindow inputWindow = new()
        {
            Message = "Enter new folder name",
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
                GlueCommands.Self.DialogCommands.ShowMessageBox("This object has a null source file.", "Error opening folder");
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
            GlueCommands.Self.DialogCommands.ShowMessageBox(String.Format("Could not create packaged file - likely because the file {0} contains files that are not relative.", rfs.Name));
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
