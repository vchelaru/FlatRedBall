using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;
using FlatRedBall.Graphics;
using FlatRedBall.Utilities;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics.Animation;
using FlatRedBall.Math;
using System.Collections.ObjectModel;


namespace FlatRedBall.ManagedSpriteGroups
{
    [Obsolete("Use Tiled - it's way more efficient and powerful")]
    public class SpriteGrid : INameable, IEquatable<SpriteGrid>
    {
        #region Enums
        public enum Plane
        {
            XY,
            XZ,
            YZ
        }
        #endregion

        #region Fields

        #region Bounds
        // These have default values for quick debugging and testing.
        float mXRightBound = 10;
        float mXLeftBound = -10;

        float mYTopBound = 10;
        float mYBottomBound = -10;

        float mZCloseBound = -10 * MathFunctions.ForwardVector3.Z;
        float mZFarBound = 10 * MathFunctions.ForwardVector3.Z;
        #endregion

        Camera mCamera;
        Sprite mBlueprint;
        FlatRedBall.Utilities.GameRandom mRandom;

        internal List<SpriteList> mVisibleSprites = new List<SpriteList>();
        ReadOnlyCollection<SpriteList> mVisibleSpritesReadOnlyCollection;

        TextureGrid<Texture2D> mTextureGrid;
        TextureGrid<FloatRectangle?> mTextureCoordinateGrid;
        internal TextureGrid<AnimationChain> mAnimationTextureGrid;

        string mName;
        string mContainedSpriteListName;

        float mGridSpacingX;
        float mGridSpacingY;

        Plane mPlane;

        SpriteList mSpritesAddedDuringManageCall = new SpriteList();

        Vector3 mLastBlueprintPosition;

        // Set this to true by default - performance isn't as great
        // but it makes the Sprites fully functional.
        bool mCreatesAutomaticallyUpdatedSprites = true; 
        bool mCreatesParticleSprites;
        bool mAddSpritesToSpriteManager = true;

        OrderingMode mOrderingMode = OrderingMode.DistanceFromCamera;


        Layer mLayer;

        #endregion

        #region Properties

        #region Bounds

        public float XRightBound
        {
            set { mXRightBound = value; }
            get { return mXRightBound; }
        }

        public float XRightFilledBound
        {
            get
            {
                return GetRightFilledBound(mXRightBound, mGridSpacingX, mBlueprint.ScaleX, mBlueprint.Position.X);
            }
        }

        public float XLeftBound
        {
            set { mXLeftBound = value; }
            get { return mXLeftBound; }
        }

        public float XLeftFilledBound
        {
            get
            {
                return GetLeftFilledBound(mXLeftBound, mGridSpacingX, mBlueprint.ScaleX, mBlueprint.Position.X);
            }
        }

        public float YTopBound
        {
            set { mYTopBound = value; }
            get { return mYTopBound; }
        }

        public float YTopFilledBound
        {
            get
            {
                return GetTopFilledBound(mYTopBound, mGridSpacingY, mBlueprint.ScaleY, mBlueprint.Position.Y);
            }
        }

        public float YBottomBound
        {
            set { mYBottomBound = value; }
            get { return mYBottomBound; }
        }

        public float YBottomFilledBound
        {
            get
            {
                return GetBottomFilledBound(mYBottomBound, mGridSpacingY, mBlueprint.ScaleY, mBlueprint.Y);
            }
        }

        public float ZCloseBound
        {
            set { mZCloseBound = value; }
            get { return mZCloseBound; }
        }

        public float ZFarBound
        {
            set
            {
                mZFarBound = value;
            }
            get
            {
                return mZFarBound;
            }
        }

        #endregion


        public bool AddSpritesToSpriteManager
        {
            get { return mAddSpritesToSpriteManager; }
            set { mAddSpritesToSpriteManager = value; }
        }


        public TextureGrid<AnimationChain> AnimationChainGrid
        {
            get { return mAnimationTextureGrid; }
            set { mAnimationTextureGrid = value; }
        }
        //public TextureGrid<AnimationChain>


        public Sprite Blueprint
        {
            get
            {
                return mBlueprint;
            }
            // no setter - just set the individual properties on the Blueprint if needed
            // to modify after the fact.
        }


        public bool CreatesAutomaticallyUpdatedSprites
        {
            get { return mCreatesAutomaticallyUpdatedSprites; }
            set { mCreatesAutomaticallyUpdatedSprites = value; }
        }


        public bool CreatesParticleSprites
        {
            get { return mCreatesParticleSprites; }
            set { mCreatesParticleSprites = value; }
        }


        public TextureGrid<FloatRectangle?> DisplayRegionGrid
        {
            get { return mTextureCoordinateGrid; }
            set { mTextureCoordinateGrid = value; }
        }


        public float GridSpacing
        {
            set
            {
                // When grid spacing changes so do the first painted values
                // Scale these values by how much has changed
                double scaleValue = (double)value / (double)mGridSpacingX;

                mTextureGrid.ScaleBy(scaleValue);
                mTextureCoordinateGrid.ScaleBy(scaleValue);
                mAnimationTextureGrid.ScaleBy(scaleValue);


                mGridSpacingX = value;
                mGridSpacingY = value;
            }
            get { return mGridSpacingX; }
        }


        public float GridSpacingX
        {
            get { return mGridSpacingX; }
            set 
            { 
                mGridSpacingX = value; 
                mTextureGrid.GridSpacingX = value;
                mTextureCoordinateGrid.GridSpacingX = value;
                mAnimationTextureGrid.GridSpacingX = value;
            }
        }


        public float GridSpacingY
        {
            get { return mGridSpacingY; }
            set 
            { 
                mGridSpacingY = value; 
                mTextureGrid.GridSpacingY = value;
                mTextureCoordinateGrid.GridSpacingY = value;
                mAnimationTextureGrid.GridSpacingY = value;
            }
        }


        public Plane GridPlane
        {
            get { return mPlane; }
        }


        public float FurthestLeftX
        {
            get
            {
                float baseX = mBlueprint.X - mBlueprint.ScaleX;
                if (baseX - GridSpacingX > XLeftBound)
                {
                    // Vic says: This was reporting the wrong values on Sept 14 2010.  I fixed it.

                    // the furthest left is to the left of the Blueprint.X
                    float distance = baseX - XLeftBound;
                    float amountToShift = (int)(distance / GridSpacingX) * GridSpacingX;
                    return baseX - amountToShift;
                }
                else
                {
                    float distance = baseX - XLeftBound;


                    float amountToShift = ((int)distance / (int)GridSpacingX) * GridSpacingX;
                    return baseX - amountToShift;
                }
            }
        }


        public float FurthestRightX
        {
            get
            {
                float baseX = mBlueprint.X + mBlueprint.ScaleX;
                if (baseX + GridSpacingX < XRightBound)
                {
                    // the furthest left is to the left of the Blueprint.X
                    float distance = XRightBound - baseX;
                    float amountToShift = ((int)distance / (int)GridSpacingX) * GridSpacingX;
                    return baseX + amountToShift;
                }
                else
                {
                    float distance = XRightBound - baseX;
                    float amountToShift = (-1 + (int)distance / (int)GridSpacingX) * GridSpacingX;
                    return baseX + amountToShift;
                }
            }
        }


        public float FurthestBottomY
        {
            get
            {
                float baseY = mBlueprint.Y - mBlueprint.ScaleY;
                if (baseY - GridSpacingY > YBottomBound)
                {
                    float distance = baseY - YBottomBound;
                    float amountToShift = ((int)distance / (int)GridSpacingY) * GridSpacingY;
                    return baseY - amountToShift;
                }
                else
                {
                    float distance = baseY - YBottomBound;
                    float amountToShift = (-1 + (int)distance / (int)GridSpacingY) * GridSpacingY;
                    return baseY - amountToShift;
                }
            }
        }


