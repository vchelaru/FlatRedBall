using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math;
using Math = System.Math;
using FlatRedBall.Math.Geometry;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Color = Microsoft.Xna.Framework.Color;
namespace FlatRedBall.AI.Pathfinding
{
    #region Enums

    public enum DirectionalType
    {
        Four,
        Eight
    }

    #endregion

    /// <summary>
    /// A node network optimized for tile-based pathfinding.
    /// </summary>
    public class TileNodeNetwork : NodeNetwork
    {
        #region Fields

        const int PropertyIndexSize = 32;

        PositionedObjectList<Circle> mOccupiedTileCircles = new PositionedObjectList<Circle>();

        PositionedNode[][] mTiledNodes;

        List<OccupiedTile> mOccupiedTiles = new List<OccupiedTile>();

        int mNumberOfXTiles;
        int mNumberOfYTiles;
        DirectionalType mDirectionalType;

        float mXSeed;
        float mYSeed;
        float mGridSpacing;

        float[] mCosts;

        #endregion

        #region Properties

        public float OccupiedCircleRadius
        {
            get;
            set;
        }

        public float GridSpacing => mGridSpacing;

        public float NumberOfXTiles => mNumberOfXTiles;
        public float NumberOfYTiles => mNumberOfYTiles;

        #endregion

        #region Methods

        #region Constructor

        private TileNodeNetwork()
        {

        }

        /// <summary>
        /// Creates a new, empty TileNodeNetwork matching the arguments.
        /// </summary>
        /// <param name="xOrigin">The X position of the left-most nodes. This, along with the ySeed, define the bottom-left of the node network. 
        /// For tile maps this should be the center X of the first tile column (typically TileWidth / 2).</param>
        /// <param name="yOrigin">The y position of the bottom-most nodes. This, along with xSeed, define the bottom-left of the node network. 
        /// For tile maps this should be the center Y of the bottom tile row. 
        /// If the top-left of the map is at 0,0, then this value would be (-EntireMapHeight + TileHeight/2)</param>
        /// <param name="gridSpacing">The X and Y distance between each node. That is, the X distance between two adjacent nodes (assumed to be equal to the Y distance). For a tile map this will equal the width of a tile.</param>
        /// <param name="numberOfXTiles">The number of nodes vertically.</param>
        /// <param name="numberOfYTiles">The number of nodes horizontally.</param>
        /// <param name="directionalType">Whether to create a Four-way or Eight-way node network. Eight creates diagonal links, enabling diagonal movement when following the node network.</param>
        public TileNodeNetwork(float xOrigin, float yOrigin, float gridSpacing, int numberOfXTiles,
            int numberOfYTiles, DirectionalType directionalType)
        {
            mCosts = new float[PropertyIndexSize]; // Maybe expand this to 64 if we ever move to a long bit field?

            OccupiedCircleRadius = .5f;
            mTiledNodes = new PositionedNode[numberOfXTiles][];
            mNumberOfXTiles = numberOfXTiles;
            mNumberOfYTiles = numberOfYTiles;
            mDirectionalType = directionalType;
            mXSeed = xOrigin;
            mYSeed = yOrigin;
            mGridSpacing = gridSpacing;

            // Do an initial loop to create the arrays so that
            // linking works properly
            for (int x = 0; x < numberOfXTiles; x++)
            {
                mTiledNodes[x] = new PositionedNode[numberOfYTiles];
            }

        }

        #endregion

        #region Public Methods

        public override void Shift(Vector3 shiftVector)
        {
            mXSeed += shiftVector.X;
            mYSeed += shiftVector.Y;
            
            for (int i = 0; i < this.Nodes.Count; i++)
            {
                this.Nodes[i].Position += shiftVector;
            }
        }

        /// <summary>
        /// Adds a new node at the world X and Y location. Internally the world coordinates are converted to x,y indexes and the node is stored
        /// in a 2D grid.
        /// </summary>
        /// <param name="worldX">The world X units.</param>
        /// <param name="worldY">The world Y units.</param>
        /// <returns>The newly-created PositionedNode</returns>
        public PositionedNode AddAndLinkTiledNodeWorld(float worldX, float worldY)
        {
            int x;
            int y;

            WorldToIndex(worldX, worldY, out x, out y);

            return AddAndLinkTiledNode(x, y, mDirectionalType);
        }

