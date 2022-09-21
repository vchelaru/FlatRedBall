using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.SpecializedXnaControls;
using FlatRedBall.AnimationEditorForms.Preview;
using FlatRedBall.AnimationEditorForms.Editing;
using FlatRedBall.AnimationEditorForms.IO;
using FlatRedBall.AnimationEditorForms.CommandsAndState;
using FlatRedBall.AnimationEditorForms.Controls;
using FlatRedBall.IO;
using FlatRedBall.AnimationEditorForms.Converters;
using FlatRedBall.AnimationEditorForms.Gif;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.AnimationEditorForms.ViewModels;
using FlatRedBall.SpecializedXnaControls.Scrolling;
using FlatRedBall.AnimationEditorForms.Content;
using RenderingLibrary.Content;
using FilePath = ToolsUtilities.FilePath;
using System.Threading.Tasks;
using FlatRedBall.AnimationEditorForms.Plugins.FrameShapePlugin;
using FlatRedBall.AnimationEditorForms.Plugins;

namespace FlatRedBall.AnimationEditorForms
{
    public partial class MainControl : UserControl
    {
        #region Fields

        // This causes problems if it's initialized in the winforms designer, so
        // we do it here in code.
        private SpecializedXnaControls.ImageRegionSelectionControl imageRegionSelectionControl1;
        ScrollBarControlLogic mScrollBarControlLogic;
        static MainControl mSelf;

        #endregion

        #region Properties

        public static MainControl Self
        {
            get
            {
                return mSelf;
            }
        }

        public AnimationChainListSave AnimationChainList
        {
            get
            {
                return ProjectManager.Self.AnimationChainListSave;
            }
            set
            {
                ProjectManager.Self.AnimationChainListSave = value;

                TreeViewManager.Self.RefreshTreeView();
            }
        }

        // Use SelectedState
        //public AnimationChainSave SelectedAnimationChain
        public WireframeEditControlsViewModel WireframeEditControlsViewModel
        {
            get; private set;
        }

        // todo - move this to a plugin manager eventually
        List<PluginBase> plugins = new List<PluginBase>();

        #endregion

        #region Events

        public event Action XnaInitialize;

        public event EventHandler AnimationChainChange;

        public event EventHandler AnimationChainSelected;

        #endregion

