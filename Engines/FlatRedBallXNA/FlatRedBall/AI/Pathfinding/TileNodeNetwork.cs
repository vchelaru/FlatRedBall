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


        #endregion

        #region Methods

        #region Constructor

        private TileNodeNetwork()
        {

        }

        public TileNodeNetwork(float xSeed, float ySeed, float gridSpacing, int numberOfXTiles,
            int numberOfYTiles, DirectionalType directionalType)
        {
            mCosts = new float[PropertyIndexSize]; // Maybe expand this to 64 if we ever move to a long bit field?

            OccupiedCircleRadius = .5f;
            mTiledNodes = new PositionedNode[numberOfXTiles][];
            mNumberOfXTiles = numberOfXTiles;
            mNumberOfYTiles = numberOfYTiles;
            mDirectionalType = directionalType;
            mXSeed = xSeed;
            mYSeed = ySeed;
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

        public PositionedNode AddAndLinkTiledNodeWorld(float worldX, float worldY)
        {
            int x;
            int y;

            WorldToIndex(worldX, worldY, out x, out y);

            return AddAndLinkTiledNode(x, y, mDirectionalType);
        }

        public PositionedNode AddAndLinkTiledNode(int x, int y)
        {
            return AddAndLinkTiledNode(x, y, mDirectionalType);
        }
        public PositionedNode AddAndLinkTiledNode(int x, int y, DirectionalType directionalType)
        {
            PositionedNode node = null;

            if (mTiledNodes[x][y] != null)
            {            
                node = mTiledNodes[x][y];
            }
            else
            {
                node = AddNode();
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

            int xTilesFromSeed = MathFunctions.RoundToInt((x - mXSeed) / mGridSpacing);
            int yTilesFromSeed = MathFunctions.RoundToInt((y - mXSeed) / mGridSpacing);

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

        public object GetOccupier(float x, float y)
        {
            object occupier = null;
            
            IsTileOccupiedWorld(x, y, out occupier);

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

        public void SetCosts(params float[] costs)
        {
            for (int i = 0; i < costs.Length; i++)
            {
                mCosts[i] = costs[i];
            }
        }


        public PositionedNode TiledNodeAtWorld(float x, float y)
        {
            int xIndex;
            int yIndex;

            WorldToIndex(x, y, out xIndex, out yIndex);

            return TiledNodeAt(xIndex, yIndex);
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

        public void WorldToIndex(float worldX, float worldY, out int xIndex, out int yIndex)
        {
            xIndex = MathFunctions.RoundToInt((worldX - mXSeed) / mGridSpacing);
            yIndex = MathFunctions.RoundToInt((worldY - mYSeed) / mGridSpacing);

            xIndex = System.Math.Max(0, xIndex);
            xIndex = System.Math.Min(xIndex, mNumberOfXTiles - 1);

            yIndex = System.Math.Max(0, yIndex);
            yIndex = System.Math.Min(yIndex, mNumberOfYTiles - 1);
        }

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
            base.Remove(nodeToRemove);
            int tileX, tileY;
            WorldToIndex(nodeToRemove.Position.X, nodeToRemove.Position.Y, out tileX, out tileY);
            mTiledNodes[tileX][tileY] = null;
        }

        public void RemoveAndUnlinkNode( ref Microsoft.Xna.Framework.Vector3 positionToRemoveNodeFrom )
        {
            PositionedNode nodeToRemove = GetClosestNodeTo(ref positionToRemoveNodeFrom);
            Remove( nodeToRemove );
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
