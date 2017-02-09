using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Content.AnimationChain;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Math;
using FlatRedBall.AnimationEditorForms.Data;
using FlatRedBall.IO;
using FlatRedBall.Glue.Controls;
using System.Windows.Forms;
using FlatRedBall.AnimationEditorForms.Preview;
using FlatRedBall.AnimationEditorForms.IO;

namespace FlatRedBall.AnimationEditorForms
{
    #region Enums

    public enum UnitType
    {
        Pixel,
        TextureCoordinate,
        SpriteSheet

    }

    #endregion

    public class AnimationFrameDisplayer : PropertyGridDisplayer
    {
        #region Fields

        Texture2D mTexture;
        UnitType mCoordinateType = UnitType.Pixel;

        #endregion

        #region Properties

        public override System.Windows.Forms.PropertyGrid PropertyGrid
        {
            get
            {
                return base.PropertyGrid;
            }
            set
            {
                if (value != null)
                {
                    value.PropertySort = System.Windows.Forms.PropertySort.Categorized;
                }
                base.PropertyGrid = value;
            }
        }
        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                if (value != null)
                {
                    throw new Exception("The AnimationFrameDisplayer requires knowledge of the Texture.  Use the SetAnimationFrame property");
                }
                else
                {
                    base.Instance = null;
                }
            }
        }

        public UnitType CoordinateType
        {
            get { return mCoordinateType; }
            set
            {
                mCoordinateType = value;

                RefreshShownProperties();
            }

        }

        TileMapInformation TileMapInformation
        {
            get
            {
                return ProjectManager.Self.TileMapInformationList.GetTileMapInformation(
                    ((AnimationFrameSave)mInstance).TextureName);
            }
        }

        public AnimationFrameSave AnimationFrameInstance
        {
            get
            {
                return Instance as AnimationFrameSave;
            }
        }

        #endregion

        public void SetFrame(AnimationFrameSave animationFrameSave, Texture2D texture2D)
        {
            mInstance = animationFrameSave;
            mTexture = texture2D;

            base.Instance = mInstance;

            RefreshShownProperties();
        }


        public AnimationFrameDisplayer() : base()
        {
            CoordinateType = AnimationEditorForms.UnitType.Pixel;
        }

        private void RefreshShownProperties()
        {
            /////////Early Out/////////////
            if (mInstance == null)
            {
                return;
            }
            ///////End Early Out //////////
            
            ExcludeAllMembers();
            
            IncludeMember("FrameLength");
            IncludeMember("FlipHorizontal");
            IncludeMember("FlipVertical");

            IncludeMember("TextureName", typeof(string),
                TextureNameChange, 
                GetTextureName,
                null,
                new Attribute[] { PropertyGridDisplayer.FileWindowAttribute });
            
            if (mCoordinateType == AnimationEditorForms.UnitType.Pixel)
            {
                IncludeMember("X", typeof(int),
                    CoordinateChange,
                    GetPixelX);

                IncludeMember("Y", typeof(int),
                    CoordinateChange,
                    GetPixelY);

                IncludeMember("Width", typeof(int),
                    CoordinateChange,
                    GetPixelWidth);

                IncludeMember("Height", typeof(int),
                    CoordinateChange,
                    GetPixelHeight);

            }
            else if (mCoordinateType == AnimationEditorForms.UnitType.TextureCoordinate) 
            {
                IncludeMember("LeftCoordinate");

                IncludeMember("TopCoordinate");

                IncludeMember("RightCoordinate");

                IncludeMember("BottomCoordinate");

            }
            else if (mCoordinateType == AnimationEditorForms.UnitType.SpriteSheet)
            {
                //IncludeMember
                IncludeMember("TileX", typeof(int),
                    CoordinateChange,
                    GetTileX);

                IncludeMember("TileY", typeof(int),
                    CoordinateChange,
                    GetTileY);
            }

            IncludeMember("RelativeX", typeof(float), 
                SetRelativeX, GetRelativeX);
            IncludeMember("RelativeY", typeof(float),
                SetRelativeY, GetRelativeY);
        }

        private object GetRelativeX()
        {
            return AnimationFrameInstance.RelativeX * PreviewManager.Self.OffsetMultiplier;
        }

        private void SetRelativeX(object sender, MemberChangeArgs args)
        {
            AnimationFrameInstance.RelativeX = (float)args.Value / PreviewManager.Self.OffsetMultiplier;
        }

        private object GetRelativeY()
        {
            return AnimationFrameInstance.RelativeY * PreviewManager.Self.OffsetMultiplier;
        }

        private void SetRelativeY(object sender, MemberChangeArgs args)
        {
            AnimationFrameInstance.RelativeY = (float)args.Value / PreviewManager.Self.OffsetMultiplier;
        }

        void CoordinateChange(object sender, MemberChangeArgs args)
        {
            AnimationFrameSave frame = Instance as AnimationFrameSave;

            if (mTexture == null)
            {
                // We can't do anything.
            }
            else
            {
                if (args.Member == "X")
                {
                    int oldValue = MathFunctions.RoundToInt( frame.LeftCoordinate * mTexture.Width );
                    int newValue = ((int)args.Value);

                    int increaseAsInt = newValue - oldValue;
                    float increaseAsTextureCoord = increaseAsInt / (float) mTexture.Width;

                    // Users expect the entire frame to shift, so we want to shift the right coordinates too.
                    frame.LeftCoordinate += increaseAsTextureCoord;
                    frame.RightCoordinate += increaseAsTextureCoord;

                    SetForPixelCoordinates(ref frame.LeftCoordinate, mTexture.Width);
                    SetForPixelCoordinates(ref frame.RightCoordinate, mTexture.Width);

                }
                else if (args.Member == "Y")
                {
                    int oldValue = MathFunctions.RoundToInt(frame.TopCoordinate * mTexture.Height);
                    int newValue = ((int)args.Value);

                    int increaseAsInt = newValue - oldValue;
                    float increaseAsTextureCoord = increaseAsInt / (float) mTexture.Height;



                    frame.TopCoordinate += increaseAsTextureCoord;
                    frame.BottomCoordinate += increaseAsTextureCoord;

                    SetForPixelCoordinates(ref frame.TopCoordinate, mTexture.Height);
                    SetForPixelCoordinates(ref frame.BottomCoordinate, mTexture.Height);
                }
                else if (args.Member == "Width")
                {
                    float widthInCoords = ((int)args.Value) / (float)mTexture.Width;
                    frame.RightCoordinate = frame.LeftCoordinate + widthInCoords;
                }
                else if (args.Member == "Height")
                {
                    float heightInCoords = ((int)args.Value) / (float)mTexture.Height;
                    frame.BottomCoordinate = frame.TopCoordinate + heightInCoords;
                }
                else if (args.Member == "TileX")
                {
                    int valueAsInt = (int)args.Value;
                    SetTileX(frame, valueAsInt);
                }
                else if (args.Member == "TileY")
                {
                    int valueAsInt = (int)args.Value;
                    SetTileY(frame, valueAsInt);
                }
            }
        }

        void SetForPixelCoordinates(ref float value, int dimension)
        {
            value = MathFunctions.RoundToInt(value * dimension) / (float)dimension;
        }

        void TextureNameChange(object sender, MemberChangeArgs args)
        {
            string absoluteFileName = (string)args.Value; 
            string achxFolder;

            if (!System.IO.File.Exists(absoluteFileName))
            {
                MessageBox.Show("The file\n" + absoluteFileName + "\ndoesn't exist");
            }
            else
            {

                bool shouldProceed =
                    TryAskToMoveFileRelativeToAnimationChainFile(false,ref absoluteFileName, out achxFolder);

                if (shouldProceed)
                {
                    ShowWarningsForTooBigTextures(absoluteFileName);


                    string relativeFileName = FileManager.MakeRelative(absoluteFileName, achxFolder);

                    AnimationFrameInstance.TextureName = relativeFileName;
                }
            }
        }

        public static void ShowWarningsForTooBigTextures(string absoluteFileName)
        {
            int width = ImageHeader.GetDimensions(absoluteFileName).Width;

            int height = ImageHeader.GetDimensions(absoluteFileName).Height;

            // In 2013 this is a reasonable
            // max width, especially for phones.
            // Maybe in the far future this will go
            // up.
            // Update December 29, 2016
            // Get with the times, 4k is what it's all
            // about!
            int maxDimension = 4096;
            bool shown = false;
            if (width > maxDimension)
            {
                MessageBox.Show(
                    $"The texture is wider than {maxDimension}.  This could cause problems.  It is recommended to keep your texture at or under {maxDimension} width.");
                shown = true;
            }
            if (!shown && height > maxDimension)
            {
                MessageBox.Show(
                    $"The texture is taller than {maxDimension}.  This could cause problems.  It is recommended to keep your texture at or under {maxDimension} height.");
            }
        }


        public static bool ShouldAskUserToCopyFile(string absoluteFileName)
        {

            string achxFolder = FileManager.GetDirectory(ProjectManager.Self.FileName);

            bool shouldAsk = !FileManager.IsRelativeTo(absoluteFileName, achxFolder);

            if (shouldAsk)
            {
                string projectFolder = FlatRedBall.AnimationEditorForms.CommandsAndState.ApplicationState.Self.ProjectFolder;

                if (!string.IsNullOrEmpty(projectFolder) && FileManager.IsRelativeTo(absoluteFileName, projectFolder))
                {
                    shouldAsk = false;
                }
            }

            return shouldAsk;
        }

        public static bool TryAskToMoveFileRelativeToAnimationChainFile(bool copyWithoutAsking, ref string absoluteFileName, out string achxFolder)
        {
            bool shouldProceed = true;

            achxFolder = FileManager.GetDirectory(ProjectManager.Self.FileName);

            if (ShouldAskUserToCopyFile(absoluteFileName))
            {

                DialogResult result = DialogResult.Yes;

                if(!copyWithoutAsking)
                {

                    MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                    mbmb.MessageText = "The selected file:\n\n" + absoluteFileName + "\n\nis not relative to the Animation Chain file.  What would you like to do?";

                    mbmb.AddButton("Copy the file to the same folder as the Animation Chain", System.Windows.Forms.DialogResult.Yes);
                    mbmb.AddButton("Keep the file where it is (this may limit the portability of the Animation Chain file)", System.Windows.Forms.DialogResult.No);

                    result = mbmb.ShowDialog();
                }

                if (result == DialogResult.Yes)
                {
                    string destination = achxFolder + FileManager.RemovePath(absoluteFileName);

                    try
                    {
                        System.IO.File.Copy(absoluteFileName, destination, true);
                        absoluteFileName = destination;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Could not copy the file:\n" + e);
                    }

                }
                else if (result == DialogResult.Cancel)
                {
                    shouldProceed = false;
                }
            }

            return shouldProceed;
        }

        object GetTextureName()
        {
            return ((AnimationFrameSave)Instance).TextureName;
        }

        public void SetTileY(AnimationFrameSave frame, int valueAsInt)
        {
            TileMapInformation information = TileMapInformation;
            if (information != null)
            {
                int pixelPosition = valueAsInt * information.TileHeight;
                frame.TopCoordinate = pixelPosition / (float)mTexture.Height;
                frame.BottomCoordinate = frame.TopCoordinate + (information.TileHeight / (float)mTexture.Height);

            }
        }

        public void SetTileX(AnimationFrameSave frame, int valueAsInt)
        {
            TileMapInformation information = TileMapInformation;
            if (information != null)
            {
                int pixelPosition = valueAsInt * information.TileWidth;
                frame.LeftCoordinate = pixelPosition / (float)mTexture.Width;
                frame.RightCoordinate = frame.LeftCoordinate + (information.TileWidth / (float)mTexture.Width);

            }
        }

        object GetPixelX()
        {
            AnimationFrameSave frame = Instance as AnimationFrameSave;
            if (mTexture == null)
            {
                return 0;
            }
            else
            {
                return MathFunctions.RoundToInt(mTexture.Width * frame.LeftCoordinate);
            }
        }

        object GetTileX()
        {
            AnimationFrameSave frame = Instance as AnimationFrameSave;
            if (mTexture == null || TileMapInformation == null || TileMapInformation.TileWidth == 0)
            {
                return 0;
            }
            else
            {
                return MathFunctions.RoundToInt((mTexture.Width * frame.LeftCoordinate) / TileMapInformation.TileWidth );
            }


        }


        object GetPixelY()
        {
            AnimationFrameSave frame = Instance as AnimationFrameSave;
            if (mTexture == null)
            {
                return 0;
            }
            else
            {
                return MathFunctions.RoundToInt(mTexture.Height * frame.TopCoordinate);
            }
        }

        object GetTileY()
        {
            AnimationFrameSave frame = Instance as AnimationFrameSave;
            if (mTexture == null || TileMapInformation == null || TileMapInformation.TileHeight == 0)
            {
                return 0;
            }
            else
            {
                return MathFunctions.RoundToInt((mTexture.Height * frame.TopCoordinate) / TileMapInformation.TileHeight);
            }


        }

        object GetPixelWidth()
        {
            AnimationFrameSave frame = Instance as AnimationFrameSave;
            if (mTexture == null)
            {
                return 0;
            }
            else
            {
                return MathFunctions.RoundToInt(mTexture.Width * (frame.RightCoordinate -  frame.LeftCoordinate));
            }
        }

        object GetPixelHeight()
        {
            AnimationFrameSave frame = Instance as AnimationFrameSave;
            if (mTexture == null)
            {
                return 0;
            }
            else
            {
                return MathFunctions.RoundToInt(mTexture.Height * (frame.BottomCoordinate - frame.TopCoordinate));
            }
        }
    }
}
