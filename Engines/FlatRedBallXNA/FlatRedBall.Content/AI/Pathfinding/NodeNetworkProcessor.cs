using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace FlatRedBall.Content.AI.Pathfinding
{
    [ContentProcessor(DisplayName="Node Network - FlatRedBall")]
    public class NodeNetworkProcessor : ContentProcessor<NodeNetworkSave, NodeNetworkSave>
    {
        public override NodeNetworkSave Process(NodeNetworkSave input, ContentProcessorContext context)
        {
            return input;
        }
    }
}