        public float FurthestTopY
        {
            get
            {
                float baseY = mBlueprint.Y + mBlueprint.ScaleY;
                if (baseY + GridSpacingY < YTopBound)
                {
                    // the furthest left is to the left of the Blueprint.X
                    float distance = YTopBound - baseY;
                    float amountToShift = ((int)distance / (int)GridSpacingY) * GridSpacingY;
                    return baseY + amountToShift;
                }
                else
                {
                    float distance = YTopBound - baseY;

                    float amountToShift = ((int)distance / (int)GridSpacingY) * GridSpacingY;
                    return baseY + amountToShift;
                }
            }
        }

         #region XML Docs
        /// <summary>
        /// The layer on which the SpriteGrid should place newly-created Sprites.
        /// </summary>
        #endregion
        public Layer Layer
        {
            get { return mLayer; }
            set { mLayer = value; }
        }


        public string Name
        {
            get { return mName; }
            set 
            { 
                mName = value;
                mContainedSpriteListName = "SpriteList in SpriteGrid " + mName;
            }
        }


        public OrderingMode OrderingMode
        {
            get { return mOrderingMode; }
            set 
            { 
                mOrderingMode = value;

#if FRB_MDX
                switch (value)
                {
                    case OrderingMode.DistanceFromCamera:

                        mBlueprint.mOrdered = true;
                        break;
                    case OrderingMode.ZBuffered:
                        mBlueprint.mOrdered = false;
                        break;
                }
#endif
            }
        }


        public System.Collections.ObjectModel.ReadOnlyCollection<SpriteList> VisibleSprites
        {
            get { return mVisibleSpritesReadOnlyCollection; }
        }


        public TextureGrid<Texture2D> TextureGrid
        {
            get
            {
                return mTextureGrid;
            }
        }

        #endregion

        #region Methods

        #region Constructors

        #region XML Docs
        /// <summary>
        /// Creates a SpriteGrid using the TextManager's DefaultFont.  This is used because it's the only
        /// Texture2D stored internally in the engine.  To set the default Texture, use the overload which
        /// accepts a Texture2D.
        /// </summary>
        #endregion
        public SpriteGrid() : this(SpriteManager.Camera, Plane.XY, SpriteManager.AddSprite(TextManager.DefaultFont.Texture))
        {
            // Since the constructor adds the blueprint to the SpriteManager, we need to remove it.
            SpriteManager.RemoveSprite(mBlueprint);
        }

        public SpriteGrid(Texture2D baseTexture)
            : this(
                SpriteManager.Camera,
                Plane.XY,
                SpriteManager.AddSprite(baseTexture))
        {
            // Since the constructor adds the blueprint to the SpriteManager, we need to remove it.
            SpriteManager.RemoveSprite(mBlueprint);
        }

        #region XML Docs
        /// <summary>
        /// Creates a new SpriteGrid.
        /// </summary>
        /// <remarks>
        /// The mBlueprintToUse argument Sprite reference is kept internally and used as the blue print.
        /// In other words, the SpriteGrid does not create a new Sprite internally, but uses the
        /// arguemnt Sprite.  When creating SpriteGrids in code, it is common to create a new Sprite
        /// only to serve as a SpriteGrid blue print, then remove it from the SpriteManager's memory. 
        /// If the Sprite passed as the mBlueprintToUse is modified after the SpriteGrid is created,
        /// this will change the SpriteGrid bluerint
        ///
        /// </remarks>
        /// <param name="camera">Reference to the camnera used to determine whether a point on the SpriteGrid is in the scren.</param>
        /// <param name="gridPlane">Whether the SpriteGrid should extend on the XY or XZ plane.</param>
        /// <param name="blueprintToUse">Reference to a Sprite representing the mBlueprint to be used for the grid.</param>
        #endregion
        public SpriteGrid(Camera camera, Plane gridPlane, Sprite blueprintToUse) : 
            this(camera, gridPlane, blueprintToUse, new TextureGrid<Texture2D>())
        {  }

        public SpriteGrid(Camera camera, Plane gridPlane, Sprite blueprintToUse, TextureGrid<Texture2D> textureGrid)
        {

            mRandom = FlatRedBallServices.Random;
            mCamera = camera;

            if (textureGrid == null)
                mTextureGrid = new TextureGrid<Texture2D>();
            else
                mTextureGrid = textureGrid;

            mTextureCoordinateGrid = new TextureGrid<FloatRectangle?>();

            mAnimationTextureGrid = new TextureGrid<AnimationChain>();


            GridSpacingX = blueprintToUse.ScaleX * 2;
            GridSpacingY = blueprintToUse.ScaleY * 2;

            mVisibleSpritesReadOnlyCollection = new System.Collections.ObjectModel.ReadOnlyCollection<SpriteList>(mVisibleSprites);

            mBlueprint = blueprintToUse;
            mLastBlueprintPosition = mBlueprint.Position;

            mPlane = gridPlane;

            mTextureGrid.mBaseTexture = mBlueprint.Texture;
            mTextureCoordinateGrid.BaseTexture = new FloatRectangle(
                blueprintToUse.TopTextureCoordinate,
                blueprintToUse.BottomTextureCoordinate,
                blueprintToUse.LeftTextureCoordinate,
                blueprintToUse.RightTextureCoordinate); 
            
            mAnimationTextureGrid.mBaseTexture = null;
        }

        #endregion

        #region Public Methods

        public SpriteGrid Clone()
        {
            SpriteGrid tempGrid = (SpriteGrid)MemberwiseClone();
            tempGrid.mTextureGrid = mTextureGrid.Clone();
            tempGrid.mTextureCoordinateGrid = mTextureCoordinateGrid.Clone();
            tempGrid.mAnimationTextureGrid = mAnimationTextureGrid.Clone();

            mSpritesAddedDuringManageCall = new SpriteList();

            tempGrid.mVisibleSprites = new List<SpriteList>();
            tempGrid.mVisibleSpritesReadOnlyCollection = new System.Collections.ObjectModel.ReadOnlyCollection<SpriteList>(tempGrid.mVisibleSprites);

            tempGrid.mBlueprint = mBlueprint.Clone();

            return tempGrid;
        }


        public void CopyToBlueprint(Sprite spriteToCopy)
        {
            mBlueprint = spriteToCopy.Clone();
        }

        #region XML Docs
        /// <summary>
        /// Destroys the SpriteGrid by removing all contained Sprites and clearing the TextureGrid.
        /// </summary>
        /// <remarks>
        /// <para>This method will only remove all contained Sprites from the SpriteManager and clear out the
        /// TextureGrid.  The SpriteGrid will still reference the the blueprint
        /// Sprite and have the same Bounds and GridSpacing.</para>
        /// <para>If the Manage method is called after this method is called, 
        /// the SpriteGrid will throw an out of bounds exception.  To refill the 
        /// SpriteGrid after this method has been called, it must first be populated.</para>
        /// </remarks>
        #endregion
        public void Destroy()
        {
            for (int i = mVisibleSprites.Count - 1; i > -1; i--)
            {
                SpriteManager.RemoveSpriteList(mVisibleSprites[i]);
            }
            this.mVisibleSprites.Clear();

            mTextureGrid.Clear();
            mTextureCoordinateGrid.Clear();
            mAnimationTextureGrid.Clear();
        }


        public virtual void FillToBounds()
        {
            /*
             * There are a few ways to till to bounds, but the easiest way is to just move the mCamera back
             * to negative infinity on the Z axis and call the expand functions.
             */
            float tempCameraZ = mCamera.Z;
            bool wasCameraOrthogonal = mCamera.Orthogonal;

            mCamera.Orthogonal = false;
#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
            mCamera.Z = float.PositiveInfinity;
#else
            mCamera.Z = float.NegativeInfinity;
#endif

            if (mVisibleSprites.Count == 0)
                PopulateGrid();
            else
            {

                if (mPlane == Plane.XY)
                {
                    expandXNeg();
                    expandXPos();
                    expandYNeg();
                    expandYPos();
                }
                else
                {
                    expandZNeg();
                    expandZPos();

                    expandXNeg();
                    expandXPos();

                }
            }
            RefreshPaint();

            mCamera.Orthogonal = wasCameraOrthogonal;
            mCamera.Z = tempCameraZ;
        }


