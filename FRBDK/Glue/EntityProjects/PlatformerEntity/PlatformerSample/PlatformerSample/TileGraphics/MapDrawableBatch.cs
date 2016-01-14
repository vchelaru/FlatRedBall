using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Graphics;
using Microsoft.Xna.Framework.Graphics;
using FlatRedBall;
using Microsoft.Xna.Framework;
using FlatRedBall.Content;
using FlatRedBall.Content.Scene;
using FlatRedBall.IO;
using FlatRedBall.Input;
using FlatRedBall.Debugging;
using FlatRedBall.Math;

namespace FlatRedBall.TileGraphics
{
    public class MapDrawableBatch: PositionedObject, IDrawableBatch
    {
        #region Fields
        protected Tileset mTileset;

        private int mMapWidth;
        private int mMapHeight;

        #region XML Docs
        /// <summary>
        /// The effect used to draw.  Shared by all instances for performance reasons
        /// </summary>
        #endregion
        private static BasicEffect mEffect;

        #region XML Docs
        /// <summary>
        /// The vertices used to draw the shape
        /// </summary>
        #endregion
        protected VertexPositionTexture[] mVertices;
        protected Texture2D mTexture;
        #region XML Docs
        /// <summary>
        /// The indices to draw the shape
        /// </summary>
        #endregion
        protected short[] mIndices;

        Dictionary<string, List<int>> mNamedTileOrderedIndexes = new Dictionary<string, List<int>>();

        private int mCurrentNumberOfTiles = 0;



        #endregion

        #region Properties

        #region XML Docs
        /// <summary>
        /// Here we tell the engine if we want this batch
        /// updated every frame.  Since we have no updating to
        /// do though, we will set this to false
        /// </summary>
        #endregion
        public bool UpdateEveryFrame
        {
            get { return true; }
        }

        public float RenderingScale
        {
            get;
            set;
        }

        public Dictionary<string, List<int>> NamedTileOrderedIndexes
        {
            get
            {
                return mNamedTileOrderedIndexes;
            }
        }

        public bool Visible
        {
            get;
            set;
        }

        #endregion

        #region Constructor / Initialization

        #region XML Docs
        /// <summary>
        /// Create and initialize all assets
        /// </summary>
        #endregion
        public MapDrawableBatch(int numberOfTiles, int textureTileDimensionWidth, int textureTileDimensionHeight, Texture2D texture): base()
        {
            if (texture == null)
                throw new ArgumentNullException("texture");

            Visible = true;
            InternalInitialize();

            mTexture = texture;
            mVertices = new VertexPositionTexture[4 * numberOfTiles];
            mIndices = new short[6 * numberOfTiles];

            mTileset = new Tileset(texture, textureTileDimensionWidth, textureTileDimensionHeight);
        }

        //public MapDrawableBatch(int mapWidth, int mapHeight, float mapTileDimension, int textureTileDimension, string tileSetFilename)
        //    : base()
        //{
        //    InternalInitialize();


        //    mTileset = new Tileset(tileSetFilename, textureTileDimension);
        //    mMapWidth = mapWidth;
        //    mMapHeight = mapHeight;

        //    int numberOfTiles = mapWidth * mapHeight;
        //    // the number of vertices is 4 times the number of tiles (each tile gets 4 vertices) 
        //    mVertices = new VertexPositionTexture[4 * numberOfTiles];

        //    // the number of indices is 6 times the number of tiles
        //    mIndices = new short[6 * numberOfTiles];
        //    for(int i = 0; i < mapHeight; i++)
        //    {
        //        for (int j = 0; j < mapWidth; j++)
        //        {
        //            int currentTile         = mapHeight * i + j;
        //            int currentVertex       = currentTile * 4;
        //            float xOffset = j * mapTileDimension;
        //            float yOffset = i * mapTileDimension;
        //            int currentIndex        = currentTile * 6; // 6 indices per tile

        //            // TEMP
        //            Vector2[] coords = mTileset.GetTextureCoordinateVectorsOfTextureIndex(new Random().Next()%4);
        //            // END TEMP


