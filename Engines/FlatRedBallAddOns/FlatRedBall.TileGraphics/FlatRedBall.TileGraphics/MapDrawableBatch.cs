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
using TMXGlueLib.DataTypes;

namespace FlatRedBall.TileGraphics
{
    public enum SortAxis
    {
        None,
        X,
        Y
    }

    public class MapDrawableBatch : PositionedObject, IVisible, IDrawableBatch
    {
        #region Fields
        protected Tileset mTileset;

        #region XML Docs
        /// <summary>
        /// The effect used to draw.  Shared by all instances for performance reasons
        /// </summary>
        #endregion
        private static BasicEffect mBasicEffect;
        private static AlphaTestEffect mAlphaTestEffect;

        /// <summary>
        /// The vertices used to draw the map.
        /// </summary>
        /// <remarks>
        /// Coordinate order is:
        /// 3   2
        ///
        /// 0   1
        /// </remarks>
        protected VertexPositionTexture[] mVertices;
        protected Texture2D mTexture;
        #region XML Docs
        /// <summary>
        /// The indices to draw the shape
        /// </summary>
        #endregion
        protected int[] mIndices;

        Dictionary<string, List<int>> mNamedTileOrderedIndexes = new Dictionary<string, List<int>>();

        private int mCurrentNumberOfTiles = 0;


        private SortAxis mSortAxis;

        #endregion

        #region Properties

        public List<TMXGlueLib.DataTypes.NamedValue> Properties
        {
            get;
            private set;
        } = new List<TMXGlueLib.DataTypes.NamedValue>();

        public SortAxis SortAxis
        {
            get
            {
                return mSortAxis;
            }
            set
            {
                mSortAxis = value;
            }
        }

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

        public bool ZBuffered
        {
            get;
            set;
        }

        public int QuadCount
        {
            get
            {
                return mVertices.Length / 4;
            }
        }

        public VertexPositionTexture[] Vertices
        {
            get
            {
                return mVertices;
            }
        }

        public Texture2D Texture
        {
            get
            {
                return mTexture;
            }

        }

        // Doing these properties this way lets me avoid a computational step of 1 - ParallaxMultiplier in the Update() function
        // To explain the get & set values, algebra:
        // if _parallaxMultiplier = 1 - value (set)
        // then _parallaxMultiplier - 1 = -value
        // so -(_parallaxMultiplier - 1) = value
        // thus -_parallaxMultiplier + 1 = value (get)
        private float _parallaxMultiplierX;
        public float ParallaxMultiplierX
        {
            get { return -_parallaxMultiplierX + 1; }
            set { _parallaxMultiplierX = 1 - value; }
        }

        private float _parallaxMultiplierY;
        public float ParallaxMultiplierY
        {
            get { return -_parallaxMultiplierY + 1; }
            set { _parallaxMultiplierY = 1 - value; }
        }

        #endregion

        #region Constructor / Initialization

        // this exists purely for Clone
        public MapDrawableBatch()
        {

        }

        public MapDrawableBatch(int numberOfTiles, Texture2D texture)
            : base()
        {
            if (texture == null)
                throw new ArgumentNullException("texture");

            Visible = true;
            InternalInitialize();

            mTexture = texture;
            mVertices = new VertexPositionTexture[4 * numberOfTiles];
            mIndices = new int[6 * numberOfTiles];
        }