        public MainControl()
        {
            mSelf = this;
            InitializeComponent();

            InitializePlugins();

            this.animationsListToolBar1.AddAnimationClick += AddAnimationToolStripMenuItem_Click;
            this.animationsListToolBar1.ExpandAllClick += TreeViewManager.Self.HandleExpandAllTreeView;
            this.animationsListToolBar1.CollapseAllClick += TreeViewManager.Self.HandleCollapseAllTreeView;

            CreateViewModel();

            this.imageRegionSelectionControl1 = new FlatRedBall.SpecializedXnaControls.ImageRegionSelectionControl();
            mScrollBarControlLogic = new ScrollBarControlLogic(PreviewSplitContainer.Panel1, imageRegionSelectionControl1);

            ApplicationEvents.Self.WireframePanning += ()=> mScrollBarControlLogic.UpdateScrollBars(); 
            ApplicationEvents.Self.WireframeTextureChange += ScrollBarHandleTextureChange;
            ApplicationEvents.Self.AfterZoomChange += delegate
            {
                mScrollBarControlLogic.ZoomPercentage = (float)AppState.Self.WireframeZoomValue;
                mScrollBarControlLogic.UpdateScrollBars();
            };

            imageRegionSelectionControl1.Click += new EventHandler(HandleImageRegionSelectionControlClick);
            this.PreviewSplitContainer.Panel1.Controls.Add(this.imageRegionSelectionControl1);

            // 
            // imageRegionSelectionControl1
            // 
            // Winforms has issues with controls that contain custom controls
            // and if this control was in custom code, it caused all kinds of
            // issues.  So we'll just instantiate it here as a workaround.
            this.imageRegionSelectionControl1.CurrentTexture = null;
            this.imageRegionSelectionControl1.DesiredFramesPerSecond = 30F;
            this.imageRegionSelectionControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            //this.imageRegionSelectionControl1.Width = 100;
            //this.imageRegionSelectionControl1.Height = 100;
            this.imageRegionSelectionControl1.Location = new System.Drawing.Point(0, 23);
            this.imageRegionSelectionControl1.Name = "imageRegionSelectionControl1";
            this.imageRegionSelectionControl1.Size = new System.Drawing.Size(296, 264);
            this.imageRegionSelectionControl1.TabIndex = 0;
            this.imageRegionSelectionControl1.Text = "imageRegionSelectionControl1";
            this.imageRegionSelectionControl1.BringToFront();

            this.WireframeTopUiControl.PropertyChanged += HandleEditorControlsPropertyChanged;

            SelectedState.Self.Initialize(this.AnimationTreeView);
            if (this.DesignMode == false)
            {
                HandleRegionXnaInitialize();
                this.imageRegionSelectionControl1.XnaUpdate += new Action(HandleXnaUpdate);
            }
            PropertyGridManager.Self.Initialize(SelectedItemPropertyGrid, this.tileMapInfoWindow1);
            PropertyGridManager.Self.AnimationChainChange += (not, used) =>
                    ApplicationEvents.Self.RaiseAnimationChainsChanged();

            PropertyGridManager.Self.AnimationFrameChange += HandleAnimationFrameChanges;

            TreeViewManager.Self.Initialize(AnimationTreeView);
            
            TreeViewManager.Self.AnimationChainSelected += (not, used) => AnimationChainSelected?.Invoke(this, null);

            RenderingLibrary.Graphics.Renderer.UseBasicEffectRendering = false;


            StatusBarManager.Self.Initialize(statusStrip1, CursorStatusLabel);

            WireframeManager.Self.AnimationFrameChange += HandleAnimationFrameChanges;


            PopulateUnitTypeComboBox();

            PreviewManager.Self.Initialize(PreviewGraphicsControl, previewControls1);
        }

        private void InitializePlugins()
        {
            this.plugins.Add(new MainFrameShapePlugin());

            foreach(var plugin in plugins)
            {
                plugin.StartUp();
            }
        }

        private void CreateViewModel()
        {

            // move this out :
            WireframeEditControlsViewModel = new WireframeEditControlsViewModel();
            WireframeEditControlsViewModel.PropertyChanged += HandleViewModelPropertyChanged;

            this.WireframeTopUiControl.DataContext = WireframeEditControlsViewModel;
        }

