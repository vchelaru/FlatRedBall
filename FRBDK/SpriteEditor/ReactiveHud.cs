using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Math;
using FlatRedBall;
using FlatRedBall.ManagedSpriteGroups;
using EditorObjects.Hud;
using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Model;
using FlatRedBall.Gui;
using FlatRedBall.Input;

namespace SpriteEditor
{
    public class ReactiveHud
    {
        #region Fields

        Polygon mObjectOverMarker;

        static PositionedObjectList<ScalableSelector> mCurrentSelectionRectangles =
            new PositionedObjectList<ScalableSelector>();

        static PositionedModel mModelOverHighlight = new PositionedModel();
        static PositionedModel mSelectedModelHighlight = new PositionedModel();

		SpriteFrameIndividualSpriteOutline mSpriteFrameIndividualSpriteOutline;
        TextWidthHandles mTextWidthHandles;

        #endregion

        #region Methods

        public ReactiveHud()
        {
            mObjectOverMarker = Polygon.CreateRectangle(1, 1);
            mObjectOverMarker.Color = System.Drawing.Color.Red;

            #region Create mModelOver

            mModelOverHighlight.Visible = false;
            ModelManager.AddModel(mModelOverHighlight);
            ModelManager.AddToLayer(mModelOverHighlight, SpriteManager.Camera.Layer);
            mModelOverHighlight.ColorOperation = Microsoft.DirectX.Direct3D.TextureOperation.Add;
            mModelOverHighlight.Red = 90f;
            mModelOverHighlight.Green = 90f;
            mModelOverHighlight.Blue = 90f;

            mSelectedModelHighlight.Visible = false;
            ModelManager.AddModel(mSelectedModelHighlight);
            ModelManager.AddToLayer(mSelectedModelHighlight, SpriteManager.Camera.Layer);
            mSelectedModelHighlight.ColorOperation = Microsoft.DirectX.Direct3D.TextureOperation.Add;
            mSelectedModelHighlight.Red = 160;
            mSelectedModelHighlight.Green = 160;
            mSelectedModelHighlight.Blue = 160;
            
            #endregion

			mSpriteFrameIndividualSpriteOutline = new SpriteFrameIndividualSpriteOutline();
            mTextWidthHandles = new TextWidthHandles();

            ShapeManager.AddPolygon(mObjectOverMarker);
        }

        #region Public Methods

        public void Activity()
        {
            UpdateObjectOverMarker();

            //Updates the position of the Rectangle Polygons which mark position.
            UpdateSelectionUI();

			UpdateSpriteFrameHud();

            UpdateTextWidthHandles();

        }



        #endregion

        #region Private Methods

        private void UpdateObjectOverMarker()
        {
            SECursor cursor = GameData.Cursor;

            float polygonScaleX = (float)mObjectOverMarker.Points[0].X;
            float polygonScaleY = (float)mObjectOverMarker.Points[0].Y;
            mModelOverHighlight.Visible = false;

            if (cursor.SpritesOver.Count != 0)
            {
                mObjectOverMarker.Visible = true;

                mObjectOverMarker.Position = cursor.SpritesOver[0].Position;
                mObjectOverMarker.RotationMatrix = cursor.SpritesOver[0].RotationMatrix;

                mObjectOverMarker.ScaleBy(cursor.SpritesOver[0].ScaleX / polygonScaleX,
                    cursor.SpritesOver[0].ScaleY / polygonScaleY);

            }
            else if (cursor.SpriteFramesOver.Count != 0)
            {
                mObjectOverMarker.Visible = true;

                mObjectOverMarker.Position = cursor.SpriteFramesOver[0].Position;
                mObjectOverMarker.RotationMatrix = cursor.SpriteFramesOver[0].RotationMatrix;

                mObjectOverMarker.ScaleBy(cursor.SpriteFramesOver[0].ScaleX / polygonScaleX,
                    cursor.SpriteFramesOver[0].ScaleY / polygonScaleY);

            }
            else if (cursor.PositionedModelsOver.Count != 0)
            {
                PositionedModel modelOver = cursor.PositionedModelsOver[0];

                mModelOverHighlight.Visible = true;
                mModelOverHighlight.SetDataFrom(modelOver);
                mModelOverHighlight.Position = modelOver.Position;
                mModelOverHighlight.RotationMatrix = modelOver.RotationMatrix;
                mModelOverHighlight.ScaleX = modelOver.ScaleX;
                mModelOverHighlight.ScaleY = modelOver.ScaleY;
                mModelOverHighlight.ScaleZ = modelOver.ScaleZ;


            }
            else if (cursor.TextsOver.Count != 0)
            {
                mObjectOverMarker.Visible = true;

                mObjectOverMarker.Position = cursor.TextsOver[0].Position;

                mObjectOverMarker.Position.X = cursor.TextsOver[0].HorizontalCenter;
                mObjectOverMarker.Position.Y = cursor.TextsOver[0].VerticalCenter;

                mObjectOverMarker.RotationMatrix = cursor.TextsOver[0].RotationMatrix;

                mObjectOverMarker.ScaleBy(cursor.TextsOver[0].ScaleX / polygonScaleX,
                    cursor.TextsOver[0].ScaleY / polygonScaleY);

            }
            else
            {
                mObjectOverMarker.Visible = false;
            }
        }

