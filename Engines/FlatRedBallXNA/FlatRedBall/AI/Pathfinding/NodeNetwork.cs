using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Microsoft.Xna.Framework;
using Color = Microsoft.Xna.Framework.Color;
using FlatRedBall;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Utilities;
using FlatRedBall.Graphics;
using System.Linq;

namespace FlatRedBall.AI.Pathfinding
{
    /// <summary>
    /// Stores a collection of PositionedNodes and provides common functionality for
    /// pathfinding logic.
    /// </summary>
    public class NodeNetwork : INameable, IEquatable<NodeNetwork>
    {
        #region Fields

        // This gives child classes like TileNodeNetwork access
        // to the original list since ReadOnlyCollection foreaches
        // allocate memory.
        protected List<PositionedNode> mNodes = new List<PositionedNode>();
        ReadOnlyCollection<PositionedNode> mNodesReadOnly;


        protected PositionedObjectList<Polygon> mNodeVisibleRepresentation = new PositionedObjectList<Polygon>();
        ReadOnlyCollection<Polygon> mNodeVisibleRepresentationReadOnly;

        protected PositionedObjectList<Line> mLinkVisibleRepresentation = new PositionedObjectList<Line>();
        ReadOnlyCollection<Line> mLinksVisibleRepresentationReadOnly;

        bool mVisible;

        // This reduces memory allocation during runtime and also reduces the argument list size
        protected List<PositionedNode> mClosedList = new List<PositionedNode>(30);
        protected List<PositionedNode> mOpenList = new List<PositionedNode>(30);

        Dictionary<float, Color> mCostColors = new Dictionary<float, Color>();

        Color mLinkColor = Color.White;
        Color mNodeColor = Color.LimeGreen;

        protected float mLinkPerpendicularOffset = 2f;

        protected float mShortestPath;

        Layer mLayerToDrawOn;

        #endregion

        #region Properties

        public float LinkPerpendicularOffset
        {
            get => mLinkPerpendicularOffset;
            set => mLinkPerpendicularOffset = value;
        }

        public Layer LayerToDrawOn
        {
            get
            {
                return mLayerToDrawOn;
            }
            set
            {
                if(mLayerToDrawOn != value)
                {
                    mLayerToDrawOn = value;
                    foreach (var item in mNodeVisibleRepresentation)
                    {
                        if (item.Visible)
                        {
                            item.Visible = false;
                            item.mLayerBelongingTo = mLayerToDrawOn;
                            item.Visible = true;
                        }
                    }

                    foreach(var item in mLinkVisibleRepresentation)
                    {
                        if(item.Visible)
                        {
                            item.Visible = false;
                            item.mLayerBelongingTo = mLayerToDrawOn;
                            item.Visible = true;
                        }
                    }

                    
                    UpdateShapes();
                }
            }
        }


        public Color LinkColor
        {
            get { return mLinkColor; }
            set
            {
                mLinkColor = value;
                foreach (Line link in mLinkVisibleRepresentation)
                {
                    link.Color = value;
                }
            }
        }


        public string Name
        {
            get;
            set;
        }

        #region XML Docs
        /// <summary>
        /// All nodes in this NodeNetwork.
        /// </summary>
        #endregion
        public ReadOnlyCollection<PositionedNode> Nodes
        {
            get { return mNodesReadOnly; }
        }

        #region XML Docs
        /// <summary>
        /// The polygons used to represent PositionedNodes.  This is populated and managed by the
        /// NodeNetwork if Visible is set to true.
        /// </summary>
        #endregion
        public ReadOnlyCollection<Polygon> NodeVisibleRepresentation
        {
            get { return mNodeVisibleRepresentationReadOnly; }
        }

        #region XML Docs
        /// <summary>
        /// The Color that Node polygons should use when Visible is true;
        /// </summary>
        #endregion
        public Color NodeColor
        {
            get { return mNodeColor; }
            set
            {
                mNodeColor = value;

                if (mVisible)
                {
                    foreach (Polygon nodePolygon in mNodeVisibleRepresentation)
                    {
                        nodePolygon.Color = value;
                    }
                }

            }
        }

        /// <summary>
        /// Controls the visibility of the NodeNetwork.  This is usually only set to 
        /// true for debugging and testing purposes.  
        /// </summary>
        /// <remarks>
        /// Setting this value to true creates Polygons and Lines to represent the
        /// NodeNetwork.  Setting it back to false destroys all objects used for visible
        /// representation.
        /// </remarks>
        public bool Visible
        {
            get { return mVisible; }
            set
            {
                if (mVisible != value)
                {
                    mVisible = value;

                    UpdateShapes();
                }

            }
        }


        #endregion

        #region Methods

        #region Constructor

