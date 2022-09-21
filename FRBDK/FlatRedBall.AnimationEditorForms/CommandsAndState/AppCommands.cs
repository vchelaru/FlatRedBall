using FlatRedBall.AnimationEditorForms.IO;
using FlatRedBall.AnimationEditorForms.Managers;
using FlatRedBall.AnimationEditorForms.Preview;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Content.Math.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FlatRedBall.AnimationEditorForms.CommandsAndState
{
    public class AppCommands : Singleton<AppCommands>
    {
        public void DoOnUiThread(Action action)
        {
            MainControl.Self.Invoke(action);
        }

        public Task DoOnUiThread(Func<Task> func) => MainControl.Self.Invoke(func);

        public T DoOnUiThread<T>(Func<T> func) => MainControl.Self.Invoke(func);

        public void LoadAnimationChain(string fileName)
        {
            lock (RenderingLibrary.Graphics.Renderer.LockObject)
            {
                MainControl.Self.WireframeEditControlsViewModel.SelectedTextureFilePath = null;
                ProjectManager.Self.LoadAnimationChain(fileName);

                TreeViewManager.Self.RefreshTreeView();
                // do this after refreshing the tree node:
                IoManager.Self.LoadAndApplyCompanionFileFor(fileName);
                WireframeManager.Self.RefreshAll();
                PreviewManager.Self.RefreshAll();
            }




        }

        public void RefreshTreeNode(AnimationChainSave animationChain) => TreeViewManager.Self.RefreshTreeNode(animationChain);

        public void RefreshTreeNode(AnimationFrameSave animationFrame) => TreeViewManager.Self.RefreshTreeNode(animationFrame);

        public void RefreshAnimationFrameDisplay() => PreviewManager.Self.RefreshAll();

        public void SaveCurrentAnimationChainList() => MainControl.Self.SaveCurrentAnimationChain(ProjectManager.Self.FileName);

        public void DeleteAnimationChains(List<AnimationChainSave> animationChains)
        {
            // copy it in case the actual list of current chains is passed or some other app-specific list.
            var chainsCopy = animationChains.ToArray();
            foreach (var chain in chainsCopy)
            {
                ProjectManager.Self.AnimationChainListSave.AnimationChains.Remove(chain);
            }

            // refresh the tree view before refreshing the PreviewManager, since refreshing the tree view deselects the animation
            TreeViewManager.Self.RefreshTreeView();

            PreviewManager.Self.RefreshAll();

            ApplicationEvents.Self.RaiseAnimationChainsChanged();

            WireframeManager.Self.RefreshAll();
        }

        public void AddAxisAlignedRectangle(AnimationFrameSave frame)
        {
            var rectangleSave = new AxisAlignedRectangleSave();
            rectangleSave.ScaleX = 8;
            rectangleSave.ScaleY = 8;
            rectangleSave.Name = "AxisAlignedRectangleInstance";

            // do this before adding it to the list
            rectangleSave.Name = ToolsUtilities.StringFunctions.MakeStringUnique(rectangleSave.Name,
                SelectedState.Self.SelectedFrame.ShapeCollectionSave.AxisAlignedRectangleSaves
                    .Select(item => item.Name).ToList()
                );

            // this loops through all frames. This could result in the wrong texture being used but....that's a pain to address so oh well...
            MatchRectangleToFrame(rectangleSave, frame);

            frame.ShapeCollectionSave.AxisAlignedRectangleSaves.Add(rectangleSave);

            AppCommands.Self.RefreshAnimationFrameDisplay();
            AppCommands.Self.RefreshTreeNode(frame);
            SelectedState.Self.SelectedRectangle = rectangleSave;
            AppCommands.Self.SaveCurrentAnimationChainList();
        }

        public void MatchRectangleToFrame(AxisAlignedRectangleSave rectangle, AnimationFrameSave animationFrame)
        {
            if (SelectedState.Self.SelectedTexture != null)
            {
                rectangle.ScaleX = SelectedState.Self.SelectedTexture.Width *
                    (animationFrame.RightCoordinate - animationFrame.LeftCoordinate) / 2.0f;
                rectangle.ScaleY = SelectedState.Self.SelectedTexture.Height *
                    (animationFrame.BottomCoordinate - animationFrame.TopCoordinate) / 2.0f;
            }
            rectangle.X = animationFrame.RelativeX;
            rectangle.Y = animationFrame.RelativeY;
        }

        public void DeleteAxisAlignedRectangle(AxisAlignedRectangleSave rectangle, AnimationFrameSave owner)
        {
            if(owner.ShapeCollectionSave.AxisAlignedRectangleSaves.Contains(rectangle))
            {
                owner.ShapeCollectionSave.AxisAlignedRectangleSaves.Remove(rectangle);

                // refresh the tree view before refreshing the PreviewManager, since refreshing the tree view deselects the animation
                AppCommands.Self.RefreshTreeNode(owner);

                PreviewManager.Self.RefreshAll();

                ApplicationEvents.Self.RaiseAnimationChainsChanged();

                // wireframe currently doesn't need to refresh
                //WireframeManager.Self.RefreshAll();

            }
        }

        public void AskToDelete(List<AxisAlignedRectangleSave> rectangles)
        {
            string message = "Delete the following rectangle(s)?\n\n";

            foreach(var rectangle in rectangles)
            {
                message += rectangle.Name + "\n";
            }

            var result = MessageBox.Show(message, "Delete?", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                // create a copy in case the list passed is the current rectangles
                foreach(var rectangle in rectangles.ToArray())
                {
                    var frame = ObjectFinder.Self.GetAnimationFrameContaining(rectangle);
                    if(frame != null)
                    {
                        AppCommands.Self.DeleteAxisAlignedRectangle(rectangle, frame);
                    }
                }
            }
        }

        public void AskToDelete(List<AnimationChainSave> animationChains)
        {
            string message = "Delete the following animation(s)?\n\n";

            foreach (var chain in SelectedState.Self.SelectedChains)
            {
                message += chain.Name + "\n";
            }

            var result = MessageBox.Show(message, "Delete?", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                AppCommands.Self.DeleteAnimationChains(SelectedState.Self.SelectedChains);
            }
        }

        public void AskToDelete(List<AnimationFrameSave> animationFrames)
        {
            string message = $"Delete the following {SelectedState.Self.SelectedFrames.Count} frame(s)?\n\n";
            foreach (var frame in SelectedState.Self.SelectedFrames)
            {
                message += $"Frame {frame.TextureName}\n";
            }
            DialogResult result =
                MessageBox.Show(message, "Delete?", MessageBoxButtons.YesNo);

            if (result == DialogResult.Yes)
            {
                var framesToDelete = SelectedState.Self.SelectedFrames.ToArray();
                foreach (var frame in framesToDelete)
                {
                    SelectedState.Self.SelectedChain.Frames.Remove(frame);
                }

                TreeViewManager.Self.RefreshTreeNode(SelectedState.Self.SelectedChain);

                WireframeManager.Self.RefreshAll();

                ApplicationEvents.Self.RaiseAnimationChainsChanged();
            }
        }
    }
}
