using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content;
using FlatRedBall.AI.Pathfinding;

namespace FlatRedBall.Content
{
    #region XML Docs
    /// <summary>
    /// Class used to read a NodeNetwork through the content pipeline.
    /// </summary>
    #endregion
    public class NodeNetworkReader : ContentTypeReader<NodeNetwork>
    {
        struct NodeConnection
        {
            public PositionedNode PositionedNode;
            public float Cost;
            public string NodeLinkingTo;

            public NodeConnection(PositionedNode positionedNode, float cost, string nodeLinkingTo)
            {
                PositionedNode = positionedNode;
                Cost = cost;
                NodeLinkingTo = nodeLinkingTo;
            }
        }

        protected override NodeNetwork Read(ContentReader input, NodeNetwork existingInstance)
        {
            if (existingInstance != null)
            {
                return existingInstance;
            }

            NodeNetwork network = new NodeNetwork();
            network.Name = input.AssetName;

            List<NodeConnection> nodeConnections = new List<NodeConnection>();

            int nodecount = input.ReadInt32();
            for (int i = 0; i < nodecount; i++)
            {
                PositionedNode node = new PositionedNode();
                node.Name = input.ReadString();
                node.X = input.ReadSingle();
                node.Y = input.ReadSingle();
                node.Z = input.ReadSingle();

                //TODO, figure out a way to do the node linking after all nodes have been read
                int linkCount = input.ReadInt32();
                for (int n = 0; n < linkCount; n++)
                {
                    float cost = input.ReadSingle();
                    string nodeLinkingTo = input.ReadString();

                    nodeConnections.Add(new NodeConnection(node, cost, nodeLinkingTo));
                }

                network.AddNode(node);
            }

            // Now make the node connections
            foreach (NodeConnection nodeConnection in nodeConnections)
            {
                nodeConnection.PositionedNode.LinkToOneWay(network.FindByName(nodeConnection.NodeLinkingTo), nodeConnection.Cost);
            }

            return network;
        }
    }
}
