using System;
using System.Collections.Generic;
using System.Linq;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Reflection;
using System.ComponentModel;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.CodeGeneration.Game1;
using FlatRedBall.IO;
using GlueFormsCore.Controls;
using GeneralResponse = ToolsUtilities.GeneralResponse;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.FormHelpers;
using GlueFormsCore.ViewModels;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using static FlatRedBall.Glue.Plugins.PluginManager;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Windows;
using System.Windows.Media;
using WpfDataUi.DataTypes;

namespace FlatRedBall.Glue.Plugins
{
    #region Tab Location Enum

    public enum TabLocation
    {
        Top,
        Left,
        Center,
        Right,
        Bottom
    }

    #endregion

    #region TreeNodeAction
    public enum TreeNodeAction
    {
        Grabbed,
        Released
    }
    #endregion

    #region StateCategoryVariableAction

    public enum StateCategoryVariableAction
    {
        Included,
        Excluded
    }

    #endregion

    #region PluginTab
    public class PluginTab
    {
        public event EventHandler AfterHide;

        public string Title
        {
            get => Page.Title;
            set => Page.Title = value;
        }

        public TabLocation SuggestedLocation
        {
            get; set;
        } = TabLocation.Center;

        PluginTabPage page;
        internal PluginTabPage Page
        {
            get => page;
            set
            {
                if (page != value)
                {
                    page = value;
                    page.TabSelected = RaiseTabShown;
                }
            }
        }

        public void RaiseTabShown() => TabShown?.Invoke();

        public event Action TabShown;

        public void Hide()
        {
            var items = Page.ParentTabControl;
            items?.Remove(Page);
            Page.ParentTabControl = null;
            AfterHide?.Invoke(this, null);
        }

        public bool IsShown => Page.ParentTabControl != null;

        public void Show()
        {
            if (Page.ParentTabControl == null)
            {
                var items = GetTabContainerFromLocation(SuggestedLocation);
                items.Add(Page);
                Page.ParentTabControl = items;
                Page.RefreshRightClickCommands();

            }
        }

        /// <summary>
        /// Selects this tab so it is visible in its tab group.
        /// </summary>
        public void Focus()
        {
            GlueCommands.Self.DoOnUiThread(() => Page.Focus());
            Page.RecordLastClick();
        }

        public bool CanClose
        {
            get => Page.DrawX;
            set => Page.DrawX = value;
        }

        public void ForceLocation(TabLocation tabLocation)
        {
            var desiredTabControl = GetTabContainerFromLocation(tabLocation);
            var parentTabControl = Page.ParentTabControl;

            if (desiredTabControl != parentTabControl)
            {
                if (parentTabControl != null)
                {
                    parentTabControl.Remove(Page);
                }

                desiredTabControl.Add(Page);
                Page.ParentTabControl = desiredTabControl;
                Page.RefreshRightClickCommands();
            }
        }

        static TabContainerViewModel GetTabContainerFromLocation(TabLocation tabLocation)
        {
            TabContainerViewModel tabContainer = null;

            switch (tabLocation)
            {
                case TabLocation.Top: tabContainer = PluginManager.TabControlViewModel.TopTabItems; break;
                case TabLocation.Left: tabContainer = PluginManager.TabControlViewModel.LeftTabItems; break;
                case TabLocation.Center: tabContainer = PluginManager.TabControlViewModel.CenterTabItems; break;
                case TabLocation.Right: tabContainer = PluginManager.TabControlViewModel.RightTabItems; break;
                case TabLocation.Bottom: tabContainer = PluginManager.TabControlViewModel.BottomTabItems; break;
            }

            return tabContainer;
        }

        public Func<string, bool> IsPreferredDisplayerForType
        {
            get => page.IsPreferredDisplayerForType;
            set => page.IsPreferredDisplayerForType = value;
        }
    }
    #endregion

    #region VariableChangeArguments