        #region XML Docs
        /// <summary>
        /// Create and initialize all assets
        /// </summary>
        #endregion
        public MapDrawableBatch(int numberOfTiles, int textureTileDimensionWidth, int textureTileDimensionHeight, Texture2D texture)
            : base()
        {
            if (texture == null)
                throw new ArgumentNullException("texture");

            Visible = true;
            InternalInitialize();

            mTexture = texture;
            mVertices = new VertexPositionTexture[4 * numberOfTiles];
            mIndices = new int[6 * numberOfTiles];

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
            if (mBasicEffect == null)
            {
                mBasicEffect = new BasicEffect(FlatRedBallServices.GraphicsDevice);

                mBasicEffect.VertexColorEnabled = false;
                mBasicEffect.TextureEnabled = true;
            }
            if (mAlphaTestEffect == null)
            {
                mAlphaTestEffect = new AlphaTestEffect(FlatRedBallServices.GraphicsDevice);
                mAlphaTestEffect.Alpha = 1;
                mAlphaTestEffect.VertexColorEnabled = false;

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

        public void AddToManagers(Layer layer)
        {
            SpriteManager.AddToLayer(this, layer);
        }

        public static MapDrawableBatch FromScnx(string sceneFileName, string contentManagerName, bool verifySameTexturePerLayer)
        {
            // TODO: This line crashes when the path is already absolute!
            string absoluteFileName = FileManager.MakeAbsolute(sceneFileName);

            // TODO: The exception doesn't make sense when the file type is wrong.
            SceneSave saveInstance = SceneSave.FromFile(absoluteFileName);

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
                tileFileDimensionsWidth = (int)System.Math.Round((double)((s.RightTextureCoordinate - s.LeftTextureCoordinate) * texture.Width));
                tileFileDimensionsHeight = (int)System.Math.Round((double)((s.BottomTextureCoordinate - s.TopTextureCoordinate) * texture.Height));

            }


            // alas, we create the MDB 
            MapDrawableBatch mMapBatch = new MapDrawableBatch(count, tileFileDimensionsWidth, tileFileDimensionsHeight, texture);

            int lastIndexExclusive = startingIndex + count;

            for (int i = startingIndex; i < lastIndexExclusive; i++)
            {
                SpriteSave spriteSave = spriteSaveList[i];

                // We don't want objects within the IDB to have a different Z than the IDB itself
                // (if possible) because that makes the IDB behave differently when using sorting vs.
                // the zbuffer.
                const bool setZTo0 = true;
                mMapBatch.Paste(spriteSave, setZTo0);

            }



            return mMapBatch;
        }

        public MapDrawableBatch Clone()
        {
            return base.Clone<MapDrawableBatch>();
        }

        // Bring the texture coordinates in to adjust for rendering issues on dx9/ogl
        public const float CoordinateAdjustment = .00002f;

        internal static MapDrawableBatch FromReducedLayer(TMXGlueLib.DataTypes.ReducedLayerInfo reducedLayerInfo, LayeredTileMap owner, TMXGlueLib.DataTypes.ReducedTileMapInfo rtmi, string contentManagerName)
        {
            int tileDimensionWidth = reducedLayerInfo.TileWidth;
            int tileDimensionHeight = reducedLayerInfo.TileHeight;
            float quadWidth = reducedLayerInfo.TileWidth;
            float quadHeight = reducedLayerInfo.TileHeight;

            string textureName = reducedLayerInfo.Texture;


#if IOS || ANDROID

			textureName = textureName.ToLowerInvariant();

#endif

            Texture2D texture = FlatRedBallServices.Load<Texture2D>(textureName, contentManagerName);

            MapDrawableBatch toReturn = new MapDrawableBatch(reducedLayerInfo.Quads.Count, tileDimensionWidth, tileDimensionHeight, texture);

            toReturn.Name = reducedLayerInfo.Name;

            Vector3 position = new Vector3();
            Vector2 tileDimensions = new Vector2(quadWidth, quadHeight);


            IEnumerable<TMXGlueLib.DataTypes.ReducedQuadInfo> quads = null;

            if (rtmi.NumberCellsWide > rtmi.NumberCellsTall)
            {
                quads = reducedLayerInfo.Quads.OrderBy(item => item.LeftQuadCoordinate).ToList();
                toReturn.mSortAxis = SortAxis.X;
            }
            else
            {
                quads = reducedLayerInfo.Quads.OrderBy(item => item.BottomQuadCoordinate).ToList();
                toReturn.mSortAxis = SortAxis.Y;
            }

            foreach (var quad in quads)
            {
                position.X = quad.LeftQuadCoordinate;
                position.Y = quad.BottomQuadCoordinate;

                // The Z of the quad should be relative to this layer, not absolute Z values.
                // A multi-layer map will offset the individual layer Z values, the quads should have a Z of 0.
                // position.Z = reducedLayerInfo.Z;


                var textureValues = new Vector4();
                textureValues.X = CoordinateAdjustment + (float)quad.LeftTexturePixel / (float)texture.Width; // Left
                textureValues.Y = -CoordinateAdjustment + (float)(quad.LeftTexturePixel + tileDimensionWidth) / (float)texture.Width; // Right
                textureValues.Z = CoordinateAdjustment + (float)quad.TopTexturePixel / (float)texture.Height; // Top
                textureValues.W = -CoordinateAdjustment + (float)(quad.TopTexturePixel + tileDimensionHeight) / (float)texture.Height; // Bottom

                // pad before doing any rotations/flipping
                const bool pad = true;
                if (pad)
                {
                    const float amountToAdd = .0000001f;
                    textureValues.X += amountToAdd; // Left
                    textureValues.Y -= amountToAdd; // Right
                    textureValues.Z += amountToAdd; // Top
                    textureValues.W -= amountToAdd; // Bottom
                }

                if ((quad.FlipFlags & TMXGlueLib.DataTypes.ReducedQuadInfo.FlippedHorizontallyFlag) == TMXGlueLib.DataTypes.ReducedQuadInfo.FlippedHorizontallyFlag)
                {
                    var temp = textureValues.Y;
                    textureValues.Y = textureValues.X;
                    textureValues.X = temp;
                }

                if ((quad.FlipFlags & TMXGlueLib.DataTypes.ReducedQuadInfo.FlippedVerticallyFlag) == TMXGlueLib.DataTypes.ReducedQuadInfo.FlippedVerticallyFlag)
                {
                    var temp = textureValues.Z;
                    textureValues.Z = textureValues.W;
                    textureValues.W = temp;
                }

                int tileIndex = toReturn.AddTile(position, tileDimensions,
                    //quad.LeftTexturePixel, quad.TopTexturePixel, quad.LeftTexturePixel + tileDimensionWidth, quad.TopTexturePixel + tileDimensionHeight);
                    textureValues);

                if ((quad.FlipFlags & TMXGlueLib.DataTypes.ReducedQuadInfo.FlippedDiagonallyFlag) == TMXGlueLib.DataTypes.ReducedQuadInfo.FlippedDiagonallyFlag)
                {
                    toReturn.ApplyDiagonalFlip(tileIndex);
                }

                // This was moved to outside of this conversion, to support shaps
                //if (quad.QuadSpecificProperties != null)
                //{
                //    var listToAdd = quad.QuadSpecificProperties.ToList();
                //    listToAdd.Add(new NamedValue { Name = "Name", Value = quad.Name });
                //    owner.Properties.Add(quad.Name, listToAdd);
                //}


                toReturn.RegisterName(quad.Name, tileIndex);
            }

            return toReturn;
        }

        public void Paste(Sprite sprite)
        {
            Paste(sprite, false);
        }

        public int Paste(Sprite sprite, bool setZTo0)
        {
            // here we have the Sprite's X and Y in absolute coords as well as its texture coords
            // NOTE: I appended the Z coordinate for the sake of iso maps. This SHOULDN'T have an effect on the ortho maps since I believe the 
            // TMX->SCNX tool sets all z to zero.

            // The AddTile method expects the bottom-left corner
            float x = sprite.X - sprite.ScaleX;
            float y = sprite.Y - sprite.ScaleY;
            float z = sprite.Z;

            if (setZTo0)
            {
                z = 0;
            }

            float width = 2f * sprite.ScaleX; // w
            float height = 2f * sprite.ScaleY; // z

            float topTextureCoordinate = sprite.TopTextureCoordinate;
            float bottomTextureCoordinate = sprite.BottomTextureCoordinate;
            float leftTextureCoordinate = sprite.LeftTextureCoordinate;
            float rightTextureCoordinate = sprite.RightTextureCoordinate;

            int tileIndex = mCurrentNumberOfTiles;

            RegisterName(sprite.Name, tileIndex);

            // add the textured tile to our map so that we may draw it.
            return AddTile(new Vector3(x, y, z),
                new Vector2(width, height),
                new Vector4(leftTextureCoordinate, rightTextureCoordinate, topTextureCoordinate, bottomTextureCoordinate));

        }

        public void Paste(SpriteSave spriteSave)
        {
            Paste(spriteSave, false);
        }

        public int Paste(SpriteSave spriteSave, bool setZTo0)
        {
            // here we have the Sprite's X and Y in absolute coords as well as its texture coords
            // NOTE: I appended the Z coordinate for the sake of iso maps. This SHOULDN'T have an effect on the ortho maps since I believe the 
            // TMX->SCNX tool sets all z to zero.

            // The AddTile method expects the bottom-left corner
            float x = spriteSave.X - spriteSave.ScaleX;
            float y = spriteSave.Y - spriteSave.ScaleY;
            float z = spriteSave.Z;
            if (setZTo0)
            {
                z = 0;
            }

            float width = 2f * spriteSave.ScaleX; // w
            float height = 2f * spriteSave.ScaleY; // z

            float topTextureCoordinate = spriteSave.TopTextureCoordinate;
            float bottomTextureCoordinate = spriteSave.BottomTextureCoordinate;
            float leftTextureCoordinate = spriteSave.LeftTextureCoordinate;
            float rightTextureCoordinate = spriteSave.RightTextureCoordinate;

            int tileIndex = mCurrentNumberOfTiles;

            RegisterName(spriteSave.Name, tileIndex);

            // add the textured tile to our map so that we may draw it.
            return AddTile(new Vector3(x, y, z), new Vector2(width, height), new Vector4(leftTextureCoordinate, rightTextureCoordinate, topTextureCoordinate, bottomTextureCoordinate));
        }

        private static void VerifySingleTexture(List<SpriteSave> spriteSaveList, int startingIndex, int count)
        {
            // Every Sprite should either have the same texture
            if (spriteSaveList.Count != 0)
            {
                string texture = spriteSaveList[startingIndex].Texture;

                for (int i = startingIndex + 1; i < startingIndex + count; i++)
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

        // Swaps the top-right for the bottom-left verts
        public void ApplyDiagonalFlip(int orderedTileIndex)
        {
            int currentVertex = orderedTileIndex * 4; // 4 vertices per tile

            // Coords are
            // 3   2
            //
            // 0   1

            var old0 = mVertices[currentVertex + 0].TextureCoordinate;

            mVertices[currentVertex + 0].TextureCoordinate = mVertices[currentVertex + 2].TextureCoordinate;
            mVertices[currentVertex + 2].TextureCoordinate = old0;
        }

        public void RotateTextureCoordinatesCounterclockwise(int orderedTileIndex)
        {
            int currentVertex = orderedTileIndex * 4; // 4 vertices per tile

            // Coords are
            // 3   2
            //
            // 0   1

            var old3 = mVertices[currentVertex + 3].TextureCoordinate;

            mVertices[currentVertex + 3].TextureCoordinate = mVertices[currentVertex + 2].TextureCoordinate;
            mVertices[currentVertex + 2].TextureCoordinate = mVertices[currentVertex + 1].TextureCoordinate;
            mVertices[currentVertex + 1].TextureCoordinate = mVertices[currentVertex + 0].TextureCoordinate;
            mVertices[currentVertex + 0].TextureCoordinate = old3;

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
        public int AddTile(Vector3 bottomLeftPosition, Vector2 dimensions, Vector4 texture)
        {
            int toReturn = mCurrentNumberOfTiles;
            int currentVertex = mCurrentNumberOfTiles * 4;

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
            mIndices[currentIndex + 0] = currentVertex + 0;
            mIndices[currentIndex + 1] = currentVertex + 1;
            mIndices[currentIndex + 2] = currentVertex + 2;
            mIndices[currentIndex + 3] = currentVertex + 0;
            mIndices[currentIndex + 4] = currentVertex + 2;
            mIndices[currentIndex + 5] = currentVertex + 3;

            mCurrentNumberOfTiles++;

            return toReturn;
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
        public int AddTile(Vector3 bottomLeftPosition, Vector2 tileDimensions, int textureTopLeftX, int textureTopLeftY, int textureBottomRightX, int textureBottomRightY)
        {
            // Form vector4 for AddTile overload
            var textureValues = new Vector4();
            textureValues.X = (float)textureTopLeftX / (float)mTexture.Width; // Left
            textureValues.Y = (float)textureBottomRightX / (float)mTexture.Width; // Right
            textureValues.Z = (float)textureTopLeftY / (float)mTexture.Height; // Top
            textureValues.W = (float)textureBottomRightY / (float)mTexture.Height; // Bottom

            return AddTile(bottomLeftPosition, tileDimensions, textureValues);
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

            if (!AbsoluteVisible)
            {
                return;
            }
            if (mVertices.Length == 0)
            {
                return;
            }

            //////////////////End Early Out/////////////////




            int firstVertIndex;
            int lastVertIndex;
            int indexStart;
            int numberOfTriangles;
            GetRenderingIndexValues(camera, out firstVertIndex, out lastVertIndex, out indexStart, out numberOfTriangles);

            if (numberOfTriangles != 0)
            {
                TextureAddressMode oldTextureAddressMode;
                Effect effectTouse = PrepareRenderingStates(camera, out oldTextureAddressMode);

                foreach (EffectPass pass in effectTouse.CurrentTechnique.Passes)
                {
                    // Start each pass

                    pass.Apply();

                    int numberVertsToDraw = lastVertIndex - firstVertIndex;

                    // Right now this uses the (slower) DrawUserIndexedPrimitives
                    // It could use DrawIndexedPrimitives instead for much faster performance,
                    // but to do that we'd have to keep VB's around and make sure to re-create them
                    // whenever the graphics device is lost.  
                    FlatRedBallServices.GraphicsDevice.DrawUserIndexedPrimitives<VertexPositionTexture>(
                        PrimitiveType.TriangleList,
                        mVertices,
                        firstVertIndex,
                        numberVertsToDraw,
                        mIndices,
                        indexStart, numberOfTriangles);

                }

                Renderer.TextureAddressMode = oldTextureAddressMode;
                if (ZBuffered)
                {
                    FlatRedBallServices.GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
                }
            }

        }

        private Effect PrepareRenderingStates(Camera camera, out TextureAddressMode oldTextureAddressMode)
        {
            // Set graphics states
            FlatRedBallServices.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            FlatRedBall.Graphics.Renderer.BlendOperation = BlendOperation.Regular;

            Effect effectTouse = null;

            if (ZBuffered)
            {
                FlatRedBallServices.GraphicsDevice.DepthStencilState = DepthStencilState.Default;
                camera.SetDeviceViewAndProjection(mAlphaTestEffect, false);

                mAlphaTestEffect.World = Matrix.CreateScale(RenderingScale) * base.TransformationMatrix;
                mAlphaTestEffect.Texture = mTexture;

                effectTouse = mAlphaTestEffect;
            }
            else
            {
                camera.SetDeviceViewAndProjection(mBasicEffect, false);

                mBasicEffect.World = Matrix.CreateScale(RenderingScale) * base.TransformationMatrix;
                mBasicEffect.Texture = mTexture;
                effectTouse = mBasicEffect;
            }



            // We won't need to use any other kind of texture
            // address mode besides clamp, and clamp is required
            // on the "Reach" profile when the texture is not power
            // of two.  Let's set it to clamp here so that we don't crash
            // on non-power-of-two textures.
            oldTextureAddressMode = Renderer.TextureAddressMode;
            Renderer.TextureAddressMode = TextureAddressMode.Clamp;




            return effectTouse;
        }

        private void GetRenderingIndexValues(Camera camera, out int firstVertIndex, out int lastVertIndex, out int indexStart, out int numberOfTriangles)
        {

            firstVertIndex = 0;

            lastVertIndex = mVertices.Length;


            float tileWidth = mVertices[1].Position.X - mVertices[0].Position.X;

            if (mSortAxis == SortAxis.X)
            {
                float minX = camera.AbsoluteLeftXEdgeAt(this.Z);
                float maxX = camera.AbsoluteRightXEdgeAt(this.Z);

                minX -= this.X;
                maxX -= this.X;

                firstVertIndex = GetFirstAfterX(mVertices, minX - tileWidth);
                lastVertIndex = GetFirstAfterX(mVertices, maxX) + 4;
            }
            else if (mSortAxis == SortAxis.Y)
            {
                float minY = camera.AbsoluteBottomYEdgeAt(this.Z);
                float maxY = camera.AbsoluteTopYEdgeAt(this.Z);

                minY -= this.Y;
                maxY -= this.Y;

                firstVertIndex = GetFirstAfterY(mVertices, minY - tileWidth);
                lastVertIndex = GetFirstAfterY(mVertices, maxY) + 4;
            }

            lastVertIndex = System.Math.Min(lastVertIndex, mVertices.Length);

            indexStart = 0;// (firstVertIndex * 3) / 2;
            int indexEndExclusive = ((lastVertIndex - firstVertIndex) * 3) / 2;

            numberOfTriangles = (indexEndExclusive - indexStart) / 3;
        }

        public static int GetFirstAfterX(VertexPositionTexture[] list, float xGreaterThan)
        {
            int min = 0;
            int originalMax = list.Length / 4;
            int max = list.Length / 4;

            int mid = (max + min) / 2;

            while (min < max)
            {
                mid = (max + min) / 2;
                float midItem = list[mid * 4].Position.X;

                if (midItem > xGreaterThan)
                {
                    // Is this the last one?
                    // Not sure why this is here, because if we have just 2 items,
                    // this will always return a value of 1 instead 
                    //if (mid * 4 + 4 >= list.Length)
                    //{
                    //    return mid * 4;
                    //}

                    // did we find it?
                    if (mid > 0 && list[(mid - 1) * 4].Position.X <= xGreaterThan)
                    {
                        return mid * 4;
                    }
                    else
                    {
                        max = mid - 1;
                    }
                }
                else if (midItem <= xGreaterThan)
                {
                    if (mid == 0)
                    {
                        return mid * 4;
                    }
                    else if (mid < originalMax - 1 && list[(mid + 1) * 4].Position.X > xGreaterThan)
                    {
                        return (mid + 1) * 4;
                    }
                    else
                    {
                        min = mid + 1;
                    }
                }
            }
            if (min == 0)
            {
                return 0;
            }
            else
            {
                return list.Length;
            }
        }

        public static int GetFirstAfterY(VertexPositionTexture[] list, float yGreaterThan)
        {
            int min = 0;
            int originalMax = list.Length / 4;
            int max = list.Length / 4;

            int mid = (max + min) / 2;

            while (min < max)
            {
                mid = (max + min) / 2;
                float midItem = list[mid * 4].Position.Y;

                if (midItem > yGreaterThan)
                {
                    // Is this the last one?
                    // See comment in GetFirstAfterX
                    //if (mid * 4 + 4 >= list.Length)
                    //{
                    //    return mid * 4;
                    //}

                    // did we find it?
                    if (mid > 0 && list[(mid - 1) * 4].Position.Y <= yGreaterThan)
                    {
                        return mid * 4;
                    }
                    else
                    {
                        max = mid - 1;
                    }
                }
                else if (midItem <= yGreaterThan)
                {
                    if (mid == 0)
                    {
                        return mid * 4;
                    }
                    else if (mid < originalMax - 1 && list[(mid + 1) * 4].Position.Y > yGreaterThan)
                    {
                        return (mid + 1) * 4;
                    }
                    else
                    {
                        min = mid + 1;
                    }
                }
            }
            if (min == 0)
            {
                return 0;
            }
            else
            {
                return list.Length;
            }
        }
        #region XML Docs
        /// <summary>
        /// Here we update our batch - but this batch doesn't
        /// need to be updated
        /// </summary>
        #endregion
        public void Update()
        {
            float leftView = Camera.Main.AbsoluteLeftXEdgeAt(0);
            float topView = Camera.Main.AbsoluteTopYEdgeAt(0);

            float cameraOffsetX = leftView - CameraOriginX;
            float cameraOffsetY = topView - CameraOriginY;

            this.RelativeX = cameraOffsetX * _parallaxMultiplierX;
            this.RelativeY = cameraOffsetY * _parallaxMultiplierY;

            this.TimedActivity(TimeManager.SecondDifference, TimeManager.SecondDifferenceSquaredDividedByTwo, TimeManager.LastSecondDifference);

            // The MapDrawableBatch may be attached to a LayeredTileMap (the container of all layers)
            // If so, the player may move the LayeredTileMap and expect all contained layers to move along
            // with it.  To allow this, we need to have dependencies updated.  We'll do this by simply updating
            // dependencies here, although I don't know at this point if there's a better way - like if we should
            // be adding this to the SpriteManager's PositionedObjectList.  This is an improvement so we'll do it for
            // now and revisit this in case there's a problem in the future.
            this.UpdateDependencies(TimeManager.CurrentTime);
        }

        // TODO: I would like to somehow make this a property on the LayeredTileMap, but right now it is easier to put them here
        public float CameraOriginY { get; set; }
        public float CameraOriginX { get; set; }

        IVisible IVisible.Parent
        {
            get
            {
                return this.Parent as IVisible;
            }
        }

        public bool AbsoluteVisible
        {
            get
            {
                if (this.Visible)
                {
                    var parentAsIVisible = this.Parent as IVisible;

                    if (parentAsIVisible == null || IgnoresParentVisibility)
                    {
                        return true;
                    }
                    else
                    {
                        // this is true, so return if the parent is visible:
                        return parentAsIVisible.AbsoluteVisible;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IgnoresParentVisibility
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// Don't call this, instead call SpriteManager.RemoveDrawableBatch
        /// </summary>
        #endregion
        public void Destroy()
        {
            this.RemoveSelfFromListsBelongingTo();
        }


        public void MergeOntoThis(IEnumerable<MapDrawableBatch> mapDrawableBatches)
        {
            int quadsToAdd = 0;
            int quadsOnThis = QuadCount;
            foreach (var mdb in mapDrawableBatches)
            {
                quadsToAdd += mdb.QuadCount;
            }


            int totalNumberOfVerts = 4 * (this.QuadCount + quadsToAdd);
            int totalNumberOfIndexes = 6 * (this.QuadCount + quadsToAdd);

            var oldVerts = mVertices;
            var oldIndexes = mIndices;

            mVertices = new VertexPositionTexture[totalNumberOfVerts];
            mIndices = new int[totalNumberOfIndexes];

            oldVerts.CopyTo(mVertices, 0);
            oldIndexes.CopyTo(mIndices, 0);

            int currentQuadIndex = quadsOnThis;


            int index = 0;
            foreach (var mdb in mapDrawableBatches)
            {
                int startVert = currentQuadIndex * 4;
                int startIndex = currentQuadIndex * 6;
                int numberOfIndices = mdb.mIndices.Length;
                int numberOfNewVertices = mdb.mVertices.Length;

                mdb.mVertices.CopyTo(mVertices, startVert);
                mdb.mIndices.CopyTo(mIndices, startIndex);


                for (int i = startIndex; i < startIndex + numberOfIndices; i++)
                {
                    mIndices[i] += startVert;
                }

                for (int i = startVert; i < startVert + numberOfNewVertices; i++)
                {
                    mVertices[i].Position.Z += index + 1;
                }

                foreach (var kvp in mdb.mNamedTileOrderedIndexes)
                {
                    string key = kvp.Key;

                    List<int> toAddTo;

                    if (mNamedTileOrderedIndexes.ContainsKey(key))
                    {
                        toAddTo = mNamedTileOrderedIndexes[key];
                    }
                    else
                    {
                        toAddTo = new List<int>();
                        mNamedTileOrderedIndexes[key] = toAddTo;
                    }

                    foreach (var namedIndex in kvp.Value)
                    {
                        toAddTo.Add(namedIndex + currentQuadIndex);
                    }
                }


                currentQuadIndex += mdb.QuadCount;
                index++;
            }
        }


        public void RemoveQuads(IEnumerable<int> quadIndexes)
        {
            var vertList = mVertices.ToList();
            // Reverse - go from biggest to smallest
            foreach (var indexToRemove in quadIndexes.Distinct().OrderBy(item => -item))
            {
                // and go from biggest to smallest here too
                vertList.RemoveAt(indexToRemove * 4 + 3);
                vertList.RemoveAt(indexToRemove * 4 + 2);
                vertList.RemoveAt(indexToRemove * 4 + 1);
                vertList.RemoveAt(indexToRemove * 4 + 0);
            }

            mVertices = vertList.ToArray();

            // The mNamedTileOrderedIndexes is a dictionary that stores which indexes are stored
            // with which tiles.  For example, the key in the dictionary may be "Lava", in which case
            // the value is the indexes of the tiles that use the Lava tile.
            // If we do end up removing any quads, then all following quads will shift, so we need to
            // adjust the indexes so the naming works correctly

            List<int> orderedInts = quadIndexes.OrderBy(item => item).Distinct().ToList();
            int numberOfRemovals = 0;
            foreach (var kvp in mNamedTileOrderedIndexes)
            {
                var ints = kvp.Value;

                numberOfRemovals = 0;

                for (int i = 0; i < ints.Count; i++)
                {
                    // Nothing left to test, so subtract and move on....
                    if (numberOfRemovals == orderedInts.Count)
                    {
                        ints[i] -= numberOfRemovals;
                    }
                    else if (ints[i] == orderedInts[numberOfRemovals])
                    {
                        ints.Clear();
                        break;
                    }
                    else if (ints[i] < orderedInts[numberOfRemovals])
                    {
                        ints[i] -= numberOfRemovals;
                    }
                    else
                    {
                        while (numberOfRemovals < orderedInts.Count && ints[i] > orderedInts[numberOfRemovals])
                        {
                            numberOfRemovals++;
                        }
                        if (numberOfRemovals < orderedInts.Count && ints[i] == orderedInts[numberOfRemovals])
                        {
                            ints.Clear();
                            break;
                        }

                        ints[i] -= numberOfRemovals;
                    }
                }
            }
        }

        #endregion
    }




    public static class MapDrawableBatchExtensionMethods
    {


    }


}
