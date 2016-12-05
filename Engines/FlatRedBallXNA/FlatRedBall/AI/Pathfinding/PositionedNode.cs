using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

using FlatRedBall;

using FlatRedBall.Math;

using FlatRedBall.Utilities;

using Microsoft.Xna.Framework;

namespace FlatRedBall.AI.Pathfinding
{
    public enum AStarState
    {
        Unused,
        Open,
        Closed
    }

    /// <summary>
    /// An object which has position properties 
    /// </summary>
    public class PositionedNode : IStaticPositionable, INameable, IEquatable<PositionedNode>
    {
        #region Fields

        string mName;

        public Vector3 Position;

        // made internal for speed boosts
        protected internal List<Link> mLinks = new List<Link>();
        ReadOnlyCollection<Link> mLinksReadOnly;

        public int PropertyField;

        public AStarState AStarState;


        #region XML Docs
        /// <summary>
        /// The node that links to this node.  This is reset every time the 
        /// containing NodeNetwork searches for a path.
        /// </summary>
        #endregion
        protected internal PositionedNode mParentNode;

        #region XML Docs
        /// <summary>
        /// The cost to get to this node from the start node.  This variable is
        /// set when the containing NodeNetwork searches for a path.
        /// </summary>
        #endregion
        protected internal float mCostToGetHere;

        /// <summary>
        /// Only active nodes are included in pathfinding and find node searches. 
        /// </summary>
        /// Update February 10, 2013
        /// Nodes should always start 
        /// out as active.  
        private bool mActive = true; 

        #endregion

        #region Properties

        #region XML docs
        /// <summary>
        /// Returns the cost to get to this node from the start node.  This
        /// value is only accurate if the node is contained in list returned
        /// by the last call to NodeNetwork.GetPath.
        /// </summary>
        /// <remarks>
        /// This value is reset anytime GetPath is called on the containing NodeNetwork.
        /// </remarks>
        #endregion
        public float CostToGetHere
        {
            get { return mCostToGetHere; }
        }

        #region XML Docs
        /// <summary>
        /// The Node's name.  Mainly used for saving NodeNetworks since saved Links reference
        /// PositionedNodes by name.
        /// </summary>
        #endregion
        public string Name
        {
            get
            {
                return mName;
            }
            set
            {
                mName = value;
            }
        }

        #region XML Docs
        /// <summary>
        /// The X position of the PositionedNode.
        /// </summary>
        #endregion
        public float X
        {
            get
            {
                return Position.X;
            }
            set
            {
                Position.X = value ;
            }
        }


        #region XML Docs
        /// <summary>
        /// The Y position of the PositionedNode.
        /// </summary>
        #endregion
        public float Y
        {
            get
            {
                return Position.Y;
            }
            set
            {
                Position.Y = value; ;
            }
        }

        #region XMl Docs
        /// <summary>
        /// The Z position of the PositionedNode.
        /// </summary>
        #endregion
        public float Z
        {
            get
            {
                return Position.Z;
            }
            set
            {
                Position.Z = value ;
            }
        }

        #region XML Docs
        /// <summary>
        /// The links belonging to this PositionedNode.
        /// </summary>
        /// <remarks>
        /// This is a list of Links which reference the PositionedNodes that this links to.
        /// Links are one-way and PositionedNodes that this links to do not necessarily contain
        /// Links back to this.
        /// </remarks>
        #endregion
        public ReadOnlyCollection<Link> Links
        {
            get { return mLinksReadOnly; }
        }

        /// <summary>
        /// Only active nodes are included in pathfinding and find node searches. 
        /// </summary>
        public virtual bool Active
        {
            get { return mActive; }
            set { mActive = value; }
        }
        #endregion

        #region Methods

        #region XML Docs
        /// <summary>
        /// Creates a new PositionedNode.
        /// </summary>
        #endregion
        public PositionedNode()
        {
            mLinksReadOnly = new ReadOnlyCollection<Link>(mLinks);
        }

        #region XML Docs
        /// <summary>
        /// Disconnects all Links between this and the argument node.
        /// </summary>
        /// <param name="node">The PositionedNode to break links between.</param>
        #endregion
        public virtual void BreakLinkBetween(PositionedNode node)
        {
            for (int i = 0; i < node.mLinks.Count; i++)
            {
                if (node.mLinks[i].NodeLinkingTo == this)
                {
                    node.mLinks.RemoveAt(i);
                    break;
                }
            }

            for (int i = 0; i < mLinks.Count; i++)
            {
                if (mLinks[i].NodeLinkingTo == node)
                {
                    mLinks.RemoveAt(i);
                    break;
                }
            }

           
        }

        public PositionedNode Clone()
        {
            PositionedNode newNode = (PositionedNode)this.MemberwiseClone();

            newNode.mLinks = new List<Link>();
            newNode.mLinksReadOnly = new ReadOnlyCollection<Link>(newNode.mLinks);
            newNode.mParentNode = null;
            newNode.mCostToGetHere = 0;

            return newNode;
        }


        public Link GetLinkTo(PositionedNode node)
        {
            foreach (Link link in mLinks)
            {
                if (link.NodeLinkingTo == node)
                {
                    return link;
                }
            }
            return null;
        }


