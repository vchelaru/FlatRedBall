using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.ManagedSpriteGroups
{

    public class TextureGrid<T>
    {
        #region Fields
        internal List<List<T>> mTextures;

        internal T mBaseTexture;

        List<float> mFirstPaintedX = new List<float>();
        List<float> mLastPaintedX = new List<float>();

        float mFirstPaintedY;
        float mLastPaintedY;

        internal float mXOffset;
        internal float mYOffset;

        float mGridSpacingX;
        float mGridSpacingY;

        float mSpaceHalfX;
        float mSpaceHalfY;

        float mSpaceFourthX;
        float mSpaceFourthY;
        #endregion

        #region Properties

        public T BaseTexture
        {
            get { return mBaseTexture; }
            set { mBaseTexture = value; }
        }

        public float GridSpacingX
        {
            get { return mGridSpacingX; }
            set
            {
                mGridSpacingX = value;
                mSpaceFourthX = mGridSpacingX / 4.0f;
                mSpaceHalfX = mGridSpacingX / 2.0f;
            }
        }

        public float GridSpacingY
        {
            get { return mGridSpacingY; }
            set
            {
                mGridSpacingY = value;
                mSpaceFourthY = mGridSpacingY / 4.0f;
                mSpaceHalfY = mGridSpacingY / 2.0f;
            }
        }

        public List<T> this[int i]
        {
            get
            {
                return mTextures[i];
            }
        }

        public List<float> FirstPaintedX
        {
            get { return mFirstPaintedX; }
            set { mFirstPaintedX = value; }
        }

        public List<float> LastPaintedX
        {
            get { return mLastPaintedX; }
            set { mLastPaintedX = value; }
        }

        public float FirstPaintedY
        {
            get { return mFirstPaintedY; }
            set { mFirstPaintedY = value; }
        }

        public float LastPaintedY
        {
            get { return mLastPaintedY; }
            set { mLastPaintedY = value; }
        }

        public List<List<T>> Textures
        {
            get { return mTextures; }
            set { mTextures = value; }
        }

        #endregion

        #region Methods

        #region Constructor

        public TextureGrid()
        {
            mTextures = new List<List<T>>();

            mFirstPaintedX = new List<float>();
            mLastPaintedX = new List<float>();
        }

        #endregion

        public void Initialize(T baseTexture, float xOffset, float yOffset, float gridSpacingX, float gridSpacingY)
        {
            this.mBaseTexture = baseTexture;
            mXOffset = xOffset;
            mYOffset = yOffset;

            mGridSpacingX = gridSpacingX;
            mGridSpacingY = gridSpacingY;

            this.mFirstPaintedY = float.NaN;
            this.mLastPaintedY = float.NaN;
        }


        public void InvertZ()
        {
            // Actually, Y is Z when the SpriteGrid that uses this goes into the Z plane
            mFirstPaintedY = -mFirstPaintedY;
            mLastPaintedY = -mLastPaintedY;
        }


        public void Clear()
        {
            mFirstPaintedX.Clear();
            mLastPaintedX.Clear();

            mFirstPaintedY = float.NaN;
            mLastPaintedY = float.NaN;

            mTextures.Clear();
        }

        #region XML Docs
        /// <summary>
        /// Shifts the position of all textures in the TextureGrid.
        /// </summary>
        /// <param name="x">The distance along the x axis to shift the grid.</param>
        /// <param name="y">The distance along the y axis to shift the grid.</param>
        #endregion
        public void ChangeGrid(float x, float y)
        {
            mFirstPaintedY += y;
            mLastPaintedY += y;

            for (int i = 0; i < mFirstPaintedX.Count; i++)
            {
                if (float.IsNaN(this.mFirstPaintedX[i]) == false)
                {
                    mFirstPaintedX[i] += x;
                    mLastPaintedX[i] += x;
                }
            }

            mXOffset += x;
            mYOffset += y;
        }


        public TextureGrid<T> Clone()
        {
            TextureGrid<T> gridToReturn = (TextureGrid<T>)(MemberwiseClone());

            gridToReturn.mTextures = new List<List<T>>();

            foreach (List<T> array in mTextures)
            {
                List<T> ta = new List<T>();
                gridToReturn.mTextures.Add(ta);

                foreach (T texture in array)
                    ta.Add(texture);

            }

            gridToReturn.mFirstPaintedX = new List<float>();
            foreach (float f in mFirstPaintedX)
                gridToReturn.mFirstPaintedX.Add(f);

            gridToReturn.mLastPaintedX = new List<float>();
            foreach (float f in mLastPaintedX)
                gridToReturn.mLastPaintedX.Add(f);


            return gridToReturn;
        }


        public T GetTextureAt(double x, double y)
        {
            if (mTextures.Count == 0)
                return mBaseTexture;

            if (y - mSpaceHalfY < mLastPaintedY && y + mSpaceHalfY > mFirstPaintedY)
            {
                int yOn = (int)System.Math.Round((y - mFirstPaintedY) / mGridSpacingY);

                if (mFirstPaintedX.Count > 0 &&
                    x > mFirstPaintedX[yOn] - mSpaceHalfX && x <= mLastPaintedX[yOn] + mSpaceHalfX)
                {
                    int xOn = (int)System.Math.Round((x - mFirstPaintedX[yOn]) / mGridSpacingX);

                    if(xOn > mTextures[yOn].Count - 1)
                        return mTextures[yOn][mTextures[yOn].Count - 1];
                    else
                        return mTextures[yOn][xOn];
                }
            }
            return mBaseTexture;
        }


        public List<TextureLocation<T>> GetTextureLocationDifferences(TextureGrid<T> originalGrid)
        {
            List<TextureLocation<T>> textureLocationArray = new List<TextureLocation<T>>();
            // These are empty grids, so there's no changes.
            if (this.FirstPaintedX.Count == 0 && originalGrid.FirstPaintedX.Count == 0)
            {
                return textureLocationArray;
            }


            int thisYOn = 0;
            int originalYOn = 0;

            #region Get minY which is the smallest painted Y

            float minY = (float)System.Math.Min(mFirstPaintedY, originalGrid.mFirstPaintedY);
            if (originalGrid.mTextures.Count == 0)
            {
                minY = mFirstPaintedY;
            }
            else if (this.mTextures.Count == 0)
            {
                minY = originalGrid.mFirstPaintedY;
            }

            if (float.IsNaN(originalGrid.mFirstPaintedY))
                minY = mFirstPaintedY;
            #endregion

            #region Get maxY which is the largest painted Y

            float maxY = (float)System.Math.Max(mLastPaintedY, originalGrid.mLastPaintedY);

            if (originalGrid.mTextures.Count == 0)
            {
                maxY = mLastPaintedY;
            }
            else if (this.mTextures.Count == 0)
            {
                maxY = originalGrid.mLastPaintedY;
            }

            if (float.IsNaN(originalGrid.mLastPaintedY))
                maxY = mLastPaintedY;
            #endregion

            if (originalGrid.mTextures.Count == 0)
            {
                originalYOn = 0;
                thisYOn = 0;

            }
            else if (originalGrid.mFirstPaintedY - mFirstPaintedY > mSpaceHalfY)
            {
                thisYOn = 0;
                originalYOn = (int)System.Math.Round((mFirstPaintedY - originalGrid.mFirstPaintedY) / mGridSpacingY);
            }
            else if (mFirstPaintedY - originalGrid.mFirstPaintedY > mSpaceHalfY)
            {// if the this instance begins below originalGrid
                originalYOn = 0;
                thisYOn = (int)System.Math.Round((mFirstPaintedY - originalGrid.mFirstPaintedY) / mGridSpacingY);
            }


            //			float minX = 0;
            float maxX = 0;



            T originalTexture = default(T);

            for (float y = minY; y - mSpaceFourthY < maxY; y += mGridSpacingY)
            {
                if (originalGrid.mTextures.Count == 0 || originalYOn < 0)
                {// if the this instance begins below originalGrid
                    for (float x = this.mFirstPaintedX[thisYOn]; x - mSpaceFourthX < this.mLastPaintedX[thisYOn]; x += mGridSpacingX)
                    {
                        textureLocationArray.Add(new TextureLocation<T>(originalGrid.mBaseTexture, x, y));
                    }
                }
                else if (thisYOn < 0)
                {// the original grid is below this grid
                    for (float x = originalGrid.mFirstPaintedX[originalYOn]; x - mSpaceFourthX < originalGrid.mLastPaintedX[originalYOn]; x += mGridSpacingX)
                    {
                        textureLocationArray.Add(new TextureLocation<T>(this.mBaseTexture, x, y));
                    }
                }
                else
                {// y is above the bottom border of both grids
                    if (thisYOn > this.mTextures.Count - 1)
                    { // y is above the this instance top
                        for (float x = originalGrid.mFirstPaintedX[originalYOn]; x - mSpaceFourthX < originalGrid.mLastPaintedX[originalYOn]; x += mGridSpacingX)
                        {
                            textureLocationArray.Add(new TextureLocation<T>(this.mBaseTexture, x, y));
                        }
                    }
                    else if (originalYOn > originalGrid.mTextures.Count - 1)
                    { // Y is above the originalGrid's top
                        for (float x = this.mFirstPaintedX[thisYOn]; x - mSpaceFourthX < this.mLastPaintedX[thisYOn]; x += mGridSpacingX)
                        {
                            textureLocationArray.Add(new TextureLocation<T>(originalGrid.mBaseTexture, x, y));
                        }
                    }
                    else
                    { // y is between the top and bottom of both grids

                        maxX = System.Math.Max(this.mLastPaintedX[thisYOn], originalGrid.mLastPaintedX[originalYOn]);


                        for (float x = System.Math.Min(this.mFirstPaintedX[thisYOn], originalGrid.mFirstPaintedX[originalYOn]); x - mSpaceFourthX < maxX; x += mGridSpacingX)
                        {
                            originalTexture = originalGrid.GetTextureAt(x, y);

                            T newTexture = this.GetTextureAt(x, y);

                            bool areBothNull = newTexture == null && originalTexture == null;

                            if (!areBothNull)
                            {
                                if ((originalTexture == null && newTexture != null) ||
                                    originalTexture.Equals(this.GetTextureAt(x, y)) == false)
                                    textureLocationArray.Add(new TextureLocation<T>(originalTexture, x, y));
                            }
                        }
                    }
                }

                // moving up one row, so increment the y value
                originalYOn++;
                thisYOn++;
            }

            return textureLocationArray;
        }


        public List<T> GetUsedTextures()
        {
            List<T> arrayToReturn = new List<T>();

            if (mBaseTexture != null)
            {
                arrayToReturn.Add(mBaseTexture);
            }

            foreach (List<T> ta in mTextures)
            {
                foreach (T tex in ta)
                {
                    if (tex != null && !arrayToReturn.Contains(tex))
                        arrayToReturn.Add(tex);
                }
            }

            return arrayToReturn;

        }


        public bool IsTextureReferenced(T texture)
        {
            if (mBaseTexture.Equals(texture))
                return true;

            foreach (List<T> fta in mTextures)
            {
                if (fta.Contains(texture))
                    return true;
            }
            return false;

        }


        public void PaintGridAtPosition(float x, float y, T textureToPaint)
        {
            T textureAtXY = this.GetTextureAt(x, y);
            if (
                (textureAtXY == null && textureToPaint == null) ||
                (textureToPaint != null && textureToPaint.Equals(textureAtXY)))
                return;

            #region locate the center position of this texture.

            //			float x = XOffset + (int)(System.Math.Round( (xFloat)/mGridSpacing)) * mGridSpacing;
            //			float y = YOffset + (int)(System.Math.Round( (yFloat)/mGridSpacing)) * mGridSpacing;

            #endregion

            #region this is the first texture we are painting
            if (mTextures.Count == 0)
            { // this is the first texture, so we mark it

                mFirstPaintedX.Add(x);
                mFirstPaintedY = y;

                mLastPaintedX.Add(x);
                mLastPaintedY = y;

                mTextures.Add(new List<T>());
                mTextures[0].Add(textureToPaint);

            }
            #endregion
            else
            {
                int yOn = (int)(System.Math.Round((y - mFirstPaintedY) / mGridSpacingY));

                #region If the position we are painting is higher than all others, Add to the ArrayLists so they include this high
                if (yOn > mFirstPaintedX.Count - 1)
                {
                    while (yOn > mFirstPaintedX.Count - 1)
                    {
                        mFirstPaintedX.Add(float.NaN);
                        mLastPaintedX.Add(float.NaN);

                        mTextures.Add(new List<T>());

                        mLastPaintedY += mGridSpacingY;
                    }
                }
                #endregion
                #region if the position we are painting is lower than all others, insert on the ArrayLists so they include this low
                else if (yOn < 0)
                {
                    while (yOn < 0)
                    {
                        mFirstPaintedX.Insert(0, float.NaN);
                        mLastPaintedX.Insert(0, float.NaN);

                        mFirstPaintedY -= mGridSpacingY;
                        mTextures.Insert(0, new List<T>());
                        yOn++;
                    }
                }
                #endregion
                #region the position is on an already-existing row
                else
                {
                    int xOn = (int)System.Math.Round((x - mFirstPaintedX[yOn]) / mGridSpacingX);

                    if (float.IsNaN(mFirstPaintedX[yOn]))
                    {
                        // first texture
                        mFirstPaintedX[yOn] = x;
                        mLastPaintedX[yOn] = x;
                        mTextures[yOn].Add(textureToPaint);
                    }

                    #region this is the furthest right on this row
                    else if (xOn > mTextures[yOn].Count - 1)
                    {
                        while (xOn > mTextures[yOn].Count - 1)
                        {
                            mLastPaintedX[yOn] += mGridSpacingX;
                            mTextures[yOn].Add(mBaseTexture);
                        }

                        mTextures[yOn][xOn] = textureToPaint;
                    }
                    #endregion
                    #region this is the furthest left on this row
                    else if (xOn < 0)
                    {
                        while (xOn < 0)
                        {
                            mFirstPaintedX[yOn] -= mGridSpacingX;
                            mTextures[yOn].Insert(0, mBaseTexture);
                            xOn++;
                        }
                        mTextures[yOn][0] = textureToPaint;

                    }
                    #endregion
                    else
                    {
                        // reduce the textures 2D array here if painting the base texture
                        mTextures[yOn][xOn] = textureToPaint;
                        if (
                            (textureToPaint == null && mBaseTexture == null) ||
                            (textureToPaint != null && textureToPaint.Equals(mBaseTexture)))
                        {
                            this.ReduceGridAtIndex(yOn, xOn);
                        }
                    }
                }
                #endregion


                /* Since we may have painted the baseTexture, the grid may have contracted.  If that's the case
				 * we need to make sure that it's not completely contracted so that firstPainted.Count == 0
				 */
                if (mFirstPaintedX.Count != 0 && yOn < mFirstPaintedX.Count && float.IsNaN(mFirstPaintedX[yOn]))
                {
                    // first texture
                    mFirstPaintedX[yOn] = x;
                    mLastPaintedX[yOn] = x;
                    mTextures[yOn].Add(textureToPaint);
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// This "shrinks" the grid if its edges are the same as its baseTexture
        /// </summary>
        /// <remarks>
        /// To use the least amount of memory, TextureGrids only store non-baseTexture strips.
        /// If the ends of a horizontal strip are the baseTexture, then the strip should be contracted
        /// inward.  If an entire horizontal strip is the baseTexture, then it should be removed.
        /// 
        /// Usually, tests should begin from a specific location, as it is usually called after the
        /// grid is painted.  This method will first check to see if the arguments are on the left or
        /// right side of a strip.  Then a loop will move inward as long as it continues to encounter
        /// the base texture.  Once it encounters a non-baseTexture, then it stops, and reduces the
        /// particular horizontal strip.
        /// 
        /// If an entire strip is cleared and it is either the top or bottom strip, then it will
        /// be removed, and the strip above or below (depending on position) will be tested as well.
        /// If the strip is in the center (not the top or bottom), then it will be reduced, but cannot
        /// be removed.
        /// </remarks>
        /// <param name="yFloat">The y location in absolute coordinates to start the tests at.</param>
        /// <param name="xFloat">The x location in absolute coordinates to start the tests at.</param>
        #endregion
        public void ReduceGrid(float yFloat, float xFloat)
        {
            //			float x = XOffset + (int)(System.Math.Round( (xFloat)/mGridSpacing)) * mGridSpacing;
            //			float y = YOffset + (int)(System.Math.Round( (yFloat)/mGridSpacing)) * mGridSpacing;

            float x = (int)(System.Math.Round((xFloat) / mGridSpacingX)) * mGridSpacingX;
            float y = (int)(System.Math.Round((yFloat) / mGridSpacingY)) * mGridSpacingY;


            int yOn = (int)(System.Math.Round((y - mFirstPaintedY) / mGridSpacingY));
            int xOn = (int)System.Math.Round((x - mFirstPaintedX[yOn]) / mGridSpacingX);
            ReduceGridAtIndex(yOn, xOn);


        }

        #region XML Docs
        /// <summary>
        /// This "shrinks" the grid if its edges are the same as its baseTexture
        /// </summary>
        /// <remarks>
        /// To use the least amount of memory, TextureGrids only store non-baseTexture strips.
        /// If the ends of a horizontal strip are the baseTexture, then the strip should be contracted
        /// inward.  If an entire horizontal strip is the baseTexture, then it should be removed.
        /// 
        /// Usually, tests should begin from a specific location, as it is usually called after the
        /// grid is painted.  This method will first check to see if the arguments are on the left or
        /// right side of a strip.  Then a loop will move inward as long as it continues to encounter
        /// the base texture.  Once it encounters a non-baseTexture, then it stops, and reduces the
        /// particular horizontal strip.
        /// 
        /// If an entire strip is cleared and it is either the top or bottom strip, then it will
        /// be removed, and the strip above or below (depending on position) will be tested as well.
        /// If the strip is in the center (not the top or bottom), then it will be reduced, but cannot
        /// be removed.
        /// </remarks>
        /// <param name="yOn">The y index start the tests at.</param>
        /// <param name="xOn">The x index to start the tests at.</param>
        #endregion
        public void ReduceGridAtIndex(int yOn, int xOn)
        {
            if (mTextures.Count == 0) return;

            // on the left side of the row
            #region move right along the row, remove textures and changing firstPaintedX[yOn]
            if (xOn == 0)
            {
                while (mTextures[yOn].Count > 0)
                {
                    T objectAt = this.mTextures[yOn][0];
                    if ((objectAt == null && mBaseTexture == null) ||
                        (objectAt != null && objectAt.Equals(mBaseTexture)))
                    {
                        mTextures[yOn].RemoveAt(0);
                        mFirstPaintedX[yOn] += this.mGridSpacingX;
                    }
                    else break;
                }
            }
            #endregion

            // on the right side of the row
            #region move left along the row, remove textures and changing lastPaintedX[yOn]
            else if (xOn == mTextures[yOn].Count - 1)
            {
                while (mTextures[yOn].Count > 0)
                {
                    T objectAt = mTextures[yOn][mTextures[yOn].Count - 1];
                    if ((objectAt == null && mBaseTexture == null) || 
                        (objectAt != null && objectAt.Equals(mBaseTexture)))
                    {
                        mTextures[yOn].RemoveAt(mTextures[yOn].Count - 1);
                        mLastPaintedX[yOn] -= this.mGridSpacingX;
                    }
                    else
                        break;
                }
            }
            #endregion


            #region if we removed the entire row, and it is the top or bottom
            if (mTextures[yOn].Count == 0 && (yOn == mFirstPaintedX.Count - 1 || yOn == 0))
            {
                mTextures.RemoveAt(yOn);
                mFirstPaintedX.RemoveAt(yOn);
                mLastPaintedX.RemoveAt(yOn);

                if (yOn == 0 && mTextures.Count != 0)
                {
                    mFirstPaintedY += mGridSpacingY;
                    ReduceGridAtIndex(0, 0);
                }
                else
                {
                    mLastPaintedY -= mGridSpacingY;
                    ReduceGridAtIndex(yOn - 1, 0);
                }
            }
            #endregion

        }


        public void ReplaceTexture(T oldTexture, T newTexture)
        {
            if ((mBaseTexture == null && oldTexture == null) ||
                (mBaseTexture != null && mBaseTexture.Equals(oldTexture)))
            {
                mBaseTexture = newTexture;
            }

            for (int k = 0; k < mTextures.Count; k++)
            {
                List<T> fta = mTextures[k];
                for (int i = 0; i < fta.Count; i++)
                {
                    if ((fta[i] == null && oldTexture == null ) ||
                        (fta[i] != null && fta[i].Equals(oldTexture)))
                        fta[i] = newTexture;
                }
            }

            TrimGrid();
        }

/*
        public void ReplaceTexture(string oldTexture, string newTexture)
        {
            if (mBaseTexture.fileName == oldTexture)
            {
                mBaseTexture = spriteManager.AddTexture(newTexture);
            }

            foreach (List<Texture2D> fta in mTextures)
            {
                for (int i = 0; i < fta.Count; i++)
                {
                    if (fta[i].fileName == oldTexture)
                        fta[i] = spriteManager.AddTexture(newTexture);
                }
            }

            TrimGrid();
        }
        */
        public void ScaleBy(double scaleValue)
        {
            GridSpacingX = (float)(mGridSpacingX * scaleValue);
            GridSpacingY = (float)(mGridSpacingY * scaleValue);

            mFirstPaintedY = (float)(mFirstPaintedY * scaleValue);
            mLastPaintedY = (float)(mLastPaintedY * scaleValue);

            mXOffset = (float)(mXOffset * scaleValue);
            mYOffset = (float)(mYOffset * scaleValue);

            for (int i = 0; i < mFirstPaintedX.Count; i++)
            {
                mFirstPaintedX[i] = (float)(mFirstPaintedX[i] * scaleValue);
                mLastPaintedX[i] = (float)(mLastPaintedX[i] * scaleValue);

            }

        }


        public void SetFrom(TextureGrid<T> instanceToSetFrom)
        {
            mBaseTexture = instanceToSetFrom.mBaseTexture;

            mTextures = new List<List<T>>();

            for (int i = 0; i < instanceToSetFrom.mTextures.Count; i++ )
            {
                mTextures.Add(new List<T>(instanceToSetFrom.mTextures[i]));
            }

            mFirstPaintedX = new List<float>(instanceToSetFrom.mFirstPaintedX);
            mLastPaintedX = new List<float>(instanceToSetFrom.mLastPaintedX);

            mFirstPaintedY = instanceToSetFrom.mFirstPaintedY;
            mLastPaintedY = instanceToSetFrom.mLastPaintedY;

            mXOffset = instanceToSetFrom.mXOffset;
            mYOffset = instanceToSetFrom.mYOffset;

            mGridSpacingX = instanceToSetFrom.mGridSpacingX;
            mGridSpacingY = instanceToSetFrom.mGridSpacingY;

            mSpaceHalfX = instanceToSetFrom.mSpaceHalfX;
            mSpaceHalfY = instanceToSetFrom.mSpaceHalfY;

            mSpaceFourthX = instanceToSetFrom.mSpaceFourthX;
            mSpaceFourthY = instanceToSetFrom.mSpaceFourthY;
        }

        #region XML Docs
        /// <summary>
        /// Checks the boundaries of the grid and removes any references to textures that match the base Texture2D.
        /// </summary>
        /// <remarks>
        /// This method is called automatically by the ReplaceTexture method so that the structure 
        /// stays as small as possible afterchanges have been made.  
        /// </remarks>
        #endregion
        public void TrimGrid()
        {
            TrimGrid(float.NegativeInfinity, float.PositiveInfinity);
        }

        public void TrimGrid(float minimumX, float maximumX)
        {
            

            // begin at the top of the grid and move down
            for (int yOn = mTextures.Count - 1; yOn > -1; yOn--)
            {
                /* The ReduceGridAtIndex will not only reduce off of the left and right side, but
                 * it will remove entire rows.  If a row is removed, the method will recur and move
                 * down one row.  It will continue doing so until it no longer removes an enire row or until
                 * all rows have been remvoed.
                 * 
                 * Because of this, it is possible that more than one row will be removed during the method call.
                 * Therefore, there needs to be an if statement making sure that row[yOn] exists
                 */


                while (yOn < mTextures.Count && mTextures[yOn].Count > 0 && mFirstPaintedX[yOn] < minimumX)
                    {
                        mTextures[yOn].RemoveAt(0);
                        mFirstPaintedX[yOn] += this.mGridSpacingX;
                    }

                    while (yOn < mTextures.Count && mTextures[yOn].Count > 0 && mLastPaintedX[yOn] > maximumX)
                    {
                        mTextures[yOn].RemoveAt(mTextures[yOn].Count - 1);
                        mLastPaintedX[yOn] -= this.mGridSpacingX;
                    }

                    if (yOn < mTextures.Count && mTextures.Count > 0)
                    {
                        ReduceGridAtIndex(yOn, 0);
                    }



                    if (yOn < mTextures.Count && mTextures.Count > 0 && mTextures[yOn].Count > 0)
                        ReduceGridAtIndex(yOn, mTextures[yOn].Count - 1);


            }
        }

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("BaseTexture:").Append(mBaseTexture.ToString());
            sb.Append("\nNumTextureRows:").Append(mTextures.Count);
            sb.Append("\nfirstPaintedY:").Append(mFirstPaintedY);
            sb.Append("\nlastPaintedY:").Append(mLastPaintedY).Append("\n");
            return sb.ToString();
            //return base.ToString();
        }

        #endregion

        internal void ChopOffBottom()
        {
            mTextures.RemoveAt(0);
            mFirstPaintedX.RemoveAt(0);
            mLastPaintedX.RemoveAt(0);

            mFirstPaintedY += mGridSpacingY;


        }

        internal void ChopOffTop()
        {
            int lastIndex = mTextures.Count - 1;

            mTextures.RemoveAt(lastIndex);
            mFirstPaintedX.RemoveAt(lastIndex);
            mLastPaintedX.RemoveAt(lastIndex);
            mLastPaintedY -= mGridSpacingY;
        }



    }
}
