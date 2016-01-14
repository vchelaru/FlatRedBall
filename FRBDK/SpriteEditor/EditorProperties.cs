using System;

using FlatRedBall;
using FlatRedBall.ManagedSpriteGroups;

using SpriteEditor.SEPositionedObjects;
using System.Drawing;
using EditorObjects;
using FlatRedBall.Graphics.Model;


namespace SpriteEditor
{
	/// <summary>
	/// Summary description for EditorProperties.
	/// </summary>
	public class EditorProperties
    {
        #region Fields

        bool mPositionsRelativeToCamera;

		bool mSnapToGrid;
		bool mConstrainDimensions;

		float mPixelSize;

        bool mSortYSecondary;

        float mSnappingGridSize;

        bool mCullSpriteGrids = true;

        Color mBackgroundColor;

        LineGrid mLineGrid;

        #endregion

        #region Properties

        public bool SnapToGrid
        {
            get { return mSnapToGrid; }
            set { mSnapToGrid = value;}
        }

        public float SnappingGridSize
        {
            get { return mSnappingGridSize; }
            set { mSnappingGridSize = value; }
        }

        public bool ConstrainDimensions
        {
            get { return mConstrainDimensions; }
            set { mConstrainDimensions = value; }
        }

        public float PixelSize
        {
            get { return mPixelSize; }
            set 
            {
                mPixelSize = value;
                if (mPixelSize != 0f)
                {
                    for (int i = 0; i < GameData.Scene.Sprites.Count; i++)
                    {
                        if (!((ISpriteEditorObject)GameData.Scene.Sprites[i]).ConstantPixelSizeExempt)
                        {
                            GameData.Scene.Sprites[i].PixelSize = GameData.EditorProperties.PixelSize;
                        }
                    }
                }            
            }
        }

        public bool SortYSecondary
        {
            get { return mSortYSecondary; }
            set { mSortYSecondary = value; }
        }

        public float AdditionalFade
        {
            get { return FlatRedBall.SpriteManager.AdditionalFade; }
            set { FlatRedBall.SpriteManager.AdditionalFade = value; }
        
        }

        public bool CullSpriteGrids
        {
            get { return mCullSpriteGrids; }
            set 
            {
                mCullSpriteGrids = false;
                if (mCullSpriteGrids == false)
                {
                    foreach (SpriteGrid sg in GameData.Scene.SpriteGrids)
                    {
                        sg.FillToBounds();
                    }
                }
            
            }
        }

        public bool FilteringOn
        {
            get 
            { 
                return FlatRedBallServices.GraphicsOptions.TextureFilter == 
                    Microsoft.DirectX.Direct3D.TextureFilter.Linear; 
            }
            set 
            {
                if (value)
                {
                    FlatRedBallServices.GraphicsOptions.TextureFilter =
                        Microsoft.DirectX.Direct3D.TextureFilter.Linear;
                }
                else
                {
                    FlatRedBallServices.GraphicsOptions.TextureFilter =
                        Microsoft.DirectX.Direct3D.TextureFilter.Point;
                }            
            }
        }

        public Color BackgroundColor
        {
            get { return SpriteManager.Camera.BackgroundColor; }
            set { SpriteManager.Camera.BackgroundColor = value; }
        }

        public LineGrid LineGrid
        {
            get { return mLineGrid; }
        }

        public bool WorldAxesDisplayVisible
        {
            get { return GameData.WorldAxesDisplay.Visible; }
            set { GameData.WorldAxesDisplay.Visible = value; }
        }

        public Color WorldAxesColor
        {
            get { return GameData.WorldAxesDisplay.Color; }
            set { GameData.WorldAxesDisplay.Color = value; }
        }

        public static FlatRedBall.Graphics.Model.ModelManager.LightSetup Lights
        {
            get { return ModelManager.ModelLightSetup; }
            set { ModelManager.ModelLightSetup = value; }
        }

        #endregion

        #region Methods
        public EditorProperties()
		{
			mPositionsRelativeToCamera = false;

			mSnapToGrid = false;
			mConstrainDimensions = false;

			mPixelSize = 0;

            mLineGrid = new LineGrid();
            mLineGrid.Visible = false;
        }
        #endregion
    }
}
