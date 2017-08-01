using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.Plugins.Interfaces;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.AutomatedGlue;
using FlatRedBall.Glue.VSHelpers.Projects;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Reflection;
using FlatRedBall.Instructions.Reflection;
using System.ComponentModel;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Content.Instructions;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.Plugins
{
    public abstract class PluginBase : IPlugin
    {
        Dictionary<ToolStripMenuItem, ToolStripMenuItem> mItemsAndParents = new Dictionary<ToolStripMenuItem, ToolStripMenuItem>();

        List<ElementComponentCodeGenerator> CodeGenerators
        {
            get;
            set;
        } = new List<ElementComponentCodeGenerator>();

        #region Properties

        public string PluginFolder
        {
            get;
            set;
        }

        public abstract string FriendlyName { get; }
        public abstract Version Version { get; }


        protected PluginTab PluginTab { get; private set; } // This is the tab that will hold our control


        #endregion

        #region Tab related fields

        TabControl mTabContainer; // This is the tab control for all tabs


        #endregion



        #region Delegates

        public AddNewFileOptionsDelegate AddNewFileOptionsHandler { get; protected set; }
        public CreateNewFileDelegate CreateNewFileHandler { get; protected set; }

        public InitializeTabDelegate InitializeBottomTabHandler { get; protected set; }
        public InitializeTabDelegate InitializeCenterTabHandler { get; protected set; }
        public InitializeTabDelegate InitializeLeftTabHandler { get; protected set; }
        public InitializeTabDelegate InitializeRightTabHandler { get; protected set; }
        public InitializeTabDelegate InitializeTopTabHandler { get; protected set; }
        public InitializeMenuDelegate InitializeMenuHandler { get; protected set; }

        public Action<ScreenSave> ReactToNewScreenCreated { get; protected set; }

        public OnErrorOutputDelegate OnErrorOutputHandler { get; protected set; }
        public OnOutputDelegate OnOutputHandler { get; protected set; }

        /// <summary>
        /// Raised when the user clicks the menu item to open a project.  This allows plugins to handle opening projects in other
        /// IDEs (like Eclipse).
        /// </summary>
        public OpenProjectDelegate OpenProjectHandler { get; protected set; }
        public OpenSolutionDelegate OpenSolutionHandler { get; protected set; }

        public ReactToChangedPropertyDelegate ReactToChangedPropertyHandler { get; protected set; }
        public ReactToFileChangeDelegate ReactToFileChangeHandler { get; protected set; }
        public ReactToFileChangeDelegate ReactToBuiltFileChangeHandler { get; protected set; }
        public ReactToItemSelectDelegate ReactToItemSelectHandler { get; protected set; }
        public ReactToNamedObjectChangedValueDelegate ReactToNamedObjectChangedValueHandler { get; protected set; }
        public ReactToNewFileDelegate ReactToNewFileHandler { get; protected set; }
        public ReactToNewObjectDelegate ReactToNewObjectHandler { get; protected set; }
        public ReactToRightClickDelegate ReactToRightClickHandler { get; protected set; }
        public ReactToTreeViewRightClickDelegate ReactToTreeViewRightClickHandler { get; protected set; }
        public ReactToStateNameChangeDelegate ReactToStateNameChangeHandler { get; protected set; }
        public ReactToStateRemovedDelegate ReactToStateRemovedHandler { get; protected set; }

        public Action<IElement, ReferencedFileSave> ReactToFileRemoved { get; protected set; }
        public Action<IElement, EventResponseSave> ReactToEventRemoved { get; protected set; }

        public Action<IElement, CustomVariable> ReactToElementVariableChange { get; protected set; }

        public Action<string> SelectItemInCurrentFile { get; protected set; }

        public Action ReactToLoadedGluxEarly { get; protected set; }
        public Action ReactToLoadedGlux { get; protected set; }

        /// <summary>
        /// Raised whenever a project is unloaded. Glue will still report the project as loaded, so that plugins can
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

        public Action<string, EditorObjects.Parsing.TopLevelOrRecursive, List<string>> GetFilesReferencedBy { get; protected set; }
        public Action<string, List<string>> GetFilesNeededOnDiskBy { get; protected set; }



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
        public AdjustDisplayedNamedObjectDelegate AdjustDisplayedNamedObject { get; protected set; }
        public Action<NamedObjectSave, List<ExposableEvent>> AddEventsForObject { get; protected set; }
        public Action<ProjectBase> ReactToLoadedSyncedProject { get; protected set; }

        public GetEventTypeAndSignature GetEventSignatureArgs { get; protected set;}

        public Func<IElement, NamedObjectSave, TypedMemberBase, TypeConverter> GetTypeConverter { get; protected set; }
        public Action<NamedObjectSave, ICodeBlock, InstructionSave> WriteInstanceVariableAssignment { get; protected set; }

        public Action<string, object> ReactToReferencedFileChangedValueHandler { get;  protected set;}

        public Func<string, bool> GetIfUsesContentPipeline { get; protected set; }

        #endregion

        #region Methods


        public abstract void StartUp();
        public abstract bool ShutDown(PluginShutDownReason shutDownReason);


        protected ToolStripMenuItem AddMenuItemTo(string whatToAdd, EventHandler eventHandler, string container)
        {
            return AddMenuItemTo(whatToAdd, eventHandler, container, -1);
        }
        protected ToolStripMenuItem AddMenuItemTo(string whatToAdd, EventHandler eventHandler, string container, int preferredIndex)
        {
            ToolStripMenuItem menuItem = new ToolStripMenuItem(whatToAdd, null, eventHandler);
            ToolStripMenuItem itemToAddTo = GetItem(container);
            mItemsAndParents.Add(menuItem, itemToAddTo);


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

        protected void AddToToolBar(System.Windows.Controls.UserControl control, string toolbarName)
        {
            var tray = PluginManager.ToolBarTray;

            var toAddTo = tray.ToolBars.FirstOrDefault(item => item.Name == toolbarName);

            if(toAddTo == null)
            {
                toAddTo = new System.Windows.Controls.ToolBar();
                toAddTo.Name = toolbarName;
                tray.ToolBars.Add(toAddTo);
            }

            control.Padding = new System.Windows.Thickness(3,0,3,0);

            toAddTo.Items.Add(control);

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

        ToolStripMenuItem GetItem(string name)
        {
            foreach (ToolStripMenuItem item in GlueGui.MenuStrip.Items)
            {
                if (item.Text == name)
                {
                    return item;
                }
            }
            return null;
        }

        public void RemoveAllMenuItems()
        {
            foreach (var kvp in mItemsAndParents)
            {
                // need to invoke this on the main thread:

                kvp.Value.DropDownItems.Remove(kvp.Key);
            }

        }

        protected PluginTab AddToTab(System.Windows.Forms.TabControl tabContainer, System.Windows.Forms.Control control, string tabName)
        {
            mTabContainer = tabContainer;

            PluginTab = new PluginTab();

            PluginTab.ClosedByUser += new PluginTab.ClosedByUserDelegate(OnClosedByUser);

            PluginTab.Text = "  " + tabName;
            PluginTab.Controls.Add(control);
            control.Dock = DockStyle.Fill;

            mTabContainer.Controls.Add(PluginTab);

            return PluginTab;
        }

        protected PluginTab AddToTab(System.Windows.Forms.TabControl tabContainer, System.Windows.Controls.UserControl control, string tabName)
        {
            System.Windows.Forms.Integration.ElementHost wpfHost;
            wpfHost = new System.Windows.Forms.Integration.ElementHost();
            wpfHost.Dock = DockStyle.Fill;
            wpfHost.Child = control;

            return AddToTab(tabContainer, wpfHost, tabName);
        }
        
        protected void RemoveTab(PluginTab pluginTab)
        {
            if (pluginTab != null && pluginTab.Parent != null)
            {
                pluginTab.Parent.Controls.Remove(pluginTab);
            }
        }

        protected void RemoveTab()
        {
            RemoveTab(PluginTab);
        }

        void OnClosedByUser(object sender)
        {
            PluginManager.ShutDownPlugin(this);
        }

        protected void AddTab()
        {
            if(PluginTab == null)
            {
                throw new Exception("You must call AddToTab first");
            }
            var container = mTabContainer;

            ShowTab(PluginTab);
        }
        protected void ShowTab(PluginTab pluginTab)
        {
            var container = mTabContainer;

            if (pluginTab.LastTabControl != null)
            {
                container = pluginTab.LastTabControl;
            }
            if (container.Controls.Contains(pluginTab) == false)
            {
                container.Controls.Add(pluginTab);
            }
        }

        protected void FocusTab()
        {
            mTabContainer.SelectTab(PluginTab);
        }

        public void RegisterCodeGenerator(ElementComponentCodeGenerator codeGenerator)
        {
            CodeGenerators.Add(codeGenerator);
            CodeWriter.CodeGenerators.Add(codeGenerator);
        }

        public void UnregisterAllCodeGenerators()
        {
            CodeWriter.CodeGenerators.RemoveAll(item => CodeGenerators.Contains(item));
        }

        #endregion

    }
}
