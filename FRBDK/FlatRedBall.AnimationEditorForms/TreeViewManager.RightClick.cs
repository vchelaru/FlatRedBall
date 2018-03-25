using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Controls;
using System.Diagnostics;
using FlatRedBall.AnimationEditorForms.Controls;
using FlatRedBall.AnimationEditorForms.Preview;
using FlatRedBall.IO;

namespace FlatRedBall.AnimationEditorForms
{
    public partial class TreeViewManager
    {
        #region Fields

        ContextMenuStrip mMenu;

        #endregion

        #region Initialize

        void InitializeRightClick()
        {
            mMenu = this.mTreeView.ContextMenuStrip;

        }

        #endregion

        public void HandleRightClick()
        {
            mMenu.Items.Clear();

            SelectedState state = SelectedState.Self;

            // If a frame is not null, then the chain will always be not null, 
            // so check the frame first
            if (state.SelectedFrame != null)
            {
                mMenu.Items.Add("Copy", null, CopyClick);
                mMenu.Items.Add("Paste", null, PasteClick);
                mMenu.Items.Add("View Texture in Explorer", null, ViewTextureInExplorer);

                mMenu.Items.Add("Delete AnimationFrame", null, DeleteAnimationFrameClick);

            }
            else if (state.SelectedChain != null)
            {
                var mMoveToTop = new ToolStripMenuItem("^^ Move To Top");
                mMoveToTop.ShortcutKeyDisplayString = "Alt+Shift+Up";
                mMoveToTop.Click += new System.EventHandler(MoveToTopClick);
                mMenu.Items.Add(mMoveToTop);

                var mMoveUp = new ToolStripMenuItem("^ Move Up");
                mMoveUp.ShortcutKeyDisplayString = "Alt+Up";
                mMoveUp.Click += new System.EventHandler(MoveUpClick);
                mMenu.Items.Add(mMoveUp);

                var mMoveDown = new ToolStripMenuItem("v Move Down");
                mMoveDown.ShortcutKeyDisplayString = "Alt+Down";
                mMoveDown.Click += new System.EventHandler(MoveDownClick);
                mMenu.Items.Add(mMoveDown);

                var mMoveToBottom = new ToolStripMenuItem("vv Move To Bottom");
                mMoveToBottom.ShortcutKeyDisplayString = "Alt+Shift+Down";
                mMoveToBottom.Click += new System.EventHandler(MoveToBottomClick);
                mMenu.Items.Add(mMoveToBottom);


                mMenu.Items.Add("-");


                mMenu.Items.Add("Adjust All Frame Time", null, AdjustFrameTimeClick);
                mMenu.Items.Add("Adjust Offsets", null, AdjustOffsetsClick);
                mMenu.Items.Add("Flip Horizontally", null, FlipAnimationChainHorizontally);
                mMenu.Items.Add("Flip Vertically", null, FlipAnimationChainVertically);

                mMenu.Items.Add("-");
                mMenu.Items.Add("Add AnimationChain", null, AddChainClick);
                mMenu.Items.Add("Add Frame", null, AddFrameClick);
                mMenu.Items.Add("-");
                mMenu.Items.Add("Copy", null, CopyClick);
                mMenu.Items.Add("Paste", null, PasteClick);
                mMenu.Items.Add("-");
                mMenu.Items.Add("Delete AnimationChain", null, DeleteAnimationChainClick);
            }
            else
            {
                mMenu.Items.Add("Add AnimationChain", null, AddChainClick);
                mMenu.Items.Add("-");
                mMenu.Items.Add("Paste", null, PasteClick);

            }

            if (mMenu.Items.Count != 0)
            {
                mMenu.Items.Add("-");
            }

            mMenu.Items.Add("Sort Animations Alphabetically", null, SortAnimationsAlphabetically );
            
        }

        public void MoveToTopClick(object sender, EventArgs e)
        {
            var chain = SelectedState.Self.SelectedChain;
            if (ProjectManager.Self.AnimationChainListSave != null && 
                chain != null &&
                ProjectManager.Self.AnimationChainListSave.AnimationChains.First() != chain
                )
            {
                ProjectManager.Self.AnimationChainListSave.AnimationChains.Remove(chain);
                ProjectManager.Self.AnimationChainListSave.AnimationChains.Insert(0, chain);
                
                TreeViewManager.Self.RefreshTreeView();
                CallAnimationChainsChange();

            }
        }

        public void MoveUpClick(object sender, EventArgs e)
        {
            var chain = SelectedState.Self.SelectedChain;
            if (ProjectManager.Self.AnimationChainListSave != null &&
                chain != null &&
                ProjectManager.Self.AnimationChainListSave.AnimationChains.First() != chain
                )
            {
                var oldIndex = ProjectManager.Self.AnimationChainListSave.AnimationChains.IndexOf(chain);

                ProjectManager.Self.AnimationChainListSave.AnimationChains.Remove(chain);
                ProjectManager.Self.AnimationChainListSave.AnimationChains.Insert(oldIndex-1, chain);

                TreeViewManager.Self.RefreshTreeView();
                CallAnimationChainsChange();

            }
        }