        #region XML Docs
        /// <summary>
        /// Creates an empty NodeNetwork.
        /// </summary>
        #endregion
        public NodeNetwork()
        {
            mNodesReadOnly = new ReadOnlyCollection<PositionedNode>(mNodes);
            mNodeVisibleRepresentationReadOnly = new ReadOnlyCollection<Polygon>(mNodeVisibleRepresentation);
            mLinksVisibleRepresentationReadOnly = new ReadOnlyCollection<Line>(mLinkVisibleRepresentation);
        }


        #endregion

        #region Public Methods

        //public void AddAndLinkNode(PositionedNode node)
        //{
        //    mNodes.Add(node);

        //}

        #region XML Docs
        /// <summary>
        /// Creates a new PositionedNode and adds it to the NodeNetwork.
        /// </summary>
        /// <returns>The newly-created PositionedNode.</returns>
        #endregion
        public virtual PositionedNode AddNode()
        {
            PositionedNode node = new PositionedNode();
            mNodes.Add(node);
            // We used to set the name on nodes, but this causes huge performance issues
            // on large node neworks (like 10k nodes), which is common for tilemaps
            //node.Name = mNodes.Count.ToString();

            //FlatRedBall.Utilities.StringFunctions.MakeNameUnique<PositionedNode, PositionedNode>(
            //    node, mNodes);
            return node;
        }

        #region XML Docs
        /// <summary>
        /// Adds an already-created PositionedNode to the NodeNetwork.
        /// </summary>
        /// <remarks>
        /// Will not add the PositionedNode if it is already part of the NodeNetwork
        /// </remarks>
        /// <param name="nodeToAdd">The PositionedNode to add.</param>
        #endregion
        public virtual void AddNode(PositionedNode nodeToAdd)
        {
            if (mNodes.Contains(nodeToAdd) == false)
            {
                mNodes.Add(nodeToAdd);
            }
        }


        public void AddCostColor(float cost, Color color)
        {
            mCostColors.Add(cost, color);
        }


        public NodeNetwork Clone()
        {
            NodeNetwork nodeNetwork = new NodeNetwork();

            for (int i = 0; i < this.mNodes.Count; i++)
            {
                PositionedNode positionedNode = mNodes[i].Clone();
                nodeNetwork.AddNode(positionedNode);
            }

            for (int i = 0; i < this.mNodes.Count; i++)
            {
                if (mNodes[i].Links.Count != 0)
                {
                    for(int j = 0; j < mNodes[i].Links.Count; j++)
                    {
                        Link link = mNodes[i].Links[j];

                        nodeNetwork.Nodes[i].LinkToOneWay(
                            nodeNetwork.FindByName(link.NodeLinkingTo.Name), 
                            link.Cost);
                    }
                }
            }


            return nodeNetwork;

        }


        #region XML Docs
        /// <summary>
        /// Finds a PositionedNode by the argument nameofNode.
        /// </summary>
        /// <param name="nameOfNode">The name of the PositionedNode to search for.</param>
        /// <returns>The PositionedNode with the matching Name, or null if no PositionedNodes match.</returns>
        #endregion
        public PositionedNode FindByName(string nameOfNode)
        {
            foreach (PositionedNode node in mNodes)
            {
                if (node.Name == nameOfNode)
                {
                    return node;
                }
            }
            return null;
        }


        public static void FollowPath(PositionedObject objectFollowingPath, List<Vector3> pointsToFollow, 
            float velocity, float pointReachedDistance, bool ignoreZ)
        {
            if (pointsToFollow.Count == 0)
            {
                // DESTINATION REACHED!
                return;
            }

            Vector3 vectorToMoveAlong;

            vectorToMoveAlong.X = 0;
            vectorToMoveAlong.Y = 0;
            vectorToMoveAlong.Z = 0;

            while (true)
            {
                vectorToMoveAlong = pointsToFollow[0] - objectFollowingPath.Position;

                if (ignoreZ)
                {
                    vectorToMoveAlong.Z = 0;
                }

                // See if objectFollowingPath has reached the point
                float length = vectorToMoveAlong.Length();

                if (length < pointReachedDistance)
                {
                    pointsToFollow.RemoveAt(0);

                    if (pointsToFollow.Count == 0)
                    {
                        // DESTINATION REACHED!
                        return;
                    }

                    vectorToMoveAlong = pointsToFollow[0] - objectFollowingPath.Position;
                }
                else
                {
                    vectorToMoveAlong /= length;
                    break;
                }
            }

            vectorToMoveAlong *= velocity;

            if (ignoreZ)
            {
                // This makes it so that setting of velocity doesn't overwrite the old Z value
                vectorToMoveAlong.Z = objectFollowingPath.ZVelocity;
            }
            objectFollowingPath.Velocity = vectorToMoveAlong;
        }

		//public void GetClosestPointInformation(Vector3 startingPosition, out PositionedNode closstNode, out Link closestLink)
		//{
		//    PositionedNode closestNode = GetClosestNodeTo(ref startingPosition);