        /// <summary>
        /// Adds a new node at the tile index x, y.
        /// </summary>
        /// <param name="x">The x index of the tile.</param>
        /// <param name="y">The y index of the tile.</param>
        /// <returns>The newly-created PositionedNode</returns>
        public PositionedNode AddAndLinkTiledNode(int x, int y)
        {
            return AddAndLinkTiledNode(x, y, mDirectionalType);
        }

        /// <summary>
        /// Creates a new node at index x, y and links the node to adjacent nodes given the TileNodeNetwork's GridSpacing
        /// </summary>
        /// <param name="x">The X index</param>
        /// <param name="y">The Y index</param>
        /// <param name="directionalType">The DirectionalType, which is either Four or Eight way. Four will link in cardinal directions, while Eight will also link diagonally.</param>
        /// <returns></returns>
        public PositionedNode AddAndLinkTiledNode(int x, int y, DirectionalType directionalType)
        {
            PositionedNode node = null;

            if (mTiledNodes[x][y] != null)
            {            
                node = mTiledNodes[x][y];
            }
            else
            {
                node = base.AddNode();
                mTiledNodes[x][y] = node;

            }

            node.Position.X = mXSeed + x * mGridSpacing;
            node.Position.Y = mYSeed + y * mGridSpacing;

            // Now attach to the adjacent tiles
            AttachNodeToNodeAtIndex(node, x, y + 1);
            AttachNodeToNodeAtIndex(node, x + 1, y);
            AttachNodeToNodeAtIndex(node, x, y - 1);
            AttachNodeToNodeAtIndex(node, x - 1, y);
            if (directionalType == DirectionalType.Eight)
            {
                AttachNodeToNodeAtIndex(node, x - 1, y + 1);
                AttachNodeToNodeAtIndex(node, x + 1, y + 1);
                AttachNodeToNodeAtIndex(node, x + 1, y - 1);
                AttachNodeToNodeAtIndex(node, x - 1, y - 1);
            }

            return node;
        }

        /// <summary>
        /// Adds an already-positioned node to the node network.
        /// </summary>
        /// <remarks>
        /// This method adds a node to the base nodes list, as well as to the 
        /// 2D array of node. The position of the node is used to add the node, so it
        /// should already be in its final position prior to calling this method.
        /// </remarks>
        /// <param name="nodeToAdd">The node to add.</param>
        public override void AddNode(PositionedNode nodeToAdd)
        {
            int xIndex;
            int yIndex;

            WorldToIndex(nodeToAdd.X, nodeToAdd.Y, out xIndex, out yIndex);

            if(mTiledNodes[xIndex][yIndex] != null)
            {
                throw new InvalidOperationException($"There is already a node at index ({xIndex}, {yIndex})");
            }

            mTiledNodes[xIndex][yIndex] = nodeToAdd;

            base.AddNode(nodeToAdd);
        }

        /// <summary>
        /// Populates every possible space on the grid with a node and creates links betwen adjacent links. Diagonal links are created only if
        /// the DirectionalType is set to DirectionalType.Eight.
        /// </summary>
        public void FillCompletely()
        {
            for (int x = 0; x < mNumberOfXTiles; x++)
            {
                for (int y = 0; y < mNumberOfYTiles; y++)
                {
                    PositionedNode newNode = AddAndLinkTiledNode(x, y, mDirectionalType);
                }
            }
        }

        public void FillNodesOverlapping(AxisAlignedRectangle rectangle)
        {
            WorldToIndex(rectangle.Left, rectangle.Bottom, out int startX, out int startY);
            WorldToIndex(rectangle.Right, rectangle.Top, out int endXInclusive, out int endYInclusive);

            for (int y = startY; y <= endYInclusive; y++)
            {
                for (int x = startX; x <= endXInclusive; x++)
                {
                    var node = mTiledNodes[x][y];

                    if(node == null)
                    {
                        AddAndLinkTiledNode(x, y);
                    }

                }
            }
        }

        public void EliminateCutCorners()
        {
            for (int x = 0; x < this.mNumberOfXTiles; x++)
            {
                for (int y = 0; y < this.mNumberOfYTiles; y++)
                {
                    EliminateCutCornersForNodeAtIndex(x, y);

                }
            }
        }