        //            // create vertices
        //            mVertices[currentVertex + 0] = new VertexPositionTexture(new Vector3(xOffset + 0f, yOffset + 0f, 0f), coords[0]);
        //            mVertices[currentVertex + 1] = new VertexPositionTexture(new Vector3(xOffset + mapTileDimension, yOffset + 0f, 0f), coords[1]);
        //            mVertices[currentVertex + 2] = new VertexPositionTexture(new Vector3(xOffset + mapTileDimension, yOffset + mapTileDimension, 0f), coords[2]);
        //            mVertices[currentVertex + 3] = new VertexPositionTexture(new Vector3(xOffset + 0f, yOffset + mapTileDimension, 0f), coords[3]);

                    
        //            // create indices
        //            mIndices[currentIndex + 0] = (short)(currentVertex + 0);
        //            mIndices[currentIndex + 1] = (short)(currentVertex + 1);
        //            mIndices[currentIndex + 2] = (short)(currentVertex + 2);
        //            mIndices[currentIndex + 3] = (short)(currentVertex + 0);
        //            mIndices[currentIndex + 4] = (short)(currentVertex + 2);
        //            mIndices[currentIndex + 5] = (short)(currentVertex + 3);

        //            mCurrentNumberOfTiles++; 
        //        }
        //    }
        //    mTexture = FlatRedBallServices.Load<Texture2D>(@"content/tiles");



        //}

        void InternalInitialize()
        {
            // We're going to share these because creating effects is slow...
            // But is this okay if we tombstone?
            if (mEffect == null)
            {
                mEffect = new BasicEffect(FlatRedBallServices.GraphicsDevice);

                mEffect.VertexColorEnabled = false;
                mEffect.TextureEnabled = true;
            }

            RenderingScale = 1;


        }

        #endregion

        #region Methods

        public void AddToManagers()
        {
            SpriteManager.AddDrawableBatch(this);
            //SpriteManager.AddPositionedObject(mMapBatch);
        }

        public static MapDrawableBatch FromScnx(string sceneFileName, string contentManagerName, bool verifySameTexturePerLayer)
        {
            // TODO: This line crashes when the path is already absolute!
            string absoluteFileName = FileManager.MakeAbsolute(sceneFileName);
            
            // TODO: The exception doesn't make sense when the file type is wrong.
            SpriteEditorScene saveInstance = SpriteEditorScene.FromFile(absoluteFileName);
            
            int startingIndex = 0;
            
            string oldRelativeDirectory = FileManager.RelativeDirectory;
            FileManager.RelativeDirectory = FileManager.GetDirectory(absoluteFileName);

            // get the list of sprites from our map file
            List<SpriteSave> spriteSaveList = saveInstance.SpriteList; 

            // we use the sprites as defined in the scnx file to create and draw the map.
            MapDrawableBatch mMapBatch = FromSpriteSaves(spriteSaveList, startingIndex, spriteSaveList.Count, contentManagerName, verifySameTexturePerLayer);

            FileManager.RelativeDirectory = oldRelativeDirectory;
            // temp
            //mMapBatch = new MapDrawableBatch(32, 32, 32f, 64, @"content/tiles");
            return mMapBatch;

        }

