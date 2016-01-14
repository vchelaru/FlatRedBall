using System;
using System.Collections.Generic;
using System.Text;

using System.Xml;
using System.Xml.Serialization;

using FlatRedBall.IO;

using PositionedNode = FlatRedBall.AI.Pathfinding.PositionedNode;

namespace FlatRedBall.Content.AI.Pathfinding
{
    public class NodeNetworkSave
    {
        #region Fields
        [XmlElementAttribute("PositionedNode")]
        public List<PositionedNodeSave> PositionedNodes = new List<PositionedNodeSave>();


        private string mFileName;
        #endregion

        public static NodeNetworkSave FromNodeNetwork(FlatRedBall.AI.Pathfinding.NodeNetwork nodeNetwork)
        {
            NodeNetworkSave nodeNetworkSave = new NodeNetworkSave();

            foreach (FlatRedBall.AI.Pathfinding.PositionedNode positionedNode in nodeNetwork.Nodes)
            {
                nodeNetworkSave.PositionedNodes.Add(
                    PositionedNodeSave.FromPositionedNode(positionedNode));
            }

            return nodeNetworkSave;

        }

        public static NodeNetworkSave FromFile(string fileName)
        {
            NodeNetworkSave nodeNetworkSave =
                FlatRedBall.IO.FileManager.XmlDeserialize<NodeNetworkSave>(fileName);

            nodeNetworkSave.mFileName = fileName;

            return nodeNetworkSave;
        }

        public void Save(string fileName)
        {
            FileManager.XmlSerialize<NodeNetworkSave>(this, fileName);
        }


        public FlatRedBall.AI.Pathfinding.NodeNetwork ToNodeNetwork()
        {
            string throwaway;
            return ToNodeNetwork(out throwaway);
        }




        public FlatRedBall.AI.Pathfinding.NodeNetwork ToNodeNetwork(out string errors)
        {
            errors = null;

            FlatRedBall.AI.Pathfinding.NodeNetwork nodeNetwork = new FlatRedBall.AI.Pathfinding.NodeNetwork();

            foreach (PositionedNodeSave nodeSave in PositionedNodes)
            {
                PositionedNode node = nodeSave.ToPositionedNode();

                nodeNetwork.AddNode(node);
            }

            List<PositionedNode> nodesAlreadyLinked =
                new List<PositionedNode>(PositionedNodes.Count);

            // Now that all of the nodes are set up, reestablish the links
            for (int i = 0; i < PositionedNodes.Count; i++)
            {
                foreach (LinkSave linkSave in PositionedNodes[i].Links)
                {
                    if (linkSave.NodeLinkingTo == PositionedNodes[i].Name)
                    {
                        string nodeName = PositionedNodes[i].Name;
                        if (string.IsNullOrEmpty(nodeName))
                        {
                            nodeName = "<UNNAMED>";
                        }
                        errors += "The node " + nodeName + " has a link to itself.\n";
                        break;
                    }
                    else
                    {
                        PositionedNode node = nodeNetwork.FindByName(linkSave.NodeLinkingTo);

                        if (nodesAlreadyLinked.Contains(node) == false)
                        {

                            nodeNetwork.Nodes[i].LinkTo(node, linkSave.Cost);

                        }
                    }
                }
                nodesAlreadyLinked.Add(nodeNetwork.Nodes[i]);
            }

            return nodeNetwork;

        }
    }
}
