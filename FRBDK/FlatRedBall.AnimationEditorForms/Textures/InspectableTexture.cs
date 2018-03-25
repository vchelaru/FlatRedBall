using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.AnimationEditorForms.Textures
{
    public class InspectableTexture
    {
        FlatRedBall.Graphics.Texture.ImageData mImageData;
        Texture2D mTexture;

        // This is used
        bool[][] mVisistedPoints;

        public Texture2D Texture
        {
            get
            {
                return mTexture;
            }
            set
            {
                
                mTexture = value;
                if (mTexture != null)
                {
                    mImageData = FlatRedBall.Graphics.Texture.ImageData.FromTexture2D(mTexture);

                    bool shouldRecreateVisited = mVisistedPoints == null;

                    if (!shouldRecreateVisited)
                    {
                        shouldRecreateVisited = mVisistedPoints.Length < mTexture.Width;

                        if (!shouldRecreateVisited)
                        {
                            shouldRecreateVisited = mVisistedPoints[0].Length < mTexture.Height;
                        }
                    }

                    if (shouldRecreateVisited)
                    {
                        mVisistedPoints = new bool[mTexture.Width][];

                        for (int i = 0; i < mTexture.Width; i++)
                        {
                            mVisistedPoints[i] = new bool[mTexture.Height];
                        }

                    }
                }
                else
                {
                    mImageData = null;
                }
            }
        }

        public void GetOpaqueWandBounds(int xPixel, int yPixel, out int minX, out int minY, out int maxX, out int maxY)
        {
#if DEBUG
            if(mImageData == null)
            {
                throw new NullReferenceException(nameof(mImageData));
            }

#endif
            for (int i = 0; i < mVisistedPoints.Length; i++)
            {
                Array.Clear(mVisistedPoints[i], 0, mVisistedPoints[i].Length);
            }
            List<System.Drawing.Point> openList = new List<System.Drawing.Point>();

            minX = int.MaxValue;
            minY = int.MaxValue;
            maxX = int.MinValue;
            maxY = int.MinValue;

            if (CanContinueSearchAt(xPixel, yPixel, mVisistedPoints))
            {
                openList.Add(new System.Drawing.Point(xPixel, yPixel));
            }

            while (openList.Count != 0)
            {
                int index = openList.Count - 1;
                System.Drawing.Point last = openList[index];
                openList.RemoveAt(index);
                int currentX = last.X;
                int currentY = last.Y;

                if (currentX < minX)
                {
                    minX = currentX;
                }
                if (currentX > maxX)
                {
                    maxX = currentX;
                }

                if (currentY < minY)
                {
                    minY = currentY;
                }
                if (currentY > maxY)
                {
                    maxY = currentY;
                }

                mVisistedPoints[currentX][currentY] = true;

                if (CanContinueSearchAt(currentX, currentY - 1, mVisistedPoints))
                {
                    openList.Add(new System.Drawing.Point(currentX, currentY - 1));
                }
                if (CanContinueSearchAt(currentX, currentY + 1, mVisistedPoints))
                {
                    openList.Add(new System.Drawing.Point(currentX, currentY + 1));
                }
                if (CanContinueSearchAt(currentX + 1, currentY, mVisistedPoints))
                {
                    openList.Add(new System.Drawing.Point(currentX + 1, currentY));
                }
                if (CanContinueSearchAt(currentX - 1, currentY, mVisistedPoints))
                {
                    openList.Add(new System.Drawing.Point(currentX - 1, currentY));
                }

            }

            if(maxX > minX && maxY > minY)
            {
                // We have to add 1 here to capture the last row/column. Before this was done by the caller,
                // but that doesn't make sense, this method should just return everything ready-to-use.
                maxX += 1;
                maxY += 1;
            }

        }


        private bool CanContinueSearchAt(int x, int y, bool[][] visitedPoints)
        {
            return x > -1 && y > -1 && x < mImageData.Width && y < mImageData.Height &&
                visitedPoints[x][y] == false &&
                mImageData.GetPixelColor(x, y).A != 0;
        }

    }
}