        public void GetIndexAt(double x, double y, double z, out int xIndex, out int yIndex)
        {
            if (mVisibleSprites.Count == 0)
            {
                xIndex = -1;
                yIndex = -1;
                return;
            }
            xIndex = 0;
            yIndex = 0;

            x = mBlueprint.X + (int)(System.Math.Round((x - mBlueprint.X) / mGridSpacingX)) * mGridSpacingX;

            if (mPlane == Plane.XY)
            {
                y = mBlueprint.Y + (int)(System.Math.Round((y - mBlueprint.Y) / mGridSpacingY)) * mGridSpacingY;
                yIndex = (int)(System.Math.Floor((y - (mVisibleSprites[0][0].Y - mGridSpacingY / 2)) / mGridSpacingY));
            }
            else
            {
                y = mBlueprint.Z + (int)(System.Math.Round((z - mBlueprint.Z) / mGridSpacingY)) * mGridSpacingY;
                yIndex = (int)(System.Math.Floor((z - (mVisibleSprites[0][0].Z - mGridSpacingY / 2)) / mGridSpacingY));
            }

            if (yIndex > -1 && yIndex < mVisibleSprites.Count)
            {
                xIndex = (int)(System.Math.Floor(((x - (mVisibleSprites[yIndex][0].X - mGridSpacingX / 2)) / mGridSpacingX)));
            }
            // If the row is not there then use the row at index 0.  This will simulate
            // a row being present at yIndex.
            else if (mVisibleSprites.Count != 0 && mVisibleSprites[0].Count != 0)
            {
                xIndex = (int)(System.Math.Floor(((x - (mVisibleSprites[0][0].X - mGridSpacingX / 2)) / mGridSpacingX)));

            }
        }


        public Sprite GetSpriteAt(double x, double y, double z)
        {
            return GetSpriteAt(x, y, z, FlatRedBall.Utilities.SpriteSelectionOptions.Default);
        }


        public Sprite GetSpriteAtIndex(int xIndex, int yIndex)
        {
            if (yIndex > -1 && yIndex < mVisibleSprites.Count)
            {
                if (xIndex < mVisibleSprites[yIndex].Count && xIndex > -1)
                    return mVisibleSprites[yIndex][xIndex];
                else
                    return null;
            }
            else
            {
                return null;
            }
        }


        public Texture2D GetTextureAt(double x, double y, double z)
        {
            return mTextureGrid.GetTextureAt(x, y);
        }


        public FloatRectangle? GetFloatRectangleAt(double x, double y, double z)
        {
            // Vic Asks:  Shouldn't this consider Z?
            return mTextureCoordinateGrid.GetTextureAt(x, y);
        }

        public void GetTilePositionAt(double x, double y, double z, out Vector3 outputVector)
        {
            outputVector = new Vector3();
            outputVector.X = mBlueprint.X + (int)(System.Math.Round((x - mBlueprint.X) / mGridSpacingX)) * mGridSpacingX;

            if (mPlane == Plane.XY)
            {
                outputVector.Y = mBlueprint.Y + (int)(System.Math.Round((y - mBlueprint.Y) / mGridSpacingY)) * mGridSpacingY;
                outputVector.Z = mBlueprint.Z;
            }
            else
            {
                outputVector.Z = mBlueprint.Z + (int)(System.Math.Round((z - mBlueprint.Z) / mGridSpacingY)) * mGridSpacingY;
                outputVector.Y = mBlueprint.Y;
            }
        }


        public List<Texture2D> GetUsedTextures()
        {
            List<Texture2D> arrayToReturn = mTextureGrid.GetUsedTextures();

            if (mBlueprint.Texture != null)
            {
                arrayToReturn.Add(mBlueprint.Texture);
            }

            return arrayToReturn;
        }


        public List<AnimationChain> GetUsedAnimationChains()
        {
            List<AnimationChain> listToReturn = mAnimationTextureGrid.GetUsedTextures();

            if (mBlueprint.CurrentChain != null && !listToReturn.Contains(mBlueprint.CurrentChain))
            {
                listToReturn.Add(mBlueprint.CurrentChain);
            }

            return listToReturn;
        }

        

        public void InitializeTextureGrid()
        {

            mTextureGrid.Initialize(mBlueprint.Texture, (float)mBlueprint.X, (float)mBlueprint.Y,
                mGridSpacingX, mGridSpacingY);
            mTextureCoordinateGrid.Initialize(FloatRectangle.Default, mBlueprint.X, mBlueprint.Y,
                mGridSpacingX, mGridSpacingY);
            mAnimationTextureGrid.Initialize(null, mBlueprint.X, mBlueprint.Y,
                mGridSpacingX, mGridSpacingY);

        }


        public void InvertZ()
        {
            //float oldFar = sg.ZFarBound;
            //float oldClose = sg.ZCloseBound;
            ZCloseBound = -ZCloseBound;
            ZFarBound = -ZFarBound;

            mBlueprint.Z = -mBlueprint.Z;

            if (mPlane == Plane.XZ) // need to handle YZ when such a grid is implemented
            {

                //this.mTextureGrid.InvertZ();
                //this.mTextureCoordinateGrid.InvertZ();
                //this.mAnimationTextureGrid.InvertZ();

            }
        }


        public bool IsTextureReferenced(Texture2D texture)
        {
            return mTextureGrid.IsTextureReferenced(texture);
        }


        public SpriteList Manage()
        {
            mSpritesAddedDuringManageCall.Clear();

            bool isTooClose;

#if FRB_MDX
            isTooClose = this.mCamera.Z - this.mBlueprint.Z > -2;
#else
            isTooClose = this.mCamera.Z - this.mBlueprint.Z < 2;
#endif


            if (this.GridPlane == Plane.XY &&
                isTooClose)
            {
                return mSpritesAddedDuringManageCall;
            }




            if (mVisibleSprites.Count == 0)
                PopulateGrid();
            else
            {

                #region if we have an XY plane, expand and contract on the XY axes
                if (mPlane == Plane.XY)
                {
                    /* When calling expandY methods, if there is an expansion, the
                     * SpriteGrid will just look at the row that it is expanding from
                     * and add that many Sprites to the new row.  This is because the
                     * visible Sprites in an XY Grid are assumed to be rectangular.
                     * 
                     * For XZ grids, the Z bounds have to be expanded first, then the X.
                     * 
                     */

                    expandXPos();
                    expandXNeg();

                    contractXPos();
                    contractXNeg();

                    expandYPos();
                    expandYNeg();

                    contractYPos();
                    contractYNeg();
                }
                #endregion

                #region else if we have a YZ plane, expand and contract on the YZ axes

                else if (mPlane == Plane.YZ)
                {
                    expandYPos();
                    expandYNeg();

                    contractYPos();
                    contractYNeg();

                    ExpandZPositiveYZPlane();
                    ExpandZNegativeYZPlane();

                    contractZPos();
                    contractZNeg(); 
                
                }

                #endregion

                #region else, we have a XZ plane, so expand and contract on the XZ axes
                else
                {
                    expandZPos();
                    expandZNeg();

                    contractZNeg();
                    contractZPos();

                    expandXPos();
                    expandXNeg();

                    contractXPos();
                    contractXNeg();
                }
                #endregion
            }

            for (int i = 0; i < mSpritesAddedDuringManageCall.Count; i++)
            {
                Sprite sprite = mSpritesAddedDuringManageCall[i];
                UpdateTextureOnSpriteXY(sprite);
            }
            return mSpritesAddedDuringManageCall;
        }


