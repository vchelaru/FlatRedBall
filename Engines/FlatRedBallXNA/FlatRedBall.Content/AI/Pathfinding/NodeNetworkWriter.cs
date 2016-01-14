using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
#if XNA4
using TargetPlatform = Microsoft.Xna.Framework.Content.Pipeline.TargetPlatform;
#else
using TargetPlatform = Microsoft.Xna.Framework.TargetPlatform;
#endif

namespace FlatRedBall.Content.AI.Pathfinding
{
    [ContentTypeWriter]
    class NodeNetworkWriter : ContentTypeWriter<NodeNetworkSave>
    {
        protected override void Write(ContentWriter output, NodeNetworkSave value)
        {
            output.Write(value.PositionedNodes.Count);
            foreach (PositionedNodeSave node in value.PositionedNodes)
            {
                output.Write(node.Name);
                output.Write(node.X);
                output.Write(node.Y);
                output.Write(node.Z);

                output.Write(node.Links.Count);
                foreach (LinkSave link in node.Links)
                {
                    output.Write(link.Cost);
                    output.Write(link.NodeLinkingTo);
                }
            }
        }

        public override string GetRuntimeReader(TargetPlatform targetPlatform)
        {
            return typeof(FlatRedBall.Content.NodeNetworkReader).AssemblyQualifiedName;
        }
    }
}
