using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FlatRedBall.TileGraphics
{
    public class Tileset
    {
        private Texture2D mTexture;
        private int mNumRows, mNumCols;
        private float mTextureTileHeight, mTextureTileWidth;
        private int mTileDimensionWidth, mTileDimensionHeight; // in pixels


        
        public Tileset(string pTilesetFilename, int pTileDimensionWidth, int pTileDimensionHeight) 
        {
            mTexture = FlatRedBall.FlatRedBallServices.Load<Texture2D>(pTilesetFilename);

            mTileDimensionWidth = pTileDimensionWidth;
            mTileDimensionHeight = pTileDimensionHeight;

            mNumCols = mTexture.Width / pTileDimensionWidth;
            mNumRows = mTexture.Height / pTileDimensionHeight;

            mTextureTileWidth = (float)(pTileDimensionWidth) / (float)(mTexture.Width);
            mTextureTileHeight = (float)(pTileDimensionHeight) / (float)(mTexture.Height);
          //  FlatRedBall.Debugging.Debugger.CommandLineWrite("Dimensions: " + mTextureTileWidth + " x " + mTextureTileHeight);
        }

        public Tileset(Texture2D tilesetTexture, int tileDimensionWidth, int tileDimensionHeight)
        {
            mTexture = tilesetTexture;

            mTileDimensionWidth = tileDimensionWidth;
            mTileDimensionHeight = tileDimensionHeight;

            if(mTexture != null)
            {
                mNumCols = mTexture.Width / tileDimensionWidth;
                mNumRows = mTexture.Height / tileDimensionHeight;

                mTextureTileWidth = (float)(tileDimensionWidth) / (float)(mTexture.Width);
                mTextureTileHeight = (float)(tileDimensionHeight) / (float)(mTexture.Height);
            }
        }

        public short GetTextureIndexFromCoordinate(Vector2 topLeftUVCoordinate)
        {
            short column = (short)(topLeftUVCoordinate.X / mTextureTileWidth);
            short row = (short)(topLeftUVCoordinate.Y / mTextureTileHeight);

            return (short)((mNumCols*row) + column);
        }

        public Vector2[] GetTextureCoordinateVectorsOfTextureIndex(int textureId)
        {
            // TODO: pId needs to be constrained! 

            Vector2[] coords = new Vector2[4];

            GetTextureCoordinateVectorsOfTextureIndex(textureId, coords);

            return coords;
        
        }

        /// <summary>
        /// Returns the texture coordinates for the argument index in this tileset. Returned coords array contains
        /// four coordinates in the following order: 
        /// 0: bottom left
        /// 1: bottom right
        /// 2: top right
        /// 3: top left
        /// </summary>
        /// <param name="textureId">The texture ID, which is the index of fthe texture beginning at the top left of the tileset and moving to the right, then down</param>
        /// <param name="coords">The resulting coordinates.</param>
        public void GetTextureCoordinateVectorsOfTextureIndex(int textureId, Vector2[] coords)
        {
            // TODO: pId needs to be constrained! 

            float x, y;
            if (textureId == 0)
            {
                x = 0;
                y = 0;
            }
            else
            {
                x = (float)(textureId % mNumCols) * mTextureTileWidth;
                
                y = ((int)((float)textureId - x) / mNumCols) * mTextureTileHeight;
            }
             

            // Coords are
            // 3   2
            //
            // 0   1

            GetCoordinatesForTile(coords, x, y);
        }

        public void GetCoordinatesForTile(Vector2[] coords, float leftTextureCoordinate, float topTextureCoordinate)
        {

            coords[0] = new Vector2(leftTextureCoordinate, topTextureCoordinate + mTextureTileHeight);
            coords[1] = new Vector2(leftTextureCoordinate + mTextureTileWidth, topTextureCoordinate + mTextureTileHeight);
            coords[2] = new Vector2(leftTextureCoordinate + mTextureTileWidth, topTextureCoordinate);
            coords[3] = new Vector2(leftTextureCoordinate, topTextureCoordinate);
        }



    }
}