        public SpriteList ManageExpandOnly()
        {
            mSpritesAddedDuringManageCall.Clear();
            if (mCamera.Z < -2.5f * Math.MathFunctions.ForwardVector3.Z) return mSpritesAddedDuringManageCall;

            #region if we have an XY plane, expand on the XY axes
            if (mPlane == Plane.XY)
            {
                /* When calling expandY methods, if there is an expansion, the
                 * SpriteGrid will just look at the row that it is expanding from
                 * and add that many Sprites to the new row.  This is because the
                 * visible Sprites in an XY Grid are assumed to be rectangular.
                 * 
                 * For XZ grids, the Z bounds have to be expanded first, then the X.
                 * 
                 */
                expandXPos();
                expandXNeg();
                expandYPos();
                expandYNeg();
            }
            #endregion
            #region else, we have a XZ plane, so expand on the XZ axes
            else
            {
                expandZPos();
                expandZNeg();
                expandXPos();
                expandXNeg();
            }
            #endregion

            for (int i = 0; i < mSpritesAddedDuringManageCall.Count; i++)
            {
                Sprite sprite = mSpritesAddedDuringManageCall[i];
                UpdateTextureOnSpriteXY(sprite);
            }

            return mSpritesAddedDuringManageCall;
        }


        public void ManualAnimationUpdate()
        {
            if (mCreatesAutomaticallyUpdatedSprites)
            {
                throw new InvalidOperationException("No need to perform an animation update on this SpriteGrid because the Sprites it creates are automatically updated so their Animation logic is called by the SpriteManager.");
            }

            int column = 0;



            for (int i = 0; i < mVisibleSprites.Count; i++)
            {
                int count = mVisibleSprites[i].Count;

                for (column = 0; column < count; column++)
                {
                    Sprite sprite = mVisibleSprites[i][column];

                    sprite.AnimateSelf(TimeManager.CurrentTime);
                }
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="textureToPaint"></param>
        /// <returns>The old FrbTexture at the argument position.</returns>
        public virtual Texture2D PaintSprite(double x, double y, double z, Texture2D textureToPaint)
        {
            x = mBlueprint.X + (int)(System.Math.Round((x - mBlueprint.X) / mGridSpacingX)) * mGridSpacingX;
            if (mPlane == Plane.XY)
            {
                y = mBlueprint.Y + (int)(System.Math.Round((y - mBlueprint.Y) / mGridSpacingY)) * mGridSpacingY;
            }
            else
            {
                y = mBlueprint.Z + (int)(System.Math.Round((z - mBlueprint.Z) / mGridSpacingY)) * mGridSpacingY;
            }

            if (mTextureGrid.GetTextureAt(x, y) == textureToPaint)
                return textureToPaint;
            else
            {
                Texture2D textureToReturn = mTextureGrid.GetTextureAt(x, y);
                mTextureGrid.PaintGridAtPosition((float)x, (float)y, textureToPaint);

                Sprite s = GetSpriteAt(x, y, z);
                if (s != null)
                    s.Texture = textureToPaint;
                return textureToReturn;
            }
        }


        public AnimationChain PaintSpriteAnimationChain(double x, double y, double z, AnimationChain newAnimationChain)
        {

            #region Convert the XYZ to XY on the grid plane.  Y becomes Z and values are relative to the blueprint position
            x = mBlueprint.X + (int)(System.Math.Round((x - mBlueprint.X) / GridSpacing)) * GridSpacing;
            if (mPlane == Plane.XY)
            {
                y = mBlueprint.Y + (int)(System.Math.Round((y - mBlueprint.Y) / GridSpacing)) * GridSpacing;
            }
            else
            {
                y = mBlueprint.Z + (int)(System.Math.Round((z - mBlueprint.Z) / GridSpacing)) * GridSpacing;
            }
            #endregion

            AnimationChain animationChain = mAnimationTextureGrid.GetTextureAt(x, y);
            if (animationChain != null && animationChain.Equals(newAnimationChain))
                return newAnimationChain;
            else
            {
                AnimationChain oldAnimationChain = mAnimationTextureGrid.GetTextureAt(x, y);
                mAnimationTextureGrid.PaintGridAtPosition((float)x, (float)y, newAnimationChain);

                Sprite sprite = GetSpriteAt(x, y, z);

                if (sprite != null)
                {
                    sprite.SetAnimationChain( newAnimationChain );
                    sprite.Animate = true;

                    if (mCreatesAutomaticallyUpdatedSprites == false)
                    {
                        SpriteManager.ManualUpdate(sprite);
                    }
                }
                return oldAnimationChain;
            }
        }


        public FloatRectangle PaintSpriteDisplayRegion(double x, double y, double z, ref FloatRectangle displayRegion)
        {

            #region Convert the XYZ to XY on the grid plane.  Y becomes Z and values are relative to the blueprint position
            x = mBlueprint.X + (int)(System.Math.Round((x - mBlueprint.X) / GridSpacing)) * GridSpacing;
            if (mPlane == Plane.XY)
            {
                y = mBlueprint.Y + (int)(System.Math.Round((y - mBlueprint.Y) / GridSpacing)) * GridSpacing;
            }
            else
            {
                y = mBlueprint.Z + (int)(System.Math.Round((z - mBlueprint.Z) / GridSpacing)) * GridSpacing;
            }
            #endregion

            if (mTextureCoordinateGrid.GetTextureAt(x, y).Equals(displayRegion))
                return displayRegion;
            else
            {
                FloatRectangle oldDisplay = mTextureCoordinateGrid.GetTextureAt(x, y).Value;
                mTextureCoordinateGrid.PaintGridAtPosition((float)x, (float)y, displayRegion);

                Sprite sprite = GetSpriteAt(x, y, z);

                if (sprite != null)
                {
                    sprite.TopTextureCoordinate = displayRegion.Top;
                    sprite.BottomTextureCoordinate = displayRegion.Bottom;
                    sprite.LeftTextureCoordinate = displayRegion.Left;
                    sprite.RightTextureCoordinate = displayRegion.Right;

                    SpriteManager.ManualUpdate(sprite);
                }
                return oldDisplay;
            }
        }



        public void PaintGrid(List<TextureLocation<Texture2D>> tla, Random random)
        {
            for (int i = 0; i < tla.Count; i++)
            {
                TextureLocation<Texture2D> tl = tla[i];
                mTextureGrid.PaintGridAtPosition(tl.X, tl.Y, tl.Texture);
            }
            RefreshPaint();
        }


        public SpriteList PopulateGrid()
        {
            return PopulateGrid(this.mCamera.Position.X, this.mCamera.Position.Y, mBlueprint.Position.Z);
        }

        // assumes mBlueprint and grid spacing have been set
        public SpriteList PopulateGrid(float x, float y, float z)
        {
            #region The user might have changed the Blueprints Texture.  If so, change the TextureGrid's base texture

            mTextureGrid.BaseTexture = mBlueprint.Texture;

            #endregion

            #region if we have the grid already full, we need to empty it
            for (int i = mVisibleSprites.Count - 1; i > -1; i--)
            {
                SpriteManager.RemoveSpriteList(mVisibleSprites[i]);
            }
            this.mVisibleSprites.Clear();

            #endregion

            #region position the sprite inside of the bounds

            #region Adjust X

            if (mPlane == Plane.YZ)
            {
                x = mBlueprint.X;
            }
            else
            {
                // multiply the scale by 1.1, just so our position is "bumped" in to our bounds so the formula
                // doesn't push us back out
                if (x + this.mGridSpacingX / 2.0f > mXRightBound) x = mXRightBound - this.mGridSpacingX / 2.0f;
                else if (x - this.mGridSpacingX / 2.0f < mXLeftBound) x = mXLeftBound + this.mGridSpacingX / 2.0f;

                x = mBlueprint.X + (int)(System.Math.Round((x - mBlueprint.X) / this.mGridSpacingX)) * mGridSpacingX;

                if (x - mBlueprint.ScaleX < mXLeftBound)
                    x += mGridSpacingX;
                if (x + mBlueprint.ScaleX > mXRightBound)
                    x -= mGridSpacingX;
            }

            #endregion

            #region Adjust Y

            if (mPlane == Plane.XZ)
            {
                y = mBlueprint.Y;
            }
            else
            {
                if (y + this.mGridSpacingY / 2.0f > mYTopBound) y = mYTopBound - this.mGridSpacingY / 2.0f;
                else if (y - this.mGridSpacingY / 2.0f < mYBottomBound) y = mYBottomBound + this.mGridSpacingY / 2.0f;

                y = mBlueprint.Y + (int)(System.Math.Round((y - mBlueprint.Y) / this.mGridSpacingY)) * mGridSpacingY;

                if (y - mBlueprint.ScaleY < mYBottomBound)
                    y += mGridSpacingY;
                if (y + mBlueprint.ScaleY > mYTopBound)
                    y -= mGridSpacingY;
            }

            #endregion

            #region Adjust the Z


            if (mPlane == Plane.XY)
            {
                z = mBlueprint.Z;
            }
            else
            {
#if FRB_MDX
                if (z - mBlueprint.ScaleY < mZFarBound) z = mZFarBound + mBlueprint.ScaleY * 1.1f;
                else if (z + mBlueprint.ScaleY > mZCloseBound) z = mZCloseBound - mBlueprint.ScaleY * 1.1f;
#else
                if (z + mBlueprint.ScaleY > mZFarBound) z = mZFarBound - mBlueprint.ScaleY * 1.1f;
                else if (z - mBlueprint.ScaleY < mZCloseBound) z = mZCloseBound + mBlueprint.ScaleY * 1.1f;

#endif

                z = mBlueprint.Z + (int)(System.Math.Round((z - mBlueprint.Z) / this.mGridSpacingY)) * mGridSpacingY;
            }

            #endregion


            #endregion

            #region if the position is outside of the screen, bring it in

            if (mPlane == Plane.XY)
            {

                while (mCamera.IsYInView(y, z) == false && y + mBlueprint.ScaleY + mGridSpacingY < mYTopBound)
                    y += mGridSpacingY;

            }
            else
            {
                float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

                // This doesn't seem right
                while (mCamera.IsYInView(y, z - maxScl) == false && z + mBlueprint.ScaleY + mGridSpacingY < this.mZFarBound)
                    z += mGridSpacingY;

                // the Z value could be too far in the positive Z, so if so, move it forward

#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
                while (z - mBlueprint.ScaleY < this.mZFarBound)
                    z += mGridSpacingY;

#elif FRB_MDX
                while (z + mBlueprint.ScaleY > this.mZFarBound)
                    z -= mGridSpacingY;
#endif
            }


            #endregion

            #region create the sprite and position it
            Sprite tempSprite = CreateSpriteFromBlueprint(x, y, z);
            UpdateTextureOnSpriteXY(tempSprite);
            #endregion

            #region create the first row and put a sprite in there
            mVisibleSprites.Add(new SpriteList());
#if DEBUG
            mVisibleSprites[0].Name = mContainedSpriteListName;
#endif
            mVisibleSprites[0].Add(tempSprite);
            #endregion

            return Manage();

        }


        public void RefreshPaint()
        {
            for (int i = 0; i < mVisibleSprites.Count; i++)
            {
                SpriteList sa = mVisibleSprites[i];
                for (int k = 0; k < sa.Count; k++)
                {
                    Sprite sprite = sa[k];
                    UpdateTextureOnSpriteXY(sprite);
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Clears all Sprites referenced by the SpriteGrid.
        /// </summary>
        /// <remarks>
        /// This method will only remove all contained Sprites from the SpriteManager.  This method does
        /// not clear out the TextureGrid.
        /// 
        /// <para>If the Manage method is called after this method is called, 
        /// the SpriteGrid will throw an out of bounds exception.  To refill the 
        /// SpriteGrid after this method has been called, it must first be populated.</para>
        /// </remarks>
        #endregion
        public void RemoveSprites()
        {
            for (int i = 0; i < mVisibleSprites.Count; i++)
            {
                SpriteList sa = mVisibleSprites[i];
                SpriteManager.RemoveSpriteList(sa);
            }

            mVisibleSprites.Clear();

        }


        public void ReplaceTexture(Texture2D oldTexture, Texture2D newTexture)
        {
            if (mBlueprint.Texture == oldTexture)
                mBlueprint.Texture = newTexture;

            for (int i = 0; i < mVisibleSprites.Count; i++)
            {
                SpriteList sa = mVisibleSprites[i];
                for (int k = 0; k < sa.Count; k++)
                {
                    Sprite s = sa[k];
                    if (s.Texture == oldTexture)
                        s.Texture = newTexture;
                }
            }

            mTextureGrid.ReplaceTexture(oldTexture, newTexture);
        }


        public void ResetTextures()
        {
            mTextureGrid.Clear();
            mTextureCoordinateGrid.Clear();
            mAnimationTextureGrid.Clear();
            this.RefreshPaint();
        }


        public void SetBaseTexture(Texture2D textureToSet)
        {
            mTextureGrid.mBaseTexture = textureToSet;
        }

        #region XML Docs
        /// <summary>
        /// Moves the grid by the passed variables 
        /// </summary>
        /// <remarks>
        /// This method does not change the bounds of the SpriteGrid; only the actual Sprites in the grid.
        /// This method is used to change the seed position of the SpriteGrid.  The location of the
        /// painted Sprites also shifts according to the arguments.
        /// </remarks>
        #endregion
        public void Shift(float xShift, float yShift, float zShift)
        {
            mBlueprint.X += xShift;
            mBlueprint.Y += yShift;
            mBlueprint.Z += zShift;

            mLastBlueprintPosition.X += xShift;
            mLastBlueprintPosition.Y += yShift;
            mLastBlueprintPosition.Z += zShift;

            for (int i = 0; i < mVisibleSprites.Count; i++)
            {
                for (int j = 0; j < mVisibleSprites[i].Count; j++)
                {
                    mVisibleSprites[i][j].X += xShift;
                    mVisibleSprites[i][j].Y += yShift;
                    mVisibleSprites[i][j].Z += zShift;
                }
            }

            if (mPlane == Plane.XY)
            {
                mTextureGrid.ChangeGrid(xShift, yShift);
                mTextureCoordinateGrid.ChangeGrid(xShift, yShift);
                mAnimationTextureGrid.ChangeGrid(xShift, yShift);
            }
            else
            {
                mTextureGrid.ChangeGrid(xShift, zShift);
                mTextureCoordinateGrid.ChangeGrid(xShift, zShift);
                mAnimationTextureGrid.ChangeGrid(xShift, yShift);
            }
        }


        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append("mXLeftBound:").Append(mXLeftBound);
            sb.Append("\nxRightBound:").Append(mXRightBound);
            if (mPlane == Plane.XY)
            {
                sb.Append("\nmYTopBound:").Append(mYTopBound);
                sb.Append("\nmYBottomBound:").Append(mYBottomBound);
            }
            else
            {
                sb.Append("\nzCloseBound:").Append(mZCloseBound);
                sb.Append("\nzFarBound:").Append(mZFarBound);
            }

            sb.Append("\nBlueprint Position: (").Append(mBlueprint.X).Append(", ").Append(mBlueprint.Y).Append(", ");
            sb.Append(mBlueprint.Z).Append(")\n");

            sb.Append(mTextureGrid.ToString());

            return sb.ToString();
        }


        public void TrimGrid()
        {
            mTextureGrid.TrimGrid();

            if (this.GridPlane == Plane.XY)
            {
                while(mTextureCoordinateGrid.FirstPaintedX.Count > 0 && 
                    mTextureCoordinateGrid.FirstPaintedY < this.YBottomBound - 1.5f * this.GridSpacingY)
                {
                    mTextureCoordinateGrid.ChopOffBottom();
                }
                while(mTextureCoordinateGrid.FirstPaintedX.Count > 0 && 
                    mTextureCoordinateGrid.LastPaintedY > this.YTopBound + 1.5f * this.GridSpacingY)
                {
                    mTextureCoordinateGrid.ChopOffTop();
                }
            }
            else
            {


            }

            float minX = this.XLeftBound - 1.5f * this.GridSpacingX;
            float maxX = this.XRightBound + 1.5f * this.GridSpacingX;

            mTextureCoordinateGrid.TrimGrid(minX, maxX);
            mAnimationTextureGrid.TrimGrid(minX, maxX);
        }


        public void UpdateRotationAndScale()
        {
            for (int i = 0; i < mVisibleSprites.Count; i++)
            {
                for (int j = 0; j < mVisibleSprites[i].Count; j++)
                {
                    mVisibleSprites[i][j].ScaleX = mBlueprint.ScaleX;
                    mVisibleSprites[i][j].ScaleY = mBlueprint.ScaleY;

                    mVisibleSprites[i][j].RotationX = mBlueprint.RotationX;
                    mVisibleSprites[i][j].RotationY = mBlueprint.RotationY;
                    mVisibleSprites[i][j].RotationZ = mBlueprint.RotationZ;
                }
            }
        }


        #endregion

        #region Internal Methods

        internal static float GetRightFilledBound(float xRightBound, float gridSpacing, float spriteScale, float seedX)
        {
            return (int)(((xRightBound - (seedX % gridSpacing)) - spriteScale) / gridSpacing) *
                gridSpacing + (seedX % gridSpacing) + spriteScale;
        }

        internal static float GetLeftFilledBound(float xLeftBound, float gridSpacing, float spriteScale, float seedX)
        {
            return (int)(((xLeftBound - (seedX % gridSpacing)) + spriteScale) / gridSpacing) *
                    gridSpacing + (seedX % gridSpacing) - spriteScale;
        }

        internal static float GetTopFilledBound(float yTopBound, float gridSpacing, float spriteScale, float seedY)
        {
            return (int)(((yTopBound - (seedY % gridSpacing)) - spriteScale) / gridSpacing) *
                gridSpacing + (seedY % gridSpacing) + spriteScale;
        }

        internal static float GetBottomFilledBound(float yBottomBound, float gridSpacing, float spriteScale, float seedY)
        {
            return (int)(((yBottomBound - (seedY % gridSpacing)) + spriteScale) / gridSpacing) *
                gridSpacing + (seedY % gridSpacing) - spriteScale;
        }

        #endregion

        #region Private Methods

        #region contract grid methods
        private void contractXPos()
        {
            Sprite currentSprite;

            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

            for (int y = 0; y < mVisibleSprites.Count; y++)
            {
                // we don't want to clear entire rows.  The Y or z should do that
                if (mVisibleSprites[y].Count < 2) continue;

                currentSprite = mVisibleSprites[y][mVisibleSprites[y].Count - 1];


                if (((mCamera.IsXInView(currentSprite.X - 2 * currentSprite.ScaleX, currentSprite.Z + Math.MathFunctions.ForwardVector3.Z * maxScl) == false && currentSprite.X - 2 * currentSprite.ScaleX > mCamera.X) ||
                    currentSprite.X + currentSprite.ScaleX > this.mXRightBound))
                {
                    SpriteManager.RemoveSprite(currentSprite);
                    y--;
                }
            }
        }

        private void contractXNeg()
        {
            Sprite currentSprite;

            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

            for (int y = 0; y < mVisibleSprites.Count; y++)
            {
                if (mVisibleSprites[y].Count < 2) continue;

                currentSprite = mVisibleSprites[y][0];

                if (((mCamera.IsXInView(currentSprite.X + 2 * currentSprite.ScaleX, currentSprite.Z + Math.MathFunctions.ForwardVector3.Z * maxScl) == false && currentSprite.X + 2 * currentSprite.ScaleX < mCamera.X) ||
                    currentSprite.X - currentSprite.ScaleX < this.mXLeftBound))
                {
                    SpriteManager.RemoveSprite(currentSprite);
                    y--;
                }
            }
        }

        private void contractYPos()
        {
            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

            while (mVisibleSprites.Count != 1 &&
                (
                    (mCamera.IsYInView(mVisibleSprites[mVisibleSprites.Count - 1][0].Y - mBlueprint.ScaleY,
                                     mVisibleSprites[mVisibleSprites.Count - 1][0].Z + Math.MathFunctions.ForwardVector3.Z * maxScl) == false && mVisibleSprites[mVisibleSprites.Count - 1][0].Y - mBlueprint.ScaleY > mCamera.Y) ||
                mVisibleSprites[mVisibleSprites.Count - 1][0].Y + mVisibleSprites[mVisibleSprites.Count - 1][0].ScaleY > this.mYTopBound))
            {
                SpriteManager.RemoveSpriteList(mVisibleSprites[mVisibleSprites.Count - 1]);
                mVisibleSprites.RemoveAt(mVisibleSprites.Count - 1);
            }
        }

        private void contractYNeg()
        {
            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

            while (mVisibleSprites.Count != 1 && // not the last row
                ((mCamera.IsYInView(mVisibleSprites[0][0].Y + mBlueprint.ScaleY, mVisibleSprites[0][0].Z + Math.MathFunctions.ForwardVector3.Z * maxScl) == false &&
                  mVisibleSprites[0][0].Y < mCamera.Y) ||
                 mVisibleSprites[0][0].Y - mBlueprint.ScaleY < this.mYBottomBound))
            {
                SpriteManager.RemoveSpriteList(mVisibleSprites[0]);
                mVisibleSprites.RemoveAt(0);
            }

        }

        private void contractZPos()
        {
            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

#if FRB_MDX
            float boundToUse = mZFarBound;
#else
            float boundToUse = mZCloseBound;
#endif
            while (mVisibleSprites.Count != 1 &&
                (mCamera.IsYInView(mVisibleSprites[mVisibleSprites.Count - 1][0].Y, mVisibleSprites[mVisibleSprites.Count - 1][0].Z + Math.MathFunctions.ForwardVector3.Z * maxScl) == false ||
                mVisibleSprites[mVisibleSprites.Count - 1][0].Z + mBlueprint.ScaleY > boundToUse))
            {
                SpriteManager.RemoveSpriteList(mVisibleSprites[mVisibleSprites.Count - 1]);
                mVisibleSprites.RemoveAt(mVisibleSprites.Count - 1);
            }
        }

        private void contractZNeg()
        {
            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

#if FRB_MDX
            float boundToUse = mZCloseBound;
#else
            float boundToUse = mZFarBound;
#endif
            while (mVisibleSprites.Count != 1 &&
                (mCamera.IsYInView(mVisibleSprites[0][0].Y,
                mVisibleSprites[0][0].Z + Math.MathFunctions.ForwardVector3.Z * maxScl) == false ||
                mVisibleSprites[0][0].Z - mBlueprint.ScaleY < boundToUse))
            {
                SpriteManager.RemoveSpriteList(mVisibleSprites[0]);
                mVisibleSprites.RemoveAt(0);
            }

        }

        #endregion


        private Sprite CreateSpriteFromBlueprint(float xPosition, float yPosition, float zPosition)
        {
            Sprite tempSprite = CreateSpriteBasedOnProperties();

            tempSprite.Position.X = xPosition;
            tempSprite.Position.Y = yPosition;
            tempSprite.Position.Z = zPosition;

            
            tempSprite.BlendOperation = mBlueprint.BlendOperation;
            tempSprite.ColorOperation = mBlueprint.ColorOperation;

            tempSprite.Alpha = mBlueprint.Alpha;

            tempSprite.RotationX = mBlueprint.RotationX;
            tempSprite.RotationY = mBlueprint.RotationY;
            tempSprite.RotationZ = mBlueprint.RotationZ;

            tempSprite.TopTextureCoordinate = mBlueprint.TopTextureCoordinate;
            tempSprite.BottomTextureCoordinate = mBlueprint.BottomTextureCoordinate;
            tempSprite.LeftTextureCoordinate = mBlueprint.LeftTextureCoordinate;
            tempSprite.RightTextureCoordinate = mBlueprint.RightTextureCoordinate;

            tempSprite.ScaleX = mBlueprint.ScaleX;
            tempSprite.ScaleY = mBlueprint.ScaleY;
            tempSprite.PixelSize = mBlueprint.PixelSize;

            tempSprite.Blue = mBlueprint.Blue;
            tempSprite.Green = mBlueprint.Green;
            tempSprite.Red = mBlueprint.Red;

            tempSprite.FlipHorizontal = mBlueprint.FlipHorizontal;
            tempSprite.FlipVertical = mBlueprint.FlipVertical;

            tempSprite.Visible = mBlueprint.Visible;

            if (mPlane == Plane.XY)
            {
                tempSprite.Z = mBlueprint.Z;
            }
            else if (mPlane == Plane.YZ)
            {
                tempSprite.X = mBlueprint.X;// is this necessary?
            }
            else
            {
                tempSprite.Y = mBlueprint.Y;
            }

            if (mCreatesAutomaticallyUpdatedSprites == false)
                SpriteManager.ManualUpdate(tempSprite);

            return tempSprite;
        }

        #region XML Docs
        /// <summary>
        /// This is called by CreateSpriteFromBlueprint - and should only be called from there.
        /// </summary>
        /// <returns>The newly created Sprite which was created obeying the SpriteGrid's properties.</returns>
        #endregion
        private Sprite CreateSpriteBasedOnProperties()
        {
            Sprite tempSprite = null;

            if (mAddSpritesToSpriteManager == false)
            {
                tempSprite = new Sprite();
                tempSprite.Texture = mBlueprint.Texture;

            }
            else
            {


                if (mCreatesAutomaticallyUpdatedSprites)
                {
                    if (CreatesParticleSprites)
                    {
                        if (mOrderingMode == OrderingMode.ZBuffered)
                        {
                            tempSprite = SpriteManager.AddZBufferedParticleSprite(mBlueprint.Texture);
                            //throw new NotImplementedException("Cannot create particle z-buffered Sprite");
                        }
                        else if (mOrderingMode == OrderingMode.DistanceFromCamera)
                        {
                            tempSprite = SpriteManager.AddParticleSprite(mBlueprint.Texture);
                        }
                    }
                    else
                    {
                        if (mOrderingMode == OrderingMode.ZBuffered)
                        {
                            tempSprite = SpriteManager.AddZBufferedSprite(mBlueprint.Texture);
                        }
                        else if (mOrderingMode == OrderingMode.DistanceFromCamera)
                        {
                            tempSprite = SpriteManager.AddSprite(mBlueprint.Texture);
                        }
                    }
                }
                else // manual sprite
                {
                    if (CreatesParticleSprites)
                    {
                        if (mOrderingMode == OrderingMode.ZBuffered)
                        {
                            tempSprite = SpriteManager.AddManualZBufferedParticleSprite(mBlueprint.Texture);
                            //throw new NotImplementedException("Cannot create particle z-buffered Sprite");
                        }
                        else if (mOrderingMode == OrderingMode.DistanceFromCamera)
                        {
                             tempSprite = SpriteManager.AddManualParticleSprite(mBlueprint.Texture);
                        }
                    }
                    else
                    {
                        if (mOrderingMode == OrderingMode.ZBuffered)
                        {
                            tempSprite = SpriteManager.AddManualZBufferedSprite(mBlueprint.Texture);
                        }
                        else
                        {
                            tempSprite = SpriteManager.AddManualSprite(mBlueprint.Texture);
                        }
                    }
                }
            }

            if (mLayer != null && tempSprite != null)
            {
                SpriteManager.AddToLayer(tempSprite, mLayer);
            }

            return tempSprite;
        }


        #region Expand grid methods
        private void expandXPos()
        {
            double zToUse = 0;

            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);


            for (int y = 0; y < mVisibleSprites.Count; y++)
            {
                if (mVisibleSprites[y].Count == 0) return;

                Sprite currentSprite = mVisibleSprites[y][mVisibleSprites[y].Count - 1];

                double leftXEdge = currentSprite.X + mGridSpacingX - mBlueprint.ScaleX;

                zToUse = currentSprite.Z + maxScl * Math.MathFunctions.ForwardVector3.Z;

                if (mVisibleSprites[y][mVisibleSprites[y].Count - 1].X + mBlueprint.ScaleX + mGridSpacingX < this.mXRightBound &&
                    (mCamera.IsXInView(leftXEdge, zToUse) || leftXEdge < mCamera.X))
                {
                    mVisibleSprites[y].Add(CreateSpriteFromBlueprint(
                        currentSprite.X + mGridSpacingX,
                        currentSprite.Y,
                        currentSprite.Z));
                    mSpritesAddedDuringManageCall.AddOneWay(mVisibleSprites[y][mVisibleSprites[y].Count - 1]);
                    y--; // we just expanded the array, so repeat it
                }
            }
        }

        private void expandXNeg()
        {
            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

            double zToUse;
            for (int y = 0; y < mVisibleSprites.Count; y++)
            {

                if (mVisibleSprites[y].Count == 0) return;

                Sprite currentSprite = mVisibleSprites[y][0];
                zToUse = currentSprite.Z + maxScl * Math.MathFunctions.ForwardVector3.Z;

                double rightEdge = currentSprite.X - mGridSpacingX + mBlueprint.ScaleX;


                if (mVisibleSprites[y][0].Position.X - mBlueprint.ScaleX - mGridSpacingX > this.mXLeftBound &&
                    (mCamera.IsXInView(rightEdge, zToUse) || rightEdge > mCamera.X))
                {
                    mVisibleSprites[y].Insert(0, CreateSpriteFromBlueprint(
                        currentSprite.Position.X - mGridSpacingX,
                        currentSprite.Position.Y,
                        currentSprite.Position.Z));
                    mSpritesAddedDuringManageCall.AddOneWay(mVisibleSprites[y][0]);
                    y--; // we just expanded the array, so repeat it
                }
            }
        }

        private void expandYPos()
        {
            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

            while (mVisibleSprites[mVisibleSprites.Count - 1][0].Y + mBlueprint.ScaleY + mGridSpacingY < this.mYTopBound &&
                (mCamera.IsYInView(
                    mVisibleSprites[mVisibleSprites.Count - 1][0].Y + mGridSpacingY - mBlueprint.ScaleY,
                    mVisibleSprites[mVisibleSprites.Count - 1][0].Z + maxScl * Math.MathFunctions.ForwardVector3.Z) ||

                 mVisibleSprites[mVisibleSprites.Count - 1][0].Y + mBlueprint.ScaleY < mCamera.Y)
                )
            {

                SpriteList currentArray = mVisibleSprites[mVisibleSprites.Count - 1];

                SpriteList newArray = new SpriteList(currentArray.Count);
#if DEBUG
                newArray.Name = mContainedSpriteListName;
#endif
                mVisibleSprites.Add(newArray);
                // add a new row


                for (int i = 0; i < currentArray.Count; i++)
                {
                    newArray.Add(this.CreateSpriteFromBlueprint(
                        currentArray[i].X,
                        currentArray[0].Y + mGridSpacingY,
                        currentArray[i].Z));

                    mSpritesAddedDuringManageCall.AddOneWay(newArray[i]);
                }

            }

        }

        

        private void expandYNeg()
        {
            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

            while (mVisibleSprites[0][0].Y - mBlueprint.ScaleY - mGridSpacingY > mYBottomBound &&
                (mCamera.IsYInView(
                    mVisibleSprites[0][0].Y - mGridSpacingY + mBlueprint.ScaleY,
                    mVisibleSprites[0][0].Z + maxScl * Math.MathFunctions.ForwardVector3.Z) ||

                  mVisibleSprites[0][0].Y > mCamera.Y)
                 )
            {
                SpriteList currentArray = mVisibleSprites[0];

                SpriteList newArray = new SpriteList(currentArray.Count);
#if DEBUG
                newArray.Name = mContainedSpriteListName;
#endif
                mVisibleSprites.Insert(0, newArray);
                // add a new row
                for (int i = 0; i < currentArray.Count; i++)
                {
                    newArray.Add(this.CreateSpriteFromBlueprint(
                        currentArray[i].X,
                        currentArray[0].Y - mGridSpacingY,
                        currentArray[i].Z));
                    mSpritesAddedDuringManageCall.AddOneWay(newArray[i]);
                }
            }
        }

        private void expandZPos()
        {
            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

            float boundValueToUse = mZCloseBound;

            while (mVisibleSprites[mVisibleSprites.Count - 1][0].Z + mBlueprint.ScaleY + mGridSpacingY < boundValueToUse &&
                mCamera.IsYInView(mVisibleSprites[mVisibleSprites.Count - 1][0].Y, mVisibleSprites[mVisibleSprites.Count - 1][0].Z + maxScl * Math.MathFunctions.ForwardVector3.Z + mGridSpacingY))
            {
                SpriteList currentArray = mVisibleSprites[mVisibleSprites.Count - 1];

                SpriteList newArray = new SpriteList();
#if DEBUG
                newArray.Name = mContainedSpriteListName;
#endif
                mVisibleSprites.Add(newArray);
                // add a new row

                newArray.Add(this.CreateSpriteFromBlueprint(
                    currentArray[0].X,
                    currentArray[0].Y,
                    currentArray[0].Z + mGridSpacingY));
                mSpritesAddedDuringManageCall.AddOneWay(newArray[0]);
            }
        }

        private void expandZNeg()
        {
            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

            float boundValueToUse = mZFarBound;

            while (mVisibleSprites[0][0].Z - mBlueprint.ScaleY - mGridSpacingY > boundValueToUse &&
                mCamera.IsYInView(mVisibleSprites[0][0].Y, mVisibleSprites[0][0].Z + maxScl * Math.MathFunctions.ForwardVector3.Z))
            {
                SpriteList currentArray = mVisibleSprites[0];

                SpriteList newArray = new SpriteList();
#if DEBUG
                newArray.Name = mContainedSpriteListName;
#endif
                mVisibleSprites.Insert(0, newArray);
                // add a new row

                newArray.Add(this.CreateSpriteFromBlueprint(
                    currentArray[0].X,
                    currentArray[0].Y,
                    currentArray[0].Z - mGridSpacingY));
                mSpritesAddedDuringManageCall.AddOneWay(newArray[0]);

                /*  In the following diagram, X's are old Sprites and the * is a new Sprite.
                 * 
                 *    XXXXXXXX
                 *    *
                 * 
                 *    However, if the mCamera is to the right of the newly created Sprite, it
                 *    needs to be shifted over so that it is either in view so it can fill
                 *    the row as follows:
                 * 
                 *    XXXXXXXX
                 *    -->***
                 * 
                 *    or if the mCamera is to the right of the entire grid:
                 * 
                 *    XXXXXXXX
                 *    ------>*
                 */
                while (mCamera.IsXInView(newArray[0].X, newArray[0].Z) == false &&
                    newArray[0].X < mCamera.X &&
                    newArray[0].X + mGridSpacingX - maxScl < mXRightBound)
                    newArray[0].X += mGridSpacingX;


            }
        }

        private void ExpandZPositiveYZPlane()
        {
            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

            float boundValueToUse = mZCloseBound;

            for (int y = 0; y < mVisibleSprites.Count; y++)
            {
                Sprite currentSprite = mVisibleSprites[y][0];
                while (currentSprite.Z + mBlueprint.ScaleY + mGridSpacingY < boundValueToUse &&
                    mCamera.IsYInView(currentSprite.Y, currentSprite.Z + maxScl * Math.MathFunctions.ForwardVector3.Z))
                {
                    mVisibleSprites[y].Insert(0, CreateSpriteFromBlueprint(
                        currentSprite.X,
                        currentSprite.Y,
                        currentSprite.Z + mGridSpacingX));

                    currentSprite = mVisibleSprites[y][0];

                    mSpritesAddedDuringManageCall.AddOneWay(currentSprite);

                }

            }
        }

        private void ExpandZNegativeYZPlane()
        {
            float maxScl = System.Math.Max(mBlueprint.ScaleX, mBlueprint.ScaleY);

            float boundValueToUse = mZFarBound;

            for (int y = 0; y < mVisibleSprites.Count; y++)
            {
                Sprite currentSprite = mVisibleSprites[y][mVisibleSprites[y].Count - 1];
                while (currentSprite.Z + mBlueprint.ScaleY + mGridSpacingY < boundValueToUse &&
                    mCamera.IsYInView(currentSprite.Y, currentSprite.Z + maxScl * Math.MathFunctions.ForwardVector3.Z))
                {
                    mVisibleSprites[y].Add(CreateSpriteFromBlueprint(
                        currentSprite.X,
                        currentSprite.Y,
                        currentSprite.Z - mGridSpacingX));
                    currentSprite = mVisibleSprites[y][mVisibleSprites[y].Count - 1];
                    mSpritesAddedDuringManageCall.AddOneWay(currentSprite);

                }

            }
        }

        #endregion


        private Sprite GetSpriteAt(double x, double y, double z, FlatRedBall.Utilities.SpriteSelectionOptions selectionOptions)
        {
            int xIndex;
            int yIndex;

            GetIndexAt(x, y, z, out xIndex, out yIndex);

            return GetSpriteAtIndex(xIndex, yIndex);
        }


        private void UpdateTextureOnSpriteXY(Sprite sprite)
        {
            if (mBlueprint.Animate == true)
            {
                sprite.Animate = true;
                sprite.SetAnimationChain(mBlueprint.CurrentChain);

                sprite.CurrentFrameIndex = mRandom.Next(sprite.CurrentChain.Count);
                sprite.Texture = sprite.CurrentChain[sprite.CurrentFrameIndex].Texture;
                sprite.FlipVertical = sprite.CurrentChain[sprite.CurrentFrameIndex].FlipVertical;
                sprite.FlipHorizontal = sprite.CurrentChain[sprite.CurrentFrameIndex].FlipHorizontal;

            }

            double yToUse;

            if (mPlane == Plane.XY)
            {
                yToUse = sprite.Y;
            }
            else
            {
                yToUse = -sprite.Z;
            }

            AnimationChain currentChain = mAnimationTextureGrid.GetTextureAt(sprite.Position.X, yToUse);

            // If the Sprite is animated then use the AnimationChain to set texture coordinates
            // and textures.  
            if (currentChain != null)
            {
                if (sprite.CurrentChain != currentChain)
                {
                    sprite.SetAnimationChain(currentChain);
                    if (currentChain != null)
                        sprite.Animate = true;

                    if (!mCreatesAutomaticallyUpdatedSprites)
                    {
                        SpriteManager.ManualUpdate(sprite);
                    }
                }
            }
            else // otherwise, use the texture grid and texture coordinate grid for painting
            {

                sprite.Texture = mTextureGrid.GetTextureAt((float)sprite.X, (float)yToUse);

                FloatRectangle? floatRectangle = mTextureCoordinateGrid.GetTextureAt(sprite.X, yToUse);

                if (floatRectangle != null)
                {
                    var rectangle = floatRectangle.Value;
                    sprite.TopTextureCoordinate = rectangle.Top;
                    sprite.BottomTextureCoordinate = rectangle.Bottom;
                    sprite.LeftTextureCoordinate = rectangle.Left;
                    sprite.RightTextureCoordinate = rectangle.Right;

                    if (!mCreatesAutomaticallyUpdatedSprites)
                    {
                        SpriteManager.ManualUpdate(sprite);
                    }
                }
            }


        }


        #endregion

        #endregion


        #region IEquatable<SpriteGrid> Members

        bool IEquatable<SpriteGrid>.Equals(SpriteGrid other)
        {
            return this == other;
        }

        #endregion
    }
}