    public class VariableChangeArguments
    {
        public string ChangedMember { get; set; }
        public object OldValue { get; set; }
        public NamedObjectSave NamedObject { get; set; }
        public bool RecordUndo { get; set; } = true;
        public SetPropertyCommitType CommitType { get; set; } = SetPropertyCommitType.Full;
        public override string ToString()
        {
            return $"{NamedObject}.{ChangedMember}";
        }
    }

    #endregion

    public abstract class PluginBase : IPlugin
    {
        Dictionary<ToolStripMenuItem, ToolStripMenuItem> toolStripItemsAndParents = new Dictionary<ToolStripMenuItem, ToolStripMenuItem>();
        protected static ConcurrentDictionary<Guid, object> PluginStorage = new ConcurrentDictionary<Guid, object>();

        #region Fields/Properties

        public string PluginFolder
        {
            get;
            set;
        }

        public virtual string FriendlyName => GetType().Name;
        public virtual Version Version => new Version(1, 0);
        public virtual string GithubRepoOwner => null;
        public virtual string GithubRepoName => null;
        public virtual bool CheckGithubForNewRelease => false;

        protected PluginTabPage PluginTab { get; private set; } // This is the tab that will hold our control

        List<AssetTypeInfo> AddedAssetTypeInfos = new List<AssetTypeInfo>();

        #endregion

        #region Delegates

        public AddNewFileOptionsDelegate AddNewFileOptionsHandler { get; protected set; }
        public CreateNewFileDelegate CreateNewFileHandler { get; protected set; }

        public InitializeMenuDelegate InitializeMenuHandler { get; protected set; }

        /// <summary>
        /// Action raised when a new Glue screen is created.
        /// </summary>
        public Func<ScreenSave, Task> NewScreenCreated { get; protected set; }
        public Action<EntitySave> NewEntityCreated { get; protected set; }

        public Action<ScreenSave, AddScreenWindow> NewScreenCreatedWithUi { get; protected set; }
        public Action<EntitySave, AddEntityWindow> NewEntityCreatedWithUi { get; protected set; }

        public OnErrorOutputDelegate OnErrorOutputHandler { get; protected set; }
        public OnOutputDelegate OnOutputHandler { get; protected set; }

        /// <summary>
        /// Raised when the user clicks the menu item to open a project.  This allows plugins to handle opening projects in other
        /// IDEs (like Eclipse).
        /// </summary>
        public OpenProjectDelegate OpenProjectHandler { get; protected set; }
        public OpenSolutionDelegate OpenSolutionHandler { get; protected set; }

        /// <summary>
        /// Delegate raised whenever a property on either a variable or an element has changed.
        /// </summary>
        /// <remarks>
        /// New plugins should use ReactToChangedNamedObjectVariableList instead if they intend to handle variables specifically
        /// </remarks>
        public ReactToChangedPropertyDelegate ReactToChangedPropertyHandler { get; protected set; }
        public Action<List<NamedObjectSavePropertyChange>> ReactToChangedNamedObjectPropertyList { get; protected set; }
        [Obsolete("Use ReactToFileChange")]
        public ReactToFileChangeDelegate ReactToFileChangeHandler { get; protected set; }
        public Action<FilePath, FileChangeType> ReactToFileChange { get; protected set; }

        public ReactToFileChangeDelegate ReactToBuiltFileChangeHandler { get; protected set; }


        public Action ReactToChangedStartupScreen { get; protected set; }
        public Action<FilePath> ReactToCodeFileChange { get; protected set; }

        /// <summary>
        /// Delegate raised when a tree node is selected. If multiple tree nodes are selected, only the first is passed
        /// to this delegate. For multi-select support, use ReactToItemsSelected;
        /// </summary>
        public ReactToItemSelectDelegate ReactToItemSelectHandler { get; protected set; }

        /// <summary>
        /// Delegate raised when the tree node selection changes. The argument list is all of the currently-selected tree nodes.
        /// </summary>
        public Action<List<ITreeNode>> ReactToItemsSelected { get; protected set; }


