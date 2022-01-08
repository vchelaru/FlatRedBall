using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.AI.Pathfinding
{
    #region XML Docs
    /// <summary>
    /// Represents a one-way cost-including path to a PositionedNode.
    /// </summary>
    #endregion
    public class Link : IEquatable<Link>
    {
        #region Fields/Properties

        /// <summary>
        /// Whether this link is active. If false, pathfinding will ignore this link.
        /// </summary>
        public bool Active
        {
            get;
            set;
        }

        /// <summary>
        /// The cost to travel the link.
        /// </summary>
        /// <remarks>
        /// This is by default the distance to travel; however it can manually
        /// be changed to be any value to reflect different terrain, altitude, or other
        /// travelling costs.
        /// </remarks>
        public float Cost
        {
            get;
            set;
        }

        /// <summary>
        /// The destination PositionedNode.  The starting PositionedNode is not stored by the Link instance.
        /// </summary>
        public PositionedNode NodeLinkingTo
        {
            get;
            set;
        }

        #endregion

        #region Methods

        #region XML Docs
        /// <summary>
        /// Creates a new Link.
        /// </summary>
        /// <param name="nodeLinkingTo">The node to link to.</param>
        /// <param name="cost">The cost to travel the link.</param>
        #endregion
        public Link(PositionedNode nodeLinkingTo, float cost)
        {
            NodeLinkingTo = nodeLinkingTo;
            Cost = cost;
            Active = true; 
        }

        public override string ToString()
        {
            if (NodeLinkingTo == null)
            {
                return Cost + " <not linking to any node>";
            }
            else
            {
                return Cost + " " + NodeLinkingTo.Name;
            }
        }

        #endregion


        #region IEquatable<Link> Members

        bool IEquatable<Link>.Equals(Link other)
        {
            return this == other;
        }

        #endregion
    }
}
