using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace FlatRedBall.Content.AI.Pathfinding
{
    [ContentImporter(".nntx", DisplayName="Node Network - FlatRedBall", DefaultProcessor="NodeNetworkProcessor")]
    public class NodeNetworkImporter : ContentImporter<NodeNetworkSave>
    {
        public override NodeNetworkSave Import(string filename, ContentImporterContext context)
        {
            return NodeNetworkSave.FromFile(filename);
        }
    }
}