        private void HandleViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName)
            {
                case nameof(WireframeEditControlsViewModel.IsSnapToGridChecked):
                case nameof(WireframeEditControlsViewModel.GridSize):
                    RefreshSnappingValues();


                    break;
            }
        }

        private void RefreshSnappingValues()
        {
            if (WireframeEditControlsViewModel.IsSnapToGridChecked)
            {
                imageRegionSelectionControl1.SnappingGridSize = WireframeEditControlsViewModel.GridSize;
            }
            else
            {
                imageRegionSelectionControl1.SnappingGridSize = null;
            }
        }

        private void HandleEditorControlsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        private void ScrollBarHandleTextureChange()
        {

            Texture2D texture = WireframeManager.Self.Texture;

            if (texture == null)
            {
                mScrollBarControlLogic.SetDisplayedArea(128, 128);

            }
            else
            {
                mScrollBarControlLogic.SetDisplayedArea(texture.Width, texture.Height);
            }
        }

        public void AddToolStripMenuItem(ToolStripMenuItem item, string parent)
        {
            ToolStripMenuItem parentItem = null;

            foreach (var candidate in MenuStrip.Items)
            {
                if (candidate is ToolStripMenuItem && ((ToolStripMenuItem)candidate).Text == parent)
                {
                    parentItem = candidate as ToolStripMenuItem;
                }
            }

            if (parentItem == null)
            {
                parentItem = new ToolStripMenuItem(parent);
                MenuStrip.Items.Add(parentItem);
            }

            parentItem.DropDownItems.Add(item);


        }

        void HandleImageRegionSelectionControlClick(object sender, EventArgs e)
        {
            int m = 3;
            imageRegionSelectionControl1.Focus();
        }

        void HandleXnaUpdate()
        {
            TimeManager.Self.Activity();
        }

        private void PopulateUnitTypeComboBox()
        {
            foreach (var value in Enum.GetValues(typeof(UnitType)))
            {
                UnitTypeComboBox.Items.Add(value);
            }
            UnitTypeComboBox.SelectedItem = UnitTypeComboBox.Items[0];
        }


        void HandleAnimationFrameChanges(object sender, EventArgs args)
        {
            PreviewManager.Self.ReactToAnimationFrameChange();
        }

        void HandleRegionXnaInitialize()
        {
            try
            {
                WireframeManager.Self.Initialize(imageRegionSelectionControl1, imageRegionSelectionControl1.SystemManagers, WireframeTopUiControl, WireframeEditControlsViewModel);
                WireframeManager.Self.AnimationChainChange += (not, used) =>
                        ApplicationEvents.Self.RaiseAnimationChainsChanged();


                mScrollBarControlLogic.Managers = imageRegionSelectionControl1.SystemManagers;
                var contentLoader = new DateCheckingContentLoader(imageRegionSelectionControl1.SystemManagers);
                LoaderManager.Self.ContentLoader = contentLoader;

                if (XnaInitialize != null)
                {
                    XnaInitialize();
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("Error initializing main AnimationEditor control:\n" + e.ToString());
            }
        }

        public void SaveCurrentAnimationChain(FilePath fileName)
        {
            // If it's null, we probably want to make one, right?

            var whatToSave = ProjectManager.Self.AnimationChainListSave;

            if (whatToSave != null)
            {
                whatToSave = FileManager.CloneObject<AnimationChainListSave>(whatToSave);
                whatToSave.ConvertToPixelCoordinates();

                int timesTried = 0;
                int maxTries = 3;

                Exception failure = null;
                while (timesTried < maxTries)
                {
                    try
                    {
                        whatToSave.Save(fileName.FullPath);
                        break;
                    }
                    catch ( Exception e)
                    {
                        failure = e;
                        timesTried++;

                        System.Threading.Thread.Sleep(100);
                    }
                }

                if (timesTried == maxTries)
                {
                    MessageBox.Show("Error saving:\n\n" + failure.ToString());
                }

                ProjectManager.Self.FileName = fileName.FullPath;

                if(failure == null)
                {
                    ProjectManager.Self.AnimationChainListSave.FileName = fileName.FullPath;
                }

                IoManager.Self.SaveCompanionFileFor(fileName);
            }
        }


        private void AnimationTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {

            TreeViewManager.Self.AfterTreeItemSelect();
        }

        private void UnitTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            AppState.Self.UnitType = (UnitType)UnitTypeComboBox.SelectedItem;
        }

        private void AnimationTreeView_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void AnimationTreeView_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.AnimationTreeView.SelectedNode =
                    AnimationTreeView.GetNodeAt(e.X, e.Y);
                TreeViewManager.Self.HandleRightClick();
            }
        }

        private void zoomControl1_ZoomChanged(object sender, EventArgs e)
        {
            int zoomValue = WireframeTopUiControl.PercentageValue;

            WireframeManager.Self.ZoomValue = zoomValue;
        }

        private void zoomControl1_Load(object sender, EventArgs e)
        {

        }

        private void TreeViewAndEverythingElse_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }

        private void resizeTextureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResizeMethods.Self.ResizeTextureClick(imageRegionSelectionControl1.GraphicsDevice);

            TreeViewManager.Self.RefreshTreeView();
            WireframeManager.Self.RefreshAll();
            PreviewManager.Self.RefreshAll();

            if (AnimationChainChange != null)
            {
                AnimationChainChange(this, null);
            }
        }

        private void AddAnimationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeViewManager.Self.AddChainClick(null, null);
        }

        private void frameToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeViewManager.Self.AddFrameClick(null, null);
        }

        private void multipleframesToolStripMenuItem_Click(object sender, EventArgs e) {
            TreeViewManager.Self.AddFramesClick(null, null);
        }

        private void previewControls1_ZoomChanged(object sender, EventArgs e)
        {
            int zoomValue = previewControls1.PercentageValue;

            PreviewManager.Self.ZoomValue = zoomValue;
        }

        private void PreviewSplitContainer_Panel1_Click(object sender, EventArgs e)
        {
            this.imageRegionSelectionControl1.Focus();
        }

        private void AnimationTreeView_DragDrop(object sender, DragEventArgs e)
        {
            TreeViewManager.Self.HandleDrop(sender, e);
        }

        private void AnimationTreeView_DragEnter(object sender, DragEventArgs e)
        {
            // Not sure why, but we have to do this to make the dropping work...
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
    
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyManager.Self.HandleCopy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CopyManager.Self.HandlePaste();
        }

        private void AnimationTreeView_KeyPress(object sender, KeyPressEventArgs e)
        {
            TreeViewManager.Self.HandleKeyPress(e);
        }

        private void AnimationTreeView_KeyDown(object sender, KeyEventArgs e)
        {
            TreeViewManager.Self.HandleKeyDown(e);
        }

        // We used to do this, but we want to support moving
        // around the wireframe
        //protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        //{
        //    if (imageRegionSelectionControl1.Focused == false)
        //    {
        //        switch (keyData)
        //        {
        //            case Keys.Control | Keys.Right:
        //                // go to next AnimationFrame or loop
        //                TreeViewManager.Self.GoToNextFrame();
        //                return true;
        //            case Keys.Control | Keys.Left:
        //                // go to previous AnimationFrame or loop
        //                TreeViewManager.Self.GoToPreviousFrame();
        //                return true;

        //            default:

        //                break;
        //        }

        //        return base.ProcessCmdKey(ref msg, keyData);
        //    }
        //}

        private void AnimationTreeView_DragOver(object sender, DragEventArgs e)
        {
            TreeViewManager.DragOver(sender, e);
        }

        private void AnimationTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            TreeViewManager.Self.ItemDrag(sender, e);
        }

        private void saveCurrentAnimationAsGIFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GifManager.Self.SaveCurrentAnimationAsGif();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if(Managers.HotkeyManager.Self.TryHandleKeys(keyData))
            {
                return true;
            }
            else
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private void WireframeTopUiControl_Load(object sender, EventArgs e)
        {

        }


        public T Invoke<T>(Func<T> func)
        {

            T toReturn = default(T);
            base.Invoke((MethodInvoker)delegate
            {
                toReturn = func();
            });

            return toReturn;
        }

        public Task Invoke(Func<Task> func)
        {
            Task toReturn = Task.CompletedTask;

            var asyncResult = base.BeginInvoke((MethodInvoker)delegate
            {
                try
                {
                    toReturn = func();
                }
                catch (Exception e)
                {
                    if (!IsDisposed)
                    {
                        throw e;
                    }
                    // otherwise, we don't care, they're exiting
                }
            });

            asyncResult.AsyncWaitHandle.WaitOne();

            return toReturn;
        }

        public Task<T> Invoke<T>(Func<Task<T>> func)
        {
            Task<T> toReturn = Task.FromResult(default(T));

            base.Invoke((MethodInvoker)delegate
            {
                try
                {
                    toReturn = func();
                }
                catch (Exception e)
                {
                    if (!IsDisposed)
                    {
                        throw e;
                    }
                    // otherwise, we don't care, they're exiting
                }
            });

            return toReturn;
        }
    }
}
