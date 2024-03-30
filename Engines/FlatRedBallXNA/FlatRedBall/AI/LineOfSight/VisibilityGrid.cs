using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall.Math;
using FlatRedBall.Graphics.Texture;
using FlatRedBall.Content;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Color = Microsoft.Xna.Framework.Color;

using Microsoft.Xna.Framework;

namespace FlatRedBall.AI.LineOfSight
{
    /// <summary>
    /// Represents a 2D grid of cells which identify what can be seen given a list of IViewers. 
    /// This supports line of sight.
    /// </summary>
    public class VisibilityGrid
    {
        #region Fields

        byte[][] mBlockedTiles;
        byte[][] mRevealedTiles;



        int mNumberOfXTiles;
        int mNumberOfYTiles;

        ImageData mImageData;
        Sprite mSprite;

        float mXSeed;
        float mYSeed;

        float mGridSpacing;

        bool mVisible;

        Dictionary<IViewer, ViewerInformation> mViewers = new Dictionary<IViewer, ViewerInformation>();
        List<Rectangle> mViewerUpdateAreas = new List<Rectangle>();

        float mVisibleDisplayZ;

        #region Fog Of War
        ImageData mFogImageData;
        ImageData mFogGradiantData;
        Texture2D mFogTexture;
        int mFogFactor;

        #endregion

        #endregion

        #region Properties

        public string Name
        {
            get;
            set;
        }

        public string ContentManagerName { get; set; }

        public Color HiddenBlockedColor
        {
            get;
            set;
        }

        public Color RevealedBlockedColor
        {
            get;
            set;
        }

        public Color HiddenClearedColor
        {
            get;
            set;
        }

        public Color RevealedClearedColor
        {
            get;
            set;
        }

        public bool Visible
        {
            get { return mVisible; }
            set
            {
                if (mVisible != value)
                {
                    mVisible = value;
                    UpdateDisplay();
                }

            }
        }

        public int NumberOfXTiles
        {
            get
            {
                return mNumberOfXTiles;
            }
        }

        public int NumberOfYTiles
        {
            get
            {
                return mNumberOfYTiles;
            }
        }

        public float Z
        {
            get
            {
                return mVisibleDisplayZ;
            }
            set
            {
                mVisibleDisplayZ = value;
                if (mSprite != null)
                {
                    mSprite.Z = mVisibleDisplayZ;
                }
            }
        }

        public ImageData VisibilityImage { get { return mImageData; } }

        #region Fog of War
        public Color FogColor
        {
            get;
            set;
        }
        public int FogResolution
        {
            get { return mFogFactor; }
            set
            {
                int newFogFactor = value;
                if (newFogFactor != mFogFactor)
                {
                    int fogWidth = NumberOfXTiles * newFogFactor;
                    int fogHeight = NumberOfYTiles * newFogFactor;
                    mFogImageData = new ImageData(fogWidth, fogHeight);
                    mFogGradiantData = new ImageData(newFogFactor, newFogFactor);
                    mFogImageData.Fill(FogColor);

                    if(string.IsNullOrEmpty(ContentManagerName) == false)
                    {
                        ContentManager contentManager = FlatRedBallServices.GetContentManagerByName(ContentManagerName);
                        string assetName = "FogOfWareTexture_" + newFogFactor.ToString();
                        mFogTexture = new Texture2D(FlatRedBallServices.GraphicsDevice, fogWidth, fogHeight);
                        mFogTexture.Name = assetName;
                        contentManager.AddDisposable(assetName, mFogTexture);
                    }
                }
                mFogFactor = newFogFactor;
            }
        }
        public byte FogShade { get; set; }
        public Texture2D FogTexture { get { return mFogTexture; } }
        #endregion

        #endregion

        #region Methods

        #region Constructor