        /// <summary>
        /// Delegate raised when a NamedObjectSave's variable or property changed. 
        /// </summary>
        public ReactToNamedObjectChangedValueDelegate ReactToNamedObjectChangedValue { get; protected set; }
        public Action<List<VariableChangeArguments>> ReactToNamedObjectChangedValueList { get; protected set; }

        /// <summary>
        /// Delegate called when the user creates a new ReferencedFileSave (adds a new file to the Glue project)
        /// </summary>
        public ReactToNewFileDelegate ReactToNewFileHandler { get; protected set; }

        /// <summary>
        /// Delegate called whenever a new NamedObjectSave is added.
        /// </summary>
        public ReactToNewObjectDelegate ReactToNewObjectHandler { get; protected set; }

        /// <summary>
        /// Delegate called whenever a group of new NamedObjectSaves is added. If this is null, then the PluginManager
        /// falls back to calling ReactToNewObjectHandler.
        /// </summary>
        public Action<List<NamedObjectSave>> ReactToNewObjectList { get; protected set; }
        public Func<List<NamedObjectSave>, Task> ReactToNewObjectListAsync { get; protected set; }

        public Action<IElement, NamedObjectSave> ReactToObjectRemoved { get; protected set; }
        public Action<List<GlueElement>, List<NamedObjectSave>> ReactToObjectListRemoved { get; protected set; }


        /// <summary>
        /// Delegate raised when right-clicking on the property grid.
        /// </summary>
        public ReactToRightClickDelegate ReactToRightClickHandler { get; protected set; }


        public ReactToTreeViewRightClickDelegate ReactToTreeViewRightClickHandler { get; protected set; }

        public Action<StateSave, StateSaveCategory> ReactToStateCreated { get; protected set; }
        public Action<StateSave, StateSaveCategory, string> ReactToStateVariableChanged { get; protected set; }
        public ReactToStateNameChangeDelegate ReactToStateNameChangeHandler { get; protected set; }
        public ReactToStateRemovedDelegate ReactToStateRemovedHandler { get; protected set; }

        public Action<IElement, ReferencedFileSave> ReactToFileRemoved { get; protected set; }

        /// <summary>
        /// Delegate raised whenever an entity is going to be removed. The first argument
        /// (EntitySave) is the entity to remove. The string list argument is 
        /// a list of to-be-removed files. Entities can add addiitonal files.
        /// </summary>
        public Action<EntitySave, List<string>> ReactToEntityRemoved { get; protected set; }

        /// <summary>
        /// Delegate raised whenever a Screen is removed. The first argument is the screen
        /// which is being removed. The second argument is a list of files to remove. Plugins
        /// can optionally add additional files to-be-removed when a Screen is removed.
        /// </summary>
        public Action<ScreenSave, List<string>> ReactToScreenRemoved { get; protected set; }

        public Action<IElement, EventResponseSave> ReactToEventRemoved { get; protected set; }

        /// <summary>
        /// Action raised when a variable changes. The IElement is the container of the variable, the CustomVariable is the changed variable.
        /// </summary>
        public Action<IElement, CustomVariable> ReactToElementVariableChange { get; protected set; }

        /// <summary>
        /// Raised whenever an element (screen or entity) is renamed. First parameter is the
        /// renamed element, the second is the old name. The element will already have its new
        /// name assigned.
        /// </summary>
        public Action<IElement, string> ReactToElementRenamed { get; protected set; }

        public Action<string> SelectItemInCurrentFile { get; protected set; }

        public Action ReactToLoadedGluxEarly { get; protected set; }

        /// <summary>
        /// Delegate raised after a project is loaded, but before any code has been generated.
        /// </summary>
        public Action ReactToLoadedGlux { get; protected set; }

        public Action<AddEntityWindow> ModifyAddEntityWindow { get; protected set; }
        public Action<AddScreenWindow> ModifyAddScreenWindow { get; protected set; }

        /// <summary>
        /// Raised right before a project is unloaded. Glue will still report the project as loaded, so that plugins can
        /// react to a specific project unloading (such as by saving content).
        /// </summary>
        public Action ReactToUnloadedGlux { get; protected set; }
        public TryHandleCopyFileDelegate TryHandleCopyFile { get; protected set; }