        public void MoveDownClick(object sender, EventArgs e)
        {
            var chain = SelectedState.Self.SelectedChain;
            if (ProjectManager.Self.AnimationChainListSave != null &&
                chain != null &&
                ProjectManager.Self.AnimationChainListSave.AnimationChains.Last() != chain
                )
            {
                var oldIndex = ProjectManager.Self.AnimationChainListSave.AnimationChains.IndexOf(chain);

                ProjectManager.Self.AnimationChainListSave.AnimationChains.Remove(chain);
                ProjectManager.Self.AnimationChainListSave.AnimationChains.Insert(oldIndex + 1, chain);

                TreeViewManager.Self.RefreshTreeView();
                CallAnimationChainsChange();

            }
        }

        public void MoveToBottomClick(object sender, EventArgs e)
        {
            var chain = SelectedState.Self.SelectedChain;
            if (ProjectManager.Self.AnimationChainListSave != null &&
                chain != null &&
                ProjectManager.Self.AnimationChainListSave.AnimationChains.Last() != chain
                )
            {
                var oldIndex = ProjectManager.Self.AnimationChainListSave.AnimationChains.IndexOf(chain);

                ProjectManager.Self.AnimationChainListSave.AnimationChains.Remove(chain);
                ProjectManager.Self.AnimationChainListSave.AnimationChains.Add(chain);

                TreeViewManager.Self.RefreshTreeView();
                CallAnimationChainsChange();

            }
        }

        internal IEnumerable<string> GetExpandedNodeAnimationChainNames()
        {
            foreach(TreeNode treeNode in mTreeView.Nodes)
            {
                if(treeNode.IsExpanded)
                {
                    yield return treeNode.Text;
                }
            }
        }

        internal void ExpandNodes(List<string> expandedNodes)
        {
            foreach (TreeNode treeNode in mTreeView.Nodes)
            {
                if (expandedNodes.Contains( treeNode.Text))
                {
                    treeNode.Expand();
                }
            }
        }

        private void AdjustOffsetsClick(object sender, EventArgs e)
        {
            AdjustOffsetForm form = new AdjustOffsetForm();
            var result = form.ShowDialog();

            if (result == DialogResult.OK)
            {
                CallAnimationChainsChange();
            }
        }

        private void SortAnimationsAlphabetically(object sender, EventArgs e)
        {
            if (ProjectManager.Self.AnimationChainListSave != null)
            {
                ProjectManager.Self.AnimationChainListSave.AnimationChains.Sort(
                    (first, second) =>
                    {
                        return first.Name.CompareTo(second.Name);
                    }
                    );
                TreeViewManager.Self.RefreshTreeView();
                CallAnimationChainsChange();

            }
        }

        private void FlipAnimationChainVertically(object sender, EventArgs e)
        {
            AnimationChainSave acs = SelectedState.Self.SelectedChain;

            if (acs != null)
            {
                foreach (AnimationFrameSave afs in acs.Frames)
                {
                    afs.RelativeY *= -1;
                    afs.FlipVertical = !afs.FlipVertical;
                }

                WireframeManager.Self.RefreshAll();
                PreviewManager.Self.RefreshAll();
                CallAnimationChainsChange();

            }
        }

        private void FlipAnimationChainHorizontally(object sender, EventArgs e)
        {
            AnimationChainSave acs = SelectedState.Self.SelectedChain;

            if (acs != null)
            {
                foreach (AnimationFrameSave afs in acs.Frames)
                {
                    afs.RelativeX *= -1;
                    afs.FlipHorizontal = !afs.FlipHorizontal;
                }

                WireframeManager.Self.RefreshAll();
                PreviewManager.Self.RefreshAll();
                CallAnimationChainsChange();
            }
        }

        public void AddChainClick(object sender, EventArgs args)
        {
            if (ProjectManager.Self.AnimationChainListSave == null)
            {
                MessageBox.Show("You must first save a file before working in the Animation Editor");
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.DisplayText = "Enter new AnimationChain name";

                if (tiw.ShowDialog() == DialogResult.OK)
                {
                    string result = tiw.Result;

                    string whyIsntValid = GetWhyNameIsntValid(result);

                    if (!string.IsNullOrEmpty(whyIsntValid))
                    {
                        MessageBox.Show(whyIsntValid);
                    }
                    else
                    {
                        AnimationChainSave acs = new AnimationChainSave();
                        acs.Name = result;

                        ProjectManager.Self.AnimationChainListSave.AnimationChains.Add(acs);


                        TreeViewManager.Self.RefreshTreeView();
                        SelectedState.Self.SelectedChain = acs;


                        CallAnimationChainsChange();
                    }
                }
            }
        }