        /// <summary>
        /// Instantiates a new VisibilityGrid.
        /// </summary>
        /// <param name="xSeed">The absolute x coordinate seed value.</param>
        /// <param name="ySeed">The absolute y coordinate seed value.</param>
        /// <param name="gridSpacing">The amount of distance in world coordinates between rows and columns.</param>
        /// <param name="numberOfXTiles">Number of tiles wide (on the X axis)</param>
        /// <param name="numberOfYTiles">Number of tiles heigh (on the Y axis)</param>
        public VisibilityGrid(float xSeed, float ySeed, float gridSpacing, int numberOfXTiles,
            int numberOfYTiles)
        {
            HiddenClearedColor = Color.DarkBlue;
            HiddenBlockedColor = Color.DarkRed;
            RevealedClearedColor = Color.LightBlue;
            RevealedBlockedColor = Color.Pink;


            mBlockedTiles = new byte[numberOfXTiles][];
            mRevealedTiles = new byte[numberOfXTiles][];



            mNumberOfXTiles = numberOfXTiles;
            mNumberOfYTiles = numberOfYTiles;

            mXSeed = xSeed;
            mYSeed = ySeed;
            mGridSpacing = gridSpacing;

            // Do an initial loop to create the arrays
            for (int x = 0; x < numberOfXTiles; x++)
            {
                mBlockedTiles[x] = new byte[numberOfYTiles];
                mRevealedTiles[x] = new byte[numberOfYTiles];
            }

        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Checks if any viewers have changed since last Update, and if so it updates the grid.
        /// </summary>
        /// <returns>Whether anything has changed.</returns>
        public bool Activity()
        {
            bool hasAnythingChanged = false;

            int xIndex;
            int yIndex;
            foreach (KeyValuePair<IViewer, ViewerInformation> kvp in this.mViewers)
            {

                IViewer viewer = kvp.Key;
                ViewerInformation information = kvp.Value;

                WorldToIndex(viewer.X, viewer.Y, out xIndex, out yIndex);

                int radiusAsInt = MathFunctions.RoundToInt(viewer.WorldViewRadius / mGridSpacing);
                if (xIndex != information.LastX || yIndex != information.LastY)
                {
                    hasAnythingChanged = true;
                    information.LastX = xIndex;
                    information.LastY = yIndex;

                    if (xIndex >= radiusAsInt && yIndex >= radiusAsInt &&
                        xIndex + radiusAsInt < mNumberOfXTiles && yIndex + radiusAsInt < mNumberOfYTiles)
                    {
                        UpdateViewersLocalVisibilityGrid(viewer, information);
                    }
                }
            }
            if (hasAnythingChanged)
            {
                UpdateRevealedFromViewers();
            }
            return hasAnythingChanged;
        }

        /// <summary>
        /// Destroys the VisibilityGrid - specifically disposing its internal fog texture.
        /// </summary>
		public void Destroy()
		{
			if ( mFogFactor > 0 ) 
			{
				mFogTexture.Dispose();
				mFogTexture = null;
			}
		}

        /// <summary>
        /// Adds an IViewer to this grid.
        /// </summary>
        /// <param name="viewerToAdd">The viewer to add.</param>
        public void AddViewer(IViewer viewerToAdd)
        {
            int localGridDimension = MathFunctions.RoundToInt(viewerToAdd.WorldViewRadius / mGridSpacing) * 2 + 1;

            VisibilityGrid localVisibilityGrid = new VisibilityGrid(0, 0, mGridSpacing,
                localGridDimension, localGridDimension);

            ViewerInformation viewerInformation = new ViewerInformation();
            viewerInformation.LocalVisibilityGrid = localVisibilityGrid;

            if (mFogFactor > 0)
            {
                localVisibilityGrid.FogColor = FogColor;
                localVisibilityGrid.FogShade = FogShade;
                localVisibilityGrid.FogResolution = FogResolution;
            }
            int xIndex;
            int yIndex;
            mViewers.Add(viewerToAdd, viewerInformation);

            WorldToIndex(viewerToAdd.X, viewerToAdd.Y, out xIndex, out yIndex);

            int radiusAsInt = MathFunctions.RoundToInt(viewerToAdd.WorldViewRadius / mGridSpacing);

            if (xIndex >= radiusAsInt && yIndex >= radiusAsInt &&
                xIndex + radiusAsInt < mNumberOfXTiles && yIndex + radiusAsInt < mNumberOfYTiles)
            {

                UpdateViewersLocalVisibilityGrid(viewerToAdd, viewerInformation);
            }

            UpdateRevealedFromViewers();
        }

        /// <summary>
        /// Makes walls visible if they are adjacent to visible non-walls.
        /// </summary>
        public void BleedDirectlyVisibleToWalls()
        {
            int y;
            // We're not going to go to the very edges to avoid if statements (for speed reasons)
            for (int x = 1; x < mNumberOfXTiles - 1; x++)
            {
                for (y = 1; y < mNumberOfYTiles - 1; y++)
                {


                    byte valueToSet =

                        (byte)((
                        mBlockedTiles[x][y] *
                        ((mRevealedTiles[x][y + 1] & 1) |
                        (mRevealedTiles[x + 1][y] & 1) |
                        (mRevealedTiles[x][y - 1] & 1) |
                        (mRevealedTiles[x - 1][y] & 1))) << 1);

                    //if (valueToSet != 0)
                    //{
                    //    //int m = 3;
                    //}

                    mRevealedTiles[x][y] = (byte)(mRevealedTiles[x][y] | valueToSet);
                }
            }
        }

        /// <summary>
        /// Adds a block (or wall) at a given world location.
        /// </summary>
        /// <param name="worldX">The world coordinate X.</param>
        /// <param name="worldY">The world coordinate Y.</param>
        public void BlockWorld(float worldX, float worldY)
        {
            int xIndex;
            int yIndex;

            WorldToIndex(worldX, worldY, out xIndex, out yIndex);

            mBlockedTiles[xIndex][yIndex] = 1;
        }

        /// <summary>
        /// Unblocks a tile that was previously marked as a world blocker
        /// </summary>
        /// <param name="X">The X coordinate of the tile</param>
        /// <param name="Y">The Y coordinate of the tile</param>
        public void UnBlockWorld( float X, float Y )
        {
            int xIndex, yIndex;
            WorldToIndex( X, Y, out xIndex, out yIndex );
            mBlockedTiles[xIndex][yIndex] = 0;
        }

        /// <summary>
        /// Clears all blocked tiles.
        /// </summary>
        public void ClearBlockedTiles()
        {
            int y;

            for (int x = 0; x < mNumberOfXTiles; x++)
            {
                for (y = 0; y < mNumberOfYTiles; y++)
                {
                    mBlockedTiles[x][y] = 0;
                }

            }
        }

        /// <summary>
        /// Returns whether a given world position is in view of a given viewer.
        /// </summary>
        /// <param name="viewer">The viewer to check visibility for.</param>
        /// <param name="targetPosition">The world coordinates.</param>
        /// <returns>Whether in view.</returns>
        public bool IsPositionInDirectView(IViewer viewer, ref Vector3 targetPosition)
        {
            if (mViewers.ContainsKey(viewer)) 
            {
                VisibilityGrid grid = mViewers[viewer].LocalVisibilityGrid;

                return grid.IsRevealedWorld(targetPosition.X, targetPosition.Y);
            }
#if DEBUG
            else 
            {
                throw new InvalidOperationException("Viewer does not exist in Visbility grid");
            }
#else
            return false;
#endif
        }

        /// <summary>
        /// Returns whether a given world coordinate is relealed.
        /// </summary>
        /// <param name="worldX">The world x coordinate.</param>
        /// <param name="worldY">The world y coordinate.</param>
        /// <returns>Whether the world coordinate is revealed or not.</returns>
        public bool IsRevealedWorld(float worldX, float worldY)
        {
            int xIndex;
            int yIndex;

            WorldToIndex(worldX, worldY, out xIndex, out yIndex);

            return mRevealedTiles[xIndex][yIndex] == 1;
        }

        /// <summary>
        /// Returns whether a given X and Y index is revealed.
        /// </summary>
        /// <param name="xIndex">The x index</param>
        /// <param name="yIndex">The y index</param>
        /// <returns>Whether the location specified by the x/y index is revealed.</returns>
        public bool IsRevealed(int xIndex, int yIndex)
        {
            return mRevealedTiles[xIndex][yIndex] == 1;
        }

        /// <summary>
        /// Hides the entire grid (makes it not revealed).
        /// </summary>
        public void MakeAllHidden()
        {
            Color shadedColor = 
				Color.FromNonPremultiplied(FogColor.R, FogColor.G, FogColor.B, FogShade);
            int y;
            for (int x = 0; x < mNumberOfXTiles; x++)
            {
                for (y = 0; y < mNumberOfYTiles; y++)
                {
                    if (mFogFactor > 0)
                    {
                        for ( int fogX = 0; fogX < mFogFactor; fogX++ )
                        {
                            for ( int fogY = 0; fogY < mFogFactor; fogY++ )
                            {
                                shadedColor = mFogImageData.GetPixelColor(x * mFogFactor + fogX, y * mFogFactor + fogY);
                                if ( shadedColor.R > FogShade )
                                    shadedColor.R = shadedColor.G = shadedColor.B = FogShade;
                                mFogImageData.SetPixel(x * mFogFactor + fogX, y * mFogFactor + fogY, shadedColor);
                            }
                        }
                        
                    }
                    mRevealedTiles[x][y] = 0;
                }
            }
        }

        /// <summary>
        /// Reveals the entire grid.
        /// </summary>
        public void MakeAllRevealed()
        {
            int y;
            for (int x = 0; x < mNumberOfXTiles; x++)
            {
                for (y = 0; y < mNumberOfYTiles; y++)
                {
                    mRevealedTiles[x][y] = 1;
                }
            }
        }

        /// <summary>
        /// Removes a viewer.
        /// </summary>
        /// <param name="viewerToRemove">The argument IViewer to remove.</param>
        public void RemoveViewer(IViewer viewerToRemove)
        {
            mViewers.Remove(viewerToRemove);

            // This have definitely changed, so we gotta refresh everything.
            // Fortunately there's no re-calculation here, just paste everything
            // down.  The newly-removed guy will not be placed.
            UpdateRevealedFromViewers();
        }

        /// <summary>
        /// Reveals a circle around the given world coordinate using a given radius
        /// </summary>
        /// <param name="worldX">The world coordinate X</param>
        /// <param name="worldY">The world coordinate Y</param>
        /// <param name="worldRadius">The radius in world units</param>
        public void RevealCircleWorld(float worldX, float worldY, float worldRadius)
        {
            int xIndex;
            int yIndex;

            WorldToIndex(worldX, worldY, out xIndex, out yIndex);

            int tileRadius = MathFunctions.RoundToInt(worldRadius / mGridSpacing);

            RevealCircle(xIndex, yIndex, tileRadius);

        }

        public void RevealCircle(int xIndex, int yIndex, int tileRadius)
        {
            int f = 1 - tileRadius;
            int ddF_x = 1;
            int ddF_y = -2 * tileRadius;
            int x = 0;
            int y = tileRadius;

            RevealLine(xIndex, yIndex, xIndex, yIndex + tileRadius);
            RevealLine(xIndex, yIndex, xIndex, yIndex - tileRadius);
            RevealLine(xIndex, yIndex, xIndex + tileRadius, yIndex);
            RevealLine(xIndex, yIndex, xIndex - tileRadius, yIndex);

            bool didYChange = false;

            while (x < y)
            {
                didYChange = false;

                // ddF_x == 2 * x + 1;
                // ddF_y == -2 * y;
                // f == x*x + y*y - radius*radius + 2*x - y + 1;
                if (f >= 0)
                {
                    y--;
                    ddF_y += 2;
                    f += ddF_y;

                    didYChange = true;
                }
                x++;
                ddF_x += 2;
                f += ddF_x;
                RevealLine(xIndex, yIndex, xIndex + x, yIndex + y);
                RevealLine(xIndex, yIndex, xIndex - x, yIndex + y);
                RevealLine(xIndex, yIndex, xIndex + x, yIndex - y);
                RevealLine(xIndex, yIndex, xIndex - x, yIndex - y);
                RevealLine(xIndex, yIndex, xIndex + y, yIndex + x);
                RevealLine(xIndex, yIndex, xIndex - y, yIndex + x);
                RevealLine(xIndex, yIndex, xIndex + y, yIndex - x);
                RevealLine(xIndex, yIndex, xIndex - y, yIndex - x);

                if (didYChange)
                {
                    x--;
                    RevealLine(xIndex, yIndex, xIndex + x, yIndex + y);
                    RevealLine(xIndex, yIndex, xIndex - x, yIndex + y);
                    RevealLine(xIndex, yIndex, xIndex + x, yIndex - y);
                    RevealLine(xIndex, yIndex, xIndex - x, yIndex - y);
                    RevealLine(xIndex, yIndex, xIndex + y, yIndex + x);
                    RevealLine(xIndex, yIndex, xIndex - y, yIndex + x);
                    RevealLine(xIndex, yIndex, xIndex + y, yIndex - x);
                    RevealLine(xIndex, yIndex, xIndex - y, yIndex - x);
                    x++;


                }



            }
        }


        public void RevealLineWorld(float worldX1, float worldY1, float worldX2, float worldY2)
        {
            int xIndex1;
            int yIndex1;

            WorldToIndex(worldX1, worldY1, out xIndex1, out yIndex1);

            int xIndex2;
            int yIndex2;

            WorldToIndex(worldX2, worldY2, out xIndex2, out yIndex2);

            RevealLine(xIndex1, yIndex1, xIndex2, yIndex2);

        }


        public void RevealLine(int x0, int y0, int x1, int y1)
        {

            int dy = y1 - y0;
            int dx = x1 - x0;
            int stepx, stepy;

            if (dy < 0) { dy = -dy; stepy = -1; } else { stepy = 1; }
            if (dx < 0) { dx = -dx; stepx = -1; } else { stepx = 1; }
            dy <<= 1;                                                  // dy is now 2*dy
            dx <<= 1;                                                  // dx is now 2*dx

            mRevealedTiles[x0][y0] = 1;

            if (mBlockedTiles[x0][y0] > 0)
            {
                return;
            }

            if (dx > dy)
            {
                int fraction = dy - (dx >> 1);                         // same as 2*dy - dx
                while (x0 != x1)
                {
                    if (fraction >= 0)
                    {
                        y0 += stepy;
                        fraction -= dx;                                // same as fraction -= 2*dx
                    }
                    x0 += stepx;
                    fraction += dy;                                    // same as fraction -= 2*dy

                    //if (x0 > -1 && y0 > -1 &&
                    //    x0 < mNumberOfXTiles && y0 < mNumberOfYTiles)
                    {
                        mRevealedTiles[x0][y0] = 1;

                        if (mBlockedTiles[x0][y0] > 0)
                        {
                            return;
                        }
                    }
                }
            }
            else
            {
                int fraction = dx - (dy >> 1);
                while (y0 != y1)
                {
                    if (fraction >= 0)
                    {
                        x0 += stepx;
                        fraction -= dy;
                    }
                    y0 += stepy;
                    fraction += dx;

                    //if (x0 > -1 && y0 > -1 &&
                    //    x0 < mNumberOfXTiles && y0 < mNumberOfYTiles)
                    {

                        mRevealedTiles[x0][y0] = 1;

                        if (mBlockedTiles[x0][y0] > 0)
                        {
                            return;
                        }
                    }
                }
            }
        }

        public void RevealAreaWorld(float worldX1, float worldY1, float worldX2, float worldY2)
        {
            int xIndex1;
            int yIndex1;

            WorldToIndex(worldX1, worldY1, out xIndex1, out yIndex1);

            int xIndex2;
            int yIndex2;

            WorldToIndex(worldX2, worldY2, out xIndex2, out yIndex2);

            RevealArea(xIndex1, yIndex1, xIndex2, yIndex2);
        }

        public void RevealArea(int x0, int y0, int x1, int y1)
        {
            int x, fogX, fogY;
            Color revealedColor = 
                Color.FromNonPremultiplied(127, 127, 127, 255);
            for (int y = y0; y < y1; y++)
            {
                for (x = x0; x < x1; x++)
                {
                    mRevealedTiles[x][y] = 1;
                    if ( mFogFactor != 0 )
                    {
                        for ( fogY = 0; fogY < mFogFactor; fogY++ )
                        {
                            for ( fogX = 0; fogX < mFogFactor; fogX++ )
                            {
                                mFogImageData.SetPixel(( x ) * mFogFactor + fogX, ( y ) * mFogFactor + fogY, revealedColor);
                            }
                        }
                    }
                }
            }

            if ( mFogFactor > 0 && mFogTexture != null )
            {
                mFogImageData.ToTexture2D(mFogTexture);
            }
        }

        static int NumberCreated = 0;
        public void UpdateDisplay()
        {

            #region Create the Sprites if needed

            if (mVisible && (mSprite == null || mImageData == null))
            {
                mSprite = SpriteManager.AddSprite((Texture2D)null);
                // mSprite.TextureAddressMode = TextureAddressMode.Clamp; // required on REACH if we're not a power of 2

                mSprite.X = mXSeed + .5f * (mNumberOfXTiles - 1) * mGridSpacing;
                mSprite.Y = mYSeed + .5f * (mNumberOfYTiles - 1) * mGridSpacing;
                mSprite.Z = this.mVisibleDisplayZ;
                mSprite.FlipVertical = true;
                mSprite.ScaleX = (mNumberOfXTiles) * mGridSpacing / 2.0f;
                mSprite.ScaleY = (mNumberOfYTiles) * mGridSpacing / 2.0f;

                mImageData = new ImageData(mNumberOfXTiles, mNumberOfYTiles);
            }

            #endregion

            if (Visible)
            {

                ForceUpdateImageData();
            }
            else
            {
                if (mSprite != null)
                {
                    SpriteManager.RemoveSprite(mSprite);
                    mSprite = null;
                }

            }


        }

        public void ForceUpdateImageData()
        {
            #region Update the texture
            if (mImageData != null)
            {

                for (int x = 0; x < mNumberOfXTiles; x++)
                {
                    for (int y = 0; y < mNumberOfYTiles; y++)
                    {
                        Color colorToSet;

                        if (mBlockedTiles[x][y] == 0)
                        {
                            if (mRevealedTiles[x][y] == 0)
                            {
                                colorToSet = HiddenClearedColor;
                            }
                            else
                            {
                                colorToSet = RevealedClearedColor;
                            }
                        }
                        else
                        {
                            if (mRevealedTiles[x][y] == 0)
                            {
                                colorToSet = HiddenBlockedColor;
                            }
                            else
                            {
                                colorToSet = RevealedBlockedColor;
                            }

                        }


                        //byte blue = (byte)(255 * (mBlockedTiles[x][y] & 1));
                        //byte green = (byte)(255 * (mRevealedTiles[x][y] & 1));
                        //byte red = 255;


                        mImageData.SetPixel(x, y, colorToSet);
                    }
                }

                ContentManager contentManager = FlatRedBallServices.GetContentManagerByName(this.ContentManagerName);
                    //FlatRedBallServices.GlobalContentManager);

                if (mSprite != null)
                {
                    if (mSprite.Texture != null)
                    {
                        contentManager.UnloadAsset(mSprite.Texture);
                        mSprite.Texture.Dispose();
                    }

                    bool generateMipmaps = false;
                    mSprite.Texture = mImageData.ToTexture2D(generateMipmaps, FlatRedBallServices.GraphicsDevice);
                    contentManager.AddDisposable("VisibilityGridTexture #" + NumberCreated, mSprite.Texture);
                    NumberCreated++;
                }
            }
            #endregion
        }

        public void ForceUpdateVisibilityGrid()
        {
            int xIndex;
            int yIndex;
            foreach (KeyValuePair<IViewer, ViewerInformation> kvp in this.mViewers)
            {

                IViewer viewer = kvp.Key;
                ViewerInformation information = kvp.Value;

                WorldToIndex(viewer.X, viewer.Y, out xIndex, out yIndex);

                int radiusAsInt = MathFunctions.RoundToInt(viewer.WorldViewRadius / mGridSpacing);

                if (xIndex >= radiusAsInt && yIndex >= radiusAsInt &&
                    xIndex + radiusAsInt < mNumberOfXTiles && yIndex + radiusAsInt < mNumberOfYTiles)
                {
                    UpdateViewersLocalVisibilityGrid(viewer, information);
                }
            }
            UpdateRevealedFromViewers();
        }

        public void UpdateFog() { UpdateFog(false); }
        public void UpdateFog(bool fullUpdate)
        {
            // For Eric:
            // This return
            // statement is
            // the first thing
            // in this method.  Are
            // we no longer using it?
            return;

            /*
            TimeManager.TimeSection("Start UpdateFog");
            if (FogResolution > 0)
            {
                Color ShadedColor = 
                    Color.FromNonPremultiplied(FogShade, FogShade, FogShade, 255);
                byte shadedValue;
                float invertedFogFactor = 1.0f / mFogFactor;

                if(fullUpdate == false)
                {
                    int x;
                    int y;

                    int xIndex;
                    int yIndex;
                    int radiusAsInt;

                    float fogXIndex;
                    float fogYIndex;

                    int fogX;
                    int fogY;

                    int fogImageX;
                    int fogImageY;

                    Color color;

                    float shift = mFogFactor / 2.0f;

                    int borderToInclude = 1;

                    IViewer viewer;
                    ViewerInformation info;
                    VisibilityGrid gridToPlace;
                    int xOffset;
                    int yOffset;

                    foreach (KeyValuePair<IViewer, ViewerInformation> kvp in mViewers)
                    {
                        viewer = kvp.Key;
                        WorldToIndex(viewer.X, viewer.Y, out xIndex, out yIndex);
                        radiusAsInt = MathFunctions.RoundToInt(viewer.WorldViewRadius / mGridSpacing)+2;

                        if (xIndex >= radiusAsInt && yIndex >= radiusAsInt &&
                            xIndex + radiusAsInt < mNumberOfXTiles && yIndex + radiusAsInt < mNumberOfYTiles)
                        {
                            info = kvp.Value;
                            gridToPlace = info.LocalVisibilityGrid;

                            xOffset = MathFunctions.RoundToInt((gridToPlace.mXSeed - mXSeed) / mGridSpacing);
                            yOffset = MathFunctions.RoundToInt((gridToPlace.mYSeed - mYSeed) / mGridSpacing);

                            for (x = -borderToInclude; x < gridToPlace.mNumberOfXTiles + borderToInclude; x++)
                            {
                                for (y = -borderToInclude; y < gridToPlace.mNumberOfYTiles + borderToInclude; y++)
                                {
                                    fogXIndex = (x + xOffset);
                                    fogImageX = (int)(fogXIndex*FogResolution);
                                    fogYIndex = (y + yOffset);
                                    fogImageY = (int)(fogYIndex*FogResolution);

                                    for (fogX = 0; fogX < mFogFactor; fogX++)
                                    {
                                        for (fogY = 0; fogY < mFogFactor; fogY++)
                                        {
                                            color = mFogImageData.GetPixelColor(fogImageX+fogX, fogImageY+fogY);
                                            shadedValue = color.A;
                                            if ( color.R > FogShade )
                                            {
                                                color = ShadedColor;
                                                shadedValue = FogShade;
                                            }
                                            if (x >= 0 && x < gridToPlace.NumberOfXTiles && y >= 0 && y < gridToPlace.NumberOfYTiles)
                                                shadedValue = CalculateFogColorByDistance(fogXIndex + (fogX * invertedFogFactor), fogYIndex + (fogY * invertedFogFactor), shadedValue);
                                            color = Color.FromNonPremultiplied(FogColor.R, FogColor.G, FogColor.B, shadedValue);
                                            //color = Color.FromNonPremultiplied(shadedValue, shadedValue, shadedValue, 255);
                                            mFogGradiantData.SetPixel(fogX, fogY, color);
                                        }
                                    }

                                    mFogGradiantData.CopyTo(mFogImageData, fogImageX, fogImageY);
                                }
                            }
                        }
                    }
                }
                else
                {
                    int xIndex, yIndex;
                    int fogX, fogY;
                    Color color;

                    for (int yTile = 0; yTile < NumberOfYTiles; yTile++)
                    {
                        for (int xTile = 0; xTile < NumberOfXTiles; xTile++)
                        {
                            xIndex = (int)(xTile * FogResolution);
                            yIndex = (int)(yTile * FogResolution);

                            for (fogX = 0; fogX < mFogFactor; fogX++)
                            {
                                for (fogY = 0; fogY < mFogFactor; fogY++)
                                {
                                    color = mFogImageData.GetPixelColor(xIndex + fogX, yIndex + fogY);
                                    shadedValue = color.R;
                                    if ( color.R > FogShade )
                                    {
                                        color = ShadedColor;
                                        shadedValue = FogShade;
                                    }
                                    shadedValue =  CalculateFogColorByDistance(xTile + (fogX * invertedFogFactor), yTile + (fogY * invertedFogFactor), shadedValue);
                                    color = Color.FromNonPremultiplied(FogColor.R, FogColor.G, FogColor.B, shadedValue);
                                    mFogGradiantData.SetPixel(fogX, fogY, color);
                                }
                            }

                            mFogGradiantData.CopyTo(mFogImageData, xIndex, yIndex);
                        }
                    }
                }
                mFogImageData.ToTexture2D(mFogTexture);
            }
             */
        }


        public void IndexToWorld(int xIndex, int yIndex, out float worldX, out float worldY)
        {
            worldX = mXSeed + mGridSpacing * xIndex;
            worldY = mYSeed + mGridSpacing * yIndex;
        }
        public void WorldToIndex(float worldX, float worldY, out int xIndex, out int yIndex)
        {
            xIndex = MathFunctions.RoundToInt((worldX - mXSeed) / mGridSpacing);
            yIndex = MathFunctions.RoundToInt((worldY - mYSeed) / mGridSpacing);

            xIndex = System.Math.Max(0, xIndex);
            xIndex = System.Math.Min(xIndex, mNumberOfXTiles - 1);

            yIndex = System.Math.Max(0, yIndex);
            yIndex = System.Math.Min(yIndex, mNumberOfYTiles - 1);
        }
        //Alternate to get partial "indices"
        public void WorldToIndex(float worldX, float worldY, out float xIndex, out float yIndex)
        {
            xIndex = (worldX - mXSeed) / mGridSpacing;
            yIndex = (worldY - mYSeed) / mGridSpacing;

            xIndex = System.Math.Max(0, xIndex);
            xIndex = System.Math.Min(xIndex, mNumberOfXTiles - 1);

            yIndex = System.Math.Max(0, yIndex);
            yIndex = System.Math.Min(yIndex, mNumberOfYTiles - 1);

        }

        #endregion

        #region Private Methods

        private void UpdateRevealedFromViewers()
        {
            MakeAllHidden();

            int x;
            int y;

            int xIndex;
            int yIndex;
            int radiusAsInt;

            foreach (KeyValuePair<IViewer, ViewerInformation> kvp in mViewers)
            {
                IViewer viewer = kvp.Key;
                WorldToIndex(viewer.X, viewer.Y, out xIndex, out yIndex);
                radiusAsInt = MathFunctions.RoundToInt(viewer.WorldViewRadius / mGridSpacing);

                if (xIndex >= radiusAsInt && yIndex >= radiusAsInt &&
                    xIndex + radiusAsInt < mNumberOfXTiles && yIndex + radiusAsInt < mNumberOfYTiles)
                {


                    ViewerInformation info = kvp.Value;
                    VisibilityGrid gridToPlace = info.LocalVisibilityGrid;

                    int xOffset = MathFunctions.RoundToInt((gridToPlace.mXSeed - mXSeed) / mGridSpacing);
                    int yOffset = MathFunctions.RoundToInt((gridToPlace.mYSeed - mYSeed) / mGridSpacing);
                    int fogX, fogY;
                    Color sourceColor;
                    Color destinationColor;
                    for (x = 0; x < gridToPlace.mNumberOfXTiles; x++)
                    {
                        for (y = 0; y < gridToPlace.mNumberOfYTiles; y++)
                        {
                            mRevealedTiles[x + xOffset][y + yOffset] |= gridToPlace.mRevealedTiles[x][y];

                            if (mFogFactor > 0)
                            {
                                for (fogX = 0; fogX < mFogFactor; fogX++)
                                {
                                    for (fogY = 0; fogY < mFogFactor; fogY++)
                                    {
                                        sourceColor = gridToPlace.mFogImageData.GetPixelColor(x*mFogFactor+fogX, y*mFogFactor+fogY);
                                        destinationColor = mFogImageData.GetPixelColor((x + xOffset)*mFogFactor+fogX, (y + yOffset)*mFogFactor+fogY);
                                        if (sourceColor.R > destinationColor.R)
                                            destinationColor = sourceColor;
                                        mFogImageData.SetPixel((x+xOffset)*mFogFactor+fogX, (y+yOffset)*mFogFactor+fogY, destinationColor);
                                    }
                                }
                            }
                        }
                    }
                }

            }

            if (mFogFactor > 0 && mFogTexture != null)
            {
                mFogImageData.ToTexture2D(mFogTexture);
            }
        }

        private void UpdateViewersLocalVisibilityGrid(IViewer viewer, ViewerInformation viewerInformation)
        {
            VisibilityGrid localGrid = viewerInformation.LocalVisibilityGrid;

            localGrid.MakeAllHidden();
            localGrid.ClearBlockedTiles();

            int viewRadius = MathFunctions.RoundToInt(viewer.WorldViewRadius / mGridSpacing);

            int xIndex;
            int yIndex;

            WorldToIndex(viewer.X, viewer.Y, out xIndex, out yIndex);

            float tileCenteredXPosition;
            float tileCenteredYPosition;

            IndexToWorld(xIndex, yIndex, out tileCenteredXPosition, out tileCenteredYPosition);

            viewerInformation.LastX = xIndex;
            viewerInformation.LastY = yIndex;

            localGrid.mXSeed =
                tileCenteredXPosition - viewer.WorldViewRadius;


            localGrid.mYSeed =
                tileCenteredYPosition - viewer.WorldViewRadius;

            int xOffset = MathFunctions.RoundToInt((localGrid.mXSeed - mXSeed) / mGridSpacing);
            int yOffset = MathFunctions.RoundToInt((localGrid.mYSeed - mYSeed) / mGridSpacing);

            // copy over the blocked areas to the viewer
            int y;
            int fogX, fogY;
            byte shadedValue;
            Color shadedColor;
            float invertedFogFactor = 1.0f;
            if (mFogFactor > 1)
                invertedFogFactor = 1.0f / mFogFactor;
            for (int x = 0; x < localGrid.mNumberOfXTiles; x++)
            {
                for (y = 0; y < localGrid.mNumberOfYTiles; y++)
                {
                    localGrid.mBlockedTiles[x][y] = mBlockedTiles[x + xOffset][y + yOffset];
                }
            }

            localGrid.MakeAllHidden();
            localGrid.RevealCircle(viewRadius, viewRadius, viewRadius);
            localGrid.BleedDirectlyVisibleToWalls();

            if (mFogFactor > 0)
            {
                for (int x = 0; x < localGrid.mNumberOfXTiles; x++)
                {
                    for (y = 0; y < localGrid.mNumberOfYTiles; y++)
                    {
                        for (fogX = 0; fogX < mFogFactor; fogX++)
                        {
                            for (fogY = 0; fogY < mFogFactor; fogY++)
                            {
                                shadedColor = localGrid.mFogImageData.GetPixelColor(x * mFogFactor + fogX, y * mFogFactor + fogY);
                                shadedValue = 0;
                                if (localGrid.mRevealedTiles[x][y] != 0)
                                    shadedValue = localGrid.CalculateFogColorByDistance(x + (fogX * invertedFogFactor), y + (fogY * invertedFogFactor), shadedColor.R, viewer);
                                else if (shadedColor.R > FogShade)
                                    shadedValue = FogShade;
                                //shadedColor = Color.FromNonPremultiplied(FogColor.R, FogColor.G, FogColor.B, shadedValue);
                                shadedColor = Color.FromNonPremultiplied(shadedValue, shadedValue, shadedValue, 255);
                                localGrid.mFogImageData.SetPixel(x * mFogFactor + fogX, y * mFogFactor + fogY, shadedColor);
                            }
                        }
                    }
                }
            }
        }

        private byte CalculateFogColorByDistance(float x, float y, byte alpha){return 0;}
        private byte CalculateFogColorByDistance(float x, float y, byte alpha, IViewer viewer)
        {
            int shadeValue = 127;// alpha;

            int radiusAsInt;
            int radiusAsIntSquared;
            //We need to extend the radius slightly to allow smooth gradients on grid squares just out side the viewable area.
            int radiusExtended;
            int radiusExtendedSquared;
            float xIndex;
            float yIndex;
            float distance;
            float calculatedValue;

            {
                radiusAsInt = MathFunctions.RoundToInt(viewer.WorldViewRadius / mGridSpacing);
                radiusExtended = radiusAsInt + 2;
                radiusAsIntSquared = radiusAsInt * radiusAsInt;
                radiusExtendedSquared = radiusExtended * radiusExtended;
                WorldToIndex(viewer.X, viewer.Y, out xIndex, out yIndex);
                distance = ((x - xIndex) * (x - xIndex)) + ((y - yIndex) * (y - yIndex));
                if (distance <= radiusAsIntSquared)
                {
                    calculatedValue = distance / radiusAsIntSquared;
 
                    calculatedValue = MathHelper.Max(0, MathHelper.Min( (calculatedValue), 1.0f));
                    shadeValue = (byte)((1.0f-calculatedValue) * 127);
                    alpha = (byte)MathHelper.Min(MathHelper.Max(alpha, shadeValue), 127);
                }
            }

            return alpha;
        }

        #endregion


        #endregion
    }
}