        /// <summary>
        /// Raised when an object references a file and needs to know the contained objects.  
        /// Returned values contain the name of the object followed by the type of the object in 
        /// parenthesis.  Example of returned file:  "UntexturedSprite (Sprite)"
        /// </summary>
        public TryAddContainedObjectsDelegate TryAddContainedObjects { get; protected set; }

        public AdjustDisplayedScreenDelegate AdjustDisplayedScreen { get; protected set; }

        public AdjustDisplayedEntityDelegate AdjustDisplayedEntity { get; protected set; }

        [Obsolete("Use FillWithReferencedFiles instead", error: true)]
        public Action<string, EditorObjects.Parsing.TopLevelOrRecursive, List<string>> GetFilesReferencedBy { get; protected set; }

        public Func<FilePath, List<FilePath>, GeneralResponse> FillWithReferencedFiles { get; protected set; }
        public Action<FilePath, GeneralResponse> ReactToFileReadError { get; protected set; }

        public Action<string, List<FilePath>> GetFilesNeededOnDiskBy { get; protected set; }

        public Action ResolutionChanged { get; protected set; }

        /// <summary>
        /// Responsible for returning whether the argument file can return content.  The file shouldn't be opened
        /// here, only the extension should be investigated to see if the file can potentially reference content.
        /// </summary>
        /// <remarks>
        /// The plugin should not open the file here (if possible) as this event will be raised a lot, and it should
        /// be very fast and not hit the disk.
        /// </remarks>
        public Func<string, bool> CanFileReferenceContent { get; protected set; }

        public AdjustDisplayedReferencedFileDelegate AdjustDisplayedReferencedFile { get; protected set; }
        public AdjustDisplayedCustomVariableDelegate AdjustDisplayedCustomVariable { get; protected set; }

        /// <summary>
        /// Adjusts the properties for the selected NamedObject (not Variables window).
        /// </summary>
        /// <remarks>
        /// To modify variables, the best way is to change the AssetTypeInfo. There is (currently) no way to dynamically add/remove variables from the property grid.</remarks>
        public AdjustDisplayedNamedObjectDelegate AdjustDisplayedNamedObject { get; protected set; }

        public Func<IElement, IEnumerable<VariableDefinition>> GetVariableDefinitionsForElement;

        public Action<NamedObjectSave, List<ExposableEvent>> AddEventsForObject { get; protected set; }
        public Action<ProjectBase> ReactToLoadedSyncedProject { get; protected set; }

        public GetEventTypeAndSignature GetEventSignatureArgs { get; protected set; }

        /// <summary>
        /// Function to return a type converter given a member as defined by the parameters:
        /// IElement container, NamedObjectSave instance, Type memberType, string memberName, string customType, TypeConverter ReturnValue
        /// </summary>
        public Func<IElement, NamedObjectSave, Type, string, string, TypeConverter> GetTypeConverter { get; protected set; }
        public Action<NamedObjectSave, ICodeBlock, InstructionSave> WriteInstanceVariableAssignment { get; protected set; }

        /// <summary>
        /// Action to raise whenever a ReferencedFileSave value changes. 
        /// string - The name of the variable that changed
        /// object - The old value for the variable
        /// </summary>
        public Action<string, object> ReactToReferencedFileChangedValueHandler { get; protected set; }
        public Action<CustomVariable> ReactToVariableAdded { get; protected set; }
        public Action<CustomVariable> ReactToVariableRemoved { get; protected set; }


        public Func<string, bool> GetIfUsesContentPipeline { get; protected set; }

        /// <summary>
        /// Delegate used to return additional types used by the plugin. Currently this is only used to populate dropdowns, so plugins only need to return enumerations, but eventually
        /// this could be used for other functionality.
        /// </summary>
        public Func<List<Type>> GetUsedTypes { get; protected set; }