        public void EliminateCutCornersForNodeAtIndex(int x, int y)
        {
            PositionedNode nodeAtIndex = TiledNodeAt(x, y);

            if (nodeAtIndex == null)
            {
                return;
            }

            PositionedNode nodeAL = TiledNodeAt(x - 1, y + 1);
            PositionedNode nodeA  = TiledNodeAt(x    , y + 1);
            PositionedNode nodeAR = TiledNodeAt(x + 1, y + 1);
            PositionedNode nodeR  = TiledNodeAt(x + 1, y    );
            PositionedNode nodeBR = TiledNodeAt(x + 1, y - 1);
            PositionedNode nodeB  = TiledNodeAt(x    , y - 1);
            PositionedNode nodeBL = TiledNodeAt(x - 1, y - 1);
            PositionedNode nodeL  = TiledNodeAt(x - 1, y    );

            if (nodeAL != null && nodeAtIndex.IsLinkedTo(nodeAL))
            {
                if (nodeA == null || nodeL == null)
                {
                    nodeAtIndex.BreakLinkBetween(nodeAL);
                }
            }
            if (nodeAR != null && nodeAtIndex.IsLinkedTo(nodeAR))
            {
                if (nodeA == null || nodeR == null)
                {
                    nodeAtIndex.BreakLinkBetween(nodeAR);
                }

            }
            if (nodeBR != null && nodeAtIndex.IsLinkedTo(nodeBR))
            {
                if (nodeB == null || nodeR == null)
                {
                    nodeAtIndex.BreakLinkBetween(nodeBR);
                }
            }
            if (nodeBL != null && nodeAtIndex.IsLinkedTo(nodeBL))
            {
                if (nodeB == null || nodeL == null)
                {
                    nodeAtIndex.BreakLinkBetween(nodeBL);
                }
            }
        }

        
        public PositionedNode GetClosestNodeTo(float x, float y)
        {
#if DEBUG
            if (float.IsNaN(x))
            {
                throw new ArgumentException("x value is NaN");
            }
            if(float.IsNaN(y))
            {
                throw new ArgumentException("y value is NaN");
            }
#endif
            int xTilesFromSeed = MathFunctions.RoundToInt((x - mXSeed) / mGridSpacing);
            int yTilesFromSeed = MathFunctions.RoundToInt((y - mYSeed) / mGridSpacing);

            xTilesFromSeed = System.Math.Max(0, xTilesFromSeed);
            xTilesFromSeed = System.Math.Min(xTilesFromSeed, mNumberOfXTiles - 1);

            yTilesFromSeed = System.Math.Max(0, yTilesFromSeed);
            yTilesFromSeed = System.Math.Min(yTilesFromSeed, mNumberOfYTiles - 1);

            PositionedNode nodeToReturn = TiledNodeAt(xTilesFromSeed, yTilesFromSeed);

            if (nodeToReturn == null)
            {
                // Well, we tried to be efficient here, but it didn't work out, so
                // let's use our slower Base functionality to get the node that we should return
                Vector3 vector3 = new Vector3(x, y, 0);
                nodeToReturn = base.GetClosestNodeTo(ref vector3);
            }



            return nodeToReturn;
        }

        public override PositionedNode GetClosestNodeTo(ref Microsoft.Xna.Framework.Vector3 position)
        {
            return GetClosestNodeTo(position.X, position.Y);

        }