        static void UpdateSelectionUI()
        {
            #region Handle Text, Sprite, and SpriteFrame selection

            #region Get the number of objects needed
            int numberOfRectangles = 
                GameData.EditorLogic.CurrentSprites.Count + 
                GameData.EditorLogic.CurrentSpriteFrames.Count;// +
                //GameData.EditorLogic.CurrentTexts.Count;
            #endregion

            #region Create and destroy to get the number needed

            while (numberOfRectangles < mCurrentSelectionRectangles.Count)
            {
                mCurrentSelectionRectangles[0].Destroy();
            }
            while (numberOfRectangles > mCurrentSelectionRectangles.Count)
            {
                mCurrentSelectionRectangles.Add(new ScalableSelector());
            }
            #endregion


            int currentIndex = 0;

            foreach (Sprite sprite in GameData.EditorLogic.CurrentSprites)
            {
                mCurrentSelectionRectangles[currentIndex].UpdateToObject(sprite, SpriteManager.Camera);
                currentIndex++;
            }

            foreach (SpriteFrame spriteFrame in GameData.EditorLogic.CurrentSpriteFrames)
            {
                mCurrentSelectionRectangles[currentIndex].UpdateToObject(spriteFrame, SpriteManager.Camera);
                currentIndex++;
            }

            //foreach (Text text in GameData.EditorLogic.CurrentTexts)
            //{
            //    mCurrentSelectionRectangles[currentIndex].UpdateToObject(text, SpriteManager.Camera);
            //    currentIndex++;
            //}

            #endregion

            #region Handle PositionedModel selection

            mSelectedModelHighlight.Visible = GameData.EditorLogic.CurrentPositionedModels.Count > 0;

            if (mSelectedModelHighlight.Visible)
            {
                PositionedModel selectedModel = GameData.EditorLogic.CurrentPositionedModels[0];

                mSelectedModelHighlight.SetDataFrom(selectedModel);

                mSelectedModelHighlight.Position = selectedModel.Position;
                mSelectedModelHighlight.RotationMatrix = selectedModel.RotationMatrix;
                mSelectedModelHighlight.ScaleX = selectedModel.ScaleX;
                mSelectedModelHighlight.ScaleY = selectedModel.ScaleY;
                mSelectedModelHighlight.ScaleZ = selectedModel.ScaleZ;
            }


            #endregion
        }

		private void UpdateSpriteFrameHud()
		{
			mSpriteFrameIndividualSpriteOutline.Visible = GameData.EditorLogic.CurrentSpriteFrames.Count != 0;



			if (mSpriteFrameIndividualSpriteOutline.Visible)
			{
				mSpriteFrameIndividualSpriteOutline.SpriteFrame = GameData.EditorLogic.CurrentSpriteFrames[0];

				mSpriteFrameIndividualSpriteOutline.Update();
			}
		}


        private void UpdateTextWidthHandles()
        {
            if (GameData.EditorLogic.CurrentTexts.Count != 0)
            {
                mTextWidthHandles.Text = GameData.EditorLogic.CurrentTexts[0];
                mTextWidthHandles.Update();
            }
            else
            {
                mTextWidthHandles.Text = null;
                mTextWidthHandles.Update();
            }
        }       
        
        
        #endregion

        #endregion
    }
}
