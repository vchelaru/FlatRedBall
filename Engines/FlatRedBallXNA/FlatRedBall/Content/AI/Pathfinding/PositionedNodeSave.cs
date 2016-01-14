using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;


namespace FlatRedBall.Content.AI.Pathfinding
{
    #region XML Docs
    /// <summary>
    /// Save class for the PositionedNode class.  This class is used in the
    /// .nntx (Node Network XML) file type.
    /// </summary>
    #endregion
    public class PositionedNodeSave
    {

        public string Name;

        public float X;
        public float Y;
        public float Z;

        [XmlElementAttribute("Link")]
        public List<LinkSave> Links = new List<LinkSave>();

        public static PositionedNodeSave FromPositionedNode(FlatRedBall.AI.Pathfinding.PositionedNode positionedNode)
        {
            PositionedNodeSave nodeSave = new PositionedNodeSave();
            nodeSave.Name = positionedNode.Name;
            nodeSave.X = positionedNode.Position.X;
            nodeSave.Y = positionedNode.Position.Y;
            nodeSave.Z = positionedNode.Position.Z;

            foreach (FlatRedBall.AI.Pathfinding.Link link in positionedNode.Links)
            {
                nodeSave.Links.Add(
                    LinkSave.FromLink(link));
            }

            return nodeSave;
        }

        public FlatRedBall.AI.Pathfinding.PositionedNode ToPositionedNode()
        {
            FlatRedBall.AI.Pathfinding.PositionedNode positionedNode = new FlatRedBall.AI.Pathfinding.PositionedNode();

            positionedNode.Name = Name;
            positionedNode.Position.X = X;
            positionedNode.Position.Y = Y;
            positionedNode.Position.Z = Z;

            // links will be established by the NodeNetworkSave.ToNodeNetwork method
            // We've done all we can do here.
            return positionedNode;
        }
    }
}