        public PositionedNode GetClosestUnoccupiedNodeTo(ref Microsoft.Xna.Framework.Vector3 targetPosition, ref Microsoft.Xna.Framework.Vector3 startPosition)
        {
            return GetClosestUnoccupiedNodeTo(ref targetPosition, ref startPosition, false);
        }
        public PositionedNode GetClosestUnoccupiedNodeTo(ref Microsoft.Xna.Framework.Vector3 targetPosition, ref Microsoft.Xna.Framework.Vector3 startPosition, bool ignoreCorners)
        {
            PositionedNode nodeToReturn = null;
            int xTile, yTile;
            WorldToIndex(targetPosition.X, targetPosition.Y, out xTile, out yTile);
            
            if (IsTileOccupied(xTile, yTile) == false)
                nodeToReturn = TiledNodeAt(xTile, yTile);
            
            if (nodeToReturn == null)
            {
                int startXTile, startYTile, xTileCheck, yTileCheck, deltaX, deltaY;
                WorldToIndex(startPosition.X, startPosition.Y, out startXTile, out startYTile);
                float shortestDistanceSquared = 999;
                int finalTileX = -1, finalTileY = -1;

                //Get the "target" Node
                PositionedNode node = TiledNodeAt(xTile, yTile);
                PositionedNode startNode = TiledNodeAt(startXTile, startYTile);
                PositionedNode checkNode;
                if (node != null && startNode != null)
                {
                    for (int i = 0; i < node.Links.Count; i++)
                    {
                        //skip any tile I am already on...
                        checkNode = node.Links[i].NodeLinkingTo;
                        if (checkNode == startNode)
                            continue;
                        WorldToIndex(checkNode.X, checkNode.Y, out xTileCheck, out yTileCheck);

                        if (IsTileOccupied(xTileCheck, yTileCheck) == false)
                        {
                            deltaX = xTileCheck - xTile;
                            deltaY = yTileCheck - yTile;

                            if (ignoreCorners == false || (ignoreCorners == true && (deltaX == 0 || deltaY == 0)))
                            {
                                float distanceFromStartSquared = ((xTileCheck - startXTile) * (xTileCheck - startXTile)) + ((yTileCheck - startYTile) * (yTileCheck - startYTile));
                                if (distanceFromStartSquared < shortestDistanceSquared)
                                {
                                    shortestDistanceSquared = distanceFromStartSquared;
                                    finalTileX = xTileCheck;
                                    finalTileY = yTileCheck;
                                }
                            }
                        }
                    }

                    if (finalTileX != -1 && finalTileY != -1)
                    {
                        nodeToReturn = TiledNodeAt(finalTileX, finalTileY);
                    }
                }
            }
            
            return nodeToReturn;
        }
        public bool AreAdjacentTiles(ref Microsoft.Xna.Framework.Vector3 targetPosition, ref Microsoft.Xna.Framework.Vector3 startPosition)
        {
            return AreAdjacentTiles(ref targetPosition, ref startPosition, false);
        }
        public bool AreAdjacentTiles(ref Microsoft.Xna.Framework.Vector3 targetPosition, ref Microsoft.Xna.Framework.Vector3 startPosition, bool ignoreCorners)
        {
            bool areAdjacent = false;

            int xTileTarget, yTileTarget, xTileStart, yTileStart, deltaX, deltaY;
            WorldToIndex(targetPosition.X, targetPosition.Y, out xTileTarget, out yTileTarget);
            WorldToIndex(startPosition.X, startPosition.Y, out xTileStart, out yTileStart);
            deltaX = xTileTarget - xTileStart;
            deltaY = yTileTarget - yTileStart;

            if (ignoreCorners == false || (ignoreCorners == true && (deltaX == 0 || deltaY == 0)))
            {
                PositionedNode targetNode = TiledNodeAt(xTileTarget, yTileTarget);
                PositionedNode startNode = TiledNodeAt(xTileStart, yTileStart);
                if (targetNode != null && startNode != null)
                {
                    for (int i = 0; i < targetNode.Links.Count; i++)
                    {
                        if (targetNode.Links[i].NodeLinkingTo == startNode)
                        {
                            areAdjacent = true;
                            break;
                        }
                    }
                }
            }

            return areAdjacent;
        }

        public Vector2 GetOccupiedTileLocation(object occupier)
        {
            foreach (OccupiedTile occupiedTile in mOccupiedTiles)
            {
                if (occupiedTile.Occupier == occupier)
                {
                    float worldX;
                    float worldY;

                    IndexToWorld(occupiedTile.X, occupiedTile.Y, out worldX, out worldY);
                    
                    return new Vector2(worldX, worldY);
                }
            }

            return new Vector2(float.NaN, float.NaN);
        }

