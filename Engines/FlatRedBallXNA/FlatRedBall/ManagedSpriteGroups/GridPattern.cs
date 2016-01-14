using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Graphics.Tile;
using FlatRedBall.Math;

#if FRB_XNA || ZUNE || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FlatRedBall.ManagedSpriteGroups
{
    #region PatternConsideration Enum
    public enum PatternConsideration
    {
        Include,
        Exclude,
        OrInclude
    }
    #endregion

    public class GridPattern<T>
    {
        #region Fields
        internal List<GridRelativeState<T>> mIncludePattern = new List<GridRelativeState<T>>();
        internal List<GridRelativeState<T>> mExcludePattern = new List<GridRelativeState<T>>();

        internal List<GridRelativeStateList<T>> mOrIncludePattern = new List<GridRelativeStateList<T>>();
        #endregion

        public List<GridRelativeState<T>> IncludePattern
        {
            get { return mIncludePattern; }
        }

        public List<GridRelativeState<T>> ExcludePattern
        {
            get { return mExcludePattern; }
        }
        

        #region Methods

        #region Constructor

        public GridPattern()
        {

        }

        public GridPattern(List<GridRelativeState<T>> includePattern, 
            List<GridRelativeState<T>> excludePattern,
            List<GridRelativeStateList<T>> orIncludePattern)
        {
            if (includePattern != null)
            {
                mIncludePattern = includePattern;
            }
            if (excludePattern != null)
            {
                mExcludePattern = excludePattern;
            }
            if (orIncludePattern != null)
            {
                mOrIncludePattern = orIncludePattern;
            }
        }

        #endregion

        #region Public Methods

        public void AddToPattern(int x, int y, T texture, PatternConsideration patternConsideration)
        {
            #region Include
            if (patternConsideration == PatternConsideration.Include)
            {
                // See if the pattern is in the mExcludePattern - remove it if so
                // since a GridRelativeTexture shouldn't exist in both lists at once.
                int indexInExclude = IndexOfGridPattern(x, y, texture, PatternConsideration.Exclude);
                if (indexInExclude != -1)
                {
                    mExcludePattern.RemoveAt(indexInExclude);
                }

                mIncludePattern.Add(new GridRelativeState<T>(x, y, texture));
            }
            #endregion

            #region OrInclude
            else if (patternConsideration == PatternConsideration.OrInclude)
            {
                int indexOfExistingPattern = IndexOfOrPatternAt(x, y);

                if (indexOfExistingPattern == -1)
                {
                    GridRelativeStateList<T> textureList = new GridRelativeStateList<T>(x, y);
                    textureList.StateList.Add(texture);
                    mOrIncludePattern.Add(textureList);
                }
                else
                {
                    if (mOrIncludePattern[indexOfExistingPattern].StateList.Contains(texture) == false)
                    {
                        mOrIncludePattern[indexOfExistingPattern].StateList.Add(texture);
                    }

                }
            }
            #endregion

            #region Exclude
            else if (patternConsideration == PatternConsideration.Exclude)
            {
                // See if the pattern is in the mIncludePattern - remove it if so
                // since a GridRelativeTexture shouldn't exist in both lists at once.
                int indexInInclude = IndexOfGridPattern(x, y, texture, PatternConsideration.Include);
                if (indexInInclude != -1)
                {
                    mIncludePattern.RemoveAt(indexInInclude);
                }

                mExcludePattern.Add(new GridRelativeState<T>(x, y, texture));
            }
            #endregion
        }
        
        public GridPattern<T> Clone()
        {
            GridPattern<T> toReturn = new GridPattern<T>();

            foreach (GridRelativeState<T> state in mIncludePattern)
            {
                toReturn.mIncludePattern.Add(state.Clone());
            }

            foreach (GridRelativeState<T> state in mExcludePattern)
            {
                toReturn.mExcludePattern.Add(state.Clone());
            }

            foreach (GridRelativeStateList<T> stateList in mOrIncludePattern)
            {
                toReturn.mOrIncludePattern.Add(stateList.Clone());
            }

            return toReturn;
        }

        public bool DoesListContainPatternAt(SpriteList spriteList, float x, float y, float gridSpacing)
        {
            foreach (GridRelativeState<T> gridRelativeTexture in mIncludePattern)
            {
                Sprite sprite = spriteList.FindUnrotatedSpriteAt(
                    x + gridRelativeTexture.X * gridSpacing,
                    y + gridRelativeTexture.Y * gridSpacing);

                if (sprite == null || sprite.Texture != gridRelativeTexture.State as Texture2D)
                {
                    return false;
                }
            }

            foreach (GridRelativeState<T> gridRelativeTexture in mExcludePattern)
            {
                Sprite sprite = spriteList.FindUnrotatedSpriteAt(
                    x + gridRelativeTexture.X * gridSpacing,
                    y + gridRelativeTexture.Y * gridSpacing);

                if (sprite != null && sprite.Texture == gridRelativeTexture.State as Texture2D)
                {
                    return false;
                }
            }

            foreach (GridRelativeStateList<T> gridRelativeTextureList in mOrIncludePattern)
            {
                Sprite sprite = spriteList.FindUnrotatedSpriteAt(
                    x + gridRelativeTextureList.X * gridSpacing,
                    y + gridRelativeTextureList.Y * gridSpacing);

                if (sprite == null)
                {
                    return false;
                }
                else
                {
                    bool found = false;

                    foreach (T texture in gridRelativeTextureList.StateList)
                    {
                        if (texture as Texture2D == sprite.Texture)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (found == false)
                        return false;
                }
            }
            return true;
        }

        public void RotateCounterClockwise90AboutCenter()
        {
            int minX;
            int maxX;
            int minY;
            int maxY;
            GetMinsAndMaxes(out minX, out maxX, out minY, out maxY);

            bool isEvenX = (maxX - minX) % 2 == 0;
            bool isEvenY = (maxY - minY) % 2 == 0;
            if (isEvenX != isEvenY)
            {
                throw new InvalidOperationException("This grid pattern is not even on both dimensions or odd on both dimensions, so it can't be rotated about its center");
            }


            float centerX = (minX + maxX) / 2.0f;
            float centerY = (minY + maxY) / 2.0f;



            RotateCounterClockwise90AboutPoint(centerX, centerY);


        }

        public void RotateCounterClockwise90AboutPoint(float centerX, float centerY)
        {
            float xFromCenter;
            float yFromCenter;

            foreach (GridRelativeState<T> state in mIncludePattern)
            {
                xFromCenter = state.X - centerX;
                yFromCenter = state.Y - centerY;

                state.Y = MathFunctions.RoundToInt(xFromCenter + centerY);
                state.X = MathFunctions.RoundToInt(-yFromCenter + centerX);
            }
            foreach (GridRelativeState<T> state in mExcludePattern)
            {
                xFromCenter = state.X - centerX;
                yFromCenter = state.Y - centerY;

                state.Y = MathFunctions.RoundToInt(xFromCenter + centerY);
                state.X = MathFunctions.RoundToInt(-yFromCenter + centerX);
            }

            foreach (GridRelativeStateList<T> stateList in mOrIncludePattern)
            {
                xFromCenter = stateList.X - centerX;
                yFromCenter = stateList.Y - centerY;

                stateList.Y = MathFunctions.RoundToInt(xFromCenter + centerY);
                stateList.X = MathFunctions.RoundToInt(-yFromCenter + centerX);
            }
        }

        public void Shift(int xShift, int yShift)
        {
            foreach (GridRelativeState<T> state in mIncludePattern)
            {
                state.X += xShift;
                state.Y += yShift;
            }
            foreach (GridRelativeState<T> state in mExcludePattern)
            {
                state.X += xShift;
                state.Y += yShift;
            }

            foreach (GridRelativeStateList<T> stateList in mOrIncludePattern)
            {
                stateList.X += xShift;
                stateList.Y += yShift;
            }

        }

        #endregion

        #region Private Methods

        private void GetMinsAndMaxes(out int minX, out int maxX, out int minY, out int maxY)
        {
            minX = int.MaxValue;
            maxX = int.MinValue;

            minY = int.MaxValue;
            maxY = int.MinValue;

            foreach (GridRelativeState<T> state in mIncludePattern)
            {
                minX = System.Math.Min(minX, state.X);
                maxX = System.Math.Max(maxX, state.X);

                minY = System.Math.Min(minY, state.Y);
                maxY = System.Math.Max(maxY, state.Y);
            }
            foreach (GridRelativeState<T> state in mExcludePattern)
            {
                minX = System.Math.Min(minX, state.X);
                maxX = System.Math.Max(maxX, state.X);

                minY = System.Math.Min(minY, state.Y);
                maxY = System.Math.Max(maxY, state.Y);
            }

            foreach (GridRelativeStateList<T> stateList in mOrIncludePattern)
            {
                minX = System.Math.Min(minX, stateList.X);
                maxX = System.Math.Max(maxX, stateList.X);

                minY = System.Math.Min(minY, stateList.Y);
                maxY = System.Math.Max(maxY, stateList.Y);
            }
        }

        private int IndexOfGridPattern(int x, int y, T texture, PatternConsideration patternConsideration)
        {
            if (patternConsideration == PatternConsideration.Include)
            {
                for (int i = 0; i < mIncludePattern.Count; i++)
                {
                    if (mIncludePattern[i].X == x &&
                        mIncludePattern[i].Y == y &&
                        mIncludePattern[i].State.Equals(texture))
                    {
                        return i;
                    }
                }
            }
            else if (patternConsideration == PatternConsideration.OrInclude)
            {
                for (int i = 0; i < mOrIncludePattern.Count; i++)
                {
                    if (mOrIncludePattern[i].X == x &&
                        mOrIncludePattern[i].Y == y &&
                        mOrIncludePattern[i].StateList.Contains(texture))
                    {
                        return i;
                    }
                }
            }
            else if (patternConsideration == PatternConsideration.Exclude)
            {
                for (int i = 0; i < mExcludePattern.Count; i++)
                {
                    if (mExcludePattern[i].X == x &&
                        mExcludePattern[i].Y == y &&
                        mExcludePattern[i].State.Equals(texture))
                    {
                        return i;
                    }
                }
            }

            return -1;
        }

        private int IndexOfOrPatternAt(int x, int y)
        {
            for (int i = 0; i < mOrIncludePattern.Count; i++)
            {
                if (mOrIncludePattern[i].X == x && mOrIncludePattern[i].Y == y)
                {
                    return i;
                }
            }

            return -1;
        }

        #endregion

        #endregion
    }
} 