		//    // Now, we know the position of the closest node.  Let's see if we're closer to
		//    // any links.  We'll do this by making segments out of the links.
		//    float closestDistance = (startingPosition - closestNode.Position).Length();

		//    for (int i = 0; i < mNodes.Count; i++)
		//    {
		//        PositionedNode node = mNodes[i];

		//        for (int j = 0; j < node.Links.Count; j++)
		//        {
		//            Link link = node.Links[j];

		//            Segment segment = new Segment(node.Position, link.NodeLinkingTo.Position);

		//            //segment.DistanceTo(
		//                // finish this
		//        }
		//    }
		//}

		#region XML Docs
		/// <summary>
		/// Returns the PositionedNode that's the closest to the argument position.
		/// </summary>
		/// <param name="position">The point to find the closest PositionedNode to.</param>
		/// <returns>The PositionedNode that is the closest to the argument position.</returns>
		#endregion
		public virtual PositionedNode GetClosestNodeTo(ref Vector3 position)
        {
            PositionedNode closestNode = null;

            float closestDistanceSquared = float.PositiveInfinity;
            float distance = 0;

            foreach (PositionedNode node in mNodes)
            {
                if (node.Active)
                {
                    distance = (node.X - position.X) * (node.X - position.X) +
                        (node.Y - position.Y) * (node.Y - position.Y) +
                        (node.Z - position.Z) * (node.Z - position.Z);

                    if (distance < closestDistanceSquared)
                    {
                        closestNode = node;
                        closestDistanceSquared = distance;
                    }
                }
            }

            return closestNode;
        }

        /// <summary>
        /// The radius of the shape used to visualize a node when the NodeNetwork is visible.
        /// </summary>
        public static float NodeVisualizationRadius
        {
            get; set;
        } = 5;

        [Obsolete("Use NodeVisualizationRadius")]
        public static float VisibleCoefficient 
        {
            get
            {
                return NodeVisualizationRadius;
            }

            set { NodeVisualizationRadius = value; }
        }

        #region XML Docs
        /// <summary>
        /// Returns the radius of the PositionedNode visible representation Polygons.
        /// </summary>
        /// <remarks>
        /// The size of the PositionedNode visible representation Polygons depends on the
        /// camera's Z position - as the Camera moves further away, the Polygons are drawn larger.
        /// If the Camera is viewing down the Z axis then changing the Z will not affect the visible
        /// size of the PositionedNode visible representation.
        /// </remarks>
        /// <param name="camera">The camera to use when calculating the size.</param>
        /// <param name="nodeIndex">The index of the PositionedNode in the NodeNetwork.  Since nodes can be in 
        /// 3D space the individual PositionedNode is required.</param>
        /// <returns>The radius of the node.</returns>
        #endregion
        public float GetVisibleNodeRadius(Camera camera, int nodeIndex)
        {
            return NodeVisualizationRadius / camera.PixelsPerUnitAt(mNodes[nodeIndex].Z);
        }


        public float GetVisibleNodeRadius(Camera camera, PositionedNode positionedNode)
        {
            return NodeVisualizationRadius / camera.PixelsPerUnitAt(positionedNode.Z);
        }

        #region XML Docs
        /// <summary>
        /// Returns the List of PositionedNodes which make the path from the start PositionedNode to the end PositionedNode.
        /// </summary>
        /// <remarks>
        /// If start and end are the same node then the List returned will contain that node.
        /// </remarks>
        /// <param name="start">The PositionedNode to begin the path at.</param>
        /// <param name="end">The destination PositionedNode.</param>
        /// <returns>The list of nodes to travel through to reach the end PositionedNode from the start PositionedNode.  The
        /// start and end nodes are included in the returned List.</returns>
        #endregion
        public List<PositionedNode> GetPath(PositionedNode start, PositionedNode end)
        {
            List<PositionedNode> pathToReturn = new List<PositionedNode>();

            GetPath(start, end, pathToReturn);

            return pathToReturn;

        }

        public virtual void GetPath(PositionedNode start, PositionedNode end, List<PositionedNode> listToFill)
        {
            if(start.Active == false || end.Active == false)
            {
                return;
            }
            else if (start == end)
            {
                listToFill.Add(start);
                return;
            }
            else
            {
                start.mParentNode = null;
                end.mParentNode = null;
                start.mCostToGetHere = 0;
                end.mCostToGetHere = 0;

                mOpenList.Clear();
                mClosedList.Clear();

                mOpenList.Add(start);
                start.AStarState = AStarState.Open;

                mShortestPath = float.PositiveInfinity;
                while (mOpenList.Count != 0)
                {
                    GetPathCalculate(mOpenList[0], end);
                }

                // inefficient, but we'll do this for now
                if (end.mParentNode != null)
                {

                    PositionedNode nodeOn = end;

                    listToFill.Insert(0, nodeOn);

                    while (nodeOn.mParentNode != null)
                    {
                        listToFill.Insert(0, nodeOn.mParentNode);

                        nodeOn = nodeOn.mParentNode;
                    }

                }

                for (int i = mClosedList.Count - 1; i > -1; i--)
                {
                    mClosedList[i].AStarState = AStarState.Unused;
                }
                for (int i = mOpenList.Count - 1; i > -1; i--)
                {
                    mOpenList[i].AStarState = AStarState.Unused;
                }
            }
        }