        #region XML Docs
        /// <summary>
        /// Returns whether this has a Link to the argument PositionedNode.
        /// </summary>
        /// <remarks>
        /// If this does not link to the argument PositionedNode, but the argument
        /// links back to this, the method will return false.  It only checks links one-way.
        /// </remarks>
        /// <param name="node">The argument to test linking.</param>
        /// <returns>Whether this PositionedNode links to the argument node.</returns>
        #endregion
        public bool IsLinkedTo(PositionedNode node)
        {
            foreach (Link link in mLinks)
            {
                if (link.NodeLinkingTo == node)
                {
                    return true;
                }
            }
            return false;
        }


        


        public void LinkTo(PositionedNode nodeToLinkTo)
        {
            float distanceToTravel = (Position - nodeToLinkTo.Position).Length();

            LinkTo(nodeToLinkTo, distanceToTravel);
        }

        #region XML Docs
        /// <summary>
        /// Creates Links from this to the argument nodeToLinkTo, and another Link from the
        /// argument nodeToLinkTo back to this.
        /// </summary>
        /// <remarks>
        /// If either this or the argument nodeToLinkTo already contains a link to the other 
        /// PositionedNode, then the cost of the link is set to the argument costTo.
        /// </remarks>
        /// <param name="nodeToLinkTo">The other PositionedNode to create Links between.</param>
        /// <param name="costTo">The cost to travel between this and the argument nodeToLinkTo.</param>
        #endregion
        public void LinkTo(PositionedNode nodeToLinkTo, float costTo)
        {
            LinkTo(nodeToLinkTo, costTo, costTo);
        }

        #region XML Docs
        /// <summary>
        /// Creates Links from this to the argument nodeToLinkTo, and another Link from the
        /// argument nodeToLinkTo back to this.
        /// </summary>
        /// <remarks>
        /// If either this or the argument nodeToLinkTo already contains a link to the other 
        /// PositionedNode, then the cost of the link is set to the argument costTo or costFrom as appropriate.
        /// </remarks>
        /// <param name="nodeToLinkTo">The other PositionedNode to create the Links between.</param>
        /// <param name="costTo">The cost to travel from this to the argument nodeToLinkTo.</param>
        /// <param name="costFrom">The cost to travel from the nodeToLinkTo back to this.</param>
        #endregion
        public void LinkTo(PositionedNode nodeToLinkTo, float costTo, float costFrom)
        {
#if DEBUG
			if (nodeToLinkTo == this)
			{
				throw new ArgumentException("Cannot have a node link to itself");
			}
#endif
            bool updated = false;

            for (int i = 0; i < mLinks.Count; i++)
            {
                if (mLinks[i].NodeLinkingTo == nodeToLinkTo)
                {
                    mLinks[i].Cost = costTo;
                    updated = true;
                    break;
                }
            }
            if (!updated)
            {
                mLinks.Add(new Link(nodeToLinkTo, costTo));
            }

            // Now do the same for the other node
            updated = false;
            for (int i = 0; i < nodeToLinkTo.mLinks.Count; i++)
            {
                if (nodeToLinkTo.mLinks[i].NodeLinkingTo == this)
                {
                    nodeToLinkTo.mLinks[i].Cost = costFrom;
                    updated = true;
                    break;
                }
            }
            if (!updated)
            {
                nodeToLinkTo.mLinks.Add(new Link(this, costFrom));
            }
        }

        #region XML Docs
        /// <summary>
        /// Creates a link from this PositionedNode to the argument nodeToLinkTo.  Links
        /// on the argument nodeToLinkTo are not modified.
        /// </summary>
        /// <remarks>
        /// If this already links to the arugment nodeToLinkTo, the cost is set to the argument
        /// costTo.
        /// </remarks>
        /// <param name="nodeToLinkTo">The PositionedNode to create a link to.</param>
        /// <param name="costTo">The cost to travel from this to the argument nodeToLinkTo.</param>
        #endregion
        public virtual void LinkToOneWay(PositionedNode nodeToLinkTo, float costTo)
        {
            foreach (Link link in mLinks)
            {
                if (link.NodeLinkingTo == nodeToLinkTo)
                {
                    link.Cost = costTo;
                    return;
                }
            }

            mLinks.Add(new Link(nodeToLinkTo, costTo));
        }

        #region XML Docs
        /// <summary>
        /// Returns the string representation of this.
        /// </summary>
        /// <returns>The string representation of this.</returns>
        #endregion
        public override string ToString()
        {
            return mName + string.Format(" ({0},{1},{2})", X, Y, Z);
        }

        #endregion


        #region IEquatable<PositionedNode> Members

        bool IEquatable<PositionedNode>.Equals(PositionedNode other)
        {
            return this == other;
        }

        #endregion
    }

    public static class PositionedNodeListExtensionMethods
    {

        public static float PathDistanceSquared(this List<PositionedNode> path)
        {
            float total = 0;

            for (int i = 0; i < path.Count - 1; i++)
            {
                total += (path[i].Position - path[i + 1].Position).LengthSquared();
            }

            return total;
        }
    }
}