        public Func<ReferencedFileSave, List<AssetTypeInfo>> GetAvailableAssetTypes { get; protected set; }

        public Func<NamedObjectSave, NamedObjectSave, Task<NamedObjectSave>> ReactToCreateCollisionRelationshipsBetween { get; protected set; }

        public Func<ITreeNode, bool> TryHandleTreeNodeDoubleClicked { get; protected set; }

        public Action<ReferencedFileSave> ReactToFileBuildCommand { get; protected set; }

        public Action<GlueElement> ReactToImportedElement { get; protected set; }

        /// <summary>
        /// Delegate raised whenever an object (first NamedObjectSave) has its container (list or ShapeCollection)
        /// changed. The second parameter is the new container which may be null if the object is being moved out of a list.
        /// </summary>
        public Action<NamedObjectSave, NamedObjectSave> ReactToObjectContainerChanged { get; protected set; }
        public Func<List<ObjectContainerChange>, Task> ReactToObjectListContainerChanged { get; protected set; }

        public Action ReactToMainWindowMoved { get; protected set; }
        public Action ReactToMainWindowResizeEnd { get; protected set; }



        // TreeNode Methods
        public Action<GlueElement, TreeNodeRefreshType> RefreshTreeNodeFor;
        public Action RefreshGlobalContentTreeNode;
        public Action RefreshDirectoryTreeNodes;
        public Action<ITreeNode, TreeNodeAction> GrabbedTreeNodeChanged;

        public Action FocusOnTreeView;

        public Action ReactToCtrlF;

        public Action<System.Windows.Input.Key> ReactToCtrlKey;

        public Action ReactToGlobalTimer;

        public Action<StateSaveCategory, string, StateCategoryVariableAction> ReactToStateCategoryExcludedVariablesChanged;

        public Action<string, string> ReactToScreenJsonSave;
        public Action<string, string> ReactToEntityJsonSave;
        public Action<string> ReactToGlueJsonSave;

        public Action<string, string> ReactToScreenJsonLoad;
        public Action<string, string> ReactToEntityJsonLoad;
        public Action<string> ReactToGlueJsonLoad;

        public Action<int?> ReactToSelectedSubIndexChanged;

        public event Action<IPlugin, string, string> ReactToPluginEventAction;
        public event Action<IPlugin, string, string> ReactToPluginEventWithReturnAction;
        protected void ReactToPluginEvent(string eventName, string payload)
        {

            if (ReactToPluginEventAction != null)
                ReactToPluginEventAction(this, eventName, payload);
        }

        protected void ReactToPluginEvent(string eventName, object payload)
        {

            if (ReactToPluginEventAction != null)
                ReactToPluginEventAction(this, eventName, JsonConvert.SerializeObject(payload));
        }

        public Action<CustomVariable> TryAssignPreferredDisplayerFromName;

        private ConcurrentDictionary<Guid, string> _pendingRequests = new ConcurrentDictionary<Guid, string>();
        protected async Task<string> ReactToPluginEventWithReturn(string eventName, string payload)
        {
            var id = Guid.NewGuid();
            if (!_pendingRequests.TryAdd(id, null))
                throw new Exception("Failed to add pending request");

            ReactToPluginEventWithReturnAction(this, eventName, JsonConvert.SerializeObject(new
            {
                Id = id,
                Payload = payload
            }));

            while (_pendingRequests.TryGetValue(id, out var result) && result == null)
            {
                await Task.Delay(10);
            }

            _pendingRequests.TryRemove(id, out var result2);

            return result2;
        }

        /// <summary>
        /// Raised whenever an object is reordered. The object may be a NamedObjectSave, StateSave, or CustomVariable. The integer values are
        /// oldIndex, newIndex, respectively.
        /// </summary>
        public Func<object, int, int, Task> ReactToObjectReordered;

        /// <summary>
        /// Raised to determine if a plugin should handle a hotkey. A plugin should return true if it has its own hotkey handling given
        /// its current state, such as if a control has focus.
        /// </summary>
        public Func<bool> IsHandlingHotkeys;