        #region XML Docs
        /// <summary>
        /// Returns the List of PositionedNodes which make the path from the start Vector3 to the end Vector3.
        /// </summary>
        /// <remarks>
        /// This method finds the closest nodes to each of the arguments, then calls the overload for GetPath which takes
        /// PositionedNode arguments.
        /// </remarks>
        /// <param name="startPoint">The world-coordinate start position.</param>
        /// <param name="endPoint">The world-coordinate end position.</param>
        /// <returns>The list of nodes to travel through to reach the closest PositionedNode to the endPoint from the closest
        /// PositionedNode to the startPoint.</returns>
        #endregion
        public virtual List<PositionedNode> GetPath(ref Vector3 startPoint, ref Vector3 endPoint)
        {
            PositionedNode startNode = GetClosestNodeTo(ref startPoint);
            PositionedNode endNode = GetClosestNodeTo(ref endPoint);

            return GetPath(startNode, endNode);

        }

        #region XML Docs
        /// <summary>
        /// Returns the List of PositionedNodes which make the path from the start PositionedNode to the end PositionedNode, or the node closest to the end node which is linked through the network to the start PositionedNode.
        /// </summary>
        /// <remarks>
        /// If start and end are the same node then the List returned will contain that node.
        /// </remarks>
        /// <param name="start">The PositionedNode to begin the path at.</param>
        /// <param name="end">The optimal destination PositionedNode.</param>
        /// <returns>The list of nodes to travel through to reach the end PositionedNode, or the node closest to the PositionedNode which is connected to the start PositionedNode, from the start PositionedNode.  The
        /// start and end nodes are included in the returned List.</returns>
        #endregion
        public virtual List<PositionedNode> GetPathOrClosest(PositionedNode start, PositionedNode end)
        {
            List<PositionedNode> pathToReturn = new List<PositionedNode>();

            GetPath(start, end, pathToReturn);

            if (pathToReturn.Count == 0)
            {
                float distanceSquared;
                float bestDistanceSquared = (end.Position - start.Position).LengthSquared();
                PositionedNode bestNode = start; 

                foreach (PositionedNode node in mClosedList)
                {
                    distanceSquared = (end.Position - node.Position).LengthSquared();
                    if (distanceSquared < bestDistanceSquared)
                    {
                        bestDistanceSquared = distanceSquared;
                        bestNode = node;
                    }                    
                }

                GetPath(start, bestNode, pathToReturn); 
            }

            return pathToReturn; 
        }     

        #region XML Docs
        /// <summary>
        /// Returns the List of PositionedNodes which make the path from the start Vector3 to the end Vector3, or to the node closest to the end Vector3 available if the end node is not linked in some way to the start node. 
        /// </summary>
        /// <remarks>
        /// This method finds the closest nodes to each of the arguments, then calls the overload for GetPathOrClosest which takes
        /// PositionedNode arguments.
        /// </remarks>
        /// <param name="startPoint">The world-coordinate start position.</param>
        /// <param name="endPoint">The world-coordinate end position.</param>
        /// <returns>The list of nodes to travel through to reach the closest PositionedNode which is linked to the closest PositionedNode to the startPoint.
        /// </returns>
        #endregion
        public virtual List<PositionedNode> GetPathOrClosest(ref Vector3 startPoint, ref Vector3 endPoint)
        {
            PositionedNode startNode = GetClosestNodeTo(ref startPoint);
            PositionedNode endNode = GetClosestNodeTo(ref endPoint);

            return GetPathOrClosest(startNode, endNode); 
        }

        public virtual List<Vector3> GetPositionPath(ref Vector3 startPoint, ref Vector3 endPoint) =>
            GetPathOrClosest(ref startPoint, ref endPoint).Select(item => item.Position).ToList();

