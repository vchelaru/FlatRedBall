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


        #endregion

        #region Events

        public event Action XnaInitialize;

        public event EventHandler AnimationChainChange;

        #endregion

        public MainControl()
        {
            mSelf = this;
            InitializeComponent();


            mScrollBarControlLogic = new ScrollBarControlLogic(PreviewSplitContainer.Panel1);

            ApplicationEvents.Self.WireframePanning += delegate { mScrollBarControlLogic.UpdateScrollBars(); };
            ApplicationEvents.Self.WireframeTextureChange += ScrollBarHandleTextureChange;
            ApplicationEvents.Self.AfterZoomChange += delegate
            {
                mScrollBarControlLogic.ZoomPercentage = (float)ApplicationState.Self.WireframeZoomValue;
                mScrollBarControlLogic.UpdateScrollBars();
            };

            this.imageRegionSelectionControl1 = new FlatRedBall.SpecializedXnaControls.ImageRegionSelectionControl();
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

            this.zoomControl1.PropertyChanged += HandleEditorControlsPropertyChanged;

            SelectedState.Self.Initialize(this.AnimationTreeView);
            if (this.DesignMode == false)
            {
                HandleRegionXnaInitialize();
                this.imageRegionSelectionControl1.XnaUpdate += new Action(HandleXnaUpdate);
            }
            PropertyGridManager.Self.Initialize(SelectedItemPropertyGrid, this.tileMapInfoWindow1);
            PropertyGridManager.Self.AnimationChainChange += RaiseAnimationChainChanges;
            PropertyGridManager.Self.AnimationFrameChange += HandleAnimationFrameChanges;


            TreeViewManager.Self.Initialize(AnimationTreeView);
            TreeViewManager.Self.AnimationChainsChange += RaiseAnimationChainChanges;

            StatusBarManager.Self.Initialize(statusStrip1, CursorStatusLabel);

            WireframeManager.Self.AnimationFrameChange += HandleAnimationFrameChanges;

            PopulateUnitTypeComboBox();

            PreviewManager.Self.Initialize(PreviewGraphicsControl, previewControls1);
        }

        private void HandleEditorControlsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (zoomControl1.SnapToGrid)
            {
                imageRegionSelectionControl1.SnappingGridSize = zoomControl1.GridSize;
            }
            else
            {
                imageRegionSelectionControl1.SnappingGridSize = null;
            }

            imageRegionSelectionControl1.ShowFullAlpha = zoomControl1.ShowFullAlpha;
        }

        private void ScrollBarHandleTextureChange()
        {

            Texture2D texture = WireframeManager.Self.Texture;

            if (texture == null)
            {
                mScrollBarControlLogic.UpdateToImage(128, 128);

            }
            else
            {
                mScrollBarControlLogic.UpdateToImage(texture.Width, texture.Height);
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

        public void RaiseAnimationChainChanges(object sender, EventArgs args)
        {
            if (AnimationChainChange != null)
            {
                AnimationChainChange(this, null);
            }
        }

        void HandleAnimationFrameChanges(object sender, EventArgs args)
        {
            PreviewManager.Self.ReactToAnimationFrameChange();

        }

        void HandleRegionXnaInitialize()
        {
            try
            {
                WireframeManager.Self.Initialize(imageRegionSelectionControl1, imageRegionSelectionControl1.SystemManagers, zoomControl1);
                WireframeManager.Self.AnimationChainChange += RaiseAnimationChainChanges;

                mScrollBarControlLogic.Managers = imageRegionSelectionControl1.SystemManagers;

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

        public void LoadAnimationChain(string fileName)
        {
            lock (RenderingLibrary.Graphics.Renderer.LockObject)
            {
                ProjectManager.Self.LoadAnimationChain(fileName);

                TreeViewManager.Self.RefreshTreeView();
                // do this after refreshing the tree node:
                IoManager.Self.LoadAndApplyCompanionFileFor(fileName);
                WireframeManager.Self.RefreshAll();
                PreviewManager.Self.RefreshAll();
            }
        }

        public void SaveCurrentAnimationChain()
        {
            SaveCurrentAnimationChain(ProjectManager.Self.FileName);
        }

        public void SaveCurrentAnimationChain(string fileName)
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
                        whatToSave.Save(fileName);
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

                ProjectManager.Self.FileName = fileName;

                IoManager.Self.SaveCompanionFileFor(fileName);
            }
        }


        private void AnimationTreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {

            TreeViewManager.Self.AfterTreeItemSelect();
        }

        private void UnitTypeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplicationState.Self.UnitType = (UnitType)UnitTypeComboBox.SelectedItem;
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
            int zoomValue = zoomControl1.PercentageValue;

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

    }
}
