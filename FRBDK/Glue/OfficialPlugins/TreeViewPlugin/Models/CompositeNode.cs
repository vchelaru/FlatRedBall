using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.Models
{
    class CompositeNode : Node
    {
        public List<Node> Children { get; private set; }

        public CompositeNode()
        {
            Children = new List<Node>();
        }
    }
}