        /* This creates a MapDrawableBatch (MDB) from the list of sprites provided to us by the FlatRedBall (FRB) Scene XML (scnx) file. */
        public static MapDrawableBatch FromSpriteSaves(List<SpriteSave> spriteSaveList, int startingIndex, int count, string contentManagerName, bool verifySameTexturesPerLayer)
        {

#if DEBUG
            if (verifySameTexturesPerLayer)
            {
                VerifySingleTexture(spriteSaveList, startingIndex, count);
            }
#endif

            // We got it!  We are going to make some assumptions:
            // First we need the texture.  We'll assume all Sprites
            // use the same texture:

            // TODO: I (Bryan) really HATE this assumption. But it will work for now.
            SpriteSave firstSprite = spriteSaveList[startingIndex];

            // This is the file name of the texture, but the file name is relative to the .scnx location
            string textureRelativeToScene = firstSprite.Texture;
            // so we load the texture
            Texture2D texture = FlatRedBallServices.Load<Texture2D>(textureRelativeToScene, contentManagerName);

            if (!MathFunctions.IsPowerOfTwo(texture.Width) || !MathFunctions.IsPowerOfTwo(texture.Height))
            {
                throw new Exception("The dimensions of the texture file " + texture.Name + " are not power of 2!");
            }
            

            // Assume all the dimensions of the textures are the same. I.e. all tiles use the same texture width and height. 
            // This assumption is safe for Iso and Ortho tile maps.
            int tileFileDimensionsWidth = 0;
            int tileFileDimensionsHeight = 0;
            if (spriteSaveList.Count > startingIndex)
            {
                SpriteSave s = spriteSaveList[startingIndex];
                
                // deduce the dimensionality of the tile from the texture coordinates
                tileFileDimensionsWidth     = (int)System.Math.Round((double)((s.RightTextureCoordinate - s.LeftTextureCoordinate) * texture.Width));
                tileFileDimensionsHeight    = (int)System.Math.Round((double)((s.BottomTextureCoordinate - s.TopTextureCoordinate) * texture.Height));
                
            }


            // alas, we create the MDB 
            MapDrawableBatch mMapBatch = new MapDrawableBatch(count, tileFileDimensionsWidth, tileFileDimensionsHeight, texture);

            int lastIndexExclusive = startingIndex + count;

            for (int i = startingIndex; i < lastIndexExclusive; i++)
            {
                SpriteSave spriteSave = spriteSaveList[i];

                // here we have the Sprite's X and Y in absolute coords as well as its texture coords
                // NOTE: I appended the Z coordinate for the sake of iso maps. This SHOULDN'T have an effect on the ortho maps since I believe the 
                // TMX->SCNX tool sets all z to zero.

                // The AddTile method expects the bottom-left corner
                float x = spriteSave.X - spriteSave.ScaleX;
                float y = spriteSave.Y - spriteSave.ScaleY;
                float z = spriteSave.Z;

                float width = 2f * spriteSave.ScaleX; // w
                float height = 2f * spriteSave.ScaleY; // z

                float topTextureCoordinate = spriteSave.TopTextureCoordinate;
                float bottomTextureCoordinate = spriteSave.BottomTextureCoordinate;
                float leftTextureCoordinate = spriteSave.LeftTextureCoordinate;
                float rightTextureCoordinate = spriteSave.RightTextureCoordinate;

                int tileIndex = mMapBatch.mCurrentNumberOfTiles;

                mMapBatch.RegisterName(spriteSave.Name, tileIndex);

                // add the textured tile to our map so that we may draw it.
                mMapBatch.AddTile(new Vector3(x, y, z), new Vector2(width, height), new Vector4(leftTextureCoordinate, rightTextureCoordinate, topTextureCoordinate, bottomTextureCoordinate));

            }



            return mMapBatch;
        }

        private static void VerifySingleTexture(List<SpriteSave> spriteSaveList, int startingIndex, int count)
        {
            // Every Sprite should either have the same texture
            if (spriteSaveList.Count != 0)
            {
                string texture = spriteSaveList[startingIndex].Texture;

                for(int i = startingIndex + 1; i < startingIndex + count; i++)
                {
                    SpriteSave ss = spriteSaveList[i];

                    if (ss.Texture != texture)
                    {
                        float leftOfSprite = ss.X - ss.ScaleX;
                        float indexX = leftOfSprite / (ss.ScaleX * 2);

                        float topOfSprite = ss.Y + ss.ScaleY;
                        float indexY = (0 - topOfSprite) / (ss.ScaleY * 2);

                        throw new Exception("All Sprites do not have the same texture");
                    }
                }

            }
        }

        private void RegisterName(string name, int tileIndex)
        {
            int throwaway;
            if (!string.IsNullOrEmpty(name) && !int.TryParse(name, out throwaway))
            {
                // TEMPORARY:
                // The tmx converter
                // names all Sprites with
                // a number if their name is
                // not explicitly set.  Therefore
                // we have to ignore those and look
                // for explicit names (names not numbers).
                // Will talk to Domenic about this to fix it.
                if (!mNamedTileOrderedIndexes.ContainsKey(name))
                {
                    mNamedTileOrderedIndexes.Add(name, new List<int>());
                }

                mNamedTileOrderedIndexes[name].Add(tileIndex);
            }
        }

        Vector2[] coords = new Vector2[4];

