using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Math;
using FlatRedBall.IO;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System.IO;

namespace FlatRedBall.AnimationEditorForms.Editing
{
    public class ResizeMethods
    {
        #region Fields

        static ResizeMethods mSelf;

        #endregion

        public static ResizeMethods Self
        {
            get 
            {
                if (mSelf == null)
                {
                    mSelf = new ResizeMethods();
                }
                return mSelf; 
            }
        }


        internal void ResizeTextureClick(GraphicsDevice graphicsDevice)
        {
            // Early out
            if (SelectedState.Self.SelectedFrame == null)
            {
                MessageBox.Show("You must first select an AnimationFrame");
                return;
            }
            // End early out
            DialogResult result = AskWhatUserWantsToDo();

            if (result == DialogResult.OK)
            {
                Texture2D oldTexture = WireframeManager.Self.GetTextureForFrame(SelectedState.Self.SelectedFrame);
                int oldWidth = oldTexture.Width;
                int oldHeight = oldTexture.Height;

                string fileToSave = WireframeManager.Self.GetTextureFileNameForFrame(SelectedState.Self.SelectedFrame);
                string unmodifiedFileToSave = fileToSave;

                // pad to power of two.
                Texture2D resized = GetModifiedTexture2D(graphicsDevice);

                result = AskToReplaceOrRename();

                bool reReference = false;

                if (result == DialogResult.Yes) // replace original
                {
                    // do nothing
                }
                else// use renamed
                {
                    fileToSave = FileManager.RemoveExtension(fileToSave) + "Resize.png";
                    reReference = true;
                }



                using (Stream stream = System.IO.File.OpenWrite(fileToSave))
                {
                    resized.SaveAsPng(stream, resized.Width, resized.Height);
                    stream.Close();
                }
                resized.Dispose();

                List<AnimationFrameSave> modifiedFrames = new List<AnimationFrameSave>();

                AdjustAnimationToResize(ProjectManager.Self.AnimationChainListSave, oldWidth, oldHeight, resized.Width, resized.Height, unmodifiedFileToSave, modifiedFrames);

                if (reReference)
                {
                    foreach (AnimationFrameSave afs in modifiedFrames)
                    {
                        afs.TextureName = FileManager.RemoveExtension(afs.TextureName) + "Resize.png";
                    }
                }
            }


        }

        private static DialogResult AskToReplaceOrRename()
        {
            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
            mbmb.AddButton("Replace original file", DialogResult.Yes);
            mbmb.AddButton("Rename and reference renamed", DialogResult.No);

            mbmb.MessageText = "What would you like to do with the resized file?";

            return mbmb.ShowDialog();
        }

        private static DialogResult AskWhatUserWantsToDo()
        {
            MultiButtonMessageBox mbmb;

            mbmb = new MultiButtonMessageBox();
            string message = "What would you like to do?";
            mbmb.MessageText = message;
            mbmb.AddButton("Pad to power of two", System.Windows.Forms.DialogResult.OK);
            mbmb.AddButton("Cancel", System.Windows.Forms.DialogResult.Cancel);

            return mbmb.ShowDialog();
        }



        public void AdjustAnimationToResize(AnimationChainListSave acls, int oldWidth, int oldHeight, int newWidth, int newHeight, string absoluteTextureFileName, List<AnimationFrameSave> adjustedFrames)
        {
            string aclsDirectory = FileManager.GetDirectory(acls.FileName);

            absoluteTextureFileName = FileManager.Standardize(absoluteTextureFileName, null, false);

            foreach (var animationChain in acls.AnimationChains)
            {
                foreach (var frame in animationChain.Frames)
                {
                    string fullFrameTextureName = FileManager.Standardize(aclsDirectory + frame.TextureName, null, false);

                    if (fullFrameTextureName == absoluteTextureFileName)
                    {
                        AdjustFrameToResize(frame, oldWidth, oldHeight, newWidth, newHeight);
                        adjustedFrames.Add(frame);
                    }
                }
            }
        }


        public void AdjustFrameToResize(AnimationFrameSave afs, int oldWidth, int oldHeight, int newWidth, int newHeight)
        {

            AdjustValue(ref afs.LeftCoordinate, oldWidth, newWidth);
            AdjustValue(ref afs.RightCoordinate, oldWidth, newWidth);
            AdjustValue(ref afs.TopCoordinate, oldHeight, newHeight);
            AdjustValue(ref afs.BottomCoordinate, oldHeight, newHeight);
        }
        
        private void AdjustValue(ref float coordinateToAdjust, int oldWidth, int newWidth)
        {
            int pixels = MathFunctions.RoundToInt( oldWidth * coordinateToAdjust  );

            coordinateToAdjust = (float)pixels / newWidth;
        }

        private static Texture2D GetModifiedTexture2D(GraphicsDevice graphicsDevice)
        {
            FlatRedBall.Graphics.Texture.ImageData imageData;

            Texture2D texture = WireframeManager.Self.GetTextureForFrame(SelectedState.Self.SelectedFrame);

            int width = texture.Width;
            if (!MathFunctions.IsPowerOfTwo(width))
            {
                width = MathFunctions.NextPowerOfTwo(width);
            }

            int height = texture.Height;
            if (!MathFunctions.IsPowerOfTwo(height))
            {
                height = MathFunctions.NextPowerOfTwo(height);
            }

            imageData = new Graphics.Texture.ImageData(width, height);

            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);

            imageData.Blit(texture, sourceRectangle, new Point(0, 0));

            return imageData.ToTexture2D(false, graphicsDevice);
        }
    }
}