        #endregion

        public abstract void StartUp();

        public virtual bool ShutDown(PluginShutDownReason shutDownReason)
        {
            UnregisterAllCodeGenerators();
            UnregisterAssetTypeInfos();
            this.RemoveAllMenuItems();
            return true;
        }

        #region Menu items

        protected ToolStripMenuItem AddTopLevelMenuItem(string whatToAdd, string id)
        {
            ToolStripMenuItem menuItem = new ToolStripMenuItem(whatToAdd);
            menuItem.Name = id;
            GlueGui.MenuStrip.Items.Add(menuItem);
            return menuItem;
        }

        protected ToolStripMenuItem AddMenuItemTo(string whatToAdd, string whatToAddId, EventHandler eventHandler, string container)
        {
            return AddMenuItemTo(whatToAdd, whatToAddId, eventHandler, container, -1);
        }

        protected ToolStripMenuItem AddMenuItemTo(string whatToAdd, string whatToAddId, Action action, string container)
        {
            return AddMenuItemTo(whatToAdd, whatToAddId, (not, used) => action?.Invoke(), container, -1);
        }

        protected ToolStripMenuItem AddMenuItemTo(string whatToAdd, string whatToAddId, EventHandler eventHandler, string container, int preferredIndex)
        {
            var menuItem = new ToolStripMenuItem(whatToAdd, null, eventHandler);
            menuItem.Name = whatToAddId;
            var itemToAddTo = GetItem(container);
            Debug.Assert(itemToAddTo != null);
            toolStripItemsAndParents.Add(menuItem, itemToAddTo);


            if (preferredIndex == -1)
            {
                itemToAddTo.DropDownItems.Add(menuItem);
            }
            else
            {
                int indexToInsertAt = System.Math.Min(preferredIndex, itemToAddTo.DropDownItems.Count);

                itemToAddTo.DropDownItems.Insert(indexToInsertAt, menuItem);
            }

            return menuItem;
        }

        ToolStripMenuItem GetItem(string name)
        {
            foreach (ToolStripMenuItem item in GlueGui.MenuStrip.Items)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }
            return null;
        }

        public void RemoveAllMenuItems()
        {
            foreach (var kvp in toolStripItemsAndParents)
            {
                // need to invoke this on the main thread:

                kvp.Value.DropDownItems.Remove(kvp.Key);
            }

        }

        #endregion

        #region Toolbar