        #region XML Docs
        /// <summary>
        /// Returns a List of Vector3s that will be an optimized version of GetPath given the NodeNetwork and a Collision Map to test against.
        /// </summary>
        /// <remarks>
        /// This method will get the optimal path between the two vectors using GetPath, and then will optimize it by
        /// testing line of sight between the nodes to see if the path can be optimized further (for more optimized
        /// pathfinding). When optimizing the path between two nodes, it will check if the midpoint is in line of sight with the
        /// startingPosition, and if it is, change the path to the midpoint instead of the current target node. the numberOfOptimizations
        /// decides how many times it will take the midpoint and optimize further.
        /// 
        /// This method assumes that the node network does NOT fall within collidable objects.
        /// </remarks>
        /// <param name="startingPosition">The world-coordinate of the starting position.</param>
        /// <param name="destination">The world-coordinate of the destination.</param>
        /// <param name="collisionMap">The collision map which will have the obstacles you are trying to path around.</param>
        /// <returns></returns>
        #endregion
        public List<Vector3> GetCollisionOptimizedPath(Vector3 startingPosition,
            Vector3 destination, PositionedObjectList<Polygon> collisionMap)
        {
            return GetCollisionOptimizedPath(startingPosition, destination, 2, 2, 0f, collisionMap);
        }

        public List<Vector3> GetCollisionOptimizedPath(Vector3 startingPosition,
            Vector3 destination, int numberOfOptimizations, PositionedObjectList<Polygon> collisionMap)
        {
            return GetCollisionOptimizedPath(startingPosition, destination, numberOfOptimizations, 2, 0f, collisionMap);
        }

        public List<Vector3> GetCollisionOptimizedPath(Vector3 startingPosition,
            Vector3 destination, float collisionThreshold, PositionedObjectList<Polygon> collisionMap)
        {
            return GetCollisionOptimizedPath(startingPosition, destination, 2, 2, collisionThreshold, collisionMap);
        }

        public List<Vector3> GetCollisionOptimizedPath(Vector3 startingPosition,
            Vector3 destination, int numberOfOptimizations, float collisionThreshold, PositionedObjectList<Polygon> collisionMap)
        {
            return GetCollisionOptimizedPath(startingPosition, destination, numberOfOptimizations, 2, collisionThreshold, collisionMap);
        }

        #region XML Docs
        /// <summary>
        /// Returns a List of Vector3s that will be an optimized version of GetPath given the NodeNetwork and a Collision Map to test against.
        /// </summary>
        /// <remarks>
        /// This method will get the optimal path between the two vectors using GetPath, and then will optimize it by
        /// testing line of sight between the nodes to see if the path can be optimized further (for more optimized
        /// pathfinding). When optimizing the path between two nodes, it will check if the midpoint is in line of sight with the
        /// startingPosition, and if it is, change the path to the midpoint instead of the current target node. the numberOfOptimizations
        /// decides how many times it will take the midpoint and optimize further.
        /// </remarks>
        /// <param name="startingPosition">The world-coordinate of the starting position.</param>
        /// <param name="destination">The world-coordinate of the destination.</param>
        /// <param name="numberOfOptimizations">The number of times the algorithm will take the midpoint between
        /// two nodes to test if they are within line of sight of each other (higher means nearer to optimal path).</param>
        /// <param name="optimizationsDeep"></param>
        /// <param name="collisionThreshold">Usually the object using the path will be larger than 0, use the size of the collision for testing line of sight.</param>
        /// <param name="collisionMap">The collision map which will have the obstacles you are trying to path around.</param>
        /// <returns></returns>
        #endregion
        public List<Vector3> GetCollisionOptimizedPath(Vector3 startingPosition,
            Vector3 destination, int numberOfOptimizations, int optimizationsDeep, float collisionThreshold, PositionedObjectList<Polygon> collisionMap)
        {
            List<PositionedNode> nodePath = GetPath(ref startingPosition, ref destination);
            List<Vector3> path = new List<Vector3>();
            if (nodePath.Count == 0)
            {
                return path;
            }
            else if (nodePath.Count == 1)
            {
                path.Add(nodePath[0].Position);
                path.Add(destination);
                return path;
            }

            Vector3 currentPosition = startingPosition;
            int currentNodeIndex = 0;
            int indexOfOutOfSightNode = 0;

            while (currentNodeIndex < nodePath.Count)
            {
                for (indexOfOutOfSightNode = currentNodeIndex; indexOfOutOfSightNode < nodePath.Count; indexOfOutOfSightNode++)
                {
                    if (PathfindingFunctions.IsInLineOfSight(currentPosition, nodePath[indexOfOutOfSightNode].Position, collisionThreshold, collisionMap))
                    {
                        //Go until we find an out of sight node.
                        continue;
                    }
                    else
                    {
                        //We found our out of sight node.
                        currentNodeIndex = indexOfOutOfSightNode;
                        break;
                    }
                }

                if (indexOfOutOfSightNode == nodePath.Count)
                { //All of the nodes in the nodePath were in line of sight, the destination should be in line of sight too.
                    if (PathfindingFunctions.IsInLineOfSight(nodePath[indexOfOutOfSightNode - 1].Position, destination, collisionMap))
                    {
                        path.Add(nodePath[indexOfOutOfSightNode - 1].Position);
                        path.Add(destination);
                        return path;
                    }
                    //If the destination isn't in line of sight, that means the end of the path can't see the destination!
                    throw new Exception("The last node in the generated path, " + nodePath[nodePath.Count - 1].Position + ", was not in line of sight with the destination, " + destination);
                    //throw new Exception("Could not find a path to the destination, did you give a CollisionThreshold that was too high for your NodeNetwork to handle?");
                }
                //else if (indexOfOutOfSightNode == 0)
                //{
                //    throw new Exception("Not enough Node Network coverage, startingPosition:" + startingPosition + " was not in line of sight with " + nodePath[0].Position + ", the first node in the generated path.");
                //}

                //#if DEBUG
                //This will only happen if there is a problem with the given NodeNetwork.
                if (indexOfOutOfSightNode == 0)
                { //If we can't see the first node, we'll trust GetPath and just go to that node.
                    path.Add(nodePath[0].Position);
                    currentPosition = nodePath[0].Position;
                    continue;
                }
                //#endif

                Vector3 pointToAdd;
                if (optimizationsDeep > 0)
                {
                    pointToAdd = PathfindingFunctions.OptimalVisiblePoint(currentPosition, nodePath[indexOfOutOfSightNode - 1].Position,
                        nodePath[indexOfOutOfSightNode].Position, numberOfOptimizations, collisionThreshold, collisionMap);
                    optimizationsDeep--;
                }
                else
                {
                    pointToAdd = nodePath[indexOfOutOfSightNode].Position;
                }
                /*
                if (path.Count > 0 && pointToAdd == path[path.Count - 1])
                { //Protect against an infinite loop of the same point being added.
                    //This only occurs if there is the collisionThreshold is too high for the nodenetwork to handle,
                    //and will make the character bump into the collisionMap when taking the path here.
                    pointToAdd = nodePath[indexOfOutOfSightNode].Position;
                }
                */

                path.Add(pointToAdd);

                if (PathfindingFunctions.IsInLineOfSight(pointToAdd, destination, collisionMap))
                {
                    path.Add(destination);
                    return path;
                }
                currentPosition = pointToAdd;
            }
            //This should never happen.
            throw new Exception("Given path never found the destination.");
        }

