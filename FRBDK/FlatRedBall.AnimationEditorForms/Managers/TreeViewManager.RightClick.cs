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
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.AnimationEditorForms.CommandsAndState;
using FlatRedBall.Utilities;
using FlatRedBall.Math;
using GlueFormsCore.Extensions;

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

            #region AxisAlignedRectangle

            if (state.SelectedAxisAlignedRectangle != null)
            {
                mMenu.Items.Add("Match Frame Size", null, HandleRectangleMatchFrameSize);
            }

            #endregion
            // If a frame is not null, then the chain will always be not null, 
            // so check the frame first
            #region Animation Frame
            else if (state.SelectedFrame != null)
            {
                AddReorderOptions();

                mMenu.Items.Add("-");

                mMenu.Items.Add("Add AxisAlignedRectangle", null, HandleAddAxisAlignedRectangle);
                mMenu.Items.Add("Add Circle", null, HandleAddCircle);

                mMenu.Items.Add("-");

                mMenu.Items.Add("Copy", null, CopyClick);
                mMenu.Items.Add("Paste", null, PasteClick);
                mMenu.Items.Add("View Texture in Explorer", null, ViewTextureInExplorer);

                mMenu.Items.Add("Delete AnimationFrame", null, DeleteAnimationFrameClick);

            }
            #endregion

            #region AnimationChain

            else if (state.SelectedChain != null)
            {
                AddReorderOptions();

                mMenu.Items.Add("-");


                mMenu.Items.Add("Adjust All Frame Time", null, AdjustFrameTimeClick);
                mMenu.Items.Add("Adjust Offsets", null, AdjustOffsetsClick);
                mMenu.Items.Add("Flip Horizontally", null, FlipAnimationChainHorizontally);
                mMenu.Items.Add("Flip Vertically", null, FlipAnimationChainVertically);
                mMenu.Items.Add("Flip Vertically", null, FlipAnimationChainVertically);
                mMenu.Items.Add("Invert Frame Order", null, InvertAnimationFrameOrder);

                mMenu.Items.Add("-");
                mMenu.Items.Add("Add AnimationChain", null, AddChainClick);

                var addframe = new ToolStripMenuItem("Add Frame");
                addframe.ShortcutKeyDisplayString = "Ctrl+plus";
                addframe.Click += new System.EventHandler(AddFrameClick);
                mMenu.Items.Add(addframe);

                mMenu.Items.Add("Add Multiple Frames", null, AddFramesClick);

                mMenu.Items.Add("-");

                CreateDuplicateToolStripItems();

                mMenu.Items.Add("Copy", null, CopyClick);
                mMenu.Items.Add("Paste", null, PasteClick);
                mMenu.Items.Add("-");
                mMenu.Items.Add("Delete AnimationChain", null, DeleteAnimationChainClick);
            }

            #endregion

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

        private void HandleAddAxisAlignedRectangle(object sender, EventArgs e)
        {
            var selectedFrames = SelectedState.Self.SelectedFrames;

            foreach(var frame in selectedFrames)
            {
                AppCommands.Self.AddAxisAlignedRectangle(frame);
            }
        }

        private void HandleAddCircle(object sender, EventArgs e)
        {
            var selectedFrames = SelectedState.Self.SelectedFrames;
            foreach(var frame in selectedFrames)
            {
                AppCommands.Self.AddCircle(frame);
            }
        }

        private void HandleRectangleMatchFrameSize(object sender, EventArgs e)
        {
            var rectangle = SelectedState.Self.SelectedRectangle;

            var animationFrame = SelectedState.Self.SelectedFrame;

            AppCommands.Self.MatchRectangleToFrame(rectangle, animationFrame);

            AppCommands.Self.RefreshAnimationFrameDisplay();

            AppCommands.Self.SaveCurrentAnimationChainList();
        }



        internal void HandleExpandAllTreeView(object sender, EventArgs e)
        {
            this.mTreeView.ExpandAll();
        }

        internal void HandleCollapseAllTreeView(object sender, EventArgs e)
        {
            this.mTreeView.CollapseAll();
        }

        private void AddReorderOptions()
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
        }

        private void CreateDuplicateToolStripItems()
        {
            SelectedState state = SelectedState.Self;

            var toolStripMenuItem = new ToolStripMenuItem("Duplicate...");

            var currentChainName = state.SelectedChain.Name;


            var originalToolStripDuplicate = toolStripMenuItem.DropDownItems.Add("Original", null, (not, used) => HandleDuplicateOriginalClicked(false, false, null))
                as ToolStripMenuItem;
            originalToolStripDuplicate.ShortcutKeyDisplayString = "Ctrl+D";

            string horizontallyText = "Flipped Horizontally";
            string newHorizontalName = null;

            var hasLeft = currentChainName?.ToLowerInvariant().Contains("left") == true;
            var hasRight = currentChainName?.ToLowerInvariant().Contains("right") == true;
            if(hasLeft)
            {
                newHorizontalName = currentChainName
                    .Replace("Left", "Right")
                    .Replace("left", "right")
                    .Replace("LEFT", "RIGHT");
            }
            else if(hasRight)
            {
                newHorizontalName = currentChainName
                    .Replace("Right", "Left")
                    .Replace("right", "left")
                    .Replace("RIGHT", "LEFT");
            }
            // Do a comparison against the original name in case the value hasn't been replaced due to caps
            if(newHorizontalName == currentChainName)
            {
                newHorizontalName = null;
            }

            if(newHorizontalName != null)
            {
                horizontallyText += $" as {newHorizontalName}";
            }
            toolStripMenuItem.DropDownItems.Add(horizontallyText, null, (not, used) => HandleDuplicateOriginalClicked(true, false, newHorizontalName));


            string verticallyText = "Flipped Vertically";
            string newVerticalName = null;

            var hasUp = currentChainName?.ToLowerInvariant().Contains("up") == true;
            var hasDown = currentChainName?.ToLowerInvariant().Contains("down") == true;
            if(hasUp)
            {
                newVerticalName = currentChainName
                    .Replace("Up", "Down")
                    .Replace("up", "down")
                    .Replace("UP", "DOWN");
            }
            else if(hasDown)
            {
                newVerticalName = currentChainName
                    .Replace("Down", "Up")
                    .Replace("down", "up")
                    .Replace("DOWN", "UP");
            }
            // Do a coparison against the original name in case the value hasn't been replaced due to caps
            if(newVerticalName == currentChainName)
            {
                newVerticalName = null;
            }

            if(newVerticalName != null )
            {
                verticallyText += $" as {newVerticalName}";
            }

            toolStripMenuItem.DropDownItems.Add(verticallyText, null, (not, used) => HandleDuplicateOriginalClicked(false, true, newVerticalName));

            mMenu.Items.Add(toolStripMenuItem);
        }

        private void HandleDuplicateOriginalClicked(bool flipHorizontal, bool flipVertical, string newName)
        {
            var newCopy = CopyManager.Self.HandleDuplicate(newName);

            if(flipHorizontal)
            {
                FlipHorizontally(newCopy);
            }

            if(flipVertical)
            {
                FlipVertically(newCopy);
            }

            CallAnimationChainsChange();
        }

        public void MoveToTopClick(object sender, EventArgs e)
        {
            if (ProjectManager.Self.AnimationChainListSave != null)
            {
                var chain = SelectedState.Self.SelectedChain;
                var frame = SelectedState.Self.SelectedFrame;
                var chains = SelectedState.Self.SelectedChains;
                if(chains.Count > 0)
                {
                    var chainsByIndex = chains.OrderBy(item => ProjectManager.Self.AnimationChainListSave.AnimationChains.IndexOf(item))
                        .ToList();

                    var allChains = ProjectManager.Self.AnimationChainListSave.AnimationChains;

                    for(int i = 0; i < chainsByIndex.Count; i++)
                    {
                        var chainToMove = chainsByIndex[i];

                        allChains.Remove(chainToMove);
                        allChains.Insert(i, chainToMove);
                    }
                    TreeViewManager.Self.RefreshTreeView();
                    CallAnimationChainsChange();
                }
                else if(frame != null && chain != null && frame != chain.Frames[0])
                {
                    chain.Frames.Remove(frame);
                    chain.Frames.Insert(0, frame);
                    TreeViewManager.Self.RefreshTreeNode(chain);
                    SelectedState.Self.SelectedFrame = frame;
                    CallAnimationChainsChange();
                }
                else if(chain != null &&
                    ProjectManager.Self.AnimationChainListSave.AnimationChains.First() != chain)
                {

                    ProjectManager.Self.AnimationChainListSave.AnimationChains.Remove(chain);
                    ProjectManager.Self.AnimationChainListSave.AnimationChains.Insert(0, chain);

                    TreeViewManager.Self.RefreshTreeView();
                    CallAnimationChainsChange();

                }

            }
        }

        public void MoveUpClick(object sender, EventArgs e)
        {
            if (ProjectManager.Self.AnimationChainListSave != null)
            {
                var chains = SelectedState.Self.SelectedChains;
                var chain = SelectedState.Self.SelectedChain;
                var frame = SelectedState.Self.SelectedFrame;

                if(chains.Count > 0)
                {
                    var chainsByIndex = chains.OrderBy(item => ProjectManager.Self.AnimationChainListSave.AnimationChains.IndexOf(item))
                        .ToList();

                    var allChains = ProjectManager.Self.AnimationChainListSave.AnimationChains;

                    foreach (var chainToMove in chainsByIndex)
                    {
                        if(chainToMove == allChains.First())
                        {
                            break;
                        }
                        var oldIndex = allChains.IndexOf(chainToMove);

                        allChains.Remove(chainToMove);
                        allChains.Insert(oldIndex - 1, chainToMove);

                    }
                    TreeViewManager.Self.RefreshTreeView();
                    CallAnimationChainsChange();
                }
                else if(frame != null && chain != null && frame != chain.Frames[0])
                {
                    var oldIndex = chain.Frames.IndexOf(frame);

                    chain.Frames.Remove(frame);
                    chain.Frames.Insert(oldIndex - 1, frame);
                    TreeViewManager.Self.RefreshTreeNode(chain);
                    SelectedState.Self.SelectedFrame = frame;

                    CallAnimationChainsChange();
                }
                else if (chain != null &&
                    ProjectManager.Self.AnimationChainListSave.AnimationChains.First() != chain)
                {
                    var oldIndex = ProjectManager.Self.AnimationChainListSave.AnimationChains.IndexOf(chain);

                    ProjectManager.Self.AnimationChainListSave.AnimationChains.Remove(chain);
                    ProjectManager.Self.AnimationChainListSave.AnimationChains.Insert(oldIndex-1, chain);

                    TreeViewManager.Self.RefreshTreeView();
                    CallAnimationChainsChange();

                }

            }
        }

        public void MoveDownClick(object sender, EventArgs e)
        {
            if (ProjectManager.Self.AnimationChainListSave != null)
            {
                var chain = SelectedState.Self.SelectedChain;
                var chains = SelectedState.Self.SelectedChains;
                var frame = SelectedState.Self.SelectedFrame;

                if (chains.Count > 0)
                {
                    var chainsByIndex = chains
                        .OrderByDescending(item => ProjectManager.Self.AnimationChainListSave.AnimationChains.IndexOf(item))
                        .ToList();

                    var allChains = ProjectManager.Self.AnimationChainListSave.AnimationChains;

                    foreach (var chainToMove in chainsByIndex)
                    {
                        if(chainToMove == allChains.Last())
                        {
                            // do nothing...
                            break;
                        }
                        var oldIndex = allChains.IndexOf(chainToMove);

                        allChains.Remove(chainToMove);
                        allChains.Insert(oldIndex + 1, chainToMove);

                    }
                    TreeViewManager.Self.RefreshTreeView();
                    CallAnimationChainsChange();
                }
                else if (frame != null && chain != null && frame != chain.Frames.Last())
                {
                    var oldIndex = chain.Frames.IndexOf(frame);

                    chain.Frames.Remove(frame);
                    chain.Frames.Insert(oldIndex + 1, frame);
                    TreeViewManager.Self.RefreshTreeNode(chain);
                    SelectedState.Self.SelectedFrame = frame;

                    CallAnimationChainsChange();
                }
                else if(chain != null &&
                    ProjectManager.Self.AnimationChainListSave.AnimationChains.Last() != chain)
                {
                    var oldIndex = ProjectManager.Self.AnimationChainListSave.AnimationChains.IndexOf(chain);

                    ProjectManager.Self.AnimationChainListSave.AnimationChains.Remove(chain);
                    ProjectManager.Self.AnimationChainListSave.AnimationChains.Insert(oldIndex + 1, chain);

                    TreeViewManager.Self.RefreshTreeView();
                    CallAnimationChainsChange();

                }

            }
        }

        public void MoveToBottomClick(object sender, EventArgs e)
        {
            if (ProjectManager.Self.AnimationChainListSave != null)
            {
                var chain = SelectedState.Self.SelectedChain;
                var chains = SelectedState.Self.SelectedChains;
                var frame = SelectedState.Self.SelectedFrame;
                if(chains.Count > 0)
                {
                    var chainsByIndex = chains
                        .OrderByDescending(item => ProjectManager.Self.AnimationChainListSave.AnimationChains.IndexOf(item))
                        .ToList();

                    var allChains = ProjectManager.Self.AnimationChainListSave.AnimationChains;

                    for (int i = 0; i < chainsByIndex.Count; i++)
                    {
                        var chainToMove = chainsByIndex[i];
                        allChains.Remove(chainToMove);
                        allChains.Insert(allChains.Count-i, chainToMove);
                    }
                    TreeViewManager.Self.RefreshTreeView();
                    CallAnimationChainsChange();
                }
                else if (frame != null && chain != null && frame != chain.Frames.Last())
                {
                    chain.Frames.Remove(frame);
                    chain.Frames.Add(frame);
                    TreeViewManager.Self.RefreshTreeNode(chain);
                    SelectedState.Self.SelectedFrame = frame;

                    CallAnimationChainsChange();
                }
                else if (chain != null &&
                    ProjectManager.Self.AnimationChainListSave.AnimationChains.Last() != chain)
                {
                    ProjectManager.Self.AnimationChainListSave.AnimationChains.Remove(chain);
                    ProjectManager.Self.AnimationChainListSave.AnimationChains.Add(chain);

                    TreeViewManager.Self.RefreshTreeView();
                    CallAnimationChainsChange();
                }
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

            var wpfForms = new AdjustOffsetWindow();
            var result = wpfForms.ShowDialog();

            if (result == true)
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
                FlipVertically(acs);

                CallAnimationChainsChange();
            }
        }

        private void InvertAnimationFrameOrder(object sender, EventArgs e)
        {
            foreach(var animation in SelectedState.Self.SelectedChains)
            {
                var frameCount = animation.Frames.Count;
                var frameMinusOne = frameCount - 1;
                for(int i = 0; i < frameMinusOne; i++)
                {
                    var frameToMove = animation.Frames[frameMinusOne];
                    animation.Frames.RemoveAt(frameMinusOne);
                    animation.Frames.Insert(i, frameToMove);
                }
            }

            PreviewManager.Self.RefreshAll();

            CallAnimationChainsChange();
        }

        private void FlipVertically(AnimationChainSave acs)
        {
            foreach (AnimationFrameSave afs in acs.Frames)
            {
                afs.RelativeY *= -1;
                afs.FlipVertical = !afs.FlipVertical;

                foreach(var rectangle in afs.ShapeCollectionSave.AxisAlignedRectangleSaves)
                {
                    rectangle.Y *= -1;
                }
                foreach(var circle in afs.ShapeCollectionSave.CircleSaves)
                {
                    circle.Y *= -1;
                }
                foreach(var polygon in afs.ShapeCollectionSave.PolygonSaves)
                {
                    polygon.Y *= -1;
                }
            }

            WireframeManager.Self.RefreshAll();
            PreviewManager.Self.RefreshAll();
        }

        private void FlipAnimationChainHorizontally(object sender, EventArgs e)
        {
            AnimationChainSave acs = SelectedState.Self.SelectedChain;

            if (acs != null)
            {
                FlipHorizontally(acs);
                CallAnimationChainsChange();
            }
        }

        private void FlipHorizontally(AnimationChainSave acs)
        {
            foreach (AnimationFrameSave afs in acs.Frames)
            {
                afs.RelativeX *= -1;
                afs.FlipHorizontal = !afs.FlipHorizontal;

                foreach(var rectangle in afs.ShapeCollectionSave.AxisAlignedRectangleSaves)
                {
                    rectangle.X *= -1;
                }
                foreach(var circle in afs.ShapeCollectionSave.CircleSaves)
                {
                    circle.X *= -1;
                }
                foreach(var polygon in afs.ShapeCollectionSave.PolygonSaves)
                {
                    polygon.X *= -1;
                }
            }

            WireframeManager.Self.RefreshAll();
            PreviewManager.Self.RefreshAll();
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
                afs.ShapeCollectionSave = new ShapeCollectionSave(); // animation editor always assumes frames have shape collections
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

                WireframeManager.Self.UpdateSelectedFrameToSelectedTexture();

                CallAnimationChainsChange();
            }
        }

        public void AddFramesClick(object sender, EventArgs args)
        {
            if (string.IsNullOrEmpty(ProjectManager.Self.FileName))
            {
                MessageBox.Show("You must first save this file before adding frames");
                return;
            }

            AnimationChainSave chain = SelectedState.Self.SelectedChain;
            if (chain == null)
            {
                MessageBox.Show("First select an Animation to add a frame to");
                return;
            }

            //Calc frame size & how many more before off the texture
            AnimationFrameSave frame = chain.Frames.Count() == 0 ? null : chain.Frames.Last();
            int NumberFramesCanMake = -1;
            float frameWidth = 0;
            float frameHeight = 0;
            int framesPerRow = 0;
            if (frame != null)
            {
                frameWidth = (frame.RightCoordinate - frame.LeftCoordinate);
                framesPerRow = MathFunctions.RoundToInt(1 / frameWidth);
                var framesLeftOnRow = framesPerRow - (frame.RightCoordinate / frameWidth);
                frameHeight = (frame.BottomCoordinate - frame.TopCoordinate);
                var rows = MathFunctions.RoundToInt(1 / frameHeight);
                var rowsLeft = rows - (frame.BottomCoordinate / frameHeight);
                NumberFramesCanMake = (int)(framesLeftOnRow + (framesPerRow * rowsLeft));
            }

            //get # to make
            var IncrementFrames = false;
            var AddFramesCount = 0;

            var aafwpf = new AnimationAddFramesWPF(chain.Frames.Count() > 0, NumberFramesCanMake);
            aafwpf.MoveToCursor();
            if (!(bool)aafwpf.ShowDialog()) return;
            IncrementFrames = aafwpf.IncrementFrames;
            AddFramesCount = aafwpf.AddCount;

            //Make em all
            for (int i = 0; i < AddFramesCount; i++)
            {
                AnimationFrameSave afs = new AnimationFrameSave();
                afs.ShapeCollectionSave = new ShapeCollectionSave(); // animation editor always assumes frames have shape collections
                if (chain.Frames.Count != 0)
                {
                    AnimationFrameSave copyFrom = chain.Frames.Last();// IncrementFrames ? chain.Frames.Last() : chain.Frames[0];
                    afs.TextureName = copyFrom.TextureName;
                    afs.FrameLength = copyFrom.FrameLength;
                    afs.LeftCoordinate = copyFrom.LeftCoordinate;
                    afs.RightCoordinate = copyFrom.RightCoordinate;
                    afs.TopCoordinate = copyFrom.TopCoordinate;
                    afs.BottomCoordinate = copyFrom.BottomCoordinate;
                    
                    if (IncrementFrames)
                    {
                        var thisFrame = MathFunctions.RoundToInt(afs.LeftCoordinate / frameWidth) + 1;
                        if (thisFrame < framesPerRow)
                        {
                            afs.LeftCoordinate = thisFrame * frameWidth;// += frameWidth;
                            afs.RightCoordinate = (thisFrame + 1) * frameWidth;//+= frameWidth;
                        }
                        else
                        {
                            afs.LeftCoordinate = 0;
                            afs.RightCoordinate = frameWidth;
                            afs.TopCoordinate += frameHeight;
                            afs.BottomCoordinate += frameHeight;
                        }
                    }
                }
                else
                {
                    afs.FrameLength = .1f; // default to .1 seconds.  
                }

                chain.Frames.Add(afs);

                //At least some of these need to be called between frame adds or it breaks (attempted to skip and call after frames were all added)
                //but it seems fine refreshing ui every add, not slow or too flashy.
                TreeViewManager.Self.RefreshTreeNode(chain);
                SelectedState.Self.SelectedFrame = chain.Frames.Last();
                WireframeManager.Self.UpdateSelectedFrameToSelectedTexture();
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
            if (SelectedState.Self.SelectedChains.Count > 0)
            {
                AppCommands.Self.AskToDelete(SelectedState.Self.SelectedChains);
            }
        }

        void DeleteAnimationFrameClick(object sender, EventArgs args)
        {
            if (SelectedState.Self.SelectedFrames.Count > 0)
            {
                AppCommands.Self.AskToDelete(SelectedState.Self.SelectedFrames);
            }
        }

        void ViewTextureInExplorer(object sender, EventArgs args)
        {

            var fileName = WireframeManager.Self.GetTextureFileNameForFrame(SelectedState.Self.SelectedFrame).FullPath;

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
