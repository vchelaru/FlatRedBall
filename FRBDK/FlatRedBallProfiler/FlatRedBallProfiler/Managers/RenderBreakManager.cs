using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall;
using FlatRedBall.Instructions;
using FlatRedBall.IO;
using FlatRedBall.Math;
using FlatRedBall.Performance.Measurement;

namespace FlatRedBallProfiler.Managers
{
    public class RenderBreakManager : Singleton<RenderBreakManager>
    {
        #region Fields

        TreeView mTreeView;
        TabPage mTabPage;
        TabControl mTabControl;
        PictureBox mPictureBox;

        #endregion

        public bool ShowEntireTexture
        {
            get;
            set;
        }

        public void Initialize(TabControl tabControl, TabPage tabPage, TreeView treeView, PictureBox pictureBox)
        {
            mTreeView = treeView;
            mTabPage = tabPage;
            mTabControl = tabControl;
            mPictureBox = pictureBox;
        }

        public void LoadRenderBreaks(string fileName)
        {
            List<RenderBreakSave> loadedRenderBreaks =
                FileManager.XmlDeserialize<List<RenderBreakSave>>(fileName);

            List<RenderBreakViewModel> viewModels = new List<RenderBreakViewModel>();
            foreach(var item in loadedRenderBreaks)
            {
                viewModels.Add(RenderBreakViewModel.FromRenderBreakSave(item));
            }


            UpdateFromRenderBreakSaves(viewModels);

        }
        private void GetRenderBreaksFromEngineInternal()
        {
            var list = FlatRedBall.Graphics.Renderer.LastFrameRenderBreakList;


            List<RenderBreakViewModel> viewModels = new List<RenderBreakViewModel>();
            foreach (var item in list)
            {
                viewModels.Add(RenderBreakViewModel.FromRenderBreak(item));
            }


            UpdateFromRenderBreakSaves(viewModels);
        }
        private void UpdateFromRenderBreakSaves(List<RenderBreakViewModel> renderBreakSaves)
        {

            mTreeView.Nodes.Clear();
            TreeNode currentTreeNode = null;


            foreach (var renderBreakSave in renderBreakSaves)
            {
                string layerName = renderBreakSave.LayerName;

                if (currentTreeNode == null || layerName != currentTreeNode.Text)
                {
                    currentTreeNode = new TreeNode(layerName);
                    mTreeView.Nodes.Add(currentTreeNode);
                }


                TreeNode node = new TreeNode(renderBreakSave.ToString());

                node.Tag = renderBreakSave;

                currentTreeNode.Nodes.Add(node);
            }

            foreach (TreeNode treeNode in mTreeView.Nodes)
            {
                treeNode.Text += " (" + treeNode.Nodes.Count + ")";

            }


            int count = renderBreakSaves.Count;

            mTabPage.Text = string.Format("Render Breaks ({0})", count);
            mTabControl.SelectedTab = mTabPage;
        }

        public void GetRenderBreaksFromEngine()
        {
            if(FlatRedBall.Graphics.Renderer.RecordRenderBreaks)
            {
                GetRenderBreaksFromEngineInternal();
            }
            else
            {
                FlatRedBall.Graphics.Renderer.RecordRenderBreaks = true;

                var instruction = new DelegateInstruction(GetRenderBreaksFromEngineInternal);
                float delay = .3f;
                instruction.TimeToExecute = FlatRedBall.TimeManager.CurrentTime + delay;


                InstructionManager.Add(instruction);
            }
        }



        internal void ReactToTreeViewItemSelect()
        {
            bool wasSet = false;

            if (mTreeView.SelectedNode != null)
            {
                var viewModel = mTreeView.SelectedNode.Tag as RenderBreakViewModel;
                if (viewModel != null)
                {
                    wasSet = SetImage(viewModel);

                }

                RenderBreakPropertyGridManager.Self.Show(viewModel);
            }
            mPictureBox.Visible = wasSet;
        }

        private bool SetImage(RenderBreakViewModel viewModel)
        {
            bool wasSet = false;
            string texture = viewModel.Texture;

            if (System.IO.File.Exists(texture))
            {
                mPictureBox.Load(texture);



                if (mPictureBox.Image != null)
                {

                    bool shouldCrop = false;
                    Rectangle cropArea = new Rectangle();

                    GetCropArea(viewModel, ref shouldCrop, ref cropArea, mPictureBox.Image);

                    if (shouldCrop)
                    {
                        mPictureBox.Image = CropImage(mPictureBox.Image, cropArea);
                    }
                    mPictureBox.Width = mPictureBox.Image.Width;
                    mPictureBox.Height = mPictureBox.Image.Height;
                }

                wasSet = true;
            }
            return wasSet;
        }

        private void GetCropArea(RenderBreakViewModel viewModel, ref bool shouldCrop, ref Rectangle cropArea, System.Drawing.Image image)
        {

            if (!ShowEntireTexture)
            {
                if (viewModel.ObjectCausingBreak is Sprite)
                {
                    var asSprite = viewModel.ObjectCausingBreak as Sprite;

                    if (asSprite.Texture != null)
                    {
                        cropArea = new Rectangle(
                            MathFunctions.RoundToInt(asSprite.LeftTexturePixel),
                            MathFunctions.RoundToInt(asSprite.TopTexturePixel),
                            MathFunctions.RoundToInt(asSprite.RightTexturePixel - asSprite.LeftTexturePixel),
                            MathFunctions.RoundToInt(asSprite.BottomTexturePixel - asSprite.TopTexturePixel));
                    }
                }
                // We don't want to have to include Gum dlls in this tool, nor do we want to force
                // users to have to include the dlls to use this, so we'll base it on reflection:
                else if(viewModel.ObjectCausingBreak.GetType().FullName == "RenderingLibrary.Graphics.Sprite")
                {
                    var field = viewModel.ObjectCausingBreak.GetType().GetField("SourceRectangle");

                    var sourceNullable = field.GetValue(viewModel.ObjectCausingBreak) as Rectangle?;

                    if(sourceNullable.HasValue)
                    {
                        var source = sourceNullable.Value;

                        cropArea = new Rectangle(
                            MathFunctions.RoundToInt(source.Left),
                            MathFunctions.RoundToInt(source.Top),
                            MathFunctions.RoundToInt(source.Width),
                            MathFunctions.RoundToInt(source.Height));
                    }
                }

                shouldCrop =
                    cropArea.X != 0 ||
                    cropArea.Y != 0 ||
                    cropArea.Width < image.Width ||
                    cropArea.Height < image.Height;

                // if the texture is displaying beyond its bounds, then
                // it's probably wrapping. We'll just display the entire texture
                // for now:
                if (cropArea.Width + cropArea.X > image.Width)
                {
                    shouldCrop = false;
                }

                if (cropArea.Height + cropArea.Y > image.Height)
                {
                    shouldCrop = false;
                }
            }
        }

        private static Image CropImage(Image img, Rectangle cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);

            //Update: April 3, 2015
            //Rick Blaylock
            //Sometimes the width or height of the cropArea is 0, this throws an exception.
            //I decided to check the values and set the width or height to 1 if those values are
            //less than or equal to 0.
            if(cropArea.Width <= 0)
            {
                cropArea.Width = 1;
            }
            if(cropArea.Height <= 0)
            {
                cropArea.Height = 1;
            }
            Bitmap bmpCrop = bmpImage.Clone(cropArea,
            bmpImage.PixelFormat);
            return (Image)(bmpCrop);
        }
    }
}