        #region XML Docs
        /// <summary>
        /// Removes the argument PositionedNode from the NodeNetwork.  Also destroys any links
        /// pointing to the argument PositionedNode.
        /// </summary>
        /// <param name="nodeToRemove">The PositionedNode to remove.</param>
        #endregion
        public virtual void Remove(PositionedNode nodeToRemove)
        {           
            for (int i = nodeToRemove.Links.Count - 1; i > -1; i--)
            {
                nodeToRemove.Links[i].NodeLinkingTo.BreakLinkBetween(nodeToRemove);
            }

            mNodes.Remove(nodeToRemove);
        }
        #region XML Docs
/// <summary>
        /// Removes the argument PositionedNode from the NodeNetwork.  Also destroys any links
        /// pointing to the argument PositionedNode.
/// </summary>
/// <param name="nodeToRemove">The PositionedNode to remove from the network.</param>
/// <param name="scanAndRemoveOneWayReferences">Scans the entire network and removes all links to this node. Used when not all link relationships are two way.</param>
        #endregion
        public virtual void Remove(PositionedNode nodeToRemove, bool scanAndRemoveOneWayReferences)
        {
            if (scanAndRemoveOneWayReferences)
            {
                foreach (PositionedNode node in mNodes)
                {
                    if (node.IsLinkedTo(nodeToRemove))
                    {
                        node.BreakLinkBetween(nodeToRemove);
                    }
                }

                mNodes.Remove(nodeToRemove);
            }
            else
            {
                Remove(nodeToRemove); 
            }
        }

        public void ScalePositions(float scaleAmount)
        {
            for (int i = 0; i < this.Nodes.Count; i++)
            {
                this.Nodes[i].Position *= scaleAmount;
            }

        }

        public virtual void Shift(Vector3 shiftVector)
        {
            for (int i = 0; i < this.Nodes.Count; i++)
            {
                this.Nodes[i].Position += shiftVector;
            }
        }

        public PositionedNode SplitLink(PositionedNode firstNode, PositionedNode secondNode)
		{
			PositionedNode newNode = new PositionedNode();
			newNode.Position = (firstNode.Position + secondNode.Position) / 2.0f;
			bool addNode = false;

			// If the first node is linked to the second
			Link link = null;

			for (int i = 0; i < firstNode.Links.Count; i++)
			{
				if (firstNode.Links[i].NodeLinkingTo == secondNode)
				{
					link = firstNode.Links[i];
					break;
				}
			}
			if (link != null)
			{
				addNode = true;
				link.NodeLinkingTo = newNode;
				newNode.LinkToOneWay(secondNode, (newNode.Position - secondNode.Position).Length());
			}



			link = null;

			for (int i = 0; i < secondNode.Links.Count; i++)
			{
				if (secondNode.Links[i].NodeLinkingTo == firstNode)
				{
					link = secondNode.Links[i];
					break;
				}
			}
			if (link != null)
			{
				addNode = true;
				link.NodeLinkingTo = newNode;
				newNode.LinkToOneWay(firstNode, (newNode.Position - firstNode.Position).Length());
			}

			if (addNode)
			{
				mNodes.Add(newNode);
			}

			return newNode;
		}