        /// <summary>
        /// Paints a texture on a tile. This method assumes that the MapDrawableBatch is a perfect recangle with no gaps, and that all
        /// tiles have been added left to right, top to bottom.
        /// </summary>
        /// <param name="row">The row (y) index, base 0</param>
        /// <param name="column">The column (x) index, base 0 </param>
        /// <param name="newTextureId">The texture ID, which is a left to right, top to bottom index</param>
        public void PaintTile(int row, int column, int newTextureId)
        {
            int currentTile = mMapHeight * column + row;
            int currentVertex = currentTile * 4; // 4 vertices per tile
        
            // Reusing the coords array saves us on allocation
            mTileset.GetTextureCoordinateVectorsOfTextureIndex(newTextureId, coords);

            mVertices[currentVertex + 0].TextureCoordinate = coords[0];
            mVertices[currentVertex + 1].TextureCoordinate = coords[1];
            mVertices[currentVertex + 2].TextureCoordinate = coords[2];
            mVertices[currentVertex + 3].TextureCoordinate = coords[3];
        }

        /// <summary>
        /// Paints a texture on a tile.  This method takes the index of the Sprite in the order it was added
        /// to the MapDrawableBatch, so it supports any configuration including non-rectangular maps and maps with
        /// gaps.
        /// </summary>
        /// <param name="orderedTileIndex">The index of the tile to paint - this matches the index of the tile as it was added.</param>
        /// <param name="newTextureId"></param>
        public void PaintTile(int orderedTileIndex, int newTextureId)
        {
            int currentVertex = orderedTileIndex * 4; // 4 vertices per tile
        
            // Reusing the coords array saves us on allocation
            mTileset.GetTextureCoordinateVectorsOfTextureIndex(newTextureId, coords);

            // Coords are
            // 3   2
            //
            // 0   1

            mVertices[currentVertex + 0].TextureCoordinate = coords[0];
            mVertices[currentVertex + 1].TextureCoordinate = coords[1];
            mVertices[currentVertex + 2].TextureCoordinate = coords[2];
            mVertices[currentVertex + 3].TextureCoordinate = coords[3];

        }

        public void PaintTileTextureCoordinates(int orderedTileIndex, float textureXCoordinate, float textureYCoordinate)
        {
            int currentVertex = orderedTileIndex * 4; // 4 vertices per tile

            mTileset.GetCoordinatesForTile(coords, textureXCoordinate, textureYCoordinate);

            mVertices[currentVertex + 0].TextureCoordinate = coords[0];
            mVertices[currentVertex + 1].TextureCoordinate = coords[1];
            mVertices[currentVertex + 2].TextureCoordinate = coords[2];
            mVertices[currentVertex + 3].TextureCoordinate = coords[3];
        }

        public void GetTextureCoordiantesForOrderedTile(int orderedTileIndex, out float textureX, out float textureY)
        {
            // The order is:
            // 3   2
            //
            // 0   1

            // So we want to add 3 to the index to get the top-left vert, then use
            // the texture coordinates there to get the 
            Vector2 vector = mVertices[(orderedTileIndex * 4) + 3].TextureCoordinate;

            textureX = vector.X;
            textureY = vector.Y;
        }

        public void GetBottomLeftWorldCoordinateForOrderedTile(int orderedTileIndex, out float x, out float y)
        {
            // The order is:
            // 3   2
            //
            // 0   1

            // So we just need to mutiply by 4 and not add anything
            Vector3 vector = mVertices[(orderedTileIndex * 4)].Position;

            x = vector.X;
            y = vector.Y;
        }

        /// <summary>
        /// Adds a tile to the tile map
        /// </summary>
        /// <param name="bottomLeftPosition"></param>
        /// <param name="dimensions"></param>
        /// <param name="texture">
        ///     4 points defining the boundaries in the texture for the tile.
        ///     (X = left, Y = right, Z = top, W = bottom)
        /// </param>
        public void AddTile(Vector3 bottomLeftPosition, Vector2 dimensions, Vector4 texture)
        {
            int currentVertex = mCurrentNumberOfTiles*4;

            int currentIndex = mCurrentNumberOfTiles * 6; // 6 indices per tile (there are mVertices.Length/4 tiles)
            
            float xOffset = bottomLeftPosition.X;
            float yOffset = bottomLeftPosition.Y;
            float zOffset = bottomLeftPosition.Z;

            float width = dimensions.X;
            float height = dimensions.Y;
            

            // create vertices
            mVertices[currentVertex + 0] = new VertexPositionTexture(new Vector3(xOffset + 0f, yOffset + 0f, zOffset), new Vector2(texture.X, texture.W));
            mVertices[currentVertex + 1] = new VertexPositionTexture(new Vector3(xOffset + width, yOffset + 0f, zOffset), new Vector2(texture.Y, texture.W));
            mVertices[currentVertex + 2] = new VertexPositionTexture(new Vector3(xOffset + width, yOffset + height, zOffset), new Vector2(texture.Y, texture.Z));
            mVertices[currentVertex + 3] = new VertexPositionTexture(new Vector3(xOffset + 0f, yOffset + height, zOffset), new Vector2(texture.X, texture.Z));

            // create indices
            mIndices[currentIndex + 0] = (short)(currentVertex + 0);
            mIndices[currentIndex + 1] = (short)(currentVertex + 1);
            mIndices[currentIndex + 2] = (short)(currentVertex + 2);
            mIndices[currentIndex + 3] = (short)(currentVertex + 0);
            mIndices[currentIndex + 4] = (short)(currentVertex + 2);
            mIndices[currentIndex + 5] = (short)(currentVertex + 3);

            mCurrentNumberOfTiles++;
        }