        public void AddFrameClick(object sender, EventArgs args)
        {
            AnimationChainSave chain = SelectedState.Self.SelectedChain;


            if (string.IsNullOrEmpty(ProjectManager.Self.FileName))
            {
                MessageBox.Show("You must first save this file before adding frames");
            }
            else if (chain == null)
            {
                MessageBox.Show("First select an Animation to add a frame to");
            }
            else
            {
                AnimationFrameSave afs = new AnimationFrameSave();

                if (chain.Frames.Count != 0)
                {
                    AnimationFrameSave copyFrom = chain.Frames[0];

                    afs.TextureName = copyFrom.TextureName;
                    afs.FrameLength = copyFrom.FrameLength;
                    afs.LeftCoordinate = copyFrom.LeftCoordinate;
                    afs.RightCoordinate = copyFrom.RightCoordinate;
                    afs.TopCoordinate = copyFrom.TopCoordinate;
                    afs.BottomCoordinate = copyFrom.BottomCoordinate;
                }
                else
                {
                    afs.FrameLength = .1f; // default to .1 seconds.  
                }

                chain.Frames.Add(afs);

                TreeViewManager.Self.RefreshTreeNode(chain);

                SelectedState.Self.SelectedFrame = afs;

                CallAnimationChainsChange();
            }
        }

        void AdjustFrameTimeClick(object sender, EventArgs args)
        {
            float oldTotalLength = 0;
            AnimationChainSave animation = SelectedState.Self.SelectedChain;
            foreach (var frame in animation.Frames)
            {
                oldTotalLength += frame.FrameLength;
            }

            AnimationChainTimeScaleWindow window = new AnimationChainTimeScaleWindow();
            window.Value = oldTotalLength;
            window.FrameCount = animation.Frames.Count;
            DialogResult result = window.ShowDialog();

            if (result == DialogResult.OK && animation.Frames.Count != 0)
            {
                float newValue = window.Value;

                if (window.ScaleMode == ScaleMode.KeepProportional)
                {
                    float scaleValue = window.Value / oldTotalLength;


                    foreach (var frame in animation.Frames)
                    {
                        frame.FrameLength *= scaleValue;
                    }
                }
                else // value is to set all frames
                {
                    int frameCount = animation.Frames.Count;

                    foreach (var frame in animation.Frames)
                    {
                        frame.FrameLength = window.Value / frameCount;
                    }
                }

                PropertyGridManager.Self.Refresh();
                PreviewManager.Self.RefreshAll();
                this.CallAnimationChainsChange();
            }
        }

        void DeleteAnimationChainClick(object sender, EventArgs args)
        {
            if (SelectedState.Self.SelectedChain != null)
            {
                DialogResult result = 
                    MessageBox.Show("Delete AnimationChain " + SelectedState.Self.SelectedChain + "?", "Delete?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    ProjectManager.Self.AnimationChainListSave.AnimationChains.Remove(SelectedState.Self.SelectedChain);

                    TreeViewManager.Self.RefreshTreeView();

                    CallAnimationChainsChange();

                    WireframeManager.Self.RefreshAll();
                }
            }
        }

        void DeleteAnimationFrameClick(object sender, EventArgs args)
        {
            if (SelectedState.Self.SelectedFrame != null)
            {
                DialogResult result = 
                    MessageBox.Show("Delete Frame?", "Delete?", MessageBoxButtons.YesNo);

                if (result == DialogResult.Yes)
                {
                    SelectedState.Self.SelectedChain.Frames.Remove(SelectedState.Self.SelectedFrame);

                    TreeViewManager.Self.RefreshTreeNode(SelectedState.Self.SelectedChain);

                    WireframeManager.Self.RefreshAll();

                    CallAnimationChainsChange();
                }

            }
        }

        void ViewTextureInExplorer(object sender, EventArgs args)
        {

            string fileName = WireframeManager.Self.GetTextureFileNameForFrame(SelectedState.Self.SelectedFrame);

            if (!string.IsNullOrEmpty(fileName))
            {
                fileName = fileName.Replace("/", "\\");

                if (!string.IsNullOrEmpty(fileName) && System.IO.File.Exists(fileName))
                {
                    Process.Start("explorer.exe", "/select," + "\"" + fileName + "\"");

                }
            }
            else
            {
                MessageBox.Show("No texture set");
            }
        }

        string GetWhyNameIsntValid(string animationChainName)
        {
            foreach (var animationChain in ProjectManager.Self.AnimationChainListSave.AnimationChains)
            {
                if(animationChain.Name == animationChainName)
                {
                    return "The name " + animationChainName + " is already being used by another AnimationChain";
                }

            }
            if (string.IsNullOrEmpty(animationChainName))
            {
                return "The name can not be empty.";
            }

            return null;

        }

        void CopyClick(object sender, EventArgs args)
        {
            CopyManager.Self.HandleCopy();
        }

        void PasteClick(object sender, EventArgs args)
        {
            CopyManager.Self.HandlePaste();
            CallAnimationChainsChange();

        }
    }
}