		/// <summary>
		/// Updates the visible representation of the NodeNetwork.  This is only needed to be called if the NodeNetwork
		/// is visible and if any contained PositionedNodes or Links have changed.
		/// </summary>
		public virtual void UpdateShapes()
        {
            Vector3 zeroVector = new Vector3();

            if (mVisible == false)
            {

                while (mNodeVisibleRepresentation.Count != 0)
                {
                    ShapeManager.Remove(mNodeVisibleRepresentation[mNodeVisibleRepresentation.Count - 1]);
                }

                while (mLinkVisibleRepresentation.Count != 0)
                {
                    ShapeManager.Remove(mLinkVisibleRepresentation[mLinkVisibleRepresentation.Count - 1]);
                }
            }
            else
            {
                #region Create nodes to match how many nodes are in the network
                while (mNodes.Count > mNodeVisibleRepresentation.Count)
                {
                    Polygon newPolygon = Polygon.CreateEquilateral(4, 
                        4, // radius
                        MathHelper.PiOver4);
                    newPolygon.Name = "NodeNetwork Polygon";

                    const bool makeAutomaticallyUpdated = false;
                    ShapeManager.AddToLayer(newPolygon, LayerToDrawOn, makeAutomaticallyUpdated);
                    // This was commented out and I'm not sure why.  With this
                    // uncommented, it makes it so the NodeNetwork is never drawn.
                    // I discovered this while working on the AIEditor on Sept 22, 2010.
                    newPolygon.Visible = true;


                    newPolygon.Color = mNodeColor;
                    mNodeVisibleRepresentation.Add(newPolygon);
                }
                #endregion

                #region Remove nodes if there are too many
                while (mNodes.Count < mNodeVisibleRepresentation.Count)
                {
                    ShapeManager.Remove(mNodeVisibleRepresentation[mNodeVisibleRepresentation.Count - 1]);
                }
                #endregion

                #region Create/update links and update node positions

                int nextLine = 0;
                //List<PositionedNode> nodesAlreadyLinkedTo = new List<PositionedNode>();

                for (int i = 0; i < mNodes.Count; i++)
                {
                    mNodeVisibleRepresentation[i].Position = mNodes[i].Position;

                    mNodeVisibleRepresentation[i].ScaleBy(
                        GetVisibleNodeRadius(SpriteManager.Camera, i) /
                        mNodeVisibleRepresentation[i].BoundingRadius);

                    mNodeVisibleRepresentation[i].ForceUpdateDependencies();
             
                    foreach (Link link in mNodes[i].mLinks)
                    {
                        {
                            // haven't drawn links to this node yet so draw it

                            #region Create a line for this link if there isn't one already
                            if (nextLine >= mLinkVisibleRepresentation.Count)
                            {
                                Line line = new Line();
                                line.Name = "NodeNetwork Link Line";
                                mLinkVisibleRepresentation.Add(line);

                                const bool makeAutomaticallyUpdated = false;
                                ShapeManager.AddToLayer(line, LayerToDrawOn, makeAutomaticallyUpdated);
                            }
                            #endregion

                            #region Adjust the line if necessary

                            Line lineModifying = mLinkVisibleRepresentation[nextLine];

                            nextLine++;

                            lineModifying.SetFromAbsoluteEndpoints(
                                mNodes[i].Position, link.NodeLinkingTo.Position);

                            Vector3 offsetVector = link.NodeLinkingTo.Position - mNodes[i].Position;
                            offsetVector.Normalize();
                            offsetVector *= mLinkPerpendicularOffset;
                            // A negative 90 degree rotation will result in forward being "on the right side"
                            MathFunctions.RotatePointAroundPoint(zeroVector, ref offsetVector, -(float)System.Math.PI / 2.0f);

                            lineModifying.Position += offsetVector;
                            //lineModifying.Position.X = (mNodes[i].X + link.NodeLinkingTo.X) / 2.0f;
                            //lineModifying.Position.Y = (mNodes[i].Y + link.NodeLinkingTo.Y) / 2.0f;

                            //lineModifying.RelativePoint1.X = mNodes[i].X - lineModifying.X;
                            //lineModifying.RelativePoint1.Y = mNodes[i].Y - lineModifying.Y;

                            //lineModifying.RelativePoint2.X = link.NodeLinkingTo.X - lineModifying.X;
                            //lineModifying.RelativePoint2.Y = link.NodeLinkingTo.Y - lineModifying.Y;

                            UpdateLinkColor(lineModifying, link.Cost);

                            AdjustLinkRepresentation(mNodes[i], link, lineModifying);
                            #endregion

                        }

                    }
                    //nodesAlreadyLinkedTo.Add(mNodes[i]);


                }
                #endregion

                while (nextLine < mLinkVisibleRepresentation.Count)
                {
                    ShapeManager.Remove(mLinkVisibleRepresentation[mLinkVisibleRepresentation.Count - 1]);
                }
            }
        }