        /// <summary>
        /// Add a tile to the map
        /// </summary>
        /// <param name="bottomLeftPosition"></param>
        /// <param name="tileDimensions"></param>
        /// <param name="textureTopLeftX">Top left X coordinate in the core texture</param>
        /// <param name="textureTopLeftY">Top left Y coordinate in the core texture</param>
        /// <param name="textureBottomRightX">Bottom right X coordinate in the core texture</param>
        /// <param name="textureBottomRightY">Bottom right Y coordinate in the core texture</param>
        public void AddTile(Vector3 bottomLeftPosition, Vector2 tileDimensions, int textureTopLeftX, int textureTopLeftY, int textureBottomRightX, int textureBottomRightY)
        {
            // Form vector4 for AddTile overload
            var textureValues = new Vector4();
            textureValues.X = (float)textureTopLeftX / (float)mTexture.Width; // Left
            textureValues.Y = (float)textureBottomRightX / (float)mTexture.Width; // Right
            textureValues.Z = (float)textureTopLeftY / (float)mTexture.Height; // Top
            textureValues.W = (float)textureBottomRightY / (float)mTexture.Height; // Bottom

            AddTile(bottomLeftPosition, tileDimensions, textureValues);
        }

        #region XML Docs
        /// <summary>
        /// Custom drawing technique - sets graphics states and
        /// draws the custom shape
        /// </summary>
        /// <param name="camera">The currently drawing camera</param>
        #endregion
        public void Draw(Camera camera)
        {
            ////////////////////Early Out///////////////////

            if (!Visible)
            {
                return;
            }

            //////////////////End Early Out/////////////////


            // Set graphics states
            FlatRedBallServices.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            // texture options
            // Have the current camera set our current view/projection variables
            camera.SetDeviceViewAndProjection(mEffect, false);

            // Here we get the positioned object's transformation (position / rotation)
            mEffect.World = Matrix.CreateScale(RenderingScale) * base.TransformationMatrix;
            mEffect.Texture = mTexture;

            // We won't need to use any other kind of texture
            // address mode besides clamp, and clamp is required
            // on the "Reach" profile when the texture is not power
            // of two.  Let's set it to clamp here so that we don't crash
            // on non-power-of-two textures.
            TextureAddressMode oldTextureAddressMode = Renderer.TextureAddressMode;
            Renderer.TextureAddressMode = TextureAddressMode.Clamp;

            // Start the effect
            
            foreach (EffectPass pass in mEffect.CurrentTechnique.Passes)
            {
                // Start each pass

                pass.Apply();

                // Draw the shape
                FlatRedBallServices.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionTexture>(
                    PrimitiveType.TriangleList,
                    mVertices, 0, mVertices.Length,
                    mIndices, 0, mIndices.Length / 3);
            }

            Renderer.TextureAddressMode = oldTextureAddressMode;
        }

        #region XML Docs
        /// <summary>
        /// Here we update our batch - but this batch doesn't
        /// need to be updated
        /// </summary>
        #endregion
        public void Update()
        {
            this.TimedActivity(TimeManager.SecondDifference, TimeManager.SecondDifferenceSquaredDividedByTwo, TimeManager.LastSecondDifference);
        }

        #region XML Docs
        /// <summary>
        /// Removes itself from the engine.
        /// </summary>
        #endregion
        public void Destroy()
        {
            this.RemoveSelfFromListsBelongingTo();
            
        }

        #endregion
    }
}
