using System;
using System.Collections.Generic;
using System.Text;

namespace OfficialPlugins.TreeViewPlugin.Models
{

    public class TreeNodeState
    {
        public string Text { get; set; }

        public List<TreeNodeState> Children { get; set; } = new List<TreeNodeState>();

        public bool IsExpanded { get; set; }

        public override string ToString()
        {
            return $"{Text} expanded: {IsExpanded}";
        }
    }


}