        public bool IsTileOccupied(int x, int y)
        {
            foreach (OccupiedTile occupiedTile in mOccupiedTiles)
            {
                if (occupiedTile.X == x && occupiedTile.Y == y)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsTileOccupied(int x, int y, out object occupier)
        {
            foreach (OccupiedTile occupiedTile in mOccupiedTiles)
            {
                if (occupiedTile.X == x && occupiedTile.Y == y)
                {
                    occupier = occupiedTile.Occupier;
                    return true;
                }
            }
            occupier = null;
            return false;
        }

        public bool IsTileOccupiedWorld(float worldX, float worldY)
        {
            int xIndex;
            int yIndex;

            WorldToIndex(worldX, worldY, out xIndex, out yIndex);

            return IsTileOccupied(xIndex, yIndex);
        }

        public bool IsTileOccupiedWorld(float worldX, float worldY, out object occupier)
        {
            int xIndex;
            int yIndex;

            WorldToIndex(worldX, worldY, out xIndex, out yIndex);

            return IsTileOccupied(xIndex, yIndex, out occupier);

        }

        public void OccupyTile(int x, int y)
        {
            OccupyTile(x, y, null);
        }

        public void OccupyTile(int x, int y, object occupier)
        {
#if DEBUG
            object objectAlreadyOccupying = GetTileOccupier(x, y);

            if (objectAlreadyOccupying != null)
            {
                throw new InvalidOperationException("The tile at " + x + ", " + y + " is already occupied by " +
                    objectAlreadyOccupying.ToString());
            }
#endif
            OccupiedTile occupiedTile = new OccupiedTile();
            occupiedTile.X = x;
            occupiedTile.Y = y;
            occupiedTile.Occupier = occupier;

            mOccupiedTiles.Add(occupiedTile);

        }

        public void OccupyTileWorld(float worldX, float worldY)
        {
            int xIndex;
            int yIndex;

            WorldToIndex(worldX, worldY, out xIndex, out yIndex);

            OccupyTile(xIndex, yIndex, null);            
        }

        public void OccupyTileWorld(float worldX, float worldY, object occupier)
        {
            int xIndex;
            int yIndex;

            WorldToIndex(worldX, worldY, out xIndex, out yIndex);

            OccupyTile(xIndex, yIndex, occupier);
        }

        public object GetOccupier(float worldX, float worldY)
        {
            object occupier = null;
            
            IsTileOccupiedWorld(worldX, worldY, out occupier);

            return occupier;
        }

        public void RecalculateCostsForCostIndex(int costIndex)
        {
            int shiftedCost = (1 << costIndex);

            foreach (PositionedNode positionedNode in mNodes)
            {
                if ((positionedNode.PropertyField & shiftedCost) == shiftedCost)
                {
                    UpdateNodeAccordingToCosts(positionedNode);

                }
            }
        }

        /// <summary>
        /// Removes all nodes which overlap the argument AxisAlignedRectangle. Note that the overlap check tests 
        /// the entire cell of the TileNodeNetwork rather than strict overlap. Therefore, nodes may be removed even though
        /// the Position of the node is not inside of the rectangle. 
        /// </summary>
        /// <param name="rectangle"></param>
        public void RemoveNodesOverlapping(AxisAlignedRectangle rectangle)
        {
            WorldToIndex(rectangle.Left, rectangle.Bottom, out int startX, out int startY);
            WorldToIndex(rectangle.Right, rectangle.Top, out int endXInclusive, out int endYInclusive);

            for(int y = startY; y <= endYInclusive; y++)
            {
                for(int x = startX; x <= endXInclusive; x++)
                {
                    var node = mTiledNodes[x][y];
                    if(node != null)
                    {
                        Remove(node);
                    }
                }
            }
        }

        public void RemoveNodesOverlapping(Polygon polygon, float minDistance = 0)
        {
            var left = polygon.X - polygon.BoundingRadius;
            var right = polygon.X + polygon.BoundingRadius;

            var top = polygon.Y + polygon.BoundingRadius;
            var bottom = polygon.Y - polygon.BoundingRadius;

            WorldToIndex(left, bottom, out int startX, out int startY);
            WorldToIndex(right, top, out int endXInclusive, out int endYInclusive);

            for (int y = startY; y <= endYInclusive; y++)
            {
                for (int x = startX; x <= endXInclusive; x++)
                {
                    var node = mTiledNodes[x][y];
                    if (node != null)
                    {
                        if(polygon.IsPointInside(ref node.Position))
                        {

                            Remove(node);
                        }
                        else if(minDistance > 0)
                        {
                            var distance = polygon.VectorFrom(node.Position.X, node.Position.Y).Length();

                            if(distance < minDistance)
                            {
                                Remove(node);
                            }
                        }
                    }
                }
            }
        }

        public void SetCosts(params float[] costs)
        {
            for (int i = 0; i < costs.Length; i++)
            {
                mCosts[i] = costs[i];
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public PositionedNode TiledNodeAtWorld(float xWorld, float yWorld)
        {
            int xIndex;
            int yIndex;

            xIndex = MathFunctions.RoundToInt((xWorld - mXSeed) / mGridSpacing);
            yIndex = MathFunctions.RoundToInt((yWorld - mYSeed) / mGridSpacing);

            // Seems like this code checks the indexes and clamps them inward, but then down below we do null checks
            // I think we should make this return null if out of bounds
            //xIndex = System.Math.Max(0, xIndex);
            //xIndex = System.Math.Min(xIndex, mNumberOfXTiles - 1);

            //yIndex = System.Math.Max(0, yIndex);
            //yIndex = System.Math.Min(yIndex, mNumberOfYTiles - 1);
            
            if (xIndex < 0 || xIndex >= mNumberOfXTiles || yIndex < 0 || yIndex >= mNumberOfYTiles)
            {
                return null;
            }
            else
            {
                return mTiledNodes[xIndex][yIndex];
            }
        }

        public PositionedNode TiledNodeAt(int x, int y)
        {
            if (x < 0 || x >= mNumberOfXTiles || y < 0 || y >= mNumberOfYTiles)
            {
                return null;
            }
            else
            {
                return mTiledNodes[x][y];
            }
        }

        public RectangleNodeNetwork ToRectangleNodeNetwork(Axis stripAxis)
        {
            var nodeNetwork = new RectangleNodeNetwork();
            nodeNetwork.StripAxis = stripAxis;
            RectangleNode currentNode = null;

            float halfDimension = mGridSpacing / 2.0f;

            if (stripAxis == Axis.X)
            {
                float startX = 0;
                for (int y = 0; y < mNumberOfYTiles; y++)
                {
                    for(int x = 0; x < mNumberOfXTiles; x++)
                    {
                        var nodeAtXY = TiledNodeAt(x, y);
                        if (nodeAtXY != null)
                        {
                            if(currentNode == null)
                            {
                                currentNode = new RectangleNode();
                                
                                currentNode.Y = nodeAtXY.Y;
                                currentNode.Height = mGridSpacing;

                                startX = nodeAtXY.X - halfDimension;
                                
                                nodeNetwork.AddNode(currentNode);
                            }
                        }
                        else if(nodeAtXY == null && currentNode != null)
                        {
                            var endX = x * mGridSpacing;
                            currentNode.X = (startX + endX) / 2.0f;
                            currentNode.Width = endX - startX;
                            currentNode = null;
                        }
                    }
                    if(currentNode != null)
                    {
                        // got to the end of the row:
                        var endX = mGridSpacing * mNumberOfXTiles;
                        currentNode.X = (startX + endX) / 2.0f;
                        currentNode.Width = endX - startX;
                        currentNode = null;
                    }
                }
            }
            else // Axis.Y
            {

            }

            return nodeNetwork;
        }

        public void Unoccupy(int x, int y)
        {
            for (int i = mOccupiedTiles.Count - 1; i > -1; i--)
            {
                OccupiedTile occupiedTile = mOccupiedTiles[i];

                if (occupiedTile.X == x && occupiedTile.Y == y)
                {
                    mOccupiedTiles.RemoveAt(i);
                    break;
                }
            }
        }

        public void Unoccupy(object occupier)
        {
            for (int i = mOccupiedTiles.Count - 1; i > -1; i--)
            {
                OccupiedTile occupiedTile = mOccupiedTiles[i];

                if (occupiedTile.Occupier == occupier)
                {
                    mOccupiedTiles.RemoveAt(i);

                    // Don't do a break here because
                    // one occupier could occupy multiple
                    // tiles.
                    // break;
                }
            }
        }

        public void UpdateNodeAccordingToCosts(PositionedNode node)
        {
            float multiplierForThisNode = 1;
            for (int propertyIndex = 0; propertyIndex < PropertyIndexSize; propertyIndex++)
            {
                int shiftedValue = 1 << propertyIndex;

                if ((node.PropertyField & shiftedValue) == shiftedValue)
                {
                    multiplierForThisNode += mCosts[propertyIndex];

                }
            }

            // Now we know the cost multiplier to get to this node, which is multiplerForThisNode
            // Next we just have to calculate the distance to get to this node and 

            foreach (Link link in node.mLinks)
            {
                PositionedNode otherNode = link.NodeLinkingTo;

                Link linkBack = otherNode.GetLinkTo(node);

                if (linkBack != null)
                {
                    // reacalculate the cost:
                    linkBack.Cost = (node.Position - otherNode.Position).Length() * multiplierForThisNode;
                }
            }
        }

        public override void UpdateShapes()
        {
            base.UpdateShapes();


            if (Visible == false)
            {
                while (mOccupiedTileCircles.Count != 0)
                {
                    ShapeManager.Remove(mOccupiedTileCircles[mOccupiedTileCircles.Count - 1]);
                }
            }
            else
            {
                // Remove circles if necessary
                while (mOccupiedTileCircles.Count > mOccupiedTiles.Count)
                {
                    ShapeManager.Remove(mOccupiedTileCircles.Last);
                }
                while (mOccupiedTileCircles.Count < mOccupiedTiles.Count)
                {
                    Circle circle = new Circle();

                    circle.Color = Color.Orange;

                    ShapeManager.AddToLayer(circle, LayerToDrawOn);

                    mOccupiedTileCircles.Add(circle);
                }



                for (int i = 0; i < mOccupiedTiles.Count; i++)
                {
                    IndexToWorld(mOccupiedTiles[i].X, mOccupiedTiles[i].Y, 
                        out mOccupiedTileCircles[i].Position.X,
                        out mOccupiedTileCircles[i].Position.Y);

                    mOccupiedTileCircles[i].Radius = OccupiedCircleRadius;
                }
            }
            
        }

        public void IndexToWorld(int xIndex, int yIndex, out float worldX, out float worldY)
        {
            worldX = mXSeed + mGridSpacing * xIndex;
            worldY = mYSeed + mGridSpacing * yIndex;
        }


        public void WorldToIndex(float worldX, float worldY, out int xIndex, out int yIndex, bool clampToBounds = true)
        {
            xIndex = MathFunctions.RoundToInt((worldX - mXSeed) / mGridSpacing);
            yIndex = MathFunctions.RoundToInt((worldY - mYSeed) / mGridSpacing);

            if(clampToBounds)
            {
                xIndex = System.Math.Max(0, xIndex);
                xIndex = System.Math.Min(xIndex, mNumberOfXTiles - 1);

                yIndex = System.Math.Max(0, yIndex);
                yIndex = System.Math.Min(yIndex, mNumberOfYTiles - 1);
            }
        }

        /// <summary>
        /// Attaches the argument node to the node at the index x,y. If the node at x,y is null or
        /// if the node is already linked, this operation does not perform any logic.
        /// </summary>
        /// <param name="node">The node to link to the node at x,y</param>
        /// <param name="x">The x index of the taget node.</param>
        /// <param name="y">The y index of the target node.</param>
        public void AttachNodeToNodeAtIndex(PositionedNode node, int x, int y)
        {
            PositionedNode nodeToLinkTo = TiledNodeAt(x, y);

            if (nodeToLinkTo != null && !node.IsLinkedTo(nodeToLinkTo))
            {
                node.LinkTo(nodeToLinkTo);
            }

        }
        public override void Remove(PositionedNode nodeToRemove)
        {
            //base.Remove(nodeToRemove);
            // bake it for performance reasons:
            for (int i = nodeToRemove.Links.Count - 1; i > -1; i--)
            {
                nodeToRemove.Links[i].NodeLinkingTo.BreakLinkBetween(nodeToRemove);
            }
            mNodes.Remove(nodeToRemove);

            int tileX, tileY;
            WorldToIndex(nodeToRemove.Position.X, nodeToRemove.Position.Y, out tileX, out tileY);
            mTiledNodes[tileX][tileY] = null;
        }
        

        public void RemoveAndUnlinkNode( ref Microsoft.Xna.Framework.Vector3 positionToRemoveNodeFrom )
        {
            // November 28, 2022
            // This is passing a position in a tile node network so let's limit it to this tile instead of spreading outward.
            // This is a breaking change, but I think it was actually broken before and this is matches expectations.
            //PositionedNode nodeToRemove = GetClosestNodeTo(ref positionToRemoveNodeFrom);
            WorldToIndex(positionToRemoveNodeFrom.X, positionToRemoveNodeFrom.Y, out int tileX, out int tileY, clampToBounds:false);

            PositionedNode node = null;
            if(tileX > -1 && tileY > -1 && tileX < mNumberOfXTiles && tileY < NumberOfYTiles)
            {
                node = mTiledNodes[tileX][tileY];
            }

            if (node != null)
            {
                Remove(node);
            }
        }
        #endregion

        #region Private Methods

        private object GetTileOccupier(int x, int y)
        {
            foreach (OccupiedTile occupiedTile in mOccupiedTiles)
            {
                if (occupiedTile.X == x && occupiedTile.Y == y)
                {
                    return occupiedTile.Occupier;
                }
            }

            return null;
        }
        #endregion

        #endregion

    }
}