        static SolidColorBrush ToolbarBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, 240, 240, 240));

        protected void AddToToolBar(System.Windows.Controls.UserControl control, string toolbarName)
        {
            var tray = PluginManager.ToolBarTray;

            var toAddTo = tray.ToolBars.FirstOrDefault(item => item.Name == toolbarName);

            if (toAddTo == null)
            {
                toAddTo = new System.Windows.Controls.ToolBar();
                toAddTo.Loaded += Toolbar_Loaded;
                toAddTo.Name = toolbarName;
                tray.ToolBars.Add(toAddTo);
            }

            if (tray.ToolBars.Count == 1 && toAddTo.Items.Count == 0)
            {
                // Align first item with the other main window panels
                control.Margin = new System.Windows.Thickness(1, 0, 3, 0);
            }
            else
            {
                control.Margin = new System.Windows.Thickness(3, 0, 3, 0);
            }

            // Remove default color for background
            control.Background = ToolbarBackgroundBrush;
            control.BorderBrush = ToolbarBackgroundBrush;

            if(control.Parent == null)
            {
                toAddTo.Items.Add(control);

            }

        }

        private void Toolbar_Loaded(object sender, RoutedEventArgs e)
        {
            // This removes the overflow buttons at the end of the toolbars
            System.Windows.Controls.ToolBar toolBar = sender as System.Windows.Controls.ToolBar;

            var overflowGrid = toolBar.Template.FindName("OverflowGrid", toolBar) as FrameworkElement;
            if (overflowGrid != null)
            {
                overflowGrid.Visibility = Visibility.Collapsed;
            }

            var mainPanelBorder = toolBar.Template.FindName("MainPanelBorder", toolBar) as FrameworkElement;
            if (mainPanelBorder != null)
            {
                mainPanelBorder.Margin = new Thickness(0);
            }

            // Remove default color for background
            toolBar.Background = ToolbarBackgroundBrush;
            toolBar.BorderBrush = ToolbarBackgroundBrush;
        }

        protected bool RemoveFromToolbar(System.Windows.Controls.UserControl control, string toolbarName)
        {
            var tray = PluginManager.ToolBarTray;

            var toRemoveFrom = tray.ToolBars.FirstOrDefault(item => item.Name == toolbarName);

            bool wasRemoved = false;

            if (toRemoveFrom != null)
            {
                toRemoveFrom.Items.Remove(control);
                wasRemoved = true;
            }

            return wasRemoved;
        }

        #endregion

        #region Code Generation

        List<ElementComponentCodeGenerator> CodeGenerators
        {
            get;
            set;
        } = new List<ElementComponentCodeGenerator>();
        List<Game1CodeGenerator> GameCodeGenerators
        {
            get;
            set;
        } = new List<Game1CodeGenerator>();

        /// <summary>
        /// Registers the argument ElementComponentCodeGenerator to Glue's code generator list. 
        /// This method also registers the code generator so that it belongs to this plugin in case
        /// the plugin wants to remove it later.
        /// Code generators that are added here run on every element, and it is up to the 
        /// code generator to decide if it should actually write code.
        /// </summary>
        /// <param name="codeGenerator">The component to add.</param>
        public void RegisterCodeGenerator(ElementComponentCodeGenerator codeGenerator)
        {
            CodeGenerators.Add(codeGenerator);
            CodeWriter.CodeGenerators.Add(codeGenerator);
        }

        public void RegisterCodeGenerator(Game1CodeGenerator gameCodeGenerator)
        {
            GameCodeGenerators.Add(gameCodeGenerator);

            Game1CodeGeneratorManager.Generators.Add(gameCodeGenerator);
        }

        public void UnregisterAllCodeGenerators()
        {
            CodeWriter.CodeGenerators.RemoveAll(item => CodeGenerators.Contains(item));
            Game1CodeGeneratorManager.Generators.RemoveAll(item => GameCodeGenerators.Contains(item));

            CodeGenerators.Clear();
            GameCodeGenerators.Clear();
        }

        #endregion

        #region Errors

        List<ErrorReporterBase> ErrorReporters = new List<ErrorReporterBase>();

        protected void AddErrorReporter(ErrorReporterBase errorReporter)
        {
            ErrorReporters.Add(errorReporter);
            EditorObjects.IoC.Container.Get<GlueErrorManager>().Add(errorReporter);
        }

        /// <summary>
        /// Refreshes all errors for all ErrorReporters referenced by this plugin.
        /// </summary>
        protected void RefreshErrors()
        {
            foreach (var reporter in ErrorReporters)
            {
                GlueCommands.Self.RefreshCommands.RefreshErrorsFor(reporter);
            }
        }

        #endregion

        protected void AddAssetTypeInfo(AssetTypeInfo ati)
        {
            // see if it already exists
            var alreadyExists = AddedAssetTypeInfos.Any(item => item.QualifiedRuntimeTypeName.QualifiedType == ati.QualifiedRuntimeTypeName.QualifiedType);
            if (!alreadyExists)
            {
                AddedAssetTypeInfos.Add(ati);
                AvailableAssetTypes.Self.AddAssetType(ati);
            }
        }

        public void UnregisterAssetTypeInfos()
        {
            foreach (var ati in AddedAssetTypeInfos)
            {
                AvailableAssetTypes.Self.RemoveAssetType(ati);
            }
        }

        #region Tab Methods

        public PluginTab CreateTab(System.Windows.FrameworkElement control, string tabName, TabLocation defaultLocation = TabLocation.Right)
        {
            //System.Windows.Forms.Integration.ElementHost wpfHost;
            //wpfHost = new System.Windows.Forms.Integration.ElementHost();
            //wpfHost.Dock = DockStyle.Fill;
            //wpfHost.Child = control;

            //return CreateTab(wpfHost, tabName);
            var page = new PluginTabPage();
            page.Resources = MainPanelControl.ResourceDictionary;

            page.Title = tabName;
            page.Content = control;
            control.Resources = MainPanelControl.ResourceDictionary;

            PluginTab pluginTab = new PluginTab();
            pluginTab.Page = page;
            page.MoveToTabSelected += async (newLocation) =>
            {
                pluginTab.ForceLocation(newLocation);
                pluginTab.SuggestedLocation = newLocation;
                pluginTab.Focus();

                await GlueCommands.Self.UpdateGlueSettingsFromCurrentGlueStateAsync();
            };

            var settings = GlueState.Self.GlueSettingsSave;
            if (settings.TopTabs.Contains(tabName))
            {
                pluginTab.SuggestedLocation = TabLocation.Top;
            }
            else if (settings.LeftTabs.Contains(tabName))
            {
                pluginTab.SuggestedLocation = TabLocation.Left;
            }
            else if (settings.CenterTabs.Contains(tabName))
            {
                pluginTab.SuggestedLocation = TabLocation.Center;
            }
            else if (settings.RightTabs.Contains(tabName))
            {
                pluginTab.SuggestedLocation = TabLocation.Right;
            }
            else if (settings.BottomTabs.Contains(tabName))
            {
                pluginTab.SuggestedLocation = TabLocation.Bottom;
            }
            else
            {
                pluginTab.SuggestedLocation = defaultLocation;
            }

            page.ClosedByUser += (sender) =>
            {
                pluginTab.Hide();
                //OnClosedByUser(sender);
            };

            return pluginTab;

        }

        public PluginTab CreateTab(System.Windows.Forms.Control control, string tabName, TabLocation tabLocation = TabLocation.Right)
        {
            var host = new System.Windows.Forms.Integration.WindowsFormsHost();

            host.Child = control;

            return CreateTab(host, tabName, tabLocation);
        }

        public PluginTab CreateAndAddTab(System.Windows.Forms.Control control, string tabName, TabLocation tabLocation = TabLocation.Right)
        {
            var tab = CreateTab(control, tabName, tabLocation);
            tab.Show();
            return tab;
        }

        public PluginTab CreateAndAddTab(System.Windows.Controls.UserControl control, string tabName, TabLocation tabLocation = TabLocation.Right)
        {
            var tab = CreateTab(control, tabName, tabLocation);
            tab.Show();
            return tab;
        }

        void OnClosedByUser(object sender)
        {
            PluginManager.ShutDownPlugin(this);
        }


        #endregion

        #region Overrideable Methods
        public virtual void HandleEvent(string eventName, string payload)
        {
        }

        public async Task<string> HandleEventWithReturn(string eventName, string payload)
        {
            var container = JObject.Parse(payload);

            if (container == null || !container.ContainsKey("Id") || !container.ContainsKey("Payload"))
                return null;

            var returnValue = await HandleEventWithReturnImplementation(eventName, container.Value<string>("Payload"));

            if (returnValue == null)
                return null;
            else
                return JsonConvert.SerializeObject(new
                {
                    Id = Guid.Parse(container.Value<string>("Id")),
                    Payload = returnValue
                });
        }

        protected virtual async Task<string> HandleEventWithReturnImplementation(string eventName, string payload)
        {
            return await Task.Run(() =>
            {
                return (string)null;
            });
        }

        public void HandleEventResponseWithReturn(string payload)
        {
            var container = JObject.Parse(payload);

            if (container == null || !container.ContainsKey("Id") || !container.ContainsKey("Payload"))
                return;

            _pendingRequests.TryUpdate(Guid.Parse(container.Value<string>("Id")), container.Value<string>("Payload"), null);
        }

        #endregion

    }
}