        public void SetNodeColor(PositionedNode node, Color color)
        {
            int index = mNodes.IndexOf(node);

            if (index < 0)
                throw new ArgumentException("Node does not belong to this NodeNetwork");

            mNodeVisibleRepresentation[index].Color = color;
        }

        public void SetAllNodesColor(Color color)
        {
            mNodeColor = color;

            for (int i = 0; i < mNodeVisibleRepresentation.Count; i++)
                mNodeVisibleRepresentation[i].Color = color;
        }

        protected virtual void AdjustLinkRepresentation(PositionedNode sourceNode, Link link, Line line)
        {

        }

        #endregion

        #region Private

        protected virtual void GetPathCalculate(PositionedNode currentNode, PositionedNode endNode)
        {
            mOpenList.Remove(currentNode);
            mClosedList.Add(currentNode);
            currentNode.AStarState = AStarState.Closed;
            bool partOfOpen = false;

            int linkCount = currentNode.Links.Count;

            // Vic asks - why do we foreach here? This should be faster as loop:
            //foreach (Link currentLink in currentNode.Links)
            for (int i = 0; i < linkCount; i++)
            {
                Link currentLink = currentNode.mLinks[i];

                //Links can be turned off, and when they are in that 
                //state they should be ignored by pathfinding calls. 
                if (!currentLink.Active)
                {
                    continue; 
                }
                PositionedNode nodeLinkingTo = currentLink.NodeLinkingTo; //currentNode.Links[i].NodeLinkingTo;

                if (nodeLinkingTo.AStarState != AStarState.Closed && nodeLinkingTo.Active)
                {
                    float cost = currentNode.mCostToGetHere + currentLink.Cost;

                    if (cost < mShortestPath)
                    {
                        partOfOpen = nodeLinkingTo.AStarState == AStarState.Open;
                    
                        if (partOfOpen == false ||
                            cost <= nodeLinkingTo.CostToGetHere)
                        {
                            nodeLinkingTo.mParentNode = currentNode;
                            nodeLinkingTo.mCostToGetHere =
                                currentNode.mCostToGetHere + currentLink.Cost;

                            if (nodeLinkingTo == endNode)
                            {
                                mShortestPath = nodeLinkingTo.mCostToGetHere;
                                // September 6th, 2012 - Jesse Crafts-Finch
                                // Removed the break because it prevents the currentNode from checking
                                //  alternative links which may end up creating a cheaper path to the endNode.                                
                                //break;
                            }

                            if (partOfOpen)
                            {
                                mOpenList.Remove(nodeLinkingTo);
                                nodeLinkingTo.AStarState = AStarState.Unused;
                            }
                        }


                        AddNodeToOpenList(nodeLinkingTo);
                    }
                }
            }
        }

        protected void AddNodeToOpenList(PositionedNode node)
        {
            bool added = false;

            // See if the node is already part of the open node list
            // If it is, just remove it and re-add it just in case its
            // cost has changed, then exit.
            if (node.AStarState == AStarState.Open)
            {
                mOpenList.Remove(node);
                node.AStarState = AStarState.Unused;
            }

            for (int i = 0; i < mOpenList.Count; i++)
            {
                if (node.mCostToGetHere < mOpenList[i].mCostToGetHere)
                {
                    mOpenList.Insert(i, node);
                    node.AStarState = AStarState.Open;
                    added = true;
                    break;
                }
            }

            if (added == false)
            {
                mOpenList.Add(node);
                node.AStarState = AStarState.Open;
            }
        }

        protected void UpdateLinkColor(Line line, float cost)
        {
            if (mCostColors.Count == 0)
            {
                line.Color = mLinkColor;
            }
            else
            {
                // Set the color here just in case there's no values that apply
                line.Color = mLinkColor;

                foreach(KeyValuePair<float, Color> kvp in mCostColors)
                {
                    if (cost > kvp.Key)
                    {
                        line.Color = kvp.Value;
                    }
                    else
                    {
                        break;
                    }
                }

            }
        }

        #endregion

        #endregion

        #region IEquatable<NodeNetwork> Members

        bool IEquatable<NodeNetwork>.Equals(NodeNetwork other)
        {
            return this == other;
        }

        #endregion
    }